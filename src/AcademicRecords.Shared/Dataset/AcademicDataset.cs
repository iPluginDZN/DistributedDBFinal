using AcademicRecords.Shared.Distributed;

namespace AcademicRecords.Shared.Dataset;

public static class AcademicDataset
{
    public const string DefaultSql = """
        SELECT s.student_id, s.name, c.title, p.name AS professor_name, e.grade
        FROM Students s
        JOIN Enrollments e ON s.student_id = e.student_id
        JOIN Courses c ON e.course_id = c.course_id
        JOIN Professors p ON c.professor_id = p.professor_id
        WHERE s.department = 'CS'
          AND c.credits >= 3
          AND p.department = 'CS'
          AND e.term = '2026S';
        """;

    public static readonly string[] Schema =
    [
        "Students(student_id, name, department, year)",
        "Courses(course_id, title, credits, professor_id)",
        "Enrollments(enrollment_id, student_id, course_id, term, grade)",
        "Professors(professor_id, name, department)"
    ];

    public static List<FragmentInfo> FragmentsForSite(string siteId)
    {
        return AllFragments()
            .Where(fragment => fragment.SiteId.Equals(siteId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static List<FragmentInfo> AllFragments()
    {
        return
        [
            new("Students", "Students_CS", "site-a", "Horizontal fragment: department = 'CS'", false),
            new("Enrollments", "Enrollments_CS", "site-a", "Enrollments for CS students", false),
            new("Courses", "Courses_Replica_A", "site-a", "Replicated course reference data", true),

            new("Students", "Students_NonCS", "site-b", "Horizontal fragment: department <> 'CS'", false),
            new("Enrollments", "Enrollments_NonCS", "site-b", "Enrollments for non-CS students", false),
            new("Professors", "Professors_Replica_B", "site-b", "Replicated professor reference data", true),

            new("Courses", "Courses_Replica_C", "site-c", "Replicated course reference data", true),
            new("Professors", "Professors_Replica_C", "site-c", "Replicated professor reference data", true),
            new("Students", "Students_Backup_Metadata", "site-c", "Backup metadata only for failure explanation", true)
        ];
    }

    public static List<DataPreviewItem> DataPreviewForPlan(IReadOnlyCollection<string> onlineSites)
    {
        var online = onlineSites.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var previews = new List<DataPreviewItem>();

        AddIfOnline(previews, online, "site-a", "Students", "Students_CS", 30,
            "1 An Nguyen CS Y2; 2 Binh Tran CS Y3; 3 Chi Le CS Y4");
        AddIfOnline(previews, online, "site-a", "Enrollments", "Enrollments_CS", 75,
            "1 student=1 course=101 term=2026S grade=A; 2 student=2 course=101 term=2026S grade=B+");
        AddIfOnline(previews, online, "site-a", "Courses", "Courses_Replica_A", 12,
            "101 Distributed Databases 3cr; 102 Query Optimization 3cr");

        AddIfOnline(previews, online, "site-b", "Students", "Students_NonCS", 30,
            "31 Hoa Vu Business Y2; 32 Khanh Do Math Y3; 33 Linh Ho Physics Y4");
        AddIfOnline(previews, online, "site-b", "Enrollments", "Enrollments_NonCS", 75,
            "31 student=31 course=104 term=2026S grade=A; 32 student=32 course=102 term=2026S grade=B");
        AddIfOnline(previews, online, "site-b", "Professors", "Professors_Replica_B", 10,
            "201 Prof. Mai Nguyen CS; 202 Prof. Quang Tran CS");

        AddIfOnline(previews, online, "site-c", "Courses", "Courses_Replica_C", 12,
            "101 Distributed Databases 3cr; 102 Query Optimization 3cr; 104 Business Analytics 3cr");
        AddIfOnline(previews, online, "site-c", "Professors", "Professors_Replica_C", 10,
            "201 Prof. Mai Nguyen CS; 202 Prof. Quang Tran CS; 204 Prof. Nam Le Business");

        return previews;
    }

    private static void AddIfOnline(
        List<DataPreviewItem> previews,
        ISet<string> onlineSites,
        string siteId,
        string relation,
        string fragment,
        int rowCount,
        string sampleRows)
    {
        if (onlineSites.Contains(siteId))
        {
            previews.Add(new DataPreviewItem(siteId, relation, fragment, rowCount, sampleRows));
        }
    }
}
