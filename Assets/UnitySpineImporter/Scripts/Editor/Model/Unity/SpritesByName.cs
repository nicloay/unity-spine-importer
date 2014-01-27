using UnityEngine;
using System.Collections.Generic;


public class SpritesByName : Dictionary<string, Sprite> {
	public HashSet<Sprite> rotatedSprites;

	public SpritesByName():base(){
		rotatedSprites = new HashSet<Sprite>();
	}
}
