using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevEx.Desktop.ViewModels.Common;

namespace RevEx.Desktop.ViewModels.Contents;

public sealed record OtherFileMetadataParameterViewModel(string Name, string Value);

/// <summary>Подготавливает произвольные метаданные обычного файла для табличного представления.</summary>
public sealed class OtherFileMetadataViewModel : ViewModelBase
{
    private static readonly IReadOnlyDictionary<string, string> DisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FileName"] = "Имя файла",
            ["Extension"] = "Расширение",
            ["SizeBytes"] = "Размер",
            ["CreationTimeUtc"] = "Дата создания",
            ["LastWriteTimeUtc"] = "Дата изменения",
            ["Attributes"] = "Атрибуты"
        };

    public IReadOnlyList<OtherFileMetadataParameterViewModel> Parameters
    {
        get;
        private set;
    } = [];

    public bool HasParameters => Parameters.Count > 0;

    public void SetMetadata(JToken? metadata)
    {
        Parameters = metadata is JObject metadataObject
            ? metadataObject.Properties()
                .Select(CreateParameter)
                .ToArray()
            : [];

        OnPropertyChanged(nameof(Parameters));
        OnPropertyChanged(nameof(HasParameters));
    }

    private static OtherFileMetadataParameterViewModel CreateParameter(JProperty property)
    {
        var name = DisplayNames.GetValueOrDefault(property.Name, property.Name);
        var value = FormatValue(property.Name, property.Value);
        return new OtherFileMetadataParameterViewModel(name, value);
    }

    private static string FormatValue(string name, JToken value)
    {
        if (string.Equals(name, "SizeBytes", StringComparison.OrdinalIgnoreCase)
            && value.Type == JTokenType.Integer)
        {
            var sizeBytes = value.Value<long>();
            return $"{sizeBytes.ToString("N0", CultureInfo.CurrentCulture)} байт";
        }

        if ((string.Equals(name, "CreationTimeUtc", StringComparison.OrdinalIgnoreCase)
             || string.Equals(name, "LastWriteTimeUtc", StringComparison.OrdinalIgnoreCase))
            && DateTimeOffset.TryParse(
                value.Value<string>(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var date))
        {
            return date.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
        }

        return value.Type switch
        {
            JTokenType.Null => "—",
            JTokenType.String => value.Value<string>() ?? string.Empty,
            JTokenType.Boolean => value.Value<bool>() ? "Да" : "Нет",
            JTokenType.Object or JTokenType.Array => value.ToString(Formatting.None),
            _ => value is JValue { Value: IFormattable formattable }
                ? formattable.ToString(null, CultureInfo.CurrentCulture)
                : value.ToString(Formatting.None)
        };
    }
}
