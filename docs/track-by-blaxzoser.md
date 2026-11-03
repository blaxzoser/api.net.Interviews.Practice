# Progress tracker

Checklist for every exercise in `forte-live-coding-mock-plan.md`. Deep-dive Q&A notes for each
exercise live in [awnsers-deep-cheatsheet.md](awnsers-deep-cheatsheet.md).

Legend: ✅ pass · ❌ not passing yet · ⏳ not started

## Track A — API

| # | Exercise | Status | Date | Time | Asked AI to clarify AC? | Notes |
|---|---|---|---|---|---|---|
| A1 | GET by id | ✅ | 2026-10-21 | 1h | Yes — needed to clarify `startLocal` / IANA time zone AC | [cheatsheet](awnsers-deep-cheatsheet.md#a1-cheatsheet---get-tee-time-by-id) |
| A2 | GET list with filter, sort, paging | ✅ | 2026-10-28 | ~1h with AI (target 30 min) | Yes — route template (query string inside the route), `Select` vs `SelectMany`, where `endUtc` comes from | Practice paging and the UTC-range step on their own until you can write them from memory. [cheatsheet](awnsers-deep-cheatsheet.md#a2-cheatsheet---get-tee-times-list-filter-sort-paging) |
| A3 | POST create booking and UPDATE tee time | ✅ | 2026-11-02 | 2h with AI (original target 35 min; realistic target now 75 min) | Yes — about 1h went on clarifying the validation. POST also needs an `UPDATE` on `TeeTimes`, the request body and `players` type weren't shown, and R4's details (status codes, exact limit) weren't defined | The original requirements were incomplete and the task was more complicated than it first looked, so the plan now has the missing details and a realistic time. **Score: 6/10** — result 9, independence 4, code quality 6, clarifying requirements 8. AI supplied the atomic `UPDATE`, `BookingRules` and the tests, and `GET /api/bookings/{id}` is still missing. Next: rewrite the conditional `UPDATE` and the parallel test from memory in under 75 min. |
| A4 | PATCH change players | ⏳ | | | | |
| A5 | DELETE cancel booking | ⏳ | | | | |
| A6 | Idempotency-Key on POST | ⏳ | | | | |
| A7 | Optimistic concurrency with ETag / If-Match | ⏳ | | | | |
| A8 | Hold expiry with BackgroundService | ⏳ | | | | |
| A9 | Middleware: correlation id + global error handling | ⏳ | | | | |
| A10 | Typed HttpClient to an external payment API | ⏳ | | | | |
| A11 | Authorization policies | ⏳ | | | | |
| A12 | Integration tests | ⏳ | | | | |

## Track B — EF Core / data access

| # | Exercise | Status | Date | Time | Asked AI to clarify AC? | Notes |
|---|---|---|---|---|---|---|
| B1 | Kill the N+1 | ⏳ | | | | |
| B2 | Tracking vs no-tracking | ⏳ | | | | |
| B3 | Cartesian explosion | ⏳ | | | | |
| B4 | Bulk operations | ⏳ | | | | |
| B5 | Migrations in production | ⏳ | | | | |
| B6 | Global query filter + soft delete | ⏳ | | | | |
| B7 | Raw SQL safely | ⏳ | | | | |

## Track D — SQL

| # | Exercise | Status | Date | Time | Asked AI to clarify AC? | Notes |
|---|---|---|---|---|---|---|
| D1 | Joins & aggregation | ⏳ | | | | |
| D2 | Window functions | ⏳ | | | | |
| D3 | Data integrity hunt | ⏳ | | | | |
| D4 | Atomic booking statement | ⏳ | | | | |
| D5 | Index design from a query | ⏳ | | | | |
| D6 | Sargability | ⏳ | | | | |
| D7 | Parameter sniffing | ⏳ | | | | |
| D8 | Isolation levels & blocking | ⏳ | | | | |
| D9 | Deadlock | ⏳ | | | | |
| D10 | Pagination | ⏳ | | | | |
| D11 | Gaps & islands: free time windows | ⏳ | | | | |
| D12 | Bulk insert | ⏳ | | | | |

## Mocks

| # | Exercise | Status | Date | Time | Asked AI to clarify AC? | Notes |
|---|---|---|---|---|---|---|
| Mock #1 | Forte technical interview (90 min) | ⏳ | | | | |
| Mock #2 | Client technical round (60 min) | ⏳ | | | | |
| Mock #3 | AI-assisted live task (60 min) | ⏳ | | | | |
