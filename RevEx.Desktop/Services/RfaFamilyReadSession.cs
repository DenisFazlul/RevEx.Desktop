using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using RevEx.Domain.Contracts.Connectors;
using RevEx.Domain.Contracts.Content;
using RevEx.Domain.Contracts.ContentVersion;
using RevEx.Domain.Contracts.Revit;

namespace RevEx.Desktop.Services;

public sealed class RfaFamilyReadSession : IDisposable
{
    private readonly string _workingDirectory;

    internal RfaFamilyReadSession(string directory, string fileName, ConnectorDescriptor connector)
    {
        _workingDirectory = directory;
        FileName = fileName;
        FilePath = Path.Combine(directory, fileName);
        Connector = connector;
    }

    public string FileName
    {
        get;
    }
    public string FilePath
    {
        get;
    }
    public string ContentName => Path.GetFileNameWithoutExtension(FileName);
    public IReadOnlyList<SelectedLocalFile> AuxiliaryFiles
    {
        get;
        internal set;
    } = [];
    public ConnectorDescriptor Connector
    {
        get;
    }
    public RevitFamilyReadResult Family { get; internal set; } = new();
    public JObject Metadata { get; internal set; } = new();
    public ContentDto? SavedContent
    {
        get; internal set;
    }
    public ContentVersionDto? SavedVersion
    {
        get; internal set;
    }
    public bool IsFileUploaded
    {
        get; internal set;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_workingDirectory))
            {
                Directory.Delete(_workingDirectory, true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning("Не удалось удалить рабочую копию семейства: {0}", exception.Message);
        }
    }
}
