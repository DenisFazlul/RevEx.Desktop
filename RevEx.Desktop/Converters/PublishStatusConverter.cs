using System;
using System.Globalization;
using Avalonia.Data.Converters;
using RevEx.Desktop.ViewModels.Contents;
using RevEx.Domain.Entities;

namespace RevEx.Desktop.Converters;

public sealed class PublishStatusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is PublishStatus status ? PublishStatusOption.GetName(status) : string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
