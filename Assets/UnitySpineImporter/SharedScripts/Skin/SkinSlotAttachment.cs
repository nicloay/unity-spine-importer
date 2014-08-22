using UnityEngine;
using System.Collections;
using System;

namespace UnitySpineImporter{
	[Serializable]
	public class SkinSlotAttachment {
		public string name;
		public GameObject gameObject;
		public SpriteRenderer sprite;
		public string ObPath;
	}
}