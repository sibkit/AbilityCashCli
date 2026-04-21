using ExcelDataReader;

namespace AbilityCashCli.Import;

public sealed class CashPayoutsImporter : IImporter
{
    public IReadOnlyList<ImportRecord> Read(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var records = new List<ImportRecord>();
        var headerSeen = false;
        int dateCol = -1, personCol = -1, amountCol = -1, commentCol = -1;

        while (reader.Read())
        {
            if (!headerSeen)
            {
                if (TryDetectHeader(reader, out dateCol, out personCol, out amountCol, out commentCol))
                    headerSeen = true;
                continue;
            }

            var rawDate = reader.GetValue(dateCol);
            if (rawDate is null) continue;

            if (!TryParseDate(rawDate, out var date))
                continue;

            var rawAmount = reader.GetValue(amountCol);
            if (rawAmount is null) continue;

            if (!TryParseDecimal(rawAmount, out var amount))
                continue;

            var person = PersonNameNormalizer.Normalize(NormalizeString(reader.GetValue(personCol)));
            var comment = commentCol >= 0 ? NormalizeString(reader.GetValue(commentCol)) : "";

            records.Add(new ImportRecord(date, amount, person, comment));
        }

        if (!headerSeen)
            throw new InvalidOperationException($"В файле {path} не найдена шапка (ожидались колонки 'Дата' и 'Сумма').");

        return records;
    }

    private static bool TryDetectHeader(IExcelDataReader reader, out int dateCol, out int personCol, out int amountCol, out int commentCol)
    {
        dateCol = personCol = amountCol = commentCol = -1;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var v = reader.GetValue(i) as string;
            if (string.IsNullOrWhiteSpace(v)) continue;
            var s = v.Trim().ToLowerInvariant();
            if (dateCol < 0 && s.StartsWith("дата")) dateCol = i;
            else if (amountCol < 0 && s.StartsWith("сумма")) amountCol = i;
            else if (personCol < 0 && (s.Contains("фио") || s.Contains("ф.и.о") || s.Contains("фамилия") || s.Contains("должность")))
                personCol = i;
        }

        if (dateCol < 0 || amountCol < 0) return false;

        if (personCol < 0)
        {
            for (var i = 0; i < reader.FieldCount; i++)
                if (i != dateCol && i != amountCol && reader.GetValue(i) is string { Length: > 0 })
                {
                    personCol = i;
                    break;
                }
        }

        for (var i = reader.FieldCount - 1; i >= 0; i--)
            if (i != dateCol && i != amountCol && i != personCol)
            {
                commentCol = i;
                break;
            }

        return personCol >= 0;
    }

    private static bool TryParseDate(object value, out DateTime date)
    {
        switch (value)
        {
            case DateTime dt:
                date = dt;
                return true;
            case double d:
                date = DateTime.FromOADate(d);
                return true;
            case int i:
                date = DateTime.FromOADate(i);
                return true;
            default:
                date = default;
                return false;
        }
    }

    private static bool TryParseDecimal(object value, out decimal amount)
    {
        switch (value)
        {
            case decimal dec:
                amount = dec;
                return true;
            case double d:
                amount = (decimal)d;
                return true;
            case int i:
                amount = i;
                return true;
            case long l:
                amount = l;
                return true;
            default:
                amount = 0;
                return false;
        }
    }

    private static string NormalizeString(object? value) =>
        value switch
        {
            null => "",
            string s => s.Trim(),
            _ => value.ToString()?.Trim() ?? ""
        };
}
