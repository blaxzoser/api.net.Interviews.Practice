# Graph Report - api.net.Interviews.Practice  (2026-10-21)

## Corpus Check
- Corpus is ~12,426 words - fits in a single context window. You may not need a graph.

## Summary
- 354 nodes · 597 edges · 21 communities (16 shown, 5 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 78 edges (avg confidence: 0.88)
- Token cost: 102,256 input · 0 output

## Community Hubs (Navigation)
- EF Core, React & SQL Exercises
- Integration & Unit Test Infra
- Domain Entities & EF Configs
- Mock A: Web API Exercises
- Projects, Compose & SQL Server
- Advanced .NET & AI Practice
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

## God Nodes (most connected - your core abstractions)
1. `Forte Group Live Coding & Mock Interview Practice Plan` - 31 edges
2. `Booking` - 23 edges
3. `TeeTime` - 20 edges
4. `TeeSheetDbContext` - 18 edges
5. `Payment` - 16 edges
6. `Mock A - .NET Web API live coding` - 16 edges
7. `Mock D - SQL Server` - 16 edges
8. `Course` - 15 edges
9. `TeeSheet.Domain` - 14 edges
10. `Golfer` - 14 edges

## Surprising Connections (you probably didn't know these)
- `Booking entity (Players, Status Held/Confirmed/Cancelled, HoldExpiresUtc, IdempotencyKey)` --references--> `Booking`  [INFERRED]
  docs/forte-live-coding-mock-plan.md → src/TeeSheet.Domain/Booking.cs
- `Course entity (TimeZoneId IANA, Country, Currency)` --references--> `Course`  [INFERRED]
  docs/forte-live-coding-mock-plan.md → src/TeeSheet.Domain/Course.cs
- `Golfer entity (IsMember)` --references--> `Golfer`  [INFERRED]
  docs/forte-live-coding-mock-plan.md → src/TeeSheet.Domain/Golfer.cs
- `Payment entity (Status Pending/Captured/Failed/Refunded)` --references--> `Payment`  [INFERRED]
  docs/forte-live-coding-mock-plan.md → src/TeeSheet.Domain/Payment.cs
- `TeeTime entity (Capacity, BookedPlayers, Price, RowVersion)` --references--> `TeeTime`  [INFERRED]
  docs/forte-live-coding-mock-plan.md → src/TeeSheet.Domain/TeeTime.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Concurrency-safe booking (no double booking, R1)** — docs_forte_live_coding_mock_plan_a3_post_create_booking, docs_forte_live_coding_mock_plan_d4_atomic_booking_statement, docs_forte_live_coding_mock_plan_a4_patch_change_players, docs_forte_live_coding_mock_plan_atomic_conditional_update, docs_forte_live_coding_mock_plan_rowversion_optimistic_concurrency, docs_forte_live_coding_mock_plan_updlock_transaction, docs_forte_live_coding_mock_plan_r1_rule, docs_forte_live_coding_mock_plan_g1_tee_time_booking_platform_design [INFERRED 0.85]
- **Booking business rules R1-R5 enforced across exercises** — docs_forte_live_coding_mock_plan_r1_rule, docs_forte_live_coding_mock_plan_r2_rule, docs_forte_live_coding_mock_plan_r3_rule, docs_forte_live_coding_mock_plan_r4_rule, docs_forte_live_coding_mock_plan_r5_rule, docs_forte_live_coding_mock_plan_a3_post_create_booking, docs_forte_live_coding_mock_plan_a5_delete_cancel_booking, docs_forte_live_coding_mock_plan_a6_idempotency_key_on_post, docs_forte_live_coding_mock_plan_a8_hold_expiry_backgroundservice, docs_forte_live_coding_mock_plan_a11_authorization_policies [INFERRED 0.85]
- **TeeSheet solution projects and local SQL Server infra** — readme_teesheet_api, readme_teesheet_domain, readme_teesheet_infrastructure, readme_teesheet_unittests, readme_teesheet_integrationtests, readme_sql_scripts, compose_sqlserver [INFERRED 0.85]

## Communities (21 total, 5 thin omitted)

### Community 0 - "EF Core, React & SQL Exercises"
Cohesion: 0.07
Nodes (35): A2 - GET list with filter, sort, paging, Answer keys (E1, E2, E3 issues and hints), B1 - Kill the N+1, B7 - Raw SQL safely (FromSql vs FromSqlRaw), C10 - Tests with React Testing Library + MSW, C1 - summarize(bookings) data transformation kata, C2 - Typed useFetch hook with discriminated union, C3 - Tee sheet list with filters (+27 more)

### Community 1 - "Integration & Unit Test Infra"
Cohesion: 0.08
Nodes (7): A1_GetTeeTimeByIdTests, TeeTimeResponse, ApiCollection, ApiFactory, SqlLog, TestData, ModelMappingTests

### Community 2 - "Domain Entities & EF Configs"
Cohesion: 0.09
Nodes (15): TeeSheet.Infrastructure.Persistence.Configurations, TeeSheet.UnitTests, TeeSheet.Domain, Payment, Amount, Booking, BookingId, CreatedUtc (+7 more)

### Community 3 - "Mock A: Web API Exercises"
Cohesion: 0.10
Nodes (27): A10 - Typed HttpClient to payment API (IPaymentGateway), A12 - Integration tests, A1 - GET by id (warm-up), A3 - POST create booking (concurrency-safe), A4 - PATCH change players, A5 - DELETE cancel booking, A6 - Idempotency-Key on POST, A7 - Optimistic concurrency with ETag / If-Match (+19 more)

### Community 4 - "Projects, Compose & SQL Server"
Cohesion: 0.08
Nodes (27): sqlserver service (mssql/server:2022-latest, container teesheet-sql), sqlserver healthcheck (sqlcmd SELECT 1), teesheet-data volume, teetime/ solution layout (TeeTime.Api, Domain, Infrastructure, tests, web, sql), Microsoft.AspNetCore.Mvc.Testing, Microsoft.AspNetCore.OpenApi, Microsoft.EntityFrameworkCore.Design, Microsoft.EntityFrameworkCore.SqlServer (+19 more)

### Community 5 - "Advanced .NET & AI Practice"
Cohesion: 0.11
Nodes (22): A11 - Authorization policies (MemberOnly, CourseAdmin), A8 - Hold expiry with BackgroundService, B2 - Tracking vs no-tracking, B3 - Cartesian explosion (AsSplitQuery), B4 - Bulk operations (ExecuteUpdateAsync), B5 - Zero-downtime migrations (expand/migrate/contract), B6 - Global query filter + soft delete, Booking entity (Players, Status Held/Confirmed/Cancelled, HoldExpiresUtc, IdempotencyKey) (+14 more)

### Community 6 - "API Endpoints & DbContext Wiring"
Cohesion: 0.14
Nodes (7): TeeSheet.Infrastructure.Persistence, TeeSheet.Api.Endpoints, DevEndpoints, TeeTimeEndpoints, Program, NullableUtcDateTimeConverter, UtcDateTimeConverter

### Community 8 - "SQL Command Logging"
Cohesion: 0.16
Nodes (3): CommandLogger, SqlCommandLog, Commands

### Community 9 - "Booking Entity"
Cohesion: 0.17
Nodes (12): Booking, BookingId, CreatedUtc, Golfer, GolferId, HoldExpiresUtc, IdempotencyKey, Payments (+4 more)

### Community 10 - "TeeSheetDbContext"
Cohesion: 0.18
Nodes (6): TeeSheetDbContext, Bookings, Courses, Golfers, Payments, TeeTimes

### Community 11 - "Launch Settings"
Cohesion: 0.18
Nodes (10): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, launchUrl, profiles (+2 more)

### Community 12 - "Booking & Payment Statuses"
Cohesion: 0.20
Nodes (9): BookingStatus, Cancelled, Confirmed, Held, PaymentStatus, Captured, Failed, Pending (+1 more)

### Community 13 - "TeeTime Entity"
Cohesion: 0.20
Nodes (10): TeeTime, BookedPlayers, Bookings, Capacity, Course, CourseId, Price, RowVersion (+2 more)

### Community 14 - "Golfer Entity"
Cohesion: 0.25
Nodes (8): Golfer, Bookings, Email, FirstName, GolferId, IsMember, LastName, GolferConfiguration

### Community 15 - "Course Entity"
Cohesion: 0.25
Nodes (7): Course, Country, CourseId, Currency, Name, TeeTimes, TimeZoneId

### Community 16 - "SQL Schema Tables"
Cohesion: 0.60
Nodes (5): Bookings, Courses, Golfers, Payments, TeeTimes

## Ambiguous Edges - Review These
- `A1 GET /api/teetimes/{id:int} -> TeeTimeDto` → `R1 capacity: non-cancelled players never exceed Capacity`  [AMBIGUOUS]
  README.md · relation: conceptually_related_to

## Knowledge Gaps
- **83 isolated node(s):** `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.NET.Sdk.Web`, `BookingId`, `TeeTimeId` (+78 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 140 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **5 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Forte Group Live Coding & Mock Interview Practice Plan` connect `Advanced .NET & AI Practice` to `EF Core, React & SQL Exercises`, `Mock A: Web API Exercises`, `Projects, Compose & SQL Server`?**
  _High betweenness centrality (0.318) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `Booking` (e.g. with `Booking entity (Players, Status Held/Confirmed/Cancelled, HoldExpiresUtc, IdempotencyKey)` and `Bookings`) actually correct?**
  _`Booking` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.NET.Sdk.Web` to the rest of the system?**
  _83 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `EF Core, React & SQL Exercises` be split into smaller, more focused modules?**
  _Cohesion score 0.07307692307692308 - nodes in this community are weakly interconnected._
- **Why does `ApiFactory` connect `Integration & Unit Test Infra` to `SQL Command Logging`, `API Endpoints & DbContext Wiring`, `WebApplicationFactory Setup`?**
  _High betweenness centrality (0.159) - this node is a cross-community bridge._
- **Are the 3 inferred relationships involving `TeeTime` (e.g. with `TeeTime entity (Capacity, BookedPlayers, Price, RowVersion)` and `TeeTimes`) actually correct?**
  _`TeeTime` has 3 INFERRED edges - model-reasoned connections that need verification._

### Low-confidence Hints
_AMBIGUOUS edges — the extractor was unsure. Verify before acting on these._

- **What is the exact relationship between `A1 GET /api/teetimes/{id:int} -> TeeTimeDto` and `R1 capacity: non-cancelled players never exceed Capacity`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._