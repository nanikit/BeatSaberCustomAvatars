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

using System;
using System.Collections.Generic;
using System.Linq;
using CustomAvatar.Logging;
using CustomAvatar.Tracking;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace CustomAvatar.Avatar
{
    /// <summary>
    /// Allows spawning instances of <see cref="LoadedAvatar"/>.
    /// </summary>
    public class AvatarSpawner
    {
        private readonly DiContainer _container;
        private readonly ILogger<AvatarSpawner> _logger;

        private readonly List<(Type type, Func<AvatarPrefab, bool> condition)> _componentsToAdd = new List<(Type, Func<AvatarPrefab, bool>)>();

        internal AvatarSpawner(DiContainer container, ILogger<AvatarSpawner> logger)
        {
            _container = container;
            _logger = logger;

            RegisterComponent<AvatarIK>(ShouldAddIK);
        }

        public void RegisterComponent<T>(Func<AvatarPrefab, bool> condition = null) where T : MonoBehaviour
        {
            if (IsComponentRegistered<T>()) throw new InvalidOperationException("Registering the same component more than once is not supported");

            _componentsToAdd.Add((typeof(T), condition));
        }

        public bool IsComponentRegistered<T>()
        {
            return _componentsToAdd.Any(vt => vt.type == typeof(T));
        }

        public void DeregisterComponent<T>() where T : MonoBehaviour
        {
            _componentsToAdd.RemoveAll(vt => vt.type == typeof(T));
        }

        /// <summary>
        /// Spawn an <see cref="AvatarPrefab"/>.
        /// </summary>
        /// <param name="avatar">The <see cref="AvatarPrefab"/> to spawn.</param>
        /// <param name="input">The <see cref="IAvatarInput"/> to use.</param>
        /// <param name="parent">The container in which to spawn the avatar (optional).</param>
        /// <returns><see cref="SpawnedAvatar"/></returns>
        public SpawnedAvatar SpawnAvatar(AvatarPrefab avatar, IAvatarInput input, Transform parent = null)
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            if (input == null) throw new ArgumentNullException(nameof(input));

            if (parent)
            {
                _logger.Info($"Spawning avatar '{avatar.descriptor.name}' into '{parent.name}'");
            }
            else
            {
                _logger.Info($"Spawning avatar '{avatar.descriptor.name}'");
            }

            DiContainer subContainer = new(_container);

            subContainer.Bind<IAvatarInput>().FromInstance(input);
            GameObject avatarInstance = Object.Instantiate(avatar, parent, false).gameObject;
            Object.DestroyImmediate(avatarInstance.GetComponent<AvatarPrefab>());
            subContainer.InjectGameObject(avatarInstance);

            subContainer.Bind<AvatarPrefab>().FromInstance(avatar);

            SpawnedAvatar spawnedAvatar = subContainer.InstantiateComponent<SpawnedAvatar>(avatarInstance);

            subContainer.Bind<SpawnedAvatar>().FromInstance(spawnedAvatar);

            foreach ((Type type, Func<AvatarPrefab, bool> condition) in _componentsToAdd)
            {
                if (condition == null || condition(avatar))
                {
                    _logger.Info($"Adding component '{type.FullName}'");
                    subContainer.InstantiateComponent(type, avatarInstance);
                }
            }

            return spawnedAvatar;
        }

        private bool ShouldAddIK(AvatarPrefab avatar)
        {
            return avatar.isIKAvatar;
        }
    }
}
