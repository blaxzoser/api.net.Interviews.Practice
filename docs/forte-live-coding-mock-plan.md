# Forte Group — Live Coding & Mock Interview Practice Plan
**Role:** Senior Full Stack .NET Engineer & AI Practitioner (likely client: golf tee-time / booking platform)
**Stack:** .NET 8+ / ASP.NET Core · EF Core · React · TypeScript · SQL Server · Claude Code / MCP

> **Goal:** Do every exercise yourself under a timer and out loud, so the live coding round feels like something you have already done many times.
> All exercises use **one domain** (courses, tee times, golfers, bookings, payments). You build one practice project and reuse it everywhere.

---

## Table of Contents
0. [How to use this plan](#0-how-to-use-this-plan)
1. [Practice project setup](#1-practice-project-setup)
2. [14-day schedule](#2-14-day-schedule)
3. [Mock A — .NET Web API live coding](#3-mock-a--net-web-api-live-coding)
4. [Mock B — EF Core](#4-mock-b--ef-core)
5. [Mock C — React + TypeScript live coding](#5-mock-c--react--typescript-live-coding)
6. [Mock D — SQL Server](#6-mock-d--sql-server)
7. [Mock E — PR / code review rounds](#7-mock-e--pr--code-review-rounds)
8. [Mock F — AI-assisted round (Claude Code, MCP)](#8-mock-f--ai-assisted-round-claude-code-mcp)
9. [Mock G — System design (client round)](#9-mock-g--system-design-client-round)
10. [Full mock interview scripts](#10-full-mock-interview-scripts)
11. [Self-scoring rubric](#11-self-scoring-rubric)
12. [Answer keys (spoilers — read only after attempting)](#12-answer-keys-spoilers)

---

## 0. How to use this plan

### Rules for every exercise
1. **Set a timer** to the time box shown. When it rings, stop and score yourself, even if you aren't finished.
2. **Talk out loud the whole time.** Record your screen and voice and watch 5 minutes of it afterwards. Silence is the most common reason people fail live coding.
3. **Do each core exercise twice:**
   - **Pass 1 — no AI, no copy-paste** (some interviews ban tools).
   - **Pass 2 — with Claude Code**, following the verification workflow in Mock F. You must be able to explain every line it writes.
4. **Every exercise ends green:** it builds, the tests pass, and you demonstrate it (Swagger/.http file, browser, or SQL result).
5. Write down which follow-up questions you couldn't answer, and review them the next day.

### The 2-minute opening talk track (use at the start of EVERY live task)
1. **Restate:** "So I need to build X, which takes Y and returns Z."
2. **Clarify (2–4 questions):** inputs/validation, edge cases, scale, auth, what "done" means.
3. **State assumptions:** "I'll assume UTC storage and a capacity of 4 per tee time."
4. **Plan:** "First the model, then the endpoint, then one happy-path test, then edge cases."
5. **Time check:** "I'll get a working version first, then improve it."

### The closing talk track (last 3 minutes)
- What you would add with more time (tests, validation, logging, caching, security).
- What the trade-offs are and where it would break at scale.

---

## 1. Practice project setup

> Allow 2–3 hours on Day 1. Leave stubs where it says so: the exercises fill them in.

### 1.1 Infrastructure
- [ ] SQL Server in Docker (`mcr.microsoft.com/mssql/server:2022-latest`), with your own local SA password
- [ ] Azure Data Studio or SSMS; turn on **Include Actual Execution Plan**
- [ ] .NET SDK 8 (what the ad says); .NET 10 LTS is fine too
- [ ] Node LTS + Vite (`react-ts` template)
- [ ] Claude Code CLI installed and logged in (for Pass 2 of each exercise)
- [ ] Git repo with one branch per exercise (`ex/A3-post-booking`), so you can show the history

### 1.2 Solution layout
```
teetime/
├── src/
│   ├── TeeTime.Api/            # ASP.NET Core (minimal APIs AND one controller — practice both)
│   ├── TeeTime.Domain/         # entities, rules (no EF references)
│   └── TeeTime.Infrastructure/ # DbContext, configurations, migrations
├── tests/
│   ├── TeeTime.UnitTests/      # xUnit
│   └── TeeTime.IntegrationTests/ # WebApplicationFactory + Testcontainers.MsSql
├── web/                        # Vite React + TS, Vitest, React Testing Library, MSW
├── sql/                        # schema.sql, seed.sql, exercises/*.sql
├── CLAUDE.md                   # you write this in Mock F
└── requests.http               # manual API calls
```

### 1.3 Domain model (use in .NET, SQL and React)
| Entity | Fields |
|---|---|
| **Course** | CourseId, Name, TimeZoneId (IANA, e.g. `America/Chicago`, `Europe/London`), Country (`US`/`UK`), Currency (`USD`/`GBP`) |
| **TeeTime** | TeeTimeId, CourseId, StartUtc, Capacity (default 4), BookedPlayers, Price, RowVersion |
| **Golfer** | GolferId, FirstName, LastName, Email, IsMember |
| **Booking** | BookingId, TeeTimeId, GolferId, Players (1–4), Status (`Held`/`Confirmed`/`Cancelled`), CreatedUtc, HoldExpiresUtc, IdempotencyKey |
| **Payment** | PaymentId, BookingId, Amount, Currency, Status (`Pending`/`Captured`/`Failed`/`Refunded`), CreatedUtc |

**Business rules** (several exercises test these):
- R1: The sum of players in non-cancelled bookings for a tee time can never be more than Capacity.
- R2: A booking can be cancelled for free up to **24 h** before the start, in the course's **local** time.
- R3: A `Held` booking expires after **10 minutes** if it isn't confirmed.
- R4: Members can book **14 days** ahead; visitors **7 days**.
- R5: The same `Idempotency-Key` must never create two bookings.

### 1.4 SQL schema + seed (run in `sql/`)
```sql
CREATE DATABASE TeeTimeDb;
GO
USE TeeTimeDb;
GO
CREATE TABLE Courses (
  CourseId   INT IDENTITY PRIMARY KEY,
  Name       NVARCHAR(100) NOT NULL,
  TimeZoneId VARCHAR(50)   NOT NULL,
  Country    CHAR(2)       NOT NULL,
  Currency   CHAR(3)       NOT NULL
);
CREATE TABLE TeeTimes (
  TeeTimeId     INT IDENTITY PRIMARY KEY,
  CourseId      INT NOT NULL REFERENCES Courses(CourseId),
  StartUtc      DATETIME2(0) NOT NULL,
  Capacity      TINYINT NOT NULL DEFAULT 4,
  BookedPlayers TINYINT NOT NULL DEFAULT 0,
  Price         DECIMAL(8,2) NOT NULL,
  RowVersion    ROWVERSION
);
CREATE TABLE Golfers (
  GolferId  INT IDENTITY PRIMARY KEY,
  FirstName NVARCHAR(50) NOT NULL,
  LastName  NVARCHAR(50) NOT NULL,
  Email     VARCHAR(200) NOT NULL,
  IsMember  BIT NOT NULL
);
CREATE TABLE Bookings (
  BookingId      INT IDENTITY PRIMARY KEY,
  TeeTimeId      INT NOT NULL REFERENCES TeeTimes(TeeTimeId),
  GolferId       INT NOT NULL REFERENCES Golfers(GolferId),
  Players        TINYINT NOT NULL,
  Status         VARCHAR(10) NOT NULL,
  CreatedUtc     DATETIME2(0) NOT NULL,
  HoldExpiresUtc DATETIME2(0) NULL,
  IdempotencyKey VARCHAR(64) NULL
);
CREATE TABLE Payments (
  PaymentId  INT IDENTITY PRIMARY KEY,
  BookingId  INT NOT NULL REFERENCES Bookings(BookingId),
  Amount     DECIMAL(8,2) NOT NULL,
  Currency   CHAR(3) NOT NULL,
  Status     VARCHAR(10) NOT NULL,
  CreatedUtc DATETIME2(0) NOT NULL
);
-- NOTE: deliberately NO indexes beyond PKs and NO check constraints. You add them in Mock D.

INSERT INTO Courses (Name, TimeZoneId, Country, Currency) VALUES
 ('Cedar Ridge GC','America/Chicago','US','USD'),
 ('Lakeside Links','America/New_York','US','USD'),
 ('Desert Palms','America/Phoenix','US','USD'),
 ('Hollow Moor','Europe/London','UK','GBP'),
 ('Kingsfield Park','Europe/London','UK','GBP');

-- 60 days x 67 slots (every 10 min) x 5 courses ≈ 20k tee times
INSERT INTO TeeTimes (CourseId, StartUtc, Capacity, BookedPlayers, Price)
SELECT c.CourseId,
       DATEADD(MINUTE, s.i*10, DATEADD(HOUR, 12, DATEADD(DAY, d.i, CAST('2026-11-01' AS DATETIME2(0))))),
       4, 0,
       CAST(40 + ABS(CHECKSUM(NEWID())) % 60 AS DECIMAL(8,2))
FROM Courses c
CROSS JOIN (SELECT TOP (60) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i FROM sys.all_objects) d
CROSS JOIN (SELECT TOP (67) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i FROM sys.all_objects) s;

INSERT INTO Golfers (FirstName, LastName, Email, IsMember)
SELECT TOP (5000)
  CONCAT('First', n), CONCAT('Last', n % 900), CONCAT('golfer', n, '@example.com'),
  CASE WHEN n % 3 = 0 THEN 1 ELSE 0 END
FROM (SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
      FROM sys.all_objects a CROSS JOIN sys.all_objects b) x;

-- Random bookings: this WILL create overbookings on purpose (used in Mock D)
DECLARE @tt INT = (SELECT COUNT(*) FROM TeeTimes);
INSERT INTO Bookings (TeeTimeId, GolferId, Players, Status, CreatedUtc)
SELECT TOP (60000)
  1 + ABS(CHECKSUM(NEWID())) % @tt,
  1 + ABS(CHECKSUM(NEWID())) % 5000,
  1 + ABS(CHECKSUM(NEWID())) % 4,
  CASE ABS(CHECKSUM(NEWID())) % 10 WHEN 0 THEN 'Cancelled' WHEN 1 THEN 'Held' ELSE 'Confirmed' END,
  DATEADD(MINUTE, -(ABS(CHECKSUM(NEWID())) % 100000), SYSUTCDATETIME())
FROM sys.all_objects a CROSS JOIN sys.all_objects b;

-- Denormalized counter (capped at 255; some rows will be WRONG on purpose)
UPDATE t SET BookedPlayers = CASE WHEN x.P > 255 THEN 255 ELSE x.P END
FROM TeeTimes t
JOIN (SELECT TeeTimeId, SUM(Players) P FROM Bookings WHERE Status <> 'Cancelled' GROUP BY TeeTimeId) x
  ON x.TeeTimeId = t.TeeTimeId;
UPDATE TOP (50) TeeTimes SET BookedPlayers = 0 WHERE BookedPlayers > 0;  -- corrupt 50 rows

INSERT INTO Payments (BookingId, Amount, Currency, Status, CreatedUtc)
SELECT b.BookingId, t.Price * b.Players, c.Currency,
       CASE ABS(CHECKSUM(NEWID())) % 20 WHEN 0 THEN 'Failed' WHEN 1 THEN 'Refunded' ELSE 'Captured' END,
       DATEADD(SECOND, 30, b.CreatedUtc)
FROM Bookings b JOIN TeeTimes t ON t.TeeTimeId = b.TeeTimeId JOIN Courses c ON c.CourseId = t.CourseId
WHERE b.Status = 'Confirmed';
```
> Seeds are random, so your numbers will differ from anyone else's. That's fine. In the EF Core project, **scaffold or map onto this existing schema first**. Practise migrations separately in B5.

---

## 2. 14-day schedule
About 2–3 h on weekdays and 4–5 h on weekend days. Adjust to your interview date.

| Day | Focus | Exercises | Mock |
|---|---|---|---|
| 1 | Setup + warm-up | §1 setup, C1, D1 | — |
| 2 | .NET API core | A1, A2, A3 | — |
| 3 | .NET API core | A4, A5, A9, theory rapid-fire A | — |
| 4 | SQL | D2, D3, D4, D5 | — |
| 5 | React core | C2, C3, C4, TS katas 1–6 | — |
| 6 | **Full Mock #1** (Forte technical) | — | §10 Mock #1 |
| 7 | EF Core + review | B1–B4, E1 (.NET PR review) | — |
| 8 | Advanced API | A6, A7, A8, A12 | — |
| 9 | React advanced | C5, C6, C7, E2 (React PR review), TS katas 7–12 | — |
| 10 | SQL advanced | D6–D10 | — |
| 11 | AI round | F1–F5 | — |
| 12 | **Full Mock #2** (client round + system design) | G1 | §10 Mock #2 |
| 13 | Weak spots | Redo your 3 lowest-scored exercises, A10, A11, C8–C10 | — |
| 14 | **Full Mock #3** (AI-assisted live task) + rest | Light review only | §10 Mock #3 |

---

## 3. Mock A — .NET Web API live coding

### 3.0 What interviewers check in ANY API exercise
- [ ] Correct **HTTP verb + status code** (table below)
- [ ] **DTOs** in and out. Never expose EF entities. No over-posting.
- [ ] **Validation** returning `400`/`422` with **ProblemDetails**
- [ ] **async all the way** + `CancellationToken` passed down to EF/HttpClient
- [ ] **No N+1**; projection with `Select`; `AsNoTracking` for reads
- [ ] **Concurrency** thought through on writes
- [ ] **DI lifetimes** correct; no `new HttpClient()`; no static DbContext
- [ ] **UTC** everywhere; local time only for display or business rules (R2)
- [ ] **Errors**: global exception handler, no swallowed exceptions, no stack traces leaked
- [ ] **At least one test**, and you can explain the testing strategy

### 3.1 HTTP verbs & status code matrix (memorize)
| Endpoint | Success | Errors to handle |
|---|---|---|
| `GET /api/courses/{courseId}/teetimes?date=&minSpots=&page=&pageSize=` | `200` + paged result | `400` bad params, `404` course |
| `GET /api/teetimes/{id}` | `200` (+ `ETag`) | `404` |
| `POST /api/bookings` | `201` + `Location` header + body | `400`/`422` validation, `404` tee time, `409` full/conflict |
| `POST /api/bookings` (same Idempotency-Key replay) | the **same** `201` response as the first call | `422` key reused with a different payload |
| `PUT /api/bookings/{id}` (full replace) | `200` or `204` | `404`, `412` stale `If-Match` |
| `PATCH /api/bookings/{id}` (players only) | `200` | `400`, `409` would exceed capacity |
| `DELETE /api/bookings/{id}` (cancel) | `204` | `404`, `409` inside cancellation window (or `422` — just justify your choice) |
| `POST /api/bookings/{id}/confirm` (state transition) | `200` | `404`, `409` hold expired / wrong state |
| Any endpoint without auth | — | `401` unauthenticated, `403` not allowed |

---

### A1 — GET by id (15 min) ★ warm-up
**Build:** `GET /api/teetimes/{id}` returns `TeeTimeDto { id, courseName, startUtc, startLocal, spotsLeft, price, currency }`.
**Acceptance:**
- `404` with ProblemDetails when not found
- `startLocal` uses the course's IANA time zone (`TimeZoneInfo.FindSystemTimeZoneById`)
- One EF query (check the SQL log), `AsNoTracking`, projection
- Route constraint `{id:int}`

**Follow-ups:** Minimal API vs controller: which and why? `Results<Ok<T>, NotFound>` typed results? What happens to `startLocal` on a DST change day?

---

### A2 — GET list with filter, sort, paging (30 min) ★ core
**Build:** `GET /api/courses/{courseId}/teetimes?date=2026-11-10&minSpots=2&sort=price&page=1&pageSize=20`
**Acceptance:**
- `date` is the **course-local date**; convert it to a UTC range (`>= startUtc AND < endUtc`). Don't use `.Date` on the column.
- `minSpots` filters on `Capacity - BookedPlayers >= minSpots`
- `sort` allowed values: `time` (default), `price`; anything else → `400`
- `pageSize` capped at 100; response `{ items, page, pageSize, totalCount }`
- `404` if the course doesn't exist

**Interviewer checks:** sargable predicates, `Skip/Take` with a stable `OrderBy`, no `ToList()` before filtering, validation of input.
**Follow-ups:** Offset vs keyset pagination: when does offset hurt? How would you cache this, and how would you invalidate it when someone books?
**Stretch:** keyset (`?after=<startUtc,id>`) version.

---

### A3 — POST create booking (35 min) ★★ most likely live task
**Build:** `POST /api/bookings` with `CreateBookingRequest { teeTimeId, golferId, players }`.
**Acceptance:**
- Validation: players 1–4; tee time exists; tee time is in the future; R4 (members 14 days / visitors 7 days)
- **R1 enforced safely under concurrency.** Pick one approach and explain it:
  - (a) atomic conditional `UPDATE ... WHERE BookedPlayers + @p <= Capacity`, check rows affected, or
  - (b) EF Core optimistic concurrency on `RowVersion` + retry, or
  - (c) a transaction with `UPDLOCK, ROWLOCK` hint
- Creates the booking as `Held` with `HoldExpiresUtc = now + 10 min` (R3)
- Returns `201 Created` with a `Location` header pointing to `GET /api/bookings/{id}`
- `409` with ProblemDetails when full

**Test to write:** an integration test that fires **10 parallel POSTs** for 1 player each at a tee time with capacity 4. Assert exactly 4 succeed and 6 get `409`. **This is the test that impresses.**
**Follow-ups:** Why is "read, check, then write" a race? Which isolation level would make it safe, and what would that cost? How do you handle `DbUpdateConcurrencyException`?

---

### A4 — PATCH change players (20 min)
**Build:** `PATCH /api/bookings/{id}` with `{ players }`. Change the booked count by the difference, respecting R1.
**Checks:** the same concurrency approach as A3; `404`; can't change a cancelled booking (`409`).
**Follow-up:** PUT vs PATCH semantics; JSON Patch vs a simple partial DTO.

---

### A5 — DELETE cancel booking (20 min)
**Build:** `DELETE /api/bookings/{id}` → soft cancel (`Status = Cancelled`), free up the spots.
**Acceptance:** R2, the 24 h rule in **course local time**; return `409` (or `422`) inside the window; `204` on success; cancelling twice → still `204` (idempotent) or `409`, but say which and why.
**Follow-ups:** Soft vs hard delete; EF global query filter for cancelled rows; what about the refund? (That leads to A10/outbox.)

---

### A6 — Idempotency-Key on POST (30 min)
**Build:** Middleware or an endpoint filter: when the `Idempotency-Key` header is present, store the key + request hash + response. Replaying the same key returns the **stored response**. The same key with a different body → `422`.
**Checks:** a unique index on the key; what happens if two identical requests arrive at the same millisecond (the unique constraint wins); key expiry.
**Follow-up:** Why do payments and booking APIs need this? (Mobile retries, client timeouts.)

---

### A7 — Optimistic concurrency with ETag / If-Match (25 min)
**Build:** `GET /api/teetimes/{id}` returns an `ETag` from `RowVersion`. `PUT /api/teetimes/{id}` (admin changes price) requires `If-Match`. A stale ETag → `412 Precondition Failed`.
**Follow-up:** `409` vs `412`; how the React UI should react (refetch and show a diff).

---

### A8 — Hold expiry with BackgroundService (30 min)
**Build:** A `BackgroundService` that runs every minute, finds `Held` bookings past `HoldExpiresUtc`, marks them `Cancelled` and releases the spots.
**Checks:** create a **scope per iteration** (`IServiceScopeFactory`), not an injected DbContext (captive dependency); `PeriodicTimer`; respect `stoppingToken`; set-based update (`ExecuteUpdateAsync`) instead of a loop; what if two API instances run the job (leader election, or make the update itself safe).
**Follow-up:** Hosted service vs Hangfire/Quartz vs Azure Functions.

---

### A9 — Middleware: correlation id + global error handling (20 min)
**Build:**
1. Middleware: read `X-Correlation-Id` or generate one, add it to the response header and the logging scope.
2. `IExceptionHandler` (.NET 8+) mapping domain exceptions to ProblemDetails (`NotFoundException` → 404, `ConflictException` → 409), and anything else → 500 without the stack trace.

**Follow-ups:** Middleware order (`UseExceptionHandler` first; auth before authorization); middleware vs filters vs endpoint filters.

---

### A10 — Typed HttpClient to an external payment API (30 min)
**Build:** `IPaymentGateway` with `ChargeAsync(bookingId, amount, currency, ct)`, implemented as a **typed client** registered with `AddHttpClient<>`. Add a resilience handler (retry with backoff for transient errors, timeout, circuit breaker). Fake the external API with a second minimal API, or WireMock.Net.
**Checks:** no `new HttpClient()`; `CancellationToken`; **don't retry non-idempotent calls without an idempotency key**; options pattern for the base URL.
**Follow-up:** "The booking saved but the payment call timed out. Now what?" → outbox, a pending status, reconciliation.

---

### A11 — Authorization policies (20 min)
**Build:** JWT bearer auth (dev tokens with `dotnet user-jwts`). Policies: `MemberOnly` for the 14-day window, `CourseAdmin` for price changes, scoped to a `courseId` claim.
**Follow-ups:** Role vs claims vs policy; resource-based authorization (`IAuthorizationService` with the course as the resource).

---

### A12 — Integration tests (40 min)
**Build:** `WebApplicationFactory<Program>` + **Testcontainers.MsSql** (a real SQL Server, not InMemory). Tests: A1 404, A2 paging, A3 parallel booking, A5 window rule (inject `TimeProvider` so you can fake "now").
**Follow-ups:** Why not the EF InMemory provider? (It doesn't behave like SQL: no transactions, no constraints, different translation.) Unit vs integration split; what you mock.

---

### A — Theory rapid-fire (practise answering in under 45 s each)
1. Request pipeline order, start to finish.
2. Singleton/scoped/transient + captive dependency + `ValidateScopes`.
3. What happens to the thread at `await`? Why is `.Result` dangerous?
4. `IOptions` vs `IOptionsSnapshot` vs `IOptionsMonitor`.
5. Why `IHttpClientFactory`? (Socket exhaustion + DNS changes.)
6. Minimal APIs vs controllers: trade-offs.
7. ProblemDetails / RFC 9457.
8. Rate limiting middleware: fixed vs sliding vs token bucket.
9. Output caching vs response caching vs `HybridCache`/`IMemoryCache`/Redis.
10. `Task` vs `ValueTask`; `IAsyncEnumerable` streaming.
11. Records vs classes for DTOs; `required` members; nullable reference types.
12. How you'd version an API (URL vs header) and keep old clients working.
13. Health checks + OpenTelemetry: what you'd trace in booking.
14. Where business rules live (domain vs service vs endpoint).

---

## 4. Mock B — EF Core

### B1 — Kill the N+1 (20 min)
Rewrite this so it uses **one** query. Prove it with the SQL log.
```csharp
// Dashboard: tee times per course for a given day
var result = new List<CourseDayDto>();
var courses = await db.Courses.ToListAsync();
foreach (var c in courses)
{
    var count = await db.TeeTimes
        .Where(t => t.CourseId == c.CourseId && t.StartUtc.Date == day)
        .CountAsync();
    var booked = db.Bookings
        .Where(b => b.TeeTime.CourseId == c.CourseId && b.Status != "Cancelled")
        .ToList()
        .Sum(b => b.Players);
    result.Add(new CourseDayDto(c.Name, count, booked));
}
```
**Find:** N+1, non-sargable `.Date`, client-side `Sum`, `booked` ignores the day, tracking, sync `ToList` inside an async method.

### B2 — Tracking vs no-tracking (15 min)
Load 1,000 bookings with `Include(b => b.Golfer)`, with and without `AsNoTracking`. Compare memory and time. Then explain `AsNoTrackingWithIdentityResolution`.

### B3 — Cartesian explosion (15 min)
Query courses with `Include(TeeTimes).ThenInclude(Bookings)`. Check the row count in SQL. Fix it with `AsSplitQuery()` or a projection. Explain the trade-off (consistency across multiple round-trips).

### B4 — Bulk operations (15 min)
Expire holds (A8) with `ExecuteUpdateAsync`. Explain what it **bypasses**: change tracker, `SaveChanges` interceptors, concurrency tokens.

### B5 — Migrations in production (20 min, partly theory)
Add a `Notes` column and rename `Price` → `GreenFee` with **zero downtime** (expand → migrate data → contract). Generate an idempotent script (`dotnet ef migrations script --idempotent`).
**Talk:** why not call `Database.Migrate()` at app start across 3 instances.

### B6 — Global query filter + soft delete (15 min)
A filter to hide cancelled bookings; `IgnoreQueryFilters()` for admin. Follow-up: the multi-tenant filter by course/club id.

### B7 — Raw SQL safely (10 min)
Search golfers by last name with `FromSql($"...")` (interpolated, parameterized) vs `FromSqlRaw` with concatenation (injection). Show the generated parameters.

### B — Theory rapid-fire
DbContext lifetime and pooling · `IDbContextFactory` in background work · lazy loading pitfalls · compiled queries · owned types/value objects · concurrency tokens · transactions and `BeginTransactionAsync` · `SaveChanges` batching · how LINQ gets translated · client vs server evaluation · interceptors for audit columns.

---

## 5. Mock C — React + TypeScript live coding

### 5.0 What interviewers check in ANY React task
- [ ] Types: no `any`; props typed; API response typed; discriminated unions for async state
- [ ] `useEffect` deps correct; cleanup / `AbortController`; no race conditions
- [ ] No state mutation; derived state computed, not stored
- [ ] Stable, unique `key`s (never the array index for dynamic lists)
- [ ] Loading, **error**, and empty states
- [ ] Accessibility: `<button>` not clickable `<div>`, labels, keyboard, `aria-*` where needed
- [ ] No double submit; disabled states
- [ ] Component split and naming; custom hooks for logic
- [ ] At least one RTL test (user-centric queries: `getByRole`)

### C1 — Data transformation kata (20 min) ★ reported Forte task type
Plain TypeScript, no React. Write `summarize(bookings)`:
```ts
type Status = "Held" | "Confirmed" | "Cancelled";
interface Booking { id: number; course: string; startUtc: string; players: number; status: Status; }

const bookings: Booking[] = [
  { id: 1, course: "Cedar Ridge", startUtc: "2026-11-02T14:00:00Z", players: 2, status: "Confirmed" },
  { id: 2, course: "Cedar Ridge", startUtc: "2026-11-02T14:10:00Z", players: 4, status: "Cancelled" },
  { id: 3, course: "Lakeside",    startUtc: "2026-11-02T15:00:00Z", players: 3, status: "Confirmed" },
  { id: 4, course: "Cedar Ridge", startUtc: "2026-11-02T14:50:00Z", players: 1, status: "Held" },
  { id: 5, course: "Lakeside",    startUtc: "2026-11-02T16:20:00Z", players: 4, status: "Confirmed" },
];
```
**Rules:** per course: `confirmedPlayers` = sum of players with status Confirmed; `byHour` = booking ids grouped by **UTC hour** (two-digit string), **excluding Cancelled**. Sort courses by `confirmedPlayers` desc.
**Expected output (write it as a Vitest test first):**
```ts
[
  { course: "Lakeside",    confirmedPlayers: 7, byHour: { "15": [3], "16": [5] } },
  { course: "Cedar Ridge", confirmedPlayers: 2, byHour: { "14": [1, 4] } },
]
```
**Then:** do it again with only `reduce`; then write a generic `groupBy<T, K extends PropertyKey>(items: T[], key: (t: T) => K): Record<K, T[]>`.
**Follow-ups:** Big-O; immutability; what about `Object.groupBy`/`Map`?

### C2 — Typed data-fetching hook (25 min) ★★
**Build:** `useFetch<T>(url: string)` returning a discriminated union:
`{ status: "idle" } | { status: "loading" } | { status: "success"; data: T } | { status: "error"; error: Error }`
**Acceptance:** `AbortController` cancels on unmount **and** on URL change (no stale response overwrites a newer one); checks `res.ok`; refetches when the URL changes; a consumer component uses `switch (state.status)` with an exhaustive `never` check.
**Follow-up:** Why would you use TanStack Query / RTK Query instead in production? (Caching, dedupe, retries, invalidation.)

### C3 — Tee sheet list with filters (35 min) ★★ most likely live task
**Build:** `<TeeSheet courseId date />` calls your A2 endpoint (or MSW mock). It shows time (course local), price, spots left, and a "Book" button.
**Acceptance:**
- Filter by min spots (select 1–4) and a max-price slider; derived list, not duplicated state
- Text search on time ("14:") **debounced** 300 ms (write `useDebounce<T>`)
- Full slots show "Full" and the button is disabled
- Loading skeleton, error with retry, and an empty state
- Keyboard accessible; labels on inputs

**Follow-ups:** Where does the filter state live (URL search params so it's shareable)? 5,000 rows → virtualization.

### C4 — Booking form (30 min)
**Build:** `<BookingForm teeTime />`: players (1–4), golfer email, accept terms. Client validation + server errors (map ProblemDetails `errors` to fields). Disable while submitting; prevent double submit; on `409` show "Someone just took that slot" and trigger a refetch.
**Stretch:** React Hook Form + Zod schema; infer the TS type from the schema (`z.infer`).

### C5 — The reported Redux question, as code (30 min) ★ reported
> "Click a button → request data → display it instead of the button."
**Build it twice:**
1. **Redux Toolkit** `createSlice` + `createAsyncThunk` (pending/fulfilled/rejected) + typed `useAppSelector`/`useAppDispatch`
2. **RTK Query** or **TanStack Query** with the same UI

**Be ready to narrate the data flow step by step** (dispatch → middleware → reducer → store → selector → re-render), and say when you would **not** use Redux at all.

### C6 — Optimistic update with rollback (25 min)
Booking a slot instantly decrements spots in the UI. If the API returns an error, roll back and show a toast. Do it with TanStack Query `onMutate`/`onError`/`onSettled`, or by hand.
**Follow-up:** When is optimistic UI wrong? (Payments, scarce inventory: maybe show "pending" instead.)

### C7 — Pagination / infinite scroll (25 min)
Infinite list of bookings using the keyset API from A2-stretch, with `IntersectionObserver`. Handle the end of the list and errors mid-scroll.

### C8 — Time zones in the UI (15 min)
Render `startUtc` in the **course** time zone, not the browser's (`Intl.DateTimeFormat` with `timeZone`). Show "Tomorrow 7:10 AM (London time)". Test it across a DST date.

### C9 — Generic table component (25 min)
`<DataTable<T> rows columns />` where `columns: { key: keyof T; header: string; render?: (row: T) => ReactNode; sortable?: boolean }[]`. Typed sorting. Use it for bookings and golfers.

### C10 — Tests with React Testing Library + MSW (30 min)
Test C3: renders slots, filtering works, the Book button is disabled when full, the error state shows retry. Use `userEvent` and `findByRole`; no implementation details.

### TypeScript katas (10–15 min each)
1. Model `BookingStatus` as a union and write an exhaustive `switch` with `assertNever`.
2. `Result<T, E> = { ok: true; value: T } | { ok: false; error: E }` + a helper `safeFetch<T>()`.
3. Type guard `isTeeTime(x: unknown): x is TeeTime` for runtime-validating an API response (then do it with Zod).
4. Typed `debounce<A extends unknown[]>(fn: (...args: A) => void, ms)`.
5. `DeepPartial<T>` and `DeepReadonly<T>`.
6. Use `Pick`, `Omit`, `Partial`, `Required`, `Record` to derive `CreateBookingRequest` from `Booking`.
7. `keyof` + indexed access: `getProp<T, K extends keyof T>(obj: T, key: K): T[K]`.
8. Template literal type for routes: `` `/api/courses/${number}/teetimes` ``.
9. `satisfies` vs type annotation: explain with a config object.
10. Typed `useLocalStorage<T>(key, initial)` with a `try/catch`.
11. Branded types: `type CourseId = number & { __brand: "CourseId" }`. Why?
12. Generate TS types from your API's OpenAPI document (`openapi-typescript`) and use them in C2.

### C — Theory rapid-fire
Reconciliation and keys · when `useMemo`/`useCallback` actually help · `React.memo` · controlled vs uncontrolled · lifting state vs context vs a store · context re-render problem · error boundaries · Suspense · StrictMode double effects · `useRef` uses · `useLayoutEffect` vs `useEffect` · code splitting with `lazy` · server state vs client state · XSS and `dangerouslySetInnerHTML` · CORS basics · where you store JWTs.

---

## 6. Mock D — SQL Server
> Write each query in `sql/exercises/Dxx.sql`. For every query, **look at the actual execution plan** and note scans vs seeks.

### D1 — Joins & aggregation (15 min) ★ warm-up
Per course, per local date (approximate with `AT TIME ZONE`), show: tee times offered, players booked (non-cancelled), utilization % (booked / capacity). Order by utilization desc.

### D2 — Window functions (25 min) ★★
a) Top 3 golfers by confirmed rounds **per course** (`DENSE_RANK`).
b) Running total of captured payment revenue per course per day (`SUM() OVER (PARTITION BY ... ORDER BY ...)`).
c) For each booking, the golfer's previous booking date and the gap in days (`LAG`).

### D3 — Data integrity hunt (20 min) ★★
a) Find tee times where the non-cancelled player sum **exceeds Capacity** (overbookings: the seed created some).
b) Find tee times where `BookedPlayers` doesn't match the real sum (the 50 corrupted rows).
c) Write a **single set-based UPDATE** that fixes `BookedPlayers`.
d) Golfers with two non-cancelled bookings within 30 minutes of each other on the same day (self-join or `LAG`).

### D4 — Atomic booking statement (20 min) ★★
Write a stored procedure `usp_BookTeeTime @TeeTimeId, @GolferId, @Players` that:
- Increments `BookedPlayers` **only if** capacity allows, in one statement, and checks `@@ROWCOUNT`
- Inserts the booking in the same transaction
- Returns a clear error (`THROW`) when full
- Uses `SET XACT_ABORT ON` and `TRY/CATCH`

**Prove it:** open two query windows and run concurrent calls (use `WAITFOR DELAY` inside the transaction to widen the window). Then add `CHECK (BookedPlayers <= Capacity)`. What happens to existing bad rows? (Fix D3 first, or use `WITH NOCHECK`, and explain why that's risky.)

### D5 — Index design from a query (25 min) ★★
Given the A2 query (course + UTC range + spots filter, ordered by start), design the index:
- Measure before: logical reads (`SET STATISTICS IO, TIME ON`) + the plan
- Create `IX_TeeTimes_CourseId_StartUtc INCLUDE (Capacity, BookedPlayers, Price)`
- Measure after. Explain key order (equality first, then range), INCLUDE, and the key lookup that disappears
- Also index `Bookings(TeeTimeId) INCLUDE (Players, Status)` and the FK columns. Should `IdempotencyKey` get a **filtered unique index** `WHERE IdempotencyKey IS NOT NULL`?

### D6 — Sargability (15 min)
Rewrite each so it can seek:
```sql
WHERE CAST(StartUtc AS DATE) = '2026-11-10'
WHERE YEAR(CreatedUtc) = 2026
WHERE LastName LIKE '%son'
WHERE Email = N'golfer1@example.com'   -- Email is VARCHAR → implicit conversion
WHERE ISNULL(HoldExpiresUtc, '9999-12-31') < SYSUTCDATETIME()
```
(Note: `CAST(... AS DATE)` is sometimes sargable in SQL Server, but know why the range version is still the safer habit.)

### D7 — Parameter sniffing (20 min)
Create a proc that searches bookings by `@CourseId`. Make one course very large (insert more bookings for it) and others tiny. Show the plan reuse problem. Try fixes: `OPTION (RECOMPILE)`, `OPTIMIZE FOR`, splitting procs. **Explain trade-offs.**

### D8 — Isolation levels & blocking (25 min)
Two sessions:
- Session 1: `BEGIN TRAN; UPDATE TeeTimes SET Price = Price + 1 WHERE TeeTimeId = 1;` (don't commit)
- Session 2: `SELECT * FROM TeeTimes WHERE TeeTimeId = 1;` → it blocks.

Then turn on `READ_COMMITTED_SNAPSHOT` and repeat. Explain row versioning and tempdb cost. Try `SNAPSHOT` and get an **update conflict**. Explain `NOLOCK` and why not to use it.

### D9 — Deadlock (20 min)
Make a deadlock with two sessions that update `TeeTimes` and `Bookings` in **opposite order**. Read the deadlock graph (system_health XE). Fix it with consistent access order. Explain retry logic in the app.

### D10 — Pagination (15 min)
`OFFSET/FETCH` page 1 vs page 2000 (compare reads), then keyset: `WHERE (StartUtc > @s OR (StartUtc = @s AND TeeTimeId > @id)) ORDER BY StartUtc, TeeTimeId`.

### D11 — Gaps & islands: free time windows (30 min) ★ stretch
For one course on one day, find **consecutive runs** of tee times with at least 2 free spots ("08:00–08:40, 5 slots"). Hint: `ROW_NUMBER` difference technique.

### D12 — Bulk insert (15 min)
Insert 500 tee times for a new course in one call: table-valued parameter or `STRING_SPLIT`/`OPENJSON`. Compare it with 500 single inserts from .NET (EF `AddRange` batching vs `SqlBulkCopy`).

### D — Theory rapid-fire
Clustered vs nonclustered · heap · fill factor and fragmentation · statistics and when they go stale · covering index · composite key order · filtered index · columnstore (when) · CTE vs temp table vs table variable · `MERGE` caveats · `EXISTS` vs `IN` vs `JOIN` · transaction log basics · `SET XACT_ABORT` · how you find the slowest queries (Query Store, DMVs).

---

## 7. Mock E — PR / code review rounds

### 7.0 How to do a live code review (talk track)
1. **Understand intent first:** "This endpoint creates a booking and charges the card."
2. **Go top-down by severity:** 🔴 correctness/security/data loss → 🟠 concurrency/performance → 🟡 design/maintainability → 🟢 style/naming.
3. **For each issue say:** what's wrong → why it matters (the real impact) → the fix.
4. **Say what is good too:** reviewers who only criticise come across as hard to work with.
5. **Ask about tests:** "Which test would have caught this?"
6. Stop at roughly the top 8–10 issues if time is short, then mention the rest in one line.

### E1 — Review this .NET PR (25 min) ★★
> **Context:** "PR #482 — Add bookings controller. Generated mostly with AI, tests pass locally."
```csharp
[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private static AppDbContext _db;
    private readonly HttpClient _http = new HttpClient();

    public BookingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public List<Booking> GetAll(int courseId)
    {
        var bookings = _db.Bookings.ToList()
            .Where(b => b.TeeTime.CourseId == courseId)
            .ToList();
        return bookings;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(string lastName)
    {
        var golfers = await _db.Golfers
            .FromSqlRaw("SELECT * FROM Golfers WHERE LastName LIKE '%" + lastName + "%'")
            .ToListAsync();
        return Ok(golfers);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Booking booking)
    {
        var slot = _db.TeeTimes.Find(booking.TeeTimeId);
        if (slot.BookedPlayers + booking.Players > slot.Capacity)
            return BadRequest("Full");

        slot.BookedPlayers += booking.Players;
        booking.CreatedAt = DateTime.Now;
        booking.Status = "Confirmed";
        _db.Bookings.Add(booking);
        _db.SaveChanges();

        var resp = _http.PostAsync("https://payments.example.com/charge",
            new StringContent(JsonSerializer.Serialize(new { booking.Id, slot.Price }))).Result;

        return Ok(booking);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync($"DELETE FROM Bookings WHERE BookingId = {id}");
        }
        catch (Exception) { }
        return Ok();
    }
}
```
**Your task:** List every issue with severity and fix. Then rewrite `Create` properly (timebox 15 min). **Target: 15+ issues.** Answer key: §12.1.

### E2 — Review this React PR (20 min) ★★
> **Context:** "PR #517 — Tee sheet page with quick booking."
```tsx
import { useEffect, useState } from "react";

export default function TeeSheet({ courseId, date }: any) {
  const [slots, setSlots] = useState([]);
  const [loading, setLoading] = useState(false);
  const [filter, setFilter] = useState("");
  const [visible, setVisible] = useState([]);

  useEffect(() => {
    setLoading(true);
    fetch(`/api/courses/${courseId}/teetimes?date=${date}`)
      .then((r) => r.json())
      .then((data) => {
        setSlots(data);
        setLoading(false);
      });
  }, []);

  useEffect(() => {
    setVisible(slots.filter((s: any) => s.time.includes(filter)));
  }, [slots, filter]);

  async function book(slot: any) {
    slot.booked = slot.booked + 1;
    setSlots(slots);
    await fetch("/api/bookings", {
      method: "POST",
      body: JSON.stringify({ teeTimeId: slot.id }),
    });
  }

  return (
    <div>
      <input placeholder="Search time" onChange={(e) => setFilter(e.target.value)} />
      {loading && <p>Loading...</p>}
      {visible.map((s: any, i: number) => (
        <div key={i} className="slot" onClick={() => book(s)}>
          {new Date(s.time).toLocaleTimeString()} — {s.capacity - s.booked} spots
          <span dangerouslySetInnerHTML={{ __html: s.notes }} />
        </div>
      ))}
    </div>
  );
}
```
**Your task:** List issues with severity and fix. Then refactor it into `useTeeTimes` hook + `TeeSheet` + `TeeTimeRow` with proper types (timebox 20 min). **Target: 14+ issues.** Answer key: §12.2.

### E3 — Review this SQL PR (10 min)
```sql
CREATE PROCEDURE GetCourseBookings @CourseName NVARCHAR(100) AS
BEGIN
  DECLARE @sql NVARCHAR(MAX) = 'SELECT * FROM Bookings b, TeeTimes t, Courses c
     WHERE b.TeeTimeId = t.TeeTimeId AND t.CourseId = c.CourseId
     AND c.Name = ''' + @CourseName + ''' AND YEAR(t.StartUtc) = YEAR(GETDATE())
     ORDER BY t.StartUtc'
  EXEC(@sql)
END
```
Answer key: §12.3.

### Senior PR checklist (keep next to you)
| Area | What you check |
|---|---|
| Correctness | Business rules (R1–R5), edge cases, null handling, off-by-one, time zones/DST |
| Security | Injection, authZ on every endpoint, over-posting, secrets in code/logs, XSS, PII in logs |
| Concurrency | Races on read-check-write, idempotency, transactions, concurrency tokens |
| Performance | N+1, unbounded queries, missing paging, sync over async, unnecessary allocations, missing indexes |
| API contract | Status codes, DTOs, ProblemDetails, versioning, breaking changes |
| Resilience | Timeouts, retries (only idempotent calls), cancellation, external call outside the transaction |
| Tests | Do they test behaviour? The edge case? Would they catch the bug? Do they run in CI? |
| Readability | Naming, small functions, no dead code, comments explain *why* |
| AI-generated code | Hallucinated APIs, plausible-but-wrong logic, unnecessary abstractions, "tests pass" claims you can't see evidence for, licence/copied code |

---

## 8. Mock F — AI-assisted round (Claude Code, MCP)

> Forte's line is *"AI proposes, engineers decide."* In this round they judge your **process and verification**, not how fast the AI is.

### F1 — Write your CLAUDE.md (30 min)
Write a CLAUDE.md for the practice repo. **Requirements:**
- Under ~60 lines
- Exact build/test/lint commands (`dotnet build`, `dotnet test --filter ...`, `npm test`, `npm run lint`)
- Repo map: one line per project
- Conventions that differ from defaults (e.g., "all endpoints take a `CancellationToken`", "store UTC, convert with the course `TimeZoneId`", "DTOs only at the API boundary", "use `IDbContextFactory` in background services")
- **Never-do list** (e.g., don't edit `Migrations/` by hand, don't touch `appsettings.Production.json`, don't add NuGet packages without asking)
- Definition of done: build + tests green + a new test for every new behaviour

**Be ready to explain:** why short beats long; what you **leave out** (things Claude can read from the code).

### F2 — Guardrails with hooks & permissions (30 min)
Using the Claude Code docs, configure in `.claude/settings.json`:
- A hook that runs `dotnet format` / `prettier` after file edits
- A hook that blocks edits to `**/Migrations/**` and secrets files
- A check that runs the tests before Claude says it's done
- Permission rules: allow `dotnet test`, ask before `git push`, deny `rm -rf` and reading `.env`

**Talking point:** "CLAUDE.md is advisory; hooks are deterministic. I put anything that *must* happen in hooks."

### F3 — Feature with Claude Code under a timer (45 min) ★★ dress rehearsal
Implement **A6 (idempotency)** or **C6 (optimistic update)** using Claude Code, **narrating as if an interviewer is watching**:
1. Plan mode first: ask for a plan, then **edit the plan** yourself (catch at least one thing it missed).
2. Tests first: have it write failing tests from **your** acceptance criteria; review them.
3. Implement; make it run the build + tests and **show the output**.
4. Review the diff line by line; explain or reject anything you don't fully understand.
5. Commit with a message that says what you verified.

**Score yourself:** Did you catch at least one wrong or over-engineered suggestion? Could you explain every line?

### F4 — Review an AI-generated PR (20 min)
Ask Claude Code to "add a GET endpoint that returns all bookings for a golfer, with their payment status" **with no extra guidance**. Then review its output with the §7 checklist as if a junior sent it. Typical findings: no paging, entities exposed, missing auth (any golfer can read another's bookings), N+1, no test for the not-found case.

### F5 — MCP setup and talking points (30 min)
- Connect one MCP server for local dev (e.g., a database MCP pointed at the **local** TeeTimeDb, read-only login; or Playwright MCP to drive the React app).
- Use it once: "Which tee times are overbooked?" (compare with your D3 answer) or "Open the tee sheet and verify the Full state renders."
- **Be ready to explain:** what MCP is (a protocol that lets the agent call tools and read resources exposed by servers); tools vs resources vs prompts; **risks**: prompt injection through data, over-privileged credentials, untrusted third-party servers; **mitigations**: least privilege, read-only, dev data only, allowlists, review tool calls.

### F — Question bank (practise 1–2 min answers)
1. Walk me through your AI workflow from ticket to merged PR.
2. How do you verify AI-generated code? What counts as evidence?
3. Tell me about a time AI was confidently wrong. How did you catch it?
4. What would you never let an agent do in a production codebase?
5. How do you keep context focused in long sessions (`/clear`, subagents, small tasks)?
6. How would you roll out Claude Code to a team without dropping quality?
7. How do you measure whether AI helps (cycle time, rework rate, escaped defects, review time)?
8. Using the Anthropic API in CI to generate tests/docs: how do you keep it reviewable and safe?
9. Security: secrets, PII in prompts, licence of generated code, prompt injection from repo/issue content.
10. How do you mentor someone who over-trusts AI? Someone who refuses to use it?

---

## 9. Mock G — System design (client round)

### G1 — Design a tee-time booking platform (45 min) ★★
**Prompt:** "Design the booking backend for 3,000+ golf courses in the US and UK. Golfers book on our consumer app and marketplace; courses use a tee sheet + POS at the clubhouse. Saturday 7 a.m. releases cause big spikes."

**Your 45-min structure:**
| Min | Step |
|---|---|
| 0–5 | Clarify: scale (courses, bookings/day, peak RPS), consistency needs, channels, US+UK, payments |
| 5–10 | Core entities + API (reuse §3.1) |
| 10–25 | High-level architecture diagram: API → service → SQL; cache; queue; payment provider; marketplace/POS integrations |
| 25–35 | **Deep dive: no double booking** (atomic update / optimistic concurrency, holds with TTL, idempotency keys, DB constraint as the last guard) |
| 35–40 | Integrations: outbox pattern → message bus → POS/marketplace sync; idempotent consumers; retries/DLQ |
| 40–45 | Scale, observability, failure modes, trade-offs |

**Checklist of things to mention:**
- [ ] UTC + course IANA time zone; DST; US/UK currency and VAT; GDPR for UK golfers
- [ ] Read-heavy tee sheet → caching with short TTL + invalidation on booking events
- [ ] Release-time spike → queue or rate limiting, pre-warming, autoscale, a waiting room
- [ ] Partitioning by course/region if one DB becomes a bottleneck
- [ ] Payment: authorize at hold, capture at confirm; reconciliation job
- [ ] Dynamic pricing: computed where, cached how, and the price locked at hold time
- [ ] Observability: OpenTelemetry traces across booking → payment → POS; SLOs; alerts
- [ ] Legacy: strangler fig for old modules; feature flags; API-first

### G2 — Mini designs (15 min each)
1. Waitlist: notify golfers when a slot frees up (events + fan-out, fairness).
2. Sync with an external POS that is offline sometimes (outbox, retries, conflict resolution).
3. Migrate a Vue/Angular screen to React incrementally (micro-frontend vs route-by-route, shared auth, a typed API client).

---

## 10. Full mock interview scripts
> Run these like the real thing: camera on, timer, no pausing. Better still, paste the **interviewer prompt** below into a fresh Claude chat and have it interview you.

### Mock #1 — Forte technical interview (90 min) · Day 6
| Min | Block | Content |
|---|---|---|
| 0–5 | Intro | 90-second pitch; one project deep-dive |
| 5–25 | Rapid-fire theory | 6 from A-theory, 4 from C-theory, 3 from D-theory, 2 from F |
| 25–45 | Live: data transformation | C1 (no AI) |
| 45–70 | Live: code review | E2 (React PR) **or** E1 (.NET PR) — whichever you haven't done |
| 70–85 | Live: small API | A1 or A5, from an empty file |
| 85–90 | Your questions | Client, product, frontend stack, team, AI tooling policy |

### Mock #2 — Client technical round (60 min) · Day 12
| Min | Block |
|---|---|
| 0–10 | Experience + "biggest production incident you led" |
| 10–45 | G1 system design |
| 45–55 | Behavioural: client asks for a shortcut that hurts quality; disagreement with an architect |
| 55–60 | Your questions |

### Mock #3 — AI-assisted live task (60 min) · Day 14
| Min | Block |
|---|---|
| 0–5 | Explain your AI workflow + guardrails |
| 5–45 | F3-style task: **A3 (POST booking with concurrency)** built end-to-end with Claude Code, including the parallel test |
| 45–55 | Interviewer asks you to explain 3 random lines and one design decision |
| 55–60 | "What would you never let the agent do here?" |

### Interviewer prompt (paste into a new Claude chat)
```
Act as a senior engineer at a consulting company interviewing me for a Senior Full Stack
.NET 8 / React / TypeScript / SQL Server engineer role at a golf tee-time booking platform,
where AI-assisted engineering with Claude Code is expected.
Run a [90]-minute mock following this agenda: [paste the table].
Rules: ask ONE question at a time and wait for my answer. Push back with follow-up questions
like a real interviewer. Interrupt if my answer runs over 90 seconds. For coding tasks, give
the task and wait for me to paste code. Do not give hints unless I ask. At the end, score me
1–4 on: clarification, correctness, code quality, testing, communication, seniority, and list
my 3 weakest answers with what a strong answer would include.
```

---

## 11. Self-scoring rubric
Score every exercise and mock 1–4. Redo anything with a **2 or lower** in any row.

| Dimension | 1 — Weak | 2 — Below bar | 3 — Hire | 4 — Strong hire |
|---|---|---|---|---|
| Clarification | Starts coding immediately | Asks 1 shallow question | Clarifies key edge cases | Clarifies + states assumptions + plan |
| Correctness | Doesn't run | Happy path only | Edge cases handled | Edge cases + concurrency + time zones |
| Code quality | Messy, entity leaks | Works but tangled | Clean, DTOs, async | Clean + idiomatic + easy to extend |
| Testing | None | Mentions tests | One meaningful test | Test-first or the race-condition test |
| Communication | Silent | Narrates sometimes | Explains as they go | Explains trade-offs and alternatives |
| Time | Didn't finish core | Finished core late | Finished in time | Finished + stretch |
| AI usage (when allowed) | Pastes without reading | Accepts output mostly | Reviews + runs tests | Plans, constrains, verifies, rejects bad output |

**Tracking sheet** (copy per exercise):
```
Exercise: ___  Date: ___  Pass: 1 (no AI) / 2 (AI)  Time used: ___/___
Scores: Clar _ Corr _ Qual _ Test _ Comm _ Time _ AI _
Follow-ups I couldn't answer:
-
Redo on: ___
```

---

## 12. Answer keys (spoilers)
> ⚠️ Only read these **after** you have attempted the exercise and scored yourself.

### 12.1 E1 — .NET PR issues
🔴 **Critical**
1. `static AppDbContext _db`: a scoped DbContext shared across requests and threads → state corruption, data leaks across users, not thread-safe.
2. `new HttpClient()` per controller instance → socket exhaustion, ignores DNS changes; use a typed client via `IHttpClientFactory`.
3. **SQL injection** in `Search` (string concatenation with `FromSqlRaw`); use `FromSql($"... {pattern}")` or LINQ `EF.Functions.Like`.
4. **Race condition** in `Create`: read-check-write without concurrency control → overbooking (R1).
5. **Over-posting:** binding the `Booking` entity lets a client set `Status`, `Id`, `CreatedAt`, any golfer id, etc.; use a request DTO.
6. **No authorization** on any endpoint; anyone can delete or read any booking.
7. Payment called **after** `SaveChanges` with no transaction or outbox → a booking can exist with no payment, or a charge can go through without a booking; the result `resp` is ignored entirely.
8. `.Result` on an async call → sync over async, thread-pool starvation, possible deadlock.
9. `Delete` swallows all exceptions and always returns `Ok` → silent data issues; also a hard delete, doesn't release `BookedPlayers`, ignores R2, no 404.

🟠 **Major**
10. `GetAll`: `ToList()` loads the **whole Bookings table** into memory, then filters in memory; `b.TeeTime` is null without `Include` (NullReferenceException) or causes N+1 with lazy loading.
11. No paging on `GetAll`/`Search`; returns entities (circular references, leaks fields such as golfer email).
12. `Find` / `SaveChanges` are sync inside an async method; no `CancellationToken`.
13. `slot` null not checked → 500 instead of 404.
14. `DateTime.Now` → server local time; use UTC (`TimeProvider`/`DateTime.UtcNow`).
15. Booking set straight to `Confirmed`, skipping the hold flow (R3); R4 booking window not checked; players range not validated.
16. `Ok(booking)` should be `201 Created` with `Location`; `BadRequest("Full")` should be `409` with ProblemDetails.
17. `ExecuteSqlRawAsync` with interpolation: safe-ish here only because `id` is an int, but it's a dangerous pattern; use `ExecuteSqlAsync($"...")` (parameterized) or LINQ `ExecuteDeleteAsync`.

🟡 **Minor**
18. `{id}` without `:int` constraint; mixed sync/async style; magic strings for status (use an enum); the hard-coded payment URL should come from options; no logging; "tests pass locally" but no tests in the PR.

**Good `Create` outline:** DTO + validation → load tee time (404) → rule checks (R4, players) → atomic conditional update or RowVersion + retry → insert `Held` booking + outbox message in **one transaction** → `201 CreatedAtAction` → payment handled asynchronously or at confirm, with an idempotency key.

### 12.2 E2 — React PR issues
🔴 **Critical**
1. `dangerouslySetInnerHTML` with server `notes` → **XSS**. Render as text, or sanitize (DOMPurify) if HTML is truly required.
2. **Mutating state** (`slot.booked = ...`) and `setSlots(slots)` with the same reference → React may not re-render; bugs.
3. POST without `Content-Type: application/json` → the server likely rejects or can't bind it; no `res.ok` check; errors ignored; the UI already shows the slot as booked (an optimistic update with no rollback).
4. Missing `players`/golfer in the POST body; no auth header/credentials.

🟠 **Major**
5. `useEffect` deps `[]` but it uses `courseId`, `date` → a stale tee sheet when props change.
6. No `AbortController` → race when props change quickly; setState after unmount.
7. No error handling: `loading` stays `true` forever on failure; `r.ok` not checked.
8. `visible` is **derived state** stored in state + an effect → extra render and possible inconsistency; compute it inline or with `useMemo`.
9. `key={i}` (index) on a dynamic, filterable list → wrong reconciliation; use `s.id`.
10. Clickable `<div>` → not keyboard accessible, no role; use `<button>` with `disabled` when full.
11. No double-click protection; can book a **full** slot (spots can go negative); no confirmation.
12. `toLocaleTimeString()` uses the **browser** time zone, not the course's; and `s.time.includes(filter)` searches the raw ISO string, not what the user sees.

🟡 **Minor**
13. `any` everywhere → no type safety; define `TeeTime` type and the props interface.
14. `<input>` without a label / `aria-label`; uncontrolled vs controlled mix; no debounce.
15. No empty state ("No tee times"); `Loading...` without a skeleton; the component does fetching + rendering + mutation (split into hook + components); no tests.

### 12.3 E3 — SQL PR issues
1. **Dynamic SQL injection** via `@CourseName` concatenation → use a static query, or `sp_executesql` with parameters.
2. Old-style comma joins → use explicit `JOIN`s (readability, accidental cross joins).
3. `SELECT *` → returns all columns from 3 tables (duplicate names, wide rows, breaks covering indexes).
4. `YEAR(t.StartUtc) = YEAR(GETDATE())` → non-sargable; use a range; also `GETDATE()` is local time vs UTC data.
5. No paging/TOP → an unbounded result.
6. Filtering by course **name**, not id (not unique, no index).
7. No `SET NOCOUNT ON`; no schema prefix (`dbo.`); the proc name has no prefix convention; cancelled bookings aren't filtered.

### 12.4 Hints for the tricky exercises
- **A3 / D4 atomic update:** `UPDATE TeeTimes SET BookedPlayers = BookedPlayers + @p WHERE TeeTimeId = @id AND BookedPlayers + @p <= Capacity;` then check `@@ROWCOUNT` (in EF: `ExecuteUpdateAsync` returns the rows affected). Rows affected = 0 → full (or missing: check which).
- **A3 parallel test:** `await Task.WhenAll(Enumerable.Range(0,10).Select(_ => client.PostAsJsonAsync(...)))`, then count status codes. Use a fresh tee time per test run.
- **A8:** `using var scope = _scopeFactory.CreateScope();` inside the loop; `PeriodicTimer` + `while (await timer.WaitForNextTickAsync(stoppingToken))`.
- **C2 race:** with an `ignore` flag or `AbortController` in the effect cleanup; check `signal.aborted` / catch `AbortError` and don't set state.
- **D2c:** `LAG(StartUtc) OVER (PARTITION BY GolferId ORDER BY StartUtc)`.
- **D3d:** `LAG` per golfer + day, then `DATEDIFF(MINUTE, prev, cur) < 30`.
- **D11 gaps & islands:** filter qualifying slots → `ROW_NUMBER() OVER (ORDER BY StartUtc)` → group key = `DATEADD(MINUTE, -10 * rn, StartUtc)` → `GROUP BY` the key → `MIN`/`MAX`/`COUNT`.
- **D5 key order:** equality columns first (`CourseId`), then the range/sort column (`StartUtc`), then `INCLUDE` the selected columns so there are no key lookups.

---
*Good luck. Every exercise you run aloud now is one fewer surprise in the real interview.*
