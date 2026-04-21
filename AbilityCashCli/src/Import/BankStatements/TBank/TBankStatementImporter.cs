using System.Globalization;
using System.Text;

namespace AbilityCashCli.Import.BankStatements.TBank;

public sealed class TBankStatementImporter
{
    private static readonly string[] MarkerColumns =
    [
        "Номер счёта",
        "Тип операции (пополнение/списание)",
        "Дата проведения",
        "Сумма в валюте счёта",
        "Назначение платежа"
    ];

    private const char Separator = ';';
    private const string DateFormat = "dd.MM.yyyy";
    private const string AccountCurrencyRub = "643";
    private static readonly CultureInfo RuRu = CultureInfo.GetCultureInfo("ru-RU");

    public static bool HasHeader(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var line = reader.ReadLine();
            if (line is null) return false;
            var cols = SplitLine(line);
            foreach (var m in MarkerColumns)
                if (cols.IndexOf(m) < 0)
                    return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<TBankStatementRecord> Read(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var header = reader.ReadLine()
            ?? throw new InvalidOperationException("Пустой файл.");
        var cols = SplitLine(header);
        var idx = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < cols.Count; i++)
            idx[cols[i]] = i;

        foreach (var m in MarkerColumns)
            if (!idx.ContainsKey(m))
                throw new InvalidOperationException($"В шапке отсутствует колонка '{m}'.");

        var records = new List<TBankStatementRecord>();
        string? singleAccount = null;
        string? line;
        var rowNum = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            rowNum++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = SplitLine(line);

            var account = Get(fields, idx, "Номер счёта");
            var dcRaw = Get(fields, idx, "Тип операции (пополнение/списание)");
            var currency = Get(fields, idx, "Валюта счёта");

            if (singleAccount is null)
                singleAccount = account;
            else if (!string.Equals(singleAccount, account, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Строка {rowNum}: 'Номер счёта' '{account}' не совпадает с '{singleAccount}'.");

            if (!string.Equals(currency, AccountCurrencyRub, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Строка {rowNum}: 'Валюта счёта' '{currency}' не {AccountCurrencyRub}.");

            string dc;
            if (string.Equals(dcRaw, "Кредит", StringComparison.Ordinal)) dc = "C";
            else if (string.Equals(dcRaw, "Дебет", StringComparison.Ordinal)) dc = "D";
            else throw new InvalidOperationException(
                $"Строка {rowNum}: 'Тип операции' '{dcRaw}' не Кредит/Дебет.");

            var date = DateTime.ParseExact(Get(fields, idx, "Дата проведения"), DateFormat, RuRu);
            var amount = decimal.Parse(Get(fields, idx, "Сумма в валюте счёта"), NumberStyles.Number, RuRu);
            var number = Get(fields, idx, "Номер платежа");
            var description = Get(fields, idx, "Описание операции");
            var purpose = Get(fields, idx, "Назначение платежа");

            string cname, cinn, cacc;
            if (dc == "C")
            {
                cname = Get(fields, idx, "Наименование плательщика");
                cinn = Get(fields, idx, "ИНН плательщика");
                cacc = Get(fields, idx, "Счет плательщика");
            }
            else
            {
                cname = Get(fields, idx, "Наименование получателя");
                cinn = Get(fields, idx, "ИНН получателя");
                cacc = Get(fields, idx, "Счет получателя");
            }

            records.Add(new TBankStatementRecord(
                date, dc, amount, number, account, description, purpose, cname, cinn, cacc));
        }

        return records;
    }

    private static string Get(IReadOnlyList<string> fields, Dictionary<string, int> idx, string col)
    {
        if (!idx.TryGetValue(col, out var i)) return "";
        return i < fields.Count ? fields[i].Trim() : "";
    }

    private static List<string> SplitLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"')
                    inQuotes = true;
                else if (c == Separator)
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                    sb.Append(c);
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }
}
