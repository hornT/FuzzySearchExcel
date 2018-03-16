using FuzzySearch;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private const string CACHE_KEY = "cache";
        //private const double DEFAULT_FUZZYNESS = 0.7;
        private const int WRONG_COLUMN_PERCENT = 20;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private const double THRESHOLD_SENTENCE = 0.33;
        private const double THRESHOLD_WORD = 0.7;
        private const int MIN_WORD_LENGTH = 3;
        private const int SUBTOKEN_LENGTH = 3;

        private static readonly object Sync = new object();

        private static Fuzzy _fuzzy;
        private static Fuzzy Fuzzy
        {
            get
            {
                if(_fuzzy == null || _fuzzy.IsReady == false)
                {
                    lock(Sync)
                    {
                        if (_fuzzy == null)
                        {
                            string fileName = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data", Fuzzy.FILE_NAME);
                            _fuzzy = new Fuzzy(fileName, THRESHOLD_SENTENCE, THRESHOLD_WORD, MIN_WORD_LENGTH, SUBTOKEN_LENGTH);
                        }
                    }
                }

                return _fuzzy;
            }
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadFile(string file, string fileName)
        {
            if (string.IsNullOrEmpty(file) == true)
                return Json(new { message = "Файл пуст"});

            var fl = file.Split(',')[1];
            byte[] fileArr = Convert.FromBase64String(fl);

            // В зависимости от разрешения файла принимаем решение - это библиотека замен или эксель файл для обработки
            if(Path.GetExtension(fileName) == ".xml")
            {
                return Json(new { message = Fuzzy.UploadBaseNamesLib(fileArr) });
            }

            SessionCache sc = GetSessionCache();
            sc.File = fileArr;
            sc.FileName = fileName;

            string[] columns = ReadColumns();

            return Json(new { message = "Файл успешно загружен", columns });
        }

        /// <summary>
        /// Получить список колонок
        /// </summary>
        private string[] ReadColumns()
        {
            var columns = GetColumns();
            _logger.Info($"Всего колонок {columns.Length}");

            return columns;
        }

        private string[] GetColumns()
        {
            SessionCache sc = GetSessionCache();

            using (MemoryStream ms = new MemoryStream(sc.File))
            {
                using (ExcelPackage package = new ExcelPackage(ms))
                {
                    ExcelWorksheet workSheet = package.Workbook.Worksheets.First();

                    int rowStart = workSheet.Dimension.Start.Row;
                    int rowEnd = workSheet.Dimension.End.Row;
                    int columnStart = workSheet.Dimension.Start.Column;
                    int columnEnd = workSheet.Dimension.End.Column;

                    string[] totalColumns = new string[workSheet.Dimension.Columns];
                    for (int i = columnStart; i <= columnEnd; i++)
                    {
                        totalColumns[i - columnStart] = workSheet.Cells[rowStart, i].Value?.ToString();
                    }

                    Dictionary<string, int> columnsDictionary = Enumerable.Range(0, totalColumns.Length)
                        .ToDictionary(x => totalColumns[x], x => x);
                    // Регулярка отсеивает даты и числа
                    Regex reg = new Regex("^[.,\\d]+$");
                    double[] wrongCells = new double[totalColumns.Length];

                    sc.Columns = columnsDictionary;
                    sc.FirstRowIndex = rowStart;
                    sc.LastRowIndex = rowEnd;
                    sc.FirstColumnIndex = columnStart;

                    // Пробежимся по всему документу
                    // Если в колонке есть хотя бы 1 значение: пустое, дата, число или короче 3х символов, то не учитываем эту колонку
                    // TODO проверить числа
                    for (int i = rowStart + 1; i <= rowEnd; i++)
                    {
                        for (int columnIndex = 0; columnIndex < totalColumns.Length; columnIndex++)
                        {
                            object cell = workSheet.Cells[i, columnIndex + columnStart].Value;
                            if (cell == null)
                            {
                                continue;
                            }

                            string value = cell.ToString();
                            //CellType cellType = sheet.GetRow(i).GetCell(columnIndex).CellType;
                            if ( /*cellType == CellType.Numeric ||*/ /* string.IsNullOrEmpty(value) ||*/
                                value.Length < 3 || reg.IsMatch(value))
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
        }

        /// <summary>
        /// Первоначальная обработка файла
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public ActionResult ProcessFile(string columnName)
        {
            _logger.Info($"Обработка файла по колонке {columnName}");

            SessionCache sc = GetSessionCache();
            if(sc.Columns == null || sc.Columns.TryGetValue(columnName, out var columnIndex) == false)
            {
                _logger.Error($"Не удалось найти колонку {columnName}");
                return Json(new { message = $"Не удалось найти колонку {columnName}" });
            }

            _logger.Info($"Найден номер колонки: {columnIndex}");

            PrepareResult prepareResult = PrepareAutoCorrection(columnIndex + sc.FirstColumnIndex);
            if (prepareResult == null)
                return Json(new { message = "Не удалось обработать файл"});

            return Json(new { message = "Файл успешно обработан", prepareResult });
        }

        /// <summary>
        /// Выполнить автокоррекцию
        /// </summary>
        /// <param name="columnIndex"></param>
        private PrepareResult PrepareAutoCorrection(int columnIndex)
        {
            // Вычитываем все значения из выбранной колонки
            SessionCache sc = GetSessionCache();
            int firstRowIndex = sc.FirstRowIndex;
            int lastRowIndex = sc.LastRowIndex;
            sc.ColumnIndex = columnIndex;

            string[] values = new string[lastRowIndex - firstRowIndex];

            using (MemoryStream ms = new MemoryStream(sc.File))
            {
                using (ExcelPackage package = new ExcelPackage(ms))
                {
                    ExcelWorksheet workSheet = package.Workbook.Worksheets.First();

                    for (int i = firstRowIndex + 1; i <= lastRowIndex; i++)
                    {
                        values[i - firstRowIndex - 1] = workSheet.Cells[i, columnIndex].Value?.ToString();
                    }
                }
            }
            
            sc.Values = values;
            
            return Fuzzy.Prepare(values);
        }

        /// <summary>
        /// Добавить компанию в базу
        /// </summary>
        /// <param name="values"></param>
        /// <param name="keyWord"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddCompany(string[] values, string keyWord)
        {
            _logger.Info($"Добавление компании для замены. {string.Join("|", values)} будут заменены на {keyWord}");

            var replaceWords = new HashSet<string>(values);
            replaceWords.Remove(keyWord);

            Fuzzy.Add(keyWord, replaceWords);

            string[] baseNames = Fuzzy.GetBaseNames();

            return Json(new { baseNames });
        }

        /// <summary>
        /// Обработать файл и скачать его
        /// </summary>
        /// <returns></returns>
        public FileResult DownloadFile()
        {
            SessionCache sc = GetSessionCache();
            int firstRowIndex = sc.FirstRowIndex;
            int lastRowIndex = sc.LastRowIndex;
            int columnIndex = sc.ColumnIndex;
            string[] values = sc.Values;

            Fuzzy.Replace(values);

            byte[] fileBytes;
            using (MemoryStream ms = new MemoryStream(sc.File))
            {
                using (ExcelPackage package = new ExcelPackage(ms))
                {
                    ExcelWorksheet workSheet = package.Workbook.Worksheets.First();

                    for (int i = firstRowIndex + 1; i <= lastRowIndex; i++)
                    {
                        workSheet.Cells[i, columnIndex].Value = values[i - firstRowIndex - 1];
                    }

                    fileBytes = package.GetAsByteArray();
                }
            }

            string originalFileName = sc.FileName;

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, originalFileName);
        }

        /// <summary>
        /// Скачать файл библиотеки
        /// </summary>
        /// <returns></returns>
        public FileResult DownloadLib()
        {
            byte[] fileBytes = Fuzzy.GetLibFile();

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, "lib.xml");
        }

        /// <summary>
        /// Отобразить предварительный результат обработки
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetPrepareResult()
        {
            SessionCache sc = GetSessionCache();
            string[] values = new string[sc.Values.Length];

            sc.Values.CopyTo(values, 0);
            Fuzzy.Replace(values);

            string[] result = (new HashSet<string>(values)).OrderBy(x => x).ToArray();

            return Json(new { values = result }, JsonRequestBehavior.AllowGet);
        }
       
        private SessionCache GetSessionCache()
        {
            if(!(Session[CACHE_KEY] is SessionCache cache))
            {
                Session[CACHE_KEY] = cache = new SessionCache();
            }

            return cache;
        }

        /// <summary>
        /// Удалить базовое наименование
        /// </summary>
        /// <param name="baseName"></param>
        [HttpPost]
        public ActionResult DeleteBaseName(string baseName)
        {
            Fuzzy.DeleteBaseName(baseName);

            string[] baseNames = Fuzzy.GetBaseNames();

            return Json(new { baseNames });
        }
    }

    internal sealed class SessionCache
    {
        public int FirstRowIndex { get; set; }

        public int LastRowIndex { get; set; }

        public int FirstColumnIndex { get; set; }

        public int ColumnIndex { get; set; }

        public Dictionary<string, int> Columns { get; set; }

        public string[] Values { get; set; }

        public string FileName { get; set; }

        public byte[] File { get; set; }
    }
}