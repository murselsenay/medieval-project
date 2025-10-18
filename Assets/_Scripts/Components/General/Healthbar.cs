using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Components.General
{
    public class Healthbar : MonoBehaviour
    {
        [SerializeField] private Slider _healthbarGreen;

        private float _max = 100f;
        private float _current = 100f;

        public void Initialize(float max)
        {
            _max = Mathf.Max(1f, max);
            _current = _max;
            if (_healthbarGreen != null)
            {
                _healthbarGreen.maxValue = _max;
                _healthbarGreen.value = _current;
            }
        }

        public void Initialize(float max, float current)
        {
            _max = Mathf.Max(1f, max);
            _current = Mathf.Clamp(current, 0f, _max);
            if (_healthbarGreen != null)
            {
                _healthbarGreen.maxValue = _max;
                _healthbarGreen.value = _current;
            }
        }

        public void SetCurrent(float current)
        {
            _current = Mathf.Clamp(current, 0f, _max);
            if (_healthbarGreen != null)
                _healthbarGreen.value = _current;
        }

        public void ApplyDamage(float amount)
        {
            SetCurrent(_current - amount);
        }
    }
}
