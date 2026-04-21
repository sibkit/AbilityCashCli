using System.Globalization;
using System.Text;

namespace AbilityCashCli.Import.BankStatements;

public sealed class AlfaBankImporter
{
    private static readonly string[] MarkerColumns =
        ["statement_unid", "Rch", "sum_rur", "d_c", "text70"];

    private static readonly Encoding Cp1251 = Encoding.GetEncoding(1251);
    private static readonly CultureInfo RuRu = CultureInfo.GetCultureInfo("ru-RU");
    private const string DateFormat = "dd.MM.yyyy";

    public static bool HasHeader(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream, Cp1251);
            var line = reader.ReadLine();
            if (line is null) return false;
            var cols = line.Split('\t');
            foreach (var m in MarkerColumns)
                if (Array.IndexOf(cols, m) < 0)
                    return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<AlfaBankRecord> Read(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream, Cp1251);

        var header = reader.ReadLine()
            ?? throw new InvalidOperationException("Пустой файл.");
        var cols = header.Split('\t');
        var idx = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < cols.Length; i++)
            idx[cols[i]] = i;

        foreach (var m in MarkerColumns)
            if (!idx.ContainsKey(m))
                throw new InvalidOperationException($"В шапке отсутствует колонка '{m}'.");

        reader.ReadLine();

        var records = new List<AlfaBankRecord>();
        string? singleRch = null;
        string? line;
        var rowNum = 2;
        while ((line = reader.ReadLine()) is not null)
        {
            rowNum++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = line.Split('\t');

            var type = Get(fields, idx, "Type");
            var dc = Get(fields, idx, "d_c");
            var rch = Get(fields, idx, "Rch");

            if (!string.Equals(type, "RUR", StringComparison.Ordinal))
                throw new InvalidOperationException($"Строка {rowNum}: валюта '{type}' не RUR.");
            if (dc != "D" && dc != "C")
                throw new InvalidOperationException($"Строка {rowNum}: d_c '{dc}' не D/C.");

            if (singleRch is null)
                singleRch = rch;
            else if (!string.Equals(singleRch, rch, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Строка {rowNum}: Rch '{rch}' не совпадает с '{singleRch}'.");

            var date = DateTime.ParseExact(Get(fields, idx, "Date"), DateFormat, RuRu);
            var amount = decimal.Parse(Get(fields, idx, "sum_rur"), NumberStyles.Number, RuRu);
            var number = Get(fields, idx, "number");
            var oDate = DateTime.ParseExact(Get(fields, idx, "o_date"), DateFormat, RuRu);
            var text70 = Get(fields, idx, "text70");

            string cname, cinn, cacc;
            if (dc == "D")
            {
                cname = Get(fields, idx, "pol_name");
                cinn = Get(fields, idx, "Pol_inn");
                cacc = Get(fields, idx, "pol_acc");
            }
            else
            {
                cname = Get(fields, idx, "plat_name");
                cinn = Get(fields, idx, "plat_inn");
                cacc = Get(fields, idx, "plat_acc");
            }

            records.Add(new AlfaBankRecord(
                date, dc, amount, number, oDate, text70, rch, type, cname, cinn, cacc));
        }

        return records;
    }

    private static string Get(string[] fields, Dictionary<string, int> idx, string col)
    {
        var i = idx[col];
        return i < fields.Length ? fields[i].Trim() : "";
    }
}
