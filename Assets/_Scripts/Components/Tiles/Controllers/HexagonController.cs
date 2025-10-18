using Components.Tiles.Enums;
using Modules.ObjectPoolSystem;
using NaughtyAttributes;
using Scriptables.Singletons;
using UnityEngine;

namespace Components.Tiles.Controllers
{
    public class HexagonController : BaseObject
    {
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private EHexagonState _state;
        public EHexagonState State
        {
            get { return _state; }
            set
            {
                _state = value;

                switch (_state)
                {
                    case EHexagonState.Enabled:
                        ActivateHexagon();
                        break;
                    case EHexagonState.Disabled:
                        DeactivateHexagon();
                        break;
                    case EHexagonState.Spawn:
                        SpawnHexagon();
                        break;
                }
            }
        }

        public void SpawnHexagon()
        {
            var lightGreen = ResourceWarehouse.Instance.HexagonLightGreenMaterial;

            _renderer.sharedMaterials = new Material[] { lightGreen, lightGreen };

            _state = EHexagonState.Spawn;
        }
        public void ActivateHexagon()
        {
            var mat0 = ResourceWarehouse.Instance.HexagonGreenMaterial;
            var mat1 = ResourceWarehouse.Instance.HexagonBrownMaterial;

            _renderer.sharedMaterials = new Material[] { mat0, mat1 };

            _state = EHexagonState.Enabled;
        }

        public void DeactivateHexagon()
        {
            var gray = ResourceWarehouse.Instance.HexagonGrayMaterial;

            _renderer.sharedMaterials = new Material[] { gray, gray };

            _state = EHexagonState.Disabled;
        }
    }
}