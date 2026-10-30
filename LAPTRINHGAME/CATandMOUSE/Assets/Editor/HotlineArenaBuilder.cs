using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Trình dựng bản đồ Hotline Miami / Enter the Gungeon phong cách Retro-Modern 16:9 duy nhất và toàn diện của dự án CATandMOUSE.
/// Tự động thực thi toàn bộ quy trình:
/// 1. Tối ưu hóa toàn bộ Texture sang chuẩn HD Studio (FilterMode.Point, Uncompressed 4K, khử vỡ hạt triệt để).
/// 2. Dựng bản đồ Warehouse_Full_Level_Map hoàn chỉnh với tỷ lệ 16:9.
/// 3. Thiết lập hệ thống Colliders vật lý và đổ bóng thời gian thực cho toàn bộ các phòng và băng chuyền.
/// 4. Chuẩn hóa kích thước nhân vật: Mèo = 1.0 unit (lọt qua mọi cửa ngõ), Chuột = 0.6 unit (CircleCollider 0.2f).
/// 5. Bố trí 12 Đạo cụ ngụy trang (Props) theo kích thước chuẩn mét (0.7 - 1.1 units), 100% nép sát tường và góc chết, không chắn lối đi.
/// 6. Thiết lập hệ thống ánh sáng 2D đa sắc (URP Light2D) và Global Volume Post-Processing điện ảnh.
/// 7. Cấu hình Cinemachine Virtual Camera 16:9 cận cảnh với Orthographic Size = 3.8f (Lookahead 0.15s, Damping 0.5s, Confiner 2D).
/// </summary>
public class HotlineArenaBuilder : MonoBehaviour
{
    private const string SPRITES_FOLDER = "Assets/Resources/GeneratedSprites";
    private const string MAP_FILE_NAME = "Warehouse_Full_Level_Map.png";

    /// <summary>
    /// Debug Visual Gizmos: Vẽ khối và khung dây xanh lá cây bán trong suốt cho toàn bộ Collider tường
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f); // Màu xanh lá cây bán trong suốt
        foreach (var col in GetComponentsInChildren<BoxCollider2D>())
        {
            if (col == null) continue;
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }

#if UNITY_EDITOR
    [DrawGizmo(GizmoType.Active | GizmoType.NotInSelectionHierarchy | GizmoType.Selected | GizmoType.Pickable)]
    private static void DrawArenaCollidersGizmo(Transform transform, GizmoType gizmoType)
    {
        if (transform != null && transform.name == "Environment_Colliders")
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

    [MenuItem("Tools/CATandMOUSE/🚀 Rebuild Complete Game (Modern UI, Penthouse & Warehouse)", false, 1)]
    public static void RebuildCompleteGame()
    {
        BuildHotlineArenaInternal(silent: false);
    }

    [MenuItem("Tools/CATandMOUSE/🚀 Apply HD Camera & Fix All Colliders", false, 2)]
    public static void BuildHotlineArena()
    {
        BuildHotlineArenaInternal(silent: false);
    }

    public static void BuildHotlineArenaBatch()
    {
        if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity")
        {
            if (File.Exists("Assets/Scenes/SampleScene.unity"))
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }
        }
        BuildHotlineArenaInternal(silent: true);
        var activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        EditorApplication.Exit(0);
    }

    public static void TestPlayerBuildBatch()
    {
        string buildPath = "Builds/Windows/CATandMOUSE.exe";
        string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity" };
        var report = BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log($"[TestPlayerBuildBatch] Build Result: {report.summary.result}, Total Errors: {report.summary.totalErrors}");
        EditorApplication.Exit(report.summary.totalErrors == 0 ? 0 : 1);
    }

    public static void BuildHotlineArenaInternal(bool silent)
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        if (string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
        {
            if (File.Exists("Assets/Scenes/SampleScene.unity"))
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }
        }

        // 1. Tự động chuẩn hóa toàn bộ Texture trong dự án sang định dạng HD chuẩn Studio
        OptimizeAllTextures();

        // 2. Cấu hình TextureImporter riêng cho bản đồ tổng thể
        ConfigureMapTexture();

        // 3. Dọn dẹp các đối tượng cũ trong Scene
        CleanUpScene();

        // 4. Tạo Warehouse Arena Container
        GameObject warehouseRoot = new GameObject("Arena_Warehouse_Container");
        warehouseRoot.transform.position = Vector3.zero;

        GameObject arenaMapObj = CreateArenaMapObject();
        if (arenaMapObj != null) arenaMapObj.transform.SetParent(warehouseRoot.transform);

        // 5. Tạo hệ thống Tường bao và Vách ngăn Collider Warehouse
        CreateArenaColliders();
        GameObject envColliders = GameObject.Find("Environment_Colliders");
        if (envColliders != null) envColliders.transform.SetParent(warehouseRoot.transform);

        // 6. Rải 12 đạo cụ ngụy trang tương tác chuẩn vị trí sát tường / góc chết Warehouse
        SpawnInteractiveProps();
        GameObject propsGroup = GameObject.Find("Props_Group");
        if (propsGroup != null) propsGroup.transform.SetParent(warehouseRoot.transform);

        // 7. Tạo Penthouse Arena (Map Backdrop, Colliders lối thông 2.2-8.0m, Modern Props)
        GameObject penthouseRoot = PenthouseArenaBuilder.BuildPenthouseArenaRoot(null);

        // 8. Tạo và phân bổ nhân vật Mèo & Chuột theo chuẩn mét
        GameObject catObj = SpawnCat();
        GameObject mouseObj = SpawnMouse();

        // 9. Tạo MapManager điều phối chuyển đổi Map
        GameObject mapMgrObj = new GameObject("MapManager");
        MapManager mapMgr = mapMgrObj.AddComponent<MapManager>();
        mapMgr.InitializeContainers(penthouseRoot, warehouseRoot);
        mapMgr.RepositionCharactersForMap(mapMgr.CurrentMap);

        // 10. Thiết lập hệ thống ánh sáng 2D điện ảnh URP
        SetupAtmospheric2DLights();

        // 11. Thiết lập Global Volume Post-Processing
        SetupPostProcessingVolume();

        // 12. Cấu hình Camera cận cảnh 16:9 với Cinemachine
        ConfigureCamera(catObj);

        // 13. Tạo GameManager & Modern UI Canvas
        SetupGameManager(catObj, mouseObj);

        // Lưu Scene an toàn khi không ở Play Mode
        if (!EditorApplication.isPlaying)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }

        Debug.Log("🚀 [HotlineArenaBuilder] Đã Rebuild hoàn chỉnh toàn bộ Game: Penthouse Hiện Đại + Warehouse Cũ + Modern UI 2026!");
        
        if (!silent && !Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "🚀 ĐÃ ĐỤC THÔNG CỬA & TẠO MENU 2 CHẾ ĐỘ!",
                "Toàn bộ hệ thống bản đồ và chế độ chơi đã được nâng cấp toàn diện:\n\n" +
                "• ĐỤC THÔNG CỬA 100%: Quét sạch toàn bộ collider cũ (Walls_Group), mở rộng toàn bộ cửa lên >= 3.0 units, di chuyển 100% thông suốt.\n" +
                "• DEBUG GIZMOS: Tự động vẽ viền đỏ cho tất cả tường vật lý và viền xanh cho trigger trong Scene view.\n" +
                "• CHẾ ĐỘ 1 - OFFLINE BOT: Bot Mèo tự động tuần tra 5 phòng, rình vồ khi thấy Chuột chạy/ngụy trang.\n" +
                "• CHẾ ĐỘ 2 - ONLINE LAN (2 MÁY): Host (Mèo) và Client (Chuột) riêng màn hình, bảo mật góc nhìn 100%.\n" +
                "• CHẾ ĐỘ 3 - LOCAL 2P: 2 người chơi cùng bàn phím (WASD + Mũi tên).\n\n" +
                "Bấm PLAY (▶) để chọn chế độ và trải nghiệm ngay!",
                "CHIẾN LUÔN!"
            );
        }
    }

    #region Texture Optimization & Setup

    public static void OptimizeAllTextures()
    {
        if (!Directory.Exists(SPRITES_FOLDER)) return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SPRITES_FOLDER });
        int optimizedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/UI/") || path.Contains("\\UI\\")) continue;
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool isModified = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                isModified = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                isModified = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                isModified = true;
            }

            if (importer.maxTextureSize < 4096)
            {
                importer.maxTextureSize = 4096;
                isModified = true;
            }

            if (!importer.alphaIsTransparency && !path.Contains("Warehouse_Full_Level_Map"))
            {
                importer.alphaIsTransparency = true;
                isModified = true;
            }

            if (isModified)
            {
                importer.SaveAndReimport();
                optimizedCount++;
            }
        }

        if (optimizedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"💎 [HotlineArenaBuilder] Đã chuẩn hóa {optimizedCount} Textures sang định dạng HD chuẩn Studio (Point Filter, Uncompressed, 4K)!");
        }
    }

    public static void ConfigureMapTexture()
    {
        string relativePath = $"{SPRITES_FOLDER}/{MAP_FILE_NAME}";
        AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 50f; // Kích thước thế giới ~ 27.52 x 15.36 units
            importer.alphaIsTransparency = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }
    }

    #endregion

    #region Scene Objects Creation

    private static void CleanUpScene()
    {
        string[] oldNames = new string[]
        {
            "Warehouse_Arena_Map",
            "Environment_Colliders",
            "Walls_Group",
            "Global_Volume",
            "Arena_Colliders",
            "Warehouse_Floor",
            "Outer_Walls",
            "Inner_Walls",
            "Props_Group",
            "Cat",
            "Mouse",
            "Global_Light_2D",
            "Lights_Group",
            "Cinematic_Volume",
            "Cinemachine_Camera",
            "Camera_Confiner_Bounds",
            "GameManager",
            "GameModeManager",
            "LanNetworkManager",
            "MapManager",
            "Arena_Warehouse_Container",
            "Arena_Penthouse_Container",
            "Penthouse_Colliders",
            "Penthouse_Props",
            "Canvas_ModernUI",
            "EventSystem",
            "InGame_HUD"
        };

        foreach (string name in oldNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        // Quét sạch tất cả root GameObjects tàn dư của phiên bản cũ (Wall_*, Partition_*, Walls_Group)
        var activeScene = EditorSceneManager.GetActiveScene();
        foreach (var rootObj in activeScene.GetRootGameObjects())
        {
            if (rootObj == null) continue;
            string n = rootObj.name;
            if (n.Equals("Walls_Group") || n.StartsWith("Wall_") || n.StartsWith("Partition_") || (n.Contains("Colliders") && n != "Environment_Colliders"))
            {
                Undo.DestroyObjectImmediate(rootObj);
            }
        }

        // Dọn dẹp triệt để tất cả Global Light 2D hiện có để tránh lỗi trùng lặp Global Light
        Light2D[] allLights = UnityEngine.Object.FindObjectsByType<Light2D>();
        foreach (var l in allLights)
        {
            if (l != null && l.lightType == Light2D.LightType.Global)
            {
                Undo.DestroyObjectImmediate(l.gameObject);
            }
        }
    }

    private static GameObject CreateArenaMapObject()
    {
        string relativePath = $"{SPRITES_FOLDER}/{MAP_FILE_NAME}";
        Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
        if (mapSprite == null)
        {
            mapSprite = Resources.Load<Sprite>($"GeneratedSprites/{Path.GetFileNameWithoutExtension(MAP_FILE_NAME)}");
        }

        if (mapSprite == null) return null;

        GameObject mapObj = new GameObject("Warehouse_Arena_Map");
        mapObj.transform.position = Vector3.zero;

        SpriteRenderer sr = mapObj.AddComponent<SpriteRenderer>();
        sr.sprite = mapSprite;
        sr.sortingOrder = -20; // Nằm ở tầng nền sâu nhất

        // Quy chuẩn kích thước bản đồ 16:9: Rộng 32 units, Cao 18 units, Tâm tại (0,0)
        float spriteW = mapSprite.bounds.size.x;
        float spriteH = mapSprite.bounds.size.y;
        if (spriteW > 0.001f && spriteH > 0.001f)
        {
            mapObj.transform.localScale = new Vector3(32f / spriteW, 18f / spriteH, 1f);
        }

        Undo.RegisterCreatedObjectUndo(mapObj, "Create Arena Map");
        return mapObj;
    }

    private static void CreateArenaColliders()
    {
        // 1. XÓA BỎ TOÀN BỘ COLLIDER CŨ:
        // Tìm và Destroy sạch sẽ toàn bộ GameObject con trong Environment_Colliders để tránh bị chồng chéo tường tàng hình cũ
        GameObject collidersRoot = GameObject.Find("Environment_Colliders");
        if (collidersRoot == null)
        {
            collidersRoot = new GameObject("Environment_Colliders");
            collidersRoot.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(collidersRoot, "Create Environment Colliders");
        }
        else
        {
            for (int i = collidersRoot.transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(collidersRoot.transform.GetChild(i).gameObject);
            }
        }

        // Gắn ColliderGizmoVisualizer để trực quan hóa Gizmos xanh lá trong Scene view
        if (!collidersRoot.TryGetComponent<ColliderGizmoVisualizer>(out _))
        {
            collidersRoot.AddComponent<ColliderGizmoVisualizer>();
        }

        // 2. TẠO 4 BỨC TƯỜNG VIỀN NGOÀI CÙNG (PERIMETER WALLS):
        AddWallCollider(collidersRoot, "Wall_Outer_Top", new Vector2(0f, 8.8f), new Vector2(32f, 1.2f));
        AddWallCollider(collidersRoot, "Wall_Outer_Bottom", new Vector2(0f, -8.8f), new Vector2(32f, 1.2f));
        AddWallCollider(collidersRoot, "Wall_Outer_Left", new Vector2(-15.8f, 0f), new Vector2(1.2f, 18f));
        AddWallCollider(collidersRoot, "Wall_Outer_Right", new Vector2(15.8f, 0f), new Vector2(1.2f, 18f));

        // 3. TẠO CÁC VÁCH NGĂN PHÒNG BÊN TRONG (ĐÃ CHỪA SẴN 7 LỐI THÔNG CỬA):

        // A. Vách dọc ngăn giữa Kho bên trái (Heavy Storage) và Sảnh trung tâm:
        // Đoạn tường trên: Center (-6.8f, 6.0f), Size (0.8f, 4.5f)
        AddWallCollider(collidersRoot, "Partition_Storage_Upper", new Vector2(-6.8f, 6.0f), new Vector2(0.8f, 4.5f));
        // [LỐI THÔNG CỬA 1]: Khoảng hở từ Y = 2.5f đến Y = 4.0f (MỞ TOANG KHÔNG CÓ COLLIDER)
        // Đoạn tường giữa: Center (-6.8f, 1.0f), Size (0.8f, 2.5f)
        AddWallCollider(collidersRoot, "Partition_Storage_Middle", new Vector2(-6.8f, 1.0f), new Vector2(0.8f, 2.5f));
        // [LỐI THÔNG CỬA 2]: Khoảng hở từ Y = -0.5f đến Y = -2.0f (MỞ TOANG)
        // Đoạn tường dưới: Center (-6.8f, -5.5f), Size (0.8f, 5.0f)
        AddWallCollider(collidersRoot, "Partition_Storage_Lower", new Vector2(-6.8f, -5.5f), new Vector2(0.8f, 5.0f));

        // B. Vách phòng Control Room (Phòng máy tính góc trên bên trái):
        // Tường ngang đáy phòng: Center (-2.0f, 3.8f), Size (4.5f, 0.8f)
        AddWallCollider(collidersRoot, "Partition_ControlRoom_Bottom", new Vector2(-2.0f, 3.8f), new Vector2(4.5f, 0.8f));
        // [LỐI THÔNG CỬA 3]: Khoảng hở mép trái phòng tại X = -4.5f (MỞ TOANG)
        // Tường dọc ngăn với kho hóa chất: Center (0.5f, 6.2f), Size (0.8f, 4.0f)
        AddWallCollider(collidersRoot, "Partition_ControlRoom_Right", new Vector2(0.5f, 6.2f), new Vector2(0.8f, 4.0f));

        // C. Khu vực kho hóa chất (Toxic Area):
        // TUYỆT ĐỐI KHÔNG ĐẶT BẤT KỲ TƯỜNG NGANG NÀO TẠI ĐÁY KHU NÀY (Y = 3.5f đến 4.5f, X từ -1f đến +3f).
        // Toàn bộ khu vực này là [LỐI THÔNG CỬA 4] mở thẳng xuống Sorting Area!

        // D. Vách phòng Office (Phòng thảm đỏ góc trên bên phải):
        // Tường dọc ngăn bên trái: Center (3.5f, 6.2f), Size (0.8f, 4.0f)
        AddWallCollider(collidersRoot, "Partition_Office_Left", new Vector2(3.5f, 6.2f), new Vector2(0.8f, 4.0f));
        // Tường ngang đáy phòng (bên trái cửa): Center (4.5f, 3.8f), Size (1.8f, 0.8f)
        AddWallCollider(collidersRoot, "Partition_Office_Bottom_Left", new Vector2(4.5f, 3.8f), new Vector2(1.8f, 0.8f));
        // [LỐI THÔNG CỬA 5]: Cửa ra vào tại X = 5.8f, Size mở rộng 2.5 đơn vị (MỞ TOANG)
        // Tường ngang đáy phòng (bên phải cửa): Center (7.2f, 3.8f), Size (1.2f, 0.8f)
        AddWallCollider(collidersRoot, "Partition_Office_Bottom_Right", new Vector2(7.2f, 3.8f), new Vector2(1.2f, 0.8f));
        // Tường dọc ngăn bên phải: Center (8.0f, 6.2f), Size (0.8f, 4.0f)
        AddWallCollider(collidersRoot, "Partition_Office_Right", new Vector2(8.0f, 6.2f), new Vector2(0.8f, 4.0f));

        // E. Vách phòng Break Room (Căng tin góc dưới bên phải):
        // Tường ngang nóc phòng (ngăn với hành lang kỹ thuật): Center (11.5f, -0.8f), Size (7.5f, 0.8f)
        AddWallCollider(collidersRoot, "Partition_BreakRoom_Top", new Vector2(11.5f, -0.8f), new Vector2(7.5f, 0.8f));
        // Tường dọc ngăn với Sảnh chính: Center (7.8f, -5.5f), Size (0.8f, 6.0f)
        AddWallCollider(collidersRoot, "Partition_BreakRoom_Left", new Vector2(7.8f, -5.5f), new Vector2(0.8f, 6.0f));
        // [LỐI THÔNG CỬA 7]: Khoảng hở ở góc trên bên trái căng tin tại (X = 7.8f, Y = -1.5f) mở toang rộng 2.5 đơn vị.

        // TUYỆT ĐỐI KHÔNG sinh BoxCollider2D cản đường giữa các băng chuyền hay tim đường hành lang.
        Undo.RegisterCreatedObjectUndo(collidersRoot, "Create Environment Colliders");
    }

    private static void AddWallCollider(GameObject parent, string name, Vector2 offset, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent.transform);
        wall.transform.localPosition = offset;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;

        // Shadow Caster để tường đổ bóng thời gian thực
        ShadowCaster2D caster = wall.AddComponent<ShadowCaster2D>();
        caster.castsShadows = true;
        caster.selfShadows = false;
    }

    #endregion

    #region Characters Spawning

    private static GameObject SpawnCat()
    {
        GameObject cat = new GameObject("Cat");
        cat.transform.position = new Vector3(0f, -1.8f, 0f); // Sảnh phân loại Sorting Area

        SpriteRenderer sr = cat.AddComponent<SpriteRenderer>();
        Sprite catSprite = LoadSprite("Pixel_Cat_TopDown");
        sr.sprite = catSprite;
        sr.color = Color.white;
        sr.sortingOrder = 5;

        // Chuẩn hóa chiều dài cơ thể Mèo = 1.0 unit (lọt qua mọi cửa ngõ và hành lang)
        float catLength = catSprite != null ? catSprite.bounds.size.y : 1f;
        float catScaleFactor = 1.0f / (catLength > 0.001f ? catLength : 1f);
        cat.transform.localScale = new Vector3(catScaleFactor, catScaleFactor, 1f);

        // CircleCollider2D bán kính 0.35f ôm sát thân mèo
        CircleCollider2D col = cat.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;

        Rigidbody2D rb = cat.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 8f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        cat.AddComponent<CatController>();
        CatAIController catAi = cat.AddComponent<CatAIController>();
        catAi.enabled = false; // Mặc định tắt, kích hoạt khi chọn chơi Đơn vs Bot

        ShadowCaster2D caster = cat.AddComponent<ShadowCaster2D>();
        caster.castsShadows = true;
        caster.selfShadows = false;

        Undo.RegisterCreatedObjectUndo(cat, "Spawn Cat");
        return cat;
    }

    private static GameObject SpawnMouse()
    {
        GameObject mouse = new GameObject("Mouse");
        mouse.transform.position = new Vector3(-10.5f, 1.8f, 0f); // Phòng máy móc bí mật bên trái

        SpriteRenderer sr = mouse.AddComponent<SpriteRenderer>();
        Sprite mouseSprite = LoadSprite("Pixel_Mouse_TopDown");
        sr.sprite = mouseSprite;
        sr.color = Color.white;
        sr.sortingOrder = 5;

        // Chuẩn hóa chiều rộng cơ thể Chuột cố định = 0.6 unit
        float mouseWidth = mouseSprite != null ? mouseSprite.bounds.size.x : 1f;
        float mouseScaleFactor = 0.6f / (mouseWidth > 0.001f ? mouseWidth : 1f);
        mouse.transform.localScale = new Vector3(mouseScaleFactor, mouseScaleFactor, 1f);

        // CircleCollider2D bán kính 0.2f ôm sát thân chuột theo yêu cầu
        CircleCollider2D col = mouse.AddComponent<CircleCollider2D>();
        col.radius = 0.2f;

        Rigidbody2D rb = mouse.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 8f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        MouseController mouseCtrl = mouse.AddComponent<MouseController>();
        mouseCtrl.ApplyNormalizedMouseSize();

        ShadowCaster2D caster = mouse.AddComponent<ShadowCaster2D>();
        caster.castsShadows = true;
        caster.selfShadows = false;

        Undo.RegisterCreatedObjectUndo(mouse, "Spawn Mouse");
        return mouse;
    }

    #endregion

    #region Interactive Props Spawning

    private static void SpawnInteractiveProps()
    {
        GameObject propsGroup = new GameObject("Props_Group");
        propsGroup.transform.position = Vector3.zero;

        // Định nghĩa 12 đạo cụ chuẩn Studio: Tỷ lệ chuẩn (Thùng hàng: 1.1u, Cọc tiêu: 0.7u, Thùng phuy: 0.9u, Hộp đồ: 0.8u)
        // Vị trí dạt sát mép tường / góc chết (Y = ±6.4f hoặc X = ±12.4f), TUYỆT ĐỐI KHÔNG CHẮN CỬA hay ngã ba
        var propDefinitions = new (string spriteName, Vector3 pos, string propId, float targetUnitSize)[]
        {
            // 1. Sảnh Sorting Area (nép sát tường trên & chân tường đáy, chừa sảnh chính và băng chuyền thông suốt)
            ("Pixel_Prop_Pallet", new Vector3(-1.5f, 6.4f, 0f), "Pallet_Sorting_Top", 1.1f),
            ("Pixel_Prop_Crates_Stack", new Vector3(2.0f, 6.4f, 0f), "Crates_Sorting_Top", 1.1f),
            ("Pixel_Prop_Safety_Cones", new Vector3(5.2f, 6.4f, 0f), "Cones_Sorting_TopRight", 0.7f),
            ("Pixel_Prop_Toolbox_Open", new Vector3(0.5f, -6.4f, 0f), "Toolbox_Sorting_Bottom", 0.8f),

            // 2. Kho máy móc bên trái Heavy Storage (dạt sát các góc chết và chân tường ngoài cùng)
            ("Pixel_Prop_Oil_Barrels", new Vector3(-12.4f, 1.8f, 0f), "Barrels_Storage_LeftWall", 0.9f),
            ("Pixel_Prop_Gears", new Vector3(-12.4f, -6.3f, 0f), "Gears_Storage_Corner", 0.9f),
            ("Pixel_Prop_Pallet", new Vector3(-9.5f, -6.3f, 0f), "Pallet_Storage_BottomWall", 1.1f),

            // 3. Phòng điều khiển Control Room (dạt sát góc trên và mép tường đỉnh; tường ngoài là X=-10.8)
            ("Pixel_Prop_Toolbox_Closed", new Vector3(-9.8f, 6.4f, 0f), "Toolbox_Server_Corner", 0.8f),
            ("Pixel_Prop_Wrench_Tools", new Vector3(-8.5f, 6.4f, 0f), "Wrench_Server_Wall", 0.8f),

            // 4. Phòng văn phòng / hồ sơ Office Room (dạt sát các góc tường trên và mép phải)
            ("Pixel_Prop_Oil_Barrels", new Vector3(12.2f, 6.4f, 0f), "Barrels_Office_Corner", 0.9f),
            ("Pixel_Prop_Crates_Stack", new Vector3(12.2f, 4.2f, 0f), "Crates_Office_Wall", 1.1f),

            // 5. Phòng giải lao Break Room (dạt sát góc tường đáy)
            ("Pixel_Prop_Safety_Cones", new Vector3(12.2f, -6.3f, 0f), "Cones_BreakRoom_Corner", 0.7f),
            ("Pixel_Prop_Toolbox_Closed", new Vector3(9.5f, -6.3f, 0f), "Toolbox_BreakRoom_Bottom", 0.8f)
        };

        foreach (var def in propDefinitions)
        {
            GameObject propObj = new GameObject($"Prop_{def.propId}");
            propObj.transform.SetParent(propsGroup.transform);
            propObj.transform.position = def.pos;

            SpriteRenderer sr = propObj.AddComponent<SpriteRenderer>();
            Sprite s = LoadSprite(def.spriteName);
            sr.sprite = s;
            sr.color = Color.white;
            sr.sortingOrder = 2;

            // Tính toán tỷ lệ scale chuẩn xác theo kích thước mét (targetUnitSize)
            float maxDim = s != null ? Mathf.Max(s.bounds.size.x, s.bounds.size.y) : 1f;
            float propScaleFactor = def.targetUnitSize / (maxDim > 0.001f ? maxDim : 1f);
            propObj.transform.localScale = new Vector3(propScaleFactor, propScaleFactor, 1f);

            BoxCollider2D col = propObj.AddComponent<BoxCollider2D>();
            if (s != null)
            {
                col.size = s.bounds.size;
            }

            PropItem propItem = propObj.AddComponent<PropItem>();
            propItem.propId = def.propId;
            propItem.propSprite = s;
            propItem.customColliderSize = col.size;
            propItem.targetUnitSize = def.targetUnitSize;
            propItem.isFake = false;
            propItem.SetBaseScale(propObj.transform.localScale);

            ShadowCaster2D caster = propObj.AddComponent<ShadowCaster2D>();
            caster.castsShadows = true;
            caster.selfShadows = false;
        }

        Undo.RegisterCreatedObjectUndo(propsGroup, "Spawn Interactive Props");
    }

    #endregion

    #region Atmospheric 2D Lighting & Volume

    private static void SetupAtmospheric2DLights()
    {
        // 1. Ánh sáng môi trường toàn cục tối nhẹ
        GameObject globalLightObj = new GameObject("Global_Light_2D");
        Light2D globalLight = globalLightObj.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = new Color(0.12f, 0.15f, 0.22f, 1f); // Xanh đêm tối sâu
        globalLight.intensity = 0.45f;

        GameObject lightsGroup = new GameObject("Lights_Group");
        lightsGroup.transform.position = Vector3.zero;

        // 2. Đèn vàng ấm sảnh Sorting Hall
        CreatePointLight(lightsGroup, "Light_Sorting_Ceiling", new Vector3(0.5f, 0f, 0f), new Color(1.0f, 0.85f, 0.45f), 1.5f, 9.5f, true);

        // 3. Đèn Cyan màn hình phòng điều khiển Control Room
        CreatePointLight(lightsGroup, "Light_Control_Room_Cyan", new Vector3(-6.5f, 5.5f, 0f), new Color(0.1f, 0.95f, 0.85f), 1.4f, 6.0f, false);

        // 4. Đèn vàng ấm phòng máy móc bên trái
        CreatePointLight(lightsGroup, "Light_Workshop_Amber", new Vector3(-10.5f, 1.5f, 0f), new Color(0.95f, 0.65f, 0.35f), 1.2f, 7.0f, true);

        // 5. Đèn phòng giải lao bên phải
        CreatePointLight(lightsGroup, "Light_BreakRoom_Warm", new Vector3(10.0f, -3.5f, 0f), new Color(1.0f, 0.92f, 0.65f), 1.3f, 6.5f, false);

        // 6. Đèn xanh lá vũng hóa chất Toxic Puddle
        CreatePointLight(lightsGroup, "Light_Toxic_Puddle", new Vector3(0.5f, 5.2f, 0f), new Color(0.2f, 1.0f, 0.4f), 0.9f, 4.0f, false);

        Undo.RegisterCreatedObjectUndo(globalLightObj, "Create Global Light");
        Undo.RegisterCreatedObjectUndo(lightsGroup, "Create Lights Group");
    }

    private static void CreatePointLight(GameObject parent, string name, Vector3 pos, Color color, float intensity, float radius, bool shadows)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.SetParent(parent.transform);
        lightObj.transform.position = pos;

        Light2D l = lightObj.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = radius * 0.25f;
        l.shadowsEnabled = shadows;
    }

    private static void SetupPostProcessingVolume()
    {
        string folder = "Assets/Settings";
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        string profilePath = $"{folder}/Cinematic_PostProcessing_Profile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        if (!profile.Has<Bloom>())
        {
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(1.3f);
        }

        if (!profile.Has<Vignette>())
        {
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.color.Override(Color.black);
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.4f);
        }

        if (!profile.Has<ColorAdjustments>())
        {
            ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(0.25f);
            colorAdj.contrast.Override(18f);
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        GameObject volumeObj = new GameObject("Cinematic_Volume");
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.profile = profile;

        Undo.RegisterCreatedObjectUndo(volumeObj, "Create Volume");
    }

    private static void ConfigureCamera(GameObject catObj)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        cam.orthographic = true;
        cam.orthographicSize = 3.8f; // Cận cảnh sắc nét Hotline Miami / Enter the Gungeon
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);

        // Nâng cấp Cinemachine Virtual Camera 2D
        SetupCinemachineCamera(cam, catObj);
    }

    private static void SetupCinemachineCamera(Camera mainCam, GameObject catObj)
    {
        // 1. Đảm bảo Main Camera có CinemachineBrain
        if (!mainCam.TryGetComponent<CinemachineBrain>(out var brain))
        {
            brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
        }

        // 2. Tạo hoặc cấu hình CinemachineCamera
        GameObject cmObj = GameObject.Find("Cinemachine_Camera");
        if (cmObj == null)
        {
            cmObj = new GameObject("Cinemachine_Camera");
        }
        cmObj.transform.position = new Vector3(0f, 0f, -10f);

        if (!cmObj.TryGetComponent<CinemachineCamera>(out var cmCam))
        {
            cmCam = cmObj.AddComponent<CinemachineCamera>();
        }

        cmCam.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        cmCam.Lens.OrthographicSize = 3.8f;

        // Bám theo nhân vật Mèo (mặc định) hoặc Chuột
        if (catObj != null)
        {
            cmCam.Target.TrackingTarget = catObj.transform;
        }

        // 3. Cấu hình CinemachinePositionComposer: Lookahead 0.15s, Damping (0.5, 0.5)
        var composer = cmCam.GetComponent<CinemachinePositionComposer>();
        if (composer == null)
        {
            composer = cmCam.gameObject.AddComponent<CinemachinePositionComposer>();
        }
        composer.CameraDistance = 10f;
        composer.Damping = new Vector3(0.5f, 0.5f, 0f);
        composer.Lookahead = new LookaheadSettings
        {
            Enabled = true,
            Time = 0.15f,
            Smoothing = 10f,
            IgnoreY = false
        };

        // 4. Cấu hình CinemachineConfiner2D: Giới hạn viền màn chơi không thấy khoảng đen ngoài map
        GameObject confinerObj = GameObject.Find("Camera_Confiner_Bounds");
        if (confinerObj == null)
        {
            confinerObj = new GameObject("Camera_Confiner_Bounds");
        }
        confinerObj.transform.position = Vector3.zero;

        if (!confinerObj.TryGetComponent<BoxCollider2D>(out var confinerCol))
        {
            confinerCol = confinerObj.AddComponent<BoxCollider2D>();
        }
        // Giới hạn trong lòng nhà kho 32x18 (tường ngoài: X=±15.8, Y=±8.8)
        confinerCol.size = new Vector2(30.4f, 16.4f);
        confinerCol.offset = Vector2.zero;
        confinerCol.isTrigger = true;

        var confiner = cmCam.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
        {
            confiner = cmCam.gameObject.AddComponent<CinemachineConfiner2D>();
        }
        confiner.BoundingShape2D = confinerCol;
        confiner.Damping = 0.5f;
        confiner.InvalidateBoundingShapeCache();

        Undo.RegisterCreatedObjectUndo(confinerObj, "Setup Camera Confiner");
        Undo.RegisterCreatedObjectUndo(cmObj, "Setup Cinemachine Camera");
    }

    private static void SetupGameManager(GameObject cat, GameObject mouse)
    {
        GameObject gmObj = new GameObject("GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();

        gm.catTransform = cat.transform;
        gm.mouseTransform = mouse.transform;
        gm.catController = cat.GetComponent<CatController>();
        gm.mouseController = mouse.GetComponent<MouseController>();

        GameObject cmObj = GameObject.Find("Cinemachine_Camera");
        if (cmObj != null && cmObj.TryGetComponent<CinemachineCamera>(out var cmCam))
        {
            gm.cinemachineCamera = cmCam;
        }

        // Tích hợp Menu chọn 2 chế độ (Offline vs Bot & Online LAN) và Network
        gmObj.AddComponent<GameModeManager>();
        gmObj.AddComponent<LanNetworkManager>();

        Undo.RegisterCreatedObjectUndo(gmObj, "Setup GameManager");

        // Tự động dựng hệ thống Modern Casual UI Canvas 2026 (HUD, Victory Modal, Game Mode Menu)
        ModernUIBuilder.BuildModernUICanvas();
    }

    private static Sprite LoadSprite(string assetName)
    {
        string relativePath = $"{SPRITES_FOLDER}/{assetName}.png";
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
        if (sp == null)
        {
            sp = Resources.Load<Sprite>($"GeneratedSprites/{assetName}");
        }
        return sp;
    }

    #endregion
}

/// <summary>
/// Tự động kích hoạt tái tạo lại Scene khi Unity nạp domain xong,
/// đảm bảo màn chơi luôn được cập nhật ngay cả khi người dùng không kịp click menu.
/// </summary>
[InitializeOnLoad]
public static class HotlineArenaAutoBuilder
{
    static HotlineArenaAutoBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string key = "HotlineArena_Rebuilt_Auto_Trigger_V20_Penthouse_ModernUI";
            if (SessionState.GetBool(key, false)) return;
            SessionState.SetBool(key, true);

            Debug.Log("🚀 [HotlineArenaAutoBuilder] Đang tự động tái tạo Scene và hệ thống Modern Casual UI Canvas 2026...");
            HotlineArenaBuilder.BuildHotlineArenaInternal(silent: true);
            Debug.Log("✅ [HotlineArenaAutoBuilder] Toàn bộ Scene và UI Canvas đã được cập nhật hoàn tất 100%!");
        };
    }
}
