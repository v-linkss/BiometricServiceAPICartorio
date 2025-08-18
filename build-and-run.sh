#!/bin/bash

# Script para compilar e executar o Scanner API (.NET 8)
# Autor: Sistema de Cartório
# Data: $(date)

echo "🚀 Iniciando build e execução do Scanner API (.NET 8)..."
echo "=================================================="

# Verificar se o .NET 8 está instalado
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK não encontrado. Por favor, instale o .NET 8.0 ou superior."
    exit 1
fi

# Verificar versão do .NET
DOTNET_VERSION=$(dotnet --version)
echo "✅ .NET SDK encontrado: $DOTNET_VERSION"

# Verificar se é .NET 8 ou superior
if [[ ! "$DOTNET_VERSION" =~ ^8\. ]]; then
    echo "❌ Versão do .NET deve ser 8.0 ou superior. Versão atual: $DOTNET_VERSION"
    echo "💡 Execute: wget -q -O - https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -"
    echo "💡 Execute: sudo apt-add-repository 'deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-$(lsb_release -cs)-prod $(lsb_release -cs) main'"
    echo "💡 Execute: sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0"
    exit 1
fi

# Navegar para o diretório do projeto
cd HelloWorldApi

echo "📁 Diretório atual: $(pwd)"

# Limpar builds anteriores
echo "🧹 Limpando builds anteriores..."
dotnet clean

# Restaurar dependências
echo "📦 Restaurando dependências..."
dotnet restore

# Compilar o projeto
echo "🔨 Compilando o projeto..."
dotnet build -c Release

if [ $? -eq 0 ]; then
    echo "✅ Compilação bem-sucedida!"
else
    echo "❌ Erro na compilação!"
    exit 1
fi

# Criar diretórios necessários
echo "📁 Criando diretórios necessários..."
mkdir -p ScannedDocuments
mkdir -p tmp

# Verificar se os diretórios foram criados
if [ -d "ScannedDocuments" ] && [ -d "tmp" ]; then
    echo "✅ Diretórios criados com sucesso!"
else
    echo "❌ Erro ao criar diretórios!"
    exit 1
fi

# Executar a aplicação
echo "🚀 Executando a aplicação..."
echo "📖 Documentação disponível em: http://localhost:5000"
echo "🔌 API disponível em: http://localhost:5000/api"
echo "🏥 Health check: http://localhost:5000/health"
echo "⏹️  Pressione Ctrl+C para parar"

dotnet run --urls "http://localhost:5000"
