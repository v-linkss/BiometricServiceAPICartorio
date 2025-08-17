# 📚 Documentação das Rotas - Scanner API

## 🌐 Visão Geral da API

**Base URL:** `http://localhost:5000`  
**Versão:** v1.0  
**Formato de Resposta:** JSON  
**Autenticação:** Não implementada (em desenvolvimento)

---

## 🖨️ **Scanner Controller** (`/api/scanner`)

### 📋 **1. Obter Fontes de Scanner Disponíveis**

**Endpoint:** `GET /api/scanner/sources`

**Descrição:** Retorna uma lista de todas as fontes de scanner disponíveis no sistema.

**Resposta de Sucesso:**

```json
[
  {
    "name": "Default Scanner",
    "productName": "Generic Scanner",
    "vendorName": "Generic Vendor",
    "isDefault": true,
    "supportsDocumentFeeder": true,
    "supportsDuplex": false,
    "maxResolution": 600,
    "supportedFormats": ["PDF", "JPEG", "PNG", "TIFF"]
  }
]
```

**Códigos de Status:**

- `200 OK`: Lista retornada com sucesso
- `500 Internal Server Error`: Erro interno do servidor

---

### 🔌 **2. Testar Conexão com Scanner**

**Endpoint:** `GET /api/scanner/test-connection/{sourceName}`

**Descrição:** Testa a conectividade com uma fonte de scanner específica.

**Parâmetros de URL:**

- `sourceName` (string): Nome da fonte do scanner (URL encoded)

**Exemplo de Uso:**

```http
GET /api/scanner/test-connection/Default%20Scanner
```

**Resposta de Sucesso:**

```json
{
  "sourceName": "Default Scanner",
  "isConnected": true
}
```

**Códigos de Status:**

- `200 OK`: Teste realizado com sucesso
- `500 Internal Server Error`: Erro interno do servidor

---

### 📄 **3. Digitalizar Documento (Página Única)**

**Endpoint:** `POST /api/scanner/scan`

**Descrição:** Digitaliza um documento de uma página e retorna o arquivo no formato especificado.

**Headers:**

```
Content-Type: application/json
```

**Corpo da Requisição:**

```json
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

**Parâmetros da Requisição:**

| Parâmetro                  | Tipo    | Obrigatório | Padrão | Descrição                               |
| -------------------------- | ------- | ----------- | ------ | --------------------------------------- |
| `sourceName`               | string  | ✅          | -      | Nome da fonte do scanner                |
| `useDocumentFeeder`        | boolean | ❌          | false  | Usar alimentador automático             |
| `showTwainUI`              | boolean | ❌          | true   | Mostrar interface do scanner            |
| `showProgressIndicatorUI`  | boolean | ❌          | true   | Mostrar indicador de progresso          |
| `useDuplex`                | boolean | ❌          | false  | Digitalização frente e verso            |
| `automaticRotate`          | boolean | ❌          | true   | Rotação automática                      |
| `automaticBorderDetection` | boolean | ❌          | true   | Detecção automática de bordas           |
| `resolution`               | integer | ❌          | 300    | Resolução em DPI (150, 300, 600)        |
| `outputFormat`             | string  | ✅          | -      | Formato de saída (PDF, JPEG, PNG, TIFF) |
| `outputFileName`           | string  | ❌          | auto   | Nome do arquivo (sem extensão)          |

**Resposta de Sucesso:**

```json
{
  "success": true,
  "message": "Documento digitalizado com sucesso",
  "filePath": "/app/ScannedDocuments/meu_documento.pdf",
  "fileName": "meu_documento.pdf",
  "fileSize": 15420,
  "pageCount": 1,
  "outputFormat": "PDF",
  "scanDateTime": "2024-12-17T14:30:00"
}
```

**Resposta de Erro:**

```json
{
  "success": false,
  "message": "Erro durante a digitalização: Parâmetros inválidos"
}
```

**Códigos de Status:**

- `200 OK`: Digitalização realizada com sucesso
- `400 Bad Request`: Parâmetros inválidos ou erro na digitalização
- `500 Internal Server Error`: Erro interno do servidor

---

### 📚 **4. Digitalizar Documento (Múltiplas Páginas)**

**Endpoint:** `POST /api/scanner/scan-multipage`

**Descrição:** Digitaliza um documento com múltiplas páginas usando o alimentador automático.

**Headers:**

```
Content-Type: application/json
```

**Corpo da Requisição:**

```json
{
  "sourceName": "Default Scanner",
  "useDocumentFeeder": true,
  "showTwainUI": true,
  "showProgressIndicatorUI": true,
  "useDuplex": false,
  "automaticRotate": true,
  "automaticBorderDetection": true,
  "resolution": 300,
  "outputFormat": "PDF",
  "outputFileName": "relatorio_completo"
}
```

**Parâmetros da Requisição:** (Mesmos da rota anterior)

**Resposta de Sucesso:**

```json
{
  "success": true,
  "message": "Documento de 3 páginas digitalizado com sucesso",
  "filePath": "/app/ScannedDocuments/relatorio_completo.pdf",
  "fileName": "relatorio_completo.pdf",
  "fileSize": 45678,
  "pageCount": 3,
  "outputFormat": "PDF",
  "scanDateTime": "2024-12-17T14:35:00"
}
```

**Códigos de Status:**

- `200 OK`: Digitalização realizada com sucesso
- `400 Bad Request`: Parâmetros inválidos ou erro na digitalização
- `500 Internal Server Error`: Erro interno do servidor

---

### 📥 **5. Obter Documento Digitalizado**

**Endpoint:** `GET /api/scanner/document/{*filePath}`

**Descrição:** Retorna o arquivo digitalizado pelo caminho especificado.

**Parâmetros de URL:**

- `filePath` (string): Caminho completo do arquivo (wildcard)

**Exemplo de Uso:**

```http
GET /api/scanner/document/ScannedDocuments/meu_documento.pdf
```

**Headers de Resposta:**

```
Content-Type: application/pdf
Content-Disposition: attachment; filename="meu_documento.pdf"
```

**Resposta:** Arquivo binário do documento

**Códigos de Status:**

- `200 OK`: Arquivo retornado com sucesso
- `404 Not Found`: Arquivo não encontrado
- `500 Internal Server Error`: Erro interno do servidor

---

### 🗑️ **6. Deletar Documento Digitalizado**

**Endpoint:** `DELETE /api/scanner/document/{*filePath}`

**Descrição:** Remove um arquivo digitalizado do sistema.

**Parâmetros de URL:**

- `filePath` (string): Caminho completo do arquivo (wildcard)

**Exemplo de Uso:**

```http
DELETE /api/scanner/document/ScannedDocuments/meu_documento.pdf
```

**Resposta de Sucesso:**

```json
{
  "message": "Documento deletado com sucesso"
}
```

**Resposta de Erro:**

```json
{
  "error": "Documento não encontrado"
}
```

**Códigos de Status:**

- `200 OK`: Arquivo deletado com sucesso
- `404 Not Found`: Arquivo não encontrado
- `500 Internal Server Error`: Erro interno do servidor

---

## 🏠 **Hello World Controller** (`/api/helloworld`)

### 🌍 **7. Hello World**

**Endpoint:** `GET /api/helloworld`

**Descrição:** Endpoint de teste básico da API.

**Resposta:**

```
Hello World!
```

**Códigos de Status:**

- `200 OK`: Mensagem retornada com sucesso

---

## 📊 **Formato de Resposta Padrão**

### **Resposta de Sucesso:**

```json
{
  "success": true,
  "message": "Operação realizada com sucesso",
  "data": { ... },
  "timestamp": "2024-12-17T14:30:00"
}
```

### **Resposta de Erro:**

```json
{
  "success": false,
  "message": "Descrição do erro",
  "errorCode": "ERROR_CODE",
  "timestamp": "2024-12-17T14:30:00"
}
```

---

## 🔧 **Configurações de Scanner**

### **Formatos Suportados:**

- **PDF**: Documentos de múltiplas páginas
- **JPEG**: Imagens com compressão
- **PNG**: Imagens com transparência
- **TIFF**: Imagens de alta qualidade

### **Resoluções Suportadas:**

- **150 DPI**: Baixa qualidade, arquivo pequeno
- **300 DPI**: Qualidade padrão (recomendado)
- **600 DPI**: Alta qualidade, arquivo grande

### **Configurações Twain:**

- **ShowUI**: Interface visual do scanner
- **ProgressIndicator**: Barra de progresso
- **DocumentFeeder**: Alimentador automático
- **Duplex**: Digitalização frente e verso
- **AutoRotate**: Rotação automática
- **BorderDetection**: Detecção de bordas

---

## 📝 **Exemplos de Uso**

### **Exemplo 1: Digitalização Simples em PDF**

```bash
curl -X POST "http://localhost:5000/api/scanner/scan" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceName": "Default Scanner",
    "outputFormat": "PDF",
    "outputFileName": "contrato_venda"
  }'
```

### **Exemplo 2: Digitalização de Múltiplas Páginas**

```bash
curl -X POST "http://localhost:5000/api/scanner/scan-multipage" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceName": "Default Scanner",
    "outputFormat": "PDF",
    "outputFileName": "relatorio_anual",
    "useDocumentFeeder": true,
    "resolution": 600
  }'
```

### **Exemplo 3: Digitalização em JPEG**

```bash
curl -X POST "http://localhost:5000/api/scanner/scan" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceName": "Default Scanner",
    "outputFormat": "JPEG",
    "outputFileName": "foto_documento",
    "resolution": 300
  }'
```

### **Exemplo 4: Testar Conexão**

```bash
curl "http://localhost:5000/api/scanner/test-connection/Default%20Scanner"
```

### **Exemplo 5: Obter Documento**

```bash
curl "http://localhost:5000/api/scanner/document/ScannedDocuments/contrato_venda.pdf" \
  -o contrato_venda.pdf
```

### **Exemplo 6: Deletar Documento**

```bash
curl -X DELETE "http://localhost:5000/api/scanner/document/ScannedDocuments/contrato_venda.pdf"
```

---

## ⚠️ **Códigos de Erro Comuns**

| Código | Descrição              | Solução                         |
| ------ | ---------------------- | ------------------------------- |
| `400`  | Parâmetros inválidos   | Verificar formato da requisição |
| `404`  | Arquivo não encontrado | Verificar caminho do arquivo    |
| `500`  | Erro interno           | Verificar logs do servidor      |

---

## 🔒 **Segurança e Limitações**

### **Segurança:**

- CORS habilitado para desenvolvimento
- Validação de entrada em todos os endpoints
- Sanitização de nomes de arquivo

### **Limitações:**

- Sem autenticação (em desenvolvimento)
- Sem rate limiting
- Sem criptografia de arquivos

---

## 📚 **Documentação Adicional**

- **Swagger UI**: `http://localhost:5000` (quando rodando)
- **README.md**: Documentação geral do projeto
- **ScannerAPI.http**: Arquivo de testes HTTP

---

## 🆘 **Suporte**

Para dúvidas ou problemas:

1. Verifique os logs da aplicação
2. Consulte a documentação Swagger
3. Abra uma issue no repositório
4. Entre em contato com a equipe de desenvolvimento

---

**Última Atualização:** 17/12/2024  
**Versão da API:** 1.0.0  
**Desenvolvedor:** Sistema de Cartório
