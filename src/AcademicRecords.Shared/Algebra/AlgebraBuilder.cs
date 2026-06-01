using AcademicRecords.Shared.Query;

namespace AcademicRecords.Shared.Algebra;

public static class AlgebraBuilder
{
    public static AlgebraNode BuildInitialTree(ParsedAcademicQuery query)
    {
        var projection = new AlgebraNode
        {
            Operator = "π",
            Label = "π " + string.Join(", ", query.Projections),
            Detail = "Final projection"
        };

        AlgebraNode current = BuildJoinTree(query);

        if (query.Predicates.Count > 0)
        {
            current = new AlgebraNode
            {
                Operator = "σ",
                Label = "σ " + string.Join(" AND ", query.Predicates),
                Detail = "Initial selection above the complete join",
                Children = [current]
            };
        }

        projection.Children.Add(current);
        return projection;
    }

    public static AlgebraNode BuildOptimizedTree(ParsedAcademicQuery query)
    {
        var projection = new AlgebraNode
        {
            Operator = "π",
            Label = "π " + string.Join(", ", query.Projections),
            Detail = "Projection after pruning unnecessary attributes"
        };

        var relationNodes = query.Aliases
            .Select(pair => ApplyLocalSelections(pair.Value, pair.Key, query.Predicates))
            .OrderBy(node => OptimizationRank(node.Relation))
            .ToList();

        AlgebraNode current = relationNodes[0];
        for (var i = 1; i < relationNodes.Count; i++)
        {
            var joinCondition = query.Joins.ElementAtOrDefault(i - 1) ?? "join predicate";
            current = new AlgebraNode
            {
                Operator = "⋈",
                Label = "⋈ " + joinCondition,
                Detail = "Join order adjusted with commutativity",
                Children = [current, relationNodes[i]]
            };
        }

        projection.Children.Add(current);
        return projection;
    }

    public static List<TransformationStep> BuildTransformations(ParsedAcademicQuery query)
    {
        var steps = new List<TransformationStep>
        {
            new(1, "SQL to relational algebra",
                "SQL SELECT-FROM-JOIN-WHERE statement",
                "Initial π over σ over ⋈ tree",
                "Query decomposition expresses SQL as relational algebra operators.")
        };

        var step = 2;
        foreach (var predicate in query.Predicates)
        {
            var relation = ResolvePredicateRelation(predicate, query);
            steps.Add(new TransformationStep(
                step++,
                "Pushing down selections",
                $"Predicate '{predicate}' above full join",
                $"Predicate '{predicate}' moved close to {relation}",
                "Filtering tuples before joins reduces intermediate result size."));
        }

        steps.Add(new TransformationStep(
            step++,
            "Commutativity of joins",
            "Original SQL join order",
            "Filtered relations are joined earlier",
            "Inner joins can be reordered when predicates are preserved, producing an equivalent result."));

        steps.Add(new TransformationStep(
            step,
            "Projection pruning",
            "All relation attributes are carried through the tree",
            "Only output columns and join predicate columns are retained",
            "Reducing attributes lowers transfer and intermediate tuple width."));

        return steps;
    }

    private static AlgebraNode BuildJoinTree(ParsedAcademicQuery query)
    {
        var relationNodes = query.Aliases
            .Select(pair => AlgebraNode.RelationNode(pair.Value, pair.Key))
            .ToList();

        AlgebraNode current = relationNodes[0];
        for (var i = 1; i < relationNodes.Count; i++)
        {
            var joinCondition = query.Joins.ElementAtOrDefault(i - 1) ?? "join predicate";
            current = new AlgebraNode
            {
                Operator = "⋈",
                Label = "⋈ " + joinCondition,
                Detail = "Initial SQL join order",
                Children = [current, relationNodes[i]]
            };
        }

        return current;
    }

    private static AlgebraNode ApplyLocalSelections(string relation, string alias, List<string> predicates)
    {
        var baseNode = AlgebraNode.RelationNode(relation, alias);
        var localPredicates = predicates
            .Where(predicate => predicate.StartsWith(alias + ".", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (localPredicates.Count == 0)
        {
            return baseNode;
        }

        return new AlgebraNode
        {
            Operator = "σ",
            Label = "σ " + string.Join(" AND ", localPredicates),
            Detail = $"Selection pushed to {relation}",
            Relation = relation,
            Children = [baseNode]
        };
    }

    private static int OptimizationRank(string? relation)
    {
        return relation?.ToLowerInvariant() switch
        {
            "students" => 0,
            "enrollments" => 1,
            "courses" => 2,
            "professors" => 3,
            _ => 9
        };
    }

    private static string ResolvePredicateRelation(string predicate, ParsedAcademicQuery query)
    {
        var alias = predicate.Split('.', 2)[0].Trim();
        return query.Aliases.TryGetValue(alias, out var relation) ? relation : "the relevant relation";
    }
}
