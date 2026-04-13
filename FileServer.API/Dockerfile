# Этап 1: Базовый образ для runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Этап 2: Сборка проекта
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем ТОЛЬКО файлы проектов для восстановления зависимостей
# Это кэшируется Docker'ом и ускоряет последующие сборки
COPY ["FileServer.API/FileServer.API.csproj", "FileServer.API/"]
COPY ["FileServer.Infrastructure/FileServer.Infrastructure.csproj", "FileServer.Infrastructure/"]
COPY ["FileServer.Domain/FileServer.Domain.csproj", "FileServer.Domain/"]

# Восстанавливаем NuGet пакеты
RUN dotnet restore "FileServer.API/FileServer.API.csproj"

# Копируем ВСЕ исходники (после восстановления, чтобы не сбрасывать кэш)
COPY . .

# Собираем проект
WORKDIR "/src/FileServer.API"
RUN dotnet build "FileServer.API.csproj" -c Release -o /app/build

# Этап 3: Публикация
FROM build AS publish
RUN dotnet publish "FileServer.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Этап 4: Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Создаем непривилегированного пользователя для безопасности
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "FileServer.API.dll"]