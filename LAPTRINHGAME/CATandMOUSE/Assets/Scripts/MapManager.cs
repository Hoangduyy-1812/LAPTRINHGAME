using System;
using UnityEngine;

/// <summary>
/// Quản lý chuyển đổi giữa 2 Map:
/// - 🏢 Penthouse Hiện Đại (Penthouse Level Map)
/// - 📦 Nhà Kho Cũ (Warehouse Full Level Map)
/// Đồng bộ hoá Containers, Colliders, Props và cập nhật lộ trình Waypoint cho Bot Mèo.
/// </summary>
public class MapManager : MonoBehaviour
{
    public enum MapType
    {
        Penthouse,
        Warehouse
    }

    public static MapManager Instance { get; private set; }

    [Header("Map Containers")]
    [SerializeField] private GameObject penthouseContainer;
    [SerializeField] private GameObject warehouseContainer;

    [Header("Current Map Configuration")]
    [SerializeField] private MapType currentMap = MapType.Penthouse;

    public MapType CurrentMap => currentMap;
    public string CurrentMapName => currentMap == MapType.Penthouse ? "🏢 PENTHOUSE HIỆN ĐẠI" : "📦 NHÀ KHO CŨ";

    public event Action<MapType> OnMapChanged;

    public void InitializeContainers(GameObject penthouse, GameObject warehouse)
    {
        penthouseContainer = penthouse;
        warehouseContainer = warehouse;
    }

    private const string PREF_KEY_MAP = "CAT_MOUSE_SELECTED_MAP";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Đọc Map đã lưu hoặc mặc định là Penthouse
        string saved = PlayerPrefs.GetString(PREF_KEY_MAP, MapType.Penthouse.ToString());
        if (Enum.TryParse(saved, out MapType parsedMap))
        {
            currentMap = parsedMap;
        }
    }

    private void Start()
    {
        ApplyMap(currentMap, repositionCharacters: false);
    }

    /// <summary>
    /// Chuyển đổi giữa Penthouse và Warehouse
    /// </summary>
    public void SwitchMap(MapType mapType)
    {
        currentMap = mapType;
        PlayerPrefs.SetString(PREF_KEY_MAP, mapType.ToString());
        PlayerPrefs.Save();

        ApplyMap(mapType, repositionCharacters: true);
        OnMapChanged?.Invoke(mapType);
    }

    /// <summary>
    /// Đổi luân phiên giữa 2 map
    /// </summary>
    public void ToggleMap()
    {
        MapType next = (currentMap == MapType.Penthouse) ? MapType.Warehouse : MapType.Penthouse;
        SwitchMap(next);
    }

    private void ApplyMap(MapType mapType, bool repositionCharacters)
    {
        bool isPenthouse = (mapType == MapType.Penthouse);

        if (penthouseContainer != null) penthouseContainer.SetActive(isPenthouse);
        if (warehouseContainer != null) warehouseContainer.SetActive(!isPenthouse);

        // Cập nhật waypoints cho CatAIController
        CatAIController catAI = FindAnyObjectByType<CatAIController>();
        if (catAI != null)
        {
            catAI.SetMapWaypoints(mapType);
        }

        if (repositionCharacters)
        {
            RepositionCharactersForMap(mapType);
        }

        Debug.Log($"🗺️ [MapManager] Đã kích hoạt bản đồ: {(isPenthouse ? "🏢 Penthouse Hiện Đại" : "📦 Nhà Kho Cũ")}");
    }

    /// <summary>
    /// Đặt lại vị trí ban đầu hợp lý cho Mèo & Chuột theo từng bản đồ
    /// </summary>
    public void RepositionCharactersForMap(MapType mapType)
    {
        CatController cat = FindAnyObjectByType<CatController>();
        MouseController mouse = FindAnyObjectByType<MouseController>();

        if (mapType == MapType.Penthouse)
        {
            // Penthouse: Mèo ở phòng khách, Chuột ở trong bếp
            if (cat != null) cat.transform.position = new Vector3(0f, -4.5f, 0f);
            if (mouse != null) mouse.transform.position = new Vector3(-10.5f, 1.5f, 0f);
        }
        else
        {
            // Warehouse: Mèo ở Sorting Area, Chuột ở Heavy Storage
            if (cat != null) cat.transform.position = new Vector3(0f, -1.8f, 0f);
            if (mouse != null) mouse.transform.position = new Vector3(-10.5f, 3.5f, 0f);
        }
    }
}
