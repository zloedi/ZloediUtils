// define this to have Unity UI API support
//#define QUI_USE_UNITY_UI

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if QUI_USE_UNITY_UI
using UnityEngine;
using UnityEngine.UI;
#endif

public static class QUI {
 

enum MouseButtonState {
    None,
    Down = 1 << 0,
    Up = 1 << 1,
}

public enum WidgetResult {
    Idle,
    Hot,
    Pressed,
    Active,
    Released,
    Count,
}

struct UIRect {
    public float x, y, w, h;
    public int handle;
    public bool scissor;
}

public static Action<float,float,float,float> DrawLineRect = (x,y,w,h)=>{};
public static Action<string> Log = (s) => {};
public static Action<string> Error = (s) => {};

public static bool showRects;

static List<UIRect> _rects = new List<UIRect>();
static MouseButtonState _mouseButton;

// this widget is hovered over
public static int hotWidget;
// this widget is usually pressed but not released yet
public static int activeWidget;

public static float cursorX;
public static float cursorY;
static float _oldCursorX;
static float _oldCursorY;

static bool CursorInRect( float x, float y, float w, float h ) {
    return cursorX >= x && cursorY >= y && cursorX < x + w && cursorY < y + h;
}

// this callback might be called multiple times inside a single frame
public static bool OnMouseButton( bool down ) {
    if ( down ) {
        _mouseButton |= MouseButtonState.Down;
    } else {
        _mouseButton |= MouseButtonState.Up;
    }
    return hotWidget != 0 || activeWidget != 0;
}

// assumes top-left origin for mouse
public static void Begin( float inCursorX, float inCursorY ) {
    cursorX = inCursorX;
    cursorY = inCursorY;
#if QUI_USE_UNITY_UI
    BeginUnityUI();
#endif
}

static UIRect Clip( UIRect r, UIRect clip ) {
    float x0 = Math.Max( clip.x, r.x );
    float y0 = Math.Max( clip.y, r.y );
    float x1 = Math.Min( clip.x + clip.w, r.x + r.w );
    float y1 = Math.Min( clip.y + clip.h, r.y + r.h );
    return new UIRect {
        x = x0,
        y = y0,
        w = Math.Max( 0, x1 - x0 ),
        h = Math.Max( 0, y1 - y0 ),
        handle = r.handle,
    };
}

public static void End( bool skipUnityUI = false ) {
    bool BUTTON_RELEASED() {
        return ( ( _mouseButton & MouseButtonState.Up ) != 0
                || _mouseButton == MouseButtonState.None );
    }

    UIRect s = new UIRect { x = 0, y = 0, w = 99999, h = 99999 };
    for ( int i = 0; i < _rects.Count; i++ ) {
        if ( _rects[i].scissor ) {
            s = _rects[i];
        } else {
            _rects[i] = Clip( _rects[i], s );
        }
    }

    if ( showRects ) {
        foreach ( var r in _rects ) {
            if ( r.w > 0 && r.h > 0 ) {
                DrawLineRect( r.x, r.y, r.w, r.h );
            }
        }
    }

    // check if 'active' widget is still present on screen

    if ( activeWidget != 0 ) {
        int i;
        for ( i = 0; i < _rects.Count; i++ ) {
            if ( _rects[i].handle == activeWidget ) {
                break;
            }
        }

        // 'active' is no longer drawn/tested
        if ( i == _rects.Count ) {
            activeWidget = 0;
        }
    }

    // go back-to-front in the rectangles stack and check for hits
    hotWidget = 0;
    for ( int i = _rects.Count - 1; i >= 0; i-- ) {
        UIRect r = _rects[i];
        if ( ! r.scissor && CursorInRect( r.x, r.y, r.w, r.h ) ) {
            if ( BUTTON_RELEASED() ) {
                // there is only one hot widget
                activeWidget = 0;
                hotWidget = r.handle;
            } else if ( activeWidget == 0 ) {
                activeWidget = r.handle;
            }
            break;
        } 
    }

    if ( BUTTON_RELEASED() ) {
        activeWidget = 0;
    }

    _rects.Clear();

    if ( ( hotWidget == 0 && activeWidget == 0 ) || ( _mouseButton & MouseButtonState.Up ) != 0 ) {
        _mouseButton = MouseButtonState.None;
    }

    _oldCursorX = cursorX;
    _oldCursorY = cursorY;

#if QUI_USE_UNITY_UI
    if ( ! skipUnityUI ) {
        EndUnityUI();
    }
#endif

}

public static void DragPosition( WidgetResult res, ref float ioX, ref float ioY ) {
    if ( res == WidgetResult.Active ) {
        float dx = cursorX - _oldCursorX;
        float dy = cursorY - _oldCursorY;
        ioX += dx;
        ioY += dy;
    }
}

public static WidgetResult Drag_wg( ref float ioX, ref float ioY, float w, float h, int handle ) {
    WidgetResult res = ClickRect_wg( ioX, ioY, w, h, handle );
    DragPosition( res, ref ioX, ref ioY );
    return res;
}

public static WidgetResult ClickRect_wg( float x, float y, float w, float h, int handle ) {
    WidgetResult res = WidgetResult.Idle;

    // push rectangle in the list

    _rects.Add( new UIRect { x = x, y = y, w = w, h = h, handle = handle, } );

    // click

    if ( activeWidget == handle ) {
        if ( ( _mouseButton & MouseButtonState.Up ) != 0 && CursorInRect( x, y, w, h ) ) {
            res = WidgetResult.Released;
        } else {
            res = WidgetResult.Active;
        }
    } else if ( handle == hotWidget ) {
        if ( ( _mouseButton & MouseButtonState.Down ) != 0 && CursorInRect( x, y, w, h ) ) {
            res = WidgetResult.Pressed;
        } else {
            res = WidgetResult.Hot;
        }
    }

    return res;
}

public static int NextHashWg( int hash, int val ) {
    return hash * 31 + val + 1;
}

public static int HashWg( int lineNumber, string caller ) {
    int id = 23;
    id = NextHashWg( id, lineNumber + 1 );
    id = NextHashWg( id, caller.GetHashCode() );
    return id;
}

public static WidgetResult ClickRect( float x, float y, float w, float h, int handle = 0,
                                                    [CallerLineNumber] int lineNumber = 0,
                                                    [CallerMemberName] string caller = null ) {
    return ClickRect_wg( x, y, w, h, NextHashWg( HashWg( lineNumber, caller ), handle ) );
}

public static WidgetResult Drag( ref float x, ref float y, float w, float h, int handle = 0,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    return Drag_wg( ref x, ref y, w, h, NextHashWg( HashWg( lineNumber, caller ), handle ) );
}

public static void Scissor( float x, float y, float w, float h ) {
    _rects.Add( new UIRect { x = x, y = y, w = w, h = h, scissor = true, } );
}



// == implementation of IMGUI on top of retained mode Unity UI ==



#if QUI_USE_UNITY_UI

struct TickItem {
	public bool skipSort;
    public RectTransform rt;
    public Rect scissor;
};

static Dictionary<int,RectTransform> _cache = new Dictionary<int,RectTransform>();
static Dictionary<RectTransform,RectTransform[]> _refChildren =
                                                    new Dictionary<RectTransform,RectTransform[]>();
static HashSet<RectTransform> _garbage = new HashSet<RectTransform>();
static List<TickItem> _tickItems = new List<TickItem>();
static TextGenerator _textGen = new TextGenerator();

public static RectTransform [] RegisterChildren( RectTransform rt, string [] refChildren ) {
    RectTransform [] result;
    if ( ! _refChildren.TryGetValue( rt, out result ) ) {
        var found = new List<RectTransform>();
        if ( refChildren != null ) {
            foreach ( var rc in refChildren ) {
                var t = rt.Find( rc );
                if ( ! t ) {
                    Error( $"Couldn't find {rc} in {rt}. Will have empty children refs array." );
                    return null;
                }
                found.Add( t as RectTransform );
            }
        }
		// add the root rect transform as last element
		found.Add( rt );
        result = found.ToArray();
        _refChildren[rt] = result;
        Log( "Registered referenced children of " + rt );
    }
    return result;
}

static int HashTransform( float x, float y, float w, float h ) {
    int hash = 23;
    hash = NextHashWg( hash, ( int )x );
    hash = NextHashWg( hash, ( int )y );
    hash = NextHashWg( hash, ( int )w );
    hash = NextHashWg( hash, ( int )h );
    return hash;
}

static int HashTransform( RectTransform rt ) {
    return HashTransform( rt.position.x,
                          ( Screen.height - rt.position.y ),
                          rt.rect.width,
                          rt.rect.height );
}

static UnityEngine.UI.Text TextInternal( string content, float x, float y, float w, float h,
                                            int handle, Font font = null, int fontSize = 20, 
                                            TextAnchor align = TextAnchor.UpperLeft,
                                            VerticalWrapMode overflow = VerticalWrapMode.Overflow,
                                            Color? color = null ) {
    UnityEngine.UI.Text txt = RegisterGraphic<UnityEngine.UI.Text>( x, y, w, h, handle, color );
    font = font == null ? defaultFont : font;
    if ( txt.fontSize != fontSize ) {
        txt.fontSize = fontSize;
    }
    if ( txt.font != font ) {
        txt.font = font;
    }
    if ( txt.alignment != align ) {
        txt.alignment = align;
    }
    if ( txt.text != content ) {
        //Log( "setting text content" );
        txt.text = content;
    }
    if ( txt.verticalOverflow != overflow ) {
        txt.verticalOverflow = overflow;
    }
    return txt;
}

static void SpriteInternal( float x, float y, float w, float h, int handle, Texture2D tex = null,
                                                                            Color? color = null,
                                                                            bool scissor = false, 
                                                                            Vector4? border = null,
                                                                            float ppuMultiplier = 0,
                                                            Image.Type type = Image.Type.Simple ) {
    void initImage( Image i ) {
        i.maskable = false;
    }
    Image img = RegisterGraphic<Image>( x, y, w, h, handle, color, initImage, scissor );
    if ( ! img.sprite || img.sprite.texture != tex ) {
        UnityEngine.Object.Destroy( img.sprite );
        img.sprite = UnityEngine.Sprite.Create( tex, new Rect( 0.0f, 0.0f, tex.width, tex.height ),
                                                Vector2.zero, 100f, 0, SpriteMeshType.FullRect,
                                                border != null ? border.Value : Vector4.zero );
    }
    if ( img.type != type ) {
        img.type = type;
    } 
    if ( img.pixelsPerUnitMultiplier != ppuMultiplier ) {
        img.pixelsPerUnitMultiplier = ppuMultiplier;
    }
}

static void TextureInternal( float x, float y, float w, float h, int handle, Texture2D tex = null,
                                                                            Color? color = null,
                                                                            bool scissor = false ) {
    void initImage( RawImage i ) {
        i.maskable = false;
    }
    RawImage img = RegisterGraphic<RawImage>( x, y, w, h, handle, color, initImage, scissor );
    if ( img.texture != tex ) {
        img.texture = tex;
    }
}

public static void RegisterRT( RectTransform rt, float x = float.MaxValue, float y = float.MaxValue,
								float w = float.MaxValue, float h = float.MaxValue, int handle = 0,
								bool isScissor = false, bool skipSort = false ) {
	x = ( x != float.MaxValue ) ? x : rt.position.x;
	y = ( y != float.MaxValue ) ? y : Screen.height - rt.position.y;
	w = ( w != float.MaxValue ) ? w : rt.rect.width;
	h = ( h != float.MaxValue ) ? h : rt.rect.height;
	if ( HashTransform( rt ) != HashTransform( x, y, w, h ) ) {
		rt.position = new Vector2( x, Screen.height - y );
		rt.sizeDelta = new Vector2( w, h );
		//Log( "resize/move: " + rt.name + " " + ( int )x + "," + ( int )y + "," + ( int )w + "," + ( int )h );
	}
    Rect scissor = Rect.zero;
    if ( isScissor ) {
        Scissor( x, y, w, h );
        Rect cvs = canvas.GetComponent<RectTransform>().rect;
        scissor = new Rect( x + cvs.x, -y - cvs.y - h, w, h );
    }
    _tickItems.Add( new TickItem {
		skipSort = skipSort,
        rt = rt,
        scissor = scissor,
    } );
}

static void RegisterGraphicCommon( Graphic graphic, float x, float y, float w, float h, int handle,
                                                                    Color? color = null,
                                                                    bool isScissor = false ) {
    Color c = color == null ? Color.white : color.Value;
    if ( graphic.color != c ) {
        graphic.color = c;
    }
    RegisterRT( graphic.rectTransform, x, y, w, h, handle, isScissor );
}

static T RegisterGraphic<T>( float x, float y, float w, float h, int handle, Color? color = null,
                            Action<T> onCreate = null, bool isScissor = false ) where T : Graphic {
    RectTransform rt;
    if ( ! _cache.TryGetValue( handle, out rt ) || ! rt ) {
        GameObject go = new GameObject();
        go.transform.parent = canvas.transform;
        go.name = typeof( T ).Name + "_" + handle.ToString( "X4" );
        T comp = go.AddComponent<T>();
        if ( onCreate != null ) {
            onCreate( comp );
        }
        comp.rectTransform.pivot = new Vector3( 0, 1 );
        comp.raycastTarget = false;
        rt = _cache[handle] = comp.rectTransform;
        Log( "Created a graphic " + comp );
    }
    Graphic graphic = rt.GetComponent<Graphic>();
    RegisterGraphicCommon( graphic, x, y, w, h, handle, color, isScissor );
    return ( T )graphic;
}

static RectTransform RegisterPrefab( float x, float y, float w, float h, int handle,
                                                GameObject prefab = null, bool isScissor = false ) {
    RectTransform rt;
    if ( ! _cache.TryGetValue( handle, out rt ) || ! rt ) {
        string name = "Prefab_" + handle.ToString( "X4" );
        if ( prefab ) {
            GameObject go = GameObject.Instantiate( prefab );
			go.name = name;
			go.transform.position = prefab.transform.position;
			if ( prefab.transform.parent == null ) {
				go.transform.SetParent( canvas.transform );
			} else {
				Log( "Parented prefab, won't be sorted " + prefab );
				go.transform.SetParent( prefab.transform.parent );
				go.transform.localScale = prefab.transform.localScale;
			}
            rt = go.GetComponent<RectTransform>();
            var pfrt = prefab.GetComponent<RectTransform>();
            if ( pfrt ) {
                rt.pivot = pfrt.pivot;
                rt.anchorMin = pfrt.anchorMin;
                rt.anchorMax = pfrt.anchorMax;
                rt.anchoredPosition = pfrt.anchoredPosition;
                rt.sizeDelta = pfrt.sizeDelta;
            }
            Log( "Instantiated prefab " + prefab );
        } else {
			GameObject go = new GameObject( name, typeof( RectTransform ) );
            go.transform.SetParent( canvas.transform );
            rt = go.GetComponent<RectTransform>();
            Log( "Creating new object. rt: " + rt );
        }
        _cache[handle] = rt;
    }
    RegisterRT( rt, x, y, w, h, handle, isScissor: isScissor,
														skipSort: rt.parent != canvas.transform );
    return rt;
}

// == public Unity UI API ==

public static Canvas canvas;
public static Font defaultFont;
public static Texture2D whiteTexture;

public static void BeginUnityUI() {
    // the items from the previous tick are potentially garbage
    _garbage.Clear();
    foreach ( var i in _tickItems ) {
        _garbage.Add( i.rt );
    }
    _tickItems.Clear();
}

public static void EndUnityUI() {
    if ( ! Application.isPlaying ) {
		return;
	}

    if ( ! canvas ) {
        Error( "No canvas for QUI. Assign canvas before use." );
        return;
    }

    Rect scissor = canvas.GetComponent<RectTransform>().rect;
    // expand the scissor a bit
    scissor.x -= 10;
    scissor.y -= 10;
    scissor.width += 20;
    scissor.height += 20;

    foreach ( var i in _tickItems ) {
        _garbage.Remove( i.rt );
    }

    foreach ( var g in _garbage ) {
        if ( g ) {
            g.gameObject.SetActive( false );
            //RectTransform [] children;
            //if ( _refChildren.TryGetValue( g, out children ) ) {
            //}
            //// deactivating seems enough
            ////GameObject.Destroy( g.gameObject );
        }
    }

    for ( int i = 0; i < _tickItems.Count; i++ ) {
        RectTransform rt = _tickItems[i].rt;
        rt.gameObject.SetActive( true );
		if ( ! _tickItems[i].skipSort ) {
			rt.SetSiblingIndex( i );
		}
        IClippable c = rt.GetComponent<Graphic>() as IClippable;
        if ( c != null ) {
            c.Cull( scissor, true);
            c.SetClipRect( scissor, true );
        }
        Rect s = _tickItems[i].scissor;
        if ( s.width + s.height > 0 ) {
            scissor = s;
        }
    }
}

public static void EnableScissor( float x, float y, float w, float h, int handle = 0,
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    RegisterPrefab( x, y, w, h, handle, isScissor: true );
}

public static void DisableScissor( int handle = 0, [CallerLineNumber] int lineNumber = 0,
                                               [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    RegisterPrefab( 0, 0, Screen.width, Screen.height, handle, isScissor: true );
}

public static void Sprite( float x, float y, float w, float h, Texture2D tex = null,
                                                        Color? color = null,
                                                        bool scissor = false,
                                                        Vector4? border = null,
                                                        float ppuMultiplier = 1,
                                                        Image.Type type = Image.Type.Simple,
                                                        int handle = 0, 
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    SpriteInternal( x, y, w, h, handle, tex, color, scissor, border, ppuMultiplier, type );
}

public static void Texture( float x, float y, float w, float h, Texture2D tex = null,
                                                        Color? color = null, bool scissor = false,
                                                        int handle = 0, 
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    TextureInternal( x, y, w, h, handle, tex, color, scissor );
}

public static RectTransform [] PrefabWH( float x, float y, float w, float h, 
                                                        GameObject prefab = null,
                                                        string [] refChildren = null,
                                                        bool scissor = false, int handle = 0, 
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    RectTransform rt = RegisterPrefab( x, y, float.MaxValue, float.MaxValue, handle, prefab, scissor );
    rt.localScale = new Vector2( w / rt.sizeDelta.x, h / rt.sizeDelta.y );
    return RegisterChildren( rt, refChildren );
}

public static void GetRTPosAndSize( RectTransform rt, out float x, out float y,
																		out float w, out float h ) { 
    w = rt.sizeDelta.x * rt.localScale.x;
    h = rt.sizeDelta.y * rt.localScale.y;
    x = rt.pivot.x * w;
    y = ( 1 - rt.pivot.y ) * h;
}

public static void GetClickRect( RectTransform rt, float canvasScale,
													float screenHeight, out float x, out float y,
													out float w, out float h ) {
	QUI.GetRTPosAndSize( rt, out x, out y, out w, out h );
	x = rt.position.x - canvasScale * x;
	y = screenHeight - rt.position.y - canvasScale * y;
	w *= canvasScale;
	h *= canvasScale;
}

public static RectTransform [] Prefab( float posX = float.MaxValue, float posY = float.MaxValue,
										float scale = float.MaxValue, GameObject prefab = null,
										string [] refChildren = null, bool scissor = false,
														int handle = 0, 
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    RectTransform rt = RegisterPrefab( posX, posY, float.MaxValue, float.MaxValue, handle, prefab,
																						scissor );
	if ( scale != float.MaxValue ) {
		rt.localScale = Vector2.one * scale;
	}
    return RegisterChildren( rt, refChildren );
}

public static void MeasuredText( string content, float x, float y, float w, float h,
                                            out float measureW, out float measureH,
                                            bool visible = true,
                                            Font font = null, int fontSize = 20, 
                                            TextAnchor align = TextAnchor.UpperLeft,
                                            VerticalWrapMode overflow = VerticalWrapMode.Overflow,
                                            Color? color = null,
                                            int handle = 0,
                                            [CallerLineNumber] int lineNumber = 0,
                                            [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    var txt = TextInternal( content, x, y, w, h, handle, font, fontSize, align, overflow, color );
    TextGenerationSettings tgs = txt.GetGenerationSettings( txt.rectTransform.rect.size ); 
    measureW = Mathf.Min( w, _textGen.GetPreferredWidth( content, tgs ) );
    measureH = _textGen.GetPreferredHeight( content, tgs );
    // QUI End() will activate it, keep it invisible for now, so string can be measured and drawn separately
    txt.gameObject.SetActive( false );
}

public static void Text( string content, float x, float y, float w, float h,
                                            Font font = null, int fontSize = 20, 
                                            TextAnchor align = TextAnchor.UpperLeft,
                                            VerticalWrapMode overflow = VerticalWrapMode.Overflow,
                                            Color? color = null,
                                            int handle = 0,
                                            [CallerLineNumber] int lineNumber = 0,
                                            [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    TextInternal( content, x, y, w, h, handle, font, fontSize, align, overflow, color );
}

public static WidgetResult ClickText( string content, float x, float y, float w, float h,
                                        Font font = null, TextAnchor align = TextAnchor.UpperLeft,
                                            int fontSize = 20,
                                            VerticalWrapMode overflow = VerticalWrapMode.Overflow,
                                            Color? color = null,
                                            int handle = 0,
                                            [CallerLineNumber] int lineNumber = 0,
                                            [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    TextInternal( content, x, y, w, h, handle, font, fontSize, align, overflow, color );
    return ClickRect_wg( x, y, w, h, handle );
}

public static WidgetResult Panel( float x, float y, float w, float h,
                                                    Texture2D texture= null, Color? color = null,
                                                    // use this handle for loops
                                                    int handle = 0,
                                                    [CallerLineNumber] int lineNumber = 0,
                                                    [CallerMemberName] string caller = null ) {
    int id = NextHashWg( HashWg( lineNumber, caller ), handle );
    WidgetResult r = ClickRect_wg( x, y, w, h, id );
    Texture( x, y, w, h, handle: handle, tex: texture, color: color );
    return r;
}

#endif // QUI_USE_UNITY_UI


}
