﻿using ME3Script.Analysis.Visitors;
using ME3Script.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3Script.Parsing;

namespace ME3Script.Language.Tree
{
    public class ArraySymbolRef : SymbolReference
    {
        public Expression Index;
        public Expression Array;

        public ArraySymbolRef(Expression array, Expression index, SourcePosition start, SourcePosition end) 
            : base(array, start: start, end: end)
        {
            Index = index;
            Type = ASTNodeType.ArrayReference;
            Array = array;
        }

        public override VariableType ResolveType()
        {
            return Array.ResolveType() switch
            {
                 DynamicArrayType dynArrType => dynArrType.ElementType,
                _ => throw new ParseError("Expected a dynamic array!")
            };
        }

        public override bool AcceptVisitor(IASTVisitor visitor)
        {
            return visitor.VisitNode(this);
        }
        public override IEnumerable<ASTNode> ChildNodes
        {
            get
            {
                yield return Array;
                yield return Index;
            }
        }
    }
}
