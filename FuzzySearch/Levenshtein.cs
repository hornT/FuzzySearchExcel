﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace FuzzySearch
{
    public static partial class Metrics
    {
        public static double LevenshteinDistance(string src, string dest)
        {
            int[,] d = new int[src.Length + 1, dest.Length + 1];
            int i, j, cost;
            char[] str1 = src.ToCharArray();
            char[] str2 = dest.ToCharArray();

            for (i = 0; i <= str1.Length; i++)
            {
                d[i, 0] = i;
            }
            for (j = 0; j <= str2.Length; j++)
            {
                d[0, j] = j;
            }
            for (i = 1; i <= str1.Length; i++)
            {
                for (j = 1; j <= str2.Length; j++)
                {

                    if (str1[i - 1] == str2[j - 1])
                        cost = 0;
                    else
                        cost = 1;

                    d[i, j] =
                        Math.Min(
                            d[i - 1, j] + 1,              // Deletion
                            Math.Min(
                                d[i, j - 1] + 1,          // Insertion
                                d[i - 1, j - 1] + cost)); // Substitution

                    if ((i > 1) && (j > 1) && (str1[i - 1] ==
                        str2[j - 2]) && (str1[i - 2] == str2[j - 1]))
                    {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                    }
                }
            }

            int distance = d[str1.Length, str2.Length];
            int length = Math.Max(str1.Length, str2.Length);

            return 1.0 - (double)distance / length;
        }



        //public static string[] Search(string word, IEnumerable<string> wordList, double fuzzyness)
        //{
        //    string[] foundWords =
        //    (
        //        from s in wordList
        //        let levenshteinDistance = Distance(word, s)
        //        where levenshteinDistance > fuzzyness
        //        select s
        //    ).ToArray();

        //    return foundWords;
        //}
    }
}
