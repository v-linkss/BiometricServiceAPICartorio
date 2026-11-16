# Agente 360 (`exeScanner`)

Unifica num único executável as três coisas que hoje estão espalhadas em dois
programas na estação do cartório:

- **scanner** — hoje `360scan.exe` (`C:\cartorio360\ScannerApp\`)
- **upload do documento escaneado** — hoje `360-serverLocal.exe` (Electron, 163 MB)
- **impressão de etiqueta de selo (ZPL)** — hoje `360-serverLocal.exe`, via libusb

Ficam **fora**, em processos próprios e intocados:

| Processo | Porta | Por quê |
|---|---|---|
| `BiometricService.exe` | 5000 | depende do SDK NITGEN, licenciado e de terceiro |
| `WS 360` (`web_service_360`) | 3333 | selos do TJ, serviço à parte |

O plano completo, com o histórico das decisões, está em
[`../Plano Agente 360 - unificacao dos exes.md`](../Plano%20Agente%20360%20-%20unificacao%20dos%20exes.md).

---

## Estado

| Parte | Situação |
|---|---|
| Projeto C# | **compila** (`dotnet build`, 0 avisos) |
| Publish self-contained | **funciona** — um `.exe` de 74 MB, sem .NET na máquina |
| Rotas HTTP | **testadas** — `/status`, `/health`, `/print`, `/upload`, `/uploadAnexo` |
| Digitalização simplex + multipágina (WIA) | **testada no Canon DR-C230** |
| Digitalização duplex (TWAIN) | **testada no Canon DR-C230** — folha única (2 págs) e multipágina (4 págs) |
| Impressão (spooler RAW) | **testada na Zebra GC420t** — 4 etiquetas, todas legíveis, incluindo lote de 2 num corpo só |
| Instalador `.iss` | **escrito, não compilado** — Inno Setup não está instalado aqui |
| Plataforma | **x86** — em x64 a EPSON some (data source TWAIN de 32 bits) |

## Estrutura

```
exeScanner/
├── lib/Interop.WIA.dll          interop pré-gerado (ver "Interop do WIA")
├── Cartorio360.Agente/
│   ├── Program.cs               mutex, porta, tray, Kestrel em 127.0.0.1
│   ├── Api/Rotas.cs             superfície HTTP
│   ├── Scanner/                 TWAIN (duplex) + WIA (simplex) + PdfSharpCore
│   ├── Servicos/                upload, impressão ZPL, última digitalização
│   ├── Bandeja/AgenteTray.cs    ícone e diagnóstico
│   └── Infra/                   config.json e log
├── installer/CartorioAgente.iss
└── publish/                     saída do dotnet publish
```

## Compilar

```powershell
cd Cartorio360.Agente
dotnet build -c Release          # verificação rápida
dotnet publish -c Release -o ..\publish
```

Gera `publish\Cartorio360Agente.exe` — arquivo único, self-contained, **x86**.

### Por que x86, e não x64

Por causa do TWAIN. Muitos *data sources* são de 32 bits, e um processo de 64
bits não os carrega. Medido nesta bancada, com a mesma sonda nas duas
arquiteturas:

| | Data sources visíveis |
|---|---|
| x64 | 1 — só a Canon DR-C230 |
| **x86** | 2 — Canon **e EPSON L355/L358** |

O que antes obrigava x64 era o SDK NITGEN, e a biometria saiu do escopo. Kestrel,
spooler, WIA e PdfSharpCore são indiferentes à arquitetura.

## Duplex: TWAIN, não WIA

O duplex é a única parte que **não** passa pelo WIA, e não foi escolha de gosto.
No driver do Canon DR-C230, o WIA declara a capacidade e recusa usá-la:

| Medição (WIA) | Resultado |
|---|---|
| 3086 (capacidades) | `37` = FEEDER+DUPLEX+FRONT_ONLY — declara duplex |
| 3088 `SubTypeValues` | `{1, 4, 32}` — declara aceitar DUPLEX |
| escrever `4` (DUPLEX) | `E_POINTER` |
| escrever `32` (FRONT_ONLY) | `E_POINTER` |
| escrever `5` (FEEDER\|DUPLEX), com e sem `PAGES=1` | grava, e a transferência morre com `E_UNEXPECTED` |

A propriedade 3088 desse driver é **efetivamente somente-leitura** — recusa
qualquer mudança, não só o duplex. Pelo TWAIN, o mesmo aparelho responde
`CAP_DUPLEXENABLED` com `CanSet=True` e liga na primeira tentativa.

Por isso: **duplex vai por TWAIN, simplex continua no WIA**. O WIA não é código
morto — é a reserva para scanner sem data source TWAIN, e o agente cai nele
sozinho quando o TWAIN não produz páginas.

Há uma sonda isolada em [`tools/SondaTwain`](tools/SondaTwain) que responde
`CAP_DUPLEXENABLED` é gravável neste aparelho? sem tocar no agente. Use-a ao
avaliar um scanner novo antes de concluir qualquer coisa.

### Duas armadilhas do TWAIN que já custaram caro aqui

1. **`Assembly.Location` é vazio em publish single-file.** `TwainSession(DataGroups)`
   e `TWIdentity.CreateFromAssembly` leem esse caminho para montar a identidade da
   aplicação, e estouram `ArgumentException: The path is empty`. Em `dotnet run`
   funciona, porque há DLL em disco — o defeito só aparece no build publicado. Por
   isso a identidade é montada à mão com `TWIdentity.Create(...)`.
2. **A contradição de thread.** O `WindowsFormsMessageLoopHook` só pode ser criado
   na thread de UI, mas essa thread não pode ficar bloqueada esperando: o TWAIN é
   protocolo de mensagens do Windows, e sem *message pump* o scanner nunca
   responde. Bloquear trava; usar `Task.Run` dá erro de thread. A saída é disparar
   na UI e aguardar por evento, com `await` e `TaskCompletionSource` — nunca
   `Wait()`.

### Instalador

Precisa do [Inno Setup 6](https://jrsoftware.org/isdl.php):

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\CartorioAgente.iss
```

Sai `installer\Output\CartorioAgente-Setup-2.0.0.exe`.

### Interop do WIA

`lib\Interop.WIA.dll` está versionado de propósito. O `WiaMultiPageScan` original
usava `<COMReference>`, que exige a task `ResolveComReference` — presente apenas
no MSBuild do .NET Framework. Com ela, `dotnet build` falha com **MSB4803** e o
projeto só compila em máquina com Visual Studio, o que atrapalharia o pipeline do
instalador.

Pré-gerando o interop, `dotnet build` funciona em qualquer lugar. Como o
`.csproj` usa `EmbedInteropTypes=true`, os tipos são embutidos no executável e a
DLL não é distribuída.

Para regerar (só é preciso se a API do WIA mudar, o que não acontece):

```powershell
& "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\TlbImp.exe" `
    C:\Windows\System32\wiaaut.dll /out:lib\Interop.WIA.dll /namespace:WIA
```

## Ícone

Vem de `Cartorio360-front/public/cartorio_icon.png`, a mesma marca do front.
Regenerar depois de trocar o PNG:

```powershell
.\tools\gerar-icone.ps1
```

Produz `Cartorio360.Agente\cartorio_icon.ico` com 9 resoluções (16 a 256), usado
em três lugares: ícone do executável (`ApplicationIcon`), ícone da bandeja e das
janelas (embutido como recurso, porque o publish é single-file) e ícone do
instalador.

**Uma limitação a saber:** o PNG de origem é 202×97, proporção ~2:1, e ícone
precisa ser quadrado. O script encaixa por largura e centraliza, mantendo a arte
fiel — mas isso deixa faixas transparentes em cima e embaixo, e no ícone de 16×16
da bandeja o "360" ocupa só a faixa central. Se quiser algo mais legível em
tamanho pequeno, o caminho é uma arte quadrada dedicada (por exemplo o "360"
branco sobre um quadrado arredondado azul), trocando o PNG de origem.

## API

Base: `http://127.0.0.1:5150/apiservice`

| Rota | O que faz |
|---|---|
| `GET /run-scanner?format=img\|pdf` | abre a janela de digitalização; retorna quando ela fecha |
| `POST /upload` | envia a última digitalização como anexo |
| `POST /uploadAnexo` | idêntico a `/upload` |
| `POST /uploadPessoa` | envia com `pessoa_token` |
| `POST /print` | ZPL → Zebra pelo spooler |
| `GET /status` | `{ agente, versao, temZebra }` |
| `GET /health` | diagnóstico completo, incluindo as filas de impressão |

As mesmas rotas respondem **sem** o prefixo `/apiservice`, para o front atual
continuar funcionando durante a migração.

### Detalhes que não são livres

O front já existe e lê campos específicos — mudá-los quebra a tela do cartório:

- `useEtiquetaImpressao.ts` lê `temZebra` no `/status` e
  `e.data.message || e.data.error` no erro do `/print`;
- `Scanner.vue` e `Documento.vue` leem `erro.data.details`.

Por isso os erros carregam `message`, `error` e `details` ao mesmo tempo.

O `/status` **sempre** responde 200, inclusive quando falha ao enumerar
impressoras. O front chama essa rota em toda tela de impressão com timeout de
1,5 s e trata qualquer falha como "sem Zebra"; um 500 viraria erro visível em
toda estação que imprime em impressora comum.

`?format=pdf` gera PDF; **qualquer outro valor, inclusive ausência**, gera PNG.
Três telas do front chamam `/run-scanner` sem o parâmetro e dependem desse padrão.

## O front precisa ser HTTPS

Não é preferência: desde o **Chrome 142** (out/2025) vale o *Local Network Access*. Uma página
pública só alcança `localhost` se **a origem for contexto seguro (HTTPS)** e o usuário conceder a
permissão que o navegador pede.

Um front servido em `http://` é barrado antes de qualquer preflight, com:

```
The request client is not a secure context and the resource is in
more-private address space `loopback`
```

Isso vale para **qualquer** agente local, não só este — o `360-serverLocal` na `:3500` caía na
mesma regra. Confirmado em campo: o mesmo ambiente que falhava em `http://` passou a funcionar em
`https://`.

O agente já responde `Access-Control-Allow-Private-Network: true` no preflight, que é a parte que
cabe a ele. Mas isso **não substitui** o HTTPS na origem.

Para uma frota de cartórios, vale conhecer a **política empresarial do Chrome** que pré-autoriza
origens para acesso à rede local: sem ela, cada atendente vê o prompt de permissão, e quem negar
fica com impressão e digitalização falhando em silêncio.

## Configuração

`config.json`, ao lado do executável. Criado pelo instalador; se faltar, o agente
cria com os padrões.

```json
{
  "porta": 5150,
  "msManagementUrl": "https://www.cartorio360.com.br:14193",
  "pastaScans": "",
  "impressoraZebra": ""
}
```

`pastaScans` vazio = subpasta `scans`. `impressoraZebra` vazio = detecta pelo
nome ou driver (`Zebra|ZDesigner|ZPL`); preencha só em estação com mais de uma
etiquetadora.

## Defeitos do sistema atual que esta implementação corrige

1. **`/uploadAnexo` não existia.** Três telas do front chamavam
   `http://localhost:3500/uploadAnexo`, que o ObserverUpload nunca serviu — 404
   silencioso, porque o `catch` só fazia `console.error`.
2. **Anexo de produção subia para o servidor de DEV.** O `api.service.js` tinha
   duas URLs fixas apontando para ambientes diferentes. Agora é uma base
   configurada na instalação.
3. **PNG velho sequestrava o upload de um PDF novo.** O `resolveUploadPath`
   sempre tentava `doc_ficha.png` primeiro, sem olhar data; uma digitalização em
   imagem que ficasse sem subir fazia a próxima, em PDF, enviar o PNG antigo.
   Agora o `/upload` usa a digitalização registrada em memória — não há caminho a
   adivinhar.
4. **Falha de upload apagava o arquivo.** O `finally` do ObserverUpload apagava
   mesmo quando o envio falhava, e a ficha escaneada se perdia. Agora o arquivo
   só é apagado após sucesso, e o atendente pode repetir sem reescanear.
5. **Lixo acumulado.** As páginas intermediárias `Page{N}_front.png` nunca eram
   apagadas no modo imagem.
6. **Rotação não ia para o arquivo.** O botão girava só a imagem na tela; o
   arquivo subia torto do mesmo jeito.

## O que foi preservado de propósito

Toda a negociação da propriedade **3088** (`WIA_DPS_DOCUMENT_HANDLING_SELECT`),
incluindo a lista de valores por fabricante e o caminho específico do **Canon
DR-C230**. Isso foi ajustado contra equipamento real (commit `4a127a9` do
`WiaMultiPageScan`) e não se reconstrói por raciocínio — é o primeiro ponto a
testar com scanner físico.

O diagnóstico de duplex, que no original ia para `Console.WriteLine` e num WinExe
não chegava a lugar nenhum, agora vai para o log — é justamente o que se quer ler
quando um scanner não duplexa no cartório.

## Pendências

- **Testar digitalização com scanner físico**, com atenção ao duplex no DR-C230.
- **Compilar o instalador** com o Inno Setup e testá-lo numa VM limpa e numa
  máquina com a instalação antiga.
- **Ajustar o front** (`.env`, `nuxt.config.ts` e os arquivos que hoje apontam
  para `:3500` e `:5000/run-scanner`) — ver a Etapa 3 do plano.
- **Assinatura de código**: o executável não é assinado. O gatilho para avaliar a
  compra do certificado é o antivírus, não o SmartScreen — ver o plano.

