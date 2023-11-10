using System.Text;
using System.Timers;
using BacklogBlazor_Server.Services;
using Dapper;
using dotenv.net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;
using LogLevel = NLog.LogLevel;
using Timer = System.Timers.Timer;

DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: false));
ConfigureNLog();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseNLog();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<AuthDataService>(_ => new AuthDataService(Environment.GetEnvironmentVariable("SQL_CONN")));
builder.Services.AddScoped<BacklogDataService>(_ => new BacklogDataService(Environment.GetEnvironmentVariable("SQL_CONN")));
builder.Services.AddScoped<UserDataService>(_ => new UserDataService(Environment.GetEnvironmentVariable("SQL_CONN")));
builder.Services.AddSingleton<HltbService>();
builder.Services.AddSingleton<ThirdPartyService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(cors => cors.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddAuthentication("Bearer").AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET"))),
        ValidateIssuer = false,
        ValidateAudience = false,
        RequireExpirationTime = true
    };
});

builder.Services.AddAuthentication().AddJwtBearer("Refresh", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey =
            new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("REFRESH_SECRET"))),
        ValidateIssuer = false,
        ValidateAudience = false,
        RequireExpirationTime = true
    };
});

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("userId")
        .AddAuthenticationSchemes("Bearer")
        .Build();

    options.AddPolicy("Refresh", new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("userId")
        .AddAuthenticationSchemes("Refresh")
        .Build());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Keep the database alive
var keepAliveTimer = new Timer();
keepAliveTimer.Interval = 24 * 60 * 60 * 1000; // Once a day
keepAliveTimer.Elapsed += KeepDatabaseAlive;
keepAliveTimer.Start();

app.Run();


static void ConfigureNLog()
{
    var config = new LoggingConfiguration();
    var env = Environment.GetEnvironmentVariables();
    
    // Where to save logs
    var logFile = new FileTarget("logFile")
    {
        FileName = (string) env["LOG_FILE"], 
        ArchiveFileName = (string) env["LOG_FILE_ARCHIVE"],
        MaxArchiveFiles = 30,
        ArchiveEvery = FileArchivePeriod.Day,
        ArchiveNumbering = ArchiveNumberingMode.Date,
        ArchiveDateFormat = "yyyy-MM-dd"
    };

    var logConsole = new ColoredConsoleTarget("logConsole");

    config.AddRule(LogLevel.Info, LogLevel.Fatal, logFile);
    config.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole);
    
    LogManager.Configuration = config;
}

static void KeepDatabaseAlive(object? obj, ElapsedEventArgs args)
{
    MySqlTransaction? tran = null;
    
    try
    {
        Console.WriteLine("Sending database keep alive");
        
        using var sqlConnection = new MySqlConnection(Environment.GetEnvironmentVariable("SQL_CONN"));
        sqlConnection.Open();

        tran = sqlConnection.BeginTransaction();
        sqlConnection.Execute(
            "insert into game_cache (GameId, CompleteMainSeconds, CompletePlusSeconds, Complete100Seconds, CompleteAllSeconds)\nvalues (-1, 0, 0, 0, 0)", transaction: tran);
        sqlConnection.Execute("delete from game_cache where GameId = -1", transaction: tran);
        tran.Commit();
        
        Console.WriteLine("Database kept alive");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error while keeping database alive: {ex}\n\t{ex.Message}");
        tran?.Rollback();
    }
}