using UnityEngine;
namespace Components.General
{
    [RequireComponent(typeof(LineRenderer))]
    public class RangeCircle : MonoBehaviour
    {
        [SerializeField] private LineRenderer _line;

        private int _segments = 64;
        private float _radius = 2f;

        private void Awake()
        {
            if (_line == null)
                _line = GetComponent<LineRenderer>();
        }

        void Start()
        {
            if (_line == null)
                return; // safety

            _line.useWorldSpace = false;
            _line.loop = true;

            // Use segments + 1 so we can duplicate the first point at the end and avoid index OOB
            _segments = Mathf.Max(3, _segments);
            _line.positionCount = _segments + 1;

            DrawCircle();

            // keep range hidden by default; will be enabled when requested
            if (_line != null)
                _line.enabled = false;
        }

        private void DrawCircle()
        {
            // draw points and duplicate the first point at the end to close the loop
            for (int i = 0; i <= _segments; i++)
            {
                // using modulo so the last point equals the first
                float t = (float)(i % _segments) / _segments;
                float angle = t * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * _radius;
                float y = Mathf.Sin(angle) * _radius;
                _line.SetPosition(i, new Vector3(x, 0f, y));
            }
        }

        public void SetRadius(float newRadius)
        {
            _radius = newRadius;
            DrawCircle();
        }

        // New: control visibility of the rendered range
        public void SetVisible(bool visible)
        {
            if (_line == null) return;
            _line.enabled = visible;
        }
    }
}