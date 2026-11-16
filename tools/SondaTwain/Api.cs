// Dump da API do NTwain, para escrever a camada de aquisicao sem adivinhar.
using System.Reflection;

static class ApiDump
{
    private static readonly Assembly Asm = typeof(NTwain.TwainSession).Assembly;

    public static void Capacidades()
    {
        var icap = Asm.GetExportedTypes().First(x => x.Name == "ICapabilities");
        Console.WriteLine("### ICapabilities - duplex / alimentador");
        Listar(icap, n => n.Contains("Duplex") || n.Contains("Feeder"));

        Console.WriteLine("\n### ICapabilities - imagem e transferencia");
        Listar(icap, n => n.Contains("Xfer") || n.Contains("Resolution") || n.Contains("PixelType")
                       || n.Contains("Compression") || n.Contains("Units") || n.Contains("Supported"));
    }

    public static void Transferencia()
    {
        // TWIdentity: como montar a identidade da aplicacao SEM depender de
        // Assembly.Location, que em publish single-file vem vazio e estoura
        // ArgumentException("The path is empty").
        var ident = Asm.GetExportedTypes().FirstOrDefault(x => x.Name == "TWIdentity");
        if (ident != null)
        {
            Console.WriteLine($"### {ident.FullName} - membros estaticos");
            foreach (var m in ident.GetMethods(BindingFlags.Public | BindingFlags.Static))
                Console.WriteLine($"  M  {m.ReturnType.Name} {m.Name}({string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
            foreach (var c in ident.GetConstructors())
                Console.WriteLine($"  C  .ctor({string.Join(", ", c.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
            Console.WriteLine("  --- propriedades gravaveis ---");
            foreach (var p in ident.GetProperties().Where(p => p.CanWrite))
                Console.WriteLine($"  P  {p.PropertyType.Name} {p.Name}");
        }

        var tver = Asm.GetExportedTypes().FirstOrDefault(x => x.Name == "TWVersion");
        if (tver != null)
        {
            Console.WriteLine($"\n### {tver.FullName} - propriedades gravaveis");
            foreach (var p in tver.GetProperties().Where(p => p.CanWrite))
                Console.WriteLine($"  P  {p.PropertyType.Name} {p.Name}");
        }

        foreach (var nome in new[] { "Language", "Country", "DataGroups" })
        {
            var t = Asm.GetExportedTypes().FirstOrDefault(x => x.Name == nome);
            if (t is { IsEnum: true })
                Console.WriteLine($"\n### {nome}: {string.Join(", ", Enum.GetNames(t).Take(12))} ...");
        }
    }

    private static void Listar(Type icap, Func<string, bool> filtro)
    {
        foreach (var p in icap.GetProperties().Where(p => filtro(p.Name)).OrderBy(p => p.Name))
        {
            var arg = p.PropertyType.IsGenericType ? p.PropertyType.GetGenericArguments()[0].Name : p.PropertyType.Name;
            Console.WriteLine($"  {p.Name} -> CapWrapper<{arg}>");
        }
    }
}
