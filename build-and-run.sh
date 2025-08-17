#!/bin/bash

# Script para compilar e executar o Scanner API
# Autor: Sistema de Cartório
# Data: $(date)

echo "🚀 Iniciando build e execução do Scanner API..."
echo "=================================================="

# Verificar se o .NET está instalado
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK não encontrado. Por favor, instale o .NET 8.0 ou superior."
    exit 1
fi

echo "✅ .NET SDK encontrado: $(dotnet --version)"

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
echo "⏹️  Pressione Ctrl+C para parar"

dotnet run --urls "http://localhost:5000"
