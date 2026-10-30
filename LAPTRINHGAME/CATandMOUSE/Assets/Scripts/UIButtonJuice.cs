using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Hiệu ứng nhấn nút đồ chơi 3D (Tactile Chunky Button Juice) phong cách Among Us / Brawl Stars.
/// - PointerEnter: Nút nảy to 1.08x.
/// - PointerDown: Nút thụt lún 5 pixel theo trục Y (tạo cảm giác nhấn lún phím 3D thật sự).
/// - PointerUp: Nút bật nảy lò xo trở lại vị trí cũ bằng DOTween Ease.OutBounce.
/// </summary>
public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Tactile Settings")]
    [SerializeField] private float hoverScaleFactor = 1.08f;
    [SerializeField] private float pressScaleFactor = 0.96f;
    [SerializeField] private float pressYOffset = -5f; // Thụt lún 5px theo trục Y
    [SerializeField] private float tweenDuration = 0.12f;
    [SerializeField] private float bounceDuration = 0.32f; // Thời gian bật lò xo nảy đàn hồi

    private RectTransform _rectTransform;
    private Vector3 _originalScale;
    private Vector2 _originalAnchoredPos;
    private bool _isHovered = false;
    private bool _isInitialized = false;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (_isInitialized) return;
        _rectTransform = GetComponent<RectTransform>();
        _originalScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        if (_rectTransform != null)
        {
            _originalAnchoredPos = _rectTransform.anchoredPosition;
        }
        _isInitialized = true;
    }

    private void OnEnable()
    {
        InitializeIfNeeded();
        KillAllTweens();
        transform.localScale = _originalScale;
        if (_rectTransform != null) _rectTransform.anchoredPosition = _originalAnchoredPos;
        _isHovered = false;
    }

    private void OnDisable()
    {
        KillAllTweens();
        transform.localScale = _originalScale;
        if (_rectTransform != null) _rectTransform.anchoredPosition = _originalAnchoredPos;
        _isHovered = false;
    }

    private void KillAllTweens()
    {
        transform.DOKill();
        if (_rectTransform != null) _rectTransform.DOKill();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        KillAllTweens();
        transform.DOScale(_originalScale * hoverScaleFactor, tweenDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        KillAllTweens();
        transform.DOScale(_originalScale, tweenDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        if (_rectTransform != null)
        {
            _rectTransform.DOAnchorPos(_originalAnchoredPos, tweenDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        KillAllTweens();

        // 1. Co lại nhẹ
        transform.DOScale(_originalScale * pressScaleFactor, tweenDuration * 0.7f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        // 2. Thụt lún 5 pixel xuống dưới theo trục Y
        if (_rectTransform != null)
        {
            Vector2 pressedPos = _originalAnchoredPos + new Vector2(0f, pressYOffset);
            _rectTransform.DOAnchorPos(pressedPos, tweenDuration * 0.7f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        KillAllTweens();

        // 1. Nảy trở lại kích thước mục tiêu với hiệu ứng đàn hồi Ease.OutBounce
        Vector3 targetScale = _isHovered ? (_originalScale * hoverScaleFactor) : _originalScale;
        transform.DOScale(targetScale, bounceDuration)
            .SetEase(Ease.OutBounce)
            .SetUpdate(true);

        // 2. Bật nảy lò xo trở lại vị trí Y ban đầu
        if (_rectTransform != null)
        {
            _rectTransform.DOAnchorPos(_originalAnchoredPos, bounceDuration)
                .SetEase(Ease.OutBounce)
                .SetUpdate(true);
        }
    }
}
