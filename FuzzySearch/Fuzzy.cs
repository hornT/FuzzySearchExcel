using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;

namespace FuzzySearch
{
    /// <summary>
    /// Автозамена
    /// </summary>
    public sealed class Fuzzy
    {
        /// <summary>
        /// Время до отложенного сохранения автозамен
        /// </summary>
        private const int SAVE_DELAY = 5 * 60 * 1000; // 5 минут в миллисекундах
        //private const int SAVE_DELAY = 10 * 1000;

        public const string FILE_NAME = "corrections.xml";

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly object _correctionsLock = new object();
        private readonly string _fileName;
        private readonly Timer _timer;

        /// <summary>
        /// Словарь для автозамен
        /// </summary>
        private Dictionary<string, string> _correctionNames;

        private readonly FuzzyComparer _comparer;

        public bool IsReady { get; }

        //public Fuzzy() : this(FILE_NAME)
        //{

        //}

        public Fuzzy(string fileName, double aThresholdSentence, double aThresholdWord, int minWordLength, int subtokenLength)
        {
            _fileName = fileName;

            // Вычитываем файл с автозаменами
            //lock (_correctionsLock)
            {
                if (File.Exists(_fileName))
                {
                    string text = File.ReadAllText(_fileName);
                    _correctionNames = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
                }
                else
                    _correctionNames = new Dictionary<string, string>();

                _logger.Info($"Объект создан. Прочитано автозамен: {_correctionNames.Count}");
            }

            _timer = new Timer(SAVE_DELAY);
            _timer.Elapsed += SaveCorrections;
            _timer.AutoReset = false;

            _comparer = new FuzzyComparer(aThresholdSentence, aThresholdWord, minWordLength, subtokenLength);

            IsReady = true;
        }

        /// <summary>
        /// Первичная обработка данных
        /// Выполняется замена уже существующих значений и поиск похожестей
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public PrepareResult Prepare(IEnumerable<string> values)
        {
            string[] valuesArr = values.ToArray();

            // Прямая замена
            HashSet<string> replacements = Replace(valuesArr);
            List<string> replacementLog = replacements.Select(x =>
            {
                _correctionNames.TryGetValue(x, out var baseName);

                return $"Компания {x} заменена на {baseName}";
            }).ToList();

            // Поиск названий, похожих на базовые
            PossibleReplace[] possibleReplaces = AutoCorrection(valuesArr, out string[] unworkedNames);

            string[] baseNames = GetBaseNames();

            return new PrepareResult(possibleReplaces, replacementLog, baseNames, unworkedNames);
        }

        /// <summary>
        /// Получить список базовых названий
        /// </summary>
        /// <returns></returns>
        public string[] GetBaseNames()
        {
            return _correctionNames.Values.Distinct().OrderBy(x => x).ToArray();
        }

        /// <summary>
        /// Получить файл с библиотекой наименований
        /// </summary>
        /// <returns></returns>
        public byte[] GetLibFile()
        {
            lock (_correctionsLock)
            {
                string text = JsonConvert.SerializeObject(_correctionNames);

                return Encoding.Default.GetBytes(text);
            }
        }

        /// <summary>
        /// Замена значений на основе ранее принятых решений
        /// </summary>
        public HashSet<string> Replace(string[] values)
        {
            var result = new HashSet<string>();

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                    continue;

                if (_correctionNames.TryGetValue(values[i], out var replaceName))
                {
                    result.Add(values[i]);
                    values[i] = replaceName;
                }
            }

            return result;
        }

        /// <summary>
        /// Поиск и замена слов, очень похожих на ранее выбранные
        /// </summary>
        /// <returns></returns>
        private PossibleReplace[] AutoCorrection(string[] values, out string[] unworkedNames)
        {
            List<PossibleReplace> replaces = new List<PossibleReplace>();

            HashSet<string> allNames = new HashSet<string>(values);
            string[] keyWords = _correctionNames.Values.ToArray();

            // Исключаем из выборки базовые названия
            Array.ForEach(keyWords, x => allNames.Remove(x));

            // Поиск похожих названий на базовые
            foreach (string keyWord in keyWords)
            {
                string[] sameNames = Search(keyWord, allNames);

                if (sameNames.Length > 0)
                {
                    replaces.Add(new PossibleReplace(sameNames, keyWord));
                    Array.ForEach(sameNames, x => allNames.Remove(x));
                }
            }
            _logger.Info($"Найдено {replaces.Count} похожих на базовые названия компаний");

            // Поиск похожих названий на замененные
            string[] replaceWords = _correctionNames.Keys.ToArray();
            foreach (string replaceWord in replaceWords)
            {
                string[] sameNames = Search(replaceWord, allNames);

                if (sameNames.Length > 0)
                {
                    replaces.Add(new PossibleReplace(sameNames, _correctionNames[replaceWord]));
                    Array.ForEach(sameNames, x => allNames.Remove(x));
                }
            }

            // Поиск похожих названий между собой
            HashSet<int> passIndexes = new HashSet<int>();
            string[] allNamesArr = allNames.ToArray();
            
            for (int i = 0; i < allNamesArr.Length; i++)
            {
                // Пропускаем уже задействованные слова
                if (passIndexes.Contains(i))
                    continue;

                string[] sameNames = Search(allNamesArr[i], allNamesArr);
                if (sameNames.Length > 1)
                {
                    replaces.Add(new PossibleReplace(sameNames, null));

                    foreach (string name in sameNames)
                        passIndexes.Add(Array.IndexOf(allNamesArr, name));

                    Array.ForEach(sameNames, x => allNames.Remove(x));
                }
            }

            unworkedNames = allNames.ToArray();

            return replaces.ToArray();
        }

        /// <summary>
        /// Поиск похожих слов
        /// </summary>
        /// <param name="value"></param>
        /// <param name="wordList"></param>
        /// <returns></returns>
        private string[] Search(string value, IEnumerable<string> wordList)
        {
            string[] result = wordList
                .Where(x =>
                    // TODO после тестирования удалить лишние модули
                    //Metrics.HammingDistance(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    //Metrics.LevenshteinDistance(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    ////Metrics.OverlapCoefficient(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    //Metrics.RatcliffObershelpSimilarity(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    //Metrics.SorensenDiceIndex(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    //Metrics.TanimotoCoefficient(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    ////Metrics.JaccardDistance(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    //false
                        _comparer.IsFuzzyEqual(value, x)
                    )
                .ToArray();

            return result;
        }

        /// <summary>
        /// Добавить пользовательские занчения замены
        /// </summary>
        /// <param name="keyWord"></param>
        /// <param name="replaceWords"></param>
        public void Add(string keyWord, IEnumerable<string> replaceWords)
        {
            if (replaceWords == null || string.IsNullOrEmpty(keyWord) == true)
                return;

            string[] words = replaceWords.ToArray();

            lock (_correctionsLock)
            {
                foreach (string replaceWord in words)
                    _correctionNames[replaceWord] = keyWord;

                _logger.Info($"Добавлена замена: {string.Join(";", words)} на {keyWord}");
                Save();
            }
        }

        /// <summary>
        /// Загрузить пользовательскую библиотеку имен
        /// </summary>
        /// <param name="fileArr"></param>
        /// <returns></returns>
        public string UploadBaseNamesLib(byte[] fileArr)
        {
            _logger.Info("Загрузка пользовательской библиотеки");

            if (fileArr == null || fileArr.Length == 0)
                return "Библиотека не загружена. Файл пуст";

            _logger.Debug($"fileArr.ln: {fileArr.Length}");

            string text = Encoding.UTF8.GetString(fileArr);

            lock (_correctionsLock)
            {
                try
                {
                    _correctionNames = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
                }
                catch(Exception ex)
                {
                    _logger.Error(ex);
                    return "Библиотека не загружена. Неверный формат";
                }
            }

            SaveCorrections(null, null);

            return "Библиотека успешно загружена";
        }

        /// <summary>
        /// Запустить таймер сохранения автозамен
        /// </summary>
        private void Save()
        {
            _timer.Stop();
            _timer.Start();

            _logger.Debug("Таймер сохранения перезапущен");
        }

        /// <summary>
        /// Сохранить автозамены
        /// </summary>
        private void SaveCorrections(Object source, ElapsedEventArgs e)
        {
            lock (_correctionsLock)
            {
                string text = JsonConvert.SerializeObject(_correctionNames);
                File.WriteAllText(_fileName, text);

                _logger.Info("Файл автозамен успешно записан");
            }
        }

        /// <summary>
        /// Удалить базовое наименование
        /// </summary>
        /// <param name="baseName"></param>
        public void DeleteBaseName(string baseName)
        {
            _logger.Info($"Удаления базового наименования {baseName}");
            // TODO временный функционал
            if (string.IsNullOrEmpty(baseName) == true)
                return;

            lock (_correctionsLock)
            {
                string[] keys = _correctionNames.Where(x => x.Value.Equals(baseName)).Select(x => x.Key).ToArray();
                _logger.Info($"Будут удалены {keys.Length} замен: {string.Join("; ", keys)}");
                foreach (string key in keys)
                    _correctionNames.Remove(key);
            }
        }
    }
}
