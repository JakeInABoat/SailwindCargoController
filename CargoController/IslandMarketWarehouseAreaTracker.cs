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
using System.Collections.Generic;
using UnityEngine;


namespace CargoController
{
    public static class IslandMarketWarehouseAreaTracker
    {
        public static List<Good> allGoodsInArea = new List<Good>();
        public static List<Good> missionGoodsInArea = new List<Good>();
        public static List<Good> nonMissionGoodsInArea = new List<Good>();
        private static List<Good> destroyedGoods = new List<Good>();
        private static IslandMarketWarehouseArea currentWarehouseArea = null;

        static void Clear()
        {
            allGoodsInArea.Clear();
            missionGoodsInArea.Clear();
            nonMissionGoodsInArea.Clear();
        }

        static void CheckWarehouseChanged(IslandMarketWarehouseArea area)
        {
            if (currentWarehouseArea == area)
                return;

            CargoController.Log(
                "Warehouse changed from '" + currentWarehouseArea?.name + "' to '" + area.name + "'");

            Clear();
            currentWarehouseArea = area;
        }

        static public void OnWarehouseEnter(IslandMarketWarehouseArea warehouse, Collider other)
        {
            Good good = other.GetComponent<Good>();
            if (good == null || !good.GetComponent<ShipItem>().sold) {
                return;
            }

            if (destroyedGoods.Contains(good)) {
                destroyedGoods.Remove(good);
                return;
            }

            CheckWarehouseChanged(warehouse);

            if (!allGoodsInArea.Contains(good)) {
                allGoodsInArea.Add(good);
            }

            if (good.GetMissionIndex() == -1 && !nonMissionGoodsInArea.Contains(good)) {
                nonMissionGoodsInArea.Add(good);
            }

            if (good.GetMissionIndex() != -1 && !missionGoodsInArea.Contains(good)) {
                missionGoodsInArea.Add(good);
            }

            CargoController.Log("OnTriggerEnter: " + allGoodsInArea.Count.ToString());
        }

        static public void OnWarehouseExit(IslandMarketWarehouseArea warehouse, Collider other)
        {
            Good good = other.GetComponent<Good>();
            if (good == null) {
                return;
            }

            CheckWarehouseChanged(warehouse);

            allGoodsInArea.Remove(good);
            missionGoodsInArea.Remove(good);
            nonMissionGoodsInArea.Remove(good);

            CargoController.Log("OnTriggerExit: " + allGoodsInArea.Count.ToString());
        }

        static public void OnDestroyShipItem(ShipItem item)
        {
            Good good = item.GetComponent<Good>();
            if (good == null)
                return;

            allGoodsInArea.Remove(good);
            nonMissionGoodsInArea.Remove(good);
            missionGoodsInArea.Remove(good);
            destroyedGoods.Add(good);

            CargoController.Log("OnDestroyShipItem => " + good.name);
        }
    }
}