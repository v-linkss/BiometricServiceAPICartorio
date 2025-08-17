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