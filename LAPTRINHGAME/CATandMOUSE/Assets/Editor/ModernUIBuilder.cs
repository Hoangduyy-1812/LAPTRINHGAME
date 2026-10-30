using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Editor Tool tự động dựng toàn bộ hệ thống giao diện UGUI phong cách Hoạt hình Chunky Among Us / Brawl Stars 3D:
/// - Thẻ kính mờ UI_Frosted_Glass_Card màu xanh navy đậm (#0F172A) viền dạ quang phát sáng.
/// - 3 Nút bấm viên thuốc 3D (Vàng Cam, Cyan, Tím Pastel) kèm hiệu ứng nhún phím cơ UIButtonJuice.
/// - Map Selector: Chuyển đổi linh hoạt Penthouse Hiện Đại ⇄ Nhà Kho Cũ.
/// - Radar Mèo đến gần (Proximity Heartbeat Indicator) với vignette đỏ nhấp nháy cảnh báo.
/// - Đồng hồ đếm ngược viền đen dày, dưới 30s tự động chuyển đỏ và đập nhịp tim dồn dập.
/// - Victory Modal bóng bẩy trượt từ trên xuống với Ease.OutBack.
/// </summary>
public static class ModernUIBuilder
{
    private const string SPRITES_UI_PATH = "GeneratedSprites/UI";
    private const string SPRITES_ROOT_PATH = "GeneratedSprites";

    public static GameObject BuildModernUICanvas()
    {
        // 1. Tự động sinh và cấu hình bộ Sprite UI 3D và Props
        ProceduralUISpriteGenerator.GenerateAllUISprites();

        // 2. Dọn dẹp Canvas và EventSystem cũ nếu đã tồn tại
        CleanUpOldUI();

        // 3. Tạo EventSystem
        EnsureEventSystem();

        // 4. Tạo Canvas gốc
        GameObject canvasObj = new GameObject("Canvas_ModernUI");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        UIManager uiMgr = canvasObj.AddComponent<UIManager>();

        // Tải các Sprite Among Us 3D đã sinh
        Sprite spBtnYellow = LoadUISprite("UI_AmongUs_Button_Yellow");
        Sprite spBtnCyan = LoadUISprite("UI_AmongUs_Button_Cyan");
        Sprite spBtnRed = LoadUISprite("UI_AmongUs_Button_Red");
        Sprite spBtnPurple = LoadUISprite("UI_AmongUs_Button_Purple");
        Sprite spPanel = LoadUISprite("UI_AmongUs_Panel");
        Sprite spFrostedCard = LoadUISprite("UI_Frosted_Glass_Card");
        Sprite spCircleCyan = LoadUISprite("UI_AmongUs_Circle_Cyan");
        Sprite spCircleRed = LoadUISprite("UI_AmongUs_Circle_Red");
        Sprite spCircleDark = LoadUISprite("UI_AmongUs_Circle_Dark");
        Sprite spGear = LoadUISprite("UI_AmongUs_Gear");
        Sprite spTimerPill = LoadUISprite("UI_AmongUs_Timer_Pill");
        Sprite spCircleFill = LoadUISprite("UI_Circle_Fill");
        Sprite spVignette = LoadUISprite("UI_Proximity_Warning_Vignette");
        Font defaultFont = UIManager.GetDefaultUIFont();

        // 5. Dựng InGameHUDView với Radar cảnh báo Mèo đến gần
        InGameHUDView hudView = BuildHUDView(canvasObj.transform, spTimerPill, spPanel, spCircleCyan, spCircleRed, spCircleDark, spGear, spCircleFill, spBtnRed, spVignette, defaultFont);

        // 6. Dựng VictoryModalView phong cách Among Us
        VictoryModalView victoryModal = BuildVictoryModalView(canvasObj.transform, spPanel, spBtnYellow, spBtnCyan, defaultFont);

        // 7. Dựng GameModeMenuView với thẻ kính mờ Frosted Glass và Map Selector
        GameModeMenuView modeMenu = BuildGameModeMenuView(canvasObj.transform, spFrostedCard, spPanel, spBtnYellow, spBtnCyan, spBtnPurple, spBtnRed, defaultFont);

        // 8. Liên kết các View vào UIManager
        SetPrivateField(uiMgr, "hudView", hudView);
        SetPrivateField(uiMgr, "victoryModalView", victoryModal);
        SetPrivateField(uiMgr, "modeMenuView", modeMenu);

        victoryModal.gameObject.SetActive(false);

        Undo.RegisterCreatedObjectUndo(canvasObj, "Build Modern UI Canvas");
        Debug.Log("🖥️ [ModernUIBuilder] Đã khởi tạo hoàn chỉnh hệ thống UI Cyber-Casual 2026 với Radar và Map Selector!");

        return canvasObj;
    }

    private static void CleanUpOldUI()
    {
        string[] oldNames = new string[] { "Canvas_ModernUI", "InGame_HUD", "Canvas", "EventSystem" };
        foreach (string name in oldNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }
    }

    private static void EnsureEventSystem()
    {
        GameObject esObj = GameObject.Find("EventSystem");
        if (esObj == null)
        {
            esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }
    }

    private static Sprite LoadUISprite(string spriteName)
    {
        string assetPath = $"Assets/Resources/{SPRITES_UI_PATH}/{spriteName}.png";
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sp == null)
        {
            assetPath = $"Assets/Resources/{SPRITES_ROOT_PATH}/{spriteName}.png";
            sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
        if (sp == null)
        {
            sp = Resources.Load<Sprite>($"{SPRITES_UI_PATH}/{spriteName}");
        }
        if (sp == null)
        {
            sp = Resources.Load<Sprite>($"{SPRITES_ROOT_PATH}/{spriteName}");
        }
        return sp;
    }

    #region InGame HUD View Builder

    private static InGameHUDView BuildHUDView(
        Transform parent,
        Sprite spTimerPill,
        Sprite spPanel,
        Sprite spCircleCyan,
        Sprite spCircleRed,
        Sprite spCircleDark,
        Sprite spGear,
        Sprite spCircleFill,
        Sprite spBtnRed,
        Sprite spVignette,
        Font font)
    {
        GameObject hudObj = CreateUIObject("InGame_HUD", parent);
        SetStretchFill(hudObj.GetComponent<RectTransform>());
        InGameHUDView hudView = hudObj.AddComponent<InGameHUDView>();

        // --- Proximity Warning Vignette Overlay (Full Screen) ---
        if (spVignette != null)
        {
            GameObject vigObj = CreateUIObject("Proximity_Vignette", hudObj.transform);
            vigObj.transform.SetAsFirstSibling();
            RectTransform vigRt = vigObj.GetComponent<RectTransform>();
            SetStretchFill(vigRt);
            Image vigImg = vigObj.AddComponent<Image>();
            vigImg.sprite = spVignette;
            vigImg.color = new Color(1f, 1f, 1f, 0f);
            vigImg.raycastTarget = false;
            vigObj.SetActive(false);
            SetPrivateField(hudView, "proximityVignetteOverlay", vigImg);
        }

        // --- 1. Top Center Countdown Timer ---
        GameObject timerObj = CreateUIObject("Timer_Container", hudObj.transform);
        RectTransform timerRt = timerObj.GetComponent<RectTransform>();
        timerRt.anchorMin = new Vector2(0.5f, 1f);
        timerRt.anchorMax = new Vector2(0.5f, 1f);
        timerRt.pivot = new Vector2(0.5f, 1f);
        timerRt.anchoredPosition = new Vector2(0f, -25f);
        timerRt.sizeDelta = new Vector2(250f, 75f);

        Image timerBg = timerObj.AddComponent<Image>();
        timerBg.sprite = spTimerPill;
        timerBg.type = Image.Type.Sliced;
        timerBg.color = Color.white;

        GameObject timerTextObj = CreateUIObject("Timer_Text", timerObj.transform);
        RectTransform timerTextRt = timerTextObj.GetComponent<RectTransform>();
        SetStretchFill(timerTextRt);
        Text timerText = timerTextObj.AddComponent<Text>();
        timerText.font = font;
        timerText.text = "05:00";
        timerText.fontSize = 36;
        timerText.fontStyle = FontStyle.Bold;
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.color = Color.white;
        AddChunkyOutlineAndShadow(timerTextObj, 2.5f, 4f);

        SetPrivateField(hudView, "timerContainer", timerRt);
        SetPrivateField(hudView, "timerBg", timerBg);
        SetPrivateField(hudView, "timerText", timerText);

        // --- Proximity Warning Badge (Top Center, dưới Timer) ---
        GameObject warnBadgeObj = CreateUIObject("Proximity_Warning_Badge", hudObj.transform);
        RectTransform warnBadgeRt = warnBadgeObj.GetComponent<RectTransform>();
        warnBadgeRt.anchorMin = new Vector2(0.5f, 1f);
        warnBadgeRt.anchorMax = new Vector2(0.5f, 1f);
        warnBadgeRt.pivot = new Vector2(0.5f, 1f);
        warnBadgeRt.anchoredPosition = new Vector2(0f, -112f);
        warnBadgeRt.sizeDelta = new Vector2(360f, 46f);

        Image warnBadgeBg = warnBadgeObj.AddComponent<Image>();
        warnBadgeBg.sprite = spTimerPill;
        warnBadgeBg.type = Image.Type.Sliced;
        warnBadgeBg.color = new Color(0.95f, 0.15f, 0.15f, 0.95f);

        GameObject warnBadgeTextObj = CreateUIObject("Warning_Text", warnBadgeObj.transform);
        RectTransform warnTextRt = warnBadgeTextObj.GetComponent<RectTransform>();
        SetStretchFill(warnTextRt);
        Text warnText = warnBadgeTextObj.AddComponent<Text>();
        warnText.font = font;
        warnText.text = "⚠️ [!] MÈO ĐANG ĐẾN GẦN!";
        warnText.fontSize = 15;
        warnText.fontStyle = FontStyle.Bold;
        warnText.alignment = TextAnchor.MiddleCenter;
        warnText.color = Color.white;
        AddChunkyOutlineAndShadow(warnBadgeTextObj, 2f, 3f);
        warnBadgeObj.SetActive(false);

        SetPrivateField(hudView, "proximityWarningBadge", warnBadgeRt);
        SetPrivateField(hudView, "proximityWarningText", warnText);

        // --- 2. Top Left Settings Button [⚙] ---
        GameObject settingsBtnObj = CreateUIObject("Btn_Settings", hudObj.transform);
        RectTransform settingsRt = settingsBtnObj.GetComponent<RectTransform>();
        settingsRt.anchorMin = new Vector2(0f, 1f);
        settingsRt.anchorMax = new Vector2(0f, 1f);
        settingsRt.pivot = new Vector2(0f, 1f);
        settingsRt.anchoredPosition = new Vector2(35f, -30f);
        settingsRt.sizeDelta = new Vector2(68f, 68f);

        Image settingsCircleImg = settingsBtnObj.AddComponent<Image>();
        settingsCircleImg.sprite = spCircleDark;
        settingsCircleImg.color = Color.white;

        Button settingsBtn = settingsBtnObj.AddComponent<Button>();
        settingsBtnObj.AddComponent<UIButtonJuice>();

        GameObject gearIconObj = CreateUIObject("Icon_Gear", settingsBtnObj.transform);
        RectTransform gearRt = gearIconObj.GetComponent<RectTransform>();
        gearRt.anchoredPosition = new Vector2(0f, 2f);
        gearRt.sizeDelta = new Vector2(40f, 40f);
        Image gearImg = gearIconObj.AddComponent<Image>();
        gearImg.sprite = spGear;
        gearImg.color = Color.white;

        SetPrivateField(hudView, "settingsButton", settingsBtn);

        // Settings Popup Dialog
        GameObject popupObj = CreateUIObject("Settings_Popup", hudObj.transform);
        RectTransform popupRt = popupObj.GetComponent<RectTransform>();
        popupRt.anchorMin = new Vector2(0f, 1f);
        popupRt.anchorMax = new Vector2(0f, 1f);
        popupRt.pivot = new Vector2(0f, 1f);
        popupRt.anchoredPosition = new Vector2(35f, -110f);
        popupRt.sizeDelta = new Vector2(370f, 240f);

        Image popupBg = popupObj.AddComponent<Image>();
        popupBg.sprite = spPanel;
        popupBg.type = Image.Type.Sliced;
        popupBg.color = Color.white;

        GameObject popTitleObj = CreateUIObject("Title", popupObj.transform);
        RectTransform popTitleRt = popTitleObj.GetComponent<RectTransform>();
        popTitleRt.anchoredPosition = new Vector2(0f, 85f);
        popTitleRt.sizeDelta = new Vector2(320f, 40f);
        Text popTitle = popTitleObj.AddComponent<Text>();
        popTitle.font = font;
        popTitle.text = "🎮 HƯỚNG DẪN ĐIỀU KHIỂN";
        popTitle.fontSize = 18;
        popTitle.fontStyle = FontStyle.Bold;
        popTitle.alignment = TextAnchor.MiddleCenter;
        popTitle.color = new Color(1f, 0.90f, 0.0f, 1f);
        AddChunkyOutlineAndShadow(popTitleObj, 2f, 3f);

        GameObject popTextObj = CreateUIObject("Text", popupObj.transform);
        RectTransform popTextRt = popTextObj.GetComponent<RectTransform>();
        popTextRt.anchoredPosition = new Vector2(0f, 15f);
        popTextRt.sizeDelta = new Vector2(310f, 95f);
        Text popText = popTextObj.AddComponent<Text>();
        popText.font = font;
        popText.text = "🐭 <b>CHUỘT:</b> [WASD] Di chuyển  |  [SPACE] Ngụy trang\n" +
                       "🐱 <b>MÈO:</b> [Mũi tên] Di chuyển  |  [J] Cào tát\n" +
                       "🎥 <b>[TAB]:</b> Đổi Camera Mèo ⇄ Chuột";
        popText.fontSize = 13;
        popText.lineSpacing = 1.35f;
        popText.alignment = TextAnchor.MiddleCenter;
        popText.color = Color.white;
        AddChunkyOutlineAndShadow(popTextObj, 1.5f, 2.5f);

        GameObject closeBtnObj = CreateButton("Btn_Close", popupObj.transform, new Vector2(0f, -75f), new Vector2(160f, 44f), spBtnRed, Color.white, font, "ĐÓNG");
        Button closeBtn = closeBtnObj.GetComponent<Button>();

        popupObj.SetActive(false);
        SetPrivateField(hudView, "settingsPopup", popupObj);
        SetPrivateField(hudView, "closeSettingsButton", closeBtn);

        // --- 3. Bottom Right Skills Bar [SPACE] & [J] ---
        GameObject skillBar = CreateUIObject("Skills_Container", hudObj.transform);
        RectTransform skillBarRt = skillBar.GetComponent<RectTransform>();
        skillBarRt.anchorMin = new Vector2(1f, 0f);
        skillBarRt.anchorMax = new Vector2(1f, 0f);
        skillBarRt.pivot = new Vector2(1f, 0f);
        skillBarRt.anchoredPosition = new Vector2(-40f, 35f);
        skillBarRt.sizeDelta = new Vector2(340f, 150f);
        SetPrivateField(hudView, "skillBarContainer", skillBarRt);

        // Skill Space (Chuột - Biến hình)
        GameObject spaceSkillObj = CreateUIObject("Skill_Space", skillBar.transform);
        RectTransform spaceRt = spaceSkillObj.GetComponent<RectTransform>();
        spaceRt.anchoredPosition = new Vector2(-185f, 0f);
        spaceRt.sizeDelta = new Vector2(140f, 140f);

        Image spaceCircle = spaceSkillObj.AddComponent<Image>();
        spaceCircle.sprite = spCircleCyan;
        spaceCircle.color = Color.white;

        GameObject spaceKeyBadge = CreateUIObject("Key_Badge", spaceSkillObj.transform);
        RectTransform skbRt = spaceKeyBadge.GetComponent<RectTransform>();
        skbRt.anchoredPosition = new Vector2(0f, 22f);
        skbRt.sizeDelta = new Vector2(100f, 40f);
        Text skbText = spaceKeyBadge.AddComponent<Text>();
        skbText.font = font;
        skbText.text = "SPACE";
        skbText.fontSize = 24;
        skbText.fontStyle = FontStyle.Bold;
        skbText.alignment = TextAnchor.MiddleCenter;
        skbText.color = Color.white;
        AddChunkyOutlineAndShadow(spaceKeyBadge, 2f, 3f);

        GameObject spaceActionObj = CreateUIObject("Action_Label", spaceSkillObj.transform);
        RectTransform saRt = spaceActionObj.GetComponent<RectTransform>();
        saRt.anchoredPosition = new Vector2(0f, -22f);
        saRt.sizeDelta = new Vector2(120f, 30f);
        Text saText = spaceActionObj.AddComponent<Text>();
        saText.font = font;
        saText.text = "BIẾN HÌNH";
        saText.fontSize = 15;
        saText.fontStyle = FontStyle.Bold;
        saText.alignment = TextAnchor.MiddleCenter;
        saText.color = Color.white;
        AddChunkyOutlineAndShadow(spaceActionObj, 1.8f, 2.5f);

        // Radial Cooldown Overlay for Space
        GameObject spaceCdObj = CreateUIObject("Cooldown_Fill", spaceSkillObj.transform);
        RectTransform scdRt = spaceCdObj.GetComponent<RectTransform>();
        SetStretchFill(scdRt);
        Image spaceCdFill = spaceCdObj.AddComponent<Image>();
        spaceCdFill.sprite = spCircleFill;
        spaceCdFill.type = Image.Type.Filled;
        spaceCdFill.fillMethod = Image.FillMethod.Radial360;
        spaceCdFill.fillOrigin = (int)Image.Origin360.Top;
        spaceCdFill.fillClockwise = false;
        spaceCdFill.color = new Color(0.08f, 0.10f, 0.15f, 0.85f);
        spaceCdObj.SetActive(false);

        GameObject spaceCdTextObj = CreateUIObject("Cooldown_Text", spaceSkillObj.transform);
        RectTransform scdtRt = spaceCdTextObj.GetComponent<RectTransform>();
        SetStretchFill(scdtRt);
        Text spaceCdText = spaceCdTextObj.AddComponent<Text>();
        spaceCdText.font = font;
        spaceCdText.text = "2.5s";
        spaceCdText.fontSize = 26;
        spaceCdText.fontStyle = FontStyle.Bold;
        spaceCdText.alignment = TextAnchor.MiddleCenter;
        spaceCdText.color = Color.white;
        AddChunkyOutlineAndShadow(spaceCdTextObj, 2f, 3f);
        spaceCdTextObj.SetActive(false);

        SetPrivateField(hudView, "spaceSkillTransform", spaceRt);
        SetPrivateField(hudView, "spaceActionLabel", saText);
        SetPrivateField(hudView, "spaceCooldownFill", spaceCdFill);
        SetPrivateField(hudView, "spaceCooldownText", spaceCdText);

        // Skill J (Mèo - Cào tát)
        GameObject jSkillObj = CreateUIObject("Skill_J", skillBar.transform);
        RectTransform jRt = jSkillObj.GetComponent<RectTransform>();
        jRt.anchoredPosition = new Vector2(-25f, 0f);
        jRt.sizeDelta = new Vector2(140f, 140f);

        Image jCircle = jSkillObj.AddComponent<Image>();
        jCircle.sprite = spCircleRed;
        jCircle.color = Color.white;

        GameObject jKeyBadge = CreateUIObject("Key_Badge", jSkillObj.transform);
        RectTransform jkbRt = jKeyBadge.GetComponent<RectTransform>();
        jkbRt.anchoredPosition = new Vector2(0f, 22f);
        jkbRt.sizeDelta = new Vector2(80f, 40f);
        Text jkbText = jKeyBadge.AddComponent<Text>();
        jkbText.font = font;
        jkbText.text = "J";
        jkbText.fontSize = 28;
        jkbText.fontStyle = FontStyle.Bold;
        jkbText.alignment = TextAnchor.MiddleCenter;
        jkbText.color = Color.white;
        AddChunkyOutlineAndShadow(jKeyBadge, 2f, 3f);

        GameObject jActionObj = CreateUIObject("Action_Label", jSkillObj.transform);
        RectTransform jaRt = jActionObj.GetComponent<RectTransform>();
        jaRt.anchoredPosition = new Vector2(0f, -22f);
        jaRt.sizeDelta = new Vector2(120f, 30f);
        Text jaText = jActionObj.AddComponent<Text>();
        jaText.font = font;
        jaText.text = "TÁT / BẮT";
        jaText.fontSize = 15;
        jaText.fontStyle = FontStyle.Bold;
        jaText.alignment = TextAnchor.MiddleCenter;
        jaText.color = Color.white;
        AddChunkyOutlineAndShadow(jActionObj, 1.8f, 2.5f);

        // Radial Cooldown Overlay for J
        GameObject jCdObj = CreateUIObject("Cooldown_Fill", jSkillObj.transform);
        RectTransform jcdRt = jCdObj.GetComponent<RectTransform>();
        SetStretchFill(jcdRt);
        Image jCdFill = jCdObj.AddComponent<Image>();
        jCdFill.sprite = spCircleFill;
        jCdFill.type = Image.Type.Filled;
        jCdFill.fillMethod = Image.FillMethod.Radial360;
        jCdFill.fillOrigin = (int)Image.Origin360.Top;
        jCdFill.fillClockwise = false;
        jCdFill.color = new Color(0.08f, 0.10f, 0.15f, 0.85f);
        jCdObj.SetActive(false);

        GameObject jCdTextObj = CreateUIObject("Cooldown_Text", jSkillObj.transform);
        RectTransform jcdtRt = jCdTextObj.GetComponent<RectTransform>();
        SetStretchFill(jcdtRt);
        Text jCdText = jCdTextObj.AddComponent<Text>();
        jCdText.font = font;
        jCdText.text = "1.5s";
        jCdText.fontSize = 26;
        jCdText.fontStyle = FontStyle.Bold;
        jCdText.alignment = TextAnchor.MiddleCenter;
        jCdText.color = Color.white;
        AddChunkyOutlineAndShadow(jCdTextObj, 2f, 3f);
        jCdTextObj.SetActive(false);

        SetPrivateField(hudView, "jSkillTransform", jRt);
        SetPrivateField(hudView, "jActionLabel", jaText);
        SetPrivateField(hudView, "jCooldownFill", jCdFill);
        SetPrivateField(hudView, "jCooldownText", jCdText);

        return hudView;
    }

    #endregion

    #region Victory Modal View Builder

    private static VictoryModalView BuildVictoryModalView(
        Transform parent,
        Sprite spPanel,
        Sprite spBtnYellow,
        Sprite spBtnCyan,
        Font font)
    {
        GameObject modalRootObj = CreateUIObject("Victory_Modal", parent);
        SetStretchFill(modalRootObj.GetComponent<RectTransform>());
        VictoryModalView modalView = modalRootObj.AddComponent<VictoryModalView>();

        // Dimmer background
        GameObject dimmerObj = CreateUIObject("Dimmer", modalRootObj.transform);
        SetStretchFill(dimmerObj.GetComponent<RectTransform>());
        Image dimmer = dimmerObj.AddComponent<Image>();
        dimmer.color = new Color(0.04f, 0.06f, 0.09f, 0.88f);
        SetPrivateField(modalView, "dimmerImage", dimmer);

        // Modal Dialog Box
        GameObject dialogObj = CreateUIObject("Dialog_Container", modalRootObj.transform);
        RectTransform dialogRt = dialogObj.GetComponent<RectTransform>();
        dialogRt.sizeDelta = new Vector2(620f, 440f);
        Image dialogBg = dialogObj.AddComponent<Image>();
        dialogBg.sprite = spPanel;
        dialogBg.type = Image.Type.Sliced;
        dialogBg.color = Color.white;
        SetPrivateField(modalView, "modalContainer", dialogRt);

        // Title: "👑 CATS WIN!" / "🧀 MICE ESCAPED!"
        GameObject titleObj = CreateUIObject("Title_Text", dialogObj.transform);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 130f);
        titleRt.sizeDelta = new Vector2(560f, 70f);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = font;
        titleText.text = "👑 CATS VICTORY!";
        titleText.fontSize = 44;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1.0f, 0.90f, 0.0f, 1f);
        AddChunkyOutlineAndShadow(titleObj, 3f, 5f);
        SetPrivateField(modalView, "titleText", titleText);

        // Subtitle
        GameObject subObj = CreateUIObject("Subtitle_Text", dialogObj.transform);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, 65f);
        subRt.sizeDelta = new Vector2(520f, 40f);
        Text subText = subObj.AddComponent<Text>();
        subText.font = font;
        subText.text = "Mèo đã tóm gọn toàn bộ đàn chuột!";
        subText.fontSize = 18;
        subText.fontStyle = FontStyle.Bold;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = Color.white;
        AddChunkyOutlineAndShadow(subObj, 2f, 3f);
        SetPrivateField(modalView, "subtitleText", subText);

        // Stats card
        GameObject statsObj = CreateUIObject("Stats_Card", dialogObj.transform);
        RectTransform statsRt = statsObj.GetComponent<RectTransform>();
        statsRt.anchoredPosition = new Vector2(0f, 5f);
        statsRt.sizeDelta = new Vector2(500f, 50f);
        Image statsBg = statsObj.AddComponent<Image>();
        statsBg.sprite = spPanel;
        statsBg.type = Image.Type.Sliced;
        statsBg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

        GameObject statsTextObj = CreateUIObject("Stats_Text", statsObj.transform);
        RectTransform stRt = statsTextObj.GetComponent<RectTransform>();
        SetStretchFill(stRt);
        Text statsText = statsTextObj.AddComponent<Text>();
        statsText.font = font;
        statsText.text = "⏱ Thời gian sinh tồn: 03:45";
        statsText.fontSize = 16;
        statsText.fontStyle = FontStyle.Bold;
        statsText.alignment = TextAnchor.MiddleCenter;
        statsText.color = Color.white;
        AddChunkyOutlineAndShadow(statsTextObj, 1.8f, 2.5f);
        SetPrivateField(modalView, "matchStatsText", statsText);

        // Buttons
        GameObject btnRestart = CreateButton("Btn_PlayAgain", dialogObj.transform, new Vector2(-125f, -85f), new Vector2(215f, 64f), spBtnYellow, Color.white, font, "▶ CHƠI LẠI");
        GameObject btnMenu = CreateButton("Btn_MainMenu", dialogObj.transform, new Vector2(125f, -85f), new Vector2(215f, 64f), spBtnCyan, Color.white, font, "🏠 MENU CHÍNH");

        SetPrivateField(modalView, "playAgainButton", btnRestart.GetComponent<Button>());
        SetPrivateField(modalView, "mainMenuButton", btnMenu.GetComponent<Button>());

        return modalView;
    }

    #endregion

    #region GameMode Menu View Builder

    private static GameModeMenuView BuildGameModeMenuView(
        Transform parent,
        Sprite spFrostedCard,
        Sprite spPanel,
        Sprite spBtnYellow,
        Sprite spBtnCyan,
        Sprite spBtnPurple,
        Sprite spBtnRed,
        Font font)
    {
        GameObject menuRootObj = CreateUIObject("GameMode_Menu", parent);
        SetStretchFill(menuRootObj.GetComponent<RectTransform>());
        GameModeMenuView menuView = menuRootObj.AddComponent<GameModeMenuView>();

        // Dimmer background
        GameObject dimmerObj = CreateUIObject("Dimmer", menuRootObj.transform);
        SetStretchFill(dimmerObj.GetComponent<RectTransform>());
        Image dimmer = dimmerObj.AddComponent<Image>();
        dimmer.color = new Color(0.04f, 0.06f, 0.09f, 0.92f);

        // ==========================================
        // 1. MAIN PANEL (THẺ KÍNH MỜ FROSTED GLASS)
        // ==========================================
        GameObject mainPanel = CreateUIObject("Main_Panel", menuRootObj.transform);
        RectTransform mainRt = mainPanel.GetComponent<RectTransform>();
        mainRt.sizeDelta = new Vector2(740f, 580f);
        Image mainBg = mainPanel.AddComponent<Image>();
        mainBg.sprite = spFrostedCard != null ? spFrostedCard : spPanel;
        mainBg.type = Image.Type.Sliced;
        mainBg.color = Color.white;
        SetPrivateField(menuView, "mainPanelContainer", mainRt);

        // Title Game: "CAT AND MOUSE"
        GameObject titleObj = CreateUIObject("Title_Text", mainPanel.transform);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0f, 205f);
        titleRt.sizeDelta = new Vector2(650f, 75f);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = font;
        titleText.text = "CAT & MOUSE";
        titleText.fontSize = 58;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1.0f, 0.92f, 0.0f, 1f); // Vàng Chanh rực rỡ
        AddChunkyOutlineAndShadow(titleObj, 3.5f, 7f);

        // Subtitle: "CHỌN CHẾ ĐỘ CHƠI"
        GameObject subObj = CreateUIObject("Subtitle_Text", mainPanel.transform);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchoredPosition = new Vector2(0f, 145f);
        subRt.sizeDelta = new Vector2(600f, 35f);
        Text subText = subObj.AddComponent<Text>();
        subText.font = font;
        subText.text = "CHỌN CHẾ ĐỘ CHƠI";
        subText.fontSize = 19;
        subText.fontStyle = FontStyle.Bold;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = Color.white;
        AddChunkyOutlineAndShadow(subObj, 2.2f, 4f);

        // 3 Nút Bấm Viên Thuốc 3D Nổi Khối
        // [NÚT 1] Vàng Cam: Chơi đơn vs Bot
        GameObject btnSolo = CreateButton("Btn_SoloBot", mainPanel.transform, new Vector2(0f, 45f), new Vector2(530f, 76f), spBtnYellow, Color.white, font, "▶ CHƠI ĐƠN (VS BOT MÈO)");
        // [NÚT 2] Xanh Cyan: Chơi Online / LAN
        GameObject btnLan = CreateButton("Btn_Lan", mainPanel.transform, new Vector2(0f, -45f), new Vector2(530f, 76f), spBtnCyan, Color.white, font, "🌐 CHƠI ONLINE / LAN (2 MÁY)");
        // [NÚT 3] Tím Pastel: Cài đặt & Chọn Map
        GameObject btnSettingsMap = CreateButton("Btn_SettingsMap", mainPanel.transform, new Vector2(0f, -135f), new Vector2(530f, 76f), spBtnPurple != null ? spBtnPurple : spBtnRed, Color.white, font, "⚙ CÀI ĐẶT & CHỌN MAP");

        SetPrivateField(menuView, "soloBotButton", btnSolo.GetComponent<Button>());
        SetPrivateField(menuView, "lanModeButton", btnLan.GetComponent<Button>());
        SetPrivateField(menuView, "settingsMapButton", btnSettingsMap.GetComponent<Button>());

        // ==========================================
        // 2. SETTINGS & MAP SELECTOR PANEL
        // ==========================================
        GameObject settingsPanel = CreateUIObject("Settings_Map_Panel", menuRootObj.transform);
        RectTransform setRt = settingsPanel.GetComponent<RectTransform>();
        setRt.sizeDelta = new Vector2(680f, 540f);
        Image setBg = settingsPanel.AddComponent<Image>();
        setBg.sprite = spFrostedCard != null ? spFrostedCard : spPanel;
        setBg.type = Image.Type.Sliced;
        setBg.color = Color.white;
        settingsPanel.SetActive(false);
        SetPrivateField(menuView, "settingsMapPanelContainer", setRt);

        // Title Cài đặt & Chọn Map
        GameObject setTitleObj = CreateUIObject("Title", settingsPanel.transform);
        RectTransform setTitleRt = setTitleObj.GetComponent<RectTransform>();
        setTitleRt.anchoredPosition = new Vector2(0f, 210f);
        setTitleRt.sizeDelta = new Vector2(580f, 50f);
        Text setTitleText = setTitleObj.AddComponent<Text>();
        setTitleText.font = font;
        setTitleText.text = "⚙ CÀI ĐẶT & CHỌN BẢN ĐỒ";
        setTitleText.fontSize = 32;
        setTitleText.fontStyle = FontStyle.Bold;
        setTitleText.alignment = TextAnchor.MiddleCenter;
        setTitleText.color = new Color(1.0f, 0.92f, 0.0f, 1f);
        AddChunkyOutlineAndShadow(setTitleObj, 2.5f, 4f);

        // Map Toggle Button (Vàng Cam)
        GameObject btnMapToggle = CreateButton("Btn_MapToggle", settingsPanel.transform, new Vector2(0f, 125f), new Vector2(520f, 68f), spBtnYellow, Color.white, font, "🏢 ĐỔI BẢN ĐỒ (PENTHOUSE ⇄ NHÀ KHO)");
        SetPrivateField(menuView, "mapToggleButton", btnMapToggle.GetComponent<Button>());

        // Current Map Display Card
        GameObject mapCard = CreateUIObject("Map_Card", settingsPanel.transform);
        RectTransform mapCardRt = mapCard.GetComponent<RectTransform>();
        mapCardRt.anchoredPosition = new Vector2(0f, 50f);
        mapCardRt.sizeDelta = new Vector2(520f, 48f);
        Image mapCardBg = mapCard.AddComponent<Image>();
        mapCardBg.sprite = spPanel;
        mapCardBg.type = Image.Type.Sliced;
        mapCardBg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

        GameObject mapLabelObj = CreateUIObject("Map_Label", mapCard.transform);
        RectTransform mlRt = mapLabelObj.GetComponent<RectTransform>();
        SetStretchFill(mlRt);
        Text mapLabel = mapLabelObj.AddComponent<Text>();
        mapLabel.font = font;
        mapLabel.text = "BẢN ĐỒ: 🏢 PENTHOUSE HIỆN ĐẠI";
        mapLabel.fontSize = 17;
        mapLabel.fontStyle = FontStyle.Bold;
        mapLabel.alignment = TextAnchor.MiddleCenter;
        mapLabel.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Cyber Cyan
        AddChunkyOutlineAndShadow(mapLabelObj, 2f, 3f);
        SetPrivateField(menuView, "currentMapLabelText", mapLabel);

        // Local 2P Button (Đỏ tươi)
        GameObject btnLocal = CreateButton("Btn_Local2P", settingsPanel.transform, new Vector2(0f, -30f), new Vector2(520f, 68f), spBtnRed, Color.white, font, "👥 CHƠI 2 NGƯỜI CỤC BỘ (1 BÀN PHÍM)");
        SetPrivateField(menuView, "local2PButton", btnLocal.GetComponent<Button>());

        // Back from Settings Button (Xanh Cyan)
        GameObject btnBackSet = CreateButton("Btn_BackSettings", settingsPanel.transform, new Vector2(0f, -130f), new Vector2(320f, 60f), spBtnCyan, Color.white, font, "← QUAY LẠI");
        SetPrivateField(menuView, "backFromSettingsButton", btnBackSet.GetComponent<Button>());

        // ==========================================
        // 3. LAN SETUP PANEL
        // ==========================================
        GameObject lanPanel = CreateUIObject("LAN_Setup_Panel", menuRootObj.transform);
        RectTransform lanRt = lanPanel.GetComponent<RectTransform>();
        lanRt.sizeDelta = new Vector2(680f, 520f);
        Image lanBg = lanPanel.AddComponent<Image>();
        lanBg.sprite = spFrostedCard != null ? spFrostedCard : spPanel;
        lanBg.type = Image.Type.Sliced;
        lanBg.color = Color.white;
        lanPanel.SetActive(false);
        SetPrivateField(menuView, "lanSetupPanelContainer", lanRt);

        // LAN Title
        GameObject lanTitleObj = CreateUIObject("Lan_Title", lanPanel.transform);
        RectTransform lanTitleRt = lanTitleObj.GetComponent<RectTransform>();
        lanTitleRt.anchoredPosition = new Vector2(0f, 200f);
        lanTitleRt.sizeDelta = new Vector2(580f, 50f);
        Text lanTitleText = lanTitleObj.AddComponent<Text>();
        lanTitleText.font = font;
        lanTitleText.text = "THIẾT LẬP MẠNG LAN (2 MÁY)";
        lanTitleText.fontSize = 32;
        lanTitleText.fontStyle = FontStyle.Bold;
        lanTitleText.alignment = TextAnchor.MiddleCenter;
        lanTitleText.color = new Color(1.0f, 0.92f, 0.0f, 1f);
        AddChunkyOutlineAndShadow(lanTitleObj, 2.5f, 4f);

        // Local IP Badge
        GameObject ipCard = CreateUIObject("IP_Card", lanPanel.transform);
        RectTransform ipCardRt = ipCard.GetComponent<RectTransform>();
        ipCardRt.anchoredPosition = new Vector2(0f, 135f);
        ipCardRt.sizeDelta = new Vector2(540f, 50f);
        Image ipCardBg = ipCard.AddComponent<Image>();
        ipCardBg.sprite = spPanel;
        ipCardBg.type = Image.Type.Sliced;
        ipCardBg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

        GameObject ipTextObj = CreateUIObject("IP_Text", ipCard.transform);
        RectTransform ipTextRt = ipTextObj.GetComponent<RectTransform>();
        SetStretchFill(ipTextRt);
        Text ipText = ipTextObj.AddComponent<Text>();
        ipText.font = font;
        ipText.text = "Địa chỉ IP máy bạn: 192.168.1.10";
        ipText.fontSize = 17;
        ipText.fontStyle = FontStyle.Bold;
        ipText.alignment = TextAnchor.MiddleCenter;
        ipText.color = Color.white;
        AddChunkyOutlineAndShadow(ipTextObj, 2f, 3f);
        SetPrivateField(menuView, "localIpText", ipText);

        // Create Host Button
        GameObject btnHost = CreateButton("Btn_CreateHost", lanPanel.transform, new Vector2(0f, 55f), new Vector2(460f, 64f), spBtnYellow, Color.white, font, "👑 TẠO PHÒNG HOST (VAI MÈO)");
        SetPrivateField(menuView, "createHostButton", btnHost.GetComponent<Button>());

        // Client IP Input Container
        GameObject inputCard = CreateUIObject("Input_Card", lanPanel.transform);
        RectTransform inputCardRt = inputCard.GetComponent<RectTransform>();
        inputCardRt.anchoredPosition = new Vector2(-100f, -25f);
        inputCardRt.sizeDelta = new Vector2(250f, 54f);
        Image inputBg = inputCard.AddComponent<Image>();
        inputBg.sprite = spPanel;
        inputBg.type = Image.Type.Sliced;
        inputBg.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);

        GameObject inputFieldObj = CreateUIObject("InputField", inputCard.transform);
        RectTransform ifRt = inputFieldObj.GetComponent<RectTransform>();
        SetStretchFill(ifRt);
        ifRt.offsetMin = new Vector2(16f, 6f);
        ifRt.offsetMax = new Vector2(-16f, -6f);

        InputField inputField = inputFieldObj.AddComponent<InputField>();

        GameObject ifTextObj = CreateUIObject("Text", inputFieldObj.transform);
        RectTransform ifTextRt = ifTextObj.GetComponent<RectTransform>();
        SetStretchFill(ifTextRt);
        Text ifText = ifTextObj.AddComponent<Text>();
        ifText.font = font;
        ifText.fontSize = 16;
        ifText.fontStyle = FontStyle.Bold;
        ifText.alignment = TextAnchor.MiddleLeft;
        ifText.color = Color.white;

        GameObject ifPhObj = CreateUIObject("Placeholder", inputFieldObj.transform);
        RectTransform ifPhRt = ifPhObj.GetComponent<RectTransform>();
        SetStretchFill(ifPhRt);
        Text ifPh = ifPhObj.AddComponent<Text>();
        ifPh.font = font;
        ifPh.text = "Nhập IP Host...";
        ifPh.fontSize = 15;
        ifPh.alignment = TextAnchor.MiddleLeft;
        ifPh.color = new Color(0.6f, 0.7f, 0.8f, 0.8f);

        inputField.textComponent = ifText;
        inputField.placeholder = ifPh;
        inputField.text = "127.0.0.1";
        SetPrivateField(menuView, "clientIpInput", inputField);

        // Join Client Button
        GameObject btnJoin = CreateButton("Btn_JoinClient", lanPanel.transform, new Vector2(160f, -25f), new Vector2(220f, 54f), spBtnCyan, Color.white, font, "🧀 VÀO (CHUỘT)");
        SetPrivateField(menuView, "joinClientButton", btnJoin.GetComponent<Button>());

        // Status Label
        GameObject statusObj = CreateUIObject("Status_Text", lanPanel.transform);
        RectTransform statusRt = statusObj.GetComponent<RectTransform>();
        statusRt.anchoredPosition = new Vector2(0f, -95f);
        statusRt.sizeDelta = new Vector2(550f, 40f);
        Text statusText = statusObj.AddComponent<Text>();
        statusText.font = font;
        statusText.text = "Sẵn sàng tạo phòng hoặc kết nối";
        statusText.fontSize = 15;
        statusText.fontStyle = FontStyle.Bold;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.white;
        AddChunkyOutlineAndShadow(statusObj, 2f, 3f);
        SetPrivateField(menuView, "lanStatusText", statusText);

        // Back Button
        GameObject btnBack = CreateButton("Btn_BackFromLan", lanPanel.transform, new Vector2(0f, -170f), new Vector2(240f, 56f), spBtnRed, Color.white, font, "← QUAY LẠI");
        SetPrivateField(menuView, "backFromLanButton", btnBack.GetComponent<Button>());

        return menuView;
    }

    #endregion

    #region Helper Methods

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void SetStretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject CreateButton(string name, Transform parent, Vector2 pos, Vector2 size, Sprite spButton, Color tintColor, Font font, string label)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.sprite = spButton;
        img.type = Image.Type.Sliced;
        img.color = tintColor;

        btnObj.AddComponent<Button>();
        btnObj.AddComponent<UIButtonJuice>();

        GameObject textObj = CreateUIObject("Text", btnObj.transform);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        SetStretchFill(textRt);
        textRt.anchoredPosition = new Vector2(0f, 3f);
        Text t = textObj.AddComponent<Text>();
        t.font = font;
        t.text = label;
        t.fontSize = (int)(size.y * 0.36f);
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        AddChunkyOutlineAndShadow(textObj, 2.2f, 3.5f);

        return btnObj;
    }

    private static void AddChunkyOutlineAndShadow(GameObject obj, float outlineDist = 2.5f, float shadowDist = 4.5f)
    {
        Outline out1 = obj.AddComponent<Outline>();
        out1.effectColor = new Color(0.06f, 0.08f, 0.10f, 1f);
        out1.effectDistance = new Vector2(outlineDist, outlineDist);

        Outline out2 = obj.AddComponent<Outline>();
        out2.effectColor = new Color(0.06f, 0.08f, 0.10f, 1f);
        out2.effectDistance = new Vector2(-outlineDist, -outlineDist);

        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.06f, 0.08f, 0.10f, 1f);
        shadow.effectDistance = new Vector2(0f, -shadowDist);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    #endregion
}
