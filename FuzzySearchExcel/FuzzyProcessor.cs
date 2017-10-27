using FuzzySearch;
using NetOffice.ExcelApi;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = NetOffice.ExcelApi.Application;

namespace FuzzySearchExcel
{
    internal class FuzzyProcessor
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Fuzzy _fuzzy = new Fuzzy();

        private int _firstRowIndex;
        private int _lastRowIndex;
        private int _columnIndex;
        private object[,] _fullRangeValues;
        private Workbook _excelBook;
        private Worksheet _sheet;

        /// <summary>
        /// Работа с файлом
        /// </summary>
        /// <param name="fileName"></param>
        public string[] ReadExcelFile(string fileName)
        {
            OnClose();

            var app = new Application
            {
                DisplayAlerts = false,
                ScreenUpdating = false,
                IgnoreRemoteRequests = true
            };

            string[] columns;
            try
            {
                _excelBook = app.Workbooks.Open(fileName);
                _sheet = _excelBook.Sheets.FirstOrDefault() as Worksheet;
                var fullRange = _sheet.UsedRange;
                _firstRowIndex = fullRange.Row;
                var firstColIndex = fullRange.Column;
                _fullRangeValues = (object[,])fullRange.Value;
                _lastRowIndex = _fullRangeValues.GetLength(0);
                var lastColumnIndex = _fullRangeValues.GetLength(1);
                _excelBook.Close();

                _logger.Info($"Строки с {_firstRowIndex} по {_lastRowIndex}. Колонки с {firstColIndex} по {lastColumnIndex}");

                // Заполняем список колонок
                columns = new string[lastColumnIndex];
                for (int i = firstColIndex; i <= lastColumnIndex; i++)
                    columns[i - firstColIndex] = _fullRangeValues[_firstRowIndex, i] as string;

                return columns;
            }
            catch(Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Выполнить автокоррекцию
        /// </summary>
        /// <param name="columnIndex"></param>
        public PrepareResult PrepareAutoCorrection(int columnIndex, double fuzzyness, double autoCorrectionFuzzyness)
        {
            // Вычитываем все значения из выбранной колонки
            _columnIndex = columnIndex;
            string[] values = new string[_lastRowIndex - _firstRowIndex];
            for (int i = _firstRowIndex + 1; i <= _lastRowIndex; i++)
            {
                values[i - _firstRowIndex - 1] = _fullRangeValues[i, _columnIndex] as string;
            }

            return _fuzzy.Prepare(values, fuzzyness, autoCorrectionFuzzyness);
        }

        /// <summary>
        /// Добавить пользовательские занчения замены
        /// </summary>
        /// <param name="keyWord"></param>
        /// <param name="replaceWords"></param>
        public void Add(string keyWord, IEnumerable<string> replaceWords)
        {
            _fuzzy.Add(keyWord, replaceWords);
        }

        /// <summary>
        /// Сохранить результат
        /// </summary>
        public void Save()
        {
            if (_excelBook == null || _sheet == null || _fuzzy.Values == null)
                return;

            // Записываем измененные значения в файл
            var firstCell = _sheet.Cells[_firstRowIndex, _columnIndex];
            var lastCell = _sheet.Cells[_lastRowIndex, _columnIndex];
            var columnRange = _sheet.Range(firstCell, lastCell);

            columnRange.Value = _fuzzy.Values;

            _excelBook.Save();
        }

        /// <summary>
        /// Действие перед закрытием
        /// </summary>
        public void OnClose()
        {
            if (_excelBook == null)
                return;

            try
            {
                _excelBook.Close();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}
