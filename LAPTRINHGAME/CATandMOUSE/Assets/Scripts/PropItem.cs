using UnityEngine;
using DG.Tweening;

/// <summary>
/// Quản lý thông tin và trạng thái của đồ vật trên bản đồ (Prop).
/// Gắn vào các GameObject đồ vật môi trường (bàn, ghế, chậu cây,...)
/// hoặc đối tượng giả lập để Chuột sao chép ngụy trang.
/// </summary>
[SelectionBase]
[DisallowMultipleComponent]
public class PropItem : MonoBehaviour
{
    [Header("Prop Identity & Configuration")]
    [Tooltip("Mã định danh của loại đồ vật này (ví dụ: Prop_Chair, Prop_Box)")]
    public string propId = "Prop_Default";

    [Tooltip("Sprite hình ảnh hiển thị của đồ vật")]
    public Sprite propSprite;

    [Tooltip("Màu sắc của đồ vật (dùng khi sao chép ngụy trang)")]
    public Color propColor = Color.white;

    [Tooltip("Kích thước BoxCollider2D tương ứng khi Chuột biến thành đồ vật này")]
    public Vector2 customColliderSize = Vector2.one;

    [Header("Prop State")]
    [Tooltip("Đánh dấu đây là đồ thật của môi trường (false) hay là Chuột đang ngụy trang (true)")]
    public bool isFake = false;

    [Header("Feedback Settings (DOTween)")]
    [SerializeField, Tooltip("Cường độ nảy scale khi bị Mèo tát trúng")]
    private float punchScaleIntensity = 0.25f;

    [SerializeField, Tooltip("Thời gian diễn ra hiệu ứng nảy")]
    private float punchDuration = 0.25f;

    [Header("Standardized Unit Sizing")]
    [Tooltip("Kích thước hiển thị mục tiêu theo mét / Unity Units (ví dụ: 1.1 cho thùng hàng, 0.7 cho cọc tiêu, 0.9 cho thùng phuy)")]
    public float targetUnitSize = 1.0f;

    #region Public Properties
    public string PropId { get => propId; set => propId = value; }
    public Sprite PropSprite { get => propSprite; set => propSprite = value; }
    public Color PropColor { get => propColor; set => propColor = value; }
    public Vector2 CustomColliderSize { get => customColliderSize; set => customColliderSize = value; }
    public float TargetUnitSize { get => targetUnitSize; set => targetUnitSize = value; }

    public bool IsFake
    {
        get => isFake;
        set => isFake = value;
    }
    #endregion

    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _boxCollider;
    private Tween _punchTween;
    private Vector3 _baseScale;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _baseScale = transform.localScale;

        // Tự động gán Sprite và Color nếu chưa thiết lập trong Inspector
        if (_spriteRenderer != null)
        {
            if (propSprite == null) propSprite = _spriteRenderer.sprite;
            if (propColor == Color.white && _spriteRenderer.color != Color.white) propColor = _spriteRenderer.color;
        }

        // Tự động lấy kích thước collider nếu chưa thiết lập
        if (customColliderSize == Vector2.zero && _boxCollider != null)
        {
            customColliderSize = _boxCollider.size;
        }
    }

    /// <summary>
    /// Kích hoạt hiệu ứng nảy nhẹ khi Mèo tát trúng đồ vật này.
    /// Dùng DOTween để tạo phản hồi xúc giác cho người chơi.
    /// </summary>
    public void OnHitByCat()
    {
        transform.DOKill();
        Vector3 baseScale = _baseScale != Vector3.zero ? _baseScale : transform.localScale;
        transform.localScale = baseScale;

        _punchTween = transform.DOPunchScale(baseScale * punchScaleIntensity, punchDuration, vibrato: 8, elasticity: 1f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.localScale = baseScale;
            });
    }

    public void SetBaseScale(Vector3 scale)
    {
        _baseScale = scale;
        transform.localScale = scale;
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        if (TryGetComponent<SpriteRenderer>(out var sr))
        {
            propSprite = sr.sprite;
            propColor = sr.color;
            propId = gameObject.name;
        }

        if (TryGetComponent<BoxCollider2D>(out var col))
        {
            customColliderSize = col.size;
        }
    }
#endif
}
