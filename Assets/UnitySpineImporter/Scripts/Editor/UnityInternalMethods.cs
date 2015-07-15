
using UnityEditor.Animations;
using UnityEngine;
using System.Reflection;
using UnityEditor;

public class UnityInternalMethods {
	public static AnimatorController GetEffectiveAnimatorController(Animator animator){
		return (AnimatorController) (typeof(AnimatorController).GetMethod("GetEffectiveAnimatorController"
                          , BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]{ animator}));
	}

	public static void GetTextureSize(TextureImporter textureImporter, out int width, out int height){
		object[] args = new object[2] { 0, 0 };
		MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
		mi.Invoke(textureImporter, args);		
		width = (int)args[0];
		height = (int)args[1];
	}
}
