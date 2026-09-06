using System;

namespace RevEx.Desktop.Navigation;

public sealed record WorkspaceTabRequest(Type TabType, object? Key = null, object? Parameter = null);
