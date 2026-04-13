Write-Host "🚀 Запуск File Server..." -ForegroundColor Green

# Проверяем наличие Docker
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Docker не установлен!" -ForegroundColor Red
    exit 1
}

# Останавливаем старые контейнеры если есть
Write-Host "📦 Остановка старых контейнеров..." -ForegroundColor Yellow
docker compose down

# Запускаем все сервисы
Write-Host "🐳 Запуск контейнеров..." -ForegroundColor Yellow
docker compose up -d

# Ждем готовности
Write-Host "⏳ Ожидание готовности сервисов..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Проверяем статус
Write-Host "✅ Статус контейнеров:" -ForegroundColor Green
docker compose ps

Write-Host ""
Write-Host "🎉 Сервер запущен!" -ForegroundColor Green
Write-Host "📱 API: http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "💾 MinIO Console: http://localhost:9001 (minioadmin/minioadmin)" -ForegroundColor Cyan
Write-Host "🗄️  PostgreSQL: localhost:5432 (fileuser/secretpass123)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Для остановки выполните: docker compose down" -ForegroundColor Yellow