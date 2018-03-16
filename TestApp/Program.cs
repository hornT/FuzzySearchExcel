using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExcelDataReader;
using OfficeOpenXml;

namespace TestApp
{
    class Program
    {
        private const int WRONG_COLUMN_PERCENT = 20;
        private const string filePath = @"d:\work\2017_1.xlsx";

        static void Main(string[] args)
        {
            //DataTable dt = GetDataSet();

            //var c = dt.Columns;

            //dt.Dispose();

            //using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            //{
            //    ExcelWorksheet workSheet = package.Workbook.Worksheets[2];

            //    int start = workSheet.Dimension.Start.Row;
            //    int end = workSheet.Dimension.End.Row;

                
            //}

            string[] columns = GetColumns();
            var r = columns.ToList();
        }

        private static string[] GetColumns()
        {
            using (ExcelPackage package = new ExcelPackage(new FileInfo(filePath)))
            {
                ExcelWorksheet workSheet = package.Workbook.Worksheets[2];

                int rowStart = workSheet.Dimension.Start.Row;
                int rowEnd = workSheet.Dimension.End.Row;
                int columnStart = workSheet.Dimension.Start.Column;
                int columnEnd = workSheet.Dimension.End.Column;

                string[] totalColumns = new string[workSheet.Dimension.Columns];
                for (int i = columnStart; i <= columnEnd; i++)
                {
                    totalColumns[i - columnStart] = workSheet.Cells[rowStart, i].Value?.ToString();
                }

                Dictionary<string, int> columnsDictionary = Enumerable.Range(0, totalColumns.Length).ToDictionary(x => totalColumns[x], x => x);
                // Регулярка отсеивает даты и числа
                Regex reg = new Regex("^[.,\\d]+$");
                double[] wrongCells = new double[totalColumns.Length];

                //SessionCache sc = GetSessionCache();
                //sc.Columns = columnsDictionary;

                // Пробежимся по всему документу
                // Если в колонке есть хотя бы 1 значение: пустое, дата, число или короче 3х символов, то не учитываем эту колонку
                for (int i = rowStart + 1; i <= rowEnd; i++)
                {
                    for (int columnIndex = 0; columnIndex < totalColumns.Length; columnIndex++)
                    {
                        object cell = workSheet.Cells[i, columnIndex + columnStart].Value;
                        if (cell == null)
                        {
                            //wrongCells[columnIndex]++;
                            continue;
                        }

                        string value = cell.ToString();
                        //CellType cellType = sheet.GetRow(i).GetCell(columnIndex).CellType;
                        if (/*cellType == CellType.Numeric ||*//* string.IsNullOrEmpty(value) ||*/ value.Length < 3 || reg.IsMatch(value))
                            wrongCells[columnIndex]++;
                    }
                }

                // Вычисляем % ненужных наименований в колонке
                List<string> columns = new List<string>();
                for (int i = 0; i < totalColumns.Length; i++)
                {
                    double wrongPercent = wrongCells[i] / rowEnd * 100;
                    if (wrongPercent < WRONG_COLUMN_PERCENT)
                        columns.Add(totalColumns[i]);
                }

                return columns.ToArray();
            }
        }

        static DataTable GetDataSet()
        {
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    using (DataSet result = reader.AsDataSet())
                    {
                        return result.Tables[0];
                    }

                }
            }
        }
    }
}
