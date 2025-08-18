FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HelloWorldApi.csproj", "./"]
RUN dotnet restore "HelloWorldApi.csproj"
COPY . .
WORKDIR "/src/HelloWorldApi"
RUN dotnet build "HelloWorldApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HelloWorldApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Criar diretório para documentos digitalizados
RUN mkdir -p /app/ScannedDocuments && \
    chmod 755 /app/ScannedDocuments

# Criar diretório temporário
RUN mkdir -p /app/tmp && \
    chmod 755 /app/tmp

ENTRYPOINT ["dotnet", "HelloWorldApi.dll"]
