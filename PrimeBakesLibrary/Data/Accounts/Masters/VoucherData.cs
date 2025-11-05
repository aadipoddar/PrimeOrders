using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Data.Accounts.Masters;

public static class VoucherData
{
	public static async Task<int> InsertVoucher(VoucherModel voucher) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertVoucher, voucher)).FirstOrDefault();
}