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
    private float _inputSendInterval = 0.05f; // 输入发送间隔，单位秒 
    private int _sendTick;
    private float _sendAccumulator;
    private readonly RemoteInputQueueSource _remoteConfirmInputSource = new RemoteInputQueueSource();
    private readonly RemoteInputQueueSource _localConfirmInputSource = new RemoteInputQueueSource();
    // Start is called before the first frame update
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

    private void OnDestroy()
    {
        if(_client == null)
            return;
        _client.OnConnected -= HandleConnected;
        _client.OnFatalError -= HandleFatalError;
        _client.OnFrameReceived -= HandleFrameReceived;
        _client.Disconnect();
    }

    // Update is called once per frame
    void Update()
    {
        _client.Tick();

        if(!_client.IsConnected)
        {
            return;
        }

        _sendAccumulator += Time.deltaTime;
        while (_sendAccumulator >= _inputSendInterval)
        {
            _sendAccumulator -= _inputSendInterval;
            InputFrame inputFrame = CreateInputFrame();
            _client.SendInput(inputFrame);
            _sendTick++;
        }
    }

    private void HandleFrameReceived(int frameTick, InputFrame localInput, InputFrame remoteInput)
    {
        Debug.Log($"Frame received tick={frameTick}, localInputTick={localInput.Tick}, remoteInputTick={remoteInput.Tick}");
        _remoteConfirmInputSource.PushInput(remoteInput);
        _localConfirmInputSource.PushInput(localInput);
    }

    public InputFrame CreateInputFrame()
    {
        sbyte moveX = _client.PlayerId == 0 ? (sbyte)1 : (sbyte)-1;
        sbyte moveY = 0;

        bool fire = _sendTick % 10 == 0;

        return new InputFrame(
            _sendTick, // Tick
            moveX,
            moveY,
            aimX: 0,
            aimY: 1,
            fire
        );
    }

    private void HandleConnected()
    {
        Debug.Log($"已连接服务器，玩家ID: {_client.PlayerId}, 玩家数量: {_client.PlayerCount}, 起始Tick: {_client.StartTick}, 随机种子: {_client.Seed}");
        SimulationDriver.Instance.SetNetworkLiveMode(_client.Seed, _localConfirmInputSource, _remoteConfirmInputSource);
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
}
