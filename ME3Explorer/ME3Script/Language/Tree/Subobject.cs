﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3Script.Analysis.Visitors;
using ME3Script.Utilities;

namespace ME3Script.Language.Tree
{
    public class Subobject : CodeBody
    {
        public string Name;

        public string Class;

        public Subobject(string name, string @class, List<Statement> contents, SourcePosition start = null, SourcePosition end = null) : base(contents, start, end)
        {
            Name = name;
            Class = @class;
            Type = ASTNodeType.Subobject;
        }

        public override bool AcceptVisitor(IASTVisitor visitor)
        {
            return visitor.VisitNode(this);
        }
    }
}
