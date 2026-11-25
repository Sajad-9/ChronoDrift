using UnityEngine;
using ChronoDrift.Combat;
using ChronoDrift.Core;

namespace ChronoDrift.Enemies
{
    /// <summary>
    /// Long-range diamond turret that projects a glowing laser telegraph before firing a fast bolt.
    /// </summary>
    public class SniperEnemy : EnemyBase
    {
        public float preferredRange = 13.5f;
        public float moveSpeed = 4.2f;
        public float fireCycle = 3.1f;

        private LineRenderer _laserSight;
        private float _cycleTimer;
        private Vector3 _lockedDir;

        public static SniperEnemy Spawn(Vector3 pos, int wave)
        {
            var go = new GameObject("SniperEnemy");
            go.transform.position = new Vector3(pos.x, 0.65f, pos.z);

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.7f;
            col.isTrigger = true;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralMeshFactory.GetDiamondMesh(1.1f, 0.5f);
            var mr = go.AddComponent<MeshRenderer>();

            var enemy = go.AddComponent<SniperEnemy>();
            enemy.maxHp = 52f;
            enemy.scoreValue = 160;
            enemy.BaseColor = new Color(1f, 0.65f, 0.12f);
            enemy.MainRenderer = mr;
            enemy.SetNormalMaterial(MaterialLibrary.GetEmissive("SniperMat", enemy.BaseColor, enemy.BaseColor, 2.0f));
            enemy._cycleTimer = Random.Range(0.5f, 1.8f);
            enemy.BuildLaserSight();
            enemy.ScaleStatsForWave(wave);
            return enemy;
        }

        private void BuildLaserSight()
        {
            var sightGo = new GameObject("LaserSight");
            sightGo.transform.SetParent(transform, false);
            _laserSight = sightGo.AddComponent<LineRenderer>();
            _laserSight.useWorldSpace = true;
            _laserSight.positionCount = 2;
            _laserSight.material = MaterialLibrary.GetGhostMaterial(new Color(1f, 0.2f, 0.1f, 0.7f));
            _laserSight.startWidth = 0.06f;
            _laserSight.endWidth = 0.04f;
            _laserSight.enabled = false;
        }

        protected override void TickAI(float dt)
        {
            if (PlayerTransform == null) return;

            Vector3 toPlayer = PlayerTransform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            Vector3 dir = dist > 0.01f ? toPlayer / dist : Vector3.forward;

            _cycleTimer += dt;

            // Movement: keep distance when not locked
            if (_cycleTimer < fireCycle - 0.9f)
            {
                if (dist > preferredRange + 1.5f)
                    transform.position += dir * (moveSpeed * dt);
                else if (dist < preferredRange - 2f)
                    transform.position -= dir * (moveSpeed * dt);
                else
                    transform.position += new Vector3(-dir.z, 0f, dir.x) * (moveSpeed * 0.6f * dt);

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), dt * 8f);
                _lockedDir = dir;
                _laserSight.enabled = false;
            }
            else if (_cycleTimer < fireCycle)
            {
                // Telegraph phase: track softly until last 0.3s, then lock
                if (_cycleTimer < fireCycle - 0.3f)
                {
                    _lockedDir = Vector3.Slerp(_lockedDir, dir, dt * 4f).normalized;
                    transform.rotation = Quaternion.LookRotation(_lockedDir);
                }

                _laserSight.enabled = true;
                float flash = Mathf.PingPong(Time.time * 18f, 1f);
                Color c = Color.Lerp(new Color(1f, 0.35f, 0.1f, 0.45f), new Color(1f, 0.1f, 0.1f, 0.95f), flash);
                _laserSight.startColor = c;
                _laserSight.endColor = c;
                _laserSight.SetPosition(0, transform.position);
                _laserSight.SetPosition(1, transform.position + _lockedDir * 24f);
            }
            else
            {
                _cycleTimer = 0f;
                _laserSight.enabled = false;
                Projectile.Spawn(
                    transform.position + _lockedDir * 1.1f,
                    _lockedDir,
                    false,
                    25f,
                    1f,
                    0,
                    0,
                    new Color(1f, 0.45f, 0.1f),
                    1.15f
                );
            }
        }
    }
}
