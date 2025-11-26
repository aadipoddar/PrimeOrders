using Syncfusion.Pdf.Graphics;

namespace PrimeBakesLibrary.Exporting.Sales.Product;

public static class ProductLocationPDFExport
{
    public static MemoryStream ExportProductLocation<T>(IEnumerable<T> productLocationData)
    {
        var props = typeof(T).GetProperties();

        var enrichedData = productLocationData.Select(productLocation =>
        {
            var id = props.FirstOrDefault(p => p.Name == "Id")?.GetValue(productLocation);
            var location = props.FirstOrDefault(p => p.Name == "Location")?.GetValue(productLocation)?.ToString();
            var productCode = props.FirstOrDefault(p => p.Name == "ProductCode")?.GetValue(productLocation)?.ToString();
            var productName = props.FirstOrDefault(p => p.Name == "ProductName")?.GetValue(productLocation)?.ToString();
            var rate = props.FirstOrDefault(p => p.Name == "Rate")?.GetValue(productLocation);
            var status = props.FirstOrDefault(p => p.Name == "Status")?.GetValue(productLocation);

            return new
            {
                Id = id,
                Location = location,
                ProductCode = productCode,
                ProductName = productName,
                Rate = rate is decimal rateVal ? $"{rateVal:N2}" : "0.00",
                Status = status is bool statusBool && statusBool ? "Active" : "Deleted"
            };
        });

        var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
        {
            ["Id"] = new() { DisplayName = "ID", StringFormat = new PdfStringFormat(PdfTextAlignment.Center), IncludeInTotal = false },
            ["Location"] = new() { DisplayName = "Location", StringFormat = new PdfStringFormat(PdfTextAlignment.Left), IncludeInTotal = false },
            ["ProductCode"] = new() { DisplayName = "Product Code", StringFormat = new PdfStringFormat(PdfTextAlignment.Left), IncludeInTotal = false },
            ["ProductName"] = new() { DisplayName = "Product Name", StringFormat = new PdfStringFormat(PdfTextAlignment.Left), IsRequired = true },
            ["Rate"] = new() { DisplayName = "Rate", StringFormat = new PdfStringFormat(PdfTextAlignment.Right), IncludeInTotal = false },
            ["Status"] = new() { DisplayName = "Status", StringFormat = new PdfStringFormat(PdfTextAlignment.Center), IncludeInTotal = false }
        };

        var columnOrder = new List<string> { "Id", "Location", "ProductCode", "ProductName", "Rate", "Status" };

        return PDFReportExportUtil.ExportToPdf(
            enrichedData,
            "PRODUCT LOCATION MASTER",
            null,
            null,
            columnSettings,
            columnOrder,
            false,
            true,
            null,
            true
        );
    }
}
