namespace PrimeBakesLibrary.Exporting.Sales.Product;

public static class ProductLocationExcelExport
{
    public static async Task<MemoryStream> ExportProductLocation<T>(IEnumerable<T> productLocationData)
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
                Rate = rate,
                Status = status is bool statusBool && statusBool ? "Active" : "Deleted"
            };
        });

        var columnSettings = new Dictionary<string, ExcelExportUtil.ColumnSetting>
        {
            ["Id"] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
            ["Location"] = new() { DisplayName = "Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["ProductCode"] = new() { DisplayName = "Product Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
            ["ProductName"] = new() { DisplayName = "Product Name", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IsRequired = true },
            ["Rate"] = new() { DisplayName = "Rate", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, Format = "0.00", IncludeInTotal = false },
            ["Status"] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
        };

        var columnOrder = new List<string> { "Id", "Location", "ProductCode", "ProductName", "Rate", "Status" };

        return await ExcelExportUtil.ExportToExcel(
            enrichedData,
            "PRODUCT LOCATION MASTER",
            "Product Location Data",
            null,
            null,
            columnSettings,
            columnOrder
        );
    }
}
