namespace SpiritStone.Prototype
{
    public static class PrototypeElementChart
    {
        public const float AdvantageMultiplier = 1.25f;
        public const float DisadvantageMultiplier = 0.8f;

        public static float GetDamageMultiplier(SpiritElement attacker, SpiritElement defender)
        {
            if (IsAdvantage(attacker, defender)) return AdvantageMultiplier;
            if (IsAdvantage(defender, attacker)) return DisadvantageMultiplier;
            return 1f;
        }

        public static bool IsAdvantage(SpiritElement attacker, SpiritElement defender)
        {
            return (attacker == SpiritElement.Water && defender == SpiritElement.Fire) ||
                   (attacker == SpiritElement.Fire && defender == SpiritElement.Wind) ||
                   (attacker == SpiritElement.Wind && defender == SpiritElement.Lightning) ||
                   (attacker == SpiritElement.Lightning && defender == SpiritElement.Water) ||
                   (attacker == SpiritElement.Light && defender == SpiritElement.Dark) ||
                   (attacker == SpiritElement.Dark && defender == SpiritElement.Light);
        }

        public static string GetDisplayName(SpiritElement element)
        {
            return element switch
            {
                SpiritElement.Fire => "불",
                SpiritElement.Water => "물",
                SpiritElement.Wind => "바람",
                SpiritElement.Lightning => "번개",
                SpiritElement.Light => "빛",
                SpiritElement.Dark => "어둠",
                _ => "미지정"
            };
        }

        public static string GetRelationshipLabel(SpiritElement attacker, SpiritElement defender)
        {
            float multiplier = GetDamageMultiplier(attacker, defender);
            if (multiplier > 1f) return "상성 유리";
            if (multiplier < 1f) return "상성 불리";
            return "상성 보통";
        }
    }
}
