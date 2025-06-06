#if UNITY_STANDALONE || UNITY_2021_0_OR_NEWER

using System;
using System.Collections.Generic;
using UnityEngine;

public static class Animo {


public class Source {
    public int NumClips => stateName.Count;
    public List<string> stateName = new List<string>{ "None" };
    public List<int> state = new List<int>{ 0 };
    public List<ushort> duration = new List<ushort>{ 1 };
}

public class Crossfade {
    public int chan;
    public float [] weight = new float[2];
    public int [] state = new int[2];
    public int [] time = new int[2];
    public bool overflow;
}

public static Action<string> Log = (s) => {};
public static Action<string> Error = (s) => {};

public static List<Source> sourcesList = new List<Source>(){ new Source() };

public static int RegisterAnimationSource( GameObject go ) {
    Animator anm = go.GetComponent<Animator>();
    if ( ! anm ) {
        Error( $"No animator supplied on '{go.name}'" );
        return 0;
    }
    var rac = anm.runtimeAnimatorController;
    if ( ! rac ) {
        Error( $"No runtime Animator Controller supplied on Animator '{go.name}'" );
        return 0;
    }
    var src = new Source();
    foreach ( var c in rac.animationClips ) {
        // assumes states are named after clips
        int hash = Animator.StringToHash( c.name );
        if ( ! src.state.Contains( hash ) ) {
            src.state.Add( hash );
            src.stateName.Add( c.name );
            src.duration.Add( ( ushort )( c.length * 1000 ) );
        }
    }
    sourcesList.Add( src );
    Log( $"Loaded source from '{go.name}' num states: {src.state.Count}" );
    return sourcesList.Count - 1;
}

public static void PrintStates( int source ) {
    if ( source < 0 || source >= sourcesList.Count ) {
        Error( "No source at " + source );
        return;
    }
    var src = sourcesList[source];
    var str = "";
    for ( int i = 0; i < src.stateName.Count; i++ ) {
        str += i + ": " + src.stateName[i] + " len: " + src.duration[i] + "\n";
    }
    Log( str );
}

static void PrintStates_cmd( string [] argv ) {
    int s = 1;
    if ( argv.Length > 1 ) {
        int.TryParse( argv[1], out s );
    }
    PrintStates( s );
}

public static void ResetToState( Crossfade cf, int state, int offset = 0 ) {
    cf.state[0] = state;
    cf.time[0] = offset;
    cf.weight[0] = 1;

    cf.state[1] = state;
    cf.time[1] = offset;
    cf.weight[1] = 0;

    cf.chan = 0;
}

// can crossfade to same state at time 0
// useful if you need to gracefully force animation
// i.e. single-shot animations forced to repeat
public static void CrossfadeToState( Crossfade cf, int state ) {
    int [] c = {
        ( cf.chan + 0 ) & 1,
        ( cf.chan + 1 ) & 1,
    };

    cf.state[c[0]] = state;
    cf.time[c[0]] = 0;
    cf.weight[c[0]] = 1 - cf.weight[c[1]];

    // the transition is always: 'c[0] crossfades into c[1]'
    // this increment will do the flip

    cf.chan++;

#if false
    Log( "switch to " + state + " chan: " + cf.chan );
#endif
}

// returns true if the entire clip was played (overflown)
public static bool UpdateState( int dt, int source, Crossfade cf, int state, bool clamp = false,
                                                        int transition = 266, float speed = 1 ) {
    Source src = sourcesList[source];

    transition = ( int )( transition / speed );
    dt = ( int )( dt * speed );

    state = Mathf.Clamp( state, 0, src.state.Count - 1 );
    for ( int i = 0; i < 2; i++ ) {
        cf.state[i] = Mathf.Clamp( cf.state[i], 0, src.state.Count - 1 );
    }
            
    int [] c = {
        ( cf.chan + 0 ) & 1,
        ( cf.chan + 1 ) & 1,
    };

    // start crossfading to another state
    if ( cf.state[c[1]] != state ) {
        CrossfadeToState( cf, state );
        c[0] = ( cf.chan + 0 ) & 1;
        c[1] = ( cf.chan + 1 ) & 1;
    }

    // clamp the transition to half the clip duration of the shorter clip
    for ( int i = 0; i < 2; i++ ) {
        transition = Mathf.Min( transition, src.duration[cf.state[i]] / 2 );
    }

    // advance crossfdade weights
    if ( transition == 0 ) {
        cf.weight[c[1]] = 1;
    } else { 
#if false
        if ( cf.weight[c[1]] < 1 ) {
            Log( $"{cf.time[0] + dt} {src.duration[cf.state[0]]} {cf.weight[0]} {cf.state[0]}" );
            Log( $"{cf.time[1] + dt} {src.duration[cf.state[1]]} {cf.weight[1]} {cf.state[1]}" );
            Log( "\n" );
        }
#endif
        // the fade-in clip is ramping UP
        cf.weight[c[1]] = Mathf.Min( 1, cf.weight[c[1]] + dt / ( float )transition );
    }

    // the fade-out clip is ramping DOWN
    cf.weight[c[0]] = 1 - cf.weight[c[1]];

    // advance timers of both clips
    cf.time[0] += dt;
    cf.time[1] += dt;
    
    // c[0] crossfades into c[1] -- it means the c[1] is the potentially looped remaining clip
    // if the target clip is overflowed, we should notify the caller to handle single shots
    cf.overflow = cf.time[c[1]] + transition >= src.duration[cf.state[c[1]]];

    // keep the rollover/clamp AFTER the overflow check
    if ( clamp ) {
        cf.time[c[1]] = Mathf.Min( cf.time[c[1]], src.duration[cf.state[c[1]]] );
    } else {
        cf.time[c[1]] %= src.duration[cf.state[c[1]]];
    }

    // freeze the fade-out clip at the last frame, so we don't get 'pops' 
    // on no-loop animations overflow while transitioning
    cf.time[c[0]] = Mathf.Min( cf.time[c[0]], src.duration[cf.state[c[0]]] );

    return cf.overflow;
}

public static void Begin() {
}

public static void SampleAnimations( int source, Animator ar, Crossfade cf ) {
    Source src = sourcesList[source];
    if ( src == null ) {
        Error( $"SampleAnimations: No animation source." );
        return;
    }
    if ( ! ar ) {
        Error( $"SampleAnimations: No animator." );
        return;
    }
    ar.applyRootMotion = true;
    ar.speed = 0;
    for ( int i = 0; i < 2; i++ ) {
        int s = cf.state[i];
        ar.SetLayerWeight( i, cf.weight[i] );
        if ( cf.weight[i] > 0 ) {
            ar.Play( src.state[s], i, cf.time[i] / ( float )src.duration[s] );
        }
    }
    ar.Update( 0 );
}

public static void End() {
}


}

#endif
