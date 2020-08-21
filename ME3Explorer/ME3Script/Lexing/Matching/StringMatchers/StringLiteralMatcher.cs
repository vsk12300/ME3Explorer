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
    public class StringLiteralMatcher : TokenMatcherBase<string>
    {
        private const string Delimiter = "\"";

        protected override Token<string> Match(TokenizableDataStream<string> data, ref SourcePosition streamPos, MessageLog log)
        {
            SourcePosition start = new SourcePosition(streamPos);
            string value = null;
            if (data.CurrentItem == Delimiter)
            {
                data.Advance();
                bool inEscape = false;
                for (;!data.AtEnd(); data.Advance())
                {
                    if (inEscape)
                    {
                        inEscape = false;
                        switch (data.CurrentItem)
                        {
                            case "\\":
                            case Delimiter:
                                value += data.CurrentItem;
                                continue;
                            case "n":
                                value += "\n";
                                continue;
                            case "r":
                                value += "\r";
                                continue;
                            case "t":
                                value += "\t";
                                continue;
                            default:
                                log.LogError(@$"Unrecognized escape sequence: '\{data.CurrentItem}'", new SourcePosition(streamPos));
                                return null;
                        }
                    }

                    if (data.CurrentItem == "\\")
                    {
                        inEscape = true;
                        continue;
                    }
                    if (data.CurrentItem == Delimiter)
                    {
                        break;
                    }
                    if (data.CurrentItem == "\n")
                    {
                        streamPos = streamPos.GetModifiedPosition(0, data.CurrentIndex - start.CharIndex, data.CurrentIndex - start.CharIndex);
                        log.LogError("String Literals can not contain line breaks!", start, new SourcePosition(streamPos));
                        return null;
                    }
                    value += data.CurrentItem;
                }

                if (data.CurrentItem == Delimiter)
                {
                    data.Advance();
                    value ??= "";
                }
                else
                {
                    streamPos = streamPos.GetModifiedPosition(0, data.CurrentIndex - start.CharIndex, data.CurrentIndex - start.CharIndex);
                    log.LogError("String Literal was not terminated properly!", start, new SourcePosition(streamPos));
                    return null;
                }
            }

            if (value != null)
            {
                streamPos = streamPos.GetModifiedPosition(0, data.CurrentIndex - start.CharIndex, data.CurrentIndex - start.CharIndex);
                SourcePosition end = new SourcePosition(streamPos);
                return new Token<string>(TokenType.StringLiteral, value, start, end);
            }
            return null;
        }
    }
}
