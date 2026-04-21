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
        if (TryResolve(path, out var id, out var error))
            return id;
        throw new InvalidOperationException(error);
    }

    public bool TryResolve(string path, out int id, out string? error)
    {
        id = 0;
        error = null;

        if (_cache.TryGetValue(path, out var cached))
        {
            id = cached;
            return true;
        }

        var parts = path.Split(_separator, StringSplitOptions.None);
        if (parts.Length == 0 || parts.Any(string.IsNullOrEmpty))
        {
            error = $"Пустой путь категории: '{path}'.";
            return false;
        }

        int? parent = null;
        Category? current = null;
        foreach (var part in parts)
        {
            current = parent is null
                ? _db.Categories.FirstOrDefault(c => c.Parent == null && c.Name == part && c.Deleted == 0)
                : _db.Categories.FirstOrDefault(c => c.Parent == parent && c.Name == part && c.Deleted == 0);

            if (current is null)
            {
                error = $"Категория не найдена по пути '{path}' (часть '{part}').";
                return false;
            }

            parent = current.Id;
        }

        id = current!.Id;
        _cache[path] = id;
        return true;
    }
}
