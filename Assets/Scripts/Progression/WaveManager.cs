using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChronoDrift.Audio;
using ChronoDrift.Combat;
using ChronoDrift.Core;
using ChronoDrift.Enemies;
using ChronoDrift.Player;
using ChronoDrift.UI;

namespace ChronoDrift.Progression
{
    /// <summary>
    /// Builds the Neon Obsidian Arena and orchestrates escalating enemy waves + boss encounters.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        public int CurrentWave { get; private set; }
        public BossEnemy ActiveBoss { get; private set; }

        private readonly List<EnemyBase> _aliveEnemies = new List<EnemyBase>();
        private readonly List<Renderer> _pillarRenderers = new List<Renderer>();
        private bool _spawningWave;
        private bool _arenaBuilt;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            EnsureArenaBuilt();
        }

        public void EnsureArenaBuilt()
        {
            if (_arenaBuilt) return;
            _arenaBuilt = true;

            var arenaRoot = new GameObject("NeonArena");

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(arenaRoot.transform);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(42f, 1f, 42f);
            floor.GetComponent<Renderer>().material = MaterialLibrary.GetArenaFloorMaterial();

            // 4 Perimeter Walls (tagged "Wall" for ricochet)
            Material wallMat = MaterialLibrary.GetEmissive("ArenaWallMat", new Color(0.08f, 0.25f, 0.45f), new Color(0.1f, 0.7f, 1f), 1.4f);
            CreateWall(arenaRoot.transform, "Wall_N", new Vector3(0f, 1.2f, 20f), new Vector3(42f, 2.4f, 1.2f), wallMat);
            CreateWall(arenaRoot.transform, "Wall_S", new Vector3(0f, 1.2f, -20f), new Vector3(42f, 2.4f, 1.2f), wallMat);
            CreateWall(arenaRoot.transform, "Wall_E", new Vector3(20f, 1.2f, 0f), new Vector3(1.2f, 2.4f, 42f), wallMat);
            CreateWall(arenaRoot.transform, "Wall_W", new Vector3(-20f, 1.2f, 0f), new Vector3(1.2f, 2.4f, 42f), wallMat);

            // 4 Tactical Ricochet Pillars
            Vector3[] pillarPositions =
            {
                new Vector3(-9f, 1.5f, -9f),
                new Vector3(9f, 1.5f, -9f),
                new Vector3(-9f, 1.5f, 9f),
                new Vector3(9f, 1.5f, 9f)
            };

            for (int i = 0; i < pillarPositions.Length; i++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"RicochetPillar_{i}";
                pillar.tag = "Obstacle";
                pillar.transform.SetParent(arenaRoot.transform);
                pillar.transform.position = pillarPositions[i];
                pillar.transform.localScale = new Vector3(2.4f, 3.0f, 2.4f);
                var rend = pillar.GetComponent<Renderer>();
                rend.material = MaterialLibrary.GetEmissive($"PillarMat_{i}", new Color(0.08f, 0.14f, 0.25f), new Color(0.15f, 0.8f, 1f), 1.1f);
                _pillarRenderers.Add(rend);
            }
        }

        private void CreateWall(Transform parent, string wallName, Vector3 pos, Vector3 scale, Material mat)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.tag = "Wall";
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().material = mat;
        }

        public void ResetAndStart()
        {
            EnsureArenaBuilt();
            StopAllCoroutines();

            foreach (var e in _aliveEnemies)
            {
                if (e != null) Destroy(e.gameObject);
            }
            _aliveEnemies.Clear();
            ActiveBoss = null;

            foreach (var proj in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                Destroy(proj.gameObject);
            foreach (var sing in FindObjectsByType<GravitySingularity>(FindObjectsSortMode.None))
                Destroy(sing.gameObject);

            CurrentWave = 0;
            StartNextWave();
        }

        public void StartNextWave()
        {
            CurrentWave++;
            AchievementSystem.Instance?.OnWaveStart(CurrentWave);
            StartCoroutine(SpawnWaveRoutine(CurrentWave));
        }

        private IEnumerator SpawnWaveRoutine(int wave)
        {
            _spawningWave = true;
            yield return new WaitForSeconds(0.6f);

            bool isBossWave = (wave % 5 == 0);
            int totalEnemies = isBossWave ? 3 + wave / 2 : 5 + wave * 2;

            if (isBossWave)
            {
                Vector3 bossPos = new Vector3(0f, 1.2f, 13f);
                VfxHelper.SpawnShockwaveRing(bossPos, new Color(1f, 0.2f, 0.5f), 5f, 0.5f);
                yield return new WaitForSeconds(0.5f);
                var boss = BossEnemy.Spawn(bossPos, wave);
                _aliveEnemies.Add(boss);
                ActiveBoss = boss;
            }

            for (int i = 0; i < totalEnemies; i++)
            {
                if (GameManager.Instance.State != GameState.Playing)
                    yield return new WaitUntil(() => GameManager.Instance.State == GameState.Playing);

                Vector3 spawnPos = PickSafeSpawnPos();
                VfxHelper.SpawnShockwaveRing(spawnPos, new Color(1f, 0.3f, 0.4f), 2.2f, 0.35f);
                yield return new WaitForSeconds(0.18f);

                EnemyBase enemy;
                float roll = Random.value;
                if (wave >= 4 && roll < 0.14f)
                    enemy = EliteEnemy.Spawn(spawnPos, wave);
                else if (wave >= 4 && roll < 0.28f)
                    enemy = MineLayerEnemy.Spawn(spawnPos, wave);
                else if (wave >= 3 && roll < 0.42f)
                    enemy = OrbiterEnemy.Spawn(spawnPos, wave);
                else if (wave >= 2 && roll < 0.65f)
                    enemy = SniperEnemy.Spawn(spawnPos, wave);
                else
                    enemy = SwarmerEnemy.Spawn(spawnPos, wave);

                _aliveEnemies.Add(enemy);
            }

            _spawningWave = false;
        }

        private Vector3 PickSafeSpawnPos()
        {
            Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector3 candidate = new Vector3(Random.Range(-16.5f, 16.5f), 0.65f, Random.Range(-16.5f, 16.5f));
                if (Vector3.Distance(candidate, playerPos) > 8.5f)
                    return candidate;
            }
            return new Vector3(15f, 0.65f, 15f);
        }

        public void NotifyEnemyKilled(EnemyBase enemy)
        {
            _aliveEnemies.Remove(enemy);
            bool wasBoss = (enemy == ActiveBoss);
            if (wasBoss)
            {
                ActiveBoss = null;
                AchievementSystem.Instance?.OnBossKilled();
            }

            AchievementSystem.Instance?.OnKill();

            if (!_spawningWave && _aliveEnemies.Count == 0 && GameManager.Instance.State == GameState.Playing)
            {
                AchievementSystem.Instance?.OnWaveComplete(CurrentWave);
                StartCoroutine(WaveClearedRoutine());
            }
        }

        private IEnumerator WaveClearedRoutine()
        {
            yield return new WaitForSecondsRealtime(0.55f);
            if (GameManager.Instance.State != GameState.Playing) yield break;

            ProceduralAudio.Instance?.PlayUpgrade();
            GameManager.Instance.SetState(GameState.UpgradeSelect);
            GameUI.Instance?.ShowUpgradeChoices(UpgradeSystem.RollThreeOptions());
        }
    }
}
