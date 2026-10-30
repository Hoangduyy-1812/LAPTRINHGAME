using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Trình quản lý mạng LAN / Online P2P (Host / Client) cho 2 người chơi 2 máy riêng biệt:
/// - Máy 1 (Host): Đóng vai MÈO (Camera bám Mèo, khóa bàn phím Chuột).
/// - Máy 2 (Client): Đóng vai CHUỘT (Camera bám Chuột, khóa bàn phím Mèo).
/// - Hai màn hình hiển thị hoàn toàn độc lập, đảm bảo 100% tính bí mật ngụy trang của Prop Hunt.
/// - Đồng bộ vị trí, hành động biến hình, vồ tát, và trạng thái trận đấu theo thời gian thực qua TCP Socket.
/// </summary>
[DisallowMultipleComponent]
public class LanNetworkManager : MonoBehaviour
{
    public static LanNetworkManager Instance { get; private set; }

    public enum NetworkRole
    {
        None,
        Host,   // Máy 1: Mèo
        Client  // Máy 2: Chuột
    }

    [Header("Network Configuration")]
    [SerializeField]
    private int port = 7777;

    [Header("Runtime State (Read Only)")]
    [SerializeField]
    private NetworkRole currentRole = NetworkRole.None;

    [SerializeField]
    private bool isConnected = false;

    public NetworkRole CurrentRole => currentRole;
    public bool IsConnected => isConnected;
    public bool IsHost => currentRole == NetworkRole.Host;
    public bool IsClient => currentRole == NetworkRole.Client;

    private TcpListener _serverListener;
    private TcpClient _clientSocket;
    private NetworkStream _networkStream;
    private StreamWriter _writer;
    private StreamReader _reader;
    private CancellationTokenSource _netCts;

    private CatController _catController;
    private MouseController _mouseController;

    private Vector2 _remoteCatPos;
    private Vector2 _remoteMousePos;
    private string _cachedLocalIp = "127.0.0.1";

    public string LocalIpAddress => _cachedLocalIp;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _cachedLocalIp = GetLocalIpAddress();
    }

    private void Start()
    {
        _catController = FindAnyObjectByType<CatController>();
        _mouseController = FindAnyObjectByType<MouseController>();
    }

    /// <summary>
    /// Lấy địa chỉ IP nội bộ của máy (LAN IP) để hiển thị cho người chơi kia nhập vào
    /// </summary>
    public static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        }
        catch { }
        return "127.0.0.1";
    }

    /// <summary>
    /// Bắt đầu tạo phòng (HOST) - Đóng vai MÈO
    /// </summary>
    public async UniTaskVoid StartHostAsync()
    {
        StopNetwork();

        currentRole = NetworkRole.Host;
        _netCts = new CancellationTokenSource();

        EnsureControllers();

        // Host là MÈO: Camera bám Mèo, Chuột trên máy Host do Client điều khiển
        if (_catController != null) _catController.isNetworkRemote = false;
        if (_mouseController != null) _mouseController.isNetworkRemote = true;

        if (GameManager.Instance != null && GameManager.Instance.cinemachineCamera != null && _catController != null)
        {
            GameManager.Instance.cinemachineCamera.Target.TrackingTarget = _catController.transform;
        }

        try
        {
            _serverListener = new TcpListener(IPAddress.Any, port);
            _serverListener.Start();
            Debug.Log($"🌐 [LanNetworkManager] Đang lắng nghe kết nối tại IP: {_cachedLocalIp}:{port} (Vai: MÈO)...");

            _clientSocket = await _serverListener.AcceptTcpClientAsync().AsUniTask();
            _networkStream = _clientSocket.GetStream();
            _writer = new StreamWriter(_networkStream, Encoding.UTF8) { AutoFlush = true };
            _reader = new StreamReader(_networkStream, Encoding.UTF8);

            isConnected = true;
            Debug.Log("✅ [LanNetworkManager] Client (Chuột) đã kết nối thành công vào phòng!");

            // Bắt đầu 2 luồng: Nhận dữ liệu và Gửi dữ liệu định kỳ
            ReceiveDataLoopAsync(_netCts.Token).Forget();
            SendDataLoopAsync(_netCts.Token).Forget();
        }
        catch (Exception ex)
        {
            if (currentRole != NetworkRole.None)
            {
                Debug.LogWarning($"[LanNetworkManager] Lỗi lắng nghe Host: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tham gia vào phòng (CLIENT) - Đóng vai CHUỘT
    /// </summary>
    public async UniTask<bool> StartClientAsync(string hostIp)
    {
        StopNetwork();

        currentRole = NetworkRole.Client;
        _netCts = new CancellationTokenSource();

        EnsureControllers();

        // Client là CHUỘT: Camera bám Chuột, Mèo trên máy Client do Host điều khiển
        if (_mouseController != null) _mouseController.isNetworkRemote = false;
        if (_catController != null) _catController.isNetworkRemote = true;

        if (GameManager.Instance != null && GameManager.Instance.cinemachineCamera != null && _mouseController != null)
        {
            GameManager.Instance.cinemachineCamera.Target.TrackingTarget = _mouseController.transform;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(hostIp)) hostIp = "127.0.0.1";

            _clientSocket = new TcpClient();
            Debug.Log($"🌐 [LanNetworkManager] Đang kết nối tới Host: {hostIp}:{port}...");

            await _clientSocket.ConnectAsync(hostIp, port).AsUniTask();
            _networkStream = _clientSocket.GetStream();
            _writer = new StreamWriter(_networkStream, Encoding.UTF8) { AutoFlush = true };
            _reader = new StreamReader(_networkStream, Encoding.UTF8);

            isConnected = true;
            Debug.Log("✅ [LanNetworkManager] Đã kết nối thành công tới Host (Vai: CHUỘT)!");

            ReceiveDataLoopAsync(_netCts.Token).Forget();
            SendDataLoopAsync(_netCts.Token).Forget();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ [LanNetworkManager] Không thể kết nối tới {hostIp}:{port} - {ex.Message}");
            StopNetwork();
            return false;
        }
    }

    /// <summary>
    /// Kết nối tới Host dưới dạng UniTaskVoid (tiện ích gọi Forget)
    /// </summary>
    public async UniTaskVoid ConnectToHostAsync(string hostIp)
    {
        await StartClientAsync(hostIp);
    }

    private void EnsureControllers()
    {
        if (_catController == null) _catController = FindAnyObjectByType<CatController>();
        if (_mouseController == null) _mouseController = FindAnyObjectByType<MouseController>();
    }

    /// <summary>
    /// Vòng lặp gửi dữ liệu trạng thái qua mạng (20 lần/giây = 50ms)
    /// </summary>
    private async UniTaskVoid SendDataLoopAsync(CancellationToken token)
    {
        while (isConnected && !token.IsCancellationRequested)
        {
            try
            {
                if (IsHost && _catController != null)
                {
                    // HOST gửi: Vị trí Mèo, Trạng thái đòn đánh, Thời gian còn lại
                    Vector2 pos = _catController.transform.position;
                    Vector2 facing = _catController.FacingDirection;
                    bool attacking = _catController.IsAttacking;
                    bool stunned = _catController.IsStunned;
                    float remainingTime = GameManager.Instance != null ? GameManager.Instance.RemainingTime : 300f;
                    int gameState = GameManager.Instance != null ? (int)GameManager.Instance.CurrentState : 1;

                    string msg = $"CAT|{pos.x:F3}|{pos.y:F3}|{facing.x:F2}|{facing.y:F2}|{(attacking ? 1 : 0)}|{(stunned ? 1 : 0)}|{remainingTime:F1}|{gameState}";
                    await _writer.WriteLineAsync(msg.AsMemory(), token);
                }
                else if (IsClient && _mouseController != null)
                {
                    // CLIENT gửi: Vị trí Chuột, Trạng thái ngụy trang và Prop ID
                    Vector2 pos = _mouseController.transform.position;
                    bool disguised = _mouseController.IsDisguised;
                    string propId = _mouseController.DisguisedPropId;

                    string msg = $"MOUSE|{pos.x:F3}|{pos.y:F3}|{(disguised ? 1 : 0)}|{propId}";
                    await _writer.WriteLineAsync(msg.AsMemory(), token);
                }

                await UniTask.Delay(50, cancellationToken: token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LanNetworkManager] Mất kết nối gửi: {ex.Message}");
                StopNetwork();
                break;
            }
        }
    }

    /// <summary>
    /// Vòng lặp nhận dữ liệu trạng thái từ máy đối diện
    /// </summary>
    private async UniTaskVoid ReceiveDataLoopAsync(CancellationToken token)
    {
        while (isConnected && !token.IsCancellationRequested)
        {
            try
            {
                string line = await _reader.ReadLineAsync().AsUniTask().AttachExternalCancellation(token);
                if (string.IsNullOrEmpty(line))
                {
                    // Kết nối bị ngắt từ phía đối diện
                    StopNetwork();
                    break;
                }

                ProcessNetworkMessage(line);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LanNetworkManager] Mất kết nối nhận: {ex.Message}");
                StopNetwork();
                break;
            }
        }
    }

    /// <summary>
    /// Phân tích và áp dụng gói tin mạng nhận được
    /// </summary>
    private void ProcessNetworkMessage(string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length < 2) return;

        string header = parts[0];

        if (header == "CAT" && IsClient && _catController != null)
        {
            // Client nhận thông tin Mèo từ Host
            if (float.TryParse(parts[1], out float x) && float.TryParse(parts[2], out float y))
            {
                _remoteCatPos = new Vector2(x, y);
                _catController.transform.position = Vector3.Lerp(_catController.transform.position, new Vector3(x, y, 0f), 0.5f);
            }

            if (float.TryParse(parts[3], out float fx) && float.TryParse(parts[4], out float fy))
            {
                _catController.SetFacingDirection(new Vector2(fx, fy));
            }

            if (parts.Length >= 7 && parts[5] == "1")
            {
                _catController.TriggerAttack();
            }

            // Đồng bộ trạng thái thắng thua từ Host
            if (parts.Length >= 9 && int.TryParse(parts[8], out int state))
            {
                if (state == (int)GameManager.GameState.CatWon && GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.CatWon)
                {
                    GameManager.Instance.TriggerCatWin();
                }
                else if (state == (int)GameManager.GameState.MouseWon && GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.MouseWon)
                {
                    GameManager.Instance.TriggerMouseWin();
                }
            }
        }
        else if (header == "MOUSE" && IsHost && _mouseController != null)
        {
            // Host nhận thông tin Chuột từ Client
            if (float.TryParse(parts[1], out float x) && float.TryParse(parts[2], out float y))
            {
                _remoteMousePos = new Vector2(x, y);
                _mouseController.transform.position = Vector3.Lerp(_mouseController.transform.position, new Vector3(x, y, 0f), 0.5f);
            }

            bool clientDisguised = parts[3] == "1";
            string propId = parts.Length > 4 ? parts[4] : "";

            if (clientDisguised != _mouseController.IsDisguised)
            {
                if (clientDisguised && !string.IsNullOrEmpty(propId))
                {
                    // Tìm PropItem có propId tương ứng
                    PropItem[] allProps = FindObjectsByType<PropItem>();
                    PropItem matchedProp = null;
                    foreach (var p in allProps)
                    {
                        if (!p.IsFake && p.PropId == propId)
                        {
                            matchedProp = p;
                            break;
                        }
                    }

                    if (matchedProp != null)
                    {
                        _mouseController.DisguiseIntoProp(matchedProp);
                    }
                }
                else if (!clientDisguised)
                {
                    _mouseController.RevertToMouseForm();
                }
            }
        }
    }

    public void StopNetwork()
    {
        isConnected = false;
        currentRole = NetworkRole.None;

        _netCts?.Cancel();
        _netCts?.Dispose();
        _netCts = null;

        try { _writer?.Dispose(); } catch { }
        try { _reader?.Dispose(); } catch { }
        try { _networkStream?.Dispose(); } catch { }
        try { _clientSocket?.Close(); } catch { }
        try { _serverListener?.Stop(); } catch { }

        _writer = null;
        _reader = null;
        _networkStream = null;
        _clientSocket = null;
        _serverListener = null;

        // Hoàn nguyên điều khiển nội bộ
        if (_catController != null) _catController.isNetworkRemote = false;
        if (_mouseController != null) _mouseController.isNetworkRemote = false;
    }

    private void OnDestroy()
    {
        StopNetwork();
    }
}
