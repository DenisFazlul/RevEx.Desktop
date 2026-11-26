using System;
using System.Collections.Generic;
using System.Linq;
using RevEx.Domain.Contracts.ContentSet;

namespace RevEx.Desktop.ViewModels.Contents;

internal static class ContentSetList
{
    public static ContentSetDto[] FilterAndSort(IEnumerable<ContentSetDto> sets, string search, string? currentAuthor)
    {
        var query = search.Trim();
        return sets.Where(item => item.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.Author.Contains(query, StringComparison.OrdinalIgnoreCase) || item.Id.ToString() == query)
            .OrderByDescending(item => !string.IsNullOrWhiteSpace(currentAuthor) &&
                string.Equals(item.Author, currentAuthor, StringComparison.Ordinal))
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Id)
            .ToArray();
    }
}
