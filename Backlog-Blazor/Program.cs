using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BacklogBlazor;
using BacklogBlazor.Services;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Blazored.Modal;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.Configuration.GetSection("API")["BaseAddress"]) });
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<LocalService>();
builder.Services.AddScoped<NotificationService>();

builder.Services.AddBlazoredSessionStorage();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredModal();

// Add AuthorizedApiService
var serviceProvider = builder.Services.BuildServiceProvider();
var authorizedApiService = new AuthorizedApiService(serviceProvider.GetService<HttpClient>(),
    serviceProvider.GetService<SessionService>(), serviceProvider.GetService<LocalService>());
await authorizedApiService.InitializeAsync();
builder.Services.AddScoped(_ => authorizedApiService);

await builder.Build().RunAsync();