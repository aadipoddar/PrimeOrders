using PrimeBakesLibrary.Models.Product;

namespace PrimeBakesLibrary.Data.Product;

public static class TaxData
{
	public static async Task InsertTax(TaxModel tax) =>
			await SqlDataAccess.SaveData(StoredProcedureNames.InsertTax, tax);
}
