using UnityEngine;
using System.Collections.Generic;

namespace UnitySpineImporter{
	public class AttachmentGOByNameBySlot : Dictionary<string, AttachmentGOByName> {	

		public GameObject tryGetValue(string slotName, string attachmentName ){
			if (this.ContainsKey(slotName) && this[slotName].ContainsKey(attachmentName))
				return this[slotName][attachmentName];
			else
				return null;
		}

		public void add(string slotName, string attachmentName, GameObject attachmentGO){
			if (this.ContainsKey(slotName)){
				this[slotName].Add(attachmentName, attachmentGO);
			} else {
				AttachmentGOByName aGOByName = new AttachmentGOByName();
				aGOByName.Add(attachmentName, attachmentGO);
				this.Add(slotName,aGOByName);
			}

		}
	}
}