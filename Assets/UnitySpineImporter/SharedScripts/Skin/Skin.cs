using UnityEngine;
using System.Collections.Generic;
using System;

namespace UnitySpineImporter{
	[Serializable]
	public class Skin {
		public string name;
		public SkinSlot[] slots;

		Dictionary<string, SkinSlot> _slots;

		public SkinSlot this[string slotName]{
			get{
				if (_slots == null)
					resetCache();
				if (!_slots.ContainsKey(slotName))
					Debug.Log(slotName+"!!!!!" + name);
				return _slots[slotName];
			}	
		}

		public void resetCache(){
			_slots = new Dictionary<string, SkinSlot>();
			for (int i = 0; i < slots.Length; i++) {
				slots[i].resetCache();
				_slots.Add(slots[i].name, slots[i]);
			}
		}

		public bool containsSlot(string slotName){
			if (_slots == null)
				resetCache();
			return _slots.ContainsKey(slotName);
		}

		public bool containsSlotAttachment(string slotName, string attachmentName){
			if (_slots == null)
				resetCache();
			if (!containsSlot(slotName))
				return false;
			return _slots[slotName].containsAttachment(attachmentName);
		}

		public void setActive(bool value){
			foreach(SkinSlot slot in slots){
				foreach(SkinSlotAttachment attachment in slot.attachments){
					attachment.gameObject.SetActive(value);
				}
			}
		}
	}
}