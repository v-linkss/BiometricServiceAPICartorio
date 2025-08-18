using HelloWorldApi.Services;
using HelloWorldApi.Interface;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Scanner API",
        Version = "v1",
        Description = "API para digitalização de documentos com suporte a múltiplas páginas e PDF"
    });
});

// Register services
builder.Services.AddSingleton<FileWatcherService>(provider =>
{
    return new FileWatcherService("/home/ewerto/Documentos/Projetos/BiometricServiceAPICartorio/HelloWorldApi/tmp");
});

builder.Services.AddScoped<IScannerService, ScannerService>();
builder.Services.AddHostedService<DocumentCleanupService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Scanner API v1");
        c.RoutePrefix = string.Empty; // Swagger na raiz
    });
}

app.UseRouting();
app.UseCors("AllowAll");

app.MapControllers();

// Minimal API endpoints for quick testing
app.MapGet("/", () => "Scanner API - Sistema de Cartório");
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

app.Run();