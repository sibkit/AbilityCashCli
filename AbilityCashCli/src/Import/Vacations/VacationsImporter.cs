using System.Globalization;
using System.Text.RegularExpressions;
using ExcelDataReader;

namespace AbilityCashCli.Import.Vacations;

public sealed class VacationsImporter : IImporter
{
    private static readonly string[] DateFormats = ["dd.MM.yyyy", "d.M.yyyy", "dd.M.yyyy", "d.MM.yyyy"];
    private static readonly Regex DotSpaceRegex = new(@"\.\s+", RegexOptions.Compiled);

    public IReadOnlyList<ImportRecord> Read(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        if (!reader.Read())
            throw new InvalidOperationException($"В файле {path} нет данных.");

        var personCol = -1;
        var datesCol = -1;
        var daysCol = -1;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var v = reader.GetValue(i) as string;
            if (string.IsNullOrWhiteSpace(v)) continue;
            var s = v.Trim().ToLowerInvariant();
            if (personCol < 0 && s == "фио") personCol = i;
            else if (datesCol < 0 && s == "даты") datesCol = i;
            else if (daysCol < 0 && s.Contains("кол-во дней")) daysCol = i;
        }

        if (personCol < 0 || datesCol < 0 || daysCol < 0)
            throw new InvalidOperationException(
                $"В файле {path} не найдена шапка (ожидаются колонки 'ФИО', 'Даты', 'Кол-во дней').");

        var records = new List<ImportRecord>();
        while (reader.Read())
        {
            var person = NormalizePersonName(reader.GetValue(personCol));
            if (string.IsNullOrEmpty(person)) continue;

            var rawDates = NormalizeString(reader.GetValue(datesCol));
            if (string.IsNullOrEmpty(rawDates)) continue;
            if (!TryParseDateRange(rawDates, out var startDate)) continue;

            var rawDays = reader.GetValue(daysCol);
            if (rawDays is null) continue;
            if (!TryParseDecimal(rawDays, out var days)) continue;

            var daysInt = (int)days;
            var comment = $"Отпуск с {startDate:dd.MM.yyyy} {daysInt} дней";
            records.Add(new ImportRecord(startDate, days, person, comment));
        }

        return records;
    }

    private static bool TryParseDateRange(string s, out DateTime start)
    {
        start = default;
        var dash = s.IndexOf('-');
        if (dash <= 0) return false;

        var startStr = s[..dash].Trim();
        return DateTime.TryParseExact(
            startStr,
            DateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out start);
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

    private static string NormalizePersonName(object? value) =>
        DotSpaceRegex.Replace(NormalizeString(value), ".");
}
