using FastEndpoints;
using FastEndpoints.Swagger;
using TheMillionthFoodOrderApp.Application;
using TheMillionthFoodOrderApp.Infrastructure;
using TheMillionthFoodOrderApp.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Host.UseWolverine();
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "TheMillionthFoodOrderApp API";
        s.Version = "v1";
    };
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
