﻿using System;
using System.Collections.Generic;
using PubNubUnityShowcase;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Character Customizer
    /// - Handles player customization in the main menu
    /// </summary>

    public class CharacterCustomizer : MonoBehaviour
    {
        public PlayerController playerPrefabController;
        public InventorySlot slotPrefab;
        public Transform playerPreviewHandler;
        public Transform slotHandler;
        private readonly Dictionary<string, GameObject> ResourceCache = new Dictionary<string, GameObject>();
        private string playerPrefab = "Player";   
        GameObject instance = null;                  

        List<InventorySlot> curSlots = new List<InventorySlot>();   // the current instatiated slots
        PlayerController preview;                                   // the instantiated player prefab for preview

        // Start is called before the first frame update
        void Start()
        {
            foreach (Transform t in slotHandler){
                Destroy(t.gameObject);
            }

            for (int i = 0; i < SampleInventory.instance.items.Length; i++)
            {
                InventorySlot s = Instantiate(slotPrefab, slotHandler);
                s.cc = this;
                curSlots.Add(s);
            }

            RefreshSlots();
        }

        /// <summary>
        /// Called in the MainMenu's "Customize" button.
        /// </summary>
        public void Open(){

            // Create a preview of the player:
            if (playerPrefabController) Destroy(playerPrefabController.gameObject);

            GameObject res = null;
            bool cached = this.ResourceCache.TryGetValue(playerPrefab, out res);
            if (!cached)
            {
                res = Resources.Load<GameObject>(playerPrefab);
                if (res == null)
                    Debug.LogError("DefaultPool failed to load " + playerPrefab + ", did you add it to a Resources folder? ");
                else
                    this.ResourceCache.Add(playerPrefab, res);
            }

            if (res.activeSelf)
                res.SetActive(false);

            instance = GameObject.Instantiate(res) as GameObject;
            PubNubPlayerProps properties = instance.GetComponent<PubNubPlayerProps>() as PubNubPlayerProps;
            if (properties == null)
            {
                Debug.LogError("Player must have a PubNubPlayer associated with the Prefab");
            }
            else
            {
                properties.IsPreview = true;
            }
            playerPrefabController = instance.GetComponent<PlayerController>();
            
            instance.SetActive(true);

            RefreshSlots();

            // Refresh the player preview:
            Cosmetics c = new Cosmetics(DataCarrier.chosenHat);
            playerPrefabController.cosmeticsManager.Refresh(c);
        }

        public void RefreshSlots(){
            // Refresh all inventory slots:
            for (int i = 0; i < curSlots.Count; i++)
            {
                if (SampleInventory.instance.availableHats.Contains(i))
                {
                    curSlots[i].curItem = SampleInventory.instance.items[i];
                }
                else
                {
                    curSlots[i].curItem = null;
                }
                curSlots[i].Refresh();
            }

        }

        /// <summary>
        /// Equips an item from the database:
        /// </summary>
        public void Equip(CosmeticItemData item){
            
            switch (item.itemType){
                case CosmeticType.Hat:
                    int hat = Array.IndexOf(ItemDatabase.instance.hats, item);
                    DataCarrier.chosenHat = DataCarrier.chosenHat == hat ? -1 : hat;    // equipping an already equipped item will unequip it (value of -1 means 'no item')
                    break;
            }

            RefreshSlots();

            // Refresh the player preview:
            Cosmetics c = new Cosmetics(DataCarrier.chosenHat);
            playerPrefabController.cosmeticsManager.Refresh(c);
        }
    }
}
