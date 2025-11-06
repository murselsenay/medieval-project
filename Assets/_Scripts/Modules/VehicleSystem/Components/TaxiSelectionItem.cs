using Modules.ClientSystem.Models;
using Modules.DriverSystem.Managers;
using Modules.ObjectPoolSystem;
using Modules.TaxiSystem.Models;
using Scriptables.Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace Modules.TaxiSystem.Components
{
    public class TaxiSelectionItem : BaseObject
    {
        [SerializeField] private Image _taxiImage;
        [SerializeField] private TMP_Text _taxiNameText;
        [SerializeField] private TMP_Text _driverNameText;
        [SerializeField] private ButtonAnimate _sendButton;

        private Taxi _taxi;
        private Client _selectedClient;
        public void Init(Taxi taxi, Client selectedClient)
        {
            _taxi = taxi;
            _selectedClient = selectedClient;

            _taxiImage.sprite = taxi.Icon;
            _taxiNameText.text = taxi.Type.ToString();

            SetButton();

            _taxi.Driver = DriverManager.GetAvailableDrivers()[0];

            SetStatus();

        }

        private void SetStatus()
        {
            switch (_taxi.Status)
            {
                case Enums.ETaxiStatus.Available:
                    Available();
                    break;
                case Enums.ETaxiStatus.NoDriver:
                    NoDriver();
                    break;
                case Enums.ETaxiStatus.Busy:
                    Busy();
                    break;
                case Enums.ETaxiStatus.Locked:
                    Locked();
                    break;
            }
        }

        private void Available()
        {
            _sendButton.Activate();
            _taxiImage.material = null;

            _sendButton.SetText("Send");
            _driverNameText.text = _taxi.Driver.DriverName;
        }
        private void NoDriver()
        {
            _sendButton.Deactivate();
            _taxiImage.material = ResourceWarehouse.Instance.GrayscaleUIMaterial;
            _sendButton.SetText("No Driver");

            _driverNameText.text = "";
        }
        private void Busy()
        {
            _sendButton.Deactivate();
            _taxiImage.material = ResourceWarehouse.Instance.GrayscaleUIMaterial;
            _sendButton.SetText("Busy");
            _driverNameText.text = "";
        }
        private void Locked()
        {
            _sendButton.Deactivate();
            _taxiImage.material = ResourceWarehouse.Instance.GrayscaleUIMaterial;
            _sendButton.SetText("Locked");
            _driverNameText.text = "";
        }
        private void SetButton()
        {
            if (_sendButton == null) return;
            _sendButton.RemoveAllListeners();
            _sendButton.AddListener(SendTaxi);
        }

        private void SendTaxi()
        {
            _taxi.Send(_selectedClient);
        }
    }
}
