; ============================================================================
;  Agente 360 — instalador
;
;  Compilar com Inno Setup 6:
;     "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" CartorioAgente.iss
;  Antes, gerar o publish:
;     dotnet publish Cartorio360.Agente -c Release -o publish
;
;  O instalador NAO carrega dependencia de terceiro nenhuma. O agente e
;  self-contained (traz o proprio .NET), o scanner virou codigo interno (some o
;  .NET 6 Desktop Runtime que o 360scan.exe exigia) e a impressao passou a usar
;  o spooler (some o Zadig). Sobra copiar arquivo e agendar a tarefa.
; ============================================================================

#define MeuNome     "Agente 360"
#define MeuExe      "Cartorio360Agente.exe"
#define MinhaVersao "2.0.0"
#define MinhaEmpresa "Cartorio 360"
#define Tarefa      "Cartorio360 Agente"

[Setup]
AppId={{8F3A1C42-9D77-4E1B-B0A6-3C2E5D74A910}
AppName={#MeuNome}
AppVersion={#MinhaVersao}
AppPublisher={#MinhaEmpresa}
DefaultGroupName={#MeuNome}
OutputBaseFilename=CartorioAgente-Setup-{#MinhaVersao}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
; Icone do proprio instalador e da entrada em "Aplicativos instalados".
SetupIconFile=..\Cartorio360.Agente\cartorio_icon.ico
UninstallDisplayIcon={app}\{#MeuExe}
UninstallDisplayName={#MeuNome}
; O agente e x86 (por causa dos data sources TWAIN de 32 bits), entao NAO ha
; ArchitecturesAllowed: restringir a x64 recusaria instalar em Windows de 32
; bits sem motivo. Ja o InstallIn64BitMode fica, para que {sys} aponte para o
; System32 nativo quando a maquina for 64 bits — o schtasks e o icacls chamados
; no [Code] rodam melhor sem passar pela redirecao do WOW64.
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes

; O caminho e convencao da estacao, nao escolha de quem instala: o agente vive
; como irmao das pastas que ja existem em C:\cartorio360.
DefaultDirName={sd}\cartorio360\Agente360
DisableDirPage=yes

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Files]
Source: "..\publish\{#MeuExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MeuNome}"; Filename: "{app}\{#MeuExe}"
Name: "{group}\Desinstalar {#MeuNome}"; Filename: "{uninstallexe}"

[Run]
; runasoriginaluser e essencial: o instalador roda elevado e, sem esse flag, o
; agente herdaria privilegio de administrador. Nada nele precisa disso (WIA,
; spooler e Kestrel em porta alta rodam como usuario comum) e privilegio a mais
; atrapalha a integracao com a sessao interativa.
Filename: "{app}\{#MeuExe}"; Description: "Iniciar o {#MeuNome} agora"; \
    Flags: postinstall nowait skipifsilent runasoriginaluser

[UninstallRun]
Filename: "{sys}\schtasks.exe"; Parameters: "/delete /tn ""{#Tarefa}"" /f"; \
    Flags: runhidden; RunOnceId: "RemoverTarefa"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\scans"
Type: files;          Name: "{app}\config.json"

[Code]
var
  PaginaConfig: TInputQueryWizardPage;
  PaginaAmbiente: TInputOptionWizardPage;

const
  URL_PROD = 'https://www.cartorio360.com.br:14193';
  URL_DEV  = 'https://dev.cartorio360.com.br:14193';

procedure InitializeWizard;
begin
  PaginaAmbiente := CreateInputOptionPage(wpSelectDir,
    'Ambiente', 'Para qual servidor este cartório envia os documentos?',
    'Escolha o ambiente. Isto define para onde as digitalizações são enviadas.',
    True, False);
  PaginaAmbiente.Add('Produção  (' + URL_PROD + ')');
  PaginaAmbiente.Add('Desenvolvimento  (' + URL_DEV + ')');
  PaginaAmbiente.Add('Outro (informar na próxima tela)');
  PaginaAmbiente.SelectedValueIndex := 0;

  PaginaConfig := CreateInputQueryPage(PaginaAmbiente.ID,
    'Configuração do agente', 'Confirme os dados de conexão',
    'A porta só precisa mudar se já houver outro programa usando-a nesta estação.');
  PaginaConfig.Add('URL do servidor (MS_MANAGEMENT):', False);
  PaginaConfig.Add('Porta local do agente:', False);
  PaginaConfig.Values[0] := URL_PROD;
  PaginaConfig.Values[1] := '5150';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Porta: Integer;
begin
  Result := True;

  if CurPageID = PaginaAmbiente.ID then
  begin
    case PaginaAmbiente.SelectedValueIndex of
      0: PaginaConfig.Values[0] := URL_PROD;
      1: PaginaConfig.Values[0] := URL_DEV;
    end;
  end
  else if CurPageID = PaginaConfig.ID then
  begin
    if Pos('http', PaginaConfig.Values[0]) <> 1 then
    begin
      MsgBox('A URL do servidor precisa começar com http:// ou https://', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    Porta := StrToIntDef(PaginaConfig.Values[1], -1);
    if (Porta < 1024) or (Porta > 65535) then
    begin
      MsgBox('A porta precisa ser um número entre 1024 e 65535.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

// Encerra o que estiver rodando antes de copiar, senao o arquivo fica travado.
procedure PararProcessos;
var
  Codigo: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM Cartorio360Agente.exe',
       '', SW_HIDE, ewWaitUntilTerminated, Codigo);
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM 360-serverLocal.exe',
       '', SW_HIDE, ewWaitUntilTerminated, Codigo);
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM 360scan.exe',
       '', SW_HIDE, ewWaitUntilTerminated, Codigo);
end;

// Resgata digitalizacao que ficou sem subir antes da instalacao. Apagar sem mais
// faria o atendente escanear a ficha de novo.
procedure ResgatarPendencias;
var
  Origem, Destino: String;
begin
  Origem := ExpandConstant('{sd}\cartorio360\ScannerApp\Imagens\');
  Destino := ExpandConstant('{app}\scans\migrado\');

  if not DirExists(Origem) then Exit;

  ForceDirectories(Destino);
  if FileExists(Origem + 'doc_ficha.png') then
    CopyFile(Origem +'doc_ficha.png', Destino + 'doc_ficha.png', False);
  if FileExists(Origem + 'doc_ficha.pdf') then
    CopyFile(Origem +'doc_ficha.pdf', Destino + 'doc_ficha.pdf', False);
end;

// ScannerApp e RENOMEADO, nao apagado: se o WIA in-process engasgar com algum
// modelo de scanner que so aparece em campo, o 360scan.exe volta com um comando.
// Uma versao futura do instalador remove a pasta.
procedure AposentarAntigos;
var
  Antigo, Aposentado: String;
begin
  Antigo := ExpandConstant('{sd}\cartorio360\ScannerApp');
  Aposentado := ExpandConstant('{sd}\cartorio360\ScannerApp.desativado');
  if DirExists(Antigo) and not DirExists(Aposentado) then
    RenameFile(Antigo, Aposentado);

  Antigo := ExpandConstant('{sd}\cartorio360\360-serverLocal');
  if DirExists(Antigo) then
    DelTree(Antigo, True, True, True);

  // NAO tocar em BiometricServiceAPI-main: o leitor biometrico continua em uso.
  // NAO apagar C:\cartorio360: e casa das outras pastas.
end;

procedure GravarConfig;
var
  Json: String;
  Escapada: String;
begin
  Escapada := PaginaConfig.Values[0];
  StringChangeEx(Escapada, '\', '\\', True);

  Json :=
    '{' + #13#10 +
    '  "porta": ' + PaginaConfig.Values[1] + ',' + #13#10 +
    '  "msManagementUrl": "' + Escapada + '",' + #13#10 +
    '  "pastaScans": "",' + #13#10 +
    '  "impressoraZebra": ""' + #13#10 +
    '}' + #13#10;

  SaveStringToFile(ExpandConstant('{app}\config.json'), Json, False);
end;

// O agente roda como usuario comum e precisa gravar em scans\, logs\ e
// config.json. Numa raiz do C: endurecida por politica de dominio a heranca de
// permissao nao vem, e a falha so aparece com conta SEM privilegio de admin —
// justamente o cenario do atendente. Conceder explicitamente evita isso.
//   *S-1-5-11 = Usuarios autenticados, por SID para nao depender do idioma do
//   Windows: em portugues o grupo tem outro nome e o icacls falharia.
procedure ConcederPermissoes;
var
  Codigo: Integer;
begin
  ForceDirectories(ExpandConstant('{app}\logs'));
  ForceDirectories(ExpandConstant('{app}\scans'));
  Exec(ExpandConstant('{sys}\icacls.exe'),
       '"' + ExpandConstant('{app}') + '" /grant *S-1-5-11:(OI)(CI)M /T /C /Q',
       '', SW_HIDE, ewWaitUntilTerminated, Codigo);
end;

// Grava texto em UTF-16LE com BOM, que e o unico encoding que o schtasks aceita
// no /xml. O SaveStringToFile do Inno nao faz isso, entao vai por ADODB.Stream,
// que existe em qualquer Windows. O Charset 'unicode' do ADO e justamente
// UTF-16LE com BOM.
//
// Nao seto o Type: adTypeText (2) ja e o padrao do ADODB.Stream, e "Type" e
// palavra reservada no Pascal Script.
function GravarUtf16(const Caminho, Conteudo: String): Boolean;
var
  Stream: Variant;
begin
  Result := False;
  try
    Stream := CreateOleObject('ADODB.Stream');
    Stream.Charset := 'unicode';
    Stream.Open;
    Stream.WriteText(Conteudo);
    Stream.SaveToFile(Caminho, 2);   // 2 = adSaveCreateOverWrite
    Stream.Close;
    Result := FileExists(Caminho);
  except
    Log('falha ao gravar o XML em UTF-16: ' + GetExceptionMessage);
  end;
end;

// Tarefa agendada em vez da chave Run do registro: a chave so sabe iniciar, e a
// tarefa reinicia sozinha se o processo morrer e permite atrasar o arranque para
// nao competir com o boot.
procedure RegistrarTarefa;
var
  Xml, Caminho: String;
  Codigo: Integer;
begin
  Caminho := ExpandConstant('{tmp}\tarefa-agente360.xml');
  // A ordem dos elementos dentro de <Settings> NAO e livre: o schema de tarefa e
  // uma <sequence>, e o schtasks recusa o XML se a ordem divergir. Esta e a
  // ordem canonica — mexer aqui sem conferir o schema quebra o agendamento.
  //
  // Encoding: o schtasks EXIGE UTF-16. Medido nesta bancada, com o mesmo XML:
  //   UTF-8 sem BOM  -> "nao e possivel alternar codificacao" (coluna 40 = o
  //                     atributo encoding da primeira linha)
  //   UTF-8 com BOM  -> "sintaxe de documento incorreta"
  //   UTF-16 com BOM -> aceito
  // Como o SaveStringToFile do Inno nao grava UTF-16, a gravacao vai por
  // ADODB.Stream (ver GravarUtf16).
  Xml :=
    '<?xml version="1.0" encoding="UTF-16"?>' + #13#10 +
    '<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">' + #13#10 +
    '  <RegistrationInfo><Description>Mantem o Agente 360 no ar para scanner, upload e impressao de selos.</Description></RegistrationInfo>' + #13#10 +
    '  <Triggers><LogonTrigger><Enabled>true</Enabled><Delay>PT30S</Delay></LogonTrigger></Triggers>' + #13#10 +
    '  <Principals><Principal id="Author"><GroupId>S-1-5-32-545</GroupId><RunLevel>LeastPrivilege</RunLevel></Principal></Principals>' + #13#10 +
    '  <Settings>' + #13#10 +
    '    <AllowStartOnDemand>true</AllowStartOnDemand>' + #13#10 +
    '    <RestartOnFailure><Interval>PT1M</Interval><Count>3</Count></RestartOnFailure>' + #13#10 +
    '    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>' + #13#10 +
    '    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>' + #13#10 +
    '    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>' + #13#10 +
    '    <AllowHardTerminate>true</AllowHardTerminate>' + #13#10 +
    '    <StartWhenAvailable>true</StartWhenAvailable>' + #13#10 +
    '    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>' + #13#10 +
    '    <Enabled>true</Enabled>' + #13#10 +
    '    <Hidden>false</Hidden>' + #13#10 +
    '    <IdleSettings><StopOnIdleEnd>false</StopOnIdleEnd><RestartOnIdle>false</RestartOnIdle></IdleSettings>' + #13#10 +
    '    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>' + #13#10 +
    '    <Priority>7</Priority>' + #13#10 +
    '  </Settings>' + #13#10 +
    '  <Actions Context="Author"><Exec>' + #13#10 +
    '    <Command>' + ExpandConstant('{app}\{#MeuExe}') + '</Command>' + #13#10 +
    '    <WorkingDirectory>' + ExpandConstant('{app}') + '</WorkingDirectory>' + #13#10 +
    '  </Exec></Actions>' + #13#10 +
    '</Task>' + #13#10;

  Codigo := 1;
  if GravarUtf16(Caminho, Xml) then
    Exec(ExpandConstant('{sys}\schtasks.exe'),
         '/create /xml "' + Caminho + '" /tn "{#Tarefa}" /f',
         '', SW_HIDE, ewWaitUntilTerminated, Codigo);

  // Plano B: agendar por linha de comando. Perde o RestartOnFailure (o
  // Agendador so aceita isso por XML), mas garante que o agente suba no logon —
  // que e o essencial. Melhor um agendamento simples do que nenhum.
  if Codigo <> 0 then
  begin
    Exec(ExpandConstant('{sys}\schtasks.exe'),
         '/create /tn "{#Tarefa}" /tr "\"' + ExpandConstant('{app}\{#MeuExe}') + '\"" ' +
         '/sc ONLOGON /rl LIMITED /delay 0000:30 /f',
         '', SW_HIDE, ewWaitUntilTerminated, Codigo);
    if Codigo = 0 then
      Log('tarefa agendada pelo plano B (sem reinicio automatico em falha)');
  end;

  if Codigo <> 0 then
    MsgBox('Não foi possível agendar a inicialização automática (código ' + IntToStr(Codigo) + ').' + #13#10 +
           'O agente foi instalado, mas precisará ser aberto manualmente após cada logon.',
           mbInformation, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    PararProcessos;
  end
  else if CurStep = ssPostInstall then
  begin
    GravarConfig;
    ConcederPermissoes;
    ResgatarPendencias;
    AposentarAntigos;
    RegistrarTarefa;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Codigo: Integer;
begin
  if CurUninstallStep = usUninstall then
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM Cartorio360Agente.exe',
         '', SW_HIDE, ewWaitUntilTerminated, Codigo);
end;
