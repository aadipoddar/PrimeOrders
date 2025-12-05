using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Sales.Product;

/// <summary>
/// Excel export functionality for Tax
/// </summary>
public static class TaxExcelExport
{
    /// <summary>
    /// Export Tax data to Excel with custom column order and formatting
    /// </summary>
    /// <param name="taxData">Collection of tax records</param>
    /// <returns>MemoryStream containing the Excel file</returns>
    public static async Task<MemoryStream> ExportTax(IEnumerable<TaxModel> taxData)
    {
        // Create enriched data with status formatting
        var enrichedData = taxData.Select(tax => new
        {
            tax.Id,
            tax.Code,
            tax.CGST,
            tax.SGST,
            tax.IGST,
            Total = tax.CGST + tax.SGST,
            Inclusive = tax.Inclusive ? "Yes" : "No",
            Extra = tax.Extra ? "Yes" : "No",
            tax.Remarks,
            Status = tax.Status ? "Active" : "Deleted"
        });

        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
        {
            // ID - Center aligned, no totals
            [nameof(TaxModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Tax Code - Left aligned
            [nameof(TaxModel.Code)] = new() { DisplayName = "Tax Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },

            // Tax percentages - Right aligned with 2 decimal places
            [nameof(TaxModel.CGST)] = new() { DisplayName = "CGST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },
            [nameof(TaxModel.SGST)] = new() { DisplayName = "SGST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },
            [nameof(TaxModel.IGST)] = new() { DisplayName = "IGST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },
            ["Total"] = new() { DisplayName = "Total GST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },

            // Boolean fields - Center aligned
            [nameof(TaxModel.Inclusive)] = new() { DisplayName = "Inclusive", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            [nameof(TaxModel.Extra)] = new() { DisplayName = "Extra", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Remarks - Left aligned
            [nameof(TaxModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Status - Center aligned
            [nameof(TaxModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order
        List<string> columnOrder =
        [
            nameof(TaxModel.Id),
            nameof(TaxModel.Code),
            nameof(TaxModel.CGST),
            nameof(TaxModel.SGST),
            nameof(TaxModel.IGST),
            "Total",
            nameof(TaxModel.Inclusive),
            nameof(TaxModel.Extra),
            nameof(TaxModel.Remarks),
            nameof(TaxModel.Status)
        ];

        // Call the generic Excel export utility
        return await ExcelReportExportUtil.ExportToExcel(
            enrichedData,
            "TAX MASTER",
            "Tax Data",
            null,
            null,
            columnSettings,
            columnOrder
        );
    }
}
