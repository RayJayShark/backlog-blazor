using System.Text;
using BacklogBlazor_Server.Services;
using dotenv.net;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: false));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<AuthDataService>(_ => new AuthDataService(Environment.GetEnvironmentVariable("SQL_CONN")));
builder.Services.AddScoped<BacklogDataService>(_ => new BacklogDataService(Environment.GetEnvironmentVariable("SQL_CONN")));
builder.Services.AddSingleton<HltbService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(cors => cors.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddAuthentication().AddJwtBearer(options =>
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