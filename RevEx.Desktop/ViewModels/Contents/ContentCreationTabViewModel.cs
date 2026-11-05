using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using RevEx.Desktop.Core.Interfaces;
using RevEx.Desktop.Services;
using RevEx.Desktop.ViewModels.Tabs;

namespace RevEx.Desktop.ViewModels.Contents;

/// <summary>Модель вкладки добавления контента. Предоставляет запуск импорта RFA и ручного редактора.</summary>
public sealed class ContentCreationTabViewModel : WorkspaceTabViewModel
{
    private readonly IRevExApiService _apiService;
    private readonly RfaFamilyImportService _rfaImportService;

    public ICommand AddContentManuallyCommand
    {
        get;
    }

    public ContentCreationTabViewModel(
        IRevExApiService apiService,
        RfaFamilyImportService rfaImportService,
        Action addContentManually)
        : base("Добавление контента", true)
    {
        _apiService = apiService;
        _rfaImportService = rfaImportService;
        AddContentManuallyCommand = new RelayCommand(addContentManually);
    }

    public RfaFamilyImportViewModel CreateRfaFamilyImport()
    {
        return new RfaFamilyImportViewModel(_apiService, _rfaImportService);
    }
}
