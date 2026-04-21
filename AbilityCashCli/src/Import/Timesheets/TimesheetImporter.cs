using System.Globalization;
using System.Text.RegularExpressions;
using AbilityCashCli.Configuration;
using ExcelDataReader;

namespace AbilityCashCli.Import.Timesheets;

public sealed class TimesheetImporter : IImporter
{
    private static readonly IReadOnlyDictionary<string, int> MonthNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["январь"] = 1,
        ["февраль"] = 2,
        ["март"] = 3,
        ["апрель"] = 4,
        ["май"] = 5,
        ["июнь"] = 6,
        ["июль"] = 7,
        ["август"] = 8,
        ["сентябрь"] = 9,
        ["октябрь"] = 10,
        ["ноябрь"] = 11,
        ["декабрь"] = 12
    };

    private static readonly Regex YearRegex = new(@"\b(\d{4})\b", RegexOptions.Compiled);

    private readonly PersonNameNormalizer _nameNormalizer;
    private readonly TimeSpan _defaultTime;

    public TimesheetImporter(PersonNameNormalizer nameNormalizer, TimesheetConfig cfg)
    {
        _nameNormalizer = nameNormalizer;
        if (!TimeSpan.TryParseExact(cfg.DefaultTime, @"h\:mm", CultureInfo.InvariantCulture, out _defaultTime)
            && !TimeSpan.TryParseExact(cfg.DefaultTime, @"hh\:mm", CultureInfo.InvariantCulture, out _defaultTime))
            throw new InvalidOperationException($"Timesheet.DefaultTime '{cfg.DefaultTime}' не распарсился (ожидается 'HH:mm').");
    }

    public IReadOnlyList<ImportRecord> Read(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var (month, year, monthName) = ParseMonthYear(fileName);
        var lastDay = DateTime.DaysInMonth(year, month);
        var operationDate = new DateTime(year, month, lastDay) + _defaultTime;

        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        int personCol = -1, workedCol = -1, normCol = -1;
        var headerSeen = false;
        var records = new List<ImportRecord>();

        while (reader.Read())
        {
            if (!headerSeen)
            {
                if (TryDetectHeader(reader, out personCol, out workedCol, out normCol))
                    headerSeen = true;
                continue;
            }

            var person = _nameNormalizer.Normalize(NormalizeString(reader.GetValue(personCol)));
            if (string.IsNullOrEmpty(person)) continue;

            var rawWorked = reader.GetValue(workedCol);
            var rawNorm = reader.GetValue(normCol);
            if (rawWorked is null || rawNorm is null) continue;

            if (!TryParseDecimal(rawWorked, out var worked))
                throw new InvalidOperationException($"Не удалось распарсить часы для '{person}' в {path}.");
            if (!TryParseDecimal(rawNorm, out var norm))
                throw new InvalidOperationException($"Не удалось распарсить норму для '{person}' в {path}.");

            if (worked < 0)
                throw new InvalidOperationException($"Отрицательные часы ({worked}) для '{person}' в {path}.");
            if (norm == 0)
                throw new InvalidOperationException($"Норма часов = 0 для '{person}' в {path}.");

            var ratio = worked / norm;
            var comment = $"Оклад за {monthName} {year}: {FormatHours(worked)} из {FormatHours(norm)} ч";
            records.Add(new ImportRecord(operationDate, ratio, person, comment));
        }

        if (!headerSeen)
            throw new InvalidOperationException($"В файле {path} не найдена шапка табеля.");

        return records;
    }

    private static bool TryDetectHeader(IExcelDataReader reader,
        out int personCol, out int workedCol, out int normCol)
    {
        personCol = workedCol = normCol = -1;

        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetValue(i) is not string s) continue;
            var v = s.Trim().ToLowerInvariant();
            if (v.Length == 0) continue;

            if (personCol < 0 && v.Contains("фамилия") && v.Contains("инициал")) personCol = i;
            else if (workedCol < 0 && v.Contains("отработано")) workedCol = i;
            else if (normCol < 0 && v.Contains("норма")) normCol = i;
        }

        return personCol >= 0 && workedCol >= 0 && normCol >= 0;
    }

    private static (int Month, int Year, string MonthName) ParseMonthYear(string fileName)
    {
        string? foundMonthName = null;
        var month = 0;
        foreach (var kv in MonthNames)
        {
            if (fileName.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
            {
                foundMonthName = kv.Key;
                month = kv.Value;
                break;
            }
        }
        if (foundMonthName is null)
            throw new InvalidOperationException($"В имени файла '{fileName}' не найден месяц (январь..декабрь).");

        var yearMatch = YearRegex.Match(fileName);
        if (!yearMatch.Success)
            throw new InvalidOperationException($"В имени файла '{fileName}' не найден год (4 цифры).");
        var year = int.Parse(yearMatch.Groups[1].Value, CultureInfo.InvariantCulture);

        return (month, year, foundMonthName);
    }

    private static string FormatHours(decimal hours) =>
        hours.ToString(hours == Math.Truncate(hours) ? "0" : "0.##", CultureInfo.InvariantCulture);

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
