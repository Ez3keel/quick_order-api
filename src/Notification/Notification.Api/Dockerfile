FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY QuickOrder.slnx .
COPY src/Notification/Notification.Api/Notification.Api.csproj src/Notification/Notification.Api/
COPY src/Shared/QuickOrder.Contracts/QuickOrder.Contracts.csproj src/Shared/QuickOrder.Contracts/
RUN dotnet restore src/Notification/Notification.Api/Notification.Api.csproj

COPY src/Notification/ src/Notification/
COPY src/Shared/ src/Shared/
RUN dotnet publish src/Notification/Notification.Api/Notification.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Notification.Api.dll"]
