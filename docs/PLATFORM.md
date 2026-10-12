# ScentMatch platform expansion

## Shared scoring model

`Application/Common/Scoring/PerfumeScoring` + `PerfumeProfile`:

- Null characteristic / intensity / price / season link means **unknown**, never treated as zero.
- Note similarity uses weighted Top (1.0) / Middle (1.2) / Base (1.4) Jaccard-like overlap.
- Final score = `100 * Σ(weight × similarity) / Σ(weight)` over criteria that had comparable data.
- Response breakdown includes components, positive reasons, mismatches, and data gaps.

## Text matching

Deterministic UA/EN parser (`TextCriteriaParser`) extracts notes, seasons, families, budget, intensity, concentration, and exclusions (`без` / `without`). Optional `ITextCriteriaEnricher` is a no-op unless configured. Results go through the same matching engine; history is stored with `ScenarioType=Text`.

## Gift matching

Uses the shared matcher with gift criteria. Authenticated runs store history as `ScenarioType=Gift` (excluded from personal preference inference).

## Similarity

`GET /api/perfumes/{id}/similar` ranks other active perfumes by notes, family, seasons, shared characteristics, optional intensity/price.

## Price tracking

Subscriptions live in `price_watches`; observations in `price_snapshots`. Catalog `Perfume.Price` is separate from shop/watch prices. Automatic checks require a real `IPriceProvider` implementation and `PriceTracking:Enabled=true`.

## Wardrobe

Built on `collections.ScenarioKey` (`Daily`, `Work`, `Evening`, `Date`, `Summer`, `Winter`, `Special`, `Custom`) — not a duplicate entity.

## Safe apply migration

```powershell
dotnet ef database update --project Infrastructure --startup-project API
# or
dotnet run --project API -- --seed-db
```

Migration: `20261012190000_ExpandPlatformFeatures` (additive; does not drop user data).
