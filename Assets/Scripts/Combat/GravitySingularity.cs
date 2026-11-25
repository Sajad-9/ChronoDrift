using UnityEngine;
using ChronoDrift.Audio;
using ChronoDrift.Core;
using ChronoDrift.Enemies;

namespace ChronoDrift.Combat
{
    /// <summary>
    /// Gravity Singularity orb (RMB): pulls enemies and destroys enemy projectiles, then detonates.
    /// </summary>
    public class GravitySingularity : MonoBehaviour
    {
        private float _duration = 1.65f;
        private float _pullRadius = 8.5f;
        private float _explosionDamage = 65f;
        private float _elapsed;
        private Vector3 _travelDir;
        private float _travelSpeed = 12f;

        public static GravitySingularity Spawn(Vector3 position, Vector3 direction, float bonusRadius = 0f, float bonusDamage = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "GravitySingularity";
            go.transform.position = new Vector3(position.x, 0.8f, position.z);
            go.transform.localScale = Vector3.one * 0.95f;

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Color violet = new Color(0.65f, 0.25f, 1f);
            var rend = go.GetComponent<Renderer>();
            rend.material = MaterialLibrary.GetEmissive("SingularityMat", violet, violet, 3.0f);

            var sing = go.AddComponent<GravitySingularity>();
            sing._travelDir = new Vector3(direction.x, 0f, direction.z).normalized;
            sing._pullRadius += bonusRadius;
            sing._explosionDamage += bonusDamage;

            VfxHelper.SpawnShockwaveRing(position, violet, sing._pullRadius, 0.45f);
            ProceduralAudio.Instance?.PlaySingularity();
            return sing;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _duration;

            // Decelerate smoothly
            float speed = Mathf.Lerp(_travelSpeed, 0f, Mathf.Clamp01(t * 2.2f));
            transform.position += _travelDir * (speed * Time.deltaTime);

            // Clamp inside arena
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, -18f, 18f);
            p.z = Mathf.Clamp(p.z, -18f, 18f);
            p.y = 0.8f;
            transform.position = p;

            // Pulse scale
            float pulse = 0.95f + Mathf.Sin(_elapsed * 18f) * 0.15f + t * 0.35f;
            transform.localScale = Vector3.one * pulse;

            // Pull enemies & swallow enemy projectiles
            Collider[] cols = Physics.OverlapSphere(transform.position, _pullRadius);
            foreach (var c in cols)
            {
                var enemy = c.GetComponentInParent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    Vector3 pull = (transform.position - enemy.transform.position);
                    pull.y = 0f;
                    enemy.ApplyExternalPull(pull.normalized * (14f * Time.deltaTime));
                }
            }

            // Swallow nearby enemy bullets
            var projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            foreach (var proj in projectiles)
            {
                if (!proj.isPlayerProjectile && Vector3.Distance(proj.transform.position, transform.position) < _pullRadius * 0.7f)
                {
                    VfxHelper.SpawnBurst(proj.transform.position, new Color(0.75f, 0.4f, 1f), 4, 4f, 0.12f);
                    Destroy(proj.gameObject);
                }
            }

            if (_elapsed >= _duration)
            {
                Detonate();
            }
        }

        private void Detonate()
        {
            Color violet = new Color(0.75f, 0.35f, 1f);
            VfxHelper.SpawnBurst(transform.position, violet, 32, 14f, 0.3f);
            VfxHelper.SpawnShockwaveRing(transform.position, violet, _pullRadius * 1.15f, 0.4f);
            CameraController.Instance?.AddTrauma(0.55f);
            CameraController.Instance?.AddFovKick(4f);
            ProceduralAudio.Instance?.PlayExplosion();

            Collider[] cols = Physics.OverlapSphere(transform.position, _pullRadius * 0.9f);
            foreach (var c in cols)
            {
                var enemy = c.GetComponentInParent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    Vector3 push = (enemy.transform.position - transform.position).normalized * 12f;
                    enemy.TakeDamage(_explosionDamage, push, true);
                }
            }

            Destroy(gameObject);
        }
    }
}
