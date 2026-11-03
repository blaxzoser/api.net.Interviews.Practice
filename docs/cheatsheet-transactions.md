# Cheat sheet: booking safely under concurrency

Three ways to enforce R1 in A3: a tee time can never have more players than `Capacity`.

## The problem: "read, check, then write" is a race

Two requests each want 1 spot. The tee time has 3 of 4 spots booked:

| Step | Request A | Request B |
|---|---|---|
| 1 | Reads `BookedPlayers = 3` | |
| 2 | | Reads `BookedPlayers = 3` |
| 3 | `3 + 1 <= 4`, so it continues | |
| 4 | | `3 + 1 <= 4`, so it continues |
| 5 | Saves `BookedPlayers = 4` | |
| 6 | | Saves `BookedPlayers = 4` |

Result: **two bookings were created, but the count only went up by one.** The tee time now really has 5 players. The check was correct when it ran, but out of date by the time of the write.

A plain transaction doesn't fix this. At the default `READ COMMITTED` level, both requests can still read 3.

Each option closes the gap differently:

| Option | Idea | What happens to the second request |
|---|---|---|
| (a) Conditional `UPDATE` | Do the check and the write in one statement | Waits briefly, then the `WHERE` no longer matches if the tee time is full |
| (b) `RowVersion` + retry | Let both proceed, and detect the conflict when saving | Gets an exception, then starts again |
| (c) `UPDLOCK` | Lock the row before reading it | Waits at the read until the first request commits |

## Quick comparison

| | (a) Conditional `UPDATE` | (b) `RowVersion` + retry | (c) `UPDLOCK, ROWLOCK` |
|---|---|---|---|
| Type of concurrency control | Atomic statement | Optimistic | Pessimistic |
| Blocks other requests | Only for the short `UPDATE` until commit | Only during the save | From the read until commit |
| Best when | The rule fits in a `WHERE` clause | Conflicts are rare, or the logic is complex | Conflicts are common, and the logic is complex |
| Performance when busy | Best | Worst, because of retries | Middle |
| Code size | Small | Largest | Medium |
| Works outside SQL Server | Yes | Yes, with that provider's concurrency support | No: the hints are SQL Server syntax |
| Can read the row before deciding | Not needed, and the rule can't use C# logic | Yes | Yes |

**For A3, use (a).** The rule is one simple comparison on one row, so it's the shortest and fastest option.

---

## (a) Atomic conditional `UPDATE`

**How it works:** the capacity check goes inside the `WHERE` clause. SQL Server locks the row while it runs the statement, so a second `UPDATE` waits and then re-checks the `WHERE` against the new value. You then look at the number of rows affected:

- `1`: the spots were reserved.
- `0`: no row matched, so the tee time is full or doesn't exist.

**SQL syntax:**

```sql
BEGIN TRANSACTION;

UPDATE TeeTimes
SET BookedPlayers = BookedPlayers + @p
WHERE TeeTimeId = @id
  AND BookedPlayers + @p <= Capacity;

IF @@ROWCOUNT = 0
BEGIN
    ROLLBACK;
    RETURN;  -- full or missing
END

INSERT INTO Bookings (TeeTimeId, GolferId, Players, Status, CreatedUtc, HoldExpiresUtc)
VALUES (@id, @golferId, @p, 'Held', @now, @holdExpires);

COMMIT;
```

**EF Core syntax:**

```csharp
var rowsAffected = await db.TeeTimes
    .Where(t => t.TeeTimeId == id && t.BookedPlayers + players <= t.Capacity)
    .ExecuteUpdateAsync(s => s.SetProperty(
        t => t.BookedPlayers,
        t => (byte)(t.BookedPlayers + players)), ct);
```

**Full sample:**

```csharp
await using var tx = await db.Database.BeginTransactionAsync(ct);

var rowsAffected = await db.TeeTimes
    .Where(t => t.TeeTimeId == request.TeeTimeId
             && t.BookedPlayers + request.Players <= t.Capacity)
    .ExecuteUpdateAsync(s => s.SetProperty(
        t => t.BookedPlayers,
        t => (byte)(t.BookedPlayers + request.Players)), ct);

if (rowsAffected == 0)
    return TypedResults.Problem(statusCode: 409, title: "Tee time is full");

var booking = new Booking
{
    TeeTimeId = request.TeeTimeId,
    GolferId = request.GolferId,
    Players = request.Players,
    Status = BookingStatus.Held,
    CreatedUtc = now,
    HoldExpiresUtc = now.AddMinutes(10)
};
db.Bookings.Add(booking);

await db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);
```

The transaction is there for the `INSERT`: it makes the two writes succeed or fail together. The capacity safety comes from the `UPDATE` itself.

**Pros**
- Least code, and no retry loop.
- Fastest under load: one short statement and one round trip.
- Can't be overbooked, even from another app, as long as everyone uses this statement.

**Cons**
- `0` rows doesn't say why, so check first that the tee time exists if you need `404` versus `409`.
- Only works when the rule fits in SQL. Something like "members can take the last spot only on weekdays" gets awkward.
- `ExecuteUpdateAsync` skips the change tracker, so an entity already loaded still shows the old `BookedPlayers`.
- It runs immediately, so it needs an explicit transaction to pair with `SaveChangesAsync`.

**Common mistakes**
- Checking capacity in C# first and using an `UPDATE` without the condition. That brings the race back.
- Ignoring the rows affected. The request would then create a booking even when no spots were reserved.

---

## (b) EF Core optimistic concurrency on `RowVersion` + retry

**How it works:** SQL Server changes a row's `ROWVERSION` value on every update. EF adds the value it originally read to the `WHERE` clause of its `UPDATE`. If someone else changed the row first, nothing matches and EF throws `DbUpdateConcurrencyException`. You then reload the row and try again.

**Setup** (this repo already has it, in the schema and the EF configuration):

```sql
RowVersion ROWVERSION
```

```csharp
builder.Property(t => t.RowVersion).IsRowVersion();
```

**The SQL EF generates:**

```sql
UPDATE TeeTimes
SET BookedPlayers = @newValue
WHERE TeeTimeId = @id
  AND RowVersion = @rowVersionFromTheRead;
-- 0 rows affected: EF throws DbUpdateConcurrencyException
```

**EF Core syntax:**

```csharp
try
{
    await db.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    // someone else changed the row: reload and try again
}
```

**Full sample:**

```csharp
const int maxAttempts = 5;

for (var attempt = 1; attempt <= maxAttempts; attempt++)
{
    // Forget the failed attempt, including the booking it tried to add.
    db.ChangeTracker.Clear();

    // Must be tracked: no AsNoTracking here.
    var teeTime = await db.TeeTimes
        .FirstOrDefaultAsync(t => t.TeeTimeId == request.TeeTimeId, ct);

    if (teeTime is null)
        return TypedResults.Problem(statusCode: 404, title: "Tee time not found");

    if (teeTime.BookedPlayers + request.Players > teeTime.Capacity)
        return TypedResults.Problem(statusCode: 409, title: "Tee time is full");

    teeTime.BookedPlayers += request.Players;

    var booking = new Booking
    {
        TeeTimeId = request.TeeTimeId,
        GolferId = request.GolferId,
        Players = request.Players,
        Status = BookingStatus.Held,
        CreatedUtc = now,
        HoldExpiresUtc = now.AddMinutes(10)
    };
    db.Bookings.Add(booking);

    try
    {
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/bookings/{booking.BookingId}");
    }
    catch (DbUpdateConcurrencyException)
    {
        // Wait a random time so the losers don't all collide again.
        await Task.Delay(Random.Shared.Next(10, 50) * attempt, ct);
    }
}

return TypedResults.Problem(statusCode: 503, title: "Too busy, try again");
```

You don't need `BeginTransactionAsync` here. `SaveChangesAsync` already saves both changes in one transaction.

If all 5 attempts conflict, the loop ends and returns the `503`. With 10 requests and 4 spots, 5 attempts are enough, because each conflict means another request took a spot.

**Pros**
- Holds no lock while your code runs, so it's great when conflicts are rare.
- Keeps normal EF code, and any C# logic can go in the rule.
- Detects a change made to any column of the row, not just the one you checked.
- Also fits edits that span user think time, such as `ETag` / `If-Match` in A7.

**Cons**
- Most code: a loop, a tracker reset, a delay and an attempt limit.
- Slow when many people want the same row, because most attempts are wasted. A popular tee time is exactly this case.
- Requests can run out of attempts even when spots remain, if unrelated updates keep changing the row.
- Every attempt repeats the read and the write.

**Common mistakes**
- Retrying without reloading. The old `RowVersion` will just fail again.
- Forgetting `ChangeTracker.Clear()`, which can save the booking from the failed attempt as well.
- Retrying forever, or with no delay.

---

## (c) Transaction with `UPDLOCK, ROWLOCK`

**How it works:** the read itself takes a lock that is held until the transaction ends. A second request's read waits at that line. When it continues, it sees the committed new value, so "read, check, then write" is safe.

| Hint | Meaning |
|---|---|
| `UPDLOCK` | Take an update lock while reading and hold it until commit. Only one transaction can hold it on a row, but normal reads can still proceed |
| `ROWLOCK` | Ask for a row-level lock rather than a page or table lock, so other tee times aren't affected. SQL Server treats it as a request and can still escalate |

**SQL syntax:**

```sql
BEGIN TRANSACTION;

SELECT BookedPlayers, Capacity
FROM TeeTimes WITH (UPDLOCK, ROWLOCK)
WHERE TeeTimeId = @id;

-- check capacity here; if full: ROLLBACK and stop

UPDATE TeeTimes SET BookedPlayers = BookedPlayers + @p WHERE TeeTimeId = @id;

INSERT INTO Bookings (TeeTimeId, GolferId, Players, Status, CreatedUtc, HoldExpiresUtc)
VALUES (@id, @golferId, @p, 'Held', @now, @holdExpires);

COMMIT;
```

**EF Core syntax:** LINQ has no way to write table hints, so use raw SQL. `FromSqlInterpolated` turns `{id}` into a real SQL parameter, so this isn't open to SQL injection.

```csharp
var teeTime = await db.TeeTimes
    .FromSqlInterpolated($"SELECT * FROM TeeTimes WITH (UPDLOCK, ROWLOCK) WHERE TeeTimeId = {id}")
    .FirstOrDefaultAsync(ct);
```

**Full sample:**

```csharp
// The transaction must start BEFORE the read, or the lock is released straight away.
await using var tx = await db.Database.BeginTransactionAsync(ct);

var teeTime = await db.TeeTimes
    .FromSqlInterpolated($"SELECT * FROM TeeTimes WITH (UPDLOCK, ROWLOCK) WHERE TeeTimeId = {request.TeeTimeId}")
    .FirstOrDefaultAsync(ct);

if (teeTime is null)
    return TypedResults.Problem(statusCode: 404, title: "Tee time not found");

if (teeTime.BookedPlayers + request.Players > teeTime.Capacity)
    return TypedResults.Problem(statusCode: 409, title: "Tee time is full");

teeTime.BookedPlayers += request.Players;

var booking = new Booking
{
    TeeTimeId = request.TeeTimeId,
    GolferId = request.GolferId,
    Players = request.Players,
    Status = BookingStatus.Held,
    CreatedUtc = now,
    HoldExpiresUtc = now.AddMinutes(10)
};
db.Bookings.Add(booking);

await db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);

return TypedResults.Created($"/api/bookings/{booking.BookingId}");
```

**Pros**
- Keeps the simple "read, check, then write" flow, with any C# logic in the check.
- No retries: each request waits its turn and succeeds first time, or gets a clear answer.
- `404` and `409` are easy to tell apart, because you have the row.
- Good when many requests want the same row.

**Cons**
- Requests queue up, so a slow transaction slows everyone wanting that row.
- Tied to SQL Server, and uses raw SQL.
- Locking several rows in different orders can cause deadlocks.
- Can't be held across user think time, such as a checkout page.

**Common mistakes**
- Reading outside the transaction, which releases the lock immediately and brings the race back.
- Calling a payment API or other slow service while holding the lock.
- Using `HOLDLOCK` alone. Two requests can both hold the shared lock, then deadlock when both try to update.
- Filtering on a column with no index, which can lock far more than one row.

---

## Testing any of them

The same test works for all three, because it only looks at the outcome. See `Ten_parallel_bookings_for_4_spots_give_exactly_4_successes_and_6_conflicts` in `tests/TeeSheet.IntegrationTests/A3_CreateBookingTests.cs`.

```csharp
var responses = await Task.WhenAll(
    Enumerable.Range(0, 10).Select(_ => PostAsync(teeTimeId, golferId, players: 1)));

Assert.Equal(4, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
Assert.Equal(6, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
```

Use a real SQL Server. The EF InMemory provider has no real transactions or row locks, and doesn't support `ExecuteUpdateAsync` or table hints.

## How to answer in an interview

**"Which one would you pick?"**
> For one row and a simple rule, a conditional `UPDATE`: the check and the write are one atomic statement, it's the fastest, and it needs no retry loop. If the rule needed C# logic, I'd use `UPDLOCK` where the row is busy, and `RowVersion` where conflicts are rare.

**"Why is read, check, then write a race?"**
> Another request can change the row between my check and my write, so I end up deciding from an out-of-date value.

**"Which isolation level would make it safe, and at what cost?"**
> `SERIALIZABLE` or `REPEATABLE READ` would stop the row changing after I read it. But both requests can hold shared locks, then deadlock when both try to update, so one is killed and has to retry. `UPDLOCK` avoids that by letting only one request read for update.

**"How do you handle `DbUpdateConcurrencyException`?"**
> Clear the tracker, reload the row, run the business check again, and retry a limited number of times with a small random delay. If it still fails, return a clear error such as `503` or `409`.

**"What's the difference between optimistic and pessimistic?"**
> Optimistic assumes conflicts are rare: take no lock, then detect a conflict when saving. Pessimistic assumes they're likely: take the lock first so conflicts can't happen, and make others wait.
