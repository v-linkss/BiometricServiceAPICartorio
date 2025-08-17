# 📚 Índice da Documentação - Scanner API

## 🚀 **Início Rápido**

### **Para Desenvolvedores:**

- [Referência Rápida das Rotas](ROUTES_QUICK_REFERENCE.md) - Consulta rápida das APIs
- [Arquivo de Testes HTTP](ScannerAPI.http) - Testes prontos para usar
- [Configuração do Projeto](README.md) - Instalação e configuração

### **Para Usuários Finais:**

- [Documentação Completa das Rotas](API_ROUTES.md) - Guia detalhado de todas as APIs
- [README Principal](../../README.md) - Visão geral do projeto

---

## 📋 **Estrutura da Documentação**

```

📁 HelloWorldApi/
├── 📄 README.md                           # Documentação geral do projeto
├── 📄 API_ROUTES.md                       # Documentação completa das rotas
├── 📄 ROUTES_QUICK_REFERENCE.md           # Referência rápida das APIs
├── 📄 ScannerAPI.http                     # Arquivo de testes HTTP
├── 📄 DOCUMENTATION_INDEX.md              # Este arquivo (índice)
├── 📄 appsettings.json                    # Configurações da aplicação
├── 📁 Controllers/                        # Controladores da API
├── 📁 Models/                             # Modelos de dados
├── 📁 Services/                           # Serviços da aplicação
└── 📁 Interface/                          # Interfaces dos serviços
```

---

## 🎯 **O que Você Precisa?**

### **🔍 "Quero entender como usar a API"**

- **Comece por:** [Referência Rápida das Rotas](ROUTES_QUICK_REFERENCE.md)
- **Depois:** [Documentação Completa das Rotas](API_ROUTES.md)
- **Exemplos práticos:** [Arquivo de Testes HTTP](ScannerAPI.http)

### **🚀 "Quero executar o projeto"**

- **Comece por:** [README Principal](../../README.md)
- **Script de execução:** `../build-and-run.sh`
- **Docker:** `../docker-compose.yml`

### **🔧 "Quero desenvolver/modificar"**

- **Estrutura:** [README Principal](../../README.md)
- **Configurações:** [appsettings.json](appsettings.json)
- **Código:** Explore as pastas Controllers/, Models/, Services/

### **🧪 "Quero testar a API"**

- **Testes HTTP:** [ScannerAPI.http](ScannerAPI.http)
- **Swagger UI:** `http://localhost:5000` (quando rodando)
- **Exemplos curl:** [Documentação Completa das Rotas](API_ROUTES.md)

---

## 📖 **Guia de Leitura Recomendado**

### **1º Passo: Visão Geral**

- [README Principal](../../README.md) - Entenda o projeto

### **2º Passo: Configuração**

- [README Principal](../../README.md#instalação-e-configuração)
- Execute `../build-and-run.sh`

### **3º Passo: Uso da API**

- [Referência Rápida das Rotas](ROUTES_QUICK_REFERENCE.md)
- [Arquivo de Testes HTTP](ScannerAPI.http)

### **4º Passo: Documentação Detalhada**

- [Documentação Completa das Rotas](API_ROUTES.md)
- [README Principal](../../README.md#desenvolvimento)

---

## 🔗 **Links Importantes**

### **Documentação:**

- 📚 [README Principal](../../README.md)
- 📋 [API Completa](API_ROUTES.md)
- 🚀 [Referência Rápida](ROUTES_QUICK_REFERENCE.md)
- 🧪 [Testes HTTP](ScannerAPI.http)

### **Configuração:**

- ⚙️ [appsettings.json](appsettings.json)
- 🐳 [Dockerfile](Dockerfile)
- 🚀 [docker-compose.yml](../../docker-compose.yml)

### **Execução:**

- 🚀 [Script de Build](../../build-and-run.sh)
- 🐳 [Docker Compose](../../docker-compose.yml)

---

## 📱 **Endpoints Principais**

| Método   | Endpoint                            | Descrição                     |
| -------- | ----------------------------------- | ----------------------------- |
| `GET`    | `/api/scanner/sources`              | Listar scanners               |
| `POST`   | `/api/scanner/scan`                 | Digitalizar 1 página          |
| `POST`   | `/api/scanner/scan-multipage`       | Digitalizar múltiplas páginas |
| `GET`    | `/api/scanner/document/{*filePath}` | Baixar arquivo                |
| `DELETE` | `/api/scanner/document/{*filePath}` | Deletar arquivo               |

---

## 🆘 **Precisa de Ajuda?**

### **Problemas Comuns:**

1. **API não responde:** Verifique se está rodando em `http://localhost:5000`
2. **Erro de compilação:** Execute `dotnet clean` e `dotnet restore`
3. **Scanner não detectado:** Use o endpoint `/test-connection`

### **Recursos de Suporte:**

- 📖 [README Principal](../../README.md#troubleshooting)
- 🧪 [Testes HTTP](ScannerAPI.http)
- 🌐 [Swagger UI](http://localhost:5000) (quando rodando)

---

## 📅 **Última Atualização**

- **Data:** 17/12/2024
- **Versão:** 1.0.0
- **Status:** Documentação completa

---

## 🔄 **Manutenção da Documentação**

Para manter esta documentação atualizada:

1. **Atualize os arquivos** quando modificar a API
2. **Teste os exemplos** para garantir que funcionam
3. **Mantenha o índice** sincronizado com os arquivos
4. **Use links relativos** para facilitar a navegação

---

**📧 Contato:** Equipe de Desenvolvimento  
**🏢 Projeto:** Sistema de Cartório  
**📅 Versão:** 1.0.0
