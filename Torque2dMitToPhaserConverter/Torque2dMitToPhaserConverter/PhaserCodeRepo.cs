using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses;

namespace Torque2dMitToPhaserConverter
{
    public class PhaserCodeRepo
    {
        public List<PhaserClass> PhaserClassList { get; set; }

        // stores the root of all PhaserClassHierarchyObjs (ie all children of this root do not have a parent class)
        public PhaserClassHierarchyObj PhaserClassHierarchyRoot { get; set; }

        public List<GlobalVariable> PhaserGlobalVars { get; set; }

        public List<string> PhaserSceneList { get; set; }
    }
}
