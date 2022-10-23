using System;
using System.Collections.Generic;

public class Timeline {
    public struct Key {
        public int milliseconds;
        public Action action;
    }

    public struct Sample {
        public int start, end;
        public Func<int,bool> scrub;
    }

    public List<Key> keys = new List<Key>();
    public List<Sample> samples = new List<Sample>();
    public int prevTick;

    public void Clear() {
        keys.Clear();
        samples.Clear();
    }

    public void InsertSampleSec( float start, float end, Action<float> scrub ) {
        InsertSampleMs( ( int )( start * 1000f ), ( int )( end * 1000f ),
                                                        dt => { scrub( dt / 1000f ); return true; } );
    }

    public void InsertSampleMs( int start, int end, Action<int> scrub ) {
        InsertSampleMs( start, end, dt => { scrub( dt ); return true; } );
    }

    public void InsertSampleMs( int start, int end, Func<int,bool> scrub ) {
        int i;
        for ( i = 0; i < samples.Count; i++ ) {
            if ( samples[i].start >= start ) {
                break;
            }
        }
        samples.Insert( i, new Sample {
            start = start,
            end = end,
            scrub = scrub,
        } );
    }

    public void InsertKeyMs( int milliseconds, Action action ) {
        int i;
        for ( i = 0; i < keys.Count; i++ ) {
            if ( keys[i].milliseconds >= milliseconds ) {
                break;
            }
        }
        keys.Insert( i, new Key {
            milliseconds = milliseconds,
            action = action,
        } );
    }

    // will remove any keys beyond 'time' in the timeline
    public void AddKeyClampMs( int milliseconds, Action action ) {
        for ( int i = keys.Count - 1; i >= 0; i-- ) {
            // keep keys at the same time position so we can stack them up
            if ( keys[i].milliseconds > milliseconds ) {
                keys.RemoveAt( i );
            }
        }
        keys.Add( new Key {
            milliseconds = milliseconds,
            action = action,
        } );
    }

    public void TickMs( int milliseconds ) {
        int n = 0;
        for ( int i = 0; i < keys.Count; i++ ) {
            // assumes keys are sorted by time
            if ( keys[i].milliseconds > milliseconds ) {
                break;
            }
            n = i + 1;
        }
        for ( int i = 0; i < n; i++ ) {
            keys[i].action();
        }
        for ( int i = n - 1; i >= 0; i-- ) {
            keys.RemoveAt( i );
        }

        if ( prevTick == 0 ) {
            prevTick = milliseconds;
            return;
        }

        if ( prevTick == milliseconds ) {
            Qonsole.Error( "Trying to sample timeline in the same ms." );
            return;
        }

        n = 0;
        for ( int i = 0; i < samples.Count; i++ ) {
            // assumes sample starts are sorted by time
            if ( samples[i].start > milliseconds ) {
                break;
            }
            n = i + 1;
        }
        int dt = milliseconds - prevTick;
        for ( int i = n - 1; i >= 0; i-- ) {
            if ( samples[i].end <= milliseconds ) {
                samples[i].scrub( samples[i].end - prevTick );
                samples.RemoveAt( i );
            } else if ( ! samples[i].scrub( dt ) ) {
                samples.RemoveAt( i );
            } else {
            }
        }
        prevTick = milliseconds;
    }

    public void InsertKeySec( float seconds, Action action ) {
        InsertKeyMs( ( int )( seconds * 1000f ), action );
    }

    public void AddKeyClamp( float seconds, Action action ) {
        AddKeyClampMs( ( int )( seconds * 1000f ), action );
    }

    public void TickSeconds( float nowSeconds ) {
        TickMs( ( int )( nowSeconds * 1000f ) );
    }
}


