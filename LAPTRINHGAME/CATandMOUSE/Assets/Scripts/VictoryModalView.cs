using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal thông báo chiến thắng / thất bại cao cấp phong cách Cyber-Casual 2026.
/// Có hiệu ứng làm mờ nền (Dimmer), Banner nảy từ trên xuống qua DOTween (Ease.OutBack),
/// và các nút bấm bo tròn 3D tương tác nảy nhẹ.
/// </summary>
public class VictoryModalView : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Image dimmerImage;
    [SerializeField] private RectTransform modalContainer;
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private Text matchStatsText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;

    private Action _onPlayAgainCallback;
    private Action _onMainMenuCallback;

    private void Awake()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(() =>
            {
                Hide();
                _onPlayAgainCallback?.Invoke();
            });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(() =>
            {
                Hide();
                _onMainMenuCallback?.Invoke();
            });
        }
    }

    /// <summary>
    /// Hiển thị Modal chiến thắng với hiệu ứng hoạt họa cao cấp
    /// </summary>
    public void Show(bool isCatWon, float survivedSeconds, Action onPlayAgain, Action onMainMenu)
    {
        _onPlayAgainCallback = onPlayAgain;
        _onMainMenuCallback = onMainMenu;

        gameObject.SetActive(true);

        // 1. Cấu hình nội dung theo phe chiến thắng
        if (isCatWon)
        {
            if (titleText != null)
            {
                titleText.text = "👑 CATS VICTORY!";
                titleText.color = new Color(0.99f, 0.83f, 0.30f, 1f); // Vàng kim #FCD34D
            }
            if (subtitleText != null)
            {
                subtitleText.text = "Mèo đã tóm gọn toàn bộ đàn chuột!";
                subtitleText.color = new Color(0.89f, 0.91f, 0.94f, 1f);
            }
            if (matchStatsText != null)
            {
                int m = Mathf.FloorToInt(survivedSeconds / 60f);
                int s = Mathf.FloorToInt(survivedSeconds % 60f);
                matchStatsText.text = $"⏱ Thời gian truy bắt: {m:00}:{s:00}  |  Chuột đã bị bắt!";
            }
        }
        else
        {
            if (titleText != null)
            {
                titleText.text = "🧀 MICE ESCAPED!";
                titleText.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Cyber Cyan #38BDF8
            }
            if (subtitleText != null)
            {
                subtitleText.text = "Chuột đã ngụy trang sinh tồn thành công!";
                subtitleText.color = new Color(0.89f, 0.91f, 0.94f, 1f);
            }
            if (matchStatsText != null)
            {
                matchStatsText.text = "⏱ Sống sót trọn vẹn 300 giây (5 phút) mà không bị phát hiện!";
            }
        }

        // 2. Hiệu ứng làm mờ nền (Dimmer Fade)
        if (dimmerImage != null)
        {
            dimmerImage.DOKill();
            dimmerImage.color = new Color(0f, 0f, 0f, 0f);
            dimmerImage.DOFade(0.75f, 0.35f).SetUpdate(true);
        }

        // 3. Hiệu ứng Banner trượt và phóng to với Ease.OutBack
        if (modalContainer != null)
        {
            modalContainer.DOKill();
            modalContainer.localScale = Vector3.one * 0.5f;
            modalContainer.anchoredPosition = new Vector2(0f, 80f);

            modalContainer.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack).SetUpdate(true);
            modalContainer.DOAnchorPos(Vector2.zero, 0.45f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void Hide()
    {
        if (modalContainer != null)
        {
            modalContainer.DOKill();
            modalContainer.DOScale(0.8f, 0.2f).SetEase(Ease.InQuad).SetUpdate(true);
        }

        if (dimmerImage != null)
        {
            dimmerImage.DOKill();
            dimmerImage.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
