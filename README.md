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
docker compose up -d --build
```

Откройте [https://localhost:8443](https://localhost:8443). Для локального HTTPS используется самоподписанный сертификат.
