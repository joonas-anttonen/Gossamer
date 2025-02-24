#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;

namespace Gossamer.External.HarfBuzz;

public enum Direction
{
    Invalid = 0,
    LeftToRight = 4,
    RightToLeft,
    TopToBottom,
    BottomToTop
}

public enum Script
{
    Invalid = 0,
    Arabic = 1098015074,
    ImperialAramaic = 1098018153,
    Armenian = 1098018158,
    Avestan = 1098281844,
    Balinese = 1113681001,
    Bamum = 1113681269,
    Batak = 1113683051,
    Bengali = 1113943655,
    Bopomofo = 1114599535,
    Brahmi = 1114792296,
    Braille = 1114792297,
    Buginese = 1114990441,
    Buhid = 1114990692,
    Chakma = 1130457965,
    CanadianSyllabics = 1130458739,
    Carian = 1130459753,
    Cham = 1130914157,
    Cherokee = 1130915186,
    Coptic = 1131376756,
    Cypriot = 1131442804,
    Cyrillic = 1132032620,
    Devanagari = 1147500129,
    Deseret = 1148416628,
    EgyptianHieroglyphs = 1164409200,
    Ethiopic = 1165256809,
    Georgian = 1197830002,
    Glagolitic = 1198285159,
    Gothic = 1198486632,
    Greek = 1198679403,
    Gujarati = 1198877298,
    Gurmukhi = 1198879349,
    Hangul = 1214344807,
    Han = 1214344809,
    Hanunoo = 1214344815,
    Hebrew = 1214603890,
    Hiragana = 1214870113,
    OldItalic = 1232363884,
    Javanese = 1247901281,
    KayahLi = 1264675945,
    Katakana = 1264676449,
    Kharoshthi = 1265131890,
    Khmer = 1265134962,
    Kannada = 1265525857,
    Kaithi = 1265920105,
    TaiTham = 1281453665,
    Lao = 1281453935,
    Latin = 1281455214,
    Lepcha = 1281716323,
    Limbu = 1281977698,
    LinearB = 1281977954,
    Lisu = 1281979253,
    Lycian = 1283023721,
    Lydian = 1283023977,
    Mandaic = 1298230884,
    MeroiticCursive = 1298494051,
    MeroiticHieroglyphs = 1298494063,
    Malayalam = 1298954605,
    Mongolian = 1299148391,
    MeeteiMayek = 1299473769,
    Myanmar = 1299803506,
    Nko = 1315663727,
    Ogham = 1332175213,
    OlChiki = 1332503403,
    OldTurkic = 1332898664,
    Oriya = 1332902241,
    Osmanya = 1332964705,
    PhagsPa = 1349017959,
    InscriptionalPahlavi = 1349020777,
    Phoenician = 1349021304,
    Miao = 1349284452,
    InscriptionalParthian = 1349678185,
    Rejang = 1382706791,
    Runic = 1383427698,
    Samaritan = 1398893938,
    OldSouthArabian = 1398895202,
    Saurashtra = 1398895986,
    Shavian = 1399349623,
    Sharada = 1399353956,
    Sinhala = 1399418472,
    SoraSompeng = 1399812705,
    Sundanese = 1400204900,
    SylotiNagri = 1400466543,
    Syriac = 1400468067,
    Tagbanwa = 1415669602,
    Takri = 1415670642,
    TaiLe = 1415670885,
    NewTaiLue = 1415670901,
    Tamil = 1415671148,
    TaiViet = 1415673460,
    Telugu = 1415933045,
    Tifinagh = 1415999079,
    Tagalog = 1416064103,
    Thaana = 1416126817,
    Thai = 1416126825,
    Tibetan = 1416192628,
    Ugaritic = 1432838514,
    Vai = 1449224553,
    OldPersian = 1483761007,
    Cuneiform = 1483961720,
    Yi = 1500080489,
    Inherited = 1516858984,
    Common = 1517910393,
    Unknown = 1517976186
}

/**
 * hb_buffer_content_type_t:
 * @HB_BUFFER_CONTENT_TYPE_INVALID: Initial value for new buffer.
 * @HB_BUFFER_CONTENT_TYPE_UNICODE: The buffer contains input characters (before shaping).
 * @HB_BUFFER_CONTENT_TYPE_GLYPHS: The buffer contains output glyphs (after shaping).
 *
 * The type of #hb_buffer_t contents.
 */
enum hb_buffer_content_type_t
{
    INVALID = 0,
    UNICODE,
    GLYPHS
}

struct hb_glyph_info_t
{
    /// <summary>
    /// Either a Unicode code point (before shaping) or a glyph index (after shaping).
    /// </summary>
    public uint CodepointOrIndex;
    public uint mask;
    public uint cluster;

    public uint var1;
    public uint var2;
}

struct hb_glyph_position_t
{
    public int xAdvance;
    public int yAdvance;
    public int xOffset;
    public int yOffset;

    public uint var;
}

[Flags]
enum hb_buffer_flags_t : uint
{
    HB_BUFFER_FLAG_DEFAULT = 0x00000000u,
    HB_BUFFER_FLAG_BOT = 0x00000001u, /* Beginning-of-text */
    HB_BUFFER_FLAG_EOT = 0x00000002u, /* End-of-text */
    HB_BUFFER_FLAG_PRESERVE_DEFAULT_IGNORABLES = 0x00000004u,
    HB_BUFFER_FLAG_REMOVE_DEFAULT_IGNORABLES = 0x00000008u,
    HB_BUFFER_FLAG_DO_NOT_INSERT_DOTTED_CIRCLE = 0x00000010u,
    HB_BUFFER_FLAG_VERIFY = 0x00000020u,
    HB_BUFFER_FLAG_PRODUCE_UNSAFE_TO_CONCAT = 0x00000040u,
    HB_BUFFER_FLAG_PRODUCE_SAFE_TO_INSERT_TATWEEL = 0x00000080u,

    HB_BUFFER_FLAG_DEFINED = 0x000000FFu
}

struct hb_feature_t
{
    public static readonly hb_feature_t EnableKerning = new() { tag = Api.HB_TAG((byte)'k', (byte)'e', (byte)'r', (byte)'n'), value = 1, start = 0, end = uint.MaxValue };
    public static readonly hb_feature_t DisableKerning = new() { tag = Api.HB_TAG((byte)'k', (byte)'e', (byte)'r', (byte)'n'), value = 0, start = 0, end = uint.MaxValue };
    public static readonly hb_feature_t EnableLigatures = new() { tag = Api.HB_TAG((byte)'l', (byte)'i', (byte)'g', (byte)'a'), value = 1, start = 0, end = uint.MaxValue };
    public static readonly hb_feature_t DisableLigatures = new() { tag = Api.HB_TAG((byte)'l', (byte)'i', (byte)'g', (byte)'a'), value = 0, start = 0, end = uint.MaxValue };

    public uint tag;
    public uint value;
    public uint start;
    public uint end;
}

[System.Security.SuppressUnmanagedCodeSecurity]
unsafe class Api
{
    public const string BinaryName = "External/libgossamer-harfbuzz";

    const CallingConvention CallConvention = CallingConvention.Cdecl;

    public static uint HB_TAG(byte c1, byte c2, byte c3, byte c4)
        => (((uint)c1 & 0xFF) << 24) | (((uint)c2 & 0xFF) << 16) | (((uint)c3 & 0xFF) << 8) | ((uint)c4 & 0xFF);

    internal delegate uint hb_buffer_message_func_t(nint buffer, nint font, nint message, nint user_data);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    internal static extern hb_buffer_flags_t hb_buffer_get_flags(nint buffer);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    internal static extern void hb_buffer_set_flags(nint buffer, hb_buffer_flags_t flags);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    internal static extern void hb_buffer_set_content_type(nint buffer, hb_buffer_content_type_t content_type);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    internal static extern hb_buffer_content_type_t hb_buffer_get_content_type(nint buffer);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    internal static extern void hb_buffer_guess_segment_properties(nint buffer);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    internal static extern void hb_buffer_set_message_func(nint buffer, nint func, nint user_data, nint destroy);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint hb_ft_font_set_load_flags(nint font, FreeType.FT_Load load_flags);

    /// <summary>
    /// Set the FreeType font functions for the HarfBuzz font.
    /// </summary>
    /// <param name="font"></param>
    /// <returns></returns>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void hb_ft_font_set_funcs(nint font);

    /// <summary>
    /// Refreshes the state of the underlying FT_Face of font when the hb_font_t font has changed. 
    /// This function should be called after changing the size or variation-axis settings on the font.
    /// This call is fast if nothing has changed on font.
    /// </summary>
    /// <param name="font"></param>
    /// <returns>true if changed, false otherwise</returns>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern bool hb_ft_hb_font_changed(nint font);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void hb_buffer_set_language(nint buffer, nint language);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint hb_language_from_string([MarshalAs(UnmanagedType.LPUTF8Str)] string str, int len);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void hb_font_set_ptem(nint font, float ptem);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint hb_buffer_create();

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint hb_buffer_destroy(nint hb_buffer);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint hb_font_destroy(nint hb_font);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void hb_ft_font_changed(nint hb_font);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint hb_ft_font_create_referenced(nint ft_face);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void hb_buffer_reset(nint buffer);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void hb_buffer_clear_contents(nint buffer);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void hb_buffer_set_direction(nint buffer, Direction direction);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void hb_buffer_set_script(nint ptr, Script script);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public unsafe static extern void hb_buffer_add_codepoints(nint buffer, uint* text, int text_length, uint item_offset, int item_length);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public unsafe static extern void hb_buffer_add_utf16(nint buffer, ushort* text, int text_length, uint item_offset, int item_length);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void hb_shape(nint font, nint buffer, nint features, int num_features);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint hb_buffer_get_glyph_infos(nint buf, out int length);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint hb_buffer_get_glyph_positions(nint buf, out int length);
}
