using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using YourNamespace.Services;
using YourNamespace.Library.Database;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var configuration = builder.Configuration;

// Register MongoDB
var mongoConnectionString = configuration.GetConnectionString("MongoDb");
var databaseName = configuration["MongoDbSettings:DatabaseName"];

if (string.IsNullOrEmpty(mongoConnectionString) || string.IsNullOrEmpty(databaseName))
{
    throw new InvalidOperationException("MongoDB configuration is missing.");
}

var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase(databaseName);
builder.Services.AddSingleton(database);

// Register services
builder.Services.AddScoped<EventService>();
builder.Services.AddSingleton<TaskService>();
builder.Services.AddScoped<EventTypeService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ChatThreadService>();
builder.Services.AddScoped<DocumentDetailsService>();
builder.Services.AddSingleton<MongoDbService>();

// Add controllers
builder.Services.AddControllers();

// Enable Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin() // Allows requests from any domain (use carefully in production)
            .AllowAnyMethod() // Allows GET, POST, PUT, DELETE, etc.
            .AllowAnyHeader()); // Allows all headers
});

var app = builder.Build();

// Enable Swagger in development mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
