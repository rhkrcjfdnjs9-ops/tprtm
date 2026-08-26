using UnityEngine;

namespace RealStone
{
    public static class ProgressService
    {
        private const string DungeonStageKey = "real_stone.dungeon_stage";
        public static int LoadDungeonStage() => Mathf.Max(1, PlayerPrefs.GetInt(DungeonStageKey, 1));
        public static void SaveDungeonStage(int stage)
        {
            PlayerPrefs.SetInt(DungeonStageKey, Mathf.Max(1, stage));
            PlayerPrefs.Save();
        }
    }
}
