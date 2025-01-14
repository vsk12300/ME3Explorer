﻿using System.Collections.Generic;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.UnrealScript.Analysis.Visitors;
using LegendaryExplorerCore.UnrealScript.Utilities;

namespace LegendaryExplorerCore.UnrealScript.Language.Tree
{
    public class VariableDeclaration : Statement, IHasFileReference
    {
        public UnrealFlags.EPropertyFlags Flags;

        public VariableType VarType;

        public string Category;
        public string Name { get; }

        public int ArrayLength;

        public bool IsStaticArray => ArrayLength > 1;

        public VariableDeclaration(VariableType type, UnrealFlags.EPropertyFlags flags,
                                   string name, int arrayLength = 1, string category = "None", SourcePosition start = null, SourcePosition end = null)
            : base(ASTNodeType.VariableDeclaration, start, end)
        {
            Flags = flags;
            Name = name;
            ArrayLength = arrayLength;
            Category = category ?? "None";
            VarType = IsStaticArray  && !(type is StaticArrayType) ? new StaticArrayType(type, ArrayLength) : type;
        }

        public override bool AcceptVisitor(IASTVisitor visitor)
        {
            return visitor.VisitNode(this);
        }
        public override IEnumerable<ASTNode> ChildNodes
        {
            get
            {
                yield return VarType;
            }
        }

        public int GetSize() => VarType?.Size ?? 0;

        public string FilePath { get; init; }
        public int UIndex { get; set; }

        public VariableDeclaration Clone()
        {
            return new VariableDeclaration(VarType, Flags, Name, ArrayLength, Category);
        }
    }
}
