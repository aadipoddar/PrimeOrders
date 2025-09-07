namespace PrimeOrdersLibrary.Data.Common;

public static class LocationData
{
	public static async Task<int> InsertLocation(LocationModel location) =>
			(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertLocation, location)).FirstOrDefault();
}
