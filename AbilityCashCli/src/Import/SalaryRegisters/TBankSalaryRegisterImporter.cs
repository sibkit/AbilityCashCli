using System.Globalization;
using ExcelDataReader;

namespace AbilityCashCli.Import.SalaryRegisters;

public sealed class TBankSalaryRegisterImporter : IImporter
{
    public static readonly IReadOnlyList<string> RequiredHeaders = new[]
    {
        "Номер реестра", "Дата создания реестра", "Фамилия", "Имя", "Отчество",
        "Сумма", "Статус", "Назначение платежа"
    };

    private const string ExecutedStatus = "ИСПОЛНЕН";

    private readonly PersonNameNormalizer _nameNormalizer;

    public TBankSalaryRegisterImporter(PersonNameNormalizer nameNormalizer)
    {
        _nameNormalizer = nameNormalizer;
    }

    public IReadOnlyList<ImportRecord> Read(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var records = new List<ImportRecord>();
        var headerSeen = false;
        int regCol = -1, dateCol = -1, lastCol = -1, firstCol = -1, patrCol = -1,
            amountCol = -1, statusCol = -1, purposeCol = -1;

        while (reader.Read())
        {
            if (!headerSeen)
            {
                if (TryDetectHeader(reader,
                        out regCol, out dateCol, out lastCol, out firstCol, out patrCol,
                        out amountCol, out statusCol, out purposeCol))
                    headerSeen = true;
                continue;
            }

            var statusRaw = NormalizeString(reader.GetValue(statusCol));
            if (string.IsNullOrEmpty(statusRaw)) continue;
            if (!string.Equals(statusRaw, ExecutedStatus, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Строка со статусом '{statusRaw}' (ожидается '{ExecutedStatus}'): {path}.");

            var rawDate = reader.GetValue(dateCol);
            if (rawDate is null) continue;
            if (!TryParseDate(rawDate, out var date)) continue;

            var rawAmount = reader.GetValue(amountCol);
            if (rawAmount is null) continue;
            if (!TryParseDecimal(rawAmount, out var amount)) continue;

            var last = NormalizeString(reader.GetValue(lastCol));
            var first = NormalizeString(reader.GetValue(firstCol));
            var patr = NormalizeString(reader.GetValue(patrCol));
            var fio = _nameNormalizer.Normalize($"{last} {first} {patr}".Trim());
            if (string.IsNullOrEmpty(fio)) continue;

            var purpose = NormalizeString(reader.GetValue(purposeCol));
            var regNumber = NormalizeString(reader.GetValue(regCol));
            var comment = $"{purpose} | реестр {regNumber}";

            records.Add(new ImportRecord(date, amount, fio, comment));
        }

        if (!headerSeen)
            throw new InvalidOperationException($"В файле {path} не найдена шапка T-Банка.");

        return records;
    }

    public static bool HasHeader(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            while (reader.Read())
                if (TryDetectHeader(reader, out _, out _, out _, out _, out _, out _, out _, out _))
                    return true;
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDetectHeader(IExcelDataReader reader,
        out int regCol, out int dateCol, out int lastCol, out int firstCol, out int patrCol,
        out int amountCol, out int statusCol, out int purposeCol)
    {
        regCol = dateCol = lastCol = firstCol = patrCol = amountCol = statusCol = purposeCol = -1;

        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetValue(i) is not string s) continue;
            var v = s.Trim();
            if (string.IsNullOrEmpty(v)) continue;

            if (regCol < 0 && Eq(v, "Номер реестра")) regCol = i;
            else if (dateCol < 0 && Eq(v, "Дата создания реестра")) dateCol = i;
            else if (lastCol < 0 && Eq(v, "Фамилия")) lastCol = i;
            else if (firstCol < 0 && Eq(v, "Имя")) firstCol = i;
            else if (patrCol < 0 && Eq(v, "Отчество")) patrCol = i;
            else if (amountCol < 0 && Eq(v, "Сумма")) amountCol = i;
            else if (statusCol < 0 && Eq(v, "Статус")) statusCol = i;
            else if (purposeCol < 0 && Eq(v, "Назначение платежа")) purposeCol = i;
        }

        return regCol >= 0 && dateCol >= 0 && lastCol >= 0 && firstCol >= 0 && patrCol >= 0
               && amountCol >= 0 && statusCol >= 0 && purposeCol >= 0;
    }

    private static bool Eq(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool TryParseDate(object value, out DateTime date)
    {
        switch (value)
        {
            case DateTime dt: date = dt; return true;
            case double d: date = DateTime.FromOADate(d); return true;
            case int i: date = DateTime.FromOADate(i); return true;
            case string s when DateTime.TryParse(s.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var v):
                date = v; return true;
            case string s when DateTime.TryParse(s.Trim(), CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None, out var v):
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
