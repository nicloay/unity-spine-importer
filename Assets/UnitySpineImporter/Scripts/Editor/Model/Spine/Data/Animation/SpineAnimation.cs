using System.Collections.Generic;
using LitJson;

namespace UnitySpineImporter{
	public class SpineAnimation {
		public Dictionary< string, SpineBoneAnimation > bones;
		public Dictionary< string, SpineSlotAnimation > slots;
		public List< SpineDrawOrderAnimation > draworder;
		public List< JsonData > events;
	}
}