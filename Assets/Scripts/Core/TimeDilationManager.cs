using System.Collections;
using UnityEngine;

namespace ChronoDrift.Core
{
    /// <summary>
    /// Controls Chrono Focus slow-motion (Shift / E) and brief hit-stop freezes on heavy impacts.
    /// </summary>
    public class TimeDilationManager : MonoBehaviour
    {
        public static TimeDilationManager Instance { get; private set; }

        [Header("Chrono Focus")]
        public float slowTimeScale = 0.26f;
        public float maxEnergy = 100f;
        public float drainPerSecond = 34f;
        public float regenPerSecond = 14f;
        public float transitionSpeed = 14f;

        public float CurrentEnergy { get; private set; }
        public bool IsChronoActive { get; private set; }
        public float NormalizedEnergy => maxEnergy > 0f ? CurrentEnergy / maxEnergy : 0f;

        private float _baseFixedDeltaTime;
        private float _hitStopTimer;
        private float _targetScale = 1f;
        private float _regenBonusMultiplier = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _baseFixedDeltaTime = Time.fixedDeltaTime;
            CurrentEnergy = maxEnergy;
        }

        public void ResetEnergy()
        {
            CurrentEnergy = maxEnergy;
            IsChronoActive = false;
            _hitStopTimer = 0f;
            _regenBonusMultiplier = 1f;
            SetTimeScaleImmediate(1f);
        }

        public void AddEnergy(float amount)
        {
            CurrentEnergy = Mathf.Clamp(CurrentEnergy + amount, 0f, maxEnergy);
        }

        public void MultiplyRegen(float factor)
        {
            _regenBonusMultiplier *= factor;
        }

        public void TriggerHitStop(float unscaledDuration)
        {
            if (unscaledDuration > _hitStopTimer)
                _hitStopTimer = unscaledDuration;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                if (GameManager.Instance != null && (GameManager.Instance.State == GameState.Paused || GameManager.Instance.State == GameState.UpgradeSelect))
                {
                    SetTimeScaleImmediate(0f);
                }
                else
                {
                    SetTimeScaleImmediate(1f);
                }
                IsChronoActive = false;
                return;
            }

            if (_hitStopTimer > 0f)
            {
                _hitStopTimer -= Time.unscaledDeltaTime;
                Time.timeScale = 0.02f;
                Time.fixedDeltaTime = _baseFixedDeltaTime * Time.timeScale;
                return;
            }

            bool wantsChrono = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.E);

            if (wantsChrono && CurrentEnergy > (IsChronoActive ? 1f : 12f))
            {
                IsChronoActive = true;
                CurrentEnergy = Mathf.Max(0f, CurrentEnergy - drainPerSecond * Time.unscaledDeltaTime);
                _targetScale = slowTimeScale;
            }
            else
            {
                IsChronoActive = false;
                CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + regenPerSecond * _regenBonusMultiplier * Time.unscaledDeltaTime);
                _targetScale = 1f;
            }

            float nextScale = Mathf.Lerp(Time.timeScale, _targetScale, Time.unscaledDeltaTime * transitionSpeed);
            SetTimeScaleImmediate(nextScale);
        }

        private void SetTimeScaleImmediate(float scale)
        {
            Time.timeScale = Mathf.Clamp(scale, 0f, 1.5f);
            Time.fixedDeltaTime = _baseFixedDeltaTime * Mathf.Max(0.01f, Time.timeScale);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baseFixedDeltaTime > 0f ? _baseFixedDeltaTime : 0.02f;
        }
    }
}
