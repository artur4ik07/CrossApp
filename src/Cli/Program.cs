using System.Runtime.InteropServices;

Console.OutputEncoding = System.Text.Encoding.UTF8;

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