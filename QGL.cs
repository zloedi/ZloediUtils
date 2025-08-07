// FIXME: right now, the lates are doubled if one of the context doesn't flush (i.e. its window was not OnGUI rendered)
#if UNITY_STANDALONE || UNITY_2021_1_OR_NEWER
#define HAS_UNITY
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

#if SDL
using SDLPorts;
using GalliumMath;
#else
using UnityEngine;
#endif

#if SDL || HAS_UNITY

public static class QGL {

public enum LateType {
    None,
    Line,
    Text,
    NokiaText,
    Image,
}

struct Late {
    public LateType type;

    public int context;
    public Color color;

    public List<Vector2> line;

    public float x, y;
    public float scale;
    public string str;

    // dest rect
    public float w, h;
    // src rect
    public float sx, sy, sw, sh;
    // orientation
    public float ox, oy;

    public Texture texture;
    public Material material;

    public int timestamp;
}

class FontInfo {
    public Texture2D tex;
    public int numColumns;
    public int numRows;
    public int charWidth;
    public int charHeight;
    public int cursorChar;
    public bool outlined;
}

struct CharInfo {
    public int hash;
    public Vector2 [] uv;
    public Vector2 [] verts;
}

public static float ScreenWidth { get; private set; }
public static float ScreenHeight { get; private set; }
public static Action<object> Log = o => {};
public static Action<string> Error = s => {};
public static float PixelsPerPoint { get; private set; } = 1;
public static int CursorChar => _currentFontInfo.cursorChar;
public static float TextDx { get; private set; } = 1;
public static float TextDy { get; private set; } = 1;

static int CharSpacingX_cvar = -3;
static int CharSpacingY_cvar = 3;
static int Font_cvar = 0;
static int ShowFontTexture_cvar = 0;

static Camera _camera;
// used with Cellophane color tags
static List<Color> _colStack = new List<Color>();
// these are postponed and drawn after all geometry in scene
static Late [] _lates = new Late[2 * 1024];
static int _lateHead, _lateTail;
static int _frameCount;
static int NewLate( LateType type, int context, Color? color ) {
    if ( _frameCount != Time.frameCount ) {
        ClearLates();
        _frameCount = Time.frameCount;
    }
    int idx = _lateTail & ( _lates.Length - 1);
    _lateTail++;
    _lates[idx].type = type;
    _lates[idx].context = context;
    _lates[idx].color = color ?? Color.green;
    _lates[idx].timestamp = Time.frameCount;
    return idx;
}
static void DeleteLate( int idx ) {
    idx &= _lates.Length - 1;
    _lates[idx].context = 0;
    _lates[idx].type = 0;
}
static bool IsValidLate( int idx ) {
    idx &= _lates.Length - 1;
    return _lates[idx].type != 0;
}
static Material _material;
static Texture _texMain;
static Texture2D _texWhite = Texture2D.whiteTexture;
static Texture2D _texChecker;
static Vector2 [] _linePair = new Vector2[2];
static Vector2 [] _lineRect = new Vector2[4];
static Vector3 [] _lineRectWorld = new Vector3[4];
static List<Vector2> _lineBuf = new();
static bool _invertedY;
static int _context;
static FontInfo [] _allFonts;
static FontInfo _currentFontInfo = new FontInfo();
static int _currentFont => _allFonts == null ? 0 : Font_cvar % _allFonts.Length;
static int _fontCharHeight => _currentFontInfo.charHeight;
static int _fontCharWidth  => _currentFontInfo.charWidth;
static int _fontNumColumns => _currentFontInfo.numColumns;
static int _fontNumRows    => _currentFontInfo.numRows;

static Dictionary<int,CharInfo> _ciMap = new Dictionary<int,CharInfo>();
static CharInfo [] _ciCache = new CharInfo[256];

static QGL() {
    for ( int i = 0; i < _lates.Length; i++ ) {
        _lates[i].line = new();
    }

    _ciCache[0].uv = new Vector2[4];
    _ciCache[0].verts = new Vector2[4];
}

public static Color TagToCol( string tag ) {
    int [] rgb = new int[3 * 2];
    if ( tag.Length > rgb.Length ) {
        for ( int i = 0; i < rgb.Length; i++ ) {
            rgb[i] = Uri.FromHex( tag[i + 1] );
        }
    }
    return new Color( ( ( rgb[0] << 4 ) | rgb[1] ) / 255.999f,
                      ( ( rgb[2] << 4 ) | rgb[3] ) / 255.999f,
                      ( ( rgb[4] << 4 ) | rgb[5] ) / 255.999f );
}

public static bool Start( bool invertedY = false ) {
    Shader shader = Shader.Find( "GLSprites" );
    if ( ! shader ) {
        // no color on sprites with this one
        shader = Shader.Find( "GUI/Text Shader" );
    }
    if ( shader ) {
        _material = new Material( shader );
        _material.hideFlags = HideFlags.HideAndDontSave;
        ClearLates();
        SetContext( null, invertedY: invertedY );
        Log( $"GL started, using shader {shader.name}" );
        return true;
    }
    Error( "Can't find GL shader" );
    return false;
}

public static Vector2 MeasureString( string s, float scale = 1 ) { 
    float x = 0, y = TextDy * scale, max = 0;
    for ( int i = 0; i < s.Length; i++ ) {
        if ( s[i] == '\n' ) {
            x = 0;
            y += TextDy * scale;
        } else {
            x += TextDx * scale;
            max = Mathf.Max( max, x );
        }
    }
    return new Vector2( max, y );
}

public static Vector2Int MeasureStringNokiaInt( string s, int scale = 1 ) { 
    int cx = 0;
    int cy = NokiaFont.NOKIA_LN_H * scale;
    int max = 0;
    foreach ( var cc in s ) {
        int c = cc & 255;
        NokiaFont.Glyph g = NokiaFont.glyphs[c];
        cx += c == '\n' ? -cx : g.xadvance * scale;
        cy += c == '\n' ? NokiaFont.NOKIA_LN_H * scale : 0;
        max = Mathf.Max( max, cx );
    }
    return new Vector2Int( max, cy );
}

public static Vector2 MeasureStringNokia( string s, float scale = 1 ) { 
    float cx = 0;
    float cy = NokiaFont.NOKIA_LN_H * scale;
    float max = 0;
    foreach ( var cc in s ) {
        int c = cc & 255;
        NokiaFont.Glyph g = NokiaFont.glyphs[c];
        cx += c == '\n' ? -cx : g.xadvance * scale;
        cy += c == '\n' ? NokiaFont.NOKIA_LN_H * scale : 0;
        max = Mathf.Max( max, cx );
    }
    return new Vector2( max, cy );
}

public static void DrawTextNokia( string s, float x, float y, Color color, float scale = 1 ) {
    y = _invertedY ? ScreenHeight - y : y;
    float ys = _invertedY ? -1 : 1;

    float cx = 0;
    float cy = 0;

    foreach ( var cc in s ) {
        int c = cc & 255;

        NokiaFont.Glyph g = NokiaFont.glyphs[c];

        var src = new float[] {
            g.x,
            g.y,
            g.width,
            g.height,
        };

        var dst = new float[] {
            x + cx,
            y + cy + ys * g.yoffset * scale,
            g.width,
            g.height,
        };

        Vector3 uvOff = new Vector3( src[0] / NokiaFont.NOKIA_IMG_W,
                                        src[1] / NokiaFont.NOKIA_IMG_H );
        float charU = src[2] / NokiaFont.NOKIA_IMG_W;
        float charV = src[3] / NokiaFont.NOKIA_IMG_H;

        var uv = new Vector3[4] {
            new Vector3( 0, 0, 0 ),
            new Vector3( charU, 0, 0 ),
            new Vector3( charU, charV, 0 ),
            new Vector3( 0, charV, 0 ),
        };

        for ( int i = 0; i < 4; i++ ) {
            uv[i] += uvOff;
        }

        Vector3 [] verts;
        verts = new Vector3 [4] {
            new Vector3( 0, 0, 0 ),
            new Vector3( g.width, 0, 0 ),
            new Vector3( g.width, ys * g.height, 0 ),
            new Vector3( 0, ys * g.height, 0 ),
        };

        GL.Color( color );
        for ( int i = 0; i < 4; i++ ) {
            GL.TexCoord( uv[i] );
            GL.Vertex( verts[i] * scale + new Vector3( dst[0], dst[1] ) );
        }

        cx += c == '\n' ? -cx : g.xadvance * scale;
        cy += c == '\n' ? ys * NokiaFont.NOKIA_LN_H * scale : 0;
    }
}

public static void DrawTextWithOutline( string s, float x, float y, Color color, float scale = 1 ) {
    for ( int i = 0, j = 0; ; i++ ) {
        while ( i < s.Length ) {
            if ( Cellophane.ColorTagLead( s, i, out string tl ) ) {
                _colStack.Add( TagToCol( tl ) );
                i += tl.Length;
            } else if ( _colStack.Count > 0 && Cellophane.ColorTagClose( s, i, out string tc ) ) {
                _colStack.RemoveAt( _colStack.Count - 1 );
                i += tc.Length;
            } else {
                break;
            }
        }

        if ( i >= s.Length )
            return;

        if ( s[i] == '\n' ) {
            j = 0;
            y += TextDy * scale;
        } else {
            var c = _colStack.Count > 0 ? _colStack[_colStack.Count - 1] : color;
            DrawScreenCharWithOutline( s[i], x + j * TextDx * scale, y, c, scale );
            j++;
        }
    }
}

public static void DrawScreenCharWithOutline( int c, float screenX, float screenY, Color color,
                                                                                float scale = 1 ) { 
    // == outline ==
    if ( ! _currentFontInfo.outlined ) {
        GL.Color( new Color( 0, 0, 0, 1 * ( color.a * color.a * color.a ) ) );
        DrawScreenChar( c, screenX + scale, screenY +     0, scale );
        DrawScreenChar( c, screenX + scale, screenY + scale, scale );
        DrawScreenChar( c, screenX +     0, screenY + scale, scale );

        DrawScreenChar( c, screenX - scale, screenY -     0, scale );
        DrawScreenChar( c, screenX - scale, screenY - scale, scale );
        DrawScreenChar( c, screenX -     0, screenY - scale, scale );

        DrawScreenChar( c, screenX + scale, screenY - scale, scale );
        DrawScreenChar( c, screenX - scale, screenY + scale, scale );
    }

    // == actual character ==
    GL.Color( color );
    DrawScreenChar( c, screenX, screenY, scale );
}

public static void SetFontTexture() {

    // make sure we work when going back to edit mode
    _currentFontInfo = _allFonts == null ? null : _allFonts[_currentFont];

    if ( _currentFontInfo == null ) {
        _allFonts = new FontInfo [] {
            new FontInfo {
                tex        = AppleFont.GetTexture(),
                numColumns = AppleFont.APPLEIIF_CLMS,
                numRows    = AppleFont.APPLEIIF_ROWS,
                charWidth  = AppleFont.APPLEIIF_CW,
                charHeight = AppleFont.APPLEIIF_CH,
                cursorChar = 127,
            },

            new FontInfo {
                tex        = CodePage437.GetTexture(),
                numColumns = CodePage437.FontSz,
                numRows    = CodePage437.FontSz,
                charWidth  = CodePage437.CharSz,
                charHeight = CodePage437.CharSz,
                cursorChar = 0xdb,
            },

            new FontInfo {
                tex        = AppleFont.GetTextureWithOutline(),
                numColumns = AppleFont.APPLEIIF_CLMS,
                numRows    = AppleFont.APPLEIIF_ROWS,
                charWidth  = AppleFont.APPLEIIF_CW + 1,
                charHeight = AppleFont.APPLEIIF_CH + 2,
                cursorChar = 127,
                outlined = true,
            },
        };
        _currentFontInfo = _allFonts[_currentFont];
    }

    TextDx = Mathf.Max( AppleFont.APPLEIIF_CW + 1, _fontCharWidth + CharSpacingX_cvar );
    TextDy = _fontCharHeight + CharSpacingY_cvar;

    SetTexture( _currentFontInfo.tex );
}

public static void SetWhiteTexture() {
    SetTexture( _texWhite );
}

public static void SetMaterialColor( Color color ) {
    _material.color = color;
}

public static void SetTexture( Texture tex ) {
    if (_texMain == tex)
        return;
    _material.SetTexture( "_MainTex", tex );
    _material.SetPass( 0 );
    _texMain = tex;
}

public static void DrawQuad( Vector2 pos, Vector2 size,
                                            Vector2? srcOrigin = null, Vector2? srcSize = null ) { 
    Vector2 uv0 = srcOrigin != null ? srcOrigin.Value : Vector2.zero;
    Vector2 uv1 = srcSize != null ? ( uv0 + srcSize.Value ) : Vector2.one;

    float y = _invertedY ? ScreenHeight - pos.y : pos.y;
    float dy = _invertedY ? y - size.y : y + size.y;

    GL.TexCoord( new Vector3( uv0.x, uv0.y, 0 ) );
    GL.Vertex( new Vector3( pos.x, y, 0 ) );
    GL.TexCoord( new Vector3( uv1.x, uv0.y, 0 ) );
    GL.Vertex( new Vector3( pos.x + size.x, y, 0 ) );
    GL.TexCoord( new Vector3( uv1.x, uv1.y, 0 ) );
    GL.Vertex( new Vector3( pos.x + size.x, dy, 0 ) );
    GL.TexCoord( new Vector3( uv0.x, uv1.y, 0 ) );
    GL.Vertex( new Vector3( pos.x, dy, 0 ) );
}

public static void DrawSolidQuad( Vector2 pos, Vector2 size ) { 
    float y = _invertedY ? ScreenHeight - pos.y : pos.y;
    float dy = _invertedY ? y - size.y : y + size.y;
    
    GL.Vertex( new Vector3( pos.x, y, 0 ) );
    GL.Vertex( new Vector3( pos.x + size.x, y, 0 ) );
    GL.Vertex( new Vector3( pos.x + size.x, dy, 0 ) );
    GL.Vertex( new Vector3( pos.x, dy, 0 ) );
}

public static void DrawScreenChar( int c, float screenX, float screenY, float scale ) { 
    int hash = ( ( _invertedY ? 1 : 0 ) << 16 ) | ( _currentFont << 8 ) | ( c & 255 );

    CharInfo ci;

    if ( _ciCache[c & 255].hash == hash ) {
        ci = _ciCache[c & 255];
    } else if ( ! _ciMap.TryGetValue( hash, out ci ) ) {
        Texture2D texFont = _currentFontInfo.tex;

        int idx = c % ( _fontNumColumns * _fontNumRows );

        float charU = ( float )( _fontCharWidth ) / texFont.width;
        float charV = ( float )( _fontCharHeight ) / texFont.height;

        ci.uv = new Vector2[4] {
            new Vector2( 0, 0 ),
            new Vector2( charU, 0 ),
            new Vector2( charU, charV ),
            new Vector2( 0, charV ),
        };

        var uvOff = new Vector2( ( idx % _fontNumColumns ) * charU,
                                        ( idx / _fontNumColumns ) * charV );

        for ( int i = 0; i < 4; i++ ) {
            ci.uv[i] += uvOff;
        }

        if ( _invertedY ) {
            ci.verts = new Vector2[4] {
                new Vector2( 0, 0 ),
                new Vector2( _fontCharWidth, 0 ),
                new Vector2( _fontCharWidth, -_fontCharHeight ),
                new Vector2( 0, -_fontCharHeight ),
            };
        } else {
            ci.verts = new Vector2[4] {
                new Vector2( 0, 0 ),
                new Vector2( _fontCharWidth, 0 ),
                new Vector2( _fontCharWidth, _fontCharHeight ),
                new Vector2( 0, _fontCharHeight ),
            };
        }

        ci.hash = hash;

        _ciMap[hash] = ci;
    }

    _ciCache[c & 255] = ci;

    float y = _invertedY ? ScreenHeight - screenY : screenY;

    for ( int i = 0; i < 4; i++ ) {
        GL.TexCoord3( ci.uv[i].x, ci.uv[i].y, 0 );
        GL.Vertex3( ci.verts[i].x * scale + screenX, ci.verts[i].y * scale + y, 0 );
    }
}

public static Vector2 LatePrintWorld( object o, Vector3 worldPos, Color? color = null,
                                                                                float scale = 1 ) {
    return LatePrintWorld( o.ToString(), worldPos, color, scale );
}

public static Vector2 LatePrintWorld( string o, Vector3 worldPos, Color? color = null,
                                                                                float scale = 1 ) {
    Vector2 pt = WorldToScreenPos( worldPos );
    LatePrint( o, pt.x, pt.y, color, scale );
    return pt;
}

public static Vector2 LatePrintWorld( object o, float x, float y, Color? color = null,
                                                                                float scale = 1 ) {
    return LatePrintWorld( o, new Vector2( x, y ), color, scale );
}

public static void LatePrint( object o, Vector2 xy, Color? color = null, float scale = 1 ) {
    LatePrint( o.ToString(), xy.x, xy.y, color, scale );
}

public static void LatePrint( object o, float x, float y, Color? color = null, float scale = 1 ) {
    LatePrint( o.ToString(), x, y, color, scale );
}

public static void LatePrint( string str, float x, float y, Color? color = null, float scale = 1 ) {
    Vector2 sz = MeasureString( str, scale );
    AddCenteredText( str, sz, x, y, color, scale );
}

// top-left version of the late prints (which are centered)
public static void LatePrint_tl( object o, Vector2 xy, Color? color = null, float scale = 1 ) {
    LatePrint_tl( o.ToString(), xy.x, xy.y, color, scale );
}

public static void LatePrint_tl( object o, float x, float y, Color? color = null, float scale = 1 ) {
    LatePrint_tl( o.ToString(), x, y, color, scale );
}

public static void LatePrint_tl( string str, float x, float y, Color? color = null, float scale = 1 ) {
    AddText( str, x, y, color, scale );
}

public static void LatePrintNokia( string str, Vector2Int xy, Color? color = null,
                                                                                float scale = 1 ) {
    LatePrintNokia( str, xy.x, xy.y, color, scale );
}

public static void LatePrintNokia( string str, float x, float y, Color? color = null,
                                                                                float scale = 1 ) {
    Vector2 sz = MeasureStringNokia( str, scale );

#if false
    var txt = new LateTextNokia {
        context = _context,
        x = Mathf.Round( x - ( int )sz.x / 2 ),
        y = Mathf.Round( y - ( int )sz.y / 2 ),
        scale = scale,
        str = str,
        color = color == null ? Color.green : color.Value,
    };
    _lates.Add( txt );
#endif

    int i = NewLate( LateType.NokiaText, _context, color );
    _lates[i].x = Mathf.Round( x - ( int )sz.x / 2 );
    _lates[i].y = Mathf.Round( y - ( int )sz.y / 2 );
    _lates[i].scale = scale;
    _lates[i].str = str;
}

public static void LatePrintNokia_tl( string str, float x, float y, Color? color = null,
                                                                                float scale = 1 ) {
#if false
    var txt = new LateTextNokia {
        context = _context,
        x = ( int )x,
        y = ( int )y,
        scale = scale,
        str = str,
        color = color == null ? Color.green : color.Value,
    };
    _lates.Add( txt );
#endif

    int i = NewLate( LateType.NokiaText, _context, color );
    _lates[i].context = _context;
    _lates[i].x = ( int )x;
    _lates[i].y = ( int )y;
    _lates[i].scale = scale;
    _lates[i].str = str;
}

public static void LateChecker( float x0, float y0, float x1, float y1, Color? color = null ) {
    if ( ! _texChecker ) {
        //var tex = new Texture2D(2, 2, textureFormat: TextureFormat.Alpha8, 
        var tex = new Texture2D(2, 2, textureFormat: TextureFormat.RGBA32, 
                                                                  mipChain: false, linear: false ); 
        var colors = new Color32[] {
            Color.clear, Color.white,
            Color.white, Color.clear,
        };
        tex.SetPixels32(colors);
        tex.Apply();
        _texChecker = tex;
    }
    int x = ( int )x0;
    int y = ( int )y0;
    int w = ( int )( x1 - x0 + 1 );
    int h = ( int )( y1 - y0 + 1 );
    LateBlitComplete( _texChecker, x, y, w, h, color: color, sw: w, sh: h );
}

public static Vector2 LateBlitWorld( Texture2D tex, Vector3 worldPos, float w, float h,
                                                                            Color? color = null ) {
    Vector2 pt = WorldToScreenPos( worldPos );
    LateBlitComplete( tex, pt.x - w / 2, pt.y - h / 2, w, h, color: color );
    return pt;
}

public static Vector2 LateBlitWorld( Vector3 worldPos, float w, float h, Color? color = null ) {
    Vector2 pt = WorldToScreenPos( worldPos );
    LateBlitComplete( null, pt.x - w / 2, pt.y - h / 2, w, h, color: color );
    return pt;
}

public static void LateBlit( Texture tex, Vector2 xy, Vector2 sz, float angle = float.MaxValue,
                                                                            Color? color = null ) {
    LateBlit( tex, xy, sz.x, sz.y, angle: angle, color: color );
}

public static void LateBlit( Vector2 xy, Vector2 sz, float angle = float.MaxValue,
                                                                            Color? color = null ) {
    LateBlit( null, xy, sz, angle: angle, color: color );
}

public static void LateBlit( Texture tex, Vector2 xy,
                                            float w = float.MaxValue, float h = float.MaxValue,
                                            float angle = float.MaxValue, Color? color = null ) {
    LateBlit( tex, xy.x, xy.y, w, h, angle: angle, color: color );
}

public static void LateBlit( float x, float y, float w = float.MaxValue, float h = float.MaxValue,
                                            float angle = float.MaxValue, Color? color = null ) {
    LateBlit( null, x, y, w, h, angle: angle, color: color );
}

public static void LateBlit( Texture tex, float x, float y,
                        float w = float.MaxValue, float h = float.MaxValue,
                        float angle = float.MaxValue, Color? color = null, Material mat = null ) {
    if ( angle != float.MaxValue ) {
        float rad = angle * Mathf.Deg2Rad;
        float sn = Mathf.Sin( rad );
        float cs = Mathf.Cos( rad );
        LateBlitComplete( tex, x, y, w, h, ox: cs, oy: sn, color: color, mat: mat );
    } else {
        LateBlitComplete( tex, x, y, w, h, color: color, mat: mat );
    }
}

public static void LateBlitComplete( Texture tex, float x, float y,
                                            float w = float.MaxValue, float h = float.MaxValue,
                                            float sx = 0, float sy = 0, float sw = 0, float sh = 0,
                                            float ox = float.MaxValue, float oy = float.MaxValue,
                                            Color? color = null, Material mat = null ) {
    tex = tex != null ? tex : _texWhite;

#if false
    var img = new LateImage {
        context = _context,

        x = x,
        y = y,
        w = w != float.MaxValue ? w : tex.width,
        h = h != float.MaxValue ? h : tex.height,

        sx = sx,
        sy = sy,
        sw = sw > 0 ? sw : tex.width,
        sh = sh > 0 ? sh : tex.height,

        ox = ox,
        oy = oy,

        color = color == null ? Color.white : color.Value,
        texture = tex,
        material = mat,
    };
    _lates.Add( img );
#endif

    int i = NewLate( LateType.Image, _context, color );
    _lates[i].x = x;
    _lates[i].y = y;
    _lates[i].w = w != float.MaxValue ? w : tex.width;
    _lates[i].h = h != float.MaxValue ? h : tex.height;
    _lates[i].sx = sx;
    _lates[i].sy = sy;
    _lates[i].sw = sw > 0 ? sw : tex.width;
    _lates[i].sh = sh > 0 ? sh : tex.height;
    _lates[i].ox = ox;
    _lates[i].oy = oy;
    _lates[i].texture = tex;
    _lates[i].material = mat;
}

// FIXME: LateDraw... is obsolete

public static void LateDrawLineLoopWorld( IList<Vector3> worldLine, Color? color = null ) {
    LateLineLoopWorld( worldLine, color );
}

public static void LateDrawRayWorld( Vector3 origin, Vector3 dir, Color? color = null ) {
    LateRayWorld( origin, dir, color );
}

public static void LateDrawLineLoop( IList<Vector2> line, Color? color = null ) {
    LateLineLoop( line, color );
}

public static void LateDrawLineWorld( Vector3 a, Vector3 b, Color? color = null ) {
    LateLineWorld( a, b, color );
}

public static void LateDrawLineWorld( IList<Vector3> worldLine, Color? color = null ) {
    LateLineWorld( worldLine, color );
}

public static void LateDrawLine( Vector2 a, Vector2 b, Color? color = null ) {
    LateLine( a, b, color );
}

public static void LateLineLoopWorld( IList<Vector3> worldLine, Color? color = null ) {
    _lineBuf.Clear();
    foreach ( var p in worldLine ) {
        _lineBuf.Add( WorldToScreenPos( p ) );
    }
    _lineBuf.Add( _lineBuf[0] );
    LateLine( _lineBuf, color );
}

public static void LateRayWorld( Vector3 origin, Vector3 dir, Color? color = null ) {
    LateLineWorld( origin, origin + dir, color );
}

public static void LateLineWorld( Vector3 a, Vector3 b, Color? color = null ) {
    LateLineWorld( new [] { a, b }, color );
}

public static void LatePointWorld( float x, float y, float size = 9, Color? color = null ) {
    LatePointWorld( new Vector3( x, y ), size, color );
}

public static void LatePointWorld( Vector3 pt, float size = 9, Color? color = null ) {
    Vector2 d = WorldToScreenPos( pt );
    Vector2 x = new Vector2( size, 0 );
    Vector2 y = new Vector2( 0, size );
    Vector2 ptx = d - x / 2;
    Vector2 pty = d - y / 2;
    LateLine( ptx, ptx + x, color );
    LateLine( pty, pty + y, color );
}

public static void LateCircleWorld( Vector2 center, float radius, Color? color = null ) {
    _lineBuf.Clear();

    Vector2 d = WorldToScreenPos( Vector2.zero ) - WorldToScreenPos( Vector2.one );
    float numIterations = Mathf.Floor( 0.1f * 2 * 3.14f * radius * Mathf.Abs(d.x));
    numIterations = Mathf.Max( 4, numIterations );
    float step = Mathf.PI * 2 / numIterations;
    for (float i = 0; i < numIterations; i++)
    {
        float a = i * step;
        Vector2 sc = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
        Vector2 p = WorldToScreenPos( center + sc * radius );
        _lineBuf.Add( p );
    }
    _lineBuf.Add( _lineBuf[0] );
    LateLine( _lineBuf, color );
}

public static void LateLineWorld( IList<Vector3> worldLine, Color? color = null ) {
    _lineBuf.Clear();
    foreach ( var p in worldLine ) {
        _lineBuf.Add( WorldToScreenPos( p ) );
    }
    LateLine( _lineBuf, color );
}

public static void LateLine( float ax, float ay, float bx, float by, Color? color = null ) {
    LateLine( new Vector2( ax, ay ), new Vector2( bx, by ), color );
}

public static void LateLine( Vector2 a, Vector2 b, Color? color = null ) {
    _linePair[0] = a;
    _linePair[1] = b;
    LateLine( _linePair, color );
}

public static void LateLineLoop( IList<Vector2> line, Color? color = null ) {
    _lineBuf.Clear();
    _lineBuf.AddRange( line );
    _lineBuf.Add( line[0] );
    LateLine( _lineBuf, color );
}

public static void LateLineRectWorld( float x, float y, float w, float h, Color? color = null ) {
    _lineRectWorld[0] = new Vector3( x, y );
    _lineRectWorld[1] = new Vector3( x + w, y );
    _lineRectWorld[2] = new Vector3( x + w, y + h );
    _lineRectWorld[3] = new Vector3( x, y + h );
    LateLineLoopWorld( _lineRectWorld, color );
}

public static void LateLineRect( float x, float y, float w, float h, Color? color = null ) {
    _lineRect[0] = new Vector2( x, y );
    _lineRect[1] = new Vector2( x + w, y );
    _lineRect[2] = new Vector2( x + w, y + h );
    _lineRect[3] = new Vector2( x, y + h );
    LateLineLoop( _lineRect, color );
}

public static void LateLine( IList<Vector2> line, Color? color = null ) {
#if false
    var ln = new LateLine {
        context = _context,
        color = color == null ? Color.white : color.Value,
    };
    ln.line.Clear();
    ln.line.AddRange( line );
    if ( _invertedY ) {
        for ( int i = 0; i < ln.line.Count; i++ ) {
            ln.line[i] = new Vector2( ln.line[i].x, ScreenHeight - ln.line[i].y );
        }
    }
    _lates.Add( ln );
#endif

    {
        int i = NewLate( LateType.Line, _context, color );
        var lateLine = _lates[i].line;
        lateLine.Clear();
        lateLine.AddRange( line );
        if ( _invertedY ) {
            for ( int j = 0; j < lateLine.Count; j++ ) {
                lateLine[j] = new Vector2( lateLine[j].x, ScreenHeight - lateLine[j].y );
            }
        }
    }
}

public static Vector2 WorldToScreenPos( Vector3 worldPos ) {
    Camera cam = _camera ?? Camera.main;

#if HAS_UNITY
    if ( ! _camera ) {
        Error("No camera, try to find one.");
        _camera = GameObject.FindObjectOfType<Camera>();
    }
#endif

    if ( cam ) {
        Vector2 pt = cam.WorldToScreenPoint( worldPos );
        pt.y = ScreenHeight - pt.y;
        return pt;
    }

    return Vector2.zero;
}

public static Vector3 ScreenToWorldPos( Vector2 screenPos ) {
    Camera cam = _camera ?? Camera.main;

#if HAS_UNITY
    if ( ! _camera ) {
        Error("No camera, try to find one.");
        _camera = GameObject.FindObjectOfType<Camera>();
    }
#endif

    if ( cam ) {
        screenPos.y = ScreenHeight - screenPos.y;
        return cam.ScreenToWorldPoint( screenPos );
    }

    return Vector2.zero;
}

// Lates after this call will be marked 'of this context'
public static void SetContext( Camera camera, float pixelsPerPoint = 1, bool invertedY = false ) {
    _camera = camera ?? Camera.main;
    _context = _camera ? _camera.GetHashCode() : -1;
    _invertedY = invertedY;
    PixelsPerPoint = pixelsPerPoint;
    UpdateScreenSize();
}

public static void Begin() {
    if ( ShowFontTexture_cvar > 0 ) {
        var tex = _currentFontInfo.tex ? _currentFontInfo.tex : Texture2D.whiteTexture;
        LateBlit( null, 0, 0, tex.width * ShowFontTexture_cvar, tex.height * ShowFontTexture_cvar,
                                                                            color: Color.magenta );
        LateBlit( tex, 0, 0, tex.width * ShowFontTexture_cvar, tex.height * ShowFontTexture_cvar );
    }
    GL.PushMatrix();
    GL.LoadPixelMatrix();
    UpdateScreenSize();
}

public static void End( bool skipLateFlush = false ) {
    if ( ! skipLateFlush ) {
        if ( _material ) {
            FlushLates();
        } else {
            Error( "Can't find GL material. Should call QGL.Start()" );
        }
    }
    _texMain = null;
    GL.PopMatrix();
}

public static void ClearLates() {
    _lateHead = _lateTail;
}

public static void FlushLates() {
    if ( _lateTail - _lateHead >= _lates.Length ) {
        Error( "Out of lates.");
        ClearLates();
        return;
    }

    int n = _lates.Length - 1;
    Late dummy = new();

    Late late(int i) {
        var result = _lates[i & n];
        return ( i < _lateTail && result.context == _context ) ? result : dummy;
    }

    int li = _lateHead;
    while ( true ) {
        int start;

        for ( ; late( li ).type == LateType.None; li++ ) {
            if ( li == _lateTail ) {
                goto done;
            }
        }

        // === gather texts ===

        for ( start = li; late( li ).type == LateType.Text; li++ )
        {}

        if ( start < li ) {
            SetFontTexture();
            GL.Begin( GL.QUADS );
            for ( int i = start; i < li; i++ ) {
                var lt = late( i );
                DrawTextWithOutline( lt.str, lt.x, lt.y, lt.color, lt.scale );
                DeleteLate( i );
            }
            GL.End();
        }

        // === gather nokia texts ===

        for ( start = li; late( li ).type == LateType.NokiaText; li++ )
        {}

        if ( start < li ) {
            SetTexture( NokiaFont.GetTexture() );
            GL.Begin( GL.QUADS );
            for ( int i = start; i < li; i++ ) {
                var lt = late( i );
                DrawTextNokia( lt.str, lt.x, lt.y, lt.color, lt.scale );
                DeleteLate( i );
            }
            GL.End();
        }

        // === gather images ===

        for ( start = li; late( li ).type == LateType.Image; li++ )
        {}

        if ( start < li ) {
            Texture tex = late( start ).texture;
            tex = tex != null ? tex : _texWhite;
            SetTexture( tex );
            GL.Begin( GL.QUADS );
            for ( int i = start; i < li; i++ ) {
                var lt = late( i );
                if ( tex != lt.texture ) {
                    GL.End();
                    tex = lt.texture != null ? lt.texture : _texWhite;
                    SetTexture( tex );
                    GL.Begin( GL.QUADS );
                }
                Vector2 srcPos = new Vector2( lt.sx, lt.sy );
                Vector2 srcSize = new Vector2( lt.sw, lt.sh );
                Vector2 dstPos = new Vector2( lt.x, lt.y );
                Vector2 dstSize = new Vector2( lt.w, lt.h );
                Vector2 dir = new Vector2( lt.ox, lt.oy );
                ImageQuad( lt.texture.width, lt.texture.height, srcPos, srcSize,
                                                            dstPos, dstSize, dir, lt.color );
                DeleteLate( i );
            }
            GL.End();
        }

        // === gather lines ===

        for ( start = li; late( li ).type == LateType.Line; li++ )
        {}

        if ( start < li ) {
            SetWhiteTexture();
            GL.Begin( GL.LINES );
            for ( int i = start; i < li; i++ ) {
                var lt = late( i );
                GL.Color( lt.color );
                for ( int j = 0; j < lt.line.Count - 1; j++ ) {
                    GL.Vertex( lt.line[j + 0] );
                    GL.Vertex( lt.line[j + 1] );
                }
                DeleteLate( i );
            }
            GL.End();
        }
    }

done:

    for ( int i = _lateHead; i < _lateTail; i++ ) {
        if ( IsValidLate( i ) ) {
            return;
        }
        _lateHead++;
    }

    ClearLates();
}

#if HAS_UNITY
// this will flush the lates and will invoke GL only if there are lates to flush
public static void OnGUIFull( bool invertedY = false ) {
    if ( _lateHead == _lateTail ) {
        return;
    }

    if ( Event.current.type != EventType.Repaint ) {
        return;
    }

    if ( !_material ) {
        Start( invertedY );
    }

    Begin();
    End();
}
#endif

static void UpdateScreenSize() {
    if ( _camera ) {
        ScreenWidth = _camera.pixelWidth;
        ScreenHeight = _camera.pixelHeight;
    } else if ( Camera.main ) {
        ScreenWidth = Camera.main.pixelWidth;
        ScreenHeight = Camera.main.pixelHeight;
    } else {
        ScreenWidth = Screen.width;
        ScreenHeight = Screen.height;
    }
}

static void AddCenteredText( string str, Vector2 sz, float x, float y, Color? color = null,
                                                                                float scale = 1 ) {
#if false
    var txt = new LateText {
        context = _context,
        x = Mathf.Round( x - ( int )sz.x / 2 ),
        y = Mathf.Round( y - ( int )sz.y / 2 ),
        scale = scale,
        str = str,
        color = color == null ? Color.green : color.Value,
    };
    _lates.Add( txt );
#endif

    int i = NewLate( LateType.Text, _context, color );
    _lates[i].x = Mathf.Round( x - ( int )sz.x / 2 );
    _lates[i].y = Mathf.Round( y - ( int )sz.y / 2 );
    _lates[i].scale = scale;
    _lates[i].str = str;
}

static void AddText( string str, float x, float y, Color? color = null, float scale = 1 ) {
#if false
    var txt = new LateText {
        context = _context,
        x = ( int )x,
        y = ( int )y,
        scale = scale,
        str = str,
        color = color == null ? Color.green : color.Value,
    };
    _lates.Add( txt );
#endif

    int i = NewLate( LateType.Text, _context, color );
    _lates[i].x = ( int )x;
    _lates[i].y = ( int )y;
    _lates[i].scale = scale;
    _lates[i].str = str;
}

static void ImageQuad( int texW, int texH, Vector2 srcPos, Vector2 srcSize,
                            Vector2 dstPos, Vector2 dstSize, Vector2 dir, Color color ) { 
    float y = _invertedY ? ScreenHeight - dstPos.y : dstPos.y;
    float tw = texW > 0 ? texW : 1;
    float th = texH > 0 ? texH : 1;
    float u0 = srcPos.x / tw;
    float u1 = u0 + srcSize.x / tw;

    float v0, v1;

    if (_invertedY)
    {
        v0 = srcPos.y / th;
        v1 = srcPos.y / th + srcSize.y / th;
    }
    else
    {
        // this is the way unity shows images if 'blitted'
        v0 = srcPos.y / th + srcSize.y / th;
        v1 = srcPos.y / th;
    }

    GL.Color( color );
    if ( dir.x != float.MaxValue && dir.y != float.MaxValue ) {
        Vector2 pos = new Vector2( dstPos.x, y );
        float dy = _invertedY ? -dstSize.y : +dstSize.y;
        Vector2 rv = new Vector2( dstSize.x, dy );
        // FIXME: use vector2 everywhere
        Vector3 rotate( float rx, float ry ) {
            rx *= rv.x * 0.5f;
            ry *= rv.y * 0.5f;
            return new Vector3( pos.x + rx * dir.x - ry * dir.y,
                                                            pos.y + rx * dir.y + ry * dir.x, 0 );
        }
        GL.TexCoord( new Vector3( u0, v0, 0 ) );
        GL.Vertex( rotate( -1, -1 ) );
        GL.TexCoord( new Vector3( u1, v0, 0 ) );
        GL.Vertex( rotate( 1, -1 ) );
        GL.TexCoord( new Vector3( u1, v1, 0 ) );
        GL.Vertex( rotate( 1, 1 ) );
        GL.TexCoord( new Vector3( u0, v1, 0 ) );
        GL.Vertex( rotate( -1, 1 ) );
    } else {
        float dy = _invertedY ? y - dstSize.y : y + dstSize.y;
        GL.TexCoord( new Vector3( u0, v0, 0 ) );
        GL.Vertex( new Vector3( dstPos.x, y, 0 ) );
        GL.TexCoord( new Vector3( u1, v0, 0 ) );
        GL.Vertex( new Vector3( dstPos.x + dstSize.x, y, 0 ) );
        GL.TexCoord( new Vector3( u1, v1, 0 ) );
        GL.Vertex( new Vector3( dstPos.x + dstSize.x, dy, 0 ) );
        GL.TexCoord( new Vector3( u0, v1, 0 ) );
        GL.Vertex( new Vector3( dstPos.x, dy, 0 ) );
    }
}

static void DrawText( string s, float x, float y ) {
    for ( int i = 0; i < s.Length; i++ ) {
        DrawScreenChar( s[i], x + i * TextDx, y, 1 );
    }
}

}


#endif
