using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses;
using Torque2dMitToPhaserConverter.Torque2dScenes.Torque2dAssets;

namespace Torque2dMitToPhaserConverter
{
    public class Torque2dModuleDatabase
    {
        public List<Torque2dAsset> Torque2dAssetList { get; set; }
        public List<ClassMethod> ClassMethodRepo { get; set; }
        public List<CodeFile> CodeFileList { get; set; }
    }
}
