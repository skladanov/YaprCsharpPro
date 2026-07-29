# WebProject API — ASP.NET Core Web API

## Технологии

* ASP.NET Core 9.0+
* Swagger/OpenAPI для документации
* PostgreSQL
* EF Core
* Docker
* Kafka
* Redis

##  Приложение состоит из 4х микросервисов:

### EventService — для управления событиями на базе ASP.NET Core

#### API предоставляет CRUD‑операции для работы с событиями:

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

* Формат ответа при ошибках соответствует стандарту RFC7807 ProblemDetails

#### Стратегия кэширования (Caching Strategy)

1. Кэш отдельных событий (events:{id})
Механизм: Cache-Aside + Explicit Invalidation.
Чтение: При запросе GET сначала проверяется Redis. При промахе — чтение из PostgreSQL и прогрев кэша.
Запись: При обновлении/удалении события (включая обработку Kafka-событий о бронировании) ключ явно удаляется (DEL). Следующий запрос автоматически прогреет кэш свежими данными.
TTL: 1 час. Используется как страховка от рассинхронизации.

2. Кэш топ‑10 событий (events:top10)
Механизм: TTL-based caching.
Обоснование: Это агрегированный рейтинг, критическая актуальность не требуется. Частая инвалидация при каждом бронировании создала бы избыточную нагрузку на БД.
TTL: 10 минут. Балансирует между актуальностью и нагрузкой.

3. Значения TTL задаются в appsettings.json:

"RedisCache": {
  "EventByIdTtl": "01:00:00",
  "Top10Ttl": "00:10:00"
}

4. Обработка сбоев
При недоступности Redis все операции кэширования считаются промахами или игнорируются. Сервис продолжает работать, используя только PostgreSQL (graceful degradation). Логирование ошибок Redis включено.


### BookingService - для управления бронированием на базе ASP.NET Core

API предоставляет операции для работы с сервисом бронирования:

* **POST** `/api/events/{id}/book` — создать бронь по ID события

* **GET** `/api/bookings/{id}` — получить бронь по ID

* **DELETE** `/api/bookings/{id}/cancel` — отмена брони

* Swagger UI - `/swagger/index.html`

Формат ответа при ошибках соответствует стандарту RFC7807 ProblemDetails


### UserService - для авторизации пользователей на базе ASP.NET Core

API предоставляет операции для работы с сервисом авторизации:

* **POST** `/api/auth/register` — регистрация пользователя с ролью User или Admin

* **POST** `/api/auth/login` — авторизация, получение JWT-токена


### Shared.Contracts - динамическа библиотека общих типов и контрактов
 

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

2. Соберите проект Shared.Contracts:
   ```bash
   dotnet build Shared.Contracts
   ```

3. Соберите сервисы EventService, BookingService, UserService:
   ```bash
   dotnet build EventService
   dotnet build BookingService
   dotnet build UserService
   ```

4. Запустите DB + Kafka:
   ```bash
   docker-compose up -d
   ```

5. Запустите сервисы:
   ```bash
   dotnet run --project UserService/Presentation/UserPresentation.csproj
   dotnet run --project EventService/Presentation/EventPresentation.csproj
   dotnet run --project BookingService/Presentation/BookingPresentation.csproj
   ```

6. Схема управляется миграциями EF Core

   EventService:
   ```bash
   dotnet ef migrations add EventServiceDB --project EventService/Infrastructure --startup-project EventService/Presentation
   dotnet ef database update --project EventService/Infrastructure --startup-project EventService/Presentation
   dotnet ef database update <PreviousMigration> --project EventService/Infrastructure --startup-project EventService/Presentation
   dotnet ef migrations remove --project EventService/Infrastructure --startup-project EventService/Presentation
   ``` 

   BookingService:
   ```bash
   dotnet ef migrations add BookingServiceDB --project BookingService/Infrastructure --startup-project BookingService/Presentation
   dotnet ef database update --project BookingService/Infrastructure --startup-project BookingService/Presentation
   dotnet ef database update <PreviousMigration> --project BookingService/Infrastructure --startup-project BookingService/Presentation
   dotnet ef migrations remove --project BookingService/Infrastructure --startup-project BookingService/Presentation
   ``` 

   UserService:
   ```bash
   dotnet ef migrations add UserServiceDB --project UserService/Infrastructure --startup-project UserService/Presentation
   dotnet ef database update --project UserService/Infrastructure --startup-project UserService/Presentation
   dotnet ef database update <PreviousMigration> --project UserService/Infrastructure --startup-project UserService/Presentation
   dotnet ef migrations remove --project UserService/Infrastructure --startup-project UserService/Presentation
   ``` 