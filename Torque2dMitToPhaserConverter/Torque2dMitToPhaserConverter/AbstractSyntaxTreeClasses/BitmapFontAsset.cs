using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class BitmapFontAsset : PhaserAsset
    {
        public string AssetKey { get; set; }
        public string ResourceUrl { get; set; }

        public override string ConvertToCode()
        {
            var pngResourceUrl = ResourceUrl.Substring(0, ResourceUrl.LastIndexOf(".")) + "Font.png";
            var xmlResourceUrl = ResourceUrl.Substring(0, ResourceUrl.LastIndexOf(".")) + "Font.xml";

            return "this.load.bitmapFont('" + AssetKey + "', '" + pngResourceUrl + "', '" + xmlResourceUrl + "');\n";
        }
    }
}
