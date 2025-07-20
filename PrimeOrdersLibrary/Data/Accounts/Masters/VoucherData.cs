using PrimeOrdersLibrary.Models.Accounts.Masters;

namespace PrimeOrdersLibrary.Data.Accounts.Masters;

public static class VoucherData
{
	public static async Task InsertVoucher(VoucherModel voucher) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertVoucher, voucher);
}