using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses;
using Torque2dMitToPhaserConverter.Torque2dScenes.Torque2dAssets;

namespace Torque2dMitToPhaserConverter
{
    public static class PhaserAssetLibrary
    {
        public static void GenerateAllPhaserAssets()
        {
            GlobalVars.PhaserAssetRepo = new PhaserAssetRepo();
            GlobalVars.PhaserAssetRepo.PhaserAssetList = GenerateCodeForAssets();
            GlobalVars.PhaserAssetRepo.PhaserCssFontFaceStyleList = GenerateCodeForCssFontFaceStyles();
        }

        public static List<PhaserAsset> GenerateCodeForAssets()
        {
            var result = new List<PhaserAsset>();

            // NOTE: Will 'not' be generating code from CreateTorque2dFontAssets here.  Done in GenerateCodeForCssFontFaceStyles
            foreach (var torque2dAsset in GlobalVars.Torque2dModuleDatabase.Torque2dAssetList)
            {
                if (torque2dAsset.GetType() == typeof(Torque2dImageAsset))
                {
                    var t2dImageAsset = (Torque2dImageAsset)torque2dAsset;

                    if (t2dImageAsset.isCells())
                    {
                        result.Add(GenerateSpritesheetAsset(t2dImageAsset));
                    }
                    else
                    {
                        result.Add(GenerateImageAsset(t2dImageAsset));
                    }
                }
            }

            return result;
        }

        public static List<CssFontFaceStyle> GenerateCodeForCssFontFaceStyles()
        {
            var result = new List<CssFontFaceStyle>();

            foreach (var torque2dAsset in GlobalVars.Torque2dModuleDatabase.Torque2dAssetList)
            {
                if (torque2dAsset.GetType() == typeof(Torque2dFontAsset))
                {
                    var t2dFontAsset = (Torque2dFontAsset)torque2dAsset;

                    result.Add(GenerateCssFontFaceStyle(t2dFontAsset));
                }
            }

            return result;
        }

        public static string GetResourceUrl(Torque2dAsset torque2DAsset)
        {
            return "assets" + torque2DAsset.AssetFile.FullName
                .Replace(GlobalVars.Torque2dAssetsFolder.FullName, "")
                .Replace("\\", "/");
        }

        public static ImageAsset GenerateImageAsset(Torque2dImageAsset torque2dImageAsset)
        {
            var resourceUrl = GetResourceUrl(torque2dImageAsset);

            var result = new ImageAsset()
            {
                AssetKey = torque2dImageAsset.Name,
                ResourceUrl = resourceUrl,
                Torque2dAssetFileReference = torque2dImageAsset.AssetFile
            };

            return result;
        }

        public static SpritesheetAsset GenerateSpritesheetAsset(Torque2dImageAsset torque2dImageAsset)
        {
            var resourceUrl = GetResourceUrl(torque2dImageAsset);

            var endFrame = (torque2dImageAsset.CellCountX.Value * torque2dImageAsset.CellCountY.Value) - 1;

            var result = new SpritesheetAsset()
            {
                AssetKey = torque2dImageAsset.Name,
                ResourceUrl = resourceUrl,
                Torque2dAssetFileReference = torque2dImageAsset.AssetFile,
                FrameWidth = torque2dImageAsset.CellWidth.Value,
                FrameHeight = torque2dImageAsset.CellHeight.Value,
                EndFrame = endFrame
            };

            return result;
        }

        public static CssFontFaceStyle GenerateCssFontFaceStyle(Torque2dFontAsset torque2dFontAsset)
        {
            var filename = torque2dFontAsset.AssetFile.Name.Replace(torque2dFontAsset.AssetFile.Extension, ".ttf");
            var resourceUrl = "assets" + torque2dFontAsset.AssetFile.DirectoryName
                .Replace(GlobalVars.Torque2dAssetsFolder.FullName, "")
                .Replace("\\", "/") 
                + "/" + filename; 

            var result = new CssFontFaceStyle()
            {
                Name = torque2dFontAsset.Name,
                ResourceUrl = resourceUrl
            };

            return result;
        }
    }
}
