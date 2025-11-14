using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// Convert Kitchen Issue data to Invoice PDF format using generic PDFInvoiceExportUtil
/// </summary>
public static class KitchenIssueInvoicePDFExport
{
    /// <summary>
    /// Export Kitchen Issue as a professional invoice PDF (automatically loads item names)
    /// Uses generic invoice utility for consistency with purchase invoices
    /// </summary>
    /// <param name="kitchenIssueHeader">Kitchen Issue header data</param>
    /// <param name="kitchenIssueDetails">Kitchen Issue detail line items</param>
    /// <param name="company">Company information</param>
    /// <param name="kitchen">Kitchen/Location information</param>
    /// <param name="logoPath">Optional: Path to company logo</param>
    /// <param name="invoiceType">Type of document (KITCHEN ISSUE, MATERIAL ISSUE, etc.)</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportKitchenIssueInvoice(
        KitchenIssueModel kitchenIssueHeader,
        List<KitchenIssueDetailModel> kitchenIssueDetails,
        CompanyModel company,
        LocationModel kitchen,
        string logoPath = null,
        string invoiceType = "KITCHEN ISSUE INVOICE")
    {
        // Load all raw materials to get names
        var allItems = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

        // Map line items with actual item names
        var cartItems = kitchenIssueDetails.Select(detail =>
        {
            var item = allItems.FirstOrDefault(i => i.Id == detail.RawMaterialId);
            string itemName = item?.Name ?? $"Item #{detail.RawMaterialId}";

            return new KitchenIssueItemCartModel
            {
                ItemId = detail.RawMaterialId,
                ItemName = itemName,
                Quantity = detail.Quantity,
                UnitOfMeasurement = detail.UnitOfMeasurement,
                Rate = detail.Rate,
                Total = detail.Total,
                Remarks = detail.Remarks
            };
        }).ToList();

        // Use the simplified export method
        return ExportKitchenIssueInvoiceWithItems(
            kitchenIssueHeader,
            cartItems,
            company,
            kitchen,
            logoPath,
            invoiceType
        );
    }

    /// <summary>
    /// Export Kitchen Issue with item names already loaded (requires additional data)
    /// Uses generic invoice utility with zero discount/tax for simplified format
    /// </summary>
    public static MemoryStream ExportKitchenIssueInvoiceWithItems(
        KitchenIssueModel kitchenIssueHeader,
        List<KitchenIssueItemCartModel> kitchenIssueItems,
        CompanyModel company,
        LocationModel kitchen,
        string logoPath = null,
        string invoiceType = "KITCHEN ISSUE INVOICE")
    {
        // Convert LocationModel to LedgerModel format for generic utility
        var kitchenAsLedger = new LedgerModel
        {
            Name = kitchen.Name,
            Address = "", // LocationModel doesn't have Address
            Phone = "", // LocationModel doesn't have Phone
            Email = "", // LocationModel doesn't have Email
            GSTNo = "" // LocationModel doesn't have GSTNo
        };

        // Map line items to generic invoice format (no discount/tax for kitchen issues)
        var lineItems = kitchenIssueItems.Select(item => new PDFInvoiceExportUtil.InvoiceLineItem
        {
            ItemId = item.ItemId,
            ItemName = item.ItemName,
            Quantity = item.Quantity,
            UnitOfMeasurement = item.UnitOfMeasurement,
            Rate = item.Rate,
            DiscountPercent = 0, // No discount for kitchen issues
            AfterDiscount = item.Quantity * item.Rate, // Same as subtotal
            CGSTPercent = 0, // No tax for kitchen issues
            SGSTPercent = 0,
            IGSTPercent = 0,
            TotalTaxAmount = 0,
            Total = item.Total
        }).ToList();

        // Map invoice header data
        var invoiceData = new PDFInvoiceExportUtil.InvoiceData
        {
            TransactionNo = kitchenIssueHeader.TransactionNo,
            TransactionDateTime = kitchenIssueHeader.TransactionDateTime,
            ItemsTotalAmount = kitchenIssueHeader.TotalAmount,
            OtherChargesAmount = 0, // Kitchen issues don't have other charges
            OtherChargesPercent = 0,
            CashDiscountAmount = 0, // Kitchen issues don't have cash discount
            CashDiscountPercent = 0,
            RoundOffAmount = 0, // Kitchen issues don't have round off
            TotalAmount = kitchenIssueHeader.TotalAmount,
            Remarks = kitchenIssueHeader.Remarks,
            Status = kitchenIssueHeader.Status
        };

        // Generate invoice PDF using generic utility
        return PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            lineItems,
            company,
            kitchenAsLedger,
            logoPath,
            invoiceType
        );
    }
}
