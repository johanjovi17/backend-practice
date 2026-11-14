using System.Text.Json.Serialization;
using BackendPractice.Data;
using BackendPractice.Models;
using BackendPractice.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure the SQL Server connection string using ConnectionStrings:DefaultConnection.");
}

builder.Services.AddOpenApi();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<CourseDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.Configure<CourseOptions>(builder.Configuration.GetSection("Courses"));
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");
app.MapGet("/", () => Results.Redirect("/api/courses"));
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();

