using Avalonia.VisualTree;
using Avalonia.Controls;
using Avalonia.Threading;
using RevEx.Desktop;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Desktop.Views.Common;
using RevEx.Desktop.Views.Contents;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentSet;

internal static class ContentSetChecks
{
    public static async Task RunAsync()
    {
        var author = "other-user";
        var api = new MockRevExApiService(() => author);
        var first = await api.ContentSets.CreateAsync(new SaveContentSetDto("Двери", "Для проекта"));
        author = "current-user";
        var second = await api.ContentSets.CreateAsync(new SaveContentSetDto("Двери", "Для офиса"));
        author = "other-user";
        var foreign = await api.ContentSets.CreateAsync(new SaveContentSetDto("А чужой набор", "Чужое описание"));
        var content = (await api.Contents.GetAllAsync(new ContentQueryDto())).First();
        var selection = new ContentSetsViewModel(api.ContentSets, content.Id, () => "current-user");
        await selection.InitializeAsync();
        Require(selection.Sets.Select(item => item.Id).SequenceEqual(new[] { second.Id, foreign.Id, first.Id }), "The picker must put my sets before foreign sets.");
        selection.Search = "ОФИС";
        Require(selection.Sets.Count == 1 && selection.Sets[0].Id == second.Id, "Client search must match descriptions ignoring case.");
        selection.Search = "Двери";
        Require(selection.Sets.Count == 2, "Duplicate names must remain independently selectable.");
        selection.SelectedSet = second;
        await selection.AddCommand.ExecuteAsync(null);

        var opened = new List<int>();
        using var tab = new ContentSetsTabViewModel(api, item => opened.Add(item.Id), () => "current-user");
        await tab.ActivateAsync();
        Require(tab.Sets.Select(item => item.Id).SequenceEqual(new[] { second.Id, foreign.Id, first.Id }), "The tab must use the same ownership ordering as the picker.");
        tab.SelectedSet = second;
        Require(tab.Contents.Count == 1, "The selected set's content was not loaded.");
        tab.OpenContent(tab.Contents.Single());
        Require(opened.SequenceEqual(new[] { content.Id }), "Opening content from the tab failed.");
        Require(new ViewLocator().Build(tab) is ContentSetsTabView, "The workspace tab view is not registered.");

        var owner = new Window();
        owner.Show();
        var tabWindow = new Window { Content = new ContentSetsTabView { DataContext = tab } };
        tabWindow.Show();
        Dispatcher.UIThread.RunJobs();
        Require(tabWindow.GetVisualDescendants().OfType<ContentRowView>().Any(), "Set content must use the shared catalog row.");

        var editor = tab.CreateEditor(false);
        var editorWindow = new ContentSetEditorWindow(editor);
        var edited = editorWindow.ShowDialog<int?>(owner);
        editor.Name = "Изменённый набор";
        editor.Description = "Обновлённое описание";
        await editor.SaveCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        Require(edited.IsCompletedSuccessfully && await edited == second.Id, "Editing must return the saved set ID and close its window.");
        await tab.RefreshAsync(second.Id);
        Require(tab.SelectedSet?.Name == "Изменённый набор" && tab.SelectedSet.Author == second.Author,
            "Editing did not refresh the tab or changed the author.");

        var creation = tab.CreateEditor(true);
        var creationWindow = new ContentSetEditorWindow(creation);
        var created = creationWindow.ShowDialog<int?>(owner);
        await creation.SaveCommand.ExecuteAsync(null);
        Require(creation.HasError && !created.IsCompleted, "Required fields must keep the editor open.");
        creation.Name = "Новый набор";
        creation.Description = "Описание";
        await creation.SaveCommand.ExecuteAsync(null);
        Dispatcher.UIThread.RunJobs();
        Require(created.IsCompletedSuccessfully && (await created).HasValue, "Creating a set failed to close its editor.");

        await api.ContentSets.RemoveContentAsync(second.Id, content.Id);
        await tab.ActivateAsync();
        Require(tab.Contents.Count == 0, "Reactivating the tab must refresh membership even when set fields are unchanged.");
        await api.ContentSets.AddContentAsync(second.Id, content.Id);
        await tab.ActivateAsync();
        tab.SelectedContent = tab.Contents.Single();
        await tab.RemoveCommand.ExecuteAsync(null);
        Require(tab.Contents.Count == 0 && await api.Contents.GetByIdAsync(content.Id) is not null,
            "Removing membership must preserve content.");
        await tab.DeleteCommand.ExecuteAsync(null);
        Require(tab.SelectedSet is null && tab.Sets.All(item => item.Id != second.Id), "Deleting a set did not refresh the tab.");

        var picker = new ContentSetsViewModel(api.ContentSets, content.Id);
        var pickerWindow = new ContentSetsWindow(picker);
        var pickerDialog = pickerWindow.ShowDialog(owner);
        Dispatcher.UIThread.RunJobs();
        picker.CancelCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Require(pickerDialog.IsCompletedSuccessfully, "The selection window failed to close.");
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"preferred_username\":\"current-user\",\"email\":\"other@example.com\"}"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Require(RevEx.Desktop.Core.Authentication.AccessTokenAuthor.Read($"header.{payload}.signature") == "current-user",
            "Ownership must use the same preferred username as the server author.");
        Require(RevEx.Desktop.Core.Authentication.AccessTokenAuthor.Read("invalid") is null,
            "An unavailable identity must not mark other sets as mine.");
        tabWindow.Close();
        owner.Close();
        Console.WriteLine("PASS: content sets workspace tab, shared catalog rows, editor dialogs, selection, author preservation and refreshed membership.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
