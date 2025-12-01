# Imagen base para runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Imagen para build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos los .csproj de todos los proyectos que referencia la API
COPY ["PaddleBook.Api/PaddleBook.Api.csproj", "PaddleBook.Api/"]
COPY ["PaddleBook.Application/PaddleBook.Application.csproj", "PaddleBook.Application/"]
COPY ["PaddleBook.Domain/PaddleBook.Domain.csproj", "PaddleBook.Domain/"]
COPY ["PaddleBook.Infrastructure/PaddleBook.Infrastructure.csproj", "PaddleBook.Infrastructure/"]

# Restauramos dependencias
RUN dotnet restore "PaddleBook.Api/PaddleBook.Api.csproj"

# Copiamos el resto del código
COPY . .

# Publicamos la API
WORKDIR "/src/PaddleBook.Api"
RUN dotnet publish "PaddleBook.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaddleBook.Api.dll"]
