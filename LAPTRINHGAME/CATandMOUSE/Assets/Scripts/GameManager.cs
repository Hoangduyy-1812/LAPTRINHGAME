using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Cysharp.Threading.Tasks;
using DG.Tweening;

/// <summary>
/// Quản lý vòng đời ván đấu (GameManager Singleton):
/// Đếm ngược 300 giây bằng UniTask, quản lý trạng thái thắng/thua,
/// hiển thị giao diện qua Canvas UI hoặc tự động vẽ qua OnGUI (không cần thiết lập Canvas thủ công).
/// </summary>
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Preparing,
        Playing,
        CatWon,
        MouseWon
    }

    #region Singleton
    public static GameManager Instance { get; private set; }
    #endregion

    [Header("Match Settings")]
    [SerializeField, Tooltip("Thời lượng trận đấu tính bằng giây (300s = 5 phút)")]
    private float matchDuration = 300f;

    [Header("Player References")]
    [Tooltip("Transform của Mèo (Cat)")]
    public Transform catTransform;

    [Tooltip("Transform của Chuột (Mouse)")]
    public Transform mouseTransform;

    [Tooltip("Script điều khiển của Mèo")]
    public CatController catController;

    [Tooltip("Script điều khiển của Chuột")]
    public MouseController mouseController;

    [Header("Camera Tracking")]
    [Tooltip("Cinemachine Camera bám theo nhân vật")]
    public Unity.Cinemachine.CinemachineCamera cinemachineCamera;

    [Header("Optional UI References (Canvas UI)")]
    [SerializeField, Tooltip("Text hiển thị đồng hồ đếm ngược (định dạng mm:ss)")]
    private TextMeshProUGUI timerText;

    [SerializeField, Tooltip("Thanh trượt biểu thị lượng thời gian còn lại")]
    private Slider timerSlider;

    [SerializeField, Tooltip("Panel hiển thị khi kết thúc ván đấu")]
    private GameObject gameOverPanel;

    [SerializeField, Tooltip("Text thông báo phe chiến thắng trên bảng Game Over")]
    private TextMeshProUGUI resultText;

    [SerializeField, Tooltip("Nút bấm để chơi lại ván mới")]
    private Button restartButton;

    [Header("State (Read Only)")]
    [SerializeField, Tooltip("Trạng thái hiện tại của ván đấu")]
    private GameState currentState = GameState.Preparing;

    public GameState CurrentState => currentState;
    public float RemainingTime => _remainingTime;
    public float MatchDuration => matchDuration;

    private float _remainingTime;
    private Tween _gameOverPanelTween;
    private string _resultTitle = "";
    private string _resultDescription = "";

    private void Awake()
    {
        // Khởi tạo Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Đăng ký sự kiện click cho nút Restart nếu có
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartMatch);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Nếu có GameModeManager và chưa chọn chế độ -> chờ chọn ở Menu khởi đầu
        if (GameModeManager.Instance != null && GameModeManager.Instance.ActiveMode == GameModeManager.SelectedMode.None)
        {
            currentState = GameState.Preparing;
            _remainingTime = matchDuration;
            UpdateTimerDisplay(_remainingTime);
            return;
        }

        // Khởi động ván đấu
        StartMatchAsync().Forget();
    }

    private void Update()
    {
        // Phím TAB: Chuyển đổi góc nhìn Camera giữa Mèo và Chuột
        if (Input.GetKeyDown(KeyCode.Tab) && cinemachineCamera != null)
        {
            if (cinemachineCamera.Target.TrackingTarget == catTransform)
            {
                cinemachineCamera.Target.TrackingTarget = mouseTransform;
            }
            else
            {
                cinemachineCamera.Target.TrackingTarget = catTransform;
            }
        }
    }

    /// <summary>
    /// Bắt đầu đếm ngược thời gian trận đấu bằng UniTask loop.
    /// Không dùng Coroutine, cập nhật mượt mà mỗi frame.
    /// </summary>
    public async UniTaskVoid StartMatchAsync()
    {
        currentState = GameState.Playing;
        _remainingTime = matchDuration;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        UpdateTimerDisplay(_remainingTime);

        try
        {
            while (_remainingTime > 0f && currentState == GameState.Playing)
            {
                _remainingTime -= Time.deltaTime;
                if (_remainingTime < 0f) _remainingTime = 0f;

                UpdateTimerDisplay(_remainingTime);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            // Hết 300 giây mà Chuột chưa bị tát trúng -> Chuột chiến thắng
            if (currentState == GameState.Playing && _remainingTime <= 0f)
            {
                TriggerMouseWin();
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    /// <summary>
    /// Cập nhật text mm:ss và Slider nếu có Canvas UI.
    /// </summary>
    private void UpdateTimerDisplay(float secondsRemaining)
    {
        int minutes = Mathf.FloorToInt(secondsRemaining / 60f);
        int seconds = Mathf.FloorToInt(secondsRemaining % 60f);

        if (timerText != null)
        {
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        if (timerSlider != null && matchDuration > 0f)
        {
            timerSlider.value = Mathf.Clamp01(secondsRemaining / matchDuration);
        }
    }

    /// <summary>
    /// Kích hoạt khi Mèo tát trúng Chuột -> Mèo chiến thắng!
    /// </summary>
    public void TriggerCatWin()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.CatWon;
        _resultTitle = "MÈO CHIẾN THẮNG!";
        _resultDescription = "Mèo đã tóm gọn Chuột ngụy trang!";

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictory(isCatWon: true, matchDuration - _remainingTime);
        }
        else
        {
            ShowGameOverUI(_resultTitle, _resultDescription);
        }
    }

    /// <summary>
    /// Kích hoạt khi hết 300 giây mà Chuột chưa bị bắt -> Chuột chiến thắng!
    /// </summary>
    public void TriggerMouseWin()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.MouseWon;
        _resultTitle = "CHUỘT CHIẾN THẮNG!";
        _resultDescription = "Chuột đã ngụy trang thành công hết 300 giây!";

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictory(isCatWon: false, matchDuration);
        }
        else
        {
            ShowGameOverUI(_resultTitle, _resultDescription);
        }
    }

    /// <summary>
    /// Hiển thị bảng Game Over với animation scale bằng DOTween (nếu có UI).
    /// </summary>
    private void ShowGameOverUI(string title, string description)
    {
        if (resultText != null)
        {
            resultText.text = $"<b>{title}</b>\n<size=70%>{description}</size>";
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.localScale = Vector3.zero;

            _gameOverPanelTween?.Kill();
            _gameOverPanelTween = gameOverPanel.transform.DOScale(Vector3.one, 0.4f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// Tải lại Scene hiện tại để chơi ván mới.
    /// </summary>
    public void RestartMatch()
    {
        _gameOverPanelTween?.Kill();
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    /// <summary>
    /// Đặt lại trận đấu và quay trở về màn hình ban đầu
    /// </summary>
    public void ResetMatch()
    {
        RestartMatch();
    }

    private void OnDestroy()
    {
        _gameOverPanelTween?.Kill();
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartMatch);
        }
    }

    /// <summary>
    /// Tự động hiển thị HUD trực tiếp trên màn hình game (Fallback OnGUI).
    /// Giúp người chơi có ngay giao diện đếm giờ và kết quả thắng/thua
    /// mà KHÔNG CẦN phải tự tạo Canvas UI phức tạp!
    /// </summary>
    private void OnGUI()
    {
        // Khi đã có UIManager Canvas hiện đại -> bỏ qua OnGUI cũ
        if (UIManager.Instance != null) return;

        // Nếu chưa chọn chế độ ở Menu chính -> không vẽ đè HUD lên Menu
        if (GameModeManager.Instance != null && GameModeManager.Instance.ActiveMode == GameModeManager.SelectedMode.None)
        {
            return;
        }

        // 1. Hiển thị HUD thời gian ở trên cùng giữa màn hình
        int minutes = Mathf.FloorToInt(_remainingTime / 60f);
        int seconds = Mathf.FloorToInt(_remainingTime % 60f);

        GUIStyle timerStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        timerStyle.normal.textColor = _remainingTime <= 30f ? Color.red : Color.white;

        float boxWidth = 240f;
        float boxHeight = 40f;
        Rect timerRect = new Rect((Screen.width - boxWidth) * 0.5f, 15f, boxWidth, boxHeight);
        GUI.Box(timerRect, $"THỜI GIAN: {minutes:00}:{seconds:00}", timerStyle);

        // 2. Bảng hướng dẫn góc trên bên trái
        GUIStyle helpStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 13,
            alignment = TextAnchor.UpperLeft
        };
        helpStyle.normal.textColor = Color.white;
        Rect helpRect = new Rect(15f, 15f, 320f, 95f);
        string helpText = "<b>[HƯỚNG DẪN ĐIỀU KHIỂN]</b>\n" +
                          "• <color=yellow>CHUỘT</color>: Di chuyển <b>WASD</b> | <b>SPACE</b>: Biến hình\n" +
                          "• <color=red>MÈO</color>: Di chuyển <b>MŨI TÊN</b> | <b>J / Click</b>: Tát\n" +
                          "• <color=cyan>TAB</color>: Đổi góc nhìn Camera (Mèo ⇄ Chuột)";
        GUI.Box(helpRect, helpText, helpStyle);

        // 3. Hiển thị Popup Game Over nếu trận đấu đã kết thúc
        if (currentState == GameState.CatWon || currentState == GameState.MouseWon)
        {
            float panelWidth = 420f;
            float panelHeight = 220f;
            Rect panelRect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);

            GUIStyle panelStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            GUI.Box(panelRect, "KẾT QUẢ VÁN ĐẤU", panelStyle);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = (currentState == GameState.CatWon) ? Color.red : Color.green;

            GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 40f, panelWidth - 20f, 40f), _resultTitle, titleStyle);

            GUIStyle descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            descStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(panelRect.x + 10f, panelRect.y + 85f, panelWidth - 20f, 35f), _resultDescription, descStyle);

            // Nút bấm Chơi Lại
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            if (GUI.Button(new Rect(panelRect.x + 110f, panelRect.y + 140f, 200f, 45f), "CHƠI LẠI (RESTART)", btnStyle))
            {
                RestartMatch();
            }
        }
    }
}
