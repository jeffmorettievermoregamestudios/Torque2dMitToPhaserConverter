using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses;
using Torque2dMitToPhaserConverter.Torque2dScenes.Torque2dAssets;

namespace Torque2dMitToPhaserConverter
{
    public class PhaserCodeRepo
    {
        public List<PhaserClass> PhaserClassList { get; set; }

        // stores the root of all PhaserClassHierarchyObjs (ie all children of this root do not have a parent class)
        public PhaserClassHierarchyObj PhaserClassHierarchyRoot { get; set; }

        public List<GlobalVariable> PhaserGlobalVars { get; set; }

        public List<string> PhaserSceneList { get; set; }

        // A mapping that will map the Scene class name (key) to a List of strings (where each string is the Asset Name of a Torque2dAnimationAsset associated with this Scene class)
        public Dictionary<string, List<string>> PhaserSceneListAndAnimationsMap { get; set; }
    }
}
