using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton điều phối toàn bộ hệ thống giao diện Modern Casual 2026 trong game CATandMOUSE.
/// Đồng bộ dữ liệu từ GameManager, MouseController, CatController lên InGameHUDView,
/// VictoryModalView và GameModeMenuView theo thời gian thực.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Views")]
    [SerializeField] private InGameHUDView hudView;
    [SerializeField] private VictoryModalView victoryModalView;
    [SerializeField] private GameModeMenuView modeMenuView;

    public InGameHUDView HUDView => hudView;
    public VictoryModalView VictoryModal => victoryModalView;
    public GameModeMenuView ModeMenu => modeMenuView;

    private MouseController _cachedMouse;
    private CatController _cachedCat;

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
        FindControllers();

        // Nếu mới vào game và chưa chọn chế độ -> hiển thị Menu chính
        if (GameModeManager.Instance != null && GameModeManager.Instance.ActiveMode == GameModeManager.SelectedMode.None)
        {
            ShowModeMenu();
        }
        else
        {
            ShowHUD();
        }
    }

    private void Update()
    {
        FindControllers();

        // 1. Cập nhật HUD khi đang thi đấu
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            if (hudView != null && hudView.gameObject.activeSelf)
            {
                hudView.UpdateTimer(GameManager.Instance.RemainingTime);

                if (_cachedMouse != null)
                {
                    hudView.UpdateMouseSkill(
                        _cachedMouse.IsDisguised,
                        _cachedMouse.IsCooldown,
                        _cachedMouse.CooldownRemaining,
                        _cachedMouse.DisguiseCooldown
                    );
                }

                if (_cachedCat != null)
                {
                    hudView.UpdateCatSkill(
                        _cachedCat.IsStunned,
                        _cachedCat.StunRemaining,
                        _cachedCat.StunDuration,
                        _cachedCat.IsAttacking,
                        _cachedCat.AttackCooldownRemaining,
                        _cachedCat.AttackCooldown
                    );
                }

                // Cập nhật Radar cảnh báo Mèo đến gần khi người chơi điều khiển Chuột
                if (_cachedMouse != null && _cachedCat != null)
                {
                    float distanceToCat = Vector2.Distance(_cachedMouse.transform.position, _cachedCat.transform.position);
                    hudView.UpdateProximity(distanceToCat);
                }
            }
        }
    }

    private void FindControllers()
    {
        if (_cachedMouse == null) _cachedMouse = FindAnyObjectByType<MouseController>();
        if (_cachedCat == null) _cachedCat = FindAnyObjectByType<CatController>();
    }

    public void ShowModeMenu()
    {
        if (hudView != null) hudView.gameObject.SetActive(false);
        if (victoryModalView != null) victoryModalView.gameObject.SetActive(false);
        if (modeMenuView != null) modeMenuView.Show();
    }

    public void ShowHUD()
    {
        if (modeMenuView != null) modeMenuView.Hide();
        if (victoryModalView != null) victoryModalView.gameObject.SetActive(false);
        if (hudView != null)
        {
            hudView.gameObject.SetActive(true);
            UpdateModeBadge();
        }
    }

    public void ShowVictory(bool isCatWon, float survivedSeconds)
    {
        if (hudView != null) hudView.gameObject.SetActive(false);
        if (victoryModalView != null)
        {
            victoryModalView.Show(
                isCatWon,
                survivedSeconds,
                onPlayAgain: () =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.RestartMatch();
                    }
                    else
                    {
                        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    }
                },
                onMainMenu: () =>
                {
                    if (GameModeManager.Instance != null)
                    {
                        GameModeManager.Instance.ResetToMenu();
                    }
                    ShowModeMenu();
                }
            );
        }
    }

    private void UpdateModeBadge()
    {
        if (hudView == null) return;

        if (GameModeManager.Instance != null)
        {
            switch (GameModeManager.Instance.ActiveMode)
            {
                case GameModeManager.SelectedMode.SingleplayerBot:
                    hudView.SetModeBadge("🤖 CHƠI ĐƠN VS BOT");
                    break;
                case GameModeManager.SelectedMode.OnlineMultiplayer:
                    hudView.SetModeBadge("🌐 MẠNG LAN 2 MÁY");
                    break;
                case GameModeManager.SelectedMode.LocalTwoPlayers:
                    hudView.SetModeBadge("👥 2 NGƯỜI CỤC BỘ");
                    break;
                default:
                    hudView.SetModeBadge("🎮 CAT AND MOUSE");
                    break;
            }
        }
        else
        {
            hudView.SetModeBadge("🎮 CAT AND MOUSE");
        }
    }

    public static Font GetDefaultUIFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        return font;
    }
}
