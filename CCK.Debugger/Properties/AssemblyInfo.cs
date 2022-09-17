using System.Reflection;
using CCK.Debugger.Properties;

[assembly: AssemblyVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyFileVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyInformationalVersion(AssemblyInfoParams.Version)]
[assembly: AssemblyTitle(AssemblyInfoParams.Name)]
[assembly: AssemblyCompany(AssemblyInfoParams.Author)]
[assembly: AssemblyProduct(AssemblyInfoParams.Name)]

namespace CCK.Debugger.Properties;
internal static class AssemblyInfoParams {
    public const string Version = "1.0.3";
    public const string Author = "kafeijao";
    public const string Name = "CCK.Debugger";
}

