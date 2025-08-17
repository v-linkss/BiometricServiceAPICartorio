using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
    public string? PastaMonitorada { get; set; }

    static void Main(string[] args)
    {
        // Define o caminho do diretório monitorado
        string pastaMonitorada = "/home/ewerto/Documentos/Projetos/BiometricServiceAPICartorio/MonitorarArquivos/tmp";

        // Cria um novo FileSystemWatcher
        FileSystemWatcher fsw = new FileSystemWatcher(pastaMonitorada);

        // Define o evento que será disparado quando um arquivo é criado
        fsw.Created += new FileSystemEventHandler(ArquivoCriado);

        // Inicia a monitoração da pasta
        fsw.EnableRaisingEvents = true;

        // Mantém o programa em execução até que o usuário pressione uma tecla
        Console.WriteLine("Pressione uma tecla para sair...");
        Console.ReadKey();
    }

    static async void ArquivoCriado(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Define os campos do formulário de dados
            string pessoaToken = "EYIuZ";
            string bucket = "1-cartorio";
            string tipo = "ficha";

            // Cria um novo formulário de dados
            var formData = new MultipartFormDataContent();
            if (e.Name != null)
            {
                formData.Add(new StreamContent(File.OpenRead(e.FullPath)), "file", e.Name);
            }
            formData.Add(new StringContent(pessoaToken), "pessoa_token");
            formData.Add(new StringContent(bucket), "bucket");
            formData.Add(new StringContent(tipo), "tipo");

            // Cria um novo cliente HTTP
            var httpClient = new HttpClient();

            // Envia a requisição HTTP para o endpoint
            var response = await httpClient.PostAsync("http://localhost:5982/upload", formData);

            // Verifica se a requisição foi bem-sucedida
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Arquivo enviado com sucesso!");
            }
            else
            {
                Console.WriteLine("Erro ao enviar arquivo: " + response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao enviar arquivo: " + ex.Message);
        }
    }
}
// using System;
// using System.IO;
// using System.Net.Http;
// using System.Net.Http.Headers;
// using System.Text;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.Http;
// using Microsoft.Extensions.Hosting;
// using Newtonsoft.Json;

// namespace MonitorarArquivos
// {
//     public class MeuProgram
//     {
//         public static void Main(string[] args)
//         {
//             var host = new WebHostBuilder()
//                 .UseKestrel()
//                 .UseUrls("http://localhost:3333")
//                 .UseStartup<Startup>()
//                 .Build();

//             host.Run();
//         }
//     }

//     public class Startup
//     {
//         public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
//         {
//             app.UseRouting();
//             app.UseEndpoints(endpoints =>
//             {
//                 endpoints.MapPost("/observarPasta", async context =>
//                 {
//                     try
//                     {
//                         // Define o caminho do diretório monitorado
//                         string pastaMonitorada = "/home/ewerto/Documentos/Projetos/BiometricServiceAPICartorio/MonitorarArquivos/tmp";

//                         // Cria um novo FileSystemWatcher
//                         FileSystemWatcher fsw = new FileSystemWatcher(pastaMonitorada);

//                         // Define o evento que será disparado quando um arquivo é criado
//                         fsw.Created += new FileSystemEventHandler(MeuArquivoCriado);

//                         // Inicia a monitoração da pasta
//                         fsw.EnableRaisingEvents = true;

//                         // Mantém o programa em execução até que o usuário pressione uma tecla
//                         Console.WriteLine("Pressione uma tecla para sair...");
//                         Console.ReadKey();
//                     }
//                     catch (Exception ex)
//                     {
//                         Console.WriteLine("Erro ao monitorar pasta: " + ex.Message);
//                     }
//                 });
//             });
//         }

//         static async void MeuArquivoCriado(object sender, FileSystemEventArgs e)
//         {
//             try
//             {
//                 // Define os parâmetros da requisição
//                 string pessoaToken = "EYIuZ";
//                 string bucket = "1-cartorio";
//                 string tipo = "ficha";
//                 string arquivo = e.FullPath;

//                 // Cria um novo cliente HTTP
//                 var httpClient = new HttpClient();

//                 // Cria um novo objeto para armazenar os parâmetros da requisição
//                 var parametros = new
//                 {
//                     pessoa_token = pessoaToken,
//                     bucket = bucket,
//                     tipo = tipo,
//                     arquivo = arquivo
//                 };

//                 // Converte o objeto em uma string JSON
//                 var json = JsonConvert.SerializeObject(parametros);

//                 // Cria um novo conteúdo para a requisição
//                 var conteudo = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

//                 // Envia a requisição HTTP para o endpoint
//                 var response = await httpClient.PostAsync("http://localhost:5982/upload", conteudo);

//                 // Verifica se a requisição foi bem-sucedida
//                 if (response.IsSuccessStatusCode)
//                 {
//                     Console.WriteLine("Arquivo enviado com sucesso!");
//                 }
//                 else
//                 {
//                     Console.WriteLine("Erro ao enviar arquivo: " + response.StatusCode);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine("Erro ao enviar arquivo: " + ex.Message);
//             }
//         }
//     }
// }