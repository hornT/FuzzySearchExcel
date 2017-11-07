using FuzzySearch;
using NLog;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private const string CACHE_KEY = "cache";
        private const double DEFAULT_FUZZYNESS = 0.7;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly double _fuzzyness;
        private readonly Fuzzy _fuzzy;

        public HomeController()
        {
            string fileName = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data", Fuzzy.FILE_NAME);
            string fuzzyValue = System.Configuration.ConfigurationManager.AppSettings["fuzzyness"].ToString();
            if (string.IsNullOrEmpty(fuzzyValue) == true ||
                double.TryParse(fuzzyValue, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out _fuzzyness) == false)
                _fuzzyness = DEFAULT_FUZZYNESS;

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

            SessionCache sc = GetSessionCache();
            sc.File = fileArr;
            sc.FileName = fileName;

            string[] columns = ReadExcelFile();

            return Json(new { message = "Файл успешно загружен", columns });
        }

        /// <summary>
        /// Работа с файлом
        /// </summary>
        /// <param name="fileName"></param>
        private string[] ReadExcelFile()
        {
            string[] columns = new string[0];

            // https://stackoverflow.com/questions/5855813/npoi-how-to-read-file-using-npoi

            XSSFWorkbook xssfwb = GetSSFWorkbook();
            ISheet sheet = xssfwb.GetSheetAt(0);

            int firstRowIndex = sheet.FirstRowNum;
            int lastRowIndex = sheet.LastRowNum;
            var firstRow = sheet.GetRow(firstRowIndex);

            columns = firstRow.Cells.Select(x => x.StringCellValue).ToArray();
            _logger.Info($"Строки с {firstRowIndex} по {lastRowIndex}. Всего колонок {columns.Length}");

            SessionCache sc = GetSessionCache();
            sc.FirstRowIndex = firstRowIndex;
            sc.LastRowIndex = lastRowIndex;

            return columns;
        }

        /// <summary>
        /// Первоначальная обработка файла
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public ActionResult ProcessFile(int columnIndex)
        {
            PrepareResult prepareResult = PrepareAutoCorrection(columnIndex, _fuzzyness);
            if (prepareResult == null)
                return Json(new { message = "Не удалось обработать файл"});

            return Json(new { message = "Файл успешно обработан", prepareResult });
        }

        /// <summary>
        /// Выполнить автокоррекцию
        /// </summary>
        /// <param name="columnIndex"></param>
        private PrepareResult PrepareAutoCorrection(int columnIndex, double fuzzyness)
        {
            // Вычитываем все значения из выбранной колонки
            SessionCache sc = GetSessionCache();
            int firstRowIndex = sc.FirstRowIndex;
            int lastRowIndex = sc.LastRowIndex;
            sc.ColumnIndex = columnIndex;

            XSSFWorkbook xssfwb = GetSSFWorkbook();
            ISheet sheet = xssfwb.GetSheetAt(0);

            string[] values = new string[lastRowIndex - firstRowIndex];
            for (int i = firstRowIndex + 1; i <= lastRowIndex; i++)
            {
                values[i - firstRowIndex - 1] = sheet.GetRow(i).GetCell(columnIndex).ToString();
            }
            sc.Values = values;
            
            return _fuzzy.Prepare(values, fuzzyness);
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
            SessionCache sc = GetSessionCache();
            int firstRowIndex = sc.FirstRowIndex;
            int lastRowIndex = sc.LastRowIndex;
            int columnIndex = sc.ColumnIndex;
            string[] values = sc.Values;

            _fuzzy.Replace(values);

            var valuesArr = new object[values.Length, 1];
            for (int i = 0; i < values.Length; i++)
                valuesArr[i, 0] = values[i];

            XSSFWorkbook xssfwb = GetSSFWorkbook();

            ISheet sheet = xssfwb.GetSheetAt(0);
            for (int i = firstRowIndex + 1; i <= lastRowIndex; i++)
            {
                sheet.GetRow(i).GetCell(columnIndex).SetCellValue(values[i - firstRowIndex - 1]);
            }

            byte[] fileBytes = null;
            using (MemoryStream ms = new MemoryStream())
            {
                xssfwb.Write(ms);
                fileBytes = ms.ToArray();
            }

            string originalFileName = sc.FileName;

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, originalFileName);
        }

        private XSSFWorkbook GetSSFWorkbook()
        {
            SessionCache sc = GetSessionCache();
            byte[] buff = sc.File;
            using (MemoryStream ms = new MemoryStream(buff))
            {
                try
                {
                    return new XSSFWorkbook(ms);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    throw;
                }
            }
        }
        
        private SessionCache GetSessionCache()
        {
            var cache = Session[CACHE_KEY] as SessionCache;
            if(cache == null)
            {
                Session[CACHE_KEY] = cache = new SessionCache();
            }

            return cache;
        }
    }

    internal sealed class SessionCache
    {
        public int FirstRowIndex { get; set; }

        public int LastRowIndex { get; set; }

        public int ColumnIndex { get; set; }

        public string[] Values { get; set; }

        public string FileName { get; set; }

        public byte[] File { get; set; }
    }
}