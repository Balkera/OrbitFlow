using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SquareFlow.Runtime
{
    public sealed class SaveDataService
    {
        private const string LevelKey = "sf-unity-level";
        private const string CompletedKey = "sf-unity-completed";
        private const string ScoresKey = "sf-unity-scores";
        private const string DarkKey = "sf-unity-dark";
        private const string MutedKey = "sf-unity-muted";
        private const int MaxScores = 10;

        [System.Serializable]
        public struct ScoreEntry
        {
            public int Level;
            public int Moves;
            public int Score;

            public ScoreEntry(int level, int moves, int score)
            {
                Level = level;
                Moves = moves;
                Score = score;
            }
        }

        [System.Serializable]
        internal sealed class ScoreList
        {
            public List<ScoreEntry> Entries = new List<ScoreEntry>();
        }

        public int Level
        {
            get => Mathf.Max(1, PlayerPrefs.GetInt(LevelKey, 1));
            set => PlayerPrefs.SetInt(LevelKey, Mathf.Max(1, value));
        }

        public bool DarkMode
        {
            get => PlayerPrefs.GetInt(DarkKey, 1) == 1;
            set => PlayerPrefs.SetInt(DarkKey, value ? 1 : 0);
        }

        public bool Muted
        {
            get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
            set => PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
        }

        public HashSet<int> CompletedLevels()
        {
            string data = PlayerPrefs.GetString(CompletedKey, string.Empty);
            return new HashSet<int>(data.Split(',').Where(x => int.TryParse(x, out _)).Select(int.Parse));
        }

        public void SaveCompletedLevels(HashSet<int> levels)
        {
            PlayerPrefs.SetString(CompletedKey, string.Join(",", levels.OrderBy(x => x)));
        }

        public void MarkCompleted(int level)
        {
            HashSet<int> completed = CompletedLevels();
            completed.Add(Mathf.Max(1, level));
            SaveCompletedLevels(completed);
        }

        public ScoreEntry[] Scores()
        {
            ScoreList list = LoadScores();
            return list.Entries
                .OrderByDescending(entry => entry.Score)
                .Take(MaxScores)
                .ToArray();
        }

        public void AddScore(int level, int moves, int score)
        {
            ScoreList list = LoadScores();
            list.Entries.Add(new ScoreEntry(Mathf.Max(1, level), Mathf.Max(0, moves), Mathf.Max(0, score)));
            list.Entries = list.Entries
                .OrderByDescending(entry => entry.Score)
                .Take(MaxScores)
                .ToList();
            PlayerPrefs.SetString(ScoresKey, JsonUtility.ToJson(list));
        }

        public void ClearProgress()
        {
            PlayerPrefs.DeleteKey(LevelKey);
            PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.DeleteKey(ScoresKey);
        }

        private static ScoreList LoadScores()
        {
            string data = PlayerPrefs.GetString(ScoresKey, string.Empty);
            if (string.IsNullOrEmpty(data))
                return new ScoreList();

            ScoreList list = JsonUtility.FromJson<ScoreList>(data);
            if (list == null || list.Entries == null)
                return new ScoreList();

            return list;
        }
    }
}
