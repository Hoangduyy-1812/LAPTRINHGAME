using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Trình quản lý chế độ chơi (GameModeManager):
/// Cung cấp giao diện Menu khởi đầu và điều phối:
/// 1. Chơi đơn Offline vs Bot Mèo (CatAIController).
/// 2. Chơi Online / LAN 2 máy riêng biệt (Host Mèo ⇄ Client Chuột qua LanNetworkManager).
/// 3. Chơi 2 người cùng bàn phím (Local 2P cổ điển).
/// </summary>
[DisallowMultipleComponent]
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    public enum SelectedMode
    {
        None,
        SingleplayerBot,    // Chơi đơn vs Bot Mèo
        OnlineMultiplayer,  // Online / Mạng LAN 2 máy
        LocalTwoPlayers     // 2 người chung bàn phím
    }

    [Header("Current Mode")]
    [SerializeField]
    private SelectedMode activeMode = SelectedMode.None;
    public SelectedMode ActiveMode => activeMode;

    [Header("Network Sub-State")]
    private bool _showOnlineSubMenu = false;
    private string _targetHostIp = "127.0.0.1";
    private string _connectionStatus = "";

    private CatController _cat;
    private MouseController _mouse;
    private CatAIController _catAi;
    private LanNetworkManager _network;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        FindGameEntities();

        // Tạm thời dừng đếm giờ cho tới khi người chơi chọn chế độ
        if (GameManager.Instance != null && activeMode == SelectedMode.None)
        {
            // Trạng thái Preparing chờ chọn Menu
        }
    }

    private void FindGameEntities()
    {
        if (_cat == null) _cat = FindAnyObjectByType<CatController>();
        if (_mouse == null) _mouse = FindAnyObjectByType<MouseController>();
        if (_network == null) _network = FindAnyObjectByType<LanNetworkManager>();

        if (_cat != null && _catAi == null)
        {
            _catAi = _cat.GetComponent<CatAIController>();
            if (_catAi == null)
            {
                _catAi = _cat.gameObject.AddComponent<CatAIController>();
            }
            _catAi.enabled = false; // Mặc định tắt AI
        }
    }

    /// <summary>
    /// Kích hoạt Chế độ 1: Chơi Đơn Offline vs Bot Mèo
    /// </summary>
    public void StartSingleplayerVsBot()
    {
        FindGameEntities();
        activeMode = SelectedMode.SingleplayerBot;

        // Bật AI cho Mèo
        if (_catAi != null)
        {
            _catAi.enabled = true;
        }

        // Camera khóa theo dõi Chuột (Người chơi)
        if (GameManager.Instance != null && GameManager.Instance.cinemachineCamera != null && _mouse != null)
        {
            GameManager.Instance.cinemachineCamera.Target.TrackingTarget = _mouse.transform;
        }

        // Bắt đầu trận đấu
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartMatchAsync().Forget();
        }
    }

    /// <summary>
    /// Kích hoạt Chế độ 2: Tạo phòng Host (Mèo)
    /// </summary>
    public void StartOnlineHost()
    {
        FindGameEntities();
        activeMode = SelectedMode.OnlineMultiplayer;
        _connectionStatus = "Đang tạo phòng...";

        if (_catAi != null) _catAi.enabled = false;

        if (_network == null)
        {
            _network = gameObject.AddComponent<LanNetworkManager>();
        }

        _network.StartHostAsync().Forget();
        _connectionStatus = $"ĐÃ TẠO PHÒNG! Chờ máy khác vào IP: {_network.LocalIpAddress}";

        // Bắt đầu đếm giờ
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartMatchAsync().Forget();
        }
    }

    /// <summary>
    /// Kích hoạt Chế độ 2: Vào phòng Client (Chuột)
    /// </summary>
    public async UniTaskVoid StartOnlineClientAsync()
    {
        FindGameEntities();
        activeMode = SelectedMode.OnlineMultiplayer;
        _connectionStatus = $"Đang kết nối tới {_targetHostIp}...";

        if (_catAi != null) _catAi.enabled = false;

        if (_network == null)
        {
            _network = gameObject.AddComponent<LanNetworkManager>();
        }

        bool ok = await _network.StartClientAsync(_targetHostIp);
        if (ok)
        {
            _connectionStatus = "ĐÃ VÀO PHÒNG THÀNH CÔNG!";
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartMatchAsync().Forget();
            }
        }
        else
        {
            _connectionStatus = $"Không thể kết nối tới {_targetHostIp}! Vui lòng thử lại.";
        }
    }

    /// <summary>
    /// Kích hoạt Chế độ 3: 2 Người chơi trên cùng 1 bàn phím (Local 2P)
    /// </summary>
    public void StartLocalTwoPlayers()
    {
        FindGameEntities();
        activeMode = SelectedMode.LocalTwoPlayers;

        if (_catAi != null) _catAi.enabled = false;
        if (_cat != null) _cat.isAIControlled = false;

        if (GameManager.Instance != null && GameManager.Instance.cinemachineCamera != null && _cat != null)
        {
            GameManager.Instance.cinemachineCamera.Target.TrackingTarget = _cat.transform;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartMatchAsync().Forget();
        }
    }

    public void StartSoloBotMode() => StartSingleplayerVsBot();
    public void StartLanHostMode() => StartOnlineHost();
    public void StartLanClientMode(string ip = "")
    {
        if (!string.IsNullOrEmpty(ip)) _targetHostIp = ip;
        StartOnlineClientAsync().Forget();
    }
    public void StartLocal2PMode() => StartLocalTwoPlayers();

    /// <summary>
    /// Đặt lại chế độ về None và quay trở lại Menu chính
    /// </summary>
    public void ResetToMenu()
    {
        activeMode = SelectedMode.None;
        if (_catAi != null) _catAi.enabled = false;
        if (_network != null) _network.StopNetwork();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetMatch();
        }
    }

    private void OnGUI()
    {
        // Khi đã có UIManager Canvas hiện đại -> bỏ qua OnGUI cũ
        if (UIManager.Instance != null) return;

        // 1. Menu chính khi chưa chọn chế độ
        if (activeMode == SelectedMode.None)
        {
            DrawMainMenuUI();
            return;
        }

        // 2. HUD trạng thái chế độ đang chơi ở góc trên bên phải
        DrawActiveModeHUD();
    }

    /// <summary>
    /// Vẽ giao diện Menu chọn chế độ phong cách Retro Hotline Miami
    /// </summary>
    private void DrawMainMenuUI()
    {
        float menuWidth = 460f;
        float menuHeight = 360f;
        Rect menuRect = new Rect((Screen.width - menuWidth) * 0.5f, (Screen.height - menuHeight) * 0.5f, menuWidth, menuHeight);

        GUIStyle windowStyle = new GUIStyle(GUI.skin.window)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold
        };
        GUI.Box(menuRect, "CHỌN CHẾ ĐỘ CHƠI", windowStyle);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(menuRect.x + 10f, menuRect.y + 35f, menuWidth - 20f, 35f), "🐱 CAT and MOUSE 🐭", titleStyle);

        GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter
        };
        subTitleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(menuRect.x + 10f, menuRect.y + 68f, menuWidth - 20f, 25f), "2D Top-down Prop Hunt Arena (Unity 6 URP)", subTitleStyle);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };

        if (!_showOnlineSubMenu)
        {
            // === MENU CHÍNH ===
            float btnY = menuRect.y + 105f;
            float btnH = 55f;
            float spacing = 15f;

            // NÚT 1: CHƠI ĐƠN VS BOT MÈO
            if (GUI.Button(new Rect(menuRect.x + 30f, btnY, menuWidth - 60f, btnH), "🎮 CHƠI ĐƠN OFFLINE (VS BOT MÈO)\n<size=11>Bạn điều khiển Chuột trốn chạy & ngụy trang</size>", btnStyle))
            {
                StartSingleplayerVsBot();
            }

            // NÚT 2: CHƠI ONLINE / LAN (2 MÁY)
            btnY += btnH + spacing;
            if (GUI.Button(new Rect(menuRect.x + 30f, btnY, menuWidth - 60f, btnH), "🌐 CHƠI ONLINE / MẠNG LAN (2 MÁY)\n<size=11>2 máy riêng biệt, góc nhìn bảo mật tuyệt đối</size>", btnStyle))
            {
                _showOnlineSubMenu = true;
            }

            // NÚT 3: 2 NGƯỜI CHUNG BÀN PHÍM
            btnY += btnH + spacing;
            if (GUI.Button(new Rect(menuRect.x + 30f, btnY, menuWidth - 60f, 45f), "👥 2 NGƯỜI CÙNG PHÍM (WASD + MŨI TÊN)", btnStyle))
            {
                StartLocalTwoPlayers();
            }
        }
        else
        {
            // === MENU ONLINE LAN (2 MÁY) ===
            float btnY = menuRect.y + 105f;

            // Nút Tạo phòng (Host)
            if (GUI.Button(new Rect(menuRect.x + 30f, btnY, menuWidth - 60f, 50f), "🏠 TẠO PHÒNG (MÁY 1 - VAI MÈO)\n<size=11>Làm máy chủ, camera khóa Mèo</size>", btnStyle))
            {
                StartOnlineHost();
            }

            btnY += 60f;
            // Nhập IP Host
            GUI.Label(new Rect(menuRect.x + 30f, btnY, 150f, 25f), "Nhập IP Máy 1 (Host):");
            _targetHostIp = GUI.TextField(new Rect(menuRect.x + 185f, btnY, 140f, 25f), _targetHostIp);

            btnY += 32f;
            // Nút Vào phòng (Client)
            if (GUI.Button(new Rect(menuRect.x + 30f, btnY, menuWidth - 60f, 50f), "🚪 VÀO PHÒNG (MÁY 2 - VAI CHUỘT)\n<size=11>Kết nối vào IP trên, camera khóa Chuột</size>", btnStyle))
            {
                StartOnlineClientAsync().Forget();
            }

            btnY += 58f;
            // Nút Quay lại
            if (GUI.Button(new Rect(menuRect.x + 130f, btnY, 200f, 35f), "⬅ QUAY LẠI"))
            {
                _showOnlineSubMenu = false;
            }
        }
    }

    /// <summary>
    /// Hiển thị HUD thông tin chế độ chơi ở góc trên bên phải
    /// </summary>
    private void DrawActiveModeHUD()
    {
        GUIStyle hudStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 12,
            alignment = TextAnchor.UpperLeft
        };
        hudStyle.normal.textColor = Color.white;

        float hudW = 280f;
        float hudH = 50f;
        Rect hudRect = new Rect(Screen.width - hudW - 15f, 15f, hudW, hudH);

        string modeStr = "";
        switch (activeMode)
        {
            case SelectedMode.SingleplayerBot:
                modeStr = "<b>[CHẾ ĐỘ: ĐƠN VS BOT MÈO]</b>\n• Chuột: Bạn điều khiển | Mèo: AI tự động";
                break;
            case SelectedMode.OnlineMultiplayer:
                string role = _network != null && _network.IsHost ? "MÈO (HOST)" : "CHUỘT (CLIENT)";
                string conn = _network != null && _network.IsConnected ? "<color=green>ĐÃ KẾT NỐI</color>" : "<color=yellow>ĐANG CHỜ...</color>";
                modeStr = $"<b>[ONLINE LAN - VAI: {role}]</b>\n• Trạng thái: {conn}";
                break;
            case SelectedMode.LocalTwoPlayers:
                modeStr = "<b>[CHẾ ĐỘ: 2 NGƯỜI CHUNG PHÍM]</b>\n• WASD: Chuột | Mũi tên: Mèo | TAB: Đổi Cam";
                break;
        }

        GUI.Box(hudRect, modeStr, hudStyle);
    }
}
