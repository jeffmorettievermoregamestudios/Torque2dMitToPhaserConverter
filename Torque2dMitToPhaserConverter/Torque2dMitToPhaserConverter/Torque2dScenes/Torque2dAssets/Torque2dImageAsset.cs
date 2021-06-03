using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.Torque2dScenes.Torque2dAssets
{
    public class Torque2dImageAsset : Torque2dAsset
    {
        public int? CellCountX { get; set; }
        public int? CellCountY { get; set; }
        public int? CellWidth { get; set; }
        public int? CellHeight { get; set; }

        public bool isCells()
        {
            return CellCountX.HasValue && CellCountY.HasValue && CellWidth.HasValue && CellHeight.HasValue;
        }
    }
}
