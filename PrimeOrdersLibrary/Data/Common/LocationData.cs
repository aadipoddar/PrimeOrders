namespace PrimeOrdersLibrary.Data.Common;

public static class LocationData
{
	public static async Task InsertLocation(LocationModel location) =>
			await SqlDataAccess.SaveData(StoredProcedureNames.InsertLocation, location);
}
