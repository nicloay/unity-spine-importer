using System.Collections.Generic;

namespace UnitySpineImporter{
	public class SpineAnimation {
		public Dictionary<string, SpineBoneAnimation> bones;
		public Dictionary<string, SpineSlotAnimation> slots;
		public List<SpineDrawOrderAnimation> draworder;

	}
}