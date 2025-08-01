using OrderBookAlgorithm;
using OrderBookAlgorithm.FileSystemAccess;
using OrderBookApi.Api;
using OrderBookApi.ExceptionHandler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddScoped<IOrderBookRepository, FileOrderBookRepository>();

builder.Services.AddScoped<IOrderAlgorithm, OrderAlgorithm>();
builder.Services.AddScoped<OrderManager>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.MapOrdersEndpoints();

await app.RunAsync();
