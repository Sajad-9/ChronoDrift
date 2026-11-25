using UnityEngine;
using System.Collections.Generic;

namespace ChronoDrift.Core
{
    /// <summary>
    /// Creates and caches neon emissive materials and procedural grid textures
    /// compatible with Unity's Built-in Render Pipeline.
    /// </summary>
    public static class MaterialLibrary
    {
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static Texture2D _gridTexture;

        public static Material GetEmissive(string key, Color baseColor, Color emissionColor, float emissionIntensity = 1.8f)
        {
            if (Materials.TryGetValue(key, out var mat) && mat != null) return mat;

            Shader shader = Shader.Find("Standard");
            mat = new Material(shader) { name = key };
            mat.color = baseColor;
            mat.SetFloat("_Metallic", 0.35f);
            mat.SetFloat("_Glossiness", 0.85f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            Materials[key] = mat;
            return mat;
        }

        public static Material GetGhostMaterial(Color color)
        {
            string key = $"Ghost_{ColorUtility.ToHtmlStringRGBA(color)}";
            if (Materials.TryGetValue(key, out var mat) && mat != null) return mat;

            Shader shader = Shader.Find("Sprites/Default");
            mat = new Material(shader) { name = key };
            mat.color = color;
            Materials[key] = mat;
            return mat;
        }

        public static Material GetParticleMaterial()
        {
            if (Materials.TryGetValue("Particle", out var mat) && mat != null) return mat;

            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            mat = new Material(shader) { name = "ParticleAdditive" };
            mat.SetColor("_Color", Color.white);
            Materials["Particle"] = mat;
            return mat;
        }

        public static Material GetArenaFloorMaterial()
        {
            if (Materials.TryGetValue("ArenaFloor", out var mat) && mat != null) return mat;

            if (_gridTexture == null)
            {
                _gridTexture = CreateGridTexture(256, new Color(0.03f, 0.04f, 0.08f), new Color(0.08f, 0.28f, 0.45f));
            }

            Shader shader = Shader.Find("Standard");
            mat = new Material(shader) { name = "ArenaFloorMat" };
            mat.mainTexture = _gridTexture;
            mat.mainTextureScale = new Vector2(22f, 22f);
            mat.SetFloat("_Metallic", 0.25f);
            mat.SetFloat("_Glossiness", 0.6f);
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", _gridTexture);
            mat.SetColor("_EmissionColor", new Color(0.15f, 0.45f, 0.7f) * 0.45f);
            Materials["ArenaFloor"] = mat;
            return mat;
        }

        private static Texture2D CreateGridTexture(int size, Color bg, Color line)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 8;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool major = x < 4 || y < 4 || x >= size - 4 || y >= size - 4;
                    bool minor = (x % 64 < 2) || (y % 64 < 2);
                    if (major)
                        pixels[y * size + x] = line;
                    else if (minor)
                        pixels[y * size + x] = Color.Lerp(bg, line, 0.3f);
                    else
                        pixels[y * size + x] = bg;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
