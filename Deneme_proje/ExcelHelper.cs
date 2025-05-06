using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;

namespace Deneme_proje
{


    public static class ExcelHelper
    {
        public static MemoryStream GenerateExcel<T>(IEnumerable<T> data, Dictionary<string, string> columnMappings)
        {
            var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");

            // Column headers
            int columnIndex = 1;
            foreach (var column in columnMappings.Keys)
            {
                worksheet.Cells[1, columnIndex].Value = column;
                columnIndex++;
            }

            // Data rows
            int rowIndex = 2;
            foreach (var item in data)
            {
                columnIndex = 1;
                foreach (var property in columnMappings.Values)
                {
                    var value = item.GetType().GetProperty(property)?.GetValue(item, null);
                    worksheet.Cells[rowIndex, columnIndex].Value = value;
                    columnIndex++;
                }
                rowIndex++;
            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }
    }
}

