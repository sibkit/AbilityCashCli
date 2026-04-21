using AbilityCashCli.Data;
using AbilityCashCli.Data.Entities;

namespace AbilityCashCli.Import;

public sealed class CategoryPathResolver
{
    private readonly AppDbContext _db;
    private readonly string _separator;
    private readonly Dictionary<string, int> _cache = new(StringComparer.Ordinal);

    public CategoryPathResolver(AppDbContext db, string separator)
    {
        _db = db;
        _separator = separator;
    }

    public int Resolve(string path)
    {
        if (_cache.TryGetValue(path, out var cached))
            return cached;

        var parts = path.Split(_separator, StringSplitOptions.None);
        if (parts.Length == 0 || parts.Any(string.IsNullOrEmpty))
            throw new InvalidOperationException($"Пустой путь категории: '{path}'.");

        int? parent = null;
        Category? current = null;
        foreach (var part in parts)
        {
            current = parent is null
                ? _db.Categories.FirstOrDefault(c => c.Parent == null && c.Name == part && c.Deleted == 0)
                : _db.Categories.FirstOrDefault(c => c.Parent == parent && c.Name == part && c.Deleted == 0);

            if (current is null)
                throw new InvalidOperationException($"Категория не найдена по пути '{path}' (часть '{part}').");

            parent = current.Id;
        }

        _cache[path] = current!.Id;
        return current.Id;
    }
}
