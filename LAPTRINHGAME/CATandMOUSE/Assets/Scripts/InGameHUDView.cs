using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện HUD trong trận đấu (In-Game HUD) phong cách Hoạt hình Chunky Among Us / Brawl Stars 3D:
/// - Đồng hồ đếm ngược đặt trong khung viên thuốc viền đen nổi bật, số đếm to rõ màu trắng viền đen.
/// - 2 Icon kỹ năng [SPACE] & [J] to gấp 2 lần bình thường ở góc dưới bên phải, 3D tròn viền đen dày.
/// - Nút bánh răng Cài đặt [⚙] ở góc trên bên trái thay thế hoàn toàn bảng chữ dài dòng.
/// </summary>
public class InGameHUDView : MonoBehaviour
{
    [Header("Timer UI (Top Center)")]
    [SerializeField] private RectTransform timerContainer;
    [SerializeField] private Image timerBg;
    [SerializeField] private Text timerText;

    [Header("Settings & Controls (Top Left)")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject settingsPopup;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private RectTransform controlsCard;
    [SerializeField] private Text modeBadgeText;

    [Header("Skills Display (Bottom Right)")]
    [SerializeField] private RectTransform skillBarContainer;

    [Header("Skill Space (Mouse Disguise)")]
    [SerializeField] private RectTransform spaceSkillTransform;
    [SerializeField] private Text spaceActionLabel;
    [SerializeField] private Image spaceCooldownFill;
    [SerializeField] private Text spaceCooldownText;

    [Header("Skill J (Cat Attack)")]
    [SerializeField] private RectTransform jSkillTransform;
    [SerializeField] private Text jActionLabel;
    [SerializeField] private Image jCooldownFill;
    [SerializeField] private Text jCooldownText;
    [Header("Proximity Heartbeat Indicator (Radar)")]
    [SerializeField] private Image proximityVignetteOverlay;
    [SerializeField] private RectTransform proximityWarningBadge;
    [SerializeField] private Text proximityWarningText;

    private Tween _timerPulseTween;
    private Tween _proximityPulseTween;
    private bool _isTimerPulsing = false;
    private bool _isTimerIntense = false;
    private bool _isProximityActive = false;
    private bool _wasSpaceCooldown = false;
    private bool _wasCatStunned = false;

    private void Awake()
    {
        if (spaceCooldownFill != null)
        {
            spaceCooldownFill.fillAmount = 0f;
        }
        if (jCooldownFill != null)
        {
            jCooldownFill.fillAmount = 0f;
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ToggleSettingsPopup);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.AddListener(() =>
            {
                if (settingsPopup != null) settingsPopup.SetActive(false);
            });
        }
    }

    private void OnDisable()
    {
        KillTimerPulse();
    }

    private void OnDestroy()
    {
        KillTimerPulse();
    }

    /// <summary>
    /// Bật/tắt popup cài đặt và bảng hướng dẫn phím bấm dạng hộp thoại hoạt hình
    /// </summary>
    public void ToggleSettingsPopup()
    {
        if (settingsPopup != null)
        {
            bool willOpen = !settingsPopup.activeSelf;
            settingsPopup.SetActive(willOpen);
            if (willOpen)
            {
                settingsPopup.transform.DOKill();
                settingsPopup.transform.localScale = Vector3.one * 0.7f;
                settingsPopup.transform.DOScale(Vector3.one, 0.28f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
        }
    }

    /// <summary>
    /// Cập nhật đồng hồ đếm ngược. Khi dưới 30s tự động chuyển sang đỏ rực rỡ và đập nhịp tim dồn dập.
    /// </summary>
    public void UpdateTimer(float remainingSeconds)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
        int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";

        if (remainingSeconds <= 30f && remainingSeconds > 0f)
        {
            timerText.color = new Color(1f, 0.20f, 0.25f, 1f); // Đỏ Neon đậm #EF4444
            if (!_isTimerIntense && timerContainer != null)
            {
                _isTimerIntense = true;
                _isTimerPulsing = true;
                timerContainer.DOKill();
                _timerPulseTween = timerContainer.DOScale(1.24f, 0.28f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }
        else if (remainingSeconds <= 60f && remainingSeconds > 0f)
        {
            timerText.color = new Color(1f, 0.40f, 0.40f, 1f);
            if (!_isTimerPulsing && timerContainer != null)
            {
                _isTimerPulsing = true;
                _isTimerIntense = false;
                timerContainer.DOKill();
                _timerPulseTween = timerContainer.DOScale(1.15f, 0.45f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
        }
        else
        {
            timerText.color = Color.white;
            KillTimerPulse();
        }
    }

    private void KillTimerPulse()
    {
        if (_isTimerPulsing || _isTimerIntense)
        {
            _isTimerPulsing = false;
            _isTimerIntense = false;
            _timerPulseTween?.Kill();
            if (timerContainer != null)
            {
                timerContainer.localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// Cập nhật tên chế độ hiển thị trên HUD
    /// </summary>
    public void SetModeBadge(string modeName)
    {
        if (modeBadgeText != null)
        {
            modeBadgeText.text = modeName;
        }
    }

    /// <summary>
    /// Cập nhật trạng thái phím [SPACE] của Chuột (Biến hình / Hoàn nguyên / Cooldown)
    /// </summary>
    public void UpdateMouseSkill(bool isDisguised, bool isCooldown, float cooldownRemaining, float totalCooldown)
    {
        if (spaceActionLabel != null)
        {
            spaceActionLabel.text = isDisguised ? "HOÀN NGUYÊN" : "BIẾN HÌNH";
            spaceActionLabel.color = isDisguised ? new Color(1f, 0.85f, 0.3f, 1f) : Color.white;
        }

        if (isCooldown && totalCooldown > 0f)
        {
            _wasSpaceCooldown = true;
            if (spaceCooldownFill != null)
            {
                spaceCooldownFill.gameObject.SetActive(true);
                spaceCooldownFill.fillAmount = Mathf.Clamp01(cooldownRemaining / totalCooldown);
            }
            if (spaceCooldownText != null)
            {
                spaceCooldownText.gameObject.SetActive(true);
                spaceCooldownText.text = $"{cooldownRemaining:0.0}s";
            }
        }
        else
        {
            if (spaceCooldownFill != null) spaceCooldownFill.gameObject.SetActive(false);
            if (spaceCooldownText != null) spaceCooldownText.gameObject.SetActive(false);

            // Khi vừa hồi chiêu xong -> nảy lò xo co giãn báo hiệu
            if (_wasSpaceCooldown)
            {
                _wasSpaceCooldown = false;
                if (spaceSkillTransform != null)
                {
                    spaceSkillTransform.DOKill();
                    spaceSkillTransform.DOPunchScale(Vector3.one * 0.28f, 0.35f, 8, 0.8f);
                }
            }
        }
    }

    /// <summary>
    /// Cập nhật trạng thái phím [J] của Mèo (Tấn công / Choáng Stun / Cooldown)
    /// </summary>
    public void UpdateCatSkill(bool isStunned, float stunRemaining, float totalStunDuration, bool isAttacking, float attackCooldownRemaining, float totalAttackCooldown)
    {
        if (isStunned && totalStunDuration > 0f)
        {
            _wasCatStunned = true;
            if (jActionLabel != null)
            {
                jActionLabel.text = "CHOÁNG!";
                jActionLabel.color = new Color(1f, 0.85f, 0.3f, 1f);
            }
            if (jCooldownFill != null)
            {
                jCooldownFill.gameObject.SetActive(true);
                jCooldownFill.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
                jCooldownFill.fillAmount = Mathf.Clamp01(stunRemaining / totalStunDuration);
            }
            if (jCooldownText != null)
            {
                jCooldownText.gameObject.SetActive(true);
                jCooldownText.text = $"{stunRemaining:0.0}s";
            }
        }
        else if (attackCooldownRemaining > 0f && totalAttackCooldown > 0f)
        {
            if (jActionLabel != null)
            {
                jActionLabel.text = "HỒI CHIÊU";
                jActionLabel.color = Color.white;
            }
            if (jCooldownFill != null)
            {
                jCooldownFill.gameObject.SetActive(true);
                jCooldownFill.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
                jCooldownFill.fillAmount = Mathf.Clamp01(attackCooldownRemaining / totalAttackCooldown);
            }
            if (jCooldownText != null)
            {
                jCooldownText.gameObject.SetActive(true);
                jCooldownText.text = $"{attackCooldownRemaining:0.0}s";
            }
        }
        else
        {
            if (jActionLabel != null)
            {
                jActionLabel.text = "TÁT / BẮT";
                jActionLabel.color = Color.white;
            }
            if (jCooldownFill != null) jCooldownFill.gameObject.SetActive(false);
            if (jCooldownText != null) jCooldownText.gameObject.SetActive(false);

            if (_wasCatStunned)
            {
                _wasCatStunned = false;
                if (jSkillTransform != null)
                {
                    jSkillTransform.DOKill();
                    jSkillTransform.DOPunchScale(Vector3.one * 0.28f, 0.35f, 8, 0.8f);
                }
            }
        }
    }

    /// <summary>
    /// Cập nhật Radar cảnh báo Mèo đến gần (Proximity Heartbeat Indicator):
    /// Khi Mèo ở cự ly <= 4.0m, góc màn hình nhấp nháy viền đỏ cảnh báo. Càng gần nhịp đập càng dồn dập.
    /// </summary>
    public void UpdateProximity(float distanceToCat)
    {
        if (distanceToCat <= 4.0f && distanceToCat > 0f)
        {
            if (!_isProximityActive)
            {
                _isProximityActive = true;
                if (proximityVignetteOverlay != null)
                {
                    proximityVignetteOverlay.gameObject.SetActive(true);
                }
                if (proximityWarningBadge != null)
                {
                    proximityWarningBadge.gameObject.SetActive(true);
                    proximityWarningBadge.DOKill();
                    proximityWarningBadge.localScale = Vector3.one * 0.7f;
                    proximityWarningBadge.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
                }
            }

            // Tần số nhấp nháy dồn dập theo cự ly: < 2.0m đập cực nhanh 0.18s
            float pulseDuration = (distanceToCat <= 2.0f) ? 0.18f : 0.35f;

            if (_proximityPulseTween == null || !_proximityPulseTween.IsActive())
            {
                if (proximityVignetteOverlay != null)
                {
                    proximityVignetteOverlay.DOKill();
                    proximityVignetteOverlay.color = new Color(1f, 1f, 1f, 0.3f);
                    _proximityPulseTween = proximityVignetteOverlay.DOFade(0.85f, pulseDuration)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
                }
            }

            if (proximityWarningText != null)
            {
                proximityWarningText.text = distanceToCat <= 2.0f
                    ? "⚠️ [!] NGUY HIỂM: MÈO NGAY BÊN CẠNH!"
                    : $"⚠️ [!] MÈO ĐANG ĐẾN GẦN! ({distanceToCat:0.0}m)";
            }
        }
        else
        {
            if (_isProximityActive)
            {
                _isProximityActive = false;
                _proximityPulseTween?.Kill();
                _proximityPulseTween = null;

                if (proximityVignetteOverlay != null)
                {
                    proximityVignetteOverlay.DOKill();
                    proximityVignetteOverlay.DOFade(0f, 0.3f).OnComplete(() =>
                    {
                        if (!_isProximityActive && proximityVignetteOverlay != null)
                        {
                            proximityVignetteOverlay.gameObject.SetActive(false);
                        }
                    });
                }

                if (proximityWarningBadge != null)
                {
                    proximityWarningBadge.gameObject.SetActive(false);
                }
            }
        }
    }
}
