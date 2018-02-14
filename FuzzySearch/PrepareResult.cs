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

        public readonly string[] BaseNames;

        /// <summary>
        /// Необработанные варианты
        /// </summary>
        public readonly string[] UnworkedNames;

        public PrepareResult(PossibleReplace[] possibleReplaces, List<string> replacementLog, string[] baseNames, string[] unworkedNames)
        {
            PossibleReplaces = possibleReplaces;
            ReplacementLog = replacementLog;
            BaseNames = baseNames;
            UnworkedNames = unworkedNames;
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
