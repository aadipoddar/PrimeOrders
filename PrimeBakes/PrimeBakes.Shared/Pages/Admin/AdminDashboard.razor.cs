using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Product;

namespace PrimeBakes.Shared.Pages.Admin;

public partial class AdminDashboard
{
	// Dashboard Statistics Properties
	public int TotalOutlets { get; set; } = 12;
	public int ActiveUsers { get; set; } = 48;
	public int RawMaterialsCount { get; set; } = 156;
	public int MaterialCategoriesCount { get; set; } = 24;
	public int FinishedProductsCount { get; set; } = 89;
	public int ProductCategoriesCount { get; set; } = 18;
	public int ActiveKitchens { get; set; } = 8;

	protected override async Task OnInitializedAsync()
	{
		var locations = await CommonData.LoadTableData<LocationModel>(TableNames.Location);
		var users = await CommonData.LoadTableData<UserModel>(TableNames.User);
		var rawMaterials = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);
		var materialCategories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		var finishedProducts = await CommonData.LoadTableData<ProductModel>(TableNames.Product);
		var productCategories = await CommonData.LoadTableData<ProductCategoryModel>(TableNames.ProductCategory);
		var kitchens = await CommonData.LoadTableData<KitchenModel>(TableNames.Kitchen);

		TotalOutlets = locations?.Count ?? 0;
		ActiveUsers = users?.Count(u => u.Status) ?? 0;
		RawMaterialsCount = rawMaterials?.Count ?? 0;
		MaterialCategoriesCount = materialCategories?.Count ?? 0;
		FinishedProductsCount = finishedProducts?.Count ?? 0;
		ProductCategoriesCount = productCategories?.Count ?? 0;
		ActiveKitchens = kitchens?.Count(k => k.Status) ?? 0;

		StateHasChanged();
	}
}