# 🚀 Referência Rápida das Rotas - Scanner API

## 📍 **Base URL:** `http://localhost:5000`

---

## 🖨️ **Scanner API** (`/api/scanner`)

| Método   | Endpoint                        | Descrição                     | Parâmetros   |
| -------- | ------------------------------- | ----------------------------- | ------------ |
| `GET`    | `/sources`                      | Listar fontes de scanner      | -            |
| `GET`    | `/test-connection/{sourceName}` | Testar conexão                | `sourceName` |
| `POST`   | `/scan`                         | Digitalizar 1 página          | JSON body    |
| `POST`   | `/scan-multipage`               | Digitalizar múltiplas páginas | JSON body    |
| `GET`    | `/document/{*filePath}`         | Baixar arquivo                | `filePath`   |
| `DELETE` | `/document/{*filePath}`         | Deletar arquivo               | `filePath`   |

---

## 🏠 **Hello World** (`/api/helloworld`)

| Método | Endpoint | Descrição           |
| ------ | -------- | ------------------- |
| `GET`  | `/`      | Teste básico da API |

---

## 📋 **Parâmetros Comuns**

### **Obrigatórios:**

- `sourceName`: Nome do scanner
- `outputFormat`: PDF, JPEG, PNG, TIFF

### **Opcionais:**

- `outputFileName`: Nome do arquivo
- `resolution`: 150, 300, 600 DPI
- `useDocumentFeeder`: true/false
- `showTwainUI`: true/false

---

## 🔥 **Exemplos Rápidos**

### **PDF Simples:**

```bash
curl -X POST "http://localhost:5000/api/scanner/scan" \
  -H "Content-Type: application/json" \
  -d '{"sourceName":"Default Scanner","outputFormat":"PDF"}'
```

### **Múltiplas Páginas:**

```bash
curl -X POST "http://localhost:5000/api/scanner/scan-multipage" \
  -H "Content-Type: application/json" \
  -d '{"sourceName":"Default Scanner","outputFormat":"PDF","useDocumentFeeder":true}'
```

---

## 📚 **Documentação Completa:**

**Ver:** `API_ROUTES.md`
