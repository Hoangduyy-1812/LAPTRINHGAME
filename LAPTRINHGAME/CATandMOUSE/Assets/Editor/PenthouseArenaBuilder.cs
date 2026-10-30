using System;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Trình dựng đấu trường Penthouse Hiện Đại (Penthouse Level Map) 16:9 cho dự án CATandMOUSE:
/// - Tạo Backdrop Penthouse_Level_Map chuẩn 32x18 units.
/// - Thiết lập 4 Perimeter Walls và các vách ngăn phòng bên trong với lối thông phòng mở rộng từ 2.2m đến 8.0m (không cấn mép).
/// - Bố trí bộ Props nội thất hiện đại (Sofa, Bàn cẩm thạch, Cây Monstera, Robot hút bụi, Ghế Scandinavian, Đèn, Ghế bar).
/// - Vẽ Gizmos màu xanh lá cây bán trong suốt chuẩn xác trong Scene View.
/// </summary>
public class PenthouseArenaBuilder : MonoBehaviour
{
    private const string SPRITES_FOLDER = "Assets/Resources/GeneratedSprites";
    private const string MAP_FILE_NAME = "Penthouse_Level_Map.jpg";

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        foreach (var col in GetComponentsInChildren<BoxCollider2D>())
        {
            if (col == null) continue;
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }

#if UNITY_EDITOR
    [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy | GizmoType.Selected | GizmoType.Pickable)]
    private static void DrawPenthouseCollidersGizmo(Transform transform, GizmoType gizmoType)
    {
        if (transform != null && transform.name == "Penthouse_Colliders")
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
            foreach (var col in transform.GetComponentsInChildren<BoxCollider2D>())
            {
                if (col == null) continue;
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
#endif

    public static GameObject BuildPenthouseArenaRoot(Transform parent)
    {
        // 1. Root Container cho toàn bộ Map Penthouse
        GameObject root = new GameObject("Arena_Penthouse_Container");
        if (parent != null) root.transform.SetParent(parent);
        root.transform.position = Vector3.zero;

        // 2. Map Backdrop Sprite
        GameObject mapObj = new GameObject("Penthouse_Map_Backdrop");
        mapObj.transform.SetParent(root.transform);
        mapObj.transform.position = Vector3.zero;

        SpriteRenderer sr = mapObj.AddComponent<SpriteRenderer>();
        string mapPath = $"{SPRITES_FOLDER}/{MAP_FILE_NAME}";
        Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(mapPath);
        if (mapSprite != null)
        {
            sr.sprite = mapSprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = -100;

            // Chuẩn hóa kích thước sang đúng 32x18 units
            Vector2 spriteSize = mapSprite.rect.size / mapSprite.pixelsPerUnit;
            if (spriteSize.x > 0 && spriteSize.y > 0)
            {
                float scaleX = 32.0f / spriteSize.x;
                float scaleY = 18.0f / spriteSize.y;
                mapObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
        }

        // 3. Colliders Vật lý
        GameObject collidersRoot = new GameObject("Penthouse_Colliders");
        collidersRoot.transform.SetParent(root.transform);
        collidersRoot.transform.position = Vector3.zero;
        collidersRoot.AddComponent<PenthouseArenaBuilder>();

        BuildPenthouseColliders(collidersRoot.transform);

        // 4. Props Nội thất hiện đại
        GameObject propsRoot = new GameObject("Penthouse_Props");
        propsRoot.transform.SetParent(root.transform);
        propsRoot.transform.position = Vector3.zero;

        BuildPenthouseProps(propsRoot.transform);

        return root;
    }

    private static void BuildPenthouseColliders(Transform root)
    {
        // ==========================================
        // 1. 4 TƯỜNG NGOẠI VI (32.0 x 18.0 UNITS)
        // ==========================================
        CreateCollider(root, "Wall_Outer_Top", new Vector2(0f, 8.8f), new Vector2(32.0f, 1.2f));
        CreateCollider(root, "Wall_Outer_Bottom", new Vector2(0f, -8.8f), new Vector2(32.0f, 1.2f));
        CreateCollider(root, "Wall_Outer_Left", new Vector2(-15.8f, 0f), new Vector2(1.2f, 18.0f));
        CreateCollider(root, "Wall_Outer_Right", new Vector2(15.8f, 0f), new Vector2(1.2f, 18.0f));

        // ==========================================
        // 2. VÁCH NGĂN BAN CÔNG (Y = 6.4f, CÓ 3 CỬA TRƯỢT RỘNG 2.5 - 3.0M)
        // ==========================================
        CreateCollider(root, "Balcony_Wall_Left", new Vector2(-13.0f, 6.4f), new Vector2(4.5f, 0.8f));
        // [CỬA TRƯỢT BẾP - BAN CÔNG]: X = -10.75f đến X = -8.25f (Rộng 2.5m mở toang)
        CreateCollider(root, "Balcony_Wall_MidLeft", new Vector2(-5.0f, 6.4f), new Vector2(6.5f, 0.8f));
        // [CỬA TRƯỢT PHÒNG KHÁCH - BAN CÔNG]: X = -1.75f đến X = 1.25f (Rộng 3.0m mở toang)
        CreateCollider(root, "Balcony_Wall_MidRight", new Vector2(3.5f, 6.4f), new Vector2(4.5f, 0.8f));
        // [CỬA TRƯỢT PHÒNG NGỦ - BAN CÔNG]: X = 5.75f đến X = 8.75f (Rộng 3.0m mở toang)
        CreateCollider(root, "Balcony_Wall_Right", new Vector2(12.5f, 6.4f), new Vector2(7.5f, 0.8f));

        // ==========================================
        // 3. VÁCH BẾP PHÍA DƯỚI (TỦ LẠNH)
        // ==========================================
        CreateCollider(root, "Kitchen_Bottom_Divider", new Vector2(-5.5f, -7.5f), new Vector2(0.8f, 2.5f));
        // Lưu ý: Không gian Bếp - Bàn Ăn - Phòng Khách mở toang 100% (Open Concept rộng 8.0m)

        // ==========================================
        // 4. VÁCH NGĂN PHÒNG KHÁCH & PHÒNG NGỦ MASTER (X = 6.0f)
        // ==========================================
        CreateCollider(root, "Living_Bedroom_Wall_Top", new Vector2(6.0f, 3.8f), new Vector2(0.8f, 4.4f));
        // [CỬA PHÒNG NGỦ MASTER]: Y = -0.5f đến Y = 1.6f (Rộng 2.5m mở toang)
        CreateCollider(root, "Living_Bedroom_Wall_Bottom", new Vector2(6.0f, -1.8f), new Vector2(0.8f, 2.0f));

        // ==========================================
        // 5. VÁCH NGĂN PHÒNG NGỦ & PHÒNG TẮM (Y = -2.5f)
        // ==========================================
        CreateCollider(root, "Bedroom_Bathroom_Wall", new Vector2(11.5f, -2.5f), new Vector2(8.0f, 0.8f));

        // ==========================================
        // 6. VÁCH NGĂN PHÒNG TẮM & HÀNH LANG (X = 8.0f)
        // ==========================================
        CreateCollider(root, "Bathroom_Hallway_Wall_Bottom", new Vector2(8.0f, -6.5f), new Vector2(0.8f, 3.5f));
        // [CỬA PHÒNG TẮM]: Y = -4.75f đến Y = -2.5f (Rộng 2.25m mở toang)
    }

    private static void CreateCollider(Transform parent, string name, Vector2 center, Vector2 size)
    {
        GameObject colObj = new GameObject(name);
        colObj.transform.SetParent(parent);
        colObj.transform.position = center;
        colObj.layer = LayerMask.NameToLayer("Default");

        BoxCollider2D box = colObj.AddComponent<BoxCollider2D>();
        box.size = size;
        box.offset = Vector2.zero;
    }

    private static void BuildPenthouseProps(Transform parent)
    {
        // 1. Ghế sofa văng kem bo tròn phòng khách
        SpawnProp(parent, "Prop_Sofa_Living", "Prop_Modern_Sofa", new Vector2(0.2f, 0.5f), new Vector2(2.0f, 1.1f), 1.6f);

        // 2. Bàn ăn cẩm thạch trắng phòng ăn
        SpawnProp(parent, "Prop_Marble_Table_Dining", "Prop_Marble_Table", new Vector2(-4.5f, 3.5f), new Vector2(1.4f, 1.4f), 1.3f);

        // 3. Chậu cây Monstera góc phòng khách
        SpawnProp(parent, "Prop_Monstera_Living", "Prop_Monstera_Plant", new Vector2(4.8f, 5.2f), new Vector2(0.9f, 0.9f), 0.9f);

        // 4. Chậu cây Monstera góc ban công trái
        SpawnProp(parent, "Prop_Monstera_Balcony_Left", "Prop_Monstera_Plant", new Vector2(-13.5f, 7.5f), new Vector2(0.9f, 0.9f), 0.9f);

        // 5. Chậu cây Monstera góc ban công phải
        SpawnProp(parent, "Prop_Monstera_Balcony_Right", "Prop_Monstera_Plant", new Vector2(13.5f, 7.5f), new Vector2(0.9f, 0.9f), 0.9f);

        // 6. Máy hút bụi robot thông minh
        SpawnProp(parent, "Prop_Robot_Vacuum", "Prop_Robot_Vacuum", new Vector2(1.8f, -2.5f), new Vector2(0.7f, 0.7f), 0.6f);

        // 7. Ghế Scandinavian nỉ xanh phòng ăn
        SpawnProp(parent, "Prop_Chair_Dining_1", "Prop_Modern_Chair", new Vector2(-6.0f, 3.5f), new Vector2(0.7f, 0.7f), 0.7f);

        // 8. Ghế bành phòng khách
        SpawnProp(parent, "Prop_Chair_Living_1", "Prop_Modern_Chair", new Vector2(2.2f, 1.2f), new Vector2(0.8f, 0.8f), 0.8f);

        // 9. Đèn ngủ góc phòng ngủ Master
        SpawnProp(parent, "Prop_Lamp_Bedroom", "Prop_Modern_Lamp", new Vector2(14.0f, 5.0f), new Vector2(0.7f, 0.9f), 0.8f);

        // 10. Ghế bar đảo bếp 1
        SpawnProp(parent, "Prop_Barstool_1", "Prop_Kitchen_Barstool", new Vector2(-7.2f, -1.5f), new Vector2(0.6f, 0.6f), 0.6f);

        // 11. Ghế bar đảo bếp 2
        SpawnProp(parent, "Prop_Barstool_2", "Prop_Kitchen_Barstool", new Vector2(-7.2f, -3.2f), new Vector2(0.6f, 0.6f), 0.6f);
    }

    private static void SpawnProp(Transform parent, string gameObjectName, string spriteName, Vector2 position, Vector2 colliderSize, float targetUnitSize)
    {
        GameObject propObj = new GameObject(gameObjectName);
        propObj.transform.SetParent(parent);
        propObj.transform.position = position;

        SpriteRenderer sr = propObj.AddComponent<SpriteRenderer>();
        string assetPath = $"{SPRITES_FOLDER}/{spriteName}.png";
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sp == null)
        {
            assetPath = $"{SPRITES_FOLDER}/UI/{spriteName}.png";
            sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        if (sp != null)
        {
            sr.sprite = sp;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 10;

            // Scale prop theo targetUnitSize
            Vector2 spritePixelSize = sp.rect.size / sp.pixelsPerUnit;
            if (spritePixelSize.x > 0 && spritePixelSize.y > 0)
            {
                float maxDim = Mathf.Max(spritePixelSize.x, spritePixelSize.y);
                float scale = targetUnitSize / maxDim;
                propObj.transform.localScale = Vector3.one * scale;
            }
        }

        BoxCollider2D col = propObj.AddComponent<BoxCollider2D>();
        col.size = colliderSize;
        col.offset = Vector2.zero;

        PropItem propItem = propObj.AddComponent<PropItem>();
        propItem.propId = spriteName;
        propItem.propSprite = sp;
        propItem.customColliderSize = colliderSize;
        propItem.targetUnitSize = targetUnitSize;
        propItem.isFake = false;
    }
}
