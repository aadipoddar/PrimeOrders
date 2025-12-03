using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Exporting.Sales.StockTransfer;

/// <summary>
/// Convert Stock Transfer data to Invoice PDF format
/// </summary>
public static class StockTransferInvoicePDFExport
{
    /// <summary>
    /// Export Stock Transfer as a professional invoice PDF (automatically loads item names and location details from ledger)
    /// </summary>
    /// <param name="stockTransferHeader">Stock Transfer header data</param>
    /// <param name="stockTransferDetails">Stock Transfer detail line items</param>
    /// <param name="company">Company information</param>
    /// <param name="logoPath">Optional: Path to company logo</param>
    /// <param name="invoiceType">Type of document (STOCK TRANSFER INVOICE, etc.)</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportStockTransferInvoice(
        StockTransferModel stockTransferHeader,
        List<StockTransferDetailModel> stockTransferDetails,
        CompanyModel company,
        string logoPath = null,
        string invoiceType = "STOCK TRANSFER INVOICE")
    {
        // Load From Location (source) and To Location (destination) ledgers
        var fromLocationLedger = await LedgerData.LoadLedgerByLocation(stockTransferHeader.LocationId);
        var toLocationLedger = await LedgerData.LoadLedgerByLocation(stockTransferHeader.ToLocationId);

        // Load all items to get names
        var allItems = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

        // Map line items with actual item names
        var lineItems = stockTransferDetails.Select(detail =>
        {
            var item = allItems.FirstOrDefault(i => i.Id == detail.ProductId);
            string itemName = item?.Name ?? $"Item #{detail.ProductId}";

            return new PDFInvoiceExportUtil.InvoiceLineItem
            {
                ItemId = detail.ProductId,
                ItemName = itemName,
                Quantity = detail.Quantity,
                Rate = detail.Rate,
                DiscountPercent = detail.DiscountPercent,
                AfterDiscount = detail.AfterDiscount,
                CGSTPercent = detail.InclusiveTax ? 0 : detail.CGSTPercent,
                SGSTPercent = detail.InclusiveTax ? 0 : detail.SGSTPercent,
                IGSTPercent = detail.InclusiveTax ? 0 : detail.IGSTPercent,
                TotalTaxAmount = detail.InclusiveTax ? 0 : detail.TotalTaxAmount,
                Total = detail.Total
            };
        }).ToList();

        // Map invoice header data
        var invoiceData = new PDFInvoiceExportUtil.InvoiceData
        {
            TransactionNo = stockTransferHeader.TransactionNo,
            TransactionDateTime = stockTransferHeader.TransactionDateTime,
            ItemsTotalAmount = stockTransferHeader.TotalAfterTax,
            OtherChargesAmount = stockTransferHeader.OtherChargesAmount,
            OtherChargesPercent = stockTransferHeader.OtherChargesPercent,
            CashDiscountAmount = stockTransferHeader.DiscountAmount,
            CashDiscountPercent = stockTransferHeader.DiscountPercent,
            RoundOffAmount = stockTransferHeader.RoundOffAmount,
            TotalAmount = stockTransferHeader.TotalAmount,
            Cash = stockTransferHeader.Cash,
            Card = stockTransferHeader.Card,
            UPI = stockTransferHeader.UPI,
            Credit = stockTransferHeader.Credit,
            Remarks = stockTransferHeader.Remarks,
            Status = stockTransferHeader.Status
        };

        // Generate invoice PDF - From Location is the source, To Location is the destination
        return await PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            lineItems,
            company,
            toLocationLedger, // Bill To = To Location (destination)
            logoPath,
            invoiceType,
            fromLocationLedger?.Name // Outlet = From Location (source)
        );
    }

    /// <summary>
    /// Export Stock Transfer with item names already loaded (requires StockTransferItemCartModel data)
    /// </summary>
    /// <param name="stockTransferHeader">Stock Transfer header data</param>
    /// <param name="stockTransferItems">Stock Transfer items with names already loaded</param>
    /// <param name="company">Company information</param>
    /// <param name="logoPath">Optional: Path to company logo</param>
    /// <param name="invoiceType">Type of document (STOCK TRANSFER INVOICE, etc.)</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportStockTransferInvoiceWithItems(
        StockTransferModel stockTransferHeader,
        List<StockTransferItemCartModel> stockTransferItems,
        CompanyModel company,
        string logoPath = null,
        string invoiceType = "STOCK TRANSFER INVOICE")
    {
        // Load From Location (source) and To Location (destination) ledgers
        var fromLocationLedger = await LedgerData.LoadLedgerByLocation(stockTransferHeader.LocationId);
        var toLocationLedger = await LedgerData.LoadLedgerByLocation(stockTransferHeader.ToLocationId);

        // Map line items to generic model
        var lineItems = stockTransferItems.Select(item => new PDFInvoiceExportUtil.InvoiceLineItem
        {
            ItemId = item.ItemId,
            ItemName = item.ItemName,
            Quantity = item.Quantity,
            Rate = item.Rate,
            DiscountPercent = item.DiscountPercent,
            AfterDiscount = item.AfterDiscount,
            CGSTPercent = item.InclusiveTax ? 0 : item.CGSTPercent,
            SGSTPercent = item.InclusiveTax ? 0 : item.SGSTPercent,
            IGSTPercent = item.InclusiveTax ? 0 : item.IGSTPercent,
            TotalTaxAmount = item.InclusiveTax ? 0 : item.TotalTaxAmount,
            Total = item.Total
        }).ToList();

        // Map invoice header data
        var invoiceData = new PDFInvoiceExportUtil.InvoiceData
        {
            TransactionNo = stockTransferHeader.TransactionNo,
            TransactionDateTime = stockTransferHeader.TransactionDateTime,
            ItemsTotalAmount = stockTransferHeader.TotalAfterTax,
            OtherChargesAmount = stockTransferHeader.OtherChargesAmount,
            OtherChargesPercent = stockTransferHeader.OtherChargesPercent,
            CashDiscountAmount = stockTransferHeader.DiscountAmount,
            CashDiscountPercent = stockTransferHeader.DiscountPercent,
            RoundOffAmount = stockTransferHeader.RoundOffAmount,
            TotalAmount = stockTransferHeader.TotalAmount,
            Cash = stockTransferHeader.Cash,
            Card = stockTransferHeader.Card,
            UPI = stockTransferHeader.UPI,
            Credit = stockTransferHeader.Credit,
            Remarks = stockTransferHeader.Remarks,
            Status = stockTransferHeader.Status
        };

        // Generate invoice PDF - From Location is the source, To Location is the destination
        return await PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            lineItems,
            company,
            toLocationLedger, // Bill To = To Location (destination)
            logoPath,
            invoiceType,
            fromLocationLedger?.Name // Outlet = From Location (source)
        );
    }
}
