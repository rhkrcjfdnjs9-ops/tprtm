class CombatFrameCue {
  const CombatFrameCue(this.frame, this.milliseconds);

  final int frame;
  final int milliseconds;
}

abstract final class CombatMotions {
  static const heroLeadIn = [CombatFrameCue(0, 115)];

  static const heroWindup = [
    CombatFrameCue(1, 110),
    CombatFrameCue(2, 82),
    CombatFrameCue(3, 54),
    CombatFrameCue(4, 38),
  ];

  static const heroRecovery = [
    CombatFrameCue(5, 88),
    CombatFrameCue(6, 112),
    CombatFrameCue(7, 145),
  ];

  static const golemLeadIn = [CombatFrameCue(8, 115), CombatFrameCue(9, 100)];

  static const golemWindup = [
    CombatFrameCue(0, 120),
    CombatFrameCue(2, 90),
    CombatFrameCue(4, 65),
  ];

  static const golemRecovery = [
    CombatFrameCue(10, 125),
    CombatFrameCue(11, 145),
  ];
}
