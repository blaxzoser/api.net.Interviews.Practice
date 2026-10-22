namespace TeeSheet.Api.Endpoints;

public static class TeeTimeEndpoints
{
    public static IEndpointRouteBuilder MapTeeTimeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teetimes").WithTags("TeeTimes");

        // -----------------------------------------------------------------------------
        // A1 - GET /api/teetimes/{id:int}                       (15 min, Pass 1: no AI)
        //
        //   Returns TeeTimeDto { id, courseName, startUtc, startLocal, spotsLeft, price, currency }
        //   Acceptance: see README.md -> "A1" and
        //   tests/TeeSheet.IntegrationTests/A1_GetTeeTimeByIdTests.cs (red until you finish).
        //
        //   Start with the 2-minute opening talk track, then write it here, out loud.
        // -----------------------------------------------------------------------------

        return app;
    }
}
