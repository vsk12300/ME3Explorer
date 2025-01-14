﻿using System.Collections.Generic;
using LegendaryExplorerCore.UnrealScript.Language.Tree;
using LegendaryExplorerCore.UnrealScript.Lexing.Matching.StringMatchers;
using LegendaryExplorerCore.UnrealScript.Lexing.Tokenizing;

namespace LegendaryExplorerCore.UnrealScript.Utilities
{
    public static class GlobalLists
    {
        public static List<KeywordMatcher> Delimiters;
        public static List<TokenType> ValidOperatorSymbols;

        static GlobalLists()
        {
            Delimiters = new List<KeywordMatcher>
            {
                new KeywordMatcher("{", TokenType.LeftBracket, null),
                new KeywordMatcher("}", TokenType.RightBracket, null),
                new KeywordMatcher("[", TokenType.LeftSqrBracket, null),
                new KeywordMatcher("]", TokenType.RightSqrBracket, null),
                new KeywordMatcher("(", TokenType.LeftParenth, null),
                new KeywordMatcher(")", TokenType.RightParenth, null),
                new KeywordMatcher("==", TokenType.Equals, null),
                new KeywordMatcher("+=", TokenType.AddAssign, null),   
                new KeywordMatcher("-=", TokenType.SubAssign, null),   
                new KeywordMatcher("*=", TokenType.MulAssign, null),   
                new KeywordMatcher("/=", TokenType.DivAssign, null),      
                new KeywordMatcher("!=", TokenType.NotEquals, null),  
                new KeywordMatcher("~=", TokenType.ApproxEquals, null), 
                new KeywordMatcher(">>>", TokenType.VectorTransform, null),
                //new KeywordMatcher(">>", TokenType.RightShift, null),    //will have to be matched manually in the parser. conflicts with arrays of delegates: array<delegate<somefunc>>
                new KeywordMatcher("<<", TokenType.LeftShift, null),
                new KeywordMatcher("<=", TokenType.LessOrEquals, null),
                new KeywordMatcher(">=", TokenType.GreaterOrEquals, null),
                new KeywordMatcher("**", TokenType.Power, null), 
                new KeywordMatcher("&&", TokenType.And, null),   
                new KeywordMatcher("||", TokenType.Or, null),         
                new KeywordMatcher("^^", TokenType.Xor, null),
                new KeywordMatcher("<", TokenType.LeftArrow, null),    
                new KeywordMatcher(">", TokenType.RightArrow, null),         
                new KeywordMatcher("%", TokenType.Modulo, null),
                new KeywordMatcher("$=", TokenType.StrConcatAssign, null),
                new KeywordMatcher("$", TokenType.DollarSign, null),
                new KeywordMatcher("@=", TokenType.StrConcAssSpace, null),
                new KeywordMatcher("@", TokenType.AtSign, null),
                new KeywordMatcher("--", TokenType.Decrement, null),
                new KeywordMatcher("++", TokenType.Increment, null),
                new KeywordMatcher("-", TokenType.MinusSign, null),      
                new KeywordMatcher("+", TokenType.PlusSign, null),        
                new KeywordMatcher("*", TokenType.StarSign, null),   
                new KeywordMatcher("/", TokenType.Slash, null),
                new KeywordMatcher("=", TokenType.Assign, null),  
                new KeywordMatcher("~", TokenType.Complement, null), 
                new KeywordMatcher("&", TokenType.BinaryAnd, null),    
                new KeywordMatcher("|", TokenType.BinaryOr, null),     
                new KeywordMatcher("^", TokenType.BinaryXor, null),     
                new KeywordMatcher("?", TokenType.QuestionMark, null),   
                new KeywordMatcher(":", TokenType.Colon, null),
                new KeywordMatcher(";", TokenType.SemiColon, null),
                new KeywordMatcher(",", TokenType.Comma, null),
                new KeywordMatcher(".", TokenType.Dot, null),
                new KeywordMatcher("!", TokenType.ExclamationMark, null),
                new KeywordMatcher("#", TokenType.Hash, null)
            };

            ValidOperatorSymbols = new List<TokenType>
            {
                TokenType.Equals,    
                TokenType.AddAssign,   
                TokenType.SubAssign,   
                TokenType.MulAssign,   
                TokenType.DivAssign,      
                TokenType.NotEquals,  
                TokenType.ApproxEquals, 
                //TokenType.RightShift,    
                TokenType.LeftShift,
                TokenType.LessOrEquals,
                TokenType.GreaterOrEquals,
                TokenType.Power, 
                TokenType.And,   
                TokenType.Or,         
                TokenType.Xor,
                TokenType.LeftArrow,    
                TokenType.RightArrow,         
                TokenType.Modulo,
                TokenType.StrConcatAssign,
                TokenType.DollarSign,
                TokenType.StrConcAssSpace,
                TokenType.AtSign,
                TokenType.MinusSign,      
                TokenType.PlusSign,        
                TokenType.StarSign,   
                TokenType.Slash,
                TokenType.Complement, 
                TokenType.BinaryAnd,    
                TokenType.BinaryOr,     
                TokenType.BinaryXor,     
                TokenType.QuestionMark,   
                //TokenType.Colon,
                //TokenType.SemiColon,
                //TokenType.Comma,
                //TokenType.Dot,
                TokenType.ExclamationMark,
                TokenType.Hash,
                TokenType.VectorTransform
            };

            //Keywords = new List<KeywordMatcher>
            //{
            //    new KeywordMatcher("VectorCross", TokenType.VectorCross, Delimiters, false),
            //    new KeywordMatcher("VectorDot", TokenType.VectorDot, Delimiters, false),
            //    new KeywordMatcher("IsClockwiseFrom", TokenType.IsClockwiseFrom, Delimiters, false),
            //    new KeywordMatcher("var", TokenType.InstanceVariable, Delimiters, false),
            //    new KeywordMatcher("local", TokenType.LocalVariable, Delimiters, false),
            //    //new KeywordMatcher("byte", TokenType.Byte, Delimiters, false),
            //    //new KeywordMatcher("int", TokenType.Int, Delimiters, false),
            //    //new KeywordMatcher("bool", TokenType.Bool, Delimiters, false),
            //    //new KeywordMatcher("float", TokenType.Float, Delimiters, false),
            //    //new KeywordMatcher("string", TokenType.String, Delimiters, false),
            //    new KeywordMatcher("enum", TokenType.Enumeration, Delimiters, false),
            //    new KeywordMatcher("array", TokenType.Array, Delimiters, false),
            //    new KeywordMatcher("struct", TokenType.Struct, Delimiters, false),
            //    new KeywordMatcher("class", TokenType.Class, Delimiters, false),
            //    new KeywordMatcher("defaultproperties", TokenType.DefaultProperties, Delimiters, false),
            //    //new KeywordMatcher("Name", TokenType.Name, Delimiters, false),
            //    //new KeywordMatcher("Object", TokenType.Object, Delimiters, false),
            //    //new KeywordMatcher("Actor", TokenType.Actor, Delimiters, false),
            //    //new KeywordMatcher("Vector", TokenType.Vector, Delimiters, false),
            //    //new KeywordMatcher("Rotator", TokenType.Rotator, Delimiters, false),
            //    new KeywordMatcher("const", TokenType.Constant, Delimiters, false),
            //    new KeywordMatcher("None", TokenType.None, Delimiters, false),
            //    new KeywordMatcher("Self", TokenType.Self, Delimiters, false),
            //    new KeywordMatcher("EnumCount", TokenType.EnumCount, Delimiters, false),
            //    new KeywordMatcher("ArrayCount", TokenType.ArrayCount, Delimiters, false),
            //    new KeywordMatcher("extends", TokenType.Extends, Delimiters, false),
            //    new KeywordMatcher("within", TokenType.Within, Delimiters, false),
            //    new KeywordMatcher("state", TokenType.State, Delimiters, false),
            //    new KeywordMatcher("function", TokenType.Function, Delimiters, false),
            //    new KeywordMatcher("ignores", TokenType.Ignores, Delimiters, false),
            //    new KeywordMatcher("if", TokenType.If, Delimiters, false),
            //    new KeywordMatcher("else", TokenType.Else, Delimiters, false),
            //    new KeywordMatcher("while", TokenType.While, Delimiters, false),
            //    new KeywordMatcher("do", TokenType.Do, Delimiters, false),
            //    new KeywordMatcher("until", TokenType.Until, Delimiters, false),
            //    new KeywordMatcher("for", TokenType.For, Delimiters, false),
            //    new KeywordMatcher("continue", TokenType.Continue, Delimiters, false),
            //    new KeywordMatcher("break", TokenType.Break, Delimiters, false),
            //    new KeywordMatcher("ForEach", TokenType.ForEach, Delimiters, false),
            //    new KeywordMatcher("return", TokenType.Return, Delimiters, false),
            //    new KeywordMatcher("switch", TokenType.Switch, Delimiters, false),
            //    new KeywordMatcher("case", TokenType.Case, Delimiters, false),
            //    new KeywordMatcher("default", TokenType.Default, Delimiters, false),
            //    new KeywordMatcher("true", TokenType.True, Delimiters, false),
            //    new KeywordMatcher("false", TokenType.False, Delimiters, false),

            //    //specifiers aren't technically keywords?
            //    //new KeywordMatcher("delegate", TokenType.Delegate, Delimiters, false),
            //    //new KeywordMatcher("auto", TokenType.AutoSpecifier, Delimiters, false),
            //    //new KeywordMatcher("event", TokenType.EventSpecifier, Delimiters, false),
            //    //new KeywordMatcher("GlobalConfig", TokenType.GlobalConfigSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Config", TokenType.ConfigSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Localized", TokenType.LocalizedSpecifier, Delimiters, false),
            //    //new KeywordMatcher("placeable", TokenType.PlaceableSpecifier, Delimiters, false),
            //    //new KeywordMatcher("PrivateWrite", TokenType.PrivateWriteSpecifier, Delimiters, false),
            //    //new KeywordMatcher("ProtectedWrite", TokenType.ProtectedWriteSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Private", TokenType.PrivateSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Protected", TokenType.ProtectedSpecifier, Delimiters, false),
            //    //new KeywordMatcher("RepNotify", TokenType.RepNotifySpecifier, Delimiters, false),
            //    //new KeywordMatcher("Deprecated", TokenType.DeprecatedSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Instanced", TokenType.InstancedSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Databinding", TokenType.DatabindingSpecifier, Delimiters, false),
            //    //new KeywordMatcher("EditorOnly", TokenType.EditorOnlySpecifier, Delimiters, false),
            //    //new KeywordMatcher("NotForConsole", TokenType.NotForConsoleSpecifier, Delimiters, false),
            //    //new KeywordMatcher("EditConst", TokenType.EditConstSpecifier, Delimiters, false),
            //    //new KeywordMatcher("EditFixedSize", TokenType.EditFixedSizeSpecifier, Delimiters, false),
            //    //new KeywordMatcher("EditInline", TokenType.EditInlineSpecifier, Delimiters, false),
            //    //new KeywordMatcher("EditInlineUse", TokenType.EditInlineUseSpecifier, Delimiters, false),
            //    //new KeywordMatcher("immutablewhencooked", TokenType.ImmutableWhenCookedSpecifier, Delimiters, false),
            //    //new KeywordMatcher("NoClear", TokenType.NoClearSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Interp", TokenType.InterpSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Input", TokenType.InputSpecifier, Delimiters, false),
            //    //new KeywordMatcher("abstract", TokenType.AbstractSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Transient", TokenType.TransientSpecifier, Delimiters, false),
            //    //new KeywordMatcher("DuplicateTransient", TokenType.DuplicateTransientSpecifier, Delimiters, false),
            //    //new KeywordMatcher("NoImport", TokenType.NoImportSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Native", TokenType.NativeSpecifier, Delimiters, false),
            //    //new KeywordMatcher("nativereplication", TokenType.NativeReplicationSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Export", TokenType.ExportSpecifier, Delimiters, false),
            //    //new KeywordMatcher("NoExport", TokenType.NoExportSpecifier, Delimiters, false),
            //    //new KeywordMatcher("NonTransactional", TokenType.NonTransactionalSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Pointer", TokenType.PointerSpecifier, Delimiters, false),
            //    //new KeywordMatcher("RepRetry", TokenType.RepRetrySpecifier, Delimiters, false),
            //    //new KeywordMatcher("AllowAbstract", TokenType.AllowAbstractSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Out", TokenType.OutSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Coerce", TokenType.CoerceSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Optional", TokenType.OptionalSpecifier, Delimiters, false),
            //    //new KeywordMatcher("Skip", TokenType.SkipSpecifier, Delimiters, false),
            //    //new KeywordMatcher("public", TokenType.PublicSpecifier, Delimiters, false),
            //    //new KeywordMatcher("final", TokenType.FinalSpecifier, Delimiters, false),
            //    //new KeywordMatcher("exec", TokenType.ExecSpecifier, Delimiters, false),
            //    //new KeywordMatcher("K2Call", TokenType.K2CallSpecifier, Delimiters, false),
            //    //new KeywordMatcher("K2Override", TokenType.K2OverrideSpecifier, Delimiters, false),
            //    //new KeywordMatcher("K2Pure", TokenType.K2PureSpecifier, Delimiters, false),
            //    //new KeywordMatcher("static", TokenType.StaticSpecifier, Delimiters, false),
            //    //new KeywordMatcher("simulated", TokenType.SimulatedSpecifier, Delimiters, false),
            //    //new KeywordMatcher("singular", TokenType.SingularSpecifier, Delimiters, false),
            //    //new KeywordMatcher("client", TokenType.ClientSpecifier, Delimiters, false),
            //    //new KeywordMatcher("DemoRecording", TokenType.DemoRecordingSpecifier, Delimiters, false),
            //    //new KeywordMatcher("reliable", TokenType.ReliableSpecifier, Delimiters, false),
            //    //new KeywordMatcher("server", TokenType.ServerSpecifier, Delimiters, false),
            //    //new KeywordMatcher("unreliable", TokenType.UnreliableSpecifier, Delimiters, false),
            //    //new KeywordMatcher("iterator", TokenType.IteratorSpecifier, Delimiters, false),
            //    //new KeywordMatcher("latent", TokenType.LatentSpecifier, Delimiters, false),
            //    //new KeywordMatcher("operator", TokenType.Operator, Delimiters, false),
            //    //new KeywordMatcher("preoperator", TokenType.PreOperator, Delimiters, false),
            //    //new KeywordMatcher("postoperator", TokenType.PostOperator, Delimiters, false),
            //};
        }

        public static List<ASTNodeType> SemicolonExceptions = new List<ASTNodeType>
        {   // TODO: check this for other types
            ASTNodeType.ForLoop,
            ASTNodeType.WhileLoop,
            ASTNodeType.IfStatement,
            ASTNodeType.VariableDeclaration,
            ASTNodeType.SwitchStatement,
            ASTNodeType.CaseStatement,
            ASTNodeType.DefaultStatement,
            ASTNodeType.StateLabel,
            ASTNodeType.ForEachLoop
        };

        #region Specifier Categories
        //public static List<TokenType> VariableSpecifiers = new List<TokenType>
        //{
        //    TokenType.ConfigSpecifier,
        //    TokenType.GlobalConfigSpecifier,
        //    TokenType.LocalizedSpecifier,
        //    TokenType.Constant,
        //    TokenType.PrivateSpecifier,
        //    TokenType.ProtectedSpecifier,
        //    TokenType.PrivateWriteSpecifier,
        //    TokenType.ProtectedWriteSpecifier,
        //    TokenType.RepNotifySpecifier,
        //    TokenType.DeprecatedSpecifier,
        //    TokenType.InstancedSpecifier,
        //    TokenType.DatabindingSpecifier,
        //    TokenType.EditorOnlySpecifier,
        //    TokenType.NotForConsoleSpecifier,
        //    TokenType.EditConstSpecifier,
        //    TokenType.EditFixedSizeSpecifier,
        //    TokenType.EditInlineSpecifier,
        //    TokenType.EditInlineUseSpecifier,
        //    TokenType.NoClearSpecifier,
        //    TokenType.InterpSpecifier,
        //    TokenType.InputSpecifier,
        //    TokenType.TransientSpecifier,
        //    TokenType.DuplicateTransientSpecifier,
        //    TokenType.NoImportSpecifier,
        //    TokenType.NativeSpecifier,
        //    TokenType.ExportSpecifier,
        //    TokenType.NoExportSpecifier,
        //    TokenType.NonTransactionalSpecifier,
        //    TokenType.PointerSpecifier,
        //    TokenType.RepRetrySpecifier,
        //    TokenType.AllowAbstractSpecifier
        //};

        //public static List<TokenType> ClassSpecifiers = new List<TokenType>
        //{
        //    TokenType.AbstractSpecifier,
        //    TokenType.ConfigSpecifier,
        //    TokenType.DependsOnSpecifier,
        //    TokenType.ImplementsSpecifier,
        //    TokenType.InstancedSpecifier,
        //    TokenType.ParseConfigSpecifier,
        //    TokenType.PerObjectConfigSpecifier,
        //    TokenType.PerObjectLocalizedSpecifier,
        //    TokenType.TransientSpecifier,
        //    TokenType.NonTransientSpecifier,
        //    TokenType.DeprecatedSpecifier,
        //    TokenType.PlaceableSpecifier,
        //    TokenType.NativeReplicationSpecifier
        //};

        //public static List<TokenType> StructSpecifiers = new List<TokenType>
        //{
        //    TokenType.ImmutableSpecifier,
        //    TokenType.ImmutableWhenCookedSpecifier,
        //    TokenType.AtomicSpecifier,
        //    TokenType.AtomicWhenCookedSpecifier,
        //    TokenType.StrictConfigSpecifier,
        //    TokenType.TransientSpecifier,
        //    TokenType.NativeSpecifier
        //};

        //public static List<TokenType> FunctionSpecifiers = new List<TokenType>
        //{
        //    TokenType.PrivateSpecifier,
        //    TokenType.ProtectedSpecifier,
        //    TokenType.PublicSpecifier,
        //    TokenType.StaticSpecifier,
        //    TokenType.FinalSpecifier,
        //    TokenType.ExecSpecifier,
        //    TokenType.K2CallSpecifier,
        //    TokenType.K2OverrideSpecifier,
        //    TokenType.K2PureSpecifier,
        //    TokenType.SimulatedSpecifier,
        //    TokenType.SingularSpecifier,
        //    TokenType.ClientSpecifier,
        //    TokenType.DemoRecordingSpecifier,
        //    TokenType.ReliableSpecifier,
        //    TokenType.ServerSpecifier,
        //    TokenType.UnreliableSpecifier,
        //    TokenType.Constant,
        //    TokenType.IteratorSpecifier,
        //    TokenType.LatentSpecifier,
        //    TokenType.NativeSpecifier,
        //    TokenType.NoExportSpecifier
        //};

        //public static List<TokenType> ParameterSpecifiers = new List<TokenType>
        //{
        //    TokenType.CoerceSpecifier,
        //    TokenType.Constant,
        //    TokenType.OptionalSpecifier,
        //    TokenType.OutSpecifier,
        //    TokenType.SkipSpecifier
        //};

        //public static List<TokenType> StateSpecifiers = new List<TokenType>
        //{
        //    TokenType.AutoSpecifier,
        //    TokenType.SimulatedSpecifier
        //};

        #endregion

    }
}
