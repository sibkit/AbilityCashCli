using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class Category
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public string Name { get; set; } = null!;

    public string Comment { get; set; } = null!;

    public int? Parent { get; set; }

    public virtual ICollection<Classifier> ClassifierExpenseTreeRootNavigations { get; set; } = new List<Classifier>();

    public virtual ICollection<Classifier> ClassifierIncomeTreeRootNavigations { get; set; } = new List<Classifier>();

    public virtual ICollection<Classifier> ClassifierTransferTreeRootNavigations { get; set; } = new List<Classifier>();

    public virtual ICollection<DataSeriesCategoryFilter> DataSeriesCategoryFilters { get; set; } = new List<DataSeriesCategoryFilter>();

    public virtual ICollection<Category> InverseParentNavigation { get; set; } = new List<Category>();

    public virtual Category? ParentNavigation { get; set; }

    public virtual ICollection<TransactionCategory> TransactionCategories { get; set; } = new List<TransactionCategory>();
}
