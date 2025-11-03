using System;
using UnityEngine;

namespace Modules.ObjectPoolSystem
{
    public class BaseObject : MonoBehaviour
    {
        [SerializeField] private string _key;
        public virtual string Key => _key;

        public event Action<BaseObject> Awaken;
        public event Action<BaseObject> Activated;
        public event Action<BaseObject> Enabled;
        public event Action<BaseObject> Started;
        public event Action<BaseObject> DeActivated;
        public event Action<BaseObject> Disabled;
        public event Action<BaseObject> Destroyed;

        private Transform _poolHolder;
        protected virtual void Awake()
        {
            Awaken?.Invoke(this);
        }

        protected virtual void OnEnable()
        {
            Enabled?.Invoke(this);
        }

        protected virtual void Start()
        {
            Started?.Invoke(this);
        }

        protected virtual void OnDisable()
        {
            Disabled?.Invoke(this);
        }

        protected virtual void OnDestroy()
        {
            Destroyed?.Invoke(this);
        }

        public virtual void Activate()
        {
            gameObject.SetActive(true);
            // Reset transforms to safe defaults when reusing from pool
            transform.localScale = Vector3.one;
            transform.localEulerAngles = new Vector3(0,0,0);
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.zero;
            Activated?.Invoke(this);
        }


        public void DeactivateInvoker()
        {
            DeActivated?.Invoke(this);
        }

        public void ActivateInvoker()
        {
            Activated?.Invoke(this);
        }
        public virtual void Deactivate()
        {
            DeActivated?.Invoke(this);

            // Ensure we have a reference to the pool holder and reset transforms before parenting
            if (_poolHolder == null)
            {
                var ph = GameObject.Find("PoolHolder");
                if (ph != null)
                    _poolHolder = ph.transform;
            }

            // Reset local transforms to safe defaults so pooled objects are stored consistently
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            transform.localEulerAngles = Vector3.zero;
            transform.localPosition = Vector3.zero;

            if (_poolHolder == null)
                transform.SetParent(GameObject.Find("PoolHolder")?.transform);
            else
                transform.SetParent(_poolHolder, false);

            gameObject.SetActive(false);
        }
    }
}
