# ScentMatch Backend — Документація API (українською)

Повна технічна документація бекенду вебплатформи **ScentMatch**: пошук, підбір, порівняння парфумів, персональні рекомендації, відгуки, колекції та сповіщення.

---

## Зміст

1. [Огляд системи](#1-огляд-системи)
2. [Стек і структура рішення](#2-стек-і-структура-рішення)
3. [Швидкий старт](#3-швидкий-старт)
4. [Seed бази даних](#4-seed-бази-даних)
5. [Автентифікація та авторизація](#5-автентифікація-та-авторизація)
6. [Загальні правила API](#6-загальні-правила-api)
7. [Довідник ендпоінтів](#7-довідник-ендпоінтів)
8. [Моделі запитів/відповідей](#8-моделі-запитіввідповідей)
9. [Бізнес-логіка модулів](#9-бізнес-логіка-модулів)
10. [Коди помилок](#10-коди-помилок)
11. [Тестування](#11-тестування)
12. [Примітки для фронтенду](#12-примітки-для-фронтенду)

---

## 1. Огляд системи

ScentMatch Backend — ASP.NET Core Web API, яке забезпечує:

- реєстрацію / логін користувачів (ролі `User`, `Admin`);
- каталог парфумів зі пошуком, фільтрами та сортуванням;
- профіль і вподобання (сезон, тип аромату, ноти, шкали характеристик, ціна);
- підбір (matching) за критеріями зі score і reasons;
- персональні рекомендації з урахуванням preferences, favorites та оцінок;
- порівняння 2–5 парфумів із аналізом спільних/відмінних характеристик;
- відгуки 1–5 ★, середній рейтинг, модерацію;
- обране, колекції, історію переглядів / підборів / рекомендацій;
- inbox-сповіщення (автоматичні події каталогу, рейтингу, відгуків, рекомендацій).

База: **MySQL 8** через EF Core + Pomelo.  
Інтеграційні тести: EF Core **InMemory**.

---

## 2. Стек і структура рішення

| Компонент | Технологія |
|---|---|
| Runtime | .NET 8 |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core + Pomelo MySQL |
| Auth | ASP.NET Identity + JWT (access + refresh) |
| Validation | FluentValidation |
| Docs UI | Swagger / OpenAPI (Development) |
| Tests | xUnit + `WebApplicationFactory` |

```text
ScentMatch/
  API/             Controllers, middleware, Program.cs, appsettings
  Application/     DTOs, interfaces, validators, options
  Domain/          Entities, constants
  Infrastructure/  EF Core, Identity, services, migrations, seed
  Tests/           Integration tests
  docs/            Документація (UK / EN)
  scripts/         Утиліти (seed тощо)
```

---

## 3. Швидкий старт

### Вимоги

- .NET 8 SDK
- MySQL 8.x

### Конфігурація

| Ключ | Призначення |
|---|---|
| `ConnectionStrings:DefaultConnection` | Рядок підключення MySQL |
| `Jwt:Key` | Ключ підпису JWT (**мінімум 32 символи**, перевіряється на старті) |
| `Jwt:Issuer` / `Jwt:Audience` | Валідація токена |
| `Cors:Origins` | Дозволені origin фронтенду |

Приклад локального override (PowerShell):

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=scentmatch;User=root;Password=YOUR_PASSWORD;"
$env:Jwt__Key="YOUR_LONG_RANDOM_SIGNING_KEY_AT_LEAST_32_CHARS"
```

### Запуск API

```powershell
dotnet restore
dotnet build
dotnet ef database update --project Infrastructure --startup-project API
dotnet run --project API
```

- Swagger (Development): `https://localhost:<port>/swagger`
- CORS за замовчуванням: `http://localhost:5173`, `http://localhost:3000`

При звичайному старті API додатково сідає ролі та демо-каталог (idempotent). Повні демо-користувачі створюються командою seed (див. нижче).

---

## 4. Seed бази даних

Команда застосовує міграції та наповнює БД ролями, довідниками, демо-парфумами та демо-акаунтами.

### Варіанти запуску

```powershell
# З кореня репозиторію
dotnet run --project API -- --seed-db

# Або короткий прапорець
dotnet run --project API -- --seed

# Або скрипт
powershell -ExecutionPolicy Bypass -File .\scripts\seed-db.ps1
```

Після seed процес **завершується** (вебсервер не стартує).

### Що створюється

| Дані | Опис |
|---|---|
| Ролі | `User`, `Admin` |
| Сезони | Spring, Summer, Autumn, Winter, All Season |
| Типи ароматів | Woody, Floral, Oriental, Fresh, Citrus, Fruity, Gourmand, Aromatic, Chypre, Leather |
| Бренди | Chanel, Dior, Tom Ford, Yves Saint Laurent |
| Ноти | Bergamot, Rose, Vanilla, Sandalwood, Jasmine, Amber, Cedar, Lemon, Patchouli, Musk, Pepper, Iris |
| Парфуми | Bleu de Chanel, J'adore, Sauvage, Black Orchid, Libre |
| Демо Admin | `admin@scentmatch.local` / `Admin123!` |
| Демо User | `user@scentmatch.local` / `User1234!` (+ preferences) |

Seed **ідемпотентний**: повторний запуск не дублює існуючі записи.

---

## 5. Автентифікація та авторизація

### Схема

1. `POST /api/auth/register` або `POST /api/auth/login` → `accessToken` + `refreshToken`
2. Для захищених маршрутів: заголовок  
   `Authorization: Bearer <accessToken>`
3. Оновлення пари токенів: `POST /api/auth/refresh`
4. Вихід / revoke refresh: `POST /api/auth/logout`

### Ролі

| Роль | Призначення |
|---|---|
| `User` | Звичайний користувач (призначається при реєстрації) |
| `Admin` | CRUD каталогу + модерація відгуків |

### Важливо

- Access token містить `NameIdentifier` (user id) і рольові claims.
- На кожному запиті перевіряється `User.IsActive`; неактивний користувач отримує `401`.
- Пароль: мін. 8 символів, цифра, верхній/нижній регістр, спецсимвол.

---

## 6. Загальні правила API

### Базовий URL

```text
/api/...
```

### Пагінація (типово)

| Параметр | Тип | За замовчуванням | Обмеження |
|---|---|---|---|
| `page` | int | `1` | ≥ 1 |
| `pageSize` | int | `20` | 1…100 |

Відповідь:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0
}
```

### Формат дат

UTC (`DateTime` ISO-8601).

### Content-Type

`application/json` для body.

---

## 7. Довідник ендпоінтів

Легенда Auth:

- **Public** — без токена
- **User** — будь-який автентифікований користувач
- **Admin** — роль `Admin`

---

### 7.1 Auth — `/api/auth`

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Реєстрація (роль User) |
| POST | `/api/auth/login` | Public | Логін |
| POST | `/api/auth/refresh` | Public | Оновлення токенів (ротація refresh) |
| POST | `/api/auth/logout` | Public | Відкликання refresh |
| GET | `/api/auth/me` | User | Поточний користувач |

**Register body**

```json
{
  "username": "inga",
  "email": "inga@example.com",
  "password": "Password1!",
  "firstName": "Inga",
  "lastName": "Matynina"
}
```

**Login body**

```json
{
  "email": "inga@example.com",
  "password": "Password1!"
}
```

**Refresh / Logout body**

```json
{
  "refreshToken": "<token>"
}
```

**AuthResponse (приклад)**

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresAt": "2026-10-09T01:00:00Z",
  "user": {
    "id": 1,
    "email": "inga@example.com",
    "username": "inga",
    "roles": ["User"]
  }
}
```

---

### 7.2 Каталог (публічний)

#### Парфуми — `/api/perfumes`

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| GET | `/api/perfumes` | Public | Список з пошуком/фільтрами/сортуванням |
| GET | `/api/perfumes/{id}` | Public* | Деталі (*якщо є JWT — пише view history) |

**Query-параметри `GET /api/perfumes`**

| Параметр | Опис |
|---|---|
| `search` | Пошук за назвою парфуму або бренду |
| `brandId` | Фільтр бренду |
| `fragranceTypeId` | Фільтр типу аромату |
| `seasonId` | Фільтр сезону |
| `noteIds` | CSV id нот, логіка **AND** (`1,5,8`) |
| `minPrice` / `maxPrice` | Діапазон ціни |
| `minRating` | Мін. рейтинг 0…5 |
| `gender` | Contains (case-insensitive) |
| `concentration` | Contains (case-insensitive) |
| `minSweetness` / `minFreshness` / `minWoody` / `minSpicy` | Мін. значення шкал 0…100 |
| `sortBy` | `name` \| `price` \| `rating` \| `releaseYear` \| `brand` |
| `sortOrder` | `asc` \| `desc` |
| `page` / `pageSize` | Пагінація |

#### Довідники

| Метод | Шлях | Auth |
|---|---|---|
| GET | `/api/brands` | Public |
| GET | `/api/brands/{id}` | Public |
| GET | `/api/notes` | Public |
| GET | `/api/notes/{id}` | Public |
| GET | `/api/seasons` | Public |
| GET | `/api/seasons/{id}` | Public |
| GET | `/api/fragrance-types` | Public |
| GET | `/api/fragrance-types/{id}` | Public |

Неактивні (`IsActive=false`) бренди/парфуми в публічному каталозі **не** віддаються.

---

### 7.3 Admin каталог — `/api/admin/*`

Усі маршрути: **Admin**.

| Метод | Шлях | Опис |
|---|---|---|
| POST | `/api/admin/perfumes` | Створити парфум (+ сповіщення NewPerfume користувачам з preferences) |
| PUT | `/api/admin/perfumes/{id}` | Оновити (+ CatalogUpdate для favorites) |
| DELETE | `/api/admin/perfumes/{id}` | Soft-delete (`IsActive=false`) |
| POST/PUT/DELETE | `/api/admin/brands` | CRUD брендів (delete = soft) |
| POST/PUT/DELETE | `/api/admin/notes` | CRUD нот (hard delete) |
| POST/PUT/DELETE | `/api/admin/seasons` | CRUD сезонів |
| POST/PUT/DELETE | `/api/admin/fragrance-types` | CRUD типів |

**CreatePerfumeRequest (скорочено)**

```json
{
  "name": "Demo Perfume",
  "brandId": 1,
  "fragranceTypeId": 1,
  "description": "...",
  "releaseYear": 2020,
  "concentration": "EDP",
  "gender": "Unisex",
  "price": 150,
  "imageUrl": "https://...",
  "characteristics": {
    "sweetness": 40,
    "freshness": 60,
    "woody": 50,
    "spicy": 20,
    "citrus": 30,
    "floral": 40,
    "fruity": 20,
    "powdery": 10,
    "smoky": 5,
    "oriental": 15
  },
  "notes": [
    { "noteId": 1, "noteType": "Top" },
    { "noteId": 2, "noteType": "Middle" },
    { "noteId": 3, "noteType": "Base" }
  ],
  "seasons": [
    { "seasonId": 2, "suitability": 80 }
  ]
}
```

`noteType`: `Top` | `Middle` | `Base`.  
`suitability`: 0…100.

---

### 7.4 Профіль — `/api/profile`

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| GET | `/api/profile` | User | Профіль |
| PUT | `/api/profile` | User | Оновити username / ім’я / avatar |
| GET | `/api/profile/preferences` | User | Вподобання (get-or-create) |
| PUT | `/api/profile/preferences` | User | Зберегти вподобання |

**UpdateUserPreferencesRequest**

```json
{
  "preferredSeasonId": 2,
  "preferredFragranceTypeId": 1,
  "preferredNoteIds": [1, 4],
  "sweetness": 25,
  "freshness": 70,
  "woody": 75,
  "spicy": 30,
  "citrus": 65,
  "floral": 20,
  "fruity": 15,
  "powdery": 10,
  "smoky": 20,
  "oriental": 25,
  "minPrice": 50,
  "maxPrice": 250
}
```

Шкали характеристик: `0…100` (nullable).  
`preferredNoteIds`: до 20 унікальних id.

---

### 7.5 Обране — `/api/favorites`

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| GET | `/api/favorites` | User | Список обраного (пагінація) |
| POST | `/api/favorites/{perfumeId}` | User | Додати |
| DELETE | `/api/favorites/{perfumeId}` | User | Прибрати |

---

### 7.6 Колекції — `/api/collections`

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| GET | `/api/collections` | User | Список колекцій |
| GET | `/api/collections/{id}` | User | Деталі + парфуми |
| POST | `/api/collections` | User | Створити |
| PUT | `/api/collections/{id}` | User | Оновити |
| DELETE | `/api/collections/{id}` | User | Видалити |
| POST | `/api/collections/{collectionId}/perfumes/{perfumeId}` | User | Додати парфум |
| DELETE | `/api/collections/{collectionId}/perfumes/{perfumeId}` | User | Прибрати парфум |

---

### 7.7 Відгуки

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| GET | `/api/perfumes/{perfumeId}/reviews` | Public | Approved відгуки |
| POST | `/api/perfumes/{perfumeId}/reviews` | User | Створити (статус Pending) |
| GET | `/api/perfumes/{perfumeId}/my-review` | User | Власний відгук на парфум |
| GET | `/api/reviews/mine` | User | Усі свої відгуки |
| PUT | `/api/reviews/{id}` | User | Оновити (знову Pending) |
| DELETE | `/api/reviews/{id}` | User | Видалити |

**Сортування списків відгуків** (`sortBy` / `sortOrder`):

- `sortBy`: `date` (default) \| `rating`
- `sortOrder`: `asc` \| `desc` (для date за замовчуванням `desc`)

**Create/Update body**

```json
{
  "rating": 5,
  "text": "Чудовий аромат на літо"
}
```

`rating`: 1…5. Один відгук на пару (user, perfume).

**Admin модерація** — `/api/admin/reviews`

| Метод | Шлях | Auth |
|---|---|---|
| GET | `/api/admin/reviews?status=Pending` | Admin |
| PUT | `/api/admin/reviews/{id}/approve` | Admin |
| PUT | `/api/admin/reviews/{id}/reject` | Admin |
| DELETE | `/api/admin/reviews/{id}` | Admin |

Середній рейтинг парфуму рахується **лише** по `Approved`.

---

### 7.8 Історія переглядів — `/api/history/views`

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| GET | `/api/history/views` | User | Список |
| POST | `/api/history/views/{perfumeId}` | User | Зареєструвати перегляд (upsert) |
| DELETE | `/api/history/views/{id}` | User | Видалити запис |
| DELETE | `/api/history/views` | User | Очистити все |

Автоматично: `GET /api/perfumes/{id}` з валідним JWT також реєструє перегляд.

---

### 7.9 Історія підборів — `/api/history/selections`

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| POST | `/api/history/selections` | User | Зберегти критерії вручну |
| GET | `/api/history/selections` | User | Список |
| GET | `/api/history/selections/{id}` | User | Деталі (+ `results`) |
| DELETE | `/api/history/selections/{id}` | User | Видалити |
| DELETE | `/api/history/selections` | User | Очистити |

`POST /api/matching` **автоматично** створює запис selection history з топ-результатами (`results`).

---

### 7.10 Matching — `/api/matching`

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| POST | `/api/matching` | User | Підбір + збереження історії |

Потрібен **хоча б один** критерій.

```json
{
  "seasonId": 2,
  "fragranceTypeId": 1,
  "selectedNoteIds": [1, 4],
  "sweetness": 30,
  "freshness": 70,
  "woody": 80,
  "spicy": 25,
  "citrus": 60,
  "floral": 20,
  "fruity": 10,
  "powdery": 10,
  "smoky": 15,
  "oriental": 20,
  "minPrice": 50,
  "maxPrice": 200
}
```

**Відповідь**: список `{ perfumeId, name, brandName, price, score, reasons[] }`, відсортований за `score` (макс. 50).

Ваги (внутрішні): Characteristics 30, Season 20, Notes 20, FragranceType 15, Price 15.

---

### 7.11 Рекомендації — `/api/recommendations`

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| GET | `/api/recommendations` | User | Персональна добірка (без запису history) |
| POST | `/api/recommendations/refresh` | User | Перерахунок + запис batch history + сповіщення |

Джерела критеріїв:

1. `UserPreferences` (сезон, тип, шкали, ціна, preferred notes);
2. якщо notes у preferences немає — топ ноти з favorites + approved-оцінок ≥ 4;
3. якщо шкали в preferences порожні — середні характеристики з високо оцінених парфумів.

**Історія рекомендацій**

| Метод | Шлях | Auth |
|---|---|---|
| GET | `/api/history/recommendations` | User |
| GET | `/api/history/recommendations/{batchId}` | User |

---

### 7.12 Порівняння — `/api/comparison`

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| POST | `/api/comparison` | Public | Порівняти 2…5 парфумів |

```json
{
  "perfumeIds": [1, 2, 3]
}
```

**Відповідь**

- `items` — повні `PerfumeDetailsDto` у порядку запиту;
- `analysis` — спільні/відмінні ноти й сезони; поля brand / fragranceType / concentration / gender / rating / price / releaseYear; порівняння 10 характеристик (`isCommon` + `values[]`).

---

### 7.13 Сповіщення — `/api/notifications`

| Метод | Шлях | Auth | Опис |
|---|---|---|---|
| GET | `/api/notifications` | User | Inbox (пагінація) |
| GET | `/api/notifications/unread-count` | User | Кількість непрочитаних |
| PUT | `/api/notifications/read-all` | User | Прочитати всі |
| PUT | `/api/notifications/{id}/read` | User | Прочитати одне |
| DELETE | `/api/notifications/{id}` | User | Видалити |
| DELETE | `/api/notifications` | User | Очистити inbox |

**Типи**

| Type | Коли створюється |
|---|---|
| `NewPerfume` | Admin створив парфум → користувачі з preferences |
| `CatalogUpdate` | Admin оновив парфум → хто має його в favorites |
| `RatingChanged` | Змінився середній рейтинг → favorites |
| `Review` | Approve/Reject відгуку → автор |
| `Recommendation` | `POST /recommendations/refresh` з результатами |
| `System` / `Info` | службові / загальні |

Публічного create-ендпоінту немає (лише внутрішня генерація).

---

### 7.14 Test (лише Dev/Testing)

| Метод | Шлях | Auth |
|---|---|---|
| GET | `/api/test/authenticated` | User |
| GET | `/api/test/admin` | Admin |

---

## 8. Моделі запитів/відповідей

### Характеристики парфуму / preferences

Усі шкали nullable `int?` у діапазоні **0…100**:

`sweetness`, `freshness`, `woody`, `spicy`, `citrus`, `floral`, `fruity`, `powdery`, `smoky`, `oriental`.

### PerfumeListItemDto / PerfumeDetailsDto

Поля: `id`, `name`, `brand`, `fragranceType`, `description`, `releaseYear`, `concentration`, `gender`, `price`, `imageUrl`, `rating`, `reviewsCount`, `characteristics`, `notes[]`, `seasons[]`, `shopLinks[]` (`storeName`, `url`).

### ComparisonAnalysisDto

- `commonNotes` / `differingNotes`
- `commonSeasons` / `differingSeasons`
- `brand`, `fragranceType`, `concentration`, `gender` → `{ isCommon, values[] }`
- `rating`, `price`, `releaseYear` → `{ isCommon, values[] }`
- `characteristics[]` → `{ name, isCommon, values[] }`

---

## 9. Бізнес-логіка модулів

| Модуль | Ключова поведінка |
|---|---|
| Soft-delete | Perfume/Brand: `IsActive=false`; публічний каталог їх ховає |
| Reviews | Pending не впливають на рейтинг; після edit знову Pending |
| Matching | Score 0…100; результати з score ≤ 0 відкидаються |
| View history | Upsert по (userId, perfumeId) |
| Selection history | Може містити `results` (топ з matching) |
| Notifications | Batch insert через dispatcher; без SignalR (polling) |
| JWT | Inactive user → fail token validation |

---

## 10. Коди помилок

| HTTP | Коли |
|---|---|
| 400 | Валідація FluentValidation / бізнес BadRequest |
| 401 | Немає/недійсний/неактивний токен |
| 403 | Недостатньо прав (не Admin) |
| 404 | Сутність не знайдена / чужий ресурс |
| 409 | Конфлікт (дубль username/email/відгук) |

Тіло помилки (типово middleware):

```json
{
  "status": 400,
  "title": "Bad Request",
  "detail": "..."
}
```

---

## 11. Тестування

```powershell
dotnet test
```

Тести використовують InMemory і **не** гарантують повну сумісність з MySQL (унікальні індекси, SQL-трансляції, міграції).

---

## 12. Примітки для фронтенду

1. Зберігайте `accessToken` + `refreshToken`; оновлюйте пару через `/api/auth/refresh`.
2. Для Swagger у Development вставляйте access token у Authorize.
3. Порівняння: використовуйте `analysis` для підсвітки спільних/відмінних полів.
4. Рекомендації: показуйте `reasons[]` як пояснення «чому рекомендовано».
5. Сповіщення: періодичний polling `unread-count` + список.
6. Після seed можна одразу логінитись демо-акаунтами (див. [розділ 4](#4-seed-бази-даних)).

---

*Документація відповідає поточній реалізації ScentMatch Backend.*
