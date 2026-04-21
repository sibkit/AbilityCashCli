using AbilityCashCli.Data;
using AbilityCashCli.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AbilityCashCli.Import;

public sealed class TransactionGroupAllocator
{
    private readonly AppDbContext _db;
    private readonly int _nowUnix;
    private readonly Dictionary<int, int> _nextPosition = new();

    public TransactionGroupAllocator(AppDbContext db, int nowUnix)
    {
        _db = db;
        _nowUnix = nowUnix;
    }

    public async Task<TransactionGroup> NewGroupAsync(int holderDateTime, CancellationToken ct)
    {
        if (!_nextPosition.TryGetValue(holderDateTime, out var position))
        {
            var maxPos = await _db.TransactionGroups
                .Where(g => g.HolderDateTime == holderDateTime)
                .MaxAsync(g => (int?)g.Position, ct);
            position = (maxPos ?? -1) + 1;
        }
        _nextPosition[holderDateTime] = position + 1;

        var group = new TransactionGroup
        {
            Guid = AbilityCashValues.NewGuidBytes(),
            Changed = _nowUnix,
            Deleted = 0,
            HolderDateTime = holderDateTime,
            Position = position,
            Recurrence = AbilityCashValues.RecurrenceEmpty
        };
        _db.TransactionGroups.Add(group);
        return group;
    }
}
