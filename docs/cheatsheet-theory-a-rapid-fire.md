# Cheat sheet: Theory A, rapid-fire (.NET API)

The 14 questions from section A of `forte-live-coding-mock-plan.md`. For each one:

- **45-second answer:** about 100 words, which is what you can say out loud in 45 seconds. Practise this part first.
- **Go deeper:** what to say if the interviewer asks a follow-up.
- **Sample:** code or a diagram you can sketch.
- **Traps:** common wrong answers and follow-up questions.

How to practise: cover everything but the question, say the answer out loud with a timer, then compare it with the 45-second answer. Only read "Go deeper" once the short answer is smooth.

| # | Question | One-line memory hook |
|---|---|---|
| 1 | Request pipeline order | Exceptions → HTTPS → static → routing → CORS → auth → authz → endpoint |
| 2 | Service lifetimes | Never inject a shorter lifetime into a longer one |
| 3 | `await` and `.Result` | `await` frees the thread; `.Result` holds it |
| 4 | `IOptions` family | Singleton fixed, scoped per request, monitor live |
| 5 | `IHttpClientFactory` | Reuse sockets, but refresh DNS |
| 6 | Minimal APIs vs controllers | Less ceremony vs more built-in structure |
| 7 | ProblemDetails | One error format: `application/problem+json` |
| 8 | Rate limiting | Fixed = simple, sliding = smooth, token bucket = bursts |
| 9 | Caching | Headers, server output, in-process, shared |
| 10 | `Task` vs `ValueTask` | `ValueTask` only when it often completes synchronously |
| 11 | DTO types | Records for data, `required` for must-have fields |
| 12 | API versioning | Add, don't break; version only when you must |
| 13 | Health checks and tracing | Liveness vs readiness; trace the booking path |
| 14 | Where rules live | Rules in the domain, orchestration in services, HTTP at the edge |

---

## 1. Request pipeline order, start to finish

**45-second answer**
> A request goes through middleware in the order it was registered, and the response comes back through them in reverse. The usual order is: exception handling first, so it catches everything below it; then HSTS and HTTPS redirection; static files; routing; rate limiting and CORS; authentication, then authorization; caching; and finally the endpoint itself. Authentication must come before authorization, because you need to know who the user is before deciding what they can do. Each middleware can short-circuit by not calling `next`.

**Go deeper**
- Middleware is a chain of `RequestDelegate`. Each one does work, calls `await next(context)`, then does work on the way back.
- **Order matters for correctness:**
  - Exception handler first, or exceptions in later middleware aren't caught.
  - `UseAuthentication` before `UseAuthorization`.
  - `UseRouting` before anything that needs to know which endpoint was matched (authorization, rate limiting policies, CORS per endpoint).
  - `UseCors` before `UseOutputCache`.
- With `WebApplication` (minimal hosting), routing and endpoint execution are added automatically at the start and end, so you only add the middleware in between.
- Short-circuiting: static files, health checks and failed authorization can end the request without reaching the endpoint.

**Sample**
```csharp
var app = builder.Build();

app.UseExceptionHandler();       // 1. catches everything below
app.UseHsts();                   // 2. production only
app.UseHttpsRedirection();       // 3.
app.UseStaticFiles();            // 4. may short-circuit
app.UseRouting();                // 5. picks the endpoint
app.UseRateLimiter();            // 6. needs the route to apply policies
app.UseCors();                   // 7.
app.UseAuthentication();         // 8. who are you?
app.UseAuthorization();          // 9. what may you do?
app.UseOutputCache();            // 10.

app.MapBookingEndpoints();       // 11. the endpoint runs last
app.Run();
```

Custom middleware:
```csharp
app.Use(async (context, next) =>
{
    // before the rest of the pipeline
    await next(context);
    // after the rest of the pipeline, on the way back
});
```

**Traps**
- "Authorization before authentication": it can't work, because nobody is identified yet.
- Changing the response after `next` when the response has already started causes an exception.
- Follow-up: "Where does your correlation id middleware go?" Near the top, after the exception handler or before it, so every log line has the id.

---

## 2. Singleton, scoped, transient + captive dependency + `ValidateScopes`

**45-second answer**
> Singleton: one instance for the whole app. Scoped: one per scope, which is one per HTTP request. Transient: a new instance every time it's requested. A captive dependency is when a longer-lived service holds a shorter-lived one, for example a singleton that takes a scoped `DbContext`. The scoped object gets stuck for the app's lifetime and can be used by many requests at once. In Development, `ValidateScopes` is on by default and throws at startup or first use, and `ValidateOnBuild` catches it earlier. The fix is to inject `IServiceScopeFactory` and create a scope when you need the scoped service.

**Go deeper**

| Lifetime | Instances | Good for | Watch out |
|---|---|---|---|
| Singleton | One per app | Caches, config, stateless helpers | Must be thread-safe; can't hold scoped services |
| Scoped | One per request | `DbContext`, unit of work, per-request state | Not available in a singleton |
| Transient | One per resolution | Light, stateless services | Disposable transients are kept until the scope ends |

- A singleton can hold a singleton, a scoped service can hold a scoped, singleton or transient, but never a longer-lived service holding a shorter-lived one.
- `DbContext` is not thread-safe. A captive `DbContext` in a singleton is shared by parallel requests, which causes errors like "A second operation was started on this context" and stale tracked data.
- `BackgroundService` is a singleton, so it can't take `DbContext` in its constructor.

**Sample**
```csharp
// Wrong: singleton captures a scoped DbContext
builder.Services.AddSingleton<ReportCache>();            // ctor takes TeeSheetDbContext

// Right: create a scope when needed
public sealed class HoldExpiryWorker(IServiceScopeFactory scopes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TeeSheetDbContext>();
            // expire holds
        }
    }
}

// Turn the checks on outside Development too
builder.Host.UseDefaultServiceProvider(o =>
{
    o.ValidateScopes = true;
    o.ValidateOnBuild = true;
});
```

**Traps**
- "Scoped means per user": it means per scope, which is per request unless you create scopes yourself.
- Disposable transients resolved from the root provider are never disposed until the app exits.
- Follow-up: "Is a static field the same as a singleton?" Similar effect, but the container controls the singleton's lifetime and disposal, and it can be replaced in tests.

---

## 3. What happens to the thread at `await`? Why is `.Result` dangerous?

**45-second answer**
> When an `await` hits something that isn't finished, the method returns to its caller and the thread goes back to the thread pool, so it can serve other requests. When the I/O completes, the rest of the method continues as a continuation, usually on a different thread. `.Result` or `.Wait()` does the opposite: it blocks the thread until the task finishes. Under load that uses up the thread pool, which is called thread pool starvation, and in contexts with a synchronization context, such as classic ASP.NET or UI apps, it can deadlock. ASP.NET Core has no synchronization context, so the risk is mainly starvation, but you still don't block on async code.

**Go deeper**
- The compiler turns an `async` method into a state machine. At the first incomplete `await`, it saves its state and returns.
- If the awaited task is already complete, the code just continues synchronously, with no thread switch.
- **Synchronization context:** classic ASP.NET and UI frameworks run continuations back on a specific thread. If that thread is blocked on `.Result`, the continuation can never run, and the result is a deadlock.
- **`ConfigureAwait(false)`:** tells the continuation not to return to the captured context. It matters in libraries, but not in ASP.NET Core app code.
- **Sync over async** (`.Result`) and **async over sync** (`Task.Run` to hide blocking work) are both anti-patterns on the server.
- A `ThreadPool` slowly adds threads when starved, so latency spikes appear instead of a clear error.

**Sample**
```csharp
// Bad: blocks a pool thread for the whole database call
var booking = db.Bookings.FirstOrDefaultAsync(b => b.BookingId == id).Result;

// Good: the thread goes back to the pool while SQL works
var booking = await db.Bookings.FirstOrDefaultAsync(b => b.BookingId == id, ct);

// Bad: SaveChanges() is synchronous inside an async handler
db.SaveChanges();
// Good
await db.SaveChangesAsync(ct);
```

**Traps**
- "`await` creates a new thread": it doesn't. No thread waits during I/O at all.
- "`Task.Run` makes it async": it just moves blocking work to another pool thread.
- Always pass the `CancellationToken`, so abandoned requests stop work.
- Follow-up: "What about `async void`?" Only for event handlers: exceptions can't be caught and can crash the process.

---

## 4. `IOptions` vs `IOptionsSnapshot` vs `IOptionsMonitor`

**45-second answer**
> All three read bound configuration. `IOptions<T>` is a singleton that reads the values once and never changes. `IOptionsSnapshot<T>` is scoped, so it re-reads the configuration on each request, and the values stay the same for that whole request. `IOptionsMonitor<T>` is a singleton that always has the current value and raises `OnChange` when the configuration reloads. So: fixed settings use `IOptions`, per-request fresh settings use `Snapshot`, and long-lived services that need live changes, like a background worker, use `Monitor`.

**Go deeper**

| | `IOptions<T>` | `IOptionsSnapshot<T>` | `IOptionsMonitor<T>` |
|---|---|---|---|
| Lifetime | Singleton | Scoped | Singleton |
| Sees config changes | No | Yes, on the next request | Yes, immediately |
| Inject into a singleton | Yes | **No** (captive dependency) | Yes |
| Named options | No | Yes | Yes |
| Change callback | No | No | `OnChange` |

- Reload only happens if the source supports it, for example `appsettings.json` with `reloadOnChange: true`.
- Validate at startup with `ValidateDataAnnotations()` and `ValidateOnStart()`, so a bad value fails the deployment instead of the first request.

**Sample**
```csharp
public sealed class BookingOptions
{
    public const string Section = "Booking";
    [Range(1, 60)] public int HoldMinutes { get; init; } = 10;
    [Range(1, 30)] public int MemberWindowDays { get; init; } = 14;
}

builder.Services
    .AddOptions<BookingOptions>()
    .BindConfiguration(BookingOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Per request, fresh values
app.MapPost("/api/bookings", (IOptionsSnapshot<BookingOptions> options) => options.Value.HoldMinutes);

// In a singleton or worker, live values
public sealed class Worker(IOptionsMonitor<BookingOptions> options)
{
    public int Current => options.CurrentValue.HoldMinutes;
}
```

**Traps**
- Injecting `IOptionsSnapshot` into a singleton fails the scope validation.
- `IOptions` doesn't update after startup, even if the file changes.
- Follow-up: "Where do secrets go?" Not in `appsettings.json`: use user secrets locally and a secret store such as Key Vault or environment variables in production.

---

## 5. Why `IHttpClientFactory`? (Socket exhaustion + DNS changes)

**45-second answer**
> Creating a new `HttpClient` for every request leaves its sockets in `TIME_WAIT`, so under load you run out of sockets. Making one static `HttpClient` fixes that, but it holds connections forever and never notices DNS changes. `IHttpClientFactory` manages and pools the underlying handlers, reuses connections, and recycles each handler on a schedule, two minutes by default, so DNS changes are picked up. It also gives named and typed clients, central configuration, and easy resilience such as retries, timeouts and circuit breakers.

**Go deeper**
- `HttpClient` is cheap and disposable. The `HttpMessageHandler` underneath owns the connections, and that's what's expensive.
- The factory keeps a pool of handlers and gives each client a handler from the pool. When a handler's lifetime expires, it's replaced, but in-flight requests finish first.
- **Typed client:** a class that receives an `HttpClient` and wraps one external API. The right choice for the payment API in A10.
- **Resilience:** `AddStandardResilienceHandler()` from `Microsoft.Extensions.Http.Resilience` adds rate limiter, total timeout, retry, circuit breaker and attempt timeout.
- **Alternative without the factory:** a single long-lived client with `SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) }`.
- Don't retry non-idempotent calls (like charging a card) without an idempotency key.

**Sample**
```csharp
builder.Services
    .AddHttpClient<IPaymentGateway, PaymentGateway>(client =>
    {
        client.BaseAddress = new Uri("https://payments.example.com/");
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddStandardResilienceHandler();

public sealed class PaymentGateway(HttpClient http) : IPaymentGateway
{
    public async Task<PaymentResult> ChargeAsync(ChargeRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("charges", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PaymentResult>(ct))!;
    }
}
```

**Traps**
- `using (var client = new HttpClient())` in a loop: the exact problem the factory solves.
- Capturing a typed client in a singleton keeps one handler forever, so DNS changes are missed. Typed clients are transient for this reason.
- Follow-up: "What does `EnsureSuccessStatusCode` do?" It throws on non-2xx, so you can map the failure to your own error.

---

## 6. Minimal APIs vs controllers: trade-offs

**45-second answer**
> Minimal APIs are lightweight: you map routes to handlers with little ceremony, they start slightly faster and allocate less, and they suit small services and vertical-slice style. Controllers give you more built-in structure: `[ApiController]` conventions, model validation, filters, model binding extensibility and a familiar class-per-resource layout, which helps large teams. Today the gap is small: minimal APIs have route groups, endpoint filters and, from .NET 10, built-in validation. I'd pick minimal APIs for a focused service with clear endpoint files, and controllers if the team already uses them or needs their richer conventions.

**Go deeper**

| | Minimal APIs | Controllers |
|---|---|---|
| Ceremony | Low | More: class, attributes, base class |
| Organisation | You choose: route groups, one file per feature | Class per resource by convention |
| Validation | Manual, or built-in in .NET 10, or a filter/library | `[ApiController]` auto-returns `400` |
| Filters | Endpoint filters | Action, result and exception filters |
| Model binding | Built in, less extensible | Very extensible |
| Testing | `WebApplicationFactory` | `WebApplicationFactory`, or unit-test the class |
| OpenAPI | `WithTags`, `Produces`, typed results | Attributes and conventions |

- **Typed results** (`Results<Ok<T>, NotFound>`) document the possible responses for OpenAPI and make handlers easy to test.
- Both share the same pipeline, routing and DI. It's a style choice, not a different engine.
- Performance difference is small compared with database time.

**Sample**
```csharp
// Minimal API: this repo's style
var group = app.MapGroup("/api/bookings").WithTags("bookings");
group.MapPatch("/{id:int}", UpdateBookedCount);

// Controller equivalent
[ApiController, Route("api/bookings")]
public class BookingsController(TeeSheetDbContext db) : ControllerBase
{
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, BookingRequestUpdateBookedDto dto, CancellationToken ct) { ... }
}
```

**Traps**
- "Minimal APIs can't scale": they can; organise with groups and one `Map...Endpoints` extension per feature.
- Don't put all endpoints in `Program.cs`.
- Follow-up: "Where does validation live?" Same answer for both: a shared validator or the domain rule, not copy-pasted checks.

---

## 7. ProblemDetails / RFC 9457

**45-second answer**
> ProblemDetails is the standard JSON format for HTTP API errors, defined in RFC 9457, which replaced RFC 7807. The media type is `application/problem+json`, and the fields are `type`, `title`, `status`, `detail` and `instance`, plus any extensions you add. Using it means every error from the API has the same shape, so clients can handle errors consistently instead of parsing custom messages. In ASP.NET Core you call `AddProblemDetails()`, return `TypedResults.Problem(...)`, and use `UseExceptionHandler` so even unhandled exceptions come back in this format, without leaking internals.

**Go deeper**
- **Fields:**
  - `type`: a URI that identifies the kind of problem (default `about:blank`)
  - `title`: short, the same for every occurrence
  - `status`: the HTTP status code
  - `detail`: specific to this occurrence
  - `instance`: the request or resource
- Extensions carry data such as `errors` for validation (`ValidationProblemDetails`) or a `traceId`.
- `AddProblemDetails()` also makes the status code pages and the developer exception page return ProblemDetails automatically.
- Don't put stack traces or SQL in `detail` in production.
- `IExceptionHandler` (.NET 8+) lets you map specific exceptions to specific statuses in one place.

**Sample**
```json
{
  "type": "https://example.com/problems/tee-time-full",
  "title": "Tee time is full",
  "status": 409,
  "detail": "Tee time 42 has 1 spot left and 3 were requested.",
  "instance": "/api/bookings"
}
```

```csharp
builder.Services.AddProblemDetails();
app.UseExceptionHandler();
app.UseStatusCodePages();

return TypedResults.Problem(
    statusCode: StatusCodes.Status409Conflict,
    title: "Tee time is full",
    detail: $"Tee time {id} has {left} spot(s) left.");
```

**Traps**
- Mixing formats: some endpoints return a plain string and others ProblemDetails.
- Returning `200` with an error body.
- Follow-up: "`400` vs `422`?" `400` for a malformed or invalid request, `422` for well-formed input that breaks a business rule (like the R4 window). In this repo, `409` is for conflicts with current state, like a full tee time.

---

## 8. Rate limiting: fixed vs sliding vs token bucket

**45-second answer**
> Rate limiting protects the API from overload and abuse by returning `429 Too Many Requests` over a limit. A fixed window allows N requests per window, say 100 per minute. It's simple, but a client can send 100 at the end of one window and 100 at the start of the next. A sliding window splits the window into segments to smooth that out. A token bucket holds tokens that refill at a steady rate, so a client can burst up to the bucket size and then must wait. I'd use token bucket for APIs that should allow short bursts, partitioned per user or IP.

**Go deeper**

| Algorithm | How it works | Good | Weak |
|---|---|---|---|
| Fixed window | N requests per fixed time block | Simple, cheap | Burst at the boundary: up to 2N in a short time |
| Sliding window | Window divided into segments that slide | Smoother, no edge burst | Slightly more bookkeeping |
| Token bucket | Tokens refill at a rate; each request takes one | Controlled bursts | Needs two numbers to tune: bucket size, refill rate |
| Concurrency | Limits requests running at once | Protects slow endpoints | Doesn't limit rate |

- Use **partitions** (by user id, API key or IP) so one client can't use up everyone's limit.
- The built-in limiter is **in-memory per instance**. With 3 instances, a limit of 100 effectively becomes 300. For a shared limit, use a gateway or a Redis-backed limiter.
- Send `Retry-After` so well-behaved clients know when to come back.

**Sample**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("bookings", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 10,                              // burst size
                TokensPerPeriod = 2,                          // refill
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                QueueLimit = 0
            }));
});

app.UseRateLimiter();
app.MapPost("/api/bookings", CreateBooking).RequireRateLimiting("bookings");
```

**Traps**
- One global limit for everyone: one noisy client blocks all.
- Using the client-supplied IP header without trusting the proxy: it's easy to fake.
- Follow-up: "Rate limiting vs idempotency?" Different jobs: limiting controls volume, idempotency makes retries safe.

---

## 9. Output caching vs response caching vs `HybridCache` / `IMemoryCache` / Redis

**45-second answer**
> Response caching follows HTTP caching headers like `Cache-Control`, and it mostly lets clients and proxies cache. It's limited and doesn't suit authenticated traffic. Output caching, from .NET 7, caches whole responses on the server, with policies, vary-by rules and tag-based invalidation. `IMemoryCache` caches objects inside one process: fast, but not shared between instances. Redis, through `IDistributedCache`, is shared across instances but slower, because of the network. `HybridCache`, from .NET 9, combines both: a local in-memory first level, a distributed second level, and protection against cache stampede, where many requests compute the same missing value at once.

**Go deeper**

| | Where it caches | Shared across instances | Good for |
|---|---|---|---|
| Response caching | Client, proxy, or server per HTTP headers | Via proxy | Public, static-ish GETs |
| Output caching | Server memory (or custom store) | No by default | Whole GET responses, with tag invalidation |
| `IMemoryCache` | Process memory | No | Small, hot lookups per instance |
| Redis / `IDistributedCache` | External store | Yes | Shared state, many instances |
| `HybridCache` | L1 memory + L2 distributed | Yes | Data you compute, with stampede protection |

- **Stampede:** a popular key expires and 500 requests all hit the database. `HybridCache.GetOrCreateAsync` runs the factory once per key and makes the others wait.
- **Invalidation** is the hard part. Tags (output cache and HybridCache) make it manageable: evict `"teetimes"` when a booking changes.
- **Don't cache** availability counts for long. For this domain a stale `SpotsLeft` can lead to a failed booking, so keep the TTL short and let the write path (the conditional `UPDATE`) stay the source of truth.

**Sample**
```csharp
// Output caching with a tag
builder.Services.AddOutputCache(o =>
    o.AddPolicy("TeeTimes", p => p.Expire(TimeSpan.FromSeconds(30)).Tag("teetimes")));
app.UseOutputCache();
group.MapGet("/", GetTeeTimes).CacheOutput("TeeTimes");

// After a booking changes availability
await cacheStore.EvictByTagAsync("teetimes", ct);

// HybridCache
var course = await hybrid.GetOrCreateAsync(
    $"course:{id}",
    async token => await db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseId == id, token),
    cancellationToken: ct);
```

**Traps**
- Caching per-user data in a shared cache without a vary-by-user key leaks data between users.
- "Cache everything": a cache can hide a missing index and hold stale data.
- Follow-up: "Cache-aside vs write-through?" Cache-aside (read, miss, load, store) is what `GetOrCreateAsync` does; write-through updates the cache on every write.

---

## 10. `Task` vs `ValueTask`; `IAsyncEnumerable` streaming

**45-second answer**
> `Task` is a reference type, so each call that returns one may allocate. `ValueTask` is a struct that avoids that allocation when the result is already available, so it helps on hot paths that often finish synchronously, like a cache hit. The cost is rules: you await it once, you don't await it twice or in parallel, and you don't read the result before it completes. For most code, `Task` is the right default. `IAsyncEnumerable<T>` lets you produce results one at a time and consume them with `await foreach`, so a large result streams to the client instead of being fully built in memory first.

**Go deeper**
- **When `ValueTask` pays off:** the method completes synchronously most of the time (cache hit, buffered read) and is called very often. Measure first.
- **`ValueTask` rules:**
  - Await it exactly once.
  - Don't call `.GetAwaiter().GetResult()` until it's complete.
  - To use it more than once, convert with `.AsTask()`.
- **`IAsyncEnumerable<T>`:**
  - Pulls items as the consumer asks, with `await foreach`.
  - Pairs with EF Core's `AsAsyncEnumerable()` to stream rows.
  - Minimal APIs and MVC stream it as a JSON array, without buffering it all.
  - Cancellation: add `[EnumeratorCancellation] CancellationToken` in `async` iterators.
- Streaming reduces memory, but the database connection stays open while the client reads, so a slow client holds a connection.

**Sample**
```csharp
// ValueTask: cache hit completes synchronously
public ValueTask<Course?> GetCourseAsync(int id)
{
    if (_cache.TryGetValue(id, out Course? course))
        return ValueTask.FromResult(course);          // no allocation
    return new ValueTask<Course?>(LoadAsync(id));
}

// IAsyncEnumerable: stream rows
app.MapGet("/api/export", (TeeSheetDbContext db) =>
    db.Bookings.AsNoTracking().AsAsyncEnumerable());

public static async IAsyncEnumerable<int> CountAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    for (var i = 0; i < 10; i++)
    {
        await Task.Delay(100, ct);
        yield return i;
    }
}
```

**Traps**
- Using `ValueTask` everywhere "for speed": it makes code riskier without a measured gain.
- Awaiting the same `ValueTask` twice: undefined behaviour.
- Follow-up: "`ToListAsync` vs streaming?" `ToListAsync` loads everything first; streaming starts sending sooner and uses less memory, but keeps the connection busy.

---

## 11. Records vs classes for DTOs; `required` members; nullable reference types

**45-second answer**
> A record is a type with value-based equality, a built-in `with` expression, and a short positional syntax, and it's immutable by default if you use `init` or positional parameters. That makes records a good fit for DTOs, which are just data. A class has reference equality and is better for entities with identity and behaviour, like EF entities. The `required` modifier, from C# 11, forces callers and the JSON deserializer to set a property, so a missing field fails instead of silently being null or zero. Nullable reference types add compile-time null checking: `string` means non-null and `string?` means it may be null. It's only compile-time, so you still validate input at the boundary.

**Go deeper**
- **Record benefits:** equality by value (great in tests: `Assert.Equal(expected, actual)`), `with` copies, readable `ToString`.
- **EF entities should stay classes:** EF tracks them by reference and mutates them.
- **`required` + `init`:**
  - `required` means it must be set in the object initializer.
  - `System.Text.Json` enforces it when deserializing: a missing property throws, which minimal APIs turn into `400`.
  - This is exactly the PUT rule from A4.1: all fields required.
- **Nullable reference types:**
  - Fix warnings with `required`, a constructor, or `= null!` for properties EF fills in.
  - `string` isn't guaranteed non-null at runtime: JSON can still send `null`.

**Sample**
```csharp
// Positional record
public record BookingRequest(int TeeTimeId, int GolferId, byte Players);

// Record with required members: a missing JSON field fails binding with 400
public record ReplaceBookingRequest
{
    public required int TeeTimeId { get; init; }
    public required int GolferId { get; init; }
    public required byte Players { get; init; }
}

// Copy with a change
var moved = request with { TeeTimeId = 43 };
```

This repo's DTOs (for example `BookingRequestDto`) are classes with `set`, which works but allows a missing field to arrive as `0`. Converting them to `required` properties is the PUT fix in A4.1.

**Traps**
- "Records are always immutable": positional ones are; a record with `set` isn't.
- Using records as EF entities.
- Follow-up: "Why `required` and not `[Required]`?" `required` is enforced by the compiler and the JSON deserializer; `[Required]` is a validation attribute checked later, and a missing `int` still arrives as `0`.

---

## 12. How you'd version an API (URL vs header) and keep old clients working

**45-second answer**
> First, avoid breaking changes: adding optional fields and new endpoints doesn't need a new version. When I must break the contract, I version. The simplest and most visible way is the URL, like `/api/v2/bookings`. It's easy to route, cache and document, and easy to see in logs. Header or query-string versioning keeps URLs clean but is easier to forget or miss. I'd use the `Asp.Versioning` package, keep v1 running side by side with v2, mark v1 deprecated with `Deprecation` and `Sunset` headers, and share the underlying logic so both versions don't diverge.

**Go deeper**

| | URL (`/v2/...`) | Header (`api-version: 2`) | Query (`?api-version=2`) |
|---|---|---|---|
| Visible | Very | No | Somewhat |
| Easy to test in a browser | Yes | No | Yes |
| Caching | Simple | Needs `Vary` | Simple |
| URL purity (REST purists) | Less | Best | Medium |

- **Non-breaking** (no new version): adding optional request fields, adding response fields, adding endpoints.
- **Breaking:** removing or renaming fields, changing types, changing status codes or meaning, making an optional field required.
- Old clients keep working because v1 stays deployed until usage drops. Track which clients call which version in logs or metrics.
- Version only the public contract (DTOs and endpoints), not the domain logic. Map both versions to the same service.
- Tell clients early: a deprecation header, release notes, and a clear sunset date.

**Sample**
```csharp
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;                 // adds api-supported-versions header
}).AddApiExplorer();

var v1 = app.NewVersionedApi();
v1.MapGroup("/api/v{version:apiVersion}/bookings").HasApiVersion(1.0).MapPost("/", CreateBookingV1);
v1.MapGroup("/api/v{version:apiVersion}/bookings").HasApiVersion(2.0).MapPost("/", CreateBookingV2);
```

**Traps**
- Changing the meaning of a field without changing its name: the worst kind of breaking change.
- Versioning every small change, which multiplies the code you maintain.
- Follow-up: "What's a safe way to rename a field?" Add the new field, return both for a while, deprecate the old one, then remove it in a new version (expand, migrate, contract).

---

## 13. Health checks + OpenTelemetry: what you'd trace in booking

**45-second answer**
> Health checks tell the platform whether the app is alive and ready. Liveness means the process is running; readiness means it can serve traffic, so it includes dependencies like SQL Server. A load balancer uses readiness to stop sending requests to a bad instance. OpenTelemetry gives traces, metrics and logs with a shared trace id. For booking, I'd trace the whole `POST /api/bookings` request, the EF Core SQL commands inside it, and add a custom span with tags like tee time id, players and outcome. I'd also record metrics: bookings created, conflicts (`409`), and request duration, and I'd keep personal data out of tags.

**Go deeper**
- **Health checks:**
  - `/health/live`: no dependency checks, just "the app responds".
  - `/health/ready`: includes the database check, and optionally external services.
  - Don't make liveness depend on the database: if the DB is down, restarting the app won't help, and restarts make things worse.
- **OpenTelemetry signals:**
  - **Traces:** one request, many spans. ASP.NET Core, `HttpClient` and EF Core/SqlClient have instrumentation.
  - **Metrics:** counters and histograms for dashboards and alerts.
  - **Logs:** structured logs that carry `TraceId`, so you can jump from a trace to its logs.
- **What to trace in booking:**
  - The request span: route, status code.
  - Child spans: load tee time, load golfer, the conditional `UPDATE`, the `INSERT`, commit.
  - Tags: `teeTimeId`, `players`, `outcome` (`created`, `full`, `window_closed`).
  - Metrics: `bookings.created`, `bookings.conflicts`, `booking.duration`.
- Sample traces in production, not 100%, to control cost.
- The **parallel booking test** from A3 shows why conflicts matter: a spike in `409` is business data, not just errors.

**Sample**
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TeeSheetDbContext>("database", tags: ["ready"]);

app.MapHealthChecks("/health/live", new() { Predicate = _ => false });          // no checks: alive
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddSource(BookingTelemetry.SourceName)
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddMeter(BookingTelemetry.SourceName)
        .AddOtlpExporter());

public static class BookingTelemetry
{
    public const string SourceName = "TeeSheet.Booking";
    public static readonly ActivitySource Source = new(SourceName);
    private static readonly Meter Meter = new(SourceName);
    public static readonly Counter<long> Created = Meter.CreateCounter<long>("bookings.created");
    public static readonly Counter<long> Conflicts = Meter.CreateCounter<long>("bookings.conflicts");
}

// In the handler
using var activity = BookingTelemetry.Source.StartActivity("CreateBooking");
activity?.SetTag("teeTimeId", request.TeeTimeId);
activity?.SetTag("players", request.Players);
// ...
activity?.SetTag("outcome", "full");
BookingTelemetry.Conflicts.Add(1);
```

**Traps**
- Putting email or names in span tags (personal data).
- A liveness check that calls the database.
- Tagging with unbounded values (like a booking id) on **metrics**: it explodes cardinality. IDs are fine on traces, not on metric tags.
- Follow-up: "How do you find why one request was slow?" Open its trace by id, and look for the longest span: usually a SQL command.

---

## 14. Where business rules live (domain vs service vs endpoint)

**45-second answer**
> The endpoint should only translate HTTP: bind the request, call something, map the result to a status code. Pure business rules, like "players must be 1 to 4" or "members can book 14 days ahead", belong in the domain, where they have no database or HTTP dependency, so they're easy to unit test. A service, or the handler, orchestrates: it loads data, calls the rules, writes to the database and manages the transaction. Rules that depend on shared state under concurrency, like capacity, must be enforced where that state lives, in the database, with an atomic statement. So I split them by what they need: rules without state go in the domain, orchestration in a service, concurrency in SQL.

**Go deeper**

| Layer | Responsibility | Example in this repo |
|---|---|---|
| Endpoint | HTTP in, HTTP out: binding, status codes, ProblemDetails | `BookingEndpoints.CreateBookingRequest` |
| Service / handler | Orchestration: load, call rules, write, transaction | The transaction in the handler |
| Domain | Pure rules, no I/O, takes `now` as a parameter | `BookingRules.IsWithinBookingWindow` |
| Database | Rules about shared state under concurrency | The conditional `UPDATE ... WHERE BookedPlayers + @p <= Capacity` |

- **Why take `now` as a parameter:** the rule becomes deterministic, so a unit test can check "exactly 14 days" without a real clock. In production code, `TimeProvider` does the same.
- **Why not in the endpoint:** the same rule is needed by POST, PATCH and PUT. Copied checks drift apart (A3 and A4 both use `IsValidPlayers` for that reason).
- **Why not only in C# for capacity:** a C# check followed by a write is a race. The check must be inside the atomic statement.
- **Don't over-engineer:** a small project doesn't need repositories, MediatR and five layers. A static rules class and an endpoint file is a reasonable start; grow it when it hurts.
- **Anemic vs rich domain model:** rich models put behaviour on the entity (`booking.ChangePlayers(n)`). It's a valid choice, but harder with EF set-based updates like `ExecuteUpdateAsync`.

**Sample**
```csharp
// Domain: pure and unit testable
public static class BookingRules
{
    public static bool IsValidPlayers(byte players) => players is >= 1 and <= 4;
    public static bool IsWithinBookingWindow(DateTime startUtc, bool isMember, DateTime nowUtc) =>
        startUtc <= nowUtc.AddDays(isMember ? 14 : 7);
}

// Endpoint/handler: orchestration and HTTP mapping
if (!BookingRules.IsValidPlayers(request.Players))
    return TypedResults.Problem(statusCode: 400, title: "Players must be 1 to 4");

// Database: the concurrency-safe rule
var rows = await db.TeeTimes
    .Where(t => t.TeeTimeId == id && t.BookedPlayers + players <= t.Capacity)
    .ExecuteUpdateAsync(s => s.SetProperty(t => t.BookedPlayers, t => (byte)(t.BookedPlayers + players)), ct);
```

**Traps**
- Business logic inside `Program.cs` lambdas that grows to hundreds of lines.
- Putting HTTP concepts (status codes, `HttpContext`) in the domain.
- Follow-up: "Where would validation attributes go?" On the request DTO for shape (required, range); business rules that need data (tee time exists, golfer is a member) stay in the handler or service.

---

## Common follow-up questions that connect these answers

| If they ask... | Connect to |
|---|---|
| "How do you make this production-ready?" | #7 ProblemDetails, #8 rate limiting, #13 health checks and tracing |
| "How does it behave under load?" | #3 async, #5 `HttpClient` reuse, #9 caching, A3 concurrency |
| "How would you test it?" | #14 pure rules (unit tests), `WebApplicationFactory` with real SQL Server (integration tests) |
| "How do you deploy a change safely?" | #12 versioning, expand-migrate-contract for the database |
| "What would you never let the AI do here?" | Skip the atomic `UPDATE`, log personal data, or change a public contract without a version |

## Rapid self-check (answer out loud in 45 seconds)

1. Name the pipeline order, and why authentication precedes authorization.
2. Give an example of a captive dependency and fix it.
3. What happens to the thread at an `await`?
4. When do you use `IOptionsMonitor` instead of `IOptionsSnapshot`?
5. Two problems with `new HttpClient()` per request and with a static one.
6. One reason to pick controllers over minimal APIs, and one for the reverse.
7. The five ProblemDetails fields.
8. Token bucket vs fixed window: when does fixed window fail?
9. What is a cache stampede, and what prevents it?
10. Why isn't `ValueTask` the default?
11. Why does `required` beat `[Required]` for a PUT body?
12. Is adding an optional response field a breaking change?
13. Why shouldn't a liveness check call the database?
14. Where does the capacity rule live, and why not only in C#?
