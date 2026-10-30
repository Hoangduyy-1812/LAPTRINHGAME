using UnityEngine;

/// <summary>
/// Công cụ trực quan hóa (Debug Gizmos) toàn bộ Collider trong Scene:
/// Tự động vẽ viền đỏ cho các bức tường vật lý (BoxCollider2D),
/// vẽ viền xanh lục cho các Trigger vùng (Camera Confiner, Sensor),
/// vẽ viền vàng cho các Collider nhân vật (CircleCollider2D).
/// Giúp phát hiện ngay lập tức bất kỳ Collider vô hình nào chắn lối đi trên Scene view.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class ColliderGizmoVisualizer : MonoBehaviour
{
    [Header("Gizmo Colors")]
    [Tooltip("Màu viền các bức tường vật lý rắn")]
    public Color wallColor = new Color(1f, 0.2f, 0.2f, 0.9f); // Đỏ tươi

    [Tooltip("Màu viền các vùng Trigger (Confiner, Sensors)")]
    public Color triggerColor = new Color(0.2f, 1f, 0.4f, 0.7f); // Xanh lục

    [Tooltip("Màu viền Collider tròn của nhân vật")]
    public Color characterColor = new Color(1f, 0.9f, 0.2f, 0.9f); // Vàng

    [Header("Visualization Settings")]
    [Tooltip("Bật / tắt hiển thị Gizmos")]
    public bool showGizmos = true;

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // 1. Vẽ các BoxCollider2D con (Tường của Environment_Colliders) bằng màu xanh lá bán trong suốt
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f); // Màu xanh lá cây bán trong suốt
        foreach (var col in GetComponentsInChildren<BoxCollider2D>())
        {
            if (col == null || !col.enabled) continue;
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        // 2. Vẽ các BoxCollider2D khác (Confiner, Sensors)
        BoxCollider2D[] boxColliders = FindObjectsByType<BoxCollider2D>();
        foreach (var box in boxColliders)
        {
            if (box == null || !box.enabled || box.transform.IsChildOf(transform)) continue;

            Gizmos.color = box.isTrigger ? triggerColor : wallColor;

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(box.transform.position, box.transform.rotation, box.transform.lossyScale);
            Gizmos.DrawWireCube((Vector3)box.offset, (Vector3)box.size);
            Gizmos.matrix = oldMatrix;
        }

        // 2. Vẽ toàn bộ CircleCollider2D (Mèo, Chuột, Bán kính tương tác)
        CircleCollider2D[] circleColliders = FindObjectsByType<CircleCollider2D>();
        foreach (var circle in circleColliders)
        {
            if (circle == null || !circle.enabled) continue;

            Gizmos.color = circle.isTrigger ? triggerColor : characterColor;
            Vector3 center = circle.transform.position + (Vector3)circle.offset;
            float scale = Mathf.Max(Mathf.Abs(circle.transform.lossyScale.x), Mathf.Abs(circle.transform.lossyScale.y));
            Gizmos.DrawWireSphere(center, circle.radius * scale);
        }
    }
}
