using System.Text;
using System.Text.Json.Serialization;
using BackendPractice.Data;
using BackendPractice.Models;
using BackendPractice.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Configure the SQL Server connection string using ConnectionStrings:DefaultConnection.");
}

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32 ||
    string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    jwtOptions.AccessTokenMinutes <= 0)
{
    throw new InvalidOperationException(
        "Configure Jwt:SigningKey (at least 32 bytes), Jwt:Issuer, Jwt:Audience, and a positive Jwt:AccessTokenMinutes.");
}

builder.Services.AddOpenApi();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<CourseDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.Configure<CourseOptions>(builder.Configuration.GetSection("Courses"));
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RoleClaimType = "role",
            NameClaimType = "sub"
        };
    });
builder.Services.AddAuthorization();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/api/courses"));
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[Users] (
                [Id] int IDENTITY(1,1) NOT NULL,
                [Name] nvarchar(100) NOT NULL,
                [Email] nvarchar(256) NOT NULL,
                [PasswordHash] nvarchar(512) NOT NULL,
                [Role] nvarchar(max) NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NOT NULL,
                CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_Users_Email] ON [dbo].[Users] ([Email]);
        END;
        """);
}

app.Run();

