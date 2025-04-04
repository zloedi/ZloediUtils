/*
   Cellophane is a 'shell-like' interface for C# programs
   Exports vars and commands to command line using reflection
   Storage of config and history
   Command-line interpreter
   Autocomplete
*/

using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Globalization;
using System;

public static class Cellophane {

public class Named : IComparable {
    public string description = "";
    public string rawName;
    public string name;

    public int CompareTo(object obj) {
        if ( obj == null ) 
            return 1;
        string other = obj as string;
        return this.name.CompareTo( other );
    }
}

public class Command : Named {
    public Action<string[],object> ActionArgv = (sa,context) => {};
}

public class Variable : Named {
    public FieldInfo fieldInfo;

    public Func<bool> Changed_f;

    public string GetValue() {
        if ( fieldInfo.FieldType == typeof( float ) ) {
            return FtoA( ( float )fieldInfo.GetValue( null ) ); 
        }
        if ( fieldInfo.FieldType == typeof( string ) ) {
            return ( string )fieldInfo.GetValue( null );
        }
        return fieldInfo.GetValue( null ).ToString();
    }

    public void SetupChanged<T>() {
        T oldValue = ( T )fieldInfo.GetValue( null );
        Changed_f = () => {
            T newVal = ( T )fieldInfo.GetValue( null );
            bool result = ! oldValue.Equals( newVal );
            oldValue = newVal;
            return result;
        };
    }

    public void SetupChanged() {
        if ( fieldInfo.FieldType == typeof( bool ) ) {
            SetupChanged<bool>();
        } else if ( fieldInfo.FieldType == typeof( int ) ) {
            SetupChanged<int>();
        } else if ( fieldInfo.FieldType == typeof( float ) ) {
            SetupChanged<float>();
        } else if ( fieldInfo.FieldType == typeof( string ) ) {
            SetupChanged<string>();
        } else {
            Changed_f = () => false;
            Error( "Unknown var type: " + fieldInfo.FieldType );
        }
    }

    public Action<string> SetValue_f;
}

static Command [] _commands = new Command[0];
static Variable [] _variables = new Variable[0];
static int [] _orderedDist;
static string [] _allNames;
static List<string> _history = new List<string>();

// when sorting the autocomplete buffer, anything above this distance doesn't 
// contain the match candidate
const int AutocompleteBorderDist = 10000;

static void History_kmd( string [] argv ) {
    foreach ( var h in _history ) {
        Log("  " + h);
    }
    Log("======");
}

static void ls_kmd( string [] argv ) {
    List_kmd( argv );
}

static void List_kmd( string [] argv ) {
    if ( Array.IndexOf(argv, "--raw") > -1 ) {
        foreach ( var c in _commands ) {
            Log( c.name + " -> " + c.rawName + "()" );
        } 
        Log( "" );
        foreach ( var v in _variables ) {
            Log( v.name + " -> " + v.rawName );
        } 
    } else {
        foreach ( var c in _commands ) {
            string str = c.name;
            if ( c.description.Length > 0 ) {
                str += " : " + c.description;
            }
            Log( str );
        }
        Log( "" );
        foreach ( var v in _variables ) {
            string str = v.name + " = " + v.GetValue();
            if ( v.description.Length > 0 ) {
                str += " : " + v.description;
            }
            Log( str );
        }
    }
    Log( "" );
    Log( "Num Commands: " + _commands.Length );
    Log( "Num Variables: " + _variables.Length );
    Log( "Use with --raw to show function names." );
}

static Variable VarCreate( Type type, FieldInfo fi ) {
    Variable cvar = new Variable {
        fieldInfo = fi,
        name = FieldNameToVarName( type, fi ),
        rawName = type.Name + "." + fi.Name,
    };

    foreach ( var ca in fi.GetCustomAttributes() ) {
        DescriptionAttribute da = ca as DescriptionAttribute;
        if ( da != null ) {
            cvar.description = da.Description;
        }
    }

    VarSetValueUpdate( cvar );
    return cvar;
}

static void VarSetValueUpdate( Variable cvar ) {
    var fi = cvar.fieldInfo;

    if ( fi.FieldType == typeof( bool ) ) {
        cvar.SetValue_f = (s) => {
            string tl = s.ToLowerInvariant();
            bool val = tl != "false" && tl != "0";
            cvar.fieldInfo.SetValue( null, val );
        };
    } else if ( fi.FieldType == typeof( int ) ) {
        cvar.SetValue_f = (s) => {
            int i;
            if ( int.TryParse( s, out i ) ) {
                cvar.fieldInfo.SetValue( null, i );
            }
        };
    } else if ( fi.FieldType == typeof( float ) ) {
        cvar.SetValue_f = (s) => {
            if ( AtoF( s, out float f ) ) {
                cvar.fieldInfo.SetValue( null, f );
            }
        };
    } else if ( fi.FieldType == typeof( string ) ) {
        cvar.SetValue_f = s => cvar.fieldInfo.SetValue( null, s );
    } else {
        Error( "Cellophane: Unsupported type of var " + fi.FieldType.ToString() );
    }
}

static bool CmdIsValid( MethodInfo mi ) {
    if ( ! ValidSuffixCmd( mi.Name ) ) {
        return false;
    }
    ParameterInfo[] parameters = mi.GetParameters();
    if ( parameters.Length == 0 ) {
        return false;
    }
    ParameterInfo pi = parameters[0];
    if ( ! pi.ParameterType.IsArray ) {
        return false;
    }
    if ( pi.ParameterType.GetElementType() != typeof( string ) ) {
        return false;
    }
    return true;
}

static Command CmdCreate( Type type, MethodInfo mi ) {
    Command cmd = new Command {
        name = MethodNameToCmdName( type, mi ),
        rawName = type.Name + "." + mi.Name,
    };

    foreach ( var ca in mi.GetCustomAttributes() ) {
        DescriptionAttribute da = ca as DescriptionAttribute;
        if ( da != null ) {
            cmd.description = da.Description;
        }
    }

    CmdCallbackUpdate( cmd, mi );
    return cmd;
}

static void CmdCallbackUpdate( Command cmd, MethodInfo mi ) {
    ParameterInfo[] parameters = mi.GetParameters();
    if ( parameters.Length == 1 ) {
        var d = mi.CreateDelegate( typeof( Action<string[]> ) ) as Action<string[]>;
        cmd.ActionArgv = (argv,context) => {
            d( argv );
        };
    } else {
        var pt = parameters[1].ParameterType;
        var objs2 = new object[2];
        cmd.ActionArgv = (argv,context) => {
            if ( context != null ) {
                if ( context.GetType() == pt ) {
                    objs2[0] = argv;
                    objs2[1] = context;
                    mi.Invoke( mi, objs2 );
                } else {
                    Error( $"cmd.name requires '{pt}' context but got '{context.GetType()}'" );
                }
            } else {
                Error( $"cmd.name requires '{pt}' context but got null" );
            }
        };
    }
}

static string FieldNameToVarName( Type type, FieldInfo fi ) {
    string name = fi.Name.EndsWith( "_kvar" ) ? fi.Name : type.Name + "_" + fi.Name;
    return NormalizeNameVar( name );
}

static string MethodNameToCmdName( Type type, MethodInfo mi ) {
    string name = mi.Name.EndsWith("_kmd") ? mi.Name : type.Name + "_" + mi.Name;
    return NormalizeNameCmd( name );
}

static bool ValidSuffixVar( string name ) {
    return name.EndsWith( "_cvar" ) || name.EndsWith( "_kvar" );
}

static bool ValidSuffixCmd( string name ) {
    return name.EndsWith( "_cmd" ) || name.EndsWith( "_kmd" );
}

static string NormalizeNameVar( string varName ) {
    if ( varName.Length < 6 ) {
        return varName.ToLowerInvariant();
    }
    int n = ValidSuffixVar( varName ) ? varName.Length - 5 : varName.Length;
    return NormalizeName( varName, n );
}

static string NormalizeNameCmd( string cmdName ) {
    if ( cmdName.Length < 5 ) {
        return cmdName.ToLowerInvariant();
    }
    int n = ValidSuffixCmd( cmdName ) ? cmdName.Length - 4 : cmdName.Length;
    return NormalizeName( cmdName, n );
}

static void OnVariable( Variable cvar, string [] argv ) {
    if ( argv.Length > 1 ) {
        int i;
        for ( i = 1; i < argv.Length; i++ ) {
            if ( ! argv[i].Contains( "=" ) ) {
                break;
            }
        }
        if ( i == argv.Length && argv[i - 1] == "=" ) {
            // user forces empty string
            cvar.SetValue_f( "" );
        } else {
            cvar.SetValue_f( argv[i] );
            Log( cvar.name + " = " + cvar.GetValue() );
        }
    } else {
        string log = cvar.name + " = " + cvar.GetValue();
        if ( ! string.IsNullOrEmpty( cvar.description ) ) {
            Log( log + " : " + cvar.description );
        } else {
            Log( log );
        }
    }
}

static string SubstrToFirstDiff( string a, string b ) {
    string result = "";
    int n = Math.Min( a.Length, b.Length );

    for ( int i = 0; i < n; i++ ) {
        if ( a[i] != b[i] ) {
            break;
        }
        result += a[i];
    }
    return result;
}

static int LevenshteinDistance( string s, string t ) {
    int n = s.Length;
    int m = t.Length;
    int[,] d = new int[n + 1, m + 1];

    // Step 1
    if (n == 0)
    {
        return m;
    }

    if (m == 0)
    {
        return n;
    }

    // Step 2
    for (int i = 0; i <= n; d[i, 0] = i++)
    {
    }

    for (int j = 0; j <= m; d[0, j] = j++)
    {
    }

    // Step 3
    for (int i = 1; i <= n; i++)
    {
        //Step 4
        for (int j = 1; j <= m; j++)
        {
            // Step 5
            int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

            // Step 6
            d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
        }
    }
    // Step 7
    return d[n, m];
}

static string GetSorted( int k ) {
    return _allNames[_orderedDist[k] & 0xffff];
}

static int GetDist( int k ) {
    return _orderedDist[k] >> 16;
}

static void PrintSuggestions( int maxToPrint, string hilight = null, 
                                                bool skipWeakMatches = false ) {
    Log( "" );
    int i;
    for ( i = maxToPrint - 1; i >= 0; i-- ) {
        if ( GetDist( i ) < AutocompleteBorderDist ) {
            break;
        }
        if ( ! skipWeakMatches ) {
            Log( "[b0b0b0]" + GetSorted( i ) + "[-]" );
        }
    }
    string lit = "[ff9000]" + hilight + "[-]";
    for ( ; i >= 0; i-- ) {
        string raw = GetSorted( i );
        string str;
        if ( hilight != null ) {
            str = raw.Replace( hilight, lit );
        } else {
            str = raw;
        }
        if ( TryFindCommand( raw, out Command c ) ) {
            string log = str;
            log += c.description.Length > 0 ? ( "[c0c0c0] : " + c.description + "[-]" ) : "";
            Log( log );
        } else if ( TryFindVariable( raw, out Variable v ) ) {
            string log = str + " = " + v.GetValue();
            log += v.description.Length > 0 ? ( "[c0c0c0] : " + v.description + "[-]" ) : "";
            Log( log );
        } else {
            Log( str );
        }
    }
}

static void CollectItems( List<Command> cmds, List<Variable> vars ) {
    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
    List<Type> asmTypes = new List<Type>();
    int numAssemblies = 0;

    foreach ( var a in assemblies ) {
        string name = a.GetName().Name;
        if ( name == "System" ) continue;
        if ( name == "Mono.Security" ) continue;
        if ( name == "netstandard" ) continue;
        if ( name.Contains( "Newtonsoft" ) ) continue;
        if ( name.Contains( "steamworks" ) ) continue;
        if ( name.Contains( "Unity." ) ) continue;
        if ( name.Contains( ".Unity" ) ) continue;
        if ( name.Contains( "UnityEditor" ) ) continue;
        if ( name.Contains( "Engine." ) ) continue;
        if ( name.Contains( "UnityEngine" ) ) continue;
        if ( name.Contains( "System." ) ) continue;
        if ( name.Contains( "Photon" ) ) continue;
        if ( name.Contains( "Microsoft" ) ) continue;
        if ( name.Contains( "Autodesk" ) ) continue;
        if ( name.Contains( "Sony" ) ) continue;
        if ( name.Contains( "mscorlib" ) ) continue;
        if ( name.Contains( "BeeDriver" ) ) continue;
        if ( name.Contains( "UniTask" ) ) continue;
        if ( name.Contains( "Cinemachine" ) ) continue;
        if ( name.Contains( "com.unity" ) ) continue;

        numAssemblies++;
        asmTypes.AddRange( a.GetTypes() );
    }

    Type [] types;
    try {
        if ( asmTypes.Count > 0 ) {
            types = asmTypes.ToArray();
            Log( $"Num assemblies parsed: {numAssemblies}." );
        } else {
            types = Assembly.GetExecutingAssembly().GetTypes();
        }
    } catch(Exception e) {
        Error( "Cellophane: Failed to parse commands: " + e );
        _commands = new Command [] {};
        _variables = new Variable [] {};
        return;
    }

    cmds = cmds != null ? cmds : new List<Command>();
    vars = vars != null ? vars : new List<Variable>();

    foreach ( Type type in types ) {
        FieldInfo [] fields = type.GetFields( BFS );
        foreach ( FieldInfo fi in fields ) {
            if ( ! ValidSuffixVar( fi.Name ) ) {
                continue;
            }

            Variable cvar = VarCreate( type, fi );

            vars.Add( cvar );
        }

        MethodInfo [] methods = type.GetMethods( BFS );
        var objs2 = new object[2];
        foreach ( MethodInfo mi in methods ) {
            if ( ! CmdIsValid( mi ) ) {
                continue;
            }
            Command cmd = CmdCreate( type, mi );
            if ( cmd.name == "cellophane_on_register" ) {
                cmd.ActionArgv( new string [] { cmd.name, type.Name }, null );
            }
            cmds.Add(cmd);
        }
    }

    vars.Sort( (a,b) => string.Compare( a.name, b.name ) );
    _variables = vars.ToArray();

    cmds.Sort( (a,b) => string.Compare( a.name, b.name ) );
    _commands = cmds.ToArray();

    PostAdd();
}

static bool TryFindVariable( string str, out Variable v ) {
    int idx = Array.BinarySearch( _variables, str );  
    if ( idx >= 0 ) {
        v = _variables[idx];
        return true;
    }
    v = null;
    return false;
}

static bool TryFindCommand( string str, out Command c ) {
    int idx = Array.BinarySearch( _commands, str );  
    if ( idx >= 0 ) {
        c = _commands[idx];
        return true;
    }
    c = null;
    return false;
}

static void PostAdd() {
    int n = _variables.Length + _commands.Length;
    _orderedDist = new int[n];
    _allNames = new string[n];
    for ( int i = 0; i < _variables.Length; i++ ) {
        _allNames[i] = _variables[i].name;
    }
    for ( int i = 0; i < _commands.Length; i++ ) {
        _allNames[i + _variables.Length] = _commands[i].name;
    }
}

// == Public API ==

public const BindingFlags BFS = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

public static bool UseColor = false;
public static Action<string> Log = (s) => {};
public static Action<string> Error = (s) => {};

// optionally fill this from the app before reading the config
public static int ConfigVersion_kvar;

public static string NormalizeName( string name, int n = 0 ) {
    n = n == 0 ? name.Length : n;
    List<char> chars = new List<char>();
    for (int i = 0; i < n - 1; i++) {
        char curr = name[i];
        char next = name[i + 1];
        chars.Add(curr);
        if ( curr != '_' 
                && ! Char.IsUpper( curr )
                && Char.IsUpper( next ) ) {
            chars.Add( '_' );
        }
    }
    chars.Add( name[n - 1] );
    return new string( chars.ToArray() ).ToLowerInvariant();
}

public static string FtoA( float f ) {
    return Convert.ToString( f, CultureInfo.InvariantCulture ); 
}

public static bool AtoF( string a, out float f ) {
    return float.TryParse( a, NumberStyles.Any, CultureInfo.InvariantCulture, out f );
}

public static float AtoF( string a ) {
    float.TryParse( a, NumberStyles.Any, CultureInfo.InvariantCulture, out float f );
    return f;
}

public static bool TryFindCommand( string str, out Action<string[],object> action ) {
    if ( TryFindCommand( str, out Command c ) ) {
        action = c.ActionArgv;
        return true;
    }
    action = null;
    return false;
}

public static string Autocomplete( string input ) {
    if ( _allNames.Length < 2 ) {
        return input;
    }

    string [] argv;
    if ( ! GetArgv( input, out argv ) ) {
        return input;
    }

    string toMatch = argv[0].ToLowerInvariant();
    
    for (int i = 0; i < _allNames.Length; i++) {
        string name = _allNames[i];
        int d;
        if ( name == toMatch ) {
            d = 0;
        } else if ( name.Contains( toMatch ) ) {
            d = 1000;
        } else {
            d = AutocompleteBorderDist + LevenshteinDistance( toMatch, name );
        }
        _orderedDist[i] = ( d << 16 ) | i;
    }

    // sort by distance
    Array.Sort( _orderedDist );

    string autocomplete = toMatch;
    int maxToPrint = Math.Min( 32, _orderedDist.Length );

    if ( GetDist( 0 ) == 0 ) {
        // if filled full command, just print closest matches
        PrintSuggestions( maxToPrint );
    } else {
        // if top match contains the candidate and is more than levenshtein 
        // distance apart from the second match, this is the one true 
        // autocomplete candidate
        int d0 = GetDist( 0 );
        int d1 = GetDist( 1 );
        if ( d0 < AutocompleteBorderDist && d0 < d1 && d1 - d0 < AutocompleteBorderDist ) {
            autocomplete = GetSorted( 0 );
        } else {
            // if we ended up here, there is more than one match (containing the command line)
            PrintSuggestions( maxToPrint, hilight: autocomplete );
        }
    }

    input = autocomplete;
    for ( int i = 1; i < argv.Length; i++ ) {
        input += " " + argv[i];
    }

    return input;
}

public static void AddToHistory( string str ) {
    if ( ! string.IsNullOrEmpty( str ) ) {
        if ( _history.Count == 0 || str != _history[_history.Count - 1] ) {
            _history.Add( str );
            if ( _history.Count > 1024 ) {
                _history.RemoveAt( 0 );
            }
        }
    }
}

public static string StripJSONTags( string str ) {
    if ( GetArgv( str, out string [] argv, keepJsonTags: false ) ) {
        return argv[0];
    }
    return str;
}

static List<string> _argvTokens = new List<string>();
public static bool GetArgv( string str, out string [] argv, bool keepJsonTags = false,
                                                                        bool keepQuotes = false ) {
    if ( string.IsNullOrEmpty( str ) ) {
        argv = new string[0];
        // spams on the console too
        //Log( "GetArgv: Empty string to split." );
        return false;
    }

    _argvTokens.Clear();
    int comment = 0;
    int quoted = 0;
    string token = "";

    bool json = false;

    void addToken() {
        if ( token.StartsWith( "<json>" ) ) {
            token = token.Substring( "<json>".Length );
            json = true;
        }
        if ( token.EndsWith( "</json>" ) ) {
            token = token.Substring( 0, token.Length - "</json>".Length );
            if ( keepJsonTags ) {
                token = $"<json>{token}</json>";
            }
            json = false;
        }
        if ( comment < 2 && ! json && token.Length > 0 ) {
            if ( keepQuotes && quoted == 2 ) {
                _argvTokens.Add( '"' + token + '"' );
            } else {
                _argvTokens.Add( token );
            }
            token = "";
        }
    }

    for ( int i = 0; i < str.Length; i++ ) {
        int c = str[i];
        if ( json ) {
            token += ( char )c;
            if ( token.EndsWith( "</json>" ) ) {
                addToken();
            }
        } else {
            if ( comment >= 2 ) {
                token = "";
                if ( c == '\n' || c == '\r' ) {
                    comment = 0;
                }
            } else if ( c == '"' ) {
                quoted++;
                if ( quoted == 2 ) {
                    addToken();
                    quoted = 0;
                }
            } else if ( quoted == 0 && ( c == '\n' || c == '\r' || c == ' ' || c == '\t' ) ) {
                addToken();
            } else if ( quoted == 0 && c == '=' ) {
                addToken();
                token = "=";
                addToken();
            } else if ( quoted == 0 && c == ';' ) {
                addToken();
                token = ";";
                addToken();
            } else if ( quoted == 0 && c == '/' ) {
                comment++;
            } else {
                token += ( char )c;
                if ( token.StartsWith( "<json>" ) ) {
                    addToken();
                }
            }
        }
    }
    addToken();
    argv = _argvTokens.ToArray();
    return argv.Length > 0;
}

// bare minimum tokenizer -- no quotes, no json, no comments
public static bool GetArgvBare( string str, out string [] argv ) {
    _argvTokens.Clear();
    string token = "";

    void addToken() {
        if ( token.Length > 0 ) {
            _argvTokens.Add( token );
            token = "";
        }
    }

    for ( int i = 0; i < str.Length; i++ ) {
        int c = str[i];
        if ( c == '\n' || c == '\r' || c == ' ' || c == '\t' ) {
            addToken();
        } else if ( c == ';' ) {
            addToken();
            token = ";";
            addToken();
        } else {
            token += ( char )c;
        }
    }
    addToken();
    argv = _argvTokens.ToArray();
    return argv.Length > 0;
}

// ignores any quoted or json strings, just split along the semicolon
public static bool SimpleCommandsSplit( string str, out string [] cmds ) {
    cmds = str.Split( new []{';'}, StringSplitOptions.RemoveEmptyEntries ); 
    return cmds.Length > 0;
}

// handles properly ';' inside tags and quotes
static List<string> _splitCmds = new List<string>();
public static bool SplitCommands( string str, out string [] cmds ) {
    _splitCmds.Clear();
    if ( GetArgv( str, out string [] argv, keepJsonTags: true, keepQuotes: true ) ) {
        string cmd = argv[0];
        for ( int i = 1; i < argv.Length; i++ ) {
            if ( argv[i] == ";" ) {
                _splitCmds.Add( cmd + ';' );
                cmd = string.Empty;
            } else {
                cmd += $" {argv[i]}";
            }
        }
        _splitCmds.Add( cmd );
        cmds = _splitCmds.ToArray();
        return cmds.Length > 0;
    }
    cmds = new string[0];
    return false;
}

// handles properly ';' inside tags and quotes
static List<string> _exeTokens = new List<string>();
public static bool TryExecuteString( string str, object context = null, bool silent = false,
                                                                    bool keepJsonTags = false ) {
    if ( GetArgv( str, out string [] argv, keepJsonTags: keepJsonTags ) ) {
        TryExecuteArgv( argv, context, silent, keepJsonTags );
        return true;
    }
    return false;
}

public static void TryExecuteArgv( string [] argv, object context = null, bool silent = false,
                                                                    bool keepJsonTags = false ) {
    _exeTokens.Clear();
    for ( int i = 0; i < argv.Length; i++ ) {
        if ( argv[i] == ";" ) {
            if ( _exeTokens.Count > 0 ) {
                TryExecute( _exeTokens.ToArray(), context, silent );
                _exeTokens.Clear();
            }
        } else {
            _exeTokens.Add( argv[i] );
        }
    }
    if ( _exeTokens.Count > 0 ) {
        TryExecute( _exeTokens.ToArray(), context, silent );
    }
}

public static bool TryExecute( string [] argv, object context = null, bool silent = false ) {
    if ( argv.Length == 0 ) {
        if ( ! silent ) {
            Error( "Zero argv-s in TryExecute." );
        }
        return false;
    }

    string str = argv[0].ToLowerInvariant();

    Variable v;
    if ( TryFindVariable( str, out v ) ) {
        OnVariable( v, argv );
        return true;
    }

    string cmdName;
    int repeat = 0;

    if ( char.IsNumber( str[0] ) ) {
        cmdName = "";
        string count = "";
        for ( int i = 0; i < str.Length; i++ ) {
            char c = str[i];
            if ( ! char.IsNumber( c ) ) {
                if ( repeat == 0 && ! int.TryParse( count, out repeat ) ) {
                    repeat = 1;
                }
            } 
            if ( repeat == 0 ) {
                count += c;
            } else {
                cmdName += c;
            }
        }
    } else {
        cmdName = str;
        repeat = 1;
    }

    Command cmd;
    if ( TryFindCommand( cmdName, out cmd ) ) {
        for ( int i = 0; i < repeat; i++ ) {
            cmd.ActionArgv( argv, context );
        }
        return true;
    }
    if ( ! silent ) {
        Log( "Unknown var/command: '" + str + "'" );
    }
    return false;
}

public static string [] GetHistory( string currentCmd ) {
    List<string> res = new List<string>{ currentCmd };
    if ( currentCmd.Length > 0 ) {
        foreach ( var s in _history ) {
            if ( s.Contains( currentCmd ) ) {
                res.Add( s );
            }
        }
    } else {
        res.AddRange( _history );
    }
    return res.ToArray();
}

public static void ReadConfig( string val, bool skipVersionCheck = false ) {
    Log( "Parsing config..." );
    var vals = val.Split( new[]{'\n'}, StringSplitOptions.RemoveEmptyEntries ); 

    Action execute = () => {
        // silent while executing
        Action<string> tempLog = Log;
        Log = (s)=>{};
        foreach ( var v in vals ) {
            string [] argv;
            if ( GetArgv( v, out argv, keepJsonTags: true ) ) {
                TryExecute( argv );
            }
        }
        // no longer silent
        Log = tempLog;
    };

    if ( skipVersionCheck ) {
        Log( "Skipping version check." );
        execute();
    } else {
        // force version to some invalid value before parsing it out
        int srcVersion = ConfigVersion_kvar;
        ConfigVersion_kvar = -1;
        foreach ( var v in vals ) {
            string [] argv;
            if ( GetArgv( v, out argv) && argv[0] == "config_version" ) {
                TryExecute( argv );
                Log( $"Config Version in cfg file: {ConfigVersion_kvar} hardcoded: {srcVersion}" );
                break;
            }
        }
        if ( srcVersion == ConfigVersion_kvar ) {
            execute();
        } else {
            ConfigVersion_kvar = srcVersion;
            Log( "Resetting Variables to defaults because different hardcoded cfg version!" );
        }
    }

    Log( "Done parsing config." );
}

public static string StoreConfig() {
    string val = "";
    foreach ( var v in _variables ) {
        if ( v.description.Length > 0 ) {
            val += $"{v.name} {v.GetValue()}  // {v.description}\n";
        } else {
            val += $"{v.name} {v.GetValue()}\n";
        }
    }
    return val;
}

public static void ReadHistory( string val ) {
    var vals = val.Split( new[]{'\n'}, StringSplitOptions.RemoveEmptyEntries ); 
    _history = new List<string>( vals );
}

public static string StoreHistory() {
    string val = "";
    foreach ( var s in _history ) {
        val += s + "\n";
    }
    return val;
}

public static void PrintInfo() {
    Log( "Num variables: " + _variables.Length );
    Log( "Num commands: " + _commands.Length );
}

// can pass down some custom vars and commands before scanning the app assemblies
public static void ScanVarsAndCommands( List<Command> cmds = null, List<Variable> vars = null ) {
    var log = Log;
    Log = (s) => log( UseColor ? s : ColorTagStripAll( s ) );
    CollectItems( cmds, vars );
}

static readonly int tagLen = "[000000]".Length;
public static bool ColorTagLead( string str, int i, out string tag ) {
    tag = string.Empty;
    if ( str.Length <= i ) {
        return false;
    }
    if ( str[i] != '[' ) {
        return false;
    }
    int n = Math.Min( str.Length, i + tagLen );
    for ( int k = i; k < n; k++ ) {
        tag += str[k];
    }
    if ( tag.Length != tagLen || tag[tagLen - 1] != ']' ) {
        return false;
    }
    for ( int k = 1; k < tagLen - 1; k++ ) {
        if ( ! Uri.IsHexDigit( tag[k] ) ) {
            return false;
        }
    }
    return true;
}

public static bool ColorTagClose( string str, int i, out string tag ) {
    tag = "[-]";

    if ( str.Length - i < 3 ) {
        return false;
    }

    return str[i + 0] == '[' && str[i + 1] == '-' && str[i + 2] == ']';
}

public static string ColorTagStripAll( string s ) {
    string result = "";
    for ( int i = 0; i < s.Length; ) {
        string tag;
        if ( ColorTagLead( s, i, out tag ) ) {
            i += tag.Length;
        } else if ( ColorTagClose( s, i, out tag ) ) {
            i += tag.Length;
        } else {
            result += s[i];
            i++;
        }
    }
    return result;
}

static Dictionary<string,Variable> _changeStash = new Dictionary<string,Variable>();
public static bool VarChanged( string name, Type type = null ) {
    if ( name.EndsWith( "_cvar" ) ) {
        if ( type == null ) {
            Error( "Need to supply type for VarChanged for '_cvar'" );
            return false;
        }
        name = type.Name + "_" + name;
    }

    bool result;
    Variable v;
    if ( _changeStash.TryGetValue( name, out v ) ) {
        result = v.Changed_f();
    } else {
        string normName = NormalizeNameVar( name );
        if ( TryFindVariable( normName, out v ) ) {
            if ( v.Changed_f == null ) {
                v.SetupChanged();
            }
            result = v.Changed_f();
            _changeStash[name] = v;
        } else {
            Error( "Can't find variable '" + normName + "'" );
            //v.Changed_f = () => false;
            result = false;
        }
    }
    return result;
}

// replace existing cmds and vars with the ones coming from this assembly
public static void ImportAndReplace( Assembly assembly ) {
    Type [] types = assembly.GetTypes();

    var cvars = new List<Variable>();
    var cmds = new List<Command>();

    foreach ( Type type in types ) {
        FieldInfo [] fields = type.GetFields( BFS );
        foreach ( FieldInfo fi in fields ) {

            if ( ! ValidSuffixVar( fi.Name ) ) {
                continue;
            }

            string name = FieldNameToVarName( type, fi );

            int idx = Array.BinarySearch( _variables, name );  
            if ( idx < 0 ) {
                Variable cvar = VarCreate( type, fi );
                cvars.Add( cvar );
                Log( $"Added new var {cvar.name}: {cvar.GetValue()}" );
                continue;
            }

            if ( fi.FieldType == _variables[idx].fieldInfo.FieldType ) {
                fi.SetValue( null, _variables[idx].fieldInfo.GetValue( null ) );
                _variables[idx].fieldInfo = fi;
            } else {
                string oldValue = _variables[idx].GetValue();
                _variables[idx].fieldInfo = fi;
                VarSetValueUpdate( _variables[idx] );
                _variables[idx].SetValue_f( oldValue );
            }
            Log( $"Replaced field info on cvar {_variables[idx].name}: {_variables[idx].GetValue()}" );
        }

        MethodInfo [] methods = type.GetMethods( BFS );
        foreach ( MethodInfo mi in methods ) {
            if ( ! CmdIsValid( mi ) ) {
                continue;
            }
            string name = MethodNameToCmdName( type, mi );
            int idx = Array.BinarySearch( _commands, name );  
            if ( idx < 0 ) {
                Command cmd = CmdCreate( type, mi );
                cmds.Add( cmd );
                Log( $"Added new command -- {cmd.name}" );
            } else {
                CmdCallbackUpdate( _commands[idx], mi );
                Log( $"Replaced callback on command {_commands[idx].name}" );
            }
        }
    }

    cvars.AddRange( _variables );
    cvars.Sort( (a,b) => string.Compare( a.name, b.name ) );
    _variables = cvars.ToArray();

    cmds.AddRange( _commands );
    cmds.Sort( (a,b) => string.Compare( a.name, b.name ) );
    _commands = cmds.ToArray();
}

public static void AddCommands( IList<Command> cmds ) {
    var list = new List<Command>( cmds );
    list.AddRange( _commands );
    list.Sort( (a,b) => string.Compare( a.name, b.name ) );
    _commands = list.ToArray();
    PostAdd();
}

} // class Cellophane
