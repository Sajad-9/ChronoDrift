using UnityEngine;
using ChronoDrift.Combat;
using ChronoDrift.Core;

namespace ChronoDrift.Enemies
{
    /// <summary>
    /// Elite Swarmer — a bigger, faster crimson swarmer with a DriftTrail afterimage
    /// and a short-range dash-lunge that knocks back the player on close contact.
    /// Spawns from wave 4 onward.
    /// </summary>
    public class EliteEnemy : EnemyBase
    {
        public float moveSpeed    = 10.5f;
        private float _weavePhase;
        private float _dashTimer  = 0f;
        private float _dashCooldown = 2.6f;
        private bool  _dashing   = false;
        private Vector3 _dashDir;
        private float _dashDuration = 0.22f;
        private float _dashElapsed;
        private DriftTrail _driftTrail;

        // ----------------------------------------------------------------
        public static EliteEnemy Spawn(Vector3 pos, int wave)
        {
            var go = new GameObject("EliteEnemy");
            go.transform.position = new Vector3(pos.x, 0.65f, pos.z);

            var col = go.AddComponent<SphereCollider>();
            col.radius   = 0.78f;
            col.isTrigger = true;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralMeshFactory.GetTetrahedronMesh(1.25f);
            var mr = go.AddComponent<MeshRenderer>();

            var enemy = go.AddComponent<EliteEnemy>();
            enemy.maxHp     = 85f;
            enemy.scoreValue = 240;
            enemy.BaseColor  = new Color(1f, 0.1f, 0.65f);
            enemy.MainRenderer = mr;
            enemy.SetNormalMaterial(MaterialLibrary.GetEmissive("EliteMat", enemy.BaseColor, enemy.BaseColor, 2.6f));
            enemy._weavePhase = Random.Range(0f, Mathf.PI * 2f);
            enemy._dashTimer  = Random.Range(0.4f, 1.2f);
            enemy.ScaleStatsForWave(wave);

            // Add afterimage trail
            enemy._driftTrail = DriftTrail.Attach(go.transform, enemy.BaseColor, 0.055f, 0.32f);

            return enemy;
        }

        // ----------------------------------------------------------------
        protected override void TickAI(float dt)
        {
            if (PlayerTransform == null) return;

            Vector3 toPlayer = PlayerTransform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            if (dist < 0.01f) return;

            Vector3 dir = toPlayer / dist;

            if (_dashing)
            {
                _dashElapsed += dt;
                transform.position += _dashDir * (moveSpeed * 2.6f * dt);
                transform.rotation = Quaternion.LookRotation(_dashDir);

                if (_dashElapsed >= _dashDuration)
                {
                    _dashing  = false;
                    _dashTimer = _dashCooldown;
                    if (_driftTrail) _driftTrail.SetEnabled(false);
                    VfxHelper.SpawnBurst(transform.position, BaseColor, 8, 7f, 0.18f);
                }
                return;
            }

            // Normal weave movement
            Vector3 perp  = new Vector3(-dir.z, 0f, dir.x);
            float weave   = Mathf.Sin(Time.time * 5.5f + _weavePhase) * (dist > 4f ? 0.65f : 0.15f);
            Vector3 moveDir = (dir + perp * weave).normalized;

            transform.position += moveDir * (moveSpeed * dt);
            transform.rotation  = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), dt * 12f);

            // Charge dash when close enough
            _dashTimer -= dt;
            if (_dashTimer <= 0f && dist < 10f)
            {
                _dashing     = true;
                _dashDir     = dir;
                _dashElapsed = 0f;
                if (_driftTrail) _driftTrail.SetEnabled(true);
                VfxHelper.SpawnShockwaveRing(transform.position, BaseColor, 2.5f, 0.22f);
                Audio.ProceduralAudio.Instance?.PlayDash();
            }
        }
    }
}
