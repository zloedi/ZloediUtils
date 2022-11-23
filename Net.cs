using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;

public class NetMsg {
    public List<byte> buffer = new List<byte>();
    public int readCount;

    public void BeginRead( IList<byte> buf, int numBytes ) {
        Reset();
        for ( int i = 0; i < numBytes; i++ ) {
            buffer.Add( buf[i] );
        }
    }

    public void BeginWrite() {
        Reset();
    }

    public void Reset() {
        readCount = 0;
        buffer.Clear();
    }

    public void AppendData( IList<byte> outData, int numBytes = -1 ) {
        numBytes = numBytes >= 0 ? numBytes : ( buffer.Count - readCount );
        for ( int i = 0; i < numBytes; i++ ) {
            outData.Add( buffer[readCount] ); 
            readCount++;
        }
    }

    public void ReadData( IList<byte> outData, int numBytes = -1 ) {
        outData.Clear();
        AppendData( outData, numBytes );
    }

    public int ReadShort() {
        int res = buffer[readCount + 0] << 8 | buffer[readCount + 1];
        readCount += 2;
        return res;
    }

    public int ReadInt() {
        int res = buffer[readCount + 0] << 24
            | buffer[readCount + 1] << 16
            | buffer[readCount + 2] << 8 
            | buffer[readCount + 3] << 0;
        readCount += 4;
        return res;
    }

    public void WriteByte( int val ) {
        buffer.Add( ( byte )val );
    }

    public void WriteInt( int val ) {
        buffer.Add( ( byte )( ( val >> 24 ) & 0xff ) );
        buffer.Add( ( byte )( ( val >> 16 ) & 0xff ) );
        buffer.Add( ( byte )( ( val >> 8  ) & 0xff ) );
        buffer.Add( ( byte )( ( val >> 0  ) & 0xff ) );
    }

    public void WriteShort( int val ) {
        buffer.Add( ( byte )( ( val >> 8  ) & 0xff ) );
        buffer.Add( ( byte )( ( val >> 0  ) & 0xff ) );
    }

    public void WriteData( IList<byte> data, int start = 0, int numBytes = -1 ) {
        numBytes = numBytes >= 0 ? numBytes : data.Count;
        for ( int i = 0; i < numBytes; i++ ) {
            buffer.Add( data[start + i] );
        }
    }

    public void OOBPrint( string s ) {
        BeginWrite();
        WriteInt( -1 );
        WriteData( Encoding.ASCII.GetBytes( s ) );
    }
}

public class NetChan {
    public const int FRAG_BIT = 1 << 31;

    public Action<string> Log = s => {};

    static int MaxPacket_cvar = 1400;

    public int zport = GetZPort();

    private int _fragSequence;

    private int _inSequence;
    private List<byte> _inFragBuffer = new List<byte>();

    private int _outSequence;
    private int _unsentStart;
    private List<byte> _outFragBuffer = new List<byte>();

    public NetMsg msg = new NetMsg();

    public NetChan() {
        Reset();
    }

    public NetChan( int zport ) {
        Reset();
        this.zport = zport;
    }

    private static ushort GetZPort() {
        byte [] rngBytes = new byte[2];
        RandomNumberGenerator.Create().GetBytes(rngBytes);
        return BitConverter.ToUInt16( rngBytes, 0 );
    }

    public void Reset() {
        _fragSequence = 0;
        _inSequence = 0;
        _inFragBuffer.Clear();
        _outSequence = 1;
        _unsentStart = 0;
        _outFragBuffer.Clear();
        msg.Reset();
    }

    public static int MaxFragment() {
        return MaxPacket() - 100;
    }

    public static int MaxPacket() {
        return Math.Max( 256, MaxPacket_cvar );
    }

    public bool Receive( IList<byte> data, int numBytes ) {
        msg.BeginRead( data, numBytes );
        int sequence = msg.ReadInt();
        int zport = msg.ReadShort();

        bool fragmented = ( sequence & FRAG_BIT ) != 0;
        sequence = fragmented ? ( sequence & ~FRAG_BIT ) : sequence;

        if ( sequence <= _inSequence ) {
            Log( "Redundant sequence" );
            Log( "  incoming sequence: " + sequence );
            Log( "  expected sequence: " + _inSequence );
            return false;
        }

        int fragStart = fragmented ? msg.ReadShort() : 0;
        int fragLength = fragmented ? msg.ReadShort() : 0;

        if ( fragmented ) {

            // a new fragmented message
            if ( sequence != _fragSequence ) {
                _inFragBuffer.Clear();
                _fragSequence = sequence;
            }

            // some fragments fell off, drop the entire message
            if ( fragStart != _inFragBuffer.Count ) {
                Log( "Dropped frags." );
                return false;
            }

            // invalid frag size
            if ( fragLength < 0 || msg.readCount + fragLength > msg.buffer.Count ) {
                Log( "Invalid frag size / read beyong buffer." );
                Log( " incoming frag len: " + fragLength );
                Log( " space in buffer: " + ( msg.buffer.Count - msg.readCount ) );
                return false;
            }

            msg.AppendData( _inFragBuffer, numBytes: fragLength );

            // not the last fragment of this message
            if ( fragLength == MaxFragment() ) {
                Log( "Waiting more bytes." );
                return false;
            }

            Log( "Received full packet from fragments, total size: " + _inFragBuffer.Count );

            // last fragment, rewrite the message
            msg.BeginWrite();
            msg.WriteInt( sequence );
            msg.WriteShort( zport );
            msg.readCount = msg.buffer.Count;
            msg.WriteData( _inFragBuffer );
        }
        _inSequence = sequence;
        return true;
    }

    public void Transmit( IList<byte> data ) {
        _unsentStart = 0;
        _outFragBuffer.Clear();
        if ( data.Count >= MaxFragment() ) {
            _outFragBuffer.AddRange( data );
            TransmitNextFragment();
        } else {
            msg.BeginWrite();
            msg.WriteInt( _outSequence );
            msg.WriteShort( zport );
            msg.WriteData( data );
            _outSequence++;
        }
    }

    public bool HasPendingFragments() {
        return _outFragBuffer.Count > 0;
    }

    public bool TransmitNextFragment() {
        if ( ! HasPendingFragments() ) {
            return false;
        }
        msg.BeginWrite();
        msg.WriteInt( _outSequence | FRAG_BIT );
        msg.WriteShort( zport );
        int fragLen = MaxFragment();
        if ( _unsentStart + fragLen > _outFragBuffer.Count ) {
            fragLen = _outFragBuffer.Count - _unsentStart;
        }
        msg.WriteShort( _unsentStart );
        msg.WriteShort( fragLen );
        msg.WriteData( _outFragBuffer, _unsentStart, fragLen );
        _unsentStart += fragLen;
        // add a 'terminating' zero fragment if we fill exactly MaxFragment bytes
        if ( _unsentStart == _outFragBuffer.Count && fragLen != MaxFragment() ) {
            _outSequence++;
            _outFragBuffer.Clear();
        }
        return true;
    }
}

// == Common to both ZServer and ZClient ==

public class Net {
    public const int SERVER_PORT = 27960;

    [Description( "Randomly drop packets for tests." )]
    public static int TestDropPackets_cvar = 0;

    public Action<string> Log = s => {};
    public Action<string> Error = s => {};
    public Action<string> TryExecuteOOB = s => {};

    public Socket socket =
                    new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );

    // temporary storages
    public EndPoint remoteEndPoint = new IPEndPoint( IPAddress.Any, 0 );
    public List<byte> packet = new List<byte>();
    public NetMsg message = new NetMsg();
    public byte [] socketBuffer = new byte[NetChan.MaxPacket()];

    int testDropCounter;
    public int Receive() {
        int numBytes = 0;
        try {
            numBytes = socket.ReceiveFrom( socketBuffer, socketBuffer.Length, SocketFlags.None,
                                                                            ref remoteEndPoint );
            if ( TestDropPackets_cvar > 0 ) {
                testDropCounter++;
                if ( ( testDropCounter % TestDropPackets_cvar ) > TestDropPackets_cvar / 2 ) {
                    Log( "===== TEST drop receive packet =====" );
                    return 0;
                }
            }
        } catch ( Exception e ) {
            Log( $"Socket failed to receive from {remoteEndPoint}: {e.Message}"  );
        }
        return numBytes;
    }

    public void Send( NetMsg msg, IPEndPoint endPoint ) {
        try {
            socket.SendTo( msg.buffer.ToArray(), 0, msg.buffer.Count, SocketFlags.None, endPoint );
        } catch ( Exception e ) {
            Log( $"Socket failed to send to {endPoint}: {e.Message}" );
        }
    }

    public bool TryExecuteOOBCommand( byte [] buffer, int numBytes ) {
        message.BeginRead( buffer, numBytes );

        if ( numBytes <= 4 || message.ReadInt() != -1 ) {
            return false;
        }

        message.ReadData( packet );
        // FIXME: just catenate into string as chars?
        string command = Encoding.ASCII.GetString( packet.ToArray(), 0, packet.Count );
        Log( $"Received '{command}' from {remoteEndPoint}" );
        TryExecuteOOB( command );
        return true;
    }

    public bool Init( int basePort ) {
        socket.Blocking = false;
        int port;
        for ( port = 0; port < 10; port++ ) {
            try {
                int realPort = SERVER_PORT + basePort + port;
                Log( "Trying port " + realPort );
                IPEndPoint endpoint = new IPEndPoint( IPAddress.Any, realPort );
                // FIXME: broadcast capable...?
                //socket.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.PacketInformation, true );
                socket.Bind( endpoint );
                Log( $"Net socket bound, endpoint: {endpoint} port: {realPort}" );
                break;
            } catch {}
        }

        if ( port == 10 ) {
            socket.Close();
            Error( "Couldn't bind socket." );
            return false;
        }

        return true;
    }

    public void Done() {
        if ( socket != null ) {
            Log( "Closing the socket." );
            socket.Close();
            socket = null;
        }
    }

    public bool GetLocalIPAddress( out string result ) {
        var host = Dns.GetHostEntry( Dns.GetHostName() );
        foreach ( var ip in host.AddressList ) {
            if ( ip.AddressFamily == AddressFamily.InterNetwork ) {
                result = ip.ToString();
                return true;
            }
        }
        result = "";
        Error( "No network adapters with an IPv4 address in the system!" );
        return false;
    }
}


// == Server ==


// server side client state
public class SvClient {
    const int MAX_DELTA = 32;

    public NetChan netChan;
    public IPEndPoint endPoint;

    // game state snapshots as deltas to previous snapshot
    public List<byte> [] deltas;
    // last acknowledged delta
    public int deltaSequenceACK;
    // deltas counter
    public int deltaSequence;
    // a rolling index in the deltas buffer
    // used to resend any unACK-ed deltas
    public int deltaRoll;
    // disconnect if this go below zero
    public int timeout;

    // client reliable command sequence
    public int reliableSequence;

    public SvClient() {
        deltaSequence = 0;
        deltas = new List<byte>[64];
        for ( int i = 0; i < deltas.Length; i++ ) {
            deltas[i] = new List<byte>();
        }
    }
}


public static class ZServer {


public const int CLIENT_TIMEOUT = 9999;

public static readonly List<byte> emptyPacket = new List<byte>();

public static Net net = new Net();

public static Action<string> LogChan = s => {};

public static Action<string> Log = s => {};
public static Action<string> Error = s => {};

public static List<SvClient> clients = new List<SvClient>();

public static Action<int> onClientDisconnect_f = zport=>{};
public static Action<int> onClientConnect_f = zport=>{};
public static Action<int,string> onClientCommand_f = (zport,str) => {};
public static Func<int,bool,List<byte>> onTick_f = (dt,needPacket)=>{ return emptyPacket; };
public static Action onExit_f = ()=>{};

public static bool Init() {
    if ( ! net.Init( 0 ) ) {
        return false;
    }

    void onProcessExit( object sender, EventArgs e ) {
        onExit_f();
        Done();
    }

    AppDomain.CurrentDomain.ProcessExit += new EventHandler( onProcessExit ); 
    Console.CancelKeyPress += new ConsoleCancelEventHandler( onProcessExit );

    var ep = ( IPEndPoint )net.socket.LocalEndPoint;
    Log( $"Server initialized; Listening on port {ep.Port}" );
    return true;
}

public static void Done() {
    net.remoteEndPoint = new IPEndPoint( IPAddress.Any, 0 );
    // drop all clients
    for ( int i = clients.Count - 1; i >= 0; i-- ) {
        DisconnectClient( i );
    }
    // kill off the socket
    net.Done();
    Log( "Hasta la vista, Baby!" );
}

public static void SendDelta( SvClient c, int sequence ) {
    net.message.BeginWrite();

    // delta sequence (will be ACK-ed)
    net.message.WriteInt( sequence );
    // ACK-ed reliable client message
    net.message.WriteInt( c.reliableSequence );
    // actual delta bytes
    net.message.WriteData( c.deltas[sequence & ( c.deltas.Length - 1 )] );

    // send out
    c.netChan.Transmit( net.message.buffer );
    net.Send( c.netChan.msg, c.endPoint );

    net.Log( $"Sending out delta seq: {sequence}({c.deltaSequenceACK}) dest: {c.endPoint} timeout: {c.timeout}" );
}

static void DisconnectClient( int index ) {
    var c = clients[index];
    Log( "Disconnected client:" );
    Log( "  zport: " + c.netChan.zport );
    Log( "  endPoint: " + c.endPoint );
    c.netChan.msg.OOBPrint( $"cl_disconnect_response {c.netChan.zport}" );
    net.Send( c.netChan.msg, c.endPoint );
    clients.RemoveAt( index );
    onClientDisconnect_f( c.netChan.zport );
}

static void TryDisconnectClient( SvClient c ) {
    TryDisconnectClient( c.netChan.zport, c.endPoint );
}

static void TryDisconnectClient( int zport, IPEndPoint clientEndpoint ) {
    for ( int i = clients.Count - 1; i >= 0; i-- ) {
        SvClient c = clients[i];
        if ( zport == c.netChan.zport && c.endPoint.Address.Equals( clientEndpoint.Address ) ) {
            DisconnectClient( i );
        }
    }
}

static void TryConnectClient( int zport ) {
    void respond( SvClient cl ) {
        cl.netChan.msg.OOBPrint( $"cl_connect_response {cl.netChan.zport}" );
        net.Send( cl.netChan.msg, cl.endPoint );
    }

    // TODO: there are a lot more reasons to ignore clients connects
    foreach ( var c in clients ) {
        var rep = net.remoteEndPoint as IPEndPoint;
        if ( c.endPoint.Address.Equals( rep.Address )
                        && ( c.endPoint.Port.Equals( rep.Port ) || zport == c.netChan.zport ) ) {
            Error( $"client {c.endPoint} already connected" );
            // FIXME: shouldn't accept bursts of OOB connect requests
            respond( c );
            return;
        }
    }

    // add to clients
    var newCl = new SvClient {
        endPoint = net.remoteEndPoint as IPEndPoint,
        netChan = new NetChan( zport ),
        timeout = CLIENT_TIMEOUT,
    };

    newCl.netChan.Log = LogChan;
    clients.Add( newCl );

    onClientConnect_f( zport );

    // send back the response
    respond( newCl );

    Log( "Connected client:" );
    Log( "  zport: " + newCl.netChan.zport );
    Log( "  endPoint: " + newCl.endPoint );
}

// returns true, if read something from the socket
public static bool Poll( out bool hadCommands, int microseconds = 0 ) {
    hadCommands = false;

    void sendPending() {
        foreach ( var c in clients ) {

            // if there are no pending fragments, try to send the next pending delta

            if ( c.netChan.TransmitNextFragment() ) {
                net.Send( c.netChan.msg, c.endPoint );
            } else {
                int start = c.deltaSequenceACK + 1;
                int end = c.deltaSequence;
                int n = end - start;
                if ( n == 0 ) {
                    SendDelta( c, c.deltaSequence );
                } else if ( n > 0 ) {
                    // FIXME: no need to be start +
                    SendDelta( c, start + c.deltaRoll % n );
                    c.deltaRoll++;
                }
            }
        }
    }

    if ( ! net.socket.Poll( microseconds, SelectMode.SelectRead ) ) {
        // nothing to read, send out any pending packets
        sendPending();
        return false;
    } 

    int numBytes = net.Receive();

    if ( net.TryExecuteOOBCommand( net.socketBuffer, numBytes ) ) {
    }

    // sequence == 4, zport == 2, deltaACK == 4 : total 10 bytes
    else if ( numBytes >= 10 ) {
        net.Log( $"Received {numBytes} bytes from {net.remoteEndPoint}" );

        net.message.BeginRead( net.socketBuffer, numBytes );

        // packet sequence
        net.message.ReadInt();

        // port
        int zport = net.message.ReadShort();

        var rep = net.remoteEndPoint as IPEndPoint;

        foreach ( var c in clients ) {
            // not my zport
            if ( c.netChan.zport != zport ) {
                //c.netChan.Log( "Not my zport: " + c.netChan.zport );
                continue;
            }

            // not my address
            if ( ! c.endPoint.Address.Equals( rep.Address ) ) {
                //c.netChan.Log( "Not my address: " + c.endPoint.Address );
                continue;
            }

            // check messed up packet
            if ( ! c.netChan.Receive( net.socketBuffer, numBytes ) ) {
                continue;
            }

            int deltaACK = c.netChan.msg.ReadInt();

            if ( deltaACK < 0 ) {

                // == actually client reliable command, not ACK ==


                int relSeq = -deltaACK;
                // make sure we get every reliable command, not just the last one
                if ( relSeq - c.reliableSequence == 1 ) {
                    c.netChan.msg.ReadData( net.packet );
                    string cmd = Encoding.ASCII.GetString( net.packet.ToArray(), 0,
                                                                                net.packet.Count );
                    hadCommands = true;
                    net.Log( $"Reliable command {cmd}" );
                    c.reliableSequence = relSeq;
                    onClientCommand_f( c.netChan.zport, cmd );
                } else {
                    net.Log( "Dropping old reliable command" );
                    net.Log( " expected " + ( c.reliableSequence + 1 ) );
                    net.Log( "      got " + relSeq );
                }

                continue;
            }
            
            if ( deltaACK <= c.deltaSequenceACK ) {
                net.Log( "Dropped old ACK: " + deltaACK );
                continue;
            }

            if ( deltaACK > c.deltaSequence ) {
                net.Log( "Dropped invalid ACK: " + deltaACK );
                continue;
            }

            // delta acknowledged on the client
            c.deltaSequenceACK = deltaACK;
            net.Log( $"{c.endPoint} acknowledged {deltaACK}" );

            // refill the timer
            c.timeout = CLIENT_TIMEOUT;
        }
    }

    sendPending();
    return true;
}

public static void Tick( int deltaTime, bool forceSendPacket ) {
    if ( clients.Count == 0 ) {
        return;
    }

    for ( int i = clients.Count - 1; i >= 0; i-- ) {
        var c = clients[i];

        if ( c.timeout < 0 ) {
            Error( $"Client {i}({c.netChan.zport}) timedout, disconnecting." );
            DisconnectClient( i );
            continue;
        }

        if ( c.deltaSequence - c.deltaSequenceACK >= c.deltas.Length ) {
            Error( "Out of deltas, abort sending." );
            DisconnectClient( i );
            continue;
        }

        c.timeout -= deltaTime;
    }

    List<byte> delta = onTick_f( deltaTime, forceSendPacket );

    if ( delta.Count == 0 ) {

        if ( ! forceSendPacket ) {
            // nothing to send
            return;
        }

        // send empty packet if forced
        delta = emptyPacket;
    }

    net.Log( $"delta sz: {delta.Count} dt: {deltaTime}" );

    for ( int i = clients.Count - 1; i >= 0; i-- ) {
        var c = clients[i];

        int numUnsent = c.deltaSequence - c.deltaSequenceACK;

        // push this delta in the queue
        c.deltaSequence++;
        int sequence = c.deltaSequence & ( c.deltas.Length - 1 );
        c.deltas[sequence].Clear();
        c.deltas[sequence].AddRange( delta );
        net.Log( $"Pushing delta into queue; client: {c.endPoint} sz: {delta.Count}; unsent deltas: {numUnsent}" );

        // if there are no pending packets, send out immediately
        if ( numUnsent == 0 && ! c.netChan.HasPendingFragments() ) {
            SendDelta( c, c.deltaSequence );
        }
    }
}

// == server commands ==

static void SvConnectClient_kmd( string [] argv ) {
    if ( argv.Length < 2 || ! int.TryParse( argv[1], out int zport ) ) {
        Log( "usage: sv_connect_client <zport>" );
        return;
    }
    TryConnectClient( zport );
}

static void SvDisconnectClient_kmd( string [] argv ) {
    if ( argv.Length < 2 || ! int.TryParse( argv[1], out int zport ) ) {
        Log( "usage: sv_disconnect_client <zport>" );
        return;
    }
    var rep = net.remoteEndPoint as IPEndPoint;
    TryDisconnectClient( zport, rep );
}


}


// == Client ==


public static class ZClient {


public enum State {
    None,
    Disconnected,
    Connected,
}

const int RELIABLE_COMMANDS_QUEUE = 32;
const int LINK_TIMEOUT = 7777;
const int RECONNECT_TIMEOUT = 5555;
const int RESEND_RELIABLE_TIMEOUT = 66;

// try reconnect after this period
static int reconnectTimeout = -1;
// try to resend next reliable command in the queue after this period
static int resendReliableTimeout = RESEND_RELIABLE_TIMEOUT;

// when this goes below zero, drop the connection
static int linkTimeout = LINK_TIMEOUT;

public static Net net = new Net();

public static Action<string> Log = s => {};
public static Action<string> Error = s => {};

public static NetChan netChan = new NetChan();
public static State state;

public static IPAddress serverIP = IPAddress.Parse( "127.0.0.1" );
public static IPEndPoint serverEndpoint = new IPEndPoint( serverIP, Net.SERVER_PORT );

// last received delta sequence number
public static int deltaSequence;

// circular buffer of reliable messages
public static List<byte> [] relMsgs = new List<byte>[0];
// last acknowledged message
public static int relSequenceACK;
// latest message
public static int relSequence;
// the rolling index in the reliable messages buffer from last ACK-ed to latest
// used to resend any unACK-ed reliable messages
public static int relRoll;

public static Action onTickBegin_f = ()=>{};
public static Action<int> onTick_f = dt=>{};
public static Action onTickEnd_f = ()=>{};
public static Action<List<byte>> onServerPacket_f = packet=>{};
public static Action onConnected_f = ()=>{};

public static void Reset( string svIP = null ) {
    if ( svIP != null ) {
        TrySetServerIP( svIP );
    }
    relSequenceACK = 0;
    relSequence = 0;
    relRoll = 0;
    linkTimeout = LINK_TIMEOUT;
    reconnectTimeout = -1;
    state = State.Disconnected;
}

public static bool Init( string svIP = null ) {
    svIP = svIP == null ? $"127.0.0.1:{Net.SERVER_PORT}" : svIP;
    if ( ! net.Init( 1 ) ) {
        return false;
    }
    Reset( svIP );
    Log( $"Client initialized; server IP: {serverIP}, zport: {netChan.zport}" );
    return true;
}

public static void Done() {
    Log( "Hasta la vista, Baby!" );
    SendDisconnectRequest();
    net.Done();
}

static bool TrySetServerIP( string svIP ) {
    if ( IPAddress.TryParse( svIP, out IPAddress addr ) ) {
        serverIP = addr;
        serverEndpoint = new IPEndPoint( serverIP, Net.SERVER_PORT );
        return true;
    }
    Error( $"Failed to set server IP {svIP}" );
    return false;
}

public static void SendDisconnectRequest( int zport = 0 ) {
    zport = zport == 0 ? netChan.zport : zport;
    Log( $"Sending out disconnect request to {serverEndpoint}" );
    net.message.OOBPrint( $"sv_disconnect_client {zport}" );
    net.Send( net.message, serverEndpoint );
}

public static bool TrySendConnectRequest( string ip ) {
    IPAddress addr;
    if ( ! IPAddress.TryParse( ip, out addr ) ) {
        Error( $"Invalid IP address: {ip}" );
        return false;
    }
    serverIP = addr;
    serverEndpoint = new IPEndPoint( serverIP, Net.SERVER_PORT );
    SendConnectRequest();
    return true;
}

public static void SendConnectRequest() {
    Log( $"Sending out connect request to {serverEndpoint}; zport: {netChan.zport}" );
    net.message.OOBPrint( $"sv_connect_client {netChan.zport}" );
    net.Send( net.message, serverEndpoint );
}

// may arrive unrequested
public static void TryDisconnectResponse( int zport ) {
    if ( zport != netChan.zport ) {
        Error( "Received Disconnect Response for another host, zport: " + zport );
        return;
    }
    Log( "Disconnect response from " + net.remoteEndPoint );
    linkTimeout = LINK_TIMEOUT;
    state = State.Disconnected;
    Log( "State: " + state );
}

public static void TryConnectResponse( int zport ) {
    if ( zport != netChan.zport ) {
        Error( "Received Connect Response for another host, zport: " + zport );
        SendDisconnectRequest( zport );
        return;
    }
    // TODO: handle actual client state change
    Log( "Connect response from " + net.remoteEndPoint );
    Log( "Connected to server" );
    Log( "  zport: " + netChan.zport );
    Log( "  endPoint: " + serverEndpoint );
    deltaSequence = 0;
    relSequenceACK = 0;
    relSequence = 0;
    linkTimeout = LINK_TIMEOUT;
    relMsgs = new List<byte>[RELIABLE_COMMANDS_QUEUE];
    for ( int i = 0; i < relMsgs.Length; i++ ) {
        relMsgs[i] = new List<byte>();
    }
    state = State.Connected;
    onConnected_f();
    Log( "State: " + state );
}

static void TrySendNextReliable() {
    int numUnsent = relSequence - relSequenceACK;

    if ( numUnsent == 0 ) {
        // nothing to send
        return;
    }

    if ( resendReliableTimeout > 0 ) {
        // don't go too fast
        return;
    }

    int next = relSequenceACK + 1;

    net.Log( $"Sending out reliable cmd, sequence: {next}" );
    net.message.BeginWrite();
    net.message.WriteInt( -next );
    net.message.WriteData( relMsgs[next & ( relMsgs.Length - 1 )] );
    netChan.Transmit( net.message.buffer );
    net.Send( netChan.msg, serverEndpoint );
    resendReliableTimeout = RESEND_RELIABLE_TIMEOUT;
}

public static bool HasUnsentReliableCommands() {
    return relSequence - relSequenceACK > 0;
}

public static void RegisterReliableCmd( string cmd ) {
    if ( state != State.Connected ) {
        return;
    }

    int numUnsent = relSequence - relSequenceACK;

    if ( numUnsent < 0 ) {
        Error( "Broken reliable command sequence." );
        return;
    }

    if ( numUnsent >= relMsgs.Length ) {
        Error( "Out of reliable commands in the buffer" );
        Error( $"  reliable sequence: {relSequence}" );
        Error( $"  acked reliable sequence: {relSequenceACK}" );
        Error( $"  num unsent commands: {numUnsent}" );
        return;
    }

    byte [] bytes = Encoding.ASCII.GetBytes( cmd );

    relSequence++;
    int sequence = relSequence & ( relMsgs.Length - 1 );
    relMsgs[sequence].Clear();
    relMsgs[sequence].AddRange( bytes );
    net.Log( $"Pushing reliable cmd into queue; sz: {bytes.Length}; unsent cmds: {numUnsent} {cmd}" );
    net.Log( $"   acked: {relSequenceACK}" );
    net.Log( $" current: {relSequence}" );

    // if no command is pending, send immediately
    if ( numUnsent == 0 ) {
        TrySendNextReliable();
    }
}

public static void Tick( int clientDeltaTime, bool sleep = false ) {
    try { 

    onTickBegin_f();

    if ( state == State.Disconnected ) {
        linkTimeout = LINK_TIMEOUT;
        deltaSequence = 0;
        relSequenceACK = 0;
        relSequence = 0;
        netChan.Reset();
        if ( reconnectTimeout < 0 ) {
            SendConnectRequest();
            reconnectTimeout = RECONNECT_TIMEOUT;
        }
        reconnectTimeout -= clientDeltaTime;
        int microseconds = sleep ? 3000 * 1000 : 0;
        if ( net.socket.Poll( microseconds, SelectMode.SelectRead ) ) {
            int numBytes = net.Receive();
            net.TryExecuteOOBCommand( net.socketBuffer, numBytes );
        }
    } else if ( state == State.Connected ) {
        reconnectTimeout = RECONNECT_TIMEOUT;

        TrySendNextReliable();

        // infinite sleep if standalone 'fake' client
        int microseconds = sleep ? -1 : 0;
        while ( net.socket.Poll( microseconds, SelectMode.SelectRead ) ) {
            int numBytes = net.Receive();

            // out of band command
            if ( net.TryExecuteOOBCommand( net.socketBuffer, numBytes ) ) {
            }
            
            // sequence == 4, zport == 2, deltaACK == 4 : total 10 bytes
            else if ( numBytes >= 10 ) {
                net.Log( $"Received {numBytes} bytes." );
                if ( netChan.Receive( net.socketBuffer, numBytes ) ) {

                    // read the header first

                    // delta sequence
                    int seq = netChan.msg.ReadInt();

                    // reliable cmd ack
                    int reliable = netChan.msg.ReadInt();

                    if ( seq - deltaSequence == 1 ) {
                        // received nice sequential delta packet, read it
                        netChan.msg.ReadData( net.packet );
                        net.Log( $"received delta seq: {seq} delta sz: {net.packet.Count}" );
                        deltaSequence = seq;
                        linkTimeout = LINK_TIMEOUT;
                        onServerPacket_f( net.packet );
                    } else {
                        // don't ACK invalid deltas
                        net.Log( "Dropping invalid delta:" );
                        net.Log( " expected " + ( deltaSequence + 1 ) );
                        net.Log( "      got " + seq );
                    }

                    // ACK last received delta immediately

                    net.Log( $"Sending out ACK {deltaSequence}" );
                    net.message.BeginWrite();
                    net.message.WriteInt( deltaSequence );
                    netChan.Transmit( net.message.buffer );
                    net.Send( netChan.msg, serverEndpoint );

                    if ( reliable > relSequenceACK ) {
                        net.Log( "Received reliable command ACK: " + reliable );
                        relSequenceACK = reliable;
                        resendReliableTimeout = 0;
                    }
                }
            }
        }

        linkTimeout -= clientDeltaTime;
        if ( linkTimeout < 0 ) {
            Error( "Inactive for too long, dropping connection..." );
            SendDisconnectRequest();
            linkTimeout = LINK_TIMEOUT;
            state = State.Disconnected;
            Log( "State: " + state );
        }
        resendReliableTimeout -= clientDeltaTime;
    }

    onTick_f( clientDeltaTime );
    onTickEnd_f();

    } catch ( Exception e ) {
        Error( e.ToString() );
    }
}

// == client commands ==

static void ClConnectResponse_kmd( string [] argv ) {
    if ( argv.Length < 2 || ! int.TryParse( argv[1], out int zport ) ) {
        Log( "usage: cl_connect_response <zport>" );
        return;
    }
    TryConnectResponse( zport );
}

static void ClDisconnectResponse_kmd( string [] argv ) {
    if ( argv.Length < 2 || ! int.TryParse( argv[1], out int zport ) ) {
        Qonsole.Log( "usage: cl_disconnect_response <zport>" );
        return;
    }
    TryDisconnectResponse( zport );
}


}
