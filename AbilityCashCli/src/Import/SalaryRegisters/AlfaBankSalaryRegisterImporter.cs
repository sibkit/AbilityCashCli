using System.Globalization;
using System.Text.RegularExpressions;
using ExcelDataReader;

namespace AbilityCashCli.Import.SalaryRegisters;

public sealed class AlfaBankSalaryRegisterImporter : IImporter
{
    private static readonly IReadOnlyList<string> RequiredHeaders = new[]
    {
        "№№", "ФИО работника", "№ текущего счета", "Сумма перевода", "Сумма удержанных средств"
    };

    private static readonly Regex QuotedText = new("[\"«]([^\"»]+)[\"»]", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);

    private readonly PersonNameNormalizer _nameNormalizer;

    public AlfaBankSalaryRegisterImporter(PersonNameNormalizer nameNormalizer)
    {
        _nameNormalizer = nameNormalizer;
    }

    public IReadOnlyList<ImportRecord> Read(string path)
    {
        var rows = ReadAllRows(path);

        var headerIdx = FindHeaderIndex(rows);
        if (headerIdx < 0)
            throw new InvalidOperationException($"В файле {path} не найдена шапка реестра Альфа-Банка.");

        var numCol = FindColumn(rows[headerIdx], "№№");
        var fioCol = FindColumn(rows[headerIdx], "ФИО работника");
        var amountCol = FindColumn(rows[headerIdx], "Сумма перевода");
        if (numCol < 0 || fioCol < 0 || amountCol < 0)
            throw new InvalidOperationException($"В шапке {path} не определены обязательные колонки.");

        var regNumber = FindValueAfterMarker(rows, "Реестр №", headerIdx)
                        ?? FindValueAfterMarker(rows, "Реестр", headerIdx)
                        ?? "";
        var enterpriseHint = ExtractEnterpriseHint(rows, headerIdx);
        var date = FindDateAfterMarker(rows, "от", headerIdx)
                   ?? FindAnyDate(rows, headerIdx)
                   ?? throw new InvalidOperationException($"В файле {path} не найдена дата реестра.");
        var purpose = FindValueAfterMarker(rows, "Назначение платежа", headerIdx) ?? "";
        var comment = $"{purpose} | реестр {regNumber}";

        var records = new List<ImportRecord>();
        for (var r = headerIdx + 1; r < rows.Count; r++)
        {
            var row = rows[r];
            var numRaw = GetCell(row, numCol);
            if (IsItogo(row))
                break;
            if (numRaw is null) continue;
            if (!IsPositiveInt(numRaw)) continue;

            var amountRaw = GetCell(row, amountCol);
            if (amountRaw is null) continue;
            if (!TryParseDecimal(amountRaw, out var amount)) continue;

            var fioRaw = NormalizeString(GetCell(row, fioCol));
            if (string.IsNullOrEmpty(fioRaw)) continue;
            var fio = _nameNormalizer.Normalize(fioRaw);
            if (string.IsNullOrEmpty(fio)) continue;

            records.Add(new ImportRecord(date, amount, fio, comment, enterpriseHint));
        }

        return records;
    }

    private static List<object?[]> ReadAllRows(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var rows = new List<object?[]>();
        while (reader.Read())
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
                row[i] = reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    private static int FindHeaderIndex(List<object?[]> rows)
    {
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var matched = 0;
            foreach (var marker in RequiredHeaders)
                if (FindColumn(row, marker) >= 0) matched++;
            if (matched == RequiredHeaders.Count) return r;
        }
        return -1;
    }

    private static int FindColumn(object?[] row, string marker)
    {
        for (var c = 0; c < row.Length; c++)
        {
            var s = NormalizeHeaderCell(row[c]);
            if (string.IsNullOrEmpty(s)) continue;
            if (s.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return -1;
    }

    private static string NormalizeHeaderCell(object? value)
    {
        var s = NormalizeString(value);
        return string.IsNullOrEmpty(s) ? s : WhitespaceRun.Replace(s, " ");
    }

    private static bool MatchesMarker(string cell, string marker) =>
        string.Equals(StripTrailingColon(cell), marker, StringComparison.OrdinalIgnoreCase);

    private static string StripTrailingColon(string s) =>
        s.EndsWith(':') ? s[..^1].TrimEnd() : s;

    private static string? FindValueAfterMarker(List<object?[]> rows, string marker, int maxRowExclusive)
    {
        for (var r = 0; r < Math.Min(maxRowExclusive, rows.Count); r++)
        {
            var row = rows[r];
            for (var c = 0; c < row.Length; c++)
            {
                var s = NormalizeHeaderCell(row[c]);
                if (string.IsNullOrEmpty(s)) continue;
                if (!MatchesMarker(s, marker)) continue;
                for (var c2 = c + 1; c2 < row.Length; c2++)
                {
                    var v = NormalizeString(row[c2]);
                    if (!string.IsNullOrEmpty(v)) return v;
                }
            }
        }
        return null;
    }

    private static DateTime? FindDateAfterMarker(List<object?[]> rows, string marker, int maxRowExclusive)
    {
        for (var r = 0; r < Math.Min(maxRowExclusive, rows.Count); r++)
        {
            var row = rows[r];
            for (var c = 0; c < row.Length; c++)
            {
                var s = NormalizeHeaderCell(row[c]);
                if (string.IsNullOrEmpty(s)) continue;
                if (!MatchesMarker(s, marker)) continue;
                for (var c2 = c + 1; c2 < row.Length; c2++)
                {
                    if (row[c2] is null) continue;
                    if (TryParseDate(row[c2]!, out var d)) return d;
                }
            }
        }
        return null;
    }

    private static DateTime? FindAnyDate(List<object?[]> rows, int maxRowExclusive)
    {
        for (var r = 0; r < Math.Min(maxRowExclusive, rows.Count); r++)
        {
            var row = rows[r];
            foreach (var cell in row)
            {
                if (cell is DateTime dt) return dt;
                if (cell is string s && TryParseDate(s, out var d)) return d;
            }
        }
        return null;
    }

    private static string? ExtractEnterpriseHint(List<object?[]> rows, int maxRowExclusive)
    {
        for (var r = 0; r < Math.Min(maxRowExclusive, rows.Count); r++)
        {
            var row = rows[r];
            var buf = string.Join(" ",
                row.Select(NormalizeString).Where(s => !string.IsNullOrEmpty(s)));
            if (string.IsNullOrEmpty(buf)) continue;
            if (buf.Contains("Общество", StringComparison.OrdinalIgnoreCase)
                || buf.Contains("ответственностью", StringComparison.OrdinalIgnoreCase))
            {
                var m = QuotedText.Match(buf);
                if (m.Success) return m.Groups[1].Value.Trim();
                return buf;
            }
        }
        return null;
    }

    private static bool IsItogo(object?[] row)
    {
        foreach (var cell in row)
        {
            var s = NormalizeString(cell);
            if (string.IsNullOrEmpty(s)) continue;
            if (s.StartsWith("Итого", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static object? GetCell(object?[] row, int col) =>
        col >= 0 && col < row.Length ? row[col] : null;

    private static bool IsPositiveInt(object value)
    {
        switch (value)
        {
            case double d: return d > 0 && d == Math.Floor(d);
            case int i: return i > 0;
            case long l: return l > 0;
            case decimal dec: return dec > 0 && dec == Math.Floor(dec);
            case string s when int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v):
                return v > 0;
            default: return false;
        }
    }

    private static bool TryParseDate(object value, out DateTime date)
    {
        switch (value)
        {
            case DateTime dt: date = dt; return true;
            case double d: date = DateTime.FromOADate(d); return true;
            case int i: date = DateTime.FromOADate(i); return true;
            case string s when DateTime.TryParse(s.Trim(), CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None, out var v):
                date = v; return true;
            case string s when DateTime.TryParse(s.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var v):
                date = v; return true;
            default: date = default; return false;
        }
    }

    private static bool TryParseDecimal(object value, out decimal amount)
    {
        switch (value)
        {
            case decimal dec: amount = dec; return true;
            case double d: amount = (decimal)d; return true;
            case int i: amount = i; return true;
            case long l: amount = l; return true;
            case string s when decimal.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v):
                amount = v; return true;
            case string s when decimal.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.GetCultureInfo("ru-RU"), out var v):
                amount = v; return true;
            default: amount = 0; return false;
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
