using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

/// <summary>
/// Convert Purchase Return data to Invoice PDF format
/// </summary>
public static class PurchaseReturnInvoicePDFExport
{
    /// <summary>
    /// Export Purchase Return as a professional invoice PDF (automatically loads item names)
    /// </summary>
    /// <param name="purchaseReturnHeader">Purchase return header data</param>
    /// <param name="purchaseReturnDetails">Purchase return detail line items</param>
    /// <param name="company">Company information</param>
    /// <param name="party">Party/Supplier information</param>
    /// <param name="logoPath">Optional: Path to company logo</param>
    /// <param name="invoiceType">Type of document (PURCHASE RETURN, DEBIT NOTE, etc.)</param>
    /// <returns>MemoryStream containing the PDF file</returns>
    public static async Task<MemoryStream> ExportPurchaseReturnInvoice(
        PurchaseReturnModel purchaseReturnHeader,
        List<PurchaseReturnDetailModel> purchaseReturnDetails,
        CompanyModel company,
        LedgerModel party,
        string logoPath = null,
        string invoiceType = "PURCHASE RETURN")
    {
        // Load all items to get names
        var allItems = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

        // Map line items with actual item names
        var lineItems = purchaseReturnDetails.Select(detail =>
        {
            var item = allItems.FirstOrDefault(i => i.Id == detail.RawMaterialId);
            string itemName = item?.Name ?? $"Item #{detail.RawMaterialId}";

            return new PDFInvoiceExportUtil.InvoiceLineItem
            {
                ItemId = detail.RawMaterialId,
                ItemName = itemName,
                Quantity = detail.Quantity,
                UnitOfMeasurement = detail.UnitOfMeasurement,
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
            TransactionNo = purchaseReturnHeader.TransactionNo,
            TransactionDateTime = purchaseReturnHeader.TransactionDateTime,
            ItemsTotalAmount = purchaseReturnHeader.ItemsTotalAmount,
            OtherChargesAmount = purchaseReturnHeader.OtherChargesAmount,
            OtherChargesPercent = purchaseReturnHeader.OtherChargesPercent,
            CashDiscountAmount = purchaseReturnHeader.CashDiscountAmount,
            CashDiscountPercent = purchaseReturnHeader.CashDiscountPercent,
            RoundOffAmount = purchaseReturnHeader.RoundOffAmount,
            TotalAmount = purchaseReturnHeader.TotalAmount,
            Remarks = purchaseReturnHeader.Remarks,
            Status = purchaseReturnHeader.Status
        };

        // Generate invoice PDF with generic models
        return PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            lineItems,
            company,
            party,
            logoPath,
            invoiceType
        );
    }

    /// <summary>
    /// Export Purchase Return with item names (requires additional data)
    /// </summary>
    public static MemoryStream ExportPurchaseReturnInvoiceWithItems(
        PurchaseReturnModel purchaseReturnHeader,
        List<PurchaseReturnItemCartModel> purchaseReturnItems,
        CompanyModel company,
        LedgerModel party,
        string logoPath = null,
        string invoiceType = "PURCHASE RETURN")
    {
        // Map line items to generic model
        var lineItems = purchaseReturnItems.Select(item => new PDFInvoiceExportUtil.InvoiceLineItem
        {
            ItemId = item.ItemId,
            ItemName = item.ItemName,
            Quantity = item.Quantity,
            UnitOfMeasurement = item.UnitOfMeasurement,
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
            TransactionNo = purchaseReturnHeader.TransactionNo,
            TransactionDateTime = purchaseReturnHeader.TransactionDateTime,
            ItemsTotalAmount = purchaseReturnHeader.ItemsTotalAmount,
            OtherChargesAmount = purchaseReturnHeader.OtherChargesAmount,
            OtherChargesPercent = purchaseReturnHeader.OtherChargesPercent,
            CashDiscountAmount = purchaseReturnHeader.CashDiscountAmount,
            CashDiscountPercent = purchaseReturnHeader.CashDiscountPercent,
            RoundOffAmount = purchaseReturnHeader.RoundOffAmount,
            TotalAmount = purchaseReturnHeader.TotalAmount,
            Remarks = purchaseReturnHeader.Remarks,
            Status = purchaseReturnHeader.Status
        };

        // Generate invoice PDF with generic models
        return PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            lineItems,
            company,
            party,
            logoPath,
            invoiceType
        );
    }
}
