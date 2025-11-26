using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Data.Inventory.Kitchen;

public static class KitchenData
{
    public static async Task<int> InsertKitchen(KitchenModel kitchen) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchen, kitchen)).FirstOrDefault();
}
