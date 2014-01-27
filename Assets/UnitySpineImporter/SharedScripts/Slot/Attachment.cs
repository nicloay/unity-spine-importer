using UnityEngine;
using System.Collections;
using System;


namespace UnitySpineImporter{
	[Serializable]
	public class Attachment {
		public string 		  name;
		public AttachmentType type;
		public GameObject 	  gameObject;

		public Attachment (string name, AttachmentType type, GameObject gameObject)
		{
			this.name = name;
			this.type = type;
			this.gameObject = gameObject;
		}		
	}
}