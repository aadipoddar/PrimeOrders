using System.Text;

using NumericWordsConversion;

using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Product;
using PrimeBakesLibrary.Data.Sale;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Order;
using PrimeBakesLibrary.Models.Sale;

namespace PrimeBakesLibrary.Exporting.Sale;

public class SaleThermalPrint
{
	public static async Task<StringBuilder> GenerateThermalBill(SaleModel sale)
	{
		StringBuilder content = new();

		await AddHeader(content);

		if (sale.LocationId != 1)
			await AddOutletDetails(sale.LocationId, content);

		await AddBillDetails(sale, content);

		await AddItemDetails(sale, content);

		await AddTotalDetails(sale, content);

		AddFooter(content);

		return content;
	}

	private static async Task AddHeader(StringBuilder content)
	{
		var mainLocation = await LedgerData.LoadLedgerByLocation(1);

		content.AppendLine("<div class='header'>");
		content.AppendLine($"<div class='company-name'>PRIME BAKES</div>");

		if (!string.IsNullOrEmpty(mainLocation.Alias))
			content.AppendLine($"<div class='header-line'>{mainLocation.Alias}</div>");

		if (!string.IsNullOrEmpty(mainLocation.GSTNo))
			content.AppendLine($"<div class='header-line'>GSTNO: {mainLocation.GSTNo}</div>");

		if (!string.IsNullOrEmpty(mainLocation.Address))
			content.AppendLine($"<div class='header-line'>{mainLocation.Address}</div>");

		if (!string.IsNullOrEmpty(mainLocation.Email))
			content.AppendLine($"<div class='header-line'>Email: {mainLocation.Email}</div>");

		if (!string.IsNullOrEmpty(mainLocation.Phone))
			content.AppendLine($"<div class='header-line'>Phone: {mainLocation.Phone}</div>");

		content.AppendLine("</div>");
		content.AppendLine("<div class='bold-separator'></div>");
	}

	/// <summary>
	/// Adds outlet details section for non-primary locations
	/// </summary>
	private static async Task AddOutletDetails(int locationId, StringBuilder content)
	{
		var location = await LedgerData.LoadLedgerByLocation(locationId);

		if (location is null)
			return;

		content.AppendLine("<div class='outlet-details'>");
		content.AppendLine("<div class='outlet-header'>OUTLET DETAILS</div>");

		// Outlet name
		content.AppendLine($"<div class='outlet-line'>Outlet: {location.Name}</div>");

		// GST Number (if available)
		if (!string.IsNullOrEmpty(location.GSTNo))
		{
			content.AppendLine($"<div class='outlet-line'>GST NO: {location.GSTNo}</div>");
		}

		// Address (if available)
		if (!string.IsNullOrEmpty(location.Address))
		{
			// For thermal printing, we might want to break long addresses
			if (location.Address.Length > 40)
			{
				// Try to break the address at a reasonable point for thermal printing
				var breakPoint = location.Address.LastIndexOf(' ', 40);
				if (breakPoint > 0)
				{
					var firstLine = location.Address.Substring(0, breakPoint);
					var secondLine = location.Address.Substring(breakPoint + 1);

					content.AppendLine($"<div class='outlet-line'>Address: {firstLine}</div>");
					content.AppendLine($"<div class='outlet-line'>         {secondLine}</div>");
				}
				else
				{
					content.AppendLine($"<div class='outlet-line'>Address: {location.Address}</div>");
				}
			}
			else
			{
				content.AppendLine($"<div class='outlet-line'>Address: {location.Address}</div>");
			}
		}

		// Contact information (if available)
		if (!string.IsNullOrEmpty(location.Phone))
			content.AppendLine($"<div class='outlet-line'>Contact: {location.Phone}</div>");

		// Email (if available)
		if (!string.IsNullOrEmpty(location.Email))
			content.AppendLine($"<div class='outlet-line'>Email: {location.Email}</div>");

		content.AppendLine("</div>");
		content.AppendLine("<div class='bold-separator'></div>");
	}

	private static async Task AddBillDetails(SaleModel sale, StringBuilder content)
	{
		var user = await CommonData.LoadTableDataById<UserModel>(TableNames.User, sale.UserId);

		content.AppendLine("<div class='bill-details'>");
		content.AppendLine($"<div class='detail-row'><span class='detail-label'>Bill No:</span> <span class='detail-value'>{sale.BillNo}</span></div>");
		content.AppendLine($"<div class='detail-row'><span class='detail-label'>Date:</span> <span class='detail-value'>{sale.SaleDateTime:dd/MM/yy HH:mm}</span></div>");
		content.AppendLine($"<div class='detail-row'><span class='detail-label'>User:</span> <span class='detail-value'>{user.Name}</span></div>");

		if (sale.OrderId.HasValue && sale.OrderId.Value > 0)
		{
			var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, sale.OrderId.Value);
			if (order is not null)
			{
				content.AppendLine($"<div class='detail-row'><span class='detail-label'>Order No:</span> <span class='detail-value'>{order.OrderNo}</span></div>");
				content.AppendLine($"<div class='detail-row'><span class='detail-label'>Order Date:</span> <span class='detail-value'>{order.OrderDateTime}</span></div>");
			}
		}

		if (sale.PartyId.HasValue && sale.PartyId.Value > 0)
		{
			var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);
			if (party is not null)
				content.AppendLine($"<div class='detail-row'><span class='detail-label'>Party:</span> <span class='detail-value'>{party.Name}</span></div>");
		}

		if (sale.CustomerId.HasValue && sale.CustomerId.Value > 0)
		{
			var customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, sale.CustomerId.Value);
			if (customer is not null)
			{
				content.AppendLine($"<div class='detail-row'><span class='detail-label'>Cust. Name:</span> <span class='detail-value'>{customer.Name}</span></div>");
				content.AppendLine($"<div class='detail-row'><span class='detail-label'>Cust. No.:</span> <span class='detail-value'>{customer.Number}</span></div>");
			}
		}

		content.AppendLine("</div>");
		content.AppendLine("<div class='bold-separator'></div>");
	}

	private static async Task AddItemDetails(SaleModel sale, StringBuilder content)
	{
		var saleDetails = await SaleData.LoadSaleDetailBySale(sale.Id);

		content.AppendLine("<table class='items-table'>");
		content.AppendLine("<thead>");
		content.AppendLine("<tr class='table-header'>");
		content.AppendLine("<th align='left'>Item</th>");
		content.AppendLine("<th align='center'>Qty</th>");
		content.AppendLine("<th align='right'>Rate</th>");
		content.AppendLine("<th align='right'>Amt</th>");
		content.AppendLine("</tr>");
		content.AppendLine("</thead>");
		content.AppendLine("<tbody>");

		foreach (var item in saleDetails)
		{
			var product = (await ProductData.LoadProductRateByProduct(item.ProductId)).Where(p => p.LocationId == sale.LocationId).FirstOrDefault();
			if (product is not null)
			{
				content.AppendLine("<tr class='table-row'>");
				content.AppendLine($"<td align='left'>{product.Name}</td>");
				content.AppendLine($"<td align='center'>{item.Quantity}</td>");
				content.AppendLine($"<td align='right'>{item.Rate.FormatDecimalWithTwoDigits()}</td>");
				content.AppendLine($"<td align='right'>{item.BaseTotal.FormatDecimalWithTwoDigits()}</td>");
				content.AppendLine("</tr>");
			}
		}
		content.AppendLine("</tbody>");
		content.AppendLine("</table>");
		content.AppendLine("<div class='bold-separator'></div>");
	}

	private static async Task AddTotalDetails(SaleModel sale, StringBuilder content)
	{
		var saleDetails = await SaleData.LoadSaleDetailBySale(sale.Id);

		// Calculate totals
		decimal baseTotal = saleDetails.Sum(x => x.BaseTotal);
		decimal discountTotal = saleDetails.Sum(x => x.DiscAmount);
		decimal subTotal = saleDetails.Sum(x => x.AfterDiscount);
		decimal cgstTotal = saleDetails.Sum(x => x.CGSTAmount);
		decimal sgstTotal = saleDetails.Sum(x => x.SGSTAmount);
		decimal igstTotal = saleDetails.Sum(x => x.IGSTAmount);
		decimal grandTotal = saleDetails.Sum(x => x.Total) + sale.RoundOff;

		// Get tax percentages (check if all items have same tax rate)
		var cgstPercent = saleDetails.FirstOrDefault()?.CGSTPercent ?? 0;
		var sgstPercent = saleDetails.FirstOrDefault()?.SGSTPercent ?? 0;
		var igstPercent = saleDetails.FirstOrDefault()?.IGSTPercent ?? 0;

		bool uniformDiscount = saleDetails.All(x => x.DiscPercent == sale.DiscPercent);

		// Check if tax rates are consistent across all items
		bool uniformTaxRates = saleDetails.All(x =>
			x.CGSTPercent == cgstPercent &&
			x.SGSTPercent == sgstPercent &&
			x.IGSTPercent == igstPercent);

		content.AppendLine("<table class='summary-table'>");
		content.AppendLine($"<tr><td class='summary-label'>Sub Total:</td><td align='right' class='summary-value'>{baseTotal.FormatIndianCurrency()}</td></tr>");

		if (discountTotal > 0)
		{
			if (uniformDiscount)
				content.AppendLine($"<tr><td class='summary-label'>Discount ({sale.DiscPercent}%):</td><td align='right' class='summary-value'>{discountTotal.FormatIndianCurrency()}</td></tr>");
			else
				content.AppendLine($"<tr><td class='summary-label'>Discount:</td><td align='right' class='summary-value'>{discountTotal.FormatIndianCurrency()}</td></tr>");
		}

		if (cgstTotal > 0)
		{
			if (uniformTaxRates)
				content.AppendLine($"<tr><td class='summary-label'>CGST ({cgstPercent}%):</td><td align='right' class='summary-value'>{cgstTotal.FormatIndianCurrency()}</td></tr>");
			else
				content.AppendLine($"<tr><td class='summary-label'>CGST:</td><td align='right' class='summary-value'>{cgstTotal.FormatIndianCurrency()}</td></tr>");
		}

		if (sgstTotal > 0)
		{
			if (uniformTaxRates)
				content.AppendLine($"<tr><td class='summary-label'>SGST ({sgstPercent}%):</td><td align='right' class='summary-value'>{sgstTotal.FormatIndianCurrency()}</td></tr>");
			else
				content.AppendLine($"<tr><td class='summary-label'>SGST:</td><td align='right' class='summary-value'>{sgstTotal.FormatIndianCurrency()}</td></tr>");
		}

		if (igstTotal > 0)
		{
			if (uniformTaxRates)
				content.AppendLine($"<tr><td class='summary-label'>IGST ({igstPercent}%):</td><td align='right' class='summary-value'>{igstTotal.FormatIndianCurrency()}</td></tr>");
			else
				content.AppendLine($"<tr><td class='summary-label'>IGST:</td><td align='right' class='summary-value'>{igstTotal.FormatIndianCurrency()}</td></tr>");
		}

		if (sale.RoundOff != 0)
			content.AppendLine($"<tr><td class='summary-label'>Round Off:</td><td align='right' class='summary-value'>{sale.RoundOff.FormatIndianCurrency()}</td></tr>");

		content.AppendLine("</table>");
		content.AppendLine("<div class='bold-separator'></div>");

		content.AppendLine("<table class='grand-total'>");
		content.AppendLine($"<tr><td class='grand-total-label'>Grand Total:</td><td align='right' class='grand-total-value'>{grandTotal.FormatIndianCurrency()}</td></tr>");
		content.AppendLine("</table>");

		CurrencyWordsConverter numericWords = new(new()
		{
			Culture = Culture.Hindi,
			OutputFormat = OutputFormat.English
		});

		string amountInWords = numericWords.ToWords(Math.Round(grandTotal));
		if (string.IsNullOrEmpty(amountInWords))
			amountInWords = "Zero";

		amountInWords += " Rupees Only";

		content.AppendLine("<div class='amount-words'>" + amountInWords + "</div>");

		content.AppendLine("<div class='amount-words'>Paid By " + GetPaymentMode(sale) + "</div>");
	}

	private static string GetPaymentMode(SaleModel sale)
	{
		if (sale.Cash > 0 && sale.Cash >= sale.Card + sale.UPI + sale.Credit)
			return "Cash";
		else if (sale.Card > 0 && sale.Card >= sale.Cash + sale.UPI + sale.Credit)
			return "Card";
		else if (sale.UPI > 0 && sale.UPI >= sale.Cash + sale.Card + sale.Credit)
			return "UPI";
		else if (sale.Credit > 0 && sale.Credit >= sale.Cash + sale.Card + sale.UPI)
			return "Credit";
		else if (sale.Cash > 0 || sale.Card > 0 || sale.UPI > 0 || sale.Credit > 0)
			return "Multiple";
		else
			return "Unknown";
	}

	private static void AddFooter(StringBuilder content)
	{
		string FooterLine = "Thanks. Visit Again";
		content.AppendLine("<div class='bold-separator'></div>");
		content.AppendLine($"<div class='footer-timestamp'>Printed: {DateTime.Now:dd/MM/yy HH:mm}</div>");
		content.AppendLine($"<div class='footer-text'>{FooterLine}</div>");
	}
}
