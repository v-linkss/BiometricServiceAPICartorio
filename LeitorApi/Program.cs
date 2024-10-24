using BiometricService;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWindowsService();
LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);
builder.Services.AddScoped<Biometric>();
builder.Services.AddSingleton<APIService>();
builder.Services.AddHostedService<APIService>();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.
        AllowAnyOrigin().
        AllowAnyHeader().
        AllowAnyMethod();
    });
});

var serviceApp = builder.Build();

serviceApp.UseRouting();
serviceApp.UseCors();
serviceApp.MapControllers();

serviceApp.Run();