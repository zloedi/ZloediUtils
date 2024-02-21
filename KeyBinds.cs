#if UNITY_STANDALONE || UNITY_2021_0_OR_NEWER || SDL

using System;
using System.Collections.Generic;

#if UNITY_STANDALONE
using UnityEngine;
#else
using SDLPorts;
#endif

public static class KeyBinds {

public static Action<string> Log = s => {};
public static Action<string> Error = s => {};

public static readonly KeyCode[] keys = new KeyCode [] {
    KeyCode.None, //Not assigned (never returned as the result of a keystroke).
    KeyCode.Backspace, //The backspace key.
    KeyCode.Delete, //The forward delete key.
    KeyCode.Tab, //The tab key.
    KeyCode.Clear, //The Clear key.
    KeyCode.Return, //Return key.
    KeyCode.Pause, //Pause on PC machines.
    KeyCode.Escape, //Escape key.
    KeyCode.Space, //Space key.
    KeyCode.Keypad0, //Numeric keypad 0.
    KeyCode.Keypad1, //Numeric keypad 1.
    KeyCode.Keypad2, //Numeric keypad 2.
    KeyCode.Keypad3, //Numeric keypad 3.
    KeyCode.Keypad4, //Numeric keypad 4.
    KeyCode.Keypad5, //Numeric keypad 5.
    KeyCode.Keypad6, //Numeric keypad 6.
    KeyCode.Keypad7, //Numeric keypad 7.
    KeyCode.Keypad8, //Numeric keypad 8.
    KeyCode.Keypad9, //Numeric keypad 9.
    KeyCode.KeypadPeriod, //Numeric keypad '.'.
    KeyCode.KeypadDivide, //Numeric keypad '/'.
    KeyCode.KeypadMultiply, //Numeric keypad '*'.
    KeyCode.KeypadMinus, //Numeric keypad '='.
    KeyCode.KeypadPlus, //Numeric keypad '+'.
    KeyCode.KeypadEnter, //Numeric keypad enter.
    KeyCode.KeypadEquals, //Numeric keypad '='.
    KeyCode.UpArrow, //Up arrow key.
    KeyCode.DownArrow, //Down arrow key.
    KeyCode.RightArrow, //Right arrow key.
    KeyCode.LeftArrow, //Left arrow key.
    KeyCode.Insert, //Insert key key.
    KeyCode.Home, //Home key.
    KeyCode.End, //End key.
    KeyCode.PageUp, //Page up.
    KeyCode.PageDown, //Page down.
    KeyCode.F1, //F1 function key.
    KeyCode.F2, //F2 function key.
    KeyCode.F3, //F3 function key.
    KeyCode.F4, //F4 function key.
    KeyCode.F5, //F5 function key.
    KeyCode.F6, //F6 function key.
    KeyCode.F7, //F7 function key.
    KeyCode.F8, //F8 function key.
    KeyCode.F9, //F9 function key.
    KeyCode.F10, //F10 function key.
    KeyCode.F11, //F11 function key.
    KeyCode.F12, //F12 function key.
    KeyCode.F13, //F13 function key.
    KeyCode.F14, //F14 function key.
    KeyCode.F15, //F15 function key.
    KeyCode.Alpha0, //The '0' key on the top of the alphanumeric keyboard.
    KeyCode.Alpha1, //The '1' key on the top of the alphanumeric keyboard.
    KeyCode.Alpha2, //The '2' key on the top of the alphanumeric keyboard.
    KeyCode.Alpha3, //The '3' key on the top of the alphanumeric keyboard.
    KeyCode.Alpha4, //The '4' key on the top of the alphanumeric keyboard.
    KeyCode.Alpha5, //The '5' key on the top of the alphanumeric keyboard.
    KeyCode.Alpha6, //The '6' key on the top of the alphanumeric keyboard.
    KeyCode.Alpha7, //The '7' key on the top of the alphanumeric keyboard.
    KeyCode.Alpha8, //The '8' key on the top of the alphanumeric keyboard.
    KeyCode.Alpha9, //The '9' key on the top of the alphanumeric keyboard.
    KeyCode.Exclaim, //Exclamation mark key '!'.
    KeyCode.DoubleQuote, //Double quote key '"'.
    KeyCode.Hash, //Hash key '#'.
    KeyCode.Dollar, //Dollar sign key '$'.
    KeyCode.Ampersand, //Ampersand key '&'.
    KeyCode.Quote, //Quote key '.
    KeyCode.LeftParen, //Left Parenthesis key '('.
    KeyCode.RightParen, //Right Parenthesis key ')'.
    KeyCode.Asterisk, //Asterisk key '*'.
    KeyCode.Plus, //Plus key '+'.
    KeyCode.Comma, //Comma ',' key.
    KeyCode.Minus, //Minus '-' key.
    KeyCode.Equals, //Equals '=' key.
    KeyCode.Period, //Period '.' key.
    KeyCode.Slash, //Slash '/' key.
    KeyCode.Colon, //Colon ':' key.
    KeyCode.Semicolon, //Semicolon ',' key.
    KeyCode.Less, //Less than '' key.
    KeyCode.Question, //Question mark '?' key.
    KeyCode.At, //At key '@'.
    KeyCode.LeftBracket, //Left square bracket key '['.
    KeyCode.Backslash, //Backslash key '\'.
    KeyCode.RightBracket, //Right square bracket key ']'.
    KeyCode.Caret, //Caret key '^'.
    KeyCode.Underscore, //Underscore '_' key.
    KeyCode.BackQuote, //Back quote key '`'.
    KeyCode.A, //'a' key.
    KeyCode.B, //'b' key.
    KeyCode.C, //'c' key.
    KeyCode.D, //'d' key.
    KeyCode.E, //'e' key.
    KeyCode.F, //'f' key.
    KeyCode.G, //'g' key.
    KeyCode.H, //'h' key.
    KeyCode.I, //'i' key.
    KeyCode.J, //'j' key.
    KeyCode.K, //'k' key.
    KeyCode.L, //'l' key.
    KeyCode.M, //'m' key.
    KeyCode.N, //'n' key.
    KeyCode.O, //'o' key.
    KeyCode.P, //'p' key.
    KeyCode.Q, //'q' key.
    KeyCode.R, //'r' key.
    KeyCode.S, //'s' key.
    KeyCode.T, //'t' key.
    KeyCode.U, //'u' key.
    KeyCode.V, //'v' key.
    KeyCode.W, //'w' key.
    KeyCode.X, //'x' key.
    KeyCode.Y, //'y' key.
    KeyCode.Z, //'z' key.
    KeyCode.Numlock, //Numlock key.
    KeyCode.CapsLock, //Capslock key.
    KeyCode.ScrollLock, //Scroll lock key.
    KeyCode.RightShift, //Right shift key.
    KeyCode.LeftShift, //Left shift key.
    KeyCode.RightControl, //Right Control key.
    KeyCode.LeftControl, //Left Control key.
    KeyCode.RightAlt, //Right Alt key.
    KeyCode.LeftAlt, //Left Alt key.

    // already added (not unique)
    // KeyCode.LeftCommand, //Left Command key.

    KeyCode.LeftApple, //Left Command key.
    KeyCode.LeftWindows, //Left Windows key.
    KeyCode.RightCommand, //Right Command key.

    // already added (not unique)
    //KeyCode.RightApple, //Right Command key.

    KeyCode.RightWindows, //Right Windows key.
    KeyCode.AltGr, //Alt Gr key.
    KeyCode.Help, //Help key.
    KeyCode.Print, //Print key.
    KeyCode.SysReq, //Sys Req key.
    KeyCode.Break, //Break key.
    KeyCode.Menu, //Menu key.
    KeyCode.Mouse0, //First (primary) mouse button.
    KeyCode.Mouse1, //Second (secondary) mouse button.
    KeyCode.Mouse2, //Third mouse button.
    KeyCode.Mouse3, //Fourth mouse button.
    KeyCode.Mouse4, //Fifth mouse button.
    KeyCode.Mouse5, //Sixth mouse button.
    KeyCode.Mouse6, //Seventh mouse button.

#if false // no joystick
    KeyCode.JoystickButton0, //Button 0 on any joystick.
    KeyCode.JoystickButton1, //Button 1 on any joystick.
    KeyCode.JoystickButton2, //Button 2 on any joystick.
    KeyCode.JoystickButton3, //Button 3 on any joystick.
    KeyCode.JoystickButton4, //Button 4 on any joystick.
    KeyCode.JoystickButton5, //Button 5 on any joystick.
    KeyCode.JoystickButton6, //Button 6 on any joystick.
    KeyCode.JoystickButton7, //Button 7 on any joystick.
    KeyCode.JoystickButton8, //Button 8 on any joystick.
    KeyCode.JoystickButton9, //Button 9 on any joystick.
    KeyCode.JoystickButton10, //Button 10 on any joystick.
    KeyCode.JoystickButton11, //Button 11 on any joystick.
    KeyCode.JoystickButton12, //Button 12 on any joystick.
    KeyCode.JoystickButton13, //Button 13 on any joystick.
    KeyCode.JoystickButton14, //Button 14 on any joystick.
    KeyCode.JoystickButton15, //Button 15 on any joystick.
    KeyCode.JoystickButton16, //Button 16 on any joystick.
    KeyCode.JoystickButton17, //Button 17 on any joystick.
    KeyCode.JoystickButton18, //Button 18 on any joystick.
    KeyCode.JoystickButton19, //Button 19 on any joystick.
    KeyCode.Joystick1Button0, //Button 0 on first joystick.
    KeyCode.Joystick1Button1, //Button 1 on first joystick.
    KeyCode.Joystick1Button2, //Button 2 on first joystick.
    KeyCode.Joystick1Button3, //Button 3 on first joystick.
    KeyCode.Joystick1Button4, //Button 4 on first joystick.
    KeyCode.Joystick1Button5, //Button 5 on first joystick.
    KeyCode.Joystick1Button6, //Button 6 on first joystick.
    KeyCode.Joystick1Button7, //Button 7 on first joystick.
    KeyCode.Joystick1Button8, //Button 8 on first joystick.
    KeyCode.Joystick1Button9, //Button 9 on first joystick.
    KeyCode.Joystick1Button10, //Button 10 on first joystick.
    KeyCode.Joystick1Button11, //Button 11 on first joystick.
    KeyCode.Joystick1Button12, //Button 12 on first joystick.
    KeyCode.Joystick1Button13, //Button 13 on first joystick.
    KeyCode.Joystick1Button14, //Button 14 on first joystick.
    KeyCode.Joystick1Button15, //Button 15 on first joystick.
    KeyCode.Joystick1Button16, //Button 16 on first joystick.
    KeyCode.Joystick1Button17, //Button 17 on first joystick.
    KeyCode.Joystick1Button18, //Button 18 on first joystick.
    KeyCode.Joystick1Button19, //Button 19 on first joystick.
    KeyCode.Joystick2Button0, //Button 0 on second joystick.
    KeyCode.Joystick2Button1, //Button 1 on second joystick.
    KeyCode.Joystick2Button2, //Button 2 on second joystick.
    KeyCode.Joystick2Button3, //Button 3 on second joystick.
    KeyCode.Joystick2Button4, //Button 4 on second joystick.
    KeyCode.Joystick2Button5, //Button 5 on second joystick.
    KeyCode.Joystick2Button6, //Button 6 on second joystick.
    KeyCode.Joystick2Button7, //Button 7 on second joystick.
    KeyCode.Joystick2Button8, //Button 8 on second joystick.
    KeyCode.Joystick2Button9, //Button 9 on second joystick.
    KeyCode.Joystick2Button10, //Button 10 on second joystick.
    KeyCode.Joystick2Button11, //Button 11 on second joystick.
    KeyCode.Joystick2Button12, //Button 12 on second joystick.
    KeyCode.Joystick2Button13, //Button 13 on second joystick.
    KeyCode.Joystick2Button14, //Button 14 on second joystick.
    KeyCode.Joystick2Button15, //Button 15 on second joystick.
    KeyCode.Joystick2Button16, //Button 16 on second joystick.
    KeyCode.Joystick2Button17, //Button 17 on second joystick.
    KeyCode.Joystick2Button18, //Button 18 on second joystick.
    KeyCode.Joystick2Button19, //Button 19 on second joystick.
    KeyCode.Joystick3Button0, //Button 0 on third joystick.
    KeyCode.Joystick3Button1, //Button 1 on third joystick.
    KeyCode.Joystick3Button2, //Button 2 on third joystick.
    KeyCode.Joystick3Button3, //Button 3 on third joystick.
    KeyCode.Joystick3Button4, //Button 4 on third joystick.
    KeyCode.Joystick3Button5, //Button 5 on third joystick.
    KeyCode.Joystick3Button6, //Button 6 on third joystick.
    KeyCode.Joystick3Button7, //Button 7 on third joystick.
    KeyCode.Joystick3Button8, //Button 8 on third joystick.
    KeyCode.Joystick3Button9, //Button 9 on third joystick.
    KeyCode.Joystick3Button10, //Button 10 on third joystick.
    KeyCode.Joystick3Button11, //Button 11 on third joystick.
    KeyCode.Joystick3Button12, //Button 12 on third joystick.
    KeyCode.Joystick3Button13, //Button 13 on third joystick.
    KeyCode.Joystick3Button14, //Button 14 on third joystick.
    KeyCode.Joystick3Button15, //Button 15 on third joystick.
    KeyCode.Joystick3Button16, //Button 16 on third joystick.
    KeyCode.Joystick3Button17, //Button 17 on third joystick.
    KeyCode.Joystick3Button18, //Button 18 on third joystick.
    KeyCode.Joystick3Button19, //Button 19 on third joystick.
    KeyCode.Joystick4Button0, //Button 0 on forth joystick.
    KeyCode.Joystick4Button1, //Button 1 on forth joystick.
    KeyCode.Joystick4Button2, //Button 2 on forth joystick.
    KeyCode.Joystick4Button3, //Button 3 on forth joystick.
    KeyCode.Joystick4Button4, //Button 4 on forth joystick.
    KeyCode.Joystick4Button5, //Button 5 on forth joystick.
    KeyCode.Joystick4Button6, //Button 6 on forth joystick.
    KeyCode.Joystick4Button7, //Button 7 on forth joystick.
    KeyCode.Joystick4Button8, //Button 8 on forth joystick.
    KeyCode.Joystick4Button9, //Button 9 on forth joystick.
    KeyCode.Joystick4Button10, //Button 10 on forth joystick.
    KeyCode.Joystick4Button11, //Button 11 on forth joystick.
    KeyCode.Joystick4Button12, //Button 12 on forth joystick.
    KeyCode.Joystick4Button13, //Button 13 on forth joystick.
    KeyCode.Joystick4Button14, //Button 14 on forth joystick.
    KeyCode.Joystick4Button15, //Button 15 on forth joystick.
    KeyCode.Joystick4Button16, //Button 16 on forth joystick.
    KeyCode.Joystick4Button17, //Button 17 on forth joystick.
    KeyCode.Joystick4Button18, //Button 18 on forth joystick.
    KeyCode.Joystick4Button19, //Button 19 on forth joystick.
    KeyCode.Joystick5Button0, //Button 0 on fifth joystick.
    KeyCode.Joystick5Button1, //Button 1 on fifth joystick.
    KeyCode.Joystick5Button2, //Button 2 on fifth joystick.
    KeyCode.Joystick5Button3, //Button 3 on fifth joystick.
    KeyCode.Joystick5Button4, //Button 4 on fifth joystick.
    KeyCode.Joystick5Button5, //Button 5 on fifth joystick.
    KeyCode.Joystick5Button6, //Button 6 on fifth joystick.
    KeyCode.Joystick5Button7, //Button 7 on fifth joystick.
    KeyCode.Joystick5Button8, //Button 8 on fifth joystick.
    KeyCode.Joystick5Button9, //Button 9 on fifth joystick.
    KeyCode.Joystick5Button10, //Button 10 on fifth joystick.
    KeyCode.Joystick5Button11, //Button 11 on fifth joystick.
    KeyCode.Joystick5Button12, //Button 12 on fifth joystick.
    KeyCode.Joystick5Button13, //Button 13 on fifth joystick.
    KeyCode.Joystick5Button14, //Button 14 on fifth joystick.
    KeyCode.Joystick5Button15, //Button 15 on fifth joystick.
    KeyCode.Joystick5Button16, //Button 16 on fifth joystick.
    KeyCode.Joystick5Button17, //Button 17 on fifth joystick.
    KeyCode.Joystick5Button18, //Button 18 on fifth joystick.
    KeyCode.Joystick5Button19, //Button 19 on fifth joystick.
    KeyCode.Joystick6Button0, //Button 0 on sixth joystick.
    KeyCode.Joystick6Button1, //Button 1 on sixth joystick.
    KeyCode.Joystick6Button2, //Button 2 on sixth joystick.
    KeyCode.Joystick6Button3, //Button 3 on sixth joystick.
    KeyCode.Joystick6Button4, //Button 4 on sixth joystick.
    KeyCode.Joystick6Button5, //Button 5 on sixth joystick.
    KeyCode.Joystick6Button6, //Button 6 on sixth joystick.
    KeyCode.Joystick6Button7, //Button 7 on sixth joystick.
    KeyCode.Joystick6Button8, //Button 8 on sixth joystick.
    KeyCode.Joystick6Button9, //Button 9 on sixth joystick.
    KeyCode.Joystick6Button10, //Button 10 on sixth joystick.
    KeyCode.Joystick6Button11, //Button 11 on sixth joystick.
    KeyCode.Joystick6Button12, //Button 12 on sixth joystick.
    KeyCode.Joystick6Button13, //Button 13 on sixth joystick.
    KeyCode.Joystick6Button14, //Button 14 on sixth joystick.
    KeyCode.Joystick6Button15, //Button 15 on sixth joystick.
    KeyCode.Joystick6Button16, //Button 16 on sixth joystick.
    KeyCode.Joystick6Button17, //Button 17 on sixth joystick.
    KeyCode.Joystick6Button18, //Button 18 on sixth joystick.
    KeyCode.Joystick6Button19, //Button 19 on sixth joystick.
    KeyCode.Joystick7Button0, //Button 0 on seventh joystick.
    KeyCode.Joystick7Button1, //Button 1 on seventh joystick.
    KeyCode.Joystick7Button2, //Button 2 on seventh joystick.
    KeyCode.Joystick7Button3, //Button 3 on seventh joystick.
    KeyCode.Joystick7Button4, //Button 4 on seventh joystick.
    KeyCode.Joystick7Button5, //Button 5 on seventh joystick.
    KeyCode.Joystick7Button6, //Button 6 on seventh joystick.
    KeyCode.Joystick7Button7, //Button 7 on seventh joystick.
    KeyCode.Joystick7Button8, //Button 8 on seventh joystick.
    KeyCode.Joystick7Button9, //Button 9 on seventh joystick.
    KeyCode.Joystick7Button10, //Button 10 on seventh joystick.
    KeyCode.Joystick7Button11, //Button 11 on seventh joystick.
    KeyCode.Joystick7Button12, //Button 12 on seventh joystick.
    KeyCode.Joystick7Button13, //Button 13 on seventh joystick.
    KeyCode.Joystick7Button14, //Button 14 on seventh joystick.
    KeyCode.Joystick7Button15, //Button 15 on seventh joystick.
    KeyCode.Joystick7Button16, //Button 16 on seventh joystick.
    KeyCode.Joystick7Button17, //Button 17 on seventh joystick.
    KeyCode.Joystick7Button18, //Button 18 on seventh joystick.
    KeyCode.Joystick7Button19, //Button 19 on seventh joystick.
    KeyCode.Joystick8Button0, //Button 0 on eighth joystick.
    KeyCode.Joystick8Button1, //Button 1 on eighth joystick.
    KeyCode.Joystick8Button2, //Button 2 on eighth joystick.
    KeyCode.Joystick8Button3, //Button 3 on eighth joystick.
    KeyCode.Joystick8Button4, //Button 4 on eighth joystick.
    KeyCode.Joystick8Button5, //Button 5 on eighth joystick.
    KeyCode.Joystick8Button6, //Button 6 on eighth joystick.
    KeyCode.Joystick8Button7, //Button 7 on eighth joystick.
    KeyCode.Joystick8Button8, //Button 8 on eighth joystick.
    KeyCode.Joystick8Button9, //Button 9 on eighth joystick.
    KeyCode.Joystick8Button10, //Button 10 on eighth joystick.
    KeyCode.Joystick8Button11, //Button 11 on eighth joystick.
    KeyCode.Joystick8Button12, //Button 12 on eighth joystick.
    KeyCode.Joystick8Button13, //Button 13 on eighth joystick.
    KeyCode.Joystick8Button14, //Button 14 on eighth joystick.
    KeyCode.Joystick8Button15, //Button 15 on eighth joystick.
    KeyCode.Joystick8Button16, //Button 16 on eighth joystick.
    KeyCode.Joystick8Button17, //Button 17 on eighth joystick.
    KeyCode.Joystick8Button18, //Button 18 on eighth joystick.
    KeyCode.Joystick8Button19, //Button 19 on eighth joystick.
#endif
};

private static Dictionary<string,string[]> _bindContext = new Dictionary<string,string[]>();
private static Dictionary<KeyCode,int> _keyToIndex = new Dictionary<KeyCode,int>();

static KeyBinds() {
    for ( int i = 0; i < keys.Length; i++ ) {
        _keyToIndex[keys[i]] = i;
    }
}

public static void BindClearAll_kmd( string [] argv ) {
    _bindContext.Clear();
    Log( "Cleared all Key Bindings." );
}

public static void Bind_kmd( string [] argv ) {
    if ( argv.Length < 3 ) {
        Log( "Usage: bind <KeyCode> <command> [context]" );
        foreach ( var kv in _bindContext ) {
            string context = kv.Key;
            string [] ctxCommands = kv.Value;
            for ( int i = 0; i < keys.Length; i++ ) {
                KeyCode key = keys[i];
                string cmd = ctxCommands[i];
                if ( cmd.Length > 0 ) {
                    Log( $"{key}: [ff9000]{cmd}[-] {context}" );
                }
            }
        }
        return;
    }

    {

    string context = "";
    if ( argv.Length > 3 ) {
        context = argv[3];
    }

    string [] ctxCommands;
    if ( ! _bindContext.TryGetValue( context, out ctxCommands ) ) {
        ctxCommands = new string[keys.Length];
        for ( int i = 0; i < ctxCommands.Length; i++ ) {
            ctxCommands[i] = "";
        }
        _bindContext[context] = ctxCommands;
    }

    if ( Enum.TryParse( argv[1], out KeyCode code ) ) {
        int idx;
        if ( _keyToIndex.TryGetValue( code, out idx ) ) {
            ctxCommands[idx] = argv[2];
        } else {
            Error( "Unsupported key code " + code );
        }
        //Log( "Bound command " + argv[2] + " to key " + code );
    } else {
        Error( "Couldn't find " + argv[1] + " in valid keys." );
    }

    }
}

public static bool GetCmd( KeyCode k, string context, out string cmd ) {
    context = context == null ? "" : context;
    string [] ctxCommands;
    if ( _bindContext.TryGetValue( context, out ctxCommands ) ) {
        cmd = ctxCommands[_keyToIndex[k]];
    } else {
        cmd = "";
    }
    return cmd.Length > 0;
}

public static string StoreConfig() {
    Log( "Store keybinds to config file." );
    string cfg = "";
    foreach ( var kv in _bindContext ) {
        string context = kv.Key;
        string [] ctxCommands = kv.Value;
        for ( int i = 0; i < keys.Length; i++ ) {
            KeyCode key = keys[i];
            string cmd = ctxCommands[i];
            if ( cmd.Length > 0 ) {
                cfg += $"bind {key} \"{cmd}\" {context}\n";
            }
        }
    }
    return cfg;
}

private static bool Execute( KeyCode key, string cmdLine ) {
    //Log( "Execute key binding: " + key + " " + cmdLine );
    return Cellophane.TryExecuteString( cmdLine );
}

public static bool TryExecuteBinds( KeyCode keyDown = KeyCode.None, KeyCode keyUp = KeyCode.None,
                                                                    KeyCode keyHold = KeyCode.None,
                                                                    string context = null ) {
#if KEYBINDS_LEGACY
    string cmd;
    foreach ( var key in keys ) {
        if ( Input.GetKeyDown( key ) && GetCmd( key, context, out cmd ) ) {
            if ( cmd[0] != '+' && cmd[0] != '-' ) {
                Execute( key, cmd );
            } else if ( cmd[0] == '+' ) {
                Execute( key, cmd.Substring( 1 ) );
            }
        } else if ( Input.GetKeyUp( key ) && GetCmd( key, context, out cmd ) ) {
            if ( cmd[0] == '-' ) {
                Execute( key, cmd.Substring( 1 ) );
            }
        }
    }
#else
    bool result = false;
    string cmd;

    if ( GetCmd( keyDown, context, out cmd ) ) {
        if ( cmd[0] != '-' && cmd[0] != '+' ) {
            if ( Execute( keyDown, cmd ) ) result = true;
        }
    }

    if ( GetCmd( keyHold, context, out cmd ) ) {
        if ( cmd[0] == '+' ) {
            if ( Execute( keyHold, cmd.Substring( 1 ) ) ) result = true;
        }
    }

    if ( GetCmd( keyUp, context, out cmd ) ) {
        if ( cmd[0] == '-' ) {
            if ( Execute( keyUp, cmd.Substring( 1 ) ) ) result = true;
        }
    }

    return result;
#endif
}


}

#endif
