using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BulletHell.Network;
using BulletHell.Simulation.Core;
using UnityEngine;

public enum ClientState
{
    Idle,           // 未连接
    Connecting,     // 已发Helle，等待Welcome，连接中
    Connected,      // 已连接(收到Welcome)
    Disconnected,   // 已断开连接
}


public class UdpLockstepClient : MonoBehaviour
{
    // 收发线程
    private Thread _receiveThread;
    private Thread _sendThread;
    private volatile bool _running;

    // 线程安全队列
    private ConcurrentQueue<byte[]> _incomingPackets; // 收到但未处理的包
    private BlockingCollection<byte[]> _outgoingPackets; // 待发送的包

    // Socket
    private Socket _socket;
    private IPEndPoint _serverEndPoint;

    // 发包序号
    private ushort _sendSequence;

    // 连接状态
    private int _clientId; // 服务器分配
    private byte _playerId; // P1=0, P2=1
    private byte _playerCount;
    private uint _seed;
    private int _startTick;
    private ClientState _state = ClientState.Idle;

    // 消息事件
    public event Action<InputFrame> OnRemoteInputReceived;
    public event Action<int, InputFrame, InputFrame> OnFrameReceived;
    public event Action<string> OnFatalError;
    public event Action OnConnected;

    public ClientState State => _state;
    public byte PlayerId => _playerId;
    public byte PlayerCount => _playerCount;
    public uint Seed => _seed;
    public int StartTick => _startTick;
    public bool IsConnected => _state == ClientState.Connected;

    public void Tick()
    {
        if (_state != ClientState.Connected && _state != ClientState.Connecting)
            return;

        // 消费收包队列（可配置每帧最多处理 N 个，防止一帧耗时过长）
        int maxProcessPerFrame = 50;
        for (int i = 0; i < maxProcessPerFrame; i++)
        {
            if (!_incomingPackets.TryDequeue(out byte[] data))
                break;

            if (data == null)
            {
                _state = ClientState.Disconnected;
                OnFatalError?.Invoke("网络错误，连接已断开");
                return;
            }

            ProcessPacket(data);
        }
    }

    public void Connect(string serverIp, int serverPort, int localPort = 0)
    {
        if(_running)
            Disconnect();
        // 创建UDP Socket并连接服务器
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, localPort));
        _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        _socket.Connect(_serverEndPoint);

        // 初始化队列
        _incomingPackets = new ConcurrentQueue<byte[]>();
        _outgoingPackets = new BlockingCollection<byte[]>();

        // 启动收发线程
        _running = true;
        _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        _sendThread = new Thread(SendLoop) { IsBackground = true };
        _receiveThread.Start();
        _sendThread.Start();
        _state = ClientState.Connecting;

        // 发送Hello包
        var helloPacket = PacketCodec.EncodeHello(0, 0, (uint)new System.Random().Next());
        _outgoingPackets.Add(helloPacket);
        Debug.Log($"正在连接服务器 {serverIp}:{serverPort}，本地端口 {localPort}");
    }

    private void ReceiveLoop()
    {
        var buffer = new byte[1024];
        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (_running)
        {
            try
            {
                int received = _socket.ReceiveFrom(buffer, ref remoteEP);
                if (received > 0)
                {
                    var packet = new byte[received];
                    Array.Copy(buffer, packet, received);
                    _incomingPackets.Enqueue(packet);
                }
            }
            catch (SocketException)
            {
                if(_running)
                    _incomingPackets.Enqueue(null);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private void SendLoop()
    {
        while (_running)
        {
            try
            {
                if(_outgoingPackets.TryTake(out byte[] packet, 500))
                {
                    _socket.SendTo(packet, _serverEndPoint);
                }
            }
            catch (SocketException)
            {
                if(_running)
                    _incomingPackets.Enqueue(null);
            }
            catch (ObjectDisposedException)
            {
                // _socket 被 _running=false 之后关闭，正常退出
                break;
            }
        }
    }

    public void SendInput(InputFrame input)
    {
        if (_state != ClientState.Connected)
            return;
        byte[] data = PacketCodec.EncodeInput(
            _sendSequence++, _clientId, _playerId, input);
        _outgoingPackets.Add(data);
    }
    
    public void Disconnect()
    {
        if (!_running)
            return;

        _running = false;
        _outgoingPackets?.CompleteAdding();

        if (_receiveThread != null && _receiveThread.IsAlive)
            _receiveThread.Join(1000);
        if (_sendThread != null && _sendThread.IsAlive)
            _sendThread.Join(1000);

        try { _socket?.Close(); } catch { }
        _socket = null;

        _outgoingPackets?.Dispose();
        _outgoingPackets = null;
        _incomingPackets = null;

        _state = ClientState.Idle;
    }

    private void ProcessPacket(byte[] data)
    {
        if (!PacketCodec.TryDecodeHeader(data, out PacketHeader header))
            return;
        if (!header.IsValid)
            return;

        switch (header.Type)
        {
            case NetMessageType.Welcome:
                if (PacketCodec.TryDecodeWelcome(data, header,
                        out byte playerId, out byte playerCount,
                        out int startTick, out uint seed))
                {
                    _clientId = header.ClientId;
                    _playerId = playerId;
                    _playerCount = playerCount;
                    _startTick = startTick;
                    _seed = seed;
                    _state = ClientState.Connected;
                    OnConnected?.Invoke();
                }
                break;

            case NetMessageType.Frame:
                if (_state != ClientState.Connected)
                    return;
                if (PacketCodec.TryDecodeFrame(data, header,
                        out int tick, out InputFrame p1, out InputFrame p2))
                {
                    InputFrame localInput = (_playerId == 0) ? p1 : p2;
                    InputFrame remoteInput = (_playerId == 0) ? p2 : p1;
                    OnFrameReceived?.Invoke(tick, localInput, remoteInput);
                    OnRemoteInputReceived?.Invoke(remoteInput);
                }
                break;

            default:
                break;
        }
    }
}
