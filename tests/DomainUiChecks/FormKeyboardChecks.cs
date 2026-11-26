using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using RevEx.Desktop.ViewModels.Common;
using RevEx.Desktop.Views.Common;
using RevEx.Desktop.Views.Contents;

internal static class FormKeyboardChecks
{
    public static void Run()
    {
        var owner = new Window();
        owner.Show();
        var editor = new TextInputWindow(new TextInputOptions("Test", "Test", "Name", "", ""));
        var result = editor.ShowDialog<string?>(owner);
        Dispatcher.UIThread.RunJobs();
        var model = (TextInputViewModel)editor.DataContext!;
        PressEnter(editor);
        Require(!result.IsCompleted && !string.IsNullOrWhiteSpace(model.ValidationMessage),
            "Enter must validate and keep an invalid form open.");

        model.Text = "Accepted";
        Dispatcher.UIThread.RunJobs();
        PressEnter(editor);
        Require(result.IsCompletedSuccessfully && result.Result == "Accepted",
            "Enter must accept valid text and close the form.");

        var multiline = new TextInputWindow(new TextInputOptions("Test", "Test", "Description", "", "", IsMultiline: true));
        var multilineResult = multiline.ShowDialog<string?>(owner);
        Dispatcher.UIThread.RunJobs();
        var box = multiline.FindControl<TextBox>("ValueBox")!;
        box.Text = "First line";
        box.CaretIndex = box.Text.Length;
        box.Focus();
        PressEnter(multiline);
        Require(!multilineResult.IsCompleted && box.Text?.Contains('\n') == true,
            "Enter in a multiline field must insert a newline instead of submitting.");
        multiline.Close();

        var details = new ContentDetailsView();
        var info = details.FindControl<Grid>("ContentInformationGrid")!;
        var fields = info.Children.OfType<TextBlock>().ToArray();
        Require(fields.All(item => item.Text != "Название"), "The content name must only appear in the header.");
        Require(fields.GroupBy(item => (Grid.GetRow(item), Grid.GetColumn(item))).All(group => group.Count() == 1),
            "Content information fields overlap in the same grid cell.");
        Require(details.FindControl<TextBlock>("ContentTitle") is not null, "The content title was removed from the action header.");
        owner.Close();
        Console.WriteLine("PASS: Enter validates and accepts forms, multiline Enter remains available, content fields do not overlap.");
    }

    private static void PressEnter(Window window)
    {
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        if (window.IsVisible)
        {
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        }
        Dispatcher.UIThread.RunJobs();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
