using Microsoft.AspNetCore.Components;

#if ANDROID
using Plugin.LocalNotification;
#endif

using PrimeBakes.Services;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Data.Inventory.Kitchen;
using PrimeOrdersLibrary.DataAccess;
using PrimeOrdersLibrary.Exporting.Kitchen;
using PrimeOrdersLibrary.Models.Common;
using PrimeOrdersLibrary.Models.Inventory;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Components.Pages.Inventory.Kitchen;

public partial class KitchenIssueCartPage
{
	[Inject] public NavigationManager NavManager { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isSaving = false;

	private bool _orderDetailsDialogVisible = false;
	private bool _orderConfirmationDialogVisible = false;
	private bool _validationErrorDialogVisible = false;

	private List<KitchenModel> _kitchens = [];
	private readonly List<KitchenIssueRawMaterialCartModel> _cart = [];

	private readonly KitchenIssueModel _kitchenIssue = new() { LocationId = 1, IssueDate = DateTime.Now, Id = 0, Status = true, Remarks = "", CreatedAt = DateTime.Now };
	private readonly List<ValidationError> _validationErrors = [];

	public class ValidationError
	{
		public string Field { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
	}

	private SfGrid<KitchenIssueRawMaterialCartModel> _sfGrid;
	private SfDialog _sfOrderDetailsDialog;
	private SfDialog _sfOrderConfirmationDialog;
	private SfDialog _sfValidationErrorDialog;

	#region Load Data
	protected override async Task OnInitializedAsync()
	{
		_user = await AuthService.AuthenticateCurrentUser(NavManager);

		await LoadData();
		_isLoading = false;
	}

	private async Task LoadData()
	{
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);

		_kitchenIssue.UserId = _user.Id;
		_kitchenIssue.KitchenId = _kitchens.FirstOrDefault()?.Id ?? 0;
		_kitchenIssue.TransactionNo = await GenerateCodes.GenerateKitchenIssueTransactionNo(_kitchenIssue);

		_cart.Clear();
		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart);
		if (File.Exists(fullPath))
		{
			var items = System.Text.Json.JsonSerializer.Deserialize<List<KitchenIssueRawMaterialCartModel>>(await File.ReadAllTextAsync(fullPath)) ?? [];
			foreach (var item in items)
				_cart.Add(item);
		}

		_cart.Sort((x, y) => string.Compare(x.RawMaterialName, y.RawMaterialName, StringComparison.Ordinal));

		if (_sfGrid is not null)
			await _sfGrid.Refresh();

		StateHasChanged();
	}
	#endregion

	#region Products
	private async Task UpdateQuantity(KitchenIssueRawMaterialCartModel item, decimal newQuantity)
	{
		if (item is null || _isSaving)
			return;

		item.Quantity = Math.Max(0, newQuantity);

		if (item.Quantity == 0)
			_cart.Remove(item);

		await _sfGrid.Refresh();
		StateHasChanged();

		if (_cart.Count == 0 && File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart)))
			File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart));
		else
			await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart), System.Text.Json.JsonSerializer.Serialize(_cart.Where(_ => _.Quantity > 0)));

		HapticFeedback.Default.Perform(HapticFeedbackType.Click);
	}

	private async Task ClearCart()
	{
		if (_isSaving)
			return;

		_cart.Clear();
		File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart));

		if (Vibration.Default.IsSupported)
			Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));

		await _sfGrid.Refresh();
		StateHasChanged();
	}
	#endregion

	#region Saving
	private bool ValidateForm()
	{
		_validationErrors.Clear();

		if (_kitchenIssue.KitchenId <= 0)
		{
			_validationErrors.Add(new()
			{
				Field = "Kitchen",
				Message = "Please select a valid Kitchen for the Kitchen Issue."
			});
			return false;
		}

		if (_cart.Count == 0)
		{
			_validationErrors.Add(new()
			{
				Field = "Cart Items",
				Message = "Please add at least one product to the Kitchen Issue."
			});
			return false;
		}

		StateHasChanged();
		return true;
	}

	private async Task SaveKitchenIssue()
	{
		if (_isSaving)
			return;

		_isSaving = true;
		StateHasChanged();

		try
		{
			if (!ValidateForm())
			{
				_validationErrorDialogVisible = true;
				_orderConfirmationDialogVisible = false;
				return;
			}

			_kitchenIssue.Id = await KitchenIssueData.SaveKitchenIssue(_kitchenIssue, _cart);
			DeleteCart();
			await PrintInvoice();
#if ANDROID
			await SendLocalNotification();
#endif

			NavManager.NavigateTo("/Inventory/KitchenIssue/Confirmed", true);
		}
		catch (Exception ex)
		{
			_validationErrors.Add(new()
			{
				Field = "Exception",
				Message = $"An error occurred while saving the kitchen issue: {ex.Message}"
			});
			_validationErrorDialogVisible = true;
			_orderConfirmationDialogVisible = false;
		}
		finally
		{
			_isSaving = false;
			StateHasChanged();
		}
	}

	private void DeleteCart()
	{
		_cart.Clear();

		var fullPath = Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileNames.KitchenIssueCart);
		if (File.Exists(fullPath))
			File.Delete(fullPath);
	}

	private async Task PrintInvoice()
	{
		var ms = await KitchenIssueA4Print.GenerateA4KitchenIssueBill(_kitchenIssue.Id);
		var fileName = $"Kitchen_Issue_Bill_{_kitchenIssue.TransactionNo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
		SaveService saveService = new();
		saveService.SaveAndView(fileName, "application/pdf", ms);
	}

#if ANDROID
	private async Task SendLocalNotification()
	{
		if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
			await LocalNotificationCenter.Current.RequestNotificationPermission();

		var kitchenIssue = await KitchenIssueData.LoadKitchenIssueOverviewByKitchenIssueId(_kitchenIssue.Id);

		var request = new NotificationRequest
		{
			NotificationId = _kitchenIssue.Id,
			Title = "Kitchen Issue Placed",
			Subtitle = $"{kitchenIssue.TransactionNo}",
			Description = $"Your kitchen issue #{kitchenIssue.TransactionNo} has been successfully placed | Total Items: {kitchenIssue.TotalProducts} | Total Qty: {kitchenIssue.TotalQuantity} | Kitchen: {kitchenIssue.KitchenName} | User: {kitchenIssue.UserName} | Date: {kitchenIssue.IssueDate:dd/MM/yy hh:mm tt} | Remarks: {kitchenIssue.Remarks}",
			Schedule = new NotificationRequestSchedule
			{
				NotifyTime = DateTime.Now.AddSeconds(5)
			}
		};

		await LocalNotificationCenter.Current.Show(request);
	}
#endif
	#endregion
}