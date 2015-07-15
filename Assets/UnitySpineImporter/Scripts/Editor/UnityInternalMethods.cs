
using UnityEditor.Animations;
using UnityEngine;
using System.Reflection;

public class UnityInternalMethods {
	public static AnimatorController GetEffectiveAnimatorController(Animator animator){
		return (AnimatorController) (typeof(AnimatorController).GetMethod("GetEffectiveAnimatorController"
                          , BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]{ animator}));
	}
}
