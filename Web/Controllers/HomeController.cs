﻿using FuzzySearch;
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
        private const string FILE_NAME_KEY = "excelFileName";
        private const string FIRST_ROW_INDEX_KEY = "firstRowIndex";
        private const string LAST_ROW_INDEX_KEY = "lastRowIndex";
        private const string COLUMN_INDEX_KEY = "columnIndex";
        private const string FULL_RANGE_VALUES_KEY = "fullRangeValues";
        private const string VALUES_KEY = "values";

        // TODO config
        private const double fuzzyness = 0.7;
        private const double autoFuzzyness = 0.9;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Fuzzy _fuzzy;

        public HomeController()
        {
            string fileName = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data", Fuzzy.FILE_NAME);
            _fuzzy = new Fuzzy(fileName);
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadFile(string file, string fileName)
        {
            byte[] fileArr = new byte[0];
            if (string.IsNullOrEmpty(file) == true)
                return Json(new { message = "Файл пуст"});

            var fl = file.Split(',')[1];
            fileArr = Convert.FromBase64String(fl);

            string tempPath = Path.GetTempFileName();
            System.IO.File.WriteAllBytes(tempPath, fileArr);

            Session[FILE_KEY] = tempPath;
            Session[FILE_NAME_KEY] = fileName;

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
                
                excelBook.Close(0);
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
            Session[VALUES_KEY] = values;

            return _fuzzy.Prepare(values, fuzzyness, autoCorrectionFuzzyness);
        }

        /// <summary>
        /// Добавить компанию в базу
        /// </summary>
        /// <param name="values"></param>
        /// <param name="selectedValue"></param>
        /// <returns></returns>
        public void AddCompany(string[] values, string keyWord)
        {
            _logger.Info($"Добавление компании для замены. {string.Join("|", values)} будут заменены на {keyWord}");

            var replaceWords = new HashSet<string>(values);
            replaceWords.Remove(keyWord);

            _fuzzy.Add(keyWord, replaceWords);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public FileResult DownloadFile()
        {
            string fileName = (string)Session[FILE_KEY];

            var app = new Application
            {
                DisplayAlerts = false,
                ScreenUpdating = false,
                IgnoreRemoteRequests = true
            };

            int firstRowIndex = (int)Session[FIRST_ROW_INDEX_KEY];
            int lastRowIndex = (int)Session[LAST_ROW_INDEX_KEY];
            int columnIndex = (int)Session[COLUMN_INDEX_KEY];
            string[] values = (string[])Session[VALUES_KEY];

            _fuzzy.Replace(values);

            var valuesArr = new object[values.Length, 1];
            for (int i = 0; i < values.Length; i++)
                valuesArr[i, 0] = values[i];

            try
            {
                var excelBook = app.Workbooks.Open(fileName);
                var sheet = excelBook.Sheets.FirstOrDefault() as Worksheet;

                var firstCell = sheet.Cells[firstRowIndex + 1, columnIndex];
                var lastCell = sheet.Cells[lastRowIndex, columnIndex];
                var range = sheet.Range(firstCell, lastCell);
                range.Value = valuesArr;

                excelBook.Save();
                excelBook.Close(0);
                app.Quit();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(fileName);
            string originalFileName = (string)Session[FILE_NAME_KEY];

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, originalFileName);
        }
    }
}