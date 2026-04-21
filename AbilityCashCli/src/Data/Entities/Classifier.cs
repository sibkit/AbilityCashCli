using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class Classifier
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public string SingularName { get; set; } = null!;

    public string PluralName { get; set; } = null!;

    public int? IncomeTreeRoot { get; set; }

    public int? ExpenseTreeRoot { get; set; }

    public int? TransferTreeRoot { get; set; }

    public virtual Category? ExpenseTreeRootNavigation { get; set; }

    public virtual Category? IncomeTreeRootNavigation { get; set; }

    public virtual ICollection<InterfacePage> InterfacePages { get; set; } = new List<InterfacePage>();

    public virtual Category? TransferTreeRootNavigation { get; set; }
}
