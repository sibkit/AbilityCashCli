using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class User
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public string Login { get; set; } = null!;

    public virtual ICollection<InterfacePage> InterfacePages { get; set; } = new List<InterfacePage>();
}
