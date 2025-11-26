using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

/// <summary>
/// Convert Kitchen Production data to Invoice PDF format using generic PDFInvoiceExportUtil
/// </summary>
public static class KitchenProductionInvoicePDFExport
{
    /// <summary>
    /// Export Kitchen Production as a professional invoice PDF (automatically loads product names)
    /// Uses generic invoice utility for consistency with other invoices
    /// </summary>
    /// <param name="kitchenProductionHeader">Kitchen Production header data</param>
    /// <param name="kitchenProductionDetails">Kitchen Production detail line items</param>
    /// <param name="company">Company information</param>
    /// <param name="kitchen">Kitchen/Location information</param>
    /// <param name="logoPath">Optional: Path to company logo</param>
    /// <param name="invoiceType">Type of document (KITCHEN PRODUCTION, etc.)</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportKitchenProductionInvoice(
        KitchenProductionModel kitchenProductionHeader,
        List<KitchenProductionDetailModel> kitchenProductionDetails,
        CompanyModel company,
        LocationModel kitchen,
        string logoPath = null,
        string invoiceType = "KITCHEN PRODUCTION INVOICE")
    {
        // Load all products to get names
        var allProducts = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

        // Map line items with actual product names
        var cartItems = kitchenProductionDetails.Select(detail =>
        {
            var product = allProducts.FirstOrDefault(p => p.Id == detail.ProductId);
            string productName = product?.Name ?? $"Product #{detail.ProductId}";

            return new KitchenProductionProductCartModel
            {
                ProductId = detail.ProductId,
                ProductName = productName,
                Quantity = detail.Quantity,
                Rate = detail.Rate,
                Total = detail.Total,
                Remarks = detail.Remarks
            };
        }).ToList();

        // Use the simplified export method
        return ExportKitchenProductionInvoiceWithItems(
            kitchenProductionHeader,
            cartItems,
            company,
            kitchen,
            logoPath,
            invoiceType
        );
    }

    /// <summary>
    /// Export Kitchen Production with product names already loaded (requires additional data)
    /// Uses generic invoice utility with zero discount/tax for simplified format
    /// </summary>
    public static MemoryStream ExportKitchenProductionInvoiceWithItems(
        KitchenProductionModel kitchenProductionHeader,
        List<KitchenProductionProductCartModel> kitchenProductionProducts,
        CompanyModel company,
        LocationModel kitchen,
        string logoPath = null,
        string invoiceType = "KITCHEN PRODUCTION INVOICE")
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

        // Map line items to generic invoice format (no discount/tax for kitchen production)
        var lineItems = kitchenProductionProducts.Select(product => new PDFInvoiceExportUtil.InvoiceLineItem
        {
            ItemId = product.ProductId,
            ItemName = product.ProductName,
            Quantity = product.Quantity,
            UnitOfMeasurement = "", // No unit of measurement for products
            Rate = product.Rate,
            DiscountPercent = 0, // No discount for kitchen production
            AfterDiscount = product.Quantity * product.Rate, // Same as subtotal
            CGSTPercent = 0, // No tax for kitchen production
            SGSTPercent = 0,
            IGSTPercent = 0,
            TotalTaxAmount = 0,
            Total = product.Total
        }).ToList();

        // Map invoice header data
        var invoiceData = new PDFInvoiceExportUtil.InvoiceData
        {
            TransactionNo = kitchenProductionHeader.TransactionNo,
            TransactionDateTime = kitchenProductionHeader.TransactionDateTime,
            ItemsTotalAmount = kitchenProductionHeader.TotalAmount,
            OtherChargesAmount = 0, // Kitchen production doesn't have other charges
            OtherChargesPercent = 0,
            CashDiscountAmount = 0, // Kitchen production doesn't have cash discount
            CashDiscountPercent = 0,
            RoundOffAmount = 0, // Kitchen production doesn't have round off
            TotalAmount = kitchenProductionHeader.TotalAmount,
            Remarks = kitchenProductionHeader.Remarks,
            Status = kitchenProductionHeader.Status
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
