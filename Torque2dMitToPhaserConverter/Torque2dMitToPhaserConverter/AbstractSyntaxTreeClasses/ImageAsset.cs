using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class ImageAsset : PhaserAsset
    {
        public string AssetKey { get; set; }
        public string ResourceUrl { get; set; }

        public override string ConvertToCode()
        {
            // example of code output:
            // this.load.image('sky', 'assets/skies/space3.png');

            return "this.load.image('" + AssetKey + "', '" + ResourceUrl + "');\n";
        }
    }
}
