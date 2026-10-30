using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tự động sinh các Sprite UI hoạt hình nổi khối 3D (Chunky Toy / Casual 3D Style)
/// và các đạo cụ nội thất hiện đại cho Penthouse:
/// - Nút bấm viên thuốc 3D (Vàng cam, Xanh Cyan, Đỏ tươi, Tím Pastel).
/// - Thẻ kính mờ UI_Frosted_Glass_Card màu xanh navy đậm viền dạ quang cyan.
/// - Khung hội thoại UI_AmongUs_Panel viền trắng dày 8px bo tròn + viền đen ngoài 4px.
/// - Radar cảnh báo Mèo đến gần: UI_Proximity_Warning_Vignette.
/// - Bộ Props nội thất hiện đại: Sofa, Bàn cẩm thạch, Cây Monstera, Robot hút bụi, Ghế Scandinavian, Đèn, Ghế bar.
/// </summary>
public static class ProceduralUISpriteGenerator
{
    private const string UI_FOLDER_RELATIVE = "Assets/Resources/GeneratedSprites/UI";
    private const string ROOT_FOLDER_RELATIVE = "Assets/Resources/GeneratedSprites";

    private static readonly Color DarkBlackBorder = new Color(0.07f, 0.08f, 0.10f, 1f); // #11151A

    // 1. Màu Vàng Cam Among Us
    private static readonly Color YellowTop = new Color(1.0f, 0.667f, 0.0f, 1f);     // #FFAA00
    private static readonly Color YellowBottom = new Color(0.831f, 0.478f, 0.0f, 1f); // #D47A00
    private static readonly Color YellowShine = new Color(1.0f, 0.878f, 0.51f, 1f);

    // 2. Màu Xanh Cyan Among Us
    private static readonly Color CyanTop = new Color(0.0f, 0.824f, 0.827f, 1f);     // #00D2D3
    private static readonly Color CyanBottom = new Color(0.0f, 0.60f, 0.60f, 1f);     // #009999
    private static readonly Color CyanShine = new Color(0.50f, 0.96f, 0.96f, 1f);

    // 3. Màu Đỏ Tươi Among Us
    private static readonly Color RedTop = new Color(1.0f, 0.278f, 0.341f, 1f);      // #FF4757
    private static readonly Color RedBottom = new Color(0.761f, 0.165f, 0.220f, 1f); // #C22A38
    private static readonly Color RedShine = new Color(1.0f, 0.627f, 0.659f, 1f);

    // 4. Màu Tím Pastel (Settings & Map Selector)
    private static readonly Color PurpleTop = new Color(0.659f, 0.333f, 0.969f, 1f);    // #A855F7
    private static readonly Color PurpleBottom = new Color(0.494f, 0.133f, 0.808f, 1f); // #7E22CE
    private static readonly Color PurpleShine = new Color(0.847f, 0.706f, 0.996f, 1f);  // #D8B4FE

    // 5. Màu Xám Slate (Dark Circle)
    private static readonly Color SlateTop = new Color(0.184f, 0.208f, 0.259f, 1f);  // #2F3542
    private static readonly Color SlateBottom = new Color(0.118f, 0.133f, 0.161f, 1f);// #1E2229
    private static readonly Color SlateShine = new Color(0.341f, 0.376f, 0.435f, 1f);

    public static void GenerateAllUISprites()
    {
        string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        string uiDir = Path.Combine(projectPath, UI_FOLDER_RELATIVE);
        string rootDir = Path.Combine(projectPath, ROOT_FOLDER_RELATIVE);

        if (!Directory.Exists(uiDir)) Directory.CreateDirectory(uiDir);
        if (!Directory.Exists(rootDir)) Directory.CreateDirectory(rootDir);

        string[] targetDirs = new string[] { uiDir, rootDir };

        foreach (string dir in targetDirs)
        {
            // 1. Các nút bấm viên thuốc Among Us 3D (320x140)
            GenerateAmongUsButton(Path.Combine(dir, "UI_AmongUs_Button_Yellow.png"), YellowTop, YellowBottom, YellowShine);
            GenerateAmongUsButton(Path.Combine(dir, "UI_AmongUs_Button.png"), YellowTop, YellowBottom, YellowShine);
            GenerateAmongUsButton(Path.Combine(dir, "UI_AmongUs_Button_Cyan.png"), CyanTop, CyanBottom, CyanShine);
            GenerateAmongUsButton(Path.Combine(dir, "UI_AmongUs_Button_Red.png"), RedTop, RedBottom, RedShine);
            GenerateAmongUsButton(Path.Combine(dir, "UI_AmongUs_Button_Purple.png"), PurpleTop, PurpleBottom, PurpleShine);
            GenerateAmongUsButton(Path.Combine(dir, "UI_Button_Pill.png"), YellowTop, YellowBottom, YellowShine);

            // 2. Hộp thoại Among Us & Thẻ kính mờ Frosted Glass
            GenerateAmongUsPanel(Path.Combine(dir, "UI_AmongUs_Panel.png"));
            GenerateAmongUsPanel(Path.Combine(dir, "UI_Rounded_Panel.png"));
            GenerateFrostedGlassCard(Path.Combine(dir, "UI_Frosted_Glass_Card.png"));
            GenerateFrostedGlassCard(Path.Combine(dir, "UI_Frosted_Card.png"));

            // 3. Nút tròn kỹ năng to gấp 2 lần (256x256)
            GenerateAmongUsCircle(Path.Combine(dir, "UI_AmongUs_Circle_Cyan.png"), CyanTop, CyanBottom, CyanShine);
            GenerateAmongUsCircle(Path.Combine(dir, "UI_AmongUs_Circle_Red.png"), RedTop, RedBottom, RedShine);
            GenerateAmongUsCircle(Path.Combine(dir, "UI_AmongUs_Circle_Purple.png"), PurpleTop, PurpleBottom, PurpleShine);
            GenerateAmongUsCircle(Path.Combine(dir, "UI_AmongUs_Circle_Dark.png"), SlateTop, SlateBottom, SlateShine);
            GenerateAmongUsCircle(Path.Combine(dir, "UI_Skill_Circle.png"), CyanTop, CyanBottom, CyanShine);

            // 4. Icon bánh răng Cài đặt [⚙]
            GenerateAmongUsGear(Path.Combine(dir, "UI_AmongUs_Gear.png"));

            // 5. Khung đếm ngược Timer Pill
            GenerateAmongUsTimerPill(Path.Combine(dir, "UI_AmongUs_Timer_Pill.png"));

            // 6. Vòng tròn trắng Cooldown Radial Fill
            GenerateCircleFill(Path.Combine(dir, "UI_Circle_Fill.png"));

            // 7. Radar cảnh báo Mèo đến gần: Vignette đỏ nhấp nháy
            GenerateProximityWarningVignette(Path.Combine(dir, "UI_Proximity_Warning_Vignette.png"));

            // 8. Các đạo cụ nội thất hiện đại cho Penthouse
            GenerateModernSofa(Path.Combine(dir, "Prop_Modern_Sofa.png"));
            GenerateMarbleTable(Path.Combine(dir, "Prop_Marble_Table.png"));
            GenerateMonsteraPlant(Path.Combine(dir, "Prop_Monstera_Plant.png"));
            GenerateRobotVacuum(Path.Combine(dir, "Prop_Robot_Vacuum.png"));
            GenerateModernChair(Path.Combine(dir, "Prop_Modern_Chair.png"));
            GenerateModernLamp(Path.Combine(dir, "Prop_Modern_Lamp.png"));
            GenerateKitchenBarstool(Path.Combine(dir, "Prop_Kitchen_Barstool.png"));
        }

        AssetDatabase.Refresh();

        // Cấu hình SpriteImporter
        string[] relativeDirs = new string[] { UI_FOLDER_RELATIVE, ROOT_FOLDER_RELATIVE };
        Vector4 buttonBorder = new Vector4(52, 52, 52, 52);
        Vector4 panelBorder = new Vector4(48, 48, 48, 48);
        Vector4 timerBorder = new Vector4(42, 42, 42, 42);

        foreach (string relDir in relativeDirs)
        {
            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Button_Yellow.png", buttonBorder);
            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Button.png", buttonBorder);
            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Button_Cyan.png", buttonBorder);
            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Button_Red.png", buttonBorder);
            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Button_Purple.png", buttonBorder);
            ConfigureSpriteImporter($"{relDir}/UI_Button_Pill.png", buttonBorder);

            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Panel.png", panelBorder);
            ConfigureSpriteImporter($"{relDir}/UI_Rounded_Panel.png", panelBorder);
            ConfigureSpriteImporter($"{relDir}/UI_Frosted_Glass_Card.png", panelBorder);
            ConfigureSpriteImporter($"{relDir}/UI_Frosted_Card.png", panelBorder);

            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Circle_Cyan.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Circle_Red.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Circle_Purple.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Circle_Dark.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/UI_Skill_Circle.png", Vector4.zero);

            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Gear.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/UI_AmongUs_Timer_Pill.png", timerBorder);
            ConfigureSpriteImporter($"{relDir}/UI_Circle_Fill.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/UI_Proximity_Warning_Vignette.png", Vector4.zero);

            // Props Sprites
            ConfigureSpriteImporter($"{relDir}/Prop_Modern_Sofa.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/Prop_Marble_Table.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/Prop_Monstera_Plant.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/Prop_Robot_Vacuum.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/Prop_Modern_Chair.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/Prop_Modern_Lamp.png", Vector4.zero);
            ConfigureSpriteImporter($"{relDir}/Prop_Kitchen_Barstool.png", Vector4.zero);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("🎨 [ProceduralUISpriteGenerator] Đã tạo thành công bộ Sprite UI 3D và Props Penthouse hiện đại!");
    }

    private static void ConfigureSpriteImporter(string relativePath, Vector4 border)
    {
        AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }
    }

    private static void GenerateAmongUsButton(string filePath, Color topColor, Color bottomColor, Color shineColor)
    {
        int width = 320;
        int height = 140;
        float radius = 50f;
        float borderWidth = 6f;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        Vector2 halfExtents = new Vector2((width - 4) * 0.5f, (height - 4) * 0.5f);
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);

        for (int y = 0; y < height; y++)
        {
            float tY = (float)y / height;

            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = SignedDistanceBox(p, halfExtents, radius);

                if (d > 0.5f)
                {
                    pixels[y * width + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(0.5f - d);

                    if (d > -borderWidth)
                    {
                        pixels[y * width + x] = new Color(DarkBlackBorder.r, DarkBlackBorder.g, DarkBlackBorder.b, alpha);
                    }
                    else
                    {
                        Color col;
                        if (tY < 0.27f)
                        {
                            col = bottomColor;
                        }
                        else if (tY < 0.29f)
                        {
                            float blend = (tY - 0.27f) / 0.02f;
                            col = Color.Lerp(bottomColor, topColor, blend);
                        }
                        else if (tY > 0.86f)
                        {
                            float shineBlend = (tY - 0.86f) / 0.14f;
                            col = Color.Lerp(topColor, shineColor, shineBlend * 0.5f);
                        }
                        else
                        {
                            col = topColor;
                        }

                        col.a = alpha;
                        pixels[y * width + x] = col;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateAmongUsPanel(string filePath)
    {
        int width = 256;
        int height = 256;
        float radius = 48f;
        float outerBlackWidth = 4f;
        float whiteBorderWidth = 8f;

        Color outerBlack = new Color(0.05f, 0.07f, 0.09f, 1f);
        Color whiteBorder = Color.white;
        Color bgSlate = new Color(0.118f, 0.153f, 0.180f, 0.96f);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        Vector2 halfExtents = new Vector2((width - 4) * 0.5f, (height - 4) * 0.5f);
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = SignedDistanceBox(p, halfExtents, radius);

                if (d > 0.5f)
                {
                    pixels[y * width + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(0.5f - d);

                    if (d > -outerBlackWidth)
                    {
                        pixels[y * width + x] = new Color(outerBlack.r, outerBlack.g, outerBlack.b, alpha);
                    }
                    else if (d > -(outerBlackWidth + whiteBorderWidth))
                    {
                        pixels[y * width + x] = new Color(whiteBorder.r, whiteBorder.g, whiteBorder.b, alpha);
                    }
                    else
                    {
                        pixels[y * width + x] = new Color(bgSlate.r, bgSlate.g, bgSlate.b, bgSlate.a * alpha);
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    /// <summary>
    /// Thẻ kính mờ Frosted Glass Card: Xanh Navy đậm (#0F172A), viền dạ quang Cyan/Trắng bo tròn mịn màng
    /// </summary>
    private static void GenerateFrostedGlassCard(string filePath)
    {
        int width = 256;
        int height = 256;
        float radius = 52f;
        float glowBorderWidth = 5f;

        Color glowCyan = new Color(0.22f, 0.74f, 0.97f, 1f); // #38BDF8
        Color bgNavy = new Color(0.059f, 0.090f, 0.165f, 0.95f); // #0F172A
        Color innerCard = new Color(0.09f, 0.13f, 0.22f, 0.95f);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        Vector2 halfExtents = new Vector2((width - 4) * 0.5f, (height - 4) * 0.5f);
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);

        for (int y = 0; y < height; y++)
        {
            float tY = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = SignedDistanceBox(p, halfExtents, radius);

                if (d > 0.5f)
                {
                    pixels[y * width + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(0.5f - d);

                    if (d > -glowBorderWidth)
                    {
                        // Viền dạ quang sáng nổi bật
                        float rimBlend = Mathf.Clamp01((-d) / glowBorderWidth);
                        Color rimCol = Color.Lerp(glowCyan, Color.white, 0.4f * (1f - rimBlend));
                        pixels[y * width + x] = new Color(rimCol.r, rimCol.g, rimCol.b, alpha);
                    }
                    else
                    {
                        // Ruột thẻ kính mờ có gradient nhẹ
                        Color cardCol = Color.Lerp(bgNavy, innerCard, tY);
                        pixels[y * width + x] = new Color(cardCol.r, cardCol.g, cardCol.b, cardCol.a * alpha);
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateAmongUsCircle(string filePath, Color topColor, Color bottomColor, Color shineColor)
    {
        int size = 256;
        float radius = 118f;
        float borderWidth = 6f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            float tY = (float)y / size;

            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float dist = p.magnitude - radius;

                if (dist > 0.5f)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(0.5f - dist);

                    if (dist > -borderWidth)
                    {
                        pixels[y * size + x] = new Color(DarkBlackBorder.r, DarkBlackBorder.g, DarkBlackBorder.b, alpha);
                    }
                    else
                    {
                        Color col;
                        if (tY < 0.27f)
                        {
                            col = bottomColor;
                        }
                        else if (tY < 0.29f)
                        {
                            float blend = (tY - 0.27f) / 0.02f;
                            col = Color.Lerp(bottomColor, topColor, blend);
                        }
                        else if (tY > 0.86f)
                        {
                            float shineBlend = (tY - 0.86f) / 0.14f;
                            col = Color.Lerp(topColor, shineColor, shineBlend * 0.5f);
                        }
                        else
                        {
                            col = topColor;
                        }

                        col.a = alpha;
                        pixels[y * size + x] = col;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateAmongUsGear(string filePath)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        float hubRadius = 42f;
        float holeRadius = 16f;
        int teethCount = 6;
        float toothHalfAngle = Mathf.PI / teethCount * 0.52f;
        float toothHeight = 14f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float r = p.magnitude;
                float angle = Mathf.Atan2(p.y, p.x);
                if (angle < 0f) angle += 2f * Mathf.PI;

                float segment = (2f * Mathf.PI) / teethCount;
                float modAngle = Mathf.Abs(Mathf.Repeat(angle + segment * 0.5f, segment) - segment * 0.5f);

                float targetMaxR = hubRadius;
                if (modAngle < toothHalfAngle) targetMaxR += toothHeight;

                if (r > targetMaxR + 0.5f || r < holeRadius - 0.5f)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else
                {
                    float alpha = 1f;
                    if (r > targetMaxR - 0.5f) alpha = Mathf.Clamp01(targetMaxR + 0.5f - r);
                    if (r < holeRadius + 0.5f) alpha = Mathf.Min(alpha, Mathf.Clamp01(r - (holeRadius - 0.5f)));

                    bool isBorder = (r > targetMaxR - 4f) || (r < holeRadius + 4f) || (modAngle > toothHalfAngle - 0.08f && r > hubRadius);

                    if (isBorder)
                    {
                        pixels[y * size + x] = new Color(DarkBlackBorder.r, DarkBlackBorder.g, DarkBlackBorder.b, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateAmongUsTimerPill(string filePath)
    {
        int width = 240;
        int height = 80;
        float radius = 38f;
        float borderWidth = 5f;

        Color borderCol = new Color(0.06f, 0.07f, 0.09f, 1f);
        Color bgNavy = new Color(0.08f, 0.11f, 0.16f, 0.94f);

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        Vector2 halfExtents = new Vector2((width - 4) * 0.5f, (height - 4) * 0.5f);
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = SignedDistanceBox(p, halfExtents, radius);

                if (d > 0.5f)
                {
                    pixels[y * width + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(0.5f - d);

                    if (d > -borderWidth)
                    {
                        pixels[y * width + x] = new Color(borderCol.r, borderCol.g, borderCol.b, alpha);
                    }
                    else
                    {
                        pixels[y * width + x] = new Color(bgNavy.r, bgNavy.g, bgNavy.b, bgNavy.a * alpha);
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateCircleFill(string filePath)
    {
        int size = 256;
        float radius = 122f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float dist = p.magnitude - radius;

                if (dist > 0.5f)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    /// <summary>
    /// Radar cảnh báo Mèo đến gần: Vignette 4 góc màn hình đỏ huỳnh quang
    /// </summary>
    private static void GenerateProximityWarningVignette(string filePath)
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        Color redGlow = new Color(0.95f, 0.15f, 0.15f, 1f); // #F43F5E

        for (int y = 0; y < size; y++)
        {
            float normY = ((float)y / size) * 2f - 1f;
            for (int x = 0; x < size; x++)
            {
                float normX = ((float)x / size) * 2f - 1f;
                float distFromCenter = Mathf.Sqrt(normX * normX + normY * normY);

                // Trong tâm trong suốt, góc ngoài cùng đỏ đậm
                float intensity = Mathf.Clamp01((distFromCenter - 0.45f) / 0.55f);
                intensity = Mathf.SmoothStep(0f, 1f, intensity);

                pixels[y * size + x] = new Color(redGlow.r, redGlow.g, redGlow.b, intensity * 0.85f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    // ==========================================
    // CÁC ĐẠO CỤ HIỆN ĐẠI CHO PENTHOUSE (PROPS)
    // ==========================================

    private static void GenerateModernSofa(string filePath)
    {
        int width = 256;
        int height = 140;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        Color sofaCream = new Color(0.93f, 0.90f, 0.84f, 1f);
        Color sofaShadow = new Color(0.80f, 0.76f, 0.70f, 1f);
        Color pillowTeal = new Color(0.12f, 0.65f, 0.65f, 1f);
        Color pillowCoral = new Color(0.96f, 0.45f, 0.38f, 1f);

        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
        Vector2 halfExtents = new Vector2((width - 16) * 0.5f, (height - 16) * 0.5f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = SignedDistanceBox(p, halfExtents, 30f);

                if (d > 0.5f)
                {
                    pixels[y * width + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (d > -4f)
                    {
                        pixels[y * width + x] = new Color(DarkBlackBorder.r, DarkBlackBorder.g, DarkBlackBorder.b, alpha);
                    }
                    else
                    {
                        // Kiểm tra gối tựa hai bên
                        bool isLeftPillow = (x > 30 && x < 70 && y > 35 && y < 105);
                        bool isRightPillow = (x > width - 70 && x < width - 30 && y > 35 && y < 105);

                        if (isLeftPillow)
                        {
                            pixels[y * width + x] = new Color(pillowTeal.r, pillowTeal.g, pillowTeal.b, alpha);
                        }
                        else if (isRightPillow)
                        {
                            pixels[y * width + x] = new Color(pillowCoral.r, pillowCoral.g, pillowCoral.b, alpha);
                        }
                        else
                        {
                            Color baseC = (y < height * 0.35f) ? sofaShadow : sofaCream;
                            pixels[y * width + x] = new Color(baseC.r, baseC.g, baseC.b, alpha);
                        }
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateMarbleTable(string filePath)
    {
        int size = 180;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = 80f;

        Color marbleWhite = new Color(0.96f, 0.97f, 0.98f, 1f);
        Color marbleVein = new Color(0.78f, 0.82f, 0.86f, 1f);
        Color goldRim = new Color(0.88f, 0.72f, 0.32f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = p.magnitude - radius;

                if (d > 0.5f)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (d > -4f)
                    {
                        pixels[y * size + x] = new Color(DarkBlackBorder.r, DarkBlackBorder.g, DarkBlackBorder.b, alpha);
                    }
                    else if (d > -9f)
                    {
                        pixels[y * size + x] = new Color(goldRim.r, goldRim.g, goldRim.b, alpha);
                    }
                    else
                    {
                        // Vân đá marble dạng sóng sin
                        float wave = Mathf.Sin(x * 0.08f + y * 0.05f) + Mathf.Cos(x * 0.04f - y * 0.06f);
                        Color c = (wave > 1.2f) ? marbleVein : marbleWhite;
                        pixels[y * size + x] = new Color(c.r, c.g, c.b, alpha);
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateMonsteraPlant(string filePath)
    {
        int size = 180;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        Color potWhite = new Color(0.92f, 0.94f, 0.96f, 1f);
        Color leafGreenDark = new Color(0.08f, 0.45f, 0.22f, 1f);
        Color leafGreenLight = new Color(0.18f, 0.68f, 0.34f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float r = p.magnitude;
                float angle = Mathf.Atan2(p.y, p.x);

                // Tán lá monstera gồm 6 nhánh xòe
                float leafPetals = Mathf.Sin(angle * 5f) * 22f;
                float maxR = 60f + leafPetals;

                if (r > maxR + 0.5f)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(maxR + 0.5f - r);

                    if (r > maxR - 4f)
                    {
                        pixels[y * size + x] = new Color(DarkBlackBorder.r, DarkBlackBorder.g, DarkBlackBorder.b, alpha);
                    }
                    else if (r < 22f)
                    {
                        // Chậu sứ tròn ở tâm
                        pixels[y * size + x] = new Color(potWhite.r, potWhite.g, potWhite.b, alpha);
                    }
                    else
                    {
                        Color leafC = (Mathf.Sin(angle * 10f) > 0f) ? leafGreenLight : leafGreenDark;
                        pixels[y * size + x] = new Color(leafC.r, leafC.g, leafC.b, alpha);
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateRobotVacuum(string filePath)
    {
        int size = 140;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = 58f;

        Color bodyGray = new Color(0.20f, 0.22f, 0.26f, 1f);
        Color cyanLed = new Color(0.0f, 0.85f, 0.95f, 1f);
        Color buttonWhite = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = p.magnitude - radius;

                if (d > 0.5f)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (d > -4f)
                    {
                        pixels[y * size + x] = new Color(DarkBlackBorder.r, DarkBlackBorder.g, DarkBlackBorder.b, alpha);
                    }
                    else if (p.magnitude > 42f && p.magnitude < 48f)
                    {
                        // Vòng đèn LED cyan chạy quanh máy
                        pixels[y * size + x] = new Color(cyanLed.r, cyanLed.g, cyanLed.b, alpha);
                    }
                    else if (p.magnitude < 12f)
                    {
                        // Nút nguồn tròn ở giữa
                        pixels[y * size + x] = new Color(buttonWhite.r, buttonWhite.g, buttonWhite.b, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = new Color(bodyGray.r, bodyGray.g, bodyGray.b, alpha);
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateModernChair(string filePath)
    {
        int size = 140;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        Vector2 halfExtents = new Vector2(50f, 50f);

        Color chairTeal = new Color(0.0f, 0.65f, 0.68f, 1f);
        Color chairShadow = new Color(0.0f, 0.48f, 0.50f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = SignedDistanceBox(p, halfExtents, 22f);

                if (d > 0.5f)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (d > -4f)
                    {
                        pixels[y * size + x] = new Color(DarkBlackBorder.r, DarkBlackBorder.g, DarkBlackBorder.b, alpha);
                    }
                    else
                    {
                        Color c = (y < size * 0.35f) ? chairShadow : chairTeal;
                        pixels[y * size + x] = new Color(c.r, c.g, c.b, alpha);
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateModernLamp(string filePath)
    {
        int width = 120;
        int height = 160;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);

        Color goldPole = new Color(0.85f, 0.70f, 0.30f, 1f);
        Color shadeWarm = new Color(1.0f, 0.92f, 0.70f, 1f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;

                // Nón chụp đèn tròn ở trên
                bool isShade = (y > height * 0.4f && Mathf.Abs(p.x) < (height - y) * 0.65f);
                bool isPole = (y <= height * 0.4f && Mathf.Abs(p.x) < 5f);
                bool isBase = (y < 20 && Mathf.Abs(p.x) < 28f);

                if (isShade)
                {
                    pixels[y * width + x] = shadeWarm;
                }
                else if (isPole || isBase)
                {
                    pixels[y * width + x] = goldPole;
                }
                else
                {
                    pixels[y * width + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateKitchenBarstool(string filePath)
    {
        int size = 120;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = 48f;

        Color leatherBrown = new Color(0.68f, 0.42f, 0.22f, 1f);
        Color stitchYellow = new Color(0.92f, 0.78f, 0.45f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = p.magnitude - radius;

                if (d > 0.5f)
                {
                    pixels[y * size + x] = Color.clear;
                }
                else
                {
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (d > -4f)
                    {
                        pixels[y * size + x] = new Color(DarkBlackBorder.r, DarkBlackBorder.g, DarkBlackBorder.b, alpha);
                    }
                    else if (p.magnitude > 36f && p.magnitude < 40f)
                    {
                        pixels[y * size + x] = new Color(stitchYellow.r, stitchYellow.g, stitchYellow.b, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = new Color(leatherBrown.r, leatherBrown.g, leatherBrown.b, alpha);
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static float SignedDistanceBox(Vector2 p, Vector2 b, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (b - Vector2.one * r);
        Vector2 maxQ = Vector2.Max(q, Vector2.zero);
        return maxQ.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }
}
