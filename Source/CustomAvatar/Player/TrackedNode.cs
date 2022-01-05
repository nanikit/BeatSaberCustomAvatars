using System.Diagnostics.CodeAnalysis;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;

namespace CustomAvatar.Player
{
    internal class TrackedNode : AvatarNode
    {
        private DeviceManager _deviceManager;

        public Transform calibrationOffset { get; set; }

        public Transform manualCalibrationOffset { get; set; }

        [Inject]
        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private void Construct(DeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        private void Start()
        {
            _deviceManager.devicesChanged += OnDevicesChanged;
        }

        private void Update()
        {
            if (_deviceManager.TryGetDeviceState(type, out TrackedDevice device) && device.isTracking)
            {
                transform.localPosition = device.position;
                transform.localRotation = device.rotation;
            }
            else
            {
                isTracking = false;
            }
        }

        private void OnDestroy()
        {
            _deviceManager.devicesChanged -= OnDevicesChanged;
        }

        private void OnDevicesChanged()
        {
            isTracking = _deviceManager.TryGetDeviceState(type, out TrackedDevice _);
        }
    }
}
