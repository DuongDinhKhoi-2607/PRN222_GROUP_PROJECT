using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BussinessLayer.Services
{
    public static class TextSimilarityHelper
    {
        public static double GetCosineSimilarity(string text1, string text2)
        {
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                return 0.0;

            var words1 = GetWordFrequency(text1);
            var words2 = GetWordFrequency(text2);

            var allWords = new HashSet<string>(words1.Keys.Concat(words2.Keys));

            double dotProduct = 0.0;
            double magnitude1 = 0.0;
            double magnitude2 = 0.0;

            foreach (var word in allWords)
            {
                words1.TryGetValue(word, out int freq1);
                words2.TryGetValue(word, out int freq2);

                dotProduct += freq1 * freq2;
                magnitude1 += freq1 * freq1;
                magnitude2 += freq2 * freq2;
            }

            if (magnitude1 == 0.0 || magnitude2 == 0.0)
                return 0.0;

            return dotProduct / (Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
        }

        private static Dictionary<string, int> GetWordFrequency(string text)
        {
            var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var matches = Regex.Matches(text.ToLowerInvariant(), @"\w+");
            foreach (Match match in matches)
            {
                var word = match.Value;
                if (freq.ContainsKey(word))
                    freq[word]++;
                else
                    freq[word] = 1;
            }
            return freq;
        }
    }
}
