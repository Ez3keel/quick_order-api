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

---

## Fase 1 — Catalog API

### 10. MediatR + FluentValidation com pipeline behavior, não handlers "gordos"

Cada caso de uso é um `IRequest`/`IRequestHandler` do MediatR (`RegisterRestaurant`,
`AddMenuItem`, `ChangeMenuItemPrice`, `GetRestaurantById`, `ListRestaurants`). A
validação roda automaticamente antes do handler via um `ValidationBehavior<TRequest,
TResponse>` registrado como `IPipelineBehavior`, não como uma chamada manual de
`validator.Validate()` dentro de cada handler.

**Por quê:** é o padrão mais comum em APIs .NET profissionais (CQRS leve com MediatR),
e o pipeline behavior evita que cada handler repita o mesmo boilerplate de validação —
um handler só lança `ValidationException` se algo além de "formato do request" falhar
(nesse caso, nenhum: toda validação de formato já é responsabilidade do validator).

### 11. Exceção de "não encontrado" é da Application, não do Domain

`RestaurantNotFoundException` vive em `Catalog.Application`, não em `Catalog.Domain`.
As exceções de domínio (`DuplicateMenuItemException`, `MenuItemNotFoundException`)
continuam no Domain porque representam invariantes de negócio; "esse id não existe no
banco" não é uma invariante, é uma falha de infraestrutura/aplicação.

**Por quê:** mantém o Domain sem saber nada sobre persistência ou sobre o conceito de
"requisição HTTP que falhou". A API tem um único middleware
(`ExceptionHandlingMiddleware`) que mapeia todas as exceções (domínio + aplicação)
para o `ProblemDetails`/status HTTP certo, então handlers de comando nunca retornam
`null`/`bool` para sinalizar erro — sempre lançam a exceção específica.

### 12. `MenuItem` mapeado como entidade própria no EF Core, não como Owned Type

A intenção original (documentada no item 5) era que `MenuItem` fosse uma entidade
filha inacessível fora de `Restaurant` — isso continua verdade no nível do domínio
(construtor e mutadores `internal`). Mas no mapeamento EF Core, `MenuItem` é
configurado como uma entidade regular (`IEntityTypeConfiguration<MenuItem>` própria,
com FK sombra `RestaurantId` via `HasMany().WithOne()`), não via `OwnsMany`.

**Por quê:** `Money` é um `readonly record struct` (Decisão de domínio: value objects
como structs, sem alocação extra). As APIs `OwnsOne`/`OwnsMany` do EF Core exigem que o
tipo dependente seja uma classe (`where TDependentEntity : class`) — não aceitam
struct. A alternativa do EF Core para value objects em struct é `ComplexProperty`
(EF Core 8+), mas essa API só está disponível em `EntityTypeBuilder<T>` de uma
entidade de primeira classe, não em `OwnedNavigationBuilder<TOwner,TDependent>`. Como
`MenuItem.Price` é `Money`, `MenuItem` precisou virar uma entidade "de primeira
classe" para poder usar `ComplexProperty` no mapeamento do preço. Na prática isso é
invisível para quem consome `Restaurant`: a API pública do agregado não mudou, e o
repositório sempre usa `.Include(r => r.Menu)` para carregar o cardápio junto —
exatamente o comportamento que um Owned Type daria de graça.

### 13. Cache-aside no Redis: query lê o cache, comando invalida (nunca atualiza)

`GetRestaurantByIdQueryHandler` tenta o Redis primeiro; num miss, busca no Postgres e
popula o cache com TTL de 10 minutos. `AddMenuItemCommandHandler` e
`ChangeMenuItemPriceCommandHandler` chamam `IMenuCache.InvalidateAsync` depois de
salvar — eles nunca escrevem o novo valor diretamente no cache.

**Por quê:** invalidar (deletar a chave) é mais simples e mais seguro que atualizar o
cache no caminho de escrita — evita o cache ficar com um valor parcialmente
consistente se a serialização do DTO um dia divergir da escrita no banco. O próximo
`GET` naturalmente repopula com o dado fresco. O TTL de 10 minutos é uma rede de
segurança para o caso (raro, mas possível em qualquer sistema distribuído) de uma
invalidação se perder.

### 14. Migração gerada e aplicada via `dotnet ef` contra `Catalog.Infrastructure`

`CatalogDbContextFactory` (`IDesignTimeDbContextFactory<CatalogDbContext>`) permite
gerar migrations direto no projeto de Infrastructure (`dotnet ef migrations add
--project Catalog.Infrastructure --startup-project Catalog.Api`) sem precisar subir a
API. Em desenvolvimento, `Program.cs` aplica `dbContext.Database.MigrateAsync()`
automaticamente ao iniciar — isso é aceitável para o ambiente de dev/portfolio, mas
será substituído por uma migration explícita no pipeline de deploy quando chegarmos à
containerização (Fase 8).

### 15. Testes de integração com Testcontainers reais (Postgres + Redis), não mocks

`CatalogApiFactory : WebApplicationFactory<Program>` sobe um Postgres e um Redis reais
em containers Docker por execução de teste, aplica as migrations, e os 7 testes de
`RestaurantsEndpointsTests` batem na API via `HttpClient` de ponta a ponta (registro,
cardápio, mudança de preço, e a prova de que a invalidação do cache funciona: o
segundo `GET` depois de um `PUT` de preço reflete o valor novo).

**Por quê:** um mock de `IRestaurantRepository` provaria que o *handler* chama os
métodos certos, mas não provaria que o mapeamento EF Core (FK sombra, `ComplexProperty`
do `Money`, `Include` do cardápio) realmente funciona contra um Postgres de verdade —
foi exatamente um desses três detalhes (`Include` faltando) que um teste de integração
pegou e um teste unitário jamais pegaria.

**Detalhe de implementação que vale registrar:** ao sobrescrever a connection string
nos testes, `builder.UseSetting("ConnectionStrings:Postgres", ...)` funciona;
`builder.ConfigureAppConfiguration(...).AddInMemoryCollection(...)` não tem efeito
neste projeto, porque o host do ASP.NET Core (com `Program.cs` de top-level statements)
já constrói o `IConfiguration` final antes desse hook rodar. `UseSetting` escreve
direto na fonte de configuração que o `WebApplicationFactory` controla, então sempre
tem prioridade.

---

## Fase 2 — Ordering API

### 16. Ordering valida contra o Catalog via HTTP síncrono só no momento de criar o pedido

`PlaceOrderCommandHandler` chama `ICatalogClient.GetRestaurantAsync` (implementado por
`HttpCatalogClient`, um `HttpClient` tipado sem retry ainda) pra buscar o restaurante,
conferir que está aberto, e validar cada item pedido contra o cardápio antes de montar
os `OrderItem` (snapshot de nome/preço). Esse é o único ponto do sistema onde Ordering
depende de uma resposta síncrona do Catalog — todo o resto do ciclo de vida do pedido
(preparo, atribuição, entrega) é interno ao agregado `Order` e não faz nenhuma outra
chamada de rede.

**Por quê:** o cliente precisa saber, na hora, se o item existe/está disponível e
qual o preço atual — isso é inerentemente uma pergunta "agora", não algo que dá pra
responder de forma assíncrona sem piorar a experiência de quem está fazendo o pedido.
Não tem política de retry ainda de propósito: reproduzir a mesma falta de resiliência
que a maioria dos projetos tem no primeiro rascunho, e só then introduzir Polly na
Fase 7 como melhoria deliberada e comparável (antes/depois), em vez de já nascer
"resiliente" sem nunca termos visto o problema que a resiliência resolve.

### 17. `RestaurantUnavailableException`/`MenuItemUnavailableException` viram 409, não 404

Quando o Catalog não conhece o restaurante, está fechado, ou o item não existe/está
indisponível, a Ordering API responde `409 Conflict`, não `404 Not Found`.

**Por quê:** o recurso que o cliente está tentando criar é o **pedido**, não o
restaurante — a URL chamada (`POST /api/orders`) sempre existe. Um 404 sugeriria que a
própria rota está errada; um 409 comunica corretamente "sua requisição é válida, mas o
estado atual do sistema não permite completá-la agora" (restaurante fechado é
exatamente esse tipo de conflito temporário).

### 18. `OrderItem` (value object sem identidade) também virou entidade no mapeamento EF

Mesmo problema do item 12 (Catalog), mesma solução: `OrderItem.UnitPrice` é `Money`
(struct), então `OrderItem` precisou de uma chave substituta (`Id` sombra,
`ValueGeneratedOnAdd`) e configuração própria (`OrderItemConfiguration`) pra poder usar
`ComplexProperty`. `OrderItem` continua sendo, no domínio, um `record` sem identidade
própria — a chave é 100% um detalhe de storage, invisível pra `Order`.

Efeito colateral que vale registrar: como `OrderItem` é um `record` sem construtor
vazio, o EF Core tentou usar **constructor binding** (materializar chamando o
construtor público direto) e falhou, porque não sabe construir um `Money` (tipo
complexo) como argumento de construtor de outra entidade. A correção foi a mesma do
`MenuItem`: adicionar um construtor privado sem parâmetros, fazendo o EF cair para
materialização via reflection sobre os backing fields das propriedades `{ get; }`.

### 19. Testes de integração do Ordering usam um Postgres real, mas um `ICatalogClient` falso

`OrderingApiFactory` sobe um Postgres real via Testcontainers (a infraestrutura que
este serviço *possui*), mas substitui `ICatalogClient` por `FakeCatalogClient` — um
dicionário em memória que cada teste popula com o restaurante/cardápio que precisa —
via `ConfigureTestServices` + `RemoveAll<ICatalogClient>()`.

**Por quê:** o Catalog é outro serviço, com seu próprio ciclo de vida e sua própria
suíte de integração (Fase 1). Subir o processo do Catalog inteiro dentro do teste do
Ordering criaria uma dependência de teste entre dois serviços que deveriam ser
deployáveis e testáveis independentemente — exatamente o acoplamento que a escolha por
microsserviços (item 1) tenta evitar. O limite certo pra fake vs. real é "esse é um
processo que este serviço não possui": Postgres do Ordering é real porque é dele;
Catalog é fake porque pertence a outro serviço.

---

## Fase 3 — Outbox Pattern + RabbitMQ

### 20. `QuickOrder.Contracts`: a única exceção deliberada à regra de "sem shared kernel"

Criamos um projeto novo, `src/Shared/QuickOrder.Contracts`, com os records dos eventos
de integração (`OrderReadyForAssignmentIntegrationEvent`,
`MenuItemPriceChangedIntegrationEvent`, etc.) referenciado por Catalog.Infrastructure e
Ordering.Infrastructure (e, nas próximas fases, por Delivery e Notification também).

**Por quê:** o item 2 (Fase 0) dizia que não haveria shared kernel de *domínio* entre
serviços — isso continua verdade, `Order`, `Restaurant`, `Money` etc. nunca são
compartilhados. Mas o **formato da mensagem publicada na fila** precisa ser idêntico
entre quem produz e quem consome; se cada serviço serializasse seu próprio evento de
domínio diretamente, qualquer refactor interno (renomear uma propriedade do domínio,
por exemplo) quebraria silenciosamente todo consumidor. Um pacote de contratos é
exatamente o que uma definição de API pública (tipo um `.proto` do gRPC ou um schema
Avro) faria em qualquer sistema de mensageria real — é a interface, não a
implementação, que é compartilhada.

### 21. Evento de domínio ≠ evento de integração — sempre existe um *mapper* entre os dois

`CatalogIntegrationEventMapper`/`OrderingIntegrationEventMapper` traduzem
`MenuItemPriceChangedEvent` (interno, `Catalog.Domain`) para
`MenuItemPriceChangedIntegrationEvent` (público, `QuickOrder.Contracts`) — nunca
serializamos o evento de domínio diretamente.

**Por quê:** o evento de domínio é livre pra mudar de forma junto com o agregado (é
código interno); o evento de integração é uma API pública com consumidores externos e
precisa ser versionado com mais cuidado. Separar os dois desde o início evita a
armadilha comum de "vazar" o modelo de domínio pra fora do serviço.

### 22. Outbox: captura de eventos dentro do `SaveChangesAsync` do próprio `DbContext`

`CatalogDbContext`/`OrderingDbContext` sobrescrevem `SaveChangesAsync`: antes de
delegar pra `base.SaveChangesAsync`, percorrem o `ChangeTracker` procurando entidades
`IHasDomainEvents` com eventos pendentes, mapeiam cada evento pra uma linha de
`OutboxMessage` (adicionada ao mesmo `DbContext`), e só então chamam
`base.SaveChangesAsync`.

**Por quê:** o EF Core já envolve uma chamada de `SaveChangesAsync` inteira numa única
transação implícita — não precisamos abrir uma transação manual. Gravar o agregado e
a mensagem de outbox na mesma chamada garante atomicidade "de graça": ou os dois
persistem, ou nenhum persiste. `IHasDomainEvents` é uma interface pequena que
`AggregateRoot<TId>` já implementa (ela só expõe o que a classe já tinha), então
capturar eventos de *qualquer* agregado não exige que o `DbContext` conheça tipos
concretos como `Restaurant` ou `Order`.

### 23. `OutboxPublisher`: um `BackgroundService` com *dois* níveis de retry

O publisher faz polling da tabela Outbox a cada 2s e publica no RabbitMQ. Existem dois
mecanismos de retentativa deliberadamente separados:

1. **Por mensagem** (`OutboxMessage.MarkFailed`): se publicar uma mensagem específica
   falhar, ela ganha um `NextAttemptAtUtc` com backoff exponencial (2s, 4s, 8s...) e
   um `RetryCount`; depois de `MaxRetries` (5), fica "presa" com o erro registrado em
   vez de bloquear as mensagens seguintes pra sempre (um poison-message guard simples,
   sem precisar de uma DLQ de verdade só pro publisher).
2. **Do laço inteiro** (`ExecuteAsync`): se a conexão com o RabbitMQ cair no meio
   (não só falhar uma mensagem, mas o laço inteiro morrer), o `ExecuteAsync` reconecta
   do zero com o mesmo backoff exponencial, em vez de deixar a exceção subir.

**Por quê o nível 2 importa mais do que parece:** por padrão, o generic host do
ASP.NET Core **derruba a aplicação inteira** se o `ExecuteAsync` de um
`BackgroundService` lançar uma exceção não tratada
(`HostOptions.BackgroundServiceExceptionBehavior = StopHost`, o padrão desde o .NET
6). Isso mordeu a gente ao vivo: os testes de integração do Catalog começaram a
falhar com `ObjectDisposedException` no `TestServer` — não porque a API quebrou, mas
porque o RabbitMQ do teste demorou a aceitar a conexão, o publisher lançou, e o host
inteiro (API incluída) foi abaixo silenciosamente. A correção foi envolver **todo** o
corpo do `ExecuteAsync` (conectar, declarar exchange, laço de publicação) num retry
externo — não só a conexão inicial. Lição prática: qualquer `BackgroundService` que
fale com infraestrutura externa precisa ser resiliente à própria falha, porque o custo
de deixar vazar é a aplicação inteira cair, não só aquele serviço.

### 24. Routing key = nome do tipo do evento, exchange topic por serviço

`RoutingKey.For<TEvent>()` (em `QuickOrder.Contracts`) usa `typeof(TEvent).Name` como
routing key — `"catalog.events"` e `"ordering.events"` são exchanges do tipo *topic*,
uma por serviço produtor.

**Por quê:** manter a lógica de nomeação num único lugar (o pacote de contratos)
garante que quem publica e quem assina (Fase 4+) derivem a mesma string sem precisar
copiar/colar um "nome mágico". Exchanges topic (em vez de fanout ou direct) deixam a
porta aberta pra um consumidor futuro assinar só um subconjunto de eventos por
padrão de routing key, sem exigir uma exchange nova por tipo de evento.
