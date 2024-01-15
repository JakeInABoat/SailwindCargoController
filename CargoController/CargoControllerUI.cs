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
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;


namespace CargoController
{
    public class CargoControllerUI : MonoBehaviour
    {
        private float timeUntilPortCheck = 1.0f;
        private Port nearestPort = null;
        private float nearestPortDistance = 100000000.0f;
        private float maximumPortDistance = 75.0f;
        private Font immortalFont = null;
        private Font architectsFont = null;
        private Texture2D darkBrownTexture = null;
        private Texture2D lightBrownTexture = null;
        private Texture2D reddishTexture = null;
        private bool m_visible;

        public static GoPointer pointer = null;

        public static CargoControllerUI Instance;


        bool ValidateGood(Good good)
        {
            if (good == null) {
                return false;
            }

            ShipItemCrate crate = good.GetComponent<ShipItemCrate>();
            ShipItemBottle bottle = good.GetComponent<ShipItemBottle>();

            if (crate != null) {
                float amount = PrefabsDirectory.instance.directory[good.GetComponent<SaveablePrefab>().prefabIndex].GetComponent<ShipItemCrate>().amount;
                if (crate.amount < amount) {
                    return false;
                }
            }

            if (bottle != null) {
                float health = PrefabsDirectory.instance.directory[good.GetComponent<SaveablePrefab>().prefabIndex].GetComponent<ShipItemBottle>().health;
                if (bottle.health < health) {
                    return false;
                }
            }

            return true;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            foreach (Font font in Resources.FindObjectsOfTypeAll<Font>()) {
                if (font.name == "IMMORTAL") {
                    immortalFont = font;
                }
                else if (font.name == "ArchitectsDaughter") {
                    architectsFont = font;
                }
            }

            Color darkBrownColor = new Color(144.0f / 255.0f, 120.0f / 255.0f, 97.0f / 255.0f);
            Color lightBrownColor = new Color(230.0f / 255.0f, 187.0f / 255.0f, 156.0f / 255.0f);
            Color reddishColor = new Color(153.0f / 255.0f, 103.0f / 255.0f, 93.0f / 255.0f);
            darkBrownTexture = new Texture2D(2, 2);
            lightBrownTexture = new Texture2D(2, 2);
            reddishTexture = new Texture2D(2, 2);
            for (int x = 0; x < 2; ++x) {
                for (int y = 0; y < 2; ++y) {
                    darkBrownTexture.SetPixel(x, y, darkBrownColor);
                    lightBrownTexture.SetPixel(x, y, lightBrownColor);
                    reddishTexture.SetPixel(x, y, reddishColor);
                }
            }
            darkBrownTexture.Apply();
            lightBrownTexture.Apply();
            reddishTexture.Apply();
        }

        private void FixedUpdate()
        {
            timeUntilPortCheck -= Time.deltaTime;
            if (timeUntilPortCheck > 0) {
                return;
            }

            timeUntilPortCheck = 5.0f;

            nearestPort = null;
            nearestPortDistance = 10000000.0f;

            foreach (Port p in Port.ports) {
                if (p == null)
                    continue;

                float d = Vector3.Distance(p.transform.position, Refs.observerMirror.transform.position);
                if (d < nearestPortDistance) {
                    nearestPortDistance = d;
                    nearestPort = p;
                }
            }
        }

        private void ApplyGuiStyles()
        {
            GUI.skin.label.font = immortalFont;
            GUI.skin.label.fontSize = 18;
            GUI.skin.label.normal.textColor = Color.black;
            GUI.skin.label.normal.background = reddishTexture;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;

            GUI.skin.button.font = architectsFont;
            GUI.skin.button.fontSize = 14;
            GUI.skin.button.normal.background = lightBrownTexture;
            GUI.skin.button.normal.textColor = Color.black;
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
        }

        private void VerticalStart()
        {
            GUIStyle style = new GUIStyle();
            style.normal.background = darkBrownTexture;
            style.margin.top = 5;
            style.margin.left = 5;
            GUILayout.BeginVertical(style, GUILayout.MinWidth(250));
        }

        private void VerticalEnd()
        {
            GUILayout.EndVertical();
        }

        private void OnGUI()
        {
            if (!CargoController.Enabled || !m_visible) {
                return;
            }

            ApplyGuiStyles();

            DoBoatGoodsUI();
            DoPortGoodsUI();
        }

        private void DoGoodsUI(List<Good> missionGoods, List<Good> nonMissionGoods)
        {
            Dictionary<Mission, List<Good>> missionMap = new Dictionary<Mission, List<Good>>();

            foreach (Mission m in PlayerMissions.missions) {
                if (m == null)
                    continue;

                missionMap.Add(m, missionGoods.FindAll(good => good.GetMissionIndex() == m.missionIndex));
            }

            foreach (KeyValuePair<Mission, List<Good>> kv in missionMap) {
                Mission m = kv.Key;
                List<Good> goods = kv.Value;
                if (goods.Count == 0) {
                    continue;
                }

                string name = m.goodPrefab.GetComponent<ShipItem>().name + " to " + m.destinationPort.GetPortName() + " x" + goods.Count.ToString();

                if (GUILayout.Button(name)) {
                    Good good = goods[0];
                    PickupGood(good);
                }
            }

            List<int> goodPrefabIndices = new List<int>();
            foreach (Good good in nonMissionGoods) {
                int index = good.GetComponent<SaveablePrefab>().prefabIndex;
                if (goodPrefabIndices.Contains(index))
                    continue;
                goodPrefabIndices.Add(index);
            }

            foreach (int index in goodPrefabIndices) {
                List<Good> goods = nonMissionGoods.FindAll(good => good.GetComponent<SaveablePrefab>().prefabIndex == index);
                string name = goods[0].GetComponent<ShipItem>().name + " x" + goods.Count.ToString();
                if (GUILayout.Button(name)) {
                    Good good = goods[0];
                    PickupGood(good);
                }
            }
        }

        private void DoBoatGoodsUI()
        {
            VerticalStart();

            GUILayout.Label("Goods :: Boat");

            if (GameState.lastBoat == null) {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                s.normal.background = darkBrownTexture;
                GUILayout.Label("boat unknown, enter a boat...", s);
                VerticalEnd();
                return;
            }

            BoatMass bm = GameState.lastBoat.GetComponent<BoatMass>();
            List<ItemRigidbody> itemsOnBoat = Traverse.Create(bm).Field("itemsOnBoat").GetValue<List<ItemRigidbody>>();

            List<Good> validGoods = new List<Good>();
            foreach (ItemRigidbody item in itemsOnBoat) {
                Good good = item.GetShipItem().GetComponent<Good>();
                if (ValidateGood(good)) {
                    validGoods.Add(good);
                }
            }

            List<Good> missionGoods = validGoods.FindAll(good => good.GetMissionIndex() != -1);
            List<Good> nonMissionGoods = validGoods.FindAll(good => good.GetMissionIndex() == -1);

            DoGoodsUI(missionGoods, nonMissionGoods);

            VerticalEnd();
        }

        private void DoPortGoodsUI()
        {
            VerticalStart();

            GUILayout.Label("Goods :: Port");

            if (nearestPortDistance > maximumPortDistance) {
                GUIStyle s = new GUIStyle(GUI.skin.label);
                s.normal.background = darkBrownTexture;
                GUILayout.Label("no port office nearby...", s);
                VerticalEnd();
                return;
            }

            DoGoodsUI(IslandMarketWarehouseAreaTracker.missionGoodsInArea, IslandMarketWarehouseAreaTracker.nonMissionGoodsInArea);

            VerticalEnd();
        }

        private void PickupGood(Good good)
        {
            CargoController.Log(good.sizeDescription);

            float extraHoldDistance = 0.0f;
            if (good.sizeDescription == "large crate") {
                extraHoldDistance = 0.2f;
            }

            ShipItem item = good.GetComponent<ShipItem>();
            Traverse.Create(PlayerNeedsUI.instance).Method("CloseNeedsUI").GetValue();
            item.transform.position = pointer.transform.position + pointer.transform.forward * (item.holdDistance + extraHoldDistance);
            item.transform.rotation = Refs.observerMirror.transform.rotation;
            pointer.PickUpItem(item);
        }

        public void SetVisible(bool visible)
        {
            m_visible = visible;
        }
    }
}