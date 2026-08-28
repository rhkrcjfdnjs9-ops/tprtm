using System;
using System.Collections;
using System.Collections.Generic;
using SpiritStone.Core;
using UnityEngine;

namespace SpiritStone.Prototype
{
    [DisallowMultipleComponent]
    public sealed class IdleBattlePrototype : MonoBehaviour
    {
        private enum BattleState
        {
            Encounter,
            Fighting,
            EnemyDefeated,
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

        private Transform protagonist;
        private Transform arca;
        private Transform enemy;
        private Transform[] enemyVisuals;
        private Transform[] spiritVisuals;
        private Transform[] arcaCoreVisuals;
        private SpriteRenderer[] arcaCoreRenderers;
        private SpriteRenderer protagonistRenderer;
        private SpriteRenderer[] spiritRenderers;
        private SpriteRenderer arcaRenderer;
        private SpriteRenderer enemyRenderer;
        private SpriteRenderer[] enemyRenderers;
        private Texture2D squareTexture;
        private Sprite squareSprite;
        private PrototypeBattleHud battleHud;
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
        private int protagonistLevel;
        private int protagonistExperience;
        private readonly PrototypeSpiritGrowthSystem spiritGrowth = new();
        private readonly PrototypeSummonSystem summonSystem = new();
        private readonly PrototypeFormationSystem formationSystem = new();
        private readonly PrototypeStageProgression stageProgression = new();
        private readonly PrototypeBattleSystem battleSystem = new();
        private readonly PrototypeCombatTargetSystem combatTargetSystem = new();
        private PrototypeGameStateSaveSystem gameStateSaveSystem;
        private string summonResultMessage = "소환할 정령을 기다리고 있습니다.";
        private float teamShield;
        private PrototypeSpiritSlot shieldSourceSlot;
        private float battleCommandTimer;
        private float battleCommandRemaining;
        private float spiritHasteTimer;
        private float spiritHasteRemaining;
        private bool isTransitioning;
        private BattleState battleState;
        private string battleMessage = "전투 준비";
        private bool isOfflineReportVisible;
        private string offlineReportMessage = string.Empty;

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
        public string SummonResultMessage => summonResultMessage;
        public int UpgradeLevel => upgradeLevel;
        public int ProtagonistLevel => protagonistLevel;
        public int ProtagonistExperience => protagonistExperience;
        public int ArcaLevel => GetSpiritProgress("arca")?.Level ?? 1;
        public int ArcaExperience => GetSpiritProgress("arca")?.Experience ?? 0;
        public float EnemyHealth => battleSystem.EnemyHealth;
        public float EnemyMaxHealth => battleSystem.EnemyMaximumHealth;
        public float TeamHealth => GetTeamHealth(false);
        public float TeamMaxHealth => GetTeamHealth(true);
        public float TeamShield => teamShield;
        public float UltimateEnergy => arcaSlot?.UltimateEnergy ?? 0f;
        public float UltimateEnergyMaximum => arcaSpirit.UltimateEnergyMaximum;
        public float ChainLightningCooldownRemaining => arcaSlot?.SkillOneCooldownRemaining ?? 0f;
        public float OverchargeCooldownRemaining => arcaSlot?.SkillTwoCooldownRemaining ?? 0f;
        public float OverchargeRemaining => arcaSlot?.SkillTwoRemaining ?? 0f;
        public float BattleCommandCooldownRemaining => Mathf.Max(0f, battleCommandTimer);
        public float BattleCommandRemaining => battleCommandRemaining;
        public float SpiritHasteCooldownRemaining => Mathf.Max(0f, spiritHasteTimer);
        public float SpiritHasteRemaining => spiritHasteRemaining;
        public float ProtagonistDamage => GetProtagonistDamage();
        public float ArcaDamage => GetArcaDamage();
        public int UpgradeCost => GetUpgradeCost();
        public bool CanPurchaseUpgrade => gold >= GetUpgradeCost();
        public bool IsBoss => currentEnemy?.IsBoss ?? wave == 10;
        public string EnemyDisplayName => currentEnemy?.DisplayName ?? "적";
        public string EnemyElementName => currentEnemy == null ? "미지정" : PrototypeElementChart.GetDisplayName(currentEnemy.Element);
        public string ArcaEvolutionName => arcaEvolution?.DisplayName ?? "정령돌 1단계";
        public string ArcaEvolutionDescription => arcaEvolution?.Description ?? string.Empty;
        public string BattleMessage => battleMessage;
        public bool IsOfflineReportVisible => isOfflineReportVisible;
        public string OfflineReportMessage => offlineReportMessage;
        public int SpiritSlotCount => spiritSlots?.Length ?? 0;
        public float ProtagonistCurrentHealth => partyMembers != null ? partyMembers[0].CurrentHealth : 0f;
        public float ProtagonistMaximumHealth => partyMembers != null ? partyMembers[0].MaximumHealth : 0f;
        public float ProtagonistDefense => partyMembers != null ? partyMembers[0].Defense : 0f;
        public string DefenseStatus
        {
            get
            {
                float attackReduction = 1f - GetEnemyAttackMultiplier();
                float damageReduction = 1f - GetIncomingDamageMultiplier();
                if (teamShield <= 0f && attackReduction <= 0f && damageReduction <= 0f) return "방어 효과 대기";
                return $"보호막 {teamShield:0}  ·  적 공격력 감소 {attackReduction * 100f:0}%  ·  받는 피해 감소 {damageReduction * 100f:0}%";
            }
        }

        private void Awake()
        {
            gameStateSaveSystem = new PrototypeGameStateSaveSystem(stageProgression, spiritGrowth, formationSystem, summonSystem);
            PrototypeSaveData saveData = PrototypeSaveService.Load();
            summonSystem.Initialize(saveData);
            InitializeSpiritFormation(saveData);
            stageProgression.Initialize(saveData);
            gold = saveData.Gold;
            upgradeLevel = saveData.UpgradeLevel;
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
            StartWave(1);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
            if (isTransitioning || battleState != BattleState.Fighting || battleSystem.IsEnemyDefeated || IsPartyDefeated()) return;

            float deltaTime = Time.deltaTime;
            battleSystem.Tick(deltaTime);
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

            if (partyMembers[0].IsAlive && battleCommandTimer <= 0f) ActivateBattleCommand();
            if (partyMembers[0].IsAlive && spiritHasteTimer <= 0f) ActivateSpiritHaste();

            if (partyMembers[0].IsAlive && battleSystem.TryBeginProtagonistAttack(protagonistAttackInterval))
            {
                StartCoroutine(PerformProtagonistAttack());
            }

            UpdateSpiritActions();

            if (battleSystem.TryBeginEnemyAttack())
            {
                PrototypePartyMemberState target = SelectEnemyTarget();
                if (target == null)
                {
                    StartCoroutine(RestartAfterDefeat());
                    return;
                }
                float incomingDamage = target.CalculateDamage(battleSystem.EnemyAttackDamage * GetEnemyAttackMultiplier() * GetIncomingDamageMultiplier());
                float absorbedDamage = Mathf.Min(teamShield, incomingDamage);
                teamShield -= absorbedDamage;
                float healthDamage = incomingDamage - absorbedDamage;
                target.TakeDamage(healthDamage);
                UpdatePartyVisualState();
                battleHud.ShowDamage(healthDamage, false);
                battleMessage = absorbedDamage > 0f
                    ? $"{target.DisplayName}의 보호막이 {absorbedDamage:0} 피해 흡수"
                    : target.IsAlive ? $"{target.DisplayName} 피격! {healthDamage:0} 피해" : $"{target.DisplayName} 전투 불능";
                Pulse(enemy, Vector3.left * 0.18f);
                if (target.IsAlive) Pulse(target.Visual, Vector3.left * 0.08f);
                if (IsPartyDefeated()) StartCoroutine(RestartAfterDefeat());
            }
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

            protagonistRenderer = CreateActor("Protagonist", GetProtagonistHomePosition(), new Vector2(0.9f, 1.55f), new Color(0.2f, 0.65f, 1f), 2);
            protagonist = protagonistRenderer.transform;
            arcaRenderer = CreateActor("Arca", GetSpiritHomePosition(0), new Vector2(0.72f, 0.92f), new Color(0.65f, 0.25f, 1f), 3);
            arca = arcaRenderer.transform;
            enemyRenderer = CreateActor("EnemyA", new Vector3(1.75f, -1.45f, 0f), new Vector2(1.15f, 1.35f), new Color(0.92f, 0.35f, 0.2f), 2);
            enemy = enemyRenderer.transform;
            SpriteRenderer enemyBRenderer = CreateActor("EnemyB", new Vector3(2.25f, -0.95f, 0f), new Vector2(0.9f, 1.05f), new Color(0.92f, 0.35f, 0.2f), 1);
            SpriteRenderer enemyCRenderer = CreateActor("EnemyC", new Vector3(2.55f, -1.75f, 0f), new Vector2(0.9f, 1.05f), new Color(0.92f, 0.35f, 0.2f), 1);
            enemyRenderers = new[] { enemyRenderer, enemyBRenderer, enemyCRenderer };
            enemyVisuals = new[] { enemyRenderer.transform, enemyBRenderer.transform, enemyCRenderer.transform };

            CreateActor("Ground", new Vector3(0f, -2.55f, 0f), new Vector2(6.5f, 0.3f), new Color(0.16f, 0.2f, 0.28f), 0);
            SpriteRenderer coreARenderer = CreateActor("ThunderCoreA", GetSpiritHomePosition(0) + new Vector3(-0.48f, 0.05f, 0f), new Vector2(0.18f, 0.18f), new Color(0.9f, 0.65f, 1f), 4);
            SpriteRenderer coreBRenderer = CreateActor("ThunderCoreB", GetSpiritHomePosition(0) + new Vector3(0f, 0.58f, 0f), new Vector2(0.18f, 0.18f), new Color(0.9f, 0.65f, 1f), 4);
            SpriteRenderer coreCRenderer = CreateActor("ThunderCoreC", GetSpiritHomePosition(0) + new Vector3(0.48f, 0.05f, 0f), new Vector2(0.18f, 0.18f), new Color(0.9f, 0.65f, 1f), 4);
            SpriteRenderer ignisRenderer = CreateActor("Ignis", GetSpiritHomePosition(1), new Vector2(0.68f, 0.82f), GetElementColor(SpiritElement.Fire), 3);
            SpriteRenderer elysiaRenderer = CreateActor("Elysia", GetSpiritHomePosition(2), new Vector2(0.72f, 0.86f), GetElementColor(SpiritElement.Water), 3);
            spiritRenderers = new[] { arcaRenderer, ignisRenderer, elysiaRenderer };
            spiritVisuals = new[] { arca, ignisRenderer.transform, elysiaRenderer.transform };
            arca = spiritVisuals[arcaSlot.SlotIndex];
            arcaCoreRenderers = new[] { coreARenderer, coreBRenderer, coreCRenderer };
            arcaCoreVisuals = new[] { coreARenderer.transform, coreBRenderer.transform, coreCRenderer.transform };
            InitializePartyMembers();
            UpdateArcaColor();
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

        private void StartWave(int nextWave)
        {
            wave = Mathf.Clamp(nextWave, 1, 10);
            battleState = BattleState.Encounter;
            currentStage = PrototypeStageCatalog.Create(Stage);
            currentEnemy = currentStage.GetEncounter(wave);
            bool isBoss = currentEnemy.IsBoss;
            battleSystem.BeginEncounter(currentEnemy);
            combatTargetSystem.BeginEncounter(currentEnemy.MaximumHealth, isBoss ? 1 : 3);
            RefreshPartyStats(wave == 1);
            if (wave != 1)
            {
                for (int index = 0; index < partyMembers.Length; index++)
                    if (!partyMembers[index].IsAlive) partyMembers[index].Revive(0.35f);
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
            battleMessage = isBoss ? $"스테이지 {Stage} 보스 등장!" : $"{currentEnemy.DisplayName} 등장";
            StartCoroutine(BeginEncounter());
        }

        private void DealDamage(float damage, string source, int maximumTargets = 1)
        {
            float appliedDamage = battleSystem.ApplyDamage(damage);
            combatTargetSystem.ApplyDamage(appliedDamage, maximumTargets);
            UpdateEnemyTargetVisuals(currentEnemy.IsBoss);
            battleHud.ShowDamage(appliedDamage, true);
            battleMessage = $"{source}! {appliedDamage:0} 피해";
            if (battleSystem.IsEnemyDefeated && !isTransitioning)
            {
                battleState = BattleState.EnemyDefeated;
                StartCoroutine(AdvanceAfterVictory());
            }
        }

        private void DealSpiritDamage(PrototypeSpiritSlot slot, float rawDamage, string source)
        {
            string relationship = PrototypeElementChart.GetRelationshipLabel(slot.Spirit.Element, currentEnemy.Element);
            DealDamage(PrototypeBattleSystem.ApplyElement(rawDamage, slot.Spirit.Element, currentEnemy.Element), $"{source} · {relationship}");
        }

        private void DealSpiritAbilityDamage(PrototypeSpiritSlot slot, PrototypeAbilityExecution execution, string source)
        {
            string relationship = PrototypeElementChart.GetRelationshipLabel(slot.Spirit.Element, currentEnemy.Element);
            float damage = PrototypeBattleSystem.ApplyElement(execution.Damage, slot.Spirit.Element, currentEnemy.Element);
            DealDamage(damage, $"{source} · {relationship}", execution.Ability.MaximumTargets);
        }

        private void UpdateEnemyTargetVisuals(bool isBoss)
        {
            if (enemyRenderers == null || enemyVisuals == null) return;
            for (int index = 0; index < enemyRenderers.Length; index++)
            {
                bool isVisible = index < combatTargetSystem.Count && combatTargetSystem.IsAlive(index);
                enemyRenderers[index].enabled = isVisible;
                enemyRenderers[index].color = currentEnemy.DisplayColor;
                enemyVisuals[index].localScale = isBoss
                    ? new Vector3(1.55f, 1.85f, 1f)
                    : index == 0 ? new Vector3(1.05f, 1.2f, 1f) : new Vector3(0.88f, 1f, 1f);
            }
            int primaryIndex = combatTargetSystem.PrimaryTargetIndex;
            if (primaryIndex < 0) primaryIndex = 0;
            enemy = enemyVisuals[primaryIndex];
            enemyRenderer = enemyRenderers[primaryIndex];
        }

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
            bool leveledUp = AddExperience(experienceReward, experienceReward + 3);
            battleMessage = leveledUp
                ? $"레벨 업! 주인공 Lv.{protagonistLevel} · 정령 성장"
                : $"승리! {reward} 골드 · 경험치 {experienceReward} 획득";
            SaveProgress();
            yield return new WaitForSeconds(respawnDelay * 0.5f);
            battleState = BattleState.Advancing;
            battleMessage = "다음 적을 향해 이동합니다.";
            yield return MovePartyForward();

            if (wave >= 10)
            {
                int stageClearGold = currentStage.ClearGoldReward;
                int stageClearExperience = currentStage.ClearExperienceReward;
                int spiritStoneReward = 20;
                gold += stageClearGold;
                summonSystem.AddSpiritStones(spiritStoneReward);
                AddExperience(stageClearExperience, stageClearExperience);
                stageProgression.CompleteStage();
                wave = 1;
                battleMessage = IsAutoChallengeEnabled
                    ? $"보스 보상 {stageClearGold} 골드 · 정령석 {spiritStoneReward}! STAGE {Stage} 도전"
                    : $"보스 보상 {stageClearGold} 골드 · 정령석 {spiritStoneReward}! STAGE {Stage} 반복 사냥";
                SaveProgress();
            }
            else
            {
                wave++;
            }

            StartWave(wave);
        }

        private IEnumerator RestartAfterDefeat()
        {
            isTransitioning = true;
            battleState = BattleState.PartyDefeated;
            teamShield = 0f;
            shieldSourceSlot = null;
            battleMessage = "아군이 쓰러졌습니다. 전투를 재정비합니다.";
            yield return new WaitForSeconds(respawnDelay * 2f);
            RefreshPartyStats(true);
            if (wave == 10 && stageProgression.ReturnFromFailedBossChallenge())
            {
                wave = 1;
                battleMessage = $"보스 도전 실패 · STAGE {Stage} 반복 사냥으로 복귀";
                SaveProgress();
            }
            StartWave(wave);
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
            yield return PulseRoutine(protagonist, Vector3.right * 0.18f);
            if (battleState == BattleState.Fighting && partyMembers[0].IsAlive) DealDamage(GetProtagonistDamage(), "주인공의 평타");
        }

        private void UpdateSpiritActions()
        {
            for (int index = 0; index < spiritSlots.Length && !battleSystem.IsEnemyDefeated; index++)
            {
                PrototypeSpiritSlot slot = spiritSlots[index];
                if (!slot.IsAssigned || slot.IsActing || !partyMembers[index + 1].IsAlive) continue;
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
            if (slot.Spirit.CombatRole == SpiritCombatRole.MeleeAttack)
                yield return MeleeStrikeRoutine(visual, 0.85f);
            else
                yield return PulseRoutine(visual, Vector3.right * 0.18f);
            if (battleState == BattleState.Fighting && IsSpiritAlive(slot))
            {
                if (slot.Spirit.CombatRole != SpiritCombatRole.MeleeAttack)
                    yield return MoveProjectile(visual, GetElementColor(slot.Spirit.Element), new Vector3(0.5f, 0.09f, 1f), projectileDuration);
                if (battleState == BattleState.Fighting && IsSpiritAlive(slot))
                {
                    PrototypeAbilityExecution execution = PrototypeSpiritAbilitySystem.Resolve(
                        slot.Spirit.BasicAttack, SpiritAbilitySlot.BasicAttack, GetSpiritDamage(slot));
                    if (execution.DealsDamage)
                        DealSpiritDamage(slot, execution.Damage, $"{slot.DisplayName}의 {execution.Ability.DisplayName}");
                    slot.GainUltimateEnergy(execution.EnergyGain);
                }
            }
            slot.SetActing(false);
        }

        private IEnumerator PerformSpiritSkillOne(PrototypeSpiritSlot slot, Transform visual)
        {
            slot.SetActing(true);
            slot.BeginSkillOne();
            battleMessage = $"{slot.DisplayName} 스킬 · {slot.Spirit.SkillOne.DisplayName}!";
            if (slot.Spirit.CombatRole == SpiritCombatRole.MeleeAttack)
                yield return MeleeStrikeRoutine(visual, 0.55f);
            else
                yield return MoveProjectile(visual, Color.Lerp(GetElementColor(slot.Spirit.Element), Color.white, 0.35f), new Vector3(0.85f, 0.16f, 1f), projectileDuration * 0.75f);
            if (battleState == BattleState.Fighting && IsSpiritAlive(slot))
            {
                PrototypeAbilityExecution execution = PrototypeSpiritAbilitySystem.Resolve(
                    slot.Spirit.SkillOne, SpiritAbilitySlot.SkillOne, GetSpiritDamage(slot));
                if (execution.DealsDamage) DealSpiritAbilityDamage(slot, execution, execution.Ability.DisplayName);
                slot.GainUltimateEnergy(execution.EnergyGain);
                PulseEnemyTargets(execution.Ability.MaximumTargets, Vector3.left * 0.12f);
            }
            slot.SetActing(false);
        }

        private void ActivateSpiritSkillTwo(PrototypeSpiritSlot slot)
        {
            slot.BeginSkillTwo();
            PrototypeAbilityExecution execution = PrototypeSpiritAbilitySystem.Resolve(
                slot.Spirit.SkillTwo, SpiritAbilitySlot.SkillTwo, GetSpiritDamage(slot));
            slot.GainUltimateEnergy(execution.EnergyGain);
            battleMessage = $"{slot.DisplayName} 스킬 · {slot.Spirit.SkillTwo.DisplayName}!";
            if (execution.GrantsShield)
            {
                teamShield = execution.Shield;
                shieldSourceSlot = slot;
            }
            if (slot == arcaSlot) UpdateArcaColor();
        }

        private IEnumerator PerformSpiritUltimate(PrototypeSpiritSlot slot, Transform visual)
        {
            slot.SetActing(true);
            slot.SpendUltimateEnergy();
            battleMessage = $"{slot.DisplayName} 궁극기 · {slot.Spirit.Ultimate.DisplayName}!";

            GameObject lightning = new($"{slot.SpiritId}_Ultimate");
            lightning.transform.SetParent(transform, false);
            bool isFire = slot.Spirit.Element == SpiritElement.Fire;
            bool isDefensive = slot.Spirit.Ultimate.Effect == SpiritAbilityEffect.DamageReduction;
            lightning.transform.position = isDefensive ? protagonist.position + Vector3.up * 0.2f : isFire ? enemy.position : enemy.position + new Vector3(0f, 2.4f, 0f);
            lightning.transform.localScale = isDefensive ? new Vector3(4.2f, 2.8f, 1f) : isFire ? new Vector3(2.2f, 2.2f, 1f) : new Vector3(0.28f, 4.2f, 1f);
            SpriteRenderer renderer = lightning.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            Color ultimateColor = Color.Lerp(GetElementColor(slot.Spirit.Element), Color.white, 0.35f);
            if (isDefensive) ultimateColor.a = 0.28f;
            renderer.color = ultimateColor;
            renderer.sortingOrder = 7;
            yield return new WaitForSeconds(0.16f);
            Destroy(lightning);

            if (battleState == BattleState.Fighting && slot.Spirit.Ultimate.Effect == SpiritAbilityEffect.Attack)
            {
                float finalBreakthroughMultiplier = GetSpiritBreakthrough(slot.SpiritId) >= PrototypeSummonSystem.MaximumBreakthrough ? 1.2f : 1f;
                PrototypeAbilityExecution execution = PrototypeSpiritAbilitySystem.Resolve(
                    slot.Spirit.Ultimate, SpiritAbilitySlot.Ultimate, GetSpiritDamage(slot), finalBreakthroughMultiplier);
                if (execution.DealsDamage)
                {
                    DealSpiritDamage(slot, execution.Damage, execution.Ability.DisplayName);
                    Pulse(enemy, Vector3.left * 0.2f);
                }
            }
            slot.SetActing(false);
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
            GameObject projectile = new("SpiritProjectile");
            projectile.transform.SetParent(transform, false);
            projectile.transform.position = origin.position + new Vector3(0.4f, 0f, 0f);
            projectile.transform.localScale = scale;
            SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = 6;

            Vector3 start = projectile.transform.position;
            Vector3 destination = enemy.position + Vector3.up * 0.25f;
            float elapsed = 0f;
            while (elapsed < duration && battleState == BattleState.Fighting)
            {
                elapsed += Time.deltaTime;
                projectile.transform.position = Vector3.Lerp(start, destination, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            Destroy(projectile);
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
            return new Vector3(-0.45f, -1.45f, 0f);
        }

        private static Vector3 GetSpiritHomePosition(int slotIndex)
        {
            return slotIndex switch
            {
                0 => new Vector3(-1.55f, 0.55f, 0f),
                1 => new Vector3(-1.95f, -0.55f, 0f),
                2 => new Vector3(-1.15f, -0.55f, 0f),
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
            return GetSpiritDamage(arcaSlot);
        }

        private float GetSpiritDamage(PrototypeSpiritSlot slot)
        {
            PrototypeSpiritProgress progress = GetSpiritProgress(slot.SpiritId);
            int spiritLevel = progress?.Level ?? 1;
            return PrototypeBattleSystem.CalculateSpiritDamage(slot, spiritLevel, upgradeLevel, GetSpiritBreakthrough(slot.SpiritId),
                battleCommandRemaining > 0f, battleCommandAttackBonus);
        }

        private float GetEnemyAttackMultiplier()
        {
            return PrototypeBattleSystem.GetEnemyAttackMultiplier(spiritSlots);
        }

        private float GetIncomingDamageMultiplier()
        {
            return PrototypeBattleSystem.GetIncomingDamageMultiplier(spiritSlots);
        }

        private float GetSpiritAttackInterval(PrototypeSpiritSlot slot)
        {
            return PrototypeBattleSystem.GetSpiritAttackInterval(slot, spiritHasteRemaining > 0f, spiritHasteIntervalMultiplier);
        }

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
            PrototypePartyMemberState member = partyMembers != null && slotIndex + 1 < partyMembers.Length ? partyMembers[slotIndex + 1] : null;
            string health = member == null ? string.Empty : member.IsAlive ? $"HP {member.CurrentHealth:0}/{member.MaximumHealth:0}" : "전투 불능";
            string relationship = currentEnemy == null ? string.Empty : PrototypeElementChart.GetRelationshipLabel(slot.Spirit.Element, currentEnemy.Element);
            return $"{slot.DisplayName} {health} 궁 {slot.UltimateEnergy:0}/{slot.Spirit.UltimateEnergyMaximum:0} · {relationship}";
        }

        public float GetSpiritAttackPower(int slotIndex)
        {
            if (spiritSlots == null || slotIndex < 0 || slotIndex >= spiritSlots.Length || !spiritSlots[slotIndex].IsAssigned) return 0f;
            return GetSpiritDamage(spiritSlots[slotIndex]);
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
            int partyIndex = slotIndex + 1;
            return partyMembers != null && slotIndex >= 0 && slotIndex < spiritSlots.Length && partyIndex < partyMembers.Length
                ? partyMembers[partyIndex]
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

        public void SummonSpirit()
        {
            if (!CanSummonSpirit) return;
            List<PrototypeSpiritData> summonPool = new(PrototypeSpiritCatalog.GetAll());
            if (summonPool.Count == 0) return;
            if (!summonSystem.TrySpendSummonCost()) return;
            PrototypeSpiritData summonedSpirit = summonPool[UnityEngine.Random.Range(0, summonPool.Count)];
            bool isNew = summonSystem.RegisterSummon(summonedSpirit, out bool convertedToCommonShard);
            if (isNew)
            {
                summonResultMessage = $"신규 정령 획득! {summonedSpirit.DisplayName}";
                int emptySlotIndex = Array.FindIndex(spiritSlots, slot => !slot.IsAssigned);
                if (emptySlotIndex >= 0)
                {
                    spiritSlots[emptySlotIndex].Assign(summonedSpirit, 0.4f + emptySlotIndex * 0.15f);
                    UpdateArcaSlotReference();
                    RefreshFormationRuntime();
                }
            }
            else
            {
                if (convertedToCommonShard)
                {
                    summonResultMessage = $"{summonedSpirit.DisplayName} 최대 돌파 · SSR 공용 조각 +1";
                }
                else
                {
                    summonResultMessage = $"{summonedSpirit.DisplayName} 중복 획득 · 전용 돌파 조각 +1";
                }
            }
            SaveProgress();
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
            arca = spiritVisuals[arcaSlot.SlotIndex];
            for (int index = 0; index < spiritSlots.Length; index++)
            {
                spiritVisuals[index].position = GetSpiritHomePosition(index);
                spiritRenderers[index].enabled = spiritSlots[index].IsAssigned;
                if (spiritSlots[index].IsAssigned) spiritRenderers[index].color = GetElementColor(spiritSlots[index].Spirit.Element);
            }
            InitializePartyMembers();
            PositionArcaCores();
            UpdateArcaColor();
        }

        private void PositionArcaCores()
        {
            if (arcaCoreVisuals == null || arcaSlot == null) return;
            Vector3 home = GetSpiritHomePosition(arcaSlot.SlotIndex);
            arcaCoreVisuals[0].position = home + new Vector3(-0.48f, 0.05f, 0f);
            arcaCoreVisuals[1].position = home + new Vector3(0f, 0.58f, 0f);
            arcaCoreVisuals[2].position = home + new Vector3(0.48f, 0.05f, 0f);
        }

        private void InitializePartyMembers()
        {
            partyMembers = new PrototypePartyMemberState[spiritSlots.Length + 1];
            partyMembers[0] = new PrototypePartyMemberState("protagonist", "주인공", protagonistRenderer, 1f, 0f, 1.2f);
            for (int index = 0; index < spiritSlots.Length; index++)
            {
                PrototypeSpiritSlot slot = spiritSlots[index];
                if (!slot.IsAssigned)
                {
                    partyMembers[index + 1] = new PrototypePartyMemberState(string.Empty, "빈 슬롯", spiritRenderers[index], 1f, 0f, 0.01f);
                    partyMembers[index + 1].Deactivate();
                    continue;
                }
                partyMembers[index + 1] = new PrototypePartyMemberState(slot.SpiritId, slot.DisplayName, spiritRenderers[index], 1f, 0f,
                    slot.Spirit.CombatRole == SpiritCombatRole.Defense ? 3.2f : 1f);
            }
            RefreshPartyStats(true);
        }

        private void RefreshPartyStats(bool refillHealth)
        {
            if (partyMembers == null) return;
            partyMembers[0].UpdateStats(480f + protagonistLevel * 20f, 18f + protagonistLevel * 1.5f, 1.2f, refillHealth);
            for (int index = 0; index < spiritSlots.Length; index++)
            {
                PrototypeSpiritSlot slot = spiritSlots[index];
                if (!slot.IsAssigned)
                {
                    partyMembers[index + 1].Deactivate();
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
                partyMembers[index + 1].UpdateStats((baseHealth + level * 16f) * breakthroughMultiplier, (baseDefense + level * 1.25f) * breakthroughMultiplier, targetWeight, refillHealth);
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
            int partyIndex = slot.SlotIndex + 1;
            return partyMembers != null && partyIndex >= 1 && partyIndex < partyMembers.Length && partyMembers[partyIndex].IsAlive;
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
            bool isArcaAlive = partyMembers[arcaSlot.SlotIndex + 1].IsAlive;
            for (int index = 0; index < arcaCoreRenderers.Length; index++)
                arcaCoreRenderers[index].enabled = isArcaAlive;
        }

        private void UpdateArcaColor()
        {
            if (spiritRenderers == null || arcaSlot == null) return;
            Color baseColor = arcaEvolution?.DisplayColor ?? new Color(0.32f, 0.28f, 0.42f);
            spiritRenderers[arcaSlot.SlotIndex].color = arcaSlot.SkillTwoRemaining > 0f ? Color.Lerp(baseColor, Color.white, 0.45f) : baseColor;
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

            double maximumMinutes = maximumOfflineHours * 60d;
            double elapsedMinutes = Math.Min(maximumMinutes, Math.Max(0d, (now - lastActive).TotalMinutes));
            int completedMinutes = Mathf.FloorToInt((float)elapsedMinutes);
            if (completedMinutes < 1)
            {
                PrototypeSaveService.SaveLastActiveUtc(now);
                return;
            }

            int offlineGold = Mathf.FloorToInt(completedMinutes * (5f + Stage * 1.5f) * offlineEfficiency);
            int offlineExperience = Mathf.FloorToInt(completedMinutes * (3f + Stage) * offlineEfficiency);
            gold += offlineGold;
            bool leveledUp = AddExperience(offlineExperience, offlineExperience);
            TimeSpan rewardedTime = TimeSpan.FromMinutes(completedMinutes);
            offlineReportMessage = $"방치 시간 {rewardedTime.Hours}시간 {rewardedTime.Minutes}분\n골드 +{offlineGold:N0}  ·  경험치 +{offlineExperience:N0}";
            if (leveledUp)
                offlineReportMessage += $"\n레벨 상승! 주인공 Lv.{protagonistLevel} · 정령 성장";
            isOfflineReportVisible = true;
            SaveProgress();
        }

        private void SaveProgress()
        {
            gameStateSaveSystem?.Save(gold, upgradeLevel, protagonistLevel, protagonistExperience, DateTime.UtcNow);
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused) SaveProgress();
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }

        private void OnDestroy()
        {
            if (squareSprite != null) Destroy(squareSprite);
            if (squareTexture != null) Destroy(squareTexture);
        }
    }
}
