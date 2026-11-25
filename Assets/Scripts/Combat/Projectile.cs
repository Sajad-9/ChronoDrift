using UnityEngine;
using ChronoDrift.Audio;
using ChronoDrift.Core;
using ChronoDrift.Enemies;
using ChronoDrift.Player;

namespace ChronoDrift.Combat
{
    /// <summary>
    /// Glowing energy projectile supporting wall ricochets (+50% damage per bounce) and chain lightning.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public bool isPlayerProjectile = true;
        public float speed = 30f;
        public float damage = 25f;
        public int remainingBounces = 2;
        public int chainLightningCount = 0;
        public float lifetime = 3.2f;

        private Vector3 _direction;
        private int _bounceCount;
        private TrailRenderer _trail;
        private Renderer _renderer;

        public static Projectile Spawn(
            Vector3 position,
            Vector3 direction,
            bool isPlayer,
            float speed,
            float damage,
            int bounces,
            int chainCount,
            Color color,
            float scale = 1f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = isPlayer ? "PlayerBolt" : "EnemyBolt";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (isPlayer ? 0.34f : 0.42f) * scale;

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var proj = go.AddComponent<Projectile>();
            proj.isPlayerProjectile = isPlayer;
            proj.speed = speed;
            proj.damage = damage;
            proj.remainingBounces = bounces;
            proj.chainLightningCount = chainCount;
            proj._direction = direction.normalized;
            proj._direction.y = 0f;
            if (proj._direction.sqrMagnitude < 0.001f) proj._direction = Vector3.forward;
            proj._direction.Normalize();

            var rend = go.GetComponent<Renderer>();
            rend.material = MaterialLibrary.GetEmissive(isPlayer ? "PlayerBoltMat" : "EnemyBoltMat", color, color, 2.5f);
            proj._renderer = rend;

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = isPlayer ? 0.16f : 0.2f;
            trail.startWidth = 0.24f * scale;
            trail.endWidth = 0.01f;
            trail.material = MaterialLibrary.GetGhostMaterial(color);
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            proj._trail = trail;

            return proj;
        }

        private void Update()
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float step = speed * Time.deltaTime;
            Ray ray = new Ray(transform.position, _direction);
            if (Physics.SphereCast(ray, 0.28f, out RaycastHit hit, step))
            {
                if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Obstacle"))
                {
                    if (remainingBounces > 0)
                    {
                        remainingBounces--;
                        _bounceCount++;
                        damage *= 1.5f;
                        Vector3 normal = hit.normal;
                        normal.y = 0f;
                        if (normal.sqrMagnitude > 0.001f)
                            _direction = Vector3.Reflect(_direction, normal.normalized).normalized;
                        else
                            _direction = -_direction;

                        transform.position = hit.point + _direction * 0.2f;
                        transform.position = new Vector3(transform.position.x, 0.6f, transform.position.z);

                        // Shift to gold on ricochet!
                        Color gold = new Color(1f, 0.85f, 0.2f);
                        if (_renderer != null)
                            _renderer.material = MaterialLibrary.GetEmissive("RicochetBoltMat", gold, gold, 3f);
                        if (_trail != null)
                        {
                            _trail.startColor = gold;
                            _trail.endColor = new Color(1f, 0.85f, 0.2f, 0f);
                        }

                        VfxHelper.SpawnBurst(hit.point, gold, 7, 6f, 0.16f);
                        ProceduralAudio.Instance?.PlayRicochet(_bounceCount);
                        return;
                    }
                    else
                    {
                        VfxHelper.SpawnBurst(hit.point, isPlayerProjectile ? Color.cyan : new Color(1f, 0.25f, 0.35f), 6, 5f, 0.14f);
                        Destroy(gameObject);
                        return;
                    }
                }

                if (isPlayerProjectile)
                {
                    var enemy = hit.collider.GetComponentInParent<EnemyBase>();
                    if (enemy != null)
                    {
                        bool isCrit = _bounceCount > 0;
                        enemy.TakeDamage(damage, _direction * 6f, isCrit);
                        VfxHelper.SpawnBurst(hit.point, isCrit ? new Color(1f, 0.85f, 0.2f) : Color.cyan, 10, 7f, 0.18f);

                        if (chainLightningCount > 0)
                        {
                            TriggerChainLightning(enemy, hit.point);
                        }

                        Destroy(gameObject);
                        return;
                    }

                    if (hit.collider.CompareTag("EnemyShield"))
                    {
                        _direction = Vector3.Reflect(_direction, hit.normal).normalized;
                        _direction.y = 0f;
                        transform.position = hit.point + _direction * 0.2f;
                        VfxHelper.SpawnBurst(hit.point, new Color(0.4f, 0.8f, 1f), 6, 5f, 0.14f);
                        ProceduralAudio.Instance?.PlayRicochet(1);
                        return;
                    }
                }
                else
                {
                    var player = hit.collider.GetComponentInParent<PlayerController>();
                    if (player != null)
                    {
                        player.TakeDamage(1);
                        Destroy(gameObject);
                        return;
                    }
                }
            }

            transform.position += _direction * step;
            transform.position = new Vector3(transform.position.x, 0.6f, transform.position.z);
        }

        private void TriggerChainLightning(EnemyBase primaryTarget, Vector3 origin)
        {
            Collider[] hits = Physics.OverlapSphere(origin, 7.5f);
            int chained = 0;
            foreach (var c in hits)
            {
                if (chained >= chainLightningCount) break;
                var other = c.GetComponentInParent<EnemyBase>();
                if (other != null && other != primaryTarget && other.IsAlive)
                {
                    other.TakeDamage(damage * 0.65f, Vector3.zero, false);
                    VfxHelper.SpawnLightningArc(origin, other.transform.position + Vector3.up * 0.6f, new Color(0.35f, 0.9f, 1f));
                    chained++;
                }
            }
        }
    }
}
