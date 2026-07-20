using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using BussinessLayer.Interfaces;

namespace BussinessLayer.Services
{
    public class ChunkingService : IChunkingService
    {
        public IEnumerable<ChunkDto> Chunk(string text, long strategyId, int? maxChars = null)
        {
            if (string.IsNullOrEmpty(text)) yield break;

            if (strategyId == 2)
            {
                // Strategy 2: Fixed-size (by character)
                int idx = 0;
                int maxSize = maxChars ?? 1000;
                for (int i = 0; i < text.Length; i += maxSize)
                {
                    var chunkText = text.Substring(i, Math.Min(maxSize, text.Length - i));
                    yield return new ChunkDto
                    {
                        Index = idx++,
                        Text = chunkText,
                        TokenCount = chunkText.Length // using character count as token approximation
                    };
                }
            }
            else if (strategyId == 3)
            {
                // Strategy 3: Sentence-Window (3 sentences per chunk, overlap 1 sentence / stride 2)
                foreach (var chunk in SentenceWindowSplit(text, 3, 2))
                {
                    yield return chunk;
                }
            }
            else
            {
                // Strategy 1 (Default): Paragraph-based
                var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                int idx = 0;
                int maxSize = maxChars ?? 1000;
                foreach (var p in paragraphs)
                {
                    var trimmed = p.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    
                    if (trimmed.Length <= maxSize)
                    {
                        yield return new ChunkDto
                        {
                            Index = idx++,
                            Text = trimmed,
                            TokenCount = trimmed.Length
                        };
                    }
                    else
                    {
                        var subChunks = RecursiveSplit(trimmed, maxSize);
                        foreach (var sc in subChunks)
                        {
                            yield return new ChunkDto
                            {
                                Index = idx++,
                                Text = sc.Text,
                                TokenCount = sc.Text.Length
                            };
                        }
                    }
                }
            }
        }

        private IEnumerable<ChunkDto> RecursiveSplit(string text, int maxChars)
        {
            var separators = new[] { "\n\n", "\n", ". ", " ", "" };
            var chunksText = SplitRecursively(text, maxChars, separators, 0);
            
            int idx = 0;
            foreach (var chunk in chunksText)
            {
                var trimmed = chunk.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                yield return new ChunkDto
                {
                    Index = idx++,
                    Text = trimmed,
                    TokenCount = trimmed.Length
                };
            }
        }

        private List<string> SplitRecursively(string text, int maxChars, string[] separators, int sepIndex)
        {
            if (text.Length <= maxChars)
            {
                return new List<string> { text };
            }

            if (sepIndex >= separators.Length)
            {
                // Fallback: forced substring splitting if we ran out of separators
                var forcedList = new List<string>();
                for (int i = 0; i < text.Length; i += maxChars)
                {
                    forcedList.Add(text.Substring(i, Math.Min(maxChars, text.Length - i)));
                }
                return forcedList;
            }

            var separator = separators[sepIndex];
            var splits = text.Split(new[] { separator }, StringSplitOptions.None);
            
            var result = new List<string>();
            var currentBuffer = "";

            foreach (var part in splits)
            {
                // Add separator back except for empty separator
                var partWithSep = part + (separator != "" ? separator : "");
                
                if (currentBuffer.Length + partWithSep.Length <= maxChars)
                {
                    currentBuffer += partWithSep;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentBuffer))
                    {
                        result.Add(currentBuffer);
                    }

                    if (partWithSep.Length <= maxChars)
                    {
                        currentBuffer = partWithSep;
                    }
                    else
                    {
                        // Current part is too large even by itself, split it using next separator
                        var subSplits = SplitRecursively(part, maxChars, separators, sepIndex + 1);
                        result.AddRange(subSplits);
                        currentBuffer = "";
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentBuffer))
            {
                result.Add(currentBuffer);
            }

            return result;
        }

        private IEnumerable<ChunkDto> SentenceWindowSplit(string text, int windowSize, int stride)
        {
            // Split into sentences using a simple regex supporting sentence terminals . ! ? followed by space or newline
            var sentenceMatches = Regex.Matches(text, @"[^.!?\s][^.!?]*(?:[.!?](?=\s|$))?");
            var sentences = sentenceMatches.Cast<Match>().Select(m => m.Value.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

            if (sentences.Count == 0)
            {
                yield return new ChunkDto { Index = 0, Text = text, TokenCount = text.Length };
                yield break;
            }

            int idx = 0;
            int start = 0;
            while (start < sentences.Count)
            {
                int end = Math.Min(start + windowSize, sentences.Count);
                var chunkSentences = sentences.GetRange(start, end - start);
                var chunkText = string.Join(" ", chunkSentences);

                yield return new ChunkDto
                {
                    Index = idx++,
                    Text = chunkText,
                    TokenCount = chunkText.Length
                };

                if (end == sentences.Count) break;
                start += stride;
            }
        }
    }
}
