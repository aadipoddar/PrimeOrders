using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

/// <summary>
/// Convert Sale Return data to Invoice PDF format
/// </summary>
public static class SaleReturnInvoicePDFExport
{
    /// <summary>
    /// Export Sale Return as a professional invoice PDF (automatically loads item names)
    /// </summary>
    /// <param name="saleReturnHeader">Sale return header data</param>
    /// <param name="saleReturnDetails">Sale return detail line items</param>
    /// <param name="company">Company information</param>
    /// <param name="party">Party/Ledger information (can be null)</param>
    /// <param name="customer">Customer information with name and phone (can be null)</param>
    /// <param name="logoPath">Optional: Path to company logo</param>
    /// <param name="invoiceType">Type of document (SALE RETURN, CREDIT NOTE, etc.)</param>
    /// <param name="outlet">Optional: Outlet/Location name</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportSaleReturnInvoice(
        SaleReturnModel saleReturnHeader,
        List<SaleReturnDetailModel> saleReturnDetails,
        CompanyModel company,
        LedgerModel party,
        CustomerModel customer,
        string logoPath = null,
        string invoiceType = "SALE RETURN INVOICE",
        string outlet = null)
    {
        // Load all items to get names
        var allItems = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

        // Map line items with actual item names
        var lineItems = saleReturnDetails.Select(detail =>
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
            TransactionNo = saleReturnHeader.TransactionNo,
            TransactionDateTime = saleReturnHeader.TransactionDateTime,
            ItemsTotalAmount = saleReturnHeader.TotalAfterTax,
            OtherChargesAmount = saleReturnHeader.OtherChargesAmount,
            OtherChargesPercent = saleReturnHeader.OtherChargesPercent,
            CashDiscountAmount = saleReturnHeader.DiscountAmount,
            CashDiscountPercent = saleReturnHeader.DiscountPercent,
            RoundOffAmount = saleReturnHeader.RoundOffAmount,
            TotalAmount = saleReturnHeader.TotalAmount,
            Cash = saleReturnHeader.Cash,
            Card = saleReturnHeader.Card,
            UPI = saleReturnHeader.UPI,
            Credit = saleReturnHeader.Credit,
            Remarks = saleReturnHeader.Remarks,
            Status = saleReturnHeader.Status
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
    /// Export Sale Return with item names (requires additional data)
    /// </summary>
    public static async Task<MemoryStream> ExportSaleReturnInvoiceWithItems(
        SaleReturnModel saleReturnHeader,
        List<SaleReturnItemCartModel> saleReturnItems,
        CompanyModel company,
        LedgerModel party,
        CustomerModel customer,
        string logoPath = null,
        string invoiceType = "SALE RETURN",
        string outlet = null)
    {
        // Map line items to generic model
        var lineItems = saleReturnItems.Select(item => new PDFInvoiceExportUtil.InvoiceLineItem
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
            TransactionNo = saleReturnHeader.TransactionNo,
            TransactionDateTime = saleReturnHeader.TransactionDateTime,
            ItemsTotalAmount = saleReturnHeader.TotalAfterTax,
            OtherChargesAmount = saleReturnHeader.OtherChargesAmount,
            OtherChargesPercent = saleReturnHeader.OtherChargesPercent,
            CashDiscountAmount = saleReturnHeader.DiscountAmount,
            CashDiscountPercent = saleReturnHeader.DiscountPercent,
            RoundOffAmount = saleReturnHeader.RoundOffAmount,
            TotalAmount = saleReturnHeader.TotalAmount,
            Cash = saleReturnHeader.Cash,
            Card = saleReturnHeader.Card,
            UPI = saleReturnHeader.UPI,
            Credit = saleReturnHeader.Credit,
            Remarks = saleReturnHeader.Remarks,
            Status = saleReturnHeader.Status
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