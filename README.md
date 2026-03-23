# Task Manager

Тестовое задание: настольное приложение для управления задачами в команде.

## Как запустить (из исходников)
- `dotnet run`

## Технологии
- C# (.NET 10)
- Avalonia UI
- Microsoft.Extensions.DependencyInjection
- SQLite
- Архитектура MVVM

## Возможности
- MVVM (чёткое разделение `Views` / `ViewModels` / `Models`)
- Асинхронные операции с SQLite через `async/await`
- Авторизация по логину/паролю (используется `TaskCompletionSource` для возврата результата)
- Роли: `User`, `Manager`, `Admin` и разграничение прав
- Admin-панель: управление пользователями и просмотр всех задач

## Роли и тестовые учетные записи
При первом запуске автоматически создаются пользователи:
- `admin` / `admin` (роль `Admin`)
- `manager` / `manager` (роль `Manager`)
- `user` / `user` (роль `User`)
