using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class VoucherData
{
	public static async Task InsertVoucher(VoucherModel voucher) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.InsertVoucher, voucher);
}