using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu chọn chế độ chơi và Cài đặt / Chọn Map phong cách Cyber-Casual 2026.
/// Khung thẻ kính mờ (Frosted Glass), 3 nút bấm viên thuốc 3D (Vàng Cam, Cyan, Tím Pastel),
/// tích hợp Map Selector (Penthouse Hiện Đại ⇄ Nhà Kho Cũ) và bảng kết nối LAN.
/// </summary>
public class GameModeMenuView : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private RectTransform mainPanelContainer;
    [SerializeField] private RectTransform lanSetupPanelContainer;
    [SerializeField] private RectTransform settingsMapPanelContainer;

    [Header("Main Mode Buttons (3 Pill Buttons)")]
    [SerializeField] private Button soloBotButton;         // [NÚT 1] Vàng Cam: Chơi đơn vs Bot
    [SerializeField] private Button lanModeButton;         // [NÚT 2] Xanh Cyan: Chơi Online / LAN
    [SerializeField] private Button settingsMapButton;     // [NÚT 3] Tím Pastel: Cài đặt & Chọn Map

    [Header("Settings & Map Selector UI")]
    [SerializeField] private Button mapToggleButton;
    [SerializeField] private Text currentMapLabelText;
    [SerializeField] private Button local2PButton;
    [SerializeField] private Button backFromSettingsButton;

    [Header("LAN Setup UI")]
    [SerializeField] private Text localIpText;
    [SerializeField] private Button createHostButton;
    [SerializeField] private InputField clientIpInput;
    [SerializeField] private Button joinClientButton;
    [SerializeField] private Button backFromLanButton;
    [SerializeField] private Text lanStatusText;

    private void Awake()
    {
        // 1. Các nút trên Menu chính
        if (soloBotButton != null) soloBotButton.onClick.AddListener(OnSoloBotClicked);
        if (lanModeButton != null) lanModeButton.onClick.AddListener(OnLanModeClicked);
        if (settingsMapButton != null) settingsMapButton.onClick.AddListener(OnSettingsMapClicked);

        // 2. Các nút trong Cài đặt & Chọn Map
        if (mapToggleButton != null) mapToggleButton.onClick.AddListener(OnMapToggleClicked);
        if (local2PButton != null) local2PButton.onClick.AddListener(OnLocal2PClicked);
        if (backFromSettingsButton != null) backFromSettingsButton.onClick.AddListener(OnBackFromSettingsClicked);

        // 3. Các nút trong bảng LAN Setup
        if (createHostButton != null) createHostButton.onClick.AddListener(OnCreateHostClicked);
        if (joinClientButton != null) joinClientButton.onClick.AddListener(OnJoinClientClicked);
        if (backFromLanButton != null) backFromLanButton.onClick.AddListener(OnBackFromLanClicked);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (lanSetupPanelContainer != null) lanSetupPanelContainer.gameObject.SetActive(false);
        if (settingsMapPanelContainer != null) settingsMapPanelContainer.gameObject.SetActive(false);

        if (mainPanelContainer != null)
        {
            mainPanelContainer.gameObject.SetActive(true);
            mainPanelContainer.DOKill();
            mainPanelContainer.localScale = Vector3.one * 0.7f;
            mainPanelContainer.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        UpdateMapLabel();
    }

    public void Hide()
    {
        if (mainPanelContainer != null)
        {
            mainPanelContainer.DOKill();
            mainPanelContainer.DOScale(0.8f, 0.2f).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnSoloBotClicked()
    {
        Hide();
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.StartSoloBotMode();
        }
    }

    private void OnLocal2PClicked()
    {
        Hide();
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.StartLocal2PMode();
        }
    }

    private void OnSettingsMapClicked()
    {
        if (mainPanelContainer != null) mainPanelContainer.gameObject.SetActive(false);

        if (settingsMapPanelContainer != null)
        {
            settingsMapPanelContainer.gameObject.SetActive(true);
            settingsMapPanelContainer.DOKill();
            settingsMapPanelContainer.localScale = Vector3.one * 0.7f;
            settingsMapPanelContainer.DOScale(Vector3.one, 0.38f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        UpdateMapLabel();
    }

    private void OnMapToggleClicked()
    {
        if (MapManager.Instance != null)
        {
            MapManager.Instance.ToggleMap();
            UpdateMapLabel();

            if (mapToggleButton != null)
            {
                mapToggleButton.transform.DOKill();
                mapToggleButton.transform.DOPunchScale(Vector3.one * 0.18f, 0.28f, 8, 0.8f);
            }
        }
    }

    private void UpdateMapLabel()
    {
        if (currentMapLabelText != null && MapManager.Instance != null)
        {
            currentMapLabelText.text = $"BẢN ĐỒ: {MapManager.Instance.CurrentMapName}";
        }
    }

    private void OnBackFromSettingsClicked()
    {
        if (settingsMapPanelContainer != null) settingsMapPanelContainer.gameObject.SetActive(false);

        if (mainPanelContainer != null)
        {
            mainPanelContainer.gameObject.SetActive(true);
            mainPanelContainer.DOKill();
            mainPanelContainer.localScale = Vector3.one * 0.8f;
            mainPanelContainer.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    private void OnLanModeClicked()
    {
        if (mainPanelContainer != null) mainPanelContainer.gameObject.SetActive(false);

        if (lanSetupPanelContainer != null)
        {
            lanSetupPanelContainer.gameObject.SetActive(true);
            lanSetupPanelContainer.DOKill();
            lanSetupPanelContainer.localScale = Vector3.one * 0.7f;
            lanSetupPanelContainer.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        if (localIpText != null && LanNetworkManager.Instance != null)
        {
            localIpText.text = $"Địa chỉ IP máy bạn: {LanNetworkManager.Instance.LocalIpAddress}";
        }

        if (lanStatusText != null)
        {
            lanStatusText.text = "Sẵn sàng tạo phòng hoặc kết nối";
            lanStatusText.color = new Color(0.6f, 0.7f, 0.8f, 1f);
        }
    }

    private void OnCreateHostClicked()
    {
        if (LanNetworkManager.Instance != null)
        {
            LanNetworkManager.Instance.StartHostAsync().Forget();
            if (lanStatusText != null)
            {
                lanStatusText.text = "Đang mở phòng chờ Client (Chuột) kết nối...";
                lanStatusText.color = new Color(0.96f, 0.62f, 0.04f, 1f);
            }
        }

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.StartLanHostMode();
        }

        Hide();
    }

    private void OnJoinClientClicked()
    {
        string hostIp = clientIpInput != null ? clientIpInput.text.Trim() : "";
        if (string.IsNullOrEmpty(hostIp))
        {
            if (lanStatusText != null)
            {
                lanStatusText.text = "Vui lòng nhập IP máy Host!";
                lanStatusText.color = new Color(0.94f, 0.27f, 0.27f, 1f);
            }
            return;
        }

        if (LanNetworkManager.Instance != null)
        {
            LanNetworkManager.Instance.ConnectToHostAsync(hostIp).Forget();
        }

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.StartLanClientMode(hostIp);
        }

        Hide();
    }

    private void OnBackFromLanClicked()
    {
        if (lanSetupPanelContainer != null) lanSetupPanelContainer.gameObject.SetActive(false);

        if (mainPanelContainer != null)
        {
            mainPanelContainer.gameObject.SetActive(true);
            mainPanelContainer.DOKill();
            mainPanelContainer.localScale = Vector3.one * 0.8f;
            mainPanelContainer.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }
}
