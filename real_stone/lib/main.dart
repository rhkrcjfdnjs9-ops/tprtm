import 'dart:async';
import 'dart:math';

import 'package:flutter/material.dart';
import 'package:flutter/scheduler.dart';
import 'package:flutter/services.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'combat_motion.dart';

void main() => runApp(const RealStoneApp());

class RealStoneApp extends StatelessWidget {
  const RealStoneApp({super.key});

  @override
  Widget build(BuildContext context) => MaterialApp(
    debugShowCheckedModeBanner: false,
    title: '리얼 돌 키우기',
    theme: ThemeData(
      brightness: Brightness.dark,
      colorScheme: ColorScheme.fromSeed(
        seedColor: const Color(0xFF64D8E8),
        brightness: Brightness.dark,
      ),
      scaffoldBackgroundColor: const Color(0xFF080D17),
      useMaterial3: true,
    ),
    home: const GamePage(),
  );
}

class _SlashTrailPainter extends CustomPainter {
  const _SlashTrailPainter({
    required this.progress,
    required this.variant,
    required this.critical,
    required this.awakened,
  });

  final double progress;
  final int variant;
  final bool critical;
  final bool awakened;

  @override
  void paint(Canvas canvas, Size size) {
    final fade = (1 - progress).clamp(0.0, 1.0);
    final cyan = awakened ? const Color(0xFFFFDF77) : const Color(0xFF8DEBFF);
    final color = critical ? const Color(0xFFFFD36A) : cyan;
    final startY = variant == 1 ? size.height * 0.18 : size.height * 0.78;
    final endY = variant == 2 ? size.height * 0.72 : size.height * 0.28;
    final controlY = variant == 0 ? -size.height * 0.08 : size.height * 0.5;
    final path = Path()
      ..moveTo(0, startY)
      ..quadraticBezierTo(
        size.width * (0.42 + progress * 0.18),
        controlY,
        size.width,
        endY,
      );
    canvas.drawPath(
      path,
      Paint()
        ..color = color.withValues(alpha: 0.32 * fade)
        ..style = PaintingStyle.stroke
        ..strokeCap = StrokeCap.round
        ..strokeWidth = critical ? 18 : 12
        ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 8),
    );
    canvas.drawPath(
      path,
      Paint()
        ..color = Colors.white.withValues(alpha: 0.92 * fade)
        ..style = PaintingStyle.stroke
        ..strokeCap = StrokeCap.round
        ..strokeWidth = critical ? 5 : 3,
    );
  }

  @override
  bool shouldRepaint(covariant _SlashTrailPainter oldDelegate) =>
      oldDelegate.progress != progress ||
      oldDelegate.variant != variant ||
      oldDelegate.critical != critical ||
      oldDelegate.awakened != awakened;
}

class _RunDustPainter extends CustomPainter {
  const _RunDustPainter({required this.progress});

  final double progress;

  @override
  void paint(Canvas canvas, Size size) {
    final dust = Paint()
      ..color = const Color(0xFFB8C1C5).withValues(alpha: 0.5);
    for (var index = 0; index < 3; index++) {
      final phase = (progress + index * 0.19) % 1;
      final x = size.width * (0.78 - phase * 0.72);
      final y = size.height * (0.72 + sin(index * 2.1) * 0.12);
      canvas.drawCircle(
        Offset(x, y),
        3 + phase * 7,
        dust..color = dust.color.withValues(alpha: (1 - phase) * 0.52),
      );
    }
  }

  @override
  bool shouldRepaint(covariant _RunDustPainter oldDelegate) =>
      oldDelegate.progress != progress;
}

class GamePage extends StatefulWidget {
  const GamePage({super.key});

  @override
  State<GamePage> createState() => _GamePageState();
}

enum BattlePhase { idle, approach, attack, hit, counter, retreat, defeated }

class _GamePageState extends State<GamePage>
    with WidgetsBindingObserver, TickerProviderStateMixin {
  int level = 1;
  int exp = 0;
  int crystals = 30;
  int liberationStones = 0;
  int bond = 0;
  int selectedEvolutionStage = 1;
  int dungeonStage = 1;
  double enemyHp = 100;
  double heroHp = 100;
  bool bossDefeated = false;
  bool halfReleased = false;
  bool fullyReleased = false;
  bool trueAwakened = false;
  bool loaded = false;
  bool walkFramesPrecached = false;
  bool monsterHit = false;
  bool heroHit = false;
  bool impactFlash = false;
  bool slashVisible = false;
  bool criticalHit = false;
  bool impactBurstVisible = false;
  bool screenShake = false;
  bool hitStop = false;
  bool battleBusy = false;
  bool enemyVisible = true;
  bool heroVisible = true;
  bool traveling = false;
  double heroPosition = 32;
  double monsterPosition = 32;
  BattlePhase battlePhase = BattlePhase.idle;
  int lastDamage = 0;
  int heroSpriteFrame = 0;
  int monsterSpriteFrame = 0;
  int animationTick = 0;
  int attackVariant = 0;
  int impactSerial = 0;
  int hitEffectFrame = 0;
  int heroAnimation = 0;
  int monsterAnimation = 0;
  String battleText = '자동 전투 준비 중';
  Timer? battleTimer;
  Timer? saveTimer;
  late final Ticker frameTicker;
  Duration previousFrameTick = Duration.zero;
  double heroFrameElapsedMs = 0;
  double monsterFrameElapsedMs = 0;
  late final AnimationController motionController;

  int get expNeeded => 40 + level * 20;
  int get unlockedEvolutionStage {
    if (trueAwakened) return 6;
    if (fullyReleased) return 5;
    if (halfReleased) return 4;
    if (level >= 10) return 3;
    if (level >= 5) return 2;
    return 1;
  }

  int get evolutionStage =>
      min(unlockedEvolutionStage, max(1, selectedEvolutionStage));

  String get evolutionName => const [
    '원석 상태',
    '형상 발현',
    '균열 발생',
    '반신 해방',
    '완전 해방',
    '진정한 각성',
  ][evolutionStage - 1];
  String get imagePath => 'assets/images/grania_stage_$evolutionStage.png';
  double get attackBonus =>
      const [0.0, 0.10, 0.25, 0.40, 0.65, 1.00][unlockedEvolutionStage - 1];
  int get attack => ((7 + level * 4) * (1 + attackBonus)).round();
  int get maxEnemyHp => 70 + dungeonStage * 30;
  int get maxHeroHp => 90 + level * 12;
  int get enemyAttack => 4 + dungeonStage * 2;
  bool get canEvolve {
    if (unlockedEvolutionStage == 3) return level >= 20 && bossDefeated;
    if (unlockedEvolutionStage == 4) {
      return level >= 35 && liberationStones >= 3;
    }
    if (unlockedEvolutionStage == 5) return level >= 50 && bond >= 100;
    return false;
  }

  String get nextCondition {
    if (unlockedEvolutionStage == 1) return '다음 단계: 형상 발현 · 레벨 5 필요';
    if (unlockedEvolutionStage == 2) return '다음 단계: 균열 발생 · 레벨 10 필요';
    if (unlockedEvolutionStage == 3) {
      final levelState = level >= 20 ? '완료' : '$level/20';
      final bossState = bossDefeated ? '완료' : '미완료';
      return '다음 단계: 반신 해방 · 레벨 $levelState · 보스 $bossState';
    }
    if (unlockedEvolutionStage == 4) {
      return '다음 단계: 완전 해방 · 레벨 $level/35 · 해방석 $liberationStones/3';
    }
    if (unlockedEvolutionStage == 5) {
      return '다음 단계: 진정한 각성 · 레벨 $level/50 · 유대 $bond/100';
    }
    return '최종 단계에 도달했습니다 · 태고의 수호자 그라니아';
  }

  String get evolveButtonText {
    if (unlockedEvolutionStage <= 2) return '봉인 성장 중';
    if (unlockedEvolutionStage == 3) return '반신 해방';
    if (unlockedEvolutionStage == 4) return '완전 해방';
    if (unlockedEvolutionStage == 5) return '진정한 각성';
    return '최종 각성 완료';
  }

  @override
  void initState() {
    super.initState();
    motionController = AnimationController(vsync: this);
    frameTicker = createTicker(_onFrameTick)..start();
    WidgetsBinding.instance.addObserver(this);
    _load();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (walkFramesPrecached) return;
    walkFramesPrecached = true;
    const actionCounts = {
      'idle': 6,
      'walk': 8,
      'attack': 8,
      'hit': 5,
      'death': 6,
    };
    for (var stage = 1; stage <= 6; stage++) {
      for (final action in actionCounts.entries) {
        for (var frame = 0; frame < action.value; frame++) {
          precacheImage(
            AssetImage(
              'assets/frames/stage_$stage/grania_${action.key}_$frame.png',
            ),
            context,
          );
        }
      }
    }
    for (var frame = 0; frame < 8; frame++) {
      precacheImage(
        AssetImage('assets/frames/stage_6/grania_idle_v2_$frame.png'),
        context,
      );
      precacheImage(
        AssetImage('assets/frames/effects/hit_effect_$frame.png'),
        context,
      );
    }
    for (var frame = 0; frame < 4; frame++) {
      precacheImage(
        AssetImage('assets/frames/stage_6/grania_attack_transition_$frame.png'),
        context,
      );
      precacheImage(
        AssetImage('assets/frames/golem_attack_transition_$frame.png'),
        context,
      );
    }
  }

  Future<void> _load() async {
    final prefs = await SharedPreferences.getInstance();
    level = prefs.getInt('level') ?? 1;
    exp = prefs.getInt('exp') ?? 0;
    crystals = prefs.getInt('crystals') ?? 30;
    dungeonStage = prefs.getInt('stage') ?? 1;
    bossDefeated = prefs.getBool('bossDefeated') ?? false;
    final legacyReleased = prefs.getBool('fullyReleased') ?? false;
    halfReleased = prefs.getBool('halfReleased') ?? legacyReleased;
    fullyReleased = prefs.getBool('stage5Released') ?? false;
    trueAwakened = prefs.getBool('trueAwakened') ?? false;
    liberationStones = prefs.getInt('liberationStones') ?? 0;
    bond = prefs.getInt('bond') ?? 0;
    selectedEvolutionStage =
        prefs.getInt('selectedEvolutionStage') ?? unlockedEvolutionStage;
    selectedEvolutionStage = min(
      unlockedEvolutionStage,
      max(1, selectedEvolutionStage),
    );
    final lastPlayed = prefs.getInt('lastPlayed');
    var offlineReward = 0;
    if (lastPlayed != null) {
      final seconds =
          DateTime.now().millisecondsSinceEpoch ~/ 1000 - lastPlayed;
      offlineReward = min(seconds, 8 * 60 * 60) * max(1, level ~/ 2);
      crystals += offlineReward;
    }
    enemyHp = maxEnemyHp.toDouble();
    heroHp = maxHeroHp.toDouble();
    loaded = true;
    if (mounted) setState(() {});
    if (offlineReward > 0 && mounted) {
      WidgetsBinding.instance.addPostFrameCallback(
        (_) => _showOffline(offlineReward),
      );
    }
    _scheduleBattle(const Duration(milliseconds: 700));
    saveTimer = Timer.periodic(const Duration(seconds: 10), (_) => _save());
  }

  double _frameDurationMs(int animation, {required bool hero}) {
    const heroDurations = [125.0, 76.0, double.infinity, 82.0, 105.0];
    const monsterDurations = [140.0, 92.0, double.infinity, 88.0, 110.0];
    return (hero ? heroDurations : monsterDurations)[animation];
  }

  void _onFrameTick(Duration elapsed) {
    if (!mounted) return;
    if (previousFrameTick == Duration.zero) {
      previousFrameTick = elapsed;
      return;
    }
    final deltaMs =
        (elapsed - previousFrameTick).inMicroseconds /
        Duration.microsecondsPerMillisecond;
    previousFrameTick = elapsed;
    if (hitStop) return;

    heroFrameElapsedMs += deltaMs;
    monsterFrameElapsedMs += deltaMs;
    final heroDuration = _frameDurationMs(heroAnimation, hero: true);
    final monsterDuration = _frameDurationMs(monsterAnimation, hero: false);
    var changed = false;
    while (heroFrameElapsedMs >= heroDuration) {
      heroFrameElapsedMs -= heroDuration;
      heroSpriteFrame++;
      changed = true;
    }
    while (monsterFrameElapsedMs >= monsterDuration) {
      monsterFrameElapsedMs -= monsterDuration;
      monsterSpriteFrame++;
      changed = true;
    }
    if (changed) {
      setState(() => animationTick++);
    }
  }

  void _scheduleBattle([Duration? delay]) {
    battleTimer?.cancel();
    final wait = delay ?? Duration(milliseconds: 520 + Random().nextInt(420));
    battleTimer = Timer(wait, _battleTick);
  }

  void _setCombatState({
    required BattlePhase phase,
    required int hero,
    required int monster,
  }) {
    if (!mounted) return;
    setState(() {
      battlePhase = phase;
      heroAnimation = hero;
      monsterAnimation = monster;
      heroSpriteFrame = 0;
      monsterSpriteFrame = 0;
      animationTick = 0;
      heroFrameElapsedMs = 0;
      monsterFrameElapsedMs = 0;
    });
  }

  Future<void> _moveActor({
    required bool hero,
    required double target,
    required Duration duration,
    required Curve curve,
  }) async {
    final start = hero ? heroPosition : monsterPosition;
    motionController.duration = duration;
    motionController.reset();
    void update() {
      if (!mounted) return;
      final value = curve.transform(motionController.value);
      setState(() {
        final position = start + (target - start) * value;
        if (hero) {
          heroPosition = position;
        } else {
          monsterPosition = position;
        }
      });
    }

    motionController.addListener(update);
    await motionController.forward();
    motionController.removeListener(update);
  }

  Future<void> _showActionFrame({
    required bool hero,
    required int frame,
    required int milliseconds,
  }) async {
    if (!mounted) return;
    setState(() {
      if (hero) {
        heroSpriteFrame = frame;
      } else {
        monsterSpriteFrame = frame;
      }
    });
    await Future<void>.delayed(Duration(milliseconds: milliseconds));
  }

  Future<void> _playMotion(
    List<CombatFrameCue> motion, {
    required bool hero,
  }) async {
    for (final cue in motion) {
      await _showActionFrame(
        hero: hero,
        frame: cue.frame,
        milliseconds: cue.milliseconds,
      );
      if (!mounted) return;
    }
  }

  Future<void> _playHitEffect() async {
    if (!mounted) return;
    setState(() {
      impactBurstVisible = true;
      hitEffectFrame = 0;
    });
    for (var frame = 1; frame < 8; frame++) {
      await Future<void>.delayed(const Duration(milliseconds: 38));
      if (!mounted) return;
      setState(() => hitEffectFrame = frame);
    }
    await Future<void>.delayed(const Duration(milliseconds: 30));
    if (mounted) setState(() => impactBurstVisible = false);
  }

  Future<void> _save() async {
    if (!loaded) return;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt('level', level);
    await prefs.setInt('exp', exp);
    await prefs.setInt('crystals', crystals);
    await prefs.setInt('stage', dungeonStage);
    await prefs.setBool('bossDefeated', bossDefeated);
    await prefs.setBool('halfReleased', halfReleased);
    await prefs.setBool('stage5Released', fullyReleased);
    await prefs.setBool('trueAwakened', trueAwakened);
    await prefs.setInt('liberationStones', liberationStones);
    await prefs.setInt('bond', bond);
    await prefs.setInt('selectedEvolutionStage', selectedEvolutionStage);
    await prefs.setInt(
      'lastPlayed',
      DateTime.now().millisecondsSinceEpoch ~/ 1000,
    );
  }

  Future<void> _battleTick() async {
    if (!mounted || !loaded || battleBusy) return;
    battleBusy = true;
    _setCombatState(phase: BattlePhase.approach, hero: 1, monster: 0);
    setState(() => battleText = '그라니아가 발걸음을 맞춰 접근한다');
    await _moveActor(
      hero: true,
      target: 78,
      duration: const Duration(milliseconds: 420),
      curve: Curves.easeInOutCubic,
    );
    if (!mounted) return;
    _setCombatState(phase: BattlePhase.attack, hero: 2, monster: 0);
    setState(() => battleText = '검에 힘을 모은다…');
    attackVariant = Random().nextInt(3);
    criticalHit = Random().nextInt(100) < 18;
    await _playMotion(CombatMotions.heroLeadIn, hero: true);
    await _playMotion(CombatMotions.heroWindup, hero: true);
    if (!mounted) return;
    final dealtDamage = criticalHit ? (attack * 1.65).round() : attack;
    setState(() {
      monsterHit = true;
      impactFlash = true;
      impactBurstVisible = true;
      impactSerial++;
      slashVisible = true;
      monsterAnimation = 3;
      monsterSpriteFrame = 0;
      hitStop = true;
      screenShake = true;
      lastDamage = dealtDamage;
      enemyHp -= dealtDamage;
      if (criticalHit) battleText = 'CRITICAL! $dealtDamage';
      battleText = '그라니아가 $attack 피해를 입혔다!';
    });
    if (criticalHit && mounted) {
      setState(() => battleText = 'CRITICAL! $dealtDamage');
    }
    unawaited(_playHitEffect());
    if (criticalHit) {
      HapticFeedback.heavyImpact();
    } else {
      HapticFeedback.mediumImpact();
    }
    SystemSound.play(SystemSoundType.click);
    await Future<void>.delayed(const Duration(milliseconds: 72));
    if (!mounted) return;
    setState(() {
      hitStop = false;
      screenShake = false;
      impactFlash = false;
    });

    final firstRecovery = CombatMotions.heroRecovery.first;
    await _showActionFrame(
      hero: true,
      frame: firstRecovery.frame,
      milliseconds: firstRecovery.milliseconds,
    );
    if (mounted) {
      setState(() {
        slashVisible = false;
      });
    }
    await _playMotion(CombatMotions.heroRecovery.sublist(1), hero: true);

    if (enemyHp <= 0) {
      _setCombatState(phase: BattlePhase.defeated, hero: 0, monster: 4);
      setState(() {
        monsterHit = false;
        battleText = '광석 골렘이 무너진다!';
      });
      await Future<void>.delayed(const Duration(milliseconds: 720));
      if (!mounted) return;
      final clearedStage = dungeonStage;
      final reward = 8 + clearedStage * 3;
      setState(() {
        crystals += reward;
        exp += 12 + clearedStage * 2;
        bond = min(100, bond + 2);
        if (clearedStage == 10) {
          bossDefeated = true;
          liberationStones++;
        }
        if (dungeonStage < 10) dungeonStage++;
        _checkLevelUp();
        enemyVisible = false;
        heroAnimation = 1;
        heroSpriteFrame = 0;
        traveling = true;
        battleText = '승리! 다음 적을 향해 이동 중…';
      });
      await _moveActor(
        hero: true,
        target: 380,
        duration: const Duration(milliseconds: 760),
        curve: Curves.easeInCubic,
      );
      if (!mounted) return;
      setState(() {
        heroVisible = false;
        heroPosition = -210;
        enemyHp = maxEnemyHp.toDouble();
        enemyVisible = true;
        monsterPosition = 32;
        monsterAnimation = 0;
        monsterSpriteFrame = 0;
        battleText = clearedStage == 10
            ? '광산 보스 처치! 해방석 1개 획득'
            : '새로운 광석 골렘 등장';
      });
      await Future<void>.delayed(const Duration(milliseconds: 140));
      if (!mounted) return;
      setState(() => heroVisible = true);
      await _moveActor(
        hero: true,
        target: 32,
        duration: const Duration(milliseconds: 720),
        curve: Curves.easeOutCubic,
      );
      if (!mounted) return;
      setState(() => traveling = false);
      _setCombatState(phase: BattlePhase.idle, hero: 0, monster: 0);
      _finishBattleCycle();
      return;
    }

    await Future<void>.delayed(const Duration(milliseconds: 220));
    if (!mounted) return;
    setState(() => monsterHit = false);
    _setCombatState(phase: BattlePhase.retreat, hero: 1, monster: 0);
    await _moveActor(
      hero: true,
      target: 32,
      duration: const Duration(milliseconds: 340),
      curve: Curves.easeOutCubic,
    );
    if (!mounted) return;
    _setCombatState(phase: BattlePhase.counter, hero: 0, monster: 1);
    setState(() => battleText = '광석 골렘이 묵직하게 다가온다');
    await Future<void>.delayed(const Duration(milliseconds: 230));
    await _moveActor(
      hero: false,
      target: 72,
      duration: const Duration(milliseconds: 360),
      curve: Curves.easeInOutCubic,
    );
    if (!mounted) return;
    _setCombatState(phase: BattlePhase.counter, hero: 0, monster: 2);
    setState(() => battleText = '광석 골렘의 반격!');
    await _playMotion(CombatMotions.golemLeadIn, hero: false);
    await _playMotion(CombatMotions.golemWindup, hero: false);
    if (!mounted) return;
    HapticFeedback.mediumImpact();
    SystemSound.play(SystemSoundType.click);
    unawaited(_playHitEffect());
    setState(() {
      heroHp -= enemyAttack;
      heroAnimation = 3;
      heroSpriteFrame = 0;
      heroHit = true;
      impactFlash = true;
      impactBurstVisible = true;
      impactSerial++;
      hitStop = true;
      screenShake = true;
      battleText = '그라니아가 $enemyAttack 피해를 받았다';
    });
    await Future<void>.delayed(const Duration(milliseconds: 72));
    if (!mounted) return;
    setState(() {
      hitStop = false;
      screenShake = false;
      impactFlash = false;
    });
    if (mounted) setState(() => monsterAnimation = 2);
    await _playMotion(CombatMotions.golemRecovery, hero: false);
    if (heroHp <= 0) {
      _setCombatState(phase: BattlePhase.defeated, hero: 4, monster: 0);
      setState(() => battleText = '그라니아가 쓰러졌다…');
      await Future<void>.delayed(const Duration(milliseconds: 720));
      if (!mounted) return;
      setState(() {
        heroHp = maxHeroHp.toDouble();
        heroAnimation = 0;
        battleText = '그라니아가 다시 일어났다';
      });
    } else {
      await Future<void>.delayed(const Duration(milliseconds: 260));
    }
    if (!mounted) return;
    setState(() => heroHit = false);
    _setCombatState(phase: BattlePhase.retreat, hero: 0, monster: 1);
    await _moveActor(
      hero: false,
      target: 32,
      duration: const Duration(milliseconds: 340),
      curve: Curves.easeOutCubic,
    );
    _setCombatState(phase: BattlePhase.idle, hero: 0, monster: 0);
    _finishBattleCycle();
  }

  void _finishBattleCycle() {
    battleBusy = false;
    _scheduleBattle();
  }

  void _checkLevelUp() {
    final oldStage = unlockedEvolutionStage;
    while (exp >= expNeeded) {
      exp -= expNeeded;
      level++;
    }
    if (unlockedEvolutionStage > oldStage) {
      selectedEvolutionStage = unlockedEvolutionStage;
      battleText = '$evolutionName 단계에 도달했다!';
    }
  }

  void _evolve() {
    if (!canEvolve) return;
    late final String message;
    setState(() {
      if (unlockedEvolutionStage == 3) {
        halfReleased = true;
        message = '상반신의 봉인이 풀렸습니다. 공격력 보너스가 40%로 상승합니다.';
      } else if (unlockedEvolutionStage == 4) {
        liberationStones -= 3;
        fullyReleased = true;
        message = '그라니아가 봉인 밖으로 걸어 나왔습니다. 공격력 보너스가 65%로 상승합니다.';
      } else {
        trueAwakened = true;
        message = '태고의 수호자로 각성했습니다. 공격력 보너스가 100%로 상승합니다.';
      }
      selectedEvolutionStage = unlockedEvolutionStage;
      battleText = '$evolutionName! 그라니아의 힘이 깨어났다.';
    });
    _save();
    showDialog<void>(
      context: context,
      builder: (_) => AlertDialog(
        title: Text(evolutionName),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('확인'),
          ),
        ],
      ),
    );
  }

  void _showOffline(int reward) => showDialog<void>(
    context: context,
    builder: (_) => AlertDialog(
      title: const Text('방치 보상'),
      content: Text('자리를 비운 동안 결정 $reward개를 모았습니다.'),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('받기'),
        ),
      ],
    ),
  );

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.paused ||
        state == AppLifecycleState.detached) {
      _save();
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    battleTimer?.cancel();
    saveTimer?.cancel();
    frameTicker.dispose();
    motionController.dispose();
    _save();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (!loaded) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            _topBar(),
            Expanded(child: _battlefield()),
            _battlePanel(),
            _controls(),
          ],
        ),
      ),
    );
  }

  Widget _topBar() => Padding(
    padding: const EdgeInsets.fromLTRB(18, 12, 18, 8),
    child: Row(
      children: [
        const Expanded(
          child: Text(
            '리얼 돌 키우기',
            style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800),
          ),
        ),
        const Icon(Icons.diamond_outlined, color: Color(0xFF68DDEA)),
        const SizedBox(width: 6),
        Text(
          '$crystals',
          style: const TextStyle(fontSize: 17, fontWeight: FontWeight.bold),
        ),
        const SizedBox(width: 12),
        const Icon(Icons.hexagon_outlined, size: 19, color: Color(0xFFEACB83)),
        Text(
          ' $liberationStones',
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        const SizedBox(width: 10),
        const Icon(Icons.favorite, size: 18, color: Color(0xFFFF8FA3)),
        Text(' $bond', style: const TextStyle(fontWeight: FontWeight.bold)),
      ],
    ),
  );

  Widget _battlefield() => Padding(
    padding: const EdgeInsets.symmetric(horizontal: 16),
    child: TweenAnimationBuilder<double>(
      key: ValueKey(impactSerial),
      tween: Tween(begin: 0, end: 1),
      duration: const Duration(milliseconds: 190),
      builder: (context, progress, child) {
        final strength = impactSerial == 0
            ? 0.0
            : (criticalHit ? 9.0 : 5.5) * (1 - progress);
        final direction = battlePhase == BattlePhase.counter ? -1.0 : 1.0;
        return Transform.translate(
          offset: Offset(
            sin(progress * pi * 6) * strength * direction,
            cos(progress * pi * 5) * strength * 0.32,
          ),
          child: child,
        );
      },
      child: ClipRRect(
        borderRadius: BorderRadius.circular(24),
        child: Stack(
          fit: StackFit.expand,
          clipBehavior: Clip.none,
          children: [
            Image.asset(
              'assets/images/crystal_mine_battlefield.png',
              fit: BoxFit.cover,
              alignment: Alignment.bottomCenter,
              filterQuality: FilterQuality.none,
            ),
            const DecoratedBox(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [
                    Color(0x22000000),
                    Colors.transparent,
                    Color(0x44000000),
                  ],
                ),
              ),
            ),
            if (evolutionStage >= 4 && heroVisible)
              Positioned(
                left: heroPosition + 35,
                bottom: 105,
                width: 120,
                height: 155,
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: evolutionStage >= 6
                        ? Border.all(color: const Color(0xAAFFE29A), width: 3)
                        : null,
                    gradient: RadialGradient(
                      colors: evolutionStage >= 6
                          ? const [
                              Color(0xAAFFF1AE),
                              Color(0x5545D9F2),
                              Colors.transparent,
                            ]
                          : const [
                              Color(0x775FE7FF),
                              Color(0x2246B9DD),
                              Colors.transparent,
                            ],
                    ),
                    boxShadow: [
                      BoxShadow(
                        color: evolutionStage >= 6
                            ? const Color(0x88FFD66B)
                            : const Color(0x6645D9F2),
                        blurRadius: evolutionStage >= 6 ? 28 : 18,
                        spreadRadius: 3,
                      ),
                    ],
                  ),
                ),
              ),
            if (heroVisible)
              Positioned(
                left: heroPosition + 51,
                bottom: 103,
                width: 88,
                height: 17,
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    color: Colors.black.withValues(alpha: 0.38),
                    borderRadius: BorderRadius.circular(50),
                  ),
                ),
              ),
            if (traveling &&
                heroVisible &&
                (heroSpriteFrame % 8 == 1 || heroSpriteFrame % 8 == 5))
              Positioned(
                left: heroPosition - 48,
                bottom: 96,
                width: 145,
                height: 78,
                child: IgnorePointer(
                  child: CustomPaint(
                    painter: _RunDustPainter(
                      progress: (animationTick % 10) / 10,
                    ),
                  ),
                ),
              ),
            Positioned(
              right: monsterPosition + 39,
              bottom: 107,
              width: 92,
              height: 18,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: Colors.black.withValues(alpha: 0.42),
                  borderRadius: BorderRadius.circular(50),
                ),
              ),
            ),
            if (heroVisible)
              Positioned(
                // Extra horizontal room keeps wide sword trails away from the
                // actor viewport edge without moving Grania's foot anchor.
                left: heroPosition - 15 + (heroHit ? -8 : 0),
                bottom: 92,
                width: 220,
                height: 190,
                child: AnimatedSlide(
                  offset: Offset(
                    heroHit
                        ? -0.055
                        : heroAnimation == 2
                        ? (heroSpriteFrame <= 3
                              ? heroSpriteFrame * 0.015
                              : heroSpriteFrame == 6
                              ? 0.02
                              : 0)
                        : 0,
                    traveling ? sin(animationTick * pi / 2) * 0.012 : 0,
                  ),
                  duration: const Duration(milliseconds: 105),
                  curve: Curves.easeOutCubic,
                  child: RepaintBoundary(child: _actorSprite(hero: true)),
                ),
              ),
            Positioned(
              right: monsterPosition + (monsterHit ? -10 : 0),
              bottom:
                  98 -
                  (monsterAnimation == 4
                      ? min(10.0, monsterSpriteFrame * 0.9)
                      : 0),
              width: 170,
              height: 170,
              child: AnimatedOpacity(
                opacity: enemyVisible
                    ? (monsterAnimation == 4
                          ? max(0.18, 1 - monsterSpriteFrame / 13)
                          : 1)
                    : 0,
                duration: const Duration(milliseconds: 70),
                child: AnimatedSlide(
                  offset: Offset(
                    monsterHit
                        ? 0.075
                        : monsterAnimation == 2
                        ? (monsterSpriteFrame < 4 ? -0.025 : 0.035)
                        : 0,
                    0,
                  ),
                  duration: const Duration(milliseconds: 115),
                  curve: Curves.easeOutCubic,
                  child: RepaintBoundary(child: _actorSprite(hero: false)),
                ),
              ),
            ),
            if (slashVisible)
              Positioned(
                left: heroPosition + 105,
                right: monsterPosition + 70,
                bottom: 135,
                height: 115,
                child: IgnorePointer(
                  child: TweenAnimationBuilder<double>(
                    tween: Tween(begin: 0, end: 1),
                    duration: const Duration(milliseconds: 165),
                    builder: (context, progress, _) => CustomPaint(
                      painter: _SlashTrailPainter(
                        progress: progress,
                        variant: attackVariant,
                        critical: criticalHit,
                        awakened: evolutionStage >= 6,
                      ),
                    ),
                  ),
                ),
              ),
            if (impactBurstVisible)
              Positioned(
                left: battlePhase == BattlePhase.counter
                    ? heroPosition + 42
                    : null,
                right: battlePhase == BattlePhase.counter
                    ? null
                    : monsterPosition + 18,
                bottom: 116,
                width: 180,
                height: 180,
                child: IgnorePointer(
                  child: Image.asset(
                    'assets/frames/effects/hit_effect_$hitEffectFrame.png',
                    key: ValueKey('hit-effect-$impactSerial-$hitEffectFrame'),
                    fit: BoxFit.contain,
                    filterQuality: FilterQuality.none,
                    gaplessPlayback: true,
                  ),
                ),
              ),
            if (monsterHit)
              Positioned(
                right: 80,
                bottom: 245,
                child: TweenAnimationBuilder<double>(
                  key: ValueKey('damage-$impactSerial'),
                  tween: Tween(begin: 0, end: 1),
                  duration: const Duration(milliseconds: 430),
                  builder: (context, progress, child) => Opacity(
                    opacity: (1 - progress * 0.82).clamp(0.0, 1.0),
                    child: Transform.translate(
                      offset: Offset(0, -28 * progress),
                      child: Transform.scale(
                        scale: 1 + 0.3 * (1 - progress),
                        child: child,
                      ),
                    ),
                  ),
                  child: Text(
                    criticalHit ? 'CRITICAL\n-$lastDamage' : '-$lastDamage',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: criticalHit
                          ? const Color(0xFFFFE078)
                          : const Color(0xFFFFD36A),
                      fontSize: criticalHit ? 21 : 24,
                      fontWeight: FontWeight.w900,
                      height: 0.9,
                      shadows: const [
                        Shadow(color: Colors.black, blurRadius: 5),
                      ],
                    ),
                  ),
                ),
              ),
            Positioned(
              top: 14,
              left: 14,
              right: 14,
              child: Row(
                children: [
                  _battleBadge('Lv.$level 그라니아', const Color(0xFF64D8E8)),
                  const Spacer(),
                  _battleBadge('광석 골렘', const Color(0xFFFFA55E)),
                ],
              ),
            ),
            Positioned(
              top: 49,
              left: 16,
              right: 16,
              child: Row(
                children: [
                  Expanded(
                    child: LinearProgressIndicator(
                      value: max(0, heroHp) / maxHeroHp,
                      minHeight: 6,
                      color: const Color(0xFF64D8E8),
                      backgroundColor: const Color(0xFF233848),
                    ),
                  ),
                  const SizedBox(width: 42),
                  Expanded(
                    child: LinearProgressIndicator(
                      value: max(0, enemyHp) / maxEnemyHp,
                      minHeight: 6,
                      color: const Color(0xFFFF785E),
                      backgroundColor: const Color(0xFF492B2B),
                    ),
                  ),
                ],
              ),
            ),
            Positioned(
              left: 16,
              right: 16,
              bottom: 12,
              child: Column(
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: LinearProgressIndicator(
                          value: exp / expNeeded,
                          minHeight: 7,
                          borderRadius: BorderRadius.circular(8),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Text(
                        'EXP $exp/$expNeeded',
                        style: const TextStyle(fontSize: 11),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '$evolutionStage단계 · $evolutionName  |  공격력 $attack',
                    style: const TextStyle(
                      fontSize: 12,
                      color: Color(0xFFB9CAD7),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    ),
  );

  Widget _actorSprite({required bool hero}) {
    final animation = hero ? heroAnimation : monsterAnimation;
    final rawFrame = hero ? heroSpriteFrame : monsterSpriteFrame;
    const names = ['idle', 'walk', 'attack', 'hit', 'death'];
    const counts = [6, 8, 8, 5, 6];
    final frameCount = hero && animation == 0 && evolutionStage == 6
        ? 8
        : counts[animation];
    var frame = animation == 4
        ? min(rawFrame, frameCount - 1)
        : rawFrame % frameCount;
    if (!hero && animation == 2) {
      const safeAttackFrames = [0, 0, 1, 1, 2, 6, 7, 7];
      frame = safeAttackFrames[rawFrame % safeAttackFrames.length];
    }
    if (hero && animation == 2 && evolutionStage >= 4) {
      // Frames 4-5 contain a detached/cropped left sword fragment in the
      // source sheet. Bridge the swing with intact anticipation/recovery.
      const safeHeroAttackFrames = [0, 1, 2, 3, 3, 6, 7, 7];
      frame = safeHeroAttackFrames[rawFrame % safeHeroAttackFrames.length];
    }
    if (hero && animation == 2 && rawFrame >= 8) {
      frame = rawFrame < 10 ? rawFrame - 8 : rawFrame - 4;
    }
    if (!hero && animation == 2 && rawFrame >= 8) {
      frame = rawFrame < 10 ? rawFrame - 8 : rawFrame - 4;
    }
    if (!hero && animation == 4) {
      // Later source frames crop the golem's back. Keep the intact body and
      // finish the death with a whole-sprite sink/fade instead.
      frame = rawFrame < 2 ? 0 : 1;
    }
    final prefix = hero ? 'grania' : 'golem';
    final frameRoot = hero
        ? 'assets/frames/stage_$evolutionStage'
        : 'assets/frames';
    var assetPath = '$frameRoot/${prefix}_${names[animation]}_$frame.png';
    if (hero && animation == 0 && evolutionStage == 6) {
      assetPath = '$frameRoot/grania_idle_v2_$frame.png';
    }
    if (hero && animation == 2 && evolutionStage == 6 && rawFrame <= 2) {
      assetPath = '$frameRoot/grania_attack_v2_$rawFrame.png';
    }
    if (hero && animation == 2 && evolutionStage == 6 && rawFrame >= 8) {
      assetPath = '$frameRoot/grania_attack_transition_${rawFrame - 8}.png';
    }
    if (!hero && animation == 2) {
      final generatedFrame = rawFrame >= 4 ? 6 : rawFrame;
      assetPath = '$frameRoot/golem_attack_v2_$generatedFrame.png';
    }
    if (!hero && animation == 2 && rawFrame >= 8) {
      assetPath = '$frameRoot/golem_attack_transition_${rawFrame - 8}.png';
    }
    return Image.asset(
      assetPath,
      key: ValueKey('$prefix-$evolutionStage-$animation'),
      fit: BoxFit.contain,
      filterQuality: FilterQuality.none,
      gaplessPlayback: true,
      alignment: Alignment.bottomCenter,
    );
  }

  Widget _battleBadge(String text, Color color) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
    decoration: BoxDecoration(
      color: const Color(0xCC0B111A),
      borderRadius: BorderRadius.circular(14),
      border: Border.all(color: color.withValues(alpha: 0.6)),
    ),
    child: Text(
      text,
      style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: color),
    ),
  );

  void _showCharacterDetails() {
    showDialog<void>(
      context: context,
      builder: (_) => StatefulBuilder(
        builder: (context, dialogSetState) => Dialog(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxHeight: 700),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Expanded(
                  child: ClipRRect(
                    borderRadius: const BorderRadius.vertical(
                      top: Radius.circular(28),
                    ),
                    child: Image.asset(imagePath, fit: BoxFit.cover),
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      Text(
                        '그라니아 · $evolutionName',
                        style: const TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 10),
                      const Align(
                        alignment: Alignment.centerLeft,
                        child: Text(
                          '해방한 모습 선택',
                          style: TextStyle(fontWeight: FontWeight.bold),
                        ),
                      ),
                      const SizedBox(height: 8),
                      SizedBox(
                        height: 82,
                        child: ListView.separated(
                          scrollDirection: Axis.horizontal,
                          itemCount: unlockedEvolutionStage,
                          separatorBuilder: (_, _) => const SizedBox(width: 8),
                          itemBuilder: (_, index) {
                            final stage = index + 1;
                            final selected = stage == evolutionStage;
                            return InkWell(
                              borderRadius: BorderRadius.circular(12),
                              onTap: () {
                                setState(() => selectedEvolutionStage = stage);
                                dialogSetState(() {});
                                _save();
                              },
                              child: Container(
                                width: 64,
                                padding: const EdgeInsets.all(4),
                                decoration: BoxDecoration(
                                  color: selected
                                      ? const Color(0x3345D9F2)
                                      : const Color(0x221F2D3D),
                                  borderRadius: BorderRadius.circular(12),
                                  border: Border.all(
                                    color: selected
                                        ? const Color(0xFF64D8E8)
                                        : const Color(0xFF3B4A59),
                                    width: selected ? 2 : 1,
                                  ),
                                ),
                                child: Column(
                                  children: [
                                    Expanded(
                                      child: Image.asset(
                                        'assets/frames/stage_$stage/grania_idle_0.png',
                                        filterQuality: FilterQuality.none,
                                      ),
                                    ),
                                    Text(
                                      '$stage단계',
                                      style: const TextStyle(fontSize: 10),
                                    ),
                                  ],
                                ),
                              ),
                            );
                          },
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        nextCondition,
                        textAlign: TextAlign.center,
                        style: const TextStyle(color: Color(0xFFEACB83)),
                      ),
                      const SizedBox(height: 8),
                      TextButton(
                        onPressed: () => Navigator.pop(context),
                        child: const Text('선택 완료'),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  /*
  The full-size evolution art remains available through the character-details
  dialog while the main screen focuses on the pixel battle.
  */

  Widget _battlePanel() => Container(
    margin: const EdgeInsets.fromLTRB(16, 12, 16, 8),
    padding: const EdgeInsets.all(14),
    decoration: BoxDecoration(
      color: const Color(0xFF121C2B),
      borderRadius: BorderRadius.circular(18),
    ),
    child: Column(
      children: [
        Row(
          children: [
            Text(
              '던전 $dungeonStage-1${dungeonStage == 10 ? '  BOSS' : ''}',
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
            const Spacer(),
            if (bossDefeated)
              const Text('보스 처치 완료', style: TextStyle(color: Color(0xFF76E3A1)))
            else
              Text('${max(0, enemyHp).ceil()} / $maxEnemyHp'),
          ],
        ),
        const SizedBox(height: 8),
        LinearProgressIndicator(
          value: max(0, enemyHp) / maxEnemyHp,
          minHeight: 11,
          color: const Color(0xFFE66B76),
          backgroundColor: const Color(0xFF40252E),
          borderRadius: BorderRadius.circular(8),
        ),
        const SizedBox(height: 8),
        Text(
          battleText,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(color: Color(0xFFAAB9C8)),
        ),
      ],
    ),
  );

  Widget _controls() => Padding(
    padding: const EdgeInsets.fromLTRB(16, 0, 16, 18),
    child: Row(
      children: [
        Expanded(
          child: FilledButton.icon(
            onPressed: _showCharacterDetails,
            icon: const Icon(Icons.person_search),
            label: const Text('캐릭터 보기'),
            style: FilledButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 15),
            ),
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: FilledButton.tonalIcon(
            onPressed: canEvolve ? _evolve : null,
            icon: Icon(
              unlockedEvolutionStage == 6
                  ? Icons.auto_awesome
                  : Icons.lock_open,
            ),
            label: Text(evolveButtonText),
            style: FilledButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 15),
            ),
          ),
        ),
      ],
    ),
  );
}
