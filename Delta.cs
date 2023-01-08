using System;
using System.Collections.Generic;
using System.Globalization;

public static class Delta {


public static Action<string> Log = s => {};
public static Action<string> Error = s => {};

static bool ParseInts( IList<string> argv, ref int i, List<int> list ) {
    for (; i < argv.Count ; i++)
    {
        if (argv[i] == ":")
        {
            i++;
            break;
        }
        list.Add( HexToInt( argv[i] ) );
    }
    return i < argv.Count;
}

static bool ParseEnum( Type elemType, IList<string> argv, ref int i, List<int> list ) {
    for ( ; i < argv.Count; i++ ) {
        if ( argv[i] == ":" ) {
            i++;
            break;
        }
        Array vals = Enum.GetValues( elemType );
        string [] input = argv[i].Split( '|' );
        int output = 0;
        foreach ( string s in input ) {
            foreach ( object e in vals ) {
                if ( e.ToString() == s ) {
                    output |= ( int )e;
                    break;
                }
            }
        }
        list.Add( output );
    }
    return i < argv.Count;
}

static bool ParseShorts( IList<string> argv, ref int i, List<ushort> list ) {
    for ( ; i < argv.Count; i++ ) {
        if ( argv[i] == ":" ) {
            i++;
            break;
        }
        list.Add( ( ushort )HexToInt( argv[i] ) );
    }
    return i < argv.Count;
}

static bool ParseBytes( IList<string> argv, ref int i, List<byte> list ) {
    for ( ; i < argv.Count; i++ ) {
        if ( argv[i] == ":" ) {
            i++;
            break;
        }
        list.Add( ( byte )HexToInt( argv[i] ) );
    }
    return i < argv.Count;
}

// == public API ==

public static char NibbleToChar( int n ) {
    switch( n & 15 ) {
        case 0x0 : return '0';
        case 0x1 : return '1';
        case 0x2 : return '2';
        case 0x3 : return '3';
        case 0x4 : return '4';
        case 0x5 : return '5';
        case 0x6 : return '6';
        case 0x7 : return '7';
        case 0x8 : return '8';
        case 0x9 : return '9';
        case 0xA : return 'A';
        case 0xB : return 'B';
        case 0xC : return 'C';
        case 0xD : return 'D';
        case 0xE : return 'E';
        case 0xF : return 'F';
    }
    return '0';
}

public static int CharToNibble( int ch ) {
    switch( ch ) {
        case '0': return 0x0;
        case '1': return 0x1;
        case '2': return 0x2;
        case '3': return 0x3;
        case '4': return 0x4;
        case '5': return 0x5;
        case '6': return 0x6;
        case '7': return 0x7;
        case '8': return 0x8;
        case '9': return 0x9;
        case 'a': return 0xA;
        case 'b': return 0xB;
        case 'c': return 0xC;
        case 'd': return 0xD;
        case 'e': return 0xE;
        case 'f': return 0xF;
        case 'A': return 0xA;
        case 'B': return 0xB;
        case 'C': return 0xC;
        case 'D': return 0xD;
        case 'E': return 0xE;
        case 'F': return 0xF;
    }
    return 0;
}

public static int HexToInt( string s ) {
    int n = s.Length;
    if ( n == 1 ) {
        return CharToNibble( s[0] );
    }
    if ( n == 2 ) {
        return ( CharToNibble( s[0] ) << 4 ) | ( CharToNibble( s[1] ) << 0 );
    }
    if ( n == 3 ) {
        return ( CharToNibble( s[0] ) << 8 )
                | ( CharToNibble( s[1] ) << 4 )
                | ( CharToNibble( s[2] ) << 0 );
    }
    if ( n == 4 ) {
        return ( CharToNibble( s[0] ) << 12 )
                | ( CharToNibble( s[1] ) << 8 )
                | ( CharToNibble( s[2] ) << 4 )
                | ( CharToNibble( s[3] ) << 0 );
    }
    int num = 0;
    for ( int i = 0; i < n; i++ ) {
        int shift = ( ( n - i ) - 1 ) * 4;
        num |= CharToNibble( s[i] ) << shift;
    }
    return num;
}

static char [] single = new char[1];
static char [] couple = new char[2];
static char [] triple = new char[3];
static char [] quad = new char[4];
static char [] octet = new char[8];
public static string IntToHex( int num ) {
    if ( ( num & ~0xf ) == 0 ) {
        single[0] = NibbleToChar( num & 0x0f );
        return new string( single );
    } 
    if ( ( num & ~0xff ) == 0 ) {
        couple[0] = NibbleToChar( ( num & 0xf0 ) >> 4 );
        couple[1] = NibbleToChar( ( num & 0x0f ) >> 0 );
        return new string( couple );
    } 
    if ( ( num & ~0xfff ) == 0 ) {
        triple[0] = NibbleToChar( ( num & 0xf00 ) >> 8 );
        triple[1] = NibbleToChar( ( num & 0x0f0 ) >> 4 );
        triple[2] = NibbleToChar( ( num & 0x00f ) >> 0 );
        return new string( triple );
    } 
    if ( ( num & ~0xffff ) == 0 ) {
        quad[0] = NibbleToChar( ( num & 0xf000 ) >> 12 );
        quad[1] = NibbleToChar( ( num & 0x0f00 ) >> 8 );
        quad[2] = NibbleToChar( ( num & 0x00f0 ) >> 4 );
        quad[3] = NibbleToChar( ( num & 0x000f ) >> 0 );
        return new string( quad );
    }
    for ( int i = 7; i >= 0; i-- ) {
        octet[7 - i] = NibbleToChar( ( num >> i * 4 ) & 0xf );
    }
    return new string( octet );
}

public static bool UndeltaBytes( ref int idx, IList<string> argv, List<ushort> changes,
                                                        List<byte> values, out bool keepGoing ) {
    changes.Clear(); 
    values.Clear();
    keepGoing = ParseShorts( argv, ref idx, changes );
    keepGoing = ParseBytes( argv, ref idx, values );
    return changes.Count > 0 && changes.Count == values.Count;
}

public static bool DeltaBytes( byte [] input, byte [] shadow,
                                        out string changes, out string values, int maxInput = 0 ) {
    changes = "";
    values = "";
    int n = 0;
    maxInput = maxInput > 0 ? Math.Min( input.Length, maxInput ) : input.Length;
    for ( int i = 0; i < maxInput; i++ ) {
        if ( input[i] != shadow[i] ) {
            changes += " " + IntToHex( i );
            values += " " + IntToHex( input[i] );
            shadow[i] = input[i];
            n++;
        }
    }
    return n > 0;
}

public static bool UndeltaShorts( ref int idx, IList<string> argv, List<ushort> changes,
                                                        List<ushort> values, out bool keepGoing ) {
    changes.Clear(); 
    values.Clear();
    keepGoing = ParseShorts( argv, ref idx, changes );
    keepGoing = ParseShorts( argv, ref idx, values );
    return changes.Count > 0 && changes.Count == values.Count;
}

public static bool DeltaShorts( ushort [] input, ushort [] shadow,
                                        out string changes, out string values, int maxInput = 0 ) {
    changes = "";
    values = "";
    int n = 0;
    maxInput = maxInput > 0 ? Math.Min( input.Length, maxInput ) : input.Length;
    for ( int i = 0; i < maxInput; i++ ) {
        if ( input[i] != shadow[i] ) {
            changes += " " + IntToHex( i );
            values += " " + IntToHex( input[i] );
            shadow[i] = input[i];
            n++;
        }
    }
    return n > 0;
}

public static bool UndeltaEnum( Type elemType, ref int idx,
                                                        IList<string> argv, List<ushort> changes,
                                                        List<int> values, out bool keepGoing ) {
    changes.Clear(); 
    values.Clear();
    keepGoing = ParseShorts( argv, ref idx, changes );
    keepGoing = ParseEnum( elemType, argv, ref idx, values );
    return changes.Count > 0 && changes.Count == values.Count;
}

public static bool DeltaEnum( Array input, Array shadow,
                                        out string changes, out string values, int maxInput = 0 ) {
    changes = "";
    values = "";
    int n = 0;
    maxInput = maxInput > 0 ? Math.Min( input.Length, maxInput ) : input.Length;
    for ( int i = 0; i < maxInput; i++ ) {
        object inVal = input.GetValue( i );
        object shVal = shadow.GetValue( i );
        if ( inVal != null
                && ( int )inVal != 0
                && ( shVal == null || ( int )inVal != ( int )shVal ) ) {
            changes += $" {IntToHex( i )}";
            values += $" {inVal}".Replace( ", ", "|" );
            shadow.SetValue( inVal, i );
            n++;
        }
    }
    return n > 0;
}

public static bool UndeltaNum( ref int idx, IList<string> argv, List<ushort> changes,
                                                        List<int> values, out bool keepGoing ) {
    changes.Clear();
    values.Clear();
    keepGoing = ParseShorts( argv, ref idx, changes );
    keepGoing = ParseInts( argv, ref idx, values );
    return changes.Count > 0 && changes.Count == values.Count;
}

public static bool DeltaInts( int[] input, int[] shadow, out string changes, out string values,
                                                                                int maxInput = 0 ) {
    changes = "";
    values = "";
    int n = 0;
    maxInput = maxInput > 0 ? Math.Min( input.Length, maxInput ) : input.Length;
    for ( int i = 0; i < maxInput; i++ ) {
        if ( input[i] != shadow[i] ) {
            changes += " " + IntToHex( i );
            values += " " + IntToHex( input[i] );
            shadow[i] = input[i];
            n++;
        }
    }
    return n > 0;
}

public static List<ushort> changes = new List<ushort>();
public static List<int> values = new List<int>();

public static bool Parse( int argvIdx, string [] argv, Array array ) {
    bool keepGoing;
    Type t = array.GetType();
    Type elemType = t.GetElementType();
    if ( elemType.IsEnum ) {
        if ( ! UndeltaEnum( elemType, ref argvIdx, argv, changes, values, out keepGoing ) ) {
            Error( "Failed to parse array delta." );
            return false;
        }
        return true;
    }

    if ( ! UndeltaNum( ref argvIdx, argv, changes, values, out keepGoing ) ) {
        Error( "Failed to parse array delta." );
        return false;
    }

    return true;
}

public static bool Apply( Array array ) {
    Type t = array.GetType();
    Type elemType = t.GetElementType();

    if ( elemType.IsEnum ) {
        for ( int i = 0; i < values.Count; i++ ) {
            array.SetValue( values[i], changes[i] );
        }
    } else if ( t == typeof( byte[] ) ) {
        for ( int i = 0; i < values.Count; i++ ) {
            ( ( byte[] )array )[changes[i]] = ( byte )values[i];
        }
    } else if ( t == typeof( ushort[] ) ) {
        for ( int i = 0; i < values.Count; i++ ) {
            ( ( ushort[] )array )[changes[i]] = ( ushort )values[i];
        }
    } else if ( t == typeof( int[] ) ) {
        for ( int i = 0; i < values.Count; i++ ) {
            ( ( int[] )array )[changes[i]] = values[i];
        }
    } else {
        Error( $"Unsupported array type {t}" );
        return false;
    }

    return true;
}

public static bool UndeltaArray( int argvIdx, string [] argv, Array array ) {
    if ( ! Parse( argvIdx, argv, array ) ) {
        return false;
    }
    return Apply( array );
}

// will copy the changed values into 'shadow'
public static bool DeltaArray( Array array, out string changes, out string values,
                                                        Array shadow = null, int maxInput = 0 ) {
    Type t = array.GetType();

    if ( t == typeof( byte[] ) ) {
        return DeltaBytes( ( byte[] )array,
                                        shadow != null ? ( byte[] )shadow : new byte[array.Length],
                                                    out changes, out values, maxInput: maxInput );
    }

    if ( t == typeof( ushort[] ) ) {
        return DeltaShorts( ( ushort[] )array,
                                    shadow != null ? ( ushort [] )shadow : new ushort[array.Length],
                                                    out changes, out values, maxInput: maxInput );
    }

    if ( t == typeof( int[] ) ) {
        return DeltaInts( ( int[] )array,
                                        shadow != null ? ( int [] )shadow : new int[array.Length],
                                                    out changes, out values, maxInput: maxInput );
    }

    if ( t.GetElementType().IsEnum ) {
        return DeltaEnum( array, shadow != null ? shadow : new Enum[array.Length],
                                                    out changes, out values, maxInput: maxInput );
    }

    Error( $"Unsupported array type {t}" );
    changes = "";
    values = "";
    return false;
}


}
