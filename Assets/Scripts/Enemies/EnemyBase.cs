using UnityEngine;
using ChronoDrift.Audio;
using ChronoDrift.Combat;
using ChronoDrift.Core;
using ChronoDrift.Player;
using ChronoDrift.Progression;

namespace ChronoDrift.Enemies
{
    /// <summary>
    /// Base enemy behaviour: health, knockback, hit-flash, death burst, and arena bounds.
    /// </summary>
    public abstract class EnemyBase : MonoBehaviour
    {
        public float maxHp = 40f;
        public int scoreValue = 100;
        public float contactDamageInterval = 0.6f;

        public float CurrentHp { get; protected set; }
        public bool IsAlive => CurrentHp > 0f;
        public virtual bool IsBoss => false;

        protected Transform PlayerTransform;
        protected Renderer MainRenderer;
        protected Color BaseColor;
        private float _hitFlashTimer;
        private Vector3 _knockbackVel;
        private Material _hitFlashMat;
        private Material _normalMat;

        protected virtual void Awake()
        {
            CurrentHp = maxHp;
            _hitFlashMat = MaterialLibrary.GetEmissive("EnemyHitFlash", Color.white, Color.white, 3.2f);
        }

        protected virtual void Start()
        {
            if (PlayerController.Instance != null)
                PlayerTransform = PlayerController.Instance.transform;
        }

        protected void SetNormalMaterial(Material mat)
        {
            _normalMat = mat;
            if (MainRenderer != null) MainRenderer.material = mat;
        }

        public void ScaleStatsForWave(int wave)
        {
            float mult = 1f + (wave - 1) * 0.16f;
            maxHp *= mult;
            CurrentHp = maxHp;
        }

        protected virtual void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            if (PlayerTransform == null && PlayerController.Instance != null)
                PlayerTransform = PlayerController.Instance.transform;

            if (_hitFlashTimer > 0f)
            {
                _hitFlashTimer -= Time.unscaledDeltaTime;
                if (_hitFlashTimer <= 0f && MainRenderer != null && _normalMat != null)
                    MainRenderer.material = _normalMat;
            }

            if (_knockbackVel.sqrMagnitude > 0.01f)
            {
                transform.position += _knockbackVel * Time.deltaTime;
                _knockbackVel = Vector3.Lerp(_knockbackVel, Vector3.zero, Time.deltaTime * 10f);
            }

            TickAI(Time.deltaTime);
            ClampToArena();
        }

        protected abstract void TickAI(float dt);

        public void ApplyExternalPull(Vector3 delta)
        {
            transform.position += delta;
        }

        public virtual void TakeDamage(float amount, Vector3 knockback, bool isCrit)
        {
            if (!IsAlive) return;

            CurrentHp -= amount;
            _knockbackVel += knockback * (IsBoss ? 0.15f : 0.65f);

            if (MainRenderer != null && _hitFlashMat != null)
            {
                MainRenderer.material = _hitFlashMat;
                _hitFlashTimer = 0.07f;
            }

            int dmgInt = Mathf.Max(1, Mathf.RoundToInt(amount));
            DamagePopup.Spawn(
                transform.position,
                isCrit ? $"{dmgInt}!" : dmgInt.ToString(),
                isCrit ? new Color(1f, 0.85f, 0.2f) : Color.white,
                isCrit ? 1.25f : 0.95f
            );

            ProceduralAudio.Instance?.PlayHit();

            if (CurrentHp <= 0f)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            VfxHelper.SpawnBurst(transform.position, BaseColor, IsBoss ? 48 : 20, IsBoss ? 15f : 9f, IsBoss ? 0.35f : 0.22f);
            if (IsBoss)
            {
                VfxHelper.SpawnShockwaveRing(transform.position, BaseColor, 12f, 0.55f);
                CameraController.Instance?.AddTrauma(0.85f);
                TimeDilationManager.Instance?.TriggerHitStop(0.08f);
            }
            else
            {
                CameraController.Instance?.AddTrauma(0.18f);
                TimeDilationManager.Instance?.TriggerHitStop(0.022f);
            }

            ProceduralAudio.Instance?.PlayExplosion();
            GameManager.Instance?.AddKill(scoreValue);
            PlayerController.Instance?.NotifyKill();
            WaveManager.Instance?.NotifyEnemyKilled(this);

            Destroy(gameObject);
        }

        private void ClampToArena()
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, -18.5f, 18.5f);
            p.z = Mathf.Clamp(p.z, -18.5f, 18.5f);
            p.y = IsBoss ? 1.2f : 0.65f;
            transform.position = p;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!IsAlive || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(1);
            }
        }
    }
}
