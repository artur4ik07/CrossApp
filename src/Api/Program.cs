using Api.Endpoints;
using Api.Infrastructure;
using Api.Validation;
using Core;
using Core.Abstractions;
using Core.Storage;
using FluentValidation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<SaveOrderRequestValidator>();

builder.Services.AddSingleton<IOrderStore>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var env = sp.GetRequiredService<IHostEnvironment>();
    var configuredPath = config["Storage:DataFile"] ?? "../../data/orders.json";
    var dataFilePath = Path.GetFullPath(Path.Combine(env.ContentRootPath, configuredPath));

    var store = new FileOrderStore(dataFilePath);
    if (store.List().Count == 0)
    {
        foreach (var order in SampleData.Orders())
            store.Add(order);
    }
    return store;
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithTags("System");
app.MapOrders();

app.Run();

public partial class Program { }
