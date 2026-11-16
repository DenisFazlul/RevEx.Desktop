using System.Text.Json;
using RevEx.Desktop.Core.Services;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Entities;
using RevEx.Domain.Identifiers;

internal static class FileVersionImportChecks
{
    public static void Run()
    {
        CheckAsync().GetAwaiter().GetResult();
        Console.WriteLine("PASS: generic file version creation, primary upload and file metadata.");
    }

    private static async Task CheckAsync()
    {
        var api = new MockRevExApiService();
        var category = (await api.Categories.GetAllAsync()).First();
        var content = await api.Contents.CreateAsync(new CreateContentDto
        {
            Cid = Uuid7.Create(),
            Name = "Generic file check",
            Description = "Generic file check",
            CategoryId = category.Id
        });
        var path = Path.Combine(Path.GetTempPath(), $"RevEx-file-version-{Guid.NewGuid():N}.pdf");
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        await File.WriteAllBytesAsync(path, bytes);

        try
        {
            var model = new FileVersionImportViewModel(api, content.Id);
            ContentVersionDto? result = null;
            model.CloseRequested += value =>
            {
                result = value as ContentVersionDto;
            };
            model.LoadFile(path);

            Require(model.CanSave, "Selecting a valid file did not enable saving.");
            Require(model.FileName == Path.GetFileName(path), "The file name was not read.");
            Require(model.Extension == ".pdf", "The extension was not normalized.");
            await model.SaveAndCloseCommand.ExecuteAsync(null);

            if (result is null)
            {
                throw new InvalidOperationException("The dialog did not return the created version.");
            }

            Require(result.Platform == Platform.Other, "The generic version has the wrong platform.");
            Require(result.Name == Path.GetFileName(path), "The version name did not come from the file.");
            Require(result.Application == "file" && result.ApplicationVersion == "pdf",
                "The generic version application fields did not come from the file type.");

            var file = (await api.ContentFiles.GetAllAsync()).Single(item =>
                item.ContentVersionId == result.Id);
            Require(file.Role == "primary", "The selected file was not uploaded as the primary file.");
            Require(file.SizeBytes == bytes.Length, "The uploaded size changed.");
            if (file.Metadata is not JsonElement metadata)
            {
                throw new InvalidOperationException("File metadata was not saved as JSON.");
            }

            Require(metadata.GetProperty("FileName").GetString() == Path.GetFileName(path),
                "The metadata file name is incorrect.");
            Require(metadata.GetProperty("Extension").GetString() == ".pdf",
                "The metadata extension is incorrect.");
            Require(metadata.GetProperty("SizeBytes").GetInt64() == bytes.Length,
                "The metadata size is incorrect.");
            Require(metadata.TryGetProperty("CreationTimeUtc", out _)
                && metadata.TryGetProperty("LastWriteTimeUtc", out _),
                "The metadata dates are missing.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
