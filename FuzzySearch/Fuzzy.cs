using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FuzzySearch
{
    /// <summary>
    /// Автозамена
    /// </summary>
    public class Fuzzy
    {
        public const string FILE_NAME = "corrections.xml";

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly object _correctionsLock = new object();
        private readonly string _fileName;

        /// <summary>
        /// Словарь для автозамен
        /// </summary>
        public readonly Dictionary<string, string> CorrectionNames;

        public Fuzzy() : this(FILE_NAME)
        {

        }

        public Fuzzy(string fileName)
        {
            _fileName = fileName;

            // Вычитываем файл с автозаменами
            lock (_correctionsLock)
            {
                if (File.Exists(_fileName))
                {
                    string text = File.ReadAllText(_fileName);
                    CorrectionNames = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
                }
                else
                    CorrectionNames = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Первичная обработка данных
        /// Выполняется замена уже существующих значений и поиск похожестей
        /// </summary>
        /// <param name="values"></param>
        /// <param name=""></param>
        /// <param name="fuzzyness"></param>
        /// <returns></returns>
        public PrepareResult Prepare(IEnumerable<string> values, double fuzzyness)
        {
            string[] valuesArr = values.ToArray();

            // Прямая замена
            HashSet<string> replacements = Replace(valuesArr);
            List<string> replacementLog = replacements.Select(x =>
            {
                string baseName;
                CorrectionNames.TryGetValue(x, out baseName);

                return $"Компания {x} заменена на {baseName}";
            }).ToList();

            // Поиск названий, похожих на базовые
            PossibleReplace[] possibleReplaces = AutoCorrection(fuzzyness, valuesArr);

            return new PrepareResult(possibleReplaces, replacementLog, CorrectionNames.Values.ToArray());
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
                string replaceName;
                if (CorrectionNames.TryGetValue(values[i], out replaceName))
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
        /// <param name="fuzzyness"></param>
        /// <returns></returns>
        private PossibleReplace[] AutoCorrection(double fuzzyness, string[] values)
        {
            List<PossibleReplace> replaces = new List<PossibleReplace>();

            HashSet<string> allNames = new HashSet<string>(values);
            string[] keyWords = CorrectionNames.Values.ToArray();

            // Исключаем из выборки базовые названия
            Array.ForEach(keyWords, x => allNames.Remove(x));

            // Поиск похожих названий на базовые
            foreach (string keyWord in keyWords)
            {
                string[] sameNames = Search(keyWord, allNames, fuzzyness);

                if (sameNames.Length > 0)
                {
                    replaces.Add(new PossibleReplace(sameNames, keyWord));
                    Array.ForEach(sameNames, x => allNames.Remove(x));
                }
            }
            _logger.Info($"Найдено {replaces.Count} похожих на базовые названия компаний");

            // Поиск похожих названий между собой
            HashSet<int> passIndexes = new HashSet<int>();
            string[] allNamesArr = allNames.ToArray();
            
            for (int i = 0; i < allNamesArr.Length; i++)
            {
                // Пропускаем уже задействованные слова
                if (passIndexes.Contains(i))
                    continue;

                string[] sameNames = Search(allNamesArr[i], allNamesArr, fuzzyness);
                if (sameNames.Length > 1)
                {
                    replaces.Add(new PossibleReplace(sameNames, null));

                    foreach (string name in sameNames)
                        passIndexes.Add(Array.IndexOf(allNamesArr, name));
                }
            }

            return replaces.ToArray();
        }

        /// <summary>
        /// Поиск похожих слов
        /// </summary>
        /// <param name="value"></param>
        /// <param name="wordList"></param>
        /// <param name="fuzzyness"></param>
        /// <returns></returns>
        private string[] Search(string value, IEnumerable<string> wordList, double fuzzyness)
        {
            string[] result = wordList
                .Where(x =>
                    Metrics.HammingDistance(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    Metrics.LevenshteinDistance(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    //Metrics.OverlapCoefficient(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    Metrics.RatcliffObershelpSimilarity(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    Metrics.SorensenDiceIndex(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    Metrics.TanimotoCoefficient(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    //Metrics.JaccardDistance(value.ToUpper(), x.ToUpper()) > fuzzyness ||
                    false
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
            lock (_correctionsLock)
            {
                foreach (string replaceWord in replaceWords)
                    CorrectionNames[replaceWord] = keyWord;

                Save();
            }

            //Replace();
        }

        /// <summary>
        /// Сохранить автозамены
        /// </summary>
        public void Save()
        {
            string text = JsonConvert.SerializeObject(CorrectionNames);
            File.WriteAllText(_fileName, text);
        }
    }
}
