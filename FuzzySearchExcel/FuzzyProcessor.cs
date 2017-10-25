﻿using FuzzySearch;
using NetOffice.ExcelApi;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application = NetOffice.ExcelApi.Application;

namespace FuzzySearchExcel
{
    internal class FuzzyProcessor
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        private int _firstRowIndex;
        private int _lastRowIndex;
        private object[,] _fullRangeValues;

        /// <summary>
        /// Работа с файлом
        /// </summary>
        /// <param name="fileName"></param>
        public string[] ReadExcelFile(string fileName)
        {
            var app = new Application
            {
                DisplayAlerts = false,
                ScreenUpdating = false,
                IgnoreRemoteRequests = true
            };

            string[] columns;
            using (var workbook = app.Workbooks.Open(fileName))
            {
                Worksheet sheet = workbook.Sheets.FirstOrDefault() as Worksheet;
                var fullRange = sheet.UsedRange;
                _firstRowIndex = fullRange.Row;
                var firstColIndex = fullRange.Column;
                _fullRangeValues = (object[,])fullRange.Value;
                _lastRowIndex = _fullRangeValues.GetLength(0);
                var lastColumnIndex = _fullRangeValues.GetLength(1);

                logger.Info($"Строки с {_firstRowIndex} по {_lastRowIndex}. Колонки с {firstColIndex} по {lastColumnIndex}");

                // Заполняем список колонок
                columns = new string[lastColumnIndex];
                for (int i = firstColIndex; i <= lastColumnIndex; i++)
                    columns[i - firstColIndex] = _fullRangeValues[_firstRowIndex, i] as string;

                //workbook.Save();
            }

            return columns;
        }

        /// <summary>
        /// Выполнить автокоррекцию
        /// </summary>
        /// <param name="columnIndex"></param>
        public void ProcessAutoCorrection(int columnIndex)
        {
            Fuzzy fuzzy = new Fuzzy();

            // Вычитываем все значения из выбранной колонки
            string[] values = new string[_lastRowIndex - _firstRowIndex];
            for (int i = _firstRowIndex + 1; i <= _lastRowIndex; i++)
            {
                values[i - _firstRowIndex - 1] = _fullRangeValues[i, columnIndex] as string;
            }

            // Сначала выполняем замену для ранее заполненных замен
            Parallel.For(0, values.Length, i =>
            {
                string replaceName;
                if (fuzzy.CorrectionNames.TryGetValue(values[i], out replaceName))
                    values[i] = replaceName;
            });

            // Ищем похожие слова
            HashSet<string> allNames = new HashSet<string>(values);
            logger.Info($"Осталось {allNames.Count} уникальных названий");

            //var firstCell = _sheet.Cells[startRowIndex, checkColumnIndex];
            //var lastCell = _sheet.Cells[startRowIndex + _rowCheckResults.Count - 1, checkColumnIndex];
            //var checkColumnRange = _sheet.Range(firstCell, lastCell);
            //checkColumnRange.Value = valuesArr;
        }
    }
}
