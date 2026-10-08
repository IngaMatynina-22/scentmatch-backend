# ScentMatch Backend

ASP.NET Core Web API for personalized perfume matching and comparison (**ScentMatch**).

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

## Run the API

```powershell
dotnet restore
dotnet build
dotnet run --project API
```

- Swagger UI (Development): `https://localhost:<port>/swagger`
- Default CORS origins: `http://localhost:5173`, `http://localhost:3000`

On startup the API seeds Identity roles (`User`, `Admin`) and optional catalog demo data (idempotent).

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
| Admin catalog | `/api/admin/*` |
| Profile | `/api/profile`, `/api/profile/preferences` |
| Favorites / Collections | `/api/favorites`, `/api/collections` |
| Reviews | `/api/perfumes/{id}/reviews`, `/api/reviews`, `/api/admin/reviews` |
| History | `/api/history/views`, `/api/history/selections`, `/api/history/recommendations` |
| Matching | `POST /api/matching` |
| Recommendations | `/api/recommendations` |
| Comparison | `POST /api/comparison` |
| Notifications | `/api/notifications` |

## Promote a user to Admin (local)

After registration, assign the `Admin` role in the database / Identity store, then log in again so the JWT includes the role claim.

## Notes

- Public catalog endpoints are anonymous; personal features require JWT.
- Review ratings on perfumes use **Approved** reviews only.
- Matching / recommendations reuse the same weighted scoring service.
- Live MySQL verification is separate from `dotnet test`.
