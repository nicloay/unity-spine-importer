using System.IO;
using System.Collections.Generic;
using LitJson;
using UnityEngine;

namespace UnitySpineImporter{
	public class SpineDatatCreationException:System.Exception{
		public SpineDatatCreationException(string message):base(message)
		{
		}
	}

	public class SpineData{	

		public List<SpineBone> bones;
		public List<SpineSlot> slots;

		public SpineSkins skins;

		public Dictionary<string, SpineAnimation> animations;


		public Dictionary<string, SpineBone> boneByName;
		public Dictionary<string, SpineSlot> slotByName;
		public Dictionary<string, int>       slotOrder;
		public Dictionary<string, string>    bonePathByName;
		public Dictionary<string, string>    slotPathByName;

		public string defaultSkinName;
		public string[] skinNames;
		public string[] defaultPoseSlots;
		public Dictionary<string,string> slotDefaultAttachments;



		public static SpineData deserializeFromFile(string spineDataFilePath){
			SpineData data = null;
			if (!File.Exists(spineDataFilePath))
				throw new SpineDatatCreationException("provided file does not exists");
			try{
				data = LitJson.JsonMapper.ToObject<SpineData>(File.ReadAllText(spineDataFilePath));
			} catch (LitJson.JsonException e){
				throw new SpineDatatCreationException("problem with parse json data \n"+e.Message);
			}
			setCachedData(data);
			fixeAttachmentNamesIfOmited(data);
			return data;
		}

		static void fixeAttachmentNamesIfOmited (SpineData data)
		{
			foreach(KeyValuePair<string, SpineSkinSlots>kvp in data.skins){
				string skinName = kvp.Key;
				foreach(KeyValuePair<string, SpineSkinSlotAttachments>kvp2 in  data.skins[skinName]){
					string slotName = kvp2.Key;
					foreach(KeyValuePair<string, SpineSkinAttachment> kvp3 in data.skins[skinName][slotName]){
						string attachmentName = kvp3.Key;
						SpineSkinAttachment attachment = kvp3.Value;
						if (string.IsNullOrEmpty(attachment.name))
							attachment.name = attachmentName; // we set actualAttachment(sprite name) here in case if it empty it equal to attachment name
					}
				}
			}
		}

		static void setCachedData (SpineData data)
		{
			data.slotByName = new Dictionary<string, SpineSlot>();


			data.boneByName = new Dictionary<string, SpineBone>();
			for (int i = 0; i < data.bones.Count; i++) {
				data.boneByName.Add(data.bones[i].name, data.bones[i]);
			}

			data.bonePathByName = new Dictionary<string, string>();

			foreach (SpineBone bone in data.bones){
				string path = "";
				SpineBone b = bone;
				do {
					path=b.name+"/"+path;
					if (!string.IsNullOrEmpty( b.parent))
						b = data.boneByName[b.parent];
					else 
						b = null;
				} while (b!=null);

				if (path.Length >0)
					path = path.Remove(path.Length - 1);

				data.bonePathByName.Add(bone.name, path);			
			}


			data.slotOrder =  new Dictionary<string, int>();

			data.slotPathByName = new Dictionary<string, string>();
			data.slotDefaultAttachments = new Dictionary<string, string>();
			for (int i = 0; i < data.slots.Count; i++) {
				string slotName = data.slots[i].name;
				string defaultAttachment = data.slots[i].attachment;
				data.slotOrder.Add(slotName, i);
				string boneName = data.slots[i].bone;
				string bonePath = data.bonePathByName[boneName];
				string slotPath = bonePath+"/" + SpineUtil.getSlotGOName(slotName);
				data.slotPathByName.Add(slotName, slotPath);
				data.slotDefaultAttachments.Add(slotName, defaultAttachment);
			}
		}

	}
}