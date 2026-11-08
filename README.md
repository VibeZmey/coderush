# CodeRush

CodeRush — платформа для решения задач по программированию. Можно тренироваться с таймером, проверять решения на тестах и соревноваться в рейтинговых дуэлях.

Стек — React, TypeScript, ASP.NET Core, PostgreSQL и Judge0.

## Локальный запуск

Из корня репозитория в PowerShell:

```powershell
cd src
Get-ChildItem -Recurse -File -Filter '*.example' |
  Where-Object { $_.FullName -notmatch '[\\/](node_modules|bin|obj|\.idea)[\\/]' } |
  ForEach-Object {
    $target = $_.FullName -replace '\.example$', ''
    if (-not (Test-Path -LiteralPath $target)) {
      Copy-Item -LiteralPath $_.FullName -Destination $target
    }
  }
# Заполните MINIO_ROOT_PASSWORD и MINIO_PROFILE_SECRET_KEY в src/.env
# разными случайными значениями перед первым запуском.
docker compose up -d --build
```

Откройте [https://localhost:8443](https://localhost:8443). Для локального HTTPS используется самоподписанный сертификат.

## Изображения профиля

Compose использует сборку MinIO от Coollabs с фиксированным тегом релиза.

Владелец аккаунта может загрузить аватар и фон в своём профиле. Поддерживаются статичные JPEG, PNG и WebP до 5 МиБ. Сервер сохраняет обработанные изображения в закрытом бакете MinIO. Данные переживают пересоздание контейнера благодаря тому `minio_data`.

Настройки MinIO находятся в `src/.env`. Если локальные файлы уже существуют, перенесите блок `MINIO_*` из `src/.env.example` и обновите `src/docker-compose.yml` по шаблону. Для генерации каждого пароля можно использовать `[Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(24))` в PowerShell 7.