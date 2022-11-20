using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

#if UNITY_EDITOR || UNITY_STANDALONE

//#define QONSOLE_BOOTSTRAP // if this is defined, the console will try to bootstrap itself
//#define QONSOLE_BOOTSTRAP_EDITOR // if this is defined, the console will try to bootstrap itself in the editor
//#define QUI_BOOTSTRAP // if this is defined, QUI gets properly setup in the bootstrap pump

using UnityEngine;

using static Qonche;

// you need to import the Qonsole source files inside the Unity editor for the Qonsole to work inside the Scene window
// OR do setup the Editor parts in a script inside Unity
#if UNITY_EDITOR && QONSOLE_BOOTSTRAP_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class QonsoleEditorSetup {
    static QonsoleEditorSetup() {
        void duringSceneGui( SceneView sv ) {
            Qonsole.OnEditorSceneGUI( sv.camera, EditorApplication.isPaused,
                                            EditorGUIUtility.pixelsPerPoint,
                                            onRepaint: Qonsole.OnEditorRepaint_f );
        }
        SceneView.duringSceneGui -= duringSceneGui;
        SceneView.duringSceneGui += duringSceneGui;
        Qonsole.Log( "Qonsole setup to work in the editor." );
    }
}
#endif

#if QONSOLE_BOOTSTRAP

public class QonsoleBootstrap : MonoBehaviour {
#if QUI_BOOTSTRAP
    static bool DebugShowUIRects_kvar = false;
    static Vector2 _mousePosition;
#endif

    void Start() {
        Qonsole.OnStoreCfg_f = () => KeyBinds.StoreConfig();
        Qonsole.OnPreLoadCfg_f = () => "echo executed before loading the cfg";
        Qonsole.Init( configVersion: 0 );
        KeyBinds.Log = s => Qonsole.Log( s );
        KeyBinds.Error = s => Qonsole.Error( s );

#if QUI_BOOTSTRAP
        QUI.DrawLineRect = (x,y,w,h) => QGL.LateDrawLineRect(x,y,w,h,color:Color.magenta);
        QUI.Log = s => Qonsole.Log( s );
        QUI.Error = s => Qonsole.Error( s );
        QUI.showRects = DebugShowUIRects_kvar;
        //QUI.canvas = ...
        //QUI.whiteTexture = ...
        //QUI.defaultFont = ...
#endif

        Qonsole.OnEditorRepaint_f = c => {};
        Qonsole.Start();
    }

    void Update() {
#if QUI_BOOTSTRAP
        QUI.Begin( ( int )_mousePosition.x, ( int )_mousePosition.y );
        Qonsole.OnUpdate_f();
        QUI.End();
#else
        Qonsole.OnUpdate();
#endif
    }

    void OnGUI() {
#if QUI_BOOTSTRAP
        _mousePosition = Event.current.mousePosition;
        if ( Event.current.type == EventType.MouseDown ) {
            QUI.OnMouseButton( true );
        } else if ( Event.current.type == EventType.MouseUp ) {
            QUI.OnMouseButton( false );
        }
#endif
        Qonsole.OnGUIEvent_f();
        Qonsole.OnGUI();
    }

    void OnApplicationQuit() {
        Qonsole.OnApplicationQuit();
    }
}

#endif // QONSOLE_BOOTSTRAP


public static class Qonsole {


#if QONSOLE_BOOTSTRAP
[RuntimeInitializeOnLoadMethod]
static void Bootstrap() {
    QonsoleBootstrap[] components = GameObject.FindObjectsOfType<QonsoleBootstrap>();
    if ( components.Length == 0 ) {
        GameObject go = new GameObject( "QonsoleBootstrap" );
        GameObject.DontDestroyOnLoad( go );
        go.AddComponent<QonsoleBootstrap>();
    } else {
        Debug.Log( "Already have QonsoleBootstrap" );
    }
}
#endif

public static bool Active;
public static bool Started;
// we hope it is the main thread?
public static readonly int ThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;

// stuff to be executed before the .cfg file is loaded
public static Func<string> OnPreLoadCfg_f = () => "";
// stuff to be executed after the .cfg file is loaded
public static Func<string> OnPostLoadCfg_f = () => "";
// provide additional string to be appended to the .cfg file on flush/store
public static Func<string> OnStoreCfg_f = () => "";
// the Unity editor (QGL) repaint callback
public static Action<Camera> OnEditorRepaint_f = c => {};
// called inside the Update pump (optionally with QUI setup) if QONSOLE_BOOTSTRAP is defined
public static Action OnUpdate_f = () => {};
public static Action OnStart_f = () => {};
public static Action OnDone_f = () => {};
// called inside OnGUI if QONSOLE_BOOTSTRAP is defined
public static Action OnGUIEvent_f = () => {};

static float _totalTime, _prevTime;
static string _historyPath;
static string _configPath;
static int _drawCharStartY;
// how much currently drawn char is faded out in the 'overlay' controlled by QonOverlayPercent_kvar
static float _overlayAlpha = 1;
// the colorization stack for nested tags
static List<Color> _drawCharColorStack = new List<Color>(){ Color.white };
static Action<string> _oneShotCmd_f;

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

static void DrawCharColorPush( Color newColor ) {
    if ( _drawCharColorStack.Count < 16 ) {
        _drawCharColorStack.Add( newColor );
    }
}

static void DrawCharColorPop() {
    if ( _drawCharColorStack.Count > 1 ) {
        _drawCharColorStack.RemoveAt( _drawCharColorStack.Count - 1 );
    }
}

static Vector2 QoncheToScreen( int x, int y ) {
    float screenX = x * QGL.TextDx * QonScale;
    float screenY = ( y - _drawCharStartY ) * QGL.TextDy * QonScale;
    return new Vector2( screenX, screenY );
}

// FIXME: remove the allocations here
static Action OverlayGetFade() {
    float timestamp = _totalTime;
    return () => {
        const float solidTime = 4.0f;
        float t = _totalTime - timestamp;
        float ts = 2 * ( solidTime - t );
        _overlayAlpha = t < solidTime ? 1 : Mathf.Max( 0, 1 - ts * ts );
    };
}

static string [] _history;
static int _historyItem;
static void GUIEvent() {
    // Handling arrows in IsKeyDown/Up on Update doesn't respect
    // the OS repeat delay, thus this
    // Also can't see a way to acquire string better than OnGUI
    // As a bonus -- no dependency on the legacy Input system
    if ( Event.current.type == EventType.KeyDown ) {
        if ( _oneShotCmd_f != null ) {
            if ( Event.current.keyCode == KeyCode.LeftArrow ) {
                QON_MoveLeft( 1 );
            } else if ( Event.current.keyCode == KeyCode.RightArrow ) {
                QON_MoveRight( 1 );
            } else if ( Event.current.keyCode == KeyCode.Home ) {
                QON_MoveLeft( 99999 );
            } else if ( Event.current.keyCode == KeyCode.End ) {
                QON_MoveRight( 99999 );
            } else if ( Event.current.keyCode == KeyCode.Delete ) {
                QON_Delete( 1 );
            } else if ( Event.current.keyCode == KeyCode.Backspace ) {
                QON_Backspace( 1 );
            } else if ( Event.current.keyCode == KeyCode.Escape ) {
                Log( "Canceled..." );
                QON_EraseCommand();
                Active = false;
                _oneShotCmd_f = null;
            } else {
                char c = Event.current.character;
                if ( c == '`' ) {
                } else if ( c == '\t' ) {
                } else if ( c == '\b' ) {
                } else if ( c == '\n' || c == '\r' ) {
                    string cmd = QON_GetCommand();
                    QON_EraseCommand();
                    Action<string> a = _oneShotCmd_f;
                    _oneShotCmd_f( cmd );
                    _oneShotCmd_f = null;
                    Active = false;
                } else {
                    QON_InsertCommand( c.ToString() );
                }
            }
        } else if ( Event.current.keyCode == KeyCode.BackQuote ) {
            Toggle();
        } else if ( Event.current.keyCode == KeyCode.LeftArrow ) {
            QON_MoveLeft( 1 );
        } else if ( Event.current.keyCode == KeyCode.Home ) {
            QON_MoveLeft( 99999 );
        } else if ( Event.current.keyCode == KeyCode.RightArrow ) {
            QON_MoveRight( 1 );
        } else if ( Event.current.keyCode == KeyCode.End ) {
            QON_MoveRight( 99999 );
        } else if ( Event.current.keyCode == KeyCode.Delete ) {
            _history = null;
            QON_Delete( 1 );
        } else if ( Event.current.keyCode == KeyCode.Backspace ) {
            _history = null;
            QON_Backspace( 1 );
        } else if ( Event.current.keyCode == KeyCode.PageUp ) {
            QON_PageUp();
        } else if ( Event.current.keyCode == KeyCode.PageDown ) {
            QON_PageDown();
        } else if ( Event.current.keyCode == KeyCode.Escape ) {
            if ( _history != null ) {
                // cancel history, store last typed-in command
                QON_SetCommand( _history[0] );
                _history = null;
            } else {
                // cancel something else?
            }
        } else if ( Event.current.keyCode == KeyCode.DownArrow
                    || Event.current.keyCode == KeyCode.UpArrow ) {
            string cmd = QON_GetCommand();
            if ( _history == null ) {
                _history = Cellophane.GetHistory( cmd );
                _historyItem = _history.Length * 100;
            }
            _historyItem += Event.current.keyCode == KeyCode.DownArrow
                                ? 1 : -1;
            if ( _historyItem >= 0 ) {
                QON_SetCommand( _history[_historyItem % _history.Length] );
            }
        } else {
            char c = Event.current.character;
            if ( c == '`' ) {
            } else if ( c == '\t' ) {
                string cmd = QON_GetCommand();
                string autocomplete = Cellophane.Autocomplete( cmd );
                QON_SetCommand( autocomplete );
            } else if ( c == '\b' ) {
            } else if ( c == '\n' || c == '\r' ) {
                _history = null;
                string cmdClean, cmdRaw;
                QON_GetCommandEx( out cmdClean, out cmdRaw );
                QON_EraseCommand();
                Log( cmdRaw );
                Cellophane.AddToHistory( cmdClean );
                TryExecute( cmdClean );
                FlushConfig();
            } else {
                _history = null;
                QON_InsertCommand( c.ToString() );
            }
        }
    }
}

static void RenderBegin() {
    float timeNow = Time.realtimeSinceStartup;
    _totalTime += Mathf.Min( timeNow - _prevTime, 0.033f );
    _prevTime = timeNow;
}

static void RenderEnd() {
    _overlayAlpha = 1;
    _drawCharStartY = 0;
    _drawCharColorStack.Clear();
    _drawCharColorStack.Add( Color.white );
}

static void RenderGL( bool skip = false ) {
    void drawChar( int c, int x, int y, bool isCursor, object param ) { 
        if ( DrawCharBegin( ref c, x, y, isCursor, out Color color, out Vector2 screenPos ) ) {
            QGL.DrawScreenCharWithOutline( c, screenPos.x, screenPos.y, color, QonScale );
        }
    }

    RenderBegin();

    GL.PushMatrix();
    GL.LoadPixelMatrix();

    QGL.LateBlitFlush();
    QGL.LatePrintFlush();
    QGL.LateDrawLineFlush();

    if ( ! skip ) {
        int maxH = ( int )QGL.ScreenHeight();
        int cW = ( int )( QGL.TextDx * QonScale );
        int cH = ( int )( QGL.TextDy * QonScale );
        int conW = Screen.width / cW;
        int conH = maxH / cH;

        if ( Active ) {
            QGL.SetWhiteTexture();
            GL.Begin( GL.QUADS );
            GL.Color( new Color( 0, 0, 0, QonAlpha_kvar ) );
            QGL.DrawSolidQuad( new Vector2( 0, 0 ), new Vector2( Screen.width, maxH ) );
            GL.End();
        } else {
            int percent = Mathf.Clamp( QonOverlayPercent_kvar, 0, 100 );
            conH = conH * percent / 100;
        }

        QGL.SetFontTexture();
        GL.Begin( GL.QUADS );
        QON_DrawChar = drawChar;
        QON_DrawEx( conW, conH, ! Active, 0 );
        GL.End();
    }

    GL.PopMatrix();
    RenderEnd();
}

static bool DrawCharBegin( ref int c, int x, int y, bool isCursor, out Color color,
                                                                        out Vector2 screenPos ) {
    color = Color.white;
    screenPos = Vector2.zero;

    if ( Active ) {
        _drawCharStartY = 0;
        c = c == 0 ? '~' : c;
    } else if ( _overlayAlpha <= 0 ) {
        _drawCharStartY = y + 1;
        return false;
    }

    if ( isCursor ) {
        c = ( ( int )( Time.realtimeSinceStartup * 1000.0f ) & 256 ) != 0 ? c : 0xdb;
    }

    if ( c == ' ' ) {
        return false;
    }

    Color stackCol = _drawCharColorStack[_drawCharColorStack.Count - 1];
    float a = Active ? stackCol.a : _overlayAlpha;
    color = new Color ( stackCol.r, stackCol.g, stackCol.b, a );
    screenPos = QoncheToScreen( x, y );

    return true;
}

static Mesh urpMesh = new Mesh();
static List<Vector3> urpVerts = new List<Vector3>();
static List<Vector2> urpUV = new List<Vector2>();
static List<Color> urpColors = new List<Color>();
static List<int> urpTris = new List<int>();
static int [] urpQuadBase = new int[6] { 0, 1, 2, 2, 3, 0 };
static int [] urpQuad = new int[6];

static void RenderURPMesh( bool skip = false ) {

    void drawChar( int c, int x, int y, bool isCursor, object param ) { 
        if ( ! DrawCharBegin( ref c, x, y, isCursor, out Color color, out Vector2 screenPos ) ) {
            return;
        }
        int idx = c & ( CodePage437.FontSz * CodePage437.FontSz - 1 );
        float csz = ( float )CodePage437.CharSz;
        float n = csz / CodePage437.FontTexSide;
        Vector3 vertOff = new Vector3( screenPos.x, screenPos.y );
        Vector3 uvOff = new Vector3( idx % CodePage437.FontSz * n, idx / CodePage437.FontSz * n );

        int numVerts = urpVerts.Count;

        urpUV.Add( CodePage437.CharUVs[0] + uvOff );
        urpVerts.Add( CodePage437.CharVerts[0] * QonScale + vertOff );
        urpUV.Add( CodePage437.CharUVs[1] + uvOff );
        urpVerts.Add( CodePage437.CharVerts[1] * QonScale + vertOff );
        urpUV.Add( CodePage437.CharUVs[2] + uvOff );
        urpVerts.Add( CodePage437.CharVerts[2] * QonScale + vertOff );
        urpUV.Add( CodePage437.CharUVs[3] + uvOff );
        urpVerts.Add( CodePage437.CharVerts[3] * QonScale + vertOff );

        for ( int i = 0; i < 6; i++ ) {
            urpQuad[i] = urpQuadBase[i] + numVerts;
        }

        urpTris.AddRange( urpQuad );

        for ( int i = 0; i < 4; i++ ) {
            urpColors.Add( color );
        }
    }

    RenderBegin();

    if ( ! skip ) {
        int maxH = ( int )QGL.ScreenHeight();
        int cW = ( int )( QGL.TextDx * QonScale );
        int cH = ( int )( QGL.TextDy * QonScale );
        int conW = Screen.width / cW;
        int conH = maxH / cH;

        if ( Active ) {
            // draw background
        } else {
            conH = conH * Mathf.Clamp( QonOverlayPercent_kvar, 0, 100 ) / 100;
        }

        QON_DrawChar = drawChar;
        QON_DrawEx( conW, conH, ! Active, 0 );

        if ( urpVerts.Count > 0 ) {
            urpMesh.vertices = urpVerts.ToArray();
            urpMesh.triangles = urpTris.ToArray();
            urpMesh.colors = urpColors.ToArray();
            urpMesh.uv = urpUV.ToArray();

            Vector2 [] outline = new Vector2 [] {
                new Vector3( QonScale, 0 ),
                new Vector3( 0, QonScale ),
                new Vector3( QonScale, QonScale ),
                new Vector3( -QonScale, QonScale ),
            };

            QGL.SetMaterialColor( Color.black );
            QGL.SetFontTexture();
            for ( int i = 0; i < outline.Length; i++ ) {
                Graphics.DrawMeshNow( urpMesh, Camera.current.transform.position
                                        + Camera.current.transform.TransformVector( outline[i] ),
                                        Camera.current.transform.rotation );
                Graphics.DrawMeshNow( urpMesh, Camera.current.transform.position
                                        + Camera.current.transform.TransformVector( -outline[i] ),
                                        Camera.current.transform.rotation );
            }

            QGL.SetMaterialColor( Color.white );
            QGL.SetFontTexture();
            Graphics.DrawMeshNow( urpMesh, Camera.current.transform.position,
                                                                Camera.current.transform.rotation );

            urpVerts.Clear();
            urpUV.Clear();
            urpTris.Clear();
            urpColors.Clear();

            urpMesh.Clear();
        }
    }

    RenderEnd();
}

static void Clear_kmd( string [] argv ) {
    for ( int i = 0; i < 50; i++ ) {
        QON_Putc( '\n' );
    }
}

static void Echo_kmd( string [] argv ) {
    if ( argv.Length == 1 ) {
        return;
    }
    string text = "";
    for ( int i = 1; i < argv.Length; i++ ) {
        text += argv[i] + " ";
    }
    Log( text );
}

static void Help_kmd( string [] argv ) {
    Log( "Qonsole history storage: '" + _historyPath + "'" );
    Log( "Qonsole config storage: '" + _configPath + "'" );
    Log( "[ff9000]~[-] -- Toggle." );
    Log( "[ff9000]PgUp/PgDown[-] -- page up / page down." );
    Log( "[ff9000]UpArrow/DownArrow[-] -- History." );
    Log( "[ff9000]Tab[-] -- Autocomplete." );
    Log( "Example method to be parsed as a command: [ff9000]static void Help_kmd(string [] argv)[-]");
    Log( "Example static member to be parsed as a variable: [ff9000]static bool ToggleFlag_kvar[-]");
    Log( "[ff9000]cmd/cvar[-] and [ff9000]kmd/kvar[-] are valid suffixes for exported names.");
    Log( "[ff9000]kmd/kvar[-] ignore class names when exporting to the command line.");
    Log( "Type [ff9000]'list'[-] or [ff9000]'ls'[-] to view existing commands." );
    Log( "Type [ff9000]'help'[-] for this help." );
    Cellophane.PrintInfo();
}

// some stuff need to be initialized before the Start() Unity callback
public static void Init( int configVersion = 0 ) {
    string fnameCfg = null, fnameHistory;

    string[] args = System.Environment.GetCommandLineArgs ();
    bool customConfig = false;
    foreach ( var a in args ) {
        if ( a.StartsWith( "--cfg" ) ) {
            string [] cfgArg = a.Split( new []{' ','='}, 
                    StringSplitOptions.RemoveEmptyEntries ); 
            if ( cfgArg.Length > 1 ) {
                fnameCfg = cfgArg[1].Replace("\"", "");
                Log( "Supplied cfg by command line: " + fnameCfg );
                customConfig = true;
            }
            break;
        }
    }
    if ( string.IsNullOrEmpty( fnameCfg ) ) {
        if ( Application.isEditor ) {
            fnameCfg = "qon_default_ed.cfg";
        } else {
            fnameCfg = "qon_default.cfg";
        }
    }
    if ( Application.isEditor ) {
        fnameHistory = "qon_history_ed.cfg";
        Log( "Run in Unity Editor." );
    } else {
        fnameHistory = "qon_history.cfg";
        Log( "Run Standalone." );
    }
    if ( QonUseRP ) {
        Log( "Uses Rendering Pipeline asset." );
    } else {
        Log( "Not using Rendering Pipeline asset." );
    }
    _historyPath = Path.Combine( Application.persistentDataPath, fnameHistory );
    _configPath = Path.Combine( Application.persistentDataPath, fnameCfg );
    string history = string.Empty;
    string config = string.Empty;
    try {
        history = File.ReadAllText( _historyPath );
        config = File.ReadAllText( _configPath );
    } catch ( Exception ) {
        Log( "Didn't read config files." );
    }
    Cellophane.ConfigVersion_kvar = configVersion;
    Cellophane.UseColor = true;
    Cellophane.Log = (s) => { Log( s ); };
    Cellophane.Error = (s) => { Error( s ); };
    Cellophane.ScanVarsAndCommands();
    TryExecute( OnPreLoadCfg_f() );
    Cellophane.ReadHistory( history );
    Cellophane.ReadConfig( config, skipVersionCheck: customConfig );
    TryExecute( OnPostLoadCfg_f() );
    Help_kmd( null );
}

public static void OnEditorSceneGUI( Camera camera, bool paused, float pixelsPerPoint = 1,
                                                                Action<Camera> onRepaint = null ) {
    if ( QonShowInEditor_kvar == 0 ) {
        return;
    }

    onRepaint = onRepaint != null ? onRepaint : c => {};

    bool notRunning = ! Application.isPlaying || paused;

    if ( Active && notRunning ) {
        if ( Event.current.button == 0 ) {
            var controlID = GUIUtility.GetControlID( FocusType.Passive );
            if ( Event.current.type == EventType.MouseDown ) {
                QUI.OnMouseButton( true );
                GUIUtility.hotControl = controlID;
            } else if ( Event.current.type == EventType.MouseUp ) {
                QUI.OnMouseButton( false );
                if ( GUIUtility.hotControl == controlID ) {
                    GUIUtility.hotControl = 0;
                }
            }
        }
    }
    if ( Event.current.type == EventType.Repaint ) {
        QGL.SetContext( camera, pixelsPerPoint, invertedY: true );
        if ( notRunning ) {
            Vector2 mouse = Event.current.mousePosition * pixelsPerPoint;
            QUI.Begin( mouse.x, mouse.y );
        }
        onRepaint( camera );
        QGL.LatePrint( "qonsole is running", Screen.width - 100, QGL.ScreenHeight() - 100 );
    }
    OnGUIInternal();
    if ( Event.current.type == EventType.Repaint ) {
        QGL.SetContext( null, invertedY: ! QonUseRP );
        if ( notRunning ) {
            QUI.End( skipUnityUI: true );
        }
    }
    if ( Active
            && Event.current.type != EventType.Repaint
            && Event.current.type != EventType.Layout ) {
        Event.current.Use();
    }
}

// == INTERNAL API ==

public static void Start() {
    if ( QGL.Start() ) {
        QGL.SetContext( null, invertedY: ! QonUseRP );
        Started = true;
        Log( "Qonsole Started." );
        OnStart_f();
    } else {
        Started = false;
    }
}

public static void FlushConfig() {
    File.WriteAllText( _historyPath, Cellophane.StoreHistory() );
    File.WriteAllText( _configPath, Cellophane.StoreConfig() + OnStoreCfg_f() );
}

public static void OnApplicationQuit() {
    OnDone_f();
    FlushConfig();
}

public static void OnGUIInternal( bool skipRender = false ) {
    if ( ! Started ) {
        return;
    }

    if ( Event.current.type == EventType.Repaint ) {
        RenderGL( skipRender );
    } else if ( Active ) {
        if ( Event.current.type != EventType.Repaint ) {
            GUIEvent();
        }
    } else if ( Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.BackQuote ) {
        Toggle();
    }
}

public static void OnGUI() {
    OnGUIInternal( skipRender: Application.isEditor && QonShowInEditor_kvar == 2 );
}

// == Public API ==

public static void TryExecute( string cmdLine, object context = null ) {
    string [] cmds;
    if ( Cellophane.SplitCommands( cmdLine, out cmds ) ) {
        string [] argv;
        foreach ( var cmd in cmds ) {
            if ( Cellophane.GetArgv( cmd, out argv ) ) {
                Cellophane.TryExecute( argv, context );
            }
        }
    }
}

public static void Toggle() {
    Active = ! Active;
}

public static void Error( object o ) {
    Error( o.ToString() );
}

static void PrintToUnityLog( string s, UnityEngine.Object o ) {
    if ( ! QonPrintToUnityLog_kvar ) {
        return;
    }

    // stack trace changes throw exception outside of the Main thread
    if ( System.Threading.Thread.CurrentThread.ManagedThreadId != ThreadID ) {
        Debug.Log( s, o );
        return;
    }

    StackTraceLogType oldType = Application.GetStackTraceLogType( LogType.Log );
    if ( Application.isEditor ) {
        Application.SetStackTraceLogType( LogType.Log, StackTraceLogType.ScriptOnly );
        Debug.Log( s, o );
    } else {
        Application.SetStackTraceLogType( LogType.Log, StackTraceLogType.None );
        Debug.Log( s, o );
    }
    Application.SetStackTraceLogType( LogType.Log, oldType );
}

public static void Error( string s, UnityEngine.Object o = null ) {
    s = "ERROR: " + s;
    Action fade = OverlayGetFade();

    // lump together colorization and overlay fade
    QON_PrintAndAct( s, (x,y) => {
        DrawCharColorPush( Color.red );
        fade();
    } );
    QON_PrintAndAct( "\n", (x,y)=>DrawCharColorPop() );

    PrintToUnityLog( s, o );
}

// this will ignore color tags
public static void PrintRaw( string s ) {
    Action fade = OverlayGetFade();
    QON_PrintAndAct( s, (x,y)=>fade() );
}

public static void Log( string s, UnityEngine.Object o = null ) {
    Print( s + "\n", o );
}

public static void Log( object o, UnityEngine.Object unityObj = null ) {
    Log( o == null ? "null" : o.ToString(), unityObj );
}

public static void PrintAndAct( string s, Action<Vector2,float> a ) {
    if ( ! string.IsNullOrEmpty( s ) ) {
        QON_PrintAndAct( s, (x,y)=> {
            float alpha = Active ? 1 : _overlayAlpha;
            if ( alpha > 0 ) {
                GL.End();
                Vector2 screenPos = QoncheToScreen( x, y );
                a( screenPos, alpha );
                QGL.SetFontTexture();
                GL.Begin( GL.QUADS );
            }
        } );
    } else {
        Error( "PrintAndAct: pass a non-empty string." );
    }
}

// print (colorized) text
public static void Print( string s, UnityEngine.Object o = null ) {
    string unityString = "";
    bool skipFade = false;
    for ( int i = 0; i < s.Length; i++ ) {

        // handle nested colorization tags by lumping their logic into a single pager Action
        string tag;
        List<Action> actions = new List<Action>();
        while ( true ) {
            if ( Cellophane.ColorTagLead( s, i, out tag ) ) {
                i += tag.Length;
                Color c = TagToCol( tag );
                actions.Add( ()=>DrawCharColorPush( c ) );
            } else if ( Cellophane.ColorTagClose( s, i, out tag ) ) {
                i += tag.Length;
                actions.Add( ()=>DrawCharColorPop() );
            } else {
                break;
            }
        }

        // add the overlay fade just once per string
        if ( ! skipFade ) {
            actions.Add( OverlayGetFade() );
            skipFade = true;
        }

        if ( actions.Count > 0 ) {
            // actual print with colorization and (overlay) fadeout
            QON_PrintAndAct( s[i].ToString(), (x,y) => {
                foreach( var a in actions ) {
                    a();
                }
            } );
        } else {
            // raw output to qonche (no colorization), never reached
            QON_Putc( s[i] );
        }

        unityString += s[i];
    }

    PrintToUnityLog( unityString, o );
}

public static void Break( string str ) {
    Log( str );
    Debug.Break();
}

public static void OneShotCmd( string fillCommandLine, Action<string> a ) {
    QON_SetCommand( fillCommandLine );
    Active = true;
    _oneShotCmd_f = a;
}

public static float LineHeight() {
    return QGL.TextDy * QonScale;
}

// == Internal commands and vars ==

[Description( "Part of the screen height occupied by the 'overlay' fading-out lines. If set to zero, Qonsole won't show anything unless Active" )]
static int QonOverlayPercent_kvar = 0;
[Description( "Show the Qonsole output to the unity log too." )]
static bool QonPrintToUnityLog_kvar = true;
[Description( "Console character size." )]
static float QonScale_kvar = 1;
static float QonScale => Mathf.Clamp( QonScale_kvar, 1, 100 );
[Description( "Show the Qonsole in the editor: 0 -- no, 1 -- yes, 2 -- editor only." )]
public static float QonShowInEditor_kvar = 0;
[Description( "Alpha blend value of the Qonsole background." )]
public static float QonAlpha_kvar = 0.65f;
[Description( "When not using RP the GL coordinates are inverted (always the case in Editor Scene window). Set this to false to use inverted GL in the Play window." )]
public static bool QonUseRP_kvar = false;
public static bool QonUseRP => QonUseRP_kvar
                    || UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null;

static void Exit_kmd( string [] argv ) {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    if ( Application.isEditor ) {
        Log( "Can't quit if not linked against Editor." );
    }
    Application.Quit();
#endif
}

static void Quit_kmd( string [] argv ) { Exit_kmd( argv ); }


}


#else


// == Multiplatform (non-Unity) API here ==


public static class Qonsole {

static string _configPath = "";

static Qonsole() {
}

public static void Init( int configVersion = 0 ) {
    string fnameCfg = null;

    string[] args = System.Environment.GetCommandLineArgs ();
    bool customConfig = false;
    foreach ( var a in args ) {
        if ( a.StartsWith( "--cfg" ) ) {
            string [] cfgArg = a.Split( new []{' ','='}, StringSplitOptions.RemoveEmptyEntries ); 
            if ( cfgArg.Length > 1 ) {
                fnameCfg = cfgArg[1].Replace("\"", "");
                Log( "Supplied cfg by command line: " + fnameCfg );
                customConfig = true;
            }
            break;
        }
    }

    if ( string.IsNullOrEmpty( fnameCfg ) ) {
        fnameCfg = "qon_default.cfg";
    }

    string config = string.Empty;
    string dir = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetEntryAssembly().Location
                );
    Qonsole.Log( dir );
    Qonsole.Log( System.Reflection.Assembly.GetEntryAssembly().Location );
    _configPath = Path.Combine( dir, fnameCfg );
    Log( $"Qonsole config storage: '{_configPath}'" );
    try {
        config = File.ReadAllText( _configPath );
    } catch ( Exception e ) {
        Log( "Didn't read config files." );
        Log( e.Message );
    }
    Cellophane.ConfigVersion_kvar = configVersion;
    Cellophane.UseColor = false;
    Cellophane.Log = (s) => { Log( s ); };
    Cellophane.Error = (s) => { Error( s ); };
    Cellophane.ScanVarsAndCommands();
    Cellophane.ReadConfig( config, skipVersionCheck: customConfig );
    FlushConfig();
}

public static void FlushConfig() {
    try {
        File.WriteAllText( _configPath, Cellophane.StoreConfig() );
        Log( "Stored config." );
    } catch ( Exception e ) {
        Log( e.Message );
    }
}

public static void TryExecute( string cmdLine, object context = null ) {
    string [] cmds;
    if ( Cellophane.SplitCommands( cmdLine, out cmds ) ) {
        string [] argv;
        foreach ( var cmd in cmds ) {
            if ( Cellophane.GetArgv( cmd, out argv ) ) {
                Cellophane.TryExecute( argv, context );
            }
        }
    }
}

public static void Log( string s ) {
    System.Console.WriteLine( Cellophane.ColorTagStripAll( s ) );
}

public static void Log( object o ) {
    Log( o == null ? "null" : o.ToString() );
}

public static void Error( object o ) {
    Log( "ERROR: " + o );
}

public static void Break( object o ) {
    Log( "BREAK: " + o );
}

}


#endif // UNITY
