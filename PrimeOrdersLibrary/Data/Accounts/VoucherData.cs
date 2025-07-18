using PrimeOrdersLibrary.Models.Accounts;

namespace PrimeOrdersLibrary.Data.Accounts;

public static class VoucherData
{
	public static async Task InsertVoucher(VoucherModel voucher) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertVoucher, voucher);
}