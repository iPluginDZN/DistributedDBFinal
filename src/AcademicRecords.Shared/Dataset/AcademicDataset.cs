using AcademicRecords.Shared.Distributed;
using AcademicRecords.Shared.Query;

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

    public static JoinedResultTable JoinedResultForQuery(ParsedAcademicQuery query, IReadOnlyCollection<string> onlineSites)
    {
        var online = onlineSites.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var columns = query.Projections.Select(GetProjectionLabel).ToList();
        columns.Add("Source Sites");

        var results =
            from student in Students()
            join enrollment in Enrollments() on student.StudentId equals enrollment.StudentId
            join course in Courses() on enrollment.CourseId equals course.CourseId
            join professor in Professors() on course.ProfessorId equals professor.ProfessorId
            where RequiredSitesOnline(student, online)
            where MatchesPredicates(query.Predicates, student, enrollment, course, professor)
            select BuildProjectedRow(columns, query.Projections, student, enrollment, course, professor);

        return new JoinedResultTable
        {
            Columns = columns,
            Rows = results.ToList()
        };
    }

    private static Dictionary<string, string> BuildProjectedRow(
        IReadOnlyList<string> columns,
        IReadOnlyList<string> projections,
        StudentRow student,
        EnrollmentRow enrollment,
        CourseRow course,
        ProfessorRow professor)
    {
        var row = new Dictionary<string, string>();

        for (var i = 0; i < projections.Count; i++)
        {
            row[columns[i]] = GetProjectionValue(projections[i], student, enrollment, course, professor);
        }

        row["Source Sites"] = SourceSites(student);
        return row;
    }

    private static string GetProjectionLabel(string projection)
    {
        var parts = SplitAlias(projection);
        if (parts.Length == 2)
        {
            return parts[1];
        }

        return NormalizeProjectionExpression(parts[0]) switch
        {
            "s.student_id" => "student_id",
            "s.name" => "student_name",
            "s.department" => "student_department",
            "s.year" => "student_year",
            "e.enrollment_id" => "enrollment_id",
            "e.student_id" => "enrollment_student_id",
            "e.course_id" => "enrollment_course_id",
            "e.term" => "term",
            "e.grade" => "grade",
            "c.course_id" => "course_id",
            "c.title" => "course_title",
            "c.credits" => "credits",
            "c.professor_id" => "course_professor_id",
            "p.professor_id" => "professor_id",
            "p.name" => "professor_name",
            "p.department" => "professor_department",
            var expression => expression.Replace(".", "_")
        };
    }

    private static string GetProjectionValue(
        string projection,
        StudentRow student,
        EnrollmentRow enrollment,
        CourseRow course,
        ProfessorRow professor)
    {
        var expression = NormalizeProjectionExpression(SplitAlias(projection)[0]);

        return expression switch
        {
            "s.student_id" => student.StudentId.ToString(),
            "s.name" => student.Name,
            "s.department" => student.Department,
            "s.year" => student.Year.ToString(),
            "e.enrollment_id" => enrollment.EnrollmentId.ToString(),
            "e.student_id" => enrollment.StudentId.ToString(),
            "e.course_id" => enrollment.CourseId.ToString(),
            "e.term" => enrollment.Term,
            "e.grade" => enrollment.Grade,
            "c.course_id" => course.CourseId.ToString(),
            "c.title" => course.Title,
            "c.credits" => course.Credits.ToString(),
            "c.professor_id" => course.ProfessorId.ToString(),
            "p.professor_id" => professor.ProfessorId.ToString(),
            "p.name" => professor.Name,
            "p.department" => professor.Department,
            _ => ""
        };
    }

    private static string[] SplitAlias(string projection)
    {
        return projection.Split(" AS ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static string NormalizeProjectionExpression(string expression)
    {
        return expression.Trim().ToLowerInvariant();
    }

    private static bool RequiredSitesOnline(StudentRow student, ISet<string> onlineSites)
    {
        var studentSite = student.Department.Equals("CS", StringComparison.OrdinalIgnoreCase) ? "site-a" : "site-b";
        return onlineSites.Contains(studentSite) && onlineSites.Contains("site-c");
    }

    private static string SourceSites(StudentRow student)
    {
        var studentSite = student.Department.Equals("CS", StringComparison.OrdinalIgnoreCase) ? "Site A" : "Site B";
        return $"Students/Enrollments: {studentSite}; Courses/Professors: Site C";
    }

    private static bool MatchesPredicates(
        IReadOnlyCollection<string> predicates,
        StudentRow student,
        EnrollmentRow enrollment,
        CourseRow course,
        ProfessorRow professor)
    {
        foreach (var predicate in predicates)
        {
            if (!MatchesPredicate(predicate, student, enrollment, course, professor))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesPredicate(
        string predicate,
        StudentRow student,
        EnrollmentRow enrollment,
        CourseRow course,
        ProfessorRow professor)
    {
        var normalized = predicate.Trim().TrimEnd(';');

        return normalized switch
        {
            var text when EqualsText(text, "s.department", student.Department) => true,
            var text when EqualsText(text, "c.title", course.Title) => true,
            var text when EqualsText(text, "p.department", professor.Department) => true,
            var text when EqualsText(text, "e.term", enrollment.Term) => true,
            var text when GreaterOrEqual(text, "c.credits", course.Credits) => true,
            var text when GreaterOrEqual(text, "s.year", student.Year) => true,
            _ => false
        };
    }

    private static bool EqualsText(string predicate, string field, string value)
    {
        var prefix = field + " = ";
        if (!predicate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var expected = predicate[prefix.Length..].Trim().Trim('\'');
        return value.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool GreaterOrEqual(string predicate, string field, int value)
    {
        var prefix = field + " >= ";
        if (!predicate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(predicate[prefix.Length..].Trim(), out var expected) && value >= expected;
    }

    private static List<StudentRow> Students()
    {
        return
        [
            new(1, "An Nguyen", "CS", 2),
            new(2, "Binh Tran", "CS", 3),
            new(3, "Chi Le", "CS", 4),
            new(4, "Dung Pham", "CS", 1),
            new(5, "Eva Nguyen", "CS", 3),
            new(6, "Finn Tran", "CS", 2),
            new(31, "Hoa Vu", "Business", 2),
            new(32, "Khanh Do", "Math", 3),
            new(33, "Linh Ho", "Physics", 4),
            new(34, "Minh Bui", "Literature", 1),
            new(35, "Nhi Phan", "Business", 3),
            new(36, "Oanh Dang", "Math", 2)
        ];
    }

    private static List<EnrollmentRow> Enrollments()
    {
        return
        [
            new(1, 1, 101, "2026S", "A"),
            new(2, 2, 101, "2026S", "B+"),
            new(3, 3, 102, "2026S", "A-"),
            new(4, 4, 103, "2025F", "B"),
            new(5, 5, 103, "2025F", "A"),
            new(6, 6, 104, "2026S", "B"),
            new(31, 31, 104, "2026S", "A"),
            new(32, 32, 102, "2026S", "B"),
            new(33, 33, 105, "2025F", "C+"),
            new(34, 34, 101, "2026S", "B+"),
            new(35, 35, 101, "2026S", "A-"),
            new(36, 36, 106, "2026S", "B")
        ];
    }

    private static List<CourseRow> Courses()
    {
        return
        [
            new(101, "Distributed Databases", 3, 201),
            new(102, "Query Optimization", 3, 202),
            new(103, "Data Structures", 4, 203),
            new(104, "Business Analytics", 3, 204),
            new(105, "Modern Physics", 4, 205),
            new(106, "Linear Algebra", 4, 206)
        ];
    }

    private static List<ProfessorRow> Professors()
    {
        return
        [
            new(201, "Prof. Mai Nguyen", "CS"),
            new(202, "Prof. Quang Tran", "CS"),
            new(203, "Prof. Sara Pham", "CS"),
            new(204, "Prof. Nam Le", "Business"),
            new(205, "Prof. Yen Vo", "Physics"),
            new(206, "Prof. Long Do", "Math")
        ];
    }

    private sealed record StudentRow(int StudentId, string Name, string Department, int Year);
    private sealed record EnrollmentRow(int EnrollmentId, int StudentId, int CourseId, string Term, string Grade);
    private sealed record CourseRow(int CourseId, string Title, int Credits, int ProfessorId);
    private sealed record ProfessorRow(int ProfessorId, string Name, string Department);

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
