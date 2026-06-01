using AcademicRecords.Shared.Dataset;
using AcademicRecords.Shared.Distributed;

var builder = WebApplication.CreateBuilder(args);

var siteId = builder.Configuration["Site:Id"] ?? Environment.GetEnvironmentVariable("SITE_ID") ?? "site-a";
var siteName = builder.Configuration["Site:Name"] ?? Environment.GetEnvironmentVariable("SITE_NAME") ?? "Site A";
var dbPath = builder.Configuration["Site:DatabasePath"] ?? Environment.GetEnvironmentVariable("DATABASE_PATH") ?? $"data/{siteId}.db";

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "Academic Records Site",
    siteId,
    siteName,
    dbPath
}));

app.MapGet("/health", (HttpContext context) =>
{
    var url = $"{context.Request.Scheme}://{context.Request.Host}";
    return new SiteHealth(siteId, siteName, url, true, $"Online; SQLite file: {dbPath}");
});

app.MapGet("/schema", () => Results.Ok(AcademicDataset.Schema));

app.MapGet("/fragments", () => Results.Ok(AcademicDataset.FragmentsForSite(siteId)));

app.MapGet("/data-preview", () =>
{
    var previews = AcademicDataset.DataPreviewForPlan([siteId])
        .Where(preview => preview.SiteId.Equals(siteId, StringComparison.OrdinalIgnoreCase))
        .ToList();
    return Results.Ok(previews);
});

app.MapPost("/execute-plan-fragment", (FragmentExecutionRequest request) =>
{
    var fragments = AcademicDataset.FragmentsForSite(siteId);
    var ownsFragment = fragments.Any(fragment =>
        fragment.Fragment.Equals(request.Fragment, StringComparison.OrdinalIgnoreCase) ||
        fragment.Relation.Equals(request.Relation, StringComparison.OrdinalIgnoreCase));

    return Results.Ok(new FragmentExecutionResponse(
        siteId,
        request.Relation,
        request.Fragment,
        ownsFragment ? "Simulated execution accepted" : "Fragment not owned by this site",
        ownsFragment));
});

app.Run();

internal sealed record FragmentExecutionRequest(string Relation, string Fragment, string Predicate);
internal sealed record FragmentExecutionResponse(string SiteId, string Relation, string Fragment, string Message, bool Accepted);
