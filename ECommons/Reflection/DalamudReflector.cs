﻿using Dalamud;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using Dalamud.Plugin;
using ECommons.DalamudServices;
using ECommons.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ECommons.Reflection
{
    public static class DalamudReflector
    {
        delegate ref int GetRefValue(int vkCode);
        static GetRefValue getRefValue;
        static Dictionary<string, dynamic> pluginCache;
        static List<Action> onPluginsChangedActions;

        internal static void Init()
        {
            onPluginsChangedActions = new();
            pluginCache = new();
            GenericHelpers.Safe(delegate
            {
                getRefValue = (GetRefValue)Delegate.CreateDelegate(typeof(GetRefValue), Svc.KeyState,
                            Svc.KeyState.GetType().GetMethod("GetRefValue",
                            BindingFlags.NonPublic | BindingFlags.Instance,
                            null, new Type[] { typeof(int) }, null));
            });
            GenericHelpers.Safe(delegate
            {
                var pm = GetPluginManager();
                pm.GetType().GetEvent("OnInstalledPluginsChanged").AddEventHandler(pm, OnInstalledPluginsChanged);
            });
        }

        internal static void Dispose()
        {
            if (pluginCache != null)
            {
                pluginCache?.Clear();
                onPluginsChangedActions?.Clear();
                GenericHelpers.Safe(delegate
                {
                    var pm = GetPluginManager();
                    pm.GetType().GetEvent("OnInstalledPluginsChanged").RemoveEventHandler(pm, OnInstalledPluginsChanged);
                });
            }
        }

        public static void RegisterOnInstalledPluginsChangedEvents(params Action[] actions)
        {
            foreach(var x in actions)
            {
                onPluginsChangedActions.Add(x);
            }
        }

        public static void SetKeyState(VirtualKey key, int state)
        {
            getRefValue((int)key) = state;
        }

        public static object GetPluginManager()
        {
            return Svc.PluginInterface.GetType().Assembly.
                    GetType("Dalamud.Service`1", true).MakeGenericType(Svc.PluginInterface.GetType().Assembly.GetType("Dalamud.Plugin.Internal.PluginManager", true)).
                    GetMethod("Get").Invoke(null, BindingFlags.Default, null, Array.Empty<object>(), null);
        }

        public static object GetService(string serviceFullName)
        {
            return Svc.PluginInterface.GetType().Assembly.
                    GetType("Dalamud.Service`1", true).MakeGenericType(Svc.PluginInterface.GetType().Assembly.GetType(serviceFullName, true)).
                    GetMethod("Get").Invoke(null, BindingFlags.Default, null, Array.Empty<object>(), null);
        }

        public static bool TryGetDalamudPlugin(string internalName, out dynamic instance, bool suppressErrors = false)
        {
            if(pluginCache.TryGetValue(internalName, out instance))
            {
                return true;
            }
            try
            {
                var pluginManager = GetPluginManager();
                var installedPlugins = (System.Collections.IList)pluginManager.GetType().GetProperty("InstalledPlugins").GetValue(pluginManager);

                foreach (var t in installedPlugins)
                {
                    if ((string)t.GetType().GetProperty("Name").GetValue(t) == internalName)
                    {
                        var type = t.GetType().Name == "LocalDevPlugin" ? t.GetType().BaseType : t.GetType();
                        var plugin = (dynamic)type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(t);
                        instance = plugin;
                        pluginCache[internalName] = plugin;
                        return true;
                    }
                }
                instance = null;
                return false;
            }
            catch (Exception e)
            {
                if (!suppressErrors)
                {
                    PluginLog.Error($"Can't find {internalName} plugin: " + e.Message);
                    PluginLog.Error(e.StackTrace);
                }
                instance = null;
                return false;
            }
        }

        public static bool TryGetDalamudStartInfo(out DalamudStartInfo dalamudStartInfo, DalamudPluginInterface pluginInterface = null)
        {
            try
            {
                if (pluginInterface == null) pluginInterface = Svc.PluginInterface;
                var info = pluginInterface.GetType().Assembly.
                        GetType("Dalamud.Service`1", true).MakeGenericType(typeof(DalamudStartInfo)).
                        GetMethod("Get").Invoke(null, BindingFlags.Default, null, Array.Empty<object>(), null);
                dalamudStartInfo = (DalamudStartInfo)info;
                return true;
            }
            catch (Exception e)
            {
                PluginLog.Error($"{e.Message}\n{e.StackTrace ?? ""}");
                dalamudStartInfo = default;
                return false;
            }
        }

        static string pluginName = null;
        public static string GetPluginName()
        {
            GenericHelpers.Safe(delegate
            {
                pluginName ??= (string)Svc.PluginInterface.GetType().GetField("pluginName", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Svc.PluginInterface);
            });
            return pluginName;
        }

        internal static void OnInstalledPluginsChanged()
        {
            PluginLog.Verbose("Installed plugins changed event fired");
            _ = new TickScheduler(delegate
            {
                pluginCache.Clear();
                foreach(var x in onPluginsChangedActions)
                {
                    x();
                }
            });
        }
    }
}
