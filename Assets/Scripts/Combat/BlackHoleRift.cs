using System.Collections.Generic;
using UnityEngine;
using ChronoDrift.Audio;
using ChronoDrift.Core;
using ChronoDrift.Enemies;

namespace ChronoDrift.Combat
{
    /// <summary>
    /// Black Hole Rift (Q) – ultimate ability.
    /// Grows from a pinpoint, pulling everything into it, then collapses
    /// in a devastating singularity nova.  Zero external assets required.
    /// </summary>
    public class BlackHoleRift : MonoBehaviour
    {
        // --- tunables ---
        private float _pullRadius       = 14f;
        private float _maxScale         = 3.2f;
        private float _growDuration     = 2.8f;
        private float _collapseDuration = 0.55f;
        private float _baseDamage       = 220f;

        private enum Phase { Growing, Collapsing }
        private Phase  _phase = Phase.Growing;
        private float  _elapsed;

        // visual rings
        private readonly List<Transform> _ringObjects = new List<Transform>();
        private Renderer _coreRenderer;
        private float _rotAngle;

        // ----------------------------------------------------------------
        public static BlackHoleRift Spawn(Vector3 position, float bonusDamage = 0f, float bonusRadius = 0f)
        {
            var go = new GameObject("BlackHoleRift");
            go.transform.position = new Vector3(position.x, 0.6f, position.z);

            var bhr = go.AddComponent<BlackHoleRift>();
            bhr._baseDamage  += bonusDamage;
            bhr._pullRadius  += bonusRadius;
            bhr.BuildVisuals();

            CameraController.Instance?.AddFovKick(8f);
            ProceduralAudio.Instance?.PlayBlackHole();
            return bhr;
        }

        // ----------------------------------------------------------------
        private void BuildVisuals()
        {
            // Dark pulsing core sphere
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "BHCore";
            core.transform.SetParent(transform, false);
            core.transform.localScale = Vector3.one * 0.3f;
            var coreCol = core.GetComponent<Collider>();
            if (coreCol) Destroy(coreCol);
            _coreRenderer = core.GetComponent<Renderer>();
            _coreRenderer.material = MaterialLibrary.GetEmissive(
                "BHCoreMat",
                new Color(0.02f, 0f, 0.08f),
                new Color(0.4f, 0.1f, 1f),
                4.5f);

            // Accretion disc rings
            Color[] ringColors =
            {
                new Color(0.6f, 0.1f, 1f),
                new Color(0.9f, 0.3f, 1f),
                new Color(1f,   0.6f, 1f),
            };
            float[] ringRadii  = { 1.1f, 1.9f, 2.9f };
            float[] ringTilts  = { 0f, 25f, -18f };

            for (int i = 0; i < 3; i++)
            {
                var ring = new GameObject($"BHRing_{i}");
                ring.transform.SetParent(transform, false);
                ring.transform.localScale  = Vector3.zero;
                ring.transform.localEulerAngles = new Vector3(ringTilts[i], 0f, 0f);

                var lr = ring.AddComponent<LineRenderer>();
                lr.loop           = true;
                lr.useWorldSpace  = false;
                lr.material       = MaterialLibrary.GetGhostMaterial(ringColors[i]);
                lr.startWidth     = 0.14f + i * 0.05f;
                lr.endWidth       = lr.startWidth;
                lr.startColor     = ringColors[i];
                lr.endColor       = ringColors[i];

                int segs = 48 + i * 12;
                lr.positionCount = segs;
                float r = ringRadii[i];
                for (int s = 0; s < segs; s++)
                {
                    float angle = s * Mathf.PI * 2f / segs;
                    lr.SetPosition(s, new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r));
                }

                _ringObjects.Add(ring.transform);
            }
        }

        // ----------------------------------------------------------------
        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_phase == Phase.Growing)
                UpdateGrowing();
            else
                UpdateCollapsing();
        }

        // ----------------------------------------------------------------
        private void UpdateGrowing()
        {
            float t = Mathf.Clamp01(_elapsed / _growDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);   // ease-out cubic

            // Scale the rift object
            float s = Mathf.Lerp(0.05f, _maxScale, eased);
            transform.localScale = Vector3.one * s;

            // Animate rings
            _rotAngle += Time.deltaTime * (120f + t * 180f);
            for (int i = 0; i < _ringObjects.Count; i++)
            {
                float show = Mathf.Clamp01(t * 3f - i * 0.3f);
                _ringObjects[i].localScale = Vector3.one * show;
                _ringObjects[i].localEulerAngles = new Vector3(
                    _ringObjects[i].localEulerAngles.x,
                    _rotAngle * (i % 2 == 0 ? 1f : -1f),
                    0f);
            }

            // Pulse core
            float pulse = 1f + Mathf.Sin(_elapsed * 22f) * 0.18f;
            if (_coreRenderer) _coreRenderer.transform.localScale = Vector3.one * (0.5f + t * 1.2f) * pulse;

            // Pull enemies every frame
            float pullForce = Mathf.Lerp(10f, 28f, t);
            Collider[] hits = Physics.OverlapSphere(transform.position, _pullRadius * eased);
            foreach (var c in hits)
            {
                var enemy = c.GetComponentInParent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    Vector3 pull = (transform.position - enemy.transform.position);
                    pull.y = 0f;
                    float dist = Mathf.Max(0.5f, pull.magnitude);
                    enemy.ApplyExternalPull(pull.normalized * (pullForce / dist * Time.deltaTime));
                }

                // Swallow enemy bullets
                var proj = c.GetComponent<Projectile>();
                if (proj != null && !proj.isPlayerProjectile)
                {
                    VfxHelper.SpawnBurst(proj.transform.position, new Color(0.6f, 0.1f, 1f), 4, 4f, 0.1f);
                    Destroy(proj.gameObject);
                }
            }

            // Camera trauma scales with size
            if (t > 0.1f)
                CameraController.Instance?.AddTrauma(0.012f * t);

            if (_elapsed >= _growDuration)
            {
                _elapsed = 0f;
                _phase   = Phase.Collapsing;
                CameraController.Instance?.AddFovKick(-12f);
                CameraController.Instance?.AddTrauma(0.9f);
            }
        }

        // ----------------------------------------------------------------
        private void UpdateCollapsing()
        {
            float t = Mathf.Clamp01(_elapsed / _collapseDuration);

            // Implode then explode
            float scale = _phase == Phase.Collapsing
                ? Mathf.Lerp(_maxScale, 0.05f, t)
                : 0f;
            transform.localScale = Vector3.one * scale;

            if (_elapsed >= _collapseDuration)
            {
                Detonate();
            }
        }

        // ----------------------------------------------------------------
        private void Detonate()
        {
            Color violet = new Color(0.7f, 0.2f, 1f);
            VfxHelper.SpawnBurst(transform.position, violet, 60, 20f, 0.4f);
            VfxHelper.SpawnShockwaveRing(transform.position, violet, _pullRadius * 1.3f, 0.6f);
            VfxHelper.SpawnShockwaveRing(transform.position, Color.white, _pullRadius * 0.7f, 0.35f);

            CameraController.Instance?.AddTrauma(1.1f);
            CameraController.Instance?.AddFovKick(14f);
            TimeDilationManager.Instance?.TriggerHitStop(0.11f);
            ProceduralAudio.Instance?.PlayExplosion();

            // Massive AOE damage + extreme knockback
            Collider[] hits = Physics.OverlapSphere(transform.position, _pullRadius * 1.1f);
            foreach (var c in hits)
            {
                var enemy = c.GetComponentInParent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    Vector3 push = (enemy.transform.position - transform.position).normalized * 22f;
                    float dist   = Vector3.Distance(transform.position, enemy.transform.position);
                    float dmg    = _baseDamage * Mathf.Clamp01(1f - dist / (_pullRadius * 1.1f) * 0.4f);
                    enemy.TakeDamage(dmg, push, true);
                }
            }

            Destroy(gameObject);
        }
    }
}
