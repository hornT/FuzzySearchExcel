using Microsoft.VisualStudio.TestTools.UnitTesting;
using FuzzySearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzySearch.Tests
{
    [TestClass()]
    public class LevenshteinTests
    {
        [TestMethod()]
        public void LevenshteinDistanceTest()
        {
            int distance = Levenshtein.LevenshteinDistance("kevin", "kevyn");
            double score = 1.0 - (double)distance / 5;
            Assert.IsTrue(score > 0.7);

            distance = Levenshtein.LevenshteinDistance("console", "console");
            score = 1.0 - (double)distance / 7;
            Assert.IsTrue(score > 0.7);
        }
    }
}