using System.Net.Http.Json;
using AcademicRecords.Shared.Algebra;
using AcademicRecords.Shared.Dataset;
using AcademicRecords.Shared.Distributed;
using AcademicRecords.Shared.Query;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddHttpClient();

var app = builder.Build();
app.UseCors();

var configuredSites = new[]
{
    new SiteConfig("site-a", "Site A", builder.Configuration["Sites:SiteA"] ?? "http://localhost:5101"),
    new SiteConfig("site-b", "Site B", builder.Configuration["Sites:SiteB"] ?? "http://localhost:5102"),
    new SiteConfig("site-c", "Site C", builder.Configuration["Sites:SiteC"] ?? "http://localhost:5103")
};

app.MapGet("/", () => Results.Ok(new
{
    service = "Academic Records Coordinator",
    endpoints = new[] { "POST /api/query/analyze", "GET /api/sites/health" }
}));

app.MapGet("/api/dataset/default-query", () => Results.Text(AcademicDataset.DefaultSql, "text/plain"));

app.MapGet("/api/sites/health", async (IHttpClientFactory factory) =>
{
    var health = await GetSiteHealth(factory, configuredSites);
    return Results.Ok(health);
});

app.MapPost("/api/query/analyze", async (AnalyzeQueryRequest request, IHttpClientFactory factory) =>
{
    try
    {
        var parsed = AcademicSqlParser.Parse(request.Sql);
        var health = await GetSiteHealth(factory, configuredSites);

        var response = new AnalyzeQueryResponse
        {
            InitialTree = AlgebraBuilder.BuildInitialTree(parsed),
            OptimizedTree = AlgebraBuilder.BuildOptimizedTree(parsed),
            Transformations = AlgebraBuilder.BuildTransformations(parsed),
            SiteHealth = health,
            DistributedPlan = DistributedPlanner.Build(health),
            DataPreview = AcademicDataset.DataPreviewForPlan(health.Where(site => site.Online).Select(site => site.SiteId).ToList()),
            JoinedResult = AcademicDataset.JoinedResultForQuery(parsed, health.Where(site => site.Online).Select(site => site.SiteId).ToList())
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

static async Task<List<SiteHealth>> GetSiteHealth(IHttpClientFactory factory, IEnumerable<SiteConfig> sites)
{
    var client = factory.CreateClient();
    var results = new List<SiteHealth>();

    foreach (var site in sites)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1.5));
            var health = await client.GetFromJsonAsync<SiteHealth>($"{site.Url}/health", cts.Token);
            results.Add(health ?? new SiteHealth(site.Id, site.Name, site.Url, false, "No response body"));
        }
        catch
        {
            results.Add(new SiteHealth(site.Id, site.Name, site.Url, false, "Offline"));
        }
    }

    return results;
}

internal sealed record SiteConfig(string Id, string Name, string Url);
