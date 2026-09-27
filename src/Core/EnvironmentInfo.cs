using System.Runtime.InteropServices;

namespace Core;

public sealed record EnvironmentReport(
    string OsDescription,
    string OsVersion,
    string FrameworkDescription,
    string ClrVersion,
    string ProcessArchitecture,
    string DetectedRid,
    string ReportedRid,
    string BaseDirectory,
    string CurrentDirectory,
    string BuildNote);

public static class EnvironmentInfo
{
#if NET9_0_OR_GREATER
    private const string BuildNote = "збірка під net9.0";
#else
    private const string BuildNote = "збірка під net8.0";
#endif

    public static EnvironmentReport Collect() => new(
        RuntimeInformation.OSDescription,
        Environment.OSVersion.ToString(),
        RuntimeInformation.FrameworkDescription,
        Environment.Version.ToString(),
        RuntimeInformation.ProcessArchitecture.ToString(),
        DetectRid(),
        RuntimeInformation.RuntimeIdentifier,
        AppContext.BaseDirectory,
        Environment.CurrentDirectory,
        BuildNote);

    // Ручне визначення RID: показує, з чого складається рядок win-x64.
    private static string DetectRid()
    {
        string os =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "unknown";

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "unknown"
        };

        return $"{os}-{arch}";
    }
}
