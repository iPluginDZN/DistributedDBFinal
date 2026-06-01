# Analysis Report

## Theoretical Basis

Following Özsu and Valduriez distributed database theory, query processing can be viewed as decomposition, localization, global optimization, and local execution. This project focuses on the visible decomposition and optimization phases.

## Query Decomposition

The SQL query is converted into a relational algebra tree. The initial tree preserves a direct reading of the SQL: projection at the root, selection above the full join, and base relations at the leaves. This makes the unoptimized query easy to compare against the optimized form.

## Pushing Down Selections

Selection pushdown moves predicates closer to the base relations they reference:

- `s.department = 'CS'` moves to `Students`
- `c.credits >= 3` moves to `Courses`
- `p.department = 'CS'` moves to `Professors`
- `e.term = '2026S'` moves to `Enrollments`

This is valid because selections over one relation can be applied before joins without changing the final result. In a distributed setting, this reduces intermediate tuple counts and lowers communication cost between sites.

## Commutativity of Joins

For inner joins, join order can be changed when join predicates are preserved. The optimized tree joins filtered relations earlier. This follows the relational algebra equivalence that joins are commutative and associative under the relevant predicates.

The benefit is smaller intermediate results. In a distributed database, smaller intermediate relations reduce network transfer and coordinator processing.

## Projection Pruning

Projection pruning keeps only attributes needed for final output and join predicates. This lowers tuple width and reduces data movement. The UI shows this as the final optimized projection and explains it in the transformation trace.

## Fragmentation and Allocation

The dataset is horizontally fragmented:

- CS students and related enrollments on Site A
- non-CS students and related enrollments on Site B
- replicated reference tables on Site C

This supports data localization. The coordinator maps predicates and relation references to fragments, then reports which site is used.

## Failure Scenario

When Site B is stopped, the coordinator marks it offline. Non-replicated Site B fragments become unavailable. Replicated reference data can be rerouted to another online site. This demonstrates how site availability affects distributed query planning.
