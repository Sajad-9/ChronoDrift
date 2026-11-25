using System.Collections.Generic;
using UnityEngine;
using ChronoDrift.Core;

namespace ChronoDrift.Combat
{
    /// <summary>
    /// DriftTrail — attaches to any enemy or the player and emits stylized
    /// afterimage ghost clones that fade out. Creates a "speed echo" visual
    /// without any additional textures or particle systems.
    /// Call DriftTrail.Attach(transform, color) to add it.
    /// </summary>
    public class DriftTrail : MonoBehaviour
    {
        private struct Ghost
        {
            public GameObject Go;
            public Renderer   Rend;
            public float      Life;
            public float      MaxLife;
        }

        private readonly List<Ghost> _ghosts = new List<Ghost>();

        private float _spawnInterval  = 0.065f;
        private float _ghostLifetime  = 0.38f;
        private Color _color          = Color.cyan;
        private float _timer          = 0f;
        private Renderer _sourceRenderer;
        private bool _enabled         = true;

        // ================================================================
        public static DriftTrail Attach(Transform target, Color color,
            float spawnInterval = 0.065f, float ghostLifetime = 0.38f)
        {
            var trail = target.gameObject.AddComponent<DriftTrail>();
            trail._color         = color;
            trail._spawnInterval = spawnInterval;
            trail._ghostLifetime = ghostLifetime;
            trail._sourceRenderer = target.GetComponentInChildren<Renderer>();
            return trail;
        }

        // ================================================================
        private void Update()
        {
            if (!_enabled) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _spawnInterval;
                SpawnGhost();
            }

            // Age and fade ghosts
            for (int i = _ghosts.Count - 1; i >= 0; i--)
            {
                var g = _ghosts[i];
                g.Life -= Time.deltaTime;
                if (g.Life <= 0f)
                {
                    Destroy(g.Go);
                    _ghosts.RemoveAt(i);
                    continue;
                }

                float alpha = g.Life / g.MaxLife;
                if (g.Rend != null)
                {
                    Color c = _color;
                    c.a = alpha * 0.45f;
                    g.Rend.material.color = c;

                    // shrink slightly as it fades
                    g.Go.transform.localScale = Vector3.one * (0.9f + alpha * 0.1f);
                }

                _ghosts[i] = g;
            }
        }

        private void SpawnGhost()
        {
            Mesh mesh = null;
            if (_sourceRenderer is MeshRenderer mr)
            {
                var mf = _sourceRenderer.GetComponent<MeshFilter>();
                if (mf) mesh = mf.sharedMesh;
            }

            if (mesh == null) mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            if (mesh == null) return;

            var go = new GameObject("DriftGhost");
            go.transform.position = transform.position;
            go.transform.rotation = transform.rotation;
            go.transform.localScale = transform.lossyScale;

            var mfr = go.AddComponent<MeshFilter>();
            mfr.sharedMesh = mesh;

            var rend = go.AddComponent<MeshRenderer>();
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Standard");
            var mat = new Material(s);
            Color c = _color;
            c.a = 0.4f;
            mat.color = c;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            rend.material = mat;

            _ghosts.Add(new Ghost { Go = go, Rend = rend, Life = _ghostLifetime, MaxLife = _ghostLifetime });
        }

        public void SetEnabled(bool val) => _enabled = val;

        private void OnDestroy()
        {
            foreach (var g in _ghosts)
            {
                if (g.Go) Destroy(g.Go);
            }
        }
    }
}
