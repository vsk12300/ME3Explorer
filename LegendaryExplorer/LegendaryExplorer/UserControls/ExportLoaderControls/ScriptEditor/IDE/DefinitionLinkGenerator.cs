﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using LegendaryExplorerCore.UnrealScript.Language.Tree;
using LegendaryExplorerCore.UnrealScript.Lexing.Tokenizing;
using LegendaryExplorerCore.UnrealScript.Parsing;

namespace LegendaryExplorer.UserControls.ExportLoaderControls.ScriptEditor.IDE
{
    public class DefinitionLinkGenerator : VisualLineElementGenerator
    {
        private readonly Dictionary<int, DefinitionLinkSpan> Spans = new();
        private readonly List<int> Offsets = new();

        readonly struct DefinitionLinkSpan
        {
            public readonly ASTNode Node;
            public readonly int Length;

            public DefinitionLinkSpan(ASTNode node, int length)
            {
                Node = node;
                Length = length;
            }
        }

        public void SetTokens(TokenStream<string> tokens)
        {
            Reset();
            foreach (Token<string> token in tokens)
            {
                if (token.AssociatedNode is not null)
                {
                    int startPosCharIndex = token.StartPos.CharIndex;
                    Spans[startPosCharIndex] = new DefinitionLinkSpan(token.AssociatedNode, token.EndPos.CharIndex - startPosCharIndex);
                    Offsets.Add(startPosCharIndex);
                }
            }
        }

        public void Reset()
        {
            Spans.Clear();
            Offsets.Clear();
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            //Debug.WriteLine($"Offset: {startOffset}");
            int endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
            foreach (int offset in Offsets)
            {
                if (offset >= startOffset)
                {
                    if (offset < endOffset)
                    {
                        return offset;
                    }

                    break;
                }
            }

            return -1;
        }

        public override VisualLineElement ConstructElement(int offset)
        {
            //Debug.WriteLine($"Construct Offset: {offset}");
            if (Spans.TryGetValue(offset, out DefinitionLinkSpan span))
            {
                //Debug.WriteLine($"Constructed at Offset: {offset}");
                return new VisualLineDefinitionLinkText(CurrentContext.VisualLine, span.Node, span.Length);
            }

            return null;
        }
    }
}
