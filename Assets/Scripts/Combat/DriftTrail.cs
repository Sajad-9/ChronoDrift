using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using ChronoDrift.Core;

namespace ChronoDrift.Combat
{
    /// <summary>
    /// DriftTrail — emits pooled, zero-allocation afterimage ghost clones using
    /// a shared material and MaterialPropertyBlock.
    /// </summary>
    public class DriftTrail : MonoBehaviour
    {
        private class Ghost
        {
            public GameObject Go;
            public Renderer Rend;
            public float Life;
            public float MaxLife;
            public bool Active;
        }

        private readonly List<Ghost> _pool = new List<Ghost>();
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _mpb;
        private Material _sharedMat;
        private Mesh _sharedMesh;

        private float _spawnInterval = 0.075f;
        private float _ghostLifetime = 0.34f;
        private Color _color = Color.cyan;
        private float _timer;
        private bool _enabled = true;
        private const int MaxPoolSize = 8;

        public static DriftTrail Attach(Transform target, Color color,
            float spawnInterval = 0.075f, float ghostLifetime = 0.34f)
        {
            var trail = target.gameObject.AddComponent<DriftTrail>();
            trail._color = color;
            trail._spawnInterval = spawnInterval;
            trail._ghostLifetime = ghostLifetime;
            trail._mpb = new MaterialPropertyBlock();
            trail._sharedMat = MaterialLibrary.GetGhostMaterial(color);

            var mf = target.GetComponentInChildren<MeshFilter>();
            trail._sharedMesh = mf != null ? mf.sharedMesh : null;
            return trail;
        }

        private void Update()
        {
            if (!_enabled) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _spawnInterval;
                SpawnGhost();
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                var g = _pool[i];
                if (!g.Active) continue;

                g.Life -= Time.deltaTime;
                if (g.Life <= 0f)
                {
                    g.Active = false;
                    g.Go.SetActive(false);
                    continue;
                }

                float alpha = g.Life / g.MaxLife;
                Color c = _color;
                c.a = alpha * 0.42f;
                _mpb.SetColor(ColorId, c);
                g.Rend.SetPropertyBlock(_mpb);
                g.Go.transform.localScale = transform.lossyScale * (0.9f + alpha * 0.1f);
            }
        }

        private void SpawnGhost()
        {
            if (_sharedMesh == null) return;

            Ghost slot = null;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].Active)
                {
                    slot = _pool[i];
                    break;
                }
            }

            if (slot == null)
            {
                if (_pool.Count >= MaxPoolSize) return;
                var go = new GameObject("DriftGhost");
                var mfr = go.AddComponent<MeshFilter>();
                mfr.sharedMesh = _sharedMesh;
                var rend = go.AddComponent<MeshRenderer>();
                rend.sharedMaterial = _sharedMat;
                rend.shadowCastingMode = ShadowCastingMode.Off;
                rend.receiveShadows = false;

                slot = new Ghost { Go = go, Rend = rend };
                _pool.Add(slot);
            }

            slot.Go.transform.SetPositionAndRotation(transform.position, transform.rotation);
            slot.Go.transform.localScale = transform.lossyScale;
            slot.Life = _ghostLifetime;
            slot.MaxLife = _ghostLifetime;
            slot.Active = true;
            slot.Go.SetActive(true);
        }

        public void SetEnabled(bool val) => _enabled = val;

        private void OnDestroy()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].Go != null) Destroy(_pool[i].Go);
            }
            _pool.Clear();
        }
    }
}
