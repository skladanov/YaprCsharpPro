# WebProject API — ASP.NET Core Web API

RESTful API для управления событиями (Events) на базе ASP.NET Core.

## Технологии


* ASP.NET Core 9.0+
* Swagger/OpenAPI для документации

## Функциональность

API предоставляет CRUD‑операции для работы с событиями:

* **GET** `/api/events?[title=...]&[from=...]&[to=...]&[page=...]&[pageSize=...]` — получить событ  ия
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

Формат ответа при ошибках соответствует стандарту RFC7807 ProblemDetails

## Быстрый старт


### Предварительные требования

* .NET 9.0 SDK
* IDE (Visual Studio, VS Code или Rider)

### Запуск проекта

1. Клонируйте репозиторий:
   ```bash
   git clone https://github.com/skladanov/YaprCsharpPro.git
   cd YaprCsharpPro/WebProject
   ```

2. Соберите и запустите проект:
   ```bash
   dotnet build
   dotnet run
   ```

3. Запустите тесты:
   ```bash
   cd YaprCsharpPro/WebProject.Tests
   dotnet test
   ```