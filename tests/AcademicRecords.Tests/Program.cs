using AcademicRecords.Shared.Algebra;
using AcademicRecords.Shared.Dataset;
using AcademicRecords.Shared.Query;

var parsed = AcademicSqlParser.Parse(AcademicDataset.DefaultSql);

Assert(parsed.Projections.Count == 5, "Default query projection count");
Assert(parsed.Aliases.Count == 4, "Default query aliases");
Assert(parsed.Predicates.Count == 4, "Default query predicates");

var initial = AlgebraBuilder.BuildInitialTree(parsed);
Assert(initial.Operator == "π", "Initial root is projection");
Assert(ContainsLabel(initial, "σ s.department = 'CS'"), "Initial tree contains full selection");

var optimized = AlgebraBuilder.BuildOptimizedTree(parsed);
Assert(ContainsDetail(optimized, "Selection pushed to Students"), "Selection pushdown for Students");
Assert(ContainsDetail(optimized, "Selection pushed to Courses"), "Selection pushdown for Courses");
Assert(ContainsDetail(optimized, "Join order adjusted"), "Join commutativity reflected");

var transformations = AlgebraBuilder.BuildTransformations(parsed);
Assert(transformations.Any(step => step.Rule == "Pushing down selections"), "Pushdown transformation present");
Assert(transformations.Any(step => step.Rule == "Commutativity of joins"), "Join commutativity transformation present");
Assert(transformations.Any(step => step.Rule == "Projection pruning"), "Projection pruning transformation present");

Console.WriteLine("All AcademicRecords.Tests checks passed.");

static void Assert(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Test failed: {name}");
    }

    Console.WriteLine($"Passed: {name}");
}

static bool ContainsLabel(AlgebraNode node, string expected)
{
    return node.Label.Contains(expected, StringComparison.OrdinalIgnoreCase) ||
           node.Children.Any(child => ContainsLabel(child, expected));
}

static bool ContainsDetail(AlgebraNode node, string expected)
{
    return node.Detail.Contains(expected, StringComparison.OrdinalIgnoreCase) ||
           node.Children.Any(child => ContainsDetail(child, expected));
}
