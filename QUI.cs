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
    Dropped,
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

public static bool DebugShowRects_cvar;

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

public static bool CursorInRect( float x, float y, float w, float h ) {
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

    if ( DebugShowRects_cvar ) {
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
        } else if ( ( _mouseButton & MouseButtonState.Up ) != 0 ) {
            res = WidgetResult.Dropped;
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

class TickItem {
	public bool skipSort;
    public RectTransform rt;
    public Rect scissor;
    public float garbageAge;
};

static Dictionary<RectTransform,RectTransform[]> _refChildren =
                                                    new Dictionary<RectTransform,RectTransform[]>();
static HashSet<TickItem> _garbage = new HashSet<TickItem>();
static Dictionary<int,TickItem> _cache = new Dictionary<int,TickItem>();
static List<TickItem> _dead = new List<TickItem>();
static List<TickItem> _tickItems = new List<TickItem>();
static TextGenerator _textGen = new TextGenerator();

// 1 -- show creation, 2 -- show destruction, 3 -- show all
public static int DebugLogGraphicLife_cvar;

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

static void InitImageGrapic( Image img ) {
    img.maskable = false;
}

static void ImageGraphicCommon( Image img, float ppuMultiplier, Image.Type type ) {
    if ( img.type != type ) {
        img.type = type;
    } 
    if ( img.pixelsPerUnitMultiplier != ppuMultiplier ) {
        img.pixelsPerUnitMultiplier = ppuMultiplier;
    }
}

static void SpriteInternal( float x, float y, float w, float h, int handle, Texture2D tex = null,
                                                            Color? color = null,
                                                            bool scissor = false, 
                                                            Vector4? border = null,
                                                            float ppuMultiplier = 0,
                                                            Image.Type type = Image.Type.Simple ) {
    Image img = RegisterGraphic<Image>( x, y, w, h, handle, color, InitImageGrapic, scissor );
    if ( ! img.sprite || img.sprite.texture != tex ) {
        UnityEngine.Object.Destroy( img.sprite );
        img.sprite = UnityEngine.Sprite.Create( tex, new Rect( 0.0f, 0.0f, tex.width, tex.height ),
                                                Vector2.zero, 100f, 0, SpriteMeshType.FullRect,
                                                border != null ? border.Value : Vector4.zero );
    }
    ImageGraphicCommon( img, ppuMultiplier, type );
}

static void SpriteInternal( float x, float y, float w, float h, int handle, Sprite sprite,
                                                                            Color? color = null,
                                                                            bool scissor = false, 
                                                                            float ppuMultiplier = 0,
                                                            Image.Type type = Image.Type.Simple ) {
    Image img = RegisterGraphic<Image>( x, y, w, h, handle, color, InitImageGrapic, scissor );
    if ( ! img.sprite || img.sprite != sprite ) {
        img.sprite = sprite;
    }
    ImageGraphicCommon( img, ppuMultiplier, type );
}

static void RegisterItem( TickItem item, float x = float.MaxValue, float y = float.MaxValue,
								float w = float.MaxValue, float h = float.MaxValue, int handle = 0,
								bool isScissor = false, bool skipSort = false ) {
    var rt = item.rt;
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
    item.scissor = scissor;
    item.skipSort = skipSort;
    item.rt = rt;
    _tickItems.Add( item );
}

static bool TryGetItemFromCache( int handle, out TickItem item ) {
    return _cache.TryGetValue( handle, out item ) && item != null && item.rt != null;
}

static T RegisterGraphic<T>( float x, float y, float w, float h, int handle, Color? color = null,
                            Action<T> onCreate = null, bool isScissor = false ) where T : Graphic {
    if ( ! TryGetItemFromCache( handle, out TickItem item ) ) {
        GameObject go = new GameObject();
        go.transform.parent = canvas.transform;
        go.name = typeof( T ).Name + "_" + handle.ToString( "X4" );
        T comp = go.AddComponent<T>();
        if ( onCreate != null ) {
            onCreate( comp );
        }
        comp.rectTransform.pivot = new Vector3( 0, 1 );
        comp.raycastTarget = false;
        item = _cache[handle] = new TickItem {
            rt = comp.rectTransform,
        };

        if ( DebugLogGraphicLife_cvar == 1 || DebugLogGraphicLife_cvar == 3 ) {
            Log( $"Created a graphic {comp}" );
        }
    }
    Graphic graphic = item.rt.GetComponent<Graphic>();
    Color c = color == null ? Color.white : color.Value;
    if ( graphic.color != c ) {
        graphic.color = c;
    }
    RegisterItem( item, x, y, w, h, handle, isScissor );
    return ( T )graphic;
}

static RectTransform RegisterPrefab( float x, float y, float w, float h, int handle,
                                                GameObject prefab = null, bool isScissor = false ) {
    if ( ! TryGetItemFromCache( handle, out TickItem item ) ) {
        string name = "Prefab_" + handle.ToString( "X4" );
        RectTransform rt;
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
        item = _cache[handle] = new TickItem {
            rt = rt,
        };
    }
    RegisterItem( item, x, y, w, h, handle, isScissor: isScissor,
                                                    skipSort: item.rt.parent != canvas.transform );
    return item.rt;
}

// == public Unity UI API ==

public static Canvas canvas;
public static Font defaultFont;

public static void BeginUnityUI() {
    if ( ! defaultFont ) {
        defaultFont = Resources.GetBuiltinResource<Font>( "Arial.ttf" );
    }
    // the items from the previous tick are potentially garbage
    foreach ( var i in _tickItems ) {
        _garbage.Add( i );
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
        _garbage.Remove( i );
    }

    _dead.Clear();

    foreach ( var g in _garbage ) {

        g.garbageAge += Time.deltaTime;

        if ( ! g.rt ) {
            _dead.Add( g );
            continue;
        }

        if ( g.garbageAge > 5f ) {
            if ( DebugLogGraphicLife_cvar == 2 || DebugLogGraphicLife_cvar == 3 ) {
                string comp = g.rt.GetComponent<Graphic>()?.ToString();
                if ( comp == null ) {
                    comp = g.rt.ToString();
                }
                Log( $"Destroyed '{comp}', num visible: {_tickItems.Count}" );
            }
            GameObject.Destroy( g.rt.gameObject );
            _dead.Add( g );
        } else {
            // hide the item
            g.rt.gameObject.SetActive( false );
        }
    }

    foreach ( var d in _dead ) {
        _garbage.Remove( d );
    }

    for ( int i = 0; i < _tickItems.Count; i++ ) {
        var ti = _tickItems[i];
        RectTransform rt = ti.rt;

        // make sure we are active
        rt.gameObject.SetActive( true );

        // reset the wait-for-death timer each tick we are apparent
        ti.garbageAge = -i * 0.1f;

        // sort by order of calling
		if ( ! _tickItems[i].skipSort ) {
			rt.SetSiblingIndex( i );
		}

        // handle scissors
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

public static void SpriteTex_wg( float x, float y, float w, float h, int handle,
                                                        Texture2D tex = null,
                                                        Color? color = null,
                                                        bool scissor = false,
                                                        Vector4? border = null,
                                                        float ppuMultiplier = 1,
                                                        Image.Type type = Image.Type.Simple ) {
    SpriteInternal( x, y, w, h, handle, tex, color, scissor, border, ppuMultiplier, type );
}

public static void SpriteTex( float x, float y, float w, float h, Texture2D tex = null,
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

public static void Sprite_wg( float x, float y, float w, float h, Sprite sprite, int handle,
                                                        Color? color = null,
                                                        bool scissor = false,
                                                        float ppuMultiplier = 1,
                                                        Image.Type type = Image.Type.Simple ) {
    SpriteInternal( x, y, w, h, handle, sprite, color, scissor, ppuMultiplier, type );
}

public static void Sprite( float x, float y, float w, float h, Sprite sprite,
                                                        Color? color = null,
                                                        bool scissor = false,
                                                        float ppuMultiplier = 1,
                                                        Image.Type type = Image.Type.Simple,
                                                        int handle = 0, 
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    SpriteInternal( x, y, w, h, handle, sprite, color, scissor, ppuMultiplier, type );
}

public static void Texture_wg( float x, float y, float w, float h, int handle, Texture2D tex = null,
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

public static void Texture( float x, float y, float w, float h, Texture2D tex = null,
                                                        Color? color = null, bool scissor = false,
                                                        int handle = 0, 
                                                        [CallerLineNumber] int lineNumber = 0,
                                                        [CallerMemberName] string caller = null ) {
    handle = NextHashWg( HashWg( lineNumber, caller ), handle );
    Texture_wg( x, y, w, h, handle, tex, color, scissor );
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

public static void Text_wg( string content, float x, float y, float w, float h,
                                            Font font = null, int fontSize = 20, 
                                            TextAnchor align = TextAnchor.UpperLeft,
                                            VerticalWrapMode overflow = VerticalWrapMode.Overflow,
                                            Color? color = null,
                                            int handle = 0 ) {
    TextInternal( content, x, y, w, h, handle, font, fontSize, align, overflow, color );
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
