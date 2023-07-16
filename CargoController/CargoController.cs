/*
 * CargoController - A mod for the game Sailwind
 * Copyright(C) 2023 Jacob Burbach (aka "JakeInABoat")
 *
 * This program is free software: you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License, version 3, as
 * published by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.If not, see<https://www.gnu.org/licenses/>
 */
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;
using System.Reflection;
using System.Collections.Generic;
using System;

namespace CargoController
{
	static class Main
	{
		static InputField consoleInput;
		static UnityModManager.ModEntry.ModLogger logger;
		static GameObject guiObject;

		public static bool guiVisible = false;
		public static bool enabled = false;

		static bool Load(UnityModManager.ModEntry modEntry)
		{
			var harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			modEntry.OnToggle = OnToggle;
			logger = modEntry.Logger;
			guiObject = new GameObject();
			guiObject.AddComponent<CargoControllerUI>();
			return true;
		}

		static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
		{
			enabled = value;
			return true;
		}

		static void CheckConsoleInput()
		{
			if (enabled) {
				return;
			}
			/*
			if (consoleInput.text == "checknull") {
				foreach (Good g in IslandMarketWarehouseAreaTracker.allGoodsInArea) {
					if (g == null) {
						Log("Whisket tango Foxtrot");
					}
				}
			}

			if (consoleInput.text == "port") {
				PlayerReputation.ChangeReputation(35000, PortRegion.medi);
				PlayerReputation.ChangeReputation(35000, PortRegion.alankh);
				PlayerReputation.ChangeReputation(35000, PortRegion.emerald);
				var fonts = Resources.FindObjectsOfTypeAll<Font>();
				foreach (var f in fonts) {
					Log(f.ToString());
				}

				float closestDistance = 10000000f;
				IslandHorizon closestIsland = null;

				foreach (IslandHorizon island in IslandDistanceTracker.instance.islands) {
					float d = Vector3.Distance(island.GetPosition(), Refs.observerMirror.transform.position);
					if (d < closestDistance) {
						closestIsland = island;
						closestDistance = d;
					}
				}

				if (closestIsland != null) {
					foreach (Port port in Port.ports) {
						if (port == null)
							continue;

						if (closestIsland.economy == port.island) {
							Log("Closest port is '" + port.GetPortName() + "' @ " + Vector3.Distance(port.transform.position, Refs.observerMirror.transform.position));
							IslandMissionOffice office = port.GetComponent<IslandMissionOffice>();
							IslandMarket market = office.GetComponent<IslandMarket>();
							IslandMarketWarehouseArea area = market.GetWarehouseArea();
							List<Good> goods = Traverse.Create(area).Field("goodsInArea").GetValue<List<Good>>();
							foreach (Good g in goods) {
								Log(g.ToString());
							}
						}
					}
					Log("Closest island is '" + closestIsland.name + " at " + closestDistance.ToString());
				}
			}
			*/
		}

		static void InitializeConsole()
		{
			if (Main.consoleInput != null) {
				return;
			}
			Main.consoleInput = GameObject.Find("ConsoleInputField")?.GetComponent<InputField>();
			Main.consoleInput?.onEndEdit.AddListener(delegate { CheckConsoleInput(); });
		}

		public static void SetGuiVisible(bool visible)
		{
			guiVisible = visible;
		}

		[HarmonyPatch(typeof(StartMenu), "GameToSettings")]
		static class StartMenu_GameToSettings
		{
			[HarmonyPriority(1000)]
			static void Postfix()
			{
				InitializeConsole();
			}
		}



		[HarmonyPatch(typeof(PlayerNeedsUI), "ToggleInventory")]
		static class PlayerNeeds_ToggleInventory
		{
			static void Postfix(bool state)
			{
				SetGuiVisible(state);
			}
		}

		public static void Log(String message)
		{
			logger.Log(message);
		}

		/*
		[HarmonyPatch(typeof(BoatMass), "UpdateMass")]
		static class BoatMass_UpdateMass
		{
			private static void Postfix(BoatMass __instance)
			{
				//Rigidbody body = (Rigidbody)Traverse.Create(__instance).Field("body").GetValue();
				//Log("BoatMass.UpdateMass => " + body.mass);
			}
		}
		*/
	}

	public class CargoControllerUI : MonoBehaviour
	{
		private float timeUntilPortCheck = 1.0f;
		private Port nearestPort = null;
		private float nearestPortDistance = 100000000.0f;
		private Font immortalFont = null;
		private Font architectsFont = null;
		private Texture2D darkBrownTexture = null;
		private Texture2D lightBrownTexture = null;
		private Texture2D reddishTexture = null;

		public static CargoControllerUI instance = null;
		public GoPointer pointer { get; private set; }

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

		void Start()
		{
			foreach (Font font in Resources.FindObjectsOfTypeAll<Font>()) {
				if (font.name == "IMMORTAL") {
					immortalFont = font;
				} else if (font.name == "ArchitectsDaughter") {
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

		void FixedUpdate()
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

		void ApplyGuiStyles()
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

		void VerticalStart()
		{
			GUIStyle style = new GUIStyle();
			style.normal.background = darkBrownTexture;
			style.margin.top = 5;
			style.margin.left = 5;
			GUILayout.BeginVertical(style, GUILayout.MinWidth(250));
		}

		void VerticalEnd()
		{
			GUILayout.EndVertical();
		}

		void OnGUI()
		{
			if (!Main.enabled || !Main.guiVisible) {
				return;
			}

			ApplyGuiStyles();

			DoBoatGoodsUI();
			DoPortGoodsUI();
		}

		void DoGoodsUI(List<Good> missionGoods, List<Good> nonMissionGoods)
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

				String name = m.goodPrefab.GetComponent<ShipItem>().name + " to " + m.destinationPort.GetPortName() + " x" + goods.Count.ToString();

				if (GUILayout.Button(name)) {
					Good good = goods[0];
					ShipItem item = good.GetComponent<ShipItem>();
					Traverse.Create(PlayerNeedsUI.instance).Method("CloseNeedsUI").GetValue();
					item.transform.position = pointer.transform.position + pointer.transform.forward * item.holdDistance;
					pointer.PickUpItem(item);
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
				String name = goods[0].GetComponent<ShipItem>().name + " x" + goods.Count.ToString();
				if (GUILayout.Button(name)) {
					Good good = goods[0];
					ShipItem item = good.GetComponent<ShipItem>();
					Traverse.Create(PlayerNeedsUI.instance).Method("CloseNeedsUI").GetValue();
					item.transform.position = pointer.transform.position + pointer.transform.forward * item.holdDistance;
					pointer.PickUpItem(item);
				}
			}
		}

		void DoBoatGoodsUI()
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

		void DoPortGoodsUI()
		{
			VerticalStart();

			GUILayout.Label("Goods :: Port");

			if (nearestPortDistance > 50.0f) {
				GUIStyle s = new GUIStyle(GUI.skin.label);
				s.normal.background = darkBrownTexture;
				GUILayout.Label("no port nearby...", s);
				VerticalEnd();
				return;
			}

			DoGoodsUI(IslandMarketWarehouseAreaTracker.missionGoodsInArea, IslandMarketWarehouseAreaTracker.nonMissionGoodsInArea);

			VerticalEnd();
		}

		[HarmonyPatch(typeof(LookUI), "RegisterPointer")]
		static class LookUI_RegisterPointer
		{
			static void Postfix(GoPointer ___pointer)
			{
				CargoControllerUI.instance.pointer = ___pointer;
			}
		}
	}


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

			Main.Log("Warehouse changed from '" + currentWarehouseArea?.name + "' to '" + area.name + "'");

			Clear();
			currentWarehouseArea = area;
		}

		[HarmonyPatch(typeof(IslandMarketWarehouseArea), "OnTriggerEnter")]
		static class IslandMarketWarehouseArea_OnTriggerEnter
		{
			static void Postfix(IslandMarketWarehouseArea __instance, Collider other)
			{
				Good good = other.GetComponent<Good>();
				if (good == null || !good.GetComponent<ShipItem>().sold) {
					return;
				}

				if (destroyedGoods.Contains(good)) {
					destroyedGoods.Remove(good);
					return;
				}

				CheckWarehouseChanged(__instance);

				if (!allGoodsInArea.Contains(good)) {
					allGoodsInArea.Add(good);
				}

				if (good.GetMissionIndex() == -1 && !nonMissionGoodsInArea.Contains(good)) {
					nonMissionGoodsInArea.Add(good);
				}

				if (good.GetMissionIndex() != -1 && !missionGoodsInArea.Contains(good)) {
					missionGoodsInArea.Add(good);
				}

				Main.Log("OnTriggerEnter: " + allGoodsInArea.Count.ToString());
			}
		}

		[HarmonyPatch(typeof(IslandMarketWarehouseArea), "OnTriggerExit")]
		static class IslandMarketWarehouseArea_OnTriggerExit
		{
			static void Postfix(IslandMarketWarehouseArea __instance, Collider other)
			{
				Good good = other.GetComponent<Good>();
				if (good == null) {
					return;
				}

				CheckWarehouseChanged(__instance);

				allGoodsInArea.Remove(good);
				missionGoodsInArea.Remove(good);
				nonMissionGoodsInArea.Remove(good);

				Main.Log("OnTriggerExit: " + allGoodsInArea.Count.ToString());
			}
		}

		[HarmonyPatch(typeof(ShipItem), "DestroyItem")]
		static class ShipItem_DestroyItem
		{
			static void Prefix(ShipItem __instance)
			{
				Good good = __instance.GetComponent<Good>();
				if (good == null)
					return;

				allGoodsInArea.Remove(good);
				nonMissionGoodsInArea.Remove(good);
				missionGoodsInArea.Remove(good);
				destroyedGoods.Add(good);

				Main.Log("ShipItem_DestroyItem => " + good.name);
			}
		}
	}
}
