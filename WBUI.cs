#if UNITY_STANDALONE || UNITY_2021_1_OR_NEWER || SDL

// #define QUI_USE_UNITY_UI
// #define QUI_USE_QGL

using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if SDL
using SDLPorts;
using GalliumMath;
#else
using UnityEngine;
#endif

#if QUI_USE_UNITY_UI
using UnityEngine.UI;
#endif

// Combines WrapBox and QUI into an Immediate mode UI library on top of Unity UI.
// It handles layout/anchoring and scaling using WrapBoxes.
// Exposes the QUI API but positions and sizes are WrapBoxes.

public static class WBUI {


public static int Hash( int handle, int lineNumber, string caller ) {
    return QUI.NextHashWg( QUI.HashWg( lineNumber, caller ), handle );
}

public static int Hash( WrapBox wbox, int handle, int lineNumber, string caller ) {
    handle = Hash( handle, lineNumber, caller );
    handle = QUI.NextHashWg( wbox.id, handle );
    return handle;
}

public static int Hash( WrapBox wbox, int handle ) {
    int id = 23;
    id = QUI.NextHashWg( id, wbox.id );
    id = QUI.NextHashWg( id, handle );
    return id;
}

public static WrapBox FitTexture( WrapBox wbox, Texture2D tex ) {
    float texR = ( float )tex.width / tex.height;
    float wboxR = ( float )wbox.W / wbox.H;
    float w = wbox.H * Mathf.Min( texR, wboxR );
    float h = w / texR;
    return wbox.Center( w, h );
}

public static bool CursorInRect( WrapBox wbox ) {
    return QUI.CursorInRect( wbox.x, wbox.y, wbox.w, wbox.h );
}

public static QUI.WidgetResult ClickRect_wg( WrapBox wbox, int handle = 0 ) {
    handle = Hash( wbox, handle );
    return QUI.ClickRect_wg( wbox.x, wbox.y, wbox.w, wbox.h, handle: handle );
}

public static QUI.WidgetResult ClickRect( WrapBox wbox, int handle = 0,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = Hash( wbox, handle, lineNumber, caller );
    return QUI.ClickRect_wg( wbox.x, wbox.y, wbox.w, wbox.h, handle: handle );
}

#if QUI_USE_QGL
public static void QGLText( string content, WrapBox wbox, int fontSize = 1, Color? color = null ) {
    fontSize = Mathf.Max( fontSize, 1 );
    Vector2Int sz = QGL.MeasureStringNokiaInt( content, scale: fontSize );
    wbox = wbox.Center( sz.x, sz.y );
    QGL.LatePrintNokia_tl( content, wbox.x, wbox.y, color: color, scale: fontSize );
}

public static void QGLTextOutlined( string content, WrapBox wbox, int align = 0,
                                                    int fontSize = 1, Color? color = null ) {
    fontSize = Mathf.Max( fontSize, 1 );
    Vector2Int sz = QGL.MeasureStringNokiaInt( content, scale: fontSize );
    WrapBox wbt = new WrapBox{ w = sz.x, h = sz.y };
    if ( align == 1 ) {
        wbox = wbox.TopLeft( wbt.W, wbt.H );
    } else if ( align == 2 ) {
        wbox = wbox.BottomLeft( wbt.W, wbt.H );
    } else if ( align == 3 ) {
        wbox = wbox.TopRight( wbt.W, wbt.H );
    } else if ( align == 4 ) {
        wbox = wbox.BottomRight( wbt.W, wbt.H );
    } else if ( align == 5 ) {
        wbox = wbox.TopCenter( wbt.W, wbt.H );
    } else if ( align == 6 ) {
        wbox = wbox.BottomCenter( wbt.W, wbt.H );
    } else {
        wbox = wbox.Center( wbt.W, wbt.H );
    }

    color = color != null ? color : Color.white;
    
    int [] offset = {
        0, -1,
        -1, -1,
        -1, 0,
        0, 1,
        1, 1,
        1, 0,
        -1, 1,
        1, -1,
    };

    var black = Color.black;
    black.a = color.Value.a * color.Value.a * color.Value.a;
    for ( int i = 0; i < offset.Length; i += 2 ) {
        QGL.LatePrintNokia_tl( content, wbox.x + offset[i + 0] * fontSize,
                                                                wbox.y + offset[i + 1] * fontSize,
                                                                    color: black, scale: fontSize );
    }
    QGL.LatePrintNokia_tl( content, wbox.x, wbox.y, color: color, scale: fontSize );
}
#endif

#if QUI_USE_UNITY_UI

public static void MeasuredText_wg( string content, WrapBox wbox, int handle,
                                            out float measureW, out float measureH,
                                            Font font = null, int fontSize = 0, 
                                            TextAnchor align = TextAnchor.UpperLeft,
                                            VerticalWrapMode overflow = VerticalWrapMode.Overflow,
                                            Color? color = null ) {
    handle = Hash( wbox, handle );
    fontSize = ( int )WrapBox.ScaleRound( fontSize );
    QUI.MeasuredText_wg( content, wbox.x, wbox.y, wbox.w, wbox.h, handle, out measureW, out measureH,
                                                font, fontSize, align, overflow, color );
    measureW /= WrapBox.canvasScale;
    measureH /= WrapBox.canvasScale;
}

public static int MeasuredText( string content, WrapBox wbox,
                                            out float measureW, out float measureH,
                                            Font font = null, int fontSize = 0, 
                                            TextAnchor align = TextAnchor.UpperLeft,
                                            VerticalWrapMode overflow = VerticalWrapMode.Overflow,
                                            Color? color = null,
                                            int handle = 0,
                                            [CallerLineNumber] int lineNumber = 0,
                                            [CallerMemberName] string caller = null ) {
    handle = Hash( wbox, handle, lineNumber, caller );
    MeasuredText_wg( content, wbox, handle, out measureW, out measureH, font, fontSize,
                                                                        align, overflow, color );
    return handle;
}

public static void Text_wg( string content, WrapBox wbox,
                                            Font font = null, int fontSize = 0, 
                                            TextAnchor align = TextAnchor.UpperLeft,
                                            VerticalWrapMode overflow = VerticalWrapMode.Overflow,
                                            Color? color = null,
                                            int handle = 0 ) {
    handle = Hash( wbox, handle );
    fontSize = ( int )WrapBox.ScaleRound( fontSize );
    QUI.Text_wg( content, wbox.x, wbox.y, wbox.w, wbox.h, font, fontSize, align, overflow,
                                                                                    color, handle );
}

public static void Text( string content, WrapBox wbox,
                                            Font font = null, int fontSize = 0, 
                                            TextAnchor align = TextAnchor.UpperLeft,
                                            VerticalWrapMode overflow = VerticalWrapMode.Overflow,
                                            Color? color = null,
                                            int handle = 0,
                                            [CallerLineNumber] int lineNumber = 0,
                                            [CallerMemberName] string caller = null ) {
    handle = Hash( wbox, handle, lineNumber, caller );
    fontSize = ( int )WrapBox.ScaleRound( fontSize );
    QUI.Text_wg( content, wbox.x, wbox.y, wbox.w, wbox.h, font, ( int )fontSize, align, overflow,
                                                                                    color, handle );
}

public static void FillRect( WrapBox wbox, Color? color = null, int handle = 0,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    QUI.Texture_wg( wbox.x, wbox.y, wbox.w, wbox.h, color: color,
                                                handle: Hash( wbox, handle, lineNumber, caller ) );
}

public static void Texture( WrapBox wbox, Texture2D tex = null, Color? color = null, int handle = 0,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    QUI.Texture_wg( wbox.x, wbox.y, wbox.w, wbox.h, tex: tex, color: color,
                                                handle: Hash( wbox, handle, lineNumber, caller ) );
}

public static void SpriteTex( WrapBox wbox, Texture2D tex = null,
                                                        Color? color = null,
                                                        bool scissor = false,
                                                        Vector4? border = null,
                                                        float ppuMultiplier = 1,
                                                        Image.Type type = Image.Type.Simple,
                                                        int handle = 0, 
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = Hash( wbox, handle, lineNumber, caller );
    ppuMultiplier = WrapBox.Scale( ppuMultiplier );
    QUI.SpriteTex_wg( wbox.x, wbox.y, wbox.w, wbox.h, handle, tex, color, scissor, border,
                                                                            ppuMultiplier, type );
}

public static void Sprite( WrapBox wbox, Sprite sprite, Color? color = null, bool scissor = false,
                                                        float ppuMultiplier = 1,
                                                        Image.Type type = Image.Type.Simple,
                                                        int handle = 0, 
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = Hash( wbox, handle, lineNumber, caller );
    ppuMultiplier /= WrapBox.canvasScale;
    QUI.Sprite_wg( wbox.x, wbox.y, wbox.w, wbox.h, sprite, handle, color, scissor, ppuMultiplier,
                                                                                            type );
}

public static void EnableScissor( WrapBox wbox, int handle = 0,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = Hash( wbox, handle, lineNumber, caller );
    QUI.EnableScissor_wg( wbox.x, wbox.y, wbox.w, wbox.h, handle );
}

public static void DisableScissor( int handle = 0, [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = Hash( handle, lineNumber, caller );
    QUI.DisableScissor_wg( handle );
}

public static WrapBox FromRectTransform( RectTransform rt ) {
    if ( ! QUI.canvas ) {
        return new WrapBox();
    }
    float x, y, w, h;
    Vector3 [] corners = new Vector3[4];
    rt.GetWorldCorners( corners );
    float minx = 999999;
    float maxx = 0;
    float miny = 999999;
    float maxy = 0;
    for ( int i = 0; i < corners.Length; i++ ) {
        var cam = QUI.canvas.worldCamera;
        var sp = corners[i];

        if ( cam ) {
            sp = cam.WorldToScreenPoint( corners[i] );
        }

        minx = Mathf.Min( sp.x, minx );
        miny = Mathf.Min( Screen.height - sp.y, miny );
        maxx = Mathf.Max( sp.x, maxx );
        maxy = Mathf.Max( Screen.height - sp.y, maxy );
    }
    x = minx;
    y = miny;
    w = maxx - minx;
    h = maxy - miny;
    return new WrapBox( x, y, w, h, id: rt.GetHashCode() );
}

#endif // Use unity UI


}

#endif // Has Unity

