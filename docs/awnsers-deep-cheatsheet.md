# A1 cheatsheet - GET tee time by id

Interview-prep notes for A1 (`GET /api/teetimes/{id:int}`). Q&A format, follow-up questions highlighted.

## Minimal API vs controller

**Q: Should this be a minimal API or a controller?**

Minimal API - this endpoint is small, standalone, and doesn't need shared conventions.

The real deciding factor isn't "how complex is the logic inside the endpoint" - it's **how much cross-cutting, convention-based behavior you need across many endpoints**:

| Use minimal API when... | Use controllers when... |
|---|---|
| Endpoints are mostly independent | You have a large surface (50+ actions) better grouped by class |
| Low ceremony is a priority | You need `[ApiController]` conventions (auto model validation, etc.) |
| No shared filters/attributes needed across many actions | You rely on attribute-based routing, versioning, or shared `ActionFilter`s |

> Follow-up to expect: "what if the logic inside one endpoint gets complex?" - that alone doesn't push you to controllers. Complexity inside one handler doesn't need controller conventions; what pushes you to controllers is needing the *same* cross-cutting behavior declared across *many* endpoints.

## Why convert to UTC / what DST has to do with it

**Q: Why store `StartUtc` and compute `StartLocal` separately?**

Not about comparing two different locations (e.g. Mexico vs USA). It's that **the same course** has a UTC offset that changes twice a year.

- `America/Chicago` is UTC-6 in winter (CST) and UTC-5 in summer (CDT) - same course, same `TimeZoneId`, different offset depending on the date.
- `StartUtc` is the one value that's always unambiguous and comparable across rows/clients - "2026-11-10T14:00:00Z" means the same instant to everyone.
- `StartLocal` is a display convenience: "what time does this look like to a golfer standing at that course right now." It has to be recomputed per-row via `TimeZoneInfo`, because the correct offset depends on the specific date, not just the course's identity.

**Why only convert UTC -> local, never local -> UTC:**

On a DST-transition day (e.g. `Europe/London`, 2026-03-29), local clocks jump from 01:00 straight to 02:00.

- Going **UTC -> local** is always well-defined - every UTC instant maps to exactly one local wall-clock time.
- Going **local -> UTC** is dangerous on that day - 01:30 local either doesn't exist (if clocks spring forward) or is ambiguous (if clocks fall back and 01:30 happens twice).

> Follow-up to expect: "what happens on the DST change day?" - that's exactly why the test suite has `Europe/London` cases straddling 2026-03-29 in `A1_GetTeeTimeByIdTests.cs`.

## Projection vs loading strategies ("the three types")

**Q: Which of the three EF Core loading strategies is cheapest, and is projection one of them?**

Projection is **not** one of the three loading strategies - it's a separate, fourth option, and it's the one A1 actually uses.

| Strategy | How | Example | Cost |
|---|---|---|---|
| Eager loading | `.Include()` | `db.TeeTimes.Include(t => t.Course)` | One query (JOIN), but loads the *full* related entity - every column |
| Explicit loading | `.Entry(x).Reference(...).Load()` | load tracked entity, pull in navigation later, on demand | Extra query per call, but only when you ask |
| Lazy loading | proxies, auto-loads on first access | `teeTime.Course.Name` fires a query the first time you touch `.Course` | Causes N+1 if done in a loop - the one to actively avoid |
| **Projection** (what A1 uses) | `.Select(t => new { ... })` | pulls only `Course.Name`, `Course.TimeZoneId`, `Course.Currency` - no full entity materialized | Cheapest: one query, only the columns you actually need |

> Follow-up to expect: "why is projection cheaper than `.Include`?" - `.Include` still materializes the complete `Course` entity (every column, tracked or not); projection asks SQL to return only the fields the DTO needs, already joined.

## `.Where()` / `.FirstOrDefaultAsync()` are not projection

**Q: If we filter and call `FirstOrDefault` directly on the entity (no anonymous type), does that count as projection?**

No. Projection, filtering, and materialization are three different axes:

| Operator | What it controls | SQL equivalent |
|---|---|---|
| `.Where(t => t.TeeTimeId == id)` | **Which rows** come back | `WHERE TeeTimeId = @id` |
| `.FirstOrDefaultAsync()` | **How many rows** come back, and triggers execution | `TOP 1` / `LIMIT 1`, actually runs the query |
| `.Select(t => new { ... })` | **Which columns** come back | the `SELECT` list itself |

```csharp
// Filtered to one row, but NOT a projection - still pulls every column of TeeTime.
var teeTime = await db.TeeTimes.AsNoTracking()
    .Where(t => t.TeeTimeId == id)
    .FirstOrDefaultAsync(ct);
```

No `.Select()` = no projection, even though it's filtered down to one row. It also still doesn't give you `Course` data - touching `teeTime.Course` afterward without `.Include()` throws (no tracking/proxy) or, with lazy loading on, fires a second query (N+1).

**Projecting straight into a named type still counts as projection.** It isn't about anonymous vs. named - it's whether `.Select()` narrows the column list at all:

```csharp
.Select(t => new TeeTimeDto { Id = t.TeeTimeId, CourseName = t.Course.Name, /* ... */ })
```

**Catch for A1 specifically:** you can't project the *whole* `TeeTimeDto` in one `.Select()`, because `StartLocal` needs `TimeZoneInfo.ConvertTimeFromUtc(...)`, and EF can't translate `TimeZoneInfo` calls to SQL. That's exactly why A1 projects into an anonymous type carrying the raw `StartUtc` + `TimeZoneId`, then does the `TimeZoneInfo` conversion in memory afterward, then maps into the real DTO.

> Follow-up to expect: "so could I skip EF and just use LINQ to Objects?" - yes, `list.FirstOrDefault(t => t.TeeTimeId == id)` is valid, but it's solving a different problem: no DB round trip, so projection/N+1/`AsNoTracking` stop being relevant (it's always a full in-memory O(n) scan), and it's sync (`FirstOrDefault`, no `Async`, no `CancellationToken`) because there's nothing to await. It also means things like `TimeZoneInfo.ConvertTimeFromUtc` work directly inside the `.Where()`/`.Select()`, since there's no SQL translation step.

## Quick reference: AC recap for A1

`GET /api/teetimes/{id:int}` -> `TeeTimeDto { id, courseName, startUtc, startLocal, spotsLeft, price, currency }`

- `404` with ProblemDetails when not found
- `startLocal` via `TimeZoneInfo.FindSystemTimeZoneById` + `ConvertTimeFromUtc` (never the reverse)
- One EF query, `AsNoTracking`, projection (not `.Include`)
- Route constraint `{id:int}`
- `SpotsLeft = Capacity - BookedPlayers` (clamp at 0 - seed data has deliberate overbookings)

---

# A2 cheatsheet - GET tee times list (filter, sort, paging)

Interview-prep notes for A2 (`GET /api/course/{id}/teetimes?date=&minSpots=&sort=&page=&pageSize=`). Code: `CoursesEndpoints.cs`, `GetByIdForTeetimes`.

> **Time log:** about **1 hour with AI** vs. the **30 min** target. The slow parts were the route template (query string inside the route), `Select` vs `SelectMany`, and working out where `endUtc` comes from. Practice paging and the UTC-range step on their own until you can write them from memory.

## Why load the course first?

**Q: Why not query `db.TeeTimes` directly and return 404 if it's empty?**

An empty list has two possible meanings: "the course doesn't exist" (404) or "the course exists but has no tee times that day" (200 with `items: []`). A `TeeTimes` query can't tell these apart. Loading the course first:

- gives a real 404 check
- returns the `TimeZoneId` needed to build the UTC range
- returns `Currency` for the DTO

It's a small projection (`Select(c => new { c.TimeZoneId, c.Currency })`), not the full entity.

> Gotcha from the first attempt: `if (rows is null)` on an `IQueryable` never fires. A query is never null, it just returns zero rows.

## Where does `endUtc` come from?

**Q: `TeeTime` has no `EndUtc` column, so what is `< endUtc` comparing against?**

`startUtc` and `endUtc` are **the bounds of the requested day**, not tee time columns:

```
date = 2026-11-10, course in America/Chicago (UTC-6 in November)
startUtc = local 2026-11-10 00:00 -> 2026-11-10 06:00Z
endUtc   = local 2026-11-11 00:00 -> 2026-11-11 06:00Z
filter:   StartUtc >= startUtc AND StartUtc < endUtc   (half-open range, so no tee time is counted on two days)
```

> Follow-up to expect: "the A1 notes say never convert local -> UTC, so why do it here?" - The risk is a local time that falls inside a DST gap or overlap. US zones change at 02:00 and UK zones at 01:00, so **midnight is never ambiguous** for these courses. Converting a midnight boundary is safe; converting an arbitrary user-typed time like 01:30 is not.

## Sargable predicates

**Q: Why not `t.StartUtc.Date == someDate`?**

Applying a function to the column (`CAST(StartUtc AS date)`) means SQL has to evaluate it for every row, so it **can't seek** an index on `(CourseId, StartUtc)` and has to scan. A range on the raw column (`StartUtc >= @a AND StartUtc < @b`) is *sargable*: the index can jump straight to `@a` and stop at `@b`. It's also wrong for time zones, because the UTC date isn't the course-local date.

## How offset paging works (the code from line 62)

```
pageSize = 20
page 1 -> Skip(0).Take(20)   -> rows 1-20
page 2 -> Skip(20).Take(20)  -> rows 21-40
page 3 -> Skip(40).Take(20)  -> rows 41-60
skip = (page - 1) * pageSize
SQL:  ORDER BY Price, TeeTimeId OFFSET 40 ROWS FETCH NEXT 20 ROWS ONLY
```

Three rules:

1. **Always `OrderBy` before `Skip`/`Take`.** Without it SQL can return rows in any order, so pages are random. EF warns about this.
2. **End with a unique key (`ThenBy(TeeTimeId)`).** Many tee times share a price, and SQL can return tied rows in a different order on each request. The same row can then show up on two pages or on none.
3. **Count the filtered query, not the page.** `totalCount = query.CountAsync()` runs before `Skip`/`Take`.

## Isolated pagination samples (no EF, no API - just the algorithm)

All three use the same in-memory data so you can trace them by hand:

```csharp
record Slot(int Id, decimal Price);

var slots = Enumerable.Range(1, 7).Select(i => new Slot(i, 50m)).ToList(); // ids 1..7, all the same price
const int pageSize = 3;
```

### Option 1 - Offset with plain `if`s (no `Math.Clamp`)

The same logic as `Math.Clamp`, written out:

```csharp
static List<Slot> OffsetPage(List<Slot> source, int page, int pageSize)
{
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 1;
    if (pageSize > 100) pageSize = 100;

    int skip = (page - 1) * pageSize;
    return source.OrderBy(s => s.Price).ThenBy(s => s.Id)
                 .Skip(skip).Take(pageSize).ToList();
}

int totalCount = slots.Count;                              // 7
int totalPages = (totalCount + pageSize - 1) / pageSize;   // (7 + 2) / 3 = 3  (integer ceiling)

OffsetPage(slots, 1, 3); // [1, 2, 3]
OffsetPage(slots, 2, 3); // [4, 5, 6]
OffsetPage(slots, 3, 3); // [7]
OffsetPage(slots, 0, 3); // page fixed to 1 -> [1, 2, 3]
```

Alternatives to fixing bad input:

- **Reject it:** `if (page < 1) return 400`. Strict and explicit, and also fine. Pick one and say why.
- **Default when missing:** bind `int? pageSize` and use `pageSize ?? 20`, so leaving the parameter off isn't an error.

### Option 2 - "Take `pageSize + 1`" (no `CountAsync`)

Ask for one extra row. If it comes back, there's a next page. You save the `COUNT(*)` round trip, but you lose `totalCount` and "page 3 of 12".

```csharp
static (List<Slot> Items, bool HasNext) PageWithLookahead(List<Slot> source, int page, int pageSize)
{
    var rows = source.OrderBy(s => s.Price).ThenBy(s => s.Id)
                     .Skip((page - 1) * pageSize)
                     .Take(pageSize + 1)          // one extra row is the "peek"
                     .ToList();

    bool hasNext = rows.Count > pageSize;
    if (hasNext) rows.RemoveAt(rows.Count - 1);   // drop the peek row, the client never sees it
    return (rows, hasNext);
}

PageWithLookahead(slots, 2, 3); // took [4,5,6,7] -> 4 > 3 -> ([4, 5, 6], hasNext: true)
PageWithLookahead(slots, 3, 3); // took [7]       -> 1 > 3 is false -> ([7], hasNext: false)
```

Good for "Load more" buttons and infinite scroll (C7). **A2 can't use it as-is**, because the AC requires `totalCount`.

### Option 3 - Keyset / cursor (no `Skip`, no `CountAsync`) - the A2 stretch

Instead of "skip N rows", say "give me rows **after the last one I saw**". The client sends back the sort key and id of the last row (the cursor).

```csharp
static (List<Slot> Items, int? NextAfterId) KeysetPage(List<Slot> source, int? afterId, int pageSize)
{
    var rows = source.Where(s => afterId == null || s.Id > afterId)  // seek, don't skip
                     .OrderBy(s => s.Id)
                     .Take(pageSize + 1)                              // reuse the lookahead trick
                     .ToList();

    bool hasNext = rows.Count > pageSize;
    if (hasNext) rows.RemoveAt(rows.Count - 1);
    return (rows, hasNext ? rows[^1].Id : null);
}

KeysetPage(slots, null, 3); // ([1, 2, 3], next: 3)
KeysetPage(slots, 3, 3);    // ([4, 5, 6], next: 6)
KeysetPage(slots, 6, 3);    // ([7], next: null) -> end
```

When ordering by a non-unique column (`StartUtc`), the cursor needs **both** values, and the comparison is a tuple:

```csharp
// ?after=2026-11-10T14:00:00Z,123
.Where(t => t.StartUtc > afterStart
         || (t.StartUtc == afterStart && t.TeeTimeId > afterId))
.OrderBy(t => t.StartUtc).ThenBy(t => t.TeeTimeId)
.Take(pageSize + 1)
```

SQL: `WHERE (StartUtc > @s OR (StartUtc = @s AND TeeTimeId > @id)) ORDER BY StartUtc, TeeTimeId`. With an index on `(CourseId, StartUtc, TeeTimeId)` this is a seek, so page 2,000 costs the same as page 1.

### Comparison

| | Offset (current A2) | Offset + lookahead | Keyset / cursor |
|---|---|---|---|
| Queries per page | 2 (COUNT + page) | 1 | 1 |
| `totalCount` / "page X of Y" | Yes | No | No |
| Jump to any page | Yes | Yes | No, next/prev only |
| Cost of deep pages | Grows: SQL reads and discards every skipped row | Same as offset | Constant, it's an index seek |
| Rows inserted/deleted between requests | Rows shift: duplicates or skipped rows | Same as offset | Stable, the cursor is a fixed point |
| Best for | Admin tables, numbered pages, small data | "Load more" | Infinite scroll, large or changing feeds |

> Follow-up to expect: "When does offset hurt?" - On deep pages (`OFFSET 100000` still reads 100,000 rows) and on data that changes while someone pages through it. A tee sheet changes on every booking: if a slot fills and `minSpots` drops it, every later row moves up one, and the user never sees the row that moved from page 2 to page 1.

> Follow-up to expect: "How would you cache this, and invalidate it on booking?" - Cache key = `courseId + date + minSpots + sort + page + pageSize`, with a short TTL. Invalidate on booking by key prefix `courseId:date`, or keep a version number per course-day, put it in the key, and bump it on every booking so old entries stop being read.

## Gotchas (REVIEW LATER in the code)

- **Missing `page`/`pageSize`/`minSpots` returns 400.** They're non-nullable `int`, so minimal APIs require them. Use `int?` with defaults if they should be optional.
- **Bad `date` returns 500.** `DateOnly.ParseExact` throws. Use `TryParseExact` and return a 400 ProblemDetails. "Validation of input" is on the interviewer checklist.
- **Overflow on huge `page`.** `(page - 1) * pageSize` with `page = int.MaxValue` wraps to a negative number, and SQL rejects a negative `OFFSET`, so the request fails with a 500. Cap `page` as well, or compute `skip` as a `long` and reject when it's too large.
- **`ToList()` before filtering.** Everything stays `IQueryable` until the final `ToListAsync()`. An earlier `.ToList()` or `.AsEnumerable()` pulls every row into memory and does the filter, sort and paging in C#.

## Quick reference: AC recap for A2

`GET /api/course/{id:int}/teetimes` -> `PagedResult<TeeTimeDto> { items, page, pageSize, totalCount }`

- `date` = course-local day -> `[startUtc, endUtc)` via the course `TimeZoneId`. No `.Date` on the column.
- `minSpots`: `Capacity - BookedPlayers >= minSpots`
- `sort`: `time` (default) or `price`, anything else -> 400
- `pageSize` capped at 100. Stable order: `OrderBy(...).ThenBy(TeeTimeId)`, then `Skip`/`Take`
- 404 if the course doesn't exist (check the course, not the tee time list)
