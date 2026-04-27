using System.Reflection;
using System.Text.Json;

var repoRoot = new DirectoryInfo(AppContext.BaseDirectory);
while (repoRoot is not null && !File.Exists(Path.Combine(repoRoot.FullName, "BTCPayServer.Plugins.OpenPatron.sln")))
{
    repoRoot = repoRoot.Parent;
}

if (repoRoot is null)
{
    throw new DirectoryNotFoundException("Could not locate the OpenPatron repository root.");
}

var plugin = Path.Combine(repoRoot.FullName, "BTCPayServer.Plugins.OpenPatron");
var assemblyConfigurationAttribute = typeof(Program).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
var buildConfigurationName = assemblyConfigurationAttribute?.Configuration;

var pluginDll = Directory
    .EnumerateFiles($"{Path.GetFullPath(plugin)}/bin/{buildConfigurationName}", $"{Path.GetFileName(plugin)}.dll", SearchOption.AllDirectories)
    .OrderByDescending(Path.GetDirectoryName)
    .First();

string path = $"{pluginDll};";

var content = JsonSerializer.Serialize(new
{
    DEBUG_PLUGINS = path
});

Console.WriteLine(content);
await File.WriteAllTextAsync(Path.Combine(repoRoot.FullName, "submodules", "btcpayserver", "BTCPayServer", "appsettings.dev.json"), content);
