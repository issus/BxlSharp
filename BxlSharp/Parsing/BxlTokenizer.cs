using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using OriginalCircuit.BxlSharp.Parsing;

namespace OriginalCircuit.BxlSharp.Parser
{
    public enum Token
    {
        NONE,

        /* XLR tokens */
        KW_LAYERDATA,
        KW_LAYER,
        KW_NAME,
        KW_LAYERTYPE,
        KW_BOARDLAYERTYPE,
        KW_LAYERORDER,
        KW_TEXTSTYLES,
        KW_PADSTACKS,
        KW_PATTERNS,
        KW_3DMODELS,
        KW_SYMBOLS,
        KW_COMPONENTS,
        KW_SUPERCOMPONENTS,
        KW_ATTACHEDFILES,
        KW_WORKSPACESIZE,
        KW_COMPONENTINSTANCES,
        KW_VIAINSTANCES,
        KW_VIA,
        KW_NETS,
        KW_NET,
        KW_SCHEMATICOMPONENTINSTANCES,
        KW_SCHEMATICNETS,
        KW_SCHEMATICDATA,
        KW_UNITS,
        KW_SHEET,
        KW_WORKSPACE,
        KW_SHEETS,
        KW_NUMBER,
        KW_SHOWBORDER,
        KW_BORDERNAME,
        KW_SCALEFACTOR,
        KW_OFFSET,
        KW_WIRE,
        KW_PORT,
        KW_JUNCTION,
        KW_COPPERPOUR,
        KW_LAYERS,
        KW_LAYERNUMBER,
        KW_LAYERTECHNICALDATA,
        KW_EOF,

        /* XLR and BXL common tokens */
        KW_PADSTACK_BEGIN,
        KW_PADSTACK_END,
        KW_PATTERN_BEGIN,
        KW_PATTERN_END,
        KW_SYMBOL_BEGIN,
        KW_SYMBOL_END,
        KW_COMPONENT_BEGIN,
        KW_COMPONENT_END,
        KW_DATA_BEGIN,
        KW_DATA_END,
        KW_COMPPINS_BEGIN,
        KW_COMPPINS_END,
        KW_COMPDATA_BEGIN,
        KW_COMPDATA_END,
        KW_ATTACHEDSYMBOLS_BEGIN,
        KW_ATTACHEDSYMBOLS_END,
        KW_PINMAP_BEGIN,
        KW_PINMAP_END,

        KW_TEXTSTYLE,
        KW_ORIGINPOINT,
        KW_PICKPOINT,
        KW_GLUEPOINT,
        KW_PINSRENAMED,
        KW_PATTERNNAME,
        KW_ALTERNATEPATTERN,
        KW_ORIGINALNAME,
        KW_EDITED,
        KW_SOURCELIBRARY,
        KW_REFDESPREFIX,
        KW_NUMBEROFPINS,
        KW_NUMPARTS,
        KW_COMPOSITION,
        KW_ALTIEEE,
        KW_ALTDEMORGAN,
        KW_PATTERNPINS,
        KW_REVISIONLEVEL,
        KW_REVISIONNOTE,
        KW_SHAPES,
        KW_PADSHAPE,
        KW_PAD,
        KW_DELETEDPAD,
        KW_POLY,
        KW_POLYKEEPOUT,
        KW_LINE,
        KW_ARC,
        KW_TEXT,
        KW_PIN,
        KW_PINDES,
        KW_PINNAME,
        KW_ATTRIBUTE,
        KW_WIZARD,
        KW_TEMPLATEDATA,
        KW_COMPPIN,
        KW_ATTACHEDSYMBOL,
        KW_RELATEDFILES,
        KW_PADNUM,

        PAREN_L,
        PAREN_R,
        COMMA,
        COLON,
        SLASH,

        LIT_DECIMAL,
        LIT_INTEGER,
        LIT_BOOLEAN,
        LIT_STRING,
        IDENTIFIER,
        COMMENT,
    }

    /// <summary>
    /// Used to represent a matched token with the proper <seealso cref="Token"/>,
    /// its <seealso cref="Lexeme"/> and the <seealso cref="Value"/> of the token
    /// lexeme if appropriate.
    /// </summary>
    internal struct TokenMatch
    {
        /// <summary>
        /// This is a placeholder constant value used for tokens are matched
        /// but that don't need a parameter value. This is needed because the
        /// token attribute value may be null so we need to be able to tell
        /// a value of null from no value.
        /// </summary>
        private static readonly object NoValue = new object();

        /// <summary>
        /// Used to represent a failed token match.
        /// </summary>
        public static readonly TokenMatch Fail = new TokenMatch { IsSuccess = false, Value = NoValue };

        public bool IsSuccess { get; private set; }
        public Token Token { get; private set; }
        public string Lexeme { get; private set; }
        public object Value { get; private set; }
        public bool HasValue => Value != NoValue;

        public static TokenMatch Success(Token token, string lexeme) =>
            new TokenMatch
            {
                Token = token,
                IsSuccess = true,
                Lexeme = lexeme,
                Value = NoValue
            };

        public static TokenMatch Success(Token token, string lexeme, object value) =>
            new TokenMatch
            {
                Token = token,
                IsSuccess = true,
                Lexeme = lexeme,
                Value = value
            };
    }

    /// <summary>
    /// Structure used to represent the current state of the tokenizer. Having this allows saving the
    /// tokenizer state and restoring it later.
    /// </summary>
    internal readonly struct TokenizerState
    {
        public readonly int ParenLevel;
        public readonly int Position;
        public readonly int Line;
        public readonly int Column;
        public readonly TokenMatch Current;

        public TokenizerState(int parenLevel, int position, int line, int column, TokenMatch current)
        {
            ParenLevel = parenLevel;
            Position = position;
            Line = line;
            Column = column;
            Current = current;
        }

        public TokenizerState With(int? parenLevel = null, int? position = null, int? line = null,
            int? column = null, TokenMatch? current = null)
        {
            return new TokenizerState(parenLevel ?? ParenLevel, position ?? Position, line ?? Line,
                column ?? Column, current ?? Current);
        }
    }

    /// <summary>
    /// Tokenizer for the BXL textual representation.
    /// </summary>
    public class BxlTokenizer
    {
        /// <summary>
        /// List of the keyword token patterns. It is separated from the general tokens because
        /// keywords are not reserved and only should be matched in the proper state.
        /// </summary>
        private static readonly List<Func<string, int, TokenMatch>> KeywordPatterns =
            new List<Func<string, int, TokenMatch>>();

        /// <summary>
        /// List of general keyword patterns that can be matched anywhere in the input.
        /// </summary>
        private static readonly List<Func<string, int, TokenMatch>> GeneralPatterns =
            new List<Func<string, int, TokenMatch>>();

        static BxlTokenizer()
        {
            /* XLR tokens */
            KeywordPatterns.Add(KeywordMatcher(Token.KW_LAYERDATA, "LayerData"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_LAYER, "Layer"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_NAME, "Name"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_LAYERTYPE, "LayerType"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_BOARDLAYERTYPE, "BoardLayerType"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_LAYERORDER, "LayerOrder"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_TEXTSTYLES, "TextStyles"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PADSTACKS, "PadStacks"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PATTERNS, "Patterns"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_3DMODELS, "3DModels"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SYMBOLS, "Symbols"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_COMPONENTS, "Components"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SUPERCOMPONENTS, "SuperComponents"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_ATTACHEDFILES, "AttachedFiles"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_WORKSPACESIZE, "WorkSpaceSize"));

            KeywordPatterns.Add(KeywordMatcher(Token.KW_COMPONENTINSTANCES, "ComponentInstances"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_VIAINSTANCES, "ViaInstances"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_VIA, "Via"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_NETS, "Nets"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_NET, "Net"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SCHEMATICOMPONENTINSTANCES, "SchematicComponentInstances"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SCHEMATICNETS, "SchematicNets"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SCHEMATICDATA, "SchematicData"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_UNITS, "Units"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SHEET, "Sheet"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_WORKSPACE, "Workspace"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SHEETS, "Sheets"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_NUMBER, "Number"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SHOWBORDER, "ShowBorder"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_BORDERNAME, "BorderName"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SCALEFACTOR, "ScaleFactor"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_OFFSET, "OffSet"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_WIRE, "Wire"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PORT, "Port"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_JUNCTION, "Junction"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_COPPERPOUR, "Copperpour"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_LAYERS, "Layers"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_LAYERNUMBER, "LayerNumber"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_LAYERTECHNICALDATA, "LayerTechnicalData"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_EOF, "End of File"));

            /* XLR and BXL common tokens */
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PADSTACK_BEGIN, "PadStack"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PADSTACK_END, "EndPadStack"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PATTERN_BEGIN, "Pattern"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PATTERN_END, "EndPattern"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SYMBOL_BEGIN, "Symbol"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SYMBOL_END, "EndSymbol"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_COMPONENT_BEGIN, "Component"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_COMPONENT_END, "EndComponent"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_DATA_BEGIN, "Data"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_DATA_END, "EndData"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_COMPPINS_BEGIN, "CompPins"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_COMPPINS_END, "EndCompPins"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_COMPDATA_BEGIN, "CompData"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_COMPDATA_END, "EndCompData"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_ATTACHEDSYMBOLS_BEGIN, "AttachedSymbols"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_ATTACHEDSYMBOLS_END, "EndAttachedSymbols"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PINMAP_BEGIN, "PinMap"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PINMAP_END, "EndPinMap"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_TEXTSTYLE, "TextStyle"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_ORIGINPOINT, "OriginPoint"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PICKPOINT, "PickPoint"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_GLUEPOINT, "GluePoint"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PINSRENAMED, "PinsRenamed"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PATTERNNAME, "PatternName"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_ALTERNATEPATTERN, "AlternatePattern"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_ORIGINALNAME, "OriginalName"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_EDITED, "Edited"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SOURCELIBRARY, "SourceLibrary"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_REFDESPREFIX, "RefDesPrefix"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_NUMBEROFPINS, "NumberofPins"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_NUMPARTS, "NumParts"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_COMPOSITION, "Composition"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_ALTIEEE, "AltIEEE"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_ALTDEMORGAN, "AltDeMorgan"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PATTERNPINS, "PatternPins"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_REVISIONLEVEL, "Revision Level"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_REVISIONNOTE, "Revision Note"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_SHAPES, "Shapes"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PADSHAPE, "PadShape"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PAD, "Pad"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_DELETEDPAD, "Deletedpad"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_POLY, "Poly"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_POLYKEEPOUT, "Polykeepout"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_LINE, "Line"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_ARC, "Arc"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_TEXT, "Text"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PIN, "Pin"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PINDES, "PinDes"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PINNAME, "PinName"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_ATTRIBUTE, "Attribute"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_WIZARD, "Wizard"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_TEMPLATEDATA, "Templatedata"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_COMPPIN, "CompPin"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_ATTACHEDSYMBOL, "AttachedSymbol"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_RELATEDFILES, "RelatedFiles"));
            KeywordPatterns.Add(KeywordMatcher(Token.KW_PADNUM, "PadNum"));

            GeneralPatterns.Add(PunctuationMatcher(Token.PAREN_L, "("));
            GeneralPatterns.Add(PunctuationMatcher(Token.PAREN_R, ")"));
            GeneralPatterns.Add(PunctuationMatcher(Token.COMMA, ","));
            GeneralPatterns.Add(PunctuationMatcher(Token.COLON, ":"));
            GeneralPatterns.Add(PunctuationMatcher(Token.SLASH, "/"));

            GeneralPatterns.Add(RegexMatcher(Token.LIT_DECIMAL, @"[-+]?\d*\.\d+(e-\d+)?\b", l => double.Parse(l, CultureInfo.InvariantCulture)));
            GeneralPatterns.Add(RegexMatcher(Token.LIT_INTEGER, @"[-+]?\d+\b", l => int.Parse(l, CultureInfo.InvariantCulture)));
            GeneralPatterns.Add(RegexMatcher(Token.LIT_BOOLEAN, @"(True|False)\b", l => l.Equals("True", StringComparison.InvariantCultureIgnoreCase)));
            GeneralPatterns.Add(RegexMatcher(Token.LIT_STRING, @""""".*?""""", l => l.Substring(2, l.Length - 4))); // some files use doubly quoted strings
            GeneralPatterns.Add(RegexMatcher(Token.LIT_STRING, @"""(.*?(\d""\s*[\(,]?\s*\d+(\.\d+)?mm)*?)*""", l => l.Substring(1, l.Length - 2))); // this odd regular expression is to allow for regular strings and also strings that contain inches... like  "0.180" (4.57mm)" or "8-SOIC (0.154", 3.90mm Width)"
            //GeneralPatterns.Add(RegexMatcher(Token.LIT_STRING, @""".*?""", l => l.Substring(1, l.Length - 2))); 
            GeneralPatterns.Add(RegexMatcher(Token.IDENTIFIER, @"Open Collector|Open Emitter", l => l)); // special case because those two values are stored with an space
            GeneralPatterns.Add(RegexMatcher(Token.IDENTIFIER, @"[\w-]+", l => l));
            GeneralPatterns.Add(RegexMatcher(Token.COMMENT, @"#.*?\n", l => l.Substring(1)));
        }

        private static IEnumerable<Func<string, int, TokenMatch>> GetPatterns(bool includeKeywords) =>
            includeKeywords ? Enumerable.Concat(KeywordPatterns, GeneralPatterns) : GeneralPatterns;

        /// <summary>
        /// Creates a matcher that returns when the input matches a <paramref name="lexeme"/> that is surrounded by separators.
        /// </summary>
        /// <param name="token">Token this attempts to match.</param>
        /// <param name="lexeme">Lexeme text to be matched.</param>
        /// <returns>
        /// Returns a token match, either indicating the token that was matched with its lexeme and possible value,
        /// or <seealso cref="TokenMatch.Fail"/> otherwise.
        /// </returns>
        private static Func<string, int, TokenMatch> KeywordMatcher(Token token, string lexeme)
        {
            var pattern = $"\\G[^(]?(?:{Regex.Escape(lexeme)})\\b";
            var re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
            return (input, start) =>
            {
                return re.IsMatch(input, start > 0 ? start - 1 : start) ? TokenMatch.Success(token, lexeme) : TokenMatch.Fail;
            };
        }

        /// <summary>
        /// Creates a matcher that returns when the input matches a punctuation <paramref name="lexeme"/>, escaping the given
        /// lexeme text if needed.
        /// </summary>
        /// <param name="token">Token this attempts to match.</param>
        /// <param name="lexeme">Lexeme text to be matched.</param>
        /// <returns>
        /// Returns a token match, either indicating the token that was matched with its lexeme and possible value,
        /// or <seealso cref="TokenMatch.Fail"/> otherwise.
        /// </returns>
        private static Func<string, int, TokenMatch> PunctuationMatcher(Token token, string lexeme)
        {
            var pattern = $"\\G(?:{Regex.Escape(lexeme)})";
            var re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
            return (input, start) =>
            {
                return re.IsMatch(input, start) ? TokenMatch.Success(token, lexeme) : TokenMatch.Fail;
            };
        }

        /// <summary>
        /// Creates a matcher that returns when the input matches a regular raw expression <paramref name="pattern"/>
        /// </summary>
        /// <param name="token">Token this attempts to match.</param>
        /// <param name="pattern">Regular expression text to be matched.</param>
        /// <param name="converter">Function used to convert the matched lexeme into a value.</param>
        /// <returns>
        /// Returns a token match, either indicating the token that was matched with its lexeme and possible value,
        /// or <seealso cref="TokenMatch.Fail"/> otherwise.
        /// </returns>
        private static Func<string, int, TokenMatch> RegexMatcher(Token token, string pattern, Func<string, object> converter)
        {
            pattern = $"\\G(?:{pattern})";
            var re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
            return (input, start) =>
            {
                var m = re.Match(input, start);
                return m.Success ? TokenMatch.Success(token, m.Value, converter(m.Value)) : TokenMatch.Fail;
            };
        }

        /// <summary>
        /// Input text being tokenized.
        /// </summary>
        public string Text { get; }
        internal TokenizerState State { get; private set; }

        private int ParenLevel
        {
            get => State.ParenLevel;
            set => State = State.With(parenLevel: value);
        }

        /// <summary>
        /// Current position in the input text.
        /// </summary>
        public int Position
        {
            get => State.Position;
            private set => State = State.With(position: value);
        }

        /// <summary>
        /// Current line. Used for error reporting.
        /// </summary>
        public int Line
        {
            get => State.Line;
            private set => State = State.With(line: value);
        }

        /// <summary>
        /// Current column. Used for error reporting.
        /// </summary>
        public int Column
        {
            get => State.Column;
            private set => State = State.With(column: value);
        }
        
        /// <summary>
        /// Current token match.
        /// </summary>
        internal TokenMatch Current
        {
            get => State.Current;
            private set => State = State.With(current: value);
        }

        /// <summary>
        /// Current token that was last matched.
        /// </summary>
        public Token Token => Current.Token;

        /// <summary>
        /// The lexeme of the current token match.
        /// </summary>
        public string Lexeme => Current.Lexeme;

        /// <summary>
        /// The attribute value of the current token match.
        /// </summary>
        public object Value => Current.Value;

        /// <summary>
        /// Indicates the tokenization has reached the end of the input.
        /// </summary>
        public bool IsEof => Position >= Text.Length;

        public BxlTokenizer(string text)
        {
            Text = text;
            Reset();
        }

        /// <summary>
        /// Resets the tokenizer so it can start reading the input from the start.
        /// </summary>
        public void Reset()
        {
            State = new TokenizerState(0, 0, 1, 1, TokenMatch.Fail);
            Next();
        }

        /// <summary>
        /// Helper method used for incrementing the current position in the input text.
        /// <para>
        /// This has 3 purposes: setting the <seealso cref="ParenLevel"/> to zero on a new line so keywords can
        /// be matched again, counting <seealso cref="Line"/>s and counting <seealso cref="Column"/>s.
        /// </para>
        /// </summary>
        /// <param name="value">Number of charactes to increment the current position.</param>
        private void IncPosition(int value = 1)
        {
            for (int i = 0; i < value; ++i)
            {
                if (Text[Position++] == '\n')
                {
                    ++Line;
                    Column = 1;
                    ParenLevel = 0; // reset level at new line to cope gracefully with missing closing paranthesis
                }
                else
                {
                    ++Column;
                }
            }
        }

        /// <summary>
        /// Increments the input while there is white space in the current position.
        /// </summary>
        private void SkipWhiteSpace()
        {
            while (!IsEof && char.IsWhiteSpace(Text[Position]))
            {
                IncPosition();
            }
        }

        /// <summary>
        /// Loops though the patterns trying to find the first one that matches the current input.
        /// </summary>
        /// <returns>Returns null if no successful <seealso cref="TokenMatch"/> is found.</returns>
        private TokenMatch? FindMatchingPattern()
        {
            // keywords are NOT reserved, meaning identifiers can have the same name as keywords,
            // so we need to test for them to only be recognized outside of any parenthesis
            var includeKeywords = ParenLevel == 0;
            var patterns = GetPatterns(includeKeywords);
            foreach (var matcher in patterns)
            {
                var tm = matcher(Text, Position);
                if (tm.IsSuccess) return tm;
            }
            return null;
        }

        /// <summary>
        /// Makes the tokenizer skip until a character equal to <paramref name="c"/> is found.
        /// <para>
        /// The skipping is achieved by replacing the current token with a <seealso cref="Token.COMMENT"/>
        /// that contains the skipped input as its lexeme. This is done so the parser
        /// <seealso cref="BxlParser.SkipUntil(bool, Token[])"/> method can take notice that
        /// this unusual manipulation has taken place.
        /// </para>
        /// </summary>
        /// <param name="c"></param>
        internal void SkipUntil(char c)
        {
            var dummyLexeme = "";
            var pos = Position;
            while (!IsEof && Text[pos] != c)
            {
                dummyLexeme += Text[pos++];
            }
            Current = TokenMatch.Success(Token.COMMENT, dummyLexeme);
        }

        /// <summary>
        /// Fetches the next token available and stores it as the current token.
        /// </summary>
        public void Next()
        {
            do
            {
                IncPosition(Lexeme?.Length ?? 0);
                SkipWhiteSpace();

                if (IsEof)
                {
                    Current = TokenMatch.Fail;
                }
                else
                {
                    Current = FindMatchingPattern() ??
                        throw new BxlTokenizerException($"Unrecognized input: {Text.Substring(Position, 10)}");

                    // keep track of parenthesis level for enabling keyword recognition
                    // only at the zero level
                    if (Current.Token == Token.PAREN_L)
                    {
                        ++ParenLevel;
                    }
                    else if (Current.Token == Token.PAREN_R)
                    {
                        --ParenLevel;
                    }
                }
            } while (Current.Token == Token.COMMENT); // ignore comments
        }
    }

    [Serializable]
    internal class BxlTokenizerException : Exception
    {
        public BxlTokenizerException()
        {
        }

        public BxlTokenizerException(string message) : base(message)
        {
        }

        public BxlTokenizerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BxlTokenizerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
