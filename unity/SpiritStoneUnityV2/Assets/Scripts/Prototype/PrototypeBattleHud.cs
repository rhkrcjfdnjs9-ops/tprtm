using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritStone.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeBattleHud : MonoBehaviour
    {
        private const float HudRefreshInterval = 0.1f;
        private const float RewardBannerBottomOffset = 21f;
        private const float RewardBannerHeight = 38f;

        private sealed class FloatingDamage
        {
            public FloatingDamage(Text label, bool targetsEnemy, Vector2 startPosition)
            {
                Label = label;
                TargetsEnemy = targetsEnemy;
                StartPosition = startPosition;
                RemainingTime = 0.75f;
            }

            public Text Label { get; }
            public bool TargetsEnemy { get; }
            public Vector2 StartPosition { get; }
            public float RemainingTime { get; set; }
        }

        private readonly List<FloatingDamage> floatingDamages = new();
        private IdleBattlePrototype battle;
        private RectTransform safeAreaRoot;
        private Rect lastSafeArea;
        private Text stageText;
        private Text currencyText;
        private Text levelText;
        private Text messageText;
        private GameObject rewardBanner;
        private Text rewardText;
        private Text enemyNameText;
        private Text teamHealthText;
        private Text enemyHealthText;
        private Text enemyTargetsText;
        private Text ultimateText;
        private Text skillText;
        private Text supportText;
        private Text attackText;
        private Text formationText;
        private Text evolutionText;
        private Text offlineReportText;
        private Text protagonistInfoText;
        private readonly Text[] spiritInfoTexts = new Text[3];
        private readonly Text[] formationSlotTexts = new Text[3];
        private readonly Button[] breakthroughButtons = new Button[3];
        private readonly Button[] shardExchangeButtons = new Button[3];
        private readonly Button[] formationRemoveButtons = new Button[3];
        private readonly List<PrototypeSpiritData> rosterSpirits = new();
        private readonly List<Text> rosterSpiritTexts = new();
        private readonly List<Button> rosterSpiritButtons = new();
        private Text selectedSpiritDetailText;
        private Image teamHealthFill;
        private Image enemyHealthFill;
        private Image ultimateFill;
        private Button previousStageButton;
        private Button nextStageButton;
        private Text autoChallengeButtonText;
        private Text ownedSpiritsText;
        private Text summonButtonText;
        private Text summonResultText;
        private Button summonButton;
        private Button summonTenButton;
        private GameObject summonInfoPopup;
        private Text summonInfoText;
        private GameObject offlinePopup;
        private GameObject formationPopup;
        private GameObject growthPopup;
        private GameObject stagePreviewPopup;
        private Text growthCurrencyText;
        private Text growthSelectedSpiritText;
        private readonly Text[] spiritTrainingButtonTexts = new Text[6];
        private GameObject combatInfoContent;
        private GameObject characterInfoContent;
        private Button combatInfoTabButton;
        private Button characterInfoTabButton;
        private int selectedSpiritSlot;
        private int selectedFormationSlot;
        private Font runtimeFont;
        private float hudRefreshTimer;
        private float rewardBannerRemaining;
        private int displayedRewardSequence;
        private Camera worldCamera;

        public void Initialize(IdleBattlePrototype battleController)
        {
            battle = battleController;
            worldCamera = Camera.main;
            if (safeAreaRoot == null) BuildCanvas();
            RefreshHud();
        }

        public void ShowDamage(float amount, bool targetsEnemy, Transform targetVisual = null)
        {
            if (safeAreaRoot == null || IsFullScreenPopupOpen()) return;
            Text label = CreateText(safeAreaRoot, targetsEnemy ? "EnemyDamage" : "TeamDamage", 38, TextAnchor.MiddleCenter);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 70f);
            Vector2 startPosition = targetsEnemy ? new Vector2(260f, 220f) : new Vector2(-260f, 220f);
            if (!targetsEnemy && targetVisual != null && worldCamera != null)
            {
                Vector2 screenPosition = worldCamera.WorldToScreenPoint(targetVisual.position + Vector3.up * 0.8f);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(safeAreaRoot, screenPosition, null, out Vector2 localPosition))
                    startPosition = localPosition;
            }
            rect.anchoredPosition = startPosition;
            label.color = targetsEnemy ? new Color(1f, 0.88f, 0.25f) : new Color(1f, 0.3f, 0.3f);
            label.text = $"-{amount:0}";
            floatingDamages.Add(new FloatingDamage(label, targetsEnemy, startPosition));
        }

        private bool IsFullScreenPopupOpen()
        {
            return (formationPopup != null && formationPopup.activeSelf)
                || (growthPopup != null && growthPopup.activeSelf)
                || (summonInfoPopup != null && summonInfoPopup.activeSelf)
                || (stagePreviewPopup != null && stagePreviewPopup.activeSelf);
        }

        private void OpenFullScreenPopup(GameObject popup)
        {
            if (popup == null) return;
            for (int index = floatingDamages.Count - 1; index >= 0; index--)
                Destroy(floatingDamages[index].Label.gameObject);
            floatingDamages.Clear();
            popup.SetActive(true);
            popup.transform.SetAsLastSibling();
        }

        private void Update()
        {
            UpdateSafeArea();
            hudRefreshTimer -= Time.unscaledDeltaTime;
            if (hudRefreshTimer <= 0f)
            {
                hudRefreshTimer = HudRefreshInterval;
                RefreshHud();
            }
            UpdateFloatingDamages();
            if (rewardBannerRemaining > 0f)
            {
                rewardBannerRemaining -= Time.unscaledDeltaTime;
                if (rewardBannerRemaining <= 0f && rewardBanner != null) rewardBanner.SetActive(false);
            }
        }

        private void BuildCanvas()
        {
            EnsureEventSystem();
            runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (runtimeFont == null)
                Debug.LogErrorFormat("[PrototypeBattleHud] Built-in runtime font could not be loaded.");

            GameObject canvasObject = new("PrototypeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            safeAreaRoot = CreateRect(canvasObject.transform, "SafeArea");
            safeAreaRoot.anchorMin = Vector2.zero;
            safeAreaRoot.anchorMax = Vector2.one;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;

            RectTransform topPanel = CreatePanel(safeAreaRoot, "TopPanel", new Color(0.055f, 0.06f, 0.11f, 0.96f));
            SetTopStretch(topPanel, 300f, 0f);
            stageText = CreateAnchoredText(topPanel, "Stage", 38, TextAnchor.MiddleCenter, 20f, -20f, -20f, -78f);
            Button stagePreviewButton = stageText.gameObject.AddComponent<Button>();
            stagePreviewButton.targetGraphic = stageText;
            stagePreviewButton.onClick.AddListener(() => OpenFullScreenPopup(stagePreviewPopup));
            currencyText = CreateAnchoredText(topPanel, "Currency", 27, TextAnchor.MiddleCenter, 20f, -82f, -20f, -125f);
            levelText = CreateAnchoredText(topPanel, "Levels", 20, TextAnchor.MiddleCenter, 20f, -128f, -20f, -184f);
            messageText = CreateAnchoredText(topPanel, "Message", 25, TextAnchor.MiddleCenter, 20f, -185f, -20f, -240f);
            RectTransform formationButtonRect = CreatePanel(topPanel, "FormationButton", new Color(0.24f, 0.18f, 0.42f, 1f));
            formationButtonRect.anchorMin = formationButtonRect.anchorMax = new Vector2(1f, 1f);
            formationButtonRect.sizeDelta = new Vector2(210f, 58f);
            formationButtonRect.anchoredPosition = new Vector2(-125f, -48f);
            Button formationButton = formationButtonRect.gameObject.AddComponent<Button>();
            formationButton.targetGraphic = formationButtonRect.GetComponent<Image>();
            formationButton.onClick.AddListener(() => OpenFullScreenPopup(formationPopup));
            CreateStretchText(formationButtonRect, "Label", 23, TextAnchor.MiddleCenter).text = "정령 편성";
            RectTransform growthButtonRect = CreatePanel(topPanel, "GrowthButton", new Color(0.34f, 0.18f, 0.58f, 1f));
            growthButtonRect.anchorMin = growthButtonRect.anchorMax = new Vector2(0f, 1f);
            growthButtonRect.sizeDelta = new Vector2(210f, 58f);
            growthButtonRect.anchoredPosition = new Vector2(125f, -48f);
            Button growthButton = growthButtonRect.gameObject.AddComponent<Button>();
            growthButton.targetGraphic = growthButtonRect.GetComponent<Image>();
            growthButton.onClick.AddListener(() => OpenFullScreenPopup(growthPopup));
            CreateStretchText(growthButtonRect, "Label", 23, TextAnchor.MiddleCenter).text = "성장";
            BuildStageControls(topPanel);
            RectTransform statusPanel = CreatePanel(safeAreaRoot, "StatusPanel", new Color(0.04f, 0.045f, 0.08f, 0.9f));
            statusPanel.anchorMin = new Vector2(0f, 1f);
            statusPanel.anchorMax = new Vector2(1f, 1f);
            statusPanel.offsetMin = new Vector2(32f, -545f);
            statusPanel.offsetMax = new Vector2(-32f, -315f);
            teamHealthFill = CreateBar(statusPanel, "TeamHealth", new Vector2(32f, -30f), new Vector2(-32f, -82f), new Color(0.2f, 0.78f, 0.52f), out teamHealthText);
            enemyNameText = CreateAnchoredText(statusPanel, "EnemyName", 25, TextAnchor.MiddleCenter, 32f, -90f, -32f, -128f);
            enemyHealthFill = CreateBar(statusPanel, "EnemyHealth", new Vector2(32f, -132f), new Vector2(-32f, -184f), new Color(0.9f, 0.3f, 0.2f), out enemyHealthText);
            enemyTargetsText = CreateAnchoredText(statusPanel, "EnemyTargets", 18, TextAnchor.MiddleCenter, 32f, -186f, -32f, -224f);

            RectTransform infoPanel = CreatePanel(safeAreaRoot, "InfoPanel", new Color(0.04f, 0.045f, 0.08f, 0.86f));
            infoPanel.anchorMin = new Vector2(0f, 0f);
            infoPanel.anchorMax = new Vector2(1f, 0f);
            infoPanel.offsetMin = new Vector2(32f, 45f);
            infoPanel.offsetMax = new Vector2(-32f, 545f);
            BuildInformationPanel(infoPanel);

            RectTransform rewardRect = CreatePanel(safeAreaRoot, "RewardBanner", new Color(0.12f, 0.08f, 0.2f, 0.96f));
            rewardRect.anchorMin = rewardRect.anchorMax = new Vector2(0.5f, 0f);
            rewardRect.sizeDelta = new Vector2(940f, RewardBannerHeight);
            rewardRect.anchoredPosition = new Vector2(0f, RewardBannerBottomOffset);
            rewardBanner = rewardRect.gameObject;
            rewardText = CreateStretchText(rewardRect, "RewardText", 18, TextAnchor.MiddleCenter);
            rewardBanner.SetActive(false);

            BuildOfflinePopup();
            BuildFormationPopup();
            BuildSummonInfoPopup();
            BuildGrowthPopup();
            BuildStagePreviewPopup();
            UpdateSafeArea(true);
        }

        private void BuildStagePreviewPopup()
        {
            RectTransform overlay = CreatePanel(safeAreaRoot, "StagePreviewOverlay", new Color(0.025f, 0.025f, 0.055f, 0.98f));
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            stagePreviewPopup = overlay.gameObject;

            Text title = CreateAnchoredText(overlay, "Title", 42, TextAnchor.MiddleCenter, 70f, -130f, -70f, -220f);
            title.text = "개발용 스테이지 콘텐츠 확인";
            Text guide = CreateAnchoredText(overlay, "Guide", 24, TextAnchor.MiddleCenter, 70f, -225f, -70f, -300f);
            guide.text = "최고 기록은 유지됩니다. 확인할 스테이지를 선택하세요.";

            for (int index = 0; index < 10; index++)
            {
                int stageNumber = index + 1;
                int row = index / 2;
                int column = index % 2;
                float left = column == 0 ? 0.09f : 0.52f;
                float right = column == 0 ? 0.48f : 0.91f;
                float top = 0.78f - row * 0.12f;
                RectTransform rect = CreatePanel(overlay, $"PreviewStage{stageNumber}", new Color(0.26f, 0.16f, 0.48f, 1f));
                SetPanelAnchors(rect, new Vector2(left, top - 0.085f), new Vector2(right, top), Vector2.zero, Vector2.zero);
                Button button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = rect.GetComponent<Image>();
                button.onClick.AddListener(() =>
                {
                    battle?.PreviewStage(stageNumber);
                    stagePreviewPopup.SetActive(false);
                });
                PrototypeStageContentData content = PrototypeStageCatalog.CreateContent(stageNumber);
                CreateStretchText(rect, "Label", 24, TextAnchor.MiddleCenter).text =
                    $"STAGE {stageNumber}  ·  {content.BossName} ({PrototypeElementChart.GetDisplayName(content.BossElement)})";
            }

            RectTransform closeRect = CreatePanel(overlay, "Close", new Color(0.34f, 0.18f, 0.58f, 1f));
            SetPanelAnchors(closeRect, new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.15f), Vector2.zero, Vector2.zero);
            Button closeButton = closeRect.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeRect.GetComponent<Image>();
            closeButton.onClick.AddListener(() => stagePreviewPopup.SetActive(false));
            CreateStretchText(closeRect, "Label", 28, TextAnchor.MiddleCenter).text = "닫기";
            stagePreviewPopup.SetActive(false);
        }

        private void BuildStageControls(RectTransform topPanel)
        {
            RectTransform previousRect = CreatePanel(topPanel, "PreviousStageButton", new Color(0.15f, 0.17f, 0.25f, 1f));
            SetPanelAnchors(previousRect, new Vector2(0.03f, 0.02f), new Vector2(0.25f, 0.19f), Vector2.zero, Vector2.zero);
            previousStageButton = previousRect.gameObject.AddComponent<Button>();
            previousStageButton.targetGraphic = previousRect.GetComponent<Image>();
            previousStageButton.onClick.AddListener(() => battle?.SelectPreviousStage());
            CreateStretchText(previousRect, "Label", 22, TextAnchor.MiddleCenter).text = "◀ 이전";

            RectTransform autoRect = CreatePanel(topPanel, "AutoChallengeButton", new Color(0.34f, 0.18f, 0.58f, 1f));
            SetPanelAnchors(autoRect, new Vector2(0.27f, 0.02f), new Vector2(0.73f, 0.19f), Vector2.zero, Vector2.zero);
            Button autoButton = autoRect.gameObject.AddComponent<Button>();
            autoButton.targetGraphic = autoRect.GetComponent<Image>();
            autoButton.onClick.AddListener(() => battle?.ToggleAutoChallenge());
            autoChallengeButtonText = CreateStretchText(autoRect, "Label", 22, TextAnchor.MiddleCenter);

            RectTransform nextRect = CreatePanel(topPanel, "NextStageButton", new Color(0.15f, 0.17f, 0.25f, 1f));
            SetPanelAnchors(nextRect, new Vector2(0.75f, 0.02f), new Vector2(0.97f, 0.19f), Vector2.zero, Vector2.zero);
            nextStageButton = nextRect.gameObject.AddComponent<Button>();
            nextStageButton.targetGraphic = nextRect.GetComponent<Image>();
            nextStageButton.onClick.AddListener(() => battle?.SelectNextStage());
            CreateStretchText(nextRect, "Label", 22, TextAnchor.MiddleCenter).text = "다음 ▶";
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void BuildOfflinePopup()
        {
            RectTransform overlay = CreatePanel(safeAreaRoot, "OfflineOverlay", new Color(0f, 0f, 0f, 0.72f));
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            offlinePopup = overlay.gameObject;

            RectTransform popup = CreatePanel(overlay, "OfflinePopup", new Color(0.09f, 0.08f, 0.16f, 1f));
            popup.anchorMin = popup.anchorMax = new Vector2(0.5f, 0.5f);
            popup.sizeDelta = new Vector2(820f, 430f);
            popup.anchoredPosition = Vector2.zero;
            Text title = CreateAnchoredText(popup, "Title", 40, TextAnchor.MiddleCenter, 30f, -35f, -30f, -105f);
            title.text = "오프라인 보상";
            offlineReportText = CreateAnchoredText(popup, "Report", 28, TextAnchor.MiddleCenter, 50f, -120f, -50f, -270f);
            RectTransform confirmRect = CreatePanel(popup, "Confirm", new Color(0.38f, 0.22f, 0.68f, 1f));
            confirmRect.anchorMin = new Vector2(0f, 0f);
            confirmRect.anchorMax = new Vector2(1f, 0f);
            confirmRect.offsetMin = new Vector2(150f, 35f);
            confirmRect.offsetMax = new Vector2(-150f, 115f);
            Button confirmButton = confirmRect.gameObject.AddComponent<Button>();
            confirmButton.targetGraphic = confirmRect.GetComponent<Image>();
            confirmButton.onClick.AddListener(() => battle?.DismissOfflineReport());
            Text confirmText = CreateStretchText(confirmRect, "Label", 28, TextAnchor.MiddleCenter);
            confirmText.text = "확인";
            offlinePopup.SetActive(false);
        }

        private void BuildInformationPanel(RectTransform infoPanel)
        {
            RectTransform combatTab = CreatePanel(infoPanel, "CombatInfoTab", new Color(0.34f, 0.18f, 0.58f, 1f));
            SetPanelAnchors(combatTab, new Vector2(0f, 0.88f), new Vector2(0.5f, 1f), new Vector2(4f, 4f), new Vector2(-2f, -4f));
            combatInfoTabButton = combatTab.gameObject.AddComponent<Button>();
            combatInfoTabButton.targetGraphic = combatTab.GetComponent<Image>();
            combatInfoTabButton.onClick.AddListener(() => SetInformationMode(false));
            CreateStretchText(combatTab, "Label", 24, TextAnchor.MiddleCenter).text = "전투 정보";

            RectTransform characterTab = CreatePanel(infoPanel, "CharacterInfoTab", new Color(0.16f, 0.12f, 0.25f, 1f));
            SetPanelAnchors(characterTab, new Vector2(0.5f, 0.88f), new Vector2(1f, 1f), new Vector2(2f, 4f), new Vector2(-4f, -4f));
            characterInfoTabButton = characterTab.gameObject.AddComponent<Button>();
            characterInfoTabButton.targetGraphic = characterTab.GetComponent<Image>();
            characterInfoTabButton.onClick.AddListener(() => SetInformationMode(true));
            CreateStretchText(characterTab, "Label", 24, TextAnchor.MiddleCenter).text = "캐릭터 정보";

            RectTransform combatContent = CreateRect(infoPanel, "CombatInfoContent");
            SetPanelAnchors(combatContent, Vector2.zero, new Vector2(1f, 0.88f), Vector2.zero, Vector2.zero);
            combatInfoContent = combatContent.gameObject;
            ultimateFill = CreateBar(combatContent, "Ultimate", new Vector2(32f, -16f), new Vector2(-32f, -64f), new Color(0.68f, 0.25f, 1f), out ultimateText);
            skillText = CreateAnchoredText(combatContent, "Skills", 22, TextAnchor.MiddleCenter, 28f, -70f, -28f, -112f);
            supportText = CreateAnchoredText(combatContent, "Support", 20, TextAnchor.MiddleCenter, 28f, -116f, -28f, -176f);
            attackText = CreateAnchoredText(combatContent, "Attack", 21, TextAnchor.MiddleCenter, 28f, -180f, -28f, -222f);
            formationText = CreateAnchoredText(combatContent, "Formation", 20, TextAnchor.MiddleLeft, 48f, -226f, -48f, -334f);
            evolutionText = CreateAnchoredText(combatContent, "Evolution", 21, TextAnchor.MiddleCenter, 28f, -338f, -28f, -392f);

            RectTransform characterContent = CreateRect(infoPanel, "CharacterInfoContent");
            SetPanelAnchors(characterContent, Vector2.zero, new Vector2(1f, 0.88f), Vector2.zero, Vector2.zero);
            characterInfoContent = characterContent.gameObject;
            protagonistInfoText = CreateAnchoredText(characterContent, "ProtagonistInfo", 22, TextAnchor.MiddleCenter, 18f, -8f, -18f, -68f);

            float[] leftAnchors = { 0.015f, 0.34f, 0.665f };
            float[] rightAnchors = { 0.335f, 0.66f, 0.985f };
            Color[] cardColors =
            {
                new Color(0.16f, 0.09f, 0.24f, 1f),
                new Color(0.24f, 0.085f, 0.04f, 1f),
                new Color(0.035f, 0.16f, 0.23f, 1f)
            };
            for (int index = 0; index < spiritInfoTexts.Length; index++)
            {
                int slotIndex = index;
                RectTransform card = CreateInfoCard(characterContent, $"SpiritSlot{index + 1}", new Vector2(leftAnchors[index], 0.47f), new Vector2(rightAnchors[index], 0.84f), cardColors[index]);
                Button cardButton = card.gameObject.AddComponent<Button>();
                cardButton.targetGraphic = card.GetComponent<Image>();
                cardButton.onClick.AddListener(() => SelectSpiritSlot(slotIndex));
                spiritInfoTexts[index] = CreateCardText(card, "Info", 20);
            }

            RectTransform selectedDetailCard = CreateInfoCard(characterContent, "SelectedSpiritDetailCard",
                new Vector2(0.015f, 0.03f), new Vector2(0.985f, 0.44f), new Color(0.09f, 0.075f, 0.16f, 1f));
            selectedSpiritDetailText = CreateCardText(selectedDetailCard, "SelectedSpiritDetail", 20);
            SetInformationMode(false);
        }

        private void BuildFormationPopup()
        {
            RectTransform overlay = CreatePanel(safeAreaRoot, "FormationOverlay", new Color(0.015f, 0.018f, 0.04f, 0.98f));
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            formationPopup = overlay.gameObject;

            Text title = CreateAnchoredText(overlay, "Title", 40, TextAnchor.MiddleCenter, 30f, -90f, -30f, -160f);
            title.text = "정령 편성";
            RectTransform summonInfoButtonRect = CreatePanel(overlay, "SummonInfoButton", new Color(0.2f, 0.25f, 0.42f, 1f));
            SetPanelAnchors(summonInfoButtonRect, new Vector2(0.76f, 0.91f), new Vector2(0.95f, 0.955f), Vector2.zero, Vector2.zero);
            Button summonInfoButton = summonInfoButtonRect.gameObject.AddComponent<Button>();
            summonInfoButton.targetGraphic = summonInfoButtonRect.GetComponent<Image>();
            summonInfoButton.onClick.AddListener(OpenSummonInfo);
            CreateStretchText(summonInfoButtonRect, "Label", 20, TextAnchor.MiddleCenter).text = "소환 정보";
            Text guide = CreateAnchoredText(overlay, "Guide", 24, TextAnchor.MiddleCenter, 40f, -170f, -40f, -250f);
            guide.text = "편성할 슬롯을 선택한 뒤 아래 정령 목록에서 원하는 정령을 누르세요.\n해제 버튼으로 슬롯을 비울 수 있으며, 중복 선택 시 두 슬롯이 교환됩니다.";

            Color[] slotColors =
            {
                new Color(0.16f, 0.09f, 0.24f, 1f),
                new Color(0.24f, 0.085f, 0.04f, 1f),
                new Color(0.035f, 0.16f, 0.23f, 1f)
            };
            for (int index = 0; index < formationSlotTexts.Length; index++)
            {
                int slotIndex = index;
                RectTransform slot = CreatePanel(overlay, $"FormationSlot{index + 1}", slotColors[index]);
                slot.anchorMin = new Vector2(0.08f, 0.67f - index * 0.115f);
                slot.anchorMax = new Vector2(0.92f, 0.77f - index * 0.115f);
                slot.offsetMin = Vector2.zero;
                slot.offsetMax = Vector2.zero;
                Button slotButton = slot.gameObject.AddComponent<Button>();
                slotButton.targetGraphic = slot.GetComponent<Image>();
                slotButton.onClick.AddListener(() => SelectFormationSlot(slotIndex));
                formationSlotTexts[index] = CreateStretchText(slot, "Label", 27, TextAnchor.MiddleCenter);
                formationSlotTexts[index].rectTransform.offsetMax = new Vector2(-360f, 0f);

                RectTransform removeRect = CreatePanel(slot, "RemoveButton", new Color(0.52f, 0.12f, 0.2f, 1f));
                SetPanelAnchors(removeRect, new Vector2(0.59f, 0.06f), new Vector2(0.72f, 0.94f), Vector2.zero, Vector2.zero);
                formationRemoveButtons[index] = removeRect.gameObject.AddComponent<Button>();
                formationRemoveButtons[index].targetGraphic = removeRect.GetComponent<Image>();
                formationRemoveButtons[index].onClick.AddListener(() => UnassignFormationSlot(slotIndex));
                CreateStretchText(removeRect, "Label", 19, TextAnchor.MiddleCenter).text = "해제";

                RectTransform breakthroughRect = CreatePanel(slot, "BreakthroughButton", new Color(0.5f, 0.25f, 0.68f, 1f));
                SetPanelAnchors(breakthroughRect, new Vector2(0.74f, 0.53f), new Vector2(0.98f, 0.94f), Vector2.zero, Vector2.zero);
                breakthroughButtons[index] = breakthroughRect.gameObject.AddComponent<Button>();
                breakthroughButtons[index].targetGraphic = breakthroughRect.GetComponent<Image>();
                breakthroughButtons[index].onClick.AddListener(() => TryBreakthroughSlot(slotIndex));
                CreateStretchText(breakthroughRect, "Label", 19, TextAnchor.MiddleCenter).text = "돌파";

                RectTransform exchangeRect = CreatePanel(slot, "ShardExchangeButton", new Color(0.22f, 0.32f, 0.5f, 1f));
                SetPanelAnchors(exchangeRect, new Vector2(0.74f, 0.06f), new Vector2(0.98f, 0.47f), Vector2.zero, Vector2.zero);
                shardExchangeButtons[index] = exchangeRect.gameObject.AddComponent<Button>();
                shardExchangeButtons[index].targetGraphic = exchangeRect.GetComponent<Image>();
                shardExchangeButtons[index].onClick.AddListener(() => ExchangeShardForSlot(slotIndex));
                CreateStretchText(exchangeRect, "Label", 17, TextAnchor.MiddleCenter).text = "공용 2→1";
            }

            BuildSpiritRoster(overlay);

            summonResultText = CreateAnchoredText(overlay, "SummonResult", 18, TextAnchor.MiddleCenter, 60f, -510f, -60f, -580f);
            summonResultText.rectTransform.anchorMin = new Vector2(0.05f, 0.18f);
            summonResultText.rectTransform.anchorMax = new Vector2(0.95f, 0.225f);
            summonResultText.rectTransform.offsetMin = Vector2.zero;
            summonResultText.rectTransform.offsetMax = Vector2.zero;

            RectTransform summonRect = CreatePanel(overlay, "SummonButton", new Color(0.46f, 0.2f, 0.68f, 1f));
            summonRect.anchorMin = new Vector2(0.12f, 0.125f);
            summonRect.anchorMax = new Vector2(0.49f, 0.18f);
            summonRect.offsetMin = Vector2.zero;
            summonRect.offsetMax = Vector2.zero;
            summonButton = summonRect.gameObject.AddComponent<Button>();
            summonButton.targetGraphic = summonRect.GetComponent<Image>();
            summonButton.onClick.AddListener(() => battle?.SummonSpirit());
            summonButtonText = CreateStretchText(summonRect, "Label", 25, TextAnchor.MiddleCenter);

            RectTransform summonTenRect = CreatePanel(overlay, "SummonTenButton", new Color(0.58f, 0.22f, 0.72f, 1f));
            summonTenRect.anchorMin = new Vector2(0.51f, 0.125f);
            summonTenRect.anchorMax = new Vector2(0.88f, 0.18f);
            summonTenRect.offsetMin = Vector2.zero;
            summonTenRect.offsetMax = Vector2.zero;
            summonTenButton = summonTenRect.gameObject.AddComponent<Button>();
            summonTenButton.targetGraphic = summonTenRect.GetComponent<Image>();
            summonTenButton.onClick.AddListener(() => battle?.SummonTenSpirits());
            CreateStretchText(summonTenRect, "Label", 25, TextAnchor.MiddleCenter).text = "10회 소환  1,000";

            ownedSpiritsText = CreateAnchoredText(overlay, "OwnedSpirits", 22, TextAnchor.MiddleCenter, 60f, -1180f, -60f, -1300f);
            ownedSpiritsText.rectTransform.anchorMin = new Vector2(0.05f, 0.09f);
            ownedSpiritsText.rectTransform.anchorMax = new Vector2(0.95f, 0.12f);
            ownedSpiritsText.rectTransform.offsetMin = Vector2.zero;
            ownedSpiritsText.rectTransform.offsetMax = Vector2.zero;

            RectTransform closeRect = CreatePanel(overlay, "CloseFormationButton", new Color(0.34f, 0.18f, 0.58f, 1f));
            closeRect.anchorMin = new Vector2(0.18f, 0.025f);
            closeRect.anchorMax = new Vector2(0.82f, 0.075f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            Button closeButton = closeRect.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeRect.GetComponent<Image>();
            closeButton.onClick.AddListener(() => formationPopup.SetActive(false));
            CreateStretchText(closeRect, "Label", 28, TextAnchor.MiddleCenter).text = "편성 완료";
            formationPopup.SetActive(false);
        }

        private void BuildSummonInfoPopup()
        {
            RectTransform overlay = CreatePanel(safeAreaRoot, "SummonInfoOverlay", new Color(0.015f, 0.018f, 0.04f, 0.99f));
            SetPanelAnchors(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            summonInfoPopup = overlay.gameObject;
            CreateAnchoredText(overlay, "Title", 40, TextAnchor.MiddleCenter, 40f, -90f, -40f, -165f).text = "소환 확률 및 기록";
            summonInfoText = CreateAnchoredText(overlay, "Info", 24, TextAnchor.UpperLeft, 70f, -210f, -70f, -330f);
            summonInfoText.rectTransform.anchorMin = new Vector2(0.06f, 0.15f);
            summonInfoText.rectTransform.anchorMax = new Vector2(0.94f, 0.86f);
            summonInfoText.rectTransform.offsetMin = Vector2.zero;
            summonInfoText.rectTransform.offsetMax = Vector2.zero;
            RectTransform closeRect = CreatePanel(overlay, "Close", new Color(0.34f, 0.18f, 0.58f, 1f));
            SetPanelAnchors(closeRect, new Vector2(0.18f, 0.05f), new Vector2(0.82f, 0.11f), Vector2.zero, Vector2.zero);
            Button closeButton = closeRect.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeRect.GetComponent<Image>();
            closeButton.onClick.AddListener(() => summonInfoPopup.SetActive(false));
            CreateStretchText(closeRect, "Label", 27, TextAnchor.MiddleCenter).text = "확인";
            summonInfoPopup.SetActive(false);
        }

        private void OpenSummonInfo()
        {
            if (battle == null || summonInfoPopup == null) return;
            summonInfoText.text = $"현재 소환 확률 (모든 정령 SSR)\n{battle.GetSummonProbabilityLabel()}\n\n최근 소환 기록 (최대 100회 저장, 최근 20회 표시)\n{battle.GetSummonHistoryLabel(20)}";
            OpenFullScreenPopup(summonInfoPopup);
        }

        private void BuildSpiritRoster(RectTransform overlay)
        {
            RectTransform viewport = CreatePanel(overlay, "SpiritRosterViewport", new Color(0.055f, 0.06f, 0.11f, 1f));
            SetPanelAnchors(viewport, new Vector2(0.06f, 0.225f), new Vector2(0.94f, 0.42f), Vector2.zero, Vector2.zero);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            RectTransform content = CreateRect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewport;
            scrollRect.content = content;

            rosterSpirits.Clear();
            rosterSpiritTexts.Clear();
            rosterSpiritButtons.Clear();
            foreach (PrototypeSpiritData spirit in PrototypeSpiritCatalog.GetAll())
            {
                rosterSpirits.Add(spirit);
                RectTransform row = CreatePanel(content, $"Roster_{spirit.Id}", new Color(0.13f, 0.1f, 0.21f, 1f));
                LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 86f;
                layoutElement.minHeight = 86f;
                Button button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row.GetComponent<Image>();
                string spiritId = spirit.Id;
                button.onClick.AddListener(() => AssignRosterSpirit(spiritId));
                rosterSpiritButtons.Add(button);
                rosterSpiritTexts.Add(CreateStretchText(row, "Label", 21, TextAnchor.MiddleLeft));
                rosterSpiritTexts[^1].rectTransform.offsetMin = new Vector2(24f, 0f);
            }
        }

        private void SelectFormationSlot(int slotIndex)
        {
            selectedFormationSlot = Mathf.Clamp(slotIndex, 0, formationSlotTexts.Length - 1);
            RefreshFormationInfo();
        }

        private void AssignRosterSpirit(string spiritId)
        {
            if (battle == null || !battle.IsSpiritOwned(spiritId)) return;
            battle.AssignSpiritToSlot(selectedFormationSlot, spiritId);
            RefreshFormationInfo();
        }

        private void UnassignFormationSlot(int slotIndex)
        {
            if (battle == null) return;
            battle.UnassignSpiritSlot(slotIndex);
            RefreshFormationInfo();
        }

        private void BuildGrowthPopup()
        {
            RectTransform overlay = CreatePanel(safeAreaRoot, "GrowthOverlay", new Color(0.015f, 0.018f, 0.04f, 0.98f));
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            growthPopup = overlay.gameObject;

            CreateAnchoredText(overlay, "Title", 40, TextAnchor.MiddleCenter, 30f, -80f, -30f, -145f).text = "성장";
            growthCurrencyText = CreateAnchoredText(overlay, "Currencies", 23, TextAnchor.MiddleCenter, 30f, -150f, -30f, -230f);

            CreateGrowthButton(overlay, "ProtagonistAttack", new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.82f),
                () => battle?.TryPurchaseUpgrade(),
                () => $"주인공 평타 Lv.{battle.UpgradeLevel}  ·  공격력 {battle.ProtagonistDamage:0.0} → {battle.NextProtagonistDamage:0.0}  ·  {battle.UpgradeCost} 골드");
            CreateGrowthButton(overlay, "BattleCommand", new Vector2(0.08f, 0.685f), new Vector2(0.49f, 0.745f),
                () => battle?.TryPurchaseProtagonistSupportUpgrade(true),
                () => $"전투 지휘 Lv.{battle.ProtagonistBattleCommandLevel}  ·  공격력 +{battle.BattleCommandBonusPercent:0}% → +{battle.NextBattleCommandBonusPercent:0}%  ·  {battle.GetProtagonistSupportUpgradeCost(true)} 골드");
            CreateGrowthButton(overlay, "SpiritHaste", new Vector2(0.51f, 0.685f), new Vector2(0.92f, 0.745f),
                () => battle?.TryPurchaseProtagonistSupportUpgrade(false),
                () => battle.ProtagonistSpiritHasteLevel >= IdleBattlePrototype.MaximumSpiritHasteLevel
                    ? $"정령 가속 Lv.{battle.ProtagonistSpiritHasteLevel}  ·  공격 간격 {battle.SpiritHastePercent:0}%  ·  MAX"
                    : $"정령 가속 Lv.{battle.ProtagonistSpiritHasteLevel}  ·  공격 간격 {battle.SpiritHastePercent:0}% → {battle.NextSpiritHastePercent:0}%  ·  {battle.GetProtagonistSupportUpgradeCost(false)} 골드");

            PrototypeSpiritTrainingStat[] stats =
            {
                PrototypeSpiritTrainingStat.Attack, PrototypeSpiritTrainingStat.Defense,
                PrototypeSpiritTrainingStat.AttackSpeed, PrototypeSpiritTrainingStat.MaximumHealth,
                PrototypeSpiritTrainingStat.CriticalChance, PrototypeSpiritTrainingStat.CriticalDamage
            };
            string[] labels = { "공격력", "방어력", "공격속도", "최대 HP", "치명타 확률", "치명타 피해" };
            for (int index = 0; index < stats.Length; index++)
            {
                int capturedIndex = index;
                int row = index / 2;
                int column = index % 2;
                float left = column == 0 ? 0.08f : 0.51f;
                float right = column == 0 ? 0.49f : 0.92f;
                float top = 0.62f - row * 0.07f;
                Button button = CreateGrowthButton(overlay, $"SpiritTraining{stats[index]}", new Vector2(left, top - 0.055f), new Vector2(right, top),
                    () => battle?.TryPurchaseSpiritTraining(stats[capturedIndex]),
                    () => $"{labels[capturedIndex]} Lv.{battle.GetSpiritTrainingLevel(stats[capturedIndex])}  ·  " +
                        $"{battle.GetSpiritTrainingEffectLabel(stats[capturedIndex], false)} → {battle.GetSpiritTrainingEffectLabel(stats[capturedIndex], true)}  ·  " +
                        $"{battle.GetSpiritTrainingCost(stats[capturedIndex])} 강화석");
                spiritTrainingButtonTexts[index] = button.GetComponentInChildren<Text>();
            }

            growthSelectedSpiritText = CreateAnchoredText(overlay, "SelectedSpirit", 23, TextAnchor.MiddleCenter, 50f, -1020f, -50f, -1090f);
            growthSelectedSpiritText.rectTransform.anchorMin = new Vector2(0.05f, 0.34f);
            growthSelectedSpiritText.rectTransform.anchorMax = new Vector2(0.95f, 0.39f);
            growthSelectedSpiritText.rectTransform.offsetMin = Vector2.zero;
            growthSelectedSpiritText.rectTransform.offsetMax = Vector2.zero;
            CreateGrowthButton(overlay, "PreviousSpirit", new Vector2(0.08f, 0.27f), new Vector2(0.25f, 0.33f),
                () => { selectedSpiritSlot = (selectedSpiritSlot + 2) % 3; }, () => "이전 정령");
            CreateGrowthButton(overlay, "SkillPower", new Vector2(0.27f, 0.27f), new Vector2(0.61f, 0.33f),
                () => UpgradeSelectedSpirit(PrototypeSpiritSpecialGrowthType.SkillPower), () => GetSelectedSpiritGrowthLabel(true));
            CreateGrowthButton(overlay, "Cooldown", new Vector2(0.63f, 0.27f), new Vector2(0.92f, 0.33f),
                () => UpgradeSelectedSpirit(PrototypeSpiritSpecialGrowthType.CooldownReduction), () => GetSelectedSpiritGrowthLabel(false));

            RectTransform closeRect = CreatePanel(overlay, "CloseGrowthButton", new Color(0.34f, 0.18f, 0.58f, 1f));
            SetPanelAnchors(closeRect, new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.15f), Vector2.zero, Vector2.zero);
            Button closeButton = closeRect.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeRect.GetComponent<Image>();
            closeButton.onClick.AddListener(() => growthPopup.SetActive(false));
            CreateStretchText(closeRect, "Label", 28, TextAnchor.MiddleCenter).text = "성장 완료";
            growthPopup.SetActive(false);
        }

        private Button CreateGrowthButton(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            UnityEngine.Events.UnityAction action, System.Func<string> labelProvider)
        {
            RectTransform rect = CreatePanel(parent, name, new Color(0.22f, 0.15f, 0.38f, 1f));
            SetPanelAnchors(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(action);
            Text label = CreateStretchText(rect, "Label", 20, TextAnchor.MiddleCenter);
            label.gameObject.AddComponent<PrototypeDynamicText>().Initialize(labelProvider);
            return button;
        }

        private void UpgradeSelectedSpirit(PrototypeSpiritSpecialGrowthType type)
        {
            PrototypeSpiritData spirit = battle?.GetSpiritData(selectedSpiritSlot);
            if (spirit != null) battle.TryPurchaseSpiritSpecialGrowth(spirit.Id, type);
        }

        private string GetSelectedSpiritGrowthLabel(bool skillPower)
        {
            PrototypeSpiritData spirit = battle?.GetSpiritData(selectedSpiritSlot);
            if (spirit == null) return "정령 없음";
            PrototypeSpiritSpecialGrowthType type = skillPower
                ? PrototypeSpiritSpecialGrowthType.SkillPower
                : PrototypeSpiritSpecialGrowthType.CooldownReduction;
            int level = battle.GetSpiritSpecialGrowthLevel(spirit.Id, type);
            if (level >= PrototypeSpiritSpecialGrowthSystem.MaximumLevel)
                return $"{(skillPower ? "스킬 효과" : "쿨타임 감소")} Lv.{level} · MAX";
            float currentValue = skillPower ? level * 8f : level * 3f;
            float nextValue = skillPower ? (level + 1) * 8f : (level + 1) * 3f;
            return $"{(skillPower ? "스킬 효과" : "쿨타임 감소")} Lv.{level}  ·  +{currentValue:0}% → +{nextValue:0}%  ·  정령 강화석 1";
        }

        private void SetInformationMode(bool showsCharacterInfo)
        {
            combatInfoContent.SetActive(!showsCharacterInfo);
            characterInfoContent.SetActive(showsCharacterInfo);
            combatInfoTabButton.targetGraphic.color = showsCharacterInfo ? new Color(0.16f, 0.12f, 0.25f, 1f) : new Color(0.34f, 0.18f, 0.58f, 1f);
            characterInfoTabButton.targetGraphic.color = showsCharacterInfo ? new Color(0.34f, 0.18f, 0.58f, 1f) : new Color(0.16f, 0.12f, 0.25f, 1f);
            if (showsCharacterInfo) RefreshCharacterInfo();
        }

        private void SelectSpiritSlot(int slotIndex)
        {
            selectedSpiritSlot = slotIndex;
            RefreshCharacterInfo();
        }

        private void TryBreakthroughSlot(int slotIndex)
        {
            PrototypeSpiritData spirit = battle?.GetSpiritData(slotIndex);
            if (spirit != null) battle.TryBreakthroughSpirit(spirit.Id);
        }

        private void ExchangeShardForSlot(int slotIndex)
        {
            PrototypeSpiritData spirit = battle?.GetSpiritData(slotIndex);
            if (spirit != null) battle.ExchangeSsrCommonShard(spirit.Id);
        }

        private static RectTransform CreateInfoCard(RectTransform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform card = CreatePanel(parent, objectName, color);
            card.anchorMin = anchorMin;
            card.anchorMax = anchorMax;
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;
            return card;
        }

        private Text CreateCardText(RectTransform parent, string objectName, int fontSize)
        {
            Text text = CreateText(parent, objectName, fontSize, TextAnchor.UpperLeft);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24f, 24f);
            rect.offsetMax = new Vector2(-24f, -24f);
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void RefreshHud()
        {
            if (battle == null || stageText == null) return;
            stageText.text = $"STAGE {battle.Stage} [선택]  ·  전투 {battle.Wave}/10  ·  최고 {battle.HighestClearedStage}";
            currencyText.text = $"골드 {battle.Gold:N0}  ·  정령석 {battle.SpiritStones:N0}  ·  훈련 +{battle.UpgradeLevel}";
            levelText.text = $"주인공 Lv.{battle.ProtagonistLevel} EXP {battle.ProtagonistExperience}/{PrototypeGrowthCalculator.GetRequiredExperience(battle.ProtagonistLevel)}\n" +
                $"① {GetSpiritLevelSummary(0)}  ·  ② {GetSpiritLevelSummary(1)}  ·  ③ {GetSpiritLevelSummary(2)}";
            messageText.text = battle.BattleMessage;
            if (battle.RewardSequence != displayedRewardSequence)
            {
                displayedRewardSequence = battle.RewardSequence;
                rewardText.text = battle.RewardMessage;
                rewardBannerRemaining = 2.5f;
                rewardBanner.SetActive(true);
                rewardBanner.transform.SetAsLastSibling();
            }
            enemyNameText.text = battle.IsBoss
                ? $"[{battle.EnemyElementName}] {battle.EnemyDisplayName}  ·  제한 {battle.BossTimeRemaining:0.0}초"
                : $"[{battle.EnemyElementName}] {battle.EnemyDisplayName}";
            SetBar(teamHealthFill, teamHealthText, battle.TeamHealth, battle.TeamMaxHealth, "아군 HP");
            SetBar(enemyHealthFill, enemyHealthText, battle.EnemyHealth, battle.EnemyMaxHealth, "적 HP");
            enemyTargetsText.text = $"{GetEnemyTargetSummary()}  ·  {battle.StageReadinessLabel}";
            SetBar(ultimateFill, ultimateText, battle.UltimateEnergy, battle.UltimateEnergyMaximum, "아르카 궁극기");
            string overcharge = battle.OverchargeRemaining > 0f ? $"과충전 {battle.OverchargeRemaining:0.0}초" : $"과충전 대기 {battle.OverchargeCooldownRemaining:0.0}초";
            skillText.text = $"라이트닝 오브 {battle.LightningOrbCooldownRemaining:0.0}초  ·  {overcharge}";
            string command = battle.BattleCommandRemaining > 0f ? $"전투 지휘 {battle.BattleCommandRemaining:0.0}초" : $"전투 지휘 대기 {battle.BattleCommandCooldownRemaining:0.0}초";
            string haste = battle.SpiritHasteRemaining > 0f ? $"정령 가속 {battle.SpiritHasteRemaining:0.0}초" : $"정령 가속 대기 {battle.SpiritHasteCooldownRemaining:0.0}초";
            supportText.text = $"{command}  ·  {haste}\n{battle.DefenseStatus}";
            attackText.text = $"공격력  주인공 {battle.ProtagonistDamage:0}  ·  ① {battle.GetSpiritAttackPower(0):0}  ·  ② {battle.GetSpiritAttackPower(1):0}";
            formationText.text = $"① {battle.GetSpiritStatusLabel(0)}\n② {battle.GetSpiritStatusLabel(1)}\n③ {battle.GetSpiritStatusLabel(2)}";
            evolutionText.text = $"성장  ① {battle.GetSpiritEvolutionName(0)}  ·  ② {battle.GetSpiritEvolutionName(1)}  ·  ③ {battle.GetSpiritEvolutionName(2)}";
            previousStageButton.interactable = battle.CanSelectPreviousStage;
            nextStageButton.interactable = battle.CanSelectNextStage;
            autoChallengeButtonText.text = battle.IsAutoChallengeEnabled ? "자동 도전 ON" : "반복 사냥";
            offlineReportText.text = battle.OfflineReportMessage;
            offlinePopup.SetActive(battle.IsOfflineReportVisible);
            RefreshFormationInfo();
            if (characterInfoContent.activeSelf) RefreshCharacterInfo();
            if (formationPopup.activeSelf) formationPopup.transform.SetAsLastSibling();
            if (summonInfoPopup.activeSelf) summonInfoPopup.transform.SetAsLastSibling();
            if (growthPopup.activeSelf)
            {
                PrototypeSpiritData selectedGrowthSpirit = battle.GetSpiritData(selectedSpiritSlot);
                growthCurrencyText.text = $"골드 {battle.Gold:N0}  ·  강화석 {battle.EnhancementStones:N0}  ·  정령 강화석 {battle.SpiritUpgradeStones:N0}\n" +
                    $"드롭: 일반 몬스터 강화석 {battle.NormalEnhancementStoneDropPercent:0}%  ·  보스 정령 강화석 {battle.BossSpiritUpgradeStoneDropPercent:0}%";
                growthSelectedSpiritText.text = selectedGrowthSpirit == null
                    ? $"{selectedSpiritSlot + 1}번 슬롯 · 정령 없음"
                    : $"{selectedSpiritSlot + 1}번 슬롯 · {selectedGrowthSpirit.DisplayName} · 정령 강화석 {battle.SpiritUpgradeStones}";
                growthPopup.transform.SetAsLastSibling();
            }
            if (offlinePopup.activeSelf) offlinePopup.transform.SetAsLastSibling();
        }

        private void RefreshFormationInfo()
        {
            if (formationSlotTexts[0] == null) return;
            ownedSpiritsText.text = $"보유 정령  ·  {battle.GetOwnedSpiritSummary()}  ·  SSR 공용 조각 {battle.SsrCommonShards}";
            summonButtonText.text = $"정령 소환  {battle.SpiritSummonCost} 정령석  ·  보유 {battle.SpiritStones}";
            summonButton.interactable = battle.CanSummonSpirit;
            summonTenButton.interactable = battle.CanSummonTenSpirits;
            summonResultText.text = battle.SummonResultMessage;
            for (int index = 0; index < formationSlotTexts.Length; index++)
            {
                PrototypeSpiritData spirit = battle.GetSpiritData(index);
                formationSlotTexts[index].text = spirit == null
                    ? $"{(index == selectedFormationSlot ? "▶ " : string.Empty)}{index + 1}번 슬롯  ·  미등록"
                    : $"{(index == selectedFormationSlot ? "▶ " : string.Empty)}{index + 1}번 · SSR {spirit.DisplayName}\nLv.{battle.GetSpiritLevel(index)} · {battle.GetSpiritBreakthrough(spirit.Id)}돌파 · 조각 {battle.GetSpiritShards(spirit.Id)}";
                breakthroughButtons[index].interactable = spirit != null && battle.CanBreakthroughSpirit(spirit.Id);
                shardExchangeButtons[index].interactable = spirit != null && battle.CanExchangeSsrCommonShard(spirit.Id);
                formationRemoveButtons[index].interactable = spirit != null;
            }
            for (int index = 0; index < rosterSpirits.Count; index++)
            {
                PrototypeSpiritData spirit = rosterSpirits[index];
                bool isOwned = battle.IsSpiritOwned(spirit.Id);
                int assignedSlot = battle.GetAssignedSlotIndex(spirit.Id);
                rosterSpiritButtons[index].interactable = isOwned;
                rosterSpiritTexts[index].text = isOwned
                    ? $"SSR {spirit.DisplayName}  ·  {GetElementName(spirit.Element)}  ·  {GetRoleName(spirit.CombatRole)}  ·  Lv.{battle.GetSpiritLevelById(spirit.Id)}" +
                      (assignedSlot >= 0 ? $"  ·  {assignedSlot + 1}번 슬롯 편성 중" : "  ·  대기")
                    : $"🔒 미보유  ·  SSR ???  ·  {GetElementName(spirit.Element)}  ·  {GetRoleName(spirit.CombatRole)}";
            }
        }

        private string GetSpiritLevelSummary(int slotIndex)
        {
            PrototypeSpiritData spirit = battle.GetSpiritData(slotIndex);
            if (spirit == null) return "미등록";
            int level = battle.GetSpiritLevel(slotIndex);
            return $"{spirit.DisplayName} Lv.{level}";
        }

        private string GetEnemyTargetSummary()
        {
            if (battle.EnemyTargetCount <= 1) return "보스 단일 대상";
            List<string> targets = new();
            for (int index = 0; index < battle.EnemyTargetCount; index++)
            {
                float health = battle.GetEnemyTargetHealth(index);
                float maximumHealth = battle.GetEnemyTargetMaximumHealth(index);
                string marker = index == battle.PrimaryEnemyTargetIndex ? "▶" : string.Empty;
                string state = health > 0f ? $"{health:0}/{maximumHealth:0}" : "처치";
                targets.Add($"{marker}적{index + 1} {state}");
            }
            return string.Join("  ·  ", targets);
        }

        private void RefreshCharacterInfo()
        {
            if (protagonistInfoText == null) return;
            protagonistInfoText.text =
                $"주인공  Lv.{battle.ProtagonistLevel}  EXP {battle.ProtagonistExperience}/{PrototypeGrowthCalculator.GetRequiredExperience(battle.ProtagonistLevel)}   " +
                $"평타 공격 {battle.ProtagonistDamage:0}   전투 지휘 +{battle.ProtagonistBattleCommandLevel}   정령 가속 +{battle.ProtagonistSpiritHasteLevel}";

            for (int index = 0; index < spiritInfoTexts.Length; index++)
            {
                PrototypeSpiritData spirit = battle.GetSpiritData(index);
                if (spirit == null)
                {
                    spiritInfoTexts[index].text = $"{index + 1}번 슬롯\n\n미등록";
                    continue;
                }

                int level = battle.GetSpiritLevel(index);
                spiritInfoTexts[index].text =
                    $"{index + 1}번 슬롯 · SSR\n{spirit.DisplayName}\n" +
                    $"Lv.{level}  {GetElementName(spirit.Element)}\n" +
                    $"HP {battle.GetSpiritCurrentHealth(index):0}/{battle.GetSpiritMaximumHealth(index):0}\n" +
                    $"공격 {battle.GetSpiritAttackPower(index):0}  방어 {battle.GetSpiritDefense(index):0}\n" +
                    $"궁극기 {battle.GetSpiritUltimateEnergy(index):0}/{spirit.UltimateEnergyMaximum:0}\n" +
                    $"{battle.GetSpiritEvolutionName(index)} · {battle.GetSpiritBreakthrough(spirit.Id)}돌파";
            }

            PrototypeSpiritData selectedSpirit = battle.GetSpiritData(selectedSpiritSlot);
            int selectedLevel = selectedSpirit == null ? 1 : battle.GetSpiritLevel(selectedSpiritSlot);
            int selectedSkillGrowth = selectedSpirit == null ? 0 : battle.GetSpiritSpecialGrowthLevel(selectedSpirit.Id, PrototypeSpiritSpecialGrowthType.SkillPower);
            int selectedCooldownGrowth = selectedSpirit == null ? 0 : battle.GetSpiritSpecialGrowthLevel(selectedSpirit.Id, PrototypeSpiritSpecialGrowthType.CooldownReduction);
            float selectedCooldownMultiplier = Mathf.Max(0.7f, 1f - selectedCooldownGrowth * 0.03f);
            selectedSpiritDetailText.text = selectedSpirit == null
                ? $"{selectedSpiritSlot + 1}번 슬롯에 등록된 정령이 없습니다."
                : $"선택: SSR {selectedSpirit.DisplayName}  ·  {GetElementName(selectedSpirit.Element)}  ·  {GetRoleName(selectedSpirit.CombatRole)}\n" +
                  $"Lv.{selectedLevel}  EXP {battle.GetSpiritExperience(selectedSpiritSlot)}/{PrototypeGrowthCalculator.GetRequiredExperience(selectedLevel)}  ·  {battle.GetSpiritEvolutionName(selectedSpiritSlot)}\n" +
                  $"HP {battle.GetSpiritCurrentHealth(selectedSpiritSlot):0}/{battle.GetSpiritMaximumHealth(selectedSpiritSlot):0}  ·  공격 {battle.GetSpiritAttackPower(selectedSpiritSlot):0}  ·  방어 {battle.GetSpiritDefense(selectedSpiritSlot):0}  ·  공격 간격 {battle.GetSpiritDisplayedAttackInterval(selectedSpiritSlot):0.00}초\n" +
                  $"평타 {selectedSpirit.BasicAttack.DisplayName}  ·  궁극기 {selectedSpirit.Ultimate.DisplayName}\n" +
                  $"스킬1 {selectedSpirit.SkillOne.DisplayName}  쿨 {selectedSpirit.SkillOne.Cooldown * selectedCooldownMultiplier:0.0}초  ·  스킬2 {selectedSpirit.SkillTwo.DisplayName}  쿨 {selectedSpirit.SkillTwo.Cooldown * selectedCooldownMultiplier:0.0}초\n" +
                  $"돌파 {battle.GetSpiritBreakthrough(selectedSpirit.Id)}/6  ·  돌파 조각 {battle.GetSpiritShards(selectedSpirit.Id)}  ·  스킬 효과 +{selectedSkillGrowth * 8}%  ·  쿨타임 감소 {selectedCooldownGrowth * 3}%";
        }

        private static string GetElementName(SpiritElement element)
        {
            return element switch
            {
                SpiritElement.Fire => "불속성",
                SpiritElement.Water => "물속성",
                SpiritElement.Wind => "바람속성",
                SpiritElement.Lightning => "번개속성",
                SpiritElement.Light => "빛속성",
                SpiritElement.Dark => "어둠속성",
                _ => "무속성"
            };
        }

        private static string GetRoleName(SpiritCombatRole role)
        {
            return role switch
            {
                SpiritCombatRole.MeleeAttack => "근거리 공격형",
                SpiritCombatRole.RangedAttack => "원거리 공격형",
                SpiritCombatRole.Defense => "방어형",
                SpiritCombatRole.Support => "지원형",
                _ => "미지정"
            };
        }

        private void UpdateFloatingDamages()
        {
            for (int index = floatingDamages.Count - 1; index >= 0; index--)
            {
                FloatingDamage damage = floatingDamages[index];
                damage.RemainingTime -= Time.deltaTime;
                float progress = 1f - damage.RemainingTime / 0.75f;
                damage.Label.rectTransform.anchoredPosition = damage.StartPosition + Vector2.up * (progress * 90f);
                Color color = damage.Label.color;
                color.a = Mathf.Clamp01(damage.RemainingTime / 0.75f);
                damage.Label.color = color;
                if (damage.RemainingTime > 0f) continue;
                Destroy(damage.Label.gameObject);
                floatingDamages.RemoveAt(index);
            }
        }

        private void UpdateSafeArea(bool force = false)
        {
            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safeArea = Screen.safeArea;
            if (!force && safeArea == lastSafeArea) return;
            lastSafeArea = safeArea;
            safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        private Text CreateAnchoredText(RectTransform parent, string objectName, int fontSize, TextAnchor alignment, float left, float top, float right, float bottom)
        {
            Text text = CreateText(parent, objectName, fontSize, alignment);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
            return text;
        }

        private Text CreateText(Transform parent, string objectName, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = runtimeFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Text CreateStretchText(RectTransform parent, string objectName, int fontSize, TextAnchor alignment)
        {
            Text text = CreateText(parent, objectName, fontSize, alignment);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
        }

        private Image CreateBar(RectTransform parent, string objectName, Vector2 topLeft, Vector2 bottomRight, Color fillColor, out Text label)
        {
            RectTransform background = CreatePanel(parent, objectName, new Color(0.12f, 0.13f, 0.18f, 1f));
            background.anchorMin = new Vector2(0f, 1f);
            background.anchorMax = new Vector2(1f, 1f);
            background.offsetMin = new Vector2(topLeft.x, bottomRight.y);
            background.offsetMax = new Vector2(bottomRight.x, topLeft.y);
            RectTransform fillRect = CreatePanel(background, "Fill", fillColor);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            Image fill = fillRect.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            label = CreateStretchText(background, "Label", 23, TextAnchor.MiddleCenter);
            return fill;
        }

        private static void SetBar(Image fill, Text label, float current, float maximum, string prefix)
        {
            fill.fillAmount = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
            label.text = $"{prefix}  {current:0}/{maximum:0}";
        }

        private static RectTransform CreatePanel(Transform parent, string objectName, Color color)
        {
            GameObject panelObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            return panelObject.GetComponent<RectTransform>();
        }

        private static RectTransform CreateRect(Transform parent, string objectName)
        {
            GameObject rectObject = new(objectName, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private static void SetPanelAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetTopStretch(RectTransform rect, float height, float top)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(0f, -top - height);
            rect.offsetMax = new Vector2(0f, -top);
        }
    }
}
