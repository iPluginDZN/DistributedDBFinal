namespace AcademicRecords.Shared.Algebra;

public sealed class AlgebraNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Operator { get; set; } = "";
    public string Label { get; set; } = "";
    public string Detail { get; set; } = "";
    public string? Relation { get; set; }
    public List<AlgebraNode> Children { get; set; } = [];

    public static AlgebraNode RelationNode(string relation, string alias)
    {
        return new AlgebraNode
        {
            Operator = "R",
            Label = $"{relation} {alias}".Trim(),
            Detail = "Base relation",
            Relation = relation
        };
    }
}
