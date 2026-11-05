using Avalonia.Controls;
using Avalonia.Threading;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.ViewModels.Catalog;
using RevEx.Desktop.ViewModels.Categories;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Desktop.ViewModels.Tags;
using RevEx.Desktop.Views.Categories;
using RevEx.Desktop.Views.Common;
using RevEx.Desktop.Views.Tags;
using RevEx.Domain.Contracts.Category;
using RevEx.Domain.Contracts.Tag;

internal static class DialogChecks
{
    public static void Run()
    {
        CheckTextValidation();
        CheckDialogResultsAndLifetime();
        CheckFilterDraft();
        Task.Run(CheckOperationsAsync).GetAwaiter().GetResult();
        Task.Run(CheckTagManagementAsync).GetAwaiter().GetResult();
        Console.WriteLine("PASS: reusable dialog validation, typed results, cancellation, busy closing, disposal, filter drafts and tag operations.");
    }

    private static void CheckTextValidation()
    {
        var model = new TextInputViewModel(new TextInputOptions("Test", "Test", "Name", "", "", MaximumLength: 5));
        object? result = null;
        model.CloseRequested += value => result = value;
        model.Text = "   ";
        model.SaveCommand.Execute(null);
        Require(result is null && !string.IsNullOrWhiteSpace(model.ValidationMessage), "Blank input was accepted.");
        model.Text = "123456";
        model.SaveCommand.Execute(null);
        Require(result is null && !string.IsNullOrWhiteSpace(model.ValidationMessage), "Text length was not validated by the model.");
        model.Text = "  abc  ";
        model.SaveCommand.Execute(null);
        Require(result is "abc" && model.ValidationMessage is null, "Valid input was not trimmed and returned.");

        var tag = new TagEditorViewModel([], null, null) { Text = "Tag" };
        tag.SaveCommand.Execute(null);
        Require(tag.ValidationMessage == "Выберите группу.", "A tag without a group was accepted.");
        var deletion = new DeleteConfirmationViewModel("Cannot delete", false);
        var deleted = false;
        deletion.CloseRequested += value => deleted = value is true;
        deletion.DeleteCommand.Execute(null);
        Require(!deleted, "Disabled deletion was accepted.");
    }

    private static void CheckDialogResultsAndLifetime()
    {
        var owner = new Window();
        owner.Show();
        var editor = new TagGroupEditorWindow("Before");
        var result = editor.ShowDialog<TagGroupEditorResult?>(owner);
        var model = (TagGroupEditorViewModel)editor.DataContext!;
        model.Text = "After";
        model.SaveCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Require(result.IsCompletedSuccessfully && result.Result?.Name == "After", "A dialog did not return its model result.");

        var now = DateTimeOffset.UtcNow;
        var selector = new CategorySelectionWindow([new CategoryDto(7, "Category", now, now)], null);
        var selected = selector.ShowDialog<CategoryDto?>(owner);
        ((CategorySelectionViewModel)selector.DataContext!).AcceptCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Require(selected.IsCompletedSuccessfully && selected.Result?.Id == 7, "Selection command lost the selected item.");

        var probe = new DisposableDialogModel { IsLoading = true };
        var busy = new DialogWindow { DataContext = probe };
        var cancelled = busy.ShowDialog<bool>(owner);
        busy.Close();
        Require(busy.IsVisible && !probe.Disposed, "An active operation lost its window or resources.");
        probe.IsLoading = false;
        probe.CancelCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Require(cancelled.IsCompletedSuccessfully && !cancelled.Result && probe.Disposed, "Cancellation did not close and dispose the dialog.");
        owner.Close();
    }

    private static void CheckFilterDraft()
    {
        using var catalog = new CatalogTabViewModel(new MockRevExApiService(), _ => { });
        var now = DateTimeOffset.UtcNow;
        catalog.Categories.Add(new CategoryItemViewModel(new CategoryDto(1, "Category", now, now)));
        catalog.Tags.Add(new TagFilterItemViewModel(new TagDto(1, "Tag", 1, "Group", now, now)));
        var cancelled = new CatalogSettingsViewModel(catalog);
        cancelled.Categories[0].IsSelected = true;
        cancelled.Tags[0].IsSelected = true;
        cancelled.CancelCommand.Execute(null);
        Require(!catalog.Categories[0].IsSelected && !catalog.Tags[0].IsSelected, "Cancelled filter edits changed the catalog.");
        var accepted = new CatalogSettingsViewModel(catalog);
        accepted.Categories[0].IsSelected = true;
        accepted.Tags[0].IsSelected = true;
        accepted.ApplyCommand.Execute(null);
        Require(catalog.Categories[0].IsSelected && catalog.Tags[0].IsSelected, "Confirmed filters were not applied.");
    }

    private static async Task CheckOperationsAsync()
    {
        var model = new OperationProbe();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = model.SaveAsync(() => completion.Task);
        Require(model.IsSaving && model.IsBusy && !model.HasError, "Busy state was not published.");
        completion.SetException(new IOException("Storage unavailable"));
        Require(!await operation && model.HasError && !model.IsBusy, "Operation error did not restore the state.");
        Require(await model.SaveAsync(() => Task.CompletedTask) && !model.HasError, "Retry did not clear the previous error.");
    }

    private static async Task CheckTagManagementAsync()
    {
        var model = new TagAdminTabViewModel(new MockRevExApiService());
        await model.ActivateAsync();
        var suffix = Guid.NewGuid().ToString("N");
        Require(await model.AddGroupFromDialogAsync("First-" + suffix), "Group creation failed.");
        Require(await model.AddGroupFromDialogAsync("Second-" + suffix), "Group creation failed.");
        var first = model.Groups.Single(group => group.Name == "First-" + suffix);
        var second = model.Groups.Single(group => group.Name == "Second-" + suffix);
        Require(await model.AddTagFromDialogAsync("Tag", first) && first.TagCount == 1, "Group counter did not follow tag creation.");
        Require(!await model.DeleteGroupFromDialogAsync(first), "A nonempty group was deleted.");
        var tag = first.Tags.Single();
        Require(await model.EditTagFromDialogAsync(tag, "Renamed", second), "Tag move failed.");
        Require(first.TagCount == 0 && second.TagCount == 1 && tag.Name == "Renamed", "Group counters did not follow tag movement.");
        Require(await model.DeleteTagFromDialogAsync(tag) && second.TagCount == 0, "Tag deletion failed.");
        Require(await model.DeleteGroupFromDialogAsync(first) && await model.DeleteGroupFromDialogAsync(second), "Empty groups could not be deleted.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class OperationProbe : AsyncViewModelBase
    {
        public Task<bool> SaveAsync(Func<Task> action)
        {
            return RunSaveAsync(action, "Save failed");
        }
    }

    private sealed class DisposableDialogModel : DialogViewModel<bool>, IDisposable
    {
        public DisposableDialogModel() : base(false)
        {
        }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
