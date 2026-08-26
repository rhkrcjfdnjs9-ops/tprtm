using System.Collections;
using UnityEngine;

namespace RealStone
{
    public sealed class BattleController : MonoBehaviour
    {
        private enum BattleState { Preparing, Approaching, HeroCombo, EnemyCounter, Victory, Transition }
        private BattleActor hero;
        private BattleActor enemy;
        private BattleHud hud;
        private BattleSettings settings;
        private BattleEffects effects;
        private CombatVfx combatVfx;
        private AudioSource audioSource;
        private int dungeonStage;
        private AudioClip[] swings;
        private AudioClip[] impacts;
        private AudioClip golemPunch;
        private AudioClip golemCollapse;

        public void Configure(BattleActor heroActor, BattleActor enemyActor, BattleHud battleHud,
            Camera camera, BattleSettings battleSettings)
        {
            hero = heroActor;
            enemy = enemyActor;
            hud = battleHud;
            settings = battleSettings;
            dungeonStage = ProgressService.LoadDungeonStage();
            audioSource = gameObject.AddComponent<AudioSource>();
            effects = gameObject.AddComponent<BattleEffects>();
            effects.Configure(camera);
            combatVfx = gameObject.AddComponent<CombatVfx>();
            swings = LoadAudio("sword_swing", 3);
            impacts = LoadAudio("sword_stone_hit", 3);
            golemPunch = Resources.Load<AudioClip>("Audio/golem_punch_1");
            golemCollapse = Resources.Load<AudioClip>("Audio/golem_collapse");
            StartCoroutine(BattleLoop());
        }

        private IEnumerator BattleLoop()
        {
            while (true)
            {
                SetState(BattleState.Preparing, "\uC0C8\uB85C\uC6B4 \uAD11\uC11D \uACE8\uB818\uC774 \uB098\uD0C0\uB0AC\uB2E4!");
                hero.ResetHealth();
                enemy.SetMaxHealth(settings.enemyBaseHp + (dungeonStage - 1) * settings.enemyHpPerStage);
                hero.transform.position = new Vector3(-3.7f, -2.2f, 0);
                enemy.transform.position = new Vector3(1.35f, -2.15f, 0);
                enemy.PlayIdle();
                Refresh("\uC804\uD22C \uC900\uBE44");
                yield return new WaitForSeconds(0.6f);

                SetState(BattleState.Approaching, "\uADF8\uB77C\uB2C8\uC544\uAC00 \uC801\uC5D0\uAC8C \uB2EC\uB824\uAC04\uB2E4");
                hero.PlayRun();
                yield return hero.MoveTo(new Vector3(-0.65f, -2.2f, 0), settings.approachSeconds);
                hero.PlayIdle();

                SetState(BattleState.HeroCombo, "\uADF8\uB77C\uB2C8\uC544\uC758 3\uB2E8 \uCF64\uBCF4!");
                for (var combo = 0; combo < 3 && !enemy.IsDead; combo++)
                {
                    PlayRandom(swings);
                    StartCoroutine(hero.AttackStep(Vector3.right, 0.12f + combo * 0.07f, 0.42f));
                    yield return hero.PlayAttackToImpact();
                    var damage = settings.DamageForCombo(combo);
                    enemy.TakeDamage(damage);
                    PlayRandom(impacts);
                    var heavy = combo == 2;
                    hero.PulseAttackAura(heavy);
                    combatVfx.PlayHit(enemy.transform.position, damage, heavy);
                    effects.Shake(heavy ? 0.18f : 0.1f, heavy ? settings.heavyShake : settings.lightShake);
                    StartCoroutine(enemy.ReactToHit(Vector2.right, heavy ? 0.3f : 0.16f));
                    yield return effects.HitStop(heavy ? settings.heavyHitStop : settings.lightHitStop);
                    Refresh($"{combo + 1}\uD0C0 \uC801\uC911!  {damage} \uD53C\uD574");
                    StartCoroutine(hero.PlayAttackRecovery());
                    yield return enemy.PlayHit();
                    enemy.PlayIdle();
                    yield return new WaitForSeconds(settings.timeBetweenHits);
                }

                if (!enemy.IsDead)
                {
                    SetState(BattleState.EnemyCounter, "\uACE8\uB818\uC758 \uBC18\uACA9!");
                    Play(golemPunch, 0.9f);
                    yield return enemy.PlayAttackToImpact();
                    hero.TakeDamage(settings.enemyDamage);
                    combatVfx.PlayHit(hero.transform.position, settings.enemyDamage, false);
                    effects.Shake(0.14f, settings.lightShake);
                    StartCoroutine(hero.ReactToHit(Vector2.left, 0.22f));
                    yield return effects.HitStop(settings.lightHitStop);
                    StartCoroutine(enemy.PlayAttackRecovery());
                    yield return hero.PlayHit();
                    hero.PlayIdle();
                }

                if (enemy.IsDead)
                {
                    SetState(BattleState.Victory, "\uAD11\uC11D \uACE8\uB818\uC744 \uC4F0\uB7EC\uB728\uB838\uB2E4!");
                    Play(golemCollapse, 0.9f);
                    yield return enemy.PlayDeath();
                    yield return new WaitForSeconds(0.45f);
                    dungeonStage++;
                    ProgressService.SaveDungeonStage(dungeonStage);
                    SetState(BattleState.Transition, "\uB2E4\uC74C \uC801\uC744 \uD5A5\uD574 \uC774\uB3D9 \uC911");
                    hero.PlayRun();
                    yield return hero.MoveTo(new Vector3(6.2f, -2.2f, 0), settings.nextWaveRunSeconds);
                }
                else yield return new WaitForSeconds(0.4f);
            }
        }

        private void SetState(BattleState state, string message) => Refresh(message);
        private void Refresh(string message) => hud.Refresh(hero, enemy, dungeonStage, message);
        private void Play(AudioClip clip, float volume) { if (clip != null) audioSource.PlayOneShot(clip, volume); }
        private void PlayRandom(AudioClip[] clips) { if (clips.Length > 0) Play(clips[Random.Range(0, clips.Length)], 0.8f); }
        private static AudioClip[] LoadAudio(string prefix, int count)
        {
            var clips = new AudioClip[count];
            for (var i = 0; i < count; i++) clips[i] = Resources.Load<AudioClip>($"Audio/{prefix}_{i + 1}");
            return clips;
        }
    }
}
