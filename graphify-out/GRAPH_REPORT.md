# Graph Report - api.net.Interviews.Practice  (2026-10-22)

## Corpus Check
- 47 files · ~16,599 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 6 file(s) not represented in the graph (top: (none) 2, .props 2, .example 1)

## Summary
- 415 nodes · 684 edges · 29 communities (25 shown, 4 thin omitted)
- Extraction: 88% EXTRACTED · 12% INFERRED · 0% AMBIGUOUS · INFERRED: 83 edges (avg confidence: 0.88)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `633eba9c`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Mock D - SQL Server
- Integration & Unit Test Infra
- TeeSheet.Domain
- Mock A - .NET Web API live coding
- Projects, Compose & SQL Server
- Forte Group Live Coding & Mock Interview Practice Plan
- API Endpoints & DbContext Wiring
- WebApplicationFactory Setup
- SQL Command Logging
- Booking Entity
- TeeSheetDbContext
- Launch Settings
- Booking & Payment Statuses
- TeeTime Entity
- Golfer Entity
- Course Entity
- SQL Schema Tables
- A2 cheatsheet - GET tee times list (filter, sort, paging)
- ResponseGenericDto
- A1 - GET by id (warm-up)
- Mock B - EF Core
- Payment
- Full Mock #1 - Forte technical interview (90 min)
- Mock C - React + TypeScript live coding
- A3 - POST create booking (concurrency-safe)

## God Nodes (most connected - your core abstractions)
1. `Forte Group Live Coding & Mock Interview Practice Plan` - 31 edges
2. `Booking` - 23 edges
3. `TeeTime` - 21 edges
4. `TeeSheetDbContext` - 20 edges
5. `Course` - 17 edges
6. `Payment` - 16 edges
7. `Mock D - SQL Server` - 16 edges
8. `Mock A - .NET Web API live coding` - 16 edges
9. `TeeSheet.Domain` - 14 edges
10. `Golfer` - 14 edges

## Surprising Connections (you probably didn't know these)
- `Projection vs loading strategies ("the three types")` --references--> `Course`  [INFERRED]
  docs/awnsers-deep-cheatsheet.md → src/TeeSheet.Domain/Course.cs
- `Where does `endUtc` come from?` --references--> `TeeTime`  [INFERRED]
  docs/awnsers-deep-cheatsheet.md → src/TeeSheet.Domain/TeeTime.cs
- `Booking entity (Players, Status Held/Confirmed/Cancelled, HoldExpiresUtc, IdempotencyKey)` --references--> `Booking`  [INFERRED]
  docs/forte-live-coding-mock-plan.md → src/TeeSheet.Domain/Booking.cs
- ``.Where()` / `.FirstOrDefaultAsync()` are not projection` --references--> `Course`  [INFERRED]
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
Cohesion: 0.12
Nodes (20): A2 - GET list with filter, sort, paging, A7 - Optimistic concurrency with ETag / If-Match, Answer keys (E1, E2, E3 issues and hints), C7 - Pagination / infinite scroll, D10 - OFFSET/FETCH vs keyset pagination, D11 - Gaps & islands: free time windows, D12 - Bulk insert (TVP, OPENJSON, SqlBulkCopy), D1 - Joins & aggregation (utilization) (+12 more)

### Community 1 - "Integration & Unit Test Infra"
Cohesion: 0.08
Nodes (6): A1_GetTeeTimeByIdTests (red until implemented), A1_GetTeeTimeByIdTests, ApiFactory, SqlLog, TestData, ModelMappingTests

### Community 2 - "TeeSheet.Domain"
Cohesion: 0.21
Nodes (4): TeeSheet.Infrastructure.Persistence.Configurations, TeeSheet.Domain, BookingConfiguration, TeeTimeConfiguration

### Community 3 - "Mock A - .NET Web API live coding"
Cohesion: 0.23
Nodes (10): A10 - Typed HttpClient to payment API (IPaymentGateway), A5 - DELETE cancel booking, A6 - Idempotency-Key on POST, Full Mock #2 - Client technical round (60 min), G1 - Design a tee-time booking platform, G2 - Mini designs (waitlist, POS sync, React migration), HTTP verbs & status code matrix, Mock A - .NET Web API live coding (+2 more)

### Community 4 - "Projects, Compose & SQL Server"
Cohesion: 0.07
Nodes (31): sqlserver service (mssql/server:2022-latest, container teesheet-sql), sqlserver healthcheck (sqlcmd SELECT 1), teesheet-data volume, A12 - Integration tests, teetime/ solution layout (TeeTime.Api, Domain, Infrastructure, tests, web, sql), net10.0, Microsoft.AspNetCore.Mvc.Testing, Microsoft.AspNetCore.OpenApi (+23 more)

### Community 5 - "Forte Group Live Coding & Mock Interview Practice Plan"
Cohesion: 0.25
Nodes (10): A11 - Authorization policies (MemberOnly, CourseAdmin), Booking entity (Players, Status Held/Confirmed/Cancelled, HoldExpiresUtc, IdempotencyKey), Course entity (TimeZoneId IANA, Country, Currency), Golfer entity (IsMember), Interviewer prompt for a Claude mock interview, Payment entity (Status Pending/Captured/Failed/Refunded), Forte Group Live Coding & Mock Interview Practice Plan, Self-scoring rubric (1-4 across 7 dimensions) (+2 more)

### Community 6 - "API Endpoints & DbContext Wiring"
Cohesion: 0.15
Nodes (7): TeeSheet.Infrastructure.Persistence, TeeSheet.Api.Endpoints, TeeSheet.UnitTests, CourseEndPoints, DevEndpoints, TeeTimeEndpoints, Program

### Community 7 - "WebApplicationFactory Setup"
Cohesion: 0.10
Nodes (3): TeeSheet.IntegrationTests.Infrastructure, TeeSheet.IntegrationTests, ApiCollection

### Community 8 - "SQL Command Logging"
Cohesion: 0.17
Nodes (3): CommandLogger, SqlCommandLog, Commands

### Community 9 - "Booking Entity"
Cohesion: 0.17
Nodes (12): Booking, BookingId, CreatedUtc, Golfer, GolferId, HoldExpiresUtc, IdempotencyKey, Payments (+4 more)

### Community 10 - "TeeSheetDbContext"
Cohesion: 0.08
Nodes (17): TeeTimeDto, CourseName, Currency, Id, Price, SpotsLeft, StartLocal, StartUtc (+9 more)

### Community 11 - "Launch Settings"
Cohesion: 0.18
Nodes (10): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, profiles (+2 more)

### Community 12 - "Booking & Payment Statuses"
Cohesion: 0.20
Nodes (9): BookingStatus, Cancelled, Confirmed, Held, PaymentStatus, Captured, Failed, Pending (+1 more)

### Community 13 - "TeeTime Entity"
Cohesion: 0.15
Nodes (10): TeeTime, BookedPlayers, Bookings, Capacity, Course, CourseId, Price, RowVersion (+2 more)

### Community 14 - "Golfer Entity"
Cohesion: 0.25
Nodes (8): Golfer, Bookings, Email, FirstName, GolferId, IsMember, LastName, GolferConfiguration

### Community 15 - "Course Entity"
Cohesion: 0.20
Nodes (8): Course, Country, CourseId, Currency, Name, TeeTimes, TimeZoneId, CourseConfiguration

### Community 16 - "SQL Schema Tables"
Cohesion: 0.60
Nodes (5): Bookings, Courses, Golfers, Payments, TeeTimes

### Community 21 - "A2 cheatsheet - GET tee times list (filter, sort, paging)"
Cohesion: 0.08
Nodes (23): A1 cheatsheet - GET tee time by id, A2 cheatsheet - GET tee times list (filter, sort, paging), Comparison, Gotchas (REVIEW LATER in the code), How offset paging works (the code from line 62), Isolated pagination samples (no EF, no API - just the algorithm), Minimal API vs controller, Option 1 - Offset with plain `if`s (no `Math.Clamp`) (+15 more)

### Community 22 - "ResponseGenericDto"
Cohesion: 0.12
Nodes (9): TeeSheet.Dto, ResponseGenericDto, Items, Page, PageSize, TotalCount, PagedResult, ProblemDetail (+1 more)

### Community 23 - "A1 - GET by id (warm-up)"
Cohesion: 0.18
Nodes (10): A1 - GET by id (warm-up), A9 - Correlation id middleware + IExceptionHandler, B1 - Kill the N+1, C4 - Booking form, C8 - Time zones in the UI, N+1 query problem, ProblemDetails (RFC 9457), A1 GET /api/teetimes/{id:int} -> TeeTimeDto (+2 more)

### Community 24 - "Mock B - EF Core"
Cohesion: 0.19
Nodes (10): A8 - Hold expiry with BackgroundService, B2 - Tracking vs no-tracking, B3 - Cartesian explosion (AsSplitQuery), B4 - Bulk operations (ExecuteUpdateAsync), B5 - Zero-downtime migrations (expand/migrate/contract), B6 - Global query filter + soft delete, F1 - Write your CLAUDE.md, F2 - Guardrails with hooks & permissions (+2 more)

### Community 25 - "Payment"
Cohesion: 0.20
Nodes (9): Payment, Amount, Booking, BookingId, CreatedUtc, Currency, PaymentId, Status (+1 more)

### Community 26 - "Full Mock #1 - Forte technical interview (90 min)"
Cohesion: 0.24
Nodes (9): B7 - Raw SQL safely (FromSql vs FromSqlRaw), C1 - summarize(bookings) data transformation kata, E1 - Review .NET PR #482 (BookingsController), E2 - Review React PR #517 (TeeSheet), E3 - Review SQL PR (GetCourseBookings), F4 - Review an AI-generated PR, Full Mock #1 - Forte technical interview (90 min), Mock E - PR / code review rounds (+1 more)

### Community 27 - "Mock C - React + TypeScript live coding"
Cohesion: 0.32
Nodes (8): C10 - Tests with React Testing Library + MSW, C2 - Typed useFetch hook with discriminated union, C3 - Tee sheet list with filters, C5 - Redux Toolkit / RTK Query click-to-fetch, C6 - Optimistic update with rollback, C9 - Generic DataTable<T> component, Mock C - React + TypeScript live coding, TypeScript katas 1-12

### Community 28 - "A3 - POST create booking (concurrency-safe)"
Cohesion: 0.33
Nodes (4): A3 - POST create booking (concurrency-safe), A4 - PATCH change players, F3 - Feature with Claude Code under a timer, Full Mock #3 - AI-assisted live task (60 min)

## Ambiguous Edges - Review These
- `A1 GET /api/teetimes/{id:int} -> TeeTimeDto` → `R1 capacity: non-cancelled players never exceed Capacity`  [AMBIGUOUS]
  README.md · relation: conceptually_related_to

## Knowledge Gaps
- **97 isolated node(s):** `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.NET.Sdk.Web`, `BookingId`, `TeeTimeId` (+92 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 170 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **4 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Forte Group Live Coding & Mock Interview Practice Plan` connect `Forte Group Live Coding & Mock Interview Practice Plan` to `Mock D - SQL Server`, `Mock A - .NET Web API live coding`, `Projects, Compose & SQL Server`, `Mock B - EF Core`, `Full Mock #1 - Forte technical interview (90 min)`, `Mock C - React + TypeScript live coding`, `A3 - POST create booking (concurrency-safe)`?**
  _High betweenness centrality (0.273) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `Booking` (e.g. with `Booking entity (Players, Status Held/Confirmed/Cancelled, HoldExpiresUtc, IdempotencyKey)` and `Bookings`) actually correct?**
  _`Booking` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.NET.Sdk.Web` to the rest of the system?**
  _97 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Mock D - SQL Server` be split into smaller, more focused modules?**
  _Cohesion score 0.11594202898550725 - nodes in this community are weakly interconnected._
- **Why does `TeeSheetDbContext` connect `TeeSheetDbContext` to `Integration & Unit Test Infra`, `API Endpoints & DbContext Wiring`, `Booking Entity`, `TeeTime Entity`, `Golfer Entity`, `Course Entity`, `Payment`?**
  _High betweenness centrality (0.166) - this node is a cross-community bridge._
- **Are the 4 inferred relationships involving `TeeTime` (e.g. with `Where does `endUtc` come from?` and `TeeTime entity (Capacity, BookedPlayers, Price, RowVersion)`) actually correct?**
  _`TeeTime` has 4 INFERRED edges - model-reasoned connections that need verification._

### Low-confidence Hints
_AMBIGUOUS edges — the extractor was unsure. Verify before acting on these._

- **What is the exact relationship between `A1 GET /api/teetimes/{id:int} -> TeeTimeDto` and `R1 capacity: non-cancelled players never exceed Capacity`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._