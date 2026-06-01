using AcademicRecords.Shared.Algebra;
using AcademicRecords.Shared.Distributed;

namespace AcademicRecords.Shared.Query;

public sealed record AnalyzeQueryRequest(string Sql);

public sealed class AnalyzeQueryResponse
{
    public AlgebraNode InitialTree { get; set; } = new();
    public AlgebraNode OptimizedTree { get; set; } = new();
    public List<TransformationStep> Transformations { get; set; } = [];
    public DistributedPlan DistributedPlan { get; set; } = new();
    public List<SiteHealth> SiteHealth { get; set; } = [];
    public List<DataPreviewItem> DataPreview { get; set; } = [];
}

public sealed class ParsedAcademicQuery
{
    public string Sql { get; set; } = "";
    public List<string> Projections { get; set; } = [];
    public Dictionary<string, string> Aliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Joins { get; set; } = [];
    public List<string> Predicates { get; set; } = [];
}
