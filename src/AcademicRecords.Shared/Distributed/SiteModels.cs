namespace AcademicRecords.Shared.Distributed;

public sealed record SiteHealth(string SiteId, string Name, string Url, bool Online, string Status);

public sealed record FragmentInfo(
    string Relation,
    string Fragment,
    string SiteId,
    string Description,
    bool Replicated);

public sealed record DistributedPlanItem(
    string Relation,
    string Fragment,
    string PreferredSite,
    string SelectedSite,
    string Status,
    string Note);

public sealed record DataPreviewItem(
    string SiteId,
    string Relation,
    string Fragment,
    int RowCount,
    string SampleRows);

public sealed class JoinedResultTable
{
    public List<string> Columns { get; set; } = [];
    public List<Dictionary<string, string>> Rows { get; set; } = [];
}

public sealed class DistributedPlan
{
    public List<DistributedPlanItem> Items { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
