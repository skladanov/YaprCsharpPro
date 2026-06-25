# WebProject API — ASP.NET Core Web API

RESTful API для управления событиями (Events) на базе ASP.NET Core.

Приложение состоит из 4х слоев:
Domain — доменные сущности: event, booking; доменные исключения.
Application — use cases, сервисы, интерфейсы репозитория, DTOs.
Infrastructure — реализации портов: репозитории, DbContext, конфигурации базы, миграции.
Presentation — контроллеры (эндпоинты), HTTP-маппинг, регистрация зависимостей.

## Технологии


* ASP.NET Core 9.0+
* Swagger/OpenAPI для документации
* PostgreSQL
* EF Core
* Docker

## Функциональность

API предоставляет CRUD‑операции для работы с событиями:

* **GET** `/api/events?[title=...]&[from=...]&[to=...]&[page=...]&[pageSize=...]` — получить события
   - title (опц.) — фильтр по названию события;
   - from (опц.) — дата начала (YYYY‑MM‑DD);
   - to (опц.) — дата окончания (YYYY‑MM‑DD);
   - page (опц., по умолчанию: 1) — номер страницы;
   - pageSize (опц., по умолчанию: 10) — количество элементов на странице.

* **GET** `/api/events/{id}` — получить событие по ID

* **POST** `/api/events` — создать новое событие

* **PUT** `/api/events/{id}` — обновить событие

* **DELETE** `/api/events/{id}` — удалить событие

* Swagger UI - `/swagger/index.html`

API предоставляет операции для работы с сервисом бронирования:

* **POST** `/api/events/{id}/book` — создать бронь по ID события

* **GET** `/api/bookings/{id}` — получить бронь по ID

Формат ответа при ошибках соответствует стандарту RFC7807 ProblemDetails

## Быстрый старт


### Предварительные требования

* .NET 9.0 SDK
* IDE (Visual Studio, VS Code или Rider)

### Запуск проекта

1. Клонируйте репозиторий:
   ```bash
   git clone https://github.com/skladanov/YaprCsharpPro.git
   cd YaprCsharpPro
   ```

2. Соберите и запустите проект:
   ```bash
   dotnet build
   dotnet run --project Presentation/WebProject.csproj
   ```

3. Запустите тесты (для интеграциооных тестов нужен Docker):
   ```bash
   dotnet test
   ```

4. Схема управляется миграциями EF Core:
   ```bash
   dotnet ef migrations add WebProjectDB --project Infrastructure --startup-project Presentation
   dotnet ef database update --project Infrastructure --startup-project Presentation
   dotnet ef database update <PreviousMigration> --project Infrastructure --startup-project Presentation
   dotnet ef migrations remove --project Infrastructure --startup-project Presentation
   ```