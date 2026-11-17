using RevEx.Desktop.ViewModels.Common;
using RevEx.Domain.Contracts.Tag;

namespace RevEx.Desktop.ViewModels.Tags;

/// <summary>Модель тега в фильтре каталога. Хранит название и признак включения в поиск.</summary>
public sealed class TagFilterItemViewModel(TagDto dto) : FilterItemViewModel(dto.Id, dto.Name);
