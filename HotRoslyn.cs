/*
How to integrate the Roslyn compiler into Unity:
- install the NuGet package: https://github.com/GlitchEnzo/NuGetForUnity.
- install Microsoft.CodeAnalysis and Microsoft.CodeAnalysis.CSharp NuGet packages.
- will have to degrade them to version 2 if don't need CSharp > 7, or ...
- ... disable Project Settings / Player / Other Settings / Assembly Version Validation
    to have LanguageVersion.CSharp9.

How to use this script:
- #define ROSLYN
- setup Log, Error, OnCompile, ScriptsRoot, ScriptFiles.
- invoke Init(), this will start listening for file changes under ScriptsRoot.
- invoke Update() i.e. on mono behaviour Update.
- eventually Update() will invoke OnCompile() passing down the newly compiled assembly.
- invoke Done() i.e. OnApplicationQuit().

TODO:
- debugger support
*/

#if ROSLYN

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp;

#if UNITY_STANDALONE
using UnityEngine;
#endif


public static class HotRoslyn {


public static Action<object> Log = o => {};
public static Action<object> Error = o => {};
public static Action<Assembly> OnCompile = a => {};
public static string ScriptsRoot = "";
public static string [] ScriptFiles = {};

static List<MetadataReference> _domainReferences = new List<MetadataReference>();

static int _numReloads;
static bool _initialized;
static bool _initializedCompiler;
static Assembly _roslynAssembly;
static FileSystemWatcher _watcher;

static readonly object _assemblyLock = new();

public static void TryInit() {
    if ( _initialized ) {
        return;
    }

    try {
        Log( $"Setting up File Watcher to {ScriptsRoot}..." );

        _watcher = new FileSystemWatcher( ScriptsRoot );
        _watcher.Filter = "*.cs";
        _watcher.NotifyFilter = NotifyFilters.LastWrite;

        _watcher.Changed += OnFileWatcherChange;
        _watcher.Error += OnFileWatcherError;

        _watcher.IncludeSubdirectories = true;
        _watcher.EnableRaisingEvents = true;

        _initialized = true;
    } catch ( Exception e ) {
        Error( e );
    }
}

// makes sure we are detached from the Unity editor on play mode off
// and the referenced assemblies can be reloaded on recompile there
public static void Done() {
    _domainReferences.Clear();
    _watcher?.Dispose();
    _watcher = null;
    _roslynAssembly = null;
    _initialized = false;
    _initializedCompiler = false;
}

public static void Update() {
    if ( _roslynAssembly != null ) {
        Assembly asm;

        lock( _assemblyLock ) {
            asm = _roslynAssembly;
            _roslynAssembly = null;
        }

        OnCompile( asm );
    }
}

static void TryInitCompiler() {
    if ( ! _initialized ) {
        Error( "Not initialized" );
        return;
    }

    if ( _initializedCompiler ) {
        return;
    }

    var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
    foreach ( var a in domainAssemblies ) {
        // will give conflicting Tuple<,>
        if ( a.FullName.Contains( "ExCSS.Unity" ) ) {
            continue;
        }

        // some assemblies won't let us get their locations, thus -- try/catch
        try {
            var location = a.Location;

            AssemblyMetadata md = AssemblyMetadata.CreateFromFile( location );

            if ( md == null ) {
                Log( $"Can't reference {location}" );
                continue;
            }

            MetadataReference reference = md.GetReference();
            if ( reference == null ) {
                Log( $"Can't reference {a.Location}, ref is null" );
                continue;
            }

            //Log( $"Added reference assembly {location}" );
            _domainReferences.Add( reference );
        } catch ( Exception e ) {
            Log( a.GetName() + ": " + e.Message );
        }
    }

    _initializedCompiler = true;
    Log( "Compiler initialized." );
}

// compile in the thread of the watcher, whatever...
static void OnFileWatcherChange( object sender, FileSystemEventArgs e ) {
    if ( e.ChangeType != WatcherChangeTypes.Changed ) {
        return;
    }

    if ( _roslynAssembly != null ) {
        Log( "Still working..." );
        return;
    }

    {
        string compareA = e.FullPath.Replace( @"\", "|" );
        int i = 0;
        for ( i = 0; i < ScriptFiles.Length; i++ ) {
            string compareB = ScriptFiles[i].Replace( @"\", "|" );
            compareB = compareB.Replace( @"/", "|" );
            if ( compareA.EndsWith( compareB ) ) {
                break;
            }
        }
        if ( i == ScriptFiles.Length ) {
            return;
        }
    }

    Log( $"Script changed: {e.FullPath}, recompiling..." );

    TryInitCompiler();

    var trees = new SyntaxTree[ScriptFiles.Length];

    for ( int i = 0; i < ScriptFiles.Length; i++ ) {
        string path = ScriptsRoot + ScriptFiles[i];
        if ( ! ParseFile( path, true, out trees[i] ) ) {
            return;
        }
        Log( $"Parsed {path}" );
    }

    if ( ! CompileSyntaxTrees( trees, out byte [] image ) ) {
        return;
    }

    Log( "...compile successful." );

    try {

        Log( "Try loading assembly..." );
        var assembly = Assembly.Load( image );
        Log( "...done" );
        _numReloads++;

        lock( _assemblyLock ) {
            _roslynAssembly = assembly;
        }

    } catch ( Exception ex ) {
        Error( ex );
        return;
    }
}

static void OnFileWatcherError( object sender, ErrorEventArgs e ) {
    Error( e.GetException() );
}

static bool ParseFile( string path, bool dll, out SyntaxTree tree ) {
    try {
        tree = Parse( File.ReadAllText( path ), path );
        return true;
    } catch ( Exception ex ) {
        tree = null;
        Error( ex );
        return false;
    }
}

static bool CompileSyntaxTrees( SyntaxTree [] trees, out byte [] image ) {
    try {
        image = null;

        var compilation = CreateCompilation( $"program_{_numReloads}", trees,
             compilerOptions: new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

        using ( var ms = new MemoryStream() ) {
            EmitResult result = compilation.Emit( ms );
            if ( ! result.Success ) {
                Error( "Compilation failed." );
                foreach ( Diagnostic d in result.Diagnostics ) {
                    // ignore Assuming assembly reference'
                    if ( d.Id == "CS1701" )
                        continue;

                    // ignore confilicting assemblies
                    if ( d.Id == "CS0436" )
                        continue;

                    if ( d.ToString().Contains( "warning CS" ) ) {
                        //Log( d );
                    } else {
                        Error( d );
                    }
                }
                return false;
            }
            ms.Seek( 0, SeekOrigin.Begin );
            image = ms.ToArray();
        }

        return true;

    } catch ( Exception ex ) {
        Error( ex );
        image = null;
        return false;
    }
}

static SyntaxTree Parse( string code, string path ) {
    var options = CSharpParseOptions.Default;
    options = options.WithPreprocessorSymbols( "UNITY_STANDALONE" );
    options = options.WithLanguageVersion( LanguageVersion.CSharp9 );
    return SyntaxFactory.ParseSyntaxTree( code, options: options, path: path );
}

static CSharpCompilation CreateCompilation( string assemblyOrModuleName, SyntaxTree [] trees,
                                                CSharpCompilationOptions compilerOptions = null,
                                                IEnumerable<MetadataReference> references = null ) {
    List<MetadataReference> allReferences = _domainReferences;
    if ( references != null ) {
        allReferences = new List<MetadataReference>( _domainReferences );
        allReferences.AddRange( references );
    }
    var compilation = CSharpCompilation.Create( assemblyOrModuleName, trees,
                                            options: compilerOptions, references: allReferences );
    return compilation;
}


}

#endif
