using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.Torque2dScenes.Torque2dAssets
{
    public class Torque2dAnimationAsset : Torque2dAsset
    {
        public string Image { get; set; }
        public string AnimationFrames { get; set; }
        public double? AnimationTime { get; set; }
        public bool? AnimationCycle { get; set; }
    }
}
