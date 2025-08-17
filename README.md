# Scanner API - Serviço de Digitalização de Documentos

## Visão Geral

Este projeto é uma API RESTful desenvolvida em ASP.NET Core que fornece funcionalidades avançadas de digitalização de documentos, incluindo suporte a múltiplas páginas e geração de PDF. A API foi projetada para ser integrada em sistemas de cartório e outros ambientes que necessitam de digitalização profissional de documentos.

## Funcionalidades Principais

### 🖨️ Digitalização de Documentos

- **Scanner de Página Única**: Digitalização de documentos de uma página
- **Scanner de Múltiplas Páginas**: Suporte a documentos com várias páginas
- **Alimentador Automático**: Suporte a alimentador automático de documentos (ADF)
- **Digitalização Duplex**: Suporte a digitalização frente e verso

### 📄 Formatos de Saída

- **PDF**: Geração de documentos PDF com múltiplas páginas
- **JPEG**: Imagens em formato JPEG de alta qualidade
- **PNG**: Imagens em formato PNG com transparência
- **TIFF**: Imagens TIFF para arquivamento de longo prazo

### ⚙️ Configurações Avançadas

- **Resolução Configurável**: Suporte a diferentes resoluções (150, 300, 600 DPI)
- **Rotação Automática**: Detecção e correção automática de orientação
- **Detecção de Bordas**: Identificação automática das bordas do documento
- **Interface Twain**: Integração com drivers Twain padrão da indústria

### 🔧 Recursos Adicionais

- **Limpeza Automática**: Remoção automática de arquivos antigos
- **Monitoramento de Arquivos**: Sistema de observação de diretórios
- **API RESTful**: Interface HTTP completa com documentação Swagger
- **Logs Detalhados**: Sistema de logging para auditoria e debug

## Arquitetura do Projeto

```
BiometricServiceAPICartorio/
├── HelloWorldApi/                 # API Principal de Scanner
│   ├── Controllers/               # Controladores da API
│   │   ├── ScannerController.cs   # Controlador principal do scanner
│   │   └── HelloWorldController.cs
│   ├── Models/                    # Modelos de dados
│   │   ├── ScanRequest.cs         # Requisição de digitalização
│   │   ├── ScanResponse.cs        # Resposta da digitalização
│   │   └── ScannerSource.cs       # Fonte de scanner
│   ├── Services/                  # Serviços da aplicação
│   │   ├── ScannerService.cs      # Serviço principal do scanner
│   │   ├── FileWatcherService.cs  # Monitoramento de arquivos
│   │   └── DocumentCleanupService.cs # Limpeza automática
│   ├── Interface/                 # Interfaces dos serviços
│   │   └── IScannerService.cs     # Interface do scanner
│   ├── appsettings.json           # Configurações da aplicação
│   ├── Startup.cs                 # Configuração da aplicação
│   └── Program.cs                 # Ponto de entrada
├── LeitorApi/                     # API de Biometria (legado)
├── ScannerAPP/                    # Aplicação Desktop de Scanner
├── MonitorarArquivos/            # Monitor de arquivos
└── uploadObserver/                # Observador de uploads
```

## Tecnologias Utilizadas

- **.NET 8.0**: Framework principal da aplicação
- **ASP.NET Core**: Framework web para APIs REST
- **iTextSharp**: Geração e manipulação de PDFs
- **System.Drawing.Common**: Manipulação de imagens
- **TwainDotNet**: Integração com drivers Twain
- **Swagger/OpenAPI**: Documentação da API
- **CORS**: Suporte a requisições cross-origin

## Instalação e Configuração

### Pré-requisitos

- .NET 8.0 SDK ou superior
- Visual Studio 2022 ou VS Code
- Scanner compatível com Twain
- Drivers Twain instalados no sistema

### Passos de Instalação

1. **Clone o repositório**

   ```bash
   git clone https://github.com/seu-usuario/BiometricServiceAPICartorio.git
   cd BiometricServiceAPICartorio
   ```

2. **Navegue para o projeto principal**

   ```bash
   cd HelloWorldApi
   ```

3. **Restaura as dependências**

   ```bash
   dotnet restore
   ```

4. **Configura as variáveis de ambiente**

   ```bash
   # No Linux/Mac
   export ASPNETCORE_ENVIRONMENT=Development

   # No Windows
   set ASPNETCORE_ENVIRONMENT=Development
   ```

5. **Executa a aplicação**

   ```bash
   dotnet run
   ```

6. **Acessa a documentação da API**
   ```
   http://localhost:5000
   ```

## Configuração

### appsettings.json

```json
{
  "ScannerSettings": {
    "DefaultOutputDirectory": "ScannedDocuments",
    "DefaultResolution": 300,
    "DefaultFormat": "PDF",
    "MaxFileSizeMB": 100,
    "SupportedFormats": ["PDF", "JPEG", "PNG", "TIFF"],
    "AutoCleanup": true,
    "CleanupIntervalHours": 24
  },
  "TwainSettings": {
    "ShowUI": true,
    "ShowProgressIndicator": true,
    "UseDocumentFeeder": false,
    "UseDuplex": false,
    "AutomaticRotate": true,
    "AutomaticBorderDetection": true
  }
}
```

## Uso da API

### Endpoints Principais

#### 1. Obter Fontes de Scanner

```http
GET /api/scanner/sources
```

#### 2. Testar Conexão

```http
GET /api/scanner/test-connection/{sourceName}
```

#### 3. Digitalizar Documento (Página Única)

```http
POST /api/scanner/scan
Content-Type: application/json

{
  "sourceName": "Default Scanner",
  "useDocumentFeeder": false,
  "showTwainUI": true,
  "showProgressIndicatorUI": true,
  "useDuplex": false,
  "automaticRotate": true,
  "automaticBorderDetection": true,
  "resolution": 300,
  "outputFormat": "PDF",
  "outputFileName": "meu_documento"
}
```

#### 4. Digitalizar Documento (Múltiplas Páginas)

```http
POST /api/scanner/scan-multipage
Content-Type: application/json

{
  "sourceName": "Default Scanner",
  "useDocumentFeeder": true,
  "outputFormat": "PDF",
  "outputFileName": "documento_multipagina"
}
```

#### 5. Obter Documento Digitalizado

```http
GET /api/scanner/document/{filePath}
```

#### 6. Deletar Documento

```http
DELETE /api/scanner/document/{filePath}
```

### Exemplos de Uso

#### Digitalização Simples

```bash
curl -X POST "http://localhost:5000/api/scanner/scan" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceName": "Default Scanner",
    "outputFormat": "PDF",
    "outputFileName": "contrato"
  }'
```

#### Digitalização de Múltiplas Páginas

```bash
curl -X POST "http://localhost:5000/api/scanner/scan-multipage" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceName": "Default Scanner",
    "useDocumentFeeder": true,
    "outputFormat": "PDF",
    "outputFileName": "relatorio_completo"
  }'
```

## Desenvolvimento

### Estrutura de Código

#### Modelos (Models)

- **ScanRequest**: Define os parâmetros para uma requisição de digitalização
- **ScanResponse**: Retorna o resultado de uma operação de digitalização
- **ScannerSource**: Representa uma fonte de scanner disponível

#### Serviços (Services)

- **ScannerService**: Implementa a lógica principal de digitalização
- **DocumentCleanupService**: Gerencia limpeza automática de arquivos
- **FileWatcherService**: Monitora mudanças em diretórios

#### Controladores (Controllers)

- **ScannerController**: Expõe os endpoints da API de scanner
- **HelloWorldController**: Controlador de exemplo

### Adicionando Novos Formatos

Para adicionar um novo formato de saída:

1. **Atualize o enum de formatos** em `ScanRequest.cs`
2. **Implemente a lógica de conversão** em `ScannerService.cs`
3. **Adicione o MIME type** em `ScannerController.cs`
4. **Atualize a documentação** e testes

### Testes

Execute os testes usando:

```bash
dotnet test
```

## Monitoramento e Logs

### Logs da Aplicação

A aplicação utiliza o sistema de logging integrado do .NET Core. Os logs incluem:

- Operações de digitalização
- Erros e exceções
- Atividades de limpeza automática
- Conexões com scanners

### Monitoramento de Arquivos

O sistema monitora automaticamente:

- Criação de novos arquivos
- Modificações em arquivos existentes
- Exclusão de arquivos

## Segurança

### Configurações de Segurança

- **CORS**: Configurado para permitir requisições cross-origin
- **Validação de Entrada**: Todos os parâmetros são validados
- **Sanitização de Arquivos**: Nomes de arquivo são sanitizados
- **Limite de Tamanho**: Controle de tamanho máximo de arquivos

### Recomendações

- Configure HTTPS em produção
- Implemente autenticação e autorização
- Restrinja acesso aos diretórios de documentos
- Monitore logs de acesso

## Troubleshooting

### Problemas Comuns

#### 1. Scanner não é detectado

- Verifique se os drivers Twain estão instalados
- Confirme se o scanner está conectado e ligado
- Teste a conexão usando o endpoint `/test-connection`

#### 2. Erro ao gerar PDF

- Verifique se o diretório de saída tem permissões de escrita
- Confirme se o iTextSharp está instalado corretamente
- Verifique os logs para detalhes do erro

#### 3. Arquivos não são limpos automaticamente

- Verifique se o `DocumentCleanupService` está registrado
- Confirme as configurações de limpeza no `appsettings.json`
- Verifique os logs do serviço

### Logs de Debug

Para habilitar logs detalhados, configure no `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "HelloWorldApi.Services": "Debug"
    }
  }
}
```

## Contribuição

### Como Contribuir

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

### Padrões de Código

- Siga as convenções de nomenclatura do C#
- Adicione comentários XML para documentação da API
- Implemente tratamento de erros adequado
- Escreva testes para novas funcionalidades

## Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## Suporte

Para suporte e dúvidas:

- Abra uma issue no GitHub
- Entre em contato com a equipe de desenvolvimento
- Consulte a documentação da API em `/swagger`

## Roadmap

### Versão 1.1

- [ ] Suporte a OCR (Reconhecimento Óptico de Caracteres)
- [ ] Compressão de PDFs
- [ ] Assinatura digital de documentos
- [ ] Integração com sistemas de armazenamento em nuvem

### Versão 1.2

- [ ] Interface web para configuração
- [ ] Dashboard de monitoramento
- [ ] Relatórios de uso
- [ ] API de webhooks para notificações

### Versão 2.0

- [ ] Suporte a múltiplos scanners simultâneos
- [ ] Processamento em lote
- [ ] Integração com sistemas de workflow
- [ ] Suporte a diferentes protocolos de scanner

---

**Desenvolvido com ❤️ para o sistema de cartório**
