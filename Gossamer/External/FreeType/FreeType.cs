using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

namespace Gossamer.External.FreeType;

[StructLayout(LayoutKind.Sequential)]
internal struct MMAxisRec
{
    [MarshalAs(UnmanagedType.LPStr)]
    internal string name;

    internal int minimum;
    internal int maximum;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MultiMasterRec
{
    internal uint num_axis;
    internal uint num_designs;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    internal MMAxisRec[] axis;
}

[StructLayout(LayoutKind.Sequential)]
internal struct VarNamedStyleRec
{
    internal IntPtr coords;
    internal uint strid;
}

[StructLayout(LayoutKind.Sequential)]
internal struct VarAxisRec
{
    [MarshalAs(UnmanagedType.LPStr)]
    internal string name;

    internal int minimum;
    internal int def;
    internal int maximum;

    internal uint tag;
    internal uint strid;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MMVar
{
    internal uint num_axis;
    internal uint num_designs;
    internal uint num_namedstyles;
    internal nint axis;
    internal nint namedstyle;
}

/// <summary>
/// A list of bit flags used in the ‘face_flags’ field of the <see cref="Face"/> structure. They inform client
/// applications of properties of the corresponding face.
/// </summary>
[Flags]
public enum FaceFlags : long
{
    /// <summary>
    /// No style flags.
    /// </summary>
    None = 0x0000,

    /// <summary>
    /// Indicates that the face contains outline glyphs. This doesn't prevent bitmap strikes, i.e., a face can have
    /// both this and and <see cref="FaceFlags.FixedSizes"/> set.
    /// </summary>
    Scalable = 0x0001,

    /// <summary>
    /// Indicates that the face contains bitmap strikes. See also <see cref="Face.FixedSizesCount"/> and
    /// <see cref="Face.AvailableSizes"/>.
    /// </summary>
    FixedSizes = 0x0002,

    /// <summary>
    /// Indicates that the face contains fixed-width characters (like Courier, Lucido, MonoType, etc.).
    /// </summary>
    FixedWidth = 0x0004,

    /// <summary>
    /// Indicates that the face uses the ‘sfnt’ storage scheme. For now, this means TrueType and OpenType.
    /// </summary>
    Sfnt = 0x0008,

    /// <summary>
    /// Indicates that the face contains horizontal glyph metrics. This should be set for all common formats.
    /// </summary>
    Horizontal = 0x0010,

    /// <summary>
    /// Indicates that the face contains vertical glyph metrics. This is only available in some formats, not all of
    /// them.
    /// </summary>
    Vertical = 0x0020,

    /// <summary>
    /// Indicates that the face contains kerning information. If set, the kerning distance can be retrieved through
    /// the function <see cref="Face.GetKerning"/>. Otherwise the function always return the vector (0,0). Note
    /// that FreeType doesn't handle kerning data from the ‘GPOS’ table (as present in some OpenType fonts).
    /// </summary>
    Kerning = 0x0040,

    /// <summary>
    /// THIS FLAG IS DEPRECATED. DO NOT USE OR TEST IT.
    /// </summary>
    [Obsolete("THIS FLAG IS DEPRECATED. DO NOT USE OR TEST IT.")]
    FastGlyphs = 0x0080,

    /// <summary>
    /// Indicates that the font contains multiple masters and is capable of interpolating between them. See the
    /// multiple-masters specific API for details.
    /// </summary>
    MultipleMasters = 0x0100,

    /// <summary>
    /// Indicates that the font contains glyph names that can be retrieved through
    /// <see cref="Face.GetGlyphName(uint, int)"/>. Note that some TrueType fonts contain broken glyph name
    /// tables. Use the function <see cref="Face.HasPSGlyphNames"/> when needed.
    /// </summary>
    GlyphNames = 0x0200,

    /// <summary>
    /// Used internally by FreeType to indicate that a face's stream was provided by the client application and
    /// should not be destroyed when <see cref="Face.Dispose()"/> is called. Don't read or test this flag.
    /// </summary>
    ExternalStream = 0x0400,

    /// <summary>
    /// Set if the font driver has a hinting machine of its own. For example, with TrueType fonts, it makes sense
    /// to use data from the SFNT ‘gasp’ table only if the native TrueType hinting engine (with the bytecode
    /// interpreter) is available and active.
    /// </summary>
    Hinter = 0x0800,

    /// <summary><para>
    /// Set if the font is CID-keyed. In that case, the font is not accessed by glyph indices but by CID values.
    /// For subsetted CID-keyed fonts this has the consequence that not all index values are a valid argument to
    /// <see cref="Face.LoadGlyph"/>. Only the CID values for which corresponding glyphs in the subsetted font
    /// exist make <see cref="Face.LoadGlyph"/> return successfully; in all other cases you get an
    /// <see cref="Error.InvalidArgument"/> error.
    /// </para><para>
    /// Note that CID-keyed fonts which are in an SFNT wrapper don't have this flag set since the glyphs are
    /// accessed in the normal way (using contiguous indices); the ‘CID-ness’ isn't visible to the application.
    /// </para></summary>
    CidKeyed = 0x1000,

    /// <summary><para>
    /// Set if the font is ‘tricky’, this is, it always needs the font format's native hinting engine to get a
    /// reasonable result. A typical example is the Chinese font ‘mingli.ttf’ which uses TrueType bytecode
    /// instructions to move and scale all of its subglyphs.
    /// </para><para>
    /// It is not possible to autohint such fonts using <see cref="LoadFlags.ForceAutohint"/>; it will also ignore
    /// <see cref="LoadFlags.NoHinting"/>. You have to set both <see cref="LoadFlags.NoHinting"/> and
    /// <see cref="LoadFlags.ForceAutohint"/> to really disable hinting; however, you probably never want this
    /// except for demonstration purposes.
    /// </para><para>
    /// Currently, there are about a dozen TrueType fonts in the list of tricky fonts; they are hard-coded in file
    /// ‘ttobjs.c’.
    /// </para></summary>
    Tricky = 0x2000,

    /// <summary>
    /// Set if the font has color glyph tables. To access color glyphs use <see cref="LoadFlags.Color"/>.
    /// </summary>
    Color = 0x4000,
}

[StructLayout(LayoutKind.Sequential)]
public struct GlyphInfo
{
    public uint codepoint;
    public uint mask;
    public uint cluster;

    public uint var1;
    public uint var2;
}

[StructLayout(LayoutKind.Sequential)]
public struct GlyphPosition
{
    public int xAdvance;
    public int yAdvance;
    public int xOffset;
    public int yOffset;

    public uint var;
}

/// <summary>
/// A list of bit-field constants use for the flags in an outline's ‘flags’ field.
/// </summary>
/// <remarks><para>
/// The flags <see cref="OutlineFlags.IgnoreDropouts"/>, <see cref="OutlineFlags.SmartDropouts"/>, and
/// <see cref="OutlineFlags.IncludeStubs"/> are ignored by the smooth rasterizer.
/// </para><para>
/// There exists a second mechanism to pass the drop-out mode to the B/W rasterizer; see the ‘tags’ field in
/// <see cref="Outline"/>.
/// </para><para>
/// Please refer to the description of the ‘SCANTYPE’ instruction in the OpenType specification (in file
/// ‘ttinst1.doc’) how simple drop-outs, smart drop-outs, and stubs are defined.
/// </para></remarks>
[Flags]
public enum OutlineFlags
{
    /// <summary>
    /// Value 0 is reserved.
    /// </summary>
    None = 0x0000,

    /// <summary>
    /// If set, this flag indicates that the outline's field arrays (i.e., ‘points’, ‘flags’, and ‘contours’) are
    /// ‘owned’ by the outline object, and should thus be freed when it is destroyed.
    /// </summary>
    Owner = 0x0001,

    /// <summary>
    /// By default, outlines are filled using the non-zero winding rule. If set to 1, the outline will be filled
    /// using the even-odd fill rule (only works with the smooth rasterizer).
    /// </summary>
    EvenOddFill = 0x0002,

    /// <summary>
    /// By default, outside contours of an outline are oriented in clock-wise direction, as defined in the TrueType
    /// specification. This flag is set if the outline uses the opposite direction (typically for Type 1 fonts).
    /// This flag is ignored by the scan converter.
    /// </summary>
    ReverseFill = 0x0004,

    /// <summary>
    /// By default, the scan converter will try to detect drop-outs in an outline and correct the glyph bitmap to
    /// ensure consistent shape continuity. If set, this flag hints the scan-line converter to ignore such cases.
    /// See below for more information.
    /// </summary>
    IgnoreDropouts = 0x0008,

    /// <summary>
    /// Select smart dropout control. If unset, use simple dropout control. Ignored if
    /// <see cref="OutlineFlags.IgnoreDropouts"/> is set. See below for more information.
    /// </summary>
    SmartDropouts = 0x0010,

    /// <summary>
    /// If set, turn pixels on for ‘stubs’, otherwise exclude them. Ignored if
    /// <see cref="OutlineFlags.IgnoreDropouts"/> is set. See below for more information.
    /// </summary>
    IncludeStubs = 0x0020,

    /// <summary>
    /// This flag indicates that the scan-line converter should try to convert this outline to bitmaps with the
    /// highest possible quality. It is typically set for small character sizes. Note that this is only a hint that
    /// might be completely ignored by a given scan-converter.
    /// </summary>
    HighPrecision = 0x0100,

    /// <summary>
    /// This flag is set to force a given scan-converter to only use a single pass over the outline to render a
    /// bitmap glyph image. Normally, it is set for very large character sizes. It is only a hint that might be
    /// completely ignored by a given scan-converter.
    /// </summary>
    SinglePass = 0x0200
}

/// <summary>
/// A structure used to hold an outline's bounding box, i.e., the
/// coordinates of its extrema in the horizontal and vertical directions.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct BBox
{
    public readonly int xMin, yMin;
    public readonly int xMax, yMax;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FTVector26Dot6
{
    public readonly int x;
    public readonly int y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct OutlineRec
{
    internal short n_contours;
    internal short n_points;

    internal nint points;
    internal nint tags;
    internal nint contours;

    internal OutlineFlags flags;
}

/// <summary>
/// An enumeration type used to describe the format of pixels in a given bitmap. Note that additional formats may
/// be added in the future.
/// </summary>
public enum PixelMode : byte
{
    /// <summary>
    /// Value 0 is reserved.
    /// </summary>
    None = 0,

    /// <summary>
    /// A monochrome bitmap, using 1 bit per pixel. Note that pixels are stored in most-significant order (MSB),
    /// which means that the left-most pixel in a byte has value 128.
    /// </summary>
    Mono,

    /// <summary>
    /// An 8-bit bitmap, generally used to represent anti-aliased glyph images. Each pixel is stored in one byte.
    /// Note that the number of ‘gray’ levels is stored in the ‘num_grays’ field of the <see cref="FTBitmap"/>
    /// structure (it generally is 256).
    /// </summary>
    Gray,

    /// <summary>
    /// A 2-bit per pixel bitmap, used to represent embedded anti-aliased bitmaps in font files according to the
    /// OpenType specification. We haven't found a single font using this format, however.
    /// </summary>
    Gray2,

    /// <summary>
    /// A 4-bit per pixel bitmap, representing embedded anti-aliased bitmaps in font files according to the
    /// OpenType specification. We haven't found a single font using this format, however.
    /// </summary>
    Gray4,

    /// <summary>
    /// An 8-bit bitmap, representing RGB or BGR decimated glyph images used for display on LCD displays; the
    /// bitmap is three times wider than the original glyph image. See also <see cref="FtRenderMode.LCD"/>.
    /// </summary>
    Lcd,

    /// <summary>
    /// An 8-bit bitmap, representing RGB or BGR decimated glyph images used for display on rotated LCD displays;
    /// the bitmap is three times taller than the original glyph image. See also
    /// <see cref="FtRenderMode.VerticalLcd"/>.
    /// </summary>
    VerticalLcd,

    /// <summary>
    /// An image with four 8-bit channels per pixel, representing a color image (such as emoticons) with alpha
    /// channel. For each pixel, the format is BGRA, which means, the blue channel comes first in memory. The color
    /// channels are pre-multiplied and in the sRGB colorspace. For example, full red at half-translucent opacity
    /// will be represented as ‘00,00,80,80’, not ‘00,00,FF,80’.
    /// </summary>
    /// <seealso cref="LoadFlags.Color"/>
    Bgra
}

[StructLayout(LayoutKind.Sequential)]
internal struct BitmapRec
{
    internal int rows;
    internal int width;
    internal int pitch;
    internal nint buffer;
    internal short num_grays;
    internal PixelMode pixel_mode;
    internal byte palette_mode;
    internal nint palette;
}

/// <summary>
/// An enumeration type used to describe the format of a given glyph image. Note that this version of FreeType only
/// supports two image formats, even though future font drivers will be able to register their own format.
/// </summary>
public enum GlyphFormat : uint
{
    /// <summary>
    /// The value 0 is reserved.
    /// </summary>
    None = 0,

    /// <summary>
    /// The glyph image is a composite of several other images. This format is only used with
    /// <see cref="LoadFlags.NoRecurse"/>, and is used to report compound glyphs (like accented characters).
    /// </summary>
    Composite = 'c' << 24 | 'o' << 16 | 'm' << 8 | 'p',

    /// <summary>
    /// The glyph image is a bitmap, and can be described as an <see cref="FTBitmap"/>. You generally need to
    /// access the ‘bitmap’ field of the <see cref="GlyphSlot"/> structure to read it.
    /// </summary>
    Bitmap = 'b' << 24 | 'i' << 16 | 't' << 8 | 's',

    /// <summary>
    /// The glyph image is a vectorial outline made of line segments and Bézier arcs; it can be described as an
    /// <see cref="Outline"/>; you generally want to access the ‘outline’ field of the <see cref="GlyphSlot"/>
    /// structure to read it.
    /// </summary>
    Outline = 'o' << 24 | 'u' << 16 | 't' << 8 | 'l',

    /// <summary>
    /// The glyph image is a vectorial path with no inside and outside contours. Some Type 1 fonts, like those in
    /// the Hershey family, contain glyphs in this format. These are described as <see cref="Outline"/>, but
    /// FreeType isn't currently capable of rendering them correctly.
    /// </summary>
    Plotter = 'p' << 24 | 'l' << 16 | 'o' << 8 | 't'
}

[StructLayout(LayoutKind.Sequential)]
internal struct GlyphMetricsRec
{
    internal uint width;
    internal uint height;

    internal uint horiBearingX;
    internal uint horiBearingY;
    internal uint horiAdvance;

    internal uint vertBearingX;
    internal uint vertBearingY;
    internal uint vertAdvance;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GlyphSlotRec
{
    internal nint library;
    internal nint face;
    internal nint next;
    internal uint glyph_index;
    internal GenericRec generic;

    internal GlyphMetricsRec metrics;
    internal uint linearHoriAdvance;
    internal uint linearVertAdvance;
    internal FTVector26Dot6 advance;

    internal GlyphFormat format;

    internal BitmapRec bitmap;
    internal int bitmap_left;
    internal int bitmap_top;

    internal OutlineRec outline;

    internal uint num_subglyphs;
    internal nint subglyphs;

    internal nint control_data;
    internal uint control_len;

    internal uint lsb_delta;
    internal uint rsb_delta;

    internal nint other;

    private readonly nint @internal;
}

/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct FaceRec
{
    internal int num_faces;
    internal int face_index;

    internal int face_flags;
    internal int style_flags;

    internal int num_glyphs;

    internal nint family_name;
    internal nint style_name;

    internal int num_fixed_sizes;
    internal nint available_sizes;

    internal int num_charmaps;
    internal nint charmaps;

    internal GenericRec generic;

    internal BBox bbox;

    internal ushort units_per_EM;
    internal short ascender;
    internal short descender;
    internal short height;

    internal short max_advance_width;
    internal short max_advance_height;

    internal short underline_position;
    internal short underline_thickness;

    internal nint glyph;
    internal nint size;
    internal nint charmap;

    private readonly nint driver;
    private readonly nint memory;
    private readonly nint stream;

    private readonly nint sizes_list;
    private GenericRec autohint;
    private readonly nint extensions;

    private readonly nint @internal;

    internal static int SizeInBytes { get { return Marshal.SizeOf(typeof(FaceRec)); } }
}

[StructLayout(LayoutKind.Sequential)]
internal struct GenericRec
{
    internal nint data;
    internal nint finalizer;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SizeMetricsRec
{
    internal ushort x_ppem;
    internal ushort y_ppem;

    internal int x_scale;
    internal int y_scale;
    internal int ascender;
    internal int descender;
    internal int height;
    internal int max_advance;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SizeRec
{
    internal nint face;
    internal GenericRec generic;
    internal SizeMetricsRec metrics;
    private readonly nint @internal;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GlyphRec
{
    internal nint library;
    internal nint clazz;
    internal uint format;
    internal FTVector26Dot6 advance;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BitmapGlyphRec
{
    internal GlyphRec root;
    internal int left;
    internal int top;
    internal BitmapRec bitmap;
}

public struct ColorRgba8(byte R, byte G, byte B, byte A)
{
    public byte R = R;
    public byte G = G;
    public byte B = B;
    public byte A = A;
}

[Flags]
public enum FT_Load : int
{
    DEFAULT = 0x0,
    NO_SCALE = 1 << 0,
    NO_HINTING = 1 << 1,
    RENDER = 1 << 2,
    NO_BITMAP = 1 << 3,
    VERTICAL_LAYOUT = 1 << 4,
    FORCE_AUTOHINT = 1 << 5,
    CROP_BITMAP = 1 << 6,
    PEDANTIC = 1 << 7,
    IGNORE_GLOBAL_ADVANCE_WIDTH = 1 << 9,
    NO_RECURSE = 1 << 10,
    IGNORE_TRANSFORM = 1 << 11,
    MONOCHROME = 1 << 12,
    LINEAR_DESIGN = 1 << 13,
    NO_AUTOHINT = 1 << 15,

    LOAD_TARGET_LIGHT = 1 << 16,
    LOAD_TARGET_MONO = 1 << 17,
    LOAD_TARGET_LCD = 1 << 18,
    LOAD_TARGET_LCD_V = 1 << 19,

    COLOR = 1 << 20,
    COMPUTE_METRICS = 1 << 21,
    BITMAP_METRICS_ONLY = 1 << 22,
    NO_SVG = 1 << 24,
}

public enum FT_Error
{
    /// <summary>No error.</summary>
    Ok = 0x00,

    /// <summary>Cannot open resource.</summary>
    CannotOpenResource = 0x01,

    /// <summary>Unknown file format.</summary>
    UnknownFileFormat = 0x02,

    /// <summary>Broken file.</summary>
    InvalidFileFormat = 0x03,

    /// <summary>Invalid FreeType version.</summary>
    InvalidVersion = 0x04,

    /// <summary>Module version is too low.</summary>
    LowerModuleVersion = 0x05,

    /// <summary>Invalid argument.</summary>
    InvalidArgument = 0x06,

    /// <summary>Unimplemented feature.</summary>
    UnimplementedFeature = 0x07,

    /// <summary>Broken table.</summary>
    InvalidTable = 0x08,

    /// <summary>Broken offset within table.</summary>
    InvalidOffset = 0x09,

    /// <summary>Array allocation size too large.</summary>
    ArrayTooLarge = 0x0A,

    /// <summary>Invalid glyph index.</summary>
    InvalidGlyphIndex = 0x10,

    /// <summary>Invalid character code.</summary>
    InvalidCharacterCode = 0x11,

    /// <summary>Unsupported glyph image format.</summary>
    InvalidGlyphFormat = 0x12,

    /// <summary>Cannot render this glyph format.</summary>
    CannotRenderGlyph = 0x13,

    /// <summary>Invalid outline.</summary>
    InvalidOutline = 0x14,

    /// <summary>Invalid composite glyph.</summary>
    InvalidComposite = 0x15,

    /// <summary>Too many hints.</summary>
    TooManyHints = 0x16,

    /// <summary>Invalid pixel size.</summary>
    InvalidPixelSize = 0x17,

    /// <summary>Invalid object handle.</summary>
    InvalidHandle = 0x20,

    /// <summary>Invalid library handle.</summary>
    InvalidLibraryHandle = 0x21,

    /// <summary>Invalid module handle.</summary>
    InvalidDriverHandle = 0x22,

    /// <summary>Invalid face handle.</summary>
    InvalidFaceHandle = 0x23,

    /// <summary>Invalid size handle.</summary>
    InvalidSizeHandle = 0x24,

    /// <summary>Invalid glyph slot handle.</summary>
    InvalidSlotHandle = 0x25,

    /// <summary>Invalid charmap handle.</summary>
    InvalidCharMapHandle = 0x26,

    /// <summary>Invalid cache manager handle.</summary>
    InvalidCacheHandle = 0x27,

    /// <summary>Invalid stream handle.</summary>
    InvalidStreamHandle = 0x28,

    /// <summary>Too many modules.</summary>
    TooManyDrivers = 0x30,

    /// <summary>Too many extensions.</summary>
    TooManyExtensions = 0x31,

    /// <summary>Out of memory.</summary>
    OutOfMemory = 0x40,

    /// <summary>Unlisted object.</summary>
    UnlistedObject = 0x41,

    /// <summary>Cannot open stream.</summary>
    CannotOpenStream = 0x51,

    /// <summary>Invalid stream seek.</summary>
    InvalidStreamSeek = 0x52,

    /// <summary>Invalid stream skip.</summary>
    InvalidStreamSkip = 0x53,

    /// <summary>Invalid stream read.</summary>
    InvalidStreamRead = 0x54,

    /// <summary>Invalid stream operation.</summary>
    InvalidStreamOperation = 0x55,

    /// <summary>Invalid frame operation.</summary>
    InvalidFrameOperation = 0x56,

    /// <summary>Nested frame access.</summary>
    NestedFrameAccess = 0x57,

    /// <summary>Invalid frame read.</summary>
    InvalidFrameRead = 0x58,

    /// <summary>Raster uninitialized.</summary>
    RasterUninitialized = 0x60,

    /// <summary>Raster corrupted.</summary>
    RasterCorrupted = 0x61,

    /// <summary>Raster overflow.</summary>
    RasterOverflow = 0x62,

    /// <summary>Negative height while rastering.</summary>
    RasterNegativeHeight = 0x63,

    /// <summary>Too many registered caches.</summary>
    TooManyCaches = 0x70,

    /// <summary>Invalid opcode.</summary>
    InvalidOpCode = 0x80,

    /// <summary>Too few arguments.</summary>
    TooFewArguments = 0x81,

    /// <summary>Stack overflow.</summary>
    StackOverflow = 0x82,

    /// <summary>Code overflow.</summary>
    CodeOverflow = 0x83,

    /// <summary>Bad argument.</summary>
    BadArgument = 0x84,

    /// <summary>Division by zero.</summary>
    DivideByZero = 0x85,

    /// <summary>Invalid reference.</summary>
    InvalidReference = 0x86,

    /// <summary>Found debug opcode.</summary>
    DebugOpCode = 0x87,

    /// <summary>Found ENDF opcode in execution stream.</summary>
    EndfInExecStream = 0x88,

    /// <summary>Nested DEFS.</summary>
    NestedDefs = 0x89,

    /// <summary>Invalid code range.</summary>
    InvalidCodeRange = 0x8A,

    /// <summary>Execution context too long.</summary>
    ExecutionTooLong = 0x8B,

    /// <summary>Too many function definitions.</summary>
    TooManyFunctionDefs = 0x8C,

    /// <summary>Too many instruction definitions.</summary>
    TooManyInstructionDefs = 0x8D,

    /// <summary>SFNT font table missing.</summary>
    TableMissing = 0x8E,

    /// <summary>Horizontal header (hhea) table missing.</summary>
    HorizHeaderMissing = 0x8F,

    /// <summary>Locations (loca) table missing.</summary>
    LocationsMissing = 0x90,

    /// <summary>Name table missing.</summary>
    NameTableMissing = 0x91,

    /// <summary>Character map (cmap) table missing.</summary>
    CMapTableMissing = 0x92,

    /// <summary>Horizontal metrics (hmtx) table missing.</summary>
    HmtxTableMissing = 0x93,

    /// <summary>PostScript (post) table missing.</summary>
    PostTableMissing = 0x94,

    /// <summary>Invalid horizontal metrics.</summary>
    InvalidHorizMetrics = 0x95,

    /// <summary>Invalid character map (cmap) format.</summary>
    InvalidCharMapFormat = 0x96,

    /// <summary>Invalid ppem value.</summary>
    InvalidPPem = 0x97,

    /// <summary>Invalid vertical metrics.</summary>
    InvalidVertMetrics = 0x98,

    /// <summary>Could not find context.</summary>
    CouldNotFindContext = 0x99,

    /// <summary>Invalid PostScript (post) table format.</summary>
    InvalidPostTableFormat = 0x9A,

    /// <summary>Invalid PostScript (post) table.</summary>
    InvalidPostTable = 0x9B,

    /// <summary>Opcode syntax error.</summary>
    SyntaxError = 0xA0,

    /// <summary>Argument stack underflow.</summary>
    StackUnderflow = 0xA1,

    /// <summary>Ignore this error.</summary>
    Ignore = 0xA2,

    /// <summary>No Unicode glyph name found.</summary>
    NoUnicodeGlyphName = 0xA3,

    /// <summary>`STARTFONT' field missing.</summary>
    MissingStartfontField = 0xB0,

    /// <summary>`FONT' field missing.</summary>
    MissingFontField = 0xB1,

    /// <summary>`SIZE' field missing.</summary>
    MissingSizeField = 0xB2,

    /// <summary>`FONTBOUNDINGBOX' field missing.</summary>
    MissingFontboudingboxField = 0xB3,

    /// <summary>`CHARS' field missing.</summary>
    MissingCharsField = 0xB4,

    /// <summary>`STARTCHAR' field missing.</summary>
    MissingStartcharField = 0xB5,

    /// <summary>`ENCODING' field missing.</summary>
    MissingEncodingField = 0xB6,

    /// <summary>`BBX' field missing.</summary>
    MissingBbxField = 0xB7,

    /// <summary>`BBX' too big.</summary>
    BbxTooBig = 0xB8,

    /// <summary>Font header corrupted or missing fields.</summary>
    CorruptedFontHeader = 0xB9,

    /// <summary>Font glyphs corrupted or missing fields.</summary>
    CorruptedFontGlyphs = 0xBA
}

/// <summary><para>
/// An enumeration type that lists the render modes supported by FreeType 2. Each mode corresponds to a specific
/// type of scanline conversion performed on the outline.
/// </para><para>
/// For bitmap fonts and embedded bitmaps the <see cref="FTBitmap.PixelMode"/> field in the <see cref="GlyphSlot"/>
/// structure gives the format of the returned bitmap.
/// </para><para>
/// All modes except <see cref="FtRenderMode.Mono"/> use 256 levels of opacity.
/// </para></summary>
/// <remarks><para>
/// The LCD-optimized glyph bitmaps produced by <see cref="GlyphSlot.RenderGlyph"/> can be filtered to reduce
/// color-fringes by using <see cref="Library.SetLcdFilter"/> (not active in the default builds). It is up to the
/// caller to either call <see cref="Library.SetLcdFilter"/> (if available) or do the filtering itself.
/// </para><para>
/// The selected render mode only affects vector glyphs of a font. Embedded bitmaps often have a different pixel
/// mode like <see cref="PixelMode.Mono"/>. You can use <see cref="FTBitmap.Convert"/> to transform them into 8-bit
/// pixmaps.
/// </para></remarks>
public enum FtRenderMode
{
    /// <summary>
    /// This is the default render mode; it corresponds to 8-bit anti-aliased bitmaps.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// This is equivalent to <see cref="FtRenderMode.Normal"/>. It is only defined as a separate value because
    /// render modes are also used indirectly to define hinting algorithm selectors.
    /// </summary>
    /// <see cref="LoadTarget"/>
    Light,

    /// <summary>
    /// This mode corresponds to 1-bit bitmaps (with 2 levels of opacity).
    /// </summary>
    Mono,

    /// <summary>
    /// This mode corresponds to horizontal RGB and BGR sub-pixel displays like LCD screens. It produces 8-bit
    /// bitmaps that are 3 times the width of the original glyph outline in pixels, and which use the
    /// <see cref="PixelMode.Lcd"/> mode.
    /// </summary>
    LCD,

    /// <summary>
    /// This mode corresponds to vertical RGB and BGR sub-pixel displays (like PDA screens, rotated LCD displays,
    /// etc.). It produces 8-bit bitmaps that are 3 times the height of the original glyph outline in pixels and
    /// use the <see cref="PixelMode.VerticalLcd"/> mode.
    /// </summary>
    VerticalLcd,
}

unsafe class Api
{
    const string FreetypeDll = "Gossamer.FreeType.dll";

    const CallingConvention CallConvention = CallingConvention.Cdecl;

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    public static extern FT_Error FT_Set_Char_Size(nint face, int char_width, int char_height, uint horz_resolution, uint vert_resolution);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    public static extern FT_Error FT_Set_Pixel_Sizes(nint face, uint pixel_width, uint pixel_height);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    public static extern FT_Error FT_Load_Glyph(nint face, uint glyph_index, int load_flags);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    public static extern FT_Error FT_Load_Char(nint face, uint char_code, FT_Load load_flags);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    public static extern FT_Error FT_Render_Glyph(nint slot, FtRenderMode render_mode);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    public static extern FT_Error FT_Get_Glyph(nint slot, out nint aglyph);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    public static extern FT_Error FT_Init_FreeType(out nint alibrary);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    public static extern FT_Error FT_Done_FreeType(nint library);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    public static extern FT_Error FT_New_Memory_Face(nint library, nint file_base, int file_size, int face_index, out nint aface);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    public static extern void FT_Done_Glyph(nint glyph);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    internal static extern FT_Error FT_Done_Face(nint face);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    internal static extern uint FT_Get_Char_Index(nint face, uint charcode);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    internal static extern uint FT_Get_First_Char(nint face, out uint agindex);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    internal static extern uint FT_Get_Next_Char(nint face, uint char_code, out uint agindex);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    internal static extern FT_Error FT_Get_Multi_Master(nint face, out nint amaster);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    internal static extern FT_Error FT_Get_MM_Var(nint face, out nint amaster);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    internal static extern FT_Error FT_Done_MM_Var(nint library, nint amaster);

    [DllImport(FreetypeDll, CallingConvention = CallConvention)]
    internal static extern FT_Error FT_Set_Named_Instance(nint face, uint instance_index);
}