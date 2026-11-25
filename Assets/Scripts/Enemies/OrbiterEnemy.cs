using UnityEngine;
using ChronoDrift.Combat;
using ChronoDrift.Core;

namespace ChronoDrift.Enemies
{
    /// <summary>
    /// Heavy octahedron with 3 orbiting shield plates that deflect straight shots
    /// and fires 4-way radial plasma bursts.
    /// </summary>
    public class OrbiterEnemy : EnemyBase
    {
        public float moveSpeed = 3.2f;
        public float fireInterval = 2.4f;

        private Transform _shieldRing;
        private float _fireTimer;

        public static OrbiterEnemy Spawn(Vector3 pos, int wave)
        {
            var go = new GameObject("OrbiterEnemy");
            go.transform.position = new Vector3(pos.x, 0.7f, pos.z);

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.85f;
            col.isTrigger = true;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralMeshFactory.GetOctahedronMesh(0.9f);
            var mr = go.AddComponent<MeshRenderer>();

            var enemy = go.AddComponent<OrbiterEnemy>();
            enemy.maxHp = 95f;
            enemy.scoreValue = 240;
            enemy.BaseColor = new Color(0.82f, 0.25f, 1f);
            enemy.MainRenderer = mr;
            enemy.SetNormalMaterial(MaterialLibrary.GetEmissive("OrbiterMat", enemy.BaseColor, enemy.BaseColor, 1.9f));
            enemy._fireTimer = Random.Range(0.5f, 1.6f);
            enemy.BuildShieldPlates();
            enemy.ScaleStatsForWave(wave);
            return enemy;
        }

        private void BuildShieldPlates()
        {
            var ringGo = new GameObject("ShieldRing");
            ringGo.transform.SetParent(transform, false);
            _shieldRing = ringGo.transform;

            Material shieldMat = MaterialLibrary.GetEmissive("EnemyShieldMat", new Color(0.35f, 0.7f, 1f), new Color(0.35f, 0.7f, 1f), 1.6f);
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 120f * Mathf.Deg2Rad;
                var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.name = "ShieldPlate";
                plate.tag = "EnemyShield";
                plate.transform.SetParent(_shieldRing, false);
                plate.transform.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.45f;
                plate.transform.localScale = new Vector3(0.22f, 0.9f, 1.05f);
                plate.transform.LookAt(transform.position);
                plate.GetComponent<Renderer>().material = shieldMat;
            }
        }

        protected override void TickAI(float dt)
        {
            if (PlayerTransform == null) return;

            if (_shieldRing != null)
                _shieldRing.Rotate(Vector3.up, 110f * dt);

            Vector3 toPlayer = PlayerTransform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 16f)
            {
                transform.position += toPlayer.normalized * (moveSpeed * dt);
            }

            _fireTimer -= dt;
            if (_fireTimer <= 0f)
            {
                _fireTimer = fireInterval;
                float baseAngle = Time.time * 35f;
                for (int i = 0; i < 4; i++)
                {
                    float a = (baseAngle + i * 90f) * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    Projectile.Spawn(
                        transform.position + dir * 1.2f,
                        dir,
                        false,
                        13.5f,
                        1f,
                        0,
                        0,
                        new Color(0.95f, 0.3f, 0.9f),
                        1.1f
                    );
                }
            }
        }
    }
}
