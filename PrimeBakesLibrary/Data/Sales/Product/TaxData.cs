using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Data.Sales.Product;

public static class TaxData
{
    public static async Task InsertTax(TaxModel tax) =>
            await SqlDataAccess.SaveData(StoredProcedureNames.InsertTax, tax);
}
