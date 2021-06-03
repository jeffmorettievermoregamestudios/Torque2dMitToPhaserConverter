using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.Torque2dScenes.Torque2dAssets
{
    public class Torque2dAsset
    {
        public string Name { get; set; }

        // NOTE: Not all assets actually need an AssetFile (example: an AnimationAsset)
        public FileInfo AssetFile { get; set; }
    }
}
