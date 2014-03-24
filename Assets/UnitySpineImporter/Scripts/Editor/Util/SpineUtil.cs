using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;
using System.Collections.Generic;
using System;
using UnityEditorInternal;
using CurveExtended;
using LitJson;
using System.Reflection;

namespace UnitySpineImporter{
	public class AtlasImageNotFoundException: System.Exception{
		public AtlasImageNotFoundException(string message):base(message){
		}
	}

	public class AtlasImageDuplicateSpriteName:System.Exception{
		public AtlasImageDuplicateSpriteName(string message):base(message){
		}
	}

	public class SpineUtil {
		public static string SLOT_PREFIX       = "slot";
		public static string SKIN_PREFIX       = "skin";
		public static string ANIMATION_FOLDER  = "animation";
		public static string SLASH_REPLACEMENT = "|";

		public static Vector2 lineToVector2(string line){
			string[] xy = null;
			try{
				line = line.Split(':')[1];
				xy = line.Split(',');
			} finally{
			}
			return new Vector2(int.Parse(xy[0]), int.Parse( xy[1]));

		}

		public static void buildPrefab(GameObject gameObject, string directory, string name){
			string prefabPath = directory + "/" + name + ".prefab";
			UnityEngine.Object oldPrefab = AssetDatabase.LoadAssetAtPath( prefabPath, typeof(GameObject));
			if (oldPrefab == null)
				PrefabUtility.CreatePrefab(prefabPath, gameObject, ReplacePrefabOptions.ConnectToPrefab);
			else {
				UnityEngine.Object instance = PrefabUtility.InstantiatePrefab(oldPrefab);
				instance.name = instance.name+"_copy";
				PrefabUtility.DisconnectPrefabInstance(instance);
				PrefabUtility.ReplacePrefab(gameObject, oldPrefab, ReplacePrefabOptions.ReplaceNameBased);
				PrefabUtility.ReplacePrefab(instance as GameObject, oldPrefab, ReplacePrefabOptions.ConnectToPrefab);
				GameObject.DestroyImmediate(instance);
			}
		}

		public static void builAvatarMask(GameObject gameObject, SpineData spineData, Animator animator, string directory, string name){
			Avatar avatar = AvatarBuilder.BuildGenericAvatar(gameObject,"");
			animator.avatar = avatar;
			AvatarMask avatarMask = new AvatarMask();
			string[] transofrmPaths = getTransformPaths(gameObject, spineData);
			avatarMask.transformCount = transofrmPaths.Length;
			for (int i=0; i< transofrmPaths.Length; i++){
				avatarMask.SetTransformPath(i, transofrmPaths[i]);
				avatarMask.SetTransformActive(i, true);
			}
			createFolderIfNoExists(directory, ANIMATION_FOLDER);
			AssetDatabase.CreateAsset(avatar    , directory + "/" + ANIMATION_FOLDER + "/" + name + ".anim.asset");
			AssetDatabase.CreateAsset(avatarMask, directory + "/" + ANIMATION_FOLDER + "/" + name + ".mask.asset");
		}

		public static string[] getTransformPaths(GameObject go, SpineData spineData){
			List<String> result = new List<string>();
			result.Add("");
			 foreach(Transform t in go.GetComponentsInChildren<Transform>(true)){
				string path = AnimationUtility.CalculateTransformPath(t,go.transform);
				if (t.name.StartsWith(SLOT_PREFIX+" [") && t.name.EndsWith("]")){
					string slotName = t.name.Remove(t.name.Length -1);
					slotName = slotName.Remove(0,(SLOT_PREFIX+" [").Length );
					if (spineData.slotPathByName.ContainsKey(slotName) && spineData.slotPathByName[slotName]==path)					
						result.Add(path);
				}else {
					if (spineData.bonePathByName.ContainsKey(t.name) && spineData.bonePathByName[t.name]==path) 
						result.Add(path);					
				}

			}
			return result.ToArray();
		}

		static int[] sizes = new int[]{0, 32, 64, 128, 256, 512, 1024, 2048, 4096};
		static string[] platforms = new string[]{"Web", "Standalone", "iPhone", "Android", "FlashPlayer"};
		static void fixTextureSize(string imagePath){			 
			TextureImporter importer =  TextureImporter.GetAtPath(imagePath) as TextureImporter;
			if (importer != null) {
				object[] args = new object[2] { 0, 0 };
				MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
				mi.Invoke(importer, args);
				
				int width = (int)args[0];
				int height = (int)args[1];

				int max = Mathf.Max(width,height);
				if (max > 4096){
					Debug.LogError("original texture size is to big " + imagePath + " size=" + width + "x" + height);
					return;
				}

				int fitSize = 0;
				for (int i = 0,nextI =1; i < max && fitSize==0; i=nextI++ ) {
					if (max > sizes[i] && max <= sizes[nextI] )
						fitSize = sizes[nextI];
				}

				if (importer.maxTextureSize!=fitSize){
					Debug.LogWarning("change default size to " + fitSize+ " for "+imagePath);
					importer.maxTextureSize = fitSize;
				}

				foreach(string platform in platforms){
					int maxTextureSize;
					TextureImporterFormat textureFormat;
					importer.GetPlatformTextureSettings(platform, out maxTextureSize, out textureFormat);
					if (maxTextureSize != fitSize){
						Debug.LogWarning("change specific size to " + fitSize + "on " + platform + " for " + imagePath);
						importer.SetPlatformTextureSettings(platform, fitSize, textureFormat);
					}
				}
				AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceSynchronousImport);
			}
		}
		
		public static void updateImporters(SpineMultiatlas multiatlas, string directory, int pixelsPerUnit, out SpritesByName spriteByName){
			spriteByName = new SpritesByName();
			foreach (SpineAtlas spineAtlas in multiatlas){
				string imagePath = directory + "/" + spineAtlas.imageName;
				if (!File.Exists(imagePath))
					throw new AtlasImageNotFoundException("can't find " + spineAtlas.imageName + " image in " + directory + " folder");
				fixTextureSize(imagePath);
				Texture2D tex = AssetDatabase.LoadAssetAtPath(imagePath, typeof(Texture2D )) as Texture2D;
				Vector2 atlasSize = new Vector2(tex.width, tex.height);
				TextureImporter importer = TextureImporter.GetAtPath(imagePath) as TextureImporter;
				importer.spritesheet = getSpriteMetadata(spineAtlas, atlasSize);
				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = SpriteImportMode.Multiple;
				importer.spritePixelsToUnits = pixelsPerUnit;
				AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceUpdate);
				AssetDatabase.SaveAssets();


				foreach(UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(imagePath)){
					Sprite s = obj as Sprite;
					if (s!=null){
						try{
							spriteByName.Add(s.name,s);
						} catch (ArgumentException e) {
							throw new AtlasImageDuplicateSpriteName("source images has duplicate name "+s.name +"\n"+e);
						}
					}
				}

				foreach(SpineSprite spineSprite in spineAtlas.sprites){
					if (spineSprite.rotate){
						spriteByName.rotatedSprites.Add(spriteByName[spineSprite.name]);
					}
				}
			}
		}

		public static SpriteMetaData[] getSpriteMetadata(SpineAtlas spineAtlas, Vector2 atlasImageSize){

			SpriteMetaData[] result = new SpriteMetaData[spineAtlas.sprites.Count];
			SpineSprite spineSprite;
			for (int i = 0; i < result.Length; i++) {
				spineSprite = spineAtlas.sprites[i];
				result[i] = new SpriteMetaData();
				result[i].name = spineSprite.name;
				result[i].rect = getRectFromSpineSprite(spineSprite, atlasImageSize);
				
				if (spineSprite.orig != spineSprite.size){
					result[i].alignment = (int) SpriteAlignment.Custom;
					result[i].pivot = getPivotFromSpineSprite(spineSprite);
				}
				
			}
			return result;	
		}

		public static Rect getRectFromSpineSprite(SpineSprite sprite, Vector2 atlasImageSize){
			float x,y,width,height;

			x = sprite.xy.x;
			width = sprite.size.x;
			height = sprite.size.y;
			if (sprite.rotate){
				y = atlasImageSize.y - sprite.size.x - sprite.xy.y;
				swap2Float(ref width, ref height);
			}else {
				y = atlasImageSize.y  - sprite.size.y - sprite.xy.y;
			}
			return new Rect(x, y, width, height);
		}

		public static Vector2 getPivotFromSpineSprite(SpineSprite sprite){
			float offsetX = sprite.offset.x;
			float offsetY = sprite.offset.y;
			if (sprite.rotate)
				swap2Float(ref offsetX, ref offsetY);
			float x = 0.5f +  (float)((offsetX + sprite.size.x/2 - sprite.orig.x/2)/ sprite.size.x);
			float y = 0.5f +  (float)(((sprite.orig.y - offsetY - sprite.size.y/2) - sprite.orig.y / 2)/ sprite.size.y);
			if (sprite.rotate)
				swap2Float(ref x, ref y);
			return new Vector2(x,y);
		}

		public static void swap2Float(ref float float1, ref float float2){
			float tmp = float1;
			float1 = float2;
			float2 = tmp;
		}

		public static GameObject buildSceleton(string name, SpineData data, int pixelsPerUnit, out Dictionary<string,GameObject> boneGOByName, out Dictionary<string, Slot> slotByName){
			float ratio = 1.0f / (float)pixelsPerUnit;
			boneGOByName = new Dictionary<string, GameObject>();
			slotByName = new Dictionary<string, Slot>();
			GameObject rootGO = new GameObject(name);
			foreach(SpineBone bone in data.bones){
				GameObject go = new GameObject(bone.name);
				boneGOByName.Add(bone.name, go);
			}

			foreach(SpineBone bone in data.bones){
				GameObject go = boneGOByName[bone.name];
				if (bone.parent == null)
					go.transform.parent = rootGO.transform;
				else 
					go.transform.parent = boneGOByName[bone.parent].transform;

				Vector3    position = new Vector3((float)bone.x * ratio, (float)bone.y * ratio, 0.0f);
				Vector3    scale    = new Vector3((float)bone.scaleX, (float)bone.scaleY, 1.0f);
				Quaternion rotation = Quaternion.Euler(0, 0, (float)bone.rotation);
				go.transform.localPosition = position;
				go.transform.localScale    = scale;
				go.transform.localRotation = rotation;
			}

			foreach(SpineSlot spineSlot in data.slots){
				GameObject go = new GameObject(getSlotGOName(spineSlot.name));
				go.transform.parent = boneGOByName[spineSlot.bone].transform;
				resetLocalTRS(go);
				Slot slot = new Slot();
				slot.bone = spineSlot.bone;
				slot.name = spineSlot.name;
				slot.color = hexStringToColor32(spineSlot.color);
				slot.gameObject = go;
				slot.defaultAttachmentName = spineSlot.attachment;
				slotByName.Add(slot.name, slot);
			}
			return rootGO;
		}

		public static string getSlotGOName(string slotName){
			return SLOT_PREFIX+" ["+slotName+"]";
		}

		public static void resetLocalTRS(GameObject go){
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = Vector3.one;
		}

		public static void addAllAttahcmentsSlots(SpineData spineData, SpritesByName spriteByName, Dictionary<string, Slot> slotByName, float zStep, int pixelsPerUnit, out List<Skin> skins, out AttachmentGOByNameBySlot attachmentGOByNameBySlot){
			float ratio = 1.0f / (float) pixelsPerUnit;
			skins = new List<Skin>();
			attachmentGOByNameBySlot= new AttachmentGOByNameBySlot();
			foreach(KeyValuePair<string, SpineSkinSlots>kvp in spineData.skins){
				string skinName = kvp.Key;
				Skin skin = new Skin();
				skin.name = skinName;
				List<SkinSlot> slotList = new List<SkinSlot>();

				bool isDefault = skinName.Equals("default");
				foreach(KeyValuePair<string, SpineSkinSlotAttachments>kvp2 in  spineData.skins[skinName]){
					string slotName = kvp2.Key;
					GameObject slotGO     = slotByName[slotName].gameObject;

					Slot slot = slotByName[slotName];



					SkinSlot skinSlot = new SkinSlot();
					skinSlot.name = slotName;
					skinSlot.gameObject = slotGO;
					List<SkinSlotAttachment> attachmentList = new List<SkinSlotAttachment>();
					foreach(KeyValuePair<string, SpineSkinAttachment> kvp3 in spineData.skins[skinName][slotName]){
						string              attachmenName = kvp3.Key;
						SkinSlotAttachment attachment = new SkinSlotAttachment();
						attachment.name = attachmenName;

						SpineSkinAttachment spineAttachment    = kvp3.Value;

						// - create skined object or direct GO for default skin
						Sprite     sprite;
						spriteByName.TryGetValue(spineAttachment.name, out sprite);
						int        drawOrder  = spineData.slotOrder[slotName];
						
						GameObject parentGO;
						GameObject spriteGO;
						string fixedName = attachmenName.Replace("/",SLASH_REPLACEMENT);
						if (isDefault){
							parentGO = slotGO;
							spriteGO = new GameObject(fixedName);
							Attachment a = new Attachment(attachmenName, AttachmentType.SINGLE_SPRITE, spriteGO);
							slot.addAttachment(a);
						} else {								
							spriteGO = new GameObject(skinName);
							Attachment a;
							slot.attachmentByName.TryGetValue(attachmenName, out a);
							if (a == null){
								GameObject attachmentGO = new GameObject(fixedName);
								attachmentGO.transform.parent = slotGO.transform;
								resetLocalTRS(attachmentGO);					
								a = new Attachment(attachmenName, AttachmentType.SKINED_SPRITE, attachmentGO);
								slot.addAttachment(a);
							}
							
							parentGO = a.gameObject;
						}
						
						attachment.gameObject = spriteGO;
						spriteGO.transform.parent = parentGO.gameObject.transform;
						// -
						if (spineAttachment.type.Equals("region")){
							SpriteRenderer sr = spriteGO.AddComponent<SpriteRenderer>();
							sr.sprite = sprite;
							spriteGO.transform.localPosition = getAttachmentPosition(spineAttachment, ratio,-( drawOrder * zStep));
							spriteGO.transform.localRotation = getAttachmentRotation(spineAttachment, spriteByName.rotatedSprites.Contains(sprite));
							spriteGO.transform.localScale    = getAttachmentScale(spineAttachment);
						} else  if (spineAttachment.type.Equals("boundingbox")) {
							PolygonCollider2D collider = spriteGO.AddComponent<PolygonCollider2D>();
							resetLocalTRS(spriteGO);
							Vector2[] vertices = new Vector2[spineAttachment.vertices.Length/2];
							for (int i = 0; i < spineAttachment.vertices.Length; i+=2) {
								float x = (float) spineAttachment.vertices[i  ] * ratio;
								float y = (float) spineAttachment.vertices[i+1] * ratio;
								vertices[i/2] = new Vector2(x,y);
							}
							collider.points = vertices;
							collider.SetPath(0,vertices);
						}else {
							Debug.LogWarning("Attachment type " + spineAttachment.type + " is not supported yiet FIX MEEE");
						}
						attachmentList.Add(attachment);
					}
					skinSlot.attachments = attachmentList.ToArray();
					slotList.Add(skinSlot);
				}
				skin.slots = slotList.ToArray();
				skins.Add(skin);
			}
		}


		public static SkinController addSkinController(GameObject gameObject, SpineData spineData, List<Skin> allSkins, Dictionary<string, Slot> slotByName){
			SkinController sk = gameObject.AddComponent<SkinController>();
			List<Skin> skins = new List<Skin>();
			Skin defaultSkin = null;
			foreach(Skin skin in allSkins){
				if (skin.name.Equals("default")){
					defaultSkin = skin;
				} else {
					skins.Add(skin);
				}
			}
			sk.defaultSkin = defaultSkin;
			sk.skins = skins.ToArray();

			Slot[] slots = new Slot[slotByName.Count];
			slotByName.Values.CopyTo(slots,0);
			sk.slots = slots;
			return sk;
		}

		public static Animator addAnimator(GameObject go){
			Animator result = go.GetComponent<Animator>();
			if (result == null)
				result = go.AddComponent<Animator>();
			return result;
		}

		public static void addAnimation(GameObject                     rootGO, 
		                                string                         rootDirectory,  
		                                SpineData                      spineData, 
		                                Dictionary<string, GameObject> boneGOByName, 
		                                AttachmentGOByNameBySlot       attachmentGOByNameBySlot,
		                                int                            pixelsPerUnit,
		                                ModelImporterAnimationType     modelImporterAnimationType,
		                                bool                           updateResources)
		{
			float ratio = 1.0f / (float)pixelsPerUnit;
			foreach(KeyValuePair<string,SpineAnimation> kvp in spineData.animations){
				string animationName = kvp.Key;
				string animationFolder  = rootDirectory+"/"+ANIMATION_FOLDER;
				string assetPath        = animationFolder + "/" + animationName+".anim";

				SpineAnimation spineAnimation = kvp.Value;
				AnimationClip animationClip = new AnimationClip();
				bool updateCurve = false;
				if (File.Exists(assetPath)){
					AnimationClip oldClip = AssetDatabase.LoadAssetAtPath(assetPath, typeof(AnimationClip)) as AnimationClip;
					if (oldClip != null){
						animationClip = oldClip;
						animationClip.ClearCurves();
						updateCurve = true;
					}
				}

				AnimationUtility.SetAnimationType(animationClip, modelImporterAnimationType);
				if (spineAnimation.bones!=null)
					addBoneAnimationToClip(animationClip,spineAnimation.bones, spineData, boneGOByName, ratio);
				if (spineAnimation.slots!=null)
					addSlotAnimationToClip(animationClip, spineAnimation.slots, spineData, attachmentGOByNameBySlot);
				if (spineAnimation.draworder!=null)
					Debug.LogWarning("draworder animation implemented yet");


				if (updateCurve){
					EditorUtility.SetDirty(animationClip);
					AssetDatabase.SaveAssets();
				} else {
					animationClip.frameRate = 30;
					createFolderIfNoExists(rootDirectory, ANIMATION_FOLDER);
					AssetDatabase.CreateAsset(animationClip, assetPath);
					AssetDatabase.SaveAssets();

					if (modelImporterAnimationType == ModelImporterAnimationType.Generic)
						AddClipToAnimatorComponent(rootGO,animationClip);
					else 
						AddClipToLegacyAnimationComponent(rootGO, animationClip);
				}

			}
		}

		static void AddClipToLegacyAnimationComponent(GameObject rootGO, AnimationClip animationClip){
			Animation animation = rootGO.GetComponent<Animation>();
			if (animation == null)
				animation = rootGO.AddComponent<Animation>();
			animation.AddClip(animationClip, animationClip.name);
		}

		static void createFolderIfNoExists(string root, string folderName){
			string path = root+"/"+folderName;
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}

		public static void addSlotAnimationToClip(AnimationClip                          clip, 
		                                          Dictionary<string, SpineSlotAnimation> slotsAnimation, 
		                                          SpineData                              spineData, 
		                                          AttachmentGOByNameBySlot               attachmentGOByNameBySlot)
		{
			foreach(KeyValuePair<string, SpineSlotAnimation> kvp in slotsAnimation){
				string slotName = kvp.Key;
				string defaultAttachment = spineData.slotDefaultAttachments[slotName];
				SpineSlotAnimation slotAnimation = kvp.Value;
				if (slotAnimation.attachment != null && slotAnimation.attachment.Count > 0){
					Dictionary<string, AnimationCurve> curveByName = new Dictionary<string, AnimationCurve>();
					for (int i = 0; i < slotAnimation.attachment.Count; i++) {
						SpineSlotAttachmentAnimation anim = slotAnimation.attachment[i];
						if (string.IsNullOrEmpty( anim.name))
							continue;
							
						AnimationCurve enableCurve;
						if (curveByName.ContainsKey(anim.name)){
							enableCurve = curveByName[anim.name];
						} else {
							enableCurve = new AnimationCurve();
							if (anim.time > 0.0f)
								enableCurve.AddKey(KeyframeUtil.GetNew(0, 0.0f, TangentMode.Stepped));							

							curveByName.Add(anim.name, enableCurve);

							if (i==0 && !anim.name.Equals(defaultAttachment)){
								AnimationCurve defSlotCurve = new AnimationCurve();
								curveByName.Add(defaultAttachment, defSlotCurve);

								if (anim.time !=0.0f){
									defSlotCurve.AddKey(KeyframeUtil.GetNew(0, 1, TangentMode.Stepped));
									defSlotCurve.AddKey(KeyframeUtil.GetNew((float)anim.time, 0, TangentMode.Stepped));
								} else {
									defSlotCurve.AddKey(KeyframeUtil.GetNew(0, 0, TangentMode.Stepped));
								}

							}
						}

						enableCurve.AddKey(KeyframeUtil.GetNew((float)anim.time, 1, TangentMode.Stepped));
						if (i< (slotAnimation.attachment.Count - 1)){
							SpineSlotAttachmentAnimation nextAnim = slotAnimation.attachment[i+1];
							if (!nextAnim.name.Equals(anim.name))
								enableCurve.AddKey(KeyframeUtil.GetNew((float)nextAnim.time, 0, TangentMode.Stepped));
						}
					}
					foreach(KeyValuePair<string, AnimationCurve> kvp2 in curveByName){
						string attachmentName = kvp2.Key;
						AnimationCurve animationCurve = kvp2.Value;
						string attachmentPath = spineData.slotPathByName[slotName] + "/" + attachmentName.Replace("/",SLASH_REPLACEMENT);	
						clip.SetCurve(attachmentPath, typeof(GameObject),"m_IsActive", animationCurve);
					}

				}

				if (slotAnimation.color != null && slotAnimation.color.Count >0){
					Debug.LogWarning("slot color animation is not supported yet");
				}
			}
		}

		public static void setTangents(AnimationCurve curve, JsonData[] curveData){
			bool showWarning = true;
			for (int i = 0; i < curve.keys.Length; i++) {
				int nextI = i + 1;
				if (nextI < curve.keys.Length){
					if (curveData[i] == null ){ 
						//Linear
						setLinearInterval(curve, i, nextI);
					} else {
						if (curveData[i].IsArray){
							if (showWarning){
								Debug.LogWarning("be carefull, smooth bezier animation is in beta state, check result animation manually");
								showWarning = false;
							}
							setCustomTangents(curve, i, nextI, curveData[i]);
						} else {
							if (((string)curveData[i]).Equals("stepped")){
								setSteppedInterval(curve, i, nextI);
							} else {
								Debug.LogError("unsupported animation type "+(string)curveData[i]);
							}
						}
					}
				}
			}
		}

		static float parseFloat(JsonData jsonData){
			if (jsonData.IsDouble)
				return (float)(double)jsonData;
			else if (jsonData.IsInt)
				return (float)(int)jsonData;
			Debug.LogError("can't parse to double ["+jsonData+"]");
			return 0.0f;
		}


		// p0, p3 - start, end points
		// p1, p2 - conrol points
		// t - value on x [0,1]
		public static Vector2 getBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t){
			float y = (1 - t) * (1 - t) * (1 - t) * p0.y +
					3 * t * (1 - t) * (1 - t) * p1.y +
					3 * t * t * (1 - t) * p2.y +
					t * t * t * p3.y;
			return new Vector2(p0.x + t * (p3.x - p0.x) ,y);
		}

		// a - start point
		// b - on t= 1/3
		// c - on t = 2/3
		// d - end point
		// c1,c2 control points of bezier.
		public static void calcControlPoints(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 c1, out Vector2 c2){
			c1 = (-5 * a + 18 * b - 9 * c + 2 * d)/6;
			c2 = ( 2 * a - 9 * b + 18 * c - 5 * d)/6;	
		}

		public static void setCustomTangents(AnimationCurve curve, int i, int nextI, JsonData tangentArray){
			float diffValue = curve[nextI].value - curve[i].value;
			float diffTime = curve[nextI].time - curve[i].time;
			if (diffValue == 0)
				return; 



			float cx1 = parseFloat(tangentArray[0]);
			float cy1 = parseFloat(tangentArray[1]);
			float cx2 = parseFloat(tangentArray[2]);
			float cy2 = parseFloat(tangentArray[3]);
			Vector2 p0     = new Vector2(0  , curve[i].value);
			Vector2 p3     = new Vector2(diffTime  , curve[nextI].value);
			Vector2 cOrig1 = new Vector2(diffTime * cx1, curve[i].value);
			cOrig1.y += diffValue > 0 ? diffValue * cy1 : -1.0f * Mathf.Abs(diffValue * cy1);

			Vector2 cOrig2 = new Vector2(diffTime * cx2, curve[i].value);
			cOrig2.y += diffValue > 0 ? diffValue * cy2 : -1.0f * Mathf.Abs(diffValue * cy2);

			Vector2 p1 = getBezierPoint(p0, cOrig1, cOrig2, p3, 1.0f / 3.0f);
			Vector2 p2 = getBezierPoint(p0, cOrig1, cOrig2, p3, 2.0f / 3.0f);


			Vector2 c1tg, c2tg, c1, c2;
			calcControlPoints(p0,p1,p2,p3, out c1, out c2);

			c1tg = c1 - p0;
			c2tg = c2 - p3;

			float outTangent = c1tg.y / c1tg.x;
			float inTangent  = c2tg.y / c2tg.x;


			object thisKeyframeBoxed = curve[i];
			object nextKeyframeBoxed = curve[nextI];


			if (!KeyframeUtil.isKeyBroken(thisKeyframeBoxed))
				KeyframeUtil.SetKeyBroken(thisKeyframeBoxed, true);		
			KeyframeUtil.SetKeyTangentMode(thisKeyframeBoxed, 1, TangentMode.Editable);

			if (!KeyframeUtil.isKeyBroken(nextKeyframeBoxed))
				KeyframeUtil.SetKeyBroken(nextKeyframeBoxed, true);		
			KeyframeUtil.SetKeyTangentMode(nextKeyframeBoxed, 0, TangentMode.Editable);

			Keyframe thisKeyframe = (Keyframe)thisKeyframeBoxed;
			Keyframe nextKeyframe = (Keyframe)nextKeyframeBoxed;

			thisKeyframe.outTangent = outTangent;
			nextKeyframe.inTangent  = inTangent;

			curve.MoveKey(i, 	 thisKeyframe);
			curve.MoveKey(nextI, nextKeyframe);

			//* test method
			bool ok = true;
			float startTime = thisKeyframe.time;

			float epsilon = 0.001f;
			for (float j=0; j < 25f; j++) {
				float t  = j/25.0f;
				Vector2 t1 = getBezierPoint(p0, cOrig1, cOrig2, p3, t);
				Vector2 t2 = getBezierPoint(p0, c1, c2, p3, t);
				float curveValue = curve.Evaluate(startTime + diffTime * t);
				if (!NearlyEqual(t1.y, t2.y, epsilon) 
				    || !NearlyEqual(t2.y, curveValue, epsilon)){
					Debug.LogError("time = "+ t + "   t1 = ["+t1.y.ToString("N8")+"]   t2 = ["+t2.y.ToString("N8")+"]    curve = ["+curveValue.ToString("N8")+"]");
					ok = false;
				}				
			}
			if (!ok)
				Debug.LogWarning("something wrong with bezier points");
			//*/

		}

		public static bool NearlyEqual(float a, float b, float epsilon)
		{
			float absA = Math.Abs(a);
			float absB = Math.Abs(b);
			float diff = Math.Abs(a - b);
			
			if (a == b)
			{ // shortcut, handles infinities
				return true;
			} 
			else if (a == 0 || b == 0 || diff < Double.MinValue) 
			{
				// a or b is zero or both are extremely close to it
				// relative error is less meaningful here
				return diff < (epsilon * Double.MinValue);
			}
			else
			{ // use relative error
				return diff / (absA + absB) < epsilon;
			}
		}


		public static void setSteppedInterval(AnimationCurve curve, int i, int nextI){

			if (curve.keys[i].value == curve.keys[nextI].value){
				return;
			}

			object thisKeyframeBoxed = curve[i];
			object nextKeyframeBoxed = curve[nextI];
		
			if (!KeyframeUtil.isKeyBroken(thisKeyframeBoxed))
				KeyframeUtil.SetKeyBroken(thisKeyframeBoxed, true);
			if (!KeyframeUtil.isKeyBroken(nextKeyframeBoxed))
				KeyframeUtil.SetKeyBroken(nextKeyframeBoxed, true);
			
			KeyframeUtil.SetKeyTangentMode(thisKeyframeBoxed, 1, TangentMode.Stepped);
			KeyframeUtil.SetKeyTangentMode(nextKeyframeBoxed, 0, TangentMode.Stepped);

			Keyframe thisKeyframe = (Keyframe)thisKeyframeBoxed;
			Keyframe nextKeyframe = (Keyframe)nextKeyframeBoxed;
			thisKeyframe.outTangent = float.PositiveInfinity;
			nextKeyframe.inTangent  = float.PositiveInfinity;
			curve.MoveKey(i, 	 thisKeyframe);
			curve.MoveKey(nextI, nextKeyframe);
		}


		public static void setLinearInterval(AnimationCurve curve, int i, int nextI){
			Keyframe thisKeyframe = curve[i];
			Keyframe nextKeyframe = curve[nextI];
			thisKeyframe.outTangent = CurveExtension.CalculateLinearTangent(curve, i, nextI);
			nextKeyframe.inTangent = CurveExtension.CalculateLinearTangent(curve, nextI, i);

			KeyframeUtil.SetKeyBroken((object)thisKeyframe, true);
			KeyframeUtil.SetKeyBroken((object)nextKeyframe, true);

			KeyframeUtil.SetKeyTangentMode((object)thisKeyframe, 1, TangentMode.Linear);
			KeyframeUtil.SetKeyTangentMode((object)nextKeyframe, 0, TangentMode.Linear);


			curve.MoveKey(i, 	 thisKeyframe);
			curve.MoveKey(nextI, nextKeyframe);
		}


		public static void addBoneAnimationToClip(AnimationClip clip, Dictionary<string, SpineBoneAnimation> bonesAnimation,
		                                          SpineData spineData, Dictionary<string, GameObject> boneGOByName, float ratio){
			foreach(KeyValuePair<string,SpineBoneAnimation> kvp in bonesAnimation){
				string boneName = kvp.Key;
				GameObject boneGO = boneGOByName[boneName];
				SpineBoneAnimation boneAnimation = kvp.Value;
				string bonePath = spineData.bonePathByName[boneName];
				if (boneAnimation.translate != null && boneAnimation.translate.Count > 0){
					AnimationCurve curveX = new AnimationCurve();
					AnimationCurve curveY = new AnimationCurve();
					JsonData[] curveData = new JsonData[boneAnimation.translate.Count];
					for (int i = 0; i < boneAnimation.translate.Count; i++) {
						Keyframe keyFrameX = new Keyframe((float)boneAnimation.translate[i].time, boneGO.transform.localPosition.x + (float)boneAnimation.translate[i].x * ratio);
						Keyframe keyFrameY = new Keyframe((float)boneAnimation.translate[i].time, boneGO.transform.localPosition.y + (float)boneAnimation.translate[i].y * ratio);
						curveX.AddKey(keyFrameX);
						curveY.AddKey(keyFrameY);					
						curveData[i] = boneAnimation.translate[i].curve;
					}

					setTangents(curveX, curveData);
					setTangents(curveY, curveData);
					AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(bonePath,typeof(Transform),"m_LocalPosition.x") ,curveX);
					AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(bonePath,typeof(Transform),"m_LocalPosition.y") ,curveY);


				} 

				if (boneAnimation.rotate != null && boneAnimation.rotate.Count > 0){
					AnimationCurve localRotationX = new AnimationCurve();
					AnimationCurve localRotationY = new AnimationCurve();
					AnimationCurve localRotationZ = new AnimationCurve();
					AnimationCurve localRotationW = new AnimationCurve();

					JsonData[] curveData = new JsonData[boneAnimation.rotate.Count];
					for (int i = 0; i < boneAnimation.rotate.Count; i++) {
						float origAngle = (float)boneAnimation.rotate[i].angle;
						if (origAngle > 0)
							origAngle = origAngle > 180 ? origAngle - 360 : origAngle;
						else 
							origAngle = origAngle < -180 ? origAngle + 360 : origAngle;

						float newZ = boneGO.transform.localRotation.eulerAngles.z + origAngle;

						Quaternion angle = Quaternion.Euler(0,0,newZ);
						float time = (float)boneAnimation.rotate[i].time;

						curveData[i] = boneAnimation.rotate[i].curve;

						localRotationX.AddKey(new Keyframe(time, angle.x));
						localRotationY.AddKey(new Keyframe(time, angle.y));
						localRotationZ.AddKey(new Keyframe(time, angle.z));
						localRotationW.AddKey(new Keyframe(time, angle.w));

					}

					fixAngles  (localRotationX   , curveData);
					setTangents(localRotationX   , curveData);

					fixAngles  (localRotationY   , curveData);
					setTangents(localRotationY   , curveData);

					fixAngles  (localRotationZ   , curveData);
					setTangents(localRotationZ   , curveData);

					fixAngles  (localRotationW   , curveData);
					setTangents(localRotationW   , curveData);

					AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bonePath,typeof(Transform),"m_LocalRotation.x"), localRotationX);
					AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bonePath,typeof(Transform),"m_LocalRotation.y"), localRotationY);
					AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bonePath,typeof(Transform),"m_LocalRotation.z"), localRotationZ);
					AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bonePath,typeof(Transform),"m_LocalRotation.w"), localRotationW);

				} 

				if (boneAnimation.scale != null && boneAnimation.scale.Count > 0){
					AnimationCurve scaleX = new AnimationCurve();
					AnimationCurve scaleY = new AnimationCurve();
					JsonData[] curveData = new JsonData[boneAnimation.scale.Count];
					for (int i = 0; i < boneAnimation.scale.Count; i++) {
						Keyframe keyFrameX = new Keyframe((float)boneAnimation.scale[i].time, boneGO.transform.localScale.x * (float)boneAnimation.scale[i].x);
						Keyframe keyFrameY = new Keyframe((float)boneAnimation.scale[i].time, boneGO.transform.localScale.y * (float)boneAnimation.scale[i].y);
						curveData[i] = boneAnimation.scale[i].curve;
						scaleX.AddKey(keyFrameX);
						scaleY.AddKey(keyFrameY);					
					}

					setTangents(scaleX,curveData);
					setTangents(scaleY,curveData);

					clip.SetCurve(bonePath, typeof(Transform),"localScale.x",scaleX);
					clip.SetCurve(bonePath, typeof(Transform),"localScale.y",scaleY);
				} 

			}
		}


		static void fixAngles(AnimationCurve curve, JsonData[] curveData){
			if (curve.keys.Length <3)
				return;
			int fullTurn = 0;
			bool forward = true;
			float currValue, previousValue, diff;
			for (int previousI=0, i = 1; i < curve.keys.Length; previousI= i++) {
				if (curveData[previousI] != null &&  curveData[previousI].IsString &&  ((string)curveData[previousI]).Equals("stepped"))
					continue;

				currValue = curve.keys[i].value;
				previousValue = curve.keys[previousI].value;

				while ((currValue - previousValue) > 180 ){
					currValue -= 360;
				}

				while ((currValue - previousValue) < -180){
					currValue += 360;
				}
				if (curve.keys[i].value != currValue){
					curve.MoveKey(i, new Keyframe(curve.keys[i].time , currValue));
				}
			}
		}


		public static AnimationClip AddClipToAnimatorComponent(GameObject animatedObject, AnimationClip newClip)
		{
			Animator animator = animatedObject.GetComponent<Animator>();
			if ( animator == null)
				animator = animatedObject.AddComponent<Animator>();
			AnimatorController animatorController = AnimatorController.GetEffectiveAnimatorController(animator);
			if (animatorController == null)
			{
				string path =  Path.GetDirectoryName( AssetDatabase.GetAssetPath(newClip)) +"/"+animatedObject.name+".controller";

             	AnimatorController controllerForClip = AnimatorController.CreateAnimatorControllerAtPathWithClip(path, newClip);
				AnimatorController.SetAnimatorController(animator, controllerForClip);
				if (controllerForClip != null)
					return newClip;
				else
					return null;
			}
			else
			{
				AnimatorController.AddAnimationClipToController(animatorController, newClip);
				return newClip;
			}
		}


		public static Quaternion getAttachmentRotation(SpineSkinAttachment spineSkinAttachment, bool rotated = false){
			if (rotated)
				return Quaternion.Euler(0.0f, 0.0f, (float)spineSkinAttachment.rotation - 90.0f);
			else
				return Quaternion.Euler(0.0f, 0.0f, (float)spineSkinAttachment.rotation);
		}

		public static Vector3 getAttachmentPosition(SpineSkinAttachment spineSkinAttachment, float ratio, float z){
			return new Vector3((float)spineSkinAttachment.x * ratio, (float)spineSkinAttachment.y * ratio, z);
		}

		public static Vector3 getAttachmentScale(SpineSkinAttachment spineSkinAttachment){
			return new Vector3((float)spineSkinAttachment.scaleX, (float)spineSkinAttachment.scaleY, 1.0f);
		}

		public static Color32? hexStringToColor32(string hex){
			if (hex == null)
				return null;
			int rgba = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
			return new Color32((byte)((rgba & 0xff000000)>> 0x18),      
			                   (byte)((rgba & 0xff0000)>> 0x10),   
			                   (byte)((rgba & 0xff00) >> 8),
			                   (byte)(rgba & 0xff));
		}
	}
}
