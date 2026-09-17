FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY QuickOrder.slnx .
COPY src/Identity/Identity.Domain/Identity.Domain.csproj src/Identity/Identity.Domain/
COPY src/Identity/Identity.Application/Identity.Application.csproj src/Identity/Identity.Application/
COPY src/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj src/Identity/Identity.Infrastructure/
COPY src/Identity/Identity.Api/Identity.Api.csproj src/Identity/Identity.Api/
RUN dotnet restore src/Identity/Identity.Api/Identity.Api.csproj

COPY src/Identity/ src/Identity/
RUN dotnet publish src/Identity/Identity.Api/Identity.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
