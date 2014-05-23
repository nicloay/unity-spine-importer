using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnitySpineImporter{
	[Serializable]
	public class Slot {

		public string name;
		public string bone;
		public string defaultAttachmentName;
		public Color32? color;
		public GameObject gameObject;
		public Attachment[] attachments;


		Dictionary<string, Attachment> _attachmentByName;
		public Dictionary<string, Attachment> attachmentByName{
			get{
				if (_attachmentByName == null){
					_attachmentByName = new Dictionary<string, Attachment>();
					if (attachments == null)
						attachments = new Attachment[0];
					for (int i = 0; i < attachments.Length; i++) {
						_attachmentByName.Add(attachments[i].name, attachments[i]);
					}
				}
				return _attachmentByName;
			}
		}

		public Slot(){}

		//TODO probably can delete it
		public Slot (string bone, string slot, string attachment = null, Color32? color = null)
		{
			this.bone = bone;
			this.name = slot;
			this.defaultAttachmentName = attachment;
			this.color = color;
		}

		public void hideAllAttachments(){
			if (attachments == null)
				return;
			foreach(Attachment a in attachments){
				a.gameObject.SetActive(false);
			}
		}

		public void showAttachment(string attachmentName){
			hideAllAttachments();
			attachmentByName[attachmentName].gameObject.SetActive(true);
		}

		public void showDefaultAttachment(){
			if (string.IsNullOrEmpty(defaultAttachmentName))
				hideAllAttachments();
			else
				showAttachment(defaultAttachmentName);
		}

		public void addAttachment(Attachment attachment){
			if (attachments == null){
				attachments = new Attachment[]{attachment};
			} else {
				Attachment[] newA = new Attachment[attachments.Length + 1];
				Array.Copy(attachments, newA,attachments.Length);
				newA[newA.Length -1] = attachment;
				attachments  = newA;
			}
			if (_attachmentByName == null)
				_attachmentByName = new Dictionary<string, Attachment>();
			_attachmentByName.Add(attachment.name, attachment);
		}
	}
}