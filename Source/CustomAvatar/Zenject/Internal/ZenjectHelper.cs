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
using HarmonyLib;
using IPA.Utilities;
using UnityEngine;
using Zenject;

#if DEBUG
using System.Diagnostics;
#endif

namespace CustomAvatar.Zenject.Internal
{
    internal class ZenjectHelper
    {
        private const string kExpectedFirstSceneContextName = "AppCoreSceneContext";

        private static readonly FieldAccessor<SceneContext, List<SceneDecoratorContext>>.Accessor kDecoratorContextsAccessor = FieldAccessor<SceneContext, List<SceneDecoratorContext>>.GetAccessor("_decoratorContexts");
        private static readonly FieldAccessor<SceneDecoratorContext, List<MonoBehaviour>>.Accessor kInjectableMonoBehavioursAccessor = FieldAccessor<SceneDecoratorContext, List<MonoBehaviour>>.GetAccessor("_injectableMonoBehaviours");

        private static bool _shouldInstall;

        private static readonly HashSet<InstallerRegistration> kInstallerRegistrations = new HashSet<InstallerRegistration>();
        private static readonly HashSet<Type> kComponentsToBind = new HashSet<Type>();
        private static readonly Dictionary<Type, List<ComponentRegistration>> kComponentsToAdd = new Dictionary<Type, List<ComponentRegistration>>();

        private static ILogger<ZenjectHelper> _logger;

        internal static void Init(IPA.Logging.Logger logger)
        {
            _logger = new IPALogger<ZenjectHelper>(logger);
        }

        public static InstallerRegistration Register<TInstaller>() where TInstaller : Installer
        {
            var registration = new InstallerRegistration(typeof(TInstaller));

            kInstallerRegistrations.Add(registration);

            return registration;
        }

        public static void BindSceneComponent<T>() where T : MonoBehaviour
        {
            kComponentsToBind.Add(typeof(T));
        }

        public static void AddComponentAlongsideExisting<TExisting, TAdd>(string childTransformName = null, Func<GameObject, bool> condition = null, Action<DiContainer, TAdd> setupFunction = null, params object[] extraArgs) where TExisting : MonoBehaviour where TAdd : MonoBehaviour
        {
            var componentRegistration = new ComponentRegistration(typeof(TAdd), childTransformName, condition, setupFunction != null ? new Action<DiContainer, object>((c, t) => setupFunction(c, (TAdd)t)) : null, extraArgs);

            if (kComponentsToAdd.TryGetValue(typeof(TExisting), out List<ComponentRegistration> types))
            {
                types.Add(componentRegistration);
            }
            else
            {
                kComponentsToAdd.Add(typeof(TExisting), new List<ComponentRegistration> { componentRegistration });
            }
        }

        private static void InstallInstallers(Context __instance)
        {
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif

            if (!_shouldInstall)
            {
                if (__instance.name == kExpectedFirstSceneContextName)
                {
                    _shouldInstall = true;
                }
                else
                {
                    if (!(__instance is ProjectContext))
                    {
                        _logger.Warning($"Ignoring {__instance.GetType().Name} '{__instance.name}' since SceneContext '{kExpectedFirstSceneContextName}' hasn't loaded yet");
                    }

                    return;
                }
            }

            _logger.Trace($"Handling {__instance.GetType().Name} '{__instance.name}' (scene '{__instance.gameObject.scene.name}')");

            foreach (MonoInstaller installer in __instance.Installers)
            {
                BindIfNeeded(__instance, installer);
            }

            foreach (InstallerRegistration installerRegistration in kInstallerRegistrations)
            {
                if (installerRegistration.TryInstallInto(__instance))
                {
                    _logger.Trace($"Installed {installerRegistration.installer.FullName}");
                }
            }

#if DEBUG
            _logger.Trace($"InstallInstallers: {stopwatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000)} us");
#endif
        }

        private static void InstallBindings(Context __instance, List<MonoBehaviour> injectableMonoBehaviours)
        {
            if (!_shouldInstall) return;

#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif

            if (__instance is SceneContext sceneContext)
            {
                injectableMonoBehaviours.AddRange(kDecoratorContextsAccessor(ref sceneContext).SelectMany(dc => kInjectableMonoBehavioursAccessor(ref dc)));
            }

            foreach (MonoBehaviour monoBehaviour in injectableMonoBehaviours)
            {
                BindIfNeeded(__instance, monoBehaviour);
                AddComponents(__instance, monoBehaviour);
            }

#if DEBUG
            _logger.Trace($"InstallBindings: {stopwatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000)} us");
#endif
        }

        private static void BindIfNeeded(Context context, MonoBehaviour monoBehaviour)
        {
            Type type = monoBehaviour.GetType();

            if (!kComponentsToBind.Contains(type)) return;

            if (!context.Container.HasBinding(type))
            {
                _logger.Trace($"Binding '{type.FullName}' from {context.GetType().Name} '{context.name}' (scene '{context.gameObject.scene.name}')");

                context.Container.Bind(type).FromInstance(monoBehaviour).AsSingle().IfNotBound();
            }
            else
            {
                _logger.Trace($"'{type.FullName}' is already bound on {context.GetType().Name} '{context.name}' (scene '{context.gameObject.scene.name}')");
            }
        }

        private static void AddComponents(Context context, MonoBehaviour monoBehaviour)
        {
            Type monoBehaviourType = monoBehaviour.GetType();

            if (!kComponentsToAdd.TryGetValue(monoBehaviourType, out List<ComponentRegistration> componentsToAdd)) return;

            foreach (ComponentRegistration componentRegistration in componentsToAdd)
            {
                GameObject target = monoBehaviour.gameObject;

                if (!string.IsNullOrEmpty(componentRegistration.childTransformName))
                {
                    Transform transform = target.transform.Find(componentRegistration.childTransformName);

                    if (!transform)
                    {
                        _logger.Warning($"Could not find transform '{componentRegistration.childTransformName}' under '{target.name}'");
                        continue;
                    }

                    target = transform.gameObject;
                }

                if (componentRegistration.condition != null && !componentRegistration.condition(target))
                {
                    _logger.Trace($"Condition not met for putting '{componentRegistration.type.FullName}' onto '{target.name}'");
                    continue;
                }

                _logger.Trace($"Adding '{componentRegistration.type.FullName}' to GameObject '{target.name}' (for '{monoBehaviourType.FullName}')");
                object component = context.Container.InstantiateComponent(componentRegistration.type, target, componentRegistration.extraArgs);
                componentRegistration.setupCallback?.Invoke(context.Container, component);
            }
        }

        private class ComponentRegistration
        {
            public Type type { get; }

            public string childTransformName { get; }

            public Func<GameObject, bool> condition { get; }

            public Action<DiContainer, object> setupCallback { get; }

            public object[] extraArgs { get; }

            public ComponentRegistration(Type type, string childTransformName, Func<GameObject, bool> condition, Action<DiContainer, object> setupCallback, object[] extraArgs)
            {
                this.type = type;
                this.childTransformName = childTransformName;
                this.condition = condition;
                this.setupCallback = setupCallback;
                this.extraArgs = extraArgs;
            }
        }

        [HarmonyPatch(typeof(Context), "InstallInstallers", new Type[0])]
        internal static class Context_InstallInstallers
        {
            public static void Postfix(Context __instance)
            {
                InstallInstallers(__instance);
            }
        }

        [HarmonyPatch(typeof(ProjectContext), "InstallBindings")]
        private static class ProjectContext_InstallBindings
        {
            public static void Postfix(ProjectContext __instance, List<MonoBehaviour> injectableMonoBehaviours)
            {
                InstallBindings(__instance, injectableMonoBehaviours);
            }
        }

        [HarmonyPatch(typeof(SceneContext), "InstallBindings")]
        private static class SceneContext_InstallBindings
        {
            public static void Postfix(SceneContext __instance, List<MonoBehaviour> injectableMonoBehaviours)
            {
                InstallBindings(__instance, injectableMonoBehaviours);
            }
        }

        [HarmonyPatch(typeof(GameObjectContext), "InstallBindings")]
        private static class GameObjectContext_InstallBindings
        {
            public static void Postfix(GameObjectContext __instance, List<MonoBehaviour> injectableMonoBehaviours)
            {
                InstallBindings(__instance, injectableMonoBehaviours);
            }
        }
    }
}
