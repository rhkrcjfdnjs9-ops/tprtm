import 'dart:async';
import 'dart:math';

import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

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

class GamePage extends StatefulWidget {
  const GamePage({super.key});

  @override
  State<GamePage> createState() => _GamePageState();
}

enum BattlePhase { idle, approach, attack, hit, counter, retreat, defeated }

class _GamePageState extends State<GamePage>
    with WidgetsBindingObserver, SingleTickerProviderStateMixin {
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
  bool monsterHit = false;
  bool screenShake = false;
  bool hitStop = false;
  bool battleBusy = false;
  bool enemyVisible = true;
  double heroPosition = 32;
  double monsterPosition = 32;
  BattlePhase battlePhase = BattlePhase.idle;
  int lastDamage = 0;
  int spriteFrame = 0;
  int heroAnimation = 0;
  int monsterAnimation = 0;
  String battleText = '자동 전투 준비 중';
  Timer? battleTimer;
  Timer? saveTimer;
  Timer? frameTimer;
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
    WidgetsBinding.instance.addObserver(this);
    _load();
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
    frameTimer = Timer.periodic(const Duration(milliseconds: 120), (_) {
      if (mounted && !hitStop) setState(() => spriteFrame++);
    });
    saveTimer = Timer.periodic(const Duration(seconds: 10), (_) => _save());
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
      spriteFrame = 0;
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
          heroPosition = position.roundToDouble();
        } else {
          monsterPosition = position.roundToDouble();
        }
      });
    }

    motionController.addListener(update);
    await motionController.forward();
    motionController.removeListener(update);
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
    await Future<void>.delayed(const Duration(milliseconds: 330));
    if (!mounted) return;
    setState(() {
      monsterHit = true;
      monsterAnimation = 3;
      hitStop = true;
      screenShake = true;
      lastDamage = attack;
      enemyHp -= attack;
      battleText = '그라니아가 $attack 피해를 입혔다!';
    });
    await Future<void>.delayed(const Duration(milliseconds: 65));
    if (!mounted) return;
    setState(() {
      hitStop = false;
      screenShake = false;
    });

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
        monsterPosition = -120;
        heroAnimation = 1;
        battleText = '승리! 다음 적을 향해 이동 중…';
      });
      await _moveActor(
        hero: true,
        target: 104,
        duration: const Duration(milliseconds: 480),
        curve: Curves.easeInOut,
      );
      if (!mounted) return;
      setState(() {
        enemyHp = maxEnemyHp.toDouble();
        enemyVisible = true;
        monsterAnimation = 1;
        battleText = clearedStage == 10
            ? '광산 보스 처치! 해방석 1개 획득'
            : '새로운 광석 골렘 등장';
      });
      await _moveActor(
        hero: false,
        target: 32,
        duration: const Duration(milliseconds: 460),
        curve: Curves.easeOutCubic,
      );
      if (!mounted) return;
      await _moveActor(
        hero: true,
        target: 32,
        duration: const Duration(milliseconds: 380),
        curve: Curves.easeInOutCubic,
      );
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
    await _moveActor(
      hero: false,
      target: 72,
      duration: const Duration(milliseconds: 360),
      curve: Curves.easeInOutCubic,
    );
    if (!mounted) return;
    _setCombatState(phase: BattlePhase.counter, hero: 0, monster: 2);
    setState(() => battleText = '광석 골렘의 반격!');
    await Future<void>.delayed(const Duration(milliseconds: 330));
    if (!mounted) return;
    setState(() {
      heroHp -= enemyAttack;
      heroAnimation = 3;
      hitStop = true;
      screenShake = true;
      battleText = '그라니아가 $enemyAttack 피해를 받았다';
    });
    await Future<void>.delayed(const Duration(milliseconds: 65));
    if (!mounted) return;
    setState(() {
      hitStop = false;
      screenShake = false;
    });
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
    frameTimer?.cancel();
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
    child: Transform.translate(
      offset: screenShake ? const Offset(5, -3) : Offset.zero,
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
            if (evolutionStage >= 4)
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
            Positioned(
              left: heroPosition,
              bottom: 92,
              width: 190,
              height: 190,
              child: RepaintBoundary(child: _actorSprite(hero: true)),
            ),
            Positioned(
              right: monsterPosition + (monsterHit ? -10 : 0),
              bottom: 98,
              width: 170,
              height: 170,
              child: AnimatedOpacity(
                opacity: enemyVisible ? 1 : 0,
                duration: const Duration(milliseconds: 180),
                child: RepaintBoundary(child: _actorSprite(hero: false)),
              ),
            ),
            if (monsterHit)
              Positioned(
                right: 80,
                bottom: 245,
                child: Text(
                  '-$lastDamage',
                  style: const TextStyle(
                    color: Color(0xFFFFD36A),
                    fontSize: 24,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ),
            if (monsterHit) ...[
              const Positioned(
                right: 45,
                bottom: 172,
                child: Icon(Icons.circle, size: 8, color: Color(0xFFB5A38C)),
              ),
              const Positioned(
                right: 66,
                bottom: 195,
                child: Icon(Icons.circle, size: 12, color: Color(0xFF806E5D)),
              ),
              const Positioned(
                right: 95,
                bottom: 162,
                child: Icon(Icons.circle, size: 6, color: Color(0xFFD0BDA2)),
              ),
            ],
            if (battlePhase == BattlePhase.attack)
              Positioned(
                right: monsterPosition + 82,
                bottom: 165,
                child: Transform.rotate(
                  angle: -0.55,
                  child: const Icon(
                    Icons.bolt,
                    size: 64,
                    color: Color(0xFFE8FAFF),
                    shadows: [Shadow(color: Color(0xFF45CDE8), blurRadius: 12)],
                  ),
                ),
              ),
            if (monsterHit)
              Positioned(
                right: monsterPosition + 76,
                bottom: 174,
                child: const Icon(
                  Icons.auto_awesome,
                  size: 48,
                  color: Color(0xFF8DEBFF),
                  shadows: [Shadow(color: Colors.white, blurRadius: 14)],
                ),
              ),
            if (battlePhase == BattlePhase.counter && monsterAnimation == 2)
              Positioned(
                left: heroPosition + 75,
                bottom: 158,
                child: Container(
                  width: 72,
                  height: 72,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: Border.all(
                      color: const Color(0xAAFF8A35),
                      width: 5,
                    ),
                    boxShadow: const [
                      BoxShadow(
                        color: Color(0x66FF6A20),
                        blurRadius: 18,
                        spreadRadius: 5,
                      ),
                    ],
                  ),
                ),
              ),
            if (battlePhase == BattlePhase.counter && hitStop)
              Positioned(
                left: heroPosition + 92,
                bottom: 175,
                child: Stack(
                  alignment: Alignment.center,
                  children: [
                    Container(
                      width: 54,
                      height: 54,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        border: Border.all(
                          color: const Color(0xFFFFB04A),
                          width: 5,
                        ),
                      ),
                    ),
                    const Icon(Icons.star, size: 34, color: Color(0xFFFFE3A0)),
                  ],
                ),
              ),
            if (battlePhase == BattlePhase.counter && hitStop)
              Positioned(
                left: heroPosition + 48,
                bottom: 120,
                width: 150,
                height: 180,
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(80),
                    gradient: const RadialGradient(
                      colors: [Color(0x77FF4E45), Colors.transparent],
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
    const names = ['idle', 'walk', 'attack', 'hit', 'death'];
    const counts = [6, 8, 8, 5, 6];
    var frame = animation == 4
        ? min(spriteFrame, hero ? counts[animation] - 1 : 3)
        : spriteFrame % counts[animation];
    if (!hero && animation == 2) {
      const safeAttackFrames = [0, 0, 1, 1, 2, 6, 7, 7];
      frame = safeAttackFrames[spriteFrame % safeAttackFrames.length];
    }
    final prefix = hero ? 'grania' : 'golem';
    final frameRoot = hero
        ? 'assets/frames/stage_$evolutionStage'
        : 'assets/frames';
    return Image.asset(
      '$frameRoot/${prefix}_${names[animation]}_$frame.png',
      key: ValueKey('$prefix-$evolutionStage-$animation-$frame'),
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
