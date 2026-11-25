using UnityEngine;

namespace ChronoDrift.Combat
{
    /// <summary>
    /// Lightweight world-space floating damage / status popup.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        private TextMesh _text;
        private float _life = 0.65f;
        private float _maxLife = 0.65f;
        private Vector3 _velocity;
        private Color _baseColor;

        public static void Spawn(Vector3 position, string message, Color color, float scale = 1f)
        {
            var go = new GameObject("DmgPopup");
            go.transform.position = position + new Vector3(Random.Range(-0.35f, 0.35f), 1.3f, Random.Range(-0.2f, 0.2f));
            go.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

            var popup = go.AddComponent<DamagePopup>();
            var tm = go.AddComponent<TextMesh>();
            tm.text = message;
            tm.fontSize = 36;
            tm.characterSize = 0.16f * scale;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;

            popup._text = tm;
            popup._baseColor = color;
            popup._velocity = new Vector3(Random.Range(-0.8f, 0.8f), 2.8f, 0.6f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _life -= dt;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += _velocity * dt;
            _velocity.y *= 0.94f;

            float t = _life / _maxLife;
            Color c = _baseColor;
            c.a = Mathf.Clamp01(t * 1.4f);
            _text.color = c;
        }
    }
}
