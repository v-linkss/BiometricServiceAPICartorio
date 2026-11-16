// Sonda TWAIN - responde UMA pergunta: este scanner deixa ligar duplex por TWAIN?
//
// Existe porque o WIA nao deixa. Medimos, no driver do Canon DR-C230:
//   - 3086 declara FEEDER+DUPLEX+FRONT_ONLY
//   - 3088 declara aceitar {1 FEEDER, 4 DUPLEX, 32 FRONT_ONLY}
//   - e recusa QUALQUER escrita que mude o valor (E_POINTER), inclusive 32
//   - o valor 5 (FEEDER|DUPLEX) grava, mas a transferencia morre com E_UNEXPECTED
// Ou seja: o WIA anuncia duplex e nao entrega.
//
// Rodar:  dotnet run           -> sonda de duplex
//         dotnet run -- api    -> dump da API de capacidades
//         dotnet run -- api2   -> dump da API de transferencia
using NTwain;
using NTwain.Data;

if (args.Length > 0 && args[0] == "api")  { ApiDump.Capacidades(); return 0; }
if (args.Length > 0 && args[0] == "api2") { ApiDump.Transferencia(); return 0; }

Console.WriteLine("=== Sonda TWAIN - Agente 360 ===");
Console.WriteLine($"Processo: {(Environment.Is64BitProcess ? "x64" : "x86")}  " +
                  "(data sources de 32 bits so carregam em processo x86)");
Console.WriteLine();

var session = new TwainSession(DataGroups.Image);

var rc = session.Open();
if (rc != ReturnCode.Success)
{
    Console.WriteLine($"FALHOU ao abrir o DSM: {rc}");
    return 1;
}
Console.WriteLine($"DSM aberto (estado {session.State}).");

try
{
    var fontes = session.GetSources().ToList();
    Console.WriteLine($"\nData sources encontrados: {fontes.Count}");
    foreach (var f in fontes)
        Console.WriteLine($"  - {f.Name}   [{f.Manufacturer}] protocolo {f.ProtocolVersion}");

    if (fontes.Count == 0) { Console.WriteLine("\nNenhum data source."); return 1; }

    var alvo = fontes.FirstOrDefault(f => f.Name.Contains("Canon", StringComparison.OrdinalIgnoreCase)
                                       || f.Name.Contains("DR-C", StringComparison.OrdinalIgnoreCase))
               ?? fontes[0];

    Console.WriteLine($"\n--- Abrindo: {alvo.Name} ---");
    rc = alvo.Open();
    if (rc != ReturnCode.Success) { Console.WriteLine($"FALHOU ao abrir: {rc}"); return 1; }

    var caps = alvo.Capabilities;
    bool conseguiuLigar = false;

    Console.WriteLine("\n[CAP_DUPLEX] - capacidade declarada pelo hardware");
    Relatar(() =>
    {
        var c = caps.CapDuplex;
        Console.WriteLine($"  CanGet={c.CanGet}  CanSet={c.CanSet}");
        if (c.CanGet) Console.WriteLine($"  atual: {c.GetCurrent()}");
        if (c.CanGet) Console.WriteLine($"  valores: {string.Join(", ", c.GetValues())}");
    });

    Console.WriteLine("\n[CAP_DUPLEXENABLED] - o interruptor do duplex");
    Relatar(() =>
    {
        var c = caps.CapDuplexEnabled;
        Console.WriteLine($"  CanGet={c.CanGet}  CanSet={c.CanSet}");
        if (c.CanGet) Console.WriteLine($"  atual: {c.GetCurrent()}");
        if (!c.CanSet) { Console.WriteLine("  >> NAO e gravavel. Mesmo teto do WIA."); return; }

        var r = c.SetValue(BoolType.True);
        Console.WriteLine($"  SetValue(True) -> {r}");
        if (r is ReturnCode.Success or ReturnCode.CheckStatus)
        {
            var agora = c.GetCurrent();
            Console.WriteLine($"  releitura: {agora}");
            conseguiuLigar = agora == BoolType.True;
        }
    });

    Console.WriteLine("\n[CAP_FEEDERENABLED] - alimentador");
    Relatar(() =>
    {
        var c = caps.CapFeederEnabled;
        Console.WriteLine($"  CanGet={c.CanGet}  CanSet={c.CanSet}");
        if (c.CanGet) Console.WriteLine($"  atual: {c.GetCurrent()}");
        if (c.CanSet) Console.WriteLine($"  SetValue(True) -> {c.SetValue(BoolType.True)}");
    });

    Console.WriteLine("\n[CAP_FEEDERLOADED] - tem papel?");
    Relatar(() => { var c = caps.CapFeederLoaded; if (c.CanGet) Console.WriteLine($"  atual: {c.GetCurrent()}"); });

    Console.WriteLine("\n==================== VEREDITO ====================");
    Console.WriteLine(conseguiuLigar
        ? "DUPLEX LIGOU POR TWAIN. Trocar a camada de aquisicao resolve."
        : "Nao foi possivel ligar o duplex por TWAIN.");
    Console.WriteLine("==================================================");

    alvo.Close();
    return conseguiuLigar ? 0 : 2;
}
finally
{
    session.Close();
}

static void Relatar(Action acao)
{
    try { acao(); }
    catch (Exception ex) { Console.WriteLine($"  ERRO: {ex.GetType().Name}: {ex.Message}"); }
}
