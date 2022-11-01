using System;
using System.Collections.Generic;
using System.Globalization;

public static class Delta {


static char NibbleToChar( int n ) {
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

static int CharToNibble( int ch ) {
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
        case 'A': return 0xA;
        case 'B': return 0xB;
        case 'C': return 0xC;
        case 'D': return 0xD;
        case 'E': return 0xE;
        case 'F': return 0xF;
    }
    return 0;
}

static int HexToInt( string s ) {
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
        return ( CharToNibble( s[0] ) << 16 )
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
        quad[0] = NibbleToChar( ( num & 0xf000 ) >> 16 );
        quad[1] = NibbleToChar( ( num & 0x0f00 ) >> 8 );
        quad[2] = NibbleToChar( ( num & 0x00f0 ) >> 4 );
        quad[3] = NibbleToChar( ( num & 0x000f ) >> 0 );
        return new string( quad );
    }
    for ( int i = 7; i >= 0; i-- ) {
        octet[i] = NibbleToChar( ( num >> i * 4 ) & 0xf );
    }
    return new string( octet );
}

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
    maxInput = maxInput > 0 ? maxInput : input.Length;
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
    maxInput = maxInput > 0 ? maxInput : input.Length;
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

public static bool Undelta( ref int idx, IList<string> argv, List<ushort> changes,
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
    maxInput = maxInput > 0 ? maxInput : input.Length;
    for ( int i = 0; i < maxInput; i++ ) {
        if ( input[i] != shadow[i] ) {
            changes += " " + IntToHex( i );
            values += " " + IntToHex( input[i] );
            shadow[i] = input[i];
            n++;
        }
    }
    changes += " : ";
    values += " : ";
    return n > 0;
}


}
