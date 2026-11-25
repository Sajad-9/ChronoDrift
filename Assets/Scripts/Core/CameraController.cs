using UnityEngine;

namespace ChronoDrift.Core
{
    /// <summary>
    /// Smooth top-down angled follow camera with mouse look-ahead, trauma screen shake, and FOV kick.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Follow")]
        public Transform target;
        public Vector3 offset = new Vector3(0f, 23f, -11.5f);
        public float followSmooth = 9f;
        public float mouseLookAhead = 2.8f;

        [Header("Shake & FOV")]
        public float maxShakeOffset = 1.15f;
        public float traumaDecay = 2.4f;
        public float baseFov = 54f;

        private float _trauma;
        private float _fovBoost;
        private Camera _cam;

        private void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
            if (_cam != null) baseFov = _cam.fieldOfView;
        }

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        public void AddFovKick(float boost)
        {
            _fovBoost = Mathf.Clamp(_fovBoost + boost, -8f, 12f);
        }

        private void LateUpdate()
        {
            Vector3 desiredPos = offset;
            if (target != null)
            {
                Vector3 lookBias = Vector3.zero;
                if (_cam != null && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
                {
                    Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
                    Plane ground = new Plane(Vector3.up, Vector3.zero);
                    if (ground.Raycast(ray, out float enter))
                    {
                        Vector3 hit = ray.GetPoint(enter);
                        Vector3 delta = hit - target.position;
                        delta.y = 0f;
                        lookBias = Vector3.ClampMagnitude(delta * 0.25f, mouseLookAhead);
                    }
                }
                desiredPos = target.position + offset + lookBias;
            }

            Vector3 smoothed = Vector3.Lerp(transform.position, desiredPos, Time.unscaledDeltaTime * followSmooth);

            if (_trauma > 0f)
            {
                float shake = _trauma * _trauma * maxShakeOffset;
                float t = Time.unscaledTime * 28f;
                Vector3 shakeVec = new Vector3(
                    (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * shake,
                    0f,
                    (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * shake
                );
                smoothed += shakeVec;
                _trauma = Mathf.Max(0f, _trauma - traumaDecay * Time.unscaledDeltaTime);
            }

            transform.position = smoothed;
            transform.rotation = Quaternion.Euler(63f, 0f, 0f);

            if (_cam != null)
            {
                float chronoFov = (TimeDilationManager.Instance != null && TimeDilationManager.Instance.IsChronoActive) ? -4.5f : 0f;
                _fovBoost = Mathf.Lerp(_fovBoost, 0f, Time.unscaledDeltaTime * 7f);
                _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, baseFov + chronoFov + _fovBoost, Time.unscaledDeltaTime * 10f);
            }
        }
    }
}
