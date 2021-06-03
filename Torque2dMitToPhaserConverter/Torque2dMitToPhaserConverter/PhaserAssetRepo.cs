using System.Collections.Generic;
using Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses;

namespace Torque2dMitToPhaserConverter
{
    public class PhaserAssetRepo
    {
        public List<PhaserAsset> PhaserAssetList { get; set; }
        public List<CssFontFaceStyle> PhaserCssFontFaceStyleList { get; set; }
    }
}
