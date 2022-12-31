using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OriginalCircuit.BxlSharp.Parsing;
using OriginalCircuit.BxlSharp.Types;

namespace OriginalCircuit.BxlSharp
{
    public enum BxlFileType
    {
        FromExtension,
        Binary,
        Text
    }

    /// <summary>
    /// Document containing parsed BXL data, including component libraries and board data, also called instances.
    /// <para>
    /// The component library data includes <seealso cref="Footprints"/>, <seealso cref="Symbols"/>, <seealso cref="Components"/>
    /// and the <seealso cref="LayerData"/>, <seealso cref="TextStyles"/> and <seealso cref="PadStyles"/> needed to describe them.
    /// This is the usual type of content found on most BXL files.
    /// </para>
    /// <para>
    /// The board data include schematics in <seealso cref="SchematicSheets"/>, PCB layout <seealso cref="Layers"/>,
    /// the net information and the component library data needed.
    /// </para>
    /// </summary>
    public class BxlDocument
    {
        #region Library Contents
        public List<Layer> LayerData { get; } = new List<Layer>();
        public List<TextStyle> TextStyles { get; } = new List<TextStyle>();
        public List<PadStack> PadStyles { get; } = new List<PadStack>();
        public List<Pattern> Footprints { get; } = new List<Pattern>();
        public List<Symbol> Symbols { get; } = new List<Symbol>();
        public List<Component> Components { get; } = new List<Component>();
        public Region WorkSpaceSize { get; set; } = new Region();
        public SchematicData SchematicData { get; } = new SchematicData();
        #endregion

        #region Instance/Board Contents
        public List<ComponentInstance> ComponentInstances { get; } = new List<ComponentInstance>();
        public List<ViaInstance> ViaInstances { get; } = new List<ViaInstance>();
        public List<NetInstance> Nets { get; } = new List<NetInstance>();
        public List<ComponentInstance> SchematicComponentInstances { get; } = new List<ComponentInstance>();
        public List<NetInstance> SchematicNets { get; } = new List<NetInstance>();
        public List<Sheet> SchematicSheets { get; } = new List<Sheet>();
        public List<LayerNumber> Layers { get; } = new List<LayerNumber>();
        #endregion

        /// <summary>
        /// Gets a text style entry by name.
        /// </summary>
        public TextStyle GetTextStyle(string name) =>
            TextStyles.FirstOrDefault(s => s.Name?.Equals(name, StringComparison.InvariantCultureIgnoreCase) == true);

        /// <summary>
        /// Gets a PCB pad stack entry by name.
        /// </summary>
        public PadStack GetPadStyle(string name) =>
            PadStyles.FirstOrDefault(s => s.Name?.Equals(name, StringComparison.InvariantCultureIgnoreCase) == true);

        /// <summary>
        /// Gets a PCB footprint by name.
        /// </summary>
        public Pattern GetFootprint(string name) =>
            Footprints.FirstOrDefault(f => f.Name?.Equals(name, StringComparison.InvariantCultureIgnoreCase) == true);

        /// <summary>
        /// Gets a schematic library symbol by name.
        /// </summary>
        public Symbol GetSymbol(string name) =>
            Symbols.FirstOrDefault(s => s.Name?.Equals(name, StringComparison.InvariantCultureIgnoreCase) == true);


        private static bool IsBinary(string fileName, BxlFileType fileType = BxlFileType.FromExtension)
        {
            // assume possible binary BXL if the extension is DIFFERENT from .XLR
            return fileType == BxlFileType.Binary ||
                (fileType == BxlFileType.FromExtension && Path.GetExtension(fileName) != ".xlr");
        }

        /// <summary>
        /// Returns a BxlDocument from parsing a BXL file.
        /// </summary>
        /// <param name="fileName">File to be read.</param>
        /// <param name="fileType">Indicates the file type.</param>
        /// <param name="logs">Information gathered during the parsing process, including errors.</param>
        /// <param name="progress">Allows reporting progress for large files.</param>
        public static BxlDocument ReadFromFile(string fileName, BxlFileType fileType, out Logs logs, IProgress<int> progress = null)
        {
            var text = DecodeFile(fileName, fileType);
            return ReadFromText(text, fileName, out logs, progress);
        }

        /// <summary>
        /// Returns a BxlDocument from parsing a BXL file.
        /// </summary>
        /// <param name="fileName">File to be read.</param>
        /// <param name="fileType">Indicates the file type.</param>
        /// <param name="progress">Allows reporting progress for large files.</param>
        public static BxlDocument ReadFromFile(string fileName, BxlFileType fileType = BxlFileType.FromExtension, IProgress<int> progress = null)
        {
            return ReadFromFile(fileName, fileType, out _, progress);
        }

        /// <summary>
        /// Returns a BxlDocument from parsing a BXL file.
        /// </summary>
        /// <param name="fileName">File to be read.</param>
        /// <param name="fileType">Indicates the file type.</param>
        /// <param name="progress">Allows reporting progress for large files.</param>
        public static async Task<BxlDocument> ReadFromFileAsync(string fileName, BxlFileType fileType = BxlFileType.FromExtension, IProgress<int> progress = null)
        {
            var text = await DecodeFileAsync(fileName, fileType);
            return ReadFromText(text, fileName, progress);
        }

        /// <summary>
        /// Returns a BxlDocument from parsing the text representation of a BXL file that has been already decoded. 
        /// </summary>
        /// <param name="text">Text representation of the BXL contents.</param>
        /// <param name="referenceFileName">Used for error reporting purposes.</param>
        /// <param name="logs">Information gathered during the parsing process, including errors.</param>
        /// <param name="progress">Allows reporting progress for large files.</param>
        public static BxlDocument ReadFromText(string text, string referenceFileName, out Logs logs, IProgress<int> progress = null)
        {
            var parser = new BxlParser(text, referenceFileName);
            var document = parser.Execute(progress);
            logs = new Logs(parser.Logs);
            return document;
        }

        /// <summary>
        /// Returns a BxlDocument from parsing the text representation of a BXL file that has been already decoded. 
        /// </summary>
        /// <param name="text">Text representation of the BXL contents.</param>
        /// <param name="referenceFileName">Used for error reporting purposes.</param>
        /// <param name="progress">Allows reporting progress for large files.</param>
        public static BxlDocument ReadFromText(string text, string referenceFileName = "", IProgress<int> progress = null)
        {
            return ReadFromText(text, referenceFileName, out _, progress);
        }

        /// <summary>
        /// Decodes BXL's adaptive huffman encoded data from file and returns its text representation.
        /// </summary>
        public static string DecodeFile(string fileName, BxlFileType fileType = BxlFileType.FromExtension)
        {
            if (IsBinary(fileName, fileType))
            {
                return DecodeBxl(File.ReadAllBytes(fileName));
            }
            else
            {
                return File.ReadAllText(fileName);
            }
        }

        /// <summary>
        /// Decodes BXL's adaptive huffman encoded data from file and returns its text representation.
        /// </summary>
        public static async Task<string> DecodeFileAsync(string fileName, BxlFileType fileType = BxlFileType.FromExtension)
        {
            if (IsBinary(fileName, fileType))
            {
                return DecodeBxl(await File.ReadAllBytesAsync(fileName));
            }
            else
            {
                return await File.ReadAllTextAsync(fileName);
            }
        }

        /// <summary>
        /// Decodes BXL's adaptive huffman encoded data and returns its text representation.
        /// </summary>
        public static string DecodeBxl(byte[] data)
        {
            return AdaptiveHuffman.Decode(data);
        }
    }
}
