using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

/// <summary>
/// Convert Sale data to Invoice PDF format
/// </summary>
public static class SaleInvoicePDFExport
{
    /// <summary>
    /// Export Sale as a professional invoice PDF (automatically loads item names)
    /// </summary>
    /// <param name="saleHeader">Sale header data</param>
    /// <param name="saleDetails">Sale detail line items</param>
    /// <param name="company">Company information</param>
    /// <param name="party">Party/Ledger information (can be null)</param>
    /// <param name="customer">Customer information with name and phone (can be null)</param>
    /// <param name="logoPath">Optional: Path to company logo</param>
    /// <param name="invoiceType">Type of document (SALE INVOICE, TAX INVOICE, etc.)</param>
    /// <param name="outlet">Optional: Outlet/Location name</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportSaleInvoice(
        SaleModel saleHeader,
        List<SaleDetailModel> saleDetails,
        CompanyModel company,
        LedgerModel party,
        CustomerModel customer,
        string orderTransactionNo = null,
        DateTime? orderDateTime = null,
        string logoPath = null,
        string invoiceType = "SALE INVOICE",
        string outlet = null)
    {
        // Load all items to get names
        var allItems = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

        // Map line items with actual item names
        var lineItems = saleDetails.Select(detail =>
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
            TransactionNo = saleHeader.TransactionNo,
            TransactionDateTime = saleHeader.TransactionDateTime,
            OrderTransactionNo = orderTransactionNo,
            OrderDateTime = orderDateTime,
            ItemsTotalAmount = saleHeader.TotalAfterTax,
            OtherChargesAmount = saleHeader.OtherChargesAmount,
            OtherChargesPercent = saleHeader.OtherChargesPercent,
            CashDiscountAmount = saleHeader.DiscountAmount,
            CashDiscountPercent = saleHeader.DiscountPercent,
            RoundOffAmount = saleHeader.RoundOffAmount,
            TotalAmount = saleHeader.TotalAmount,
            Cash = saleHeader.Cash,
            Card = saleHeader.Card,
            UPI = saleHeader.UPI,
            Credit = saleHeader.Credit,
            Remarks = saleHeader.Remarks,
            Status = saleHeader.Status
        };

        // Generate invoice PDF with generic models
        return await PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            lineItems,
            company,
            party,
            logoPath,
            invoiceType,
            outlet,
            customer
        );
    }

    /// <summary>
    /// Export Sale with item names (requires additional data)
    /// </summary>
    public static async Task<MemoryStream> ExportSaleInvoiceWithItems(
        SaleModel saleHeader,
        List<SaleItemCartModel> saleItems,
        CompanyModel company,
        LedgerModel party,
        CustomerModel customer,
        string orderTransactionNo = null,
        DateTime? orderDateTime = null,
        string logoPath = null,
        string invoiceType = "SALE INVOICE",
        string outlet = null)
    {
        // Map line items to generic model
        var lineItems = saleItems.Select(item => new PDFInvoiceExportUtil.InvoiceLineItem
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
            TransactionNo = saleHeader.TransactionNo,
            TransactionDateTime = saleHeader.TransactionDateTime,
            OrderTransactionNo = orderTransactionNo,
            OrderDateTime = orderDateTime,
            ItemsTotalAmount = saleHeader.TotalAfterTax,
            OtherChargesAmount = saleHeader.OtherChargesAmount,
            OtherChargesPercent = saleHeader.OtherChargesPercent,
            CashDiscountAmount = saleHeader.DiscountAmount,
            CashDiscountPercent = saleHeader.DiscountPercent,
            RoundOffAmount = saleHeader.RoundOffAmount,
            TotalAmount = saleHeader.TotalAmount,
            Cash = saleHeader.Cash,
            Card = saleHeader.Card,
            UPI = saleHeader.UPI,
            Credit = saleHeader.Credit,
            Remarks = saleHeader.Remarks,
            Status = saleHeader.Status
        };

        // Generate invoice PDF with generic models
        return await PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            lineItems,
            company,
            party,
            logoPath,
            invoiceType,
            outlet,
            customer
        );
    }
}
