using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Data.Sales.Product;

public static class TaxData
{
	public static async Task<int> InsertTax(TaxModel tax) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertTax, tax)).FirstOrDefault();
}
