using System.Runtime.InteropServices;
using WIA;

namespace Cartorio360.Agente.Scanner;

/// <summary>
/// Aquisicao via WIA. E o caminho de simplex e multipagina, e a reserva quando
/// nao ha data source TWAIN na estacao.
///
/// O duplex vive no TwainAquisicao, nao aqui. A tentativa por WIA continua
/// existindo — algum scanner pode ter driver WIA melhor que o do DR-C230 — mas
/// deliberadamente enxuta: so os valores que o driver DECLARA aceitar, sem lista
/// de palpites por fabricante e sem a segunda transferencia manual que o
/// WiaMultiPageScan fazia para Canon. Aquele andaime foi construido para fazer o
/// DR-C230 duplexar, e a investigacao provou que ele nao duplexa por WIA em
/// hipotese nenhuma: a propriedade 3088 daquele driver recusa QUALQUER mudanca de
/// valor, inclusive FRONT_ONLY. As medicoes estao no README.
/// </summary>
public sealed class WiaAquisicao
{
    // WIA_IPS_* (item): imagem.       WIA_DPS_* (dispositivo): alimentacao.
    private const int PropModoCor = 6146, PropDpiH = 6147, PropDpiV = 6148;
    private const int PropLargura = 6151, PropAltura = 6152;
    private const int PropAlimentacao = 3088;   // DOCUMENT_HANDLING_SELECT
    private const int FlagDuplex = 0x004;
    private const int ValorFeeder = 1;
    private const string FormatoJpeg = "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}";
    private const int Dpi = 150;

    private readonly Device _scanner;

    public WiaAquisicao(Device scanner) => _scanner = scanner;

    public sealed record Resultado(IReadOnlyList<string> Arquivos, string? Erro, string? Aviso);

    public Resultado Digitalizar(string pastaSaida, bool duplex, bool multiplasPaginas)
    {
        var arquivos = new List<string>();
        string? erro = null, aviso = null;

        var dialogo = new WIA.CommonDialog();   // qualificado: existe em WinForms tambem
        try
        {
            ConfigurarImagem();

            bool duplexLigado = duplex && LigarDuplex();
            if (duplex && !duplexLigado)
                aviso = "Este scanner não aceitou o modo frente e verso pelo WIA. " +
                        "A digitalização foi feita apenas da frente das páginas.";

            Directory.CreateDirectory(pastaSaida);
            erro = Transferir(dialogo, pastaSaida, multiplasPaginas, duplex, arquivos);
        }
        catch (Exception ex)
        {
            Log.Erro("WIA: falha na digitalizacao", ex);
            erro = $"Falha na digitalização: {ex.Message}";
        }
        finally
        {
            try { Marshal.ReleaseComObject(dialogo); } catch { }
        }

        return new Resultado(arquivos, erro, aviso);
    }

    /// <summary>
    /// Cor, resolucao e A4. Gravado por let_Value: com set_Value, que era o que o
    /// WiaMultiPageScan usava, estas atribuicoes falhavam em silencio e o scanner
    /// digitalizava com a configuracao dele em vez da pedida aqui.
    /// </summary>
    private void ConfigurarImagem()
    {
        foreach (Property prop in _scanner.Items[1].Properties)
        {
            try
            {
                switch (prop.PropertyID)
                {
                    case PropModoCor: Definir(prop, 1); break;
                    case PropDpiH:
                    case PropDpiV: Definir(prop, Dpi); break;
                    case PropLargura: Definir(prop, (int)(8.27 * Dpi)); break;
                    case PropAltura: Definir(prop, (int)(11.69 * Dpi)); break;
                }
            }
            catch (Exception ex)
            {
                Log.Aviso($"WIA: nao consegui definir a propriedade {prop.PropertyID}: {ex.Message}");
            }
        }
    }

    private bool LigarDuplex()
    {
        var prop = Achar(_scanner.Properties, PropAlimentacao)
                ?? Achar(_scanner.Items[1].Properties, PropAlimentacao);

        if (prop == null) { Log.Aviso("WIA: propriedade 3088 nao encontrada"); return false; }

        // So os valores que o driver declara aceitar, e so os que trazem o bit
        // DUPLEX. Gravar valor fora da lista e o que fazia o DR-C230 aceitar a
        // escrita e depois morrer na transferencia com E_UNEXPECTED.
        var candidatos = ValoresAceitos(prop)?.Where(v => (v & FlagDuplex) != 0).ToArray() ?? Array.Empty<int>();
        if (candidatos.Length == 0) { Log.Aviso("WIA: scanner nao declara modo com duplex"); return false; }

        foreach (int valor in candidatos)
        {
            try
            {
                Definir(prop, valor);
                int lido = Convert.ToInt32(prop.get_Value());
                if ((lido & FlagDuplex) == FlagDuplex)
                {
                    Log.Info($"WIA: duplex habilitado (valor {lido})");
                    return true;
                }
                Log.Aviso($"WIA: valor {valor} aceito mas sem flag DUPLEX (leu {lido})");
            }
            catch (Exception ex)
            {
                // NullReferenceException aqui costuma ser E_POINTER do driver, e
                // nao erro nosso.
                Log.Aviso($"WIA: valor {valor} recusado: {ex.Message}");
            }
        }

        // Deixar a propriedade num estado valido: um valor estranho gravado numa
        // tentativa ficaria la e derrubaria a digitalizacao seguinte.
        try { Definir(prop, ValorFeeder); } catch { }
        return false;
    }

    /// <summary>Devolve a mensagem de erro, ou null se terminou bem.</summary>
    private string? Transferir(WIA.CommonDialog dialogo, string pastaSaida,
                               bool multiplasPaginas, bool duplexSolicitado, List<string> arquivos)
    {
        int pagina = 0;

        while (true)
        {
            try
            {
                var imagem = (ImageFile?)dialogo.ShowTransfer(_scanner.Items[1], FormatoJpeg, false);
                if (imagem == null) break;

                string caminho = Path.Combine(pastaSaida, $"Page{++pagina}.png");
                imagem.SaveFile(caminho);
                Marshal.ReleaseComObject(imagem);

                if (Imagens.EstaEmBranco(caminho))
                {
                    Imagens.Apagar(new[] { caminho });
                    pagina--;
                }
                else
                {
                    Imagens.Redimensionar(caminho);
                    arquivos.Add(caminho);
                    Log.Info($"WIA: pagina {pagina} escaneada");
                }

                if (!multiplasPaginas) break;
            }
            catch (COMException ex)
            {
                uint codigo = (uint)ex.ErrorCode;
                Log.Aviso($"WIA: erro durante scan (0x{codigo:X8}): {ex.Message}");

                if (ErrosWia.EhFimNormal(codigo, arquivos.Count))
                {
                    Log.Info("WIA: fim do documento");
                    break;
                }
                return ErrosWia.Mensagem(codigo, duplexSolicitado);
            }
            catch (Exception ex)
            {
                Log.Erro("WIA: erro geral durante scan", ex);
                return $"Falha na digitalização: {ex.Message}";
            }
        }

        return null;
    }

    // ------------------------------------------------------------- utilitarios

    private static Property? Achar(Properties colecao, int id)
    {
        try
        {
            foreach (Property p in colecao)
                if (p.PropertyID == id) return p;
        }
        catch (Exception ex) { Log.Aviso($"WIA: falha ao varrer propriedades procurando {id}: {ex.Message}"); }
        return null;
    }

    private static List<int>? ValoresAceitos(Property prop)
    {
        try
        {
            var valores = prop.SubTypeValues;
            if (valores == null || valores.Count == 0) return null;

            var lista = new List<int>();
            for (int i = 1; i <= valores.Count; i++)
            {
                try { lista.Add(Convert.ToInt32(valores.get_Item(i))); } catch { }
            }
            return lista.Count > 0 ? lista : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// Grava valor numa propriedade WIA.
    ///
    /// let_Value e NAO set_Value: a interface expoe os dois, herdados da semantica
    /// do VB — Let atribui VALOR, Set atribui REFERENCIA DE OBJETO. Para um
    /// inteiro o correto e o primeiro. O WiaMultiPageScan usava set_Value em tudo.
    /// Mantemos set_Value como ultimo recurso, para driver que so responda a ele.
    /// </summary>
    private static void Definir(Property prop, int valor)
    {
        object caixa = valor;
        try
        {
            prop.let_Value(ref caixa);
            return;
        }
        catch (Exception ex)
        {
            Log.Aviso($"WIA: let_Value({valor}) falhou em {prop.PropertyID}: {ex.Message}; tentando set_Value");
        }

        object caixa2 = valor;
        prop.set_Value(ref caixa2);
    }
}
