using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using HelloWorldApi.Services;
using HelloWorldApi.Interface;

namespace HelloWorldApi
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            // Configurar Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Scanner API",
                    Version = "v1",
                    Description = "API para digitalização de documentos com suporte a múltiplas páginas e PDF"
                });
            });
            
            // Registrar serviços
            services.AddSingleton<FileWatcherService>(provider =>
            {
                return new FileWatcherService("/home/ewerto/Documentos/Projetos/BiometricServiceAPICartorio/HelloWorldApi/tmp");
            });
            
            services.AddScoped<IScannerService, ScannerService>();
            
            // Registrar serviço de limpeza automática
            services.AddHostedService<DocumentCleanupService>();
            
            // Configurar CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
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
            
            // Usar CORS
            app.UseCors("AllowAll");
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}