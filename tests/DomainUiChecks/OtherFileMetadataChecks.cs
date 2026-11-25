using Newtonsoft.Json.Linq;
using RevEx.Desktop.ViewModels.Contents;

internal static class OtherFileMetadataChecks
{
    public static void Run()
    {
        var model = new OtherFileMetadataViewModel();
        model.SetMetadata(JObject.Parse(
            """
            {
              "FileName": "manual.pdf",
              "Extension": ".pdf",
              "SizeBytes": 12345,
              "CreationTimeUtc": "2026-11-16T10:00:00Z",
              "Custom": true
            }
            """));

        Require(model.HasParameters, "Other file metadata was not exposed to the table.");
        Require(model.Parameters.Count == 5, "The metadata table lost parameters.");
        Require(model.Parameters[0].Name == "Имя файла" && model.Parameters[0].Value == "manual.pdf",
            "The file name row is incorrect.");
        Require(model.Parameters.Single(item => item.Name == "Размер").Value.Contains("12"),
            "The size row is not formatted.");
        Require(model.Parameters.Single(item => item.Name == "Custom").Value == "Да",
            "Unknown boolean metadata was not preserved.");

        model.SetMetadata(null);
        Require(!model.HasParameters && model.Parameters.Count == 0,
            "Clearing metadata retained stale table rows.");
        Console.WriteLine("PASS: Other file metadata table, labels, values and empty state.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
