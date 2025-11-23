using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Product;
using PrimeBakesLibrary.Models.Sales.Order;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Pages.Sales.Order;

public partial class OrderMobilePage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private ProductCategoryModel _selectedCategory;

	private List<ProductCategoryModel> _productCategories = [];
	private List<OrderItemCartModel> _cart = [];

	private SfGrid<OrderItemCartModel> _sfCartGrid;

	private string _errorTitle = string.Empty;
	private string _errorMessage = string.Empty;

	private string _successTitle = string.Empty;
	private string _successMessage = string.Empty;

	private SfToast _sfSuccessToast;
	private SfToast _sfErrorToast;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Order);
		_user = authResult.User;
		await LoadData();
		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadData()
	{
		await LoadItems();
		await LoadExistingCart();
		await SaveOrderFile();

		if (_sfCartGrid is not null)
			await _sfCartGrid?.Refresh();
	}

	private async Task LoadItems()
	{
		try
		{
			_productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(TableNames.ProductCategory);
			_productCategories.Add(new()
			{
				Id = 0,
				Name = "All Categories"
			});
			_productCategories = [.. _productCategories.OrderBy(s => s.Name)];
			_selectedCategory = _productCategories.FirstOrDefault(s => s.Id == 0);

			var mainLocationProducts = await ProductData.LoadProductByLocation(1);
			var orderLocationProducts = await ProductData.LoadProductByLocation(_user.LocationId);
			var allProducts = mainLocationProducts.Where(x => orderLocationProducts.Any(y => y.ProductId == x.ProductId)).ToList();
			foreach (var product in allProducts)
				_cart.Add(new()
				{
					ItemCategoryId = product.ProductCategoryId,
					ItemId = product.ProductId,
					ItemName = product.Name,
					Remarks = null,
					Quantity = 0
				});
			_cart = [.. _cart.OrderBy(s => s.ItemName)];
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Items", ex.Message, "error");
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			if (await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName))
			{
				var existingCart = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderMobileCartDataFileName));
				foreach (var item in existingCart)
					_cart.Where(p => p.ItemId == item.ItemId).FirstOrDefault().Quantity = item.Quantity;
			}
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Loading Existing Cart", ex.Message, "error");
			await DeleteLocalFiles();
		}
		finally
		{
			await SaveOrderFile();
		}
	}
	#endregion

	#region Cart
	private async Task AddToCart(OrderItemCartModel item)
	{
		if (item is null)
			return;

		item.Quantity = 1;
		await SaveOrderFile();
	}

	private async Task UpdateQuantity(OrderItemCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		await SaveOrderFile();
	}

	private async Task SaveOrderFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			if (!_cart.Any(x => x.Quantity > 0) && await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName))
				await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);
			else
				await DataStorageService.LocalSaveAsync(StorageFileNames.OrderMobileCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

			VibrationService.VibrateHapticClick();
		}
		catch (Exception ex)
		{
			await ShowToast("An Error Occurred While Saving Cart Data", ex.Message, "error");
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task GoToCart()
	{
		await SaveOrderFile();

		if (_cart.Sum(x => x.Quantity) <= 0 || await DataStorageService.LocalExists(StorageFileNames.OrderMobileCartDataFileName) == false)
			return;

		VibrationService.VibrateWithTime(500);
		_cart.Clear();

		NavigationManager.NavigateTo(PageRouteNames.OrderMobileCart);
	}
	#endregion

	#region Utilities
	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.OrderMobileCartDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.OrderMobileDataFileName);
	}

	private async Task ShowToast(string title, string message, string type)
	{
		VibrationService.VibrateWithTime(200);

		if (type == "error")
		{
			_errorTitle = title;
			_errorMessage = message;
			await _sfErrorToast.ShowAsync(new()
			{
				Title = _errorTitle,
				Content = _errorMessage
			});
		}

		else if (type == "success")
		{
			_successTitle = title;
			_successMessage = message;
			await _sfSuccessToast.ShowAsync(new()
			{
				Title = _successTitle,
				Content = _successMessage
			});
		}
	}
	#endregion
}