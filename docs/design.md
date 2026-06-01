# Design Document

## Overview

The system is a distributed database simulator for academic records. It accepts a SQL query, converts it into an initial relational algebra tree, applies optimization rules, and displays the optimized result in a native Windows UI.

The design separates visualization from query analysis. WPF renders the result, while the coordinator API performs parsing, algebra tree construction, optimization, site health checks, and distributed planning.

## Architecture

```text
AcademicRecords.App
  -> AcademicRecords.Coordinator
      -> AcademicRecords.Site A
      -> AcademicRecords.Site B
      -> AcademicRecords.Site C
```

The frontend talks only to the coordinator. The coordinator calls each site through HTTP/REST. Each site represents a database location and owns one SQLite database file.

## Data Model and Fragmentation

The project uses four tables:

- `Students`
- `Courses`
- `Enrollments`
- `Professors`

Site A owns CS student fragments and CS enrollments. Site B owns non-CS student fragments and non-CS enrollments. Site C stores replicated reference data for courses and professors. This layout makes selection pushdown meaningful because the predicate `s.department = 'CS'` maps naturally to Site A.

## Query Processing Flow

1. WPF sends SQL to `POST /api/query/analyze`.
2. Coordinator parses aliases, projections, joins, and predicates.
3. Coordinator builds the initial tree as projection over selection over joins.
4. Coordinator pushes single-relation selections close to their base relations.
5. Coordinator reorders joins to place filtered relations earlier.
6. Coordinator applies projection pruning conceptually in the optimized projection node.
7. Coordinator calls site health endpoints and creates the distributed plan.
8. Coordinator includes a deterministic data preview for online fragments, matching the seeded SQLite dataset.
9. WPF renders the response as trees, transformation rows, site status, data preview, and fragment placement.

## UI Layout

The WPF screen contains a SQL editor at the top, initial and optimized tree panels in the middle, and tabbed transformation/distributed/data/health information at the bottom. The vertical regions and tree columns are resizable with splitters. JSON is not shown in the main workflow; it is only the internal API response format.

## Failure Handling

The demo failure is stopping Site B with Docker Compose. The coordinator uses short HTTP timeouts to mark Site B offline. The distributed plan then reports unavailable Site B fragments and identifies whether replicated reference data can be served from another online site.
