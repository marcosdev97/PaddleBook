FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NotificationService.Api/NotificationService.Api.csproj", "NotificationService.Api/"]
# Si este proyecto referencia otros, añade sus .csproj igual que en la API principal

RUN dotnet restore "NotificationService.Api/NotificationService.Api.csproj"

COPY . .

WORKDIR "/src/NotificationService.Api"
RUN dotnet publish "NotificationService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]
