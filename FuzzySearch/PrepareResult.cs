using System.Collections.Generic;

namespace FuzzySearch
{
    public class PrepareResult
    {
        /// <summary>
        /// Возможные варианты замен
        /// </summary>
        public readonly List<string[]> PossibleReplaces;

        /// <summary>
        /// Лог с результатом замены
        /// </summary>
        public readonly HashSet<string> ReplacementLog;

        /// <summary>
        /// Лог с результатом автопоиска замен
        /// </summary>
        public Dictionary<string, string> AutoCorrectionResult;

        public PrepareResult(List<string[]> possibleReplaces, HashSet<string> replacementLog, Dictionary<string, string> autoCorrectionResult)
        {
            PossibleReplaces = possibleReplaces;
            ReplacementLog = replacementLog;
            AutoCorrectionResult = autoCorrectionResult;
        }
    }
}
