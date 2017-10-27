using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzySearch
{
    public static partial class Metrics
    {
        public static double HammingDistance(this string source, string target)
        {
            int distance = 0;

            if (source.Length == target.Length)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    if (!source[i].Equals(target[i]))
                    {
                        distance++;
                    }
                }
                //return distance;
            }
            else
                distance = 99999;

            return 1 - (double)distance / source.Length;
        }
    }
}
