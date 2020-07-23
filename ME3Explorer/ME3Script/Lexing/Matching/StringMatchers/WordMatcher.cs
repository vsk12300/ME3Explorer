﻿using ME3Script.Compiling.Errors;
using ME3Script.Lexing.Tokenizing;
using ME3Script.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME3Script.Lexing.Matching.StringMatchers
{
    public class WordMatcher : TokenMatcherBase<string>
    {
        private List<KeywordMatcher> Delimiters;

        public WordMatcher(List<KeywordMatcher> delimiters)
        {
            Delimiters = delimiters == null ? new List<KeywordMatcher>() : delimiters;
        }

        protected override Token<string> Match(TokenizableDataStream<string> data, ref SourcePosition streamPos, MessageLog log)
        {
            SourcePosition start = new SourcePosition(streamPos);
            string peek = data.CurrentItem;
            string word = null;
            while (!data.AtEnd() && !string.IsNullOrWhiteSpace(peek) 
                && Delimiters.All(d => d.Keyword != peek)
                && peek != "\"" && peek != "'")
            {
                word += peek;
                data.Advance();
                peek = data.CurrentItem;
            }

            if (word != null)
            {
                streamPos = streamPos.GetModifiedPosition(0, data.CurrentIndex - start.CharIndex, data.CurrentIndex - start.CharIndex);
                SourcePosition end = new SourcePosition(streamPos);
                return new Token<string>(TokenType.Word, word, start, end);
            }
            return null;
        }
    }
}
