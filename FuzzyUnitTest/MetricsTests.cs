using FuzzySearch;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FuzzyUnitTest
{
    [TestClass()]
    public class LevenshteinTests
    {
        const double THRESHOLD_SENTENCE = 0.33;
        const double A_THRESHOLD_WORD = 0.7;
        const int A_MIN_WORD_LENGTH = 3;
        const int A_SUBTOKEN_LENGTH = 3;

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

        [TestMethod()]
        public void CompanyWrongsTest()
        {
            var comparer = new FuzzyComparer(THRESHOLD_SENTENCE, A_THRESHOLD_WORD, A_MIN_WORD_LENGTH, A_SUBTOKEN_LENGTH);

            string[][] wrongs =
            {
                new string[] { "HINO MOTORS LTD", "ISUZU MOTORS LTD.", "NISSAN MOTORS LTD." },
                new string[] { "DAIHATSU  MOTOR CORP", "ISUZU MOTOR CORP.", "MITSUBISHI MOTORS CORP.", "TOYOTA MOTOR CORP." },
                new string[] { "CHENGDU DAYUN AUTOMOBILE GROUP CO.,LTD", "SHAANXI AUTOMOBILE GROUP CO.,LTD" },
                new string[] { "ISUZU MOTORS CO., ТАИЛАНД", "MOTORS CO., ТАИЛАНД" }
            };

            foreach(string[] testNames in wrongs)
                for (int i = 0; i < testNames.Length; i++)
                    for (int j = i + 1; j < testNames.Length; j++)
                        Assert.IsFalse(comparer.IsFuzzyEqual(testNames[i], testNames[j]));
        }

        [TestMethod]
        public void CompanyCorrectTest()
        {
            var comparer = new FuzzyComparer(THRESHOLD_SENTENCE, A_THRESHOLD_WORD, A_MIN_WORD_LENGTH, A_SUBTOKEN_LENGTH);

            string[][] corrects =
            {
                new string[] { "DAF TRUCKS", "DAF TRUCK", "DAF TRUCKS NV", "DAF TRUCKS N.V."},
                new string[] { "FCA ITALY S.P.A", "FCA ITALY S.P.A." },
                new string[] { "BEIQI FOTON MOTOR CO., LTD", "BEIQI FOTON MOTOR CO., LTD., КИТАЙ", "BEIQI FOTON MOTOR CO., LTD." },

                new string[]{ "CATERPILLAR (THAILAND) LTD", "CATERPILLAR INC.", "CATERPILLAR INC. DECATUR, IL USA" },
                new string[]{ "CATERPILLAR (THAILAND) LTD", "CATERPILLAR INC." }
            };

            foreach (string[] testNames in corrects)
                for (int i = 0; i < testNames.Length; i++)
                    for (int j = i + 1; j < testNames.Length; j++)
                        Assert.IsTrue(comparer.IsFuzzyEqual(testNames[i], testNames[j]));
        }

        [TestMethod]
        public void CompanyCorrectTest2()
        {
            var comparer = new FuzzyComparer(THRESHOLD_SENTENCE, A_THRESHOLD_WORD, A_MIN_WORD_LENGTH, A_SUBTOKEN_LENGTH);

            string[][] corrects =
            {
                new string[] { "CATERPILLAR INC.", "CATERPILLAR INC. DECATUR, IL USA"},

                new string[] { "DAF", "DAF TRUCK", "DAF-LEYLAND" },

                //new string[] { "FORD", "FORD MOTOR COMPANY OF SOUTHERN AFRICA", "FORD-WERKE GMBH" },

                new string[]{ "ISUZU", "ISUZU MOTOR CORPORATION", "ISUZU MOTORS CO., ТАИЛАНД" }
            };

            foreach (string[] testNames in corrects)
                for (int i = 0; i < testNames.Length; i++)
                    for (int j = i + 1; j < testNames.Length; j++)
                        Assert.IsTrue(comparer.IsFuzzyEqual(testNames[i], testNames[j]));
        }
    }
}