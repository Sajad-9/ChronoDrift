using UnityEngine;
using ChronoDrift.Audio;
using ChronoDrift.Combat;
using ChronoDrift.Core;
using ChronoDrift.Player;

namespace ChronoDrift.Enemies
{
    /// <summary>
    /// MineLayer — slow drone that drifts toward the player and deploys
    /// proximity mines along its path.  Mines glow, pulse, and detonate
    /// when the player gets within 2 units — or when shot.
    /// </summary>
    public class MineLayerEnemy : EnemyBase
    {
        public float moveSpeed    = 2.8f;
        private float _mineTimer  = 2.2f;
        private float _mineCooldown = 2.2f;

        // ----------------------------------------------------------------
        public static MineLayerEnemy Spawn(Vector3 pos, int wave)
        {
            var go = new GameObject("MineLayerEnemy");
            go.transform.position = new Vector3(pos.x, 0.65f, pos.z);

            var col = go.AddComponent<SphereCollider>();
            col.radius   = 0.72f;
            col.isTrigger = true;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralMeshFactory.GetOctahedronMesh(1.05f);
            var mr = go.AddComponent<MeshRenderer>();

            var enemy = go.AddComponent<MineLayerEnemy>();
            enemy.maxHp     = 60f;
            enemy.scoreValue = 180;
            enemy.BaseColor  = new Color(0.2f, 1f, 0.45f);
            enemy.MainRenderer = mr;
            enemy.SetNormalMaterial(MaterialLibrary.GetEmissive("MineLayerMat", enemy.BaseColor, enemy.BaseColor, 2.0f));
            enemy._mineTimer = 0.8f;
            enemy.ScaleStatsForWave(wave);
            return enemy;
        }

        // ----------------------------------------------------------------
        protected override void TickAI(float dt)
        {
            if (PlayerTransform == null) return;

            Vector3 toPlayer = PlayerTransform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            // Keep ~8 units away and slowly circle
            if (dist > 9f)
            {
                transform.position += (toPlayer / dist) * (moveSpeed * dt);
            }
            else
            {
                Vector3 strafe = new Vector3(-toPlayer.z, 0f, toPlayer.x).normalized;
                transform.position += strafe * (moveSpeed * 0.9f * dt);
            }

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(toPlayer / Mathf.Max(0.1f, dist)),
                dt * 6f);

            _mineTimer -= dt;
            if (_mineTimer <= 0f)
            {
                _mineTimer = _mineCooldown;
                ProximityMine.Spawn(transform.position + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f)));
            }
        }
    }

    // ====================================================================
    /// <summary>
    /// Proximity mine — deployed by MineLayerEnemy.
    /// Pulses green, detonates when the player or a player projectile
    /// gets within trigger radius, or after 12 seconds.
    /// </summary>
    public class ProximityMine : MonoBehaviour
    {
        private float _lifetime   = 12f;
        private float _triggerRadius = 2.1f;
        private float _damage     = 55f;
        private float _elapsed    = 0f;
        private Renderer _rend;
        private bool _detonated   = false;

        public static ProximityMine Spawn(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ProximityMine";
            go.transform.position = new Vector3(pos.x, 0.25f, pos.z);
            go.transform.localScale = Vector3.one * 0.42f;

            var col = go.GetComponent<Collider>();
            if (col) Destroy(col);

            Color green = new Color(0.15f, 1f, 0.4f);
            var rend = go.GetComponent<Renderer>();
            rend.material = MaterialLibrary.GetEmissive("MineMat", green, green, 2.5f);

            var mine = go.AddComponent<ProximityMine>();
            mine._rend = rend;

            VfxHelper.SpawnBurst(pos, green, 5, 3f, 0.1f);
            return mine;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            // Pulse glow
            float pulse = 1f + Mathf.Sin(_elapsed * 5f) * 0.3f;
            transform.localScale = Vector3.one * 0.42f * pulse;

            if (_elapsed >= _lifetime)
            {
                Detonate(false);
                return;
            }

            // Check player proximity
            if (PlayerController.Instance != null)
            {
                float dist = Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
                if (dist <= _triggerRadius) Detonate(true);
            }

            // Check if hit by player projectile via active registry
            for (int i = Projectile.ActiveProjectiles.Count - 1; i >= 0; i--)
            {
                var proj = Projectile.ActiveProjectiles[i];
                if (proj != null && proj.isPlayerProjectile &&
                    (proj.transform.position - transform.position).sqrMagnitude <= 0.45f)
                {
                    Destroy(proj.gameObject);
                    Detonate(false);
                    return;
                }
            }
        }

        private void Detonate(bool hitsPlayer)
        {
            if (_detonated) return;
            _detonated = true;

            Color green = new Color(0.15f, 1f, 0.4f);
            VfxHelper.SpawnBurst(transform.position, green, 24, 12f, 0.3f);
            VfxHelper.SpawnShockwaveRing(transform.position, green, 5f, 0.4f);
            CameraController.Instance?.AddTrauma(0.4f);
            TimeDilationManager.Instance?.TriggerHitStop(0.045f);
            ProceduralAudio.Instance?.PlayExplosion();

            if (hitsPlayer && PlayerController.Instance != null)
            {
                PlayerController.Instance.TakeDamage(Mathf.RoundToInt(_damage / 15f));
            }

            Destroy(gameObject);
        }
    }
}
