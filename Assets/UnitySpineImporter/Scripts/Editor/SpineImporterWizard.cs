using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Collections.Generic;


namespace UnitySpineImporter{
	public enum AnimationImportType{
		MECANIM,
		LEGACY
	}

	public class SpineImporterWizard :ScriptableWizard {
		public int pixelsPerUnit = 100;
		public bool buildAvatarMask = true;
		public AnimationImportType animationImportType = AnimationImportType.MECANIM;
		public bool updateResources = true;
		public float zStep = 0.01f;
		[HideInInspector]
		public string path;

		[MenuItem("Assets/Spine build prefab", false)]
		public static void spineBuildPrefab(){
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			SpineImporterWizard wizard = ScriptableWizard.DisplayWizard<SpineImporterWizard>("Generate Prefab from Spine data", "Generate");
			wizard.path = path;
		}

		[MenuItem("Assets/Spine build prefab", true)]
		public static bool validateContext(){
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			return path.EndsWith(".json");
		}

		void OnWizardUpdate() {
			helpString = "Be carefull, don't use small amout of pixels per unit (e.g. 1 or 10) \n" +
				"if you are going to use result model with unity 2d physics and gravity\n" +
				"update resources means - instead of create new animator and new animations update them";
			if (pixelsPerUnit <=0)
				errorString = "PixelsPerUnit must be greater than zero";
			else 
				errorString ="";
			isValid = errorString.Equals("");
			if (animationImportType == AnimationImportType.LEGACY && buildAvatarMask)
				helpString += "\n buildAvatarMask will be ignored";
		}


		void OnWizardCreate(){
			string atlasPath = getAtlasFilePath(path);
			string directory = Path.GetDirectoryName(atlasPath);
			string name = Path.GetFileNameWithoutExtension(path);
			SpritesByName                   spriteByName;
			Dictionary<string, GameObject>  boneGOByName;
			Dictionary<string, Slot>        slotByName;
			List<Skin>                      skins;
			AttachmentGOByNameBySlot 		attachmentGOByNameBySlot;

			if (File.Exists(path)){
				try{
					SpineMultiatlas spineMultiAtlas = SpineMultiatlas.deserializeFromFile(atlasPath    );
					SpineData       spineData       = SpineData      .deserializeFromFile(path);

					SpineUtil.updateImporters(spineMultiAtlas, directory, pixelsPerUnit, out spriteByName);
					GameObject rootGO = SpineUtil.buildSceleton(name, spineData, pixelsPerUnit, zStep, out boneGOByName, out slotByName);
					rootGO.name = name;
					SpineUtil.addAllAttahcmentsSlots(spineData, spriteByName, slotByName, pixelsPerUnit, out skins, out attachmentGOByNameBySlot);
					SkinController sk = SpineUtil.addSkinController(rootGO, spineData, skins, slotByName);
					if (animationImportType == AnimationImportType.MECANIM){
						Animator animator = SpineUtil.addAnimator(rootGO);
						if (buildAvatarMask)
							SpineUtil.builAvatarMask(rootGO,spineData, animator, directory, name);
					}

					ModelImporterAnimationType modelImporterAnimationType = getModelImporterAnimationType();
					if (spineData.animations !=null && spineData.animations.Count > 0)
						SpineUtil.addAnimation(rootGO, directory, spineData, boneGOByName, slotByName, attachmentGOByNameBySlot, skins,
											   pixelsPerUnit, zStep, modelImporterAnimationType, updateResources );
					sk.showDefaulSlots();
					SpineUtil.buildPrefab(rootGO, directory, name);
					GameObject.DestroyImmediate(rootGO);

				} catch (SpineMultiatlasCreationException e){ 
					Debug.LogException(e);
				} catch (SpineDatatCreationException e){
					Debug.LogException(e);
				} catch (AtlasImageDuplicateSpriteName e){
					Debug.LogException(e);
				}
			}
		}

		ModelImporterAnimationType getModelImporterAnimationType(){
			ModelImporterAnimationType result = ModelImporterAnimationType.Generic;
			switch(animationImportType){
			case AnimationImportType.LEGACY:
				result = ModelImporterAnimationType.Legacy;
				break;
			case AnimationImportType.MECANIM:
				result = ModelImporterAnimationType.Generic;
				break;
			}
			return result;
		}
		
		static string getAtlasFilePath(string path){
			string dir = Path.GetDirectoryName(path);
			string fileName = Path.GetFileNameWithoutExtension(path+"ffff");
			return dir + "/" + fileName + ".atlas";
		}
	}
}