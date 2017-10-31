using FuzzySearch;
using NetOffice.ExcelApi;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private const string FILE_KEY = "excelFile";
        private const string FIRST_ROW_INDEX_KEY = "firstRowIndex";
        private const string LAST_ROW_INDEX_KEY = "lastRowIndex";
        private const string COLUMN_INDEX_KEY = "columnIndex";
        private const string FULL_RANGE_VALUES_KEY = "fullRangeValues";

        // TODO config
        private const double fuzzyness = 0.7;
        private const double autoFuzzyness = 0.9;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Fuzzy _fuzzy = new Fuzzy();

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadFile(string file)
        {
            byte[] fileArr = new byte[0];
            if (string.IsNullOrEmpty(file) == true)
                return Json(new { message = "Файл пуст"});

            var fl = file.Split(',')[1];
            fileArr = Convert.FromBase64String(fl);

            string tempPath = Path.GetTempFileName();
            System.IO.File.WriteAllBytes(tempPath, fileArr);

            Session[FILE_KEY] = tempPath;

            string[] columns = ReadExcelFile(tempPath);

            return Json(new { message = "Файл успешно загружен", columns });
        }

        /// <summary>
        /// Работа с файлом
        /// </summary>
        /// <param name="fileName"></param>
        private string[] ReadExcelFile(string fileName)
        {
            var app = new Application
            {
                DisplayAlerts = false,
                ScreenUpdating = false,
                IgnoreRemoteRequests = true
            };

            string[] columns;
            try
            {
                var excelBook = app.Workbooks.Open(fileName);
                var sheet = excelBook.Sheets.FirstOrDefault() as Worksheet;
                var fullRange = sheet.UsedRange;
                int firstRowIndex = fullRange.Row;
                var firstColIndex = fullRange.Column;
                var fullRangeValues = (object[,])fullRange.Value;
                int lastRowIndex = fullRangeValues.GetLength(0);
                var lastColumnIndex = fullRangeValues.GetLength(1);

                
                excelBook.Close();
                app.Quit();

                _logger.Info($"Строки с {firstRowIndex} по {lastRowIndex}. Колонки с {firstColIndex} по {lastColumnIndex}");

                // Заполняем список колонок
                columns = new string[lastColumnIndex];
                for (int i = firstColIndex; i <= lastColumnIndex; i++)
                    columns[i - firstColIndex] = fullRangeValues[firstRowIndex, i] as string;

                Session[FIRST_ROW_INDEX_KEY] = firstRowIndex;
                Session[LAST_ROW_INDEX_KEY] = lastRowIndex;
                Session[FULL_RANGE_VALUES_KEY] = fullRangeValues;

                return columns;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Первоначальная обработка файла
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public ActionResult ProcessFile(int columnIndex)
        {
            PrepareResult prepareResult = PrepareAutoCorrection(columnIndex + 1, fuzzyness, autoFuzzyness);
            if (prepareResult == null)
                return Json(new { message = "Не удалось обработать файл"});

            return Json(new { message = "Файл успешно обработан", prepareResult });
        }

        /// <summary>
        /// Выполнить автокоррекцию
        /// </summary>
        /// <param name="columnIndex"></param>
        private PrepareResult PrepareAutoCorrection(int columnIndex, double fuzzyness, double autoCorrectionFuzzyness)
        {
            // Вычитываем все значения из выбранной колонки
            int firstRowIndex = (int)Session[FIRST_ROW_INDEX_KEY];
            int lastRowIndex = (int)Session[LAST_ROW_INDEX_KEY];
            object[,] fullRangeValues = (object[,])Session[FULL_RANGE_VALUES_KEY];
            Session[COLUMN_INDEX_KEY] = columnIndex;
            
            string[] values = new string[lastRowIndex - firstRowIndex];
            for (int i = firstRowIndex + 1; i <= lastRowIndex; i++)
            {
                values[i - firstRowIndex - 1] = fullRangeValues[i, columnIndex] as string;
            }

            return _fuzzy.Prepare(values, fuzzyness, autoCorrectionFuzzyness);
        }
    }
}