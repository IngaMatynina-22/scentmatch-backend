# ScentMatch Backend

ASP.NET Core Web API for personalized perfume matching and comparison (**ScentMatch**).

## Documentation

| Language | File |
| --- | --- |
| Українська | [docs/API.uk.md](docs/API.uk.md) |
| English | [docs/API.en.md](docs/API.en.md) |

The docs cover setup, seed, auth, every route, request/response models, business rules, and error codes.

## Stack

- ASP.NET Core 8 Web API
- Entity Framework Core + Pomelo MySQL
- ASP.NET Identity + JWT (access + refresh tokens)
- FluentValidation
- Swagger / OpenAPI
- xUnit integration tests (EF Core InMemory)

## Solution structure

```text
ScentMatch/
  API/             Controllers, middleware, Program.cs
  Application/     DTOs, interfaces, validators, options
  Domain/          Entities, constants
  Infrastructure/  EF Core, Identity, services, migrations, seed
  Tests/           Integration tests
  docs/            Full API documentation (UK / EN)
  scripts/         Helper scripts (database seed)
```

## Prerequisites

- .NET 8 SDK
- MySQL 8.x (for running the API against a real database)

## Configuration

Do **not** commit real production secrets.

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | MySQL connection |
| `Jwt:Key` | JWT signing key (**min 32 characters**) |
| `Jwt:Issuer` / `Jwt:Audience` | Token validation |
| `Cors:Origins` | Allowed frontend origins |
| `PriceTracking:Enabled` | Background price-check job (`false` by default) |
| `PriceTracking:CheckIntervalMinutes` | Interval between price-check runs |

See [`.env.example`](.env.example) for environment-variable names (no real secrets).

### Local development

`appsettings.Development.json` contains local development defaults (DEV JWT key and local MySQL).

Override secrets with environment variables or [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), for example:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=scentmatch;User=root;Password=YOUR_PASSWORD;"
$env:Jwt__Key="YOUR_LONG_RANDOM_SIGNING_KEY_AT_LEAST_32_CHARS"
```

`Jwt:Key` is validated on startup. An empty/short key prevents the API from starting.

## Database migrations

From the solution root:

```powershell
dotnet ef database update --project Infrastructure --startup-project API
```

Create a new migration only when the model changes:

```powershell
dotnet ef migrations add MigrationName --project Infrastructure --startup-project API --output-dir Data/Migrations
```

## Seed the database

Apply migrations and fill demo catalog + demo users:

```powershell
dotnet run --project API -- --seed-db
```

Or:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\seed-db.ps1
```

Also accepted: `--seed`.

### Demo accounts created by seed

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@scentmatch.local` | `Admin123!` |
| User | `user@scentmatch.local` | `User1234!` |

Seed is idempotent. After seed the process exits (API host does not start).

Catalog seed includes **40+ perfumes** across many brands, each with:
- `imageUrl` (photo)
- `shopLinks` (Notino, Sephora, Douglas, brand site)

On normal `dotnet run`, roles + catalog demo data are still seeded lightly; use `--seed-db` for full demo users and migration apply.

## Run the API

```powershell
dotnet restore
dotnet build
dotnet run --project API
```

- Swagger UI (Development): `https://localhost:<port>/swagger`
- Default CORS origins: `http://localhost:5173`, `http://localhost:3000`

## Run tests

Tests use EF Core **InMemory** (not MySQL):

```powershell
dotnet test
```

InMemory does **not** fully reproduce MySQL unique constraints, SQL translations, or migration application.

## Auth overview

| Endpoint | Auth |
| --- | --- |
| `POST /api/auth/register` | Anonymous |
| `POST /api/auth/login` | Anonymous |
| `POST /api/auth/refresh` | Anonymous (refresh token body) |
| `POST /api/auth/logout` | Anonymous (refresh token body) |
| `GET /api/auth/me` | Bearer |

Admin catalog/moderation routes require role `Admin`.

## Main API areas

| Area | Base routes |
| --- | --- |
| Catalog | `/api/perfumes`, `/api/brands`, `/api/notes`, `/api/seasons`, `/api/fragrance-types` |
| Similar / barcode | `GET /api/perfumes/{id}/similar`, `GET /api/perfumes/by-barcode/{barcode}` |
| Admin | `/api/admin/*` (CRUD, dashboard, catalog quality/duplicates/import, audit, jobs, integrations) |
| Profile | `/api/profile`, `/api/profile/preferences` |
| Favorites / Collections / Wardrobe | `/api/favorites`, `/api/collections`, `/api/wardrobe` |
| Wishlist | `/api/wishlist` (purchase intent; separate from favorites) |
| Journal | `/api/journal` (private scent testing diary) |
| Blind comparison | `/api/blind-comparison` |
| Reviews | `/api/perfumes/{id}/reviews`, `/api/reviews/mine`, `/api/admin/reviews` |
| History | `/api/history/views`, `/api/history/selections`, `/api/history/recommendations` |
| Matching | `POST /api/matching`, `/api/matching/text`, `/api/matching/gift` |
| Analytics | `/api/analytics/preferences`, `/api/analytics/personalization/reset` |
| Recommendations | `/api/recommendations` (transparent score breakdown) |
| Comparison | `POST /api/comparison` |
| Price watches | `/api/price-watches` |
| Notifications | `/api/notifications` |

See [docs/API.en.md](docs/API.en.md) / [docs/API.uk.md](docs/API.uk.md) and [docs/PLATFORM.md](docs/PLATFORM.md) for contracts and formulas.

## Background jobs

| Job | Config | Behavior |
| --- | --- | --- |
| `PriceWatchBackgroundService` | `PriceTracking:Enabled` | Checks active watches via `IPriceProvider`. Default provider is **unconfigured** (no invented prices). Writes `background_job_logs` and `PriceDrop` notifications when a real provider reports a drop. |

## Limitations

- External price scraping / LLM text enrichment are **optional** and off by default (`UnconfiguredPriceProvider`, `NullTextCriteriaEnricher`).
- Score values are **relative match scores (0–100)**, not calibrated purchase probabilities.
- Blind-comparison identity mapping is revealed only after session completion.
- Admin APIs never expose private journal notes or auth secrets.

## Notes

- Public catalog endpoints are anonymous; personal features require JWT.
- Review ratings on perfumes use **Approved** reviews only.
- Matching / recommendations / similarity share `PerfumeScoring` + `PerfumeProfile` (null ≠ zero).
- Gift matching history uses `ScenarioType=Gift` and is excluded from personalization.
- Live MySQL verification is separate from `dotnet test` (194 tests use InMemory).
