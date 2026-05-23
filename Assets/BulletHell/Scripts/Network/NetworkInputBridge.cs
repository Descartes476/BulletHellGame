using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulletHell.Simulation.Core;

public class NetworkInputBridge : MonoBehaviour
{
    [SerializeField]
    private UdpLockstepClient _client;

    [SerializeField]
    private string _serverIp = "127.0.0.1";

    [SerializeField]
    private int _serverPort = 7777;

    [SerializeField]
    private int[] _localPortArray = { 7778 , 7779, 7780, 7781 }; // 支持多个客户端实例时使用不同的本地端口

    [SerializeField]
    private int _inputLeadTick = 3;

    [SerializeField]
    private int _maxInputPerUpdate = 4; // 每帧最多补发的未来输入数量，用于快速填满输入提前窗口。
    
    [SerializeField]
    private int _redundantInputCount = 3;

    private int _lastReceivedTick = -1;

    private int _lastSentTick = -1;

    private PlayerController _controlledPlayer;

    private Dictionary<int, InputFrame> _sentInputs = new Dictionary<int, InputFrame>();

    private readonly RemoteInputQueueSource _p2ConfirmInputSource = new RemoteInputQueueSource();

    private readonly RemoteInputQueueSource _p1ConfirmInputSource = new RemoteInputQueueSource();


    void Awake()
    {
        if(_client == null)
        {
            _client = GetComponent<UdpLockstepClient>();
        }
    }

    void Start()
    {
        _client.OnConnected += HandleConnected;
        _client.OnFatalError += HandleFatalError;
        _client.OnFrameReceived += HandleFrameReceived;
        SimulationDriver.OnNetworkHashSampled += HandleNetworkHashSampled;
        int localPort = 0;
        foreach (var port in _localPortArray)
        {
            if (CheckPortAvailable(port))
            {
                localPort = port;
                break;
            }
        }
        _client.Connect(_serverIp, _serverPort, localPort);
    }

    void Update()
    {
        _client.Tick();

        if(!_client.IsConnected)
        {
            return;
        }

        SendInputAhead();
    }

    private void OnDestroy()
    {
        if(_client == null)
            return;
        _client.OnConnected -= HandleConnected;
        _client.OnFatalError -= HandleFatalError;
        _client.OnFrameReceived -= HandleFrameReceived;
        SimulationDriver.OnNetworkHashSampled -= HandleNetworkHashSampled;
        _client.Disconnect();
    }

    private void SendInputAhead()
    {
        int simulationTick = SimulationDriver.Instance != null
            ? SimulationDriver.Instance.CurrentTick
            : _client.StartTick;

        int targetSendTick = Mathf.Max(
            _client.StartTick + _inputLeadTick,
            simulationTick + _inputLeadTick
        );

        int sendThisUpdate = 0;

        while (_lastSentTick < targetSendTick && sendThisUpdate < _maxInputPerUpdate)
        {
            int tickToSend = _lastSentTick + 1;
            InputFrame inputFrame = GetInputFrame(tickToSend);
            _sentInputs[tickToSend] = inputFrame;
            _client.SendInput(inputFrame);
            ResendInputs(tickToSend);
            TrimSentInputCache(tickToSend);
            _lastSentTick = tickToSend;
            sendThisUpdate++;
        }
    }

    public InputFrame GetInputFrame(int tick)
    {
        if(_controlledPlayer == null)
        {
            Debug.LogError($"无法获取玩家控制器，玩家ID: {_client.PlayerId}");
            return new InputFrame();
        }
        return _controlledPlayer.SampleCurrentInputFrame(tick);
    }

    private void HandleFrameReceived(int frameTick, InputFrame p1Input, InputFrame p2Input)
    {
        if(frameTick > _lastReceivedTick)
        {
            _lastReceivedTick = frameTick;
        }
        _p1ConfirmInputSource.PushInput(p1Input);
        _p2ConfirmInputSource.PushInput(p2Input);
        SimulationDriver.Instance.SetLastConfirmedNetworkTick(frameTick);
    }

    private void HandleNetworkHashSampled(int tick, ulong hash)
    {
        _client.SendHashReport(tick, hash);
    }

    private void HandleConnected()
    {
        Debug.Log($"已连接服务器，玩家ID: {_client.PlayerId}, 玩家数量: {_client.PlayerCount}, 起始Tick: {_client.StartTick}, 随机种子: {_client.Seed}");
        ViewSyncManager.Instance.SetPlayerView(_client.PlayerId);
        _controlledPlayer = ViewSyncManager.Instance.GetPlayerView(_client.PlayerId);
        if(_controlledPlayer == null)
        {
            Debug.LogError($"无法绑定本地控制玩家，玩家ID: {_client.PlayerId}");
            return;
        }
        _lastReceivedTick = _client.StartTick - 1;
        _lastSentTick = _client.StartTick - 1;
        _sentInputs.Clear();
        _p1ConfirmInputSource.Clear();
        _p2ConfirmInputSource.Clear();
        SimulationDriver.Instance.SetNetworkLiveMode(_client.Seed, _p1ConfirmInputSource, _p2ConfirmInputSource, _client.PlayerId);
    }

    private void HandleFatalError(string error)
    {
        Debug.LogError("Fatal error occurred: " + error);
    }

    private bool CheckPortAvailable(int port)
    {
        try
        {
            using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp))
            {
                socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
                return true;
            }
        }
        catch (System.Net.Sockets.SocketException)
        {
            return false;
        }
    }

    private void ResendInputs(int latestTick)
    {
        int startResendTick = Mathf.Max(_client.StartTick, latestTick - _redundantInputCount);
        for (int tick = startResendTick; tick < latestTick; tick++)
        {
            if (_sentInputs.TryGetValue(tick, out InputFrame inputFrame))
            {
                _client.SendInput(inputFrame);
            }
        }
    }

    private void TrimSentInputCache(int latestTick)
    {
        List<int> ticksToRemove = new List<int>();
        int minTickToKeep = latestTick - _redundantInputCount - 10;
        foreach (var kvp in _sentInputs)
        {
            if (kvp.Key < minTickToKeep)
            {
                ticksToRemove.Add(kvp.Key);
            }
        }
        foreach (int tick in ticksToRemove)
        {
            _sentInputs.Remove(tick);
        }
    }
}
