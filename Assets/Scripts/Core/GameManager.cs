using UnityEngine;
using ChronoDrift.Audio;
using ChronoDrift.Player;
using ChronoDrift.Progression;
using ChronoDrift.UI;

namespace ChronoDrift.Core
{
    public enum GameState
    {
        Title,
        Playing,
        UpgradeSelect,
        Paused,
        GameOver
    }

    /// <summary>
    /// Central game state, score/combo tracker, and run lifecycle coordinator.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Title;
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public int Kills { get; private set; }
        public int Combo { get; private set; } = 1;
        public int BestCombo { get; private set; } = 1;
        public float ComboTimer { get; private set; }
        public float ComboDuration => 3.5f;

        private const string HighScoreKey = "ChronoDrift_HighScore";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        private void Update()
        {
            if (State == GameState.Playing)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SetState(GameState.Paused);
                    return;
                }

                if (Combo > 1)
                {
                    ComboTimer -= Time.deltaTime;
                    if (ComboTimer <= 0f)
                    {
                        Combo = 1;
                    }
                }
            }
            else if (State == GameState.Paused)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    SetState(GameState.Playing);
                }
            }
            else if (State == GameState.GameOver)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    StartNewRun();
                }
            }
        }

        public void StartNewRun()
        {
            Score = 0;
            Kills = 0;
            Combo = 1;
            BestCombo = 1;
            ComboTimer = 0f;

            if (TimeDilationManager.Instance != null)
                TimeDilationManager.Instance.ResetEnergy();

            if (PlayerController.Instance != null)
                PlayerController.Instance.ResetForNewRun();

            if (WaveManager.Instance != null)
                WaveManager.Instance.ResetAndStart();

            SetState(GameState.Playing);
            ProceduralAudio.Instance?.PlayUpgrade();
        }

        public void SetState(GameState newState)
        {
            State = newState;
            GameUI.Instance?.RefreshStatePanels();
        }

        public void AddKill(int basePoints)
        {
            Kills++;
            Combo = Mathf.Min(16, Combo + 1);
            if (Combo > BestCombo) BestCombo = Combo;
            ComboTimer = ComboDuration;

            Score += basePoints * Combo;
            if (Score > HighScore)
            {
                HighScore = Score;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
            }

            TimeDilationManager.Instance?.AddEnergy(8f);
        }

        public void TriggerGameOver()
        {
            if (State == GameState.GameOver) return;
            PlayerPrefs.Save();
            SetState(GameState.GameOver);
            ProceduralAudio.Instance?.PlayExplosion();
        }
    }
}
