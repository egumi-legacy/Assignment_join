using System;
using UnityEngine;

namespace MenuLevelProgression
{
    [Serializable]
    public sealed class LevelDefinition
    {
        [SerializeField] private int levelId = 1;
        [SerializeField] private string displayName = "Level 1";
        [SerializeField] private string sceneName = "SampleScene";

        public int LevelId => Mathf.Max(1, levelId);
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? $"Level {LevelId}" : displayName;
        public string SceneName => sceneName;
    }
}
