using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.Torque2dScenes.Torque2dAssets
{
    public static class Torque2dAssetLibrary
    {
        public static void CreateTorque2dImageAsset(string assetName, string imageFile, int? cellCountX, int? cellCountY, int? cellWidth, int? cellHeight, FileInfo assetFile)
        {
            var newImageAsset = new Torque2dImageAsset();
            newImageAsset.Name = assetName;
            newImageAsset.AssetFile = new FileInfo(assetFile.DirectoryName + "\\" + imageFile);

            newImageAsset.CellCountX = cellCountX;
            newImageAsset.CellCountY = cellCountY;
            newImageAsset.CellWidth = cellWidth;
            newImageAsset.CellHeight = cellHeight;

            GlobalVars.Torque2dModuleDatabase.Torque2dAssetList.Add(newImageAsset);
        }

        // NOTE: Will use 'Custom Webfont's in Phaser IO for working with fonts
        public static void CreateTorque2dFontAsset(string assetName, string fontFile, FileInfo assetFile)
        {
            var newFontAsset = new Torque2dFontAsset();
            newFontAsset.Name = assetName;
            newFontAsset.AssetFile = new FileInfo(assetFile.DirectoryName + "\\" + fontFile);

            GlobalVars.Torque2dModuleDatabase.Torque2dAssetList.Add(newFontAsset);
        }

        public static void CreateTorque2dAnimationAsset(string assetName, string image, string animationFrames, double? animationTime, bool? animationCycle, FileInfo assetFile)
        {
            var newAnimationAsset = new Torque2dAnimationAsset();
            newAnimationAsset.Name = assetName;
            newAnimationAsset.Image = image;
            newAnimationAsset.AnimationFrames = animationFrames;
            newAnimationAsset.AnimationTime = animationTime;
            newAnimationAsset.AnimationCycle = animationCycle;

            GlobalVars.Torque2dModuleDatabase.Torque2dAssetList.Add(newAnimationAsset);
        }
    }
}
