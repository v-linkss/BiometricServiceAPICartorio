# 🚀 Instalação do .NET 8 no Linux

## 📋 Pré-requisitos

- Ubuntu 18.04+ ou distribuição Linux compatível
- Acesso sudo
- Conexão com a internet

## 🔧 Instalação Automática

### **Ubuntu/Debian:**

```bash
# Baixar e executar o script de instalação
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

### **Usando o repositório oficial da Microsoft:**

```bash
# Adicionar chave GPG da Microsoft
wget -q -O - https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -

# Adicionar repositório
sudo apt-add-repository 'deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-$(lsb_release -cs)-prod $(lsb_release -cs) main'

# Atualizar e instalar
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

## 🔍 Verificação da Instalação

```bash
# Verificar versão
dotnet --version

# Verificar SDKs instalados
dotnet --list-sdks

# Verificar runtimes instalados
dotnet --list-runtimes
```

## 🐳 Instalação via Docker (Alternativa)

Se preferir usar Docker:

```bash
# Executar com Docker
docker run -it --rm -p 5000:5000 -v $(pwd):/app mcr.microsoft.com/dotnet/sdk:8.0

# Ou usar docker-compose
docker-compose up --build
```

## 🆘 Solução de Problemas

### **Erro de permissão:**

```bash
sudo chown -R $USER:$USER ~/.dotnet
```

### **PATH não configurado:**

```bash
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc
```

### **Dependências faltando:**

```bash
sudo apt-get install -y libc6-dev libgcc1 libgssapi-krb5-2 libicu-dev libssl-dev libstdc++6 zlib1g-dev
```

## 📚 Recursos Adicionais

- [Documentação oficial do .NET](https://docs.microsoft.com/dotnet/)
- [Downloads do .NET](https://dotnet.microsoft.com/download)
- [Guia de instalação](https://docs.microsoft.com/dotnet/core/install/)

## ✅ Próximos Passos

Após a instalação:

1. **Verifique a instalação:** `dotnet --version`
2. **Execute o projeto:** `./build-and-run.sh`
3. **Acesse a API:** `http://localhost:5000`

---

**🎯 Objetivo:** Ter o .NET 8 funcionando para executar a Scanner API  
**📅 Última Atualização:** 17/12/2024  
**🏢 Projeto:** Sistema de Cartório
