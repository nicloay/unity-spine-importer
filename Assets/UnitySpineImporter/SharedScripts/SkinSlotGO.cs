using UnityEngine;
using System.Collections.Generic;
using System;

namespace UnitySpineImporter{
	[Serializable]
	public class SkinSlotGO{
		public GameObject attachmentGO;
		public SpriteRenderer[] innerSprites;

		Dictionary<string, SpriteRenderer> _spriteByName;
		public Dictionary<string, SpriteRenderer> spriteByName{
			get{
				if (spriteByName == null){
					_spriteByName = new Dictionary<string, SpriteRenderer>();
					for (int i = 0; i < innerSprites.Length; i++) {
						_spriteByName.Add(innerSprites[i].name, innerSprites[i]);
					}
				}
				return _spriteByName;
			}
		}
	}
}