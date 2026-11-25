using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ChronoDrift.Core;
using ChronoDrift.Player;
using ChronoDrift.Progression;

namespace ChronoDrift.UI
{
    /// <summary>
    /// Programmatic sci-fi UI & HUD controller: Title Screen, In-Game HUD,
    /// 3-Card Upgrade Selector, Pause Menu, and Game Over Summary.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        private Font _font;
        private Sprite _whiteSprite;
        private GameObject _titlePanel;
        private GameObject _hudPanel;
        private GameObject _upgradePanel;
        private GameObject _pausePanel;
        private GameObject _gameOverPanel;

        // HUD elements
        private Image _hpFill;
        private Text _hpText;
        private Image _chronoFill;
        private Image _dashCdFill;
        private Image _singularityCdFill;
        private Text _waveText;
        private Text _scoreText;
        private Text _comboText;
        private GameObject _bossBarRoot;
        private Image _bossBarFill;

        // Title & GameOver texts
        private Text _titleHighScoreText;
        private Text _gameOverStatsText;

        // Upgrade cards
        private readonly List<Button> _upgradeButtons = new List<Button>();
        private readonly List<Text> _upgradeTitles = new List<Text>();
        private readonly List<Text> _upgradeDescs = new List<Text>();
        private readonly List<Image> _upgradeAccents = new List<Image>();
        private List<UpgradeOption> _currentOptions;

        private void Awake()
        {
            Instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            _whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            BuildUI();
            RefreshStatePanels();
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            if (GameManager.Instance.State == GameState.Playing)
            {
                UpdateHUD();
            }
            else if (GameManager.Instance.State == GameState.UpgradeSelect && _currentOptions != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) SelectUpgrade(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectUpgrade(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectUpgrade(2);
            }
        }

        private void UpdateHUD()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                float hpNorm = (float)player.CurrentHealth / Mathf.Max(1, player.maxHealth);
                _hpFill.fillAmount = hpNorm;
                _hpText.text = $"HULL  {player.CurrentHealth} / {player.maxHealth}";
                _dashCdFill.fillAmount = 1f - player.DashCooldownNormalized;
                _singularityCdFill.fillAmount = 1f - player.SingularityCooldownNormalized;
            }

            if (TimeDilationManager.Instance != null)
            {
                _chronoFill.fillAmount = TimeDilationManager.Instance.NormalizedEnergy;
            }

            if (WaveManager.Instance != null)
            {
                _waveText.text = $"WAVE {WaveManager.Instance.CurrentWave}";
                var boss = WaveManager.Instance.ActiveBoss;
                if (boss != null && boss.IsAlive)
                {
                    _bossBarRoot.SetActive(true);
                    _bossBarFill.fillAmount = Mathf.Clamp01(boss.CurrentHp / boss.maxHp);
                }
                else
                {
                    _bossBarRoot.SetActive(false);
                }
            }

            _scoreText.text = $"{GameManager.Instance.Score:N0}";
            int combo = GameManager.Instance.Combo;
            _comboText.text = combo > 1 ? $"x{combo} COMBO" : "";
            _comboText.color = combo >= 8 ? new Color(1f, 0.85f, 0.2f) : new Color(0.2f, 0.95f, 1f);
        }

        public void RefreshStatePanels()
        {
            if (GameManager.Instance == null) return;
            var s = GameManager.Instance.State;

            _titlePanel.SetActive(s == GameState.Title);
            _hudPanel.SetActive(s == GameState.Playing || s == GameState.UpgradeSelect || s == GameState.Paused);
            _upgradePanel.SetActive(s == GameState.UpgradeSelect);
            _pausePanel.SetActive(s == GameState.Paused);
            _gameOverPanel.SetActive(s == GameState.GameOver);

            if (s == GameState.Title && _titleHighScoreText != null)
            {
                _titleHighScoreText.text = $"BEST SCORE: {GameManager.Instance.HighScore:N0}";
            }
            else if (s == GameState.GameOver && _gameOverStatsText != null)
            {
                int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 1;
                _gameOverStatsText.text =
                    $"WAVE REACHED:  {wave}\n" +
                    $"FINAL SCORE:   {GameManager.Instance.Score:N0}\n" +
                    $"BEST COMBO:    x{GameManager.Instance.BestCombo}\n" +
                    $"ENEMIES SLAIN: {GameManager.Instance.Kills}\n" +
                    $"HIGH SCORE:    {GameManager.Instance.HighScore:N0}";
            }
        }

        public void ShowUpgradeChoices(List<UpgradeOption> options)
        {
            _currentOptions = options;
            for (int i = 0; i < 3 && i < options.Count; i++)
            {
                _upgradeTitles[i].text = $"[{i + 1}] {options[i].Title}";
                _upgradeDescs[i].text = options[i].Description;
                _upgradeAccents[i].color = options[i].AccentColor;
            }
        }

        private void SelectUpgrade(int index)
        {
            if (_currentOptions == null || index < 0 || index >= _currentOptions.Count) return;
            _currentOptions[index].Apply?.Invoke();
            _currentOptions = null;
            GameManager.Instance.SetState(GameState.Playing);
            WaveManager.Instance?.StartNextWave();
        }

        private void BuildUI()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("GameCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            BuildHUD(canvasGo.transform);
            BuildTitlePanel(canvasGo.transform);
            BuildUpgradePanel(canvasGo.transform);
            BuildPausePanel(canvasGo.transform);
            BuildGameOverPanel(canvasGo.transform);
        }

        private void BuildHUD(Transform root)
        {
            _hudPanel = CreatePanel(root, "HUD", Color.clear);

            // Top-Left: Hull & Chrono bars
            var topLeft = CreateBox(_hudPanel.transform, "TopLeft", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(360f, 110f), new Color(0.04f, 0.06f, 0.11f, 0.72f));

            _hpText = CreateText(topLeft.transform, "HULL 5 / 5", 18, new Vector2(16f, -12f), new Vector2(320f, 26f), TextAnchor.MiddleLeft, Color.white);
            _hpFill = CreateBar(topLeft.transform, new Vector2(16f, -42f), new Vector2(328f, 18f), new Color(0.15f, 0.95f, 0.55f));

            CreateText(topLeft.transform, "CHRONO FOCUS  [SHIFT / E]", 14, new Vector2(16f, -66f), new Vector2(320f, 20f), TextAnchor.MiddleLeft, new Color(0.55f, 0.85f, 1f));
            _chronoFill = CreateBar(topLeft.transform, new Vector2(16f, -88f), new Vector2(328f, 12f), new Color(0.2f, 0.85f, 1f));

            // Top-Right: Wave, Score, Combo
            var topRight = CreateBox(_hudPanel.transform, "TopRight", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -24f), new Vector2(300f, 110f), new Color(0.04f, 0.06f, 0.11f, 0.72f));
            _waveText = CreateText(topRight.transform, "WAVE 1", 22, new Vector2(-16f, -12f), new Vector2(268f, 30f), TextAnchor.MiddleRight, new Color(0.25f, 0.92f, 1f));
            AnchorRight(_waveText.rectTransform);
            _scoreText = CreateText(topRight.transform, "0", 30, new Vector2(-16f, -42f), new Vector2(268f, 36f), TextAnchor.MiddleRight, Color.white);
            AnchorRight(_scoreText.rectTransform);
            _comboText = CreateText(topRight.transform, "", 20, new Vector2(-16f, -78f), new Vector2(268f, 26f), TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.2f));
            AnchorRight(_comboText.rectTransform);

            // Bottom-Left: Ability Cooldowns
            var botLeft = CreateBox(_hudPanel.transform, "Abilities", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 24f), new Vector2(340f, 64f), new Color(0.04f, 0.06f, 0.11f, 0.72f));
            CreateText(botLeft.transform, "DASH [SPACE]", 14, new Vector2(14f, -8f), new Vector2(145f, 20f), TextAnchor.MiddleLeft, Color.white);
            _dashCdFill = CreateBar(botLeft.transform, new Vector2(14f, -34f), new Vector2(145f, 14f), new Color(0.15f, 0.95f, 1f));

            CreateText(botLeft.transform, "SINGULARITY [RMB]", 14, new Vector2(178f, -8f), new Vector2(150f, 20f), TextAnchor.MiddleLeft, new Color(0.85f, 0.65f, 1f));
            _singularityCdFill = CreateBar(botLeft.transform, new Vector2(178f, -34f), new Vector2(148f, 14f), new Color(0.75f, 0.35f, 1f));

            // Top-Center: Boss Health Bar
            _bossBarRoot = CreateBox(_hudPanel.transform, "BossBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(600f, 54f), new Color(0.08f, 0.02f, 0.06f, 0.85f));
            var bossTitle = CreateText(_bossBarRoot.transform, "HEX-TITAN OVERLORD", 18, new Vector2(0f, -6f), new Vector2(560f, 22f), TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.55f));
            CenterTop(bossTitle.rectTransform);
            _bossBarFill = CreateBar(_bossBarRoot.transform, new Vector2(20f, -32f), new Vector2(560f, 14f), new Color(1f, 0.18f, 0.5f));
            _bossBarRoot.SetActive(false);
        }

        private void BuildTitlePanel(Transform root)
        {
            _titlePanel = CreatePanel(root, "TitlePanel", new Color(0.02f, 0.03f, 0.07f, 0.86f));

            var title = CreateText(_titlePanel.transform, "CHRONO DRIFT", 72, new Vector2(0f, 170f), new Vector2(900f, 100f), TextAnchor.MiddleCenter, new Color(0.2f, 0.95f, 1f));
            Center(title.rectTransform);

            var sub = CreateText(_titlePanel.transform, "RICOCHET ARENA ROGUELIKE", 24, new Vector2(0f, 100f), new Vector2(700f, 40f), TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 1f));
            Center(sub.rectTransform);

            var controls = CreateText(_titlePanel.transform,
                "WASD: Thrusters   |   MOUSE: Aim & Railgun (Wall Ricochets = +50% DMG)\n" +
                "SPACE: Phase-Dash   |   RMB: Gravity Singularity   |   SHIFT / E: Chrono Slow-Mo",
                20, new Vector2(0f, 10f), new Vector2(920f, 80f), TextAnchor.MiddleCenter, new Color(0.85f, 0.9f, 0.95f));
            Center(controls.rectTransform);

            _titleHighScoreText = CreateText(_titlePanel.transform, "BEST SCORE: 0", 22, new Vector2(0f, -65f), new Vector2(500f, 36f), TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.25f));
            Center(_titleHighScoreText.rectTransform);

            CreateButton(_titlePanel.transform, "INITIATE RUN", new Vector2(0f, -155f), new Vector2(300f, 64f), new Color(0.15f, 0.85f, 1f), () =>
            {
                GameManager.Instance.StartNewRun();
            });
        }

        private void BuildUpgradePanel(Transform root)
        {
            _upgradePanel = CreatePanel(root, "UpgradePanel", new Color(0.02f, 0.03f, 0.07f, 0.85f));
            var header = CreateText(_upgradePanel.transform, "WAVE CLEARED — SELECT CORE UPGRADE", 36, new Vector2(0f, 230f), new Vector2(900f, 60f), TextAnchor.MiddleCenter, new Color(0.25f, 0.95f, 1f));
            Center(header.rectTransform);

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                float x = (i - 1) * 340f;
                var card = CreateBox(_upgradePanel.transform, $"UpgradeCard_{i}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -20f), new Vector2(300f, 320f), new Color(0.06f, 0.09f, 0.16f, 0.95f));

                var accentGo = new GameObject("Accent");
                accentGo.transform.SetParent(card.transform, false);
                var accentImg = accentGo.AddComponent<Image>();
                accentImg.color = Color.cyan;
                var accentRt = accentImg.rectTransform;
                accentRt.anchorMin = new Vector2(0f, 1f);
                accentRt.anchorMax = new Vector2(1f, 1f);
                accentRt.pivot = new Vector2(0.5f, 1f);
                accentRt.sizeDelta = new Vector2(0f, 10f);
                _upgradeAccents.Add(accentImg);

                var btn = card.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectUpgrade(idx));
                _upgradeButtons.Add(btn);

                var t = CreateText(card.transform, "UPGRADE", 24, new Vector2(0f, 65f), new Vector2(260f, 70f), TextAnchor.MiddleCenter, Color.white);
                Center(t.rectTransform);
                _upgradeTitles.Add(t);

                var d = CreateText(card.transform, "Description", 19, new Vector2(0f, -45f), new Vector2(260f, 140f), TextAnchor.MiddleCenter, new Color(0.8f, 0.88f, 0.95f));
                Center(d.rectTransform);
                _upgradeDescs.Add(d);
            }
        }

        private void BuildPausePanel(Transform root)
        {
            _pausePanel = CreatePanel(root, "PausePanel", new Color(0.02f, 0.03f, 0.07f, 0.8f));
            var title = CreateText(_pausePanel.transform, "PAUSED", 54, new Vector2(0f, 100f), new Vector2(500f, 70f), TextAnchor.MiddleCenter, Color.white);
            Center(title.rectTransform);

            CreateButton(_pausePanel.transform, "RESUME", new Vector2(0f, -10f), new Vector2(260f, 56f), new Color(0.2f, 0.9f, 1f), () =>
            {
                GameManager.Instance.SetState(GameState.Playing);
            });
            CreateButton(_pausePanel.transform, "RESTART RUN", new Vector2(0f, -85f), new Vector2(260f, 56f), new Color(1f, 0.45f, 0.25f), () =>
            {
                GameManager.Instance.StartNewRun();
            });
        }

        private void BuildGameOverPanel(Transform root)
        {
            _gameOverPanel = CreatePanel(root, "GameOverPanel", new Color(0.05f, 0.01f, 0.03f, 0.88f));
            var title = CreateText(_gameOverPanel.transform, "SYSTEM TERMINATED", 56, new Vector2(0f, 175f), new Vector2(800f, 80f), TextAnchor.MiddleCenter, new Color(1f, 0.25f, 0.4f));
            Center(title.rectTransform);

            _gameOverStatsText = CreateText(_gameOverPanel.transform, "", 26, new Vector2(0f, 20f), new Vector2(600f, 200f), TextAnchor.MiddleCenter, Color.white);
            Center(_gameOverStatsText.rectTransform);

            CreateButton(_gameOverPanel.transform, "RETRY  [R]", new Vector2(0f, -145f), new Vector2(280f, 60f), new Color(0.2f, 0.95f, 1f), () =>
            {
                GameManager.Instance.StartNewRun();
            });
        }

        // UI Factory Helpers
        private GameObject CreatePanel(Transform parent, string name, Color bg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bg;
            img.raycastTarget = bg.a > 0.01f;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            return go;
        }

        private GameObject CreateBox(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color bg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var rt = img.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            if (anchorMin == new Vector2(0.5f, 0.5f)) rt.pivot = new Vector2(0.5f, 0.5f);
            if (anchorMin == new Vector2(0.5f, 1f)) rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private Text CreateText(Transform parent, string content, int size, Vector2 pos, Vector2 boxSize, TextAnchor align, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.text = content;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.fontStyle = FontStyle.Bold;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = boxSize;
            return t;
        }

        private Image CreateBar(Transform parent, Vector2 pos, Vector2 size, Color fillColor)
        {
            var bgGo = new GameObject("BarBg");
            bgGo.transform.SetParent(parent, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.16f, 0.24f, 0.9f);
            var bgRt = bgImg.rectTransform;
            bgRt.anchorMin = new Vector2(0f, 1f);
            bgRt.anchorMax = new Vector2(0f, 1f);
            bgRt.pivot = new Vector2(0f, 1f);
            bgRt.anchoredPosition = pos;
            bgRt.sizeDelta = size;

            var fillGo = new GameObject("BarFill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.sprite = _whiteSprite;
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            var fillRt = fillImg.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            return fillImg;
        }

        private void CreateButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.95f);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(2f, -2f);

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var txt = CreateText(go.transform, label, 24, Vector2.zero, size, TextAnchor.MiddleCenter, Color.white);
            Center(txt.rectTransform);
        }

        private static void Center(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void CenterTop(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
        }

        private static void AnchorRight(RectTransform rt)
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
        }
    }
}
