using System.Collections;
using UnityEngine;
using ChronoDrift.Core;

namespace ChronoDrift.Combat
{
    /// <summary>
    /// Full-screen screen effects component: damage vignette pulse,
    /// chromatic aberration simulation via multiple camera overlay quads,
    /// and a "time warp" distortion ring when Chrono Focus activates.
    /// All procedural — zero external assets or post-process packages.
    /// Attach to Main Camera via ChronoDriftSetup.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ScreenEffects : MonoBehaviour
    {
        public static ScreenEffects Instance { get; private set; }

        // Vignette
        private float _vignettePulse    = 0f;
        private float _vignetteDecay    = 3.5f;
        private Texture2D _vignetteTexture;
        private Material  _vignetteMat;

        // Chromatic aberration (simulated by scaling a colour-tinted overlay)
        private float _chromaStrength   = 0f;
        private float _chromaDecay      = 5f;

        // HitStop flash
        private float _flashAlpha       = 0f;
        private Color _flashColor       = Color.white;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            BuildVignetteTexture();
        }

        // ================================================================
        private void BuildVignetteTexture()
        {
            int size = 256;
            _vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _vignetteTexture.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2(0.5f, 0.5f);

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float dist = Vector2.Distance(new Vector2(u, v), center);
                    // Vignette shape: dark border, transparent center
                    float alpha = Mathf.Clamp01((dist - 0.3f) / 0.4f);
                    alpha = Mathf.Pow(alpha, 1.6f);
                    pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
                }
            }
            _vignetteTexture.SetPixels(pixels);
            _vignetteTexture.Apply();

            Shader s = Shader.Find("UI/Default");
            if (s == null) s = Shader.Find("Sprites/Default");
            _vignetteMat = new Material(s);
            _vignetteMat.mainTexture = _vignetteTexture;
        }

        // ================================================================
        private void Update()
        {
            _vignettePulse = Mathf.Max(0f, _vignettePulse - _vignetteDecay * Time.unscaledDeltaTime);
            _chromaStrength = Mathf.Max(0f, _chromaStrength - _chromaDecay * Time.unscaledDeltaTime);
            _flashAlpha    = Mathf.Max(0f, _flashAlpha - 6f * Time.unscaledDeltaTime);
        }

        // ================================================================
        //  OnGUI draws overlays after camera render
        // ================================================================
        private void OnGUI()
        {
            if (_flashAlpha > 0.01f)
            {
                Color fc = _flashColor;
                fc.a = _flashAlpha;
                GUI.color = fc;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            if (_vignettePulse > 0.01f)
            {
                Color vc = new Color(1f, 0.1f, 0.1f, _vignettePulse * 0.85f);
                GUI.color = vc;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _vignetteTexture);
                GUI.color = Color.white;
            }

            if (_chromaStrength > 0.01f)
            {
                // Simulate chromatic aberration: draw 3 semi-transparent overlays slightly offset
                float o = _chromaStrength * 12f;
                DrawChromaLayer(new Color(1f, 0f, 0f, _chromaStrength * 0.09f), -o, 0);
                DrawChromaLayer(new Color(0f, 1f, 0f, _chromaStrength * 0.06f), 0, o);
                DrawChromaLayer(new Color(0f, 0.4f, 1f, _chromaStrength * 0.09f), o, -o);
            }
        }

        private static void DrawChromaLayer(Color c, float dx, float dy)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(dx, dy, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // ================================================================
        //  Public trigger methods
        // ================================================================

        public void TriggerDamageVignette(float intensity = 1f)
        {
            _vignettePulse  = Mathf.Min(1f, _vignettePulse + intensity);
            _chromaStrength = Mathf.Min(1f, _chromaStrength + intensity * 0.8f);
        }

        public void TriggerFlash(Color color, float alpha = 0.35f)
        {
            _flashColor = color;
            _flashAlpha = Mathf.Max(_flashAlpha, alpha);
        }

        public void TriggerBlackHoleVignette()
        {
            _vignettePulse  = Mathf.Min(1f, _vignettePulse + 0.6f);
            _vignetteDecay  = 1.2f;
        }
    }
}
