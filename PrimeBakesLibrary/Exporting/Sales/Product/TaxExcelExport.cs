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
    public static MemoryStream ExportTax(IEnumerable<TaxModel> taxData)
    {
        // Create enriched data with status formatting
        var enrichedData = taxData.Select(tax => new
        {
            tax.Id,
            tax.Code,
            CGST = tax.CGST,
            SGST = tax.SGST,
            IGST = tax.IGST,
            Total = tax.CGST + tax.SGST,
            Inclusive = tax.Inclusive ? "Yes" : "No",
            Extra = tax.Extra ? "Yes" : "No",
            tax.Remarks,
            Status = tax.Status ? "Active" : "Deleted"
        });

        // Define custom column settings
        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            // ID - Center aligned, no totals
            ["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Tax Code - Left aligned
            ["Code"] = new() { DisplayName = "Tax Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },

            // Tax percentages - Right aligned with 2 decimal places
            ["CGST"] = new() { DisplayName = "CGST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },
            ["SGST"] = new() { DisplayName = "SGST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },
            ["IGST"] = new() { DisplayName = "IGST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },
            ["Total"] = new() { DisplayName = "Total GST %", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },

            // Boolean fields - Center aligned
            ["Inclusive"] = new() { DisplayName = "Inclusive", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["Extra"] = new() { DisplayName = "Extra", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

            // Remarks - Left aligned
            ["Remarks"] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

            // Status - Center aligned
            ["Status"] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        // Define column order
        List<string> columnOrder =
        [
            "Id", "Code", "CGST", "SGST", "IGST", "Total", "Inclusive", "Extra", "Remarks", "Status"
        ];

        // Call the generic Excel export utility
        return ExcelExportUtil.ExportToExcel(
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
