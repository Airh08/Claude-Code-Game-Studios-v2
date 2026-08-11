using System.Text.Json;
using System.Text.Json.Serialization;
using Ccgs.ProjectScanner;

if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("Usage: dotnet run -- <unity-project-root> [--pretty]");
    return;
}

var root = Path.GetFullPath(args[0]);
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"Project root does not exist: {root}");
    Environment.ExitCode = 2;
    return;
}

var report = UnityProjectScanner.Scan(root);
var options = new JsonSerializerOptions
{
    WriteIndented = args.Contains("--pretty", StringComparer.OrdinalIgnoreCase),
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
Console.WriteLine(JsonSerializer.Serialize(report, options));
