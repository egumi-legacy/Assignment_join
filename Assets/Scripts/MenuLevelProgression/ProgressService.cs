using UnityEngine;

namespace MenuLevelProgression
{
    public sealed class ProgressService
    {
        private const string HighestUnlockedLevelKey = "menu-level-progression.highest-unlocked-level";

        public int HighestUnlockedLevel => Mathf.Max(1, PlayerPrefs.GetInt(HighestUnlockedLevelKey, 1));

        public void UnlockThrough(int levelId)
        {
            int clampedLevel = Mathf.Max(1, levelId);
            if (clampedLevel <= HighestUnlockedLevel)
            {
                return;
            }

            PlayerPrefs.SetInt(HighestUnlockedLevelKey, clampedLevel);
            PlayerPrefs.Save();
        }

        public void ClearProgressForDebug()
        {
            PlayerPrefs.DeleteKey(HighestUnlockedLevelKey);
            PlayerPrefs.Save();
        }
    }
}
