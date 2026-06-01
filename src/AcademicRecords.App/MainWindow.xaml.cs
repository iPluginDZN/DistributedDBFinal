using System.Net.Http;
using System.Net.Http.Json;
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
            StatusText.Text = "Analysis complete. JSON was rendered as visual trees, rule trace, plan, and health.";
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
        WarningsList.ItemsSource = analysis.DistributedPlan.Warnings;
        SiteHealthList.ItemsSource = analysis.SiteHealth;
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
