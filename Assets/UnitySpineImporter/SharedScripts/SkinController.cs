using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnitySpineImporter;





namespace UnitySpineImporter{
	public class SkinController : MonoBehaviour {
		public Skin defaultSkin;		
		public Skin[]   skins;
		public Slot[] 	slots;

		public int      activeSkinId;

		
		Skin[]        _allSkins;
		public Skin[] allSkins{
			get{
				if (_allSkins == null){
					if (defaultSkin != null && defaultSkin.slots !=null && defaultSkin.slots.Length > 0){
						_allSkins = new Skin[skins.Length+1];
						Array.Copy(skins,_allSkins,skins.Length);
						_allSkins[_allSkins.Length -1] = defaultSkin;
					} else {
						_allSkins = skins;
					}
				}
				return _allSkins;
			}
		}		

		public void deactivateAllAttachments(){
			foreach(Skin skin in allSkins){
				foreach(SkinSlot slot in skin.slots){
					foreach(SkinSlotAttachment attachment in slot.attachments){
						attachment.gameObject.SetActive(false);
					}
				}
			}
		}		

		public void showDefaulSlots(){
			deactivateAllAttachments();

			if (skins.Length > 0){
				activeSkinId = 0;			
				setSkin(activeSkinId);
			} else {
				activeSkinId = -1;
			}

			foreach (Slot slot in slots){
				slot.showDefaultAttachment();
			}
		}

		public void setSkin(int skinId){
			skins[activeSkinId].setActive(false);
			skins[skinId].setActive(true);
			activeSkinId = skinId;

		}

	}
}