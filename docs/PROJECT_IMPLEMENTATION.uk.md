# ScentMatch — опис програмної реалізації та тестування

## 1.1 Розробка та опис програмної реалізації системи

Програмне забезпечення ScentMatch призначене для пошуку парфумів, підбору ароматів за заданими критеріями, порівняння продуктів і ведення персональної бібліотеки користувача. Система також підтримує персональні рекомендації, відгуки, історію взаємодій, цінові підписки та адміністрування каталогу.

На відміну від настільних застосунків, frontend і backend ScentMatch є окремими вебзастосунками та взаємодіють через HTTP API. Frontend створено з React і TypeScript, backend — як ASP.NET Core Web API на .NET 8. Клієнт надсилає JSON-запити, а для захищених операцій передає access token у заголовку `Authorization: Bearer`.

Основні репозиторії:

- Backend: `D:\project\scentmatch-backend`
- Frontend: `D:\project\scentmatch-frontend`

Фрагменти нижче наведені з поточної реалізації. Частина прикладів скорочена для пояснення: декларації DTO, `using` та допоміжні методи можуть бути опущені, якщо це зазначено в тексті.

## 1.2 Загальна архітектура та запуск backend

Backend поділено на чотири .NET-проєкти. `API` відповідає за маршрути HTTP, middleware та початкове налаштування застосунку. `Application` містить DTO, інтерфейси сервісів і правила валідації. `Domain` визначає сутності та сталі бізнес-значення. `Infrastructure` реалізує прикладні інтерфейси, доступ до MySQL через Entity Framework Core, Identity, seed-дані й фонові задачі.

Скорочений фрагмент `API/Program.cs` показує реєстрацію залежностей і послідовність middleware:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

`AddApplication()` і `AddInfrastructure()` реєструють сервіси відповідних шарів у контейнері залежностей. Порядок `UseAuthentication()` перед `UseAuthorization()` важливий: спочатку формується поточний principal із токена, потім перевіряються політики й ролі endpoint.

У середовищі Development також вмикається Swagger UI. Під час запуску застосунок засіває необхідні ролі й демонстраційні дані. Режим `--seed-db` застосовує міграції та seed-дані, після чого завершує роботу без запуску HTTP-сервера.

## 1.3 Доступ до даних і доменна модель

Для зберігання даних використовується MySQL 8. Entity Framework Core працює через `ScentMatchDbContext`, а зміни структури бази оформлені міграціями в `Infrastructure/Data/Migrations`.

Нижче — фрагмент контексту, який показує центральні таблиці системи:

```csharp
public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
public DbSet<Brand> Brands => Set<Brand>();
public DbSet<FragranceType> FragranceTypes => Set<FragranceType>();
public DbSet<Season> Seasons => Set<Season>();
public DbSet<Note> Notes => Set<Note>();
public DbSet<Perfume> Perfumes => Set<Perfume>();
public DbSet<PerfumeNote> PerfumeNotes => Set<PerfumeNote>();
public DbSet<PerfumeSeason> PerfumeSeasons => Set<PerfumeSeason>();
public DbSet<FavoritePerfume> FavoritePerfumes => Set<FavoritePerfume>();
public DbSet<Collection> Collections => Set<Collection>();
public DbSet<CollectionPerfume> CollectionPerfumes => Set<CollectionPerfume>();
public DbSet<Review> Reviews => Set<Review>();
public DbSet<RecommendationHistory> RecommendationHistory => Set<RecommendationHistory>();
public DbSet<Notification> Notifications => Set<Notification>();
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
public DbSet<PriceWatch> PriceWatches => Set<PriceWatch>();
public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
public DbSet<BlindComparisonSession> BlindComparisonSessions
    => Set<BlindComparisonSession>();
```

Модель пов'язує парфум із брендом і типом аромату, а ноти та сезони зберігає через асоціативні сутності. Персональні дані окремі для кожного користувача: обране, колекції, wishlist, щоденник, історія, відгуки та підписки на ціну. Колекція має склад із `CollectionPerfume`; wishlist і favorites є окремими сценаріями.

Налаштування моделі виконуються через конфігурації Entity Framework Core, які автоматично застосовуються з assembly:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<User>().ToTable("users");
    modelBuilder.Entity<Role>().ToTable("roles");
    modelBuilder.Entity<UserRole>().ToTable("user_roles");

    modelBuilder.ApplyConfigurationsFromAssembly(
        typeof(ScentMatchDbContext).Assembly);

    ReferenceDataSeed.Seed(modelBuilder);
}
```

Наприклад, зв'язки парфуму й нот, брендів, сезонів та цільових цін задаються окремими конфігураціями. Міграції слід створювати після зміни доменної моделі й перевіряти на реальному MySQL, а не лише на тестовій InMemory-БД.

## 1.4 Реєстрація, вхід і керування JWT

Auth API містить анонімні `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout` та захищений `GET /api/auth/me`. Controller передає DTO сервісу автентифікації:

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(
            request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> Me(
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        return Ok(await _authService.GetCurrentUserAsync(
            userId, cancellationToken));
    }
}
```

Під час реєстрації перевіряються дублікати email і username, Identity створює користувача та призначає роль `User`. У разі успіху система випускає access token і refresh token. Спрощений фрагмент із `AuthService`:

```csharp
var existingByEmail = await _userManager.FindByEmailAsync(request.Email);
if (existingByEmail is not null)
    throw new ConflictException("A user with this email already exists.");

var user = new User
{
    UserName = request.Username,
    Email = request.Email,
    FirstName = request.FirstName,
    LastName = request.LastName,
    IsActive = true,
    EmailConfirmed = true,
    CreatedAt = DateTime.UtcNow
};

var createResult = await _userManager.CreateAsync(user, request.Password);
if (!createResult.Succeeded)
{
    var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
    throw new BadRequestException(errors);
}

var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.User);
if (!roleResult.Succeeded)
{
    var errors = string.Join(" ", roleResult.Errors.Select(e => e.Description));
    throw new BadRequestException(errors);
}

return await IssueTokensAsync(user, cancellationToken);
```

Refresh token не зберігається в базі у відкритому вигляді. Backend формує випадкове значення, зберігає його SHA-256 hash і під час оновлення відкликає старий token та видає нову пару:

```csharp
public string GenerateRefreshToken()
{
    var bytes = RandomNumberGenerator.GetBytes(64);
    return Convert.ToBase64String(bytes);
}

public string HashRefreshToken(string refreshToken)
{
    var hashBytes = SHA256.HashData(
        Encoding.UTF8.GetBytes(refreshToken));
    return Convert.ToHexString(hashBytes);
}
```

Спрощений фрагмент ротації:

```csharp
var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
var storedToken = await _dbContext.RefreshTokens
    .Include(x => x.User)
    .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

if (storedToken is null || !storedToken.IsActive ||
    storedToken.User is null || !storedToken.User.IsActive)
{
    throw new UnauthorizedException("Invalid refresh token.");
}

storedToken.RevokedAt = DateTime.UtcNow;
return await IssueTokensAsync(storedToken.User, cancellationToken);
```

JWT містить user id, email, username та role claims. Під час кожного запиту Identity-пайплайн перевіряє issuer, audience, підпис і строк дії токена; окремо перевіряється, що користувач активний. Адміністративні маршрути додатково позначені `[Authorize(Roles = AppRoles.Admin)]`.

## 1.5 Frontend: структура, маршрути та захищені сторінки

Frontend організований за функціональними модулями. У `src/app` розміщені router, providers, layouts і guards; сторінки формують сценарії; у `src/features/<feature>/api` зосереджені типізовані виклики API; `src/shared` містить спільний HTTP-клієнт, помилки, конфігурацію та UI.

Приклад композиції глобальних провайдерів:

```tsx
export function AppProviders({ children }: AppProvidersProps) {
  return (
    <BrowserRouter>
      <ThemeProvider>
        <AuthProvider>
          <ComparisonProvider>
            <FavoritesProvider>
              <NotificationsProvider>
                {children}
              </NotificationsProvider>
            </FavoritesProvider>
          </ComparisonProvider>
        </AuthProvider>
      </ThemeProvider>
    </BrowserRouter>
  )
}
```

Захищені користувацькі сторінки вкладаються у `ProtectedRoute`, а сторінки адміністрування — у `AdminRoute` та окремий layout:

```tsx
<Route element={<ProtectedRoute />}>
  <Route path={ROUTES.match} element={<MatchPage />} />
  <Route path={ROUTES.recommendations}
         element={<RecommendationsPage />} />
  <Route path={ROUTES.collections}
         element={<CollectionsPage />} />
  <Route path={ROUTES.journal} element={<JournalPage />} />
  <Route path={ROUTES.notifications}
         element={<NotificationsPage />} />
</Route>

<Route element={<AdminRoute />}>
  <Route element={<AdminLayout />}>
    <Route path={ROUTES.admin.dashboard}
           element={<AdminDashboardPage />} />
    <Route path={ROUTES.admin.perfumes}
           element={<AdminPerfumesPage />} />
    <Route path={ROUTES.admin.reviews}
           element={<AdminReviewsPage />} />
  </Route>
</Route>
```

Route guard відповідає за навігаційний досвід, а не за безпеку даних. Серверна перевірка `[Authorize]`, ролі та власника ресурсу залишається обов'язковою.

## 1.6 Frontend HTTP-клієнт і автоматичне оновлення сесії

Адреса backend налаштовується через `VITE_API_BASE_URL`. Frontend не має доступу до секретів сервера; будь-яке `VITE_*` значення потрапляє до клієнтського bundle.

У `httpClient` функції API передають шлях, HTTP-метод, query-параметри та body. Клієнт додає `Authorization` header, якщо токен є, і серіалізує JSON:

```ts
const headers: Record<string, string> = {
  Accept: options.accept ?? 'application/json',
  ...options.headers,
}

if (options.body !== undefined && !isFormData) {
  headers['Content-Type'] = 'application/json'
}

const accessToken = tokenAccessor?.getAccessToken()
if (!options.skipAuth && accessToken) {
  headers.Authorization = `Bearer ${accessToken}`
}

response = await fetch(buildUrl(options.path, options.query), {
  method: options.method ?? 'GET',
  headers,
  body: options.body === undefined
    ? undefined
    : isFormData
      ? (options.body as FormData)
      : JSON.stringify(options.body),
  signal: options.signal,
})
```

Якщо захищений запит отримав 401, клієнт один раз оновлює токени та повторює запит. `refreshPromise` об'єднує паралельні спроби refresh, щоб кілька одночасних запитів не використали той самий refresh token конкурентно:

```ts
if (
  response.status === 401 &&
  !options.skipAuth &&
  !options.skipRefresh &&
  tokenAccessor
) {
  const nextAccessToken = await runRefresh()
  return httpRequest<T>({
    ...options,
    skipRefresh: true,
    headers: {
      ...options.headers,
      Authorization: `Bearer ${nextAccessToken}`,
    },
  })
}
```

Access і refresh tokens зберігаються у `sessionStorage` — сесія очищається після закриття вкладки/браузерного контексту. Це не HttpOnly storage: XSS у межах сторінки може отримати доступ до токена, тому безпечне кодування й контроль сторонніх скриптів є важливими.

Auth-контекст підключає token storage до API-клієнта та встановлює оновлену пару токенів після refresh:

```tsx
configureHttpClient({
  getAccessToken: () => tokenStorage.read()?.accessToken ?? null,
  refreshAccessToken: async () => {
    const current = tokenStorage.read()
    if (!current?.refreshToken) {
      throw new ApiError('Missing refresh token', {
        status: 401,
        code: 'UNAUTHORIZED',
      })
    }

    const refreshed = await authApi.refresh(current.refreshToken)
    const nextTokens = {
      accessToken: refreshed.accessToken,
      refreshToken: refreshed.refreshToken,
      expiresAt: refreshed.expiresAt,
    }
    tokenStorage.write(nextTokens)
    setTokens(nextTokens)
    setUser(refreshed.user)
    return nextTokens.accessToken
  },
  onSessionInvalid: () => {
    tokenStorage.clear()
    setTokens(null)
    setUser(null)
  },
})
```

## 1.7 Каталог: типізований запит від UI до API

Каталог приймає пошуковий текст, фільтри, сортування та параметри пагінації. Frontend перетворює тип `PerfumeQuery` на query string, після чого викликає спільний транспорт:

```ts
function toQueryRecord(
  query: PerfumeQuery,
): Record<string, string | number | boolean | null | undefined> {
  return {
    search: query.search,
    brandId: query.brandId,
    fragranceTypeId: query.fragranceTypeId,
    seasonId: query.seasonId,
    noteIds: query.noteIds,
    minPrice: query.minPrice,
    maxPrice: query.maxPrice,
    minRating: query.minRating,
    gender: query.gender,
    concentration: query.concentration,
    minSweetness: query.minSweetness,
    minFreshness: query.minFreshness,
    minWoody: query.minWoody,
    minSpicy: query.minSpicy,
    sortBy: query.sortBy,
    sortOrder: query.sortOrder,
    page: query.page,
    pageSize: query.pageSize,
  }
}

export const catalogApi = {
  getPerfumes(
    query: PerfumeQuery,
    signal?: AbortSignal,
  ): Promise<PagedResult<Perfume>> {
    return httpRequest<PagedResult<Perfume>>({
      path: '/api/perfumes',
      query: toQueryRecord(query),
      signal,
    })
  },
}
```

Backend endpoint каталогу публічний; при відкритті деталей парфуму з валідним JWT backend також може зареєструвати перегляд. Неактивні парфуми та бренди не показуються у публічному каталозі. Детальні фільтри та DTO описані у [API.uk.md](./API.uk.md).

## 1.8 Matching: валідація, оцінювання та збереження історії

Підбір приймає JSON з бажаними нотами, сезоном, типом аромату, характеристиками, інтенсивністю, концентрацією, гендером і діапазоном ціни. Наприклад:

```json
{
  "seasonId": 2,
  "selectedNoteIds": [1, 4],
  "sweetness": 30,
  "freshness": 70,
  "minPrice": 50,
  "maxPrice": 200,
  "excludedNoteIds": [9]
}
```

FluentValidation перевіряє, що передано хоча б один критерій, шкали знаходяться в діапазоні 0–100, ціни не від'ємні, `minPrice` не перевищує `maxPrice`, а список нот не містить повторів. Приклад правила для числового діапазону:

```csharp
RuleFor(x => x.Sweetness)
    .InclusiveBetween(0, 100)
    .When(x => x.Sweetness.HasValue);

RuleFor(x => x)
    .Must(x => !x.MinPrice.HasValue ||
              !x.MaxPrice.HasValue ||
              x.MinPrice <= x.MaxPrice)
    .WithMessage("minPrice must be less than or equal to maxPrice.");
```

Після перевірки зовнішніх ключів MatchingService завантажує активні профілі, відкидає аромати з виключеними нотами та сортує результати детерміновано:

```csharp
var profiles = await PerfumeProfileLoader.LoadActiveAsync(
    _dbContext, cancellationToken);

var excluded = request.ExcludedNoteIds is { Count: > 0 }
    ? request.ExcludedNoteIds.ToHashSet()
    : null;

var results = profiles
    .Where(p => excluded is null ||
                !p.Notes.Any(n => excluded.Contains(n.NoteId)))
    .Select(p =>
    {
        var breakdown = MatchingCriteriaScorer.Score(p, request);
        return breakdown.TotalScore <= 0m
            ? null
            : MatchingCriteriaScorer.ToMatchingResult(p, breakdown);
    })
    .Where(x => x is not null)
    .Select(x => x!)
    .OrderByDescending(x => x.Score)
    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
    .ThenBy(x => x.PerfumeId)
    .Take(MatchingWeights.MaxResults)
    .ToList();
```

Similarity для характеристики відображає відстань на шкалі 0–100. Якщо відомі значення однакові, similarity дорівнює 1; зі збільшенням відстані вона зменшується:

```csharp
public static decimal CharacteristicSimilarity(int a, int b) =>
    Clamp01(1m - Math.Abs(a - b) / 100m);
```

Фінальний score нормалізується лише за критеріями, які можна порівняти. Значення з пропущеними даними не вважаються автоматично невідповідними:

```csharp
var active = components
    .Where(component => !component.SkippedDueToMissingData)
    .ToList();

var totalWeight = active.Sum(component => component.Weight);
var score = totalWeight <= 0m
    ? 0m
    : Math.Round(
        100m * active.Sum(c => c.Weight * c.Similarity) / totalWeight,
        2,
        MidpointRounding.AwayFromZero);

return new ScoreBreakdown
{
    TotalScore = Math.Clamp(score, 0m, 100m),
    Components = components.ToList(),
    PositiveReasons = positives.ToList(),
    MismatchReasons = mismatches.ToList(),
    DataGaps = gaps.ToList(),
    IncompleteData = gaps.Count > 0
};
```

Отже, score — відносна оцінка відповідності, а не ймовірність покупки. API також повертає компоненти оцінки, позитивні причини, невідповідності та прогалини даних.

Контролер персонального matching після розрахунку зберігає критерії та список результатів в історії підборів:

```csharp
[HttpPost]
[Authorize]
public async Task<ActionResult<MatchingResponse>> Match(
    [FromBody] MatchingRequest request,
    CancellationToken cancellationToken)
{
    var userId = GetCurrentUserId();
    var result = await _matchingService.MatchAsync(request, cancellationToken);

    await _selectionHistoryService.CreateFromMatchingAsync(
        userId,
        request,
        result.Items,
        MatchingScenarioTypes.Personal,
        cancellationToken);

    return Ok(result);
}
```

## 1.9 Текстовий підбір і рекомендації

Текстовий сценарій спочатку завантажує доступні назви нот, сезонів і типів, далі викликає детермінований UA/EN parser і передає отримані критерії до спільного matching-сервісу. Неоднозначні фрази повертаються окремо, а не вгадуються мовчки:

```csharp
var catalog = await LoadCatalogAsync(cancellationToken);
var parsed = TextCriteriaParser.Parse(request.Text, catalog);
var ambiguous = parsed.AmbiguousCriteria.ToList();

await _enricher.EnrichAsync(
    request.Text,
    parsed.Criteria,
    ambiguous,
    cancellationToken);

var matchingRequest =
    TextCriteriaParser.ToMatchingRequest(parsed.Criteria);

if (!HasAnyCriterion(matchingRequest))
{
    return new TextMatchingResponse
    {
        ParsedCriteria = parsed.Criteria,
        AmbiguousCriteria = ambiguous,
        Items = Array.Empty<MatchingResultDto>()
    };
}

var matching = await _matchingService.MatchAsync(
    matchingRequest, cancellationToken);
```

Поточний `ITextCriteriaEnricher` зареєстрований як `NullTextCriteriaEnricher`; зовнішній LLM не є необхідною умовою роботи текстового matching.

Рекомендації використовують ті самі scoring-компоненти. Якщо явно заданих характеристик не вистачає, backend може обчислити середні значення за Approved reviews з оцінкою не нижче 4 та записами щоденника з високою оцінкою. Ноти можуть агрегуватися з обраного та особистої історії. Сценарії Gift/Text не домішуються до Personal selection history:

```csharp
var highRated = await LoadHighRatedSignalsAsync(
    userId, resetAt, cancellationToken);

request.Sweetness ??= AverageInt(highRated.Select(x => x.Sweetness));
request.Freshness ??= AverageInt(highRated.Select(x => x.Freshness));
request.Woody ??= AverageInt(highRated.Select(x => x.Woody));

var historyQuery = _dbContext.SelectionHistory
    .AsNoTracking()
    .Where(x => x.UserId == userId &&
                x.ScenarioType == MatchingScenarioTypes.Personal);
```

`GET /api/recommendations` обчислює поточну добірку без запису batch history. `POST /api/recommendations/refresh` додатково зберігає batch, після чого створює inbox-сповіщення, якщо є результати. Це розділяє звичайне читання сторінки й явну дію оновлення рекомендацій.

## 1.10 Сліпе порівняння та статуси сесій

Сліпе порівняння реалізоване як окрема серверна сесія. Backend перевіряє наявність активних парфумів, перемішує їх криптографічно випадковим алгоритмом, присвоює нейтральні мітки й зберігає ID ароматів у приватних слотах.

```csharp
var perfumeIds = request.PerfumeIds.ToList();
Shuffle(perfumeIds);

var now = DateTime.UtcNow;
var session = new BlindComparisonSession
{
    Id = Guid.NewGuid(),
    UserId = userId,
    Status = BlindComparisonStatuses.Active,
    CreatedAt = now,
    ExpiresAt = now.AddHours(24)
};

for (var i = 0; i < perfumeIds.Count; i++)
{
    session.Slots.Add(new BlindComparisonSlot
    {
        PerfumeId = perfumeIds[i],
        Label = ((char)('A' + i)).ToString(),
        SortOrder = i
    });
}
```

Звичайний DTO активної сесії містить label, оцінку та коментар, але не назву чи бренд. Endpoint результатів дозволяє розкрити зв'язок label → perfume тільки після `Completed`. Стан `Expired` встановлюється, якщо активна сесія прострочена; подальша мутація завершеної чи простроченої сесії заборонена.

## 1.11 Фонові задачі та інтеграція цін

`PriceWatchBackgroundService` є `BackgroundService`, який періодично обробляє активні цінові підписки. Значення `PriceTracking:Enabled` за замовчуванням вимкнене. Навіть якщо фонову задачу ввімкнено, провайдер `UnconfiguredPriceProvider` не створює фіктивних цін: job записує стан `Skipped` та пояснення в журнал.

Фрагмент перевірки конфігурації провайдера:

```csharp
if (!provider.IsConfigured)
{
    jobLog.Status = "Skipped";
    jobLog.FinishedAt = DateTime.UtcNow;
    jobLog.Message =
        $"Provider '{provider.GetName()}' is not configured; no prices invented.";
    await db.SaveChangesAsync(cancellationToken);
    return;
}
```

Для справжнього моніторингу потрібна реалізація `IPriceProvider`, яка повертає валідні спостереження. У разі реального падіння ціни backend створює `PriceSnapshot`, оновлює стан підписки та надсилає `PriceDrop` notification. Каталожна ціна не є заміною ціни з магазину.

## 1.12 Обробка помилок і валідація API

FluentValidation перевіряє межі вхідних даних до виконання бізнес-операції. `ExceptionHandlingMiddleware` централізовано перетворює відомі прикладні винятки на HTTP-відповідь, а неочікувані помилки записує в лог і повертає 500 без витоку внутрішнього stack trace:

```csharp
try
{
    await _next(context);
}
catch (AppException ex)
{
    _logger.LogWarning(ex, "Application error: {Message}", ex.Message);
    await WriteErrorAsync(context, ex.StatusCode, ex.Message);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unhandled exception");
    await WriteErrorAsync(
        context,
        StatusCodes.Status500InternalServerError,
        "An unexpected error occurred.");
}
```

Звичні статуси: 400 — валідація або бізнес-правило, 401 — недійсний токен чи сесія, 403 — недостатня роль/заборонена дія, 404 — ресурс відсутній або недоступний користувачу, 409 — конфлікт унікальності. Точний JSON-контракт визначається middleware та відповідним endpoint.

## 1.13 Автоматизовані тести backend

Backend інтеграційні тести використовують `WebApplicationFactory<Program>`, окрему InMemory базу для тестового host і реальні HTTP-запити до controller pipeline. У тестовому середовищі задаються окремі JWT значення, а MySQL DbContext замінюється:

```csharp
builder.UseEnvironment("Testing");
builder.UseSetting("Jwt:Key",
    "TEST_ONLY_ScentMatch_Jwt_Signing_Key_32chars!");
builder.UseSetting("Jwt:Issuer", "ScentMatchTest");
builder.UseSetting("Jwt:Audience", "ScentMatchTestClients");

builder.ConfigureServices(services =>
{
    var descriptors = services
        .Where(d =>
            d.ServiceType == typeof(DbContextOptions<ScentMatchDbContext>) ||
            d.ServiceType == typeof(DbContextOptions) ||
            d.ServiceType == typeof(ScentMatchDbContext))
        .ToList();

    foreach (var descriptor in descriptors)
        services.Remove(descriptor);

    services.AddDbContext<ScentMatchDbContext>(options =>
        options.UseInMemoryDatabase(_databaseName));
});
```

Приклад перевірки, що закритий endpoint відхиляє анонімний запит:

```csharp
[Fact]
public async Task Match_Anonymous_ReturnsUnauthorized()
{
    var response = await _client.PostAsJsonAsync(
        "/api/matching",
        new MatchingRequest { Sweetness = 50 });

    Assert.Equal(
        HttpStatusCode.Unauthorized,
        response.StatusCode);
}
```

Перевіряється не лише HTTP-статус, а й поведінка scoring. Наступний тест підтверджує детермінований результат та те, що ближчий рівень характеристики має вищий score:

```csharp
var first = await MatchAsync(token, requestBody);
var second = await MatchAsync(token, requestBody);

var close1 = first.Items.Single(x => x.PerfumeId == close.Id);
var far1 = first.Items.Single(x => x.PerfumeId == far.Id);

Assert.True(close1.Score > far1.Score);
Assert.InRange(close1.Score, 0m, 100m);
Assert.Equal(100m, close1.Score);
Assert.Equal(
    first.Items.Select(x => (x.PerfumeId, x.Score)).ToList(),
    second.Items.Select(x => (x.PerfumeId, x.Score)).ToList());
```

Також перевіряються валідація цін і шкал, відсутні ID довідників, пропущені nullable характеристики, виключення неактивних даних і запис історії. `InMemory` не відтворює всі властивості MySQL (зокрема SQL-переклад, міграції та реальні індекси), тому потрібні окремі перевірки на MySQL.

Запуск:

```powershell
dotnet test
```

## 1.14 Перевірка frontend і запуск локального середовища

Frontend має скрипти Vite, TypeScript build і Oxlint. Окремий test runner не оголошений у його `package.json`, тому відповідні UI-сценарії вимагають ручної перевірки або додавання frontend-тестового фреймворку окремим завданням.

```powershell
# scentmatch-frontend
Copy-Item .env.example .env
npm install
npm run dev
npm run build
npm run lint
```

Файл `.env` фронтенду має містити адресу API:

```dotenv
VITE_API_BASE_URL=http://localhost:5190
```

Backend використовує MySQL connection string та JWT налаштування через змінні середовища:

```powershell
# scentmatch-backend — локальний приклад, замініть пароль і signing key
$env:ConnectionStrings__DefaultConnection =
  "Server=localhost;Port=3306;Database=scentmatch;User=root;Password=CHANGE_ME;"
$env:Jwt__Key = "LOCAL_ONLY_RANDOM_SIGNING_KEY_AT_LEAST_32_CHARACTERS"
$env:Jwt__Issuer = "ScentMatch"
$env:Jwt__Audience = "ScentMatchClients"

dotnet restore
dotnet ef database update --project Infrastructure --startup-project API
dotnet run --project API
```

Секрети production не можна зберігати в `.env.example`, клієнтських `VITE_*` змінних або вихідному коді. Для production потрібно задати сильний JWT key, HTTPS, дозволені CORS origins і підключення до production MySQL.

## 1.15 Висновок

ScentMatch реалізує наскрізний вебсценарій: React-клієнт збирає критерії та формує типізований API-запит; ASP.NET controller перевіряє контекст користувача й передає роботу application service; Infrastructure завантажує профілі ароматів, обчислює score та зберігає необхідні персональні дані у MySQL. Такий поділ відділяє UI, HTTP-контракти, бізнес-правила й зберігання даних.

Докладний огляд функціоналу, інфраструктури й восьми UML-діаграм наведений у [PROJECT_OVERVIEW.uk.md](./PROJECT_OVERVIEW.uk.md). Контракти маршрутів — у [API.uk.md](./API.uk.md), правила matching і платформні модулі — у [PLATFORM.md](./PLATFORM.md).

### Основні файли-джерела прикладів

- Backend startup: `API/Program.cs`
- Backend auth: `API/Controllers/AuthController.cs`, `Infrastructure/Auth/AuthService.cs`, `Infrastructure/Auth/JwtTokenService.cs`
- Backend matching: `API/Controllers/MatchingController.cs`, `Infrastructure/Matching/MatchingService.cs`, `Infrastructure/Matching/MatchingCriteriaScorer.cs`
- Backend scoring: `Application/Common/Scoring/PerfumeScoring.cs`
- Backend persistence: `Infrastructure/Data/ScentMatchDbContext.cs`
- Backend tests: `Tests/Infrastructure/CustomWebApplicationFactory.cs`, `Tests/Matching/MatchingEndpointsTests.cs`
- Frontend routes: `src/app/router/AppRouter.tsx`
- Frontend auth: `src/features/auth/model/AuthContext.tsx`, `src/features/auth/api/authApi.ts`
- Frontend transport: `src/shared/api/httpClient.ts`
- Frontend APIs: `src/features/catalog/api/catalogApi.ts`, `src/features/matching/api/matchingApi.ts`
