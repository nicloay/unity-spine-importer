using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace UnitySpineImporter{
	[CustomEditor(typeof(SkinController))]
	public class SkinControllerEditor : Editor {

		public override void OnInspectorGUI ()
		{
			DrawDefaultInspector();
			SkinController sk = (SkinController)target;

			if(sk.skins.Length > 0 ){
				List<string> names = new List<string>();
				foreach(Skin skin in sk.skins)
					names.Add(skin.name);

				int newId = EditorGUILayout.Popup(sk.activeSkinId, names.ToArray());
				if (newId != sk.activeSkinId){
					sk.setSkin	(newId);
					EditorUtility.SetDirty(target);
				}
			}
		}

	}
}