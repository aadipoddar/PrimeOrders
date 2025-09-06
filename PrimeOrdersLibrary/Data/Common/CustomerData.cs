namespace PrimeOrdersLibrary.Data.Common;

public static class CustomerData
{
	public static async Task<int> InsertCustomer(CustomerModel customer) =>
			(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertCustomer, customer)).FirstOrDefault();

	public static async Task<CustomerModel> LoadCustomerByNumber(string number) =>
			(await SqlDataAccess.LoadData<CustomerModel, dynamic>(StoredProcedureNames.LoadCustomerByNumber, new { Number = number })).FirstOrDefault();
}
