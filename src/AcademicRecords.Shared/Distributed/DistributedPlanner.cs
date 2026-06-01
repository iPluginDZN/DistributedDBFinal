using AcademicRecords.Shared.Dataset;

namespace AcademicRecords.Shared.Distributed;

public static class DistributedPlanner
{
    public static DistributedPlan Build(IReadOnlyCollection<SiteHealth> health)
    {
        var healthBySite = health.ToDictionary(site => site.SiteId, StringComparer.OrdinalIgnoreCase);
        var plan = new DistributedPlan();

        Add(plan, healthBySite, "Students", "Students_CS", "site-a", "CS student fragment selected by s.department = 'CS'.");
        Add(plan, healthBySite, "Enrollments", "Enrollments_CS", "site-a", "Enrollments colocated with CS students.");
        AddWithReplica(plan, healthBySite, "Courses", "Courses_Replica", ["site-c", "site-a"], "Course reference data can be read from replicas.");
        AddWithReplica(plan, healthBySite, "Professors", "Professors_Replica", ["site-c", "site-b"], "Professor reference data can be read from replicas.");
        Add(plan, healthBySite, "Students", "Students_NonCS", "site-b", "Not needed by the default CS predicate, but shown for failure impact.");

        return plan;
    }

    private static void Add(
        DistributedPlan plan,
        IReadOnlyDictionary<string, SiteHealth> health,
        string relation,
        string fragment,
        string preferredSite,
        string note)
    {
        var online = health.TryGetValue(preferredSite, out var site) && site.Online;
        var status = online ? "Available" : "Unavailable";
        if (!online)
        {
            plan.Warnings.Add($"{fragment} is unavailable because {preferredSite} is offline.");
        }

        plan.Items.Add(new DistributedPlanItem(relation, fragment, preferredSite, online ? preferredSite : "None", status, note));
    }

    private static void AddWithReplica(
        DistributedPlan plan,
        IReadOnlyDictionary<string, SiteHealth> health,
        string relation,
        string fragment,
        string[] candidateSites,
        string note)
    {
        var selected = candidateSites.FirstOrDefault(site => health.TryGetValue(site, out var siteHealth) && siteHealth.Online);
        if (selected is null)
        {
            plan.Warnings.Add($"{fragment} is unavailable because all replicas are offline.");
        }

        plan.Items.Add(new DistributedPlanItem(
            relation,
            fragment,
            candidateSites[0],
            selected ?? "None",
            selected is null ? "Unavailable" : selected == candidateSites[0] ? "Available" : "Rerouted",
            note));
    }
}
