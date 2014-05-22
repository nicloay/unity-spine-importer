using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace UnitySpineImporter{
	public class SpineMultiatlasCreationException: System.Exception{
		public SpineMultiatlasCreationException(string message):base(message)
		{
		}
	}


	public class SpineMultiatlas : List<SpineAtlas> {

		public static  SpineMultiatlas deserializeFromFile(string multiatlasFilePath){
			SpineMultiatlas multiAtlas = new SpineMultiatlas();
			if (!File.Exists(multiatlasFilePath))
				throw new SpineMultiatlasCreationException("provided file does not exists");
			using(StreamReader streamReader = new StreamReader(multiatlasFilePath)){
				string line;
				string spriteNameAfterProps = "";
				bool setMainProps = false;
				SpineAtlas spineAtlas = null;
				SpineSprite sprite    = null;
				while((line = streamReader.ReadLine())!=null){
					if (line==""){
						setMainProps = true;
					} else {
						if (setMainProps){
							spineAtlas  = new SpineAtlas();
							multiAtlas.Add(spineAtlas);
							spineAtlas.imageName = line;
							Dictionary<string,string> keyValue = new Dictionary<string, string >();
							string[] kvp;
							while( (kvp= streamReader.ReadLine().Split(':')).Length == 2)
								keyValue.Add(kvp[0].Trim(), kvp[1].Trim());
							
							spineAtlas.format = keyValue["format"];
							spineAtlas.filter = keyValue["filter"];
							spineAtlas.repeat = keyValue["repeat"];
							
							spriteNameAfterProps = kvp[0];
							spineAtlas.sprites = new List<SpineSprite>();
							setMainProps = false;
							
						} 
						
						if (!setMainProps){
							sprite = new SpineSprite();
							
							if (string.IsNullOrEmpty( spriteNameAfterProps)){
								sprite.name     = line;		
							} else {
								sprite.name     = spriteNameAfterProps;
								spriteNameAfterProps  = "";
							}
							try{
								sprite.rotate   = bool.Parse(streamReader.ReadLine().Split(':')[1]);
								sprite.xy       = SpineUtil.lineToVector2(streamReader.ReadLine());
								sprite.size     = SpineUtil.lineToVector2(streamReader.ReadLine());
								sprite.orig     = SpineUtil.lineToVector2(streamReader.ReadLine());
								sprite.offset   = SpineUtil.lineToVector2(streamReader.ReadLine());
								sprite.index    = int.Parse(streamReader.ReadLine().Split(':')[1]);
							} catch (System.FormatException e) {
								throw new SpineMultiatlasCreationException("can't parse source file \n" + multiatlasFilePath +"\n"+e);
							}
							spineAtlas.sprites.Add(sprite);
						}
					}
				}
			}

			if (multiAtlas.Count == 0)
				throw new SpineMultiatlasCreationException("don't have any atlases in provided file");
			return multiAtlas;
		}
	}
}