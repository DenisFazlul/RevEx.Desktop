using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RevEx.Desktop.ViewModels.Revit;
using RevEx.Desktop.Views.Revit;
using RevEx.Domain.Contracts.Revit;

internal static class RevitFamilyMetadataChecks
{
    public static void Run()
    {
        var first = new RevitFamilyReadResultType
        {
            Name = "600 x 900",
            Parameters =
            [
                new() { Name = "Width", Value = "600", StorageType = "Double", GroupId = "PG_GEOMETRY", GroupName = "Размеры" },
                new() { Name = "Height", Value = "900", StorageType = "Double", GroupId = "PG_GEOMETRY", GroupName = "Dimensions" },
                new() { Name = "Mark", Value = "ОК-01", StorageType = "String", GroupId = "PG_IDENTITY_DATA", GroupName = "Данные идентификации" },
                new() { Name = "Other size", Value = "1", StorageType = "Integer", GroupId = "PG_OTHER", GroupName = "Размеры" },
                new() { Name = "Legacy", Value = "Без группы", StorageType = "String" }
            ]
        };
        var second = new RevitFamilyReadResultType
        {
            Name = "900 x 1200",
            Parameters = [new() { Name = "Width", Value = "900", StorageType = "Double", GroupId = "PG_GEOMETRY", GroupName = "Размеры" }]
        };
        var empty = new RevitFamilyReadResultType { Name = "Без параметров" };
        var model = new RevitFamilyReadResultViewModel
        {
            Family = new RevitFamilyReadResult { Name = "Окно", RevitCategory = "Окна", Types = [first, second, empty] }
        };
        var view = new RevitFamilyReadResultView { DataContext = model };
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            var types = view.FindControl<ComboBox>("TypesBox")!;
            var groups = view.FindControl<ItemsControl>("ParameterGroupsList")!;
            var emptyText = view.FindControl<TextBlock>("EmptyParametersText")!;
            var category = view.GetVisualDescendants().OfType<HeaderedContentControl>()
                .Single(control => control.Header as string == "Категория Revit");
            Require(category.Content as string == "Окна", "The view is not bound to RevitCategory.");
            Require(types.ItemCount == 3 && ReferenceEquals(types.SelectedItem, first), "The first family type was not selected.");
            Require(groups.ItemCount == 4 && ReferenceEquals(groups.ItemsSource, model.ParameterGroups), "Initial parameter groups were not displayed.");
            Require(model.ParameterGroups[0].GroupId == "PG_GEOMETRY" && model.ParameterGroups[0].Name == "Размеры"
                && model.ParameterGroups[0].Parameters.SequenceEqual(first.Parameters.Take(2)), "Parameters must be grouped by ID, independently of localized names.");
            Require(model.ParameterGroups[2].Name == "Размеры" && model.ParameterGroups[2].Parameters.Count == 1, "Different group IDs with the same label were merged.");
            Require(model.ParameterGroups[3].Name == "Прочее" && model.ParameterGroups[3].Parameters.Single().Name == "Legacy", "Legacy parameters without group metadata were lost.");

            var expanders = groups.GetVisualDescendants().OfType<Expander>().ToArray();
            Require(expanders.Length == 4 && expanders.All(expander => !expander.IsExpanded), "Parameter groups must initially be collapsed.");
            var dimensions = expanders[0];
            Require(dimensions.Header as string == "Размеры", "The group label was not displayed.");
            dimensions.IsExpanded = true;
            Dispatcher.UIThread.RunJobs();
            Require(model.ParameterGroups[0].IsExpanded, "Expanding the group did not update its state.");
            var parameters = dimensions.GetVisualDescendants().OfType<ItemsControl>()
                .Single(control => ReferenceEquals(control.ItemsSource, model.ParameterGroups[0].Parameters));
            Require(parameters.ItemCount == 2, "Expanding a group did not show its parameters.");
            dimensions.IsExpanded = false;
            Dispatcher.UIThread.RunJobs();
            Require(!model.ParameterGroups[0].IsExpanded, "The group did not collapse.");

            types.SelectedItem = second;
            Dispatcher.UIThread.RunJobs();
            Require(ReferenceEquals(model.SelectedType, second), "ComboBox selection did not update the model.");
            Require(groups.ItemCount == 1 && model.ParameterGroups.Single().Parameters.Single().Value == "900", "Selecting a type did not replace its groups and parameters.");

            types.SelectedItem = empty;
            Dispatcher.UIThread.RunJobs();
            Require(groups.ItemCount == 0 && emptyText.IsVisible, "An empty type retained the previous parameters.");

            var replacement = new RevitFamilyReadResultType { Name = "Другой тип" };
            model.Family = new RevitFamilyReadResult { Types = [replacement] };
            Dispatcher.UIThread.RunJobs();
            Require(types.ItemCount == 1 && ReferenceEquals(types.SelectedItem, replacement), "Replacing the family retained a stale type.");

            model.Family = new RevitFamilyReadResult();
            Dispatcher.UIThread.RunJobs();
            Require(types.SelectedItem is null && !types.IsEnabled && groups.ItemCount == 0, "A family without types retained a selection.");

            model.Family = null;
            Dispatcher.UIThread.RunJobs();
            Require(!model.HasFamily && model.SelectedType is null && groups.ItemCount == 0, "Clearing metadata retained family data.");
        }
        finally
        {
            window.Close();
        }

        Console.WriteLine("PASS: family metadata view, type selection, groups by ID, expand/collapse, legacy metadata, family replacement and empty states.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
