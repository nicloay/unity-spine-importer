using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UnitySpineImporter{
	[CustomPropertyDrawer(typeof(SkinSlot))]
	public class SlotPropDrawer : PropertyDrawer {

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label){
			int childCount = property.FindPropertyRelative("attachments").arraySize;
			if (property.isExpanded)
				return EditorGUIUtility.singleLineHeight * (childCount+1);
			else
				return EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label){
			float baseHeight = EditorGUIUtility.singleLineHeight;
			float fullWidth  = position.width;
			float labelWidth = EditorGUIUtility.labelWidth;
			float fieldWidth = fullWidth - labelWidth;
			position.height  = baseHeight;

			string slotName               = property.FindPropertyRelative("name").stringValue;
			GameObject slotGO = (GameObject)property.FindPropertyRelative("gameObject").objectReferenceValue;
			position.width = labelWidth;
			property.isExpanded = EditorGUI.Foldout(position,property.isExpanded, new GUIContent(slotName));
			position.x    += labelWidth;
			position.width = fieldWidth;
			EditorGUI.ObjectField(position, slotGO ,typeof(GameObject), true);

			position.x    -= labelWidth;
			position.width = fullWidth;
			if (!property.isExpanded)
				return;

			EditorGUI.indentLevel++;
			foreach (SerializedProperty attachment in  property.FindPropertyRelative("attachments")){
				position.y+=baseHeight;
				GameObject attachmentGO = attachment.FindPropertyRelative("gameObject").objectReferenceValue as GameObject;
				bool newValue = EditorGUI.Toggle(position, attachmentGO.name, attachmentGO.activeSelf, EditorStyles.radioButton);
				if (newValue!= attachmentGO.activeSelf){
					foreach(SerializedProperty resetAttachment in property.FindPropertyRelative("attachments")){
						(resetAttachment.FindPropertyRelative("gameObject").objectReferenceValue as GameObject).SetActive(false);
					}  
					attachmentGO.SetActive(newValue);
				}
			}
			EditorGUI.indentLevel--;

		}
	}
}