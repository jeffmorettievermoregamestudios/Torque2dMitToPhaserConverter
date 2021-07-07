using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class CssFontFaceStyle : PhaserAsset
    {
        public string Name { get; set; }
        public string ResourceUrl { get; set; }

        public override string ConvertToCode()
        {
            return "'@font-face { font-family: \"" + Name + "\"; src: url(\"" + ResourceUrl + "\") format(\"truetype\"); }';";
        }
    }
}
