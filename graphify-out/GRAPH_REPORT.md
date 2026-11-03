# Graph Report - api.net.Interviews.Practice  (2026-11-02)

## Corpus Check
- 53 files · ~20,750 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 6 file(s) not represented in the graph (top: (none) 2, .props 2, .example 1)

## Summary
- 476 nodes · 893 edges · 29 communities (25 shown, 4 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 90 edges (avg confidence: 0.88)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `21cf801c`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Mock D - SQL Server
- Task
- EntityTypeBuilder
- Mock A - .NET Web API live coding
- Projects, Compose & SQL Server
- Forte Group Live Coding & Mock Interview Practice Plan
- API Endpoints & DbContext Wiring
- Course
- SQL Command Logging
- Booking Entity
- TeeSheetDbContext
- Launch Settings
- Payment
- TeeTime Entity
- Golfer Entity
- Cheat sheet: booking safely under concurrency
- SQL Schema Tables
- A2 cheatsheet - GET tee times list (filter, sort, paging)
- A1 - GET by id (warm-up)
- Mock B - EF Core
- Full Mock #1 - Forte technical interview (90 min)
- Mock C - React + TypeScript live coding
- Mock A - .NET Web API live coding
- Mock F - AI-assisted round (Claude Code, MCP)
- DateTime

## God Nodes (most connected - your core abstractions)
1. `Forte Group Live Coding & Mock Interview Practice Plan` - 31 edges
2. `Booking` - 25 edges
3. `A3_CreateBookingTests` - 22 edges
4. `TeeTime` - 21 edges
5. `TeeSheetDbContext` - 21 edges
6. `TeeSheet.Domain` - 18 edges
7. `Course` - 17 edges
8. `Payment` - 16 edges
9. `ApiFactory` - 16 edges
10. `Mock D - SQL Server` - 16 edges

## Surprising Connections (you probably didn't know these)
- `Where does `endUtc` come from?` --references--> `TeeTime`  [INFERRED]
  docs/awnsers-deep-cheatsheet.md → src/TeeSheet.Domain/TeeTime.cs
- `Booking entity (Players, Status Held/Confirmed/Cancelled, HoldExpiresUtc, IdempotencyKey)` --references--> `Booking`  [INFERRED]
  docs/forte-live-coding-mock-plan.md → src/TeeSheet.Domain/Booking.cs
- `Track A — API` --references--> `BookingRules`  [INFERRED]
  docs/track-by-blaxzoser.md → src/TeeSheet.Domain/BookingRules.cs
- `Projection vs loading strategies ("the three types")` --references--> `Course`  [INFERRED]
  docs/awnsers-deep-cheatsheet.md → src/TeeSheet.Domain/Course.cs
- `Course entity (TimeZoneId IANA, Country, Currency)` --references--> `Course`  [INFERRED]
  docs/forte-live-coding-mock-plan.md → src/TeeSheet.Domain/Course.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Booking business rules R1-R5 enforced across exercises** — docs_forte_live_coding_mock_plan_r1_rule, docs_forte_live_coding_mock_plan_r2_rule, docs_forte_live_coding_mock_plan_r3_rule, docs_forte_live_coding_mock_plan_r4_rule, docs_forte_live_coding_mock_plan_r5_rule, docs_forte_live_coding_mock_plan_a3_post_create_booking, docs_forte_live_coding_mock_plan_a5_delete_cancel_booking, docs_forte_live_coding_mock_plan_a6_idempotency_key_on_post, docs_forte_live_coding_mock_plan_a8_hold_expiry_backgroundservice, docs_forte_live_coding_mock_plan_a11_authorization_policies [INFERRED 0.85]
- **Concurrency-safe booking (no double booking, R1)** — docs_forte_live_coding_mock_plan_a3_post_create_booking, docs_forte_live_coding_mock_plan_d4_atomic_booking_statement, docs_forte_live_coding_mock_plan_a4_patch_change_players, docs_forte_live_coding_mock_plan_atomic_conditional_update, docs_forte_live_coding_mock_plan_rowversion_optimistic_concurrency, docs_forte_live_coding_mock_plan_updlock_transaction, docs_forte_live_coding_mock_plan_r1_rule, docs_forte_live_coding_mock_plan_g1_tee_time_booking_platform_design [INFERRED 0.85]
- **TeeSheet solution projects and local SQL Server infra** — readme_teesheet_api, readme_teesheet_domain, readme_teesheet_infrastructure, readme_teesheet_unittests, readme_teesheet_integrationtests, readme_sql_scripts, compose_sqlserver [INFERRED 0.85]

## Communities (29 total, 4 thin omitted)

### Community 0 - "Mock D - SQL Server"
Cohesion: 0.19
Nodes (13): Answer keys (E1, E2, E3 issues and hints), D11 - Gaps & islands: free time windows, D12 - Bulk insert (TVP, OPENJSON, SqlBulkCopy), D1 - Joins & aggregation (utilization), D2 - Window functions (DENSE_RANK, running total, LAG), D3 - Data integrity hunt (overbookings, wrong counters), D4 - usp_BookTeeTime atomic booking proc, D5 - Index design (IX_TeeTimes_CourseId_StartUtc) (+5 more)

### Community 1 - "Task"
Cohesion: 0.15
Nodes (5): A1_GetTeeTimeByIdTests, A3_CreateBookingTests, ApiFactory, SqlLog, TestData

### Community 2 - "EntityTypeBuilder"
Cohesion: 0.32
Nodes (3): BookingConfiguration, CourseConfiguration, TeeTimeConfiguration

### Community 3 - "Mock A - .NET Web API live coding"
Cohesion: 0.27
Nodes (7): A10 - Typed HttpClient to payment API (IPaymentGateway), A5 - DELETE cancel booking, Full Mock #2 - Client technical round (60 min), G1 - Design a tee-time booking platform, G2 - Mini designs (waitlist, POS sync, React migration), HTTP verbs & status code matrix, Mock G - System design (client round)

### Community 4 - "Projects, Compose & SQL Server"
Cohesion: 0.08
Nodes (29): sqlserver service (mssql/server:2022-latest, container teesheet-sql), sqlserver healthcheck (sqlcmd SELECT 1), teesheet-data volume, teetime/ solution layout (TeeTime.Api, Domain, Infrastructure, tests, web, sql), net10.0, Microsoft.AspNetCore.Mvc.Testing, Microsoft.AspNetCore.OpenApi, Microsoft.EntityFrameworkCore.Design (+21 more)

### Community 5 - "Forte Group Live Coding & Mock Interview Practice Plan"
Cohesion: 0.30
Nodes (9): Booking entity (Players, Status Held/Confirmed/Cancelled, HoldExpiresUtc, IdempotencyKey), Course entity (TimeZoneId IANA, Country, Currency), Golfer entity (IsMember), Interviewer prompt for a Claude mock interview, Payment entity (Status Pending/Captured/Failed/Refunded), Forte Group Live Coding & Mock Interview Practice Plan, Self-scoring rubric (1-4 across 7 dimensions), TeeTimeDb SQL schema + random seed (+1 more)

### Community 6 - "API Endpoints & DbContext Wiring"
Cohesion: 0.06
Nodes (21): TeeSheet.Infrastructure.Persistence, TeeSheet.Api.Endpoints, TeeSheet.Dto, TeeSheet.IntegrationTests.Infrastructure, TeeSheet.Infrastructure.Persistence.Configurations, TeeSheet.UnitTests, TeeSheet.Domain, TeeSheet.IntegrationTests (+13 more)

### Community 7 - "Course"
Cohesion: 0.15
Nodes (13): A1 cheatsheet - GET tee time by id, Minimal API vs controller, Projection vs loading strategies ("the three types"), Quick reference: AC recap for A1, `.Where()` / `.FirstOrDefaultAsync()` are not projection, Why convert to UTC / what DST has to do with it, Course, Country (+5 more)

### Community 8 - "SQL Command Logging"
Cohesion: 0.07
Nodes (4): PagedResult, CommandLogger, SqlCommandLog, Commands

### Community 9 - "Booking Entity"
Cohesion: 0.15
Nodes (12): Booking, BookingId, CreatedUtc, Golfer, GolferId, HoldExpiresUtc, IdempotencyKey, Payments (+4 more)

### Community 10 - "TeeSheetDbContext"
Cohesion: 0.08
Nodes (18): BookingRequestDto, GolferId, Players, TeeTimeId, TeeTimeDto, CourseName, Currency, Id (+10 more)

### Community 11 - "Launch Settings"
Cohesion: 0.18
Nodes (10): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, profiles (+2 more)

### Community 12 - "Payment"
Cohesion: 0.10
Nodes (18): Payment, Amount, Booking, BookingId, CreatedUtc, Currency, PaymentId, Status (+10 more)

### Community 13 - "TeeTime Entity"
Cohesion: 0.18
Nodes (10): TeeTime, BookedPlayers, Bookings, Capacity, Course, CourseId, Price, RowVersion (+2 more)

### Community 14 - "Golfer Entity"
Cohesion: 0.20
Nodes (8): Golfer, Bookings, Email, FirstName, GolferId, IsMember, LastName, GolferConfiguration

### Community 15 - "Cheat sheet: booking safely under concurrency"
Cohesion: 0.22
Nodes (8): (a) Atomic conditional `UPDATE`, (b) EF Core optimistic concurrency on `RowVersion` + retry, (c) Transaction with `UPDLOCK, ROWLOCK`, Cheat sheet: booking safely under concurrency, How to answer in an interview, Quick comparison, Testing any of them, The problem: "read, check, then write" is a race

### Community 16 - "SQL Schema Tables"
Cohesion: 0.60
Nodes (5): Bookings, Courses, Golfers, Payments, TeeTimes

### Community 21 - "A2 cheatsheet - GET tee times list (filter, sort, paging)"
Cohesion: 0.11
Nodes (16): A2 cheatsheet - GET tee times list (filter, sort, paging), Comparison, Gotchas (REVIEW LATER in the code), How offset paging works (the code from line 62), Isolated pagination samples (no EF, no API - just the algorithm), Option 1 - Offset with plain `if`s (no `Math.Clamp`), Option 2 - "Take `pageSize + 1`" (no `CountAsync`), Option 3 - Keyset / cursor (no `Skip`, no `CountAsync`) - the A2 stretch (+8 more)

### Community 23 - "A1 - GET by id (warm-up)"
Cohesion: 0.15
Nodes (13): A12 - Integration tests, A1 - GET by id (warm-up), A9 - Correlation id middleware + IExceptionHandler, B1 - Kill the N+1, C4 - Booking form, C8 - Time zones in the UI, N+1 query problem, ProblemDetails (RFC 9457) (+5 more)

### Community 24 - "Mock B - EF Core"
Cohesion: 0.29
Nodes (7): A8 - Hold expiry with BackgroundService, B2 - Tracking vs no-tracking, B3 - Cartesian explosion (AsSplitQuery), B4 - Bulk operations (ExecuteUpdateAsync), B5 - Zero-downtime migrations (expand/migrate/contract), B6 - Global query filter + soft delete, Mock B - EF Core

### Community 26 - "Full Mock #1 - Forte technical interview (90 min)"
Cohesion: 0.27
Nodes (9): B7 - Raw SQL safely (FromSql vs FromSqlRaw), C1 - summarize(bookings) data transformation kata, E1 - Review .NET PR #482 (BookingsController), E2 - Review React PR #517 (TeeSheet), F4 - Review an AI-generated PR, Full Mock #1 - Forte technical interview (90 min), Mock E - PR / code review rounds, 14-day schedule (+1 more)

### Community 27 - "Mock C - React + TypeScript live coding"
Cohesion: 0.19
Nodes (13): A2 - GET list with filter, sort, paging, C10 - Tests with React Testing Library + MSW, C2 - Typed useFetch hook with discriminated union, C3 - Tee sheet list with filters, C5 - Redux Toolkit / RTK Query click-to-fetch, C6 - Optimistic update with rollback, C7 - Pagination / infinite scroll, C9 - Generic DataTable<T> component (+5 more)

### Community 28 - "Mock A - .NET Web API live coding"
Cohesion: 0.31
Nodes (5): A11 - Authorization policies (MemberOnly, CourseAdmin), A3 - POST create booking (concurrency-safe), A4 - PATCH change players, A7 - Optimistic concurrency with ETag / If-Match, Mock A - .NET Web API live coding

### Community 29 - "Mock F - AI-assisted round (Claude Code, MCP)"
Cohesion: 0.22
Nodes (8): A6 - Idempotency-Key on POST, F1 - Write your CLAUDE.md, F2 - Guardrails with hooks & permissions, F3 - Feature with Claude Code under a timer, F5 - MCP setup and talking points, Full Mock #3 - AI-assisted live task (60 min), Model Context Protocol (tools, resources, prompts; risks and mitigations), Mock F - AI-assisted round (Claude Code, MCP)

### Community 30 - "DateTime"
Cohesion: 0.10
Nodes (7): Track A — API, BookingRules, NullableUtcDateTimeConverter, UtcDateTimeConverter, TeeTimeResponse, BookingRulesTests, ModelMappingTests

## Ambiguous Edges - Review These
- `A1 GET /api/teetimes/{id:int} -> TeeTimeDto` → `R1 capacity: non-cancelled players never exceed Capacity`  [AMBIGUOUS]
  README.md · relation: conceptually_related_to

## Knowledge Gaps
- **100 isolated node(s):** `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.NET.Sdk.Web`, `BookingId`, `TeeTimeId` (+95 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 175 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Forte Group Live Coding & Mock Interview Practice Plan` connect `Forte Group Live Coding & Mock Interview Practice Plan` to `Mock D - SQL Server`, `Mock A - .NET Web API live coding`, `Projects, Compose & SQL Server`, `Mock B - EF Core`, `Full Mock #1 - Forte technical interview (90 min)`, `Mock C - React + TypeScript live coding`, `Mock A - .NET Web API live coding`, `Mock F - AI-assisted round (Claude Code, MCP)`?**
  _High betweenness centrality (0.246) - this node is a cross-community bridge._
- **Are the 3 inferred relationships involving `Booking` (e.g. with `Booking entity (Players, Status Held/Confirmed/Cancelled, HoldExpiresUtc, IdempotencyKey)` and `.CreateBookingRequest()`) actually correct?**
  _`Booking` has 3 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.NET.Sdk.Web` to the rest of the system?**
  _100 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Task` be split into smaller, more focused modules?**
  _Cohesion score 0.14904862579281183 - nodes in this community are weakly interconnected._
- **Why does `Booking` connect `Booking Entity` to `Task`, `EntityTypeBuilder`, `Forte Group Live Coding & Mock Interview Practice Plan`, `TeeSheetDbContext`, `Payment`, `TeeTime Entity`, `Golfer Entity`, `SQL Schema Tables`, `DateTime`?**
  _High betweenness centrality (0.176) - this node is a cross-community bridge._
- **Should `Projects, Compose & SQL Server` be split into smaller, more focused modules?**
  _Cohesion score 0.07539118065433854 - nodes in this community are weakly interconnected._

### Low-confidence Hints
_AMBIGUOUS edges — the extractor was unsure. Verify before acting on these._

- **What is the exact relationship between `A1 GET /api/teetimes/{id:int} -> TeeTimeDto` and `R1 capacity: non-cancelled players never exceed Capacity`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._