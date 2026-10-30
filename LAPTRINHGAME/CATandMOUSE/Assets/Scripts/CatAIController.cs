using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Trí tuệ nhân tạo (Bot AI) cho Mèo trong chế độ Chơi Đơn (Offline vs Bot):
/// Tự động tuần tra giữa 5 phân khu nhà kho (Sorting Area, Heavy Storage, Control Room, Office, Break Room),
/// phát hiện Chuột đang di chuyển hoặc nghi vấn đồ vật đứng im, truy đuổi và vồ bắt thông minh.
/// </summary>
[RequireComponent(typeof(CatController))]
[DisallowMultipleComponent]
public class CatAIController : MonoBehaviour
{
    public enum AIState
    {
        Patrol,         // Tuần tra định tuyến giữa các phòng
        Investigate,    // Đứng ngửi / soi xét đồ vật nghi vấn
        Chase,          // Phát hiện và truy đuổi Chuột
        AttackCooldown  // Tạm nghỉ sau cú vồ
    }

    [Header("AI Settings")]
    [SerializeField, Tooltip("Tốc độ tuần tra của Bot (chậm hơn lúc truy đuổi)")]
    private float patrolSpeed = 4.2f;

    [SerializeField, Tooltip("Tốc độ truy đuổi khi nhìn thấy Chuột")]
    private float chaseSpeed = 6.0f;

    [SerializeField, Tooltip("Bán kính phát hiện Chuột khi Chuột không ngụy trang")]
    private float visionRadiusNormal = 6.0f;

    [SerializeField, Tooltip("Bán kính phát hiện Chuột khi ngụy trang nhưng đang di chuyển")]
    private float visionRadiusMovingDisguise = 4.5f;

    [SerializeField, Tooltip("Bán kính nghi vấn khi Chuột ngụy trang đứng im như tượng")]
    private float visionRadiusStillDisguise = 1.6f;

    [SerializeField, Tooltip("Khoảng cách đủ gần để tung đòn tát bắt")]
    private float attackTriggerDistance = 1.15f;

    [Header("State (Read Only)")]
    [SerializeField]
    private AIState currentState = AIState.Patrol;
    public AIState CurrentState => currentState;

    private CatController _catController;
    private MouseController _targetMouse;
    private List<Vector2> _patrolWaypoints;
    private int _currentWaypointIndex = 0;
    private float _investigateTimer = 0f;
    private float _suspicionMeter = 0f;
    private Vector2 _currentMoveDirection = Vector2.zero;

    private void Awake()
    {
        _catController = GetComponent<CatController>();
        SetupWaypoints();
    }

    private void OnEnable()
    {
        if (_catController != null)
        {
            _catController.isAIControlled = true;
        }
    }

    private void OnDisable()
    {
        if (_catController != null)
        {
            _catController.isAIControlled = false;
            _catController.SetMoveInput(Vector2.zero);
        }
    }

    private void Start()
    {
        // Tự động tìm kiếm MouseController nếu chưa gán
        if (_targetMouse == null)
        {
            _targetMouse = FindAnyObjectByType<MouseController>();
        }
    }

    /// <summary>
    /// Thiết lập lộ trình các điểm tuần tra qua lại thông suốt giữa các phòng
    /// </summary>
    private void SetupWaypoints()
    {
        MapManager.MapType activeMap = MapManager.Instance != null ? MapManager.Instance.CurrentMap : MapManager.MapType.Penthouse;
        SetMapWaypoints(activeMap);
    }

    /// <summary>
    /// Cập nhật waypoints phù hợp với Map đang chọn (Penthouse hoặc Warehouse)
    /// </summary>
    public void SetMapWaypoints(MapManager.MapType mapType)
    {
        _currentWaypointIndex = 0;
        if (mapType == MapManager.MapType.Penthouse)
        {
            _patrolWaypoints = new List<Vector2>
            {
                // 1. Sảnh phòng khách trung tâm
                new Vector2(0f, -4.5f),
                new Vector2(0f, 0f),
                new Vector2(1.5f, 2.5f),

                // 2. Khu bếp mở và quầy bar (Trái)
                new Vector2(-5.0f, -2.5f),
                new Vector2(-10.0f, -2.5f),
                new Vector2(-10.0f, 2.0f),

                // 3. Phòng ăn Bắc Âu
                new Vector2(-4.5f, 3.5f),

                // 4. Ban công dài trên cao (Top Balcony)
                new Vector2(-9.0f, 7.2f),
                new Vector2(0f, 7.2f),
                new Vector2(9.0f, 7.2f),

                // 5. Phòng ngủ Master (Phải)
                new Vector2(10.5f, 4.0f),
                new Vector2(10.5f, 0.5f),

                // 6. Phòng tắm En-Suite (Góc dưới phải)
                new Vector2(9.0f, -4.5f),
                new Vector2(12.0f, -6.0f),
                new Vector2(9.0f, -4.5f),

                // 7. Về lại sảnh phòng khách
                new Vector2(4.0f, -4.5f),
                new Vector2(0f, -4.5f)
            };
        }
        else
        {
            _patrolWaypoints = new List<Vector2>
            {
                // 1. Sảnh phân loại Sorting Area
                new Vector2(0f, -1.8f),
                new Vector2(0f, 2.5f),
                new Vector2(3.5f, 0.5f),

                // 2. [LỐI THÔNG CỬA 7]: Cửa phòng Break Room (X = 7.8f, Y = -1.5f) và trong phòng
                new Vector2(7.8f, -1.5f),
                new Vector2(11.0f, -4.5f),
                new Vector2(7.8f, -1.5f),

                // 3. Sảnh giữa
                new Vector2(1.0f, 0f),

                // 4. [LỐI THÔNG CỬA 5]: Cửa phòng Office Room (X = 5.8f, Y = 3.8f) và trong phòng
                new Vector2(5.8f, 3.8f),
                new Vector2(5.8f, 5.8f),
                new Vector2(5.8f, 3.8f),

                // 5. [LỐI THÔNG CỬA 4]: Kho hóa chất Toxic Area mở toang xuống Sorting Area
                new Vector2(1.5f, 3.8f),
                new Vector2(1.5f, 5.8f),
                new Vector2(1.5f, 3.8f),

                // 6. [LỐI THÔNG CỬA 3]: Cửa phòng Control Room (X = -4.5f, Y = 3.8f) và trong phòng
                new Vector2(-4.5f, 3.8f),
                new Vector2(-4.5f, 5.8f),
                new Vector2(-4.5f, 3.8f),

                // 7. [LỐI THÔNG CỬA 1]: Cửa trên sang kho Heavy Storage (X = -6.8f, Y = 3.25f)
                new Vector2(-6.8f, 3.25f),
                new Vector2(-11.0f, 3.25f),
                new Vector2(-11.0f, -4.5f),

                // 8. [LỐI THÔNG CỬA 2]: Cửa dưới từ kho Heavy Storage về lại sảnh (X = -6.8f, Y = -1.25f)
                new Vector2(-6.8f, -1.25f),
                new Vector2(0f, -1.8f)
            };
        }
    }

    private void Update()
    {
        if (_catController == null || !_catController.enabled) return;

        // Nếu trận đấu chưa bắt đầu hoặc đã kết thúc -> dừng lại
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            _catController.SetMoveInput(Vector2.zero);
            return;
        }

        // Nếu Mèo đang bị Stun (choáng) hoặc đang vung tay đánh -> đứng im chịu phạt
        if (_catController.IsStunned || _catController.IsAttacking)
        {
            _catController.SetMoveInput(Vector2.zero);
            return;
        }

        // Tìm Chuột nếu bị mất reference
        if (_targetMouse == null)
        {
            _targetMouse = FindAnyObjectByType<MouseController>();
            if (_targetMouse == null) return;
        }

        // Đánh giá khoảng cách và trạng thái phát hiện Chuột
        EvaluatePerception();

        // Thực thi hành vi theo State
        switch (currentState)
        {
            case AIState.Patrol:
                ExecutePatrol();
                break;

            case AIState.Investigate:
                ExecuteInvestigate();
                break;

            case AIState.Chase:
                ExecuteChase();
                break;
        }
    }

    /// <summary>
    /// Giác quan của Mèo: Quét tìm Chuột dựa trên trạng thái ngụy trang và khoảng cách
    /// </summary>
    private void EvaluatePerception()
    {
        if (_targetMouse == null || _targetMouse.IsCaught) return;

        float distToMouse = Vector2.Distance(transform.position, _targetMouse.transform.position);

        // Trường hợp 1: Chuột KHÔNG ngụy trang -> Bị phát hiện ngay nếu trong tầm mắt
        if (!_targetMouse.IsDisguised)
        {
            if (distToMouse <= visionRadiusNormal)
            {
                currentState = AIState.Chase;
                return;
            }
        }
        else
        {
            // Trường hợp 2: Chuột ĐANG ngụy trang nhưng CÓ DI CHUYỂN
            bool isMouseMoving = _targetMouse.MoveInput.sqrMagnitude > 0.01f;
            if (isMouseMoving && distToMouse <= visionRadiusMovingDisguise)
            {
                // Thùng hàng biết đi! Mèo phát hiện ngay lập tức
                currentState = AIState.Chase;
                return;
            }

            // Trường hợp 3: Chuột ĐANG ngụy trang và ĐỨNG IM
            if (distToMouse <= visionRadiusStillDisguise)
            {
                // Mèo đứng gần ngửi ngửi, thanh nghi vấn (suspicion) tăng dần
                _suspicionMeter += Time.deltaTime * 1.5f;
                if (_suspicionMeter >= 1.2f)
                {
                    // Quá khả nghi! Mèo vồ tát thử vào đồ vật này!
                    currentState = AIState.Chase;
                    return;
                }
            }
            else
            {
                _suspicionMeter = Mathf.Max(0f, _suspicionMeter - Time.deltaTime);
            }
        }

        // Nếu đang đuổi theo mà Chuột trốn quá xa -> quay lại tuần tra
        if (currentState == AIState.Chase && distToMouse > visionRadiusNormal * 1.4f)
        {
            currentState = AIState.Patrol;
            _suspicionMeter = 0f;
        }
    }

    /// <summary>
    /// Hành vi tuần tra định tuyến giữa các phòng
    /// </summary>
    private void ExecutePatrol()
    {
        if (_patrolWaypoints == null || _patrolWaypoints.Count == 0) return;

        Vector2 targetPos = _patrolWaypoints[_currentWaypointIndex];
        Vector2 currentPos = transform.position;
        float dist = Vector2.Distance(currentPos, targetPos);

        if (dist <= 0.6f)
        {
            // Đã đến điểm tuần tra -> chuyển điểm tiếp theo
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _patrolWaypoints.Count;

            // 20% khả năng dừng lại soi xét trong 1.5 giây
            if (UnityEngine.Random.value < 0.25f)
            {
                currentState = AIState.Investigate;
                _investigateTimer = UnityEngine.Random.Range(1.0f, 2.0f);
                _catController.SetMoveInput(Vector2.zero);
                return;
            }
        }

        Vector2 dir = (targetPos - currentPos).normalized;
        dir = AvoidObstacles(dir);
        _catController.SetMoveInput(dir * (patrolSpeed / _catController.MoveSpeed));
    }

    /// <summary>
    /// Hành vi dừng lại quan sát, ngửi mùi xung quanh
    /// </summary>
    private void ExecuteInvestigate()
    {
        _investigateTimer -= Time.deltaTime;
        _catController.SetMoveInput(Vector2.zero);

        if (_investigateTimer <= 0f)
        {
            currentState = AIState.Patrol;
        }
    }

    /// <summary>
    /// Hành vi truy đuổi áp sát và vồ bắt Chuột
    /// </summary>
    private void ExecuteChase()
    {
        if (_targetMouse == null) return;

        Vector2 targetPos = _targetMouse.transform.position;
        Vector2 currentPos = transform.position;
        float dist = Vector2.Distance(currentPos, targetPos);

        // Nếu đã đủ tầm tát -> Tung móng vuốt vồ bắt!
        if (dist <= attackTriggerDistance)
        {
            _catController.SetMoveInput(Vector2.zero);
            _catController.TriggerAttack();
            return;
        }

        // Phi hết tốc lực truy đuổi Chuột
        Vector2 dir = (targetPos - currentPos).normalized;
        dir = AvoidObstacles(dir);
        _catController.SetMoveInput(dir * (chaseSpeed / _catController.MoveSpeed));
    }

    /// <summary>
    /// Tránh va chạm góc tường đơn giản bằng CircleCast
    /// </summary>
    private Vector2 AvoidObstacles(Vector2 desiredDirection)
    {
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.35f, desiredDirection, 0.8f, LayerMask.GetMask("Default"));
        if (hit.collider != null && !hit.collider.isTrigger)
        {
            // Bẻ góc vuông sang trái hoặc phải để lượn qua góc tường
            Vector2 normal = hit.normal;
            Vector2 tangent = new Vector2(-normal.y, normal.x);
            if (Vector2.Dot(tangent, desiredDirection) < 0f)
            {
                tangent = -tangent;
            }
            return (desiredDirection + tangent * 1.5f).normalized;
        }

        return desiredDirection;
    }
}
