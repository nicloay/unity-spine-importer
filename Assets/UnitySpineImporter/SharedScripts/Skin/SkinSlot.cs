using UnityEngine;
using System.Collections.Generic;
using System;

namespace UnitySpineImporter{
	[Serializable]
	public class SkinSlot {
		public string name;
		public GameObject gameObject;
		public SkinSlotAttachment[] attachments;

		Dictionary<string, SkinSlotAttachment> _attachments;
		public SkinSlotAttachment this[string attahcmentName]{
			get{
				if(_attachments == null)
					resetCache();
				return _attachments[attahcmentName];
			}
		}

		public void resetCache(){
			_attachments = new Dictionary<string, SkinSlotAttachment>();
			for (int i = 0; i < attachments.Length; i++) {
				_attachments.Add(attachments[i].name, attachments[i]);
			}
		}

		public bool containsAttachment(string attachmentName){
			if (_attachments == null)
				resetCache();
			return _attachments.ContainsKey(attachmentName);
		}
	}
}
