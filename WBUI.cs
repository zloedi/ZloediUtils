#if UNITY_STANDALONE || UNITY_2021_0_OR_NEWER

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

// Combines WrapBox and QUI into an Immediate mode UI library on top of Unity UI

public static class WBUI {


public static int Hash( int handle, int lineNumber, string caller ) {
    return QUI.NextHashWg( QUI.HashWg( lineNumber, caller ), handle );
}

public static int Hash( WrapBox wbox, int handle, int lineNumber, string caller ) {
    handle = Hash( handle, lineNumber, caller );
    handle = QUI.NextHashWg( wbox.id, handle );
    return handle;
}

public static void Text( string content, WrapBox wbox,
                                            Font font = null, float fontSize = 20, 
                                            TextAnchor align = TextAnchor.UpperLeft,
                                            VerticalWrapMode overflow = VerticalWrapMode.Overflow,
                                            Color? color = null,
                                            int handle = 0,
                                            [CallerLineNumber] int lineNumber = 0,
                                            [CallerMemberName] string caller = null ) {
    handle = Hash( wbox, handle, lineNumber, caller );
    fontSize = WrapBox.ScaleRound( fontSize );
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

public static QUI.WidgetResult ClickRect( WrapBox wbox, int handle = 0,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    return QUI.ClickRect_wg( wbox.x, wbox.y, wbox.w, wbox.h,
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


}

#endif
