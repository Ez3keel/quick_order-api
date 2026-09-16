# Decisões de Arquitetura — QuickOrder

Este arquivo registra o *porquê* de cada decisão relevante, na ordem em que foram
tomadas. Cada entrada é adicionada conforme o projeto avança por fase.

---

## Fase 0 — Domínio

### 1. Microsserviços desde o início, não monolito-depois-split

QuickOrder nasce com processos separados por contexto delimitado: **Ordering**,
**Catalog** e **Delivery** (+ Notification, a partir da Fase 5). Cada um terá sua
própria API/worker, seu próprio schema de banco e seu próprio ciclo de deploy.

**Por quê:** o objetivo do projeto é praticar os problemas reais de sistemas
distribuídos (consistência eventual, comunicação assíncrona, falhas parciais), não só
separar camadas dentro de um processo. Começar com os limites de serviço já definidos
evita o retrabalho de "extrair" um módulo do monolito depois.

### 2. Sem *shared kernel* de domínio entre serviços

Cada domínio (`Ordering.Domain`, `Catalog.Domain`, `Delivery.Domain`) tem sua própria
cópia de tipos base (`Entity<TId>`, `AggregateRoot<TId>`, `IDomainEvent`, `Money`).
Não existe um projeto `Shared.Domain` referenciado por todos.

**Por quê:** um shared kernel de domínio cria acoplamento de deploy entre serviços que
deveriam ser independentes — mudar `Money` pra atender o Catalog não pode forçar
recompilar/reimplantar o Ordering. A duplicação de ~30 linhas de tipos base é um preço
baixo comparado a esse acoplamento. O que *vai* ser compartilhado futuramente (Fase 3)
são **contratos de integração** (os DTOs dos eventos publicados na fila), em um pacote
enxuto e versionado à parte — isso é diferente de compartilhar o modelo de domínio.

### 3. Identidades fortemente tipadas (`OrderId`, `RestaurantId`, `CourierId`, ...)

Em vez de `Guid` cru circulando pelo código, cada agregado tem um `readonly record
struct` próprio (`OrderId`, `MenuItemId`, `CourierId`, `DeliveryAssignmentId`).

**Por quê:** evita o bug clássico de trocar `orderId` por `courierId` sem o compilador
acusar, já que ambos seriam `Guid` no C# comum. Sem overhead de runtime relevante
(é um struct sobre um `Guid`).

### 4. Máquina de estado do pedido dentro do próprio agregado `Order`

`Order.TransitionTo` valida contra uma tabela de transições permitidas
(`Received → Preparing → AwaitingCourier → OutForDelivery → Delivered`, com
`Cancelled` permitido em qualquer ponto antes de `OutForDelivery`). Uma transição
inválida lança `InvalidOrderStateTransitionException` — nunca falha silenciosamente.

**Por quê:** o pedido é o agregado mais crítico do sistema; sua máquina de estado
precisa ser impossível de violar por fora. Modelar as transições como dados
(`Dictionary<OrderStatus, OrderStatus[]>`) em vez de um `switch` espalhado facilita
tanto testar exaustivamente quanto auditar visualmente quais transições existem.

### 5. Cardápio como agregado próprio (`Restaurant`), não acoplado a pedidos

`Restaurant` é o agregado raiz; `MenuItem` é uma entidade filha cujo construtor e
mutadores são `internal` — só pode ser criado/alterado através de `Restaurant`
(`AddMenuItem`, `ChangePrice`, `SetItemAvailability`).

**Por quê:** preço de item muda com muito mais frequência do que dados do
restaurante em si, e mudar de agregado facilita o cache-aside no Redis que vamos
implementar no Catalog (Fase 1): o `MenuItemPriceChangedEvent` já existe desde a
Fase 0 pensando nessa invalidação futura.

### 6. `Order` guarda uma *cópia* (snapshot) do item do cardápio, não uma referência

`OrderItem` armazena `ProductName` e `UnitPrice` copiados no momento do pedido, e não
uma referência a `MenuItemId` resolvida em tempo de leitura.

**Por quê:** se o restaurante mudar o preço de um item às 19h05, um pedido feito às
19h00 não pode "mudar de preço" retroativamente. Isso também é o que torna o Ordering
independente do Catalog em tempo de leitura — a consulta de preço acontece uma vez, no
momento de criar o pedido (via chamada HTTP ao Catalog, Fase 2), e depois o pedido é
autossuficiente.

### 7. Atribuição de entregador: fila com consumidor único, não lock distribuído

Diferente do problema do assento no TicketFlow (resolvido com lock distribuído no
Redis), aqui a serialização da atribuição vem de um **worker com um único consumer**
processando a fila de pedidos aguardando entregador (`OrderReadyForAssignmentEvent`).
Como só existe um consumer ativo por vez, não há corrida a resolver com lock — a
ordem de processamento da fila já garante isso.

**Por quê:** é uma técnica diferente pra um problema com a mesma forma (dois eventos
concorrentes disputando o mesmo recurso escasso), o que vale a pena comparar
explicitamente com o TicketFlow. Também reflete uma dor real: sistemas de delivery
tipicamente processam atribuição como um pipeline sequencial (nem sempre precisa ser
paralelo), enquanto reserva de assento numa venda de ingressos é inerentemente
paralela e de alto throughput.

`Courier.Reserve()` e `Courier.Release()` continuam validando a invariante
(`CourierNotAvailableException` se o status não for `Available`) mesmo sem lock
distribuído — a invariante do agregado se defende sozinha, a serialização é só uma
otimização de quem chama.

### 8. `DeliveryAssignment` referencia `Order` e `Courier` só por Id

`DeliveryAssignment` guarda `Guid OrderId` (tipo cru, porque `Order` pertence a outro
serviço/bounded context) e `CourierId` (tipo forte, porque `Courier` é um agregado
*deste mesmo* serviço, mas ainda assim uma referência por Id, não por composição).

**Por quê:** regra padrão de DDD — um agregado nunca carrega outro agregado inteiro
dentro de si, só a identidade. Isso mantém as transações pequenas (criar uma
`DeliveryAssignment` não travamos `Courier` nem `Order` na mesma transação) e é
exatamente o que vai permitir que `Courier.Reserve()` e `DeliveryAssignment.Create()`
sejam duas operações do mesmo worker, não uma transação distribuída.

### 9. Outbox Pattern como diferencial técnico principal (chega na Fase 3)

Decisão já tomada, vale registrar o motivo aqui: `OrderPlacedEvent`,
`OrderReadyForAssignmentEvent`, `MenuItemPriceChangedEvent`, `CourierAssignedEvent` e
`DeliveryCompletedEvent` já existem como eventos de domínio desde a Fase 0. A partir da
Fase 3, cada serviço vai persistir esses eventos numa tabela `OutboxMessages` na mesma
transação que salva o agregado, e um publisher em background os manda pro RabbitMQ.
Consumers serão idempotentes (`ProcessedMessageId` único por handler).

**Por quê:** eliminar o cenário "salvei a entidade mas a mensagem se perdeu" (ou o
inverso, mensagem duplicada) é o problema mais citado em sistemas de mensageria de
produção e o principal diferencial técnico que queremos destacar no portfólio.
