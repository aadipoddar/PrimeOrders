using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sale;

namespace PrimeBakes.Shared.Pages.Sale;

public partial class SaleReturnPage
{
	private UserModel _user;
	private bool _isLoading = true;

	private SaleReturnModel _saleReturn = new()
	{
		Id = 0,
		LocationId = 1,
		SaleId = 0,
		UserId = 0,
		TransactionNo = string.Empty,
		ReturnDateTime = DateTime.Now,
		Remarks = string.Empty,
		Status = true
	};

	private List<LocationModel> _locations = [];

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Sales);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		//await LoadLocations();

		if (_user.LocationId == 1)
			_saleReturn.LocationId = _locations.FirstOrDefault()?.Id ?? _user.LocationId;
		else
			_saleReturn.LocationId = _user.LocationId;

		//if (SaleReturnId.HasValue && SaleReturnId.Value > 0)
		//	await LoadSaleReturn();

		//await LoadAvailableSales();

		StateHasChanged();
	}

	//private async Task LoadLocations()
	//{
	//	if (_user.LocationId == 1)
	//	{
	//		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(TableNames.Location);
	//		_locations.RemoveAll(l => l.Id == 1);
	//	}
	//}

	//private async Task LoadAvailableSales()
	//{
	//	_availableSales = await SaleData.LoadSaleDetailsByDateLocationId(
	//		_startDate.ToDateTime(new TimeOnly(0, 0)),
	//		_endDate.ToDateTime(new TimeOnly(23, 59)),
	//		1);

	//	var filteredSales = new List<SaleOverviewModel>();

	//	foreach (var sale in _availableSales)
	//		if (sale.PartyId.HasValue && sale.PartyId > 0)
	//		{
	//			var supplier = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
	//			if (supplier?.LocationId == _saleReturn.LocationId)
	//				filteredSales.Add(sale);
	//		}

	//	_availableSales = filteredSales;

	//	StateHasChanged();
	//}

	//private async Task LoadSaleReturn()
	//{
	//	_saleReturn = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, SaleReturnId.Value);

	//	if (_saleReturn is null)
	//		NavManager.NavigateTo("/SaleReturn");

	//	if (_saleReturn.SaleId > 0)
	//		await LoadSaleForReturn(_saleReturn.SaleId);

	//	_saleReturnProductCart.Clear();

	//	var saleReturnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(_saleReturn.Id);
	//	foreach (var item in saleReturnDetails)
	//	{
	//		var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
	//		var cartItem = _availableSaleProducts.FirstOrDefault(p => p.ProductId == item.ProductId);

	//		_saleReturnProductCart.Add(new()
	//		{
	//			ProductId = product.Id,
	//			ProductName = product.Name,
	//			Quantity = item.Quantity,
	//			MaxQuantity = cartItem?.SoldQuantity ?? 0,
	//			SoldQuantity = cartItem?.SoldQuantity ?? 0,
	//			AlreadyReturnedQuantity = cartItem?.AlreadyReturnedQuantity ?? 0,
	//			Rate = item.Rate,
	//			BaseTotal = item.BaseTotal,
	//			DiscPercent = item.DiscPercent,
	//			DiscAmount = item.DiscAmount,
	//			AfterDiscount = item.AfterDiscount,
	//			CGSTPercent = item.CGSTPercent,
	//			CGSTAmount = item.CGSTAmount,
	//			SGSTPercent = item.SGSTPercent,
	//			SGSTAmount = item.SGSTAmount,
	//			IGSTPercent = item.IGSTPercent,
	//			IGSTAmount = item.IGSTAmount,
	//			NetRate = item.NetRate,
	//			Total = item.Total,
	//		});
	//	}

	//	StateHasChanged();
	//}

	//private async Task LoadSaleForReturn(int saleId)
	//{
	//	_selectedSale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, saleId);
	//	_selectedSaleId = saleId;
	//	_saleReturn.SaleId = saleId;

	//	if (_selectedSale is not null)
	//	{
	//		_saleReturn.TransactionNo = await GenerateCodes.GenerateSaleReturnTransactionNo(_saleReturn);

	//		var saleDetails = await SaleData.LoadSaleDetailBySale(saleId);
	//		var existingReturns = await SaleReturnData.LoadSaleReturnBySale(saleId);

	//		_availableSaleProducts.Clear();

	//		foreach (var detail in saleDetails)
	//		{
	//			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, detail.ProductId);

	//			decimal alreadyReturnedQty = 0;
	//			foreach (var returnRecord in existingReturns.Where(r => r.Status))
	//			{
	//				var returnDetails = await SaleReturnData.LoadSaleReturnDetailBySaleReturn(returnRecord.Id);
	//				alreadyReturnedQty += returnDetails.Where(rd => rd.ProductId == detail.ProductId && rd.Status).Sum(rd => rd.Quantity);
	//			}

	//			decimal maxReturnableQty = detail.Quantity - alreadyReturnedQty;

	//			if (maxReturnableQty > 0)
	//				_availableSaleProducts.Add(new()
	//				{
	//					ProductId = product.Id,
	//					ProductName = product.Name,
	//					Quantity = 0,
	//					MaxQuantity = maxReturnableQty,
	//					SoldQuantity = detail.Quantity,
	//					AlreadyReturnedQuantity = alreadyReturnedQty,
	//					Rate = detail.Rate,
	//					BaseTotal = detail.BaseTotal,
	//					DiscPercent = detail.DiscPercent,
	//					DiscAmount = detail.DiscAmount,
	//					AfterDiscount = detail.AfterDiscount,
	//					CGSTPercent = detail.CGSTPercent,
	//					CGSTAmount = detail.CGSTAmount,
	//					SGSTPercent = detail.SGSTPercent,
	//					SGSTAmount = detail.SGSTAmount,
	//					IGSTPercent = detail.IGSTPercent,
	//					IGSTAmount = detail.IGSTAmount,
	//					NetRate = detail.NetRate,
	//					Total = detail.Total
	//				});
	//		}

	//		_products = [];
	//		foreach (var availableProduct in _availableSaleProducts)
	//			_products.Add(availableProduct);

	//		_filteredProducts = [.. _products];
	//	}

	//	StateHasChanged();
	//}
	#endregion
}