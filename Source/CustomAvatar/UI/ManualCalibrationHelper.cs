//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
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

using CustomAvatar.Avatar;
using CustomAvatar.Logging;
using CustomAvatar.Player;
using CustomAvatar.Tracking;
using CustomAvatar.Utilities;
using UnityEngine;
using Zenject;

namespace CustomAvatar.UI
{
    internal class ManualCalibrationHelper : MonoBehaviour
    {
        private static readonly int kColor = Shader.PropertyToID("_Color");

        private bool _loaded;

        private ILogger<ManualCalibrationHelper> _logger;
        private ShaderLoader _shaderLoader;
        private PlayerSpaceController _playerSpace;

        private Material _sphereMaterial;
        private Material _rodMaterial;

        private GameObject _waistSphere;
        private GameObject _leftFootSphere;
        private GameObject _rightFootSphere;

        private GameObject _waistRod;
        private GameObject _leftFootRod;
        private GameObject _rightFootRod;

        private void Awake()
        {
            enabled = false;
        }

        [Inject]
        private void Construct(ILogger<ManualCalibrationHelper> logger, ShaderLoader shaderLoader, PlayerSpaceController playerSpace)
        {
            _logger = logger;
            _shaderLoader = shaderLoader;
            _playerSpace = playerSpace;
        }

        private void Start()
        {
            if (_shaderLoader.unlitShader)
            {
                _sphereMaterial = new Material(_shaderLoader.unlitShader);
                _rodMaterial = new Material(_shaderLoader.unlitShader);

                _rodMaterial.SetColor(kColor, new Color(0, 1f, 0, 1));

                _waistSphere = CreateCalibrationSphere();
                _leftFootSphere = CreateCalibrationSphere();
                _rightFootSphere = CreateCalibrationSphere();

                _waistRod = CreateRod();
                _leftFootRod = CreateRod();
                _rightFootRod = CreateRod();

                _loaded = true;
            }
            else
            {
                _logger.Error("Unlit shader not loaded; not spawning calibration indicators");
            }
        }

        private void Update()
        {
            UpdateTrackingMarker(_waistSphere, _waistRod, _playerSpace.waist, _playerSpace.waistReference);
            UpdateTrackingMarker(_leftFootSphere, _leftFootRod, _playerSpace.leftFoot, _playerSpace.leftFootReference);
            UpdateTrackingMarker(_rightFootSphere, _rightFootRod, _playerSpace.rightFoot, _playerSpace.rightFootReference);
        }

        private void OnDisable()
        {
            if (!_loaded) return;

            _waistSphere.SetActive(false);
            _leftFootSphere.SetActive(false);
            _rightFootSphere.SetActive(false);

            _waistRod.SetActive(false);
            _leftFootRod.SetActive(false);
            _rightFootRod.SetActive(false);
        }

        private GameObject CreateCalibrationSphere()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.layer = AvatarLayers.kAlwaysVisible;
            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.GetComponent<Renderer>().material = _sphereMaterial;

            return sphere;
        }

        private GameObject CreateRod()
        {
            GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            rod.transform.localScale = new Vector3(0.01f, 0.5f, 0.01f);
            rod.GetComponent<Renderer>().material = _rodMaterial;

            return rod;
        }

        private void UpdateTrackingMarker(GameObject sphere, GameObject rod, AvatarNode tracker, Transform avatarReference)
        {
            if (tracker.isTracking)
            {
                Vector3 position = tracker.transform.position;
                Quaternion rotation = tracker.transform.rotation;

                sphere.SetActive(true);
                sphere.transform.SetPositionAndRotation(position, rotation);

                Vector3 trackerToPoint = position - avatarReference.position;
                Vector3 pivot = (position + avatarReference.position) * 0.5f;
                Vector3 localScale = rod.transform.localScale;
                rod.SetActive(true);
                rod.transform.SetPositionAndRotation(pivot, Quaternion.LookRotation(trackerToPoint) * Quaternion.Euler(90, 0, 0));
                rod.transform.localScale = new Vector3(localScale.x, trackerToPoint.magnitude * 0.5f, localScale.z);
            }
            else
            {
                sphere.SetActive(false);
                rod.SetActive(false);
            }
        }
    }
}
