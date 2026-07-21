# BookingService API — ASP.NET Core Web API

RESTful API сервиса бронирования (Booking) на базе ASP.NET Core.

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

* Swagger UI - `/swagger/index.html`

API предоставляет операции для работы с сервисом бронирования:

* **POST** `/api/events/{id}/book` — создать бронь по ID события

* **GET** `/api/bookings/{id}` — получить бронь по ID

* **DELETE** `/api/bookings/{id}/cancel` — отмена брони

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

2. Соберите проект:
   ```bash
   dotnet build
   ```

3. Запустите проект:
   ```bash
   dotnet run --project Presentation/BookingPresentation.csproj
   ```


4. Схема управляется миграциями EF Core:
   ```bash
   dotnet ef migrations add BookingProjectDB --project Infrastructure --startup-project Presentation
   dotnet ef database update --project Infrastructure --startup-project Presentation
   dotnet ef database update <PreviousMigration> --project Infrastructure --startup-project Presentation
   dotnet ef migrations remove --project Infrastructure --startup-project Presentation
   ```