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
        private const string FILE_NAME = "corrections.xml";

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Значения для замены
        /// </summary>
        public string[] Values { get; private set; }

        /// <summary>
        /// Словарь для автозамен
        /// </summary>
        public readonly Dictionary<string, string> CorrectionNames;

        public Fuzzy()
        {
            // Вычитываем файл с автозаменами
            if (File.Exists(FILE_NAME))
            {
                string text = File.ReadAllText(FILE_NAME);
                CorrectionNames = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
            }
            else
                CorrectionNames = new Dictionary<string, string>();
        }

        /// <summary>
        /// Первичная обработка данных
        /// Выполняется замена уже существующих значений и поиск похожестей
        /// </summary>
        /// <param name="values"></param>
        /// <param name=""></param>
        /// <param name="fuzzyness"></param>
        /// <returns></returns>
        public PrepareResult Prepare(IEnumerable<string> values, double fuzzyness, double autoCorrectionFuzzyness)
        {
            Values = values.ToArray();

            // Прямая замена
            HashSet<string> replacementLog = Replace();

            // Замена очень похожих слов
            Dictionary<string, string> autoCorrectionResult = AutoCorrection(autoCorrectionFuzzyness);
            if(autoCorrectionResult.Count > 0)
                Replace();

            // Ищем похожие слова
            HashSet<string> allNames = new HashSet<string>(Values);
            _logger.Info($"Осталось {allNames.Count} уникальных названий");

            HashSet<int> passIndexes = new HashSet<int>();
            string[] allNamesArr = allNames.ToArray();
            List<string[]> possibleReplaces = new List<string[]>();

            for (int i = 0; i < allNamesArr.Length; i++)
            {
                // Пропускаем уже задействованные слова
                if (passIndexes.Contains(i))
                    continue;

                string[] sameNames = Search(allNamesArr[i], allNamesArr, fuzzyness);
                if (sameNames.Length > 1)
                {
                    possibleReplaces.Add(sameNames);

                    foreach (string name in sameNames)
                        passIndexes.Add(Array.IndexOf(allNamesArr, name));
                }
            }

            return new PrepareResult(possibleReplaces, replacementLog, autoCorrectionResult);
        }

        /// <summary>
        /// Замена значений на основе ранее принятых решений
        /// </summary>
        private HashSet<string> Replace()
        {
            var result = new HashSet<string>();

            for (int i = 0; i < Values.Length; i++)
            {
                string replaceName;
                if (CorrectionNames.TryGetValue(Values[i], out replaceName))
                {
                    result.Add(Values[i]);
                    Values[i] = replaceName;
                }
            }

            return result;
        }

        /// <summary>
        /// Поиск и замена слов, очень похожих на ранее выбранные
        /// </summary>
        /// <param name="fuzzyness"></param>
        /// <returns></returns>
        private Dictionary<string, string> AutoCorrection(double fuzzyness)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            HashSet<string> allNames = new HashSet<string>(Values);
            string[] keyWords = CorrectionNames.Values.ToArray();

            foreach(string keyWord in keyWords)
            {
                string[] sameNames = Search(keyWord, allNames, fuzzyness);

                if (sameNames.Length > 0)
                {
                    string replaceWords = string.Join("; ", sameNames);
                    result[replaceWords] = keyWord;

                    // Добавляем новые значения в словарь для дальнейшей замены
                    foreach (string sameName in sameNames)
                        CorrectionNames[sameName] = keyWord;
                }
            }

            return result;
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
            //return Levenshtein.Search(value, wordList, fuzzyness);
        }

        /// <summary>
        /// Добавить пользовательские занчения замены
        /// </summary>
        /// <param name="keyWord"></param>
        /// <param name="replaceWords"></param>
        public void Add(string keyWord, IEnumerable<string> replaceWords)
        {
            foreach (string replaceWord in replaceWords)
                CorrectionNames[replaceWord] = keyWord;

            Save();

            Replace();
        }

        /// <summary>
        /// Сохранить автозамены
        /// </summary>
        public void Save()
        {
            string text = JsonConvert.SerializeObject(CorrectionNames);
            File.WriteAllText(FILE_NAME, text);
        }
    }
}
