using Api.Endpoints;
using Core;
using Core.Abstractions;
using Core.Storage;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IOrderStore>(_ => new InMemoryOrderStore(SampleData.Orders()));

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
