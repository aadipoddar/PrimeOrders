using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Purchase;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Product;

using PrimeOrders.Common;

namespace PrimeOrders.Inventory;

/// <summary>
/// Interaction logic for PurchaseWindow.xaml
/// </summary>
public partial class PurchaseWindow : Window
{
	private bool _isSaving = false;

	private readonly UserModel _user;

	private readonly ObservableCollection<PurchaseRawMaterialCartModel> _cart = [];

	#region Load Data
	public PurchaseWindow(UserModel user)
	{
		InitializeComponent();

		_user = user;
		cartDataGrid.ItemsSource = _cart;

		if (_user.LocationId != 1)
			Close();
	}

	private async void Window_Loaded(object sender, RoutedEventArgs e)
	{
		var parties = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
		partyAutoCompleteTextBox.AutoCompleteSource = parties.OrderBy(p => p.Name).ToList();
		partyAutoCompleteTextBox.ValueMemberPath = nameof(LedgerModel.Id);
		partyAutoCompleteTextBox.SearchItemPath = nameof(LedgerModel.Name);
		partyAutoCompleteTextBox.SelectedItem = parties.FirstOrDefault(x => x.Id == 1);

		purchaseDateTimePicker.DateTime = DateTime.Now;

		await LoadRawMaterial();
		await UpdateFinancials();

		partyAutoCompleteTextBox.Focus();
	}

	private async Task LoadRawMaterial()
	{
		try
		{
			List<PurchaseRawMaterialCartModel> allCart = [];

			var allRawMaterials = await RawMaterialData.LoadRawMaterialRateBySupplierPurchaseDateTime(
				partyAutoCompleteTextBox.SelectedItem is not null ? ((LedgerModel)partyAutoCompleteTextBox.SelectedItem).Id : 0,
				DateOnly.FromDateTime(purchaseDateTimePicker.DateTime.Value).ToDateTime(TimeOnly.MaxValue));

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
					BaseTotal = 0,
					DiscPercent = 0,
					DiscAmount = 0,
					AfterDiscount = 0,
					CGSTPercent = rawMaterialTax.Extra ? rawMaterialTax.CGST : 0,
					CGSTAmount = 0,
					SGSTPercent = rawMaterialTax.Extra ? rawMaterialTax.SGST : 0,
					SGSTAmount = 0,
					IGSTPercent = rawMaterialTax.Extra ? rawMaterialTax.IGST : 0,
					IGSTAmount = 0,
					Total = 0,
					NetRate = 0
				});
			}

			selectedRawMaterialAutoCompleteTextBox.AutoCompleteSource = allCart.OrderBy(p => p.RawMaterialName).ToList();
			selectedRawMaterialAutoCompleteTextBox.ValueMemberPath = nameof(PurchaseRawMaterialCartModel.RawMaterialId);
			selectedRawMaterialAutoCompleteTextBox.SearchItemPath = nameof(PurchaseRawMaterialCartModel.RawMaterialName);
			selectedRawMaterialAutoCompleteTextBox.SelectedItem = null;
			selectedRawMaterialQuantityTextBox.Value = 0;
			selectedRawMaterialDiscountPercentTextBox.PercentValue = 0;
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error loading Raw materials: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			Close();
		}
	}
	#endregion

	#region Party and Cash Discount
	private async void partyAutoCompleteTextBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		await LoadRawMaterial();
		await UpdateFinancials();
	}

	private async void purchaseDateTimePicker_DateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		await LoadRawMaterial();
		await UpdateFinancials();
	}

	private async void cashDiscountPercentTextBox_PercentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		await UpdateFinancials();
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
			var rawMaterial = (PurchaseRawMaterialCartModel)selectedRawMaterialAutoCompleteTextBox.SelectedItem;
			selectedRawMaterialRateTextBox.Value = double.Parse(rawMaterial.Rate.ToString());
			selectedRawMaterialQuantityTextBox.Value = 1;
			selectedRawMaterialDiscountPercentTextBox.PercentValue = 0;
		}

		else
		{
			selectedRawMaterialRateTextBox.Value = 0;
			selectedRawMaterialQuantityTextBox.Value = 0;
			selectedRawMaterialDiscountPercentTextBox.PercentValue = 0;
		}

		UpdateSelectedRawMaterialFinancialDetails();
	}

	private void selectedRawMaterialQuantityTextBox_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		UpdateSelectedRawMaterialFinancialDetails();

	private void selectedRawMaterialRateTextBox_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		UpdateSelectedRawMaterialFinancialDetails();

	private void selectedRawMaterialDiscountPercentTextBox_PercentValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		UpdateSelectedRawMaterialFinancialDetails();

	private void UpdateSelectedRawMaterialFinancialDetails()
	{
		if (selectedRawMaterialRateTextBox is null ||
			selectedRawMaterialQuantityTextBox is null ||
			selectedRawMaterialDiscountPercentTextBox is null)
			return;

		selectedRawMaterialQuantityTextBox.Value ??= 0;
		selectedRawMaterialDiscountPercentTextBox.PercentValue ??= 0;

		if (selectedRawMaterialAutoCompleteTextBox.SelectedItem is null)
			return;

		var rawMaterial = (PurchaseRawMaterialCartModel)selectedRawMaterialAutoCompleteTextBox.SelectedItem;
		var rate = decimal.Parse(selectedRawMaterialRateTextBox.Value.ToString());
		var quantity = decimal.Parse(selectedRawMaterialQuantityTextBox.Value.ToString());
		var discPercent = decimal.Parse(selectedRawMaterialDiscountPercentTextBox.PercentValue.ToString());
		var baseTotal = rate * quantity;
		var discAmount = baseTotal * discPercent / 100;
		var afterDiscount = baseTotal - discAmount;
		var cgstAmount = afterDiscount * rawMaterial.CGSTPercent / 100;
		var sgstAmount = afterDiscount * rawMaterial.SGSTPercent / 100;
		var igstAmount = afterDiscount * rawMaterial.IGSTPercent / 100;
		var totalTax = cgstAmount + sgstAmount + igstAmount;
		var total = afterDiscount + totalTax;

		selectedRawMaterialDiscountAmountTextBox.Text = discAmount.FormatIndianCurrency();
		selectedRawMaterialTaxTextBox.Text = totalTax.FormatIndianCurrency();
		selectedRawMaterialTotalTextBox.Text = total.FormatIndianCurrency();
		selectedRawMaterialMeasuringUnitTextBox.Text = rawMaterial.MeasurementUnit;

		selectedRawMaterialDiscountAmountTextBox.ToolTip = $"Base Total: {baseTotal.FormatIndianCurrency()}\nDiscount ({discPercent}%): {discAmount.FormatIndianCurrency()}\nAfter Discount: {afterDiscount.FormatIndianCurrency()}";
		selectedRawMaterialTaxTextBox.ToolTip = $"CGST ({rawMaterial.CGSTPercent}%): {cgstAmount.FormatIndianCurrency()}\nSGST ({rawMaterial.SGSTPercent}%: {sgstAmount.FormatIndianCurrency()}\nIGST ({rawMaterial.IGSTPercent}%: {igstAmount.FormatIndianCurrency()}";
		selectedRawMaterialTotalTextBox.ToolTip = $"Base Total: {baseTotal.FormatIndianCurrency()}\nAfter Discount: {afterDiscount.FormatIndianCurrency()}\nTotal Tax: {totalTax.FormatIndianCurrency()}";
	}

	private async void addRawMaterialButton_Click(object sender, RoutedEventArgs e)
	{
		if (selectedRawMaterialAutoCompleteTextBox.SelectedItem is null ||
			selectedRawMaterialQuantityTextBox.Value is null ||
			selectedRawMaterialQuantityTextBox.Value <= 0 ||
			selectedRawMaterialDiscountPercentTextBox.PercentValue is null ||
			selectedRawMaterialDiscountPercentTextBox.PercentValue < 0 ||
			selectedRawMaterialDiscountPercentTextBox.PercentValue > 100 ||
			_isSaving)
			return;

		var existingCartItem = _cart.FirstOrDefault(x => x.RawMaterialId == ((PurchaseRawMaterialCartModel)selectedRawMaterialAutoCompleteTextBox.SelectedItem).RawMaterialId);
		if (existingCartItem is not null)
			_cart.FirstOrDefault(x => x.RawMaterialId == existingCartItem.RawMaterialId).Quantity += decimal.Parse(selectedRawMaterialQuantityTextBox.Value.ToString());

		else
		{
			var rawMaterial = (PurchaseRawMaterialCartModel)selectedRawMaterialAutoCompleteTextBox.SelectedItem;
			var rate = decimal.Parse(selectedRawMaterialRateTextBox.Value.ToString());
			var quantity = decimal.Parse(selectedRawMaterialQuantityTextBox.Value.ToString());
			var discPercent = decimal.Parse(selectedRawMaterialDiscountPercentTextBox.PercentValue.ToString());

			_cart.Add(new()
			{
				RawMaterialId = rawMaterial.RawMaterialId,
				RawMaterialName = rawMaterial.RawMaterialName,
				RawMaterialCategoryId = rawMaterial.RawMaterialCategoryId,
				MeasurementUnit = rawMaterial.MeasurementUnit,
				Rate = rate,
				Quantity = quantity,
				BaseTotal = 0,
				DiscPercent = discPercent,
				DiscAmount = 0,
				AfterDiscount = 0,
				CGSTPercent = 0,
				CGSTAmount = 0,
				SGSTPercent = 0,
				SGSTAmount = 0,
				IGSTPercent = 0,
				IGSTAmount = 0,
				Total = 0,
				NetRate = 0
			});
		}

		selectedRawMaterialAutoCompleteTextBox.SelectedItem = null;
		selectedRawMaterialQuantityTextBox.Value = 0;
		selectedRawMaterialDiscountPercentTextBox.PercentValue = 0;
		selectedRawMaterialMeasuringUnitTextBox.Text = string.Empty;

		selectedRawMaterialAutoCompleteTextBox.Focus();

		await UpdateFinancials();
	}

	private async void cartDataGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		if (e.OriginalSource is FrameworkElement element && element.DataContext is PurchaseRawMaterialCartModel model)
		{
			_cart.Remove(model);
			selectedRawMaterialAutoCompleteTextBox.SelectedItem = model;
			selectedRawMaterialQuantityTextBox.Value = double.Parse(model.Quantity.ToString());
			selectedRawMaterialRateTextBox.Value = double.Parse(model.Rate.ToString());
			selectedRawMaterialDiscountPercentTextBox.PercentValue = double.Parse(model.DiscPercent.ToString());
			UpdateSelectedRawMaterialFinancialDetails();
			selectedRawMaterialAutoCompleteTextBox.Focus();
		}

		await UpdateFinancials();
	}
	#endregion

	#region Saving
	private async void roundOffTextBox_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		await UpdateFinancials(true);

	private async Task UpdateFinancials(bool customRoundOff = false)
	{
		if (_user is null || _isSaving)
			return;

		try
		{
			_isSaving = true;

			var cashDiscountPercent = decimal.Parse(cashDiscountPercentTextBox.PercentValue.Value.ToString());

			foreach (var cart in _cart)
			{
				var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(TableNames.RawMaterial, cart.RawMaterialId);
				var rawMaterialTax = await CommonData.LoadTableDataById<TaxModel>(TableNames.Tax, rawMaterial.TaxId);

				cart.BaseTotal = cart.Rate * cart.Quantity;
				cart.DiscAmount = cart.BaseTotal * cart.DiscPercent / 100;
				cart.AfterDiscount = cart.BaseTotal - cart.DiscAmount;
				cart.CGSTPercent = rawMaterialTax.Extra ? rawMaterialTax.CGST : 0;
				cart.CGSTAmount = cart.AfterDiscount * cart.CGSTPercent / 100;
				cart.SGSTPercent = rawMaterialTax.Extra ? rawMaterialTax.SGST : 0;
				cart.SGSTAmount = cart.AfterDiscount * cart.SGSTPercent / 100;
				cart.IGSTPercent = rawMaterialTax.Extra ? rawMaterialTax.IGST : 0;
				cart.IGSTAmount = cart.AfterDiscount * cart.IGSTPercent / 100;
				cart.Total = cart.AfterDiscount + cart.CGSTAmount + cart.SGSTAmount + cart.IGSTAmount;
				cart.NetRate = cart.Total / cart.Quantity * (1 - cashDiscountPercent / 100);
			}

			var baseTotal = _cart.Sum(x => x.BaseTotal);
			var subTotal = _cart.Sum(x => x.AfterDiscount);
			var rawMaterialDiscount = baseTotal - subTotal;
			var afterTax = _cart.Sum(x => x.Total);
			var totalTax = afterTax - subTotal;
			var discountAmount = afterTax * cashDiscountPercent / 100;

			decimal roundOff = 0;
			if (customRoundOff)
				roundOff = decimal.Parse(roundOffTextBox.Value.ToString());
			else
				roundOff = Math.Round(afterTax - discountAmount) - (afterTax - discountAmount);

			var total = Math.Round(afterTax - discountAmount + roundOff);

			cashDiscountAmountTextBox.Text = discountAmount.FormatIndianCurrency();
			baseTotalTextBox.Text = baseTotal.FormatIndianCurrency();
			rawMaterialDiscountTextBox.Text = rawMaterialDiscount.FormatIndianCurrency();
			subTotalTextBox.Text = subTotal.FormatIndianCurrency();
			taxAmountTextBox.Text = totalTax.FormatIndianCurrency();
			afterTaxTextBox.Text = afterTax.FormatIndianCurrency();
			roundOffTextBox.Value = double.Parse(roundOff.ToString());
			totalTextBox.Text = total.FormatIndianCurrency();

			_cart.OrderBy(p => p.RawMaterialName).ToList();
			cartDataGrid.Items.Refresh();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error in Updating Financials: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
			await UpdateFinancials(true);

			if (_cart.Count == 0)
			{
				MessageBox.Show("Cart is empty. Please add items to the cart before saving the purchase.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (partyAutoCompleteTextBox.SelectedItem is null)
			{
				MessageBox.Show("Please select a Supplier for the purchase.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (string.IsNullOrWhiteSpace(billNoTextBox.Text))
			{
				MessageBox.Show("Please enter a Bill No. for the purchase.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			_isSaving = true;

			var purchaseModel = new PurchaseModel()
			{
				Id = 0,
				UserId = _user.Id,
				SupplierId = partyAutoCompleteTextBox.SelectedItem is not null ? ((LedgerModel)partyAutoCompleteTextBox.SelectedItem).Id : 0,
				BillNo = billNoTextBox.Text,
				BillDateTime = purchaseDateTimePicker.DateTime.Value,
				CDPercent = decimal.Parse(cashDiscountPercentTextBox.PercentValue.Value.ToString()),
				RoundOff = decimal.Parse(roundOffTextBox.Value.ToString()),
				Remarks = remarksTextBox.Text,
				CreatedAt = DateTime.Now,
				Status = true,
			};

			var purchaseId = await PurchaseData.SavePurchase(purchaseModel, [.. _cart]);
			if (purchaseId <= 0)
			{
				MessageBox.Show("Error saving purchase data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// qlawait PrintInvoice(purchaseId);

			MessageBox.Show("Purchase saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
			Close();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error saving purchase data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
		finally
		{
			_isSaving = false;
		}
	}

	private static async Task PrintInvoice(int purchaseId)
	{
		var purchase = await PurchaseData.LoadPurchaseOverviewByPurchaseId(purchaseId);
		var ms = await PurchaseA4Print.GenerateA4PurchaseBill(purchaseId);
		var fileName = $"PurchaseBill_{purchase.BillNo}_{DateTime.Now:dd/MM/yy}.pdf";
		using FileStream stream = new(Path.Combine(Path.GetTempPath(), fileName), FileMode.Create, FileAccess.Write);
		ms.Position = 0;
		await ms.CopyToAsync(stream);
		Process.Start(new ProcessStartInfo($"{Path.GetTempPath()}\\{fileName}") { UseShellExecute = true });
	}
	#endregion
}
