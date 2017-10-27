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
            const string s1 = "console"; // все английские
            const string s2 = "соnsole"; // со  в нчале русские
            const string s3 = "сonsole"; // с в начале русская

            const string s4 = "соnsole";

            double distance = Metrics.LevenshteinDistance(s1, s2);
            Assert.IsTrue(distance > 0.7);

            distance = Metrics.HammingDistance(s1, s2);
            Assert.IsTrue(distance > 0.7);

            distance = Metrics.JaccardDistance(s1, s2);
            Assert.IsTrue(distance > 0.6);

            distance = Metrics.OverlapCoefficient(s1, s2);
            Assert.IsTrue(distance > 0.7);

            distance = Metrics.RatcliffObershelpSimilarity(s1, s2);
            Assert.IsTrue(distance > 0.7);

            distance = Metrics.SorensenDiceIndex(s1, s2);
            Assert.IsTrue(distance > 0.7);

            distance = Metrics.TanimotoCoefficient(s1, s2);
            Assert.IsTrue(distance > 0.5);

            //distance = Metrics.LevenshteinDistance("kevin", "kevyn");
            //Assert.IsTrue(distance > 0.7);
        }
    }
}