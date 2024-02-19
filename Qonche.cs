using System;
using System.Text;
using System.Collections.Generic;

public static class Qonche {


const int QON_MAX_CMD = 512;
const int QON_MAX_PAGER = 256 * 1024;
const int QON_MAX_PAGER_MASK = QON_MAX_PAGER - 1;
const string QON_PROMPT = "] ";
const string QON_TRAILING_SPACE = " ";

// how many lines to scan up offscreen for line breaks
const int QON_MAX_OFFSCREEN_LOOKUP = 256;

static int qon_prevPage = QON_MAX_PAGER;
static int qon_nextPage = QON_MAX_PAGER;
static int qon_currPage = QON_MAX_PAGER;
static int qon_pagerHead = QON_MAX_PAGER;
static byte [] qon_pager = new byte [QON_MAX_PAGER];
static Action<int,int> [] qon_pagerActions = new Action<int,int>[QON_MAX_PAGER];
static int qon_cursor = QON_PROMPT.Length;
static byte [] qon_cmdBuf = new byte[QON_MAX_CMD];

static Qonche() {
    byte [] ba = Encoding.UTF8.GetBytes( QON_PROMPT + QON_TRAILING_SPACE );
    Array.Copy( ba, qon_cmdBuf, ba.Length );
}

static int QON_Min( int a, int b ) {
    return a < b ? a : b;
}

static int QON_Max( int a, int b ) {
    return a > b ? a : b;
}

static int QON_Len( byte [] p ) {
    int n;
    for ( n = 0; p[n] != 0; n++ ) {}
    return n;
}

static byte [] GetZT( string str ) {
    List<byte> bytes = new List<byte>( Encoding.UTF8.GetBytes( str ) );
    bytes.Add( 0 );
    return bytes.ToArray();
}

static void QON_Dummy( int x, int y ) {}

static int QON_Printn( byte [] str, int n ) {
#if false // no callbacks buffer and stuff
    return QON_PrintClamp( str, n );
#else
    return QON_PrintAndActClamp( str, n, QON_Dummy );
#endif
}

static int QON_PrintClamp( byte [] str, int n ) {
    int i;
    for ( i = 0; i < n && str[i] != 0; i++, qon_pagerHead++ ) {
        qon_pager[qon_pagerHead & QON_MAX_PAGER_MASK] = str[i];
    }
    qon_pager[qon_pagerHead & QON_MAX_PAGER_MASK] = 0;
    qon_currPage = qon_pagerHead;
    return i;
}

static int QON_GetPagerChar( int i ) {
    return qon_pagerHead - i >= QON_MAX_PAGER ? 0 : qon_pager[i & QON_MAX_PAGER_MASK];
}

static bool QON_IsLineInc( int idx, int x, int conWidth, int c ) {
    // handle the case where the last character in the buffer is not a new line
    return ( idx == qon_pagerHead - 1 && c != '\n' ) 
                || x == conWidth - 1 
                || c == 0 
                || c == '\n';
}

static int QON_PrintAndActClamp( byte [] str, int n, Action<int,int> act ) {
    // replace callbacks in the pager string
    qon_pagerActions[qon_pagerHead & QON_MAX_PAGER_MASK] = act;
    // FIXME: no action on empty strings?
    for ( int i = 1; i < n && str[i] != 0; i++ ) {
        int idx = qon_pagerHead + i;
        qon_pagerActions[idx & QON_MAX_PAGER_MASK] = QON_Dummy;
    }
    // actual printing to pager
    return QON_PrintClamp( str, n );
}

static byte [] QON_GetCommandBuf( out int cmdBufLen ) {
    // skip the trailing space
    cmdBufLen = QON_Len( qon_cmdBuf ) - 1;
    int n = cmdBufLen - QON_PROMPT.Length;
    byte [] outBuf = new byte[n];
    for ( int i = 0; i < n; i++ ) {
        outBuf[i] = qon_cmdBuf[QON_PROMPT.Length + i];
    }
    return outBuf;
}

// == Public API ==

public static Action<int,int,int,bool,object> QON_DrawChar = (c, x, y, isCursor, obj) => {};

public static void QON_MoveRight( int numChars ) {
    int max = QON_Len( qon_cmdBuf ) - 1 - qon_cursor;
    numChars = QON_Min( numChars, max );
    qon_cursor += numChars;
}

public static void QON_MoveLeft( int numChars ) {
    int max = qon_cursor - QON_PROMPT.Length;
    numChars = QON_Min( numChars, max );
    qon_cursor -= numChars;
}

public static void QON_Delete( int numChars ) {
    int bufLen = QON_Len( qon_cmdBuf );
    // keep the trailing space
    int max = ( bufLen - 1 ) - qon_cursor;
    int shift = QON_Min( numChars, max );
    if ( shift > 0 ) {
        int n = bufLen - shift;
        for ( int i = qon_cursor; i < n; i++ ) {
            qon_cmdBuf[i] = qon_cmdBuf[i + shift];
        }
        Array.Clear( qon_cmdBuf, n, shift );
    }
}

public static void QON_Backspace( int numChars ) {
    // always keep the prompt
    int max = qon_cursor - QON_PROMPT.Length;
    int shift = QON_Min( numChars, max );
    if ( shift > 0 ) {
        int bufLen = QON_Len( qon_cmdBuf );
        for ( int i = qon_cursor; i < bufLen; i++ ) {
            qon_cmdBuf[i - shift] = qon_cmdBuf[i];
        }
        Array.Clear( qon_cmdBuf, bufLen - shift, shift );
        qon_cursor -= shift;
    }
}

public static void QON_InsertCommand( string str ) {
    if ( string.IsNullOrEmpty( str ) ) {
        return;
    }
    byte [] strBytes = GetZT( str );
    int bufLen = QON_Len( qon_cmdBuf );
    // always leave a trailing space along with the term zero
    int max = ( QON_MAX_CMD - 2 ) - bufLen;
    int shift = QON_Min( QON_Len( strBytes ), max );
    if ( shift > 0 ) {
        for ( int i = bufLen - 1; i >= qon_cursor; i-- ) {
            qon_cmdBuf[i + shift] = qon_cmdBuf[i];
        }
        for ( int i = 0; i < shift; i++ ) {
            qon_cmdBuf[qon_cursor + i] = strBytes[i];
        }
        // deletion always fills zeros, no need to zero terminate here
        qon_cursor += shift;
    }
    // cancel the page-up if started typing
    qon_currPage = qon_pagerHead;
}

static byte [] qon_putcBuf = new byte[2];
public static void QON_Putc( int c ) {
    qon_putcBuf[0] = ( byte )c;
    QON_Printn( qon_putcBuf, 1 );
}

public static int QON_Print( string str ) {
    return QON_Printn( GetZT( str ), QON_MAX_PAGER );
}

public static int QON_PrintAndAct( string str, Action<int,int> act ) {
    return QON_PrintAndActClamp( GetZT( str ), QON_MAX_PAGER, act );
}

public static void QON_EraseCommand() {
    QON_MoveLeft( QON_MAX_CMD );
    QON_Delete( QON_MAX_CMD );
}

public static int QON_SetCommand( string str ) {
    QON_EraseCommand();
    QON_InsertCommand( str );
    int cursor = qon_cursor - QON_PROMPT.Length;
    return cursor;
}

public static string QON_GetCommand() {
    int cmdBufLen;
    byte [] outBuf = QON_GetCommandBuf( out cmdBufLen );
    return System.Text.Encoding.UTF8.GetString( outBuf, 0, outBuf.Length );
}

public static string QON_GetCommand( out int cursor ) {
    cursor = qon_cursor - QON_PROMPT.Length;
    return QON_GetCommand();
}

public static void QON_GetCommandEx( out string cmdClean, out string cmdRaw ) {
    int cmdBufLen;
    byte [] outBuf = QON_GetCommandBuf( out cmdBufLen );
    cmdClean = System.Text.Encoding.UTF8.GetString( outBuf, 0, outBuf.Length );
    cmdRaw = System.Text.Encoding.UTF8.GetString( qon_cmdBuf, 0, cmdBufLen );
}

public static string QON_EmitCommand() {
    string cmdClean, cmdRaw;
    QON_GetCommandEx( out cmdClean, out cmdRaw );
    QON_Print( cmdRaw + "\n" );
    QON_EraseCommand();
    return cmdClean;
}

public static void QON_PageUp() {
    qon_currPage = qon_prevPage;
}

public static void QON_PageDown() {
    qon_currPage = qon_nextPage;
}

public static void QON_DrawEx( int conWidth, int conHeight, bool skipCommandLine, 
                                                        object drawCharParam ) {
    int numCmdLines = 0;

    // == command field ==

    // don't show the prompt if paged up from the head
    if ( qon_currPage == qon_pagerHead && ! skipCommandLine ) {
        // ignore the trailing space when counting lines
        int cmdLen = QON_Len( qon_cmdBuf ) - 1;

        // command may take more than one screen of lines, clamp it to screen
        // always atleast one line
        numCmdLines = QON_Min( conHeight, cmdLen / conWidth + 1 );

        // cursor is always inside the window
        int numLinesToCursor = qon_cursor / conWidth + 1;

        int start = QON_Max( numLinesToCursor - conHeight, 0 ) * conWidth;
        int numChars = QON_Min( conHeight * conWidth, QON_MAX_CMD - start );
        int cmdCaret = ( conHeight - numCmdLines ) * conWidth;
        int caretCursor = cmdCaret + qon_cursor - start;
        for ( int i = 0; i < numChars; i++, cmdCaret++ ) {
            int c = qon_cmdBuf[i + start]; 

            //Debug.Log( c );

            if ( c == 0 ) {
                break;
            }

            int x = cmdCaret % conWidth;
            int y = cmdCaret / conWidth;
            QON_DrawChar( c, x, y, cmdCaret == caretCursor, drawCharParam );
        }
    }

    // out of lines for the pager
    if ( numCmdLines >= conHeight ) {
        return;
    }

    // == pager ==

    {
        int maxY = conHeight - numCmdLines;
        int start = QON_Min( qon_pagerHead, qon_currPage ) - 1;
        int x = 0;
        int y = maxY;

        // go up the pager to the nearest offscreen new line character
        while ( true ) {
            int c = QON_GetPagerChar( start );

            if ( y < -QON_MAX_OFFSCREEN_LOOKUP && ( c == 0 || c == '\n' ) ) {
                x = 0;
                break;
            }
            
            if ( QON_IsLineInc( start, x, conWidth, c ) ) {
                x = 0;
                y--;
            } else {
                x++;
            }

            start--;
        }

        qon_prevPage = qon_currPage;

        int i;
        for ( i = start + 1; y < 2 * maxY - 1; i++ ) {
            int c = QON_GetPagerChar( i );

            if ( c != 0 ) {
                qon_pagerActions[i & QON_MAX_PAGER_MASK]( x, y );
            }

            if ( y >= 0 && y < maxY ) {

                // previous page ends with the first line of the current one
                // don't bother going above any zeros/out of buffer
                if ( y == 1 && c != 0 ) {
                    qon_prevPage = i;
                }

                if ( c == 0 ) {
                    QON_DrawChar( 0, 0, y, false, drawCharParam );
                } else if ( c == '\n' ) {
                    // TODO: debug draw the new lines
                } else {
                    QON_DrawChar( c, x, y, false, drawCharParam );
                }
            }

            if ( QON_IsLineInc( i, x, conWidth, c ) ) {
                x = 0;
                y++;
            } else {
                x++;
            } 
        }

        // will eventually overflow
        qon_nextPage = QON_Min( qon_pagerHead, i );
    }
}

public static void QON_Draw( int conWidth, int conHeight ) {
    QON_DrawEx( conWidth, conHeight, false, 0 );
}


}
