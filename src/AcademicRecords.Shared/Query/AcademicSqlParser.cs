using System.Text.RegularExpressions;

namespace AcademicRecords.Shared.Query;

public static partial class AcademicSqlParser
{
    public static ParsedAcademicQuery Parse(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL is required.", nameof(sql));
        }

        var normalized = Regex.Replace(sql.Trim().TrimEnd(';'), @"\s+", " ");
        var query = new ParsedAcademicQuery { Sql = sql };

        var selectMatch = Regex.Match(normalized, @"select\s+(?<select>.*?)\s+from\s+", RegexOptions.IgnoreCase);
        if (!selectMatch.Success)
        {
            throw new InvalidOperationException("Only SELECT-FROM-JOIN-WHERE queries are supported.");
        }

        query.Projections = selectMatch.Groups["select"].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var fromMatch = Regex.Match(normalized, @"from\s+(?<table>\w+)\s+(?<alias>\w+)", RegexOptions.IgnoreCase);
        if (!fromMatch.Success)
        {
            throw new InvalidOperationException("The FROM clause must include a table alias, for example: FROM Students s.");
        }

        query.Aliases[fromMatch.Groups["alias"].Value] = fromMatch.Groups["table"].Value;

        foreach (Match join in Regex.Matches(normalized, @"join\s+(?<table>\w+)\s+(?<alias>\w+)\s+on\s+(?<condition>.*?)(?=\s+join\s+|\s+where\s+|$)", RegexOptions.IgnoreCase))
        {
            query.Aliases[join.Groups["alias"].Value] = join.Groups["table"].Value;
            query.Joins.Add(join.Groups["condition"].Value.Trim());
        }

        var whereMatch = Regex.Match(normalized, @"\swhere\s+(?<where>.*)$", RegexOptions.IgnoreCase);
        if (whereMatch.Success)
        {
            query.Predicates = Regex.Split(whereMatch.Groups["where"].Value, @"\s+and\s+", RegexOptions.IgnoreCase)
                .Select(predicate => predicate.Trim())
                .Where(predicate => predicate.Length > 0)
                .ToList();
        }

        if (query.Aliases.Count < 4)
        {
            throw new InvalidOperationException("This visualizer expects the four academic tables with aliases.");
        }

        return query;
    }
}
