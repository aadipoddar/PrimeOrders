
using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Admin;

public partial class RawMaterialCategoryPage
{
	private bool _isLoading = true;
	private bool _isSubmitting = false;

	private RawMaterialCategoryModel _rawMaterialCategoryModel = new()
	{
		Id = 0,
		Name = "",
		Status = true
	};

	private List<RawMaterialCategoryModel> _rawMaterialCategories = [];

	private SfGrid<RawMaterialCategoryModel> _sfGrid;

	protected override async Task OnInitializedAsync()
	{
		var authResult = await AuthService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, UserRoles.Admin, true);
		await LoadRawMaterialCategories();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task LoadRawMaterialCategories()
	{
		_rawMaterialCategories = await CommonData.LoadTableData<RawMaterialCategoryModel>(TableNames.RawMaterialCategory);
		if (_sfGrid is not null)
			await _sfGrid.Refresh();
		StateHasChanged();
	}

	private async Task SaveRawMaterialCategory()
	{
		await RawMaterialData.InsertRawMaterialCategory(_rawMaterialCategoryModel);
	}
}