using System.Collections.Generic;
using NUnit.Framework;
using SquareFlow.Runtime;
using UnityEngine;

namespace SquareFlow.Tests
{
    public sealed class SaveDataServiceTests
    {
        // Mirrors SaveDataService keys so tests clean only this feature's PlayerPrefs state.
        private const string LevelKey = "sf-unity-level";
        private const string CompletedKey = "sf-unity-completed";
        private const string ScoresKey = "sf-unity-scores";
        private const string DarkKey = "sf-unity-dark";
        private const string MutedKey = "sf-unity-muted";

        [SetUp]
        public void SetUp()
        {
            ClearKeys();
        }

        [TearDown]
        public void TearDown()
        {
            ClearKeys();
        }

        [Test]
        public void LevelDefaultsToOneAndClampsMinimum()
        {
            SaveDataService save = new SaveDataService();

            Assert.That(save.Level, Is.EqualTo(1));

            save.Level = -4;

            Assert.That(save.Level, Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetInt(LevelKey), Is.EqualTo(1));
        }

        [Test]
        public void CompletedLevelsRoundTripSortedSetValues()
        {
            SaveDataService save = new SaveDataService();

            save.SaveCompletedLevels(new HashSet<int> { 3, 1, 2 });

            Assert.That(save.CompletedLevels(), Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(PlayerPrefs.GetString(CompletedKey), Is.EqualTo("1,2,3"));
        }

        [Test]
        public void MarkCompletedAddsLevelWithoutDroppingExistingValues()
        {
            SaveDataService save = new SaveDataService();
            save.SaveCompletedLevels(new HashSet<int> { 1, 3 });

            save.MarkCompleted(2);
            save.MarkCompleted(3);

            Assert.That(save.CompletedLevels(), Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(PlayerPrefs.GetString(CompletedKey), Is.EqualTo("1,2,3"));
        }

        [Test]
        public void ScoresRoundTripSortedByScoreDescending()
        {
            SaveDataService save = new SaveDataService();

            save.AddScore(2, 12, 800);
            save.AddScore(1, 8, 1200);
            save.AddScore(3, 14, 400);

            SaveDataService.ScoreEntry[] scores = new SaveDataService().Scores();
            Assert.That(scores.Length, Is.EqualTo(3));
            Assert.That(scores[0].Score, Is.EqualTo(1200));
            Assert.That(scores[0].Level, Is.EqualTo(1));
            Assert.That(scores[0].Moves, Is.EqualTo(8));
            Assert.That(scores[1].Score, Is.EqualTo(800));
            Assert.That(scores[2].Score, Is.EqualTo(400));
        }

        [Test]
        public void AddScoreKeepsOnlyTopTenEntries()
        {
            SaveDataService save = new SaveDataService();

            for (int i = 1; i <= 12; i++)
                save.AddScore(i, i + 2, i * 100);

            SaveDataService.ScoreEntry[] scores = save.Scores();
            Assert.That(scores.Length, Is.EqualTo(10));
            Assert.That(scores[0].Score, Is.EqualTo(1200));
            Assert.That(scores[9].Score, Is.EqualTo(300));
            Assert.That(scores, Has.None.Matches<SaveDataService.ScoreEntry>(entry => entry.Score == 100 || entry.Score == 200));
        }

        [Test]
        public void DarkModeAndMutedUseExpectedDefaultsAndPersistFlags()
        {
            SaveDataService save = new SaveDataService();

            Assert.That(save.DarkMode, Is.True);
            Assert.That(save.Muted, Is.False);

            save.DarkMode = false;
            save.Muted = true;

            Assert.That(new SaveDataService().DarkMode, Is.False);
            Assert.That(new SaveDataService().Muted, Is.True);
        }

        [Test]
        public void ClearProgressDeletesLevelCompletedAndScoresOnly()
        {
            SaveDataService save = new SaveDataService();
            save.Level = 7;
            save.SaveCompletedLevels(new HashSet<int> { 1, 4 });
            PlayerPrefs.SetString(ScoresKey, "score-data");
            save.DarkMode = false;
            save.Muted = true;

            save.ClearProgress();

            Assert.That(PlayerPrefs.HasKey(LevelKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(CompletedKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(ScoresKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(DarkKey), Is.True);
            Assert.That(PlayerPrefs.HasKey(MutedKey), Is.True);
        }

        private static void ClearKeys()
        {
            PlayerPrefs.DeleteKey(LevelKey);
            PlayerPrefs.DeleteKey(CompletedKey);
            PlayerPrefs.DeleteKey(ScoresKey);
            PlayerPrefs.DeleteKey(DarkKey);
            PlayerPrefs.DeleteKey(MutedKey);
        }
    }
}
