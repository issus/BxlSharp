using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using BxlSharp.Parser;
using BxlSharp.Types;

namespace BxlSharp.Parsing
{
    /// <summary>
    /// Parser for the BXL textual representation.
    /// <para>
    /// Supports parsing component libraries and board data, working in tandem with <seealso cref="BxlTokenizer"/>.
    /// </para>
    /// </summary>
    public class BxlParser
    {
        private readonly BxlTokenizer _tokenizer;
        private readonly string _filename;
        private IProgress<int> _progress;
        private int _progressLast;
        private int _progressDelta;

        /// <summary>
        /// Information of the parsing exectution, including information messages, warnings and errors encountered.
        /// </summary>
        public Logs Logs { get; } = new Logs();

        /// <summary>
        /// Parsed document data.
        /// </summary>
        public BxlDocument Data { get; private set; }
        private bool IsEof => _tokenizer.IsEof;
        private Token Peek => _tokenizer.Token;

        /// <summary>
        /// Creates a parser instance for the given input.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <param name="filename">File name of the source input text used for error reporting.</param>
        public BxlParser(string text, string filename)
        {
            _tokenizer = new BxlTokenizer(text);
            _filename = filename;
        }

        /// <summary>
        /// Executes the parsing starting from the root <seealso cref="ReadDocument"/>.
        /// </summary>
        /// <param name="progress">
        /// Optional parameter used for progress reporting of the percentage parsed.
        /// </param>
        /// <returns>
        /// Returns the parsed document, same as the <seealso cref="Data"/> property.
        /// </returns>
        public BxlDocument Execute(IProgress<int> progress = null)
        {
            _tokenizer.Reset();
            _progress = progress;
            _progressLast = 0;
            _progressDelta = _tokenizer.Text.Length / 100;
            Logs.Data.Clear();
            Data = new BxlDocument();
            ReportProgress(true);
            try
            {
                ReadDocument(Data);
            }
            catch (BxlTokenizerException tokenizerException)
            {
                Log(LogSeverity.Error, FormatMessage(tokenizerException.Message));
            }
            catch (BxlParserException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                Log(LogSeverity.InternalError, FormatMessage($"Internal error: {ex}"));
            }
            ReportProgress(true);
            return Data; // allow returning document with partial data
        }

        /// <summary>
        /// Reads the document data and is the main parsing loop.
        /// </summary>
        /// <param name="document">Document into which the data will be read.</param>
        private void ReadDocument(BxlDocument document)
        {
            void SkipUntilNextSupported(bool warnIgnoredData)
            {
                SkipUntil(warnIgnoredData, Token.KW_LAYERDATA, Token.KW_TEXTSTYLES, Token.KW_PADSTACKS, Token.KW_PATTERNS,
                    Token.KW_SYMBOLS, Token.KW_COMPONENTS, Token.KW_WORKSPACESIZE, Token.KW_COMPONENTINSTANCES,
                    Token.KW_VIAINSTANCES, Token.KW_NETS, Token.KW_SCHEMATICOMPONENTINSTANCES, Token.KW_SCHEMATICNETS,
                    Token.KW_SCHEMATICDATA, Token.KW_SHEETS, Token.KW_LAYERS, Token.KW_EOF,
                    Token.KW_TEXTSTYLE, Token.KW_PADSTACK_BEGIN, Token.KW_PATTERN_BEGIN, Token.KW_SYMBOL_BEGIN,
                    Token.KW_COMPONENT_BEGIN);
            }

            while (!IsEof)
            {
                switch (Peek)
                {
                    case Token.KW_LAYERDATA:
                        var layerData = ReadCollection(Token.KW_LAYERDATA, Token.NONE, ReadLayer);
                        document.LayerData.AddRange(layerData);
                        break;
                    case Token.KW_TEXTSTYLES:
                        var textStyles = ReadCollection(Token.KW_TEXTSTYLES, Token.NONE, ReadTextStyle);
                        document.TextStyles.AddRange(textStyles);
                        break;
                    case Token.KW_PADSTACKS:
                        var padStyles = ReadCollection(Token.KW_PADSTACKS, Token.NONE, ReadPadStack);
                        document.PadStyles.AddRange(padStyles);
                        break;
                    case Token.KW_PATTERNS:
                        var patterns = ReadCollection(Token.KW_PATTERNS, Token.NONE, ReadPattern);
                        document.Footprints.AddRange(patterns);
                        break;
                    case Token.KW_SYMBOLS:
                        var symbols = ReadCollection(Token.KW_SYMBOLS, Token.NONE, ReadSymbol);
                        document.Symbols.AddRange(symbols);
                        break;
                    case Token.KW_COMPONENTS:
                        var components = ReadCollection(Token.KW_COMPONENTS, Token.NONE, ReadComponent);
                        document.Components.AddRange(components);
                        break;
                    case Token.KW_WORKSPACESIZE:
                        ReadWorkSpaceSize(document.WorkSpaceSize);
                        break;

                    case Token.KW_COMPONENTINSTANCES:
                        var componentInstances = ReadCollection(Token.KW_COMPONENTINSTANCES, Token.NONE, ReadComponentInstance);
                        document.ComponentInstances.AddRange(componentInstances);
                        break;
                    case Token.KW_VIAINSTANCES:
                        var viaInstances = ReadCollection(Token.KW_VIAINSTANCES, Token.NONE, () => ReadSimpleItem<ViaInstance>(Token.KW_VIA));
                        document.ViaInstances.AddRange(viaInstances);
                        break;
                    case Token.KW_NETS:
                        var nets = ReadCollection(Token.KW_NETS, Token.NONE, ReadNet);
                        document.Nets.AddRange(nets);
                        break;
                    case Token.KW_SCHEMATICOMPONENTINSTANCES:
                        var schematicComponentInstances = ReadCollection(Token.KW_SCHEMATICOMPONENTINSTANCES, Token.NONE, ReadComponentInstance);
                        document.SchematicComponentInstances.AddRange(schematicComponentInstances);
                        break;
                    case Token.KW_SCHEMATICNETS:
                        var schematicNets = ReadCollection(Token.KW_SCHEMATICNETS, Token.NONE, ReadNet);
                        document.SchematicNets.AddRange(schematicNets);
                        break;
                    case Token.KW_SCHEMATICDATA:
                        ReadSchematicData(document.SchematicData);
                        break;
                    case Token.KW_SHEETS:
                        var schematicSheets = ReadCollection(Token.KW_SHEETS, Token.NONE, ReadSchematicSheet);
                        document.SchematicSheets.AddRange(schematicSheets);
                        break;
                    case Token.KW_LAYERS:
                        var layers = ReadCollection(Token.KW_LAYERS, Token.NONE, ReadLayerNumber);
                        document.Layers.AddRange(layers);
                        break;

                    case Token.KW_TEXTSTYLE:
                        document.TextStyles.Add(ReadTextStyle());
                        break;
                    case Token.KW_PADSTACK_BEGIN:
                        document.PadStyles.Add(ReadPadStack());
                        break;
                    case Token.KW_PATTERN_BEGIN:
                        document.Footprints.Add(ReadPattern());
                        break;
                    case Token.KW_SYMBOL_BEGIN:
                        document.Symbols.Add(ReadSymbol());
                        break;
                    case Token.KW_COMPONENT_BEGIN:
                        document.Components.Add(ReadComponent());
                        break;

                    case Token.KW_EOF:
                        return; // early exit

                    // unsupported
                    case Token.KW_3DMODELS:
                    case Token.KW_SUPERCOMPONENTS:
                    case Token.KW_ATTACHEDFILES:
                    case Token.KW_LAYERTECHNICALDATA:
                        var unsupportedToken = Peek;
                        var count = ReadCount(Peek);
                        if (count > 0)
                        {
                            Warning($"Ignored unsupported {unsupportedToken} with {count} items");
                        }
                        SkipUntilNextSupported(false);
                        break;
                    default:
                        SkipUntilNextSupported(true);
                        break;
                }
            }
        }

        private Layer ReadLayer()
        {
            var layer = new Layer();
            layer.Id = ReadCount(Token.KW_LAYER);
            Expect(Token.KW_NAME);
            layer.Name = ReadIdentifier();
            if (Accept(Token.KW_LAYERTYPE))
            {
                var layerTypeName = ReadOptionalText();
                if (layerTypeName != null)
                {
                    if (Enum.TryParse<LayerType>(layerTypeName, true, out var layerType))
                    {
                        layer.LayerType = layerType;
                    }
                    else
                    {
                        Warning($"Unsupported {nameof(Layer.LayerType)} value: {layerTypeName}");
                    }
                }
            }
            if (Accept(Token.KW_BOARDLAYERTYPE))
            {
                layer.BoardLayerType = ReadOptionalText();
            }
            if (Accept(Token.KW_LAYERORDER))
            {
                layer.LayerOrder = ReadInteger();
            }
            return layer;
        }

        private TextStyle ReadTextStyle()
        {
            Expect(Token.KW_TEXTSTYLE);
            var textStyle = new TextStyle(ReadString());
            ReadPropertiesInto(textStyle);
            return textStyle;
        }

        private PadStack ReadPadStack()
        {
            Expect(Token.KW_PADSTACK_BEGIN);

            var padStack = new PadStack(ReadString());
            ReadPropertiesInto(padStack);

            // read Shapes collection
            var shapes = ReadCollection(Token.KW_SHAPES, Token.NONE, () =>
            {
                Expect(Token.KW_PADSHAPE);
                var padShape = new PadShape();
                var padShapeName = ReadString();
                if (Enum.TryParse<PadShapeKind>(padShapeName, true, out var padShapeKind))
                {
                    padShape.Kind = padShapeKind;
                }
                else
                {
                    Warning($"Unsupported {nameof(PadShape.Kind)} value: {padShapeName}");
                }
                ReadPropertiesInto(padShape);
                return padShape;
            });
            padStack.Shapes.AddRange(shapes);

            Expect(Token.KW_PADSTACK_END);
            return padStack;
        }

        /// <summary>
        /// Reads a simple item that is just a keyword and a sequence of properties.
        /// </summary>
        /// <typeparam name="T">Type of the item to be read.</typeparam>
        /// <param name="token">Keyword token for the item.</param>
        /// <returns></returns>
        private T ReadSimpleItem<T>(Token token) where T : new()
        {
            Expect(token);
            var item = new T();
            ReadPropertiesInto(item);
            return item;
        }

        private LibPin ReadLibPin()
        {
            var pin = ReadSimpleItem<LibPin>(Token.KW_PIN);

            Expect(Token.KW_PINDES);
            pin.Designator.Text = ReadString();
            ReadPropertiesInto(pin.Designator);

            Expect(Token.KW_PINNAME);
            pin.Name.Text = ReadString();
            ReadPropertiesInto(pin.Name);

            return pin;
        }

        /// <summary>
        /// Reads the items found in symbols and footprints data.
        /// </summary>
        /// <returns></returns>
        private LibItem ReadLibItem()
        {
            switch (Peek)
            {
                case Token.KW_PIN:
                    return ReadLibPin();
                case Token.KW_PAD:
                    return ReadSimpleItem<LibPad>(Token.KW_PAD);
                case Token.KW_DELETEDPAD:
                    return ReadSimpleItem<LibDeletedPad>(Token.KW_DELETEDPAD);
                case Token.KW_POLY:
                    return ReadSimpleItem<LibPoly>(Token.KW_POLY);
                case Token.KW_POLYKEEPOUT:
                    return ReadSimpleItem<LibPoly>(Token.KW_POLYKEEPOUT);
                case Token.KW_LINE:
                    return ReadSimpleItem<LibLine>(Token.KW_LINE);
                case Token.KW_ARC:
                    return ReadSimpleItem<LibArc>(Token.KW_ARC);
                case Token.KW_TEXT:
                    return ReadSimpleItem<LibText>(Token.KW_TEXT);
                case Token.KW_ATTRIBUTE:
                    return ReadSimpleItem<LibAttribute>(Token.KW_ATTRIBUTE);
                case Token.KW_WIZARD:
                    return ReadSimpleItem<LibWizard>(Token.KW_WIZARD);
                case Token.KW_TEMPLATEDATA:
                    return ReadSimpleItem<LibTemplateData>(Token.KW_TEMPLATEDATA);
                default:
                    // attempt to skip unknown item
                    var identifier = Expect(Token.IDENTIFIER);
                    Warning($"Unsupported library item: {identifier}");
                    IgnoreProperties();
                    break;
            }

            return null;
        }

        /// <summary>
        /// Reads a footprint, called "Pattern" in the data.
        /// </summary>
        /// <returns></returns>
        private Pattern ReadPattern()
        {
            Expect(Token.KW_PATTERN_BEGIN);

            var pattern = new Pattern(ReadString());
            while (Peek != Token.KW_DATA_BEGIN)
            {
                if (Accept(Token.KW_ORIGINPOINT))
                {
                    pattern.OriginPoint = ReadPoint();
                }
                else if (Accept(Token.KW_PICKPOINT))
                {
                    pattern.PickPoint = ReadPoint();
                }
                else if (Accept(Token.KW_GLUEPOINT))
                {
                    pattern.GluePoint = ReadPoint();
                }
                else if (Accept(Token.KW_PINSRENAMED))
                {
                    pattern.PinsRenamed = ReadBoolean();
                }
                else
                {
                    var identifier = Expect(Token.IDENTIFIER);
                    Warning($"{nameof(Pattern)} ignored property: {identifier}");
                    SkipUntil(true, Token.KW_DATA_BEGIN);
                }
            }

            // read Data collection
            pattern.Data.AddRange(ReadCollection(Token.KW_DATA_BEGIN, Token.KW_DATA_END, ReadLibItem));

            Expect(Token.KW_PATTERN_END);
            return pattern;
        }

        /// <summary>
        /// Reads a schematic symbol.
        /// </summary>
        /// <returns></returns>
        private Symbol ReadSymbol()
        {
            Expect(Token.KW_SYMBOL_BEGIN);

            var symbol = new Symbol(ReadString());
            while (Peek != Token.KW_DATA_BEGIN)
            {
                if (Accept(Token.KW_ORIGINPOINT))
                {
                    symbol.OriginPoint = ReadPoint();
                }
                else if (Accept(Token.KW_ORIGINALNAME))
                {
                    symbol.OriginalName = ReadString();
                }
                else if (Accept(Token.KW_EDITED))
                {
                    symbol.Edited = ReadBoolean();
                }
                else
                {
                    var identifier = Expect(Token.IDENTIFIER);
                    Warning($"{nameof(Symbol)} ignored property: {identifier}");
                    SkipUntil(true, Token.KW_DATA_BEGIN);
                }
            }

            // read Data collection
            symbol.Data.AddRange(ReadCollection(Token.KW_DATA_BEGIN, Token.KW_DATA_END, ReadLibItem));

            Expect(Token.KW_SYMBOL_END);
            return symbol;
        }

        /// <summary>
        /// Reads a library component.
        /// </summary>
        /// <returns></returns>
        private Component ReadComponent()
        {
            Expect(Token.KW_COMPONENT_BEGIN);

            var component = new Component(ReadString());
            while (Peek != Token.KW_COMPPINS_BEGIN)
            {
                if (Accept(Token.KW_PATTERNNAME))
                {
                    component.PatternName = ReadString();
                }
                else if (Accept(Token.KW_ALTERNATEPATTERN))
                {
                    component.AlternatePatterns.Add(ReadString());
                }
                else if (Accept(Token.KW_ORIGINALNAME))
                {
                    component.OriginalName = ReadString();
                }
                else if (Accept(Token.KW_SOURCELIBRARY))
                {
                    component.SourceLibrary = ReadString();
                }
                else if (Accept(Token.KW_REFDESPREFIX))
                {
                    component.RefDesPrefix = ReadString();
                }
                else if (Accept(Token.KW_NUMBEROFPINS))
                {
                    ReadInteger(); // ignored
                }
                else if (Accept(Token.KW_NUMPARTS))
                {
                    ReadInteger(); // ignored
                }
                else if (Accept(Token.KW_COMPOSITION))
                {
                    component.Composition = ReadIdentifier();
                }
                else if (Accept(Token.KW_ALTIEEE))
                {
                    component.AltIeee = ReadBoolean();
                }
                else if (Accept(Token.KW_ALTDEMORGAN))
                {
                    component.AltDeMorgan = ReadBoolean();
                }
                else if (Accept(Token.KW_PATTERNPINS))
                {
                    ReadInteger(); // ignored
                }
                else if (Accept(Token.KW_REVISIONLEVEL))
                {
                    component.RevisionLevel = ReadOptionalText();
                }
                else if (Accept(Token.KW_REVISIONNOTE))
                {
                    component.RevisionNote = ReadOptionalText();
                }
                else
                {
                    var identifier = Expect(Token.IDENTIFIER);
                    Warning($"{nameof(Component)} ignored property: {identifier}");
                    SkipUntil(true, Token.KW_COMPPINS_BEGIN);
                }
            }

            // read CompPins collection
            var compPins = ReadCollection(Token.KW_COMPPINS_BEGIN, Token.KW_COMPPINS_END, ReadCompPin);
            component.Pins.AddRange(compPins);

            // read CompData collection
            var compData = ReadCollection(Token.KW_COMPDATA_BEGIN, Token.KW_COMPDATA_END, ReadCompDataLibItem);
            component.Data.AddRange(compData);

            // read RelatedFiles collection manually because it repeats the count header for each item
            while (Peek == Token.KW_RELATEDFILES)
            {
                ReadCount(Token.KW_RELATEDFILES);
                component.RelatedFiles.Add(ReadRelatedFile());
            }

            // read AttachedSymbols collection
            var attachedSymbols = ReadCollection(Token.KW_ATTACHEDSYMBOLS_BEGIN, Token.KW_ATTACHEDSYMBOLS_END, ReadAttachedSymbol);
            component.AttachedSymbols.AddRange(attachedSymbols);

            // read PinMap collection
            var pinMap = ReadCollection(Token.KW_PINMAP_BEGIN, Token.KW_PINMAP_END, ReadPadNum);
            component.PinMap.AddRange(pinMap);

            Expect(Token.KW_COMPONENT_END);
            return component;
        }

        private CompPin ReadCompPin()
        {
            Expect(Token.KW_COMPPIN);
            var pinNum = Peek == Token.LIT_INTEGER ? $"{ReadInteger()}" : ReadIdentifier();
            var pinName = ReadString();
            var compPin = new CompPin(pinNum, pinName);
            ReadPropertiesInto(compPin);
            return compPin;
        }

        private LibItem ReadCompDataLibItem()
        {
            switch (Peek)
            {
                case Token.KW_ATTRIBUTE:
                    return ReadSimpleItem<LibAttribute>(Token.KW_ATTRIBUTE);
                case Token.KW_WIZARD:
                    return ReadSimpleItem<LibWizard>(Token.KW_WIZARD);
                default:
                    Expect(Token.NONE);
                    break;
            }
            return null;
        }

        private RelatedFile ReadRelatedFile()
        {
            var file = new RelatedFile();
            ReadPropertiesInto(file);
            return file;
        }

        private AttachedSymbol ReadAttachedSymbol()
        {
            return ReadSimpleItem<AttachedSymbol>(Token.KW_ATTACHEDSYMBOL);
        }

        private PadNum ReadPadNum()
        {
            Expect(Token.KW_PADNUM);
            var padNum = new PadNum(ReadInteger());
            ReadPropertiesInto(padNum);
            return padNum;
        }

        private void ReadWorkSpaceSize(Region workSpaceSize)
        {
            Expect(Token.KW_WORKSPACESIZE);
            ReadPropertiesInto(workSpaceSize);
        }

        private ComponentInstance ReadComponentInstance()
        {
            Expect(Token.KW_COMPONENT_BEGIN);
            var component = new ComponentInstance(ReadString());
            ReadPropertiesInto(component);
            while (Peek == Token.KW_ATTRIBUTE)
            {
                component.Attributes.Add(ReadSimpleItem<InstAttribute>(Token.KW_ATTRIBUTE));
            }
            return component;
        }

        private NetInstance ReadNet()
        {
            Expect(Token.KW_NET);
            var net = new NetInstance(ReadString());
            ReadPropertiesInto(net);
            return net;
        }

        private void ReadSchematicData(SchematicData schematicData)
        {
            // for some reason the header contains a colon followed by nothing
            Expect(Token.KW_SCHEMATICDATA);
            Expect(Token.COLON);
            Accept(Token.LIT_INTEGER); // try to see if there is an integer after the colon, just in case...

            Expect(Token.KW_UNITS);
            schematicData.Units = ReadString();
            Expect(Token.KW_WORKSPACE);
            ReadPropertiesInto(schematicData.Workspace);

            while (Accept(Token.KW_ATTRIBUTE))
            {
                var key = ReadString();
                Expect(Token.COMMA);
                var value = ReadString();
                schematicData.Attributes.Add(new KeyValuePair<string, string>(key, value));
            }

            while (Accept(Token.KW_SHEET))
            {
                Expect(Token.PAREN_L);
                Expect(Token.IDENTIFIER);
                var id = ReadInteger();
                Expect(Token.COMMA);
                var name = ReadString();
                Expect(Token.PAREN_R);
                schematicData.Sheets.Add(new KeyValuePair<int, string>(id, name));
            }
        }

        /// <summary>
        /// Reads an instance item.
        /// </summary>
        /// <returns></returns>
        private InstItem ReadInstItem()
        {
            switch (Peek)
            {
                case Token.KW_WIRE:
                    return ReadSimpleItem<InstWire>(Token.KW_WIRE);
                case Token.KW_PORT:
                    return ReadSimpleItem<InstPort>(Token.KW_PORT);
                case Token.KW_JUNCTION:
                    return ReadSimpleItem<InstJunction>(Token.KW_JUNCTION);
                case Token.KW_LINE:
                    return ReadSimpleItem<InstLine>(Token.KW_LINE);
                case Token.KW_ARC:
                    return ReadSimpleItem<InstArc>(Token.KW_ARC);
                case Token.KW_POLY:
                    return ReadSimpleItem<InstPoly>(Token.KW_POLY);
                case Token.KW_COPPERPOUR:
                    return ReadSimpleItem<InstCopperpour>(Token.KW_COPPERPOUR);
                case Token.KW_TEXT:
                    return ReadSimpleItem<InstText>(Token.KW_TEXT);
                case Token.KW_SYMBOL_BEGIN:
                    return ReadSimpleItem<InstSymbol>(Token.KW_SYMBOL_BEGIN);
                case Token.KW_ATTRIBUTE:
                    return ReadSimpleItem<InstAttribute>(Token.KW_ATTRIBUTE);
                default:
                    // attempt to skip unknown item
                    var identifier = Expect(Token.IDENTIFIER);
                    Warning($"Unsupported instance item: {identifier}");
                    IgnoreProperties();
                    break;
            }

            return null;
        }

        private Sheet ReadSchematicSheet()
        {
            ReadCount(Token.KW_SHEET);

            var sheet = new Sheet();
            while (Peek != Token.KW_DATA_BEGIN)
            {
                if (Accept(Token.KW_NAME))
                {
                    sheet.Name = ReadString();
                }
                else if (Accept(Token.KW_NUMBER))
                {
                    sheet.Number = ReadInteger();
                }
                else if (Accept(Token.KW_SHOWBORDER))
                {
                    sheet.ShowBorder = ReadString().Equals("True", StringComparison.InvariantCultureIgnoreCase);
                }
                else if (Accept(Token.KW_BORDERNAME))
                {
                    sheet.BorderName = ReadString();
                }
                else if (Accept(Token.KW_SCALEFACTOR))
                {
                    sheet.ScaleFactor = ReadDouble();
                }
                else if (Accept(Token.KW_OFFSET))
                {
                    sheet.OffSet = ReadPointCoords();
                }
                else
                {
                    var identifier = Expect(Token.IDENTIFIER);
                    Warning($"{nameof(Symbol)} ignored property: {identifier}");
                    SkipUntil(true, Token.KW_DATA_BEGIN);
                }
            }

            // read Data collection
            sheet.Data.AddRange(ReadCollection(Token.KW_DATA_BEGIN, Token.NONE, ReadInstItem));

            return sheet;
        }

        public LayerNumber ReadLayerNumber()
        {
            var layerNumber = new LayerNumber();
            layerNumber.Id = ReadCount(Token.KW_LAYERNUMBER);
            ReadPropertiesInto(layerNumber);

            // because the "LayerNumber" item doesn't have a count of its sub items we read until we find something that doesn't belong
            var itemTokens = new[] { Token.KW_WIRE, Token.KW_PORT, Token.KW_JUNCTION, Token.KW_LINE, Token.KW_ARC, Token.KW_POLY,
                Token.KW_COPPERPOUR, Token.KW_TEXT, Token.KW_SYMBOL_BEGIN, Token.KW_ATTRIBUTE };
            while (itemTokens.Contains(Peek))
            {
                layerNumber.Data.Add(ReadInstItem());
            }
            return layerNumber;
        }

        /// <summary>
        /// Reports progress during the parsing, usually only calling the progress report callback if more than 1% has
        /// been parsed since the last call.
        /// </summary>
        /// <param name="force">Forces calling the progress report callback.</param>
        private void ReportProgress(bool force = false)
        {
            if (_progress == null || (!force && _tokenizer.Position < _progressLast + _progressDelta)) return;

            var value = (_tokenizer.Position * 100) / _tokenizer.Text.Length;
            _progress.Report(value);
        }

        /// <summary>
        /// Formats a message for the <seealso cref="Logs"/>.
        /// </summary>
        private string FormatMessage(string message)
        {
            return $"{Path.GetFileName(_filename)}:{_tokenizer.Line}:{_tokenizer.Column} {message}";
        }

        /// <summary>
        /// Adds a new entry to the <seealso cref="Logs"/>.
        /// </summary>
        /// <param name="severity">Severity of the entry.</param>
        /// <param name="message">Message of the log entry.</param>
        private void Log(LogSeverity severity, string message)
        {
            Logs.Data.Add(new LogEntry(severity, FormatMessage(message)));
        }

        /// <summary>
        /// Adds a new information log entry.
        /// </summary>
        private void Information(string message)
        {
            Log(LogSeverity.Information, message);
        }

        /// <summary>
        /// Adds a new warning log entry.
        /// </summary>
        private void Warning(string message)
        {
            Log(LogSeverity.Warning, message);
        }

        /// <summary>
        /// Adds an error log entry and throws so the parsing is interrupted.
        /// </summary>
        private void Error(string message)
        {
            Log(LogSeverity.Error, message);
            throw new BxlParserException(message);
        }

        /// <summary>
        /// If the current <seealso cref="Peek"/> token is the same as <paramref name="token"/>
        /// then that token is consumed with <seealso cref="BxlTokenizer.Next"/>, and <c>true</c>
        /// is returned.
        /// <para>
        /// It's similar to <seealso cref="Expect"/> but for <b>optional tokens</b> and, as such, not "accepting" a token
        /// is a normal occurrence and that doesn't need to log any information or warning.
        /// </para>
        /// <para>
        /// This is useful for testing with conditionals for optional elements or branches in the grammar.
        /// </para>
        /// </summary>
        /// <param name="token">Token to be tested for acceptance.</param>
        /// <returns>Returns if the <paramref name="token"/> is the current one and it was consumed.</returns>
        /// <see cref="Accept{T}"/>
        /// <see cref="Expect"/>
        /// <see cref="Expect{T}"/>
        private bool Accept(Token token) =>
            Accept<object>(token, out _);

        /// <summary>
        /// <inheritdoc cref="Accept"/>
        /// <para>
        /// If the <paramref name="token"/> was accepted this also returns the attribute <paramref name="value"/>
        /// of the accepted token's lexeme.
        /// </para> 
        /// </summary>
        /// <typeparam name="T">Type of the expected value to be returned.</typeparam>
        /// <param name="token"><inheritdoc cref="Accept"/></param>
        /// <param name="value">Attribute value of the accepted token, or <c>default(<typeparamref name="T"/>)</c> otherwise</param>
        /// <returns><inheritdoc cref="Accept"/></returns>
        /// <see cref="Accept"/>
        /// <see cref="Expect"/>
        /// <see cref="Expect{T}"/>
        private bool Accept<T>(Token token, out T value)
        {
            if (Peek == token)
            {
                value = (T)_tokenizer.Value;
                _tokenizer.Next();
                ReportProgress();
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Asserts that the current <seealso cref="Peek"/> token must be the same as
        /// <paramref name="token"/> and then consumes it using <seealso cref="BxlTokenizer.Next"/>.<br/>
        /// Otherwise, it calls <seealso cref="Error"/> which logs the problem and interrupts the parsing process.
        /// <para>
        /// Similar to <seealso cref="Accept"/> but for a <b>required tokens</b> at the current position.
        /// </para>
        /// <para>
        /// Internally this works by calling <seealso cref="Accept"/> and producing an error if that failed.
        /// </para>
        /// </summary>
        /// <param name="token">
        /// Token that is expected to be the current one. This should be the same as the <seealso cref="Peek"/> value,
        /// that is, the last token read by the tokenizer.</param>
        /// <returns>
        /// Returns the attribute value of the token, and a placeholder no-value object if the current token
        /// doesn't produce values, like is the case with keywords.
        /// </returns>
        /// <see cref="Expect"/>
        /// <see cref="Accept"/>
        /// <see cref="Accept{T}"/>
        private object Expect(Token token) => Expect<object>(token);

        /// <summary>
        /// <inheritdoc cref="Expect"/>
        /// </summary>
        /// <typeparam name="T">Type of the expected attribute value.</typeparam>
        /// <param name="token"><inheritdoc cref="Expect"/></param>
        /// <returns><inheritdoc cref="Expect"/></returns>
        private T Expect<T>(Token token)
        {
            if (Accept(token, out T value))
            {
                return value;
            }
            else
            {
                Error($"Expected {token}, actual {Peek}.");
                return default;
            }
        }

        /// <summary>
        /// Skips tokens until one of the values in <paramref name="tokens"/> is found.
        /// <para>
        /// This is used for:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// error recovery scenarios, like skipping until finding the next thing that likely
        /// will makes sense after finding something unexpected;
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// for parts of the grammar that we assume nothing needs to be skipped, but as the BXL file format
        /// is not publicly available, we want to make what the next token actually is even if we face some
        /// unsupported input;
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// for parts of the file contents that we want to ignore, like unsupported parts of the grammar.
        /// </description>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="warnIgnoredData">If <c>true</c> a warning is logged when data is skipped.</param>
        /// <param name="tokens"></param>
        private void SkipUntil(bool warnIgnoredData, params Token[] tokens)
        {
            var startState = _tokenizer.State;

            while (!IsEof && !tokens.Contains(Peek))
            {
                _tokenizer.Next();
            }

            if (IsEof) Error("Expected " + string.Join(",", tokens));

            var endState = _tokenizer.State;
            var hasIgnoredData = endState.Position != startState.Position;
            if (warnIgnoredData && hasIgnoredData)
            {
                Warning($"Ignored from {startState.Line}:{startState.Column} to {endState.Line}:{endState.Column}");
            }
        }

        /// <summary>
        /// Expects a <seealso cref="Token.LIT_INTEGER"/> and returns its attribute value.
        /// </summary>
        private int ReadInteger()
        {
            return Expect<int>(Token.LIT_INTEGER);
        }

        /// <summary>
        /// Expects either a <seealso cref="Token.LIT_DECIMAL"/> or a <seealso cref="Token.LIT_DECIMAL"/> and returns its attribute value as double.
        /// </summary>
        private double ReadDouble()
        {
            if (Accept(Token.LIT_DECIMAL, out double doubleValue))
            {
                return doubleValue;
            }
            else
            {
                return Expect<int>(Token.LIT_INTEGER);
            }
        }

        /// <summary>
        /// Expects a <seealso cref="Token.LIT_BOOLEAN"/> and returns its attribute value.
        /// </summary>        
        private bool ReadBoolean()
        {
            return Expect<bool>(Token.LIT_BOOLEAN);
        }

        /// <summary>
        /// Expects a <seealso cref="Token.LIT_STRING"/> and returns its attribute value.
        /// </summary>
        private string ReadString()
        {
            return Expect<string>(Token.LIT_STRING);
        }

        /// <summary>
        /// Expects a <seealso cref="Token.IDENTIFIER"/> and returns its attribute value.
        /// </summary>
        private string ReadIdentifier()
        {
            return Expect<string>(Token.IDENTIFIER);
        }

        /// <summary>
        /// Accepts either a <seealso cref="Token.IDENTIFIER"/> or <seealso cref="Token.LIT_STRING"/> and,
        /// if accepted, returns its attribute value.
        /// </summary>
        private string ReadOptionalText()
        {
            if (Peek == Token.IDENTIFIER)
            {
                return ReadIdentifier();
            }
            else if (Peek == Token.LIT_STRING)
            {
                return ReadString();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Expects a pair of doubles separated by a <seealso cref="Token.COMMA"/> and creates a <seealso cref="Point"/> with them.
        /// </summary>
        /// <see cref="ReadPoint"/>
        /// <see cref="ReadDouble"/>
        private Point ReadPointCoords()
        {
            var x = ReadDouble();
            Expect(Token.COMMA);
            var y = ReadDouble();
            return new Point(x, y);
        }

        /// <summary>
        /// Expects an enclosed in parentheses pair of doubles separated by a <seealso cref="Token.COMMA"/>
        /// and creates a <seealso cref="Point"/> with them.
        /// </summary>
        /// <see cref="ReadPointCoords"/>
        private Point ReadPoint()
        {
            Expect(Token.PAREN_L);
            var point = ReadPointCoords();
            Expect(Token.PAREN_R);
            return point;
        }

        /// <summary>
        /// Reads the list of nodes in a net.
        /// </summary>
        /// <seealso cref="ReadNet"/>
        private List<NetNode> ReadNodes()
        {
            var nodes = new List<NetNode>();
            do
            {
                var item1 = Peek == Token.LIT_INTEGER ? $"{ReadInteger()}" : ReadIdentifier();
                Expect(Token.SLASH);
                var item2 = Peek == Token.LIT_INTEGER ? $"{ReadInteger()}" : ReadIdentifier();
                nodes.Add(new NetNode(item1, item2));
            }
            while (Accept(Token.COMMA));
            return nodes;
        }

        /// <summary>
        /// Iterates through a sequence properties in the form of <c>("propertyName" values...)</c> or <c>(double double)</c> from the input
        /// and calls either <paramref name="propertyHandler"/> or <paramref name="pointHandler"/> as needed for interpreting those
        /// values read.
        /// </summary>
        /// <param name="propertyHandler">Callback used for interpreting properties that have a name.</param>
        /// <param name="pointHandler">Callback used for interpreting points that do not have a property name.</param>
        private void ReadProperties(Func<string, bool> propertyHandler, Action<Point> pointHandler)
        {
            var seenProperties = new HashSet<string>();

            while (Accept(Token.PAREN_L))
            {
                var propertyName = "";
                var isDuplicateProperty = false;
                var startState = _tokenizer.State;
                if (Peek == Token.LIT_DECIMAL || Peek == Token.LIT_INTEGER)
                {
                    pointHandler(ReadPointCoords());
                }
                else
                {
                    propertyName = Expect<string>(Token.IDENTIFIER);
                    startState = _tokenizer.State; // update start state to not consider the identifier

                    var isCollection = propertyHandler(propertyName);
                    isDuplicateProperty = !isCollection && seenProperties.Contains(propertyName);
                    seenProperties.Add(propertyName);
                }
                var endState = _tokenizer.State;
                var hasReadData = (endState.Position - startState.Position) != 0;

                if (hasReadData && isDuplicateProperty)
                {
                    Information($"Duplicate value for {propertyName}");
                }

                if (Peek == Token.PAREN_L)
                {
                    // check if we have unmatched parentheses and try to fix the situation
                    Information("Missing closing paranthesis");
                    continue;
                }

                if (hasReadData && Peek != Token.PAREN_R)
                {
                    Warning("Excess or poorly formatted property value");
                    // This can happen in properties with embedded quote characters like:
                    //     (Attr "Description" "CONN RCPT .100" 50POS SNGL TIN")
                    //
                    // The parser's SkipUntil() may not be enough if the tokenizer gets into
                    // a weird state due a wild string with quote characters being present.
                    // Messing with the tokenizer state is one of the few options for recovery.
                    _tokenizer.SkipUntil(')');
                }

                SkipUntil(true, Token.PAREN_R);
                Expect(Token.PAREN_R);
            }
        }

        /// <summary>
        /// Used for parsing over the list of properties but not storing their value anywhere and
        /// emitting a warning for any property that has been passed over.
        /// </summary>
        private void IgnoreProperties()
        {
            ReadProperties(propertyName =>
            {
                Warning($"Ignored property: {propertyName}");
                return false;
            }, _ => Warning("Ignored coordinates"));
        }

        private static PropertyInfo GetProperty<T>(string propertyName) =>
                typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static void CollectionAdd<TInstance, TValue>(TInstance instance, PropertyInfo property, TValue value)
        {
            property.PropertyType.GetMethod("Add", new[] { typeof(TValue) })?
                .Invoke(property.GetValue(instance), new object[] { value });
        }

        private static void CollectionAddRange<TInstance, TValue>(TInstance instance, PropertyInfo property, IEnumerable<TValue> values)
        {
            property.PropertyType.GetMethod("AddRange", new[] { typeof(IEnumerable<TValue>) })?
                .Invoke(property.GetValue(instance), new object[] { values });
        }

        /// <summary>
        /// Reads from the input a property named <paramref name="propertyName"/> into the class properties
        /// of the <paramref name="instance"/> using reflection.
        /// </summary>
        /// <typeparam name="T"><inheritdoc cref="ReadPropertiesInto{T}"/></typeparam>
        /// <param name="instance"><inheritdoc cref="ReadPropertiesInto{T}"/></param>
        /// <param name="propertyName">Name of the property being read.</param>
        /// <returns>
        /// Returns <c>true</c> if the propery was read into a collection, this information
        /// is used by <seealso cref="ReadProperties"/> for detecting duplicate values.
        /// </returns>
        private bool ReadProperty<T>(T instance, string propertyName)
        {
            var isCollection = false;
            var property = GetProperty<T>(propertyName);
            if (property == null)
            {
                Warning($"{instance.GetType().Name} missing property: {propertyName}");
            }
            // first see if the class property exists as a collection because collection properties
            // don't need to be writable (CanWrite) in order to be modifiable
            else if (property.PropertyType.IsAssignableFrom(typeof(List<Point>))) 
            {
                isCollection = true;
                CollectionAdd(instance, property, ReadPointCoords());
            }
            else if (property.PropertyType.IsAssignableFrom(typeof(List<NetNode>)))
            {
                isCollection = true;
                CollectionAddRange(instance, property, ReadNodes());
            }
            else if (!property.CanWrite)
            {
                Warning($"{instance.GetType().Name} missing writable property: {propertyName}");
            }
            else if (property.PropertyType.IsAssignableFrom(typeof(double)))
            {
                property.SetValue(instance, ReadDouble());
            }
            else if (property.PropertyType.IsAssignableFrom(typeof(int)))
            {
                property.SetValue(instance, ReadInteger());
            }
            else if (property.PropertyType.IsAssignableFrom(typeof(bool)))
            {
                property.SetValue(instance, ReadBoolean());
            }
            else if (property.PropertyType.IsAssignableFrom(typeof(string)))
            {
                if (Peek == Token.IDENTIFIER)
                {
                    property.SetValue(instance, ReadIdentifier());
                }
                else
                {
                    property.SetValue(instance, ReadString());
                }
            }
            else if (property.PropertyType.IsAssignableFrom(typeof(Point)))
            {
                property.SetValue(instance, ReadPointCoords());
            }
            else if (property.PropertyType.IsAssignableFrom(typeof(KeyValuePair<string, string>)))
            {
                var key = ReadString();
                Accept(Token.COMMA); // the comma is not always present
                var value = ReadString();
                property.SetValue(instance, new KeyValuePair<string, string>(key, value));
            }
            else if (property.PropertyType.IsAssignableFrom(typeof(KeyValuePair<int, string>)))
            {
                var key = ReadInteger();
                Accept(Token.COMMA); // the comma is not always present
                var value = ReadString();
                property.SetValue(instance, new KeyValuePair<int, string>(key, value));
            }
            else if (property.PropertyType.IsEnum)
            {
                if (Peek == Token.LIT_INTEGER)
                {
                    property.SetValue(instance, Enum.ToObject(property.PropertyType, ReadInteger()));
                }
                else
                {
                    var valueName = Peek == Token.IDENTIFIER ? Regex.Replace(ReadIdentifier(), @"[-_\s]", "") : ReadString();
                    var isDefinedEnumName = Enum.GetNames(property.PropertyType).FirstOrDefault(name => name.Equals(valueName, StringComparison.InvariantCultureIgnoreCase)) != null;
                    if (isDefinedEnumName)
                    {
                        property.SetValue(instance, Enum.Parse(property.PropertyType, valueName, true));
                    }
                    else
                    {
                        Warning($"Unsupported {propertyName} value: {valueName}");
                    }
                }
            }
            else
            {
                Warning($"{instance.GetType().Name} has unsupported property: {propertyName}");
            }
            return isCollection;
        }

        /// <summary>
        /// Reads a sequence of <b>("propertyName" values...)</b> properties from the input and stores those values in the
        /// <paramref name="instance"/> object properties using reflection.
        /// </summary>
        /// <typeparam name="T">Type of the <paramref name="instance"/> object.</typeparam>
        /// <param name="instance">Object to have its properties assigned from the list of properties in the input.</param>
        /// <see cref="ReadProperties"/>
        /// <see cref="ReadProperty{T}"/>
        private void ReadPropertiesInto<T>(T instance)
        {
            if (instance == null) return;

            var points = new List<Point>();
            ReadProperties(propertyName => ReadProperty(instance, propertyName), points.Add);

            // do we have points?
            if (points.Count > 0)
            {
                // if it's only a single point try to assign it to the Origin property
                var property = GetProperty<T>("Origin");
                if (points.Count == 1 && property != null)
                {
                    property.SetValue(instance, points[0]);
                }
                else
                {
                    // if it's more than one (or the Origin property doesn't exist) try to add the points to a property
                    // named Points that is a collection which features an accessible AddRange(IEnumerable<T>) method.
                    property = GetProperty<T>("Points");
                    if (property != null)
                    {
                        CollectionAddRange(instance, property, points);
                    }
                    else
                    {
                        Warning($"{instance.GetType().Name} missing property: Origin or Points[{points.Count}]");
                    }
                }
            }
        }

        /// <summary>
        /// Reads a count in the format <c>(marker) : (integer)</c>
        /// </summary>
        private int ReadCount(Token marker)
        {
            Expect(marker);
            Expect(Token.COLON);
            return Expect<int>(Token.LIT_INTEGER);
        }

        /// <summary>
        /// Reads a collection of values that is prefixed by the number of items it contains,
        /// as read by <seealso cref="ReadCount"/> using the <paramref name="start"/> token as argument.
        /// <para>
        /// It also works when the collection has the wrong number of elements but an <paramref name="end"/>
        /// marker token is provided.
        /// </para>
        /// </summary>
        /// <typeparam name="T">Type of the items in the collection.</typeparam>
        /// <param name="start">
        /// Token that marks the start of the collection and is used when reading the expected number of items.
        /// </param>
        /// <param name="end">
        /// Optional token that marks the end of the collection for those collections that do have one. Otherwise,
        /// if can be <seealso cref="Token.NONE"/> and the count value will be used for determining when to stop
        /// reading elements.
        /// </param>
        /// <param name="itemReader">Callback for processing the read collection elements.</param>
        /// <returns></returns>
        private List<T> ReadCollection<T>(Token start, Token end, Func<T> itemReader)
        {
            var result = new List<T>();
            var count = ReadCount(start);
            if (end != Token.NONE)
            {
                while (Peek != end)
                {
                    result.Add(itemReader());
                }
                Expect(end);
                if (count != result.Count) Information($"Collection size mis-match, expected {count}, actual {result.Count}");
            }
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    result.Add(itemReader());
                }
            }
            return result;
        }
    }

    [Serializable]
    internal class BxlParserException : Exception
    {
        public BxlParserException(string message) : base(message)
        {
        }

        public BxlParserException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BxlParserException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
