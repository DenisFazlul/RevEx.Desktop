using Avalonia.Controls;
using Avalonia.Threading;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Contents;
using RevEx.Domain.Contracts.Content;

internal static class OpenContentByIdChecks
{
    public static async Task RunAsync()
    {
        var repository = new MockRevExApiService().Contents;
        var contents = (await repository.GetAllAsync(new ContentQueryDto())).Take(2).ToArray();
        Require(contents.Length == 2, "The fixture needs two contents.");
        var opened = new List<int>();
        var model = new OpenContentByIdViewModel(repository, content => opened.Add(content.Id));
        foreach (var text in new[] { "", "0", "-1", "abc", "2147483648", $"{contents[0].Id},bad" })
        {
            model.Identifiers = text;
            await model.OpenCommand.ExecuteAsync(null);
            Require(model.HasError && opened.Count == 0 && !model.IsBusy,
                "Invalid input opened content or left the dialog busy.");
        }

        model.Identifiers = $"{contents[0].Id}, {int.MaxValue};\n{contents[1].Id}\t{contents[0].Id}";
        await model.OpenCommand.ExecuteAsync(null);
        Require(opened.SequenceEqual(contents.Select(content => content.Id)) && model.HasError && !model.IsBusy,
            "A missing content blocked valid IDs, changed ordering, or duplicates were opened.");

        opened.Clear();
        var owner = new Window();
        owner.Show();
        var window = new OpenContentByIdWindow(model);
        var result = window.ShowDialog(owner);
        model.Identifiers = $"{contents[0].Id}\r\n{contents[1].Id}";
        await model.OpenCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        Require(result.IsCompletedSuccessfully && !window.IsVisible && !model.HasError && !model.IsBusy,
            "Successful opening failed to close the dialog or retained the previous error.");
        Require(opened.SequenceEqual(contents.Select(content => content.Id)), "Valid IDs were not opened.");
        owner.Close();
        Console.WriteLine("PASS: open by ID validation, separators, duplicate removal, partial success, retry and dialog closing.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
