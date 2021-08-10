using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torque2dMitToPhaserConverter.Torque2dScenes;


namespace Torque2dMitToPhaserConverter
{
    public static class GlobalVars
    {
        // Legacy switch for upconverting legacy T2D 'objects' to their newer Torque 2D MIT counterpart
        // (ie like changing a reference from a t2dSceneObject to instead a SceneObject).  This will then
        // allow the converter to properly handle this items later on
        public static bool PerformUpconversionFromOlderTorqueObjects = true;
        public static bool IsProcessingConversion { get; set; }

        public static string Torque2dProjectModulesFolder { get; set; }
        public static string PhaserProjectOutputFolder { get; set; }

        public static string Torque2dProjectAppCoreVersion { get; set; }
        public static string Torque2dProjectModuleVersion { get; set; }

        public static string Torque2dProjectModuleName { get; set; }

        public static float Torque2dCameraSizeWidth { get; set; }
        public static float Torque2dCameraSizeHeight { get; set; }

        public static string PhaserProjectType = "Phaser.AUTO";
        public static int PhaserProjectWidthInPixels = 800;
        public static int PhaserProjectHeightInPixels = 600;

        public static DirectoryInfo Torque2dAssetsFolder { get; set; }

        public static Torque2dModuleDatabase Torque2dModuleDatabase { get; set; }

        public static List<Torque2dScene> Torque2dSceneList { get; set; }

        public static PhaserAssetRepo PhaserAssetRepo { get; set; }

        public static string StartingPointVariableName = "entranceToOurPhaserGameWithThisVariableMadeByT2DConverter";

        public static string PhaserClassesFolder = "classes";
        public static string PhaserGlobalVarsFolder = "globalVars";
        public static string PhaserGlobalVarsFilename = "globalVars.js";

        public static string PhaserUtilFolder = "util";

        public static PhaserCodeRepo PhaserCodeRepo { get; set; }

        public static bool GenerateScriptIncludeForModuleClass = true;

        public static bool AddSceneWindowRemovalWarningComments = true;
        public static bool AddActionMapManualConversionWarningComments = true;
        public static bool AddVariablePostfixStringToPreventClassAndVariableCollision = true;
    }
}
