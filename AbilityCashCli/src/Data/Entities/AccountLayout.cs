using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class AccountLayout
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public int Folder { get; set; }

    public int Account { get; set; }

    public virtual Account AccountNavigation { get; set; } = null!;

    public virtual AccountFolder FolderNavigation { get; set; } = null!;
}
