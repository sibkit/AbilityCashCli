using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class Property
{
    public int Changed { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;
}
