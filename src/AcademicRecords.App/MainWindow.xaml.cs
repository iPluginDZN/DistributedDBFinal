using System.Net.Http;
using System.Net.Http.Json;
using System.Data;
using System.Windows;
using AcademicRecords.Shared.Algebra;
using AcademicRecords.Shared.Dataset;
using AcademicRecords.Shared.Distributed;
using AcademicRecords.Shared.Query;

namespace AcademicRecords.App;

public partial class MainWindow : Window
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri("http://localhost:5000") };

    public MainWindow()
    {
        InitializeComponent();
        SqlEditor.Text = AcademicDataset.DefaultSql;
    }

    private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
    {
        AnalyzeButton.IsEnabled = false;
        StatusText.Text = "Analyzing query...";

        try
        {
            var response = await _http.PostAsJsonAsync("/api/query/analyze", new AnalyzeQueryRequest(SqlEditor.Text));
            if (!response.IsSuccessStatusCode)
            {
                StatusText.Text = await response.Content.ReadAsStringAsync();
                return;
            }

            var analysis = await response.Content.ReadFromJsonAsync<AnalyzeQueryResponse>();
            if (analysis is null)
            {
                StatusText.Text = "Coordinator returned an empty response.";
                return;
            }

            RenderAnalysis(analysis);
            StatusText.Text = $"Analysis complete. Joined result rows: {analysis.JoinedResult.Rows.Count}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Coordinator unavailable: {ex.Message}";
        }
        finally
        {
            AnalyzeButton.IsEnabled = true;
        }
    }

    private async void RefreshHealthButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var health = await _http.GetFromJsonAsync<List<SiteHealth>>("/api/sites/health") ?? [];
            SiteHealthList.ItemsSource = health;
            StatusText.Text = "Site health refreshed.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not refresh site health: {ex.Message}";
        }
    }

    private void RenderAnalysis(AnalyzeQueryResponse analysis)
    {
        InitialTreeView.ItemsSource = new[] { analysis.InitialTree };
        OptimizedTreeView.ItemsSource = new[] { analysis.OptimizedTree };
        ExpandTree(InitialTreeView);
        ExpandTree(OptimizedTreeView);

        TransformationsGrid.ItemsSource = analysis.Transformations;
        DistributedPlanGrid.ItemsSource = analysis.DistributedPlan.Items;
        DataPreviewGrid.ItemsSource = analysis.DataPreview;
        RenderJoinedResult(analysis.JoinedResult);
        WarningsList.ItemsSource = analysis.DistributedPlan.Warnings;
        SiteHealthList.ItemsSource = analysis.SiteHealth;
    }

    private void RenderJoinedResult(JoinedResultTable joinedResult)
    {
        var table = new DataTable();

        if (joinedResult.Columns.Count == 0)
        {
            JoinedResultEmptyText.Text = "The coordinator response does not include joined-result columns. Rebuild/restart the backend services so the WPF app talks to the latest API.";
            JoinedResultEmptyText.Visibility = Visibility.Visible;
            JoinedResultGrid.Visibility = Visibility.Collapsed;
            JoinedResultGrid.ItemsSource = null;
            return;
        }

        var displayColumns = BuildDisplayColumnMap(joinedResult.Columns);

        foreach (var column in joinedResult.Columns)
        {
            table.Columns.Add(displayColumns[column]);
        }

        foreach (var sourceRow in joinedResult.Rows)
        {
            var row = table.NewRow();
            foreach (var column in joinedResult.Columns)
            {
                row[displayColumns[column]] = sourceRow.TryGetValue(column, out var value) ? value : "";
            }

            table.Rows.Add(row);
        }

        JoinedResultEmptyText.Text = "No joined records match this query, or a required site is offline.";
        JoinedResultEmptyText.Visibility = table.Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        JoinedResultGrid.Visibility = table.Rows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        JoinedResultGrid.ItemsSource = table.DefaultView;
    }

    private static Dictionary<string, string> BuildDisplayColumnMap(IReadOnlyList<string> sourceColumns)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var map = new Dictionary<string, string>();

        foreach (var sourceColumn in sourceColumns)
        {
            var display = ToDisplayColumnName(sourceColumn);
            var unique = display;
            var suffix = 2;

            while (!used.Add(unique))
            {
                unique = $"{display}_{suffix++}";
            }

            map[sourceColumn] = unique;
        }

        return map;
    }

    private static string ToDisplayColumnName(string sourceColumn)
    {
        return sourceColumn.Trim().ToLowerInvariant() switch
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
            "source sites" => "Source Sites",
            var value => value.Replace(".", "_")
        };
    }

    private static void ExpandTree(System.Windows.Controls.TreeView treeView)
    {
        treeView.UpdateLayout();
        foreach (var item in treeView.Items)
        {
            if (treeView.ItemContainerGenerator.ContainerFromItem(item) is System.Windows.Controls.TreeViewItem treeViewItem)
            {
                ExpandTreeItem(treeViewItem);
            }
        }
    }

    private static void ExpandTreeItem(System.Windows.Controls.TreeViewItem item)
    {
        item.IsExpanded = true;
        item.UpdateLayout();

        foreach (var child in item.Items)
        {
            if (item.ItemContainerGenerator.ContainerFromItem(child) is System.Windows.Controls.TreeViewItem childItem)
            {
                ExpandTreeItem(childItem);
            }
        }
    }
}
