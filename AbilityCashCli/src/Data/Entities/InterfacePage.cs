using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class InterfacePage
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public int Owner { get; set; }

    public int Position { get; set; }

    public int Visible { get; set; }

    public string Caption { get; set; } = null!;

    public int Platform { get; set; }

    public int PageType { get; set; }

    public int? Classifier { get; set; }

    public byte[]? Settings { get; set; }

    public byte[]? SavedSettings { get; set; }

    public virtual Classifier? ClassifierNavigation { get; set; }

    public virtual User OwnerNavigation { get; set; } = null!;
}
