using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzySearch
{
    public class Fuzzy
    {
        private const string FILE_NAME = "corrections.xml";

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

        public void Save()
        {
            string text = JsonConvert.SerializeObject(CorrectionNames);
            File.WriteAllText(FILE_NAME, text);
        }
    }
}
