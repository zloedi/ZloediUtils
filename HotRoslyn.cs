/*
This script is intended for compiling a bunch of .cs files into an assembly, while the C# app is running.
The result assembly is loaded after successful compile, so the user can instantiate/invoke stuff.
I use it to implement hot-reloading in my C#/Unity code.

How to integrate the Roslyn compiler into Unity:
- install the NuGet package: https://github.com/GlitchEnzo/NuGetForUnity.
- install Microsoft.CodeAnalysis and Microsoft.CodeAnalysis.CSharp NuGet packages.
- will have to degrade them to version 2 if don't need CSharp > 7, or ...
- ... disable Project Settings / Player / Other Settings / Assembly Version Validation
    to have LanguageVersion.CSharp9.

How to use this script:
- #define ROSLYN
- setup Log, Error, OnCompile, ScriptsRoot, ScriptFiles, Defines
- invoke Init(), this will start listening for file changes under ScriptsRoot.
- invoke Update() i.e. on mono behaviour Update.
- eventually Update() will invoke OnCompile() passing down the newly compiled assembly.
- invoke Done() i.e. OnApplicationQuit().
*/

#if ROSLYN

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
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
public static string [] Defines = {};
public static string [] ExtraDefines = {};
public static bool Initialized { get; private set; }

static List<MetadataReference> _domainReferences = new List<MetadataReference>();

static int _numReloads;
static bool _initializedCompiler;
static Assembly _roslynAssembly;
static FileSystemWatcher _watcher;

static readonly object _assemblyLock = new();

public static bool TryInit() {
    if ( Initialized ) {
        return true;
    }

    try {
        Log( $"Setting up File Watcher to '{ScriptsRoot}'" );

        _watcher = new FileSystemWatcher( ScriptsRoot );
        _watcher.Filter = "*.cs";
        _watcher.NotifyFilter = NotifyFilters.LastWrite;

        _watcher.Changed += OnFileWatcherChange;
        _watcher.Error += OnFileWatcherError;

        _watcher.IncludeSubdirectories = true;
        _watcher.EnableRaisingEvents = true;

        Initialized = true;
    } catch ( Exception e ) {
        Error( e );
    }

    return Initialized;
}

// makes sure we are detached from the Unity editor on play mode off
// and the referenced assemblies can be reloaded on recompile there
public static void Done() {
    _domainReferences.Clear();
    _watcher?.Dispose();
    _watcher = null;
    _roslynAssembly = null;
    _initializedCompiler = false;
    _numReloads = 0;
    GC.Collect();
    Initialized = false;
    Log( "Compiler Done." );
}

public static void Update() {
    if ( _roslynAssembly != null ) {
        Assembly asm;

        lock( _assemblyLock ) {
            asm = _roslynAssembly;
            _roslynAssembly = null;
        }

        try {
            OnCompile( asm );
        } catch ( Exception e) {
            Error( e );
        }
    }
}

static void TryInitCompiler() {
    if ( ! Initialized ) {
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

    lock( _assemblyLock ) {

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

    if ( _roslynAssembly != null ) {
        Log( "Still working..." );
        return;
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

    if ( ! CompileSyntaxTrees( trees, out byte [] imageAssembly, out byte [] imagePDB ) ) {
        return;
    }

    Log( "...compile successful." );

    try {
        Log( "Try loading assembly..." );
        var assembly = Assembly.Load( imageAssembly, imagePDB );
        Log( "...done" );
        _numReloads++;
        _roslynAssembly = assembly;
    } catch ( Exception ex ) {
        Error( ex );
    }

    } // lock
}

static void OnFileWatcherError( object sender, ErrorEventArgs e ) {
    Error( e.GetException() );
}

static bool ParseFile( string path, bool dll, out SyntaxTree tree ) {
    try {
        string code = null;
        for ( int i = 0; code == null && i < 10; i++) {
            try {
                code = File.ReadAllText( path );
            } catch {
                code = null;
                Thread.Sleep( 33 );
            }
        }

        if ( code != null)
        {
            tree = Parse( code, path );
            return true;
        }

        tree = null;
        Error( $"Failed to read '{path}'" );
        return false;
    } catch ( Exception ex ) {
        tree = null;
        Error( ex );
        return false;
    }
}

static bool CompileSyntaxTrees( SyntaxTree [] trees, out byte [] imageAssembly,
                                                                            out byte [] imagePDB ) {
    imageAssembly = imagePDB = null;

    try {
        var options = new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary )
                                         .WithOptimizationLevel(OptimizationLevel.Debug);

        var compilation = CreateCompilation( $"program_{_numReloads}", trees,
             compilerOptions: options );

        using ( var assemblyStream = new MemoryStream() ) {
           using ( var symbolStream = new MemoryStream() ) {
               var emitOptions = new EmitOptions( false, DebugInformationFormat.PortablePdb );
               EmitResult result = compilation.Emit( assemblyStream, symbolStream,
                                                                           options: emitOptions );
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
               symbolStream.Seek( 0, SeekOrigin.Begin );
               imagePDB = symbolStream.ToArray();
            }
            assemblyStream.Seek( 0, SeekOrigin.Begin );
            imageAssembly = assemblyStream.ToArray();
        }

        compilation.RemoveAllSyntaxTrees();
        compilation.RemoveAllReferences();

        return true;

    } catch ( Exception ex ) {
        Error( ex );
        return false;
    }
}

static SyntaxTree Parse( string code, string path ) {
    var stringText = SourceText.From( code, Encoding.UTF8 );
    var options = CSharpParseOptions.Default;
    var defines = new List<string>( Defines );
    defines.AddRange( ExtraDefines );
    options = options.WithPreprocessorSymbols( defines );
    options = options.WithLanguageVersion( LanguageVersion.CSharp9 );
    return SyntaxFactory.ParseSyntaxTree( stringText, options: options, path: path );
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
