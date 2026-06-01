using AcademicRecords.Shared.Algebra;
using AcademicRecords.Shared.Dataset;
using AcademicRecords.Shared.Query;

var parsed = AcademicSqlParser.Parse(AcademicDataset.DefaultSql);
var onlineSites = new[] { "site-a", "site-b", "site-c" };

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

var defaultRows = AcademicDataset.JoinedResultForQuery(parsed, onlineSites);
Assert(defaultRows.Rows.Count == 3, "Default query joined result rows");
Assert(defaultRows.Columns.SequenceEqual(["student_id", "student_name", "course_title", "professor_name", "grade", "Source Sites"]), "Default query joined result columns match SELECT list");
Assert(defaultRows.Rows[0]["student_name"] == "An Nguyen", "Default query fills student_name");
Assert(defaultRows.Rows[0]["professor_name"] == "Prof. Mai Nguyen", "Default query fills professor_name");

var unaliasedDuplicateNameQuery = AcademicSqlParser.Parse("""
    SELECT s.student_id, s.name, s.department, c.title, p.name
    FROM Students s
    JOIN Enrollments e ON s.student_id = e.student_id
    JOIN Courses c ON e.course_id = c.course_id
    JOIN Professors p ON c.professor_id = p.professor_id
    WHERE s.department = 'CS'
      AND c.credits >= 3
      AND p.department = 'CS'
      AND e.term = '2026S';
    """);
var unaliasedRows = AcademicDataset.JoinedResultForQuery(unaliasedDuplicateNameQuery, onlineSites);
Assert(unaliasedRows.Columns.SequenceEqual(["student_id", "student_name", "student_department", "course_title", "professor_name", "Source Sites"]), "Unaliased duplicate name columns are unique");
Assert(unaliasedRows.Rows[0]["student_name"] == "An Nguyen", "Unaliased student name has data");
Assert(unaliasedRows.Rows[0]["professor_name"] == "Prof. Mai Nguyen", "Unaliased professor name has data");

var emptyHighCreditQuery = AcademicSqlParser.Parse("""
    SELECT s.student_id, s.name, c.title, c.credits, e.grade
    FROM Students s
    JOIN Enrollments e ON s.student_id = e.student_id
    JOIN Courses c ON e.course_id = c.course_id
    JOIN Professors p ON c.professor_id = p.professor_id
    WHERE s.department = 'CS'
      AND c.credits >= 4
      AND e.term = '2026S'
      AND p.department = 'CS';
    """);
var emptyRows = AcademicDataset.JoinedResultForQuery(emptyHighCreditQuery, onlineSites);
Assert(emptyRows.Rows.Count == 0, "High-credit CS demo query intentionally returns no rows");

var siteBQuery = AcademicSqlParser.Parse("""
    SELECT s.student_id, s.name, s.department, c.title, e.grade
    FROM Students s
    JOIN Enrollments e ON s.student_id = e.student_id
    JOIN Courses c ON e.course_id = c.course_id
    JOIN Professors p ON c.professor_id = p.professor_id
    WHERE s.department = 'Math'
      AND e.term = '2026S'
      AND c.credits >= 3
      AND p.department = 'CS';
    """);
var siteBRows = AcademicDataset.JoinedResultForQuery(siteBQuery, onlineSites);
Assert(siteBRows.Rows.Count == 1, "Site B demo query returns one row when Site B is online");
Assert(AcademicDataset.JoinedResultForQuery(siteBQuery, ["site-a", "site-c"]).Rows.Count == 0, "Site B demo query returns no rows when Site B is offline");

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
