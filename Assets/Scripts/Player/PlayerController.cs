using System.Collections.Generic;
using UnityEngine;
using ChronoDrift.Audio;
using ChronoDrift.Combat;
using ChronoDrift.Core;
using ChronoDrift.Enemies;

namespace ChronoDrift.Player
{
    /// <summary>
    /// Player delta-wing craft: WASD movement, mouse aim, Ricochet Railgun (LMB),
    /// Gravity Singularity (RMB), Phase-Dash (Space), Black Hole Rift (Q),
    /// Chrono Focus (Shift/E), and upgradeable Orbital Shields.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement & Dash")]
        public float moveSpeed = 13f;
        public float dashSpeed = 34f;
        public float dashDuration = 0.17f;
        public float dashCooldown = 1.05f;

        [Header("Combat")]
        public int maxHealth = 5;
        public float fireInterval = 0.17f;
        public float projectileSpeed = 34f;
        public float baseDamage = 26f;
        public int baseBounces = 2;
        public float singularityCooldown = 4.5f;
        public float blackHoleCooldown = 18f;

        // Runtime upgrade stats
        public int SplitShotLevel { get; private set; }
        public int BonusBounces { get; private set; }
        public int ChainLightningLevel { get; private set; }
        public int DashShockwaveLevel { get; private set; }
        public float VampiricChance { get; private set; }
        public int SingularityUpgradeLevel { get; private set; }
        public int OrbitalShieldCount { get; private set; }
        public float BlackHoleCooldownNormalized => Mathf.Clamp01(_blackHoleCdTimer / blackHoleCooldown);
        public int BlackHoleUpgradeLevel { get; private set; }

        public int CurrentHealth { get; private set; }
        public float DashCooldownNormalized => Mathf.Clamp01(_dashCdTimer / dashCooldown);
        public float SingularityCooldownNormalized => Mathf.Clamp01(_singularityCdTimer / singularityCooldown);

        private Transform _visualRoot;
        private Camera _cam;
        private Vector3 _velocity;
        private bool _isDashing;
        private float _dashTimer;
        private float _dashCdTimer;
        private Vector3 _dashDir;
        private float _fireTimer;
        private float _singularityCdTimer;
        private float _blackHoleCdTimer;
        private float _invulnTimer;
        private float _ghostSpawnTimer;
        private int _killHealCounter;

        private readonly List<Transform> _orbitalShields = new List<Transform>();
        private float _orbitalAngle;

        private void Awake()
        {
            Instance = this;
            _cam = Camera.main;
            BuildVisual();
            CurrentHealth = maxHealth;
        }

        private void BuildVisual()
        {
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.65f;
            }

            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            var visualGo = new GameObject("ShipVisual");
            visualGo.transform.SetParent(transform, false);
            var mf = visualGo.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralMeshFactory.GetPlayerShipMesh();
            var mr = visualGo.AddComponent<MeshRenderer>();
            Color cyan = new Color(0.15f, 0.92f, 1f);
            mr.material = MaterialLibrary.GetEmissive("PlayerShipMat", cyan, cyan, 1.9f);
            _visualRoot = visualGo.transform;

            var trail = visualGo.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.startWidth = 0.35f;
            trail.endWidth = 0.02f;
            trail.material = MaterialLibrary.GetGhostMaterial(cyan);
            trail.startColor = new Color(0.15f, 0.92f, 1f, 0.7f);
            trail.endColor = new Color(0.15f, 0.92f, 1f, 0f);
        }

        public void ResetForNewRun()
        {
            transform.position = new Vector3(0f, 0.6f, 0f);
            _velocity = Vector3.zero;
            _isDashing = false;
            _dashTimer = 0f;
            _dashCdTimer = 0f;
            _fireTimer = 0f;
            _singularityCdTimer = 0f;
            _invulnTimer = 0.5f;

            maxHealth = 5;
            CurrentHealth = maxHealth;
            fireInterval = 0.17f;
            baseDamage = 26f;

            SplitShotLevel = 0;
            BonusBounces = 0;
            ChainLightningLevel = 0;
            DashShockwaveLevel = 0;
            VampiricChance = 0f;
            SingularityUpgradeLevel = 0;
            OrbitalShieldCount = 0;
            BlackHoleUpgradeLevel = 0;
            _killHealCounter = 0;
            _blackHoleCdTimer = 0f;
            RebuildOrbitalShields();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            if (_cam == null) _cam = Camera.main;

            float dt = Time.deltaTime;
            _dashCdTimer = Mathf.Max(0f, _dashCdTimer - dt);
            _fireTimer = Mathf.Max(0f, _fireTimer - dt);
            _singularityCdTimer = Mathf.Max(0f, _singularityCdTimer - dt);
            _invulnTimer = Mathf.Max(0f, _invulnTimer - dt);

            HandleAim();
            HandleMovement(dt);
            HandleActions();
            UpdateOrbitalShields(dt);
        }

        private void HandleAim()
        {
            if (_cam == null) return;
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            Plane ground = new Plane(Vector3.up, new Vector3(0f, 0.6f, 0f));
            if (ground.Raycast(ray, out float enter))
            {
                Vector3 hit = ray.GetPoint(enter);
                Vector3 dir = hit - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 25f);
                }
            }
        }

        private void HandleMovement(float dt)
        {
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

            if (!_isDashing && Input.GetKeyDown(KeyCode.Space) && _dashCdTimer <= 0f)
            {
                _isDashing = true;
                _dashTimer = dashDuration;
                _dashCdTimer = dashCooldown;
                _invulnTimer = Mathf.Max(_invulnTimer, dashDuration + 0.08f);
                _dashDir = input.sqrMagnitude > 0.01f ? input : transform.forward;
                _ghostSpawnTimer = 0f;

                CameraController.Instance?.AddFovKick(3.2f);
                CameraController.Instance?.AddTrauma(0.22f);
                ProceduralAudio.Instance?.PlayDash();

                if (DashShockwaveLevel > 0)
                {
                    TriggerDashShockwave();
                }
            }

            Vector3 moveDelta;
            if (_isDashing)
            {
                _dashTimer -= dt;
                moveDelta = _dashDir * (dashSpeed * dt);
                _ghostSpawnTimer -= dt;
                if (_ghostSpawnTimer <= 0f)
                {
                    SpawnAfterimage();
                    _ghostSpawnTimer = 0.035f;
                }
                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                }
            }
            else
            {
                Vector3 targetVel = input * moveSpeed;
                _velocity = Vector3.Lerp(_velocity, targetVel, dt * 16f);
                moveDelta = _velocity * dt;

                // Subtle banking tilt
                if (_visualRoot != null)
                {
                    Vector3 localVel = transform.InverseTransformDirection(_velocity);
                    float bankZ = Mathf.Clamp(-localVel.x * 2.2f, -26f, 26f);
                    float pitchX = Mathf.Clamp(localVel.z * 1.1f, -14f, 14f);
                    _visualRoot.localRotation = Quaternion.Slerp(_visualRoot.localRotation, Quaternion.Euler(pitchX, 0f, bankZ), dt * 12f);
                }
            }

            Vector3 nextPos = transform.position + moveDelta;
            nextPos.x = Mathf.Clamp(nextPos.x, -18.5f, 18.5f);
            nextPos.z = Mathf.Clamp(nextPos.z, -18.5f, 18.5f);
            nextPos.y = 0.6f;

            // Simple obstacle push-out
            Collider[] overlaps = Physics.OverlapSphere(nextPos, 0.7f);
            foreach (var c in overlaps)
            {
                if (c.CompareTag("Obstacle"))
                {
                    Vector3 away = nextPos - c.ClosestPoint(nextPos);
                    away.y = 0f;
                    if (away.sqrMagnitude > 0.0001f)
                        nextPos += away.normalized * (0.7f - away.magnitude);
                }
            }

            transform.position = nextPos;
        }

        private void HandleActions()
        {
            if (Input.GetMouseButton(0) && _fireTimer <= 0f)
            {
                _fireTimer = fireInterval;
                FirePrimary();
            }

            if (Input.GetMouseButtonDown(1) && _singularityCdTimer <= 0f)
            {
                _singularityCdTimer = singularityCooldown;
                GravitySingularity.Spawn(
                    transform.position + transform.forward * 1.2f,
                    transform.forward,
                    SingularityUpgradeLevel * 2.2f,
                    SingularityUpgradeLevel * 30f
                );
            }

            // Q — Black Hole Rift (ultimate)
            if (Input.GetKeyDown(KeyCode.Q) && _blackHoleCdTimer <= 0f)
            {
                _blackHoleCdTimer = blackHoleCooldown;
                BlackHoleRift.Spawn(
                    transform.position + transform.forward * 2f,
                    BlackHoleUpgradeLevel * 55f,
                    BlackHoleUpgradeLevel * 2.5f
                );
                AchievementSystem.Instance?.OnBlackHoleActivated();
                ScreenEffects.Instance?.TriggerBlackHoleVignette();
                CameraController.Instance?.AddFovKick(6f);
            }

            _blackHoleCdTimer = Mathf.Max(0f, _blackHoleCdTimer - Time.deltaTime);
        }

        private void FirePrimary()
        {
            int totalShots = 1 + SplitShotLevel * 2;
            float spreadStep = 9f;
            Color cyan = new Color(0.15f, 0.95f, 1f);

            for (int i = 0; i < totalShots; i++)
            {
                float offsetAngle = (i - (totalShots - 1) * 0.5f) * spreadStep;
                Vector3 dir = Quaternion.Euler(0f, offsetAngle, 0f) * transform.forward;
                Projectile.Spawn(
                    transform.position + dir * 1.15f,
                    dir,
                    true,
                    projectileSpeed,
                    baseDamage,
                    baseBounces + BonusBounces,
                    ChainLightningLevel,
                    cyan
                );
            }

            CameraController.Instance?.AddTrauma(0.06f);
            ProceduralAudio.Instance?.PlayShoot();
        }

        private void TriggerDashShockwave()
        {
            float radius = 4.5f + DashShockwaveLevel * 1.2f;
            float dmg = 22f * DashShockwaveLevel;
            VfxHelper.SpawnShockwaveRing(transform.position, new Color(0.2f, 1f, 0.9f), radius, 0.3f);

            Collider[] cols = Physics.OverlapSphere(transform.position, radius);
            foreach (var c in cols)
            {
                var enemy = c.GetComponentInParent<EnemyBase>();
                if (enemy != null && enemy.IsAlive)
                {
                    Vector3 push = (enemy.transform.position - transform.position).normalized * 10f;
                    enemy.TakeDamage(dmg, push, false);
                }
            }
        }

        private void SpawnAfterimage()
        {
            var ghost = new GameObject("DashGhost");
            ghost.transform.SetPositionAndRotation(transform.position, transform.rotation);
            var mf = ghost.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralMeshFactory.GetPlayerShipMesh();
            var mr = ghost.AddComponent<MeshRenderer>();
            mr.material = MaterialLibrary.GetGhostMaterial(new Color(0.15f, 0.92f, 1f, 0.45f));
            Destroy(ghost, 0.22f);
        }

        public void TakeDamage(int amount)
        {
            if (_invulnTimer > 0f || GameManager.Instance.State != GameState.Playing) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            _invulnTimer = 0.85f;
            CameraController.Instance?.AddTrauma(0.65f);
            TimeDilationManager.Instance?.TriggerHitStop(0.055f);
            VfxHelper.SpawnBurst(transform.position, new Color(1f, 0.25f, 0.3f), 18, 10f, 0.25f);
            DamagePopup.Spawn(transform.position, $"-{amount} HP", new Color(1f, 0.3f, 0.35f), 1.2f);
            ProceduralAudio.Instance?.PlayPlayerHit();

            // Screen damage vignette
            ScreenEffects.Instance?.TriggerDamageVignette(0.9f);
            ScreenEffects.Instance?.TriggerFlash(new Color(1f, 0.1f, 0.1f), 0.22f);
            AchievementSystem.Instance?.OnPlayerHit();

            if (CurrentHealth <= 0)
            {
                VfxHelper.SpawnBurst(transform.position, Color.cyan, 36, 14f, 0.32f);
                GameManager.Instance.TriggerGameOver();
                AchievementSystem.Instance?.OnRunEnd();
            }
        }

        public void Heal(int amount)
        {
            if (CurrentHealth >= maxHealth) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            DamagePopup.Spawn(transform.position, $"+{amount} HP", new Color(0.3f, 1f, 0.55f), 1.1f);
        }

        public void NotifyKill()
        {
            if (VampiricChance > 0f)
            {
                _killHealCounter++;
                int threshold = Mathf.Max(4, Mathf.RoundToInt(12f / (1f + VampiricChance * 2f)));
                if (_killHealCounter >= threshold)
                {
                    _killHealCounter = 0;
                    Heal(1);
                }
            }
        }

        // Upgrade hooks
        public void AddRicochet(int amount) => BonusBounces += amount;
        public void AddSplitShot() => SplitShotLevel++;
        public void AddChainLightning() => ChainLightningLevel += 2;
        public void AddDashShockwave() => DashShockwaveLevel++;
        public void AddVampiric() => VampiricChance += 0.5f;
        public void AddSingularityUpgrade()
        {
            SingularityUpgradeLevel++;
            singularityCooldown = Mathf.Max(2.2f, singularityCooldown * 0.85f);
        }
        public void OverclockFireRate()
        {
            fireInterval = Mathf.Max(0.08f, fireInterval * 0.82f);
            baseDamage *= 1.12f;
        }
        public void AddOrbitalShield()
        {
            OrbitalShieldCount += 2;
            RebuildOrbitalShields();
        }
        public void AddBlackHoleUpgrade()
        {
            BlackHoleUpgradeLevel++;
            blackHoleCooldown = Mathf.Max(8f, blackHoleCooldown * 0.78f);
        }

        private void RebuildOrbitalShields()
        {
            foreach (var s in _orbitalShields)
            {
                if (s != null) Destroy(s.gameObject);
            }
            _orbitalShields.Clear();

            Color shieldColor = new Color(0.3f, 1f, 0.8f);
            for (int i = 0; i < OrbitalShieldCount; i++)
            {
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "OrbitalShield";
                orb.transform.localScale = Vector3.one * 0.45f;
                var col = orb.GetComponent<Collider>();
                if (col != null) Destroy(col);
                var rend = orb.GetComponent<Renderer>();
                rend.material = MaterialLibrary.GetEmissive("OrbitalShieldMat", shieldColor, shieldColor, 2.2f);
                _orbitalShields.Add(orb.transform);
            }
        }

        private void UpdateOrbitalShields(float dt)
        {
            if (_orbitalShields.Count == 0) return;
            _orbitalAngle += dt * 190f;
            float radius = 2.15f;

            for (int i = 0; i < _orbitalShields.Count; i++)
            {
                float a = (_orbitalAngle + i * (360f / _orbitalShields.Count)) * Mathf.Deg2Rad;
                Vector3 pos = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                _orbitalShields[i].position = pos;

                // Damage enemies & block enemy bullets
                Collider[] cols = Physics.OverlapSphere(pos, 0.55f);
                foreach (var c in cols)
                {
                    var enemy = c.GetComponentInParent<EnemyBase>();
                    if (enemy != null && enemy.IsAlive)
                    {
                        enemy.TakeDamage(18f * dt * 10f, (enemy.transform.position - transform.position).normalized * 4f, false);
                    }
                }
            }

            var projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            foreach (var proj in projectiles)
            {
                if (proj.isPlayerProjectile) continue;
                foreach (var orb in _orbitalShields)
                {
                    if (Vector3.Distance(proj.transform.position, orb.position) < 0.7f)
                    {
                        VfxHelper.SpawnBurst(proj.transform.position, new Color(0.3f, 1f, 0.8f), 5, 5f, 0.14f);
                        Destroy(proj.gameObject);
                        break;
                    }
                }
            }
        }
    }
}
