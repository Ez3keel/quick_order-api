FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY QuickOrder.slnx .
COPY src/Ordering/Ordering.Domain/Ordering.Domain.csproj src/Ordering/Ordering.Domain/
COPY src/Ordering/Ordering.Application/Ordering.Application.csproj src/Ordering/Ordering.Application/
COPY src/Ordering/Ordering.Infrastructure/Ordering.Infrastructure.csproj src/Ordering/Ordering.Infrastructure/
COPY src/Ordering/Ordering.Api/Ordering.Api.csproj src/Ordering/Ordering.Api/
COPY src/Shared/QuickOrder.Contracts/QuickOrder.Contracts.csproj src/Shared/QuickOrder.Contracts/
RUN dotnet restore src/Ordering/Ordering.Api/Ordering.Api.csproj

COPY src/Ordering/ src/Ordering/
COPY src/Shared/ src/Shared/
RUN dotnet publish src/Ordering/Ordering.Api/Ordering.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Ordering.Api.dll"]
