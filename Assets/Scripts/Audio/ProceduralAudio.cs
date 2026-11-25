using System;
using UnityEngine;
using ChronoDrift.Core;

namespace ChronoDrift.Audio
{
    /// <summary>
    /// Procedural sound synthesizer generating all SFX and an ambient synthwave pulse at runtime.
    /// Zero external audio files required.
    /// </summary>
    public class ProceduralAudio : MonoBehaviour
    {
        public static ProceduralAudio Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioSource _ambientSource;

        private AudioClip _shootClip;
        private AudioClip _ricochetClip;
        private AudioClip _dashClip;
        private AudioClip _hitClip;
        private AudioClip _explosionClip;
        private AudioClip _singularityClip;
        private AudioClip _upgradeClip;
        private AudioClip _playerHitClip;
        private AudioClip _bossWarningClip;
        private AudioClip _ambientClip;
        private AudioClip _blackHoleClip;
        private AudioClip _mineDeployClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = 0.55f;

            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.loop = true;
            _ambientSource.volume = 0.16f;

            GenerateClips();
            _ambientSource.clip = _ambientClip;
            _ambientSource.Play();
        }

        private void Update()
        {
            if (_ambientSource != null)
            {
                float targetPitch = (TimeDilationManager.Instance != null && TimeDilationManager.Instance.IsChronoActive) ? 0.62f : 1.0f;
                _ambientSource.pitch = Mathf.Lerp(_ambientSource.pitch, targetPitch, Time.unscaledDeltaTime * 8f);
            }
        }

        private void GenerateClips()
        {
            _shootClip = CreateSweepClip("SFX_Shoot", 0.11f, 880f, 260f, 0.35f, false);
            _ricochetClip = CreateSweepClip("SFX_Ricochet", 0.14f, 920f, 1680f, 0.42f, false);
            _dashClip = CreateNoiseSweepClip("SFX_Dash", 0.18f, 0.38f);
            _hitClip = CreateSweepClip("SFX_Hit", 0.07f, 420f, 140f, 0.3f, true);
            _explosionClip = CreateNoiseSweepClip("SFX_Explosion", 0.36f, 0.55f);
            _singularityClip = CreateSweepClip("SFX_Singularity", 0.45f, 180f, 620f, 0.45f, false);
            _upgradeClip = CreateChordClip("SFX_Upgrade", 0.48f, new[] { 440f, 554.37f, 659.25f, 880f });
            _playerHitClip = CreateSweepClip("SFX_PlayerHit", 0.25f, 240f, 65f, 0.55f, true);
            _bossWarningClip = CreateSweepClip("SFX_BossWarning", 0.65f, 140f, 95f, 0.5f, true);
            _ambientClip = CreateAmbientPulseClip("Ambient_Synth", 4.0f);
            _blackHoleClip = CreateSweepClip("SFX_BlackHole", 1.1f, 320f, 28f, 0.72f, false);
            _mineDeployClip = CreateSweepClip("SFX_MineDeploy", 0.18f, 740f, 1100f, 0.3f, false);
        }

        public void PlayShoot() => PlayOneShot(_shootClip, 0.45f, UnityEngine.Random.Range(0.95f, 1.08f));
        public void PlayRicochet(int bounce) => PlayOneShot(_ricochetClip, 0.55f, 1.0f + bounce * 0.18f);
        public void PlayDash() => PlayOneShot(_dashClip, 0.6f, UnityEngine.Random.Range(0.95f, 1.1f));
        public void PlayHit() => PlayOneShot(_hitClip, 0.35f, UnityEngine.Random.Range(0.9f, 1.15f));
        public void PlayExplosion() => PlayOneShot(_explosionClip, 0.7f, UnityEngine.Random.Range(0.88f, 1.05f));
        public void PlaySingularity() => PlayOneShot(_singularityClip, 0.65f, 1.0f);
        public void PlayUpgrade() => PlayOneShot(_upgradeClip, 0.65f, 1.0f);
        public void PlayPlayerHit() => PlayOneShot(_playerHitClip, 0.8f, 1.0f);
        public void PlayBossWarning() => PlayOneShot(_bossWarningClip, 0.8f, 1.0f);
        public void PlayBlackHole() => PlayOneShot(_blackHoleClip, 0.85f, 1.0f);
        public void PlayMineDeploy() => PlayOneShot(_mineDeployClip, 0.4f, UnityEngine.Random.Range(0.9f, 1.1f));

        private void PlayOneShot(AudioClip clip, float volume, float pitch)
        {
            if (_sfxSource == null || clip == null) return;
            _sfxSource.pitch = pitch * (TimeDilationManager.Instance != null && TimeDilationManager.Instance.IsChronoActive ? 0.72f : 1f);
            _sfxSource.PlayOneShot(clip, volume);
        }

        private static AudioClip CreateSweepClip(string name, float duration, float startFreq, float endFreq, float amp, bool square)
        {
            int sampleRate = 44100;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];
            double phase = 0.0;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += freq / sampleRate;
                float wave = square ? (Mathf.Sin((float)(phase * Math.PI * 2.0)) >= 0 ? 1f : -1f) : Mathf.Sin((float)(phase * Math.PI * 2.0));
                float env = (1f - t) * (1f - t);
                data[i] = wave * env * amp;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateNoiseSweepClip(string name, float duration, float amp)
        {
            int sampleRate = 44100;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];
            var rng = new System.Random(name.GetHashCode());
            float lowPass = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float cutoff = Mathf.Lerp(0.45f, 0.05f, t);
                lowPass += cutoff * (noise - lowPass);
                float env = (1f - t);
                data[i] = lowPass * env * amp;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateChordClip(string name, float duration, float[] freqs)
        {
            int sampleRate = 44100;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float sum = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    float startOffset = f * 0.08f;
                    if (t >= startOffset)
                    {
                        float localT = (t - startOffset) / (1f - startOffset);
                        sum += Mathf.Sin(2f * Mathf.PI * freqs[f] * (i / (float)sampleRate)) * (1f - localT);
                    }
                }
                data[i] = (sum / freqs.Length) * 0.65f;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateAmbientPulseClip(string name, float duration)
        {
            int sampleRate = 44100;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] data = new float[samples];
            float[] bassNotes = { 55f, 55f, 65.41f, 73.42f }; // A1, A1, C2, D2

            for (int i = 0; i < samples; i++)
            {
                float time = i / (float)sampleRate;
                int bar = Mathf.FloorToInt(time) % bassNotes.Length;
                float freq = bassNotes[bar];
                float pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 2f * time);
                float sub = Mathf.Sin(2f * Mathf.PI * freq * time);
                float harmonic = Mathf.Sin(2f * Mathf.PI * freq * 2f * time) * 0.25f;
                data[i] = (sub + harmonic) * (0.25f + 0.35f * pulse) * 0.4f;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
