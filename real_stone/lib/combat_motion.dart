class CombatFrameCue {
  const CombatFrameCue(this.frame, this.milliseconds);

  final int frame;
  final int milliseconds;
}

abstract final class CombatMotions {
  static const heroLeadIn = [CombatFrameCue(8, 90), CombatFrameCue(9, 75)];

  static const heroWindup = [
    CombatFrameCue(0, 105),
    CombatFrameCue(1, 82),
    CombatFrameCue(2, 58),
    CombatFrameCue(3, 42),
  ];

  static const heroRecovery = [
    CombatFrameCue(6, 82),
    CombatFrameCue(7, 125),
    CombatFrameCue(10, 90),
    CombatFrameCue(11, 110),
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
