using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class AccountFolder
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public string Name { get; set; } = null!;

    public string Comment { get; set; } = null!;

    public int? Parent { get; set; }

    public int Locked { get; set; }

    public virtual ICollection<AccountLayout> AccountLayouts { get; set; } = new List<AccountLayout>();

    public virtual ICollection<AccountFolder> InverseParentNavigation { get; set; } = new List<AccountFolder>();

    public virtual AccountFolder? ParentNavigation { get; set; }
}
