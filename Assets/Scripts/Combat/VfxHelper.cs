using System.Collections;
using UnityEngine;
using ChronoDrift.Core;

namespace ChronoDrift.Combat
{
    /// <summary>
    /// Procedural VFX spawner: particle bursts, expanding shockwave rings, and chain lightning arcs.
    /// </summary>
    public static class VfxHelper
    {
        private static readonly AnimationCurve SizeFadeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f)
        );

        public static void SpawnBurst(Vector3 pos, Color color, int count = 16, float speed = 9f, float size = 0.22f)
        {
            var go = new GameObject("VfxBurst");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, SizeFadeCurve);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = MaterialLibrary.GetParticleMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Object.Destroy(go, 1.2f);
        }

        public static void SpawnShockwaveRing(Vector3 pos, Color color, float maxRadius = 6f, float duration = 0.35f)
        {
            var go = new GameObject("ShockwaveRing");
            go.transform.position = new Vector3(pos.x, 0.2f, pos.z);
            var ring = go.AddComponent<ShockwaveRingBehaviour>();
            ring.Init(color, maxRadius, duration);
        }

        public static void SpawnLightningArc(Vector3 from, Vector3 to, Color color)
        {
            var go = new GameObject("LightningArc");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = MaterialLibrary.GetGhostMaterial(color);
            lr.startColor = color;
            lr.endColor = Color.white;
            lr.startWidth = 0.16f;
            lr.endWidth = 0.08f;
            lr.useWorldSpace = true;

            int segments = 7;
            lr.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                Vector3 p = Vector3.Lerp(from, to, t);
                if (i > 0 && i < segments - 1)
                {
                    p += new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(-0.2f, 0.3f), Random.Range(-0.7f, 0.7f));
                }
                lr.SetPosition(i, p);
            }

            Object.Destroy(go, 0.14f);
        }
    }

    public class ShockwaveRingBehaviour : MonoBehaviour
    {
        private LineRenderer _lr;
        private Color _color;
        private float _maxRadius;
        private float _duration;
        private float _elapsed;
        private const int Segments = 36;

        public void Init(Color color, float maxRadius, float duration)
        {
            _color = color;
            _maxRadius = maxRadius;
            _duration = duration;

            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.loop = true;
            _lr.useWorldSpace = false;
            _lr.positionCount = Segments;
            _lr.material = MaterialLibrary.GetGhostMaterial(color);
            _lr.startWidth = 0.22f;
            _lr.endWidth = 0.22f;
            UpdateRing(0.3f, 1f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            float radius = Mathf.Lerp(0.4f, _maxRadius, 1f - Mathf.Pow(1f - t, 3f));
            float alpha = 1f - t;
            UpdateRing(radius, alpha);
        }

        private void UpdateRing(float radius, float alpha)
        {
            Color c = _color;
            c.a = alpha;
            _lr.startColor = c;
            _lr.endColor = c;
            _lr.startWidth = 0.25f * alpha + 0.04f;
            _lr.endWidth = _lr.startWidth;

            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                _lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }
    }
}
