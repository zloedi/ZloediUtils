// A library of routines to calculate and debug hex grids

// define this to draw hex diagrams using QGL
//#define HEXES_QONSOLE

#if UNITY_STANDALONE || UNITY_2021_0_OR_NEWER
#define HAS_UNITY
#endif

using System;
using System.Globalization;
using System.Collections.Generic;

#if HAS_UNITY
using UnityEngine;
#elif SDL
using GalliumMath;
using SDLPorts;
#endif


public static class Hexes {


public const float SQRT_3 = 1.73205080757f;

// we really hope this goes on the main thread
static Hexes() {
    CreateHexTexture();
    CreateHexRegularTexture();
}


public static void Neighbours( Vector2Int hxc,
                            out Vector2Int n0,
                            out Vector2Int n1,
                            out Vector2Int n2,
                            out Vector2Int n3,
                            out Vector2Int n4,
                            out Vector2Int n5 ) {
    n0 = new Vector2Int( hxc.x + 1, hxc.y - 1 );
    n1 = new Vector2Int( hxc.x + 1, hxc.y - 0 );
    n2 = new Vector2Int( hxc.x - 0, hxc.y + 1 );
    n3 = new Vector2Int( hxc.x - 1, hxc.y + 1 );
    n4 = new Vector2Int( hxc.x - 1, hxc.y + 0 );
    n5 = new Vector2Int( hxc.x + 0, hxc.y - 1 );
}

public static Vector3Int AxialToCubeInt( Vector2Int axial ) {
    return AxialToCubeInt( axial.x, axial.y );
}

public static Vector3Int AxialToCubeInt( int q, int r ) {
    return new Vector3Int( q, -q - r, r ); 
}

public static Vector3 AxialToCube( float q, float r ) {
    return new Vector3( q, -q - r, r ); 
}

public static Vector3 AxialToCube( Vector2 axial ) {
    return AxialToCube( axial.x, axial.y );
}

public static Vector2Int CubeToAxial( Vector3 cube ) {
    return new Vector2Int( ( int )cube.x, ( int )cube.z );
}

public static Vector3 CubeRound( Vector3 cube ) {
    float rx = ( float )Math.Round( cube.x );
    float ry = ( float )Math.Round( cube.y );
    float rz = ( float )Math.Round( cube.z );

    float x_diff = ( float )Math.Abs( rx - cube.x );
    float y_diff = ( float )Math.Abs( ry - cube.y );
    float z_diff = ( float )Math.Abs( rz - cube.z );

    if ( x_diff > y_diff && x_diff > z_diff )
        rx = -ry-rz;
    else if ( y_diff > z_diff )
        ry = -rx-rz;
    else
        rz = -rx-ry;

    return new Vector3( rx, ry, rz );
}

public static Vector2Int AxialRound( Vector2 hex ) {
    return CubeToAxial( CubeRound( AxialToCube( hex ) ) );
}

public static int CubeDistance( Vector3Int a, Vector3Int b ) {
    var vec = a - b;
    return ( Mathf.Abs( vec.x ) + Mathf.Abs( vec.y ) + Mathf.Abs( vec.z ) ) / 2;
}

public static float CubeDistance( Vector3 a, Vector3 b ) {
    var vec = a - b;
    return ( Mathf.Abs( vec.x ) + Mathf.Abs( vec.y ) + Mathf.Abs( vec.z ) ) / 2f;
}

public static int AxialDistance( Vector2Int a, Vector2Int b ) {
    Vector3Int ca = AxialToCubeInt( a );
    Vector3Int cb = AxialToCubeInt( b );
    return CubeDistance( ca, cb );
}

// actually from square grid
// it uses the 'pointy-top-half-height' as base size, thus the *= sqrt_3
public static Vector2Int ScreenToHex( Vector2 screenPos, float size = 1 ) {
    screenPos /= size;
    screenPos *= SQRT_3;
    var q = SQRT_3/3f * screenPos.x - 1f/3f * screenPos.y;
    var r =                           2f/3f * screenPos.y;
    return AxialRound( new Vector2( q, r ) );
}

// actually to square grid
// it uses the 'pointy-top-half-height' as base size, thus the /= sqrt_3
public static Vector2 HexToScreen( int q, int r, float size = 1 ) {
#if false
    float x = size * ( SQRT_3 * q + SQRT_3/2f * r );
    float y = size * (                  3f/2f * r );
    return new Vector2( x / SQRT_3, y / SQRT_3 );
#else
    float x = size * ( q + 1/2f * r );
    float y = size * (    3f/2f * r );
    return new Vector2( x, y / SQRT_3 );
#endif
}

// actually to square grid
public static Vector2 HexToScreen( Vector2Int hex, float size = 1 ) {
    return HexToScreen( hex.x, hex.y, size );
}

public static Vector2Int OddRToAxial( int col, int row ) {
    var q = col - ( row - ( row & 1 ) ) / 2;
    var r = row;
    return new Vector2Int( q, r );
}

public static Vector2Int EvenRToAxial( int col, int row ) {
    var q = col - ( row + ( row & 1 ) ) / 2;
    var r = row;
    return new Vector2Int( q, r );
}


// == hexes visual stuff ==


#if HAS_UNITY || SDL

public static int hexSpriteWidth;
public static int hexSpriteHeight;
public static float hexSpriteAspect;
public static Texture2D hexSprite;

public static int hexSpriteRegularWidth;
public static int hexSpriteRegularHeight;
public static Texture2D hexSpriteRegular;

#if false
    str += "       @@       ";
    str += "     @@@@@@     ";
    str += "   @@@    @@@   ";
    str += " @@@        @@@ ";
    str += "@@            @@";
    str += "@@            @@";
    str += "@@            @@";
    str += "@@            @@";
    str += "@@            @@";
    str += "@@            @@";
    str += "@@            @@";
    str += "@@            @@";
    str += " @@@        @@@ ";
    str += "   @@@    @@@   ";
    str += "     @@@@@@     ";
    str += "       @@       ";
#endif

public static void CreateHexTexture() { 
    string str = "";
    string

      sz = "                ";

    str += "       @@       ";
    str += "     @@@@@@     ";
    str += "   @@@@@@@@@@   ";
    str += " @@@@@@@@@@@@@@ ";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += " @@@@@@@@@@@@@@ ";
    str += "   @@@@@@@@@@   ";
    str += "     @@@@@@     ";
    str += "       @@       ";

    hexSpriteWidth = sz.Length;
    hexSpriteHeight = str.Length / hexSpriteWidth;
    hexSpriteAspect = ( float )hexSpriteWidth / hexSpriteHeight;

    hexSprite = new Texture2D( hexSpriteWidth, hexSpriteHeight, textureFormat: TextureFormat.RGBA32, 
                                                                mipChain: false, 
                                                                linear: false); 
    hexSprite.filterMode = FilterMode.Point;
    for ( int y = 0, i = 0; y < hexSpriteHeight; y++ ) {
        for ( int x = 0; x < hexSpriteWidth; x++, i++ ) {
            int alpha = str[i] != ' ' ? 0xff : 0;
            hexSprite.SetPixel( x, y, new Color32( 0xff, 0xff, 0xff, ( byte )alpha ) );
        }
    }
    hexSprite.Apply();
}

public static void CreateHexRegularTexture() { 
    string str = "";
    string

      sz = "                ";

    str += "       @@       ";
    str += "     @@@@@@     ";
    str += "   @@@@@@@@@@   ";
    str += " @@@@@@@@@@@@@@ ";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += "@@@@@@@@@@@@@@@@";
    str += " @@@@@@@@@@@@@@ ";
    str += "   @@@@@@@@@@   ";
    str += "     @@@@@@     ";
    str += "       @@       ";

    hexSpriteRegularWidth = sz.Length;
    hexSpriteRegularHeight = str.Length / hexSpriteRegularWidth;
    hexSpriteAspect = ( float )hexSpriteRegularWidth / hexSpriteRegularHeight;

    hexSpriteRegular = new Texture2D( hexSpriteRegularWidth, hexSpriteRegularHeight,
                                                                textureFormat: TextureFormat.RGBA32, 
                                                                mipChain: false, 
                                                                linear: false); 
    hexSpriteRegular.filterMode = FilterMode.Point;
    for ( int y = 0, i = 0; y < hexSpriteRegularHeight; y++ ) {
        for ( int x = 0; x < hexSpriteRegularWidth; x++, i++ ) {
            int alpha = str[i] != ' ' ? 0xff : 0;
            hexSpriteRegular.SetPixel( x, y, new Color32( 0xff, 0xff, 0xff, ( byte )alpha ) );
        }
    }
    hexSpriteRegular.Apply();

    Log( "Created regular hex texture." );
}

static Vector2 ShearAndScale( int x, int y, int gridHeight, Vector2 sz ) {
    Vector2 pos;
    pos.x = ( x + y * 0.5f ) * ( sz.x + ( int )( sz.x / 16 ) );
    pos.y = ( gridHeight - 1 - y ) * ( int )( sz.y * 0.83f );
    return pos;
}

#if HAS_UNITY

static void WangsGenerate_cmd( string [] argv ) {
    Log( "Generating..." );
    string rootName = "HexWangsGenerated";
    GameObject root = null;
    Transform [] trans = GameObject.FindObjectsOfType<Transform>( includeInactive: true );
    for ( int i = trans.Length - 1; i >= 0; i-- ) {
        var t = trans[i];
        if ( t && t.name == rootName ) {
            Log( "Found old root, removing...", t );
            GameObject.DestroyImmediate( t.gameObject );
        }
    }
    root = new GameObject( rootName );
    MeshRenderer [] rends = GameObject.FindObjectsOfType<MeshRenderer>();
    GameObject [] prism = new GameObject[2];
    GameObject wall = null;
    foreach ( var r in rends ) {
        if ( r.gameObject.transform.parent.name.ToLowerInvariant().Contains( "prism" ) ) {
            Log( "Found prism mesh...", r.gameObject );
            prism[0] = r.gameObject.transform.parent.GetChild( 0 )?.gameObject;
            prism[1] = r.gameObject.transform.parent.GetChild( 1 )?.gameObject;
            break;
        }
    }
#if false
    foreach ( var r in rends ) {
        if ( r.gameObject.transform.parent.name.ToLowerInvariant().Contains( "wall_basic" ) ) {
            Log( "Found wall mesh...", r.gameObject );
            wall = r.gameObject.transform.parent.gameObject;
            break;
        }
    }
#endif
    if ( ! prism[0] || ! prism[1] ) {
        Log( "Needs a game object with 'prism' in its name and two prism mesh children, QUIT." );
        return;
    }
    var colors = new Color[2] {
        new Color32(18, 159, 251, 255),
        new Color32(247, 255, 0, 255),
    };
    Bounds bounds = prism[0].GetComponent<MeshRenderer>().bounds;
    var dz = bounds.min.z - prism[0].transform.position.z;
    Vector3 tip = new Vector3( 0, 0, dz );
    Log( $"Tip position: {tip}" );
    for ( int i = 0; i < ( 1 << 6 ); i++ ) {
        GameObject hex = new GameObject();
        hex.name = i.ToString( "D2" );
        hex.transform.parent = root.transform;
        const int maxSide = 8;
        int x = 2 * ( i % maxSide );
        int z = -2 * ( i / maxSide );
        hex.transform.localPosition = new Vector3( x, 0, z );
        for ( int side = 0; side < 6; side++ ) {
            int mask = 1 << side;
            bool bit = ( i & mask ) != 0;

            GameObject hexPrism = GameObject.Instantiate( prism[bit ? 1 : 0] );

            hexPrism.name = side.ToString();
            hexPrism.transform.parent = hex.transform;
            hexPrism.transform.localPosition = Vector3.zero;

            hexPrism.transform.RotateAround( hex.transform.position + tip, Vector3.up, 30 + 60 * side );
            hexPrism.transform.localPosition -= tip;
        }
        if ( wall ) {
            GameObject wi = GameObject.Instantiate( wall );
            wi.transform.parent = hex.transform;
            wi.transform.localPosition = Vector3.zero;
        }
    }
    Log( "Generated Hexagonal Wang Tiles Set." );
}

static void WangsAssignWalls_cmd( string [] argv ) {
    Transform [] trans = GameObject.FindObjectsOfType<Transform>( includeInactive: false );
    Transform [] walls = new Transform[64];
    Transform root = null;

    foreach ( var t in trans ) {
        if ( ! root && t.name == "HexWangsGenerated" ) {
            root = t;
        }

        if ( t.name.ToLowerInvariant().Contains( "wall_basic" )
                                                    && t.parent.parent.name == "HexWangsWalls" ) {
            int.TryParse( t.parent.name, out int idx );
            walls[idx & 63] = t;
        }
    }

    if ( ! root ) {
        Log( "Can't find root called HexWangsGenerated." );
    }

    for ( int i = 0; i < 64; i++ ) {

        if ( ! walls[i] ) {
            Log( $"Missing wall {i}" );
            continue;
        }

        Transform hex = root.GetChild( i );

        if ( hex ) {
            GameObject wall = GameObject.Instantiate( walls[i].gameObject );
            wall.name = "wall_basic_" + i.ToString( "D2" );
            wall.transform.parent = hex;
            wall.transform.localPosition = walls[i].localPosition;
        }
    }
}

static void Log( string s, UnityEngine.Object o ) {
#if HEXES_QONSOLE
    Qonsole.Log( s, o );
#else
    Debug.Log( s, o );
#endif
}

#endif // HAS_UNITY

static void Log( string s ) {
#if HEXES_QONSOLE
    Qonsole.Log( s );
#elif HAS_UNITY
    Debug.Log( s );
#else
    System.Console.Write( s );
#endif
}

#if HEXES_QONSOLE
const float _hexPts30 = ( float )( Math.PI * 2f / 12f );
const float _hexPts60 = ( float )( Math.PI * 2f / 6f );
static Vector2 [] _hexPts = new Vector2[6] {
    new Vector2 { x = Mathf.Cos( 0 * _hexPts60 ), y = Mathf.Sin( 0 * _hexPts60 ) },
    new Vector2 { x = Mathf.Cos( 1 * _hexPts60 ), y = Mathf.Sin( 1 * _hexPts60 ) },
    new Vector2 { x = Mathf.Cos( 2 * _hexPts60 ), y = Mathf.Sin( 2 * _hexPts60 ) },
    new Vector2 { x = Mathf.Cos( 3 * _hexPts60 ), y = Mathf.Sin( 3 * _hexPts60 ) },
    new Vector2 { x = Mathf.Cos( 4 * _hexPts60 ), y = Mathf.Sin( 4 * _hexPts60 ) },
    new Vector2 { x = Mathf.Cos( 5 * _hexPts60 ), y = Mathf.Sin( 5 * _hexPts60 ) },
};
static Vector2 [] _hexPtsPointy = new Vector2[6] {
    new Vector2 { x = Mathf.Cos( _hexPts30 + 0 * _hexPts60 ), y = Mathf.Sin( _hexPts30 + 0 * _hexPts60 ) },
    new Vector2 { x = Mathf.Cos( _hexPts30 + 1 * _hexPts60 ), y = Mathf.Sin( _hexPts30 + 1 * _hexPts60 ) },
    new Vector2 { x = Mathf.Cos( _hexPts30 + 2 * _hexPts60 ), y = Mathf.Sin( _hexPts30 + 2 * _hexPts60 ) },
    new Vector2 { x = Mathf.Cos( _hexPts30 + 3 * _hexPts60 ), y = Mathf.Sin( _hexPts30 + 3 * _hexPts60 ) },
    new Vector2 { x = Mathf.Cos( _hexPts30 + 4 * _hexPts60 ), y = Mathf.Sin( _hexPts30 + 4 * _hexPts60 ) },
    new Vector2 { x = Mathf.Cos( _hexPts30 + 5 * _hexPts60 ), y = Mathf.Sin( _hexPts30 + 5 * _hexPts60 ) },
};
static Vector2 [] _hexPtsBuf = new Vector2[6];
public static void DrawHexWithLines( Vector2 screenPos, float diameter, Color c ) {
    float r = diameter / SQRT_3;
    for ( int i = 0; i < 6; i++ ) {
        _hexPtsBuf[i] = _hexPtsPointy[i] * r + screenPos;
    }
    QGL.LateDrawLineLoop( _hexPtsBuf, color: c );
}

public static void DrawGLHex( Vector2 screenPos, int x, int y, int gridHeight, Vector2 sz,
                                                                            float consoleAlpha,
                                                                            Color? color = null ) {
    Vector2 pos = ShearAndScale( x, y, gridHeight, sz );
    Color c = color != null ? color.Value : new Color( 1, 0.65f, 0.1f );
    c.a *= consoleAlpha;
    GL.Color( c );
    QGL.DrawQuad( screenPos + pos, sz );
}

public static void DrawGLText( string logText, Vector2 screenPos, int x, int y, int h, Vector2 sz ) {
    Vector2 strSz = QGL.MeasureString( logText );
    Vector2 pos = ShearAndScale( x, y, h, sz );
    pos += screenPos;
    pos += ( sz - strSz ) * 0.5f;
    QGL.DrawTextWithOutline( logText, ( int )pos.x, ( int )pos.y, Color.white );
}

public static void PrintList( IList<ushort> list, int gridW, int gridH, string logText = null,
                                        float hexSize = 0,
                                        bool isOffset = false,
                                        IList<Color> colors = null,
                                        IList<string> texts = null,
                                        Func<IList<Vector2Int>,int,string> hexListString = null ) {
    var coords = new Vector2Int[list.Count];
    for ( int i = 0; i < list.Count; i++ ) {
        coords[i] = new Vector2Int( list[i] % gridW, list[i] / gridW );
    }
    PrintList( coords, logText, hexSize, gridW, gridH, isOffset, colors, texts, hexListString );
}

public static void PrintList( IList<Vector2Int> list, string logText = null, float hexSize = 0,
                    int gridW = -1, int gridH = -1, bool isOffset = false,
                    IList<Color> colors = null,
                    IList<string> texts = null,
                    Func<IList<Vector2Int>,int,string> hexListString = null ) {
    if ( logText != null ) {
        Qonsole.Log( logText );
    }

    if ( ! hexSprite) {
        CreateHexTexture();
    }

    // we shouldn't use texture.width/height, since this could be called from another thread
    // and texture access is allowed only on the main thread
    hexSize = hexSize <= 0 ? hexSpriteWidth : hexSize;
    Vector2 quadSize = new Vector2( hexSize, hexSize * hexSpriteAspect );
    hexListString = hexListString != null ? hexListString : (l,i) => i.ToString();

    var captureList = new List<Vector2Int>( list );
    var captureColors = colors != null ? new List<Color>( colors ) : null;
    var captureTexts = texts != null ? new List<string>( texts ) : null;
    
    if ( gridW < 0 || gridH < 0 ) {
        int maxX = 0, maxY = 0;
        foreach ( var p in captureList ) {
            maxX = p.x > maxX ? p.x : maxX;
            maxY = p.y > maxY ? p.y : maxY;
        }
        gridW = maxX + 1;
        gridH = maxY + 1;
    }

    Qonsole.PrintAndAct( "\n", (screenPos,alpha) => {
        QGL.SetTexture( hexSprite );
        GL.Begin( GL.QUADS );
#if false
        for ( int y = 0; y < gridH; y++ ) {
            for ( int x = 0; x < gridW; x++ ) {
                DrawGLHex( screenPos, x, y, gridH, quadSize, alpha, 0.4f );
            }
        }
#endif

        if ( isOffset ) {
            for ( int i = 0; i < captureList.Count; i++ ) {
                var p = captureList[i];
                var c = captureColors != null ? captureColors[i] : new Color( 1, 0.65f, 0.1f );
                Vector2Int q = OddRToAxial( p.x, p.y );
                DrawGLHex( screenPos, q.x, q.y, gridH, quadSize, alpha, c );
            }
        } else {
            for ( int i = 0; i < captureList.Count; i++ ) {
                var p = captureList[i];
                var c = captureColors != null ? captureColors[i] : new Color( 1, 0.65f, 0.1f );
                DrawGLHex( screenPos, p.x, p.y, gridH, quadSize, alpha, c );
            }
        }

        GL.End();

        // draw hex text
        QGL.SetFontTexture();
        GL.Begin( GL.QUADS );
        if ( isOffset ) {
            for ( int i = 0; i < captureList.Count; i++ ) {
                Vector2Int p = captureList[i];
                Vector2Int q = OddRToAxial( p.x, p.y );
                string txt = captureTexts != null ? captureTexts[i] : hexListString( captureList, i );
                DrawGLText( txt, screenPos, q.x, q.y, gridH, quadSize );
            }
        } else {
            for ( int i = 0; i < captureList.Count; i++ ) {
                Vector2Int p = captureList[i];
                string txt = captureTexts != null ? captureTexts[i] : hexListString( captureList, i );
                DrawGLText( hexListString( captureList, i ), screenPos, p.x, p.y, gridH, quadSize );
            }
        }
        GL.End();
    } );
    float hexH = ( int )( quadSize.y * 0.83f );
    int numLines = ( int )( gridH * hexH / Qonsole.LineHeight() + 0.5f );
    for ( int i = 0; i < numLines; i++ ) {
        Qonsole.Print( "\n" );
    }
}

#if false // example usage
static void PrintHexes_kmd( string [] argv ) {
    PrintList( x_grid, isOffset: true, hexListString: (l,i) => $"{l[i].x},{l[i].y}", hexSize: 48 );
}
#endif

#endif // HEXES_QONSOLE

#endif // HAS_UNITY


}
