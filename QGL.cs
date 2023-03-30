#if UNITY_STANDALONE || UNITY_2021_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;


public static class QGL {


static Texture2D _texWhite = Texture2D.whiteTexture;
static Material _material;

static int _context;
static Camera _camera;
static bool _invertedY;

struct LateLine {
    public int context;
    public List<Vector2> line;
    public Color color;
}

struct LateText {
    public int context;
    public float x, y;
    public float scale;
    public string str;
    public Color color;
}

struct LateImage {
    public int context;
    public float x, y, w, h;
    public Color color;
    public Texture2D texture;
    public Material material;
}

// these are postponed and drawn after all geometry in scene
static List<LateText> _texts = new List<LateText>();
static List<LateImage> _images = new List<LateImage>();
static List<LateLine> _lines = new List<LateLine>();

static void BlitSlow( Texture2D texture, Vector2 srcPos, Vector2 srcSize, Vector3 dstPos,
                                Vector3 dstSize, Color? color = null, Material material = null) { 
    Color col = color == null ? Color.white : color.Value;
    texture = texture ? texture : _texWhite;
    float y = _invertedY ? ScreenHeight() - dstPos.y : dstPos.y;
    float dy = _invertedY ? y - dstSize.y : y + dstSize.y;
    float tw = texture.width > 0 ? texture.width : 1;
    float th = texture.height > 0 ? texture.height : 1;
    float u0 = srcPos.x / tw;
    float u1 = u0 + srcSize.x / tw;
    float v0 = 1 - srcPos.y / th;
    float v1 = v0 - srcSize.y / th;

    GL.PushMatrix();
    if ( material != null ) {
        material.SetPass(0);
        material.SetColor("_Color", color.Value);
    } else {
        SetTexture( texture );
    }
    GL.LoadPixelMatrix();
    GL.Begin( GL.QUADS );
    GL.Color( col );
    GL.TexCoord( new Vector3( u0, v0, 0 ) );
    GL.Vertex( new Vector3( dstPos.x, y, 0 ) );
    GL.TexCoord( new Vector3( u1, v0, 0 ) );
    GL.Vertex( new Vector3( dstPos.x + dstSize.x, y, 0 ) );
    GL.TexCoord( new Vector3( u1, v1, 0 ) );
    GL.Vertex( new Vector3( dstPos.x + dstSize.x, dy, 0 ) );
    GL.TexCoord( new Vector3( u0, v1, 0 ) );
    GL.Vertex( new Vector3( dstPos.x, dy, 0 ) );
    GL.End();
    GL.PopMatrix();
}

static void DrawText( string s, float x, float y ) {
    for ( int i = 0; i < s.Length; i++ ) {
        DrawScreenChar( s[i], x + i * TextDx, y, 1 );
    }
}

static Color TagToCol( string tag ) {
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

static int _font => _fonts == null ? 0 : Font_cvar % _fonts.Length;
static int _fontNumColumns => _font == 0 ? AppleFont.APPLEIIF_CLMS : CodePage437.FontSz;
static int _fontNumRows    => _font == 0 ? AppleFont.APPLEIIF_ROWS : CodePage437.FontSz;
static int _fontCharWidth  => _font == 0 ? AppleFont.APPLEIIF_CW   : CodePage437.CharSz;
static int _fontCharHeight => _font == 0 ? AppleFont.APPLEIIF_CH   : CodePage437.CharSz;

static int GetCharInFont( int c ) {
    return c % ( _fontNumColumns * _fontNumRows );
}

// == Public API ==

static int CharSpacingX_cvar = -3;
static int CharSpacingY_cvar = 3;
static int Font_cvar = 0;

public static float pixelsPerPoint = 1;

public static float TextDx => Mathf.Max( AppleFont.APPLEIIF_CW + 1, _fontCharWidth + CharSpacingX_cvar );
public static float TextDy => _fontCharHeight + CharSpacingY_cvar;

public static int GetCursorChar() {
    return _font == 0 ? 127 : 0xdb;
}

public static float ScreenWidth() {
    Camera cam = _camera ? _camera : Camera.main;
    if ( cam ) {
        return cam.pixelWidth;
    }
    return Screen.width;
}

public static float ScreenHeight() {
    Camera cam = _camera ? _camera : Camera.main;
    if ( cam ) {
        return cam.pixelHeight;
    }
    return Screen.height;
}

public static bool Start( bool invertedY = false ) {
    Shader shader = Shader.Find( "GLSprites" );
    if ( shader ) {
        _material = new Material( shader );
        _material.hideFlags = HideFlags.HideAndDontSave;
        SetContext( null, invertedY: invertedY );
        return true;
    }
    Debug.LogError( "Can't find GL shader" );
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

public static void DrawTextWithOutline( string s, float x, float y, Color color, float scale = 1 ) {
    for ( int i = 0, j = 0; i < s.Length; i++ ) {
        if ( s[i] == '\n' ) {
            j = 0;
            y += TextDy * scale;
        } else {
            DrawScreenCharWithOutline( s[i], x + j * TextDx * scale, y, color, scale );
            j++;
        }
    }
}

public static void DrawScreenCharWithOutline( int c, float screenX, float screenY, Color color,
                                                                                float scale = 1 ) { 
    // == outline ==
    Vector2 [] outline = new Vector2 [] {
        new Vector3( scale, 0 ),
        new Vector3( 0, scale ),
        new Vector3( scale, scale ),
        new Vector3( -scale, scale ),
    };

    GL.Color( new Color( 0, 0, 0, 1 * ( color.a * color.a * color.a ) ) );
    for ( int i = 0; i < outline.Length; i++ ) {
        DrawScreenChar( c, screenX + outline[i].x, screenY + outline[i].y, scale );
        DrawScreenChar( c, screenX - outline[i].x, screenY - outline[i].y, scale );
    }

    // == actual character ==
    GL.Color( color );
    DrawScreenChar( c, screenX, screenY, scale );
}

static Texture2D _texFont;
static Texture2D [] _fonts;

public static void SetFontTexture() {
    // make sure we work when going back to edit mode
    _texFont = _fonts == null ? null : _fonts[_font];
    if ( ! _texFont ) {
        _fonts = new Texture2D [] {
            AppleFont.GetTexture(),
            CodePage437.GetTexture(),
        };
        _texFont = _fonts[_font];
    }
    SetTexture( _texFont );
}

public static void SetWhiteTexture() {
    SetTexture( _texWhite );
}

public static void SetMaterialColor( Color color ) {
    _material.color = color;
}

public static void SetTexture( Texture2D tex ) {
    _material.SetTexture( "_MainTex", tex );
    _material.SetPass( 0 );
}

public static void DrawQuad( Vector2 pos, Vector2 size,
                                            Vector2? srcOrigin = null, Vector2? srcSize = null ) { 
    Vector2 uv0 = srcOrigin != null ? srcOrigin.Value : Vector2.zero;
    Vector2 uv1 = srcSize != null ? ( uv0 + srcSize.Value ) : Vector2.one;

    float y = _invertedY ? ScreenHeight() - pos.y : pos.y;
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
    float y = _invertedY ? ScreenHeight() - pos.y : pos.y;
    float dy = _invertedY ? y - size.y : y + size.y;
    
    GL.Vertex( new Vector3( pos.x, y, 0 ) );
    GL.Vertex( new Vector3( pos.x + size.x, y, 0 ) );
    GL.Vertex( new Vector3( pos.x + size.x, dy, 0 ) );
    GL.Vertex( new Vector3( pos.x, dy, 0 ) );
}

struct CharInfo {
    public Vector3 [] uv;
    public Vector3 [] verts;
}
static Dictionary<int,CharInfo> _charsMap = new Dictionary<int,CharInfo>();
public static void DrawScreenChar( int c, float screenX, float screenY, float scale ) { 
    int idx = GetCharInFont( c );

    float y = _invertedY ? ScreenHeight() - screenY : screenY;
    Vector3 vertOff = new Vector3( screenX, y );

    int a = _font | ( ( _invertedY ? 1 : 0 ) << 8 );
    int b = idx;
    int hash = ( a + b ) * ( ( a + b + 1 ) >> 1 ) + a;
    if ( ! _charsMap.TryGetValue( hash, out CharInfo ci ) ) {
        float tw = ( float )_fontCharWidth / _texFont.width;
        float th = ( float )_fontCharHeight / _texFont.height;
        Vector3 uvOff = new Vector3( ( idx % _fontNumColumns ) * tw, ( idx / _fontNumColumns ) * th );

        float charU = ( float )_texFont.width / _fontNumColumns / _texFont.width;
        float charV = ( float )_texFont.height / _fontNumRows / _texFont.height;
        ci.uv = new Vector3[4] {
            new Vector3( 0, 0, 0 ),
            new Vector3( charU, 0, 0 ),
            new Vector3( charU, charV, 0 ),
            new Vector3( 0, charV, 0 ),
        };

        for ( int i = 0; i < 4; i++ ) {
            ci.uv[i] += uvOff;
        }

        if ( _invertedY ) {
            ci.verts = new Vector3[4] {
                new Vector3( 0, 0, 0 ),
                new Vector3( _fontCharWidth, 0, 0 ),
                new Vector3( _fontCharWidth, -_fontCharHeight, 0 ),
                new Vector3( 0, -_fontCharHeight, 0 ),
            };
        } else {
            ci.verts = new Vector3[4] {
                new Vector3( 0, 0, 0 ),
                new Vector3( _fontCharWidth, 0, 0 ),
                new Vector3( _fontCharWidth, _fontCharHeight, 0 ),
                new Vector3( 0, _fontCharHeight, 0 ),
            };
        }

        _charsMap[hash] = ci;
    }

    for ( int i = 0; i < 4; i++ ) {
        GL.TexCoord( ci.uv[i] );
        GL.Vertex( ci.verts[i] * scale + vertOff );
    }
}

public static Vector2 LatePrintWorld( object o, Vector3 worldPos, Color? color = null,
                                                                                float scale = 1 ) {
    return LatePrintWorld( o.ToString(), worldPos, color, scale );
}

public static Vector2 LatePrintWorld( string str, Vector3 worldPos, Color? color = null,
                                                                                float scale = 1 ) {
    Vector2 pt = WorldToScreenPos( worldPos );
    LatePrint( str, pt.x, pt.y, color, scale );
    return pt;
}
    
public static void LatePrint( object o, Vector2 xy, Color? color = null, float scale = 1 ) {
    LatePrint( o.ToString(), xy.x, xy.y, color, scale );
}

public static void LatePrint( object o, float x, float y, Color? color = null, float scale = 1 ) {
    LatePrint( o.ToString(), x, y, color, scale );
}

public static void LatePrint( string str, float x, float y, Color? color = null, float scale = 1 ) {
    Vector2 sz = MeasureString( str, scale );
    _texts.Add( new LateText {
        context = _context,
        x = ( int )( x - sz.x / 2f ),
        y = ( int )( y - sz.y / 2f ),
        scale = scale,
        str = str,
        color = color == null ? Color.green : color.Value,
    } );
}

public static void LatePrint_tl( object o, Vector2 xy, Color? color = null, float scale = 1 ) {
    LatePrint_tl( o.ToString(), xy.x, xy.y, color, scale );
}

public static void LatePrint_tl( object o, float x, float y, Color? color = null, float scale = 1 ) {
    LatePrint_tl( o.ToString(), x, y, color, scale );
}

public static void LatePrint_tl( string str, float x, float y, Color? color = null, float scale = 1 ) {
    Vector2 sz = MeasureString( str, scale );
    _texts.Add( new LateText {
        context = _context,
        x = ( int )x,
        y = ( int )y,
        scale = scale,
        str = str,
        color = color == null ? Color.green : color.Value,
    } );
}

public static void LatePrintFlush() {
    SetFontTexture();
    GL.Begin( GL.QUADS );
    foreach ( var s in _texts ) {
        if ( s.context == _context ) {
            DrawTextWithOutline( s.str, s.x, s.y, s.color, s.scale );
        }
    }
    GL.End();
    for ( int i = _texts.Count - 1; i >= 0; i-- ) {
        if ( _texts[i].context == _context ) {
            _texts.RemoveAt( i );
        }
    }
}

public static Vector2 LateBlitWorld( Texture2D tex, Vector3 worldPos, float w, float h ) {
    Vector2 pt = WorldToScreenPos( worldPos );
    LateBlit( tex, pt.x, pt.y, w, h );
    return pt;
}

public static void LateBlit( Vector2 xy, Vector2 sz, Color? color = null ) {
    LateBlit( null, xy, sz.x, sz.y, color );
}

public static void LateBlit( Texture2D tex, Vector2 xy, float w, float h, Color? color = null ) {
    LateBlit( tex, xy.x, xy.y, w, h, color );
}

public static void LateBlit( float x, float y, float w, float h, Color? color = null ) {
    LateBlit( null, x, y, w, h, color );
}

public static void LateBlit( Texture2D tex, float x, float y, float w, float h, Color? color = null,
                                                                            Material mat = null ) {
    _images.Add( new LateImage {
        context = _context,
        x = x,
        y = y,
        w = w,
        h = h,
        color = color == null ? Color.white : color.Value,
        texture = tex != null ? tex : _texWhite,
        material = mat,
    } );
}

public static void LateBlitFlush() {
    foreach ( var i in _images ) {
        if ( i.context == _context ) {
            Vector2 srcPos = new Vector2( 0, 0 );
            Vector2 srcSize = new Vector2( i.texture.width, i.texture.height );
            Vector2 dstPos = new Vector2( i.x, i.y );
            Vector2 dstSize = new Vector2( i.w, i.h );
            BlitSlow( i.texture, srcPos, srcSize, dstPos, dstSize, i.color, i.material );
        }
    }
    for ( int i = _images.Count - 1; i >= 0; i-- ) {
        if ( _images[i].context == _context ) {
            _images.RemoveAt( i );
        }
    }
}

public static void LateDrawLineLoopWorld( IList<Vector3> worldLine, Color? color = null ) {
    List<Vector2> l = new List<Vector2>();
    foreach ( var p in worldLine ) {
        l.Add( WorldToScreenPos( p ) );
    }
    LateDrawLineLoop( l, color );
}

public static void LateDrawLineWorld( Vector3 a, Vector3 b, Color? color = null ) {
    LateDrawLineWorld( new [] { a, b }, color );
}

public static void LateDrawLineWorld( IList<Vector3> worldLine, Color? color = null ) {
    List<Vector2> l = new List<Vector2>();
    foreach ( var p in worldLine ) {
        l.Add( WorldToScreenPos( p ) );
    }
    LateDrawLine( l, color );
}

public static void LateDrawLine( float ax, float ay, float bx, float by, Color? color = null ) {
    LateDrawLine( new Vector2( ax, ay ), new Vector2( bx, by ), color );
}

public static void LateDrawLine( Vector2 a, Vector2 b, Color? color = null ) {
    LateDrawLine( new [] { a, b }, color );
}

public static void LateDrawLineLoop( IList<Vector2> line, Color? color = null ) {
    List<Vector2> loop = new List<Vector2>( line );
    loop.Add( line[0] );
    LateDrawLine( loop, color );
}

private static Vector2 [] _lineRect = new Vector2[4];
public static void LateDrawLineRect( float x, float y, float w, float h, Color? color = null ) {
    _lineRect[0] = new Vector2( x, y );
    _lineRect[1] = new Vector2( x + w, y );
    _lineRect[2] = new Vector2( x + w, y + h );
    _lineRect[3] = new Vector2( x, y + h );
    LateDrawLineLoop( _lineRect, color );
}

public static void LateDrawLine( IList<Vector2> line, Color? color = null ) {
    var l = new List<Vector2>( line );
    for ( int i = 0; i < l.Count; i++ ) {
        float y = _invertedY ? ScreenHeight() - l[i].y : l[i].y;
        l[i] = new Vector2( l[i].x, y );
    }
    _lines.Add( new LateLine {
        context = _context,
        line = l,
        color = color == null ? Color.white : color.Value,
    } );
}

public static void LateDrawLineFlush() {
    SetWhiteTexture();
    GL.Begin( GL.LINES);
    foreach ( var l in _lines ) {
        if ( l.context == _context ) {
            GL.Color( l.color );
            for ( int i = 0; i < l.line.Count - 1; i++ ) {
                GL.Vertex( l.line[i + 0] );
                GL.Vertex( l.line[i + 1] );
            }
        }
    }
    GL.End();
    for ( int i = _lines.Count - 1; i >= 0; i-- ) {
        if ( _lines[i].context == _context ) {
            _lines.RemoveAt( i );
        }
    }
}

public static Vector2 WorldToScreenPos( Vector3 worldPos ) {
    Camera cam = _camera ? _camera : Camera.main;
    if ( cam ) {
        Vector2 pt = cam.WorldToScreenPoint( worldPos );
        pt.y = ScreenHeight() - pt.y;
        return pt;
    }
    return Vector2.zero;
}

public static void SetContext( Camera camera, float pixelsPerPoint = 1, bool invertedY = false ) {
    _context = camera ? camera.GetHashCode() : 0;
    _camera = camera;
    _invertedY = invertedY;
    QGL.pixelsPerPoint = pixelsPerPoint;
}

public static void Begin() {
    GL.PushMatrix();
    GL.LoadPixelMatrix();
}

public static void End( bool skipLateFlush = false ) {
    if ( ! skipLateFlush ) {
        if ( _material ) {
            LateBlitFlush();
            LatePrintFlush();
            LateDrawLineFlush();
        } else {
            Debug.LogError( "Can't find GL material. Should call QGL.Start()" );
        }
    }
    GL.PopMatrix();
}


}


#endif // UNITY
