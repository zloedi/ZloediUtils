using System;
using System.Collections.Generic;
using System.Reflection;
using Libs.GameConsole;

public static class SnapshotCommands {


#if QONSOLE_BOOTSTRAP
public static Action<object> Log = o => Qonsole.Log( o );
public static Action<string> Error = o => Qonsole.Error( o );
#else
public static Action<object> Log = o => {};
public static Action<string> Error = s => {};
#endif

public static string asmLookupPath = "";

[ConsoleCommand(Command = "run_assembly")]
public static void RunAssembly( IConsole console, string path ) {
    RunAssembly_kmd( new string [] { "run_assembly", path } );
}

static void RunAssembly_kmd( string [] argv ) {
    string name = argv.Length > 1 ? argv[1] : "Qronos.dll";
    string path = asmLookupPath + name;
    if ( ! path.EndsWith( ".dll" ) ) {
        path += ".dll";
    }
    Log( "Loading " + path );
    try {
        Assembly asm = Assembly.LoadFrom( path );
        Log( "Qronos assembly loaded from " + path );
        List<ConsoleCommand> cmds = ConsoleCommandAttribute.LoadCommands( new Assembly [] { asm } );
        foreach ( var cmd in cmds ) {
            Log( "Found command " + cmd.Name );
            if ( cmd.Name == "assembly_entry_point" ) {
                cmd.Execute( null, "", new string [] { "" } );
            }
        }
    } catch ( Exception e ) {
        Log( "Qronos assembly not supplied at " + path );
        Log( e );
    }
}


}
