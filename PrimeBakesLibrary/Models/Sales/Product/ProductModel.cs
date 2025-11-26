namespace PrimeBakesLibrary.Models.Sales.Product;

public class ProductModel
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public int ProductCategoryId { get; set; }
    public decimal Rate { get; set; }
    public int TaxId { get; set; }
    public bool Status { get; set; }
}

public class ProductCategoryModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public class ProductLocationModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal Rate { get; set; }
    public int LocationId { get; set; }
    public bool Status { get; set; }
}

public class ProductLocationOverviewModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public int ProductCategoryId { get; set; }
    public decimal Rate { get; set; }
    public int TaxId { get; set; }
    public int LocationId { get; set; }
}