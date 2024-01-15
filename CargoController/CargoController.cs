// /*
//  * CargoController - A mod for the game Sailwind
//  * Copyright(C) 2023 Jacob Burbach (aka "JakeInABoat")
//  *
//  * This program is free software: you can redistribute it and/or modify it
//  * under the terms of the GNU Lesser General Public License, version 3, as
//  * published by the Free Software Foundation.
//  *
//  * This program is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//  * GNU General Public License for more details.
//  *
//  * You should have received a copy of the GNU Lesser General Public License
//  * along with this program.If not, see <https://www.gnu.org/licenses/>
//  */
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

#if BUILD_UMM
using UnityModManagerNet;
#endif

#if BUILD_BEPINEX
using BepInEx;
#endif


namespace CargoController
{

#if BUILD_UMM
    public static class CargoController
    {
        private static UnityModManager.ModEntry.ModLogger m_logger;
        private static GameObject m_guiObject;

        public static bool Enabled => m_enabled;
        private static bool m_enabled = false;

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            modEntry.OnToggle = OnToggle;
            m_logger = modEntry.Logger;
            m_guiObject = new GameObject();
            m_guiObject.AddComponent<CargoControllerUI>();
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            m_enabled = value;
            return true;
        }

        public static void Log(string message)
        {
            m_logger.Log(message);
        }
    }
#endif

#if BUILD_BEPINEX
    [BepInPlugin(GUID, NAME, VERSION)]
    public class CargoController : BaseUnityPlugin
    {
        public const string GUID = "com.jakeinaboat.cargocontroller";
        public const string NAME = "Cargo Controller";
        public const string VERSION = "0.3.0";

        public static CargoController Instance;

        public static bool Enabled { 
            get {
                if (Instance != null && Instance.IsEnabled()) {
                    return true;
                }
                return false;
            }
        }

        private bool m_enabled;
        private GameObject m_guiObject;

        private void Awake()
        {
            Instance = this;

            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            m_enabled = true;

            m_guiObject = new GameObject();
            m_guiObject.AddComponent<CargoControllerUI>();
        }

        public static void Log(string message)
        {
            Instance.Logger.LogInfo(message);
        }

        public bool IsEnabled()
        {
            return m_enabled;
        }
    }
#endif
}
