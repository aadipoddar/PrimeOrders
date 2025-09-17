using Microsoft.AspNetCore.Components;

using PrimeBakes.Services;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Data.Inventory;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Models.Common;
using PrimeOrdersLibrary.Models.Inventory;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Components.Pages.Inventory.Kitchen;

public partial class KitchenIssuePage
{
	[Inject] public NavigationManager NavManager { get; set; }

	private UserModel _user;
	private bool _isLoading = true;

	private int _selectedCategoryId = 0;

	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];
	private readonly List<KitchenIssueRawMaterialCartModel> _cart = [];

	private SfGrid<KitchenIssueRawMaterialCartModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		_user = await AuthService.AuthenticateCurrentUser(NavManager);

		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_rawMaterialCategories = await CommonData.LoadTableDataByStatus<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		_rawMaterialCategories.Add(new()
		{
			Id = 0,
			Name = "All Categories"
		});
		_rawMaterialCategories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
		_selectedCategoryId = 0;

		var allRawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDateTime(0, TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "India Standard Time"));
		foreach (var rawMaterial in allRawMaterials)
			_cart.Add(new()
			{
				RawMaterialCategoryId = rawMaterial.RawMaterialCategoryId,
				RawMaterialId = rawMaterial.Id,
				RawMaterialName = rawMaterial.Name,
				Quantity = 0
			});
		_cart.Sort((x, y) => string.Compare(x.RawMaterialName, y.RawMaterialName, StringComparison.Ordinal));

		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart);
		if (File.Exists(fullPath))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<KitchenIssueRawMaterialCartModel>>(
				await File.ReadAllTextAsync(fullPath)) ?? [];
			foreach (var item in items)
				_cart.Where(p => p.RawMaterialId == item.RawMaterialId).FirstOrDefault().Quantity = item.Quantity;
		}

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}

	private async Task OnRawMaterialCategoryChanged(ChangeEventArgs<int, RawMaterialCategoryModel> args)
	{
		if (args is null || args.Value <= 0)
			_selectedCategoryId = 0;

		else
			_selectedCategoryId = args.Value;

		await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task AddToCart(KitchenIssueRawMaterialCartModel item)
	{
		if (item is null)
			return;

		item.Quantity = 1;
		await _sfGrid.Refresh();
		StateHasChanged();

		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));
		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private async Task UpdateQuantity(KitchenIssueRawMaterialCartModel item, decimal newQuantity)
	{
		if (item is null)
			return;

		item.Quantity = Math.Max(0, newQuantity);
		await _sfGrid.Refresh();
		StateHasChanged();

		if (_cart.Where(x => x.Quantity > 0).Count() == 0 && File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart)))
			File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart));
		else
			await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private async Task GoToCart()
	{
		if (_cart.Sum(x => x.Quantity) <= 0)
			return;

		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));
		_cart.Clear();

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		NavManager.NavigateTo("/Inventory/KitchenIssue/Cart");
	}
}