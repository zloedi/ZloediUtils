using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class SingleShot {

public const float PostponeFrameSec = 0.001f;

class ActionItem {
    public Func<int,bool> tick;
    public Action done;
    public int duration;
    public int postpone;
}

private static List<ActionItem> _list = new List<ActionItem>();
private static HashSet<int> _uniques = new HashSet<int>();

public static void Clear() {
    _list.Clear();
}

public static void Add( Action<float> tick = null, 
                         // duration in seconds
                         float duration = 5,
                         // optional on callback on finish
                         Action done = null, 
                         // optional postpone in seconds
                         float postpone = 0 ) {
    tick = tick == null ? dummy => {} : tick;
    AddMs( dt => tick( dt / 1000f ), ( int )( duration * 1000f ), done,
                                                        ( int )( postpone * 1000f ) );
}

public static void AddMs( Action<int> tick = null, 
                         // duration in milliseconds
                         int duration = 5000,
                         // optional on callback on finish
                         Action done = null, 
                         // optional postpone in milliseconds
                         int postpone = 0 ) {
    tick = tick == null ? dummy => {} : tick;
    AddConditionalMs( dt => { tick( dt ); return true; }, duration, done, postpone );
}

public static void AddConditional( Func<float,bool> tick = null, 
                         // duration in seconds
                         float duration = 5,
                         // optional on callback on finish
                         Action done = null, 
                         // optional postpone in milliseconds
                         float postpone = 0 ) {
    tick = tick == null ? dummy => true : tick;
    AddConditionalMs( dt => tick( dt / 1000f ), ( int )( duration * 1000f ), done,
                                                                    ( int )( postpone * 1000f ) );
}

public static void AddConditionalMs( Func<int,bool> tick = null, 
                         // duration in milliseconds
                         int duration = 5000,
                         // optional on callback on finish
                         Action done = null, 
                         // optional postpone in milliseconds
                         int postpone = 0 ) {
    _list.Add( new ActionItem { 
        tick = tick != null ? tick : f => true,
        done = done != null ? done : () => {},
        duration = duration,
        postpone = postpone,
    } );
}

public static void AddUnique( Action<float> tick = null, 
                                int handle = 0,
                                // duration in seconds
                                float duration = 0.00001f,
                                // optional on callback on finish
                                Action done = null, 
                                // optional postpone in seconds
                                float postpone = 0,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerMemberName] string caller = null ) {
    const int sh = 20;
    const int mask0 = ( 1 << ( 32 - sh ) ) - 1;
    const int mask1 = ( 1 << sh ) - 1;
    int h0 = ( lineNumber & mask0 ) << sh;
    int h1 = caller.GetHashCode() & mask1;
    int key = ( h0 | h1 ) ^ ( handle * 689287499 );

    if ( _uniques.Contains( key ) )
        return;

    _uniques.Add( key );

    Add( tick, duration, done, postpone );
}

public static void TickSeconds( float deltaTime ) {
    TickMs( ( int )( deltaTime * 1000f ) );
}

public static void TickMs( int deltaTime ) {
    void Done( int i ) {
        _list[i].done();
        _list.RemoveAt( i );
    }

    for ( int i = _list.Count - 1; i >= 0; i-- ) {
        ActionItem item = _list[i];

        if ( item.postpone > 0 ) {
            item.postpone -= deltaTime;
            continue;
        }

        if ( ! item.tick( deltaTime ) ) {
            Done( i );
            continue;
        }

        // tick beyond duration so any lerps can go beyond 0/1
        item.duration -= deltaTime;
        if ( item.duration < 0 ) {
            Done( i );
        }
    }

    _uniques.Clear();
}

}
