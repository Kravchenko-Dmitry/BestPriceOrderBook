using OrderBookAlgorithm;
using OrderBookApi.Api;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IOrderBookRepository, OrderBookRepository>();
builder.Services.AddScoped<IOrderAlgorithm, OrderAlgorithm>();
builder.Services.AddScoped<OrderManager>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapOrdersEndpoints();

await app.RunAsync();
