using System.Collections.Generic;

namespace FuzzySearch
{
    public sealed class PrepareResult
    {
        /// <summary>
        /// Возможные варианты замен
        /// </summary>
        public readonly PossibleReplace[] PossibleReplaces;

        /// <summary>
        /// Лог с результатом замены
        /// </summary>
        public readonly List<string> ReplacementLog;

        ///// <summary>
        ///// Лог с результатом автопоиска замен
        ///// </summary>
        //public Dictionary<string, string> AutoCorrectionResult;

        public readonly string[] BaseNames;

        public PrepareResult(PossibleReplace[] possibleReplaces, List<string> replacementLog, string[] baseNames)
        {
            PossibleReplaces = possibleReplaces;
            ReplacementLog = replacementLog;
            //AutoCorrectionResult = autoCorrectionResult;
            BaseNames = baseNames;
        }
    }

    public sealed class PossibleReplace
    {
        public readonly string[] Values;

        public readonly string BaseName;

        public PossibleReplace(string[] values, string baseName)
        {
            Values = values;
            BaseName = baseName;
        }
    }
}
