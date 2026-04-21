using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class TransactionGroup
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public int HolderDateTime { get; set; }

    public int Position { get; set; }

    public string? Recurrence { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
