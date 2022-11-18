// A library of routines to calculate and debug hex grids

//#define HEXES_QONSOLE
//#define HEXES_QGL

using System;
using System.Globalization;
using System.Collections.Generic;

#if UNITY_STANDALONE
using UnityEngine;
#else
using GalliumMath;
#endif


public static class Hexes {


public const float SQRT_3 = 1.73205080757f;

// we really hope this goes on the main thread
static Hexes() {
#if UNITY_STANDALONE
    CreateHexTexture();
#endif
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

// actually from square grid
public static Vector2Int ScreenToHex( Vector2 screenPos ) {
    screenPos *= SQRT_3;
    var q = SQRT_3/3f * screenPos.x  -  1f/3f * screenPos.y;
    var r =                            2f/3f * screenPos.y;
    return AxialRound( new Vector2( q, r ) );
}

// actually to square grid
public static Vector2 HexToScreen( int q, int r ) {
#if false
    float x = SQRT_3 * q + SQRT_3/2f * r;
    float y =                3f/2f * r;
    return new Vector2( x / SQRT_3, y / SQRT_3 );
#else
    float x = q + 1/2f * r;
    float y =    3f/2f * r;
    return new Vector2( x, y / SQRT_3 );
#endif
}

// actually to square grid
public static Vector2 HexToScreen( Vector2Int hex ) {
    return HexToScreen( hex.x, hex.y );
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


#if UNITY_STANDALONE

public static int hexSpriteWidth;
public static int hexSpriteHeight;
public static float hexSpriteAspect;
public static Texture2D hexSprite;

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

static Vector2 ShearAndScale( int x, int y, int gridHeight, Vector2 sz ) {
    Vector2 pos;
    pos.x = ( x + y * 0.5f ) * ( sz.x + ( int )( sz.x / 16 ) );
    pos.y = ( gridHeight - 1 - y ) * ( int )( sz.y * 0.83f );
    return pos;
}

#if HEXES_QGL
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
#endif // HEXES_QGL

#if HEXES_QONSOLE
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

    if ( ! hexSprite ) {
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

#endif // UNITY_STANDALONE


}
