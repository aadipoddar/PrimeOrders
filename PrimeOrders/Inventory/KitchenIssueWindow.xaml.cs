using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.Data.Inventory.Kitchen;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Kitchen;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Product;

using PrimeOrders.Common;

namespace PrimeOrders.Inventory;

/// <summary>
/// Interaction logic for KitchenIssueWindow.xaml
/// </summary>
public partial class KitchenIssueWindow : Window
{
	private bool _isSaving = false;

	private readonly UserModel _user;

	private readonly ObservableCollection<KitchenIssueRawMaterialCartModel> _cart = [];

	#region Load Data
	public KitchenIssueWindow(UserModel user)
	{
		InitializeComponent();

		_user = user;
		cartDataGrid.ItemsSource = _cart;

		if (_user.LocationId != 1)
			Close();
	}

	private async void Window_Loaded(object sender, RoutedEventArgs e)
	{
		var kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(TableNames.Kitchen);
		kitchenAutoCompleteTextBox.AutoCompleteSource = kitchens.OrderBy(p => p.Name).ToList();
		kitchenAutoCompleteTextBox.ValueMemberPath = nameof(KitchenModel.Id);
		kitchenAutoCompleteTextBox.SearchItemPath = nameof(KitchenModel.Name);
		kitchenAutoCompleteTextBox.SelectedItem = kitchens.FirstOrDefault();

		kitchenIssueDateTimePicker.DateTime = DateTime.Now;

		await LoadRawMaterial();
		await LoadExistingKitchenIssue();
		await SaveKitchenIssueFile();

		kitchenAutoCompleteTextBox.Focus();
	}

	private async Task LoadRawMaterial()
	{
		try
		{
			List<KitchenIssueRawMaterialCartModel> allCart = [];

			var allRawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDateTime(0,
				DateOnly.FromDateTime(kitchenIssueDateTimePicker.DateTime.Value).ToDateTime(TimeOnly.MaxValue));

			var taxes = await CommonData.LoadTableData<TaxModel>(TableNames.Tax);

			foreach (var rawMaterial in allRawMaterials)
			{
				var rawMaterialTax = taxes.FirstOrDefault(t => t.Id == rawMaterial.TaxId) ?? new();

				allCart.Add(new()
				{
					RawMaterialId = rawMaterial.Id,
					RawMaterialName = rawMaterial.Name,
					RawMaterialCategoryId = rawMaterial.RawMaterialCategoryId,
					MeasurementUnit = rawMaterial.MeasurementUnit,
					Rate = rawMaterial.MRP,
					Quantity = 0,
					Total = 0
				});
			}

			selectedRawMaterialAutoCompleteTextBox.AutoCompleteSource = allCart.OrderBy(p => p.RawMaterialName).ToList();
			selectedRawMaterialAutoCompleteTextBox.ValueMemberPath = nameof(KitchenIssueRawMaterialCartModel.RawMaterialId);
			selectedRawMaterialAutoCompleteTextBox.SearchItemPath = nameof(KitchenIssueRawMaterialCartModel.RawMaterialName);
			selectedRawMaterialAutoCompleteTextBox.SelectedItem = null;
			selectedRawMaterialQuantityTextBox.Value = 0;
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading Raw materials: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			Close();
		}
	}

	private async Task LoadExistingKitchenIssue()
	{
		try
		{
			var kitchenIssueFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.KitchenIssueDataFileName);
			if (File.Exists(kitchenIssueFilePath))
			{
				var kitchenIssueData = System.Text.Json.JsonSerializer.Deserialize<KitchenIssueModel>(await File.ReadAllTextAsync(kitchenIssueFilePath));
				if (kitchenIssueData is not null)
				{
					transactionNoTextBox.Text = kitchenIssueData.TransactionNo;
					kitchenIssueDateTimePicker.DateTime = kitchenIssueData.IssueDate;
					remarksTextBox.Text = kitchenIssueData.Remarks;

					kitchenAutoCompleteTextBox.SelectedItem = kitchenAutoCompleteTextBox.AutoCompleteSource.Cast<KitchenModel>().FirstOrDefault(x => x.Id == kitchenIssueData.KitchenId);
					await LoadRawMaterial();
				}

				await LoadExistingCart();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading existing kitchen issue data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.KitchenIssueDataFileName));
		}
	}

	private async Task LoadExistingCart()
	{
		try
		{
			var kitchenIssueCartFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.KitchenIssueCartDataFileName);
			if (File.Exists(kitchenIssueCartFilePath))
			{
				var cartData = System.Text.Json.JsonSerializer.Deserialize<List<KitchenIssueRawMaterialCartModel>>(await File.ReadAllTextAsync(kitchenIssueCartFilePath));
				if (cartData is not null)
				{
					_cart.Clear();
					foreach (var item in cartData)
						_cart.Add(item);
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading existing kitchen issue cart data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.KitchenIssueCartDataFileName));
		}
	}
	#endregion

	#region Kitchen and Date
	private async void kitchenAutoCompleteTextBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		await LoadRawMaterial();
		await SaveKitchenIssueFile();
	}

	private async void kitchenIssueDateTimePicker_DateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		await LoadRawMaterial();
		await SaveKitchenIssueFile();
	}
	#endregion

	#region Validation
	private void numberTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) =>
		WindowsHelper.ValidateIntegerInput(sender, e);

	private void decimalTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) =>
		WindowsHelper.ValidateDecimalInput(sender, e);
	#endregion

	#region Cart
	private void selectedRawMaterialAutoCompleteTextBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (selectedRawMaterialAutoCompleteTextBox.SelectedItem is not null)
		{
			var rawMaterial = (KitchenIssueRawMaterialCartModel)selectedRawMaterialAutoCompleteTextBox.SelectedItem;
			selectedRawMaterialRateTextBox.Value = double.Parse(rawMaterial.Rate.ToString());
			selectedRawMaterialQuantityTextBox.Value = 1;
		}

		else
		{
			selectedRawMaterialRateTextBox.Value = 0;
			selectedRawMaterialQuantityTextBox.Value = 0;
		}

		UpdateSelectedRawMaterialFinancialDetails();
	}

	private void selectedRawMaterialQuantityTextBox_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		UpdateSelectedRawMaterialFinancialDetails();

	private void selectedRawMaterialRateTextBox_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		UpdateSelectedRawMaterialFinancialDetails();

	private void UpdateSelectedRawMaterialFinancialDetails()
	{
		if (selectedRawMaterialRateTextBox is null ||
			selectedRawMaterialQuantityTextBox is null)
			return;

		selectedRawMaterialQuantityTextBox.Value ??= 0;

		if (selectedRawMaterialAutoCompleteTextBox.SelectedItem is null)
			return;

		var rawMaterial = (KitchenIssueRawMaterialCartModel)selectedRawMaterialAutoCompleteTextBox.SelectedItem;
		var rate = decimal.Parse(selectedRawMaterialRateTextBox.Value.ToString());
		var quantity = decimal.Parse(selectedRawMaterialQuantityTextBox.Value.ToString());
		var total = rate * quantity;

		selectedRawMaterialTotalTextBox.Text = total.FormatIndianCurrency();
		selectedRawMaterialMeasuringUnitTextBox.Text = rawMaterial.MeasurementUnit;
	}

	private async void addRawMaterialButton_Click(object sender, RoutedEventArgs e)
	{
		if (selectedRawMaterialAutoCompleteTextBox.SelectedItem is null ||
			selectedRawMaterialQuantityTextBox.Value is null ||
			selectedRawMaterialQuantityTextBox.Value <= 0 ||
			_isSaving)
			return;

		var existingCartItem = _cart.FirstOrDefault(x => x.RawMaterialId == ((KitchenIssueRawMaterialCartModel)selectedRawMaterialAutoCompleteTextBox.SelectedItem).RawMaterialId);
		if (existingCartItem is not null)
			_cart.FirstOrDefault(x => x.RawMaterialId == existingCartItem.RawMaterialId).Quantity += decimal.Parse(selectedRawMaterialQuantityTextBox.Value.ToString());

		else
		{
			var rawMaterial = (KitchenIssueRawMaterialCartModel)selectedRawMaterialAutoCompleteTextBox.SelectedItem;
			var rate = decimal.Parse(selectedRawMaterialRateTextBox.Value.ToString());
			var quantity = decimal.Parse(selectedRawMaterialQuantityTextBox.Value.ToString());

			_cart.Add(new()
			{
				RawMaterialId = rawMaterial.RawMaterialId,
				RawMaterialName = rawMaterial.RawMaterialName,
				RawMaterialCategoryId = rawMaterial.RawMaterialCategoryId,
				MeasurementUnit = rawMaterial.MeasurementUnit,
				Rate = rate,
				Quantity = quantity,
				Total = 0
			});
		}

		selectedRawMaterialAutoCompleteTextBox.SelectedItem = null;
		selectedRawMaterialQuantityTextBox.Value = 0;
		selectedRawMaterialMeasuringUnitTextBox.Text = string.Empty;

		selectedRawMaterialAutoCompleteTextBox.Focus();

		await SaveKitchenIssueFile();
	}

	private async void cartDataGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		if (e.OriginalSource is FrameworkElement element && element.DataContext is KitchenIssueRawMaterialCartModel model)
		{
			_cart.Remove(model);
			selectedRawMaterialAutoCompleteTextBox.SelectedItem = model;
			selectedRawMaterialQuantityTextBox.Value = double.Parse(model.Quantity.ToString());
			selectedRawMaterialRateTextBox.Value = double.Parse(model.Rate.ToString());
			UpdateSelectedRawMaterialFinancialDetails();
			selectedRawMaterialAutoCompleteTextBox.Focus();
		}

		await SaveKitchenIssueFile();
	}
	#endregion

	#region Saving
	private async void roundOffTextBox_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		await SaveKitchenIssueFile();

	private async Task UpdateFinancials()
	{
		transactionNoTextBox.Text = await GenerateCodes.GenerateKitchenIssueTransactionNo(new()
		{
			Id = 0,
			IssueDate = kitchenIssueDateTimePicker.DateTime.Value,
			LocationId = _user.LocationId
		});

		foreach (var cart in _cart)
			cart.Total = cart.Rate * cart.Quantity;

		var total = _cart.Sum(p => p.Total);

		totalItemsTextBox.Text = _cart.Count.ToString();
		totalQuantityTextBox.Text = _cart.Sum(p => p.Quantity).ToString();
		totalTextBox.Text = total.FormatIndianCurrency();

		_cart.OrderBy(p => p.RawMaterialName).ToList();
		cartDataGrid.Items.Refresh();
	}

	private async Task SaveKitchenIssueFile()
	{
		if (_user is null || _isSaving)
			return;

		_isSaving = true;

		try
		{
			await UpdateFinancials();

			await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.KitchenIssueDataFileName),
				System.Text.Json.JsonSerializer.Serialize(new KitchenIssueModel()
				{
					Id = 0,
					UserId = _user.Id,
					IssueDate = kitchenIssueDateTimePicker.DateTime.Value,
					KitchenId = kitchenAutoCompleteTextBox.SelectedItem is not null ? ((KitchenModel)kitchenAutoCompleteTextBox.SelectedItem).Id : 0,
					TransactionNo = transactionNoTextBox.Text,
					LocationId = 1,
					Remarks = remarksTextBox.Text,
					CreatedAt = DateTime.Now,
					Status = true,
				}));

			await File.WriteAllTextAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.KitchenIssueCartDataFileName),
				System.Text.Json.JsonSerializer.Serialize(_cart.ToList()));
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error saving kitchen issue data to File: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
		finally
		{
			_isSaving = false;
		}
	}

	private async void saveButton_Click(object sender, RoutedEventArgs e)
	{
		if (_user is null || _isSaving)
			return;

		try
		{
			await SaveKitchenIssueFile();

			if (_cart.Count == 0)
			{
				MessageBox.Show("Cart is empty. Please add items to the cart before saving the kitchen issue.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (kitchenAutoCompleteTextBox.SelectedItem is null)
			{
				MessageBox.Show("Please select a Kitchen for the issue.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			_isSaving = true;

			var kitchenIssueModel = new KitchenIssueModel()
			{
				Id = 0,
				UserId = _user.Id,
				IssueDate = kitchenIssueDateTimePicker.DateTime.Value,
				LocationId = 1,
				KitchenId = ((KitchenModel)kitchenAutoCompleteTextBox.SelectedItem).Id,
				TransactionNo = transactionNoTextBox.Text,
				Remarks = remarksTextBox.Text,
				CreatedAt = DateTime.Now,
				Status = true,
			};

			var kitchenIssueId = await KitchenIssueData.SaveKitchenIssue(kitchenIssueModel, [.. _cart]);
			if (kitchenIssueId <= 0)
			{
				MessageBox.Show("Error saving kitchen issue data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			await PrintInvoice(kitchenIssueId);
			DeleteCart();

			MessageBox.Show("Kitchen Issue saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
			Close();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error saving kitchen issue data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
		finally
		{
			_isSaving = false;
		}
	}

	private static async Task PrintInvoice(int kitchenIssueId)
	{
		var kitchenIssue = await KitchenIssueData.LoadKitchenIssueOverviewByKitchenIssueId(kitchenIssueId);
		var ms = await KitchenIssueA4Print.GenerateA4KitchenIssueBill(kitchenIssueId);
		var fileName = $"KitchenIssue_{kitchenIssue.TransactionNo}_{DateTime.Now:dd/MM/yy}.pdf";
		using FileStream stream = new(Path.Combine(Path.GetTempPath(), fileName), FileMode.Create, FileAccess.Write);
		ms.Position = 0;
		await ms.CopyToAsync(stream);
		Process.Start(new ProcessStartInfo($"{Path.GetTempPath()}\\{fileName}") { UseShellExecute = true });
	}

	private static void DeleteCart()
	{
		File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.KitchenIssueDataFileName));
		File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StorageFileNames.KitchenIssueCartDataFileName));
	}
	#endregion
}
