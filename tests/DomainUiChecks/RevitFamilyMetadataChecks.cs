using Avalonia.Controls;
using Avalonia.Threading;
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
            Parameters = [new() { Name = "Width", Value = "600", StorageType = "Double" }]
        };
        var second = new RevitFamilyReadResultType
        {
            Name = "900 x 1200",
            Parameters = [new() { Name = "Width", Value = "900", StorageType = "Double" }]
        };
        var empty = new RevitFamilyReadResultType { Name = "Без параметров" };
        var model = new RevitFamilyReadResultViewModel
        {
            Family = new RevitFamilyReadResult { Name = "Окно", Types = [first, second, empty] }
        };
        var view = new RevitFamilyReadResultView { DataContext = model };
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            var types = view.FindControl<ComboBox>("TypesBox")!;
            var parameters = view.FindControl<ItemsControl>("ParametersList")!;
            var emptyText = view.FindControl<TextBlock>("EmptyParametersText")!;
            Require(types.ItemCount == 3 && ReferenceEquals(types.SelectedItem, first), "The first family type was not selected.");
            Require(ReferenceEquals(parameters.ItemsSource, first.Parameters), "Initial parameters were not displayed.");

            types.SelectedItem = second;
            Dispatcher.UIThread.RunJobs();
            Require(ReferenceEquals(model.SelectedType, second), "ComboBox selection did not update the model.");
            Require(ReferenceEquals(parameters.ItemsSource, second.Parameters), "Selecting a type did not replace its parameters.");

            types.SelectedItem = empty;
            Dispatcher.UIThread.RunJobs();
            Require(parameters.ItemCount == 0 && emptyText.IsVisible, "An empty type retained the previous parameters.");

            var replacement = new RevitFamilyReadResultType { Name = "Другой тип" };
            model.Family = new RevitFamilyReadResult { Types = [replacement] };
            Dispatcher.UIThread.RunJobs();
            Require(types.ItemCount == 1 && ReferenceEquals(types.SelectedItem, replacement), "Replacing the family retained a stale type.");

            model.Family = new RevitFamilyReadResult();
            Dispatcher.UIThread.RunJobs();
            Require(types.SelectedItem is null && !types.IsEnabled && parameters.ItemCount == 0, "A family without types retained a selection.");

            model.Family = null;
            Dispatcher.UIThread.RunJobs();
            Require(!model.HasFamily && model.SelectedType is null && parameters.ItemCount == 0, "Clearing metadata retained family data.");
        }
        finally
        {
            window.Close();
        }

        Console.WriteLine("PASS: reusable family metadata view, type selection, parameter bindings, family replacement and empty states.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
