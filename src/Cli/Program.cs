using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Encodings.Web;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var appData = new
{
    Application = "CrossApp",
    Student = "Подфедько Артур",
    Group = "ФЕІ-37",
    OSDescription = RuntimeInformation.OSDescription,
    OSVersion = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotNetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    BaseDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Замовлення (клієнт, товар, замовлення, рядок замовлення)"
};

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions 
    { 
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
    };
    string jsonOutput = JsonSerializer.Serialize(appData, options);
    Console.WriteLine(jsonOutput);
}
else
{
Console.WriteLine("Hi everyone");
Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
Console.WriteLine("Студент: Подфедько Артур, група ФЕІ-37");
Console.WriteLine(new string('-', 52));
Console.WriteLine($"ОС (OSDescription)   : {RuntimeInformation.OSDescription}");
Console.WriteLine($"ОС (Environment)     : {Environment.OSVersion}");
Console.WriteLine($"Архітектура процесу  : {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"Версія .NET (CLR)    : {Environment.Version}");
Console.WriteLine($"Runtime              : {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"Каталог застосунку   : {AppContext.BaseDirectory}");
Console.WriteLine($"Поточний каталог     : {Environment.CurrentDirectory}");
Console.WriteLine(new string('-', 52));
Console.WriteLine("Предметна область: Замовлення (клієнт, товар, замовлення, рядок замовлення)");
}