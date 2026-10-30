using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

/// <summary>
/// Điều khiển nhân vật Chuột: di chuyển 2D bằng phím WASD, quét đồ vật lân cận,
/// sao chép hình dáng (cải trang) và giải trừ ngụy trang bằng phím Space,
/// chuẩn hóa kích thước cố định theo mét (Unity Units) chống biến dạng tuyệt đối,
/// quản lý hiệu ứng nảy bằng DOTween và Cooldown bằng UniTask.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class MouseController : MonoBehaviour
{
    [Header("Standardized Unit Dimensions")]
    [SerializeField, Tooltip("Chiều rộng hiển thị cố định của Chuột theo đơn vị thế giới (Unity Units)")]
    private float targetMouseWidth = 0.6f;

    [SerializeField, Tooltip("Bán kính CircleCollider2D chuẩn ôm sát thân chuột")]
    private float targetMouseColliderRadius = 0.2f;

    [Header("Movement Settings (WASD)")]
    [SerializeField, Tooltip("Tốc độ di chuyển của Chuột")]
    private float moveSpeed = 5f;

    [Header("Disguise Settings")]
    [SerializeField, Tooltip("Thời gian chờ (giây) giữa các lần biến hình / giải trừ")]
    private float disguiseCooldown = 2.5f;

    [SerializeField, Tooltip("Bán kính quét tìm đồ vật xung quanh để sao chép")]
    private float interactionRadius = 2.0f;

    [SerializeField, Tooltip("Layer chứa các đồ vật (Prop) môi trường")]
    private LayerMask propLayerMask = ~0;

    [Header("Transform Effects (DOTween)")]
    [SerializeField, Tooltip("Cường độ nảy scale khi biến hình hoặc giải trừ")]
    private float transformPunchScale = 0.25f;

    [SerializeField, Tooltip("Thời gian hiệu ứng nảy")]
    private float transformPunchDuration = 0.25f;

    [Header("State (Read Only)")]
    [SerializeField, Tooltip("Chuột đang ngụy trang thành đồ vật hay không")]
    private bool isDisguised = false;

    [SerializeField, Tooltip("Đang trong thời gian chờ hồi chiêu biến hình")]
    private bool isCooldown = false;

    [SerializeField, Tooltip("Chuột đã bị Mèo bắt")]
    private bool isCaught = false;

    [Header("Control Mode")]
    [Tooltip("Điều khiển từ xa qua mạng LAN/Online")]
    public bool isNetworkRemote = false;

    #region Public Properties
    public bool IsDisguised => isDisguised;
    public bool IsCooldown => isCooldown;
    public bool IsCaught => isCaught;
    public float MoveSpeed => moveSpeed;
    public float DisguiseCooldown => disguiseCooldown;
    public float CooldownRemaining => _cooldownRemaining;
    public float TargetMouseWidth => targetMouseWidth;
    public float TargetMouseColliderRadius => targetMouseColliderRadius;
    public Vector3 OriginalScale => originalScale;
    public string DisguisedPropId => isDisguised && _attachedPropItem != null ? _attachedPropItem.PropId : "";
    public SpriteRenderer SpriteRenderer => _spriteRenderer;
    public Vector2 MoveInput => _moveInput;
    #endregion

    private Rigidbody2D _rigidbody2D;
    private BoxCollider2D _boxCollider;
    private CircleCollider2D _circleCollider;
    private SpriteRenderer _spriteRenderer;
    private PropItem _attachedPropItem;

    [Header("Original Mouse State")]
    private Vector3 originalScale = new Vector3(0.25f, 0.25f, 1f);
    private Sprite originalSprite;
    private Color originalColor = Color.white;

    private Vector2 _moveInput;
    private Tween _scaleTween;
    private float _cooldownRemaining = 0f;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _circleCollider = GetComponent<CircleCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // 1. Lưu trữ hình ảnh và màu sắc nguyên bản
        if (_spriteRenderer != null)
        {
            originalSprite = _spriteRenderer.sprite;
            originalColor = _spriteRenderer.color;
        }

        // 2. Tự đính kèm PropItem để phục vụ nhận diện đồ giả khi biến hình
        if (!TryGetComponent<PropItem>(out _attachedPropItem))
        {
            _attachedPropItem = gameObject.AddComponent<PropItem>();
        }
        _attachedPropItem.IsFake = false;

        // 3. Cấu hình Rigidbody 2D tối ưu góc nhìn Top-down
        if (_rigidbody2D != null)
        {
            _rigidbody2D.gravityScale = 0f;
            _rigidbody2D.freezeRotation = true;
            _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // 4. Đo đạc kích thước thực tế và khóa cứng kích thước hiển thị mục tiêu: Chiều rộng = 0.6 units
        ApplyNormalizedMouseSize();
    }

    private void Start()
    {
        // Tái đảm bảo kích thước chuẩn xác 100% sau khi Scene đã tải xong
        ApplyNormalizedMouseSize();
    }

    /// <summary>
    /// Đo đạc kích thước thực tế của Sprite Chuột bằng spriteRenderer.bounds.size
    /// và khóa cứng kích thước hiển thị mục tiêu: Chiều rộng Chuột = 0.6 đơn vị (Units),
    /// tự động gán CircleCollider2D.radius = 0.2f ôm sát thân chuột.
    /// </summary>
    public void ApplyNormalizedMouseSize()
    {
        if (isDisguised) return;

        transform.DOKill();

        if (_spriteRenderer != null && _spriteRenderer.sprite != null)
        {
            // Đo đạc chiều rộng thực tế unscaled của sprite Chuột trong hệ tọa độ Unity Units
            float rawSpriteWidth = _spriteRenderer.sprite.bounds.size.x;
            if (rawSpriteWidth > 0.001f)
            {
                float calculatedScale = targetMouseWidth / rawSpriteWidth;
                originalScale = new Vector3(calculatedScale, calculatedScale, 1f);
            }
            else
            {
                originalScale = new Vector3(0.25f, 0.25f, 1f);
            }
        }
        else
        {
            originalScale = new Vector3(0.25f, 0.25f, 1f);
        }

        transform.localScale = originalScale;

        // Tự động gán lại CircleCollider2D.radius = 0.2f ôm sát thân chuột
        if (_circleCollider != null)
        {
            _circleCollider.radius = targetMouseColliderRadius;
            _circleCollider.offset = Vector2.zero;
        }
        if (_boxCollider != null)
        {
            _boxCollider.size = new Vector2(targetMouseWidth, targetMouseWidth);
            _boxCollider.offset = Vector2.zero;
        }
    }

    private void Update()
    {
        if (isCaught) return;

        // Không di chuyển nếu game chưa bắt đầu hoặc đã kết thúc
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            _moveInput = Vector2.zero;
            return;
        }

        // Chỉ đọc bàn phím nếu không phải do mạng điều khiển từ xa
        if (!isNetworkRemote)
        {
            // Đọc điều khiển di chuyển riêng biệt bằng phím WASD
            HandleKeyboardMovement();

            // Bấm phím Space để kích hoạt hoặc giải trừ ngụy trang
            if (Input.GetKeyDown(KeyCode.Space))
            {
                HandleDisguiseTrigger();
            }
        }
    }

    /// <summary>
    /// Cho phép Network thiết lập vector di chuyển cho Chuột.
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        _moveInput = input;
        if (!isDisguised && _spriteRenderer != null && Mathf.Abs(input.x) > 0.05f)
        {
            _spriteRenderer.flipX = input.x < 0f;
        }
    }

    /// <summary>
    /// Cho phép Network kích hoạt hành động biến hình / giải trừ từ xa.
    /// </summary>
    public void TriggerDisguiseAction()
    {
        HandleDisguiseTrigger();
    }

    private void FixedUpdate()
    {
        if (isCaught || (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing))
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
            return;
        }

        // Áp dụng vận tốc di chuyển vật lý
        if (_rigidbody2D != null)
        {
            _rigidbody2D.linearVelocity = _moveInput.normalized * moveSpeed;
        }
    }

    /// <summary>
    /// Đọc phím WASD để điều khiển Chuột (tách biệt hoàn toàn với phím Mũi tên của Mèo).
    /// </summary>
    private void HandleKeyboardMovement()
    {
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.D)) h += 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;

        _moveInput = new Vector2(h, v);

        // Lật mặt Sprite theo hướng trái / phải nếu đang ở hình dạng Chuột
        if (!isDisguised && _spriteRenderer != null && Mathf.Abs(h) > 0.05f)
        {
            _spriteRenderer.flipX = h < 0f;
        }
    }

    /// <summary>
    /// Xử lý logic khi người chơi bấm Space để ngụy trang hoặc giải trừ ngụy trang.
    /// </summary>
    private void HandleDisguiseTrigger()
    {
        if (isCooldown || isCaught) return;

        if (!isDisguised)
        {
            // Quét tìm đồ vật thật gần nhất trong bán kính quét
            PropItem nearestProp = FindNearestRealProp();
            if (nearestProp != null)
            {
                DisguiseIntoProp(nearestProp);
            }
        }
        else
        {
            // Nếu đang ngụy trang -> giải trừ trở về dạng Chuột gốc
            RevertToMouseForm();
        }
    }

    /// <summary>
    /// Quét các collider xung quanh tìm đồ vật thật (isFake == false) gần nhất.
    /// </summary>
    private PropItem FindNearestRealProp()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius, propLayerMask);
        PropItem closestProp = null;
        float minDistanceSqr = float.MaxValue;
        Vector2 currentPosition = transform.position;

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            if (hit.TryGetComponent<PropItem>(out var prop))
            {
                if (prop.IsFake) continue; // Bỏ qua nếu là đồ vật giả

                float distSqr = ((Vector2)prop.transform.position - currentPosition).sqrMagnitude;
                if (distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    closestProp = prop;
                }
            }
        }

        return closestProp;
    }

    /// <summary>
    /// Sao chép ngoại hình, màu sắc và tự động tính toán scale theo kích thước chuẩn của đồ vật (0.8 - 1.2 units).
    /// </summary>
    public void DisguiseIntoProp(PropItem targetProp)
    {
        isDisguised = true;

        // BẮT BUỘC gọi DOKill() trước khi thay đổi scale để tránh bị cộng dồn kích thước
        transform.DOKill();

        // 1. Cập nhật Sprite và màu sắc giống đồ vật
        Sprite propSprite = targetProp.PropSprite != null ? targetProp.PropSprite : targetProp.GetComponent<SpriteRenderer>()?.sprite;
        if (_spriteRenderer != null && propSprite != null)
        {
            _spriteRenderer.sprite = propSprite;
            _spriteRenderer.color = targetProp.PropColor;
        }

        // 2. Tính toán tỷ lệ scale sao cho Sprite đồ vật hiển thị đúng kích cỡ của đồ vật đó (khoảng 0.8 - 1.2 units)
        float targetPropUnitSize = targetProp.TargetUnitSize > 0.1f ? targetProp.TargetUnitSize : 1.0f;
        Vector3 targetScale;

        if (propSprite != null)
        {
            float maxSpriteDim = Mathf.Max(propSprite.bounds.size.x, propSprite.bounds.size.y);
            if (maxSpriteDim > 0.001f)
            {
                float calculatedPropScale = targetPropUnitSize / maxSpriteDim;
                targetScale = new Vector3(calculatedPropScale, calculatedPropScale, 1f);
            }
            else
            {
                targetScale = targetProp.transform.localScale;
            }
        }
        else
        {
            targetScale = targetProp.transform.localScale;
        }

        // Khóa cứng scale theo kích thước chuẩn mục tiêu
        transform.localScale = targetScale;

        // 3. Cập nhật Collider tương ứng với đồ vật
        if (_circleCollider != null)
        {
            _circleCollider.radius = (targetPropUnitSize * 0.5f) / Mathf.Max(targetScale.x, 0.001f);
            _circleCollider.offset = Vector2.zero;
        }
        if (_boxCollider != null)
        {
            _boxCollider.size = targetProp.CustomColliderSize;
            _boxCollider.offset = Vector2.zero;
        }

        // 4. Đánh dấu component PropItem trên mình là đồ giả để Mèo nhận diện
        if (_attachedPropItem != null)
        {
            _attachedPropItem.IsFake = true;
            _attachedPropItem.targetUnitSize = targetPropUnitSize;
        }

        // 5. Hiệu ứng nảy scale biến hình qua DOTween (DOPunchScale):
        // Luôn kill trước khi chạy và khóa cứng lại targetScale ở OnComplete
        transform.DOKill();
        _scaleTween = transform.DOPunchScale(targetScale * transformPunchScale, transformPunchDuration, vibrato: 7, elasticity: 0.8f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (isDisguised)
                {
                    transform.localScale = targetScale;
                }
            });

        // 6. Bắt đầu thời gian hồi chiêu
        StartCooldownAsync().Forget();
    }

    /// <summary>
    /// Khôi phục chính xác 100% kích thước ban đầu (0.6 units) và Collider (radius 0.2f),
    /// khóa chặt không cho scale bị cộng dồn hay phình to.
    /// </summary>
    public void RevertToMouseForm()
    {
        isDisguised = false;

        // BẮT BUỘC gọi DOKill() trước khi hoàn nguyên
        transform.DOKill();

        // 1. Khôi phục Sprite và màu sắc gốc của Chuột
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = originalSprite;
            _spriteRenderer.color = originalColor;
        }

        // 2. Phục hồi chính xác 100% kích thước ban đầu (0.6 units)
        transform.localScale = originalScale;

        // 3. Tự động gán lại CircleCollider2D.radius = 0.2f ôm sát thân chuột
        if (_circleCollider != null)
        {
            _circleCollider.radius = targetMouseColliderRadius;
            _circleCollider.offset = Vector2.zero;
        }
        if (_boxCollider != null)
        {
            _boxCollider.size = new Vector2(targetMouseWidth, targetMouseWidth);
            _boxCollider.offset = Vector2.zero;
        }

        // 4. Đánh dấu không còn là đồ giả
        if (_attachedPropItem != null)
        {
            _attachedPropItem.IsFake = false;
        }

        // 5. Hiệu ứng nảy scale hoàn nguyên qua DOTween:
        // Punch trên originalScale và khóa cứng lại transform.localScale = originalScale
        transform.DOKill();
        _scaleTween = transform.DOPunchScale(originalScale * transformPunchScale, transformPunchDuration, vibrato: 7, elasticity: 0.8f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (!isDisguised)
                {
                    transform.localScale = originalScale;
                    if (_circleCollider != null)
                    {
                        _circleCollider.radius = targetMouseColliderRadius;
                    }
                }
            });

        // 6. Bắt đầu thời gian hồi chiêu
        StartCooldownAsync().Forget();
    }

    /// <summary>
    /// Đếm ngược thời gian hồi chiêu bằng UniTask có truyền CancellationToken triệt để.
    /// </summary>
    private async UniTaskVoid StartCooldownAsync()
    {
        isCooldown = true;
        _cooldownRemaining = disguiseCooldown;

        try
        {
            while (_cooldownRemaining > 0f)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: this.GetCancellationTokenOnDestroy());
                _cooldownRemaining -= Time.deltaTime;
            }
            _cooldownRemaining = 0f;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        isCooldown = false;
    }

    /// <summary>
    /// Gọi khi Mèo phát hiện và tát trúng Chuột.
    /// Dừng di chuyển và kích hoạt kết thúc ván đấu.
    /// </summary>
    public void CaughtByCat()
    {
        if (isCaught) return;

        isCaught = true;
        if (_rigidbody2D != null) _rigidbody2D.linearVelocity = Vector2.zero;
        if (_boxCollider != null) _boxCollider.enabled = false;
        if (_circleCollider != null) _circleCollider.enabled = false;

        // Dừng mọi tween và thu nhỏ Chuột tỷ lệ thuận với kích thước hiện tại
        transform.DOKill();
        Vector3 baseScale = isDisguised ? transform.localScale : originalScale;
        _scaleTween = transform.DOScale(baseScale * 0.6f, 0.2f).SetEase(Ease.InBack);

        // Báo cho GameManager rằng Mèo đã thắng
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerCatWin();
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }

    private void OnDrawGizmosSelected()
    {
        // Hiển thị bán kính quét đồ vật trong Scene View
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
