#if NETSTANDARD2_0
// Allows record and init properties when targeting .NET Standard 2.0.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
