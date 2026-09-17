FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY QuickOrder.slnx .
COPY src/Delivery/Delivery.Domain/Delivery.Domain.csproj src/Delivery/Delivery.Domain/
COPY src/Delivery/Delivery.Application/Delivery.Application.csproj src/Delivery/Delivery.Application/
COPY src/Delivery/Delivery.Infrastructure/Delivery.Infrastructure.csproj src/Delivery/Delivery.Infrastructure/
COPY src/Delivery/Delivery.Api/Delivery.Api.csproj src/Delivery/Delivery.Api/
COPY src/Shared/QuickOrder.Contracts/QuickOrder.Contracts.csproj src/Shared/QuickOrder.Contracts/
RUN dotnet restore src/Delivery/Delivery.Api/Delivery.Api.csproj

COPY src/Delivery/Delivery.Domain/ src/Delivery/Delivery.Domain/
COPY src/Delivery/Delivery.Application/ src/Delivery/Delivery.Application/
COPY src/Delivery/Delivery.Infrastructure/ src/Delivery/Delivery.Infrastructure/
COPY src/Delivery/Delivery.Api/ src/Delivery/Delivery.Api/
COPY src/Shared/ src/Shared/
RUN dotnet publish src/Delivery/Delivery.Api/Delivery.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Delivery.Api.dll"]
