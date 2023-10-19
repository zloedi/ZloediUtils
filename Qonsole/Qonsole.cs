#if UNITY_STANDALONE || UNITY_2021_0_OR_NEWER
#define HAS_UNITY
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

#if SDL || HAS_UNITY

//#define QONSOLE_BOOTSTRAP // if this is defined, the console will try to bootstrap itself
//#define QONSOLE_BOOTSTRAP_EDITOR // if this is defined, the console will try to bootstrap itself in the editor
//#define QONSOLE_QUI // if this is defined, QUI gets properly setup in the bootstrap pump
//#define QONSOLE_KEYBINDS // if this is defined, use the KeyBinds inside the Qonsole loop

#if HAS_UNITY
using UnityEngine;
using QObject = UnityEngine.Object;
#else
using System.Runtime.InteropServices;
using static AppleFont;
using static SDL2.SDL;
using static SDL2.SDL.SDL_BlendMode;
using static SDL2.SDL.SDL_EventType;
using static SDL2.SDL.SDL_Keycode;
using GalliumMath;
using QObject = System.Object;
#endif

using static Qonche;

// you need to import the Qonsole source files inside the Unity editor for the Qonsole to work inside the Scene window
// OR if you work in an outside assembly, setup the Editor parts in a script inside Unity
#if UNITY_EDITOR && QONSOLE_BOOTSTRAP && QONSOLE_BOOTSTRAP_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class QonsoleEditorSetup {
    static QonsoleEditorSetup() {
        QonsoleBootstrap.TrySetupQonsole();

        void duringSceneGui( SceneView sv ) {
            Qonsole.OnEditorSceneGUI( sv.camera, EditorApplication.isPaused,
                                            EditorGUIUtility.pixelsPerPoint,
                                            onRepaint: Qonsole.onEditorRepaint_f );
        }

        SceneView.duringSceneGui -= duringSceneGui;
        SceneView.duringSceneGui += duringSceneGui;
        Qonsole.Log( "Qonsole setup to work in the editor." );
    }
}
#endif

#if QONSOLE_BOOTSTRAP

public class QonsoleBootstrap : MonoBehaviour {
    public static void TrySetupQonsole() {
		if ( Qonsole.Started ) {
			return;
		}

        Qonsole.onEditorRepaint_f = c => {};
        Qonsole.Init();
        Qonsole.Start();

        KeyBinds.Log = s => Qonsole.Log( s );
        KeyBinds.Error = s => Qonsole.Error( s );
    }

    void Awake() {
        TrySetupQonsole();
    }

    void Update() {
        Qonsole.Update();
    }

    void OnGUI() {
        Qonsole.OnGUI();
    }

    void OnApplicationQuit() {
        Qonsole.OnApplicationQuit();
    }
}

#endif // QONSOLE_BOOTSTRAP


public static class Qonsole {


#if HAS_UNITY && QONSOLE_BOOTSTRAP
[RuntimeInitializeOnLoadMethod]
static void CreateBootstrapObject() {
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
public static bool ConsumeEditorInputOnce;

public const string featuresDescription = @"Features:
. Persistent history, browse previously issued commands (in previous app runs) using up/down arrows.
    Tries to match previous commands by the string on the prompt.
. Persistent variables stored in config file in the app config directory.
. Versioned config files -- changing the config version resets all vars to defaults.
. Works in the Editor (Scene) window too, if the game is paused or not running, setup vars before running the game.
. No dependencies in the code that uses it, no need of attributes, code using it compiles without Qonsole.
. Can modify persistent console variables from within code.
. Doesn't need any Unity assets; drawn using GL calls and font bytes are part of the source code.
. Supports custom config files by command line param: [ff9000]--cfg=zloedi.cfg[-].
    Custom config files don't get erased/reset on config version change (the defaults do).
. Separate configs in Build and inside Unity Editor: qon_default.cfg vs qon_default_ed.cfg
. Autocompletes partial commands, not only start-of-command.
. Supports multiple commands chained together; example:
      [ff9000]] open_all_doors ; kill_all_enemies ; spawn_monster living Ally 9 12 ; spawn_monster cityguard 8 10 ; teleport player 6 6[-]
. Repeat commands by prepending a number i.e. [ff9000]] 3clear[-].
. Colorized output.
. Config file supports arbitrary commands.
. Variable descriptions stored as comments in the config file.
. Hook on different events (Start, Update, Done) by defining Qonsole commands with no dependencies.
    and more...
    
";

[Description( "Part of the screen height occupied by the 'overlay' fading-out lines. If set to zero, Qonsole won't show anything unless Active" )]
static int QonOverlayPercent_kvar = 0;
[Description( "Show the Qonsole output to the system (unity) log too." )]
static bool QonPrintToSystemLog_kvar = true;
[Description( "Console character size." )]
static float QonScale_kvar = 1;
static float QonScale => Mathf.Clamp( QonScale_kvar, 1, 100 );
[Description( "Show the Qonsole in the editor: 0 -- no, 1 -- yes, 2 -- editor only." )]
public static float QonShowInEditor_kvar = 1;
[Description( "Alpha blend value of the Qonsole background." )]
public static float QonAlpha_kvar = 0.65f;
[Description( "When not using RP the GL coordinates are inverted (always the case in Editor Scene window). Set this to false to use inverted GL in the Play window." )]
public static bool QonInvertPlayY_kvar = false;
#if QONSOLE_INVERTED_PLAY_Y
public static bool QonInvertPlayY = true;
#else
public static bool QonInvertPlayY => QonInvertPlayY_kvar;
#endif

// OBSOLETE, USE COMMANDS INSTEAD: stuff to be executed before the .cfg file is loaded
public static Func<string> onPreLoadCfg_f = () => "";
// OBSOLETE, USE COMMANDS INSTEAD: stuff to be executed after the .cfg file is loaded
public static Func<string> onPostLoadCfg_f = () => "";
// provide additional string to be appended to the .cfg file on flush/store
public static Func<string> onStoreCfg_f;
// OBSOLETE, USE COMMANDS INSTEAD: called inside the Update pump (optionally with QUI setup) if QONSOLE_BOOTSTRAP is defined
public static Action tick_f = () => {};
// OBSOLETE, USE COMMANDS INSTEAD: 
public static Action onStart_f = () => {};
// OBSOLETE, USE COMMANDS INSTEAD: 
public static Action onDone_f = () => {};
// OBSOLETE, USE COMMANDS INSTEAD: called inside OnGUI if QONSOLE_BOOTSTRAP is defined
public static Action onGUI_f = () => {};

#if HAS_UNITY
// we hope it is the main thread?
public static readonly int ThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
// the Unity editor (QGL) repaint callback
public static Action<Camera> onEditorRepaint_f = c => {};
static bool _isEditor => Application.isEditor;
static string _dataPath => Application.persistentDataPath;
static float _textDx => QGL.TextDx;
static float _textDy => QGL.TextDy;
static int _cursorChar => QGL.GetCursorChar();
#else
static bool _isEditor = false;
static string _dataPath = "./";
static int _textDx = AppleFont.APPLEIIF_CW + 1;
static int _textDy = AppleFont.APPLEIIF_CH + 3;
static int _cursorChar => 127;
#endif

static int _totalTime;
static string _historyPath;
static string _configPath;
static int _drawCharStartY;
// how much currently drawn char is faded out in the 'overlay' controlled by QonOverlayPercent_kvar
static float _overlayAlpha = 1;
// the colorization stack for nested tags
static List<Color> _drawCharColorStack = new List<Color>(){ Color.white };
// the internal commands have a different path of execution
// to avoid recursion of Cellophane.TryExecute
static Dictionary<string,Action<string[],object>> _internalCommands =
                                                new Dictionary<string,Action<string[],object>>();
static Action<string> _oneShotCmd_f;
static string [] _history;
static int _historyItem;

#if QONSOLE_QUI
static Vector2 _mousePosition;
#endif

#if QONSOLE_KEYBINDS
static HashSet<KeyCode> _holdKeys = new HashSet<KeyCode>();
#endif

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
    float screenX = x * _textDx * QonScale;
    float screenY = ( y - _drawCharStartY ) * _textDy * QonScale;
    return new Vector2( screenX, screenY );
}

// FIXME: remove the allocations here
static Action OverlayGetFade() {
    int timestamp = _totalTime;
    return () => {
        const float solidTime = 4.0f;
        float t = ( _totalTime - timestamp ) / 1000f;
        float ts = 2f * ( solidTime - t );
        _overlayAlpha = t < solidTime ? 1 : Mathf.Max( 0, 1 - ts * ts );
    };
}

static void RenderBegin() {
#if HAS_UNITY
    _totalTime = ( int )( Time.realtimeSinceStartup * 1000.0f );
#else
    _totalTime = ( int )SDL_GetTicks();
#endif
}

static void RenderEnd() {
    _overlayAlpha = 1;
    _drawCharStartY = 0;
    _drawCharColorStack.Clear();
    _drawCharColorStack.Add( Color.white );
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
        c = ( _totalTime & 256 ) != 0 ? c : _cursorChar;
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

static void InternalCommand( string cmd, object context = null ) {
    Action<string[],object> action;
    if ( ! _internalCommands.TryGetValue( cmd, out action ) ) {
        if ( ! Cellophane.TryFindCommand( cmd, out action ) ) {
            return;
        }
    }
    action( null, context );
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

static void Exit_kmd( string [] argv ) {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#elif HAS_UNITY
    if ( _isEditor ) {
        Log( "Can't quit if not linked against Editor." );
    }
    Application.Quit();
#else
    Environment.Exit( 1 );
    // ...
#endif
}

static void Quit_kmd( string [] argv ) { Exit_kmd( argv ); }

// some stuff need to be initialized before the Start() Unity callback
public static void Init( int configVersion = -1 ) {
    Log( featuresDescription );

    string fnameCfg = null, fnameHistory;

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
        if ( _isEditor ) {
            fnameCfg = "qon_default_ed.cfg";
        } else {
            fnameCfg = "qon_default.cfg";
        }
    }
    if ( _isEditor ) {
        fnameHistory = "qon_history_ed.cfg";
        Log( "Run in Unity Editor." );
    } else {
        fnameHistory = "qon_history.cfg";
        Log( "Run Standalone." );
    }

#if HAS_UNITY
    Log( $"Unity version: {Application.unityVersion}" );
#endif

    Log( "Inverted Y in Play window: " + QonInvertPlayY );

    _historyPath = Path.Combine( _dataPath, fnameHistory );
    _configPath = Path.Combine( _dataPath, fnameCfg );
    string history = string.Empty;
    string config = string.Empty;
    try {
        history = File.ReadAllText( _historyPath );
        config = File.ReadAllText( _configPath );
    } catch ( Exception ) {
        Log( "Didn't read config files." );
    }
    if ( configVersion >= 0 ) {
        Cellophane.ConfigVersion_kvar = configVersion;
    }
    Cellophane.UseColor = true;
    Cellophane.Log = s => Log( s );
    Cellophane.Error = s => Error( $"[Cellophane] {s}" );
    Cellophane.ScanVarsAndCommands();
    InternalCommand( "qonsole_pre_config" );
    TryExecuteLegacy( onPreLoadCfg_f() );
    Cellophane.ReadHistory( history );
    Cellophane.ReadConfig( config, skipVersionCheck: customConfig );
    TryExecuteLegacy( onPostLoadCfg_f() );
    InternalCommand( "qonsole_post_config" );
    Help_kmd( null );

    if ( onStoreCfg_f == null ) {
#if QONSOLE_KEYBINDS
        onStoreCfg_f = () => KeyBinds.StoreConfig();
#else
        onStoreCfg_f = () => "";
#endif
    }

#if QONSOLE_QUI
    QUI.DrawLineRect = (x,y,w,h) => QGL.LateDrawLineRect(x,y,w,h,color:Color.magenta);
    QUI.Log = s => Qonsole.Log( s );
    QUI.Error = s => Qonsole.Error( s );
    //QUI.canvas = ...
    //QUI.whiteTexture = ...
    //QUI.defaultFont = ...
#endif
}

#if HAS_UNITY

public static void OnEditorSceneGUI( Camera camera, bool paused, float pixelsPerPoint = 1,
                                                                Action<Camera> onRepaint = null ) {
    if ( QonShowInEditor_kvar == 0 ) {
        return;
    }

    onRepaint = onRepaint != null ? onRepaint : c => {};

    bool notRunning = ! Application.isPlaying || paused;

    InternalCommand( "qonsole_on_editor_event" );

    if ( ( Active || ConsumeEditorInputOnce ) && notRunning ) {
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
        ConsumeEditorInputOnce = false;
        onRepaint( camera );
        InternalCommand( "qonsole_on_editor_repaint" );
        QGL.LatePrint( "qonsole is running", Screen.width - 100, QGL.ScreenHeight() - 100 );
    }
    OnGUIInternal();
    if ( Event.current.type == EventType.Repaint ) {
        QGL.SetContext( null, invertedY: QonInvertPlayY );
        if ( notRunning ) {
            QUI.End( skipUnityUI: true );
        }
    }
    if ( ( Active || ConsumeEditorInputOnce )
            && Event.current.type != EventType.Repaint
            && Event.current.type != EventType.Layout ) {
        Event.current.Use();
    }
}

public static void OnGUIInternal( bool skipRender = false ) {
    if ( ! Started ) {
        return;
    }

    if ( Event.current.type == EventType.Repaint ) {
        RenderGL( skipRender );
    } else if ( Active ) {
        if ( Event.current.type != EventType.Repaint ) {
            // Handling arrows in IsKeyDown/Up on Update doesn't respect
            // the OS repeat delay, thus this
            // Also can't see a way to acquire a string better than OnGUI
            // As a bonus -- no dependency on the legacy Input system
            if ( Event.current.type == EventType.KeyDown ) {
                //if ( _oneShotCmd_f != null ) {
                //    if ( Event.current.keyCode == KeyCode.LeftArrow ) {
                //        QON_MoveLeft( 1 );
                //    } else if ( Event.current.keyCode == KeyCode.RightArrow ) {
                //        QON_MoveRight( 1 );
                //    } else if ( Event.current.keyCode == KeyCode.Home ) {
                //        QON_MoveLeft( 99999 );
                //    } else if ( Event.current.keyCode == KeyCode.End ) {
                //        QON_MoveRight( 99999 );
                //    } else if ( Event.current.keyCode == KeyCode.Delete ) {
                //        QON_Delete( 1 );
                //    } else if ( Event.current.keyCode == KeyCode.Backspace ) {
                //        QON_Backspace( 1 );
                //    } else if ( Event.current.keyCode == KeyCode.Escape ) {
                //        Log( "Canceled..." );
                //        QON_EraseCommand();
                //        Active = false;
                //        _oneShotCmd_f = null;
                //    } else {
                //        char c = Event.current.character;
                //        if ( c == '`' ) {
                //        } else if ( c == '\t' ) {
                //        } else if ( c == '\b' ) {
                //        } else if ( c == '\n' || c == '\r' ) {
                //            string cmd = QON_GetCommand();
                //            QON_EraseCommand();
                //            _oneShotCmd_f( cmd );
                //            _oneShotCmd_f = null;
                //            Active = false;
                //        } else {
                //            QON_InsertCommand( c.ToString() );
                //        }
                //    }
                //} else 

                if ( Event.current.keyCode == KeyCode.BackQuote ) {
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
                        // just erase the prompt if no history
                        QON_EraseCommand();
                    }
                } else if ( Event.current.keyCode == KeyCode.DownArrow
                            || Event.current.keyCode == KeyCode.UpArrow ) {
                    string cmd = QON_GetCommand();
                    if ( _history == null ) {
                        _history = Cellophane.GetHistory( cmd );
                        _historyItem = _history.Length * 100;
                    }
                    _historyItem += Event.current.keyCode == KeyCode.DownArrow ? 1 : -1;
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
                        OnEnter();
                    } else {
                        _history = null;
                        QON_InsertCommand( c.ToString() );
                    }
                }
            }
        }
    } else if ( Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.BackQuote ) {
        Toggle();
    }
}

public static void OnGUI() {
#if QONSOLE_QUI
    _mousePosition = Event.current.mousePosition;
    if ( Event.current.button == 0 ) {
        if ( Event.current.type == EventType.MouseDown ) {
            QUI.OnMouseButton( true );
        } else if ( Event.current.type == EventType.MouseUp ) {
            QUI.OnMouseButton( false );
        }
    }
#endif
#if QONSOLE_KEYBINDS
    if ( ! Active ) {
        KeyCode kc = Event.current.button == 0 ? KeyCode.Mouse0 : KeyCode.Mouse1;
        if ( Event.current.type == EventType.MouseDown ) {
            KeyBinds.TryExecuteBinds( keyDown: kc );
            _holdKeys.Add( kc );
        } else if ( Event.current.type == EventType.MouseUp ) {
            KeyBinds.TryExecuteBinds( keyUp: kc );
            _holdKeys.Remove( kc );
        }

        if ( Event.current.type == EventType.KeyDown ) {
            KeyBinds.TryExecuteBinds( keyDown: Event.current.keyCode );
            _holdKeys.Add( Event.current.keyCode );
        } else if ( Event.current.type == EventType.KeyUp ) {
            KeyBinds.TryExecuteBinds( keyUp: Event.current.keyCode );
            _holdKeys.Remove( Event.current.keyCode );
        }
    }
#endif
    InternalCommand( "qonsole_on_gui" );
    onGUI_f();
    OnGUIInternal( skipRender: _isEditor && QonShowInEditor_kvar == 2 );
}

static void PrintToSystemLog( string s, QObject o ) {
    if ( ! QonPrintToSystemLog_kvar ) {
        return;
    }

    // stack trace changes throw exception outside of the Main thread
    if ( System.Threading.Thread.CurrentThread.ManagedThreadId != ThreadID ) {
        Debug.Log( s, o );
        return;
    }

    StackTraceLogType oldType = Application.GetStackTraceLogType( LogType.Log );
    if ( _isEditor ) {
        Application.SetStackTraceLogType( LogType.Log, StackTraceLogType.ScriptOnly );
        Debug.Log( s, o );
    } else {
        Application.SetStackTraceLogType( LogType.Log, StackTraceLogType.None );
        Debug.Log( s, o );
    }
    Application.SetStackTraceLogType( LogType.Log, oldType );
}

static void RenderGL( bool skip = false ) {
    void drawChar( int c, int x, int y, bool isCursor, object param ) { 
        if ( DrawCharBegin( ref c, x, y, isCursor, out Color color, out Vector2 screenPos ) ) {
            QGL.DrawScreenCharWithOutline( c, screenPos.x, screenPos.y, color, QonScale );
        }
    }

    RenderBegin();

    QGL.Begin();

    QGL.FlushLates();

    //QGL.LateBlitFlush();
    //QGL.LatePrintFlush();
    //QGL.LateDrawLineFlush();

    if ( ! skip ) {
        int maxH = ( int )QGL.ScreenHeight();
        int cW = ( int )( _textDx * QonScale );
        int cH = ( int )( _textDy * QonScale );
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

    QGL.End( skipLateFlush: true );

    RenderEnd();
}

#else // if not HAS_UNITY

static void PrintToSystemLog( string s, QObject o ) {
    if ( QonPrintToSystemLog_kvar ) {
        System.Console.Write( Cellophane.ColorTagStripAll( s ) );
    }
}

#endif // HAS_UNITY

static void OnEnter() {
    _history = null;
    string cmdClean, cmdRaw;
    QON_GetCommandEx( out cmdClean, out cmdRaw );
    QON_EraseCommand();
    Log( cmdRaw );
    Cellophane.AddToHistory( cmdClean );
    TryExecute( cmdClean );
    FlushConfig();
}

public static void Update() {
#if QONSOLE_KEYBINDS
    foreach ( var k in _holdKeys ) {
        KeyBinds.TryExecuteBinds( keyHold: k );
    }
#endif

#if QONSOLE_QUI
    QUI.Begin( ( int )_mousePosition.x, ( int )_mousePosition.y );
#endif
    InternalCommand( "qonsole_tick" );
    Qonsole.tick_f();
#if QONSOLE_QUI
    QUI.End();
#endif
}

public static void FlushConfig() {
    File.WriteAllText( _historyPath, Cellophane.StoreHistory() );
    File.WriteAllText( _configPath, Cellophane.StoreConfig() + onStoreCfg_f() );
}

public static void OnApplicationQuit() {
    InternalCommand( "qonsole_done" );
    onDone_f();
    FlushConfig();
}

// == public API ==

public static void Start() {
#if HAS_UNITY
    if ( QGL.Start( invertedY: QonInvertPlayY ) ) {
        Started = true;
        Log( "Qonsole Started." );
        InternalCommand( "qonsole_post_start" );
        onStart_f();
    } else {
        Started = false;
    }
#else
    Started = true;
    Log( "Qonsole Started." );
    onStart_f();
#endif
}

public static void TryExecute( string cmdLine, object context = null, bool keepJsonTags = false ) {
    Cellophane.TryExecuteString( cmdLine, context: context, keepJsonTags: keepJsonTags );
}

public static void TryExecuteLegacy( string cmdLine, object context = null ) {
    string [] cmds;
    if ( Cellophane.SimpleCommandsSplit( cmdLine, out cmds ) ) {
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

public static void Error( string s, QObject o = null ) {
    s = "ERROR: " + s;
    Action fade = OverlayGetFade();

    // lump together colorization and overlay fade
    QON_PrintAndAct( s, (x,y) => {
        DrawCharColorPush( Color.red );
        fade();
    } );
    QON_PrintAndAct( "\n", (x,y)=>DrawCharColorPop() );

    PrintToSystemLog( s, o );
}

// this will ignore color tags
public static void PrintRaw( string s ) {
    Action fade = OverlayGetFade();
    QON_PrintAndAct( s, (x,y)=>fade() );
}

public static void Log( string s, QObject o = null ) {
    Print( s + "\n", o );
}

public static void Log( object o, QObject unityObj = null ) {
    Log( o == null ? "null" : o.ToString(), unityObj );
}

public static void PrintAndAct( string s, Action<Vector2,float> a ) {
    if ( ! string.IsNullOrEmpty( s ) ) {
        QON_PrintAndAct( s, (x,y)=> {
            float alpha = Active ? 1 : _overlayAlpha;
            if ( alpha > 0 ) {
                Vector2 screenPos = QoncheToScreen( x, y );
#if HAS_UNITY
                GL.End();
                a( screenPos, alpha );
                QGL.SetFontTexture();
                GL.Begin( GL.QUADS );
#else
                a( screenPos, alpha );
#endif
            }
        } );
    } else {
        Error( "PrintAndAct: pass a non-empty string." );
    }
}

// print (colorized) text
public static void Print( string s, QObject o = null ) {
    string sysString = "";
    bool skipFade = false;
    for ( int i = 0; i < s.Length; i++ ) {

        // handle nested colorization tags by lumping their logic into a single pager Action
        string tag;
        List<Action> actions = new List<Action>();
        while ( true ) {
            if ( Cellophane.ColorTagLead( s, i, out tag ) ) {
                i += tag.Length;
                Color c = QGL.TagToCol( tag );
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

        sysString += s[i];
    }

    PrintToSystemLog( sysString, o );
}

public static void Break( string str ) {
    Log( str );
#if HAS_UNITY
    UnityEngine.Debug.Break();
#endif
}

public static void OneShotCmd( string fillCommandLine, Action<string> a ) {
    QON_SetCommand( fillCommandLine );
    Active = true;
    _oneShotCmd_f = a;
}

public static float LineHeight() {
    return _textDy * QonScale;
}


#if SDL

static IntPtr x_renderer;
static IntPtr x_window;

static void SDLDrawChar( int c, int x, int y, int w, int h, Color32 col ) {
    int idx = c & ( APPLEIIF_ROWS * APPLEIIF_CLMS - 1 );
    var tex = AppleFont.GetTexture( x_renderer );
    SDL_SetTextureAlphaMod( tex, 0xff );
    SDL_SetTextureBlendMode( tex, SDL_BLENDMODE_BLEND );
    SDL_Rect src = new SDL_Rect {
        x = idx % APPLEIIF_CLMS * APPLEIIF_CW,
        y = idx / APPLEIIF_CLMS * APPLEIIF_CH,
        w = APPLEIIF_CW,
        h = APPLEIIF_CH,
    };
    SDL_SetTextureColorMod( tex, col.r, col.g, col.b );
    SDL_Rect dst = new SDL_Rect { x = x, y = y, w = w, h = h };
    SDL_RenderCopy( x_renderer, tex, ref src, ref dst );
}

public static void SDLDone() {
    OnApplicationQuit();
}

public static bool SDLTick( IntPtr renderer, IntPtr window, bool skipRender = false ) {
    x_renderer = renderer;
    x_window = window;

    while ( SDL_PollEvent( out SDL_Event ev ) != 0 ) {
        SDL_Keycode code = ev.key.keysym.sym;
        switch( ev.type ) {
            case SDL_TEXTINPUT:
                byte [] b = new byte[SDL_TEXTINPUTEVENT_TEXT_SIZE];
                unsafe {
                    Marshal.Copy( ( IntPtr )ev.text.text, b, 0, b.Length );
                }
                string txt = System.Text.Encoding.UTF8.GetString( b, 0, b.Length );
                if ( txt.Length > 0 && txt[0] != '`' && txt[0] != '~' ) {
                    QON_InsertCommand( txt );
                }
                break;

            case SDL_KEYDOWN:
                switch ( code ) {
                    case SDLK_LEFT:      QON_MoveLeft( 1 );      break;
                    case SDLK_RIGHT:     QON_MoveRight( 1 );     break;
                    case SDLK_HOME:      QON_MoveLeft( 99999 );  break;
                    case SDLK_END:       QON_MoveRight( 99999 ); break;

                    case SDLK_PAGEUP:    QON_PageUp();           break;
                    case SDLK_PAGEDOWN:  QON_PageDown();         break;
                    case SDLK_ESCAPE:    QON_EraseCommand();     break;
                    case SDLK_BACKQUOTE: Toggle();               break;

                    case SDLK_BACKSPACE: {
                        _history = null;
                        QON_Backspace( 1 );
                        break;
                    }

                    case SDLK_DELETE: {
                        _history = null;
                        QON_Delete( 1 );
                        break;
                    }

                    case SDLK_TAB: {
                        string autocomplete = Cellophane.Autocomplete( QON_GetCommand() );
                        QON_SetCommand( autocomplete );
                    }
                    break;

                    case SDLK_RETURN: {
                        OnEnter();
                    }
                    break;

                    default: break;
                }
                break;

			case SDL_MOUSEMOTION:
				//x_mouseX = ev.motion.x;
                //x_mouseY = ev.motion.y;
				break;

            case SDL_MOUSEBUTTONDOWN:
                //ZH_UI_OnMouseButton( 1 );
                break;

            case SDL_MOUSEBUTTONUP:
                //ZH_UI_OnMouseButton( 0 );
                break;

            case SDL_QUIT:
                return false;

            default:
                break;
        }
    }

    if ( ! Started ) {
        return true;
    }

    Update();

    void drawChar( int c, int x, int y, bool isCursor, object param ) { 
        if ( DrawCharBegin( ref c, x, y, isCursor, out Color color, out Vector2 screenPos ) ) {
            int cw = ( int )( APPLEIIF_CW * QonScale );
            int ch = ( int )( APPLEIIF_CH * QonScale );
            int cx = ( int )screenPos.x;
            int cy = ( int )screenPos.y;

            int off = ( int )QonScale;
            SDLDrawChar( c, cx + off, cy + off, cw, ch, Color.black );
            
            SDLDrawChar( c, cx, cy, cw, ch, color );
        }
    }

    RenderBegin();

    SDL_GetWindowSize( x_window, out int screenW, out int screenH );

    int cW = ( int )( _textDx * QonScale );
    int cH = ( int )( _textDy * QonScale );
    int conW = screenW / cW;
    int conH = screenH / cH;

    if ( Active ) {
        SDL_SetRenderDrawBlendMode( x_renderer, SDL_BLENDMODE_BLEND );
        SDL_SetRenderDrawColor( x_renderer, 0, 0, 0, ( byte )( QonAlpha_kvar * 255f ) );
        SDL_Rect bgrRect = new SDL_Rect {
            x = 0, y = 0, w = screenW, h = screenH
        };
        SDL_RenderFillRect( x_renderer, ref bgrRect );
    } else {
        int percent = Mathf.Clamp( QonOverlayPercent_kvar, 0, 100 );
        conH = conH * percent / 100;
    }

    QON_DrawChar = drawChar;
    QON_DrawEx( conW, conH, ! Active, 0 );

    RenderEnd();

    return true;
}

#endif // SDL


} // class Qonsole


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
    Cellophane.Log = s => Log( s );
    Cellophane.Error = s => Error( $"[Cellophane] {s}" );
    Cellophane.ScanVarsAndCommands();
    Cellophane.ReadConfig( config, skipVersionCheck: customConfig );
    FlushConfig();
}

public static void FlushConfig() {
    try {
        File.WriteAllText( _configPath, Cellophane.StoreConfig() );
        Log( $"Stored qonsole config '{_configPath}'" );
    } catch ( Exception e ) {
        Log( e.Message );
    }
}

public static void TryExecute( string cmdLine, object context = null, bool keepJsonTags = false ) {
    Cellophane.TryExecuteString( cmdLine, context: context, keepJsonTags: keepJsonTags );
}

public static void Print( string s ) {
    System.Console.Write( Cellophane.ColorTagStripAll( s ) );
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
