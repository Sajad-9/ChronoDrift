using System.Collections.Generic;
using UnityEngine;
using ChronoDrift.Core;
using ChronoDrift.Player;

namespace ChronoDrift.Combat
{
    /// <summary>
    /// Achievement System: triggers popup banners for in-game feats.
    /// Tracks: First Blood, Combo Milestones, Untouchable wave, Triple Kill,
    /// Ricochet Master, Singularity Swallowed, Black Hole Activated, Boss Slayer,
    /// Speed Demon (wave clear time), and more.
    /// Zero assets — builds its own UI canvas at runtime.
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        public static AchievementSystem Instance { get; private set; }

        // --- tracking state ---
        private bool _firstKill      = false;
        private bool _untouchable    = false;    // cleared per-wave
        private int  _waveKillCount  = 0;
        private float _waveStartTime = 0f;
        private bool _bossSlayer     = false;
        private bool _ricochetMaster = false;
        private int  _singularitySwallows = 0;
        private int  _blackHolesThisRun   = 0;
        private bool _combo8Done          = false;
        private bool _combo16Done         = false;
        private bool _tripleKillWindow    = false;
        private int  _tripleKillCount     = 0;
        private float _tripleKillTimer    = 0f;

        // --- popup queue ---
        private readonly Queue<AchievementData> _queue = new Queue<AchievementData>();
        private bool   _showing  = false;
        private float  _showTimer = 0f;
        private const float ShowDuration = 2.4f;

        // --- UI ---
        private GameObject _canvas;
        private UnityEngine.UI.Text _titleText;
        private UnityEngine.UI.Text _descText;
        private UnityEngine.UI.Image _bgImage;
        private float _slideInY;
        private float _currentY;
        private bool  _slidingIn;

        private struct AchievementData
        {
            public string Title;
            public string Desc;
            public Color  Accent;
        }

        // ================================================================
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildCanvas();
        }

        // ================================================================
        private void BuildCanvas()
        {
            _canvas = new GameObject("AchievementCanvas");
            var canvasComp = _canvas.AddComponent<Canvas>();
            canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComp.sortingOrder = 100;
            _canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            _canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Banner panel
            var panel = new GameObject("AchievementBanner");
            panel.transform.SetParent(_canvas.transform, false);

            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(480f, 72f);
            rt.anchoredPosition = new Vector2(0f, 0f);

            _bgImage = panel.AddComponent<UnityEngine.UI.Image>();
            Sprite white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            _bgImage.sprite = white;
            _bgImage.color = new Color(0.04f, 0.06f, 0.14f, 0.92f);
            _bgImage.enabled = false;

            // Trophy icon label
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(panel.transform, false);
            var iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0f);
            iconRt.anchorMax = new Vector2(0f, 1f);
            iconRt.pivot     = new Vector2(0f, 0.5f);
            iconRt.offsetMin = new Vector2(10f, 6f);
            iconRt.offsetMax = new Vector2(60f, -6f);
            var iconText = iconObj.AddComponent<UnityEngine.UI.Text>();
            iconText.text      = "🏆";
            iconText.fontSize  = 28;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.color     = Color.white;

            // Title text
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(68f, 0f);
            titleRt.offsetMax = new Vector2(-10f, -2f);
            _titleText = titleObj.AddComponent<UnityEngine.UI.Text>();
            _titleText.fontSize  = 17;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.LowerLeft;
            _titleText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _titleText.color     = Color.white;

            // Desc text
            var descObj = new GameObject("Desc");
            descObj.transform.SetParent(panel.transform, false);
            var descRt = descObj.AddComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 0.5f);
            descRt.offsetMin = new Vector2(68f, 4f);
            descRt.offsetMax = new Vector2(-10f, 0f);
            _descText = descObj.AddComponent<UnityEngine.UI.Text>();
            _descText.fontSize  = 13;
            _descText.alignment = TextAnchor.UpperLeft;
            _descText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _descText.color     = new Color(0.8f, 0.85f, 1f);

            _slideInY  = 0f;
            _currentY  = -80f;
            SetBannerY(_currentY);
            _bgImage.enabled  = false;
            _canvas.SetActive(true);
        }

        // ================================================================
        private void Update()
        {
            if (_tripleKillWindow)
            {
                _tripleKillTimer -= Time.unscaledDeltaTime;
                if (_tripleKillTimer <= 0f) { _tripleKillWindow = false; _tripleKillCount = 0; }
            }

            if (_showing)
            {
                _showTimer -= Time.unscaledDeltaTime;

                // Slide in
                if (_slidingIn)
                {
                    _currentY = Mathf.Lerp(_currentY, _slideInY, Time.unscaledDeltaTime * 14f);
                    SetBannerY(_currentY);
                    if (Mathf.Abs(_currentY - _slideInY) < 0.5f) _slidingIn = false;
                }

                // Slide out when done
                if (_showTimer <= 0f)
                {
                    _currentY = Mathf.Lerp(_currentY, -80f, Time.unscaledDeltaTime * 14f);
                    SetBannerY(_currentY);
                    if (_currentY < -75f)
                    {
                        _showing = false;
                        _bgImage.enabled = false;
                        if (_queue.Count > 0) ShowNext();
                    }
                }
            }
            else if (_queue.Count > 0)
            {
                ShowNext();
            }
        }

        private void SetBannerY(float y)
        {
            var panel = _canvas.transform.GetChild(0) as RectTransform;
            if (panel) panel.anchoredPosition = new Vector2(0f, y);
        }

        private void ShowNext()
        {
            var data = _queue.Dequeue();
            _titleText.text  = data.Title;
            _descText.text   = data.Desc;
            _titleText.color = data.Accent;
            _bgImage.enabled = true;
            _currentY   = -80f;
            _slideInY   = -4f;
            _slidingIn  = true;
            _showTimer  = ShowDuration;
            _showing    = true;
        }

        private void Unlock(string title, string desc, Color accent)
        {
            _queue.Enqueue(new AchievementData { Title = title, Desc = desc, Accent = accent });
        }

        // ================================================================
        //  Public notification hooks — called by game systems
        // ================================================================

        public void OnKill()
        {
            if (!_firstKill) { _firstKill = true; Unlock("FIRST BLOOD", "The arena runs hot.", new Color(1f, 0.3f, 0.3f)); }

            _waveKillCount++;

            // Triple Kill
            if (!_tripleKillWindow) { _tripleKillWindow = true; _tripleKillCount = 1; _tripleKillTimer = 2.5f; }
            else
            {
                _tripleKillCount++;
                if (_tripleKillCount == 3) Unlock("TRIPLE KILL", "Three in 2.5 seconds.", new Color(1f, 0.65f, 0.1f));
                if (_tripleKillCount == 5) Unlock("KILLING SPREE", "Five fast kills. Ruthless.", new Color(1f, 0.4f, 0.1f));
            }

            // Combo milestones
            int combo = GameManager.Instance != null ? GameManager.Instance.Combo : 1;
            if (!_combo8Done && combo >= 8)
            {
                _combo8Done = true;
                Unlock("COMBO × 8", "Momentum is power.", new Color(0.3f, 1f, 0.7f));
            }
            if (!_combo16Done && combo >= 16)
            {
                _combo16Done = true;
                Unlock("COMBO × 16  — MAX!", "Absolute peak performance.", new Color(0.1f, 1f, 0.5f));
            }
        }

        public void OnWaveStart(int wave)
        {
            _untouchable   = true;
            _waveKillCount = 0;
            _waveStartTime = Time.time;
        }

        public void OnPlayerHit()
        {
            _untouchable = false;
        }

        public void OnWaveComplete(int wave)
        {
            if (_untouchable && wave > 1)
                Unlock("UNTOUCHABLE", $"Wave {wave} cleared without taking damage.", new Color(0.2f, 0.9f, 1f));

            float elapsed = Time.time - _waveStartTime;
            if (elapsed < 18f && wave > 2)
                Unlock("SPEED DEMON", $"Wave {wave} cleared in {elapsed:0.0}s!", new Color(1f, 0.8f, 0.1f));
        }

        public void OnBossKilled()
        {
            if (!_bossSlayer) { _bossSlayer = true; Unlock("BOSS SLAYER", "The Hex-Titan Overlord falls.", new Color(1f, 0.4f, 0.9f)); }
        }

        public void OnRicochetKill()
        {
            if (!_ricochetMaster) { _ricochetMaster = true; Unlock("RICOCHET MASTER", "Wall-bounced bolt scores a kill.", new Color(1f, 0.85f, 0.2f)); }
        }

        public void OnSingularitySwallow(int count)
        {
            _singularitySwallows += count;
            if (_singularitySwallows >= 10 && !_firstKill /* reuse a flag: */ )
                Unlock("VACUUM", "10 enemy bolts swallowed.", new Color(0.75f, 0.35f, 1f));
        }

        public void OnBlackHoleActivated()
        {
            _blackHolesThisRun++;
            if (_blackHolesThisRun == 1)
                Unlock("EVENT HORIZON", "Black Hole Rift deployed.", new Color(0.55f, 0.1f, 1f));
            if (_blackHolesThisRun >= 5)
                Unlock("SPACE IS MINE", "5 Black Hole Rifts this run.", new Color(0.4f, 0.05f, 0.9f));
        }

        public void OnRunEnd()
        {
            // Reset per-run flags
            _firstKill    = false;
            _bossSlayer   = false;
            _ricochetMaster = false;
            _combo8Done   = false;
            _combo16Done  = false;
            _singularitySwallows = 0;
            _blackHolesThisRun   = 0;
        }
    }

    // Lightweight alias so GameManager can call it without namespace confusion
    public static class Achievements
    {
        public static AchievementSystem I => AchievementSystem.Instance;
    }
}
