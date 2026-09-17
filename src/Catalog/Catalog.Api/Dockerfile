# Build context is the repository root (docker-compose.yml sets it that way) — every
# service's Dockerfile needs the whole solution tree available at build time because
# `dotnet publish` walks the ProjectReference graph (Catalog.Api -> Application ->
# Infrastructure -> Domain, plus the shared QuickOrder.Contracts), not just its own folder.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY QuickOrder.slnx .
COPY src/Catalog/Catalog.Domain/Catalog.Domain.csproj src/Catalog/Catalog.Domain/
COPY src/Catalog/Catalog.Application/Catalog.Application.csproj src/Catalog/Catalog.Application/
COPY src/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj src/Catalog/Catalog.Infrastructure/
COPY src/Catalog/Catalog.Api/Catalog.Api.csproj src/Catalog/Catalog.Api/
COPY src/Shared/QuickOrder.Contracts/QuickOrder.Contracts.csproj src/Shared/QuickOrder.Contracts/
RUN dotnet restore src/Catalog/Catalog.Api/Catalog.Api.csproj

COPY src/Catalog/ src/Catalog/
COPY src/Shared/ src/Shared/
RUN dotnet publish src/Catalog/Catalog.Api/Catalog.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
