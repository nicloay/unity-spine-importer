
namespace UnitySpineImporter{
	public class SpineSkinAttachment {
		public string name;
		public string type = "region"; //region|regionsequence|boundingbox  . TODO - regionsequence not implemented
		public double x    = 0;
		public double y    = 0;
		public double scaleX = 1;
		public double scaleY = 1;
		public double rotation = 0;
		public double width; //TODO - not implemented
		public double height; //TODO - not implemented

		//- regionsequence block TODO - not implementd
		public double fps; //TODO - not implemented
		public string mode; //TODO - not implemented (forward, backward, forwardLoop, backwardLoop, pingPong, or random)
		//

		//- boundingbox block
		public double[] vertices; 
	}
}