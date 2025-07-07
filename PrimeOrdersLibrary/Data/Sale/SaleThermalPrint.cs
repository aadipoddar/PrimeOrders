using System.Text;

using NumericWordsConversion;

using PrimeOrdersLibrary.Data.Common;
using PrimeOrdersLibrary.Models.Product;
using PrimeOrdersLibrary.Models.Sale;

namespace PrimeOrdersLibrary.Data.Sale;

public class SaleThermalPrint
{
	public static async Task<StringBuilder> GenerateThermalBill(SaleModel sale)
	{
		StringBuilder content = new();

		await AddHeader(sale.LocationId, content);

		await AddBillDetails(sale, content);

		await AddItemDetails(sale, content);

		await AddTotalDetails(sale, content);

		AddFooter(content);

		return content;
	}

	private static async Task AddHeader(int locationId, StringBuilder content)
	{
		string HeaderLine1 = "GST NO: XXXXXXX";
		string HeaderLine2 = "Salasar Foods Guwahati";
		string HeaderLine3 = "Mobile No: XXXXXX";

		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, locationId);

		content.AppendLine("<div class='header'>");
		content.AppendLine($"<div class='company-name'>{location.Name}</div>");
		content.AppendLine($"<div class='header-line'>{HeaderLine1}</div>");
		content.AppendLine($"<div class='header-line'>{HeaderLine2}</div>");
		content.AppendLine($"<div class='header-line'>{HeaderLine3}</div>");
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
				content.AppendLine($"<div class='detail-row'><span class='detail-label'>Order Date:</span> <span class='detail-value'>{order.OrderDate}</span></div>");
			}
		}

		if (sale.PartyId.HasValue && sale.PartyId.Value > 0)
		{
			var party = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, sale.PartyId.Value);
			if (party is not null)
				content.AppendLine($"<div class='detail-row'><span class='detail-label'>Party:</span> <span class='detail-value'>{party.Name}</span></div>");
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
			var product = await CommonData.LoadTableDataById<ProductModel>(TableNames.Product, item.ProductId);
			if (product != null)
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
		decimal grandTotal = saleDetails.Sum(x => x.Total);

		// Get tax percentages (check if all items have same tax rate)
		var cgstPercent = saleDetails.FirstOrDefault()?.CGSTPercent ?? 0;
		var sgstPercent = saleDetails.FirstOrDefault()?.SGSTPercent ?? 0;
		var igstPercent = saleDetails.FirstOrDefault()?.IGSTPercent ?? 0;

		// Check if tax rates are consistent across all items
		bool uniformTaxRates = saleDetails.All(x =>
			x.CGSTPercent == cgstPercent &&
			x.SGSTPercent == sgstPercent &&
			x.IGSTPercent == igstPercent);

		content.AppendLine("<table class='summary-table'>");
		content.AppendLine($"<tr><td class='summary-label'>Sub Total:</td><td align='right' class='summary-value'>{baseTotal.FormatIndianCurrency()}</td></tr>");

		if (discountTotal > 0)
			content.AppendLine($"<tr><td class='summary-label'>Discount ({sale.DiscPercent}%):</td><td align='right' class='summary-value'>{discountTotal.FormatIndianCurrency()}</td></tr>");

		if (cgstTotal > 0)
			content.AppendLine($"<tr><td class='summary-label'>CGST ({cgstPercent}%):</td><td align='right' class='summary-value'>{cgstTotal.FormatIndianCurrency()}</td></tr>");

		if (sgstTotal > 0)
			content.AppendLine($"<tr><td class='summary-label'>SGST ({sgstPercent}%):</td><td align='right' class='summary-value'>{sgstTotal.FormatIndianCurrency()}</td></tr>");

		if (igstTotal > 0)
			content.AppendLine($"<tr><td class='summary-label'>IGST ({igstPercent}%):</td><td align='right' class='summary-value'>{igstTotal.FormatIndianCurrency()}</td></tr>");

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
