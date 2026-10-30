using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

/// <summary>
/// Điều khiển nhân vật Mèo: di chuyển bằng phím Mũi tên (Arrow keys),
/// tấn công tát/bắt bằng phím J hoặc Click chuột trái.
/// Bị phạt Choáng (Stun) 1.5s + rung sprite khi đánh nhầm đồ thật,
/// bắt trúng Chuột thì giành chiến thắng.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class CatController : MonoBehaviour
{
    [Header("Movement Settings (Arrow Keys)")]
    [SerializeField, Tooltip("Tốc độ di chuyển của Mèo (nhanh hơn Chuột)")]
    private float moveSpeed = 6f;

    [Header("Attack Settings (J / Left Click)")]
    [SerializeField, Tooltip("Bán kính vùng quét đòn tát / bắt")]
    private float attackRadius = 1.3f;

    [SerializeField, Tooltip("Điểm phát sinh đòn tấn công trước mặt Mèo")]
    private Transform attackPoint;

    [SerializeField, Tooltip("Khoảng cách lệch trước mặt nếu không gán attackPoint riêng")]
    private float attackPointOffset = 0.8f;

    [SerializeField, Tooltip("LayerMask quét Chuột và các đồ vật trên bản đồ")]
    private LayerMask attackLayerMask = ~0;

    [SerializeField, Tooltip("Thời gian hồi giữa các đòn tát thường (giây)")]
    private float attackCooldown = 0.35f;

    [Header("Stun Penalty Settings (DOTween & UniTask)")]
    [SerializeField, Tooltip("Thời gian Mèo bị choáng khi tát nhầm đồ vật thật (giây)")]
    private float stunDuration = 1.5f;

    [SerializeField, Tooltip("Cường độ rung Sprite khi bị choáng")]
    private float shakeStrength = 0.3f;

    [SerializeField, Tooltip("Thời gian hiệu ứng rung khi bị choáng")]
    private float shakeDuration = 0.3f;

    [Header("State (Read Only)")]
    [SerializeField, Tooltip("Mèo đang trong trạng thái bị choáng hay không")]
    private bool isStunned = false;

    [SerializeField, Tooltip("Mèo đang trong quá trình thực hiện đòn tấn công")]
    private bool isAttacking = false;

    [Header("Control Mode")]
    [Tooltip("Điều khiển bởi AI Bot")]
    public bool isAIControlled = false;

    [Tooltip("Điều khiển từ xa qua mạng LAN/Online")]
    public bool isNetworkRemote = false;

    #region Public Properties
    public bool IsStunned => isStunned;
    public bool IsAttacking => isAttacking;
    public float MoveSpeed => moveSpeed;
    public float StunDuration => stunDuration;
    public float AttackCooldown => attackCooldown;
    public float StunRemaining => _stunRemaining;
    public float AttackCooldownRemaining => _attackCooldownRemaining;
    public Vector2 FacingDirection => _facingDirection;
    public SpriteRenderer SpriteRenderer => _spriteRenderer;
    #endregion

    private Rigidbody2D _rigidbody2D;
    private SpriteRenderer _spriteRenderer;
    private Vector2 _moveInput;
    private Vector2 _facingDirection = Vector2.right;

    private float _stunRemaining = 0f;
    private float _attackCooldownRemaining = 0f;

    private Tween _shakeTween;
    private Tween _attackPunchTween;
    private Color _originalColor;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        _rigidbody2D.gravityScale = 0f;
        _rigidbody2D.freezeRotation = true;
        _rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        _originalColor = _spriteRenderer.color;

        // Nếu chưa gán attackPoint thì tự tạo 1 child transform phía trước
        if (attackPoint == null)
        {
            GameObject pointObj = new GameObject("AutoAttackPoint");
            pointObj.transform.SetParent(transform);
            pointObj.transform.localPosition = new Vector3(attackPointOffset, 0f, 0f);
            attackPoint = pointObj.transform;
        }
    }

    private void Update()
    {
        // Kiểm tra trạng thái trận đấu
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            _moveInput = Vector2.zero;
            return;
        }

        if (isStunned)
        {
            _moveInput = Vector2.zero;
            return;
        }

        // Chỉ đọc bàn phím nếu không phải do AI hoặc Remote điều khiển
        if (!isAIControlled && !isNetworkRemote)
        {
            // Đọc phím điều khiển di chuyển Mũi tên (Arrow Keys)
            HandleArrowKeysMovement();

            // Tấn công bằng phím J hoặc Click chuột trái
            if ((Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)) && !isAttacking)
            {
                PerformAttackAsync().Forget();
            }
        }
    }

    /// <summary>
    /// Cho phép AI Bot hoặc Network thiết lập vector di chuyển cho Mèo.
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        _moveInput = input;
        if (_moveInput.sqrMagnitude > 0.01f)
        {
            _facingDirection = _moveInput.normalized;
            if (Mathf.Abs(input.x) > 0.05f && _spriteRenderer != null)
            {
                _spriteRenderer.flipX = input.x < 0f;
            }
            if (attackPoint != null)
            {
                attackPoint.localPosition = (Vector3)_facingDirection * attackPointOffset;
            }
        }
    }

    /// <summary>
    /// Cho phép AI Bot hoặc Network kích hoạt đòn tấn công cho Mèo.
    /// </summary>
    public void TriggerAttack()
    {
        if (!isAttacking && !isStunned)
        {
            PerformAttackAsync().Forget();
        }
    }

    /// <summary>
    /// Cho phép đồng bộ hướng nhìn của Mèo từ máy Host/Client.
    /// </summary>
    public void SetFacingDirection(Vector2 direction)
    {
        _facingDirection = direction;
        if (Mathf.Abs(direction.x) > 0.05f && _spriteRenderer != null)
        {
            _spriteRenderer.flipX = direction.x < 0f;
        }
        if (attackPoint != null)
        {
            attackPoint.localPosition = (Vector3)_facingDirection * attackPointOffset;
        }
    }

    private void FixedUpdate()
    {
        if (isStunned || (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing))
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
            return;
        }

        _rigidbody2D.linearVelocity = _moveInput.normalized * moveSpeed;
    }

    /// <summary>
    /// Điều khiển Mèo bằng 4 phím Mũi tên (Up, Down, Left, Right).
    /// Tách biệt hoàn toàn với phím WASD của Chuột.
    /// </summary>
    private void HandleArrowKeysMovement()
    {
        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.UpArrow)) v += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) v -= 1f;

        _moveInput = new Vector2(h, v);

        if (_moveInput.sqrMagnitude > 0.01f)
        {
            _facingDirection = _moveInput.normalized;

            // Lật mặt Sprite theo hướng nhìn trái / phải
            if (Mathf.Abs(h) > 0.05f)
            {
                _spriteRenderer.flipX = h < 0f;
            }

            // Cập nhật vị trí attackPoint theo hướng nhìn
            if (attackPoint != null)
            {
                attackPoint.localPosition = (Vector3)_facingDirection * attackPointOffset;
            }
        }
    }

    /// <summary>
    /// Thực hiện đòn tát: quét đối tượng trong vùng attackRadius,
    /// phân định bắt trúng Chuột (Thắng) hay tát nhầm đồ thật (Phạt Choáng 1.5s).
    /// </summary>
    private async UniTaskVoid PerformAttackAsync()
    {
        isAttacking = true;

        Vector2 attackOrigin = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;

        // Hiệu ứng nảy scale vung tay đòn bằng DOTween
        _attackPunchTween?.Kill();
        _attackPunchTween = transform.DOPunchScale(Vector3.one * 0.18f, 0.15f, vibrato: 6, elasticity: 0.6f);

        // Quét các đối tượng trong phạm vi tát
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackOrigin, attackRadius, attackLayerMask);

        MouseController caughtMouse = null;
        PropItem hitRealProp = null;

        foreach (var col in hitColliders)
        {
            if (col.gameObject == gameObject) continue;

            // Kiểm tra xem có trúng Chuột không (cả dạng nguyên bản lẫn dạng cải trang)
            if (col.TryGetComponent<MouseController>(out var mouse) || (col.transform.parent != null && col.transform.parent.TryGetComponent<MouseController>(out mouse)))
            {
                caughtMouse = mouse;
                break;
            }

            // Kiểm tra nếu va vào PropItem
            if (col.TryGetComponent<PropItem>(out var prop))
            {
                if (prop.IsFake)
                {
                    // Prop giả chính là Chuột đang ngụy trang
                    if (prop.TryGetComponent<MouseController>(out var fakeMouse))
                    {
                        caughtMouse = fakeMouse;
                        break;
                    }
                }
                else
                {
                    // Ghi nhận đồ thật bị tát trúng
                    hitRealProp = prop;
                }
            }
        }

        if (caughtMouse != null)
        {
            // === TRƯỜNG HỢP 1: BẮT TRÚNG CHUỘT (MÈO THẮNG) ===
            caughtMouse.CaughtByCat();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerCatWin();
            }
        }
        else if (hitRealProp != null)
        {
            // === TRƯỜNG HỢP 2: TÁT NHẦM ĐỒ THẬT (BỊ CHOÁNG 1.5S) ===
            hitRealProp.OnHitByCat();
            await ApplyStunPenaltyAsync();
        }
        else
        {
            // === TRƯỜNG HỢP 3: TÁT HỤT VÀO KHÔNG KHÍ ===
            _attackCooldownRemaining = attackCooldown;
            try
            {
                while (_attackCooldownRemaining > 0f)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: this.GetCancellationTokenOnDestroy());
                    _attackCooldownRemaining -= Time.deltaTime;
                }
                _attackCooldownRemaining = 0f;
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        isAttacking = false;
    }

    /// <summary>
    /// Xử lý hình phạt Choáng (Stun):
    /// Bất động trong stunDuration giây qua UniTask, rung Sprite bằng DOShakePosition.
    /// </summary>
    private async UniTask ApplyStunPenaltyAsync()
    {
        isStunned = true;
        _stunRemaining = stunDuration;
        _rigidbody2D.linearVelocity = Vector2.zero;

        // Đổi màu xám mờ để báo hiệu bị choáng
        _spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f, 1f);

        // Hiệu ứng rung Sprite bằng DOShakePosition theo yêu cầu
        _shakeTween?.Kill();
        _shakeTween = transform.DOShakePosition(shakeDuration, strength: shakeStrength, vibrato: 16, randomness: 90f);

        try
        {
            while (_stunRemaining > 0f)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: this.GetCancellationTokenOnDestroy());
                _stunRemaining -= Time.deltaTime;
            }
            _stunRemaining = 0f;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Hồi phục sau khi hết thời gian choáng
        _spriteRenderer.color = _originalColor;
        isStunned = false;
    }

    private void OnDestroy()
    {
        _shakeTween?.Kill();
        _attackPunchTween?.Kill();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 origin = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;
        Gizmos.DrawWireSphere(origin, attackRadius);
    }
}
