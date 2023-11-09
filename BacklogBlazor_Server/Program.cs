using System.Text;
using BacklogBlazor_Server.Services;
using dotenv.net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;
using LogLevel = NLog.LogLevel;

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