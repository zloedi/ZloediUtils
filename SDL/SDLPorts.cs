using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GalliumMath;

using static SDL2.SDL;
using static SDL2.SDL.SDL_WindowFlags;
using static SDL2.SDL.SDL_EventType;
using static SDL2.SDL.SDL_TextureAccess;
using static SDL2.SDL.SDL_BlendMode;
using static SDL2.SDL.SDL_Keycode;

namespace SDLPorts {
    public enum KeyCode {
        None, //Not assigned (never returned as the result of a keystroke).
        Backspace, //The backspace key.
        Delete, //The forward delete key.
        Tab, //The tab key.
        Clear, //The Clear key.
        Return, //Return key.
        Pause, //Pause on PC machines.
        Escape, //Escape key.
        Space, //Space key.
        Keypad0, //Numeric keypad 0.
        Keypad1, //Numeric keypad 1.
        Keypad2, //Numeric keypad 2.
        Keypad3, //Numeric keypad 3.
        Keypad4, //Numeric keypad 4.
        Keypad5, //Numeric keypad 5.
        Keypad6, //Numeric keypad 6.
        Keypad7, //Numeric keypad 7.
        Keypad8, //Numeric keypad 8.
        Keypad9, //Numeric keypad 9.
        KeypadPeriod, //Numeric keypad '.'.
        KeypadDivide, //Numeric keypad '/'.
        KeypadMultiply, //Numeric keypad '*'.
        KeypadMinus, //Numeric keypad '='.
        KeypadPlus, //Numeric keypad '+'.
        KeypadEnter, //Numeric keypad enter.
        KeypadEquals, //Numeric keypad '='.
        UpArrow, //Up arrow key.
        DownArrow, //Down arrow key.
        RightArrow, //Right arrow key.
        LeftArrow, //Left arrow key.
        Insert, //Insert key key.
        Home, //Home key.
        End, //End key.
        PageUp, //Page up.
        PageDown, //Page down.
        F1, //F1 function key.
        F2, //F2 function key.
        F3, //F3 function key.
        F4, //F4 function key.
        F5, //F5 function key.
        F6, //F6 function key.
        F7, //F7 function key.
        F8, //F8 function key.
        F9, //F9 function key.
        F10, //F10 function key.
        F11, //F11 function key.
        F12, //F12 function key.
        F13, //F13 function key.
        F14, //F14 function key.
        F15, //F15 function key.
        Alpha0, //The '0' key on the top of the alphanumeric keyboard.
        Alpha1, //The '1' key on the top of the alphanumeric keyboard.
        Alpha2, //The '2' key on the top of the alphanumeric keyboard.
        Alpha3, //The '3' key on the top of the alphanumeric keyboard.
        Alpha4, //The '4' key on the top of the alphanumeric keyboard.
        Alpha5, //The '5' key on the top of the alphanumeric keyboard.
        Alpha6, //The '6' key on the top of the alphanumeric keyboard.
        Alpha7, //The '7' key on the top of the alphanumeric keyboard.
        Alpha8, //The '8' key on the top of the alphanumeric keyboard.
        Alpha9, //The '9' key on the top of the alphanumeric keyboard.
        Exclaim, //Exclamation mark key '!'.
        DoubleQuote, //Double quote key '"'.
        Hash, //Hash key '#'.
        Dollar, //Dollar sign key '$'.
        Ampersand, //Ampersand key '&'.
        Quote, //Quote key '.
        LeftParen, //Left Parenthesis key '('.
        RightParen, //Right Parenthesis key ')'.
        Asterisk, //Asterisk key '*'.
        Plus, //Plus key '+'.
        Comma, //Comma ',' key.
        Minus, //Minus '-' key.
        Equals, //Equals '=' key.
        Period, //Period '.' key.
        Slash, //Slash '/' key.
        Colon, //Colon ':' key.
        Semicolon, //Semicolon ',' key.
        Less, //Less than '' key.
        Question, //Question mark '?' key.
        At, //At key '@'.
        LeftBracket, //Left square bracket key '['.
        Backslash, //Backslash key '\'.
        RightBracket, //Right square bracket key ']'.
        Caret, //Caret key '^'.
        Underscore, //Underscore '_' key.
        BackQuote, //Back quote key '`'.
        A, //'a' key.
        B, //'b' key.
        C, //'c' key.
        D, //'d' key.
        E, //'e' key.
        F, //'f' key.
        G, //'g' key.
        H, //'h' key.
        I, //'i' key.
        J, //'j' key.
        K, //'k' key.
        L, //'l' key.
        M, //'m' key.
        N, //'n' key.
        O, //'o' key.
        P, //'p' key.
        Q, //'q' key.
        R, //'r' key.
        S, //'s' key.
        T, //'t' key.
        U, //'u' key.
        V, //'v' key.
        W, //'w' key.
        X, //'x' key.
        Y, //'y' key.
        Z, //'z' key.
        Numlock, //Numlock key.
        CapsLock, //Capslock key.
        ScrollLock, //Scroll lock key.
        RightShift, //Right shift key.
        LeftShift, //Left shift key.
        RightControl, //Right Control key.
        LeftControl, //Left Control key.
        RightAlt, //Right Alt key.
        LeftAlt, //Left Alt key.

        // already added (not unique)
        // KeyCode.LeftCommand, //Left Command key.

        LeftApple, //Left Command key.
        LeftWindows, //Left Windows key.
        RightCommand, //Right Command key.

        // already added (not unique)
        //KeyCode.RightApple, //Right Command key.

        RightWindows, //Right Windows key.
        AltGr, //Alt Gr key.
        Help, //Help key.
        Print, //Print key.
        SysReq, //Sys Req key.
        Break, //Break key.
        Menu, //Menu key.
        Mouse0, //First (primary) mouse button.
        Mouse1, //Second (secondary) mouse button.
        Mouse2, //Third mouse button.
        Mouse3, //Fourth mouse button.
        Mouse4, //Fifth mouse button.
        Mouse5, //Sixth mouse button.
        Mouse6, //Seventh mouse button.

#if false // no joystick
        JoystickButton0, //Button 0 on any joystick.
        JoystickButton1, //Button 1 on any joystick.
        JoystickButton2, //Button 2 on any joystick.
        JoystickButton3, //Button 3 on any joystick.
        JoystickButton4, //Button 4 on any joystick.
        JoystickButton5, //Button 5 on any joystick.
        JoystickButton6, //Button 6 on any joystick.
        JoystickButton7, //Button 7 on any joystick.
        JoystickButton8, //Button 8 on any joystick.
        JoystickButton9, //Button 9 on any joystick.
        JoystickButton10, //Button 10 on any joystick.
        JoystickButton11, //Button 11 on any joystick.
        JoystickButton12, //Button 12 on any joystick.
        JoystickButton13, //Button 13 on any joystick.
        JoystickButton14, //Button 14 on any joystick.
        JoystickButton15, //Button 15 on any joystick.
        JoystickButton16, //Button 16 on any joystick.
        JoystickButton17, //Button 17 on any joystick.
        JoystickButton18, //Button 18 on any joystick.
        JoystickButton19, //Button 19 on any joystick.
        Joystick1Button0, //Button 0 on first joystick.
        Joystick1Button1, //Button 1 on first joystick.
        Joystick1Button2, //Button 2 on first joystick.
        Joystick1Button3, //Button 3 on first joystick.
        Joystick1Button4, //Button 4 on first joystick.
        Joystick1Button5, //Button 5 on first joystick.
        Joystick1Button6, //Button 6 on first joystick.
        Joystick1Button7, //Button 7 on first joystick.
        Joystick1Button8, //Button 8 on first joystick.
        Joystick1Button9, //Button 9 on first joystick.
        Joystick1Button10, //Button 10 on first joystick.
        Joystick1Button11, //Button 11 on first joystick.
        Joystick1Button12, //Button 12 on first joystick.
        Joystick1Button13, //Button 13 on first joystick.
        Joystick1Button14, //Button 14 on first joystick.
        Joystick1Button15, //Button 15 on first joystick.
        Joystick1Button16, //Button 16 on first joystick.
        Joystick1Button17, //Button 17 on first joystick.
        Joystick1Button18, //Button 18 on first joystick.
        Joystick1Button19, //Button 19 on first joystick.
        Joystick2Button0, //Button 0 on second joystick.
        Joystick2Button1, //Button 1 on second joystick.
        Joystick2Button2, //Button 2 on second joystick.
        Joystick2Button3, //Button 3 on second joystick.
        Joystick2Button4, //Button 4 on second joystick.
        Joystick2Button5, //Button 5 on second joystick.
        Joystick2Button6, //Button 6 on second joystick.
        Joystick2Button7, //Button 7 on second joystick.
        Joystick2Button8, //Button 8 on second joystick.
        Joystick2Button9, //Button 9 on second joystick.
        Joystick2Button10, //Button 10 on second joystick.
        Joystick2Button11, //Button 11 on second joystick.
        Joystick2Button12, //Button 12 on second joystick.
        Joystick2Button13, //Button 13 on second joystick.
        Joystick2Button14, //Button 14 on second joystick.
        Joystick2Button15, //Button 15 on second joystick.
        Joystick2Button16, //Button 16 on second joystick.
        Joystick2Button17, //Button 17 on second joystick.
        Joystick2Button18, //Button 18 on second joystick.
        Joystick2Button19, //Button 19 on second joystick.
        Joystick3Button0, //Button 0 on third joystick.
        Joystick3Button1, //Button 1 on third joystick.
        Joystick3Button2, //Button 2 on third joystick.
        Joystick3Button3, //Button 3 on third joystick.
        Joystick3Button4, //Button 4 on third joystick.
        Joystick3Button5, //Button 5 on third joystick.
        Joystick3Button6, //Button 6 on third joystick.
        Joystick3Button7, //Button 7 on third joystick.
        Joystick3Button8, //Button 8 on third joystick.
        Joystick3Button9, //Button 9 on third joystick.
        Joystick3Button10, //Button 10 on third joystick.
        Joystick3Button11, //Button 11 on third joystick.
        Joystick3Button12, //Button 12 on third joystick.
        Joystick3Button13, //Button 13 on third joystick.
        Joystick3Button14, //Button 14 on third joystick.
        Joystick3Button15, //Button 15 on third joystick.
        Joystick3Button16, //Button 16 on third joystick.
        Joystick3Button17, //Button 17 on third joystick.
        Joystick3Button18, //Button 18 on third joystick.
        Joystick3Button19, //Button 19 on third joystick.
        Joystick4Button0, //Button 0 on forth joystick.
        Joystick4Button1, //Button 1 on forth joystick.
        Joystick4Button2, //Button 2 on forth joystick.
        Joystick4Button3, //Button 3 on forth joystick.
        Joystick4Button4, //Button 4 on forth joystick.
        Joystick4Button5, //Button 5 on forth joystick.
        Joystick4Button6, //Button 6 on forth joystick.
        Joystick4Button7, //Button 7 on forth joystick.
        Joystick4Button8, //Button 8 on forth joystick.
        Joystick4Button9, //Button 9 on forth joystick.
        Joystick4Button10, //Button 10 on forth joystick.
        Joystick4Button11, //Button 11 on forth joystick.
        Joystick4Button12, //Button 12 on forth joystick.
        Joystick4Button13, //Button 13 on forth joystick.
        Joystick4Button14, //Button 14 on forth joystick.
        Joystick4Button15, //Button 15 on forth joystick.
        Joystick4Button16, //Button 16 on forth joystick.
        Joystick4Button17, //Button 17 on forth joystick.
        Joystick4Button18, //Button 18 on forth joystick.
        Joystick4Button19, //Button 19 on forth joystick.
        Joystick5Button0, //Button 0 on fifth joystick.
        Joystick5Button1, //Button 1 on fifth joystick.
        Joystick5Button2, //Button 2 on fifth joystick.
        Joystick5Button3, //Button 3 on fifth joystick.
        Joystick5Button4, //Button 4 on fifth joystick.
        Joystick5Button5, //Button 5 on fifth joystick.
        Joystick5Button6, //Button 6 on fifth joystick.
        Joystick5Button7, //Button 7 on fifth joystick.
        Joystick5Button8, //Button 8 on fifth joystick.
        Joystick5Button9, //Button 9 on fifth joystick.
        Joystick5Button10, //Button 10 on fifth joystick.
        Joystick5Button11, //Button 11 on fifth joystick.
        Joystick5Button12, //Button 12 on fifth joystick.
        Joystick5Button13, //Button 13 on fifth joystick.
        Joystick5Button14, //Button 14 on fifth joystick.
        Joystick5Button15, //Button 15 on fifth joystick.
        Joystick5Button16, //Button 16 on fifth joystick.
        Joystick5Button17, //Button 17 on fifth joystick.
        Joystick5Button18, //Button 18 on fifth joystick.
        Joystick5Button19, //Button 19 on fifth joystick.
        Joystick6Button0, //Button 0 on sixth joystick.
        Joystick6Button1, //Button 1 on sixth joystick.
        Joystick6Button2, //Button 2 on sixth joystick.
        Joystick6Button3, //Button 3 on sixth joystick.
        Joystick6Button4, //Button 4 on sixth joystick.
        Joystick6Button5, //Button 5 on sixth joystick.
        Joystick6Button6, //Button 6 on sixth joystick.
        Joystick6Button7, //Button 7 on sixth joystick.
        Joystick6Button8, //Button 8 on sixth joystick.
        Joystick6Button9, //Button 9 on sixth joystick.
        Joystick6Button10, //Button 10 on sixth joystick.
        Joystick6Button11, //Button 11 on sixth joystick.
        Joystick6Button12, //Button 12 on sixth joystick.
        Joystick6Button13, //Button 13 on sixth joystick.
        Joystick6Button14, //Button 14 on sixth joystick.
        Joystick6Button15, //Button 15 on sixth joystick.
        Joystick6Button16, //Button 16 on sixth joystick.
        Joystick6Button17, //Button 17 on sixth joystick.
        Joystick6Button18, //Button 18 on sixth joystick.
        Joystick6Button19, //Button 19 on sixth joystick.
        Joystick7Button0, //Button 0 on seventh joystick.
        Joystick7Button1, //Button 1 on seventh joystick.
        Joystick7Button2, //Button 2 on seventh joystick.
        Joystick7Button3, //Button 3 on seventh joystick.
        Joystick7Button4, //Button 4 on seventh joystick.
        Joystick7Button5, //Button 5 on seventh joystick.
        Joystick7Button6, //Button 6 on seventh joystick.
        Joystick7Button7, //Button 7 on seventh joystick.
        Joystick7Button8, //Button 8 on seventh joystick.
        Joystick7Button9, //Button 9 on seventh joystick.
        Joystick7Button10, //Button 10 on seventh joystick.
        Joystick7Button11, //Button 11 on seventh joystick.
        Joystick7Button12, //Button 12 on seventh joystick.
        Joystick7Button13, //Button 13 on seventh joystick.
        Joystick7Button14, //Button 14 on seventh joystick.
        Joystick7Button15, //Button 15 on seventh joystick.
        Joystick7Button16, //Button 16 on seventh joystick.
        Joystick7Button17, //Button 17 on seventh joystick.
        Joystick7Button18, //Button 18 on seventh joystick.
        Joystick7Button19, //Button 19 on seventh joystick.
        Joystick8Button0, //Button 0 on eighth joystick.
        Joystick8Button1, //Button 1 on eighth joystick.
        Joystick8Button2, //Button 2 on eighth joystick.
        Joystick8Button3, //Button 3 on eighth joystick.
        Joystick8Button4, //Button 4 on eighth joystick.
        Joystick8Button5, //Button 5 on eighth joystick.
        Joystick8Button6, //Button 6 on eighth joystick.
        Joystick8Button7, //Button 7 on eighth joystick.
        Joystick8Button8, //Button 8 on eighth joystick.
        Joystick8Button9, //Button 9 on eighth joystick.
        Joystick8Button10, //Button 10 on eighth joystick.
        Joystick8Button11, //Button 11 on eighth joystick.
        Joystick8Button12, //Button 12 on eighth joystick.
        Joystick8Button13, //Button 13 on eighth joystick.
        Joystick8Button14, //Button 14 on eighth joystick.
        Joystick8Button15, //Button 15 on eighth joystick.
        Joystick8Button16, //Button 16 on eighth joystick.
        Joystick8Button17, //Button 17 on eighth joystick.
        Joystick8Button18, //Button 18 on eighth joystick.
        Joystick8Button19, //Button 19 on eighth joystick.
#endif
    }

    public enum FilterMode {
        Point,
    }

    public enum HideFlags {
        HideAndDontSave,
    }

    public enum TextureFormat {
        RGBA32,
    }

    public static unsafe class Application {
        public static bool isFocused = true;
        public static bool isPlaying = true;
        public static bool isEditor = false;
        public static string persistentDataPath = "./";

        public static IntPtr renderer;
        public static IntPtr window;
        public static long nowMs;
        public static int deltaMs;

        public static Action<string> Log = s => {};
        public static Action<string> Error = s => {};
        
        public static Action Init = () => {};
        public static Action Tick = () => {};
        public static Action Done = () => {};
        public static Action<string> OnText = s => {};
        public static Action<KeyCode> OnKey = kc => {};

        public static void Run( string [] argv ) {
            SDL_Init( SDL_INIT_VIDEO );
            SDL_WindowFlags flags = SDL_WINDOW_RESIZABLE;
            SDL_CreateWindowAndRenderer( 1024, 768, flags, out window, out renderer );
            SDL_GetRendererInfo( renderer, out SDL_RendererInfo info );
            SDL_SetWindowTitle( window, $"Radical Rumble {UTF8_ToManaged( info.name )}" );
            
            //for ( int i = 0; i < SDL_GetNumRenderDrivers(); i++ ) {
            {
                //SDL_GetRenderDriverInfo( i, out SDL_RendererInfo info );
            }

            Init();

            while ( true ) {
                SDL_GetWindowSize( window, out int w, out int h );
                Screen.width = w;
                Screen.height = h;
                Time.Tick();

                //SDL_SetRenderDrawColor( renderer, 40, 45, 50, 255 );
                //SDL_RenderClear( renderer );

                Array.Clear( Input.keyEvents, 0, Input.keyEvents.Length );

                while ( SDL_PollEvent( out SDL_Event ev ) != 0 ) {
                    SDL_Keycode code = ev.key.keysym.sym;
                    switch( ev.type ) {
                        case SDL_TEXTINPUT:
                            byte [] b = new byte[SDL_TEXTINPUTEVENT_TEXT_SIZE];
                            Marshal.Copy( ( IntPtr )ev.text.text, b, 0, b.Length );
                            string txt = System.Text.Encoding.UTF8.GetString( b, 0, b.Length );
                            OnText( txt );
                            break;

                        case SDL_KEYDOWN: {
                            if ( Input._sdlKeyToKeyCode.TryGetValue( code, out KeyCode kc ) ) {
                                OnKey( kc );
                                if ( kc == KeyCode.None ) {
                                    Error( $"Can't find key map for {code}" );
                                }
                                Input.keyEvents[( int )kc * 2 + 0] = 1;
                            } }
                            break;

                        case SDL_KEYUP: {
                            if ( Input._sdlKeyToKeyCode.TryGetValue( code, out KeyCode kc ) ) {
                                if ( kc == KeyCode.None ) {
                                    Error( $"Can't find key map for {code}" );
                                }
                                Input.keyEvents[( int )kc * 2 + 1] = 1;
                            } }
                            break;

                        case SDL_MOUSEMOTION:
                            Input.mousePosition = new Vector2( ev.motion.x,
                                                    // we try to emulate unity mouse position ffs
                                                    Screen.height - ev.motion.y );
                            break;

                        case SDL_MOUSEBUTTONDOWN:
                            break;

                        case SDL_MOUSEBUTTONUP:
                            break;

                        case SDL_QUIT:
                            goto quit;

                        default:
                            break;
                    }
                }

                Tick();

                SDL_RenderPresent( renderer );
            }

quit:

            Done();

            SDL_DestroyRenderer( renderer );
            SDL_DestroyWindow( window );
            SDL_Quit();
        }
    }

    public static class Time {
        public static float deltaTime;
        public static float realtimeSinceStartup;
        public static float unscaledTime;
        public static float time;

        static ulong _beginTime;
        static ulong _last;
        static ulong _now;
        static double _deltaTimeDouble = 0;
        static double _timeSinceStartDouble = 0;

        public static void Tick() {
            if ( _beginTime == 0 ) {
                _beginTime = SDL_GetPerformanceCounter();
            }

            _last = _now;
            _now = SDL_GetPerformanceCounter();

            _deltaTimeDouble = (double)((_now - _last) / (double)SDL_GetPerformanceFrequency() );
            _timeSinceStartDouble = (double)((_now - _beginTime) / (double)SDL_GetPerformanceFrequency() );

            deltaTime = ( float )_deltaTimeDouble;
            time = unscaledTime = realtimeSinceStartup = ( float )_timeSinceStartDouble;
        }
    }

    public static class Input {
        public static Vector2 mousePosition;

        public static bool GetKeyDown( KeyCode kc ) { return keyEvents[( int )kc * 2 + 0] != 0; }
        public static bool GetKeyUp( KeyCode kc ) { return keyEvents[( int )kc * 2 + 1] != 0; }

        // make it private
        public static readonly Dictionary<SDL_Keycode,KeyCode> _sdlKeyToKeyCode
                                                            = new Dictionary<SDL_Keycode,KeyCode>();
        public static readonly KeyCode [] AllKeyCodes
                                                = ( KeyCode[] )Enum.GetValues( typeof( KeyCode ) );
        public static byte [] keyEvents = new byte[AllKeyCodes.Length * 2];

        static Input() {
            void sdlK( SDL_Keycode sdlk, KeyCode k ) {
                _sdlKeyToKeyCode[sdlk] = k;
            }

			sdlK( SDLK_UNKNOWN, KeyCode.None );

			sdlK( SDLK_RETURN, KeyCode.Return );
			sdlK( SDLK_ESCAPE, KeyCode.Escape );
			sdlK( SDLK_BACKSPACE, KeyCode.Backspace );
			sdlK( SDLK_TAB, KeyCode.Tab );
			sdlK( SDLK_SPACE, KeyCode.Space );
			sdlK( SDLK_EXCLAIM, KeyCode.Exclaim );
			sdlK( SDLK_QUOTEDBL, KeyCode.DoubleQuote );
			sdlK( SDLK_HASH, KeyCode.Hash );
			sdlK( SDLK_PERCENT, KeyCode.None );
			sdlK( SDLK_DOLLAR, KeyCode.Dollar );
			sdlK( SDLK_AMPERSAND, KeyCode.Ampersand );
			sdlK( SDLK_QUOTE, KeyCode.Quote );
			sdlK( SDLK_LEFTPAREN, KeyCode.LeftParen );
			sdlK( SDLK_RIGHTPAREN, KeyCode.RightParen );
			sdlK( SDLK_ASTERISK, KeyCode.Asterisk );
			sdlK( SDLK_PLUS, KeyCode.Plus );
			sdlK( SDLK_COMMA, KeyCode.Comma );
			sdlK( SDLK_MINUS, KeyCode.Minus );
			sdlK( SDLK_PERIOD, KeyCode.Period );
			sdlK( SDLK_SLASH, KeyCode.Slash );
			sdlK( SDLK_0, KeyCode.Alpha0 );
			sdlK( SDLK_1, KeyCode.Alpha1 );
			sdlK( SDLK_2, KeyCode.Alpha2 );
			sdlK( SDLK_3, KeyCode.Alpha3 );
			sdlK( SDLK_4, KeyCode.Alpha4 );
			sdlK( SDLK_5, KeyCode.Alpha5 );
			sdlK( SDLK_6, KeyCode.Alpha6 );
			sdlK( SDLK_7, KeyCode.Alpha7 );
			sdlK( SDLK_8, KeyCode.Alpha8 );
			sdlK( SDLK_9, KeyCode.Alpha9 );
			sdlK( SDLK_COLON, KeyCode.Colon );
			sdlK( SDLK_SEMICOLON, KeyCode.Semicolon );
			sdlK( SDLK_LESS, KeyCode.Less );
			sdlK( SDLK_EQUALS, KeyCode.Equals );
			sdlK( SDLK_GREATER, KeyCode.None );
			sdlK( SDLK_QUESTION, KeyCode.Question );
			sdlK( SDLK_AT, KeyCode.At );
			sdlK( SDLK_LEFTBRACKET, KeyCode.LeftBracket );
			sdlK( SDLK_BACKSLASH, KeyCode.Backslash );
			sdlK( SDLK_RIGHTBRACKET, KeyCode.RightBracket );
			sdlK( SDLK_CARET, KeyCode.Caret );
			sdlK( SDLK_UNDERSCORE, KeyCode.Underscore );
			sdlK( SDLK_BACKQUOTE, KeyCode.BackQuote );
			sdlK( SDLK_a, KeyCode.A );
			sdlK( SDLK_b, KeyCode.B );
			sdlK( SDLK_c, KeyCode.C );
			sdlK( SDLK_d, KeyCode.D );
			sdlK( SDLK_e, KeyCode.E );
			sdlK( SDLK_f, KeyCode.F );
			sdlK( SDLK_g, KeyCode.G );
			sdlK( SDLK_h, KeyCode.H );
			sdlK( SDLK_i, KeyCode.I );
			sdlK( SDLK_j, KeyCode.J );
			sdlK( SDLK_k, KeyCode.K );
			sdlK( SDLK_l, KeyCode.L );
			sdlK( SDLK_m, KeyCode.M );
			sdlK( SDLK_n, KeyCode.N );
			sdlK( SDLK_o, KeyCode.O );
			sdlK( SDLK_p, KeyCode.P );
			sdlK( SDLK_q, KeyCode.Q );
			sdlK( SDLK_r, KeyCode.R );
			sdlK( SDLK_s, KeyCode.S );
			sdlK( SDLK_t, KeyCode.T );
			sdlK( SDLK_u, KeyCode.U );
			sdlK( SDLK_v, KeyCode.V );
			sdlK( SDLK_w, KeyCode.W );
			sdlK( SDLK_x, KeyCode.X );
			sdlK( SDLK_y, KeyCode.Y );
			sdlK( SDLK_z, KeyCode.Z );

			sdlK( SDLK_CAPSLOCK, KeyCode.CapsLock );

			sdlK( SDLK_F1, KeyCode.F1 );
			sdlK( SDLK_F2, KeyCode.F2 );
			sdlK( SDLK_F3, KeyCode.F3 );
			sdlK( SDLK_F4, KeyCode.F4 );
			sdlK( SDLK_F5, KeyCode.F5 );
			sdlK( SDLK_F6, KeyCode.F6 );
			sdlK( SDLK_F7, KeyCode.F7 );
			sdlK( SDLK_F8, KeyCode.F8 );
			sdlK( SDLK_F9, KeyCode.F9 );
			sdlK( SDLK_F10, KeyCode.F10 );
			sdlK( SDLK_F11, KeyCode.F11 );
			sdlK( SDLK_F12, KeyCode.F12 );

			sdlK( SDLK_PRINTSCREEN, KeyCode.None );
			sdlK( SDLK_SCROLLLOCK, KeyCode.ScrollLock );
			sdlK( SDLK_PAUSE, KeyCode.Pause );
			sdlK( SDLK_INSERT, KeyCode.Insert );
			sdlK( SDLK_HOME, KeyCode.Home );
			sdlK( SDLK_PAGEUP, KeyCode.PageUp );
			sdlK( SDLK_DELETE, KeyCode.Delete );
			sdlK( SDLK_END, KeyCode.End );
			sdlK( SDLK_PAGEDOWN, KeyCode.PageDown );
			sdlK( SDLK_RIGHT, KeyCode.RightArrow );
			sdlK( SDLK_LEFT, KeyCode.LeftArrow );
			sdlK( SDLK_DOWN, KeyCode.DownArrow );
			sdlK( SDLK_UP, KeyCode.UpArrow );

			sdlK( SDLK_NUMLOCKCLEAR, KeyCode.Numlock );
			sdlK( SDLK_KP_DIVIDE, KeyCode.KeypadDivide );
			sdlK( SDLK_KP_MULTIPLY, KeyCode.KeypadMultiply );
			sdlK( SDLK_KP_MINUS, KeyCode.KeypadMinus );
			sdlK( SDLK_KP_PLUS, KeyCode.KeypadPlus );
			sdlK( SDLK_KP_ENTER, KeyCode.KeypadEnter );
			sdlK( SDLK_KP_1, KeyCode.Keypad0 );
			sdlK( SDLK_KP_2, KeyCode.Keypad1 );
			sdlK( SDLK_KP_3, KeyCode.Keypad2 );
			sdlK( SDLK_KP_4, KeyCode.Keypad3 );
			sdlK( SDLK_KP_5, KeyCode.Keypad4 );
			sdlK( SDLK_KP_6, KeyCode.Keypad5 );
			sdlK( SDLK_KP_7, KeyCode.Keypad6 );
			sdlK( SDLK_KP_8, KeyCode.Keypad7 );
			sdlK( SDLK_KP_9, KeyCode.Keypad8 );
			sdlK( SDLK_KP_0, KeyCode.Keypad9 );
			sdlK( SDLK_KP_PERIOD, KeyCode.KeypadPeriod );

			sdlK( SDLK_APPLICATION, KeyCode.None );
			sdlK( SDLK_POWER, KeyCode.None );
			sdlK( SDLK_KP_EQUALS, KeyCode.None );
			sdlK( SDLK_F13, KeyCode.None );
			sdlK( SDLK_F14, KeyCode.None );
			sdlK( SDLK_F15, KeyCode.None );
			sdlK( SDLK_F16, KeyCode.None );
			sdlK( SDLK_F17, KeyCode.None );
			sdlK( SDLK_F18, KeyCode.None );
			sdlK( SDLK_F19, KeyCode.None );
			sdlK( SDLK_F20, KeyCode.None );
			sdlK( SDLK_F21, KeyCode.None );
			sdlK( SDLK_F22, KeyCode.None );
			sdlK( SDLK_F23, KeyCode.None );
			sdlK( SDLK_F24, KeyCode.None );
			sdlK( SDLK_EXECUTE, KeyCode.None );
			sdlK( SDLK_HELP, KeyCode.None );
			sdlK( SDLK_MENU, KeyCode.None );
			sdlK( SDLK_SELECT, KeyCode.None );
			sdlK( SDLK_STOP, KeyCode.None );
			sdlK( SDLK_AGAIN, KeyCode.None );
			sdlK( SDLK_UNDO, KeyCode.None );
			sdlK( SDLK_CUT, KeyCode.None );
			sdlK( SDLK_COPY, KeyCode.None );
			sdlK( SDLK_PASTE, KeyCode.None );
			sdlK( SDLK_FIND, KeyCode.None );
			sdlK( SDLK_MUTE, KeyCode.None );
			sdlK( SDLK_VOLUMEUP, KeyCode.None );
			sdlK( SDLK_VOLUMEDOWN, KeyCode.None );
			sdlK( SDLK_KP_COMMA, KeyCode.None );
			sdlK( SDLK_KP_EQUALSAS400, KeyCode.None );

			sdlK( SDLK_ALTERASE, KeyCode.None );
			sdlK( SDLK_SYSREQ, KeyCode.None );
			sdlK( SDLK_CANCEL, KeyCode.None );
			sdlK( SDLK_CLEAR, KeyCode.None );
			sdlK( SDLK_PRIOR, KeyCode.None );
			sdlK( SDLK_RETURN2, KeyCode.None );
			sdlK( SDLK_SEPARATOR, KeyCode.None );
			sdlK( SDLK_OUT, KeyCode.None );
			sdlK( SDLK_OPER, KeyCode.None );
			sdlK( SDLK_CLEARAGAIN, KeyCode.None );
			sdlK( SDLK_CRSEL, KeyCode.None );
			sdlK( SDLK_EXSEL, KeyCode.None );

			sdlK( SDLK_KP_00, KeyCode.None );
			sdlK( SDLK_KP_000, KeyCode.None );
			sdlK( SDLK_THOUSANDSSEPARATOR, KeyCode.None );
			sdlK( SDLK_DECIMALSEPARATOR, KeyCode.None );
			sdlK( SDLK_CURRENCYUNIT, KeyCode.None );
			sdlK( SDLK_CURRENCYSUBUNIT, KeyCode.None );
			sdlK( SDLK_KP_LEFTPAREN, KeyCode.None );
			sdlK( SDLK_KP_RIGHTPAREN, KeyCode.None );
			sdlK( SDLK_KP_LEFTBRACE, KeyCode.None );
			sdlK( SDLK_KP_RIGHTBRACE, KeyCode.None );
			sdlK( SDLK_KP_TAB, KeyCode.None );
			sdlK( SDLK_KP_BACKSPACE, KeyCode.None );
			sdlK( SDLK_KP_A, KeyCode.None );
			sdlK( SDLK_KP_B, KeyCode.None );
			sdlK( SDLK_KP_C, KeyCode.None );
			sdlK( SDLK_KP_D, KeyCode.None );
			sdlK( SDLK_KP_E, KeyCode.None );
			sdlK( SDLK_KP_F, KeyCode.None );
			sdlK( SDLK_KP_XOR, KeyCode.None );
			sdlK( SDLK_KP_POWER, KeyCode.None );
			sdlK( SDLK_KP_PERCENT, KeyCode.None );
			sdlK( SDLK_KP_LESS, KeyCode.None );
			sdlK( SDLK_KP_GREATER, KeyCode.None );
			sdlK( SDLK_KP_AMPERSAND, KeyCode.None );
			sdlK( SDLK_KP_DBLAMPERSAND, KeyCode.None );
			sdlK( SDLK_KP_VERTICALBAR, KeyCode.None );
			sdlK( SDLK_KP_DBLVERTICALBAR, KeyCode.None );
			sdlK( SDLK_KP_COLON, KeyCode.None );
			sdlK( SDLK_KP_HASH, KeyCode.None );
			sdlK( SDLK_KP_SPACE, KeyCode.None );
			sdlK( SDLK_KP_AT, KeyCode.None );
			sdlK( SDLK_KP_EXCLAM, KeyCode.None );
			sdlK( SDLK_KP_MEMSTORE, KeyCode.None );
			sdlK( SDLK_KP_MEMRECALL, KeyCode.None );
			sdlK( SDLK_KP_MEMCLEAR, KeyCode.None );
			sdlK( SDLK_KP_MEMADD, KeyCode.None );
			sdlK( SDLK_KP_MEMSUBTRACT, KeyCode.None );
			sdlK( SDLK_KP_MEMMULTIPLY, KeyCode.None );
			sdlK( SDLK_KP_MEMDIVIDE, KeyCode.None );
			sdlK( SDLK_KP_PLUSMINUS, KeyCode.None );
			sdlK( SDLK_KP_CLEAR, KeyCode.None );
			sdlK( SDLK_KP_CLEARENTRY, KeyCode.None );
			sdlK( SDLK_KP_BINARY, KeyCode.None );
			sdlK( SDLK_KP_OCTAL, KeyCode.None );
			sdlK( SDLK_KP_DECIMAL, KeyCode.None );
			sdlK( SDLK_KP_HEXADECIMAL, KeyCode.None );

			sdlK( SDLK_LCTRL, KeyCode.LeftControl );
			sdlK( SDLK_LSHIFT, KeyCode.LeftShift );
			sdlK( SDLK_LALT, KeyCode.LeftAlt );
			sdlK( SDLK_LGUI, KeyCode.None );
			sdlK( SDLK_RCTRL, KeyCode.RightControl );
			sdlK( SDLK_RSHIFT, KeyCode.RightShift );
			sdlK( SDLK_RALT, KeyCode.RightAlt );
			sdlK( SDLK_RGUI, KeyCode.None );

			sdlK( SDLK_MODE, KeyCode.None );

			sdlK( SDLK_AUDIONEXT, KeyCode.None );
			sdlK( SDLK_AUDIOPREV, KeyCode.None );
			sdlK( SDLK_AUDIOSTOP, KeyCode.None );
			sdlK( SDLK_AUDIOPLAY, KeyCode.None );
			sdlK( SDLK_AUDIOMUTE, KeyCode.None );
			sdlK( SDLK_MEDIASELECT, KeyCode.None );
			sdlK( SDLK_WWW, KeyCode.None );
			sdlK( SDLK_MAIL, KeyCode.None );
			sdlK( SDLK_CALCULATOR, KeyCode.None );
			sdlK( SDLK_COMPUTER, KeyCode.None );
			sdlK( SDLK_AC_SEARCH, KeyCode.None );
			sdlK( SDLK_AC_HOME, KeyCode.None );
			sdlK( SDLK_AC_BACK, KeyCode.None );
			sdlK( SDLK_AC_FORWARD, KeyCode.None );
			sdlK( SDLK_AC_STOP, KeyCode.None );
			sdlK( SDLK_AC_REFRESH, KeyCode.None );
			sdlK( SDLK_AC_BOOKMARKS, KeyCode.None );

			sdlK( SDLK_BRIGHTNESSDOWN, KeyCode.None );
			sdlK( SDLK_BRIGHTNESSUP, KeyCode.None );
			sdlK( SDLK_DISPLAYSWITCH, KeyCode.None );
			sdlK( SDLK_KBDILLUMTOGGLE, KeyCode.None );
			sdlK( SDLK_KBDILLUMDOWN, KeyCode.None );
			sdlK( SDLK_KBDILLUMUP, KeyCode.None );
			sdlK( SDLK_EJECT, KeyCode.None );
			sdlK( SDLK_SLEEP, KeyCode.None );
			sdlK( SDLK_APP1, KeyCode.None );
			sdlK( SDLK_APP2, KeyCode.None );

			sdlK( SDLK_AUDIOREWIND, KeyCode.None );
			sdlK( SDLK_AUDIOFASTFORWARD, KeyCode.None );
        }
    }

    public static class Screen {
        public static int width, height;
    }

    public class Camera {
        public int pixelWidth, pixelHeight;

        public static implicit operator bool( Camera c ) => c != null;
        public static Camera main = null;//new Camera();

        public Vector2 WorldToScreenPoint( Vector3 pt ) { return Vector2.zero; }
    }

    public class Texture {
        public int width, height;
        public static implicit operator bool( Texture t ) => t != null;
        public IntPtr sdlTex;
    }

    public class Texture2D : Texture {
        public FilterMode filterMode;

        public Texture2D() {}

        public Texture2D( int width, int height ) {
            Create( width, height );
        }

        public Texture2D( int width, int height, TextureFormat textureFormat,
                                                                    bool mipChain, bool linear ) {
            Create( width, height );
        }

        public static Texture2D whiteTexture = new Texture2D();

        List<byte> _buf = new List<byte>();
        public void SetPixel( int x, int y, Color32 color ) {
            int pitch = width * 4;
            int sz = pitch * height;
            if ( _buf.Count != sz ) {
                _buf.Clear();
                for ( int i = 0; i < sz; i++ ) {
                    _buf.Add( 0 );
                }
            }
            _buf[x * 4 + y * pitch + 0] = color.r;
            _buf[x * 4 + y * pitch + 1] = color.b;
            _buf[x * 4 + y * pitch + 2] = color.g;
            _buf[x * 4 + y * pitch + 3] = color.a;
        }

        public void Apply() {
            Update( _buf.ToArray() );
            _buf.Clear();
        }

        void Create( int w, int h ) {
            width = w;
            height = h;
            SDL_SetHint( SDL_HINT_RENDER_SCALE_QUALITY, "0" );
            sdlTex = SDL_CreateTexture( Application.renderer, SDL_PIXELFORMAT_ABGR8888, 
                                                ( int )SDL_TEXTUREACCESS_STATIC, width, height );
        }

        void Update( byte [] bytes ) {
            IntPtr unmanagedPointer = Marshal.AllocHGlobal( bytes.Length );
            Marshal.Copy( bytes, 0, unmanagedPointer, bytes.Length );
            SDL_UpdateTexture( sdlTex, IntPtr.Zero, unmanagedPointer, width * 4 );
        }
    }

    public class Shader {
        public static implicit operator bool( Shader sh ) => sh != null;
        public static Shader Find( string name ) { return new Shader(); }
    }

    public class Material {
        public static implicit operator bool( Material mat ) => mat != null;
        public Color color;
        public Texture texture;

        public Material( Shader s ) {}

        public void SetPass( int p ) {
            GL.texture = texture;
        }

        public void SetTexture( string name, Texture tex ) {
            texture = tex;
        }

        public void SetColor( string name, Color val ) {}

        public HideFlags hideFlags;
    }

    public static unsafe class GL {
        public const int QUADS = 0;
        public const int LINES = 1;

        public static Texture texture;

        const int MAX_VERTS = 256 * 1024;
        const int MAX_INDS = 256 * 1024;

        static SDL_Color _color;

        static int _mode;

        static int _numVertices;
        static SDL_Vertex *_vertices = ( SDL_Vertex* )Marshal.AllocHGlobal( MAX_VERTS * SizeOf<SDL_Vertex>() );

        static int _numIndices;
        static int *_indices = ( int* )Marshal.AllocHGlobal( MAX_INDS * SizeOf<int>() );
        
        public static void Begin( int mode ) {
            _mode = mode;
            _numVertices = 0;
            _numIndices = 0;
        }

        public static void End() {
            if ( _numVertices > MAX_VERTS ) {
                Application.Error( $"Out of vertices: {_numVertices}" );
            }
            if ( _numIndices > MAX_INDS ) {
                Application.Error( $"Out of indices: {_numIndices}" );
            }
            if ( _mode == QUADS ) {
                //SDL_SetRenderDrawColor( Application.renderer, 255, 255, 255, 255 );
                //SDL_SetRenderDrawBlendMode( Application.renderer, SDL_BLENDMODE_BLEND );
                //SDL_SetTextureAlphaMod( texture.sdlTex, 0xff );
                SDL_SetTextureBlendMode( texture.sdlTex, SDL_BLENDMODE_BLEND );
                SDL_RenderGeometry( Application.renderer, texture.sdlTex, _vertices, _numVertices,
                                                                            _indices, _numIndices );
            } else if ( _mode == LINES ) {
                SDL_SetRenderDrawBlendMode( Application.renderer, SDL_BLENDMODE_BLEND );
                for ( int i = 0; i < _numVertices - 1; i += 2 ) {
                    SDL_Vertex v0 = _vertices[( i + 0 ) & ( MAX_VERTS - 1 )];
                    SDL_Vertex v1 = _vertices[( i + 1 ) & ( MAX_VERTS - 1 )];
                    SDL_FPoint p0 = v0.position;
                    SDL_FPoint p1 = v1.position;
                    SDL_SetRenderDrawColor( Application.renderer, v0.color.r, v0.color.g,
                                                                        v0.color.b, v0.color.a );
                    SDL_RenderDrawLineF( Application.renderer, p0.x, p0.y, p1.x, p1.y );
                }
            }
        }

        public static void Color( Color32 color ) {
            _color = new SDL_Color { r = color.r, g = color.g, b = color.b, a = color.a, };
        }

        public static void TexCoord( Vector3 uv ) {
            var p = new SDL_FPoint { x = uv.x, y = uv.y };
            _vertices[_numVertices & ( MAX_VERTS - 1 )].tex_coord = p;
        }

        public static void Vertex( Vector3 v ) {
            var p = new SDL_FPoint { x = v.x, y = v.y };
            int nv = _numVertices & ( MAX_VERTS - 1 );

            _vertices[nv].position = p;
            _vertices[nv].color = _color;
            _numVertices++;

            if ( _mode == QUADS && ( _numVertices & 3 ) == 0 ) {
                const int mask = MAX_INDS - 1;
                int bv = ( _numVertices - 4 ) & ( MAX_VERTS - 1 );

                _indices[( _numIndices + 0 ) & mask] = bv + 0;
                _indices[( _numIndices + 1 ) & mask] = bv + 1;
                _indices[( _numIndices + 2 ) & mask] = bv + 2;

                _indices[( _numIndices + 3 ) & mask] = bv + 3;
                _indices[( _numIndices + 4 ) & mask] = bv + 0;
                _indices[( _numIndices + 5 ) & mask] = bv + 2;

                _numIndices = Mathf.Min( _numIndices + 6, MAX_INDS );
            }
        }

        public static void LoadPixelMatrix() {}
        public static void PushMatrix() {}
        public static void PopMatrix() {}
    }
}


