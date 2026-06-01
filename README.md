# Algebraic Query Tree Visualizer: Academic Records

Distributed database simulator for visualizing SQL query decomposition into relational algebra trees. The project uses a native WPF frontend, a C#/.NET coordinator API, three HTTP/REST site services, and SQLite-backed simulated sites.

## Architecture

```text
WPF App -> Coordinator API -> Site A / Site B / Site C
```

- `src/AcademicRecords.App`: native Windows WPF UI.
- `src/AcademicRecords.Coordinator`: query analysis and distributed planning API.
- `src/AcademicRecords.Site`: reusable site API for Site A, Site B, and Site C.
- `src/AcademicRecords.Shared`: algebra models, SQL parser, optimizer, dataset metadata.
- `tests/AcademicRecords.Tests`: lightweight offline test runner.

## Requirements

- .NET SDK 8.0 or newer
- Docker Desktop
- Windows for the WPF app

## Run Backend With Docker

Seed the local SQLite files:

```powershell
python scripts/seed_sqlite.py
```

```powershell
docker compose up --build
```

Coordinator URL:

```text
http://localhost:5000
```

Site URLs:

```text
http://localhost:5101
http://localhost:5102
http://localhost:5103
```

## Run WPF App

In another terminal:

```powershell
dotnet run --project src/AcademicRecords.App/AcademicRecords.App.csproj
```

Click `Analyze Query`. The UI renders the coordinator JSON response as:

- Initial algebraic query tree
- Optimized algebraic query tree
- Transformation analysis
- Distributed plan
- Site health

## Failure Demo

Stop Site B:

```powershell
docker compose stop site-b
```

Click `Analyze Query` or `Refresh Health` in the WPF app. Expected result:

```text
Site A: Online
Site B: Offline
Site C: Online
```

The distributed plan reports affected fragments or replica rerouting.

Restart Site B:

```powershell
docker compose start site-b
```

## Tests

```powershell
dotnet run --project tests/AcademicRecords.Tests/AcademicRecords.Tests.csproj
```

## Demo Query

```sql
SELECT s.student_id, s.name, c.title, p.name AS professor_name, e.grade
FROM Students s
JOIN Enrollments e ON s.student_id = e.student_id
JOIN Courses c ON e.course_id = c.course_id
JOIN Professors p ON c.professor_id = p.professor_id
WHERE s.department = 'CS'
  AND c.credits >= 3
  AND p.department = 'CS'
  AND e.term = '2026S';
```

## Additional Demo Queries

<details>
<summary>1. CS students taking 3+ credit CS-taught courses in 2026S</summary>

```sql
SELECT s.student_id, s.name, c.title, p.name AS professor_name, e.grade
FROM Students s
JOIN Enrollments e ON s.student_id = e.student_id
JOIN Courses c ON e.course_id = c.course_id
JOIN Professors p ON c.professor_id = p.professor_id
WHERE s.department = 'CS'
  AND c.credits >= 3
  AND p.department = 'CS'
  AND e.term = '2026S';
```

</details>

<details>
<summary>2. High-credit courses for CS students</summary>

```sql
SELECT s.student_id, s.name, c.title, c.credits, e.grade
FROM Students s
JOIN Enrollments e ON s.student_id = e.student_id
JOIN Courses c ON e.course_id = c.course_id
JOIN Professors p ON c.professor_id = p.professor_id
WHERE s.department = 'CS'
  AND c.credits >= 4
  AND e.term = '2026S'
  AND p.department = 'CS';
```

</details>

<details>
<summary>3. Non-CS students enrolled in CS professor courses</summary>

```sql
SELECT s.student_id, s.name, s.department, c.title, p.name AS professor_name
FROM Students s
JOIN Enrollments e ON s.student_id = e.student_id
JOIN Courses c ON e.course_id = c.course_id
JOIN Professors p ON c.professor_id = p.professor_id
WHERE s.department = 'Business'
  AND p.department = 'CS'
  AND c.credits >= 3
  AND e.term = '2026S';
```

</details>

<details>
<summary>4. Query Optimization course enrollments</summary>

```sql
SELECT s.student_id, s.name, c.title, p.name AS professor_name, e.grade
FROM Students s
JOIN Enrollments e ON s.student_id = e.student_id
JOIN Courses c ON e.course_id = c.course_id
JOIN Professors p ON c.professor_id = p.professor_id
WHERE c.title = 'Query Optimization'
  AND e.term = '2026S'
  AND p.department = 'CS'
  AND s.department = 'CS';
```

</details>

<details>
<summary>5. Distributed Databases course enrollments</summary>

```sql
SELECT s.student_id, s.name, c.title, e.grade, p.name AS professor_name
FROM Students s
JOIN Enrollments e ON s.student_id = e.student_id
JOIN Courses c ON e.course_id = c.course_id
JOIN Professors p ON c.professor_id = p.professor_id
WHERE c.title = 'Distributed Databases'
  AND e.term = '2026S'
  AND c.credits >= 3
  AND p.department = 'CS';
```

</details>

<details>
<summary>6. Senior CS students in 2026S</summary>

```sql
SELECT s.student_id, s.name, s.year, c.title, e.grade
FROM Students s
JOIN Enrollments e ON s.student_id = e.student_id
JOIN Courses c ON e.course_id = c.course_id
JOIN Professors p ON c.professor_id = p.professor_id
WHERE s.department = 'CS'
  AND s.year >= 3
  AND e.term = '2026S'
  AND c.credits >= 3;
```

</details>

<details>
<summary>7. Courses taught by CS professors</summary>

```sql
SELECT s.name, c.title, p.name AS professor_name, p.department, e.grade
FROM Students s
JOIN Enrollments e ON s.student_id = e.student_id
JOIN Courses c ON e.course_id = c.course_id
JOIN Professors p ON c.professor_id = p.professor_id
WHERE p.department = 'CS'
  AND c.credits >= 3
  AND e.term = '2026S'
  AND s.department = 'CS';
```

</details>

<details>
<summary>8. Failure-demo query: references non-CS data on Site B</summary>

```sql
SELECT s.student_id, s.name, s.department, c.title, e.grade
FROM Students s
JOIN Enrollments e ON s.student_id = e.student_id
JOIN Courses c ON e.course_id = c.course_id
JOIN Professors p ON c.professor_id = p.professor_id
WHERE s.department = 'Math'
  AND e.term = '2026S'
  AND c.credits >= 3
  AND p.department = 'CS';
```

</details>

For the screen recording, query 1 is the best normal optimization walkthrough. Query 8 is useful before and after stopping Site B because it makes the Site B dependency easier to explain.

## Scope

This project focuses on query planning, visualization, optimization explanation, and distributed failure demonstration. It intentionally supports a controlled class of `SELECT-FROM-JOIN-WHERE` academic-record queries instead of a full general-purpose distributed SQL engine.

## Data Preview

The WPF app includes a `Data Preview` tab. It shows the seeded fragments currently available from online sites: site id, relation, fragment name, row count, and representative sample rows. This makes the distributed plan visible against the underlying academic-record data instead of only showing tree metadata.

The preview is deterministic and aligned with the SQLite seed scripts in `data/`. The site services expose the preview through `GET /data-preview`, and the coordinator includes it in `POST /api/query/analyze`.

## Joined Result

The WPF app includes a `Joined Result` tab for the default 4-table query. It shows joined rows after the pushed-down predicates are applied, including the source sites used for each row. This is a demonstrative result preview for the academic scenario, not a full general-purpose SQL execution engine.
