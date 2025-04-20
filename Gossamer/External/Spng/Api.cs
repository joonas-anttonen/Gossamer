#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;
using System.Security;

namespace Gossamer.External.Spng;

static class Constants
{
    public const int SPNG_VERSION_MAJOR = 0;
    public const int SPNG_VERSION_MINOR = 7;
    public const int SPNG_VERSION_PATCH = 4;
}

enum spng_errno
{
    SPNG_IO_ERROR = -2,
    SPNG_IO_EOF = -1,
    SPNG_OK = 0,
    SPNG_EINVAL,
    SPNG_EMEM,
    SPNG_EOVERFLOW,
    SPNG_ESIGNATURE,
    SPNG_EWIDTH,
    SPNG_EHEIGHT,
    SPNG_EUSER_WIDTH,
    SPNG_EUSER_HEIGHT,
    SPNG_EBIT_DEPTH,
    SPNG_ECOLOR_TYPE,
    SPNG_ECOMPRESSION_METHOD,
    SPNG_EFILTER_METHOD,
    SPNG_EINTERLACE_METHOD,
    SPNG_EIHDR_SIZE,
    SPNG_ENOIHDR,
    SPNG_ECHUNK_POS,
    SPNG_ECHUNK_SIZE,
    SPNG_ECHUNK_CRC,
    SPNG_ECHUNK_TYPE,
    SPNG_ECHUNK_UNKNOWN_CRITICAL,
    SPNG_EDUP_PLTE,
    SPNG_EDUP_CHRM,
    SPNG_EDUP_GAMA,
    SPNG_EDUP_ICCP,
    SPNG_EDUP_SBIT,
    SPNG_EDUP_SRGB,
    SPNG_EDUP_BKGD,
    SPNG_EDUP_HIST,
    SPNG_EDUP_TRNS,
    SPNG_EDUP_PHYS,
    SPNG_EDUP_TIME,
    SPNG_EDUP_OFFS,
    SPNG_EDUP_EXIF,
    SPNG_ECHRM,
    SPNG_EPLTE_IDX,
    SPNG_ETRNS_COLOR_TYPE,
    SPNG_ETRNS_NO_PLTE,
    SPNG_EGAMA,
    SPNG_EICCP_NAME,
    SPNG_EICCP_COMPRESSION_METHOD,
    SPNG_ESBIT,
    SPNG_ESRGB,
    SPNG_ETEXT,
    SPNG_ETEXT_KEYWORD,
    SPNG_EZTXT,
    SPNG_EZTXT_COMPRESSION_METHOD,
    SPNG_EITXT,
    SPNG_EITXT_COMPRESSION_FLAG,
    SPNG_EITXT_COMPRESSION_METHOD,
    SPNG_EITXT_LANG_TAG,
    SPNG_EITXT_TRANSLATED_KEY,
    SPNG_EBKGD_NO_PLTE,
    SPNG_EBKGD_PLTE_IDX,
    SPNG_EHIST_NO_PLTE,
    SPNG_EPHYS,
    SPNG_ESPLT_NAME,
    SPNG_ESPLT_DUP_NAME,
    SPNG_ESPLT_DEPTH,
    SPNG_ETIME,
    SPNG_EOFFS,
    SPNG_EEXIF,
    SPNG_EIDAT_TOO_SHORT,
    SPNG_EIDAT_STREAM,
    SPNG_EZLIB,
    SPNG_EFILTER,
    SPNG_EBUFSIZ,
    SPNG_EIO,
    SPNG_EOF,
    SPNG_EBUF_SET,
    SPNG_EBADSTATE,
    SPNG_EFMT,
    SPNG_EFLAGS,
    SPNG_ECHUNKAVAIL,
    SPNG_ENCODE_ONLY,
    SPNG_EOI,
    SPNG_ENOPLTE,
    SPNG_ECHUNK_LIMITS,
    SPNG_EZLIB_INIT,
    SPNG_ECHUNK_STDLEN,
    SPNG_EINTERNAL,
    SPNG_ECTXTYPE,
    SPNG_ENOSRC,
    SPNG_ENODST,
    SPNG_EOPSTATE,
    SPNG_ENOTFINAL,
};

enum spng_text_type
{
    SPNG_TEXT = 1,
    SPNG_ZTXT = 2,
    SPNG_ITXT = 3
};

enum spng_color_type
{
    SPNG_COLOR_TYPE_GRAYSCALE = 0,
    SPNG_COLOR_TYPE_TRUECOLOR = 2,
    SPNG_COLOR_TYPE_INDEXED = 3,
    SPNG_COLOR_TYPE_GRAYSCALE_ALPHA = 4,
    SPNG_COLOR_TYPE_TRUECOLOR_ALPHA = 6
};

enum spng_filter
{
    SPNG_FILTER_NONE = 0,
    SPNG_FILTER_SUB = 1,
    SPNG_FILTER_UP = 2,
    SPNG_FILTER_AVERAGE = 3,
    SPNG_FILTER_PAETH = 4
};

enum spng_filter_choice
{
    SPNG_DISABLE_FILTERING = 0,
    SPNG_FILTER_CHOICE_NONE = 8,
    SPNG_FILTER_CHOICE_SUB = 16,
    SPNG_FILTER_CHOICE_UP = 32,
    SPNG_FILTER_CHOICE_AVG = 64,
    SPNG_FILTER_CHOICE_PAETH = 128,
    SPNG_FILTER_CHOICE_ALL = (8 | 16 | 32 | 64 | 128)
};

enum spng_interlace_method
{
    SPNG_INTERLACE_NONE = 0,
    SPNG_INTERLACE_ADAM7 = 1
};

/* Channels are always in byte-order */
enum spng_format
{
    SPNG_FMT_RGBA8 = 1,
    SPNG_FMT_RGBA16 = 2,
    SPNG_FMT_RGB8 = 4,

    /* Partially implemented, see documentation */
    SPNG_FMT_GA8 = 16,
    SPNG_FMT_GA16 = 32,
    SPNG_FMT_G8 = 64,

    /* No conversion or scaling */
    SPNG_FMT_PNG = 256,
    SPNG_FMT_RAW = 512  /* big-endian (everything else is host-endian) */
};

enum spng_ctx_flags
{
    SPNG_CTX_IGNORE_ADLER32 = 1, /* Ignore checksum in DEFLATE streams */
    SPNG_CTX_ENCODER = 2 /* Create an encoder context */
};

enum spng_decode_flags
{
    SPNG_DECODE_USE_TRNS = 1, /* Deprecated */
    SPNG_DECODE_USE_GAMA = 2, /* Deprecated */
    SPNG_DECODE_USE_SBIT = 8, /* Undocumented */

    SPNG_DECODE_TRNS = 1, /* Apply transparency */
    SPNG_DECODE_GAMMA = 2, /* Apply gamma correction */
    SPNG_DECODE_PROGRESSIVE = 256 /* Initialize for progressive reads */
};

enum spng_crc_action
{
    /* Default for critical chunks */
    SPNG_CRC_ERROR = 0,

    /* Discard chunk, invalid for critical chunks.
       Since v0.6.2: default for ancillary chunks */
    SPNG_CRC_DISCARD = 1,

    /* Ignore and don't calculate checksum.
       Since v0.6.2: also ignores checksums in DEFLATE streams */
    SPNG_CRC_USE = 2
};

enum spng_encode_flags
{
    SPNG_ENCODE_PROGRESSIVE = 1, /* Initialize for progressive writes */
    SPNG_ENCODE_FINALIZE = 2, /* Finalize PNG after encoding image */
};

struct spng_ihdr
{
    public uint width;
    public uint height;
    public byte bit_depth;
    public byte color_type;
    public byte compression_method;
    public byte filter_method;
    public byte interlace_method;
};

/*struct spng_plte_entry
{
    public byte red;
    public byte green;
    public byte blue;
    public byte alpha;
};

unsafe struct spng_plte
{
    public uint n_entries;
    public fixed spng_plte_entry entries[256];
};*/

unsafe struct spng_trns
{
    public ushort gray;

    public ushort red;
    public ushort green;
    public ushort blue;

    public uint n_type3_entries;
    public fixed byte type3_alpha[256];
};

struct spng_chrm_int
{
    public uint white_point_x;
    public uint white_point_y;
    public uint red_x;
    public uint red_y;
    public uint green_x;
    public uint green_y;
    public uint blue_x;
    public uint blue_y;
};

struct spng_chrm
{
    public double white_point_x;
    public double white_point_y;
    public double red_x;
    public double red_y;
    public double green_x;
    public double green_y;
    public double blue_x;
    public double blue_y;
};

unsafe struct spng_iccp
{
    public fixed byte profile_name[80];
    public ulong profile_len;
    public byte* profile;
};

struct spng_sbit
{
    public byte grayscale_bits;
    public byte red_bits;
    public byte green_bits;
    public byte blue_bits;
    public byte alpha_bits;
};

unsafe struct spng_text
{
    public fixed byte keyword[80];
    public int type;

    public ulong length;
    public byte* text;

    public byte compression_flag; /* iTXt only */
    public byte compression_method; /* iTXt, ztXt only */
    public byte* language_tag; /* iTXt only */
    public byte* translated_keyword; /* iTXt only */
};

struct spng_bkgd
{
    public ushort gray; /* Only for gray/gray alpha */
    public ushort red;
    public ushort green;
    public ushort blue;
    public ushort plte_index; /* Only for indexed color */
};

unsafe struct spng_hist
{
    public fixed ushort frequency[256];
};

struct spng_phys
{
    public uint ppu_x, ppu_y;
    public byte unit_specifier;
};

struct spng_splt_entry
{
    public ushort red;
    public ushort green;
    public ushort blue;
    public ushort alpha;
    public ushort frequency;
};

unsafe struct spng_splt
{
    public fixed byte name[80];
    public byte sample_depth;
    public uint n_entries;
    public spng_splt_entry *entries;
};

struct spng_time
{
    public ushort year;
    public byte month;
    public byte day;
    public byte hour;
    public byte minute;
    public byte second;
};

unsafe struct spng_offs
{
    public int x;
    public int y;
    public byte unit_specifier;
};

unsafe struct spng_exif
{
    public ulong length;
    public byte* data;
};

unsafe struct spng_chunk
{
    public ulong offset;
    public uint length;
    public fixed byte type[4];
    public uint crc;
};

enum spng_location
{
    SPNG_AFTER_IHDR = 1,
    SPNG_AFTER_PLTE = 2,
    SPNG_AFTER_IDAT = 8,
};

unsafe struct spng_unknown_chunk
{
    public fixed byte type[4];
    public ulong length;
    public void* data;
    public spng_location location;
};

enum spng_option
{
    SPNG_KEEP_UNKNOWN_CHUNKS = 1,

    SPNG_IMG_COMPRESSION_LEVEL,
    SPNG_IMG_WINDOW_BITS,
    SPNG_IMG_MEM_LEVEL,
    SPNG_IMG_COMPRESSION_STRATEGY,

    SPNG_TEXT_COMPRESSION_LEVEL,
    SPNG_TEXT_WINDOW_BITS,
    SPNG_TEXT_MEM_LEVEL,
    SPNG_TEXT_COMPRESSION_STRATEGY,

    SPNG_FILTER_CHOICE,
    SPNG_CHUNK_COUNT_LIMIT,
    SPNG_ENCODE_TO_BUFFER,
};

//typedef void* SPNG_CDECL spng_malloc_fn(ulong size);
//typedef void* SPNG_CDECL spng_realloc_fn(void* ptr, ulong size);
//typedef void* SPNG_CDECL spng_calloc_fn(ulong count, ulong size);
//typedef void SPNG_CDECL spng_free_fn(void* ptr);

struct spng_alloc
{
    public nint malloc_fn;
    public nint realloc_fn;
    public nint calloc_fn;
    public nint free_fn;
};

struct spng_row_info
{
    public uint scanline_idx;
    public uint row_num; /* deinterlaced row index */
    public int pass;
    public byte filter;
};

readonly struct spng_ctx { }

[SuppressUnmanagedCodeSecurity]
unsafe static class Api
{
    public const string BinaryName = "External/spng-0.7.4";
    public const CallingConvention CallConvention = CallingConvention.Cdecl;

    //typedef int spng_read_fn(spng_ctx* ctx, void* user, void* dest, ulong length);
    //typedef int spng_write_fn(spng_ctx* ctx, void* user, void* src, ulong length);

    //typedef int spng_rw_fn(spng_ctx* ctx, void* user, void* dst_src, ulong length);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern spng_ctx* spng_ctx_new(spng_ctx_flags flags);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern spng_ctx* spng_ctx_new2(spng_alloc* alloc, spng_ctx_flags flags);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void spng_ctx_free(spng_ctx* ctx);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern spng_errno spng_set_png_buffer(spng_ctx* ctx, void* buf, ulong size);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_png_stream(spng_ctx* ctx, nint rw_func, void* user);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_png_file(spng_ctx* ctx, nint file);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void* spng_get_png_buffer(spng_ctx* ctx, ulong* len, int* error);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_image_limits(spng_ctx* ctx, uint width, uint height);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_image_limits(spng_ctx* ctx, uint* width, uint* height);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_chunk_limits(spng_ctx* ctx, ulong chunk_size, ulong cache_size);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_chunk_limits(spng_ctx* ctx, ulong* chunk_size, ulong* cache_size);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_crc_action(spng_ctx* ctx, int critical, int ancillary);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_option(spng_ctx* ctx, spng_option option, int value);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_option(spng_ctx* ctx, spng_option option, int* value);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern spng_errno spng_decoded_image_size(spng_ctx* ctx, spng_format fmt, ulong* len);

    /* Decode */
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern spng_errno spng_decode_image(spng_ctx* ctx, byte* _out, ulong len, spng_format fmt, spng_decode_flags flags);

    /* Progressive decode */
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_decode_scanline(spng_ctx* ctx, void* _out, ulong len);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_decode_row(spng_ctx* ctx, void* _out, ulong len);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_decode_chunks(spng_ctx* ctx);

    /* Encode/decode */
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_row_info(spng_ctx* ctx, spng_row_info* row_info);

    /* Encode */
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_encode_image(spng_ctx* ctx, void* img, ulong len, spng_format fmt, spng_encode_flags flags);

    /* Progressive encode */
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_encode_scanline(spng_ctx* ctx, void* scanline, ulong len);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_encode_row(spng_ctx* ctx, void* row, ulong len);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_encode_chunks(spng_ctx* ctx);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern spng_errno spng_get_ihdr(spng_ctx* ctx, spng_ihdr* ihdr);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_plte(spng_ctx* ctx, nint plte);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_trns(spng_ctx* ctx, spng_trns* trns);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_chrm(spng_ctx* ctx, spng_chrm* chrm);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_chrm_int(spng_ctx* ctx, spng_chrm_int* chrm_int);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_gama(spng_ctx* ctx, double* gamma);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_gama_int(spng_ctx* ctx, uint* gama_int);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_iccp(spng_ctx* ctx, spng_iccp* iccp);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_sbit(spng_ctx* ctx, spng_sbit* sbit);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_srgb(spng_ctx* ctx, byte* rendering_intent);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_text(spng_ctx* ctx, spng_text* text, uint* n_text);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_bkgd(spng_ctx* ctx, spng_bkgd* bkgd);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_hist(spng_ctx* ctx, spng_hist* hist);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_phys(spng_ctx* ctx, spng_phys* phys);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_splt(spng_ctx* ctx, spng_splt* splt, uint* n_splt);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_time(spng_ctx* ctx, spng_time* time);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_unknown_chunks(spng_ctx* ctx, spng_unknown_chunk* chunks, uint* n_chunks);

    /* Official extensions */
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_offs(spng_ctx* ctx, spng_offs* offs);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_get_exif(spng_ctx* ctx, spng_exif* exif);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_ihdr(spng_ctx* ctx, spng_ihdr* ihdr);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_plte(spng_ctx* ctx, nint plte);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_trns(spng_ctx* ctx, spng_trns* trns);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_chrm(spng_ctx* ctx, spng_chrm* chrm);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_chrm_int(spng_ctx* ctx, spng_chrm_int* chrm_int);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_gama(spng_ctx* ctx, double gamma);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_gama_int(spng_ctx* ctx, uint gamma);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_iccp(spng_ctx* ctx, spng_iccp* iccp);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_sbit(spng_ctx* ctx, spng_sbit* sbit);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_srgb(spng_ctx* ctx, byte rendering_intent);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_text(spng_ctx* ctx, spng_text* text, uint n_text);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_bkgd(spng_ctx* ctx, spng_bkgd* bkgd);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_hist(spng_ctx* ctx, spng_hist* hist);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_phys(spng_ctx* ctx, spng_phys* phys);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_splt(spng_ctx* ctx, spng_splt* splt, uint n_splt);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_time(spng_ctx* ctx, spng_time* time);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_unknown_chunks(spng_ctx* ctx, spng_unknown_chunk* chunks, uint n_chunks);

    /* Official extensions */
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_offs(spng_ctx* ctx, spng_offs* offs);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int spng_set_exif(spng_ctx* ctx, spng_exif* exif);


    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern byte* spng_strerror(int err);
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern byte* spng_version_string();
}
