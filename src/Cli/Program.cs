using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core;

Console.OutputEncoding = System.Text.Encoding.UTF8;

EnvironmentReport report = EnvironmentInfo.Collect();

if (args.Contains("--json"))
{
    var context = new ReportJsonContext(new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
    Console.WriteLine(JsonSerializer.Serialize(report, context.EnvironmentReport));
    return;
}

Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
Console.WriteLine("Студент: Подфедько Артур, група ФЕІ-37");
Console.WriteLine(new string('-', 52));
Console.WriteLine($"ОС (OSDescription)  : {report.OsDescription}");
Console.WriteLine($"ОС (Environment)    : {report.OsVersion}");
Console.WriteLine($"Архітектура процесу : {report.ProcessArchitecture}");
Console.WriteLine($"Версія .NET (CLR)   : {report.ClrVersion}");
Console.WriteLine($"Runtime             : {report.FrameworkDescription}");
Console.WriteLine($"RID (визначено)     : {report.DetectedRid}");
Console.WriteLine($"RID (від .NET)      : {report.ReportedRid}");
Console.WriteLine($"Збірка Core         : {report.BuildNote}");
Console.WriteLine($"Каталог застосунку  : {report.BaseDirectory}");
Console.WriteLine($"Поточний каталог    : {report.CurrentDirectory}");
Console.WriteLine(new string('-', 52));
Console.WriteLine("Предметна область: Замовлення (клієнт, товар, замовлення, рядок замовлення)");

[JsonSerializable(typeof(EnvironmentReport))]
internal partial class ReportJsonContext : JsonSerializerContext
{
}
