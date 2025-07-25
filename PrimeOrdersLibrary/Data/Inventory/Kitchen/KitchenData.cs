﻿using PrimeOrdersLibrary.Models.Inventory;

namespace PrimeOrdersLibrary.Data.Inventory.Kitchen;

public static class KitchenData
{
	public static async Task InsertKitchen(KitchenModel kitchen) =>
			await SqlDataAccess.SaveData(StoredProcedureNames.InsertKitchen, kitchen);
}
