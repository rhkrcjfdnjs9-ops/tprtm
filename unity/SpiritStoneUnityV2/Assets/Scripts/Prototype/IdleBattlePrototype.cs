using System;
using System.Collections;
using System.Collections.Generic;
using SpiritStone.Characters;
using SpiritStone.Core;
using SpiritStone.Characters.Arca;
using UnityEngine;
using UnityEngine.U2D;

namespace SpiritStone.Prototype
{
    [DisallowMultipleComponent]
    public sealed class IdleBattlePrototype : MonoBehaviour
    {
        public const int MaximumSpiritHasteLevel = 25;
        private enum BattleState
        {
            Encounter,
            Fighting,
            EnemyDefeated,
            BossTimedOut,
            Advancing,
            PartyDefeated
        }

        [Header("Battle")]
        [SerializeField, Min(1f)] private float protagonistAttack = 8f;
        [SerializeField, Min(0.1f)] private float protagonistAttackInterval = 1.2f;
        [SerializeField, Min(0.1f)] private float respawnDelay = 0.75f;
        [SerializeField, Min(0.1f)] private float encounterDuration = 0.45f;
        [SerializeField, Min(0.1f)] private float advanceDuration = 0.65f;
        [SerializeField, Min(0.1f)] private float projectileDuration = 0.2f;
        [SerializeField, Min(5f)] private float bossTimeLimit = 30f;
        [Header("Offline Progress")]
        [SerializeField, Range(1f, 24f)] private float maximumOfflineHours = 8f;
        [SerializeField, Range(0.1f, 1f)] private float offlineEfficiency = 0.6f;
        [Header("Protagonist Support")]
        [SerializeField, Min(0.1f)] private float battleCommandCooldown = 12f;
        [SerializeField, Min(0.1f)] private float battleCommandDuration = 5f;
        [SerializeField, Min(0f)] private float battleCommandAttackBonus = 0.3f;
        [SerializeField, Min(0.1f)] private float spiritHasteCooldown = 15f;
        [SerializeField, Min(0.1f)] private float spiritHasteDuration = 5f;
        [SerializeField, Range(0.1f, 1f)] private float spiritHasteIntervalMultiplier = 0.7f;
        [Header("Prototype Visuals")]
        [SerializeField] private bool showProtagonistVisual;
        [Header("Editor Death Preview")]
        [SerializeField] private bool enableLethalEnemyDamagePreview;
        [SerializeField, Min(1f)] private float lethalEnemyDamageMultiplier = 1f;

        private Transform protagonist;
        private Transform arca;
        private Transform enemy;
        private Transform[] enemyVisuals;
        private Transform[] spiritVisuals;
        private Transform[] arcaCoreVisuals;
        private SpriteRenderer[] arcaCoreRenderers;
        private ThunderCoreCombatController arcaCoreController;
        private SpriteRenderer protagonistRenderer;
        private SpriteRenderer[] spiritRenderers;
        private PixelCharacterView protagonistPixelView;
        private PixelCharacterView[] spiritPixelViews;
        private Sprite[] spiritPlaceholderSprites;
        private Sprite arcaPixelSprite;
        private SpriteRenderer arcaRenderer;
        private SpriteRenderer enemyRenderer;
        private SpriteRenderer[] enemyRenderers;
        private PixelEnemyView[] enemyPixelViews;
        private Texture2D squareTexture;
        private Sprite squareSprite;
        private readonly List<Texture2D> pixelCharacterTextures = new();
        private readonly List<Sprite> pixelCharacterSprites = new();
        private PrototypeBattleHud battleHud;
        private PrototypeCombatVfxSystem combatVfxSystem;
        private PrototypeSpiritSlot[] spiritSlots;
        private PrototypeSpiritSlot arcaSlot;
        private PrototypeSpiritData arcaSpirit;
        private PrototypePartyMemberState[] partyMembers;
        private PrototypeStageData currentStage;
        private PrototypeEnemyData currentEnemy;
        private PrototypeSpiritEvolutionData arcaEvolution;
        private int wave;
        private int gold;
        private int upgradeLevel;
        private int protagonistBattleCommandLevel;
        private int protagonistSpiritHasteLevel;
        private int protagonistLevel;
        private int protagonistExperience;
        private readonly PrototypeSpiritGrowthSystem spiritGrowth = new();
        private readonly PrototypeSummonSystem summonSystem = new();
        private readonly PrototypeFormationSystem formationSystem = new();
        private readonly PrototypeStageProgression stageProgression = new();
        private readonly PrototypeBattleSystem battleSystem = new();
        private readonly PrototypeSpiritTrainingSystem spiritTrainingSystem = new();
        private readonly PrototypeSpiritSpecialGrowthSystem spiritSpecialGrowthSystem = new();
        private readonly PrototypeCombatTargetSystem combatTargetSystem = new();
        private readonly PrototypeCombatStatusSystem combatStatusSystem = new();
        private PrototypeGameStateSaveSystem gameStateSaveSystem;
        private string summonResultMessage = "소환할 정령을 기다리고 있습니다.";
        private float teamShield;
        private PrototypeSpiritSlot shieldSourceSlot;
        private float battleCommandTimer;
        private float battleCommandRemaining;
        private float spiritHasteTimer;
        private float spiritHasteRemaining;
        private int enemyAttackSequence;
        private int lastEnemyHitCount;
        private bool wasLastEnemyAttackArea;
        private bool isEnemyActing;
        private float bossTimeRemaining;
        private bool isTransitioning;
        private BattleState battleState;
        private string battleMessage = "전투 준비";
        private string rewardMessage = string.Empty;
        private int rewardSequence;
        private bool isOfflineReportVisible;
        private string offlineReportMessage = string.Empty;
        private bool wasSentToBackground;

        public int Stage => stageProgression.Stage;
        public int HighestClearedStage => stageProgression.HighestClearedStage;
        public bool IsAutoChallengeEnabled => stageProgression.IsAutoChallengeEnabled;
        public bool CanSelectPreviousStage => stageProgression.CanSelectPrevious;
        public bool CanSelectNextStage => stageProgression.CanSelectNext;
        public int Wave => wave;
        public int Gold => gold;
        public int SpiritStones => summonSystem.SpiritStones;
        public int SsrCommonShards => summonSystem.SsrCommonShards;
        public int SpiritSummonCost => PrototypeSummonSystem.SummonCost;
        public bool CanSummonSpirit => summonSystem.CanSummon;
        public bool CanSummonTenSpirits => summonSystem.CanSummonTen;
        public string SummonResultMessage => summonResultMessage;
        public int UpgradeLevel => upgradeLevel;
        public int ProtagonistBattleCommandLevel => protagonistBattleCommandLevel;
        public int ProtagonistSpiritHasteLevel => protagonistSpiritHasteLevel;
        public int SpiritUpgradeStones => spiritTrainingSystem.UpgradeStones;
        public int EnhancementStones => spiritTrainingSystem.EnhancementStones;
        public float NormalEnhancementStoneDropPercent => (currentStage?.NormalEnhancementStoneDropChance ?? 0f) * 100f;
        public float BossSpiritUpgradeStoneDropPercent => (currentStage?.BossSpiritUpgradeStoneDropChance ?? 0f) * 100f;
        public int ProtagonistLevel => protagonistLevel;
        public int ProtagonistExperience => protagonistExperience;
        public int ArcaLevel => GetSpiritProgress("arca")?.Level ?? 1;
        public int ArcaExperience => GetSpiritProgress("arca")?.Experience ?? 0;
        public float EnemyHealth => battleSystem.EnemyHealth;
        public float EnemyMaxHealth => battleSystem.EnemyMaximumHealth;
        public int EnemyTargetCount => combatTargetSystem.Count;
        public int AliveEnemyTargetCount => combatTargetSystem.AliveCount;
        public int PrimaryEnemyTargetIndex => combatTargetSystem.PrimaryTargetIndex;
        public int EnemyAttackSequence => enemyAttackSequence;
        public int LastEnemyHitCount => lastEnemyHitCount;
        public bool WasLastEnemyAttackArea => wasLastEnemyAttackArea;
        public float TeamHealth => GetTeamHealth(false);
        public float TeamMaxHealth => GetTeamHealth(true);
        public float TeamShield => teamShield;
        public float UltimateEnergy => arcaSlot?.UltimateEnergy ?? 0f;
        public float UltimateEnergyMaximum => arcaSpirit?.UltimateEnergyMaximum ?? 1f;
        public float ChainLightningCooldownRemaining => arcaSlot?.SkillOneCooldownRemaining ?? 0f;
        public float OverchargeCooldownRemaining => arcaSlot?.SkillTwoCooldownRemaining ?? 0f;
        public float OverchargeRemaining => arcaSlot?.SkillTwoRemaining ?? 0f;
        public float BattleCommandCooldownRemaining => Mathf.Max(0f, battleCommandTimer);
        public float BattleCommandRemaining => battleCommandRemaining;
        public float SpiritHasteCooldownRemaining => Mathf.Max(0f, spiritHasteTimer);
        public float SpiritHasteRemaining => spiritHasteRemaining;
        public float ProtagonistDamage => GetProtagonistDamage();
        public float NextProtagonistDamage => PrototypeBattleSystem.CalculateProtagonistDamage(protagonistAttack, protagonistLevel, upgradeLevel + 1);
        public float BattleCommandBonusPercent => GetBattleCommandAttackBonus() * 100f;
        public float NextBattleCommandBonusPercent => (battleCommandAttackBonus + (protagonistBattleCommandLevel + 1) * 0.02f) * 100f;
        public float SpiritHastePercent => GetSpiritHasteIntervalMultiplier() * 100f;
        public float NextSpiritHastePercent => Mathf.Max(0.45f, spiritHasteIntervalMultiplier - (protagonistSpiritHasteLevel + 1) * 0.01f) * 100f;
        public float ArcaDamage => GetArcaDamage();
        public int UpgradeCost => GetUpgradeCost();
        public bool CanPurchaseUpgrade => gold >= GetUpgradeCost();
        public bool IsBoss => currentEnemy?.IsBoss ?? wave == 10;
        public float BossTimeRemaining => Mathf.Max(0f, bossTimeRemaining);
        public float BossTimeLimit => bossTimeLimit;
        public string EnemyDisplayName => currentEnemy?.DisplayName ?? "적";
        public string EnemyElementName => currentEnemy == null ? "미지정" : PrototypeElementChart.GetDisplayName(currentEnemy.Element);
        public string ArcaEvolutionName => arcaEvolution?.DisplayName ?? "정령돌 1단계";
        public string ArcaEvolutionDescription => arcaEvolution?.Description ?? string.Empty;
        public string BattleMessage => battleMessage;
        public string RewardMessage => rewardMessage;
        public string CombatStatusSummary => combatStatusSystem.GetSummary();
        public int RewardSequence => rewardSequence;
        public string StageReadinessLabel => GetStageReadinessLabel();
        public bool IsOfflineReportVisible => isOfflineReportVisible;
        public string OfflineReportMessage => offlineReportMessage;
        public int SpiritSlotCount => spiritSlots?.Length ?? 0;
        public string DefenseStatus
        {
            get
            {
                float attackReduction = 1f - GetEnemyAttackMultiplier();
                float damageReduction = 1f - GetIncomingDamageMultiplier();
                string status = combatStatusSystem.GetSummary();
                if (teamShield <= 0f && attackReduction <= 0f && damageReduction <= 0f) return status;
                return $"보호막 {teamShield:0}  ·  적 공격력 감소 {attackReduction * 100f:0}%  ·  받는 피해 감소 {damageReduction * 100f:0}%  ·  {status}";
            }
        }

        private void Awake()
        {
            ConfigureBattleCamera();
            gameStateSaveSystem = new PrototypeGameStateSaveSystem(stageProgression, spiritGrowth, formationSystem, summonSystem,
                spiritTrainingSystem, spiritSpecialGrowthSystem);
            PrototypeSaveData saveData = PrototypeSaveService.Load();
            summonSystem.Initialize(saveData);
            InitializeSpiritFormation(saveData);
            stageProgression.Initialize(saveData);
            gold = saveData.Gold;
            upgradeLevel = saveData.UpgradeLevel;
            protagonistBattleCommandLevel = saveData.ProtagonistBattleCommandLevel;
            protagonistSpiritHasteLevel = Mathf.Clamp(saveData.ProtagonistSpiritHasteLevel, 0, MaximumSpiritHasteLevel);
            spiritTrainingSystem.Initialize(saveData);
            spiritSpecialGrowthSystem.Initialize(saveData);
            protagonistLevel = saveData.ProtagonistLevel;
            protagonistExperience = saveData.ProtagonistExperience;
            InitializeSpiritProgress(saveData);
            NormalizeLoadedExperience();
            arcaEvolution = arcaSpirit.GetEvolutionForLevel(ArcaLevel);
            battleHud = GetComponent<PrototypeBattleHud>();
            if (battleHud == null) battleHud = gameObject.AddComponent<PrototypeBattleHud>();
            battleHud.Initialize(this);
            ApplyOfflineProgress();
            CreatePrototypeWorld();
            combatVfxSystem = gameObject.AddComponent<PrototypeCombatVfxSystem>();
            combatVfxSystem.Initialize(squareSprite);
            StartWave(1);
        }

        private static void ConfigureBattleCamera()
        {
            Camera battleCamera = Camera.main;
            if (battleCamera == null) return;
            PixelPerfectCamera pixelPerfectCamera = battleCamera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfectCamera == null) return;
            pixelPerfectCamera.assetsPPU = 32;
            pixelPerfectCamera.refResolutionX = 216;
            pixelPerfectCamera.refResolutionY = 384;
            pixelPerfectCamera.upscaleRT = true;
            pixelPerfectCamera.pixelSnapping = true;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (isTransitioning || battleState != BattleState.Fighting || battleSystem.IsEnemyDefeated || IsPartyDefeated()) return;

            float deltaTime = Time.deltaTime;
            if (currentEnemy.IsBoss)
            {
                bossTimeRemaining = Mathf.Max(0f, bossTimeRemaining - deltaTime);
                if (bossTimeRemaining <= 0f)
                {
                    StartCoroutine(RestartAfterBossTimeout());
                    return;
                }
            }
            battleSystem.Tick(deltaTime);
            float burnDamage = combatStatusSystem.Tick(deltaTime);
            if (burnDamage > 0f && !battleSystem.IsEnemyDefeated)
                DealDamage(burnDamage, "화상 지속 피해");
            for (int index = 0; index < spiritSlots.Length; index++)
                spiritSlots[index].Tick(deltaTime);
            if (shieldSourceSlot != null && shieldSourceSlot.SkillTwoRemaining <= 0f)
            {
                teamShield = 0f;
                shieldSourceSlot = null;
            }
            battleCommandTimer -= deltaTime;
            battleCommandRemaining = Mathf.Max(0f, battleCommandRemaining - deltaTime);
            spiritHasteTimer -= deltaTime;
            spiritHasteRemaining = Mathf.Max(0f, spiritHasteRemaining - deltaTime);
            UpdateArcaColor();

            if (battleCommandTimer <= 0f) ActivateBattleCommand();
            if (spiritHasteTimer <= 0f) ActivateSpiritHaste();

            if (battleSystem.TryBeginProtagonistAttack(protagonistAttackInterval))
            {
                StartCoroutine(PerformProtagonistAttack());
            }

            UpdateSpiritActions();

            if (!combatStatusSystem.IsEnemyStunned && !isEnemyActing && battleSystem.TryBeginEnemyAttack())
                StartCoroutine(PerformEnemyAttack());
        }

        private IEnumerator PerformEnemyAttack()
        {
            isEnemyActing = true;
            enemyAttackSequence++;
            bool isBossAreaAttack = currentEnemy.Archetype == PrototypeEnemyArchetype.Boss && enemyAttackSequence % 3 == 0;
            PrototypeEnemyAttackPattern pattern = isBossAreaAttack
                ? PrototypeEnemyAttackPatternSystem.GetBossSpecialPattern(currentEnemy.Element)
                : PrototypeEnemyAttackPatternSystem.GetBasicPattern(currentEnemy.Archetype);
            lastEnemyHitCount = 0;
            wasLastEnemyAttackArea = pattern.TargetsAll;
            if (pattern.TargetsAll)
            {
                battleMessage = $"{currentEnemy.DisplayName} 특수 공격 · {pattern.DisplayName}!";
                Pulse(enemy, Vector3.left * 0.28f);
                yield return new WaitForSeconds(0.18f);
                if (battleState != BattleState.Fighting)
                {
                    isEnemyActing = false;
                    yield break;
                }
                for (int index = 0; index < partyMembers.Length; index++)
                {
                    PrototypePartyMemberState member = partyMembers[index];
                    if (member.IsAlive)
                    {
                        ApplyEnemyHit(member, pattern.DamageMultiplier, pattern.DisplayName);
                        lastEnemyHitCount++;
                    }
                }
            }
            else
            {
                for (int hitIndex = 0; hitIndex < pattern.HitCount; hitIndex++)
                {
                    PrototypePartyMemberState target = SelectEnemyTarget();
                    if (target == null) break;
                    string attackName = pattern.HitCount > 1
                        ? $"{pattern.DisplayName} {hitIndex + 1}/{pattern.HitCount}"
                        : pattern.DisplayName;
                    ApplyEnemyHit(target, pattern.DamageMultiplier, attackName);
                    lastEnemyHitCount++;
                    if (hitIndex + 1 < pattern.HitCount)
                    {
                        yield return new WaitForSeconds(0.1f);
                        if (battleState != BattleState.Fighting) break;
                    }
                }
            }

            isEnemyActing = false;
            if (IsPartyDefeated() && !isTransitioning) StartCoroutine(RestartAfterDefeat());
        }

        private void ApplyEnemyHit(PrototypePartyMemberState target, float damageMultiplier, string attackName)
        {
            PrototypeSpiritSlot targetSlot = Array.Find(spiritSlots,
                slot => slot.IsAssigned && slot.SpiritId.Equals(target.Id, StringComparison.OrdinalIgnoreCase));
            float elementMultiplier = targetSlot == null
                ? 1f
                : PrototypeElementChart.GetDamageMultiplier(currentEnemy.Element, targetSlot.Spirit.Element);
            string relationship = targetSlot == null
                ? string.Empty
                : $" · {PrototypeElementChart.GetRelationshipLabel(currentEnemy.Element, targetSlot.Spirit.Element)}";
            float rawDamage = battleSystem.EnemyAttackDamage * damageMultiplier * elementMultiplier
                * GetEnemyAttackMultiplier() * GetIncomingDamageMultiplier();
#if UNITY_EDITOR
            if (enableLethalEnemyDamagePreview) rawDamage *= lethalEnemyDamageMultiplier;
#endif
            float incomingDamage = target.CalculateDamage(rawDamage);
            float absorbedDamage = Mathf.Min(teamShield, incomingDamage);
            teamShield -= absorbedDamage;
            float healthDamage = incomingDamage - absorbedDamage;
            target.TakeDamage(healthDamage);
            UpdatePartyVisualState();
            if (healthDamage > 0f) battleHud.ShowDamage(healthDamage, false, target.Visual);
            if (healthDamage > 0f)
            {
                if (targetSlot != null && spiritPixelViews != null
                    && targetSlot.SlotIndex >= 0 && targetSlot.SlotIndex < spiritPixelViews.Length)
                {
                    PixelCharacterView targetView = spiritPixelViews[targetSlot.SlotIndex];
                    if (target.IsAlive) targetView?.PlayHit();
                    else targetView?.PlayDeath();
                    if (targetSlot == arcaSlot)
                    {
                        if (target.IsAlive) arcaCoreController?.PlayHit(0.24f);
                        else arcaCoreController?.PlayDeath();
                    }
                }
                combatVfxSystem?.PlayImpact(target.Visual, currentEnemy.Element,
                    currentEnemy.Archetype == PrototypeEnemyArchetype.Heavy || currentEnemy.IsBoss);
                if (target.IsAlive) combatVfxSystem?.Flash(target.Renderer, Color.white);
            }
            battleMessage = absorbedDamage > 0f
                ? $"{attackName}{relationship} · {target.DisplayName} 보호막이 {absorbedDamage:0} 피해 흡수"
                : target.IsAlive
                    ? $"{attackName}{relationship} · {target.DisplayName} {healthDamage:0} 피해"
                    : $"{attackName}{relationship} · {target.DisplayName} 전투 불능";
            Pulse(enemy, Vector3.left * (currentEnemy.Archetype == PrototypeEnemyArchetype.Heavy ? 0.28f : 0.18f));
            if (target.IsAlive) Pulse(target.Visual, Vector3.left * 0.08f);
        }

        private void CreatePrototypeWorld()
        {
            squareTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "PrototypeSquareTexture",
                filterMode = FilterMode.Point
            };
            squareTexture.SetPixel(0, 0, Color.white);
            squareTexture.Apply();
            squareSprite = Sprite.Create(squareTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            squareSprite.name = "PrototypeSquareSprite";

            protagonistPixelView = CreatePixelCharacter("Protagonist", GetProtagonistHomePosition(), new Color(0.2f, 0.65f, 1f), 3, 50);
            protagonistRenderer = protagonistPixelView.Renderer;
            protagonist = protagonistRenderer.transform;
            protagonistRenderer.enabled = showProtagonistVisual;
            PixelCharacterView arcaPixelView = CreatePixelCharacter("Arca", GetSpiritHomePosition(0), new Color(0.65f, 0.25f, 1f), 3, 50);
            arcaRenderer = arcaPixelView.Renderer;
            arca = arcaRenderer.transform;
            Sprite enemyNormalSprite = CreatePlaceholderPixelSprite("Enemy_Normal", new Color(0.92f, 0.35f, 0.2f), 46);
            Sprite enemyBossSprite = CreatePlaceholderPixelSprite("Enemy_Boss", new Color(0.72f, 0.12f, 0.16f), 60);
            PixelEnemyView enemyAView = CreatePixelEnemy("EnemyA", new Vector3(1.75f, -1.45f, 0f), enemyNormalSprite, enemyBossSprite, 2);
            enemyRenderer = enemyAView.Renderer;
            enemy = enemyRenderer.transform;
            PixelEnemyView enemyBView = CreatePixelEnemy("EnemyB", new Vector3(2.25f, -0.95f, 0f), enemyNormalSprite, enemyBossSprite, 1);
            SpriteRenderer enemyBRenderer = enemyBView.Renderer;
            PixelEnemyView enemyCView = CreatePixelEnemy("EnemyC", new Vector3(2.55f, -1.75f, 0f), enemyNormalSprite, enemyBossSprite, 1);
            SpriteRenderer enemyCRenderer = enemyCView.Renderer;
            enemyRenderers = new[] { enemyRenderer, enemyBRenderer, enemyCRenderer };
            enemyPixelViews = new[] { enemyAView, enemyBView, enemyCView };
            enemyVisuals = new[] { enemyRenderer.transform, enemyBRenderer.transform, enemyCRenderer.transform };

            CreateActor("Ground", new Vector3(0f, -2.55f, 0f), new Vector2(6.5f, 0.3f), new Color(0.16f, 0.2f, 0.28f), 0);
            SpriteRenderer coreARenderer = CreateActor("ThunderCoreA", GetSpiritHomePosition(0), new Vector2(0.18f, 0.18f), new Color(0.9f, 0.65f, 1f), 2);
            SpriteRenderer coreBRenderer = CreateActor("ThunderCoreB", GetSpiritHomePosition(0), new Vector2(0.18f, 0.18f), new Color(0.9f, 0.65f, 1f), 2);
            SpriteRenderer coreCRenderer = CreateActor("ThunderCoreC", GetSpiritHomePosition(0), new Vector2(0.18f, 0.18f), new Color(0.9f, 0.65f, 1f), 2);
            Sprite[] thunderCoreFrames = Resources.LoadAll<Sprite>("Characters/Arca/ThunderCore/IdleRotateV2");
            Array.Sort(thunderCoreFrames, (left, right) => string.CompareOrdinal(left.name, right.name));
            ConfigureThunderCoreAnimation(coreARenderer, thunderCoreFrames, 0);
            ConfigureThunderCoreAnimation(coreBRenderer, thunderCoreFrames, 2);
            ConfigureThunderCoreAnimation(coreCRenderer, thunderCoreFrames, 5);
            PixelCharacterView ignisPixelView = CreatePixelCharacter("Ignis", GetSpiritHomePosition(1), GetElementColor(SpiritElement.Fire), 3, 50);
            SpriteRenderer ignisRenderer = ignisPixelView.Renderer;
            PixelCharacterView elysiaPixelView = CreatePixelCharacter("Elysia", GetSpiritHomePosition(2), GetElementColor(SpiritElement.Water), 3, 50);
            SpriteRenderer elysiaRenderer = elysiaPixelView.Renderer;
            spiritRenderers = new[] { arcaRenderer, ignisRenderer, elysiaRenderer };
            spiritPixelViews = new[] { arcaPixelView, ignisPixelView, elysiaPixelView };
            spiritPlaceholderSprites = new[] { arcaRenderer.sprite, ignisRenderer.sprite, elysiaRenderer.sprite };
            arcaPixelSprite = Resources.Load<Sprite>("Characters/Arca/character_arca_idle_01_v3");
            arcaPixelView.SetAnimatorController(Resources.Load<RuntimeAnimatorController>("Characters/Arca/Animations/Arca_Idle"));
            spiritVisuals = new[] { arca, ignisRenderer.transform, elysiaRenderer.transform };
            arcaCoreRenderers = new[] { coreARenderer, coreBRenderer, coreCRenderer };
            arcaCoreVisuals = new[] { coreARenderer.transform, coreBRenderer.transform, coreCRenderer.transform };
            arcaCoreController = gameObject.AddComponent<ThunderCoreCombatController>();
            RefreshFormationRuntime();
        }

        private static void ConfigureThunderCoreAnimation(SpriteRenderer renderer, Sprite[] frames, int startFrame)
        {
            if (renderer == null || frames == null || frames.Length == 0) return;

            renderer.color = Color.white;
            renderer.transform.localScale = Vector3.one * 0.42f;
            ThunderCoreSpriteAnimator animator = renderer.gameObject.AddComponent<ThunderCoreSpriteAnimator>();
            animator.Configure(renderer, frames, startFrame);
        }

        private SpriteRenderer CreateActor(string objectName, Vector3 position, Vector2 size, Color color, int sortingOrder)
        {
            GameObject actor = new(objectName);
            actor.transform.SetParent(transform, false);
            actor.transform.position = position;
            actor.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = actor.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private PixelCharacterView CreatePixelCharacter(string objectName, Vector3 position, Color mainColor, int sortingOrder, int contentHeight)
        {
            GameObject characterRoot = new(objectName);
            characterRoot.transform.SetParent(transform, false);
            characterRoot.transform.position = position;
            characterRoot.transform.localScale = Vector3.one;
            SpriteRenderer renderer = characterRoot.AddComponent<SpriteRenderer>();
            characterRoot.AddComponent<Animator>();
            PixelCharacterView view = characterRoot.AddComponent<PixelCharacterView>();
            view.Configure(CreatePlaceholderPixelSprite(objectName, mainColor, contentHeight), sortingOrder);
            return view;
        }

        private PixelEnemyView CreatePixelEnemy(string objectName, Vector3 position, Sprite normalSprite, Sprite bossSprite, int sortingOrder)
        {
            GameObject enemyRoot = new(objectName);
            enemyRoot.transform.SetParent(transform, false);
            enemyRoot.transform.position = position;
            enemyRoot.transform.localScale = Vector3.one;
            enemyRoot.AddComponent<SpriteRenderer>();
            enemyRoot.AddComponent<Animator>();
            PixelEnemyView view = enemyRoot.AddComponent<PixelEnemyView>();
            view.Configure(normalSprite, bossSprite, sortingOrder);
            return view;
        }

        private Sprite CreatePlaceholderPixelSprite(string characterName, Color mainColor, int contentHeight)
        {
            const int canvasSize = 64;
            Texture2D texture = new(canvasSize, canvasSize, TextureFormat.RGBA32, false)
            {
                name = $"{characterName}_PixelPlaceholder_64",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[canvasSize * canvasSize];
            for (int index = 0; index < pixels.Length; index++) pixels[index] = Color.clear;

            int bottom = 3;
            int top = Mathf.Clamp(bottom + contentHeight - 1, bottom + 20, canvasSize - 2);
            int headHeight = Mathf.Max(12, contentHeight / 3);
            int headBottom = top - headHeight + 1;
            int bodyTop = headBottom - 1;
            int bodyBottom = bottom + Mathf.Max(12, contentHeight / 4);
            Color shadow = Color.Lerp(mainColor, Color.black, 0.38f);
            Color highlight = Color.Lerp(mainColor, Color.white, 0.4f);

            FillPixelRect(pixels, canvasSize, 23, headBottom, 41, top, shadow);
            FillPixelRect(pixels, canvasSize, 25, headBottom + 2, 39, top - 1, mainColor);
            FillPixelRect(pixels, canvasSize, 27, bodyBottom, 37, bodyTop, mainColor);
            FillPixelRect(pixels, canvasSize, 20, bodyBottom + 3, 26, bodyTop - 2, shadow);
            FillPixelRect(pixels, canvasSize, 38, bodyBottom + 3, 44, bodyTop - 2, shadow);
            FillPixelRect(pixels, canvasSize, 27, bottom, 31, bodyBottom, shadow);
            FillPixelRect(pixels, canvasSize, 34, bottom, 38, bodyBottom, shadow);
            FillPixelRect(pixels, canvasSize, 27, headBottom + 5, 29, headBottom + 7, highlight);
            FillPixelRect(pixels, canvasSize, 35, headBottom + 5, 37, headBottom + 7, highlight);

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, canvasSize, canvasSize), new Vector2(0.5f, 3f / canvasSize), 32f, 0, SpriteMeshType.FullRect);
            sprite.name = $"{characterName}_PixelPlaceholder_64";
            pixelCharacterTextures.Add(texture);
            pixelCharacterSprites.Add(sprite);
            return sprite;
        }

        private static void FillPixelRect(Color[] pixels, int canvasSize, int xMin, int yMin, int xMax, int yMax, Color color)
        {
            for (int y = Mathf.Clamp(yMin, 0, canvasSize - 1); y <= Mathf.Clamp(yMax, 0, canvasSize - 1); y++)
                for (int x = Mathf.Clamp(xMin, 0, canvasSize - 1); x <= Mathf.Clamp(xMax, 0, canvasSize - 1); x++)
                    pixels[y * canvasSize + x] = color;
        }

        private void StartWave(int nextWave)
        {
            wave = Mathf.Clamp(nextWave, 1, 10);
            battleState = BattleState.Encounter;
            currentStage = PrototypeStageCatalog.Create(Stage);
            currentEnemy = currentStage.GetEncounter(wave);
            combatStatusSystem.Clear();
            bossTimeRemaining = currentEnemy.IsBoss ? bossTimeLimit : 0f;
            enemyAttackSequence = 0;
            lastEnemyHitCount = 0;
            wasLastEnemyAttackArea = false;
            isEnemyActing = false;
            bool isBoss = currentEnemy.IsBoss;
            battleSystem.BeginEncounter(currentEnemy);
            combatTargetSystem.BeginEncounter(currentEnemy.MaximumHealth, isBoss ? 1 : 3);
            RefreshPartyStats(wave == 1);
            List<int> revivedSlotIndices = null;
            if (wave != 1)
            {
                for (int index = 0; index < partyMembers.Length; index++)
                {
                    if (!partyMembers[index].IsActive || partyMembers[index].IsAlive) continue;
                    revivedSlotIndices ??= new List<int>();
                    revivedSlotIndices.Add(index);
                    partyMembers[index].Revive(0.35f);
                }
                UpdatePartyVisualState();
            }
            for (int index = 0; index < spiritSlots.Length; index++)
            {
                if (!spiritSlots[index].IsAssigned) continue;
                spiritSlots[index].BeginAttackCooldown(0.4f + index * 0.12f);
                spiritSlots[index].SetActing(false);
            }
            if (battleCommandTimer <= 0f) battleCommandTimer = 2.5f;
            if (spiritHasteTimer <= 0f) spiritHasteTimer = 4f;
            UpdateEnemyTargetVisuals(isBoss);
            protagonist.position = GetProtagonistHomePosition();
            for (int index = 0; index < spiritVisuals.Length; index++)
                spiritVisuals[index].position = GetSpiritHomePosition(index);
            PositionArcaCores();
            arcaCoreController?.ReturnToIdle();
            ResetAlivePartyAnimations();
            PlayReviveEffects(revivedSlotIndices);
            battleMessage = isBoss ? $"스테이지 {Stage} 보스 등장!" : $"{currentEnemy.DisplayName} 등장";
            StartCoroutine(BeginEncounter());
        }

        private void PlayReviveEffects(List<int> revivedSlotIndices)
        {
            if (revivedSlotIndices == null || combatVfxSystem == null) return;
            foreach (int index in revivedSlotIndices)
            {
                if (index < 0 || index >= spiritSlots.Length || index >= spiritRenderers.Length) continue;
                PrototypeSpiritSlot slot = spiritSlots[index];
                SpriteRenderer renderer = spiritRenderers[index];
                if (slot == null || !slot.IsAssigned || renderer == null) continue;

                bool isArca = slot.SpiritId.Equals("arca", StringComparison.OrdinalIgnoreCase);
                if (isArca && arcaCoreRenderers != null)
                {
                    foreach (SpriteRenderer coreRenderer in arcaCoreRenderers)
                        if (coreRenderer != null) coreRenderer.enabled = false;
                }

                combatVfxSystem.PlayRevive(spiritVisuals[index], renderer, slot.Spirit.Element,
                    isArca ? RevealArcaCores : null);
            }
        }

        private void RevealArcaCores()
        {
            if (arcaCoreRenderers == null || arcaSlot == null || !arcaSlot.IsAssigned) return;
            foreach (SpriteRenderer coreRenderer in arcaCoreRenderers)
                if (coreRenderer != null) coreRenderer.enabled = true;
        }

        private void DealDamage(float damage, string source, int maximumTargets = 1)
        {
            int previousPrimaryTarget = combatTargetSystem.PrimaryTargetIndex;
            float appliedDamage = combatTargetSystem.ApplyDamage(damage, maximumTargets);
            battleSystem.SynchronizeEnemyHealth(combatTargetSystem.TotalHealth);
            UpdateEnemyTargetVisuals(currentEnemy.IsBoss);
            if (appliedDamage > 0f) battleHud.ShowDamage(appliedDamage, true);
            int currentPrimaryTarget = combatTargetSystem.PrimaryTargetIndex;
            bool targetChanged = previousPrimaryTarget >= 0 && currentPrimaryTarget >= 0 && previousPrimaryTarget != currentPrimaryTarget;
            battleMessage = targetChanged
                ? $"{source}! {appliedDamage:0} 피해 · 적 {previousPrimaryTarget + 1} 처치, 적 {currentPrimaryTarget + 1} 자동 지정"
                : $"{source}! {appliedDamage:0} 피해";
            if (combatTargetSystem.AliveCount == 0 && !isTransitioning)
            {
                battleState = BattleState.EnemyDefeated;
                StartCoroutine(AdvanceAfterVictory());
            }
        }

        private void DealSpiritDamage(PrototypeSpiritSlot slot, float rawDamage, string source)
        {
            string relationship = PrototypeElementChart.GetRelationshipLabel(slot.Spirit.Element, currentEnemy.Element);
            bool isCritical = UnityEngine.Random.value < spiritTrainingSystem.CriticalChance;
            float criticalDamage = isCritical ? rawDamage * spiritTrainingSystem.CriticalDamageMultiplier : rawDamage;
            DealDamage(PrototypeBattleSystem.ApplyElement(criticalDamage, slot.Spirit.Element, currentEnemy.Element),
                $"{source}{(isCritical ? " · 치명타" : string.Empty)} · {relationship}");
        }

        private void DealSpiritAbilityDamage(PrototypeSpiritSlot slot, PrototypeAbilityExecution execution, string source)
        {
            string relationship = PrototypeElementChart.GetRelationshipLabel(slot.Spirit.Element, currentEnemy.Element);
            bool isCritical = UnityEngine.Random.value < spiritTrainingSystem.CriticalChance;
            float rawDamage = isCritical ? execution.Damage * spiritTrainingSystem.CriticalDamageMultiplier : execution.Damage;
            float damage = PrototypeBattleSystem.ApplyElement(rawDamage, slot.Spirit.Element, currentEnemy.Element);
            DealDamage(damage, $"{source}{(isCritical ? " · 치명타" : string.Empty)} · {relationship}", execution.Ability.MaximumTargets);
        }

        private void UpdateEnemyTargetVisuals(bool isBoss)
        {
            if (enemyRenderers == null || enemyVisuals == null) return;
            for (int index = 0; index < enemyRenderers.Length; index++)
            {
                bool isVisible = index < combatTargetSystem.Count && combatTargetSystem.IsAlive(index);
                enemyRenderers[index].enabled = isVisible;
                enemyRenderers[index].color = Color.white;
                enemyPixelViews[index].SetBossAppearance(isBoss);
            }
            int primaryIndex = combatTargetSystem.PrimaryTargetIndex;
            if (primaryIndex < 0) primaryIndex = 0;
            enemy = enemyVisuals[primaryIndex];
            enemyRenderer = enemyRenderers[primaryIndex];
        }

        public float GetEnemyTargetHealth(int targetIndex) => combatTargetSystem.GetHealth(targetIndex);

        public float GetEnemyTargetMaximumHealth(int targetIndex) => combatTargetSystem.GetMaximumHealth(targetIndex);

        private void PulseEnemyTargets(int maximumTargets, Vector3 offset)
        {
            int pulsed = 0;
            for (int index = 0; index < enemyVisuals.Length && pulsed < maximumTargets; index++)
            {
                if (!combatTargetSystem.IsAlive(index)) continue;
                Pulse(enemyVisuals[index], offset);
                pulsed++;
            }
            if (pulsed == 0) Pulse(enemy, offset);
        }

        private IEnumerator AdvanceAfterVictory()
        {
            isTransitioning = true;
            for (int index = 0; index < enemyRenderers.Length; index++) enemyRenderers[index].enabled = false;
            int reward = currentEnemy.GoldReward;
            int experienceReward = currentEnemy.ExperienceReward;
            gold += reward;
            bool enhancementStoneDropped = !currentEnemy.IsBoss && UnityEngine.Random.value < currentStage.NormalEnhancementStoneDropChance;
            if (enhancementStoneDropped) spiritTrainingSystem.AddEnhancementStones(1);
            bool leveledUp = AddExperience(experienceReward, experienceReward + 3);
            battleMessage = leveledUp
                ? $"레벨 업! 주인공 Lv.{protagonistLevel} · 정령 성장"
                : $"승리! {reward} 골드 · 경험치 {experienceReward} 획득";
            if (enhancementStoneDropped) battleMessage += " · 강화석 1개 획득";
            if (!currentEnemy.IsBoss)
            {
                PublishReward($"일반 전투 보상  ·  골드 +{reward:N0}  ·  경험치 +{experienceReward:N0}" +
                    (enhancementStoneDropped ? "  ·  강화석 +1" : string.Empty));
            }
            SaveProgress();
            yield return new WaitForSeconds(respawnDelay * 0.5f);
            battleState = BattleState.Advancing;
            battleMessage = "다음 적을 향해 이동합니다.";
            yield return MovePartyForward();

            if (wave >= 10)
            {
                int stageClearGold = currentStage.ClearGoldReward;
                int stageClearExperience = currentStage.ClearExperienceReward;
                bool isFirstClear = Stage > HighestClearedStage;
                int spiritStoneReward = isFirstClear
                    ? currentStage.FirstClearSpiritStoneReward
                    : currentStage.RepeatClearSpiritStoneReward;
                bool spiritUpgradeStoneDropped = UnityEngine.Random.value < currentStage.BossSpiritUpgradeStoneDropChance;
                gold += stageClearGold;
                summonSystem.AddSpiritStones(spiritStoneReward);
                if (spiritUpgradeStoneDropped) spiritTrainingSystem.AddUpgradeStones(1);
                AddExperience(stageClearExperience, stageClearExperience);
                stageProgression.CompleteStage();
                wave = 1;
                battleMessage = IsAutoChallengeEnabled
                    ? $"보스 보상 {stageClearGold} 골드 · 정령석 {spiritStoneReward}{(spiritUpgradeStoneDropped ? " · 정령 강화석 1개" : string.Empty)}! STAGE {Stage} 도전"
                    : $"보스 보상 {stageClearGold} 골드 · 정령석 {spiritStoneReward}{(spiritUpgradeStoneDropped ? " · 정령 강화석 1개" : string.Empty)}! STAGE {Stage} 반복 사냥";
                PublishReward($"보스 클리어 보상  ·  골드 +{reward + stageClearGold:N0}  ·  경험치 +{experienceReward + stageClearExperience:N0}  ·  정령석 +{spiritStoneReward}" +
                    (spiritUpgradeStoneDropped ? "  ·  정령 강화석 +1" : string.Empty));
                SaveProgress();
            }
            else
            {
                wave++;
            }

            StartWave(wave);
        }

        private void PublishReward(string message)
        {
            rewardMessage = message ?? string.Empty;
            rewardSequence++;
        }

        private IEnumerator RestartAfterDefeat()
        {
            isTransitioning = true;
            battleState = BattleState.PartyDefeated;
            teamShield = 0f;
            shieldSourceSlot = null;
            battleMessage = "아군이 쓰러졌습니다. 전투를 재정비합니다.";
            yield return new WaitForSeconds(respawnDelay * 2f);
            SetPartyVisualsVisible(false);
            yield return new WaitForSeconds(0.25f);
            RefreshPartyStats(true);
            ResetAlivePartyAnimations();
            if (wave == 10 && stageProgression.ReturnFromFailedBossChallenge())
            {
                wave = 1;
                battleMessage = $"보스 도전 실패 · STAGE {Stage} 반복 사냥으로 복귀";
                SaveProgress();
            }
            StartWave(wave);
        }

        private IEnumerator RestartAfterBossTimeout()
        {
            if (isTransitioning || battleState != BattleState.Fighting) yield break;
            isTransitioning = true;
            battleState = BattleState.BossTimedOut;
            isEnemyActing = false;
            battleMessage = "보스 제한시간 종료 · 일반 전투로 복귀합니다.";
            yield return new WaitForSeconds(respawnDelay);

            bool returnedFromChallenge = stageProgression.ReturnFromFailedBossChallenge();
            wave = 1;
            teamShield = 0f;
            shieldSourceSlot = null;
            RefreshPartyStats(true);
            battleMessage = returnedFromChallenge
                ? $"보스 도전 실패 · STAGE {Stage} 반복 사냥으로 복귀"
                : $"STAGE {Stage} 보스 시간 초과 · 1전투부터 재시작";
            SaveProgress();
            isTransitioning = false;
            StartWave(1);
        }

        public void ToggleAutoChallenge()
        {
            stageProgression.ToggleAutoChallenge();
            battleMessage = IsAutoChallengeEnabled ? "자동 도전을 시작합니다." : "현재 스테이지를 반복 사냥합니다.";
            SaveProgress();
        }

        public void SelectPreviousStage()
        {
            SelectStage(Stage - 1);
        }

        public void SelectNextStage()
        {
            SelectStage(Stage + 1);
        }

        public void PreviewStage(int requestedStage)
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (!stageProgression.TryPreview(requestedStage)) return;
            StopAllCoroutines();
            isTransitioning = false;
            wave = 1;
            teamShield = 0f;
            shieldSourceSlot = null;
            RefreshPartyStats(true);
            battleMessage = $"STAGE {Stage} 콘텐츠 확인";
            SaveProgress();
            StartWave(1);
        }

        private void SelectStage(int requestedStage)
        {
            if (!stageProgression.TrySelect(requestedStage)) return;
            StopAllCoroutines();
            isTransitioning = false;
            wave = 1;
            teamShield = 0f;
            shieldSourceSlot = null;
            RefreshPartyStats(true);
            battleMessage = $"STAGE {Stage} 선택";
            SaveProgress();
            StartWave(1);
        }

        private IEnumerator BeginEncounter()
        {
            isTransitioning = true;
            yield return new WaitForSeconds(encounterDuration);
            battleState = BattleState.Fighting;
            isTransitioning = false;
            battleMessage = wave == 10 ? "보스 전투 시작!" : "자동 전투 시작";
        }

        private IEnumerator PerformProtagonistAttack()
        {
            if (battleState != BattleState.Fighting) yield break;
            protagonistPixelView?.PlayAttack();
            yield return PulseRoutine(protagonist, Vector3.right * 0.18f);
            if (battleState == BattleState.Fighting && !IsPartyDefeated()) DealDamage(GetProtagonistDamage(), "주인공의 평타");
        }

        private void UpdateSpiritActions()
        {
            for (int index = 0; index < spiritSlots.Length && !battleSystem.IsEnemyDefeated; index++)
            {
                PrototypeSpiritSlot slot = spiritSlots[index];
                if (!slot.IsAssigned || slot.IsActing || !partyMembers[index].IsAlive) continue;
                if (slot.IsUltimateReady)
                    StartCoroutine(PerformSpiritUltimate(slot, spiritVisuals[index]));
                else if (slot.IsSkillOneReady)
                    StartCoroutine(PerformSpiritSkillOne(slot, spiritVisuals[index]));
                else if (slot.IsSkillTwoReady)
                    ActivateSpiritSkillTwo(slot);
                else if (slot.IsAttackReady)
                {
                    slot.BeginAttackCooldown(GetSpiritAttackInterval(slot));
                    StartCoroutine(PerformSpiritAttack(slot, spiritVisuals[index]));
                }
            }
        }

        private IEnumerator PerformSpiritAttack(PrototypeSpiritSlot slot, Transform visual)
        {
            if (battleState != BattleState.Fighting) yield break;
            slot.SetActing(true);
            spiritPixelViews?[slot.SlotIndex]?.PlayAttack();
            bool isArca = slot.SpiritId.Equals("arca", StringComparison.OrdinalIgnoreCase);
            if (isArca)
                yield return ChargeArcaCores(0.18f);
            else if (slot.Spirit.CombatRole == SpiritCombatRole.MeleeAttack)
                yield return MeleeStrikeRoutine(visual, 0.85f);
            else
                yield return PulseRoutine(visual, Vector3.right * 0.18f);
            if (battleState == BattleState.Fighting && IsSpiritAlive(slot))
            {
                if (slot.Spirit.CombatRole != SpiritCombatRole.MeleeAttack)
                {
                    Transform projectileOrigin = isArca && arcaCoreVisuals != null && arcaCoreVisuals.Length > 1
                        ? arcaCoreVisuals[1]
                        : visual;
                    if (isArca && combatVfxSystem != null)
                        yield return combatVfxSystem.PlayArcaBasicAttack(projectileOrigin, enemy, projectileDuration);
                    else
                        yield return MoveProjectile(projectileOrigin, GetElementColor(slot.Spirit.Element), new Vector3(0.5f, 0.09f, 1f), projectileDuration);
                }
                if (battleState == BattleState.Fighting && IsSpiritAlive(slot))
                {
                    PrototypeAbilityExecution execution = PrototypeSpiritAbilitySystem.Resolve(
                        slot.Spirit.BasicAttack, SpiritAbilitySlot.BasicAttack, GetSpiritDamage(slot));
                    ApplySpiritAbilityExecution(slot, execution);
                    slot.GainUltimateEnergy(execution.EnergyGain);
                }
            }
            slot.SetActing(false);
        }

        private IEnumerator ChargeArcaCores(float duration)
        {
            arcaCoreController?.PlayBasicAttack(duration + projectileDuration);
            yield return new WaitForSeconds(duration);
        }

        private IEnumerator PerformSpiritSkillOne(PrototypeSpiritSlot slot, Transform visual)
        {
            slot.SetActing(true);
            spiritPixelViews?[slot.SlotIndex]?.PlaySkill();
            if (slot == arcaSlot) arcaCoreController?.PlaySkillOne(projectileDuration + 0.35f);
            slot.BeginSkillOne(spiritSpecialGrowthSystem.GetCooldownMultiplier(slot.SpiritId));
            battleMessage = $"{slot.DisplayName} 스킬 · {slot.Spirit.SkillOne.DisplayName}!";
            if (slot.Spirit.CombatRole == SpiritCombatRole.MeleeAttack)
                yield return MeleeStrikeRoutine(visual, 0.55f);
            else if (slot == arcaSlot)
                yield return combatVfxSystem.PlayArcaChainLightning(arcaCoreVisuals, visual,
                    GetAliveEnemyVisuals(slot.Spirit.SkillOne.MaximumTargets));
            else
                yield return MoveProjectile(visual, Color.Lerp(GetElementColor(slot.Spirit.Element), Color.white, 0.35f), new Vector3(0.85f, 0.16f, 1f), projectileDuration * 0.75f);
            if (battleState == BattleState.Fighting && IsSpiritAlive(slot))
            {
                PrototypeAbilityExecution execution = PrototypeSpiritAbilitySystem.Resolve(
                    slot.Spirit.SkillOne, SpiritAbilitySlot.SkillOne,
                    GetSpiritDamage(slot), spiritSpecialGrowthSystem.GetSkillPowerMultiplier(slot.SpiritId));
                ApplySpiritAbilityExecution(slot, execution);
                slot.GainUltimateEnergy(execution.EnergyGain);
                if (execution.DealsDamage) PulseEnemyTargets(execution.Ability.MaximumTargets, Vector3.left * 0.12f);
            }
            slot.SetActing(false);
        }

        private List<Transform> GetAliveEnemyVisuals(int maximumTargets)
        {
            var targets = new List<Transform>();
            if (enemyVisuals == null) return targets;
            int safeMaximum = Mathf.Max(1, maximumTargets);
            for (int index = 0; index < enemyVisuals.Length && targets.Count < safeMaximum; index++)
            {
                if (!combatTargetSystem.IsAlive(index)) continue;
                targets.Add(enemyVisuals[index]);
            }
            return targets;
        }

        private void ActivateSpiritSkillTwo(PrototypeSpiritSlot slot)
        {
            spiritPixelViews?[slot.SlotIndex]?.PlaySkillTwo();
            if (slot == arcaSlot)
            {
                arcaCoreController?.PlayOvercharge(0.8f);
                combatVfxSystem?.PlayArcaOvercharge(spiritVisuals[slot.SlotIndex], arcaCoreVisuals);
            }
            slot.BeginSkillTwo(spiritSpecialGrowthSystem.GetCooldownMultiplier(slot.SpiritId));
            PrototypeAbilityExecution execution = PrototypeSpiritAbilitySystem.Resolve(
                slot.Spirit.SkillTwo, SpiritAbilitySlot.SkillTwo,
                GetSpiritDamage(slot), spiritSpecialGrowthSystem.GetSkillPowerMultiplier(slot.SpiritId));
            slot.GainUltimateEnergy(execution.EnergyGain);
            battleMessage = $"{slot.DisplayName} 스킬 · {slot.Spirit.SkillTwo.DisplayName}!";
            ApplySpiritAbilityExecution(slot, execution);
            if (slot == arcaSlot) UpdateArcaColor();
        }

        private IEnumerator PerformSpiritUltimate(PrototypeSpiritSlot slot, Transform visual)
        {
            slot.SetActing(true);
            slot.SpendUltimateEnergy();
            battleMessage = $"{slot.DisplayName} 궁극기 · {slot.Spirit.Ultimate.DisplayName}!";

            spiritPixelViews?[slot.SlotIndex]?.PlayUltimate();
            if (slot.SpiritId.Equals("arca", StringComparison.OrdinalIgnoreCase))
            {
                arcaCoreController?.PlayUltimate(1.15f);
                yield return new WaitForSeconds(0.5f);
            }

            GameObject lightning = new($"{slot.SpiritId}_Ultimate");
            lightning.transform.SetParent(transform, false);
            bool isFire = slot.Spirit.Element == SpiritElement.Fire;
            bool isTeamSupport = slot.Spirit.Ultimate.Effect == SpiritAbilityEffect.DamageReduction
                || slot.Spirit.Ultimate.Effect == SpiritAbilityEffect.TeamAttackPowerBuff;
            lightning.transform.position = isTeamSupport ? protagonist.position + Vector3.up * 0.2f : isFire ? enemy.position : enemy.position + new Vector3(0f, 2.4f, 0f);
            lightning.transform.localScale = isTeamSupport ? new Vector3(4.2f, 2.8f, 1f) : isFire ? new Vector3(2.2f, 2.2f, 1f) : new Vector3(0.28f, 4.2f, 1f);
            SpriteRenderer renderer = lightning.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            Color ultimateColor = Color.Lerp(GetElementColor(slot.Spirit.Element), Color.white, 0.35f);
            if (isTeamSupport) ultimateColor.a = 0.28f;
            renderer.color = ultimateColor;
            renderer.sortingOrder = 7;
            yield return new WaitForSeconds(0.16f);
            Destroy(lightning);

            if (battleState == BattleState.Fighting)
            {
                float finalBreakthroughMultiplier = GetSpiritBreakthrough(slot.SpiritId) >= PrototypeSummonSystem.MaximumBreakthrough ? 1.2f : 1f;
                PrototypeAbilityExecution execution = PrototypeSpiritAbilitySystem.Resolve(
                    slot.Spirit.Ultimate, SpiritAbilitySlot.Ultimate,
                    GetSpiritDamage(slot), spiritSpecialGrowthSystem.GetSkillPowerMultiplier(slot.SpiritId) * finalBreakthroughMultiplier);
                if (ApplySpiritAbilityExecution(slot, execution)) Pulse(enemy, Vector3.left * 0.2f);
            }
            slot.SetActing(false);
        }

        private bool ApplySpiritAbilityExecution(PrototypeSpiritSlot slot, PrototypeAbilityExecution execution)
        {
            bool dealtDamage = false;
            if (execution.DealsDamage)
            {
                DealSpiritAbilityDamage(slot, execution, execution.Ability.DisplayName);
                dealtDamage = true;
            }
            if (execution.GrantsShield)
            {
                teamShield = execution.Shield;
                shieldSourceSlot = slot;
            }
            if (execution.HealsParty)
            {
                float healed = HealAllSpirits(execution.HealingRatio);
                battleMessage = $"{slot.DisplayName} · {execution.Ability.DisplayName}! 아군 HP {healed:0} 회복";
            }
            ApplyCombatStatus(slot, execution);
            return dealtDamage;
        }

        private void ApplyCombatStatus(PrototypeSpiritSlot slot, PrototypeAbilityExecution execution)
        {
            PrototypeSpiritAbilityData ability = execution.Ability;
            float duration = Mathf.Max(0.1f, ability.Duration);
            switch (ability.Effect)
            {
                case SpiritAbilityEffect.AttackPowerBuff:
                    combatStatusSystem.Apply(PrototypeCombatStatusType.AttackPower, PrototypeCombatStatusTarget.Spirit,
                        slot.SpiritId, ability.Id, ability.PowerMultiplier - 1f, duration);
                    combatVfxSystem?.PlayStatus(spiritVisuals[slot.SlotIndex], PrototypeCombatStatusType.AttackPower);
                    break;
                case SpiritAbilityEffect.TeamAttackPowerBuff:
                    combatStatusSystem.Apply(PrototypeCombatStatusType.AttackPower, PrototypeCombatStatusTarget.Team,
                        string.Empty, ability.Id, ability.PowerMultiplier - 1f, duration);
                    combatVfxSystem?.PlayStatus(protagonist, PrototypeCombatStatusType.AttackPower);
                    break;
                case SpiritAbilityEffect.AttackSpeedBuff:
                    float speedBonus = 1f / Mathf.Max(0.1f, ability.PowerMultiplier) - 1f;
                    PrototypeCombatStatusTarget speedTarget = slot.Spirit.CombatRole == SpiritCombatRole.Support
                        ? PrototypeCombatStatusTarget.Team : PrototypeCombatStatusTarget.Spirit;
                    combatStatusSystem.Apply(PrototypeCombatStatusType.AttackSpeed, speedTarget,
                        speedTarget == PrototypeCombatStatusTarget.Spirit ? slot.SpiritId : string.Empty,
                        ability.Id, speedBonus, duration);
                    combatVfxSystem?.PlayStatus(speedTarget == PrototypeCombatStatusTarget.Team ? protagonist : spiritVisuals[slot.SlotIndex],
                        PrototypeCombatStatusType.AttackSpeed);
                    break;
                case SpiritAbilityEffect.EnemyAttackReduction:
                    combatStatusSystem.Apply(PrototypeCombatStatusType.AttackPower, PrototypeCombatStatusTarget.Enemy,
                        string.Empty, ability.Id, ability.PowerMultiplier - 1f, duration);
                    combatVfxSystem?.PlayStatus(enemy, PrototypeCombatStatusType.AttackPower);
                    break;
                case SpiritAbilityEffect.DamageReduction:
                    float defenseBonus = 1f / Mathf.Max(0.1f, ability.PowerMultiplier) - 1f;
                    combatStatusSystem.Apply(PrototypeCombatStatusType.Defense, PrototypeCombatStatusTarget.Team,
                        string.Empty, ability.Id, defenseBonus, duration);
                    combatVfxSystem?.PlayStatus(protagonist, PrototypeCombatStatusType.Defense);
                    break;
            }

            if (slot.SpiritId == "arca" && ability.Id == "chain_lightning")
            {
                combatStatusSystem.Apply(PrototypeCombatStatusType.Stun, PrototypeCombatStatusTarget.Enemy,
                    string.Empty, ability.Id, 1f, 0.65f);
                combatVfxSystem?.PlayStatus(enemy, PrototypeCombatStatusType.Stun);
            }
            else if (slot.SpiritId == "ignis" && ability.Id == "blazing_charge")
            {
                combatStatusSystem.Apply(PrototypeCombatStatusType.Burn, PrototypeCombatStatusTarget.Enemy,
                    string.Empty, ability.Id, GetSpiritDamage(slot) * 0.35f, 4f, 3);
                combatVfxSystem?.PlayStatus(enemy, PrototypeCombatStatusType.Burn);
            }
        }

        private void ActivateBattleCommand()
        {
            battleCommandTimer = battleCommandCooldown;
            battleCommandRemaining = battleCommandDuration;
            battleMessage = "주인공 스킬 · 전투 지휘! 아르카 공격력 증가";
            StartCoroutine(ShowSupportLink(new Color(1f, 0.72f, 0.25f)));
        }

        private void ActivateSpiritHaste()
        {
            spiritHasteTimer = spiritHasteCooldown;
            spiritHasteRemaining = spiritHasteDuration;
            battleMessage = "주인공 스킬 · 정령 가속! 아르카 공격속도 증가";
            StartCoroutine(ShowSupportLink(new Color(0.3f, 0.85f, 1f)));
        }

        private IEnumerator ShowSupportLink(Color color)
        {
            GameObject link = new("ProtagonistSupportLink");
            link.transform.SetParent(transform, false);
            Vector3 midpoint = (protagonist.position + arca.position) * 0.5f;
            Vector3 direction = arca.position - protagonist.position;
            link.transform.position = midpoint;
            link.transform.localScale = new Vector3(direction.magnitude, 0.07f, 1f);
            link.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            SpriteRenderer renderer = link.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = 5;
            yield return new WaitForSeconds(0.3f);
            Destroy(link);
        }

        private IEnumerator MoveProjectile(Transform origin, Color color, Vector3 scale, float duration)
        {
            SpiritElement element = GetClosestElement(color);
            if (combatVfxSystem != null)
                yield return combatVfxSystem.PlayProjectile(origin, enemy, element, scale, duration);
        }

        private IEnumerator MeleeStrikeRoutine(Transform actor, float stoppingDistance)
        {
            Vector3 origin = actor.position;
            Vector3 destination = enemy.position + Vector3.left * stoppingDistance;
            const float approachDuration = 0.14f;
            const float returnDuration = 0.16f;
            float elapsed = 0f;
            while (elapsed < approachDuration && battleState == BattleState.Fighting)
            {
                elapsed += Time.deltaTime;
                actor.position = Vector3.Lerp(origin, destination, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / approachDuration)));
                yield return null;
            }
            Pulse(enemy, Vector3.left * 0.14f);
            combatVfxSystem?.PlayImpact(enemy, currentEnemy.Element, false);
            elapsed = 0f;
            Vector3 strikePosition = actor.position;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                actor.position = Vector3.Lerp(strikePosition, origin, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / returnDuration)));
                yield return null;
            }
            actor.position = origin;
        }

        private IEnumerator MovePartyForward()
        {
            Vector3 protagonistStart = protagonist.position;
            Vector3[] spiritStarts = new Vector3[spiritVisuals.Length];
            Vector3[] coreStarts = new Vector3[arcaCoreVisuals.Length];
            for (int index = 0; index < spiritVisuals.Length; index++) spiritStarts[index] = spiritVisuals[index].position;
            for (int index = 0; index < arcaCoreVisuals.Length; index++) coreStarts[index] = arcaCoreVisuals[index].position;
            Vector3 movement = Vector3.right * 0.75f;
            float elapsed = 0f;
            while (elapsed < advanceDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / advanceDuration));
                protagonist.position = Vector3.Lerp(protagonistStart, protagonistStart + movement, t);
                for (int index = 0; index < spiritVisuals.Length; index++)
                    spiritVisuals[index].position = Vector3.Lerp(spiritStarts[index], spiritStarts[index] + movement, t);
                for (int index = 0; index < arcaCoreVisuals.Length; index++)
                    arcaCoreVisuals[index].position = Vector3.Lerp(coreStarts[index], coreStarts[index] + movement, t);
                yield return null;
            }
        }

        private static Vector3 GetProtagonistHomePosition()
        {
            return new Vector3(-2.65f, -1.45f, 0f);
        }

        private static Vector3 GetSpiritHomePosition(int slotIndex)
        {
            return slotIndex switch
            {
                0 => new Vector3(-1.75f, -1.45f, 0f),
                1 => new Vector3(-0.85f, -1.45f, 0f),
                2 => new Vector3(0.05f, -1.45f, 0f),
                _ => throw new ArgumentOutOfRangeException(nameof(slotIndex))
            };
        }

        private void Pulse(Transform actor, Vector3 offset)
        {
            if (actor != null) StartCoroutine(PulseRoutine(actor, offset));
        }

        private static IEnumerator PulseRoutine(Transform actor, Vector3 offset)
        {
            Vector3 origin = actor.position;
            float duration = 0.12f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                actor.position = origin + offset * Mathf.Sin(t * Mathf.PI);
                yield return null;
            }
            actor.position = origin;
        }

        private float GetProtagonistDamage()
        {
            return PrototypeBattleSystem.CalculateProtagonistDamage(protagonistAttack, protagonistLevel, upgradeLevel);
        }

        private float GetArcaDamage()
        {
            return arcaSlot == null ? 0f : GetSpiritDamage(arcaSlot);
        }

        private float GetSpiritDamage(PrototypeSpiritSlot slot)
        {
            PrototypeSpiritProgress progress = GetSpiritProgress(slot.SpiritId);
            int spiritLevel = progress?.Level ?? 1;
            return PrototypeBattleSystem.CalculateSpiritDamage(slot, spiritSlots, spiritLevel, spiritTrainingSystem.AttackMultiplier,
                GetSpiritBreakthrough(slot.SpiritId), battleCommandRemaining > 0f, GetBattleCommandAttackBonus())
                * combatStatusSystem.GetAttackPowerMultiplier(slot.SpiritId);
        }

        private float HealAllSpirits(float maximumHealthRatio)
        {
            if (partyMembers == null) return 0f;
            float totalHealed = 0f;
            for (int index = 0; index < partyMembers.Length; index++)
                totalHealed += partyMembers[index].HealByMaximumHealthRatio(maximumHealthRatio);
            return totalHealed;
        }

        private float GetEnemyAttackMultiplier()
        {
            return combatStatusSystem.GetEnemyAttackMultiplier();
        }

        private float GetIncomingDamageMultiplier()
        {
            return combatStatusSystem.GetIncomingDamageMultiplier();
        }

        private float GetSpiritAttackInterval(PrototypeSpiritSlot slot)
        {
            return PrototypeBattleSystem.GetSpiritAttackInterval(slot, null, spiritHasteRemaining > 0f,
                GetSpiritHasteIntervalMultiplier(), spiritTrainingSystem.AttackIntervalMultiplier)
                * combatStatusSystem.GetAttackIntervalMultiplier(slot.SpiritId);
        }

        private string GetStageReadinessLabel()
        {
            if (currentEnemy == null || spiritSlots == null) return "전투력 계산 중";
            float totalDamagePerSecond = GetProtagonistDamage() / Mathf.Max(0.1f, protagonistAttackInterval);
            for (int index = 0; index < spiritSlots.Length; index++)
            {
                PrototypeSpiritSlot slot = spiritSlots[index];
                if (!slot.IsAssigned || !IsSpiritAlive(slot)) continue;
                totalDamagePerSecond += GetSpiritDamage(slot) / Mathf.Max(0.1f, GetSpiritAttackInterval(slot));
            }
            float estimatedSeconds = currentEnemy.MaximumHealth / Mathf.Max(1f, totalDamagePerSecond);
            float targetSeconds = currentEnemy.IsBoss ? bossTimeLimit : 12f;
            if (estimatedSeconds <= targetSeconds * 0.7f) return $"전투력 여유 · 예상 {estimatedSeconds:0.0}초";
            if (estimatedSeconds <= targetSeconds) return $"클리어 가능 · 예상 {estimatedSeconds:0.0}초";
            return $"화력 부족 · 예상 {estimatedSeconds:0.0}초";
        }

        private float GetBattleCommandAttackBonus() => battleCommandAttackBonus + protagonistBattleCommandLevel * 0.02f;

        private float GetSpiritHasteIntervalMultiplier() =>
            Mathf.Max(0.45f, spiritHasteIntervalMultiplier - protagonistSpiritHasteLevel * 0.01f);

        private static Color GetElementColor(SpiritElement element)
        {
            return element switch
            {
                SpiritElement.Fire => new Color(1f, 0.3f, 0.12f),
                SpiritElement.Water => new Color(0.2f, 0.65f, 1f),
                SpiritElement.Wind => new Color(0.35f, 1f, 0.65f),
                SpiritElement.Lightning => new Color(0.82f, 0.45f, 1f),
                SpiritElement.Light => new Color(1f, 0.92f, 0.55f),
                SpiritElement.Dark => new Color(0.34f, 0.12f, 0.5f),
                _ => Color.white
            };
        }

        private static SpiritElement GetClosestElement(Color color)
        {
            SpiritElement closest = SpiritElement.Lightning;
            float closestDistance = float.MaxValue;
            foreach (SpiritElement element in Enum.GetValues(typeof(SpiritElement)))
            {
                Color candidate = GetElementColor(element);
                float distance = (new Vector3(color.r, color.g, color.b)
                    - new Vector3(candidate.r, candidate.g, candidate.b)).sqrMagnitude;
                if (distance >= closestDistance) continue;
                closestDistance = distance;
                closest = element;
            }
            return closest;
        }

        public string GetSpiritSlotLabel(int slotIndex)
        {
            if (spiritSlots == null || slotIndex < 0 || slotIndex >= spiritSlots.Length) return "잘못된 슬롯";
            PrototypeSpiritSlot slot = spiritSlots[slotIndex];
            return slot.IsAssigned ? slot.DisplayName : "비어 있음";
        }

        public string GetSpiritStatusLabel(int slotIndex)
        {
            if (spiritSlots == null || slotIndex < 0 || slotIndex >= spiritSlots.Length) return "잘못된 슬롯";
            PrototypeSpiritSlot slot = spiritSlots[slotIndex];
            if (!slot.IsAssigned) return "비어 있음";
            PrototypePartyMemberState member = partyMembers != null && slotIndex < partyMembers.Length ? partyMembers[slotIndex] : null;
            string health = member == null ? string.Empty : member.IsAlive ? $"HP {member.CurrentHealth:0}/{member.MaximumHealth:0}" : "전투 불능";
            string relationship = currentEnemy == null ? string.Empty : PrototypeElementChart.GetRelationshipLabel(slot.Spirit.Element, currentEnemy.Element);
            return $"{slot.DisplayName} {health} 궁 {slot.UltimateEnergy:0}/{slot.Spirit.UltimateEnergyMaximum:0} · {relationship}";
        }

        public float GetSpiritAttackPower(int slotIndex)
        {
            if (spiritSlots == null || slotIndex < 0 || slotIndex >= spiritSlots.Length || !spiritSlots[slotIndex].IsAssigned) return 0f;
            return GetSpiritDamage(spiritSlots[slotIndex]);
        }

        public float GetSpiritDisplayedAttackInterval(int slotIndex)
        {
            if (spiritSlots == null || slotIndex < 0 || slotIndex >= spiritSlots.Length || !spiritSlots[slotIndex].IsAssigned) return 0f;
            return GetSpiritAttackInterval(spiritSlots[slotIndex]);
        }

        public float GetSpiritUltimateEnergy(int slotIndex)
        {
            if (spiritSlots == null || slotIndex < 0 || slotIndex >= spiritSlots.Length || !spiritSlots[slotIndex].IsAssigned) return 0f;
            return spiritSlots[slotIndex].UltimateEnergy;
        }

        public PrototypeSpiritData GetSpiritData(int slotIndex)
        {
            if (spiritSlots == null || slotIndex < 0 || slotIndex >= spiritSlots.Length || !spiritSlots[slotIndex].IsAssigned) return null;
            return spiritSlots[slotIndex].Spirit;
        }

        public float GetSpiritCurrentHealth(int slotIndex)
        {
            PrototypePartyMemberState member = GetSpiritPartyMember(slotIndex);
            return member?.CurrentHealth ?? 0f;
        }

        public float GetSpiritMaximumHealth(int slotIndex)
        {
            PrototypePartyMemberState member = GetSpiritPartyMember(slotIndex);
            return member?.MaximumHealth ?? 0f;
        }

        public float GetSpiritDefense(int slotIndex)
        {
            PrototypePartyMemberState member = GetSpiritPartyMember(slotIndex);
            return member?.Defense ?? 0f;
        }

        private PrototypePartyMemberState GetSpiritPartyMember(int slotIndex)
        {
            return partyMembers != null && slotIndex >= 0 && slotIndex < spiritSlots.Length && slotIndex < partyMembers.Length
                ? partyMembers[slotIndex]
                : null;
        }

        public int GetSpiritLevel(int slotIndex)
        {
            PrototypeSpiritProgress progress = GetSlotProgress(slotIndex);
            return progress?.Level ?? 1;
        }

        public int GetSpiritExperience(int slotIndex)
        {
            PrototypeSpiritProgress progress = GetSlotProgress(slotIndex);
            return progress?.Experience ?? 0;
        }

        public string GetSpiritEvolutionName(int slotIndex)
        {
            if (spiritSlots == null || slotIndex < 0 || slotIndex >= spiritSlots.Length || !spiritSlots[slotIndex].IsAssigned) return "비어 있음";
            PrototypeSpiritSlot slot = spiritSlots[slotIndex];
            return slot.Spirit.GetEvolutionForLevel(GetSpiritLevel(slotIndex)).DisplayName;
        }

        private PrototypeSpiritProgress GetSlotProgress(int slotIndex)
        {
            if (spiritSlots == null || slotIndex < 0 || slotIndex >= spiritSlots.Length || !spiritSlots[slotIndex].IsAssigned) return null;
            return GetSpiritProgress(spiritSlots[slotIndex].SpiritId);
        }

        private PrototypeSpiritProgress GetSpiritProgress(string spiritId)
        {
            return spiritGrowth.Get(spiritId);
        }

        private void InitializeSpiritProgress(PrototypeSaveData saveData)
        {
            spiritGrowth.Initialize(saveData);
        }

        private void InitializeSpiritFormation(PrototypeSaveData saveData)
        {
            arcaSpirit = PrototypeSpiritCatalog.GetRequired("arca");
            formationSystem.Initialize(saveData, summonSystem);
            spiritSlots = formationSystem.Slots;
            UpdateArcaSlotReference();
        }

        public void CycleSpiritInSlot(int slotIndex)
        {
            PrototypeSpiritData nextSpirit = formationSystem.Cycle(slotIndex, summonSystem);
            if (nextSpirit == null) return;
            UpdateArcaSlotReference();
            RefreshFormationRuntime();
            battleMessage = $"{slotIndex + 1}번 슬롯에 {nextSpirit.DisplayName} 편성";
            SaveProgress();
        }

        public bool AssignSpiritToSlot(int slotIndex, string spiritId)
        {
            if (!formationSystem.Assign(slotIndex, spiritId, summonSystem)) return false;
            UpdateArcaSlotReference();
            RefreshFormationRuntime();
            battleMessage = $"{slotIndex + 1}번 슬롯에 {PrototypeSpiritCatalog.GetRequired(spiritId).DisplayName} 편성";
            SaveProgress();
            return true;
        }

        public bool UnassignSpiritSlot(int slotIndex)
        {
            if (!formationSystem.Unassign(slotIndex))
            {
                battleMessage = "정령이 행동 중이거나 이미 비어 있는 슬롯입니다.";
                return false;
            }
            UpdateArcaSlotReference();
            RefreshFormationRuntime();
            battleMessage = $"{slotIndex + 1}번 슬롯의 정령을 편성 해제했습니다.";
            SaveProgress();
            return true;
        }

        public int GetAssignedSlotIndex(string spiritId)
        {
            if (spiritSlots == null || string.IsNullOrWhiteSpace(spiritId)) return -1;
            for (int index = 0; index < spiritSlots.Length; index++)
                if (spiritSlots[index].SpiritId.Equals(spiritId, StringComparison.OrdinalIgnoreCase)) return index;
            return -1;
        }

        public int GetSpiritLevelById(string spiritId) => GetSpiritProgress(spiritId)?.Level ?? 1;

        public void SummonSpirit()
        {
            SummonSpirits(1);
        }

        public void SummonTenSpirits()
        {
            SummonSpirits(10);
        }

        private void SummonSpirits(int count)
        {
            if (count <= 0 || !summonSystem.TrySpendSummonCost(count)) return;
            List<PrototypeSpiritData> summonPool = new(PrototypeSpiritCatalog.GetAll());
            if (summonPool.Count == 0) throw new InvalidOperationException("The summon pool is empty.");
            List<string> results = new(count);
            for (int summonIndex = 0; summonIndex < count; summonIndex++)
            {
                PrototypeSpiritData summonedSpirit = summonPool[UnityEngine.Random.Range(0, summonPool.Count)];
                bool isNew = summonSystem.RegisterSummon(summonedSpirit, out bool convertedToCommonShard);
                if (isNew)
                {
                    spiritGrowth.Reset(summonedSpirit.Id);
                    results.Add($"{summonedSpirit.DisplayName}(신규)");
                    int emptySlotIndex = Array.FindIndex(spiritSlots, slot => !slot.IsAssigned);
                    if (emptySlotIndex >= 0)
                        spiritSlots[emptySlotIndex].Assign(summonedSpirit, 0.4f + emptySlotIndex * 0.15f);
                }
                else
                {
                    results.Add(convertedToCommonShard
                        ? $"{summonedSpirit.DisplayName}(공용 조각)"
                        : $"{summonedSpirit.DisplayName}(전용 조각)");
                }
            }
            UpdateArcaSlotReference();
            RefreshFormationRuntime();
            summonResultMessage = count == 1
                ? results[0]
                : $"10회 소환 결과\n{string.Join(" · ", results)}";
            SaveProgress();
        }

        public string GetSummonProbabilityLabel()
        {
            List<PrototypeSpiritData> pool = new(PrototypeSpiritCatalog.GetAll());
            if (pool.Count == 0) return "소환 대상 없음";
            float probability = 100f / pool.Count;
            return string.Join("  ·  ", pool.ConvertAll(spirit => $"{spirit.DisplayName} {probability:0.##}%"));
        }

        public string GetSummonHistoryLabel(int maximumVisible = 20)
        {
            IReadOnlyList<string> history = summonSystem.SummonHistoryIds;
            if (history.Count == 0) return "소환 기록이 없습니다.";
            int count = Mathf.Min(Mathf.Max(1, maximumVisible), history.Count);
            List<string> names = new(count);
            for (int index = 0; index < count; index++)
            {
                try { names.Add($"{index + 1}. {PrototypeSpiritCatalog.GetRequired(history[index]).DisplayName}"); }
                catch (ArgumentException) { names.Add($"{index + 1}. 알 수 없는 정령"); }
            }
            return string.Join("\n", names);
        }

        public bool IsSpiritOwned(string spiritId) => summonSystem.IsOwned(spiritId);

        public int GetSpiritShards(string spiritId) => summonSystem.GetShards(spiritId);

        public int GetSpiritBreakthrough(string spiritId) => summonSystem.GetBreakthrough(spiritId);

        public bool CanBreakthroughSpirit(string spiritId)
        {
            return summonSystem.CanBreakthrough(spiritId);
        }

        public void TryBreakthroughSpirit(string spiritId)
        {
            if (!CanBreakthroughSpirit(spiritId)) return;
            int nextLevel = summonSystem.Breakthrough(spiritId);
            PrototypeSpiritData spirit = PrototypeSpiritCatalog.GetRequired(spiritId);
            battleMessage = $"{spirit.DisplayName} {nextLevel}돌파 완료!";
            RefreshPartyStats(false);
            SaveProgress();
        }

        public bool CanExchangeSsrCommonShard(string spiritId)
        {
            return summonSystem.CanExchange(spiritId);
        }

        public void ExchangeSsrCommonShard(string spiritId)
        {
            if (!CanExchangeSsrCommonShard(spiritId)) return;
            if (!summonSystem.Exchange(spiritId)) return;
            summonResultMessage = $"SSR 공용 조각 2개를 {PrototypeSpiritCatalog.GetRequired(spiritId).DisplayName} 조각 1개로 교환";
            SaveProgress();
        }

        public string GetOwnedSpiritSummary()
        {
            List<string> names = new();
            foreach (PrototypeSpiritData spirit in PrototypeSpiritCatalog.GetAll())
                if (summonSystem.IsOwned(spirit.Id)) names.Add(spirit.DisplayName);
            return names.Count > 0 ? string.Join(" · ", names) : "없음";
        }

        private void UpdateArcaSlotReference()
        {
            arcaSlot = Array.Find(spiritSlots, slot => slot.SpiritId.Equals("arca", StringComparison.OrdinalIgnoreCase));
        }

        private void RefreshFormationRuntime()
        {
            if (spiritVisuals == null || spiritRenderers == null) return;
            int supportTargetSlot = arcaSlot?.SlotIndex ?? Array.FindIndex(spiritSlots, slot => slot.IsAssigned);
            arca = supportTargetSlot >= 0 ? spiritVisuals[supportTargetSlot] : protagonist;
            arcaCoreController?.Configure(arca, arcaCoreVisuals, arcaCoreRenderers);
            for (int index = 0; index < spiritSlots.Length; index++)
            {
                spiritVisuals[index].position = GetSpiritHomePosition(index);
                spiritRenderers[index].enabled = spiritSlots[index].IsAssigned;
                if (spiritSlots[index].IsAssigned)
                {
                    Sprite displaySprite = spiritSlots[index].SpiritId.Equals("arca", StringComparison.OrdinalIgnoreCase)
                        && arcaPixelSprite != null
                            ? arcaPixelSprite
                            : spiritPlaceholderSprites[index];
                    spiritPixelViews[index].SetSprite(displaySprite);
                }
            }
            InitializePartyMembers();
            PositionArcaCores();
            UpdateArcaColor();
        }

        private void PositionArcaCores()
        {
            if (arcaCoreVisuals == null || arcaSlot == null) return;
            Vector3 home = GetSpiritHomePosition(arcaSlot.SlotIndex);
            arcaCoreVisuals[0].position = home + new Vector3(-0.55f, 0.48f, 0f);
            arcaCoreVisuals[1].position = home + new Vector3(0f, 1.08f, 0f);
            arcaCoreVisuals[2].position = home + new Vector3(0.55f, 0.48f, 0f);
        }

        private void InitializePartyMembers()
        {
            Dictionary<string, float> previousHealthRatios = new(StringComparer.OrdinalIgnoreCase);
            if (partyMembers != null)
            {
                for (int index = 0; index < partyMembers.Length; index++)
                {
                    PrototypePartyMemberState previousMember = partyMembers[index];
                    if (!previousMember.IsActive || string.IsNullOrWhiteSpace(previousMember.Id)) continue;
                    previousHealthRatios[previousMember.Id] = previousMember.MaximumHealth > 0f
                        ? previousMember.CurrentHealth / previousMember.MaximumHealth
                        : 0f;
                }
            }

            bool isInitialCreation = partyMembers == null;
            partyMembers = new PrototypePartyMemberState[spiritSlots.Length];
            for (int index = 0; index < spiritSlots.Length; index++)
            {
                PrototypeSpiritSlot slot = spiritSlots[index];
                if (!slot.IsAssigned)
                {
                    partyMembers[index] = new PrototypePartyMemberState(string.Empty, "빈 슬롯", spiritRenderers[index], 1f, 0f, 0.01f);
                    partyMembers[index].Deactivate();
                    continue;
                }
                partyMembers[index] = new PrototypePartyMemberState(slot.SpiritId, slot.DisplayName, spiritRenderers[index], 1f, 0f,
                    slot.Spirit.CombatRole == SpiritCombatRole.Defense ? 3.2f : 1f);
                RestorePreviousHealth(partyMembers[index], previousHealthRatios);
            }
            RefreshPartyStats(isInitialCreation);
        }

        private static void RestorePreviousHealth(PrototypePartyMemberState member, Dictionary<string, float> previousHealthRatios)
        {
            if (previousHealthRatios.TryGetValue(member.Id, out float healthRatio)) member.RestoreHealthRatio(healthRatio);
        }

        private void RefreshPartyStats(bool refillHealth)
        {
            if (partyMembers == null) return;
            for (int index = 0; index < spiritSlots.Length; index++)
            {
                PrototypeSpiritSlot slot = spiritSlots[index];
                if (!slot.IsAssigned)
                {
                    partyMembers[index].Deactivate();
                    continue;
                }
                int level = GetSpiritLevel(index);
                float baseHealth = slot.Spirit.CombatRole switch
                {
                    SpiritCombatRole.Defense => 620f,
                    SpiritCombatRole.MeleeAttack => 460f,
                    SpiritCombatRole.RangedAttack => 360f,
                    _ => 400f
                };
                float baseDefense = slot.Spirit.CombatRole switch
                {
                    SpiritCombatRole.Defense => 42f,
                    SpiritCombatRole.MeleeAttack => 24f,
                    SpiritCombatRole.RangedAttack => 14f,
                    _ => 18f
                };
                float targetWeight = slot.Spirit.CombatRole == SpiritCombatRole.Defense ? 3.2f : 1f;
                float breakthroughMultiplier = 1f + GetSpiritBreakthrough(slot.SpiritId) * 0.08f;
                partyMembers[index].UpdateStats(
                    (baseHealth + level * 16f) * breakthroughMultiplier * spiritTrainingSystem.MaximumHealthMultiplier,
                    (baseDefense + level * 1.25f) * breakthroughMultiplier * spiritTrainingSystem.DefenseMultiplier,
                    targetWeight, refillHealth);
            }
            UpdatePartyVisualState();
        }

        private float GetTeamHealth(bool maximum)
        {
            if (partyMembers == null) return 0f;
            float total = 0f;
            for (int index = 0; index < partyMembers.Length; index++)
                if (partyMembers[index].IsActive) total += maximum ? partyMembers[index].MaximumHealth : partyMembers[index].CurrentHealth;
            return total;
        }

        private bool IsPartyDefeated()
        {
            if (partyMembers == null) return true;
            for (int index = 0; index < partyMembers.Length; index++)
                if (partyMembers[index].IsAlive) return false;
            return true;
        }

        private bool IsSpiritAlive(PrototypeSpiritSlot slot)
        {
            int partyIndex = slot.SlotIndex;
            return partyMembers != null && partyIndex >= 0 && partyIndex < partyMembers.Length && partyMembers[partyIndex].IsAlive;
        }

        private PrototypePartyMemberState SelectEnemyTarget()
        {
            float totalWeight = 0f;
            for (int index = 0; index < partyMembers.Length; index++)
                if (partyMembers[index].IsAlive) totalWeight += partyMembers[index].TargetWeight;
            if (totalWeight <= 0f) return null;
            float selection = UnityEngine.Random.value * totalWeight;
            PrototypePartyMemberState lastAlive = null;
            for (int index = 0; index < partyMembers.Length; index++)
            {
                PrototypePartyMemberState member = partyMembers[index];
                if (!member.IsAlive) continue;
                lastAlive = member;
                selection -= member.TargetWeight;
                if (selection <= 0f) return member;
            }
            return lastAlive;
        }

        private void UpdatePartyVisualState()
        {
            if (partyMembers == null || arcaCoreRenderers == null) return;
            bool showArcaCores = arcaSlot != null && arcaSlot.IsAssigned;
            for (int index = 0; index < arcaCoreRenderers.Length; index++)
                arcaCoreRenderers[index].enabled = showArcaCores;
        }

        private void SetPartyVisualsVisible(bool isVisible)
        {
            if (spiritRenderers != null)
            {
                for (int index = 0; index < spiritRenderers.Length; index++)
                    if (spiritRenderers[index] != null)
                        spiritRenderers[index].enabled = isVisible && spiritSlots[index].IsAssigned;
            }

            if (arcaCoreRenderers == null) return;
            for (int index = 0; index < arcaCoreRenderers.Length; index++)
                if (arcaCoreRenderers[index] != null)
                    arcaCoreRenderers[index].enabled = isVisible && arcaSlot != null;
        }

        private void ResetAlivePartyAnimations()
        {
            if (partyMembers == null || spiritPixelViews == null) return;
            for (int index = 0; index < partyMembers.Length && index < spiritPixelViews.Length; index++)
            {
                if (!partyMembers[index].IsAlive) continue;
                spiritPixelViews[index]?.ResetToIdle();
            }
        }

        private void UpdateArcaColor()
        {
            if (spiritRenderers == null || arcaSlot == null) return;
            Color baseColor = arcaEvolution?.DisplayColor ?? new Color(0.32f, 0.28f, 0.42f);
            spiritRenderers[arcaSlot.SlotIndex].color = Color.white;
            if (arcaCoreRenderers == null) return;
            Color coreColor = Color.Lerp(baseColor, Color.white, arcaSlot.SkillTwoRemaining > 0f ? 0.75f : 0.4f);
            for (int index = 0; index < arcaCoreRenderers.Length; index++)
                arcaCoreRenderers[index].color = coreColor;
        }

        private int GetUpgradeCost() => 40 + upgradeLevel * 30;

        public void TryPurchaseUpgrade()
        {
            int cost = GetUpgradeCost();
            if (gold < cost) return;
            gold -= cost;
            upgradeLevel++;
            battleMessage = $"공격력이 강화되었습니다. +{upgradeLevel}";
            SaveProgress();
        }

        public int GetProtagonistSupportUpgradeCost(bool battleCommand) =>
            !battleCommand && protagonistSpiritHasteLevel >= MaximumSpiritHasteLevel
                ? 0
                : 100 + (battleCommand ? protagonistBattleCommandLevel : protagonistSpiritHasteLevel) * 75;

        public bool TryPurchaseProtagonistSupportUpgrade(bool battleCommand)
        {
            if (!battleCommand && protagonistSpiritHasteLevel >= MaximumSpiritHasteLevel)
            {
                battleMessage = "정령 가속은 최대 레벨입니다.";
                return false;
            }
            int cost = GetProtagonistSupportUpgradeCost(battleCommand);
            if (gold < cost) return false;
            gold -= cost;
            if (battleCommand) protagonistBattleCommandLevel++;
            else protagonistSpiritHasteLevel++;
            battleMessage = battleCommand ? "주인공 전투 지휘 효과가 강화되었습니다." : "주인공 정령 가속 효과가 강화되었습니다.";
            SaveProgress();
            return true;
        }

        public int GetSpiritTrainingLevel(PrototypeSpiritTrainingStat stat) => spiritTrainingSystem.GetLevel(stat);

        public int GetSpiritTrainingCost(PrototypeSpiritTrainingStat stat) => spiritTrainingSystem.GetCost(stat);

        public string GetSpiritTrainingEffectLabel(PrototypeSpiritTrainingStat stat, bool nextLevel)
        {
            float value = spiritTrainingSystem.GetEffectValue(stat, nextLevel ? 1 : 0);
            return stat switch
            {
                PrototypeSpiritTrainingStat.Attack => $"공격력 +{value:0.#}%",
                PrototypeSpiritTrainingStat.Defense => $"방어력 +{value:0.#}%",
                PrototypeSpiritTrainingStat.AttackSpeed => $"공격속도 +{value:0.#}%",
                PrototypeSpiritTrainingStat.MaximumHealth => $"최대 HP +{value:0.#}%",
                PrototypeSpiritTrainingStat.CriticalChance => $"치명타 확률 {value:0.#}%",
                PrototypeSpiritTrainingStat.CriticalDamage => $"치명타 피해 {value:0.#}%",
                _ => value.ToString("0.#")
            };
        }

        public bool TryPurchaseSpiritTraining(PrototypeSpiritTrainingStat stat)
        {
            if (!spiritTrainingSystem.TryUpgrade(stat)) return false;
            RefreshPartyStats(false);
            battleMessage = $"정령 공통 훈련이 강화되었습니다. ({stat} +{spiritTrainingSystem.GetLevel(stat)})";
            SaveProgress();
            return true;
        }

        public int GetSpiritSpecialGrowthLevel(string spiritId, PrototypeSpiritSpecialGrowthType type) =>
            spiritSpecialGrowthSystem.GetLevel(spiritId, type);

        public bool CanPurchaseSpiritSpecialGrowth(string spiritId, PrototypeSpiritSpecialGrowthType type)
        {
            if (string.IsNullOrWhiteSpace(spiritId) || !summonSystem.IsOwned(spiritId)) return false;
            try
            {
                PrototypeSpiritCatalog.GetRequired(spiritId);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return spiritTrainingSystem.UpgradeStones > 0 && spiritSpecialGrowthSystem.CanUpgrade(spiritId, type);
        }

        public bool TryPurchaseSpiritSpecialGrowth(string spiritId, PrototypeSpiritSpecialGrowthType type)
        {
            if (!CanPurchaseSpiritSpecialGrowth(spiritId, type) || !spiritTrainingSystem.TrySpendSpiritUpgradeStone()) return false;
            if (!spiritSpecialGrowthSystem.Upgrade(spiritId, type)) return false;
            battleMessage = type == PrototypeSpiritSpecialGrowthType.SkillPower
                ? $"{PrototypeSpiritCatalog.GetRequired(spiritId).DisplayName}의 스킬 효과가 강화되었습니다."
                : $"{PrototypeSpiritCatalog.GetRequired(spiritId).DisplayName}의 스킬 재사용 시간이 감소했습니다.";
            SaveProgress();
            return true;
        }

        public void DismissOfflineReport()
        {
            isOfflineReportVisible = false;
        }

        private bool AddExperience(int protagonistAmount, int arcaAmount)
        {
            SpiritEvolutionStage previousEvolutionStage = arcaEvolution?.Stage ?? arcaSpirit.GetEvolutionForLevel(ArcaLevel).Stage;
            protagonistExperience += Mathf.Max(0, protagonistAmount);
            int protagonistLevelsGained = PrototypeGrowthCalculator.ApplyLevelUps(ref protagonistLevel, ref protagonistExperience);
            int spiritLevelsGained = 0;
            HashSet<string> rewardedSpiritIds = new(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < spiritSlots.Length; index++)
            {
                PrototypeSpiritSlot slot = spiritSlots[index];
                if (!slot.IsAssigned || !rewardedSpiritIds.Add(slot.SpiritId)) continue;
                PrototypeSpiritProgress progress = GetSpiritProgress(slot.SpiritId);
                if (progress != null) spiritLevelsGained += progress.AddExperience(arcaAmount);
            }
            arcaEvolution = arcaSpirit.GetEvolutionForLevel(ArcaLevel);
            bool evolved = previousEvolutionStage != arcaEvolution.Stage;
            if (evolved) UpdateArcaColor();
            RefreshPartyStats(false);
            return protagonistLevelsGained > 0 || spiritLevelsGained > 0 || evolved;
        }

        private void NormalizeLoadedExperience()
        {
            PrototypeGrowthCalculator.ApplyLevelUps(ref protagonistLevel, ref protagonistExperience);
        }

        private void ApplyOfflineProgress()
        {
            DateTime now = DateTime.UtcNow;
            DateTime? savedLastActive = PrototypeSaveService.LoadLastActiveUtc();
            if (!savedLastActive.HasValue)
            {
                PrototypeSaveService.SaveLastActiveUtc(now);
                return;
            }

            DateTime lastActive = savedLastActive.Value;

            PrototypeOfflineReward reward = PrototypeOfflineRewardCalculator.Calculate(
                now, lastActive, Stage, maximumOfflineHours, offlineEfficiency);
            if (!reward.HasReward)
            {
                PrototypeSaveService.SaveLastActiveUtc(now);
                return;
            }

            int offlineGold = reward.Gold;
            int offlineExperience = reward.Experience;
            gold += offlineGold;
            bool leveledUp = AddExperience(offlineExperience, offlineExperience);
            TimeSpan rewardedTime = TimeSpan.FromMinutes(reward.CompletedMinutes);
            offlineReportMessage = $"방치 시간 {rewardedTime.Hours}시간 {rewardedTime.Minutes}분\n골드 +{offlineGold:N0}  ·  경험치 +{offlineExperience:N0}";
            if (leveledUp)
                offlineReportMessage += $"\n레벨 상승! 주인공 Lv.{protagonistLevel} · 정령 성장";
            isOfflineReportVisible = true;
            SaveProgress();
        }

        private void SaveProgress()
        {
            gameStateSaveSystem?.Save(gold, upgradeLevel, protagonistBattleCommandLevel, protagonistSpiritHasteLevel,
                protagonistLevel, protagonistExperience, DateTime.UtcNow);
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                wasSentToBackground = true;
                SaveProgress();
                return;
            }
            if (!wasSentToBackground) return;
            wasSentToBackground = false;
            ApplyOfflineProgress();
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }

        private void OnDestroy()
        {
            if (squareSprite != null) Destroy(squareSprite);
            if (squareTexture != null) Destroy(squareTexture);
            foreach (Sprite sprite in pixelCharacterSprites)
                if (sprite != null) Destroy(sprite);
            foreach (Texture2D texture in pixelCharacterTextures)
                if (texture != null) Destroy(texture);
        }
    }
}
