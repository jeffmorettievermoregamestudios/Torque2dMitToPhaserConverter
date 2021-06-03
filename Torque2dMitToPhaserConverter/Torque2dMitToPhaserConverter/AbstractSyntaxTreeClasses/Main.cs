﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses
{
    public class Main : CodeBlock
    {
        public PhaserProjectConfig PhaserProjectConfig { get; set; }
        public List<CodeBlock> InitCode { get; set; }
        public List<CodeBlock> MainModuleCreateFunctionCall { get; set; }

        public override string ConvertToCode()
        {
            string mainCodeString = File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\StubProject.txt");

            mainCodeString = mainCodeString.Replace("**{HTML_HEAD_ELEMENT}**", GenerateHtmlHeadElement());

            mainCodeString = mainCodeString.Replace("**{PHASER_PROJECT_TYPE}**", PhaserProjectConfig.PhaserProjectType);
            mainCodeString = mainCodeString.Replace("**{PHASER_PROJECT_WIDTH}**", PhaserProjectConfig.PhaserProjectWidthInPixels.ToString());
            mainCodeString = mainCodeString.Replace("**{PHASER_PROJECT_HEIGHT}**", PhaserProjectConfig.PhaserProjectHeightInPixels.ToString());

            var sceneListString = "[";

            foreach (var sceneClass in GlobalVars.PhaserCodeRepo.PhaserSceneList)
            {
                sceneListString += " " + sceneClass + ",";
            }

            sceneListString = sceneListString.Substring(0, sceneListString.Length - 1);
            sceneListString += " ]";

            mainCodeString = mainCodeString.Replace("**{PHASER_SCENES}**", sceneListString);

            mainCodeString = mainCodeString.Replace("**{INIT_CODE}**", (new CssFontFaceStylesTemplate()).ConvertToCode());

            var mainModuleCreateFunctionCallAsString = "let " + GlobalVars.StartingPointVariableName + " = new " + GlobalVars.Torque2dProjectModuleName + "();\n";
            mainModuleCreateFunctionCallAsString +=
                GlobalVars.StartingPointVariableName + ".create();";

            mainCodeString = mainCodeString.Replace("**{MAIN_MODULE_CREATE_FUNCTION_CALL}**", mainModuleCreateFunctionCallAsString);

            return mainCodeString;
        }

        public void WriteToFileSystem()
        {
            File.WriteAllText(GlobalVars.PhaserProjectOutputFolder + "\\index.html", this.ConvertToCode());
        }

        private string GenerateHtmlHeadElement()
        {
            var result = "";

            result += $"<script src='{GlobalVars.PhaserUtilFolder}/JavascriptUtil.js'></script>\n";
            result += $"<script src='{GlobalVars.PhaserUtilFolder}/MathConvertUtil.js'></script>\n";
            result += $"<script src='{GlobalVars.PhaserUtilFolder}/SceneUtil.js'></script>\n";
            result += $"<script src='{GlobalVars.PhaserGlobalVarsFolder}/{GlobalVars.PhaserGlobalVarsFilename}'></script>\n";
            result += $"<script src='{GlobalVars.PhaserClassesFolder}/SpriteBaseClass.js'></script>\n";
            result += $"<script src='{GlobalVars.PhaserClassesFolder}/SceneBaseClass.js'></script>\n";

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var relativePath = codeFile.Filename.Replace(Torque2dToPhaserConverterFunctionLibrary.GetModuleFolderPath(), GlobalVars.PhaserProjectOutputFolder);
                var fileExtensionIdx = relativePath.LastIndexOf(".cs");

                if (fileExtensionIdx != -1)
                {
                    relativePath = relativePath.Substring(0, fileExtensionIdx) + ".js";
                }

                relativePath = relativePath.Replace(GlobalVars.PhaserProjectOutputFolder, "");
                relativePath = relativePath.Replace("\\", "/");

                if (relativePath[0] == '/')
                {
                    relativePath = relativePath.Substring(1);
                }

                result += "<script src='" + relativePath + "'></script>\n";
            }

            result += GeneratePhaserClassScriptIncludes();
            
            if (GlobalVars.GenerateScriptIncludeForModuleClass)
            {
                result += $"<script src='{GlobalVars.PhaserClassesFolder}/{GlobalVars.Torque2dProjectModuleName}Class.js'></script>\n";
            }

            return result;
        }

        private string GeneratePhaserClassScriptIncludes()
        {
            var result = "";

            if (GlobalVars.PhaserCodeRepo.PhaserClassHierarchyRoot.SubClasses != null)
            {
                foreach (var phaserClass in GlobalVars.PhaserCodeRepo.PhaserClassHierarchyRoot.SubClasses)
                {
                    if (!phaserClass.IsAPhaserApiClass)
                    {
                        result += "<script src='" + GlobalVars.PhaserClassesFolder + "/" + phaserClass.ClassName + "Class.js'></script>\n";
                    }

                    if (phaserClass.SubClasses != null)
                    {
                        result += GeneratePhaserClassScriptIncludesHelper(phaserClass);
                    }
                }
            }

            return result;
        }

        private string GeneratePhaserClassScriptIncludesHelper(PhaserClassHierarchyObj phaserClass)
        {
            var result = "";

            foreach (var subClass in phaserClass.SubClasses)
            {
                if (!subClass.IsAPhaserApiClass)
                {
                    result += "<script src='" + GlobalVars.PhaserClassesFolder + "/" + subClass.ClassName + "Class.js'></script>\n";
                }

                if (subClass.SubClasses != null)
                {
                    result += GeneratePhaserClassScriptIncludesHelper(subClass);
                }
            }

            return result;
        }
    }
}