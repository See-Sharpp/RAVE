using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class tokenizer
{
    private readonly Dictionary<string, int> _vocab;
    private readonly string _unkToken = "[UNK]";
    private readonly string _clsToken = "[CLS]";
    private readonly string _sepToken = "[SEP]";
    private readonly string _padToken = "[PAD]";
    private readonly Regex _basicTokenizerRegex = new Regex(@"\w+|[^\s\w]", RegexOptions.Compiled);

    public tokenizer(string vocabFilePath)
    {
        _vocab = File
            .ReadAllLines(vocabFilePath)
            .Select((token, idx) => (token, idx))
            .ToDictionary(x => x.token, x => x.idx);
    }

    public (long[] inputIds, long[] attentionMask, long[] tokenTypeIds) Encode(string text, int maxLen = 128)
    {
        // 1. Basic lowercase + clean
        text = text.ToLowerInvariant().Trim();

        // 2. Basic tokenization
        var basicTokens = _basicTokenizerRegex
            .Matches(text)
            .Cast<Match>()
            .Select(m => m.Value)
            .ToArray();

        // 3. WordPiece sub-tokenization
        var wpTokens = new List<string>();
        foreach (var token in basicTokens)
        {
            if (_vocab.ContainsKey(token))
            {
                wpTokens.Add(token);
                continue;
            }

            int start = 0;
            bool isBad = false;
            var subTokens = new List<string>();

            while (start < token.Length)
            {
                int end = token.Length;
                string curSub = null;
                while (start < end)
                {
                    var substr = (start > 0 ? "##" : "") + token.Substring(start, end - start);
                    if (_vocab.ContainsKey(substr))
                    {
                        curSub = substr;
                        break;
                    }
                    end--;
                }
                if (curSub == null)
                {
                    isBad = true;
                    break;
                }
                subTokens.Add(curSub);
                start = end;
            }

            if (isBad)
                wpTokens.Add(_unkToken);
            else
                wpTokens.AddRange(subTokens);
        }

        // 4. Add special tokens
        var tokens = new List<string> { _clsToken };
        tokens.AddRange(wpTokens);
        tokens.Add(_sepToken);

        // 5. Convert to IDs
        var ids = tokens
            .Select(t => _vocab.TryGetValue(t, out var idx) ? idx : _vocab[_unkToken])
            .ToList();

        // 6. Pad or truncate to maxLen
        if (ids.Count > maxLen)
        {
            ids = ids.Take(maxLen).ToList();
        }
        else
        {
            ids.AddRange(Enumerable.Repeat(_vocab[_padToken], maxLen - ids.Count));
        }

        // 7. attention_mask: 1 for real tokens, 0 for PAD
        var mask = ids
            .Select(id => id == _vocab[_padToken] ? 0L : 1L)
            .ToArray();

        // 8. token_type_ids: all zeros for single-sentence
        var typeIds = new long[maxLen];

        return (ids.Select(i => (long)i).ToArray(), mask, typeIds);
    }
}
