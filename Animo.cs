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
    public float weight;
    public int [] state = new int[2];
    public int [] time = new int[2];
}

public static Action<string> Log = (s) => {};
public static Action<string> Error = (s) => {};

public static List<Source> sourcesList = new List<Source>(){ new Source() };

public static int prevTimeMs, currentTimeMs;

public static int RegisterAnimationSource( GameObject go ) {
    Animator anm = go.GetComponent<Animator>();
    if ( ! anm ) {
        Error( $"No animator supplied on '{go.name}'" );
        return 0;
    }
    var rac = anm.runtimeAnimatorController;
    var src = new Source();
    foreach ( var c in rac.animationClips ) {
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
    var src = sourcesList[source];
    var str = "";
    for ( int i = 0; i < src.stateName.Count; i++ ) {
        str += i + ": " + src.stateName[i] + " len: " + src.duration[i] + "\n";
    }
    Log( str );
}

public static void ResetToState( Crossfade cf, int state ) {
    cf.state[0] = state;
    cf.time[0] = 0;
    cf.weight = 1;

    cf.state[1] = state;
    cf.time[1] = 0;
    cf.weight = 0;

    cf.chan = 0;
}

public static bool UpdateState( int source, Crossfade cf, int state, bool clamp = false,
                                                        int transition = 150, float speed = 1 ) {
    int [] c = {
        cf.chan & 1,
        ( cf.chan + 1 ) & 1,
    };

    // start crossfading to another state
    if ( cf.state[c[1]] != state ) {
        cf.state[c[0]] = state;
        cf.time[c[0]] = 0;
        cf.weight = 0;
        cf.chan++;
    }

    int dt = ( int )( ( currentTimeMs - prevTimeMs ) * speed );

    // update crossfdade weights
    cf.weight = Mathf.Min( 1, cf.weight + dt / ( float )transition );

    // advance timers
    bool result = false;
    Source src = sourcesList[source];
    for ( int i = 0; i < 2; i++ ) {
        int chan = c[i];
        int s = cf.state[chan];
        cf.time[chan] += dt;
        result = s == state && cf.time[chan] + transition >= src.duration[s];
        cf.time[chan] %= src.duration[s];
    }

    return result;
}

public static void Begin( int nowMs ) {
    prevTimeMs = currentTimeMs;
    currentTimeMs = nowMs;
}

public static void SampleAnimations( int source, Animator ar, Crossfade cf ) {
    int [] c = {
        cf.chan & 1,
        ( cf.chan + 1 ) & 1,
    };
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
        float weight = i == 1 ? cf.weight : 1 - cf.weight;
        int s = cf.state[c[i]];
        ar.SetLayerWeight( c[i], weight );
        ar.Play( src.state[s], c[i], cf.time[c[i]] / ( float )src.duration[s] );
    }
    ar.Update( 0 );
}

public static void End() {
}


}
