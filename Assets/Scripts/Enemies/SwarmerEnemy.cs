using UnityEngine;
using ChronoDrift.Core;

namespace ChronoDrift.Enemies
{
    /// <summary>
    /// Fast, aggressive crimson tetrahedron that weaves toward the player.
    /// </summary>
    public class SwarmerEnemy : EnemyBase
    {
        public float moveSpeed = 7.6f;
        private float _weavePhase;

        public static SwarmerEnemy Spawn(Vector3 pos, int wave)
        {
            var go = new GameObject("SwarmerEnemy");
            go.transform.position = new Vector3(pos.x, 0.65f, pos.z);

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.65f;
            col.isTrigger = true;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralMeshFactory.GetTetrahedronMesh(0.95f);
            var mr = go.AddComponent<MeshRenderer>();

            var enemy = go.AddComponent<SwarmerEnemy>();
            enemy.maxHp = 38f;
            enemy.scoreValue = 100;
            enemy.BaseColor = new Color(1f, 0.2f, 0.38f);
            enemy.MainRenderer = mr;
            enemy.SetNormalMaterial(MaterialLibrary.GetEmissive("SwarmerMat", enemy.BaseColor, enemy.BaseColor, 1.9f));
            enemy._weavePhase = Random.Range(0f, Mathf.PI * 2f);
            enemy.ScaleStatsForWave(wave);
            return enemy;
        }

        protected override void TickAI(float dt)
        {
            if (PlayerTransform == null) return;

            Vector3 toPlayer = PlayerTransform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            if (dist < 0.01f) return;

            Vector3 dir = toPlayer / dist;
            Vector3 perp = new Vector3(-dir.z, 0f, dir.x);
            float weave = Mathf.Sin(Time.time * 4.5f + _weavePhase) * (dist > 4f ? 0.55f : 0.1f);
            Vector3 moveDir = (dir + perp * weave).normalized;

            transform.position += moveDir * (moveSpeed * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), dt * 10f);
        }
    }
}
