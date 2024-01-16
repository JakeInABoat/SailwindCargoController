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
using HarmonyLib;
using UnityEngine;


namespace CargoController
{
    [HarmonyPatch(typeof(LookUI), "RegisterPointer")]
    static class LookUI_RegisterPointer
    {
         static void Postfix(GoPointer ___pointer)
         {
            CargoController.Log("LookUI_RegisterPointer");
            CargoControllerUI.SetPointer(___pointer);
         }
    }

    [HarmonyPatch(typeof(PlayerNeedsUI), "ToggleInventory")]
    static class PlayerNeeds_ToggleInventory
    {
        static void Postfix(bool state)
        {
            CargoController.Log("PlayerNeedsUI_ToggleInventory");
            CargoControllerUI.Instance?.SetVisible(state);
        }
    }

    [HarmonyPatch(typeof(ShipItem), "DestroyItem")]
    static class ShipItem_DestroyItem
    {
        static void Prefix(ShipItem __instance)
        {
            IslandMarketWarehouseAreaTracker.OnDestroyShipItem(__instance);
        }
    }

    [HarmonyPatch(typeof(IslandMarketWarehouseArea), "OnTriggerExit")]
    static class IslandMarketWarehouseArea_OnTriggerExit
    {
        static void Postfix(IslandMarketWarehouseArea __instance, Collider other)
        {
            IslandMarketWarehouseAreaTracker.OnWarehouseExit(__instance, other);
        }
    }

    [HarmonyPatch(typeof(IslandMarketWarehouseArea), "OnTriggerEnter")]
    static class IslandMarketWarehouseArea_OnTriggerEnter
    {
        static void Postfix(IslandMarketWarehouseArea __instance, Collider other)
        {
            IslandMarketWarehouseAreaTracker.OnWarehouseEnter(__instance, other);
        }
    }
}