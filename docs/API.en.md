# ScentMatch Backend — API Documentation (English)

Complete technical documentation for the **ScentMatch** backend: perfume search, matching, comparison, personalized recommendations, reviews, collections, and notifications.

---

## Table of contents

1. [System overview](#1-system-overview)
2. [Stack and solution layout](#2-stack-and-solution-layout)
3. [Quick start](#3-quick-start)
4. [Database seed](#4-database-seed)
5. [Authentication and authorization](#5-authentication-and-authorization)
6. [API conventions](#6-api-conventions)
7. [Endpoint reference](#7-endpoint-reference)
8. [Request/response models](#8-requestresponse-models)
9. [Module business rules](#9-module-business-rules)
10. [Error codes](#10-error-codes)
11. [Testing](#11-testing)
12. [Frontend integration notes](#12-frontend-integration-notes)

---

## 1. System overview

ScentMatch Backend is an ASP.NET Core Web API that provides:

- user registration / login with roles `User` and `Admin`;
- perfume catalog with search, filters, and sorting;
- profile and preferences (season, fragrance type, notes, characteristic scales, price range);
- criterion-based matching with score and explanation reasons;
- personalized recommendations using preferences, favorites, and prior ratings;
- comparison of 2–5 perfumes with common/different trait analysis;
- 1–5 ★ reviews, average rating, and moderation;
- favorites, collections, and history of views / selections / recommendations;
- inbox notifications driven by catalog, rating, review, and recommendation events.

Database: **MySQL 8** via EF Core + Pomelo.  
Integration tests: EF Core **InMemory**.

---

## 2. Stack and solution layout

| Component | Technology |
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
  docs/            Documentation (UK / EN)
  scripts/         Utilities (seed, etc.)
```

---

## 3. Quick start

### Prerequisites

- .NET 8 SDK
- MySQL 8.x

### Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | MySQL connection string |
| `Jwt:Key` | JWT signing key (**min 32 characters**, validated on startup) |
| `Jwt:Issuer` / `Jwt:Audience` | Token validation |
| `Cors:Origins` | Allowed frontend origins |

Local override example (PowerShell):

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=scentmatch;User=root;Password=YOUR_PASSWORD;"
$env:Jwt__Key="YOUR_LONG_RANDOM_SIGNING_KEY_AT_LEAST_32_CHARS"
```

### Run the API

```powershell
dotnet restore
dotnet build
dotnet ef database update --project Infrastructure --startup-project API
dotnet run --project API
```

- Swagger (Development): `https://localhost:<port>/swagger`
- Default CORS origins: `http://localhost:5173`, `http://localhost:3000`

On normal startup the API also seeds roles and demo catalog data (idempotent). Full demo users are created by the seed command (see below).

---

## 4. Database seed

The seed command applies EF migrations and fills the database with roles, reference data, demo perfumes, and demo accounts.

### How to run

```powershell
# From repository root
dotnet run --project API -- --seed-db

# Short flag
dotnet run --project API -- --seed

# Script helper
powershell -ExecutionPolicy Bypass -File .\scripts\seed-db.ps1
```

After seeding, the process **exits** (the web host does not start).

### What is created

| Data | Description |
|---|---|
| Roles | `User`, `Admin` |
| Seasons | Spring, Summer, Autumn, Winter, All Season |
| Fragrance types | Woody, Floral, Oriental, Fresh, Citrus, Fruity, Gourmand, Aromatic, Chypre, Leather |
| Brands | Chanel, Dior, Tom Ford, Yves Saint Laurent |
| Notes | Bergamot, Rose, Vanilla, Sandalwood, Jasmine, Amber, Cedar, Lemon, Patchouli, Musk, Pepper, Iris |
| Perfumes | Bleu de Chanel, J'adore, Sauvage, Black Orchid, Libre |
| Demo Admin | `admin@scentmatch.local` / `Admin123!` |
| Demo User | `user@scentmatch.local` / `User1234!` (+ preferences) |

Seed is **idempotent**: re-running does not duplicate existing rows.

---

## 5. Authentication and authorization

### Flow

1. `POST /api/auth/register` or `POST /api/auth/login` → `accessToken` + `refreshToken`
2. For protected routes send:  
   `Authorization: Bearer <accessToken>`
3. Refresh token pair: `POST /api/auth/refresh`
4. Logout / revoke refresh: `POST /api/auth/logout`

### Roles

| Role | Purpose |
|---|---|
| `User` | Regular user (assigned on registration) |
| `Admin` | Catalog CRUD + review moderation |

### Important rules

- Access token contains `NameIdentifier` (user id) and role claims.
- Every authenticated request validates `User.IsActive`; inactive users get `401`.
- Password policy: min 8 chars, digit, upper/lower case, non-alphanumeric.

---

## 6. API conventions

### Base path

```text
/api/...
```

### Pagination (typical)

| Parameter | Type | Default | Limits |
|---|---|---|---|
| `page` | int | `1` | ≥ 1 |
| `pageSize` | int | `20` | 1…100 |

Response shape:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0
}
```

### Dates

UTC (`DateTime` ISO-8601).

### Content-Type

`application/json` for request bodies.

---

## 7. Endpoint reference

Auth legend:

- **Public** — no token required
- **User** — any authenticated user
- **Admin** — `Admin` role required

---

### 7.1 Auth — `/api/auth`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register (role User) |
| POST | `/api/auth/login` | Public | Login |
| POST | `/api/auth/refresh` | Public | Refresh tokens (refresh rotation) |
| POST | `/api/auth/logout` | Public | Revoke refresh token |
| GET | `/api/auth/me` | User | Current user |

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

**AuthResponse (example)**

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

### 7.2 Public catalog

#### Perfumes — `/api/perfumes`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/perfumes` | Public | List with search/filters/sort |
| GET | `/api/perfumes/{id}` | Public* | Details (*with JWT also registers view history) |

**Query parameters for `GET /api/perfumes`**

| Parameter | Description |
|---|---|
| `search` | Search by perfume name or brand name |
| `brandId` | Brand filter |
| `fragranceTypeId` | Fragrance type filter |
| `seasonId` | Season filter |
| `noteIds` | CSV note ids, **AND** semantics (`1,5,8`) |
| `minPrice` / `maxPrice` | Price range |
| `minRating` | Minimum rating 0…5 |
| `gender` | Case-insensitive contains |
| `concentration` | Case-insensitive contains |
| `minSweetness` / `minFreshness` / `minWoody` / `minSpicy` | Min scale values 0…100 |
| `sortBy` | `name` \| `price` \| `rating` \| `releaseYear` \| `brand` |
| `sortOrder` | `asc` \| `desc` |
| `page` / `pageSize` | Pagination |

#### Reference data

| Method | Path | Auth |
|---|---|---|
| GET | `/api/brands` | Public |
| GET | `/api/brands/{id}` | Public |
| GET | `/api/notes` | Public |
| GET | `/api/notes/{id}` | Public |
| GET | `/api/seasons` | Public |
| GET | `/api/seasons/{id}` | Public |
| GET | `/api/fragrance-types` | Public |
| GET | `/api/fragrance-types/{id}` | Public |

Inactive (`IsActive=false`) brands/perfumes are **not** returned by public catalog endpoints.

---

### 7.3 Admin catalog — `/api/admin/*`

All routes require **Admin**.

| Method | Path | Description |
|---|---|---|
| POST | `/api/admin/perfumes` | Create perfume (+ NewPerfume notifications for users with preferences) |
| PUT | `/api/admin/perfumes/{id}` | Update (+ CatalogUpdate for users who favorited it) |
| DELETE | `/api/admin/perfumes/{id}` | Soft-delete (`IsActive=false`) |
| POST/PUT/DELETE | `/api/admin/brands` | Brand CRUD (delete = soft) |
| POST/PUT/DELETE | `/api/admin/notes` | Note CRUD (hard delete) |
| POST/PUT/DELETE | `/api/admin/seasons` | Season CRUD |
| POST/PUT/DELETE | `/api/admin/fragrance-types` | Fragrance type CRUD |

**CreatePerfumeRequest (abbreviated)**

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

### 7.4 Profile — `/api/profile`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/profile` | User | Profile |
| PUT | `/api/profile` | User | Update username / names / avatar |
| GET | `/api/profile/preferences` | User | Preferences (get-or-create) |
| PUT | `/api/profile/preferences` | User | Save preferences |

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

Characteristic scales: `0…100` (nullable).  
`preferredNoteIds`: up to 20 unique ids.

---

### 7.5 Favorites — `/api/favorites`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/favorites` | User | Favorites list (paged) |
| POST | `/api/favorites/{perfumeId}` | User | Add |
| DELETE | `/api/favorites/{perfumeId}` | User | Remove |

---

### 7.6 Collections — `/api/collections`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/collections` | User | List collections |
| GET | `/api/collections/{id}` | User | Details + perfumes |
| POST | `/api/collections` | User | Create |
| PUT | `/api/collections/{id}` | User | Update |
| DELETE | `/api/collections/{id}` | User | Delete |
| POST | `/api/collections/{collectionId}/perfumes/{perfumeId}` | User | Add perfume |
| DELETE | `/api/collections/{collectionId}/perfumes/{perfumeId}` | User | Remove perfume |

---

### 7.7 Reviews

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/perfumes/{perfumeId}/reviews` | Public | Approved reviews |
| POST | `/api/perfumes/{perfumeId}/reviews` | User | Create (status Pending) |
| GET | `/api/perfumes/{perfumeId}/my-review` | User | Own review for perfume |
| GET | `/api/reviews/mine` | User | All own reviews |
| PUT | `/api/reviews/{id}` | User | Update (back to Pending) |
| DELETE | `/api/reviews/{id}` | User | Delete |

**Review list sorting** (`sortBy` / `sortOrder`):

- `sortBy`: `date` (default) \| `rating`
- `sortOrder`: `asc` \| `desc` (default for date is `desc`)

**Create/Update body**

```json
{
  "rating": 5,
  "text": "Great summer fragrance"
}
```

`rating`: 1…5. One review per (user, perfume) pair.

**Admin moderation** — `/api/admin/reviews`

| Method | Path | Auth |
|---|---|---|
| GET | `/api/admin/reviews?status=Pending` | Admin |
| PUT | `/api/admin/reviews/{id}/approve` | Admin |
| PUT | `/api/admin/reviews/{id}/reject` | Admin |
| DELETE | `/api/admin/reviews/{id}` | Admin |

Perfume average rating uses **Approved** reviews only.

---

### 7.8 View history — `/api/history/views`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/history/views` | User | List |
| POST | `/api/history/views/{perfumeId}` | User | Register view (upsert) |
| DELETE | `/api/history/views/{id}` | User | Delete one |
| DELETE | `/api/history/views` | User | Clear all |

Automatic: `GET /api/perfumes/{id}` with a valid JWT also registers a view.

---

### 7.9 Selection history — `/api/history/selections`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/history/selections` | User | Save criteria manually |
| GET | `/api/history/selections` | User | List |
| GET | `/api/history/selections/{id}` | User | Details (+ `results`) |
| DELETE | `/api/history/selections/{id}` | User | Delete |
| DELETE | `/api/history/selections` | User | Clear |

`POST /api/matching` **automatically** creates a selection-history row including top match `results`.

---

### 7.10 Matching — `/api/matching`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/matching` | User | Match + persist selection history |

At least **one** criterion is required.

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

**Response**: list of `{ perfumeId, name, brandName, price, score, reasons[] }`, sorted by `score` (max 50 items).

Internal weights: Characteristics 30, Season 20, Notes 20, FragranceType 15, Price 15.

---

### 7.11 Recommendations — `/api/recommendations`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/recommendations` | User | Personalized list (no history write) |
| POST | `/api/recommendations/refresh` | User | Recalculate + persist batch history + notification |

Criterion sources:

1. `UserPreferences` (season, type, scales, price, preferred notes);
2. if preference notes are empty — top notes from favorites + approved ratings ≥ 4;
3. if preference scales are empty — averages from highly rated perfumes.

**Recommendation history**

| Method | Path | Auth |
|---|---|---|
| GET | `/api/history/recommendations` | User |
| GET | `/api/history/recommendations/{batchId}` | User |

---

### 7.12 Comparison — `/api/comparison`

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/comparison` | Public | Compare 2…5 perfumes |

```json
{
  "perfumeIds": [1, 2, 3]
}
```

**Response**

- `items` — full `PerfumeDetailsDto` in request order;
- `analysis` — common/differing notes and seasons; brand / fragranceType / concentration / gender / rating / price / releaseYear fields; 10 characteristic comparisons (`isCommon` + `values[]`).

---

### 7.13 Notifications — `/api/notifications`

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/notifications` | User | Inbox (paged) |
| GET | `/api/notifications/unread-count` | User | Unread count |
| PUT | `/api/notifications/read-all` | User | Mark all read |
| PUT | `/api/notifications/{id}/read` | User | Mark one read |
| DELETE | `/api/notifications/{id}` | User | Delete one |
| DELETE | `/api/notifications` | User | Clear inbox |

**Types**

| Type | When created |
|---|---|
| `NewPerfume` | Admin created perfume → users with preferences |
| `CatalogUpdate` | Admin updated perfume → users who favorited it |
| `RatingChanged` | Average rating changed → favorites |
| `Review` | Review approve/reject → author |
| `Recommendation` | `POST /recommendations/refresh` with results |
| `System` / `Info` | generic / informational |

There is no public create endpoint (internal generation only).

---

### 7.14 Test endpoints (Dev/Testing only)

| Method | Path | Auth |
|---|---|---|
| GET | `/api/test/authenticated` | User |
| GET | `/api/test/admin` | Admin |

---

## 8. Request/response models

### Perfume / preference characteristics

All scales are nullable `int?` in range **0…100**:

`sweetness`, `freshness`, `woody`, `spicy`, `citrus`, `floral`, `fruity`, `powdery`, `smoky`, `oriental`.

### PerfumeListItemDto / PerfumeDetailsDto

Fields: `id`, `name`, `brand`, `fragranceType`, `description`, `releaseYear`, `concentration`, `gender`, `price`, `imageUrl`, `rating`, `reviewsCount`, `characteristics`, `notes[]`, `seasons[]`, `shopLinks[]` (`storeName`, `url`).

### ComparisonAnalysisDto

- `commonNotes` / `differingNotes`
- `commonSeasons` / `differingSeasons`
- `brand`, `fragranceType`, `concentration`, `gender` → `{ isCommon, values[] }`
- `rating`, `price`, `releaseYear` → `{ isCommon, values[] }`
- `characteristics[]` → `{ name, isCommon, values[] }`

---

## 9. Module business rules

| Module | Key behavior |
|---|---|
| Soft-delete | Perfume/Brand: `IsActive=false`; hidden from public catalog |
| Reviews | Pending does not affect rating; edit resets to Pending |
| Matching | Score 0…100; results with score ≤ 0 are dropped |
| View history | Upsert by (userId, perfumeId) |
| Selection history | May include `results` (top matching items) |
| Notifications | Batch insert via dispatcher; no SignalR (polling) |
| JWT | Inactive user fails token validation |

---

## 10. Error codes

| HTTP | When |
|---|---|
| 400 | FluentValidation / business BadRequest |
| 401 | Missing / invalid / inactive token |
| 403 | Insufficient role (not Admin) |
| 404 | Entity not found / foreign resource |
| 409 | Conflict (duplicate username/email/review) |

Typical error body (middleware):

```json
{
  "status": 400,
  "title": "Bad Request",
  "detail": "..."
}
```

---

## 11. Testing

```powershell
dotnet test
```

Tests use InMemory and do **not** fully reproduce MySQL unique constraints, SQL translations, or migration application.

---

## 12. Platform expansion modules

Formulas and limitations: [PLATFORM.md](PLATFORM.md).

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| GET | `/api/perfumes/{id}/similar` | Anonymous | Similar scents + score breakdown |
| GET | `/api/perfumes/by-barcode/{barcode}` | Anonymous | Barcode lookup |
| POST | `/api/matching/text` | User | Natural-language matching (UA/EN) |
| POST | `/api/matching/gift` | Anonymous/User | Gift matching |
| GET | `/api/analytics/preferences` | User | Personal preference analytics |
| POST | `/api/analytics/personalization/reset` | User | Reset behavioral personalization |
| CRUD | `/api/journal` | User | Private scent test journal |
| * | `/api/wardrobe*` | User | Wardrobe by scenario |
| CRUD | `/api/wishlist` | User | Purchase wishlist |
| * | `/api/blind-comparison*` | User | Blind comparison sessions |
| GET/POST | `/api/collections/export`, `/import` | User | Collection import/export |
| CRUD | `/api/price-watches` | User | Price-drop subscriptions |
| GET | `/api/admin/dashboard` | Admin | Aggregated dashboard |
| GET | `/api/admin/catalog/quality` | Admin | Catalog quality report |
| GET | `/api/admin/catalog/duplicates` | Admin | Potential duplicates |
| POST | `/api/admin/catalog/import` | Admin | Bulk catalog import |
| GET | `/api/admin/audit-logs` | Admin | Admin audit log |
| GET | `/api/admin/jobs` | Admin | Background job monitoring |
| GET | `/api/admin/integrations` | Admin | Integration health (no secrets) |

Perfume DTOs also expose nullable `currency`, `volumeMl`, `intensity`, `barcode`.

---

## 13. Frontend integration notes

1. Persist `accessToken` + `refreshToken`; renew via `/api/auth/refresh`.
2. In Development Swagger, paste the access token into Authorize.
3. Comparison: use `analysis` to highlight common/different fields in the UI.
4. Recommendations: show `reasons[]` / score components (relative match score, not purchase probability).
5. Notifications: poll `unread-count` + inbox; new type `PriceDrop`.
6. After seed you can log in with the demo accounts (see [section 4](#4-database-seed)).
7. Wishlist ≠ Favorites. Wardrobe = collections + `scenarioKey`.
8. Blind comparison: do not show names/brands until `complete` — API withholds them while Active.

---

*This documentation matches the current ScentMatch Backend implementation.*
