﻿//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using CustomAvatar.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;

namespace CustomAvatar.Tracking.OpenVR
{
    internal class OpenVRDeviceProvider : IDeviceProvider
    {
        private static readonly ETrackingResult[] kValidTrackingResults = { ETrackingResult.Running_OK, ETrackingResult.Running_OutOfRange, ETrackingResult.Calibrating_OutOfRange };

        private readonly ILogger<OpenVRDeviceProvider> _logger;
        private readonly OpenVRFacade _openVRFacade;

        private readonly TrackedDevicePose_t[] _poses = new TrackedDevicePose_t[OpenVRFacade.kMaxTrackedDeviceCount];
        private readonly OpenVRDevice[] _devices = new OpenVRDevice[OpenVRFacade.kMaxTrackedDeviceCount];

        public OpenVRDeviceProvider(ILogger<OpenVRDeviceProvider> logger, OpenVRFacade openVRFacade)
        {
            _logger = logger;
            _openVRFacade = openVRFacade;
        }

        public bool GetDevices(Dictionary<string, TrackedDevice> devices)
        {
            devices.Clear();
            bool changeDetected = false;

            _openVRFacade.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, _poses);

            for (uint i = 0; i < _poses.Length; i++)
            {
                OpenVRDevice device = _devices[i];
                TrackedDevicePose_t pose = _poses[i];

                TrackedNodeType use = TrackedNodeType.Unknown;

                string id = device.id;
                bool isConnected = device.isConnected;
                string modelName = device.modelName;

                if (pose.bDeviceIsConnected != isConnected)
                {
                    isConnected = pose.bDeviceIsConnected;

                    if (pose.bDeviceIsConnected)
                    {
                        string serialNumber = _openVRFacade.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_SerialNumber_String);

                        modelName = _openVRFacade.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_ModelNumber_String);
                        id = string.Concat(modelName ?? "Unknown", " ", (uint)serialNumber?.GetHashCode(), "@", i);

                        _logger.Info($"Device '{id}' connected");
                    }
                    else
                    {
                        _logger.Info($"Device '{id}' disconnected");

                        id = null;
                        modelName = null;
                    }

                    changeDetected = true;
                }

                if (!isConnected)
                {
                    _devices[i] = default;
                    continue;
                }

                ETrackedDeviceClass deviceClass = _openVRFacade.GetTrackedDeviceClass(i);
                ETrackedControllerRole controllerRole = _openVRFacade.GetControllerRoleForTrackedDeviceIndex(i);
                string role = _openVRFacade.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_ControllerType_String);

                if (deviceClass != device.deviceClass)
                {
                    _logger.Trace($"Device '{id}' class changed from '{device.deviceClass}' to '{deviceClass}'");

                    changeDetected = true;
                }

                if (controllerRole != device.controllerRole)
                {
                    _logger.Trace($"Device '{id}' controller role changed from '{device.controllerRole}' to '{controllerRole}'");

                    changeDetected = true;
                }

                if (role != device.role)
                {
                    if (role == null)
                    {
                        _logger.Trace($"Device '{id}' role unset from '{device.role}'");
                    }
                    else if (device.role == null)
                    {
                        _logger.Trace($"Device '{id}' role set to '{role}'");
                    }
                    else
                    {
                        _logger.Trace($"Device '{id}' role changed from '{device.role}' to '{role}'");
                    }

                    changeDetected = true;
                }

                switch (deviceClass)
                {
                    case ETrackedDeviceClass.HMD:
                        use = TrackedNodeType.Head;
                        break;

                    case ETrackedDeviceClass.Controller:
                        switch (controllerRole)
                        {
                            case ETrackedControllerRole.LeftHand:
                                use = TrackedNodeType.LeftHand;
                                break;

                            case ETrackedControllerRole.RightHand:
                                use = TrackedNodeType.RightHand;
                                break;
                        }

                        break;

                    case ETrackedDeviceClass.GenericTracker:
                        switch (role)
                        {
                            case "vive_tracker_waist":
                                use = TrackedNodeType.Waist;
                                break;

                            case "vive_tracker_left_foot":
                                use = TrackedNodeType.LeftFoot;
                                break;

                            case "vive_tracker_right_foot":
                                use = TrackedNodeType.RightFoot;
                                break;
                        }

                        break;
                }

                bool isTracking = pose.bPoseIsValid && kValidTrackingResults.Contains(pose.eTrackingResult);

                if (device.isTracking != isTracking)
                {
                    if (isTracking)
                    {
                        _logger.Info($"Acquired tracking of device '{id}'");
                    }
                    else
                    {
                        _logger.Info($"Lost tracking of device '{id}'");
                    }

                    changeDetected = true;
                }

                Vector3 position = Vector3.zero;
                Quaternion rotation = Quaternion.identity;

                if (isTracking)
                {
                    _openVRFacade.GetPositionAndRotation(pose.mDeviceToAbsoluteTracking, out position, out rotation);

                    // Driver4VR rotation correction
                    if (role.StartsWith("d4vr_tracker_") && (use == TrackedNodeType.LeftFoot || use == TrackedNodeType.RightFoot))
                    {
                        rotation *= Quaternion.Euler(-90, 180, 0);
                    }

                    // KinectToVR rotation correction
                    if (role == "kinect_device")
                    {
                        if (use == TrackedNodeType.Waist)
                        {
                            rotation *= Quaternion.Euler(-90, 180, 0);
                        }

                        if (use is TrackedNodeType.LeftFoot or TrackedNodeType.RightFoot)
                        {
                            rotation *= Quaternion.Euler(0, 180, 0);
                        }
                    }
                }

                _devices[i] = new OpenVRDevice(id, isConnected, isTracking, controllerRole, deviceClass, modelName, role);
                devices.Add(id, new TrackedDevice(id, use, isTracking, position, rotation));
            }

            return changeDetected;
        }

        private readonly struct OpenVRDevice
        {
            public readonly string id;
            public readonly bool isConnected;
            public readonly bool isTracking;
            public readonly ETrackedControllerRole controllerRole;
            public readonly ETrackedDeviceClass deviceClass;
            public readonly string modelName;
            public readonly string role;

            public OpenVRDevice(string id, bool isConnected, bool isTracking, ETrackedControllerRole controllerRole, ETrackedDeviceClass deviceClass, string modelName, string role)
            {
                this.id = id;
                this.isConnected = isConnected;
                this.isTracking = isTracking;
                this.controllerRole = controllerRole;
                this.deviceClass = deviceClass;
                this.modelName = modelName;
                this.role = role;
            }
        }
    }
}
