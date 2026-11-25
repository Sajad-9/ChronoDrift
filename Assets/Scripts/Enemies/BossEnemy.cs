using UnityEngine;
using ChronoDrift.Audio;
using ChronoDrift.Combat;
using ChronoDrift.Core;

namespace ChronoDrift.Enemies
{
    /// <summary>
    /// Hex-Titan Boss spawning every 5 waves with Spiral Bullet Hell, Dash Slam, and Radial Nova attacks.
    /// </summary>
    public class BossEnemy : EnemyBase
    {
        public override bool IsBoss => true;

        private Transform _outerRing;
        private float _patternTimer;
        private int _phase;
        private float _spiralAngle;
        private float _spiralTick;
        private Vector3 _dashTarget;
        private bool _isDashing;

        public static BossEnemy Spawn(Vector3 pos, int wave)
        {
            var go = new GameObject("BossTitan");
            go.transform.position = new Vector3(pos.x, 1.2f, pos.z);

            var col = go.AddComponent<SphereCollider>();
            col.radius = 2.0f;
            col.isTrigger = true;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralMeshFactory.GetOctahedronMesh(1.9f);
            var mr = go.AddComponent<MeshRenderer>();

            var boss = go.AddComponent<BossEnemy>();
            boss.maxHp = 650f + wave * 120f;
            boss.CurrentHp = boss.maxHp;
            boss.scoreValue = 1500;
            boss.BaseColor = new Color(1f, 0.15f, 0.55f);
            boss.MainRenderer = mr;
            boss.SetNormalMaterial(MaterialLibrary.GetEmissive("BossCoreMat", boss.BaseColor, boss.BaseColor, 2.4f));
            boss.BuildOuterRing();

            ProceduralAudio.Instance?.PlayBossWarning();
            CameraController.Instance?.AddTrauma(0.55f);
            return boss;
        }

        private void BuildOuterRing()
        {
            var ring = new GameObject("BossRing");
            ring.transform.SetParent(transform, false);
            _outerRing = ring.transform;

            Material ringMat = MaterialLibrary.GetEmissive("BossRingMat", new Color(1f, 0.55f, 0.15f), new Color(1f, 0.55f, 0.15f), 2.1f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                var node = GameObject.CreatePrimitive(PrimitiveType.Cube);
                node.transform.SetParent(_outerRing, false);
                node.transform.localPosition = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 2.65f;
                node.transform.localScale = new Vector3(0.45f, 0.55f, 0.9f);
                node.transform.LookAt(transform.position);
                var c = node.GetComponent<Collider>();
                if (c != null) Destroy(c);
                node.GetComponent<Renderer>().material = ringMat;
            }
        }

        protected override void TickAI(float dt)
        {
            if (PlayerTransform == null) return;

            if (_outerRing != null)
                _outerRing.Rotate(Vector3.up, 75f * dt);

            _patternTimer += dt;

            if (_phase == 0)
            {
                // Phase 0: Spiral Bullet Hell while drifting toward player
                Vector3 toPlayer = (PlayerTransform.position - transform.position);
                toPlayer.y = 0f;
                if (toPlayer.magnitude > 6f)
                    transform.position += toPlayer.normalized * (3.8f * dt);

                _spiralTick -= dt;
                if (_spiralTick <= 0f)
                {
                    _spiralTick = 0.14f;
                    _spiralAngle += 22f;
                    for (int arm = 0; arm < 3; arm++)
                    {
                        float a = (_spiralAngle + arm * 120f) * Mathf.Deg2Rad;
                        Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        Projectile.Spawn(
                            transform.position + dir * 2.1f,
                            dir,
                            false,
                            13f,
                            1f,
                            0,
                            0,
                            new Color(1f, 0.2f, 0.55f),
                            1.15f
                        );
                    }
                }

                if (_patternTimer >= 4.2f)
                {
                    _patternTimer = 0f;
                    _phase = 1;
                    _isDashing = false;
                    _dashTarget = PlayerTransform.position;
                    VfxHelper.SpawnShockwaveRing(transform.position, new Color(1f, 0.6f, 0.15f), 4.5f, 0.45f);
                }
            }
            else if (_phase == 1)
            {
                // Phase 1: Telegraph then Dash Slam + Radial Nova
                if (_patternTimer < 0.7f)
                {
                    _dashTarget = PlayerTransform.position;
                }
                else if (!_isDashing)
                {
                    _isDashing = true;
                    Vector3 dashDir = (_dashTarget - transform.position);
                    dashDir.y = 0f;
                    if (dashDir.sqrMagnitude > 0.01f)
                        _dashTarget = transform.position + dashDir.normalized * Mathf.Min(13f, dashDir.magnitude + 2f);
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, _dashTarget, 24f * dt);
                    if (Vector3.Distance(transform.position, _dashTarget) < 0.5f || _patternTimer >= 1.6f)
                    {
                        FireRadialNova(12);
                        CameraController.Instance?.AddTrauma(0.45f);
                        _patternTimer = 0f;
                        _phase = 0;
                    }
                }
            }
        }

        private void FireRadialNova(int count)
        {
            VfxHelper.SpawnShockwaveRing(transform.position, BaseColor, 6.5f, 0.35f);
            for (int i = 0; i < count; i++)
            {
                float a = i * (360f / count) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Projectile.Spawn(
                    transform.position + dir * 2f,
                    dir,
                    false,
                    15f,
                    1f,
                    0,
                    0,
                    new Color(1f, 0.55f, 0.15f),
                    1.2f
                );
            }
        }
    }
}
