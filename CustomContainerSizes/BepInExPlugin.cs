﻿using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CustomContainerSizes
{
    [BepInPlugin("aedenthorn.CustomContainerSizes", "Custom Container Sizes", "0.3.2")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<int> chestWidth;
        private static ConfigEntry<int> chestHeight;
        private static ConfigEntry<int> vikingShipChestWidth;
        private static ConfigEntry<int> vikingShipChestHeight;
        private static ConfigEntry<int> privateChestWidth;
        private static ConfigEntry<int> privateChestHeight;
        private static ConfigEntry<int> reinforcedChestWidth;
        private static ConfigEntry<int> reinforcedChestHeight;
        private static ConfigEntry<int> karveChestWidth;
        private static ConfigEntry<int> karveChestHeight;
        private static ConfigEntry<int> wagonWidth;
        private static ConfigEntry<int> wagonHeight;
        private static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            chestWidth = Config.Bind<int>("Sizes", "ChestWidth", 8, "Number of items wide for chest containers");
            chestHeight = Config.Bind<int>("Sizes", "ChestHeight", 4, "Number of items tall for chest containers");
            karveChestWidth = Config.Bind<int>("Sizes", "KarveChestWidth", 6, "Number of items wide for Karve chest containers");
            karveChestHeight = Config.Bind<int>("Sizes", "KarveChestHeight", 3, "Number of items tall for karve chest containers");
            vikingShipChestWidth = Config.Bind<int>("Sizes", "VikingShipChestWidth", 8, "Number of items wide for longship chest containers");
            vikingShipChestHeight = Config.Bind<int>("Sizes", "VikingShipChestHeight", 4, "Number of items tall for longship chest containers");
            privateChestWidth = Config.Bind<int>("Sizes", "PrivateChestWidth", 6, "Number of items wide for private chest containers");
            privateChestHeight = Config.Bind<int>("Sizes", "PrivateChestHeight", 3, "Number of items tall for private chest containers");
            reinforcedChestWidth = Config.Bind<int>("Sizes", "ReinforcedChestWidth", 8, "Number of items wide for reinforced chest containers");
            reinforcedChestHeight = Config.Bind<int>("Sizes", "ReinforcedChestHeight", 8, "Number of items tall for reinforced chest containers");
            wagonWidth = Config.Bind<int>("Sizes", "WagonWidth", 8, "Number of items wide for chest containers");
            wagonHeight = Config.Bind<int>("Sizes", "WagonHeight", 4, "Number of items tall for chest containers");
            nexusID = Config.Bind<int>("General", "NexusID", 111, "Mod ID on the Nexus for update checks");
            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Container), "Awake")]
        static class Container_Awake_Patch
        {
            static void Postfix(Container __instance, Inventory ___m_inventory)
            {
                if (___m_inventory == null)
                    return;

                Dbgl($"spawning container {__instance.name}, parent {__instance.gameObject.transform.parent.name}");
                Ship ship = __instance.gameObject.transform.parent.GetComponent<Ship>();
                if (ship != null)
                {
                    Dbgl($"container is on a ship: {ship.name}");
                    if (ship.name.ToLower().Contains("karve"))
                    {
                        Dbgl($"setting Karve chest size to {karveChestWidth.Value},{karveChestHeight.Value}");

                        typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, karveChestWidth.Value);
                        typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, karveChestHeight.Value);
                    }
                    else if (ship.name.ToLower().Contains("vikingship"))
                    {
                        Dbgl($"setting VikingShip chest size to {vikingShipChestWidth.Value},{vikingShipChestHeight.Value}");

                        typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, vikingShipChestWidth.Value);
                        typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, vikingShipChestHeight.Value);
                    }
                }
                else if (__instance.name.StartsWith("piece_chest_wood"))
                {
                    Dbgl($"setting chest size to {chestWidth.Value},{chestHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, chestWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, chestHeight.Value);
                }
                else if (__instance.name.StartsWith("piece_chest_private"))
                {
                    Dbgl($"setting private chest size to {privateChestWidth.Value},{privateChestHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, privateChestWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, privateChestHeight.Value);
                }
                else if (__instance.name.StartsWith("piece_chest"))
                {
                    Dbgl($"setting reinforced chest size to {reinforcedChestWidth.Value},{reinforcedChestHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, reinforcedChestWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, reinforcedChestHeight.Value);
                }
                else if (__instance.m_wagon)
                {
                    Dbgl($"setting wagon size to {wagonWidth.Value},{wagonHeight.Value}");

                    typeof(Inventory).GetField("m_width", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, wagonWidth.Value);
                    typeof(Inventory).GetField("m_height", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(___m_inventory, wagonHeight.Value);
                }
            }
        }
    }
}