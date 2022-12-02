using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Torque2dMitToPhaserConverter.AbstractSyntaxTreeClasses;
using Torque2dMitToPhaserConverter.Torque2dScenes.Torque2dAssets;

namespace Torque2dMitToPhaserConverter
{
    public static class Torque2dToPhaserConverterFunctionLibrary
    {
        public static void ConvertProject()
        {
            // first check to make sure entered folder paths are created
            if (!Directory.Exists(GlobalVars.Torque2dProjectModulesFolder))
            {
                MessageBox.Show("The Torque 2D Project Folder entered does not seem to exist.  Please check that the Torque 2D Project ('modules') folder has been entered correctly.");
                GlobalVars.IsProcessingConversion = false;
                return;
            }

            if (!Directory.Exists(GlobalVars.PhaserProjectOutputFolder))
            {
                MessageBox.Show("The Phaser Project Folder entered does not seem to exist.  Please check that the Phaser Project Project (output) folder has been entered correctly.");
                GlobalVars.IsProcessingConversion = false;
                return;
            }

            // TODO:  Should have a system that can handle a project with more than just an 'AppCore' folder and a '<INSERT PROJECT NAME HERE>' folder

            // 1) Find AppCore module and handle appropriately
            var appCoreMainCsAsString = File.ReadAllText(GetAppCoreFolderPath() + "\\main.cs");

            GlobalVars.Torque2dProjectModuleName = GetModuleNameFromAppCoreMain(appCoreMainCsAsString);

            // initialize the Torque2dModuleDatabase (ie for storing all Torque2d items)
            GlobalVars.Torque2dModuleDatabase = new Torque2dModuleDatabase();
            GlobalVars.Torque2dModuleDatabase.Torque2dAssetList = new List<Torque2dAsset>();
            GlobalVars.Torque2dModuleDatabase.ClassMethodRepo = new List<ClassMethod>();
            GlobalVars.Torque2dModuleDatabase.CodeFileList = new List<CodeFile>();

            // initialize PhaserCodeRepo
            GlobalVars.PhaserCodeRepo = new PhaserCodeRepo();
            GlobalVars.PhaserCodeRepo.PhaserClassList = new List<PhaserClass>();
            GlobalVars.PhaserCodeRepo.PhaserClassHierarchyRoot = new PhaserClassHierarchyObj();
            GlobalVars.PhaserCodeRepo.PhaserClassHierarchyRoot.SubClasses = new List<PhaserClassHierarchyObj>();
            GlobalVars.PhaserCodeRepo.PhaserGlobalVars = new List<GlobalVariable>();
            GlobalVars.PhaserCodeRepo.PhaserSceneList = new List<string>();
            GlobalVars.PhaserCodeRepo.PhaserSceneListAndAnimationsMap = new Dictionary<string, List<string>>();

            // Will first want to iterate through all .taml files, and setup any resources (ie sprites) in the 'preload' function

            // Parse Torque2D assets (ie sprites, etc)
            ParseAllTorque2dAssets();

            // Generate Phaser assets (from Torque2D assets)
            PhaserAssetLibrary.GenerateAllPhaserAssets();

            // 2) Process all other .cs script files
            ProcessAllTorque2dTorquescriptFiles();

            if (GlobalVars.PerformUpconversionFromOlderTorqueObjects)
            {
                PerformUpconversionFromOlderTorqueObjects();
            }

            // 3) Perform a 2nd pass over the Code Files and process again as necessary
            bool finished2ndPasses = false;

            while (!finished2ndPasses)
            {
                finished2ndPasses = Process2ndPass();
            }

            // Remove any 'exec' calls from the code.  These were used by Torque2D to 'add' a cs file to be executed for the game.
            // Instead, will later handle by adding <script> tags for all js code files in the Repo.
            RemoveExecFunctionCalls();

            // Converts echo function calls to a console.log call instead
            ConvertEchoFunctionCalls();

            // Remove ModuleName prefix from assets
            RemoveModuleNamePrefixFromAssets();

            // process audio play function calls (alxPlay)
            ProcessAudioPlayFunctionCalls();

            // process audio stop function calls (alxStop)
            ProcessAudioStopFunctionCalls();

            // Populate Phaser GlobalVariables
            PopulatePhaserGlobalVar();

            // Propagate PhaserObjectType property of LocalVariables and GlobalVariables
            PropagatePhaserObjectTypeForLocalVariablesAndGlobalVariables();

            // Generate the Phaser Class Hierarchy (so we know how the class hierarchy & inheritance structure looks)
            ProcessPhaserClassHierarchy();

            // assigns the class name of the Scene to the Local/Global variables that are instantiated
            Process2ndPassForLocalVariableScenes();
            Process2ndPassForGlobalVariableScenes();

            // Processes the Phaser GlobalVariables and assigns them classes (when defined) for global Scene variables
            ProcessPhaserGlobalVarsWithSceneClasses();

            // Propagate Class property of LocalVariables and GlobalVariables
            PropagateClassForLocalVariablesAndGlobalVariables();

            // perform a 2nd pass over the whole codebase for processing sprites, and setting them up so that the Sprite constructor has the necessary
            // fields it needs to instantiate a sprite (ie the scene the sprite belongs to and the asset key of the sprite)
            Process2ndPassForLocalVariableSprites();
            Process2ndPassForGlobalVariableSprites();

            // perform a 2nd pass over the whole codebase for processing text objects, and setting them up so that the Text constructor has the necessary
            // fields it needs to instantiate a text (ie the scene the text belongs to and the font of the text)
            Process2ndPassForLocalVariablePhaserTexts();
            Process2ndPassForGlobalVariablePhaserTexts();

            // convert 'FontSize = X' field property to a 'setFontSize(X)' method call (will convert the size of the text properly with this)
            ProcessSetFontSizeMethodUsageForPhaserTexts();

            // adds the T2dFunctionsUtil class library to common t2dFunctions (ie changes 'getWord(...)' to 'T2dFunctionsUtil.getWord(...)' etc)
            PerformConversionForT2dFunctions();

            // Generates Code Files from templates (ie JavascriptUtil, SpriteBaseClass, etc)
            GenerateCodeFilesFromTemplates();

            // 8) Compile the list of Scenes created within our game (will need this list for later, when generating the index.html file)
            //    Also does some more Scene Processing
            CompileListOfScenesCreatedInOurGameAndDoMoreSceneProcessing();

            // performs a 2nd pass over the whole codebase, delaying the execution of code when starting a new scene and making a callback
            // once the scene is 'active', and thus resuming the code/execution from there
            Process2ndPassForWaitForSceneIsActiveCallbacks();

            // performs a 2nd pass for converting all 'SceneLayer' values (flips them to negative values, ie similar to multiplying by -1)
            Process2ndPassForSceneLayerValues();

            // process converting T2d (ie getRandom, strlen) function calls
            ProcessConvertingT2dFunctionCalls();

            // 4) Process classes/methods in torquescript files.  Will also remove the class methods etc from the object 
            //    model (so they are not generated later when generating the converted torquescript files)
            ProcessTorque2dTorquescriptFilesClassesAndMethods();

            // generate 'var' token for local variables in code (for the scope they belong too)
            GenerateVarTokensForLocalVariables();

            // generate 'var' token for local variables in code (for the scope they belong too) but this time for the PhaserClassList
            GenerateVarTokensForLocalVariablesInPhaserClasses();

            // process schedule function calls (ie convert to setTimeout calls instead, which javascript can use)
            ProcessScheduleCalls();
            ProcessScheduleCallsInPhaserClasses();

            // generate the actual javascript files for the Phaser classes
            GeneratePhaserClassJavascriptFiles();

            // 5) Generate a GlobalVars script file, to store all the global variables in our Phaser game (ie global variables converted from T2D)
            GenerateGlobalVarsJavascriptFile();

            // 7) Generate Preload Assets file
            GeneratePreloadAssetsFile();

            // ) Will now parse module Main and convert to Phaser code.  
            var indexHtmlObj = CreateStubProject();

            // 9) Write index.html to file system
            indexHtmlObj.WriteToFileSystem();

            // 10) Write all (remaining) torquescript files to file system
            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                codeFile.WriteToFileSystem();
            }

            // 11) Copy all asset files
            CopyAssetFiles();

            MessageBox.Show("Project has been successfully converted!");

            GlobalVars.IsProcessingConversion = false;
        }

        public static Main CreateStubProject()
        {
            // first create the stub index.html file as a start for the project
            var mainNode = CreateMainNode();
            return mainNode;
        }

        public static Main CreateMainNode()
        {
            var mainNode = new Main();

            mainNode.PhaserProjectConfig = CreatePhaserProjectConfigNode();

            return mainNode;
        }

        public static PhaserProjectConfig CreatePhaserProjectConfigNode()
        {
            var phaserProjectConfigNode = new PhaserProjectConfig();

            phaserProjectConfigNode.PhaserProjectType = GlobalVars.PhaserProjectType;

            // TODO:  Retrieve Width and Height from Torque2D project
            // FOR NOW:  Will just use the GlobalVars to determine Width and Height
            phaserProjectConfigNode.PhaserProjectWidthInPixels = GlobalVars.PhaserProjectWidthInPixels;
            phaserProjectConfigNode.PhaserProjectHeightInPixels = GlobalVars.PhaserProjectHeightInPixels;

            return phaserProjectConfigNode;
        }

        public static string GetModuleNameFromAppCoreMain(string appCoreMainCsAsString)
        {
            string literalToSearch = "ModuleDatabase.loadExplicit(".ToUpper();
            int idxBegin = appCoreMainCsAsString.ToUpper().IndexOf(literalToSearch) + literalToSearch.Length + 1;
            int idxEnd = appCoreMainCsAsString.ToUpper().IndexOf(")", idxBegin);

            string moduleName = appCoreMainCsAsString.Substring(idxBegin, idxEnd - idxBegin - 1);

            return moduleName;
        }

        public static string GetAppCoreFolderPath()
        {
            return GlobalVars.Torque2dProjectModulesFolder + "\\AppCore\\"
                                             + GlobalVars.Torque2dProjectAppCoreVersion;
        }

        public static string GetModuleFolderPath()
        {
            return GlobalVars.Torque2dProjectModulesFolder + "\\" + GlobalVars.Torque2dProjectModuleName + "\\"
                                             + GlobalVars.Torque2dProjectModuleVersion;
        }

        public static void ParseAllTorque2dAssets()
        {
            XDocument moduleTaml = XDocument.Load(GetModuleFolderPath() + "\\module.taml");

            var assetsFolder = (from declaredAssets in moduleTaml.Root.Descendants("DeclaredAssets")
                                select declaredAssets.Attribute("Path").Value).FirstOrDefault();

            var assetExtension = (from declaredAssets in moduleTaml.Root.Descendants("DeclaredAssets")
                                  select declaredAssets.Attribute("Extension").Value).FirstOrDefault();

            var isRecurse = (from declaredAssets in moduleTaml.Root.Descendants("DeclaredAssets")
                             select declaredAssets.Attribute("Recurse").Value).FirstOrDefault();

            var currentDirectory = new DirectoryInfo(GetModuleFolderPath() + "\\" + assetsFolder);

            // NOTE: Will need the assets folder directory for later use.  Storing in GlobalVars for now.
            GlobalVars.Torque2dAssetsFolder = currentDirectory;

            ParseTorque2dAssetDirectory(currentDirectory, assetExtension, System.Convert.ToBoolean(isRecurse));
        }

        public static void ParseTorque2dAssetDirectory(DirectoryInfo dir, string assetExtension, bool isRecurseDirectories)
        {
            foreach (var assetFile in dir.GetFiles("*." + assetExtension))
            {
                ParseTorque2dAssetFile(assetFile);
            }

            if (isRecurseDirectories)
            {
                foreach (var subdir in dir.EnumerateDirectories())
                {
                    ParseTorque2dAssetDirectory(subdir, assetExtension, true);
                }
            }
        }

        public static void ParseTorque2dAssetFile(FileInfo assetFile)
        {
            // Maybe create a collection of assets (ie in memory, ie ImageAssetsCollection, 'Animation'Assets collection)
            // and also parse and store/write assets to file system/etc on the fly
            XDocument assetTaml = XDocument.Load(assetFile.FullName);

            var assetRootNode = assetTaml.Root;

            switch (assetRootNode.Name.ToString())
            {
                case "ImageAsset":

                    var cellCountXAttr = assetRootNode.Attribute("CellCountX");
                    int? cellCountX = cellCountXAttr == null ? (int?)null : System.Convert.ToInt32(cellCountXAttr.Value);

                    var cellCountYAttr = assetRootNode.Attribute("CellCountY");
                    int? cellCountY = cellCountYAttr == null ? (int?)null : System.Convert.ToInt32(cellCountYAttr.Value);

                    var cellWidthAttr = assetRootNode.Attribute("CellWidth");
                    int? cellWidth = cellWidthAttr == null ? (int?)null : System.Convert.ToInt32(cellWidthAttr.Value);

                    var cellHeightAttr = assetRootNode.Attribute("CellHeight");
                    int? cellHeight = cellHeightAttr == null ? (int?)null : System.Convert.ToInt32(cellHeightAttr.Value);

                    Torque2dAssetLibrary.CreateTorque2dImageAsset(
                        assetRootNode.Attribute("AssetName").Value,
                        assetRootNode.Attribute("ImageFile").Value,
                        cellCountX,
                        cellCountY,
                        cellWidth,
                        cellHeight,
                        assetFile);

                    break;

                case "FontAsset":
                    Torque2dAssetLibrary.CreateTorque2dFontAsset(assetRootNode.Attribute("AssetName").Value, assetRootNode.Attribute("FontFile").Value, assetFile);
                    break;

                case "AnimationAsset":

                    var animationFramesAttr = assetRootNode.Attribute("AnimationFrames");
                    var animationFrames = animationFramesAttr == null ? null : animationFramesAttr.Value;

                    var animationTimeAttr = assetRootNode.Attribute("AnimationTime");
                    var animationTime = animationTimeAttr == null ? (double?)null : System.Convert.ToDouble(animationTimeAttr.Value);

                    var animationCycleAttr = assetRootNode.Attribute("AnimationCycle");
                    var animationCycle = animationCycleAttr == null ? (bool?)null : System.Convert.ToBoolean(animationCycleAttr.Value);

                    Torque2dAssetLibrary.CreateTorque2dAnimationAsset(assetRootNode.Attribute("AssetName").Value, assetRootNode.Attribute("Image").Value, animationFrames, animationTime, animationCycle, assetFile);
                    break;

                case "AudioAsset":

                    var loopingAttr = assetRootNode.Attribute("Looping");
                    var isLooping = loopingAttr == null ? (bool?)null : System.Convert.ToBoolean(loopingAttr.Value);

                    Torque2dAssetLibrary.CreateTorque2dAudioAsset(assetRootNode.Attribute("AssetName").Value, assetRootNode.Attribute("AudioFile").Value, isLooping, assetFile);
                    break;

            }
        }

        public static void ProcessAllTorque2dTorquescriptFiles()
        {
            // NOTE: Only handle files that have .cs file extension
            var rootModuleDirectory = new DirectoryInfo(GetModuleFolderPath());

            ProcessAllTorque2dTorquescriptFilesHelper(rootModuleDirectory);
        }

        public static void ProcessAllTorque2dTorquescriptFilesHelper(DirectoryInfo dir)
        {
            foreach (var torquescriptFile in dir.GetFiles("*.cs"))
            {
                ProcessTorque2dTorquescriptFile(torquescriptFile);
            }

            foreach (var subdir in dir.EnumerateDirectories())
            {
                ProcessAllTorque2dTorquescriptFilesHelper(subdir);
            }
        }

        public static void ProcessTorque2dTorquescriptFile(FileInfo torquescriptFile)
        {
            var codeFile = new CodeFile
            {
                Filename = torquescriptFile.FullName,
                Contents = new List<CodeBlock>()
            };

            var fileLines = File.ReadAllLines(torquescriptFile.FullName);

            var currentlyInCommentBlock = false;

            foreach (var line in fileLines)
            {
                List<CodeBlock> lineCodeBlock;
                lineCodeBlock = InputRules.GenerateCodeBlockFromLine(line, ref currentlyInCommentBlock, codeFile.Contents);
                codeFile.Contents.AddRange(lineCodeBlock);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList.Add(codeFile);
        }

        private static void ProcessPhaserClassHierarchy()
        {
            // will iterate over all BasicCodeTokens, in order to determine the class hierarchy structure that we need
            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];
                    if (codeBlock.GetType() == typeof(BasicCodeToken))
                    {
                        var token = (BasicCodeToken)codeBlock;

                        if (!string.IsNullOrWhiteSpace(token.Class))
                        {
                            ProcessPhaserClassIntoPhaserClassHierarchy(token);
                        }
                    }
                }
            }
        }

        public static void ProcessTorque2dTorquescriptFilesClassesAndMethods()
        {
            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];
                    if (codeBlock.GetType() == typeof(ClassMethod))
                    {
                        codeFile.Contents = ProcessClassMethod(i, codeFile.Contents);
                    }
                }
            }
        }

        public static void GeneratePhaserClassJavascriptFiles()
        {
            // create PhaserClasses directory if it does not yet exist
            var phaserClassesDir = Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder);

            if (!Directory.Exists(phaserClassesDir))
            {
                Directory.CreateDirectory(phaserClassesDir);
            }

            // now generate class files (as javascript files)
            foreach (var phaserClass in GlobalVars.PhaserCodeRepo.PhaserClassList)
            {
                // formats phaser class (ex: Will rename method 'onSceneUpdate' to just 'update', which is what Phaser uses as the update method)
                phaserClass.FormatToPhaserData();

                // generates the actual phaser class file (ie javascript file)
                GeneratePhaserClassFile(phaserClass);
            }

            // last, we stub in any classes that exist in the phaserClassHierarchy but not in the PhaserClassList (can happen for classes defined that have no methods in them)
            GenerateMissingPhaserClassFiles();
        }

        public static string ConvertTorquescriptFilePathToPhaserFilePath(string torquescriptFilePath)
        {
            var result = torquescriptFilePath.Replace(GetModuleFolderPath(), GlobalVars.PhaserProjectOutputFolder);
            var fileExtensionIdx = result.LastIndexOf(".cs");

            if (fileExtensionIdx != -1)
            {
                result = result.Substring(0, fileExtensionIdx) + ".js";
            }

            var dir = result.Substring(0, result.LastIndexOf('\\'));

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return result;
        }

        private static List<CodeBlock> ProcessClassMethod(int codeBlockIdx, List<CodeBlock> contents)
        {
            var torqueClassMethodCodeBlock = (ClassMethod)contents[codeBlockIdx];

            var phaserClass = GlobalVars.PhaserCodeRepo.PhaserClassList.FirstOrDefault(pClass => pClass.ClassName == torqueClassMethodCodeBlock.ClassName);

            if (phaserClass == null)
            {
                phaserClass = new PhaserClass()
                {
                    ClassName = torqueClassMethodCodeBlock.ClassName,
                    ClassMethods = new List<Tuple<string, List<string>, List<CodeBlock>>>()
                };

                GlobalVars.PhaserCodeRepo.PhaserClassList.Add(phaserClass);
            }

            var methodParameters = new List<string>();
            var idx = codeBlockIdx + 1;

            while (true)
            {
                if (contents[idx].GetType() == typeof(LocalVariable))
                {
                    var token = (LocalVariable)contents[idx];

                    if (token.Name != "%this")
                    {
                        methodParameters.Add(token.ConvertToCode());
                    }
                }

                idx++;

                if (contents[idx].GetType() == typeof(OpenCurlyBracket))
                {
                    break;
                }
            }

            idx = codeBlockIdx;
            var openCurlyBracketCount = 0;

            for (var i = idx + 1; i < contents.Count; i++)
            {
                if (contents[i].GetType() == typeof(OpenCurlyBracket))
                {
                    openCurlyBracketCount++;
                    idx = i + 1;
                    break;
                }
            }

            var closedCurlyBracketCount = 0;

            for (var j = idx; j < contents.Count; j++)
            {
                idx = j;

                if (contents[j].GetType() == typeof(OpenCurlyBracket))
                {
                    openCurlyBracketCount++;
                }
                else if (contents[j].GetType() == typeof(ClosedCurlyBracket))
                {
                    closedCurlyBracketCount++;
                }

                if (openCurlyBracketCount <= closedCurlyBracketCount)
                {
                    break;
                }
            }

            // add all the code blocks of the class method 'scope' to the phaserClass
            phaserClass.ClassMethods.Add(
                Tuple.Create(torqueClassMethodCodeBlock.MethodName,
                    methodParameters,
                    contents.GetRange(codeBlockIdx + 1, idx - codeBlockIdx)
                )
            );

            // remove code blocks from the contents (so they are not generated in the torquescript files)
            contents.RemoveRange(codeBlockIdx, (idx - codeBlockIdx) + 1);
            return contents;
        }

        private static void GeneratePhaserClassFile(PhaserClass phaserClass)
        {
            var phaserClassHierarchyObj = GetPhaserHierarchyObj(phaserClass);

            if (phaserClassHierarchyObj != null)
            {
                if (phaserClassHierarchyObj.IsAPhaserApiClass)
                {
                    // do not want to build class file for Phaser API classes; simply return out of function
                    return;
                }
            }

            File.WriteAllText(
                Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder, phaserClass.ClassName + "Class.js"),
                GenerateJavascriptClass(phaserClass, phaserClassHierarchyObj)
            );
        }

        private static void GenerateMissingPhaserClassFiles()
        {
            if (GlobalVars.PhaserCodeRepo.PhaserClassHierarchyRoot.SubClasses != null)
            {
                foreach (var phaserClassHierarchyObj in GlobalVars.PhaserCodeRepo.PhaserClassHierarchyRoot.SubClasses)
                {
                    GenerateMissingPhaserClassFileIfRequired(phaserClassHierarchyObj);

                    if (phaserClassHierarchyObj.SubClasses != null)
                    {
                        GenerateMissingPhaserClassFilesHelper(phaserClassHierarchyObj);
                    }
                }
            }
        }

        private static void GenerateMissingPhaserClassFilesHelper(PhaserClassHierarchyObj phaserClassHierarchyObj)
        {
            foreach (var phaserClassHierarchyObjSubClass in phaserClassHierarchyObj.SubClasses)
            {
                GenerateMissingPhaserClassFileIfRequired(phaserClassHierarchyObjSubClass);

                if (phaserClassHierarchyObjSubClass.SubClasses != null)
                {
                    GenerateMissingPhaserClassFilesHelper(phaserClassHierarchyObjSubClass);
                }
            }
        }

        private static void GenerateMissingPhaserClassFileIfRequired(PhaserClassHierarchyObj phaserClassHierarchyObj)
        {
            if (!phaserClassHierarchyObj.IsAPhaserApiClass)
            {
                if (IsMissingInPhaserClassList(phaserClassHierarchyObj))
                {
                    var stubPhaserClass = new PhaserClass()
                    {
                        ClassName = phaserClassHierarchyObj.ClassName,
                        ClassMethods = new List<Tuple<string, List<string>, List<CodeBlock>>>()
                    };

                    File.WriteAllText(
                        Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder, stubPhaserClass.ClassName + "Class.js"),
                        GenerateJavascriptClass(stubPhaserClass, phaserClassHierarchyObj)
                    );
                }
            }
        }

        private static bool IsMissingInPhaserClassList(PhaserClassHierarchyObj phaserClassHierarchyObj)
        {
            return !GlobalVars.PhaserCodeRepo.PhaserClassList.Any(x => x.ClassName.ToLower() == phaserClassHierarchyObj.ClassName.ToLower());
        }

        public static void ProcessPhaserClassIntoPhaserClassHierarchy(BasicCodeToken tokenWithClass)
        {
            var rootNode = GlobalVars.PhaserCodeRepo.PhaserClassHierarchyRoot;

            if (PhaserClassHierarchyObjAlreadyExists(rootNode, tokenWithClass))
            {
                return;
            }

            if (rootNode.SubClasses == null)
            {
                rootNode.SubClasses = new List<PhaserClassHierarchyObj>();
            }

            if (rootNode.SubClasses.Count < 1)
            {
                var newNode = BuildNewPhaserClassHierarchyObj(tokenWithClass);
                rootNode.SubClasses.Add(newNode);
                return;
            }

            if (tokenWithClass.Value != null)
            {
                var matchingNode = FindPhaserClassHierarchyObjWithPhaserObjectClass(Convert.FromTorque2dClassStringToPhaserClassString(tokenWithClass.Value));

                if (matchingNode != null)
                {
                    if (tokenWithClass.SuperClass != null)
                    {
                        var superClassNode = FindPhaserClassHierarchyObj(tokenWithClass.SuperClass, matchingNode);

                        if (superClassNode == null)
                        {
                            var newNode = new PhaserClassHierarchyObj
                            {
                                ClassName = tokenWithClass.SuperClass,
                                IsAPhaserApiClass = false,
                                Parent = matchingNode
                            };

                            var newNode2ndLevel = new PhaserClassHierarchyObj
                            {
                                ClassName = tokenWithClass.Class,
                                IsAPhaserApiClass = false,
                                Parent = newNode
                            };

                            newNode.SubClasses = new List<PhaserClassHierarchyObj>();
                            newNode.SubClasses.Add(newNode2ndLevel);

                            if (matchingNode.SubClasses == null)
                            {
                                matchingNode.SubClasses = new List<PhaserClassHierarchyObj>();
                            }

                            matchingNode.SubClasses.Add(newNode);
                            return;
                        }

                        var classNode = FindPhaserClassHierarchyObj(tokenWithClass.Class, superClassNode);

                        if (classNode == null)
                        {
                            var newNode = new PhaserClassHierarchyObj
                            {
                                ClassName = tokenWithClass.Class,
                                IsAPhaserApiClass = false,
                                Parent = superClassNode
                            };

                            if (superClassNode.SubClasses == null)
                            {
                                superClassNode.SubClasses = new List<PhaserClassHierarchyObj>();
                            }

                            superClassNode.SubClasses.Add(newNode);
                            return;
                        }

                        return;
                    }
                    else if (tokenWithClass.Class != null)
                    {
                        var classNode = FindPhaserClassHierarchyObj(tokenWithClass.Class, matchingNode);

                        if (classNode == null)
                        {
                            var newNode = new PhaserClassHierarchyObj
                            {
                                ClassName = tokenWithClass.Class,
                                IsAPhaserApiClass = false,
                                Parent = matchingNode
                            };

                            if (matchingNode.SubClasses == null)
                            {
                                matchingNode.SubClasses = new List<PhaserClassHierarchyObj>();
                            }

                            matchingNode.SubClasses.Add(newNode);
                            return;
                        }

                        return;
                    }
                }

                var newNodePhaserObject = BuildNewPhaserClassHierarchyObj(tokenWithClass);
                rootNode.SubClasses.Add(newNodePhaserObject);
                return;
            }

            if (tokenWithClass.SuperClass != null)
            {
                var superClassNode = FindPhaserClassHierarchyObj(tokenWithClass.SuperClass, rootNode);

                if (superClassNode == null)
                {
                    var newNode = new PhaserClassHierarchyObj
                    {
                        ClassName = tokenWithClass.SuperClass,
                        IsAPhaserApiClass = false
                    };

                    var newNode2ndLevel = new PhaserClassHierarchyObj
                    {
                        ClassName = tokenWithClass.Class,
                        IsAPhaserApiClass = false,
                        Parent = newNode
                    };

                    newNode.SubClasses = new List<PhaserClassHierarchyObj>();
                    newNode.SubClasses.Add(newNode2ndLevel);

                    if (rootNode.SubClasses == null)
                    {
                        rootNode.SubClasses = new List<PhaserClassHierarchyObj>();
                    }

                    rootNode.SubClasses.Add(newNode);
                    return;
                }

                var classNode = FindPhaserClassHierarchyObj(tokenWithClass.Class, superClassNode);

                if (classNode == null)
                {
                    var newNode = new PhaserClassHierarchyObj
                    {
                        ClassName = tokenWithClass.Class,
                        IsAPhaserApiClass = false,
                        Parent = superClassNode
                    };

                    if (superClassNode.SubClasses == null)
                    {
                        superClassNode.SubClasses = new List<PhaserClassHierarchyObj>();
                    }

                    superClassNode.SubClasses.Add(newNode);
                    return;
                }

                return;
            }

            if (tokenWithClass.Class != null)
            {
                var classNode = FindPhaserClassHierarchyObj(tokenWithClass.Class, rootNode);

                if (classNode == null)
                {
                    var newNode = new PhaserClassHierarchyObj
                    {
                        ClassName = tokenWithClass.Class,
                        IsAPhaserApiClass = false
                    };

                    if (rootNode.SubClasses == null)
                    {
                        rootNode.SubClasses = new List<PhaserClassHierarchyObj>();
                    }

                    rootNode.SubClasses.Add(newNode);
                    return;
                }

                return;
            }
        }

        public static PhaserClassHierarchyObj FindPhaserClassHierarchyObjWithPhaserObjectClass(string classNameToSearch)
        {
            var rootNode = GlobalVars.PhaserCodeRepo.PhaserClassHierarchyRoot;

            if (rootNode.SubClasses == null)
            {
                return null;
            }

            foreach (var node in rootNode.SubClasses)
            {
                if (node.IsAPhaserApiClass)
                {
                    if (node.ClassName.ToLower() == classNameToSearch.ToLower())
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        public static PhaserClassHierarchyObj FindPhaserClassHierarchyObj(string classNameToSearch, PhaserClassHierarchyObj currentNode)
        {
            if (currentNode.SubClasses == null)
            {
                return null;
            }

            foreach (var node in currentNode.SubClasses)
            {
                if (node.ClassName.ToLower() == classNameToSearch.ToLower())
                {
                    return node;
                }

                var secondLevelNode = FindPhaserClassHierarchyObj(classNameToSearch, node);

                if (secondLevelNode != null)
                {
                    return secondLevelNode;
                }
            }

            return null;
        }

        private static PhaserClassHierarchyObj BuildNewPhaserClassHierarchyObj(BasicCodeToken token)
        {
            var resultNode = new PhaserClassHierarchyObj();

            if (token.Value != null)
            {
                resultNode.ClassName = Convert.FromTorque2dClassStringToPhaserClassString(token.Value);
                resultNode.IsAPhaserApiClass = true;

                if (token.SuperClass != null)
                {
                    resultNode.SubClasses = new List<PhaserClassHierarchyObj>();

                    var resultNode2ndLevel = new PhaserClassHierarchyObj
                    {
                        ClassName = token.SuperClass,
                        SubClasses = new List<PhaserClassHierarchyObj>(),
                        IsAPhaserApiClass = false,
                        Parent = resultNode
                    };

                    var resultNode3rdLevel = new PhaserClassHierarchyObj
                    {
                        ClassName = token.Class,
                        IsAPhaserApiClass = false,
                        Parent = resultNode2ndLevel
                    };

                    resultNode2ndLevel.SubClasses.Add(resultNode3rdLevel);
                    resultNode.SubClasses.Add(resultNode2ndLevel);
                }
                else if (token.Class != null)
                {
                    resultNode.SubClasses = new List<PhaserClassHierarchyObj>();

                    var resultNode2ndLevel = new PhaserClassHierarchyObj
                    {
                        ClassName = token.Class,
                        IsAPhaserApiClass = false,
                        Parent = resultNode
                    };

                    resultNode.SubClasses.Add(resultNode2ndLevel);
                }
            }
            else if (token.SuperClass != null)
            {
                resultNode.ClassName = token.SuperClass;
                resultNode.SubClasses = new List<PhaserClassHierarchyObj>();
                resultNode.IsAPhaserApiClass = false;

                var resultNode2ndLevel = new PhaserClassHierarchyObj
                {
                    ClassName = token.Class,
                    IsAPhaserApiClass = false,
                    Parent = resultNode

                };

                resultNode.SubClasses.Add(resultNode2ndLevel);
            }
            else if (token.Class != null)
            {
                resultNode.ClassName = token.Class;
                resultNode.IsAPhaserApiClass = false;
            }

            return resultNode;
        }

        public static bool PhaserClassHierarchyObjAlreadyExists(PhaserClassHierarchyObj rootNode, BasicCodeToken token)
        {
            if (rootNode.SubClasses != null)
            {
                foreach (var node in rootNode.SubClasses)
                {
                    if (node.IsAPhaserApiClass)
                    {
                        if (token.Class == null)
                        {
                            if (node.ClassName.ToLower() == token.Value.ToLower())
                            {
                                return true;
                            }
                        }
                    }

                    if (node.ClassName.ToLower() == token.Class.ToLower())
                    {
                        return true;
                    }

                    if (PhaserClassHierarchyObjAlreadyExistsHelper(node, token))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool PhaserClassHierarchyObjAlreadyExistsHelper(PhaserClassHierarchyObj node, BasicCodeToken token)
        {
            if (node.SubClasses != null)
            {
                foreach (var subNode in node.SubClasses)
                {
                    if (subNode.ClassName.ToLower() == token.Class.ToLower())
                    {
                        return true;
                    }

                    return PhaserClassHierarchyObjAlreadyExistsHelper(subNode, token);
                }
            }

            return false;
        }

        public static string GenerateJavascriptClass(PhaserClass phaserClass, PhaserClassHierarchyObj phaserClassHierarchyObj)
        {
            var javascriptFileString = "class " + phaserClass.ClassName;

            if (phaserClassHierarchyObj != null)
            {
                if (phaserClassHierarchyObj.Parent != null)
                {
                    javascriptFileString += " extends " + phaserClassHierarchyObj.Parent.ClassName;
                }
            }

            javascriptFileString += " {\n\n";

            if (phaserClassHierarchyObj != null)
            {
                if (phaserClassHierarchyObj.Parent != null)
                {
                    if (phaserClassHierarchyObj.Parent.ClassName == PhaserConstants.SpriteBaseClassName)
                    {
                        // in this case, want to generate a 'constructor' for this class, which calls a super to SpriteBaseClass (which in turns calls super to Phaser.GameObjects.Sprite)
                        javascriptFileString += "constructor (scene, x, y, texture) {\n";
                        javascriptFileString += "super(scene, x, y, texture);\n";
                        javascriptFileString += "}\n\n";
                    }

                    if (phaserClassHierarchyObj.Parent.ClassName == PhaserConstants.SceneBaseClassName)
                    {
                        // in this case, want to generate a 'constructor' for this class, which calls a super to SceneBaseClass (which in turn calls super to Phaser.Scene)
                        javascriptFileString += "constructor () {\n";
                        javascriptFileString += "super({ key: '" + phaserClass.ClassName + "', active: false });\n";
                        javascriptFileString += "}\n\n";

                        javascriptFileString += "preload() {\n";
                        javascriptFileString += "// NOTE TO DEVELOPERS: will want to preload your assets here; can manually accomplish \n";
                        javascriptFileString += "//this by cut/paste from the preload.js file in the root output folder\n";
                        javascriptFileString += "}\n\n";

                        if (GlobalVars.PhaserCodeRepo.PhaserSceneListAndAnimationsMap.ContainsKey(phaserClass.ClassName))
                        {
                            javascriptFileString += "create() {\n";

                            foreach (var animationAssetKey in GlobalVars.PhaserCodeRepo.PhaserSceneListAndAnimationsMap[phaserClass.ClassName])
                            {
                                var animationAsset = (Torque2dAnimationAsset)GlobalVars.Torque2dModuleDatabase.Torque2dAssetList.FirstOrDefault(asset => asset.Name == animationAssetKey);

                                var frameRate = (int)Math.Round(1 / animationAsset.AnimationTime.Value);

                                javascriptFileString += "this.anims.create({\n";
                                javascriptFileString += "key: '" + animationAsset.Name + "'\n";
                                javascriptFileString += "frames: [ " + animationAsset.Name + "'\n";

                                foreach (var frame in animationAsset.AnimationFrames.Split())
                                {
                                    javascriptFileString += "{ key: '" + animationAsset.Name + "', frame: " + frame + " }, ";
                                }

                                javascriptFileString = javascriptFileString.Substring(0, javascriptFileString.Length - 2);
                                javascriptFileString += " ],\n";
                                javascriptFileString += "frameRate: " + frameRate + ",\n";
                                javascriptFileString += "repeat: -1\n";
                                javascriptFileString += "});\n\n";
                            }

                            javascriptFileString += "}\n\n";
                        }
                    }

                    if (phaserClassHierarchyObj.Parent.ClassName == PhaserConstants.PhaserTextBaseClassName)
                    {
                        // in this case, want to generate a 'constructor' for this class, which calls a super to PhaserTextBaseClass (which in turns calls super to Phaser.GameObjects.Text)
                        javascriptFileString += "constructor (scene, x, y, text, style) {\n";
                        javascriptFileString += "super(scene, x, y, text, style);\n";
                        javascriptFileString += "}\n\n";
                    }
                }
            }

            foreach (var method in phaserClass.ClassMethods)
            {
                javascriptFileString += method.Item1 + "(";

                for (var i = 0; i < method.Item2.Count; i++)
                {
                    javascriptFileString += method.Item2[i];

                    if (i < (method.Item2.Count - 1))
                    {
                        javascriptFileString += ",";
                    }
                }

                javascriptFileString += ") ";

                var startIdx = 0;

                for (var i = 0; i < method.Item3.Count; i++)
                {
                    if (method.Item3[i].GetType() == typeof(OpenCurlyBracket))
                    {
                        startIdx = i;
                        break;
                    }
                }

                for (var i = startIdx; i < method.Item3.Count; i++)
                {
                    javascriptFileString += method.Item3[i].ConvertToCode();
                }

                javascriptFileString += " \n\n";
            }

            javascriptFileString += "}\n\n";

            return javascriptFileString;
        }

        public static PhaserClassHierarchyObj GetPhaserHierarchyObj(PhaserClass phaserClass)
        {
            var currentNode = GlobalVars.PhaserCodeRepo.PhaserClassHierarchyRoot;

            if (currentNode.SubClasses != null)
            {
                foreach (var subClass in currentNode.SubClasses)
                {
                    var result = GetPhaserHierarchyObjHelper(phaserClass, subClass);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        private static PhaserClassHierarchyObj GetPhaserHierarchyObjHelper(PhaserClass phaserClass, PhaserClassHierarchyObj currentNode)
        {
            if (currentNode.ClassName.ToLower() == phaserClass.ClassName.ToLower())
            {
                return currentNode;
            }

            if (currentNode.SubClasses != null)
            {
                foreach (var subClass in currentNode.SubClasses)
                {
                    var result = GetPhaserHierarchyObjHelper(phaserClass, subClass);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        // returns true if we no longer have to '2nd pass' (or well, multiple 2nd passes :) ) over the code files
        private static bool Process2ndPass()
        {
            bool done2ndPasses = true;

            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                // the 'identifier'/object/variable that we want to store as our object that we are setting as 'new' instance of object
                List<CodeBlock> identifierBlob = new List<CodeBlock>();

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(Semicolon) || codeBlock.GetType() == typeof(OpenCurlyBracket) || codeBlock.GetType() == typeof(ClosedCurlyBracket) || codeBlock.GetType() == typeof(CommentBlockEnd))
                    {
                        identifierBlob.Clear();
                    }
                    else
                    {
                        identifierBlob.Add(codeBlock);
                    }

                    bool needToContinue = false;

                    // Will now process any 'new' object creations that have squiggly bracket initializations following
                    if (codeBlock.GetType() == typeof(NewOperator))
                    {
                        // remove all trailing tokens of identifierBlob up until a basicToken
                        var y = i;
                        while (!(codeFile.Contents[y].GetType() == typeof(BasicCodeToken) ||
                                    codeFile.Contents[y].GetType() == typeof(GlobalVariable) ||
                                    codeFile.Contents[y].GetType() == typeof(LocalVariable)
                            ))
                        {
                            if (identifierBlob.Count < 1)
                            {
                                needToContinue = true;
                                break;
                            }

                            if (y == 0)
                            {
                                needToContinue = true;
                                break;
                            }

                            identifierBlob.Remove(identifierBlob.Last());
                            y--;
                        }

                        if (needToContinue)
                        {
                            // NOTE: Sometimes there is such a thing as a new operator with no variable assigned to it (ie with an equals sign assignment).  Will add new operator codeblock here
                            newCodeFile.Contents.Add(new NewOperator());
                            continue;
                        }

                        var done2ndPassesTemp = false;

                        newCodeFile.Contents.Add(codeBlock);

                        // now continue thru codeBlocks until we get to the squiggly bracket part following the new object initialization
                        var openCurlyBracketCount = 0;
                        var openRoundBracketCount = 0;

                        var idx = i + 1;

                        while (idx < codeFile.Contents.Count)
                        {
                            if (codeFile.Contents[idx].GetType() == typeof(OpenCurlyBracket))
                            {
                                if (openCurlyBracketCount == 0 && openRoundBracketCount == 0)
                                {
                                    idx++;
                                    break;
                                }

                                openCurlyBracketCount++;
                                newCodeFile.Contents.Add(codeFile.Contents[idx]);
                                idx++;
                                continue;
                            }

                            if (codeFile.Contents[idx].GetType() == typeof(OpenRoundBracket))
                            {
                                openRoundBracketCount++;
                                newCodeFile.Contents.Add(codeFile.Contents[idx]);
                                idx++;
                                continue;
                            }

                            if (codeFile.Contents[idx].GetType() == typeof(ClosedCurlyBracket))
                            {
                                openCurlyBracketCount--;
                                newCodeFile.Contents.Add(codeFile.Contents[idx]);
                                idx++;
                                continue;
                            }

                            if (codeFile.Contents[idx].GetType() == typeof(ClosedRoundBracket))
                            {
                                openRoundBracketCount--;
                                newCodeFile.Contents.Add(codeFile.Contents[idx]);
                                idx++;
                                continue;
                            }

                            if (codeFile.Contents[idx].GetType() == typeof(Semicolon))
                            {
                                newCodeFile.Contents.Add(codeFile.Contents[idx]);

                                if (openCurlyBracketCount == 0 && openRoundBracketCount == 0)
                                {
                                    done2ndPassesTemp = true;
                                    break;
                                }

                                idx++;
                                continue;
                            }

                            newCodeFile.Contents.Add(codeFile.Contents[idx]);
                            idx++;
                        }

                        if (done2ndPasses)
                        {
                            done2ndPasses = done2ndPassesTemp;
                        }

                        if (done2ndPassesTemp)
                        {
                            // no need to process squiggly brackets part for this one; continue for loop
                            // will also add a newline character for formatting reasons
                            newCodeFile.Contents.Add(new NewLineCharacter());
                            i = idx + 1;
                            identifierBlob.Clear();
                            continue;
                        }
                        else
                        {
                            newCodeFile.Contents.Add(new Semicolon());
                        }

                        var openCurlyBracketCountInner = 1;
                        var openRoundBracketCountInner = 0;
                        bool foundClosingBracket = false;

                        // NOTE: before entering the 'j' for loop, sometimes there happens to be zero fields (ie BasicCodeTokens)
                        // in the new object creation block.  Must skip the entire block if this is the case.  This 'k' loop
                        // handles that

                        needToContinue = true;

                        for (var k = idx + 1; k < codeFile.Contents.Count; k++)
                        {
                            if (codeFile.Contents[k].GetType() == typeof(BasicCodeToken))
                            {
                                needToContinue = false;
                                break;
                            }

                            if (codeFile.Contents[k].GetType() == typeof(ClosedCurlyBracket))
                            {
                                foundClosingBracket = true;
                                continue;
                            }

                            if (foundClosingBracket)
                            {
                                if (codeFile.Contents[k].GetType() == typeof(Semicolon))
                                {
                                    idx = k + 1;
                                    break;
                                }
                            }
                        }

                        if (needToContinue)
                        {
                            i = idx;
                            identifierBlob.Clear();
                            continue;
                        }

                        openCurlyBracketCountInner = 1;
                        openRoundBracketCountInner = 0;
                        foundClosingBracket = false;

                        // want to store any 'scenegraph' assignment fields to be placed at the end of the 'new' action, and add to the scenegraph
                        List<CodeBlock> sceneGraphFieldValBlob = new List<CodeBlock>();
                        bool workingOnSceneGraphField = false;
                        bool hitFirstDesiredTokenWithSceneGraphField = false;

                        // can now handle the inner squiggly brackets part
                        for (var j = idx + 1; j < codeFile.Contents.Count; j++)
                        {
                            var codeBlockInner = codeFile.Contents[j];

                            if (codeBlockInner.GetType() == typeof(BasicCodeToken))
                            {
                                if (((BasicCodeToken)codeBlockInner).Value.ToLower() == "scenegraph")
                                {
                                    workingOnSceneGraphField = true;
                                }
                                else
                                {
                                    newCodeFile.Contents.AddRange(identifierBlob);

                                    newCodeFile.Contents.Add(new Dot());
                                    newCodeFile.Contents.Add(codeBlockInner);
                                }

                                var done2ndPassesInnerTemp = true;

                                bool foundNewField = false;

                                var idxInner = j + 1;

                                while (idxInner < codeFile.Contents.Count)
                                {
                                    if (foundClosingBracket)
                                    {
                                        if (codeFile.Contents[idxInner].GetType() == typeof(Semicolon))
                                        {
                                            break;
                                        }
                                    }

                                    if (codeFile.Contents[idxInner].GetType() == typeof(Semicolon))
                                    {
                                        foundNewField = true;

                                        if (workingOnSceneGraphField)
                                        {
                                            workingOnSceneGraphField = false;
                                            hitFirstDesiredTokenWithSceneGraphField = false;
                                        }
                                        else
                                        {
                                            newCodeFile.Contents.Add(codeFile.Contents[idxInner]);
                                        }
                                        idxInner++;
                                        continue;
                                    }

                                    if (codeFile.Contents[idxInner].GetType() == typeof(BasicCodeToken) && foundNewField)
                                    {
                                        foundNewField = false;

                                        newCodeFile.Contents.AddRange(identifierBlob);

                                        newCodeFile.Contents.Add(new Dot());

                                        newCodeFile.Contents.Add(codeFile.Contents[idxInner]);
                                        idxInner++;
                                        continue;
                                    }

                                    if (codeFile.Contents[idxInner].GetType() == typeof(OpenCurlyBracket))
                                    {
                                        if (openCurlyBracketCountInner == 1 && openRoundBracketCountInner == 0)
                                        {
                                            done2ndPassesInnerTemp = false;
                                        }

                                        openCurlyBracketCountInner++;

                                        if (workingOnSceneGraphField)
                                        {
                                            sceneGraphFieldValBlob.Add(codeFile.Contents[idxInner]);
                                        }
                                        else
                                        {
                                            newCodeFile.Contents.Add(codeFile.Contents[idxInner]);
                                        }

                                        idxInner++;
                                        continue;
                                    }

                                    if (codeFile.Contents[idxInner].GetType() == typeof(OpenRoundBracket))
                                    {
                                        openRoundBracketCountInner++;

                                        if (workingOnSceneGraphField)
                                        {
                                            sceneGraphFieldValBlob.Add(codeFile.Contents[idxInner]);
                                        }
                                        else
                                        {
                                            newCodeFile.Contents.Add(codeFile.Contents[idxInner]);
                                        }

                                        idxInner++;
                                        continue;
                                    }

                                    if (codeFile.Contents[idxInner].GetType() == typeof(ClosedCurlyBracket))
                                    {
                                        if (openCurlyBracketCountInner == 1 && openRoundBracketCountInner == 0)
                                        {
                                            idxInner++;
                                            foundClosingBracket = true;
                                            continue;
                                        }

                                        openCurlyBracketCountInner--;

                                        if (workingOnSceneGraphField)
                                        {
                                            sceneGraphFieldValBlob.Add(codeFile.Contents[idxInner]);
                                        }
                                        else
                                        {
                                            newCodeFile.Contents.Add(codeFile.Contents[idxInner]);
                                        }

                                        idxInner++;
                                        continue;
                                    }

                                    if (codeFile.Contents[idxInner].GetType() == typeof(ClosedRoundBracket))
                                    {
                                        openRoundBracketCountInner--;

                                        if (workingOnSceneGraphField)
                                        {
                                            sceneGraphFieldValBlob.Add(codeFile.Contents[idxInner]);
                                        }
                                        else
                                        {
                                            newCodeFile.Contents.Add(codeFile.Contents[idxInner]);
                                        }

                                        idxInner++;
                                        continue;
                                    }

                                    if (workingOnSceneGraphField && (!hitFirstDesiredTokenWithSceneGraphField))
                                    {
                                        if (codeFile.Contents[idxInner].GetType() == typeof(BasicCodeToken) ||
                                                codeFile.Contents[idxInner].GetType() == typeof(LocalVariable) ||
                                                codeFile.Contents[idxInner].GetType() == typeof(GlobalVariable)
                                            )
                                        {
                                            hitFirstDesiredTokenWithSceneGraphField = true;
                                        }
                                    }

                                    if (workingOnSceneGraphField && hitFirstDesiredTokenWithSceneGraphField)
                                    {
                                        sceneGraphFieldValBlob.Add(codeFile.Contents[idxInner]);
                                    }
                                    else if (workingOnSceneGraphField && (!hitFirstDesiredTokenWithSceneGraphField))
                                    {
                                        // skip
                                    }
                                    else
                                    {
                                        newCodeFile.Contents.Add(codeFile.Contents[idxInner]);
                                    }

                                    idxInner++;
                                }

                                if (sceneGraphFieldValBlob.Count > 0)
                                {
                                    newCodeFile.Contents.AddRange(sceneGraphFieldValBlob);
                                    newCodeFile.Contents.Add(new Dot());
                                    newCodeFile.Contents.Add(new BasicCodeToken { Value = "add" });
                                    newCodeFile.Contents.Add(new OpenRoundBracket());
                                    newCodeFile.Contents.AddRange(identifierBlob);
                                    newCodeFile.Contents.Add(new ClosedRoundBracket());
                                    newCodeFile.Contents.Add(new Semicolon());
                                    newCodeFile.Contents.Add(new NewLineCharacter());
                                }

                                if (done2ndPasses)
                                {
                                    done2ndPasses = done2ndPassesInnerTemp;
                                }

                                i = idxInner + 1;
                                break;
                            }
                            else
                            {
                                newCodeFile.Contents.Add(codeBlockInner);
                            }
                        }

                        identifierBlob.Clear();
                    }
                    else
                    {
                        newCodeFile.Contents.Add(codeBlock);
                    }
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;

            return done2ndPasses;
        }

        private static void RemoveExecFunctionCalls()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var idx = i;

                    if (codeFile.Contents[i].GetType() == typeof(BasicCodeToken))
                    {
                        if (((BasicCodeToken)codeFile.Contents[i]).Value.ToLower() == "exec")
                        {
                            idx++;

                            if (codeFile.Contents[idx].GetType() == typeof(OpenRoundBracket))
                            {
                                i = RemoveExecFunctionCall(codeFile, i);
                                continue;
                            }
                            else
                            {
                                while (codeFile.Contents[idx].GetType() == typeof(WhitespaceCharacter) ||
                                        codeFile.Contents[idx].GetType() == typeof(NewLineCharacter)
                                    )
                                {
                                    if (codeFile.Contents[idx + 1].GetType() == typeof(OpenRoundBracket))
                                    {
                                        i = RemoveExecFunctionCall(codeFile, i);
                                        continue;
                                    }

                                    idx++;
                                }
                            }
                        }
                    }

                    newCodeFile.Contents.Add(codeFile.Contents[i]);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        // Returns an index to where the RemoveExecFunctionCalls (calling code) should continue from
        private static int RemoveExecFunctionCall(CodeFile codeFile, int startIdx)
        {
            int openRoundBracketCount = 0;
            int closedRoundBracketCount = 0;

            for (var i = startIdx; i < codeFile.Contents.Count; i++)
            {
                if (codeFile.Contents[i].GetType() == typeof(OpenRoundBracket))
                {
                    openRoundBracketCount++;
                }
                else if (codeFile.Contents[i].GetType() == typeof(ClosedRoundBracket))
                {
                    closedRoundBracketCount++;

                    if (closedRoundBracketCount >= openRoundBracketCount)
                    {
                        int j = i + 1;

                        while (codeFile.Contents[j].GetType() != typeof(Semicolon))
                        {
                            j++;
                        }

                        while (codeFile.Contents[j].GetType() == typeof(WhitespaceCharacter) ||
                                codeFile.Contents[j].GetType() == typeof(NewLineCharacter)
                            )
                        {
                            j++;
                        }

                        return j;
                    }
                }
            }

            return codeFile.Contents.Count - 1;
        }

        private static void ConvertEchoFunctionCalls()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var idx = i;

                    if (codeFile.Contents[i].GetType() == typeof(BasicCodeToken))
                    {
                        var basicCodeToken = (BasicCodeToken)codeFile.Contents[i];

                        if (basicCodeToken.Value.ToLower() == "echo")
                        {
                            basicCodeToken.Value = "console.log";
                        }
                    }

                    newCodeFile.Contents.Add(codeFile.Contents[i]);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        private static void CopyAssetFiles()
        {
            var assetsFolder = GlobalVars.Torque2dAssetsFolder.FullName.Replace(GetModuleFolderPath() + "\\", "");

            var dir = Path.Combine(GlobalVars.PhaserProjectOutputFolder, assetsFolder);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            foreach (var phaserAsset in GlobalVars.PhaserAssetRepo.PhaserAssetList)
            {
                if (phaserAsset.GetType() == typeof(ImageAsset))
                {
                    var destinationFilePath = Path.Combine(GlobalVars.PhaserProjectOutputFolder, ((ImageAsset)phaserAsset).ResourceUrl.Replace('/', '\\'));

                    var destinationFolderPath = Path.GetDirectoryName(destinationFilePath);
                    if (!Directory.Exists(destinationFolderPath))
                    {
                        Directory.CreateDirectory(destinationFolderPath);
                    }

                    File.Copy(phaserAsset.Torque2dAssetFileReference.FullName, destinationFilePath, true);
                }
                else if (phaserAsset.GetType() == typeof(SpritesheetAsset))
                {
                    var destinationFilePath = Path.Combine(GlobalVars.PhaserProjectOutputFolder, ((SpritesheetAsset)phaserAsset).ResourceUrl.Replace('/', '\\'));

                    var destinationFolderPath = Path.GetDirectoryName(destinationFilePath);
                    if (!Directory.Exists(destinationFolderPath))
                    {
                        Directory.CreateDirectory(destinationFolderPath);
                    }

                    File.Copy(phaserAsset.Torque2dAssetFileReference.FullName, destinationFilePath, true);
                }
                else if (phaserAsset.GetType() == typeof(AudioAsset))
                {
                    var destinationFilePath = Path.Combine(GlobalVars.PhaserProjectOutputFolder, ((AudioAsset)phaserAsset).ResourceUrl.Replace('/', '\\'));

                    var destinationFolderPath = Path.GetDirectoryName(destinationFilePath);
                    if (!Directory.Exists(destinationFolderPath))
                    {
                        Directory.CreateDirectory(destinationFolderPath);
                    }

                    File.Copy(phaserAsset.Torque2dAssetFileReference.FullName, destinationFilePath, true);
                }
            }

            // will also create a folder for any ttf font files as well, although we will not be populating the fonts into that folder (T2D projects don't have ttf files usually, will expect the user/developer to provide)
            if (GlobalVars.PhaserAssetRepo.PhaserCssFontFaceStyleList.Count > 0)
            {
                var destinationFilePath = Path.Combine(GlobalVars.PhaserProjectOutputFolder,
                    GlobalVars.PhaserAssetRepo.PhaserCssFontFaceStyleList.First().ResourceUrl.Replace('/', '\\'));

                var destinationFolderPath = Path.GetDirectoryName(destinationFilePath);
                if (!Directory.Exists(destinationFolderPath))
                {
                    Directory.CreateDirectory(destinationFolderPath);
                }
            }
        }

        private static void GenerateGlobalVarsJavascriptFile()
        {
            if (!Directory.Exists(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserGlobalVarsFolder)))
            {
                Directory.CreateDirectory(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserGlobalVarsFolder));
            }

            var globalVarsJsFileAsString = "";

            foreach (var globalVar in GlobalVars.PhaserCodeRepo.PhaserGlobalVars)
            {
                globalVarsJsFileAsString += "var " + globalVar.ConvertToCode() + " = undefined;\n";
            }

            var globalVarsJsFilePath = Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserGlobalVarsFolder, GlobalVars.PhaserGlobalVarsFilename);
            File.WriteAllText(globalVarsJsFilePath, globalVarsJsFileAsString);
        }

        private static void RemoveModuleNamePrefixFromAssets()
        {
            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var idx = i;

                    if (codeFile.Contents[i].GetType() == typeof(StringValue))
                    {
                        var stringToken = (StringValue)codeFile.Contents[i];

                        var stringLiteralToCheck = GlobalVars.Torque2dProjectModuleName + ":";
                        var indexOfLastCharToCheck = stringLiteralToCheck.Length + 1;

                        if (stringToken.Val.IndexOf(stringLiteralToCheck) == 1)
                        {
                            if (Char.IsLetter(stringToken.Val[indexOfLastCharToCheck]))
                            {
                                // found a Torque2dAssetName.  Remove the stringLiteralToCheck literal
                                stringToken.Val = stringToken.Val.Replace(stringLiteralToCheck, "");
                            }
                        }
                    }
                }
            }
        }

        private static void GeneratePreloadAssetsFile()
        {
            var preloadFunction = new FunctionDeclaration();
            preloadFunction.Name = "preload";

            var functionContents = new List<CodeBlock>();

            foreach (var phaserAsset in GlobalVars.PhaserAssetRepo.PhaserAssetList)
            {
                functionContents.Add(phaserAsset);
            }

            preloadFunction.Contents = functionContents;
            preloadFunction.CanWriteAsEmptyFunction = true;

            var preloadFunctionComment = "\n\n\n\n\n\n// NOTE: For Phaser Text Objects, you don't need to preload such assets but you do need to copy the necessary ttf (or, perhaps other font file if you decide to) files to the assets/fonts/ttf directory.\n";

            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, "preload.js"), preloadFunctionComment + preloadFunction.ConvertToCode());
        }

        private static void GenerateCodeFilesFromTemplates()
        {
            if (!Directory.Exists(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder)))
            {
                Directory.CreateDirectory(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder));
            }

            if (!Directory.Exists(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserUtilFolder)))
            {
                Directory.CreateDirectory(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserUtilFolder));
            }

            // JavascriptUtil.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserUtilFolder, "JavascriptUtil.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\JavascriptUtil.txt"));

            // MathConvertUtil.js
            var mathConvertUtilJsFile = File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\MathConvertUtil.txt");

            mathConvertUtilJsFile = mathConvertUtilJsFile
                                        .Replace("**{TORQUE_2D_CAMERA_SIZE_WIDTH}**", GlobalVars.Torque2dCameraSizeWidth.ToString())
                                        .Replace("**{TORQUE_2D_CAMERA_SIZE_HEIGHT}**", GlobalVars.Torque2dCameraSizeHeight.ToString())
                                        .Replace("**{PHASER_PROJECT_WIDTH}**", GlobalVars.PhaserProjectWidthInPixels.ToString())
                                        .Replace("**{PHASER_PROJECT_HEIGHT}**", GlobalVars.PhaserProjectHeightInPixels.ToString());

            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserUtilFolder, "MathConvertUtil.js"), mathConvertUtilJsFile);

            // SceneUtil.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserUtilFolder, "SceneUtil.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\SceneUtil.txt"));

            // GeneralUtil.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserUtilFolder, "GeneralUtil.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\GeneralUtil.txt"));

            // T2dFunctionsUtil.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserUtilFolder, "T2dFunctionsUtil.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\T2dFunctionsUtil.txt"));

            // CookiesUtil.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserUtilFolder, "CookiesUtil.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\CookiesUtil.txt"));

            // SceneBaseClass.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder, "SceneBaseClass.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\SceneBaseClass.txt"));

            // SpriteBaseClass.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder, "SpriteBaseClass.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\SpriteBaseClass.txt"));

            // CustomContainerClass.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder, "CustomContainerClass.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\CustomContainerClass.txt"));

            // PhaserTextBaseClass.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder, "PhaserTextBaseClass.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\PhaserTextBaseClass.txt"));

            // PhaserSceneObjectClass.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder, "PhaserSceneObjectClass.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\PhaserSceneObjectClass.txt"));

            // PhaserSimObjectClass.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder, "PhaserSimObjectClass.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\PhaserSimObjectClass.txt"));

            // PhaserSimSetClass.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder, "PhaserSimSetClass.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\PhaserSimSetClass.txt"));

            // ParticleEmitterManagerBaseClass.js
            File.WriteAllText(Path.Combine(GlobalVars.PhaserProjectOutputFolder, GlobalVars.PhaserClassesFolder, "ParticleEmitterManagerBaseClass.js"),
                File.ReadAllText(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Templates\ParticleEmitterManagerBaseClass.txt"));
        }

        private static void CompileListOfScenesCreatedInOurGameAndDoMoreSceneProcessing()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                var lastNewlineMarker = -1;

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(NewLineCharacter))
                    {
                        newCodeFile.Contents.Add(codeBlock);
                        lastNewlineMarker = newCodeFile.Contents.Count;
                    }
                    else if (codeBlock.GetType() == typeof(NewOperator))
                    {
                        var idx = i + 1;

                        while (idx < codeFile.Contents.Count)
                        {
                            if (codeFile.Contents[idx].GetType() == typeof(BasicCodeToken))
                            {
                                break;
                            }

                            idx++;
                        }

                        var matchingSceneClass = FindMatchingSceneClass((BasicCodeToken)codeFile.Contents[idx]);

                        if (matchingSceneClass != null)
                        {
                            if (!GlobalVars.PhaserCodeRepo.PhaserSceneList.Contains(matchingSceneClass.ClassName))
                            {
                                GlobalVars.PhaserCodeRepo.PhaserSceneList.Add(matchingSceneClass.ClassName);
                            }

                            if (lastNewlineMarker >= 0)
                            {
                                newCodeFile.Contents.InsertRange(lastNewlineMarker, new List<CodeBlock>
                                {
                                    new BasicCodeToken { Value = "SceneUtil" },
                                    new Dot(),
                                    new BasicCodeToken { Value = "runScene" },
                                    new OpenRoundBracket(),
                                    new StringValue { Val = "'" + matchingSceneClass.ClassName + "'" },
                                    new Comma(),
                                    new WhitespaceCharacter { WhitespaceChar = ' ' },
                                    new BasicCodeToken { Value = PhaserConstants.T2dToPhaserRunSceneTodoMarker },
                                    new ClosedRoundBracket(),
                                    new Semicolon(),
                                    new NewLineCharacter()
                                });
                            }

                            // Need to find the end of the new Scene class method call.  Will determine it once we hit the index of the semicolon
                            // once found, can then replace this whole block with simply "game.scene.getScene('introSceneGraph');"
                            while (idx < codeFile.Contents.Count)
                            {
                                if (codeFile.Contents[idx].GetType() == typeof(Semicolon))
                                {
                                    break;
                                }

                                idx++;
                            }

                            newCodeFile.Contents.Add(new BasicCodeToken { Value = "game" });
                            newCodeFile.Contents.Add(new Dot());
                            newCodeFile.Contents.Add(new BasicCodeToken { Value = "scene", DoNotConvertVariable = true });
                            newCodeFile.Contents.Add(new Dot());
                            newCodeFile.Contents.Add(new BasicCodeToken { Value = "getScene" });
                            newCodeFile.Contents.Add(new OpenRoundBracket());
                            newCodeFile.Contents.Add(new StringValue { Val = "'" + matchingSceneClass.ClassName + "'" });
                            newCodeFile.Contents.Add(new ClosedRoundBracket());

                            newCodeFile.Contents.Add(codeFile.Contents[idx]);
                            i = idx;
                        }
                        else
                        {
                            newCodeFile.Contents.Add(codeBlock); // will end up adding the new operator after all, since this is not a scene class instantiation
                        }
                    }
                    else
                    {
                        newCodeFile.Contents.Add(codeBlock);
                    }
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        private static PhaserClassHierarchyObj FindMatchingSceneClass(BasicCodeToken token)
        {
            var node = GlobalVars.PhaserCodeRepo.PhaserClassHierarchyRoot;

            node = node.SubClasses.First(x => x.ClassName == PhaserConstants.SceneBaseClassName)
                            .SubClasses.FirstOrDefault(y => y.ClassName == token.Class);

            return node;
        }

        private static void GenerateVarTokensForLocalVariables()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                var currentLocalVariablesNode = new LocalVariablesTreeNode
                {
                    Parent = null,
                    LocalVariablesDiscovered = new List<LocalVariable>()
                };

                bool workingOnFunctionParameters = false;

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(FunctionDeclaration))
                    {
                        workingOnFunctionParameters = true;
                        newCodeFile.Contents.Add(codeBlock);
                    }
                    else if (codeBlock.GetType() == typeof(ClosedRoundBracket))
                    {
                        if (workingOnFunctionParameters)
                        {
                            workingOnFunctionParameters = false;
                        }

                        newCodeFile.Contents.Add(codeBlock);
                    }
                    else if (codeBlock.GetType() == typeof(LocalVariable))
                    {
                        if (!currentLocalVariablesNode.ContainsLocalVariable((LocalVariable)codeBlock))
                        {
                            if (!workingOnFunctionParameters)
                            {
                                // hack - ignore for cases where the local variable is followed by a dot (ie is not really a new local variable, and is calling a method)
                                if (codeFile.Contents[i + 1].GetType() != typeof(Dot))
                                {
                                    // will add the 'var' keyword in this case.  Also will add this local variable to the LocalVariablesDiscovered
                                    newCodeFile.Contents.Add(new BasicCodeToken { Value = "var" });
                                    newCodeFile.Contents.Add(new WhitespaceCharacter { WhitespaceChar = ' ' });
                                }
                            }

                            currentLocalVariablesNode.LocalVariablesDiscovered.Add((LocalVariable)codeBlock);
                        }

                        newCodeFile.Contents.Add(codeBlock);
                    }
                    else if (codeBlock.GetType() == typeof(OpenCurlyBracket))
                    {
                        var nextLevelLocalVariablesNode = new LocalVariablesTreeNode
                        {
                            Parent = currentLocalVariablesNode,
                            LocalVariablesDiscovered = new List<LocalVariable>()
                        };

                        currentLocalVariablesNode = nextLevelLocalVariablesNode;

                        newCodeFile.Contents.Add(codeBlock);
                    }
                    else if (codeBlock.GetType() == typeof(ClosedCurlyBracket))
                    {
                        var tempNode = currentLocalVariablesNode.Parent;
                        currentLocalVariablesNode.LocalVariablesDiscovered.Clear();
                        currentLocalVariablesNode = tempNode;

                        newCodeFile.Contents.Add(codeBlock);
                    }
                    else
                    {
                        newCodeFile.Contents.Add(codeBlock);
                    }
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        private static void GenerateVarTokensForLocalVariablesInPhaserClasses()
        {
            var newPhaserClassList = new List<PhaserClass>();

            foreach (var phaserClass in GlobalVars.PhaserCodeRepo.PhaserClassList)
            {
                var newPhaserClass = new PhaserClass
                {
                    ClassName = phaserClass.ClassName,
                    ClassMethods = new List<Tuple<string, List<string>, List<CodeBlock>>>()
                };

                var currentLocalVariablesNode = new LocalVariablesTreeNode
                {
                    Parent = null,
                    LocalVariablesDiscovered = new List<LocalVariable>()
                };

                foreach (var classMethod in phaserClass.ClassMethods)
                {
                    foreach (var parameter in classMethod.Item2)
                    {
                        currentLocalVariablesNode.LocalVariablesDiscovered.Add(
                            new LocalVariable { Name = "%" + parameter }
                        );
                    }

                    var newClassMethodContents = new List<CodeBlock>();

                    foreach (var codeBlock in classMethod.Item3)
                    {
                        if (codeBlock.GetType() == typeof(LocalVariable))
                        {
                            if (!currentLocalVariablesNode.ContainsLocalVariable((LocalVariable)codeBlock))
                            {
                                // will add the 'var' keyword in this case.  Also will add this local variable to the LocalVariablesDiscovered
                                newClassMethodContents.Add(new BasicCodeToken { Value = "var" });
                                newClassMethodContents.Add(new WhitespaceCharacter { WhitespaceChar = ' ' });

                                currentLocalVariablesNode.LocalVariablesDiscovered.Add((LocalVariable)codeBlock);
                            }

                            newClassMethodContents.Add(codeBlock);
                        }
                        else if (codeBlock.GetType() == typeof(OpenCurlyBracket))
                        {
                            var nextLevelLocalVariablesNode = new LocalVariablesTreeNode
                            {
                                Parent = currentLocalVariablesNode,
                                LocalVariablesDiscovered = new List<LocalVariable>()
                            };

                            currentLocalVariablesNode = nextLevelLocalVariablesNode;

                            newClassMethodContents.Add(codeBlock);
                        }
                        else if (codeBlock.GetType() == typeof(ClosedCurlyBracket))
                        {
                            var tempNode = currentLocalVariablesNode.Parent;
                            currentLocalVariablesNode.LocalVariablesDiscovered.Clear();
                            currentLocalVariablesNode = tempNode;

                            newClassMethodContents.Add(codeBlock);
                        }
                        else
                        {
                            newClassMethodContents.Add(codeBlock);
                        }
                    }

                    newPhaserClass.ClassMethods.Add(
                        Tuple.Create(classMethod.Item1,
                            classMethod.Item2,
                            newClassMethodContents
                        )
                    );
                }

                newPhaserClassList.Add(newPhaserClass);
            }

            GlobalVars.PhaserCodeRepo.PhaserClassList = newPhaserClassList;
        }

        // TODO - Extend functionality for cases where the sprite variable itself isn't just a local variable, but
        // instead a property of an object
        // TODO - need to handle for cases where the local variable is re-initialized to a 'new Sprite()' again later on
        private static void Process2ndPassForLocalVariableSprites()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                // Note that in our case for this function, we simply add the local variables once we come across them the 'first' time,
                // and then dont worry about processing them again
                var currentLocalVariablesNode = new LocalVariablesTreeNode
                {
                    Parent = null,
                    LocalVariablesDiscovered = new List<LocalVariable>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(LocalVariable))
                    {
                        var localVariableRef = (LocalVariable)codeBlock;
                        bool continueForLoop = false;

                        // Check to make sure this is a statement where the local variable is initialized.  If its something else (ie like
                        // a local variable in a function declaration) then should continue instead
                        for (var j = i + 1; j < codeFile.Contents.Count; j++)
                        {
                            var codeBlock2 = codeFile.Contents[j];

                            if (codeBlock2.GetType() == typeof(EqualsOperator))
                            {
                                break;
                            }
                            else if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                            {
                                continueForLoop = true;
                                break;
                            }
                        }

                        if (continueForLoop)
                        {
                            newCodeFile.Contents.Add(codeBlock);
                            continue;
                        }

                        if (!currentLocalVariablesNode.ContainsLocalVariable((LocalVariable)codeBlock))
                        {
                            if (codeBlock.PhaserObjectType == PhaserObjectType.Sprite)
                            {
                                var resultAssetKeyTuple = GetAssetKeyForSprite(codeFile, i, codeBlock);

                                if (resultAssetKeyTuple.Item2 >= 0)
                                {
                                    codeFile.Contents.Insert(resultAssetKeyTuple.Item2 - 1, new CommentBlockBegin());
                                }

                                if (resultAssetKeyTuple.Item3 >= 0)
                                {
                                    codeFile.Contents.Insert(resultAssetKeyTuple.Item3 + 2, new CommentBlockEnd());
                                }

                                var resultSceneToAddThisSpriteToTuple = GetSceneToAddSpriteTo(codeFile, i, codeBlock);

                                if (resultSceneToAddThisSpriteToTuple.Item2 >= 0)
                                {
                                    codeFile.Contents.Insert(resultSceneToAddThisSpriteToTuple.Item2 - 1, new CommentBlockBegin());
                                }

                                if (resultSceneToAddThisSpriteToTuple.Item3 >= 0)
                                {
                                    codeFile.Contents.Insert(resultSceneToAddThisSpriteToTuple.Item3 + 2, new CommentBlockEnd());
                                }

                                // NOTE: Not all sprites are expected to have an Animation.  Many will not have an animation.
                                var resultAnimationForSpriteTuple = GetAnimationForSprite(codeFile, i, codeBlock);

                                if (resultAnimationForSpriteTuple.Item2 >= 0)
                                {
                                    codeFile.Contents.Insert(resultAnimationForSpriteTuple.Item2 - 1, new CommentBlockBegin());
                                }

                                if (resultAnimationForSpriteTuple.Item3 >= 0)
                                {
                                    codeFile.Contents.Insert(resultAnimationForSpriteTuple.Item3 + 2, new CommentBlockEnd());
                                }

                                // store set of Scenes and associated Animations needed to add to them.  Will assign the animation to the Scene class, and add in the create function of the class (to be done later)
                                if (resultAnimationForSpriteTuple.Item1 != null)
                                {
                                    var sceneVariable = resultSceneToAddThisSpriteToTuple.Item1;

                                    if (sceneVariable != null)
                                    {
                                        if (sceneVariable.Class != null)
                                        {
                                            if (!GlobalVars.PhaserCodeRepo.PhaserSceneListAndAnimationsMap.ContainsKey(sceneVariable.Class))
                                            {
                                                GlobalVars.PhaserCodeRepo.PhaserSceneListAndAnimationsMap.Add(sceneVariable.Class, new List<string> { resultAnimationForSpriteTuple.Item1 });
                                            }
                                            else if (!GlobalVars.PhaserCodeRepo.PhaserSceneListAndAnimationsMap[sceneVariable.Class].Contains(resultAnimationForSpriteTuple.Item1))
                                            {
                                                GlobalVars.PhaserCodeRepo.PhaserSceneListAndAnimationsMap[sceneVariable.Class].Add(resultAnimationForSpriteTuple.Item1);
                                            }
                                        }
                                    }
                                }

                                for (var j = i + 1; j < codeFile.Contents.Count; j++)
                                {
                                    bool finishedSpriteInstantiationCodeConvert = false;
                                    var idx = -1;

                                    var codeBlock2 = codeFile.Contents[j];

                                    if (codeBlock2.GetType() == typeof(WhitespaceCharacter) || codeBlock2.GetType() == typeof(EqualsOperator) || codeBlock2.GetType() == typeof(NewOperator))
                                    {
                                        continue;
                                    }

                                    if (codeBlock2.GetType() == typeof(BasicCodeToken))
                                    {
                                        if (((BasicCodeToken)codeBlock2).Value.ToLower() == Torque2dConstants.SpriteClassName.ToLower())
                                        {
                                            for (var k = j + 1; k < codeFile.Contents.Count; k++)
                                            {
                                                var codeBlock3 = codeFile.Contents[k];

                                                if (codeBlock3.GetType() == typeof(WhitespaceCharacter))
                                                {
                                                    continue;
                                                }

                                                if (codeBlock3.GetType() == typeof(OpenRoundBracket))
                                                {
                                                    if (resultSceneToAddThisSpriteToTuple.Item1 == null)
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new LocalVariable{ Name = "%undefinedOccurredDuringPhaserConversionForSceneToAddTo", PhaserObjectType = PhaserObjectType.None },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            resultAssetKeyTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForAssetKey" } : resultAssetKeyTuple.Item1.DeepCopy()
                                                        });
                                                    }
                                                    else if (resultSceneToAddThisSpriteToTuple.Item1.GetType() == typeof(LocalVariable))
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new LocalVariable{ Name = resultSceneToAddThisSpriteToTuple.Item1.Name, PhaserObjectType = resultSceneToAddThisSpriteToTuple.Item1.PhaserObjectType },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            resultAssetKeyTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForAssetKey" } : resultAssetKeyTuple.Item1.DeepCopy()
                                                        });
                                                    }
                                                    else if (resultSceneToAddThisSpriteToTuple.Item1.GetType() == typeof(GlobalVariable))
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new GlobalVariable{ Name = resultSceneToAddThisSpriteToTuple.Item1.Name, PhaserObjectType = resultSceneToAddThisSpriteToTuple.Item1.PhaserObjectType },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            resultAssetKeyTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForAssetKey" } : resultAssetKeyTuple.Item1.DeepCopy()
                                                        });
                                                    }

                                                    finishedSpriteInstantiationCodeConvert = true;
                                                    idx = k + 7;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }

                                    if (finishedSpriteInstantiationCodeConvert)
                                    {
                                        var breakOutOfLoop = false;

                                        if (resultAnimationForSpriteTuple.Item1 != null)
                                        {
                                            for (var k = idx; k < codeFile.Contents.Count; k++)
                                            {
                                                var codeBlock3 = codeFile.Contents[k];

                                                if (codeBlock3.GetType() == typeof(Semicolon))
                                                {
                                                    var codeBlock4 = codeFile.Contents[k + 1];

                                                    if (codeBlock4.GetType() == typeof(NewLineCharacter))
                                                    {
                                                        k++;
                                                    }

                                                    codeFile.Contents.InsertRange(k + 1, GenerateAnimationStatementForSprite(localVariableRef, resultAnimationForSpriteTuple.Item1));

                                                    breakOutOfLoop = true;
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }

                                        if (breakOutOfLoop)
                                        {
                                            break;
                                        }
                                    }
                                }

                                if (!currentLocalVariablesNode.ContainsLocalVariable((LocalVariable)codeBlock))
                                {
                                    currentLocalVariablesNode.LocalVariablesDiscovered.Add((LocalVariable)codeBlock);
                                }
                            }
                        }
                    }
                    else if (codeBlock.GetType() == typeof(OpenCurlyBracket))
                    {
                        var nextLevelLocalVariablesNode = new LocalVariablesTreeNode
                        {
                            Parent = currentLocalVariablesNode,
                            LocalVariablesDiscovered = new List<LocalVariable>()
                        };

                        currentLocalVariablesNode = nextLevelLocalVariablesNode;
                    }
                    else if (codeBlock.GetType() == typeof(ClosedCurlyBracket))
                    {
                        var tempNode = currentLocalVariablesNode.Parent;
                        currentLocalVariablesNode.LocalVariablesDiscovered.Clear();
                        currentLocalVariablesNode = tempNode;
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        // TODO - Extend functionality for cases where the sprite variable itself isn't just a local or global variable, but
        // instead a property of an object
        // TODO - should consider the Asset Key as more of 'blob' (ie List<CodeBlock>) rather than just an individual codeblock
        private static Tuple<CodeBlock, int, int> GetAssetKeyForSprite(CodeFile codeFile, int idx, CodeBlock inputCodeBlock)
        {
            var variable = (Variable)inputCodeBlock;

            CodeBlock assetKey = null;
            int beginCommentIdx = -1;
            int endCommentIdx = -1;

            for (var i = idx + 1; i < codeFile.Contents.Count; i++)
            {
                var codeBlock = codeFile.Contents[i];

                if (codeBlock.GetType().IsSubclassOf(typeof(Variable)))
                {
                    if (variable.Name.ToLower() == ((Variable)codeBlock).Name.ToLower())
                    {
                        beginCommentIdx = i;

                        var codeBlock2 = codeFile.Contents[i + 1];

                        if (codeBlock2.GetType() == typeof(Dot))
                        {
                            var codeBlock3 = codeFile.Contents[i + 2];

                            if (codeBlock3.GetType() == typeof(BasicCodeToken))
                            {
                                if (((BasicCodeToken)codeBlock3).Value.ToLower() == "image")
                                {
                                    for (var j = i + 3; j < codeFile.Contents.Count; j++)
                                    {
                                        var codeBlock4 = codeFile.Contents[j];

                                        if (codeBlock4.GetType() == typeof(WhitespaceCharacter) || codeBlock4.GetType() == typeof(EqualsOperator))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            assetKey = codeBlock4;

                                            for (var k = j + 1; k < codeFile.Contents.Count; k++)
                                            {
                                                var codeBlock5 = codeFile.Contents[k];

                                                if (codeBlock5.GetType() == typeof(Semicolon))
                                                {
                                                    endCommentIdx = k;
                                                    break;
                                                }
                                            }

                                            return Tuple.Create<CodeBlock, int, int>(assetKey, beginCommentIdx, endCommentIdx);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return Tuple.Create<CodeBlock, int, int>(null, -1, -1);
        }

        // TODO - Extend functionality for cases where the scene variable itself isn't just a local or global variable, but
        // instead a property of an object
        private static Tuple<Variable, int, int> GetSceneToAddSpriteTo(CodeFile codeFile, int idx, CodeBlock inputCodeBlock)
        {
            var spriteVariable = (Variable)inputCodeBlock;

            Variable sceneToAddSpriteTo = null;
            int beginCommentIdx = -1;
            int endCommentIdx = -1;

            for (var i = idx + 1; i < codeFile.Contents.Count; i++)
            {
                var codeBlock = codeFile.Contents[i];

                if (codeBlock.GetType().IsSubclassOf(typeof(Variable)))
                {
                    if (codeBlock.PhaserObjectType == PhaserObjectType.Scene)
                    {
                        beginCommentIdx = i;
                        sceneToAddSpriteTo = (Variable)codeBlock;

                        var codeBlock2 = codeFile.Contents[i + 1];

                        if (codeBlock2.GetType() == typeof(Dot))
                        {
                            var codeBlock3 = codeFile.Contents[i + 2];

                            if (codeBlock3.GetType() == typeof(BasicCodeToken))
                            {
                                if (((BasicCodeToken)codeBlock3).Value.ToLower() == "add")
                                {
                                    for (var j = i + 3; j < codeFile.Contents.Count; j++)
                                    {
                                        var codeBlock4 = codeFile.Contents[j];

                                        if (codeBlock4.GetType() == typeof(WhitespaceCharacter))
                                        {
                                            continue;
                                        }

                                        if (codeBlock4.GetType() == typeof(OpenRoundBracket))
                                        {
                                            for (var k = j + 1; k < codeFile.Contents.Count; k++)
                                            {
                                                var codeBlock5 = codeFile.Contents[k];

                                                if (codeBlock5.GetType() == typeof(WhitespaceCharacter))
                                                {
                                                    continue;
                                                }
                                                if (codeBlock5.GetType().IsSubclassOf(typeof(Variable)))
                                                {
                                                    if (((Variable)codeBlock5).Name.ToLower() == spriteVariable.Name.ToLower() || codeBlock5.GetType() == spriteVariable.GetType())
                                                    {
                                                        for (var m = k + 1; m < codeFile.Contents.Count; m++)
                                                        {
                                                            var codeBlock6 = codeFile.Contents[m];

                                                            if (codeBlock6.GetType() == typeof(Semicolon))
                                                            {
                                                                endCommentIdx = m;
                                                                break;
                                                            }
                                                        }

                                                        return Tuple.Create<Variable, int, int>(sceneToAddSpriteTo, beginCommentIdx, endCommentIdx);
                                                    }
                                                    else
                                                    {
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                        }

                                        if (codeBlock4.GetType() == typeof(Semicolon))
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return Tuple.Create<Variable, int, int>(null, -1, -1);
        }

        private static Tuple<string, int, int> GetAnimationForSprite(CodeFile codeFile, int idx, CodeBlock inputCodeBlock)
        {
            var variable = (Variable)inputCodeBlock;

            string animationAssetKey = null;
            int beginCommentIdx = -1;
            int endCommentIdx = -1;

            for (var i = idx + 1; i < codeFile.Contents.Count; i++)
            {
                var codeBlock = codeFile.Contents[i];

                if (codeBlock.GetType().IsSubclassOf(typeof(Variable)))
                {
                    if (variable.Name.ToLower() == ((Variable)codeBlock).Name.ToLower())
                    {
                        beginCommentIdx = i;

                        var codeBlock2 = codeFile.Contents[i + 1];

                        if (codeBlock2.GetType() == typeof(Dot))
                        {
                            var codeBlock3 = codeFile.Contents[i + 2];

                            if (codeBlock3.GetType() == typeof(BasicCodeToken))
                            {
                                if (((BasicCodeToken)codeBlock3).Value.ToLower() == "animation")
                                {
                                    for (var j = i + 3; j < codeFile.Contents.Count; j++)
                                    {
                                        var codeBlock4 = codeFile.Contents[j];

                                        if (codeBlock4.GetType() == typeof(WhitespaceCharacter) || codeBlock4.GetType() == typeof(EqualsOperator))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            animationAssetKey = ((StringValue)codeBlock4).Val.Trim('"');

                                            for (var k = j + 1; k < codeFile.Contents.Count; k++)
                                            {
                                                var codeBlock5 = codeFile.Contents[k];

                                                if (codeBlock5.GetType() == typeof(Semicolon))
                                                {
                                                    endCommentIdx = k;
                                                    break;
                                                }
                                            }

                                            return Tuple.Create<string, int, int>(animationAssetKey, beginCommentIdx, endCommentIdx);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return Tuple.Create<string, int, int>(null, -1, -1);
        }

        private static void PopulatePhaserGlobalVar()
        {
            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                foreach (var codeBlock in codeFile.Contents)
                {
                    if (codeBlock.GetType() == typeof(GlobalVariable))
                    {
                        var globalVarCodeBlock = (GlobalVariable)codeBlock;

                        if (GlobalVars.PhaserCodeRepo.PhaserGlobalVars.FirstOrDefault(x => x.Name.ToLower() == globalVarCodeBlock.Name.ToLower()) == null)
                        {
                            GlobalVars.PhaserCodeRepo.PhaserGlobalVars.Add(globalVarCodeBlock);
                        }
                        else
                        {
                            if (GlobalVars.PhaserCodeRepo.PhaserGlobalVars.FirstOrDefault(x => x.Name.ToLower() == globalVarCodeBlock.Name.ToLower()).PhaserObjectType == PhaserObjectType.None)
                            {
                                GlobalVars.PhaserCodeRepo.PhaserGlobalVars.FirstOrDefault(x => x.Name.ToLower() == globalVarCodeBlock.Name.ToLower()).PhaserObjectType = globalVarCodeBlock.PhaserObjectType;
                            }
                        }
                    }
                }
            }
        }

        private static void PropagatePhaserObjectTypeForLocalVariablesAndGlobalVariables()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                var currentLocalVariablesNode = new LocalVariablesTreeNode
                {
                    Parent = null,
                    LocalVariablesDiscovered = new List<LocalVariable>()
                };

                var currentLocalVariablesCollection = new List<LocalVariable>();

                foreach (var codeBlock in codeFile.Contents)
                {
                    if (codeBlock.GetType() == typeof(GlobalVariable))
                    {
                        var globalVarCodeBlock = (GlobalVariable)codeBlock;
                        globalVarCodeBlock.PhaserObjectType = GlobalVars.PhaserCodeRepo.PhaserGlobalVars.FirstOrDefault(x => x.Name.ToLower() == globalVarCodeBlock.Name.ToLower()).PhaserObjectType;
                    }
                    else if (codeBlock.GetType() == typeof(LocalVariable))
                    {
                        if (!currentLocalVariablesNode.ContainsLocalVariable((LocalVariable)codeBlock))
                        {
                            currentLocalVariablesNode.LocalVariablesDiscovered.Add((LocalVariable)codeBlock);
                        }
                        else
                        {
                            if (((LocalVariable)codeBlock).PhaserObjectType != PhaserObjectType.None)
                            {
                                currentLocalVariablesNode.SetPhaserObjectTypeForLocalVariables((LocalVariable)codeBlock);
                            }
                        }

                        currentLocalVariablesCollection.Add((LocalVariable)codeBlock);
                    }
                    else if (codeBlock.GetType() == typeof(OpenCurlyBracket))
                    {
                        var nextLevelLocalVariablesNode = new LocalVariablesTreeNode
                        {
                            Parent = currentLocalVariablesNode,
                            LocalVariablesDiscovered = new List<LocalVariable>()
                        };

                        currentLocalVariablesNode = nextLevelLocalVariablesNode;
                    }
                    else if (codeBlock.GetType() == typeof(ClosedCurlyBracket))
                    {
                        // set the Phaser Object Type for all local variables in the currentLocalVariablesCollection
                        foreach (var localVar in currentLocalVariablesCollection)
                        {
                            localVar.PhaserObjectType = currentLocalVariablesNode.GetLocalVariableByName(localVar.Name).PhaserObjectType;
                        }

                        // and remove the currentLocalVariablesNode local variables from the currentLocalVariablesCollection
                        currentLocalVariablesCollection.RemoveAll(lvColl => currentLocalVariablesNode.LocalVariablesDiscovered.Exists(lvNode => lvNode.Name.ToLower() == lvColl.Name.ToLower()));

                        var tempNode = currentLocalVariablesNode.Parent;
                        currentLocalVariablesNode.LocalVariablesDiscovered.Clear();
                        currentLocalVariablesNode = tempNode;
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        // TODO - Extend functionality for cases where the sprite variable itself isn't just a global variable, but
        // instead a property of an object
        // TODO - need to handle for cases where the global variable is re-initialized to a 'new Sprite()' again later on
        private static void Process2ndPassForGlobalVariableSprites()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                // Note that in our case for this function, we simply add the global variables once we come across them the 'first' time,
                // and then dont worry about processing them again
                var globalVariablesCollection = new List<GlobalVariable>();

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(GlobalVariable))
                    {
                        var globalVariableRef = (GlobalVariable)codeBlock;

                        if (!globalVariablesCollection.Exists(gv => ((GlobalVariable)codeBlock).Name.ToLower() == gv.Name.ToLower()))
                        {
                            if (codeBlock.PhaserObjectType == PhaserObjectType.Sprite)
                            {
                                var resultAssetKeyTuple = GetAssetKeyForSprite(codeFile, i, codeBlock);

                                if (resultAssetKeyTuple.Item1 != null)
                                {
                                    if (resultAssetKeyTuple.Item2 >= 0)
                                    {
                                        codeFile.Contents.Insert(resultAssetKeyTuple.Item2 - 1, new CommentBlockBegin());
                                    }

                                    if (resultAssetKeyTuple.Item3 >= 0)
                                    {
                                        codeFile.Contents.Insert(resultAssetKeyTuple.Item3 + 2, new CommentBlockEnd());
                                    }
                                }
                                else
                                {
                                    foreach (var codeFileAssetKeySearch in GlobalVars.Torque2dModuleDatabase.CodeFileList)
                                    {
                                        resultAssetKeyTuple = GetAssetKeyForSprite(codeFileAssetKeySearch, i, codeBlock);

                                        if (resultAssetKeyTuple.Item1 != null)
                                        {
                                            if (resultAssetKeyTuple.Item2 >= 0)
                                            {
                                                codeFileAssetKeySearch.Contents.Insert(resultAssetKeyTuple.Item2 - 1, new CommentBlockBegin());
                                            }

                                            if (resultAssetKeyTuple.Item3 >= 0)
                                            {
                                                codeFileAssetKeySearch.Contents.Insert(resultAssetKeyTuple.Item3 + 2, new CommentBlockEnd());
                                            }

                                            break;
                                        }
                                    }
                                }

                                var resultSceneToAddThisSpriteToTuple = GetSceneToAddSpriteTo(codeFile, i, codeBlock);

                                if (resultSceneToAddThisSpriteToTuple.Item1 != null)
                                {
                                    if (resultSceneToAddThisSpriteToTuple.Item2 >= 0)
                                    {
                                        codeFile.Contents.Insert(resultSceneToAddThisSpriteToTuple.Item2 - 1, new CommentBlockBegin());
                                    }

                                    if (resultSceneToAddThisSpriteToTuple.Item3 >= 0)
                                    {
                                        codeFile.Contents.Insert(resultSceneToAddThisSpriteToTuple.Item3 + 2, new CommentBlockEnd());
                                    }
                                }
                                else
                                {
                                    foreach (var codeFileSceneToAddThisSpriteToSearch in GlobalVars.Torque2dModuleDatabase.CodeFileList)
                                    {
                                        resultSceneToAddThisSpriteToTuple = GetSceneToAddSpriteTo(codeFileSceneToAddThisSpriteToSearch, i, codeBlock);

                                        if (resultSceneToAddThisSpriteToTuple.Item1 != null)
                                        {
                                            if (resultSceneToAddThisSpriteToTuple.Item2 >= 0)
                                            {
                                                codeFileSceneToAddThisSpriteToSearch.Contents.Insert(resultSceneToAddThisSpriteToTuple.Item2 - 1, new CommentBlockBegin());
                                            }

                                            if (resultSceneToAddThisSpriteToTuple.Item3 >= 0)
                                            {
                                                codeFileSceneToAddThisSpriteToSearch.Contents.Insert(resultSceneToAddThisSpriteToTuple.Item3 + 2, new CommentBlockEnd());
                                            }

                                            break;
                                        }
                                    }
                                }

                                // NOTE: Not all sprites are expected to have an Animation.  Many will not have an animation.
                                var resultAnimationForSpriteTuple = GetAnimationForSprite(codeFile, i, codeBlock);

                                if (resultAnimationForSpriteTuple.Item2 >= 0)
                                {
                                    codeFile.Contents.Insert(resultAnimationForSpriteTuple.Item2 - 1, new CommentBlockBegin());
                                }

                                if (resultAnimationForSpriteTuple.Item3 >= 0)
                                {
                                    codeFile.Contents.Insert(resultAnimationForSpriteTuple.Item3 + 2, new CommentBlockEnd());
                                }

                                // store set of Scenes and associated Animations needed to add to them.  Will assign the animation to the Scene class, and add in the create function of the class (to be done later)
                                if (resultAnimationForSpriteTuple.Item1 != null)
                                {
                                    var sceneVariable = resultSceneToAddThisSpriteToTuple.Item1;

                                    if (sceneVariable != null)
                                    {
                                        if (sceneVariable.Class != null)
                                        {
                                            if (!GlobalVars.PhaserCodeRepo.PhaserSceneListAndAnimationsMap.ContainsKey(sceneVariable.Class))
                                            {
                                                GlobalVars.PhaserCodeRepo.PhaserSceneListAndAnimationsMap.Add(sceneVariable.Class, new List<string> { resultAnimationForSpriteTuple.Item1 });
                                            }
                                            else if (!GlobalVars.PhaserCodeRepo.PhaserSceneListAndAnimationsMap[sceneVariable.Class].Contains(resultAnimationForSpriteTuple.Item1))
                                            {
                                                GlobalVars.PhaserCodeRepo.PhaserSceneListAndAnimationsMap[sceneVariable.Class].Add(resultAnimationForSpriteTuple.Item1);
                                            }
                                        }
                                    }
                                }

                                for (var j = i + 1; j < codeFile.Contents.Count; j++)
                                {
                                    bool finishedSpriteInstantiationCodeConvert = false;
                                    var idx = -1;

                                    var codeBlock2 = codeFile.Contents[j];

                                    if (codeBlock2.GetType() == typeof(WhitespaceCharacter) || codeBlock2.GetType() == typeof(EqualsOperator) || codeBlock2.GetType() == typeof(NewOperator))
                                    {
                                        continue;
                                    }

                                    if (codeBlock2.GetType() == typeof(BasicCodeToken))
                                    {
                                        if (((BasicCodeToken)codeBlock2).Value.ToLower() == Torque2dConstants.SpriteClassName.ToLower())
                                        {
                                            for (var k = j + 1; k < codeFile.Contents.Count; k++)
                                            {
                                                var codeBlock3 = codeFile.Contents[k];

                                                if (codeBlock3.GetType() == typeof(WhitespaceCharacter))
                                                {
                                                    continue;
                                                }

                                                if (codeBlock3.GetType() == typeof(OpenRoundBracket))
                                                {
                                                    if (resultSceneToAddThisSpriteToTuple.Item1 == null)
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new LocalVariable{ Name = "%undefinedOccurredDuringPhaserConversionForSceneToAddTo", PhaserObjectType = PhaserObjectType.None },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            resultAssetKeyTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForAssetKey" } : resultAssetKeyTuple.Item1.DeepCopy()
                                                        });
                                                    }
                                                    else if (resultSceneToAddThisSpriteToTuple.Item1.GetType() == typeof(LocalVariable))
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new LocalVariable{ Name = resultSceneToAddThisSpriteToTuple.Item1.Name, PhaserObjectType = resultSceneToAddThisSpriteToTuple.Item1.PhaserObjectType },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            resultAssetKeyTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForAssetKey" } : resultAssetKeyTuple.Item1.DeepCopy()
                                                        });
                                                    }
                                                    else if (resultSceneToAddThisSpriteToTuple.Item1.GetType() == typeof(GlobalVariable))
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new GlobalVariable{ Name = resultSceneToAddThisSpriteToTuple.Item1.Name, PhaserObjectType = resultSceneToAddThisSpriteToTuple.Item1.PhaserObjectType },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            resultAssetKeyTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForAssetKey" } : resultAssetKeyTuple.Item1.DeepCopy()
                                                        });
                                                    }

                                                    finishedSpriteInstantiationCodeConvert = true;
                                                    idx = k + 7;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }

                                    if (finishedSpriteInstantiationCodeConvert)
                                    {
                                        var breakOutOfLoop = false;

                                        if (resultAnimationForSpriteTuple.Item1 != null)
                                        {
                                            for (var k = idx; k < codeFile.Contents.Count; k++)
                                            {
                                                var codeBlock3 = codeFile.Contents[k];

                                                if (codeBlock3.GetType() == typeof(Semicolon))
                                                {
                                                    var codeBlock4 = codeFile.Contents[k + 1];

                                                    if (codeBlock4.GetType() == typeof(NewLineCharacter))
                                                    {
                                                        k++;
                                                    }

                                                    codeFile.Contents.InsertRange(k + 1, GenerateAnimationStatementForSprite(globalVariableRef, resultAnimationForSpriteTuple.Item1));

                                                    breakOutOfLoop = true;
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }

                                        if (breakOutOfLoop)
                                        {
                                            break;
                                        }
                                    }
                                }

                                if (!globalVariablesCollection.Exists(gv => ((GlobalVariable)codeBlock).Name.ToLower() == gv.Name.ToLower()))
                                {
                                    globalVariablesCollection.Add((GlobalVariable)codeBlock);
                                }
                            }
                        }
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        private static void Process2ndPassForWaitForSceneIsActiveCallbacks()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                FunctionDeclaration currentFunction = null;
                ClassMethod currentMethod = null;
                var callbackName = "";

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(FunctionDeclaration))
                    {
                        currentFunction = (FunctionDeclaration)codeBlock;
                        callbackName = currentFunction.Name + PhaserConstants.CallbackNameSuffix;
                    }

                    if (codeBlock.GetType() == typeof(ClassMethod))
                    {
                        currentMethod = (ClassMethod)codeBlock;
                        callbackName = currentMethod.MethodName + PhaserConstants.CallbackNameSuffix;
                    }

                    if (codeBlock.GetType() == typeof(BasicCodeToken))
                    {
                        if (((BasicCodeToken)codeBlock).Value == PhaserConstants.T2dToPhaserRunSceneTodoMarker)
                        {
                            if (currentMethod != null)
                            {
                                newCodeFile.Contents.AddRange(new List<CodeBlock> {
                                    new LocalVariable { Name = "%this" },
                                    new Dot(),
                                    new BasicCodeToken { Value = callbackName }
                                });
                            }
                            else
                            {
                                newCodeFile.Contents.Add(new BasicCodeToken { Value = callbackName });
                            }

                            for (i = i + 1; i < codeFile.Contents.Count; i++)
                            {
                                var codeBlock2 = codeFile.Contents[i];

                                if (codeBlock2.GetType() == typeof(Semicolon))
                                {
                                    newCodeFile.Contents.Add(codeBlock2);
                                    i++;

                                    var callbackContents = new List<CodeBlock>();
                                    var curlyBracketCount = 0;

                                    for (var j = i; j < codeFile.Contents.Count; /* do not need to increment, since we keep removing a CodeBlock each iteration - ie codeFile.Contents.RemoveAt(j);*/)
                                    {
                                        var codeBlock3 = codeFile.Contents[j];
                                        callbackContents.Add(codeBlock3);
                                        codeFile.Contents.RemoveAt(j);

                                        if (codeBlock3.GetType() == typeof(OpenCurlyBracket))
                                        {
                                            curlyBracketCount++;
                                        }
                                        if (codeBlock3.GetType() == typeof(ClosedCurlyBracket))
                                        {
                                            curlyBracketCount--;

                                            if (curlyBracketCount < 0)
                                            {
                                                // can now append the callback to our current codeFile
                                                if (currentMethod != null)
                                                {
                                                    codeFile.Contents.AddRange(new List<CodeBlock> {
                                                        new NewLineCharacter(),
                                                        new NewLineCharacter(),
                                                        new ClassMethod { ClassName = currentMethod.ClassName, MethodName = callbackName },
                                                        new OpenRoundBracket(),
                                                        new ClosedRoundBracket(),
                                                        new WhitespaceCharacter { WhitespaceChar = ' ' },
                                                        new OpenCurlyBracket(),
                                                        new NewLineCharacter(),
                                                        new NewLineCharacter(),
                                                    });

                                                    codeFile.Contents.AddRange(callbackContents);
                                                }
                                                else
                                                {
                                                    codeFile.Contents.AddRange(new List<CodeBlock> {
                                                        new NewLineCharacter(),
                                                        new NewLineCharacter(),
                                                        new FunctionDeclaration { Name = callbackName },
                                                        new OpenRoundBracket(),
                                                        new ClosedRoundBracket(),
                                                        new WhitespaceCharacter { WhitespaceChar = ' ' },
                                                        new OpenCurlyBracket(),
                                                        new NewLineCharacter(),
                                                        new NewLineCharacter(),
                                                    });

                                                    codeFile.Contents.AddRange(callbackContents);
                                                }

                                                break;
                                            }
                                        }
                                    }

                                    // add in comment to let ppl know that this resumes at the callback
                                    var comment = new SingleLineComment();

                                    if (currentMethod != null)
                                    {
                                        comment.CodeBlock = "// resumes at '" + callbackName + "' callback method";
                                    }
                                    else
                                    {
                                        comment.CodeBlock = "// resumes at '" + callbackName + "' callback function";
                                    }

                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                    {
                                        new NewLineCharacter(),
                                        new NewLineCharacter(),
                                        comment,
                                        new NewLineCharacter(),
                                        new ClosedCurlyBracket()
                                    });

                                    currentFunction = null;
                                    currentMethod = null;
                                    callbackName = "";

                                    i--;

                                    break;
                                }

                                newCodeFile.Contents.Add(codeBlock2);
                            }

                            continue;
                        }
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        private static void Process2ndPassForSceneLayerValues()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                var foundSceneLayerField = false;

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(BasicCodeToken))
                    {
                        if (((BasicCodeToken)codeBlock).Value.ToLower() == "scenelayer")
                        {
                            foundSceneLayerField = true;
                        }
                    }
                    else if (foundSceneLayerField && codeBlock.GetType() == typeof(NumericValue))
                    {
                        // make numeric value a negative number, unless it is a zero, then leave as-is
                        if (((NumericValue)codeBlock).NumberAsString != "0")
                        {
                            ((NumericValue)codeBlock).NumberAsString = "-" + ((NumericValue)codeBlock).NumberAsString;
                        }
                        foundSceneLayerField = false;
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        private static List<CodeBlock> GenerateAnimationStatementForSprite(Variable spriteVar, string animationAssetKey)
        {
            var animationAsset = (Torque2dAnimationAsset)GlobalVars.Torque2dModuleDatabase.Torque2dAssetList.FirstOrDefault(asset => asset.Name == animationAssetKey);

            if (animationAsset != null)
            {
                if (spriteVar.GetType() == typeof(GlobalVariable))
                {
                    return new List<CodeBlock>
                    {
                        new GlobalVariable { Name = spriteVar.Name, PhaserObjectType = spriteVar.PhaserObjectType },
                        new Dot(),
                        new BasicCodeToken { Value = "anims" },
                        new Dot(),
                        new BasicCodeToken { Value = "play" },
                        new OpenRoundBracket(),
                        new StringValue { Val = "\"" + animationAssetKey + "\"" },
                        new Comma(),
                        new WhitespaceCharacter { WhitespaceChar = ' ' },
                        new BooleanTrueValue(),
                        new ClosedRoundBracket(),
                        new Semicolon(),
                        new NewLineCharacter(),
                    };
                }
                else // spriteVar.GetType() == typeof(LocalVariable)
                {
                    return new List<CodeBlock>
                    {
                        new LocalVariable { Name = spriteVar.Name, PhaserObjectType = spriteVar.PhaserObjectType },
                        new Dot(),
                        new BasicCodeToken { Value = "anims" },
                        new Dot(),
                        new BasicCodeToken { Value = "play" },
                        new OpenRoundBracket(),
                        new StringValue { Val = "\"" + animationAssetKey + "\"" },
                        new Comma(),
                        new WhitespaceCharacter { WhitespaceChar = ' ' },
                        new BooleanTrueValue(),
                        new ClosedRoundBracket(),
                        new Semicolon(),
                        new NewLineCharacter(),
                    };
                }
            }

            // return empty list if for some reason the animation asset cannot be found
            return new List<CodeBlock>();
        }

        private static void Process2ndPassForLocalVariableScenes()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                var currentLocalVariablesNode = new LocalVariablesTreeNode
                {
                    Parent = null,
                    LocalVariablesDiscovered = new List<LocalVariable>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(LocalVariable))
                    {
                        var localVariableRef = (LocalVariable)codeBlock;
                        bool continueForLoop = false;

                        // Check to make sure this is a statement where the local variable is initialized.  If its something else (ie like
                        // a local variable in a function declaration) then should continue instead
                        for (var j = i + 1; j < codeFile.Contents.Count; j++)
                        {
                            var codeBlock2 = codeFile.Contents[j];

                            if (codeBlock2.GetType() == typeof(EqualsOperator))
                            {
                                break;
                            }
                            else if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                            {
                                continueForLoop = true;
                                break;
                            }
                            else if (codeBlock2.GetType() == typeof(Dot))
                            {
                                continueForLoop = true;
                                break;
                            }
                        }

                        if (continueForLoop)
                        {
                            newCodeFile.Contents.Add(codeBlock);
                            continue;
                        }

                        if (!currentLocalVariablesNode.ContainsLocalVariable((LocalVariable)codeBlock))
                        {
                            if (codeBlock.PhaserObjectType == PhaserObjectType.Scene)
                            {
                                for (var j = i; j < codeFile.Contents.Count; j++)
                                {
                                    var codeBlock2 = codeFile.Contents[j];

                                    if (codeBlock2.GetType() == typeof(NewOperator))
                                    {
                                        var idx = j + 1;

                                        while (idx < codeFile.Contents.Count)
                                        {
                                            if (codeFile.Contents[idx].GetType() == typeof(BasicCodeToken))
                                            {
                                                break;
                                            }

                                            idx++;
                                        }

                                        var matchingSceneClass = FindMatchingSceneClass((BasicCodeToken)codeFile.Contents[idx]);

                                        if (matchingSceneClass != null)
                                        {
                                            localVariableRef.Class = matchingSceneClass.ClassName;
                                        }

                                        break;
                                    }
                                }
                            }

                            if (!currentLocalVariablesNode.ContainsLocalVariable((LocalVariable)codeBlock))
                            {
                                currentLocalVariablesNode.LocalVariablesDiscovered.Add((LocalVariable)codeBlock);
                            }
                        }
                    }

                    else if (codeBlock.GetType() == typeof(OpenCurlyBracket))
                    {
                        var nextLevelLocalVariablesNode = new LocalVariablesTreeNode
                        {
                            Parent = currentLocalVariablesNode,
                            LocalVariablesDiscovered = new List<LocalVariable>()
                        };

                        currentLocalVariablesNode = nextLevelLocalVariablesNode;
                    }
                    else if (codeBlock.GetType() == typeof(ClosedCurlyBracket))
                    {
                        var tempNode = currentLocalVariablesNode.Parent;
                        currentLocalVariablesNode.LocalVariablesDiscovered.Clear();
                        currentLocalVariablesNode = tempNode;
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        private static void Process2ndPassForGlobalVariableScenes()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                // Note that in our case for this function, we simply add the global variables once we come across them the 'first' time,
                // and then dont worry about processing them again
                var globalVariablesCollection = new List<GlobalVariable>();

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(GlobalVariable))
                    {
                        var globalVariableRef = (GlobalVariable)codeBlock;
                        bool continueForLoop = false;

                        // Check to make sure this is a statement where the local variable is initialized.  If its something else (ie like
                        // a local variable in a function declaration) then should continue instead
                        for (var j = i + 1; j < codeFile.Contents.Count; j++)
                        {
                            var codeBlock2 = codeFile.Contents[j];

                            if (codeBlock2.GetType() == typeof(EqualsOperator))
                            {
                                break;
                            }
                            else if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                            {
                                continueForLoop = true;
                                break;
                            }
                            else if (codeBlock2.GetType() == typeof(Dot))
                            {
                                continueForLoop = true;
                                break;
                            }
                        }

                        if (continueForLoop)
                        {
                            newCodeFile.Contents.Add(codeBlock);
                            continue;
                        }

                        if (!globalVariablesCollection.Exists(gv => ((GlobalVariable)codeBlock).Name.ToLower() == gv.Name.ToLower()))
                        {
                            if (codeBlock.PhaserObjectType == PhaserObjectType.Scene)
                            {
                                for (var j = i; j < codeFile.Contents.Count; j++)
                                {
                                    var codeBlock2 = codeFile.Contents[j];

                                    if (codeBlock2.GetType() == typeof(NewOperator))
                                    {
                                        var idx = j + 1;

                                        while (idx < codeFile.Contents.Count)
                                        {
                                            if (codeFile.Contents[idx].GetType() == typeof(BasicCodeToken))
                                            {
                                                break;
                                            }

                                            idx++;
                                        }

                                        var matchingSceneClass = FindMatchingSceneClass((BasicCodeToken)codeFile.Contents[idx]);

                                        if (matchingSceneClass != null)
                                        {
                                            globalVariableRef.Class = matchingSceneClass.ClassName;
                                        }

                                        break;
                                    }
                                }
                            }

                            if (!globalVariablesCollection.Exists(gv => ((GlobalVariable)codeBlock).Name.ToLower() == gv.Name.ToLower()))
                            {
                                globalVariablesCollection.Add((GlobalVariable)codeBlock);
                            }
                        }
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        private static void PropagateClassForLocalVariablesAndGlobalVariables()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                var currentLocalVariablesNode = new LocalVariablesTreeNode
                {
                    Parent = null,
                    LocalVariablesDiscovered = new List<LocalVariable>()
                };

                var currentLocalVariablesCollection = new List<LocalVariable>();

                foreach (var codeBlock in codeFile.Contents)
                {
                    if (codeBlock.GetType() == typeof(GlobalVariable))
                    {
                        var globalVarCodeBlock = (GlobalVariable)codeBlock;
                        globalVarCodeBlock.Class = GlobalVars.PhaserCodeRepo.PhaserGlobalVars.FirstOrDefault(x => x.Name.ToLower() == globalVarCodeBlock.Name.ToLower()).Class;
                    }
                    else if (codeBlock.GetType() == typeof(LocalVariable))
                    {
                        if (!currentLocalVariablesNode.ContainsLocalVariable((LocalVariable)codeBlock))
                        {
                            currentLocalVariablesNode.LocalVariablesDiscovered.Add((LocalVariable)codeBlock);
                        }
                        else
                        {
                            if (((LocalVariable)codeBlock).Class != null)
                            {
                                currentLocalVariablesNode.SetClassForLocalVariables((LocalVariable)codeBlock);
                            }
                        }

                        currentLocalVariablesCollection.Add((LocalVariable)codeBlock);
                    }
                    else if (codeBlock.GetType() == typeof(OpenCurlyBracket))
                    {
                        var nextLevelLocalVariablesNode = new LocalVariablesTreeNode
                        {
                            Parent = currentLocalVariablesNode,
                            LocalVariablesDiscovered = new List<LocalVariable>()
                        };

                        currentLocalVariablesNode = nextLevelLocalVariablesNode;
                    }
                    else if (codeBlock.GetType() == typeof(ClosedCurlyBracket))
                    {
                        // set the Phaser Object Type for all local variables in the currentLocalVariablesCollection
                        foreach (var localVar in currentLocalVariablesCollection)
                        {
                            localVar.PhaserObjectType = currentLocalVariablesNode.GetLocalVariableByName(localVar.Name).PhaserObjectType;
                        }

                        // and remove the currentLocalVariablesNode local variables from the currentLocalVariablesCollection
                        currentLocalVariablesCollection.RemoveAll(lvColl => currentLocalVariablesNode.LocalVariablesDiscovered.Exists(lvNode => lvNode.Name.ToLower() == lvColl.Name.ToLower()));

                        var tempNode = currentLocalVariablesNode.Parent;
                        currentLocalVariablesNode.LocalVariablesDiscovered.Clear();
                        currentLocalVariablesNode = tempNode;
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        private static void ProcessPhaserGlobalVarsWithSceneClasses()
        {
            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                foreach (var codeBlock in codeFile.Contents)
                {
                    if (codeBlock.GetType() == typeof(GlobalVariable))
                    {
                        var globalVarCodeBlock = (GlobalVariable)codeBlock;

                        if (GlobalVars.PhaserCodeRepo.PhaserGlobalVars.FirstOrDefault(x => x.Name.ToLower() == globalVarCodeBlock.Name.ToLower()) == null)
                        {
                            GlobalVars.PhaserCodeRepo.PhaserGlobalVars.Add(globalVarCodeBlock);
                        }
                        else
                        {
                            if (GlobalVars.PhaserCodeRepo.PhaserGlobalVars.FirstOrDefault(x => x.Name.ToLower() == globalVarCodeBlock.Name.ToLower()).Class == null)
                            {
                                GlobalVars.PhaserCodeRepo.PhaserGlobalVars.FirstOrDefault(x => x.Name.ToLower() == globalVarCodeBlock.Name.ToLower()).Class = globalVarCodeBlock.Class;
                            }
                        }
                    }
                }
            }
        }

        // TODO - Extend functionality for cases where the text variable itself isn't just a local variable, but
        // instead a property of an object
        // TODO - need to handle for cases where the local variable is re-initialized to a 'new Text()' again later on
        private static void Process2ndPassForLocalVariablePhaserTexts()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                // Note that in our case for this function, we simply add the local variables once we come across them the 'first' time,
                // and then dont worry about processing them again
                var currentLocalVariablesNode = new LocalVariablesTreeNode
                {
                    Parent = null,
                    LocalVariablesDiscovered = new List<LocalVariable>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(LocalVariable))
                    {
                        var localVariableRef = (LocalVariable)codeBlock;
                        bool continueForLoop = false;

                        // Check to make sure this is a statement where the local variable is initialized.  If its something else (ie like
                        // a local variable in a function declaration) then should continue instead
                        for (var j = i + 1; j < codeFile.Contents.Count; j++)
                        {
                            var codeBlock2 = codeFile.Contents[j];

                            if (codeBlock2.GetType() == typeof(EqualsOperator))
                            {
                                break;
                            }
                            else if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                            {
                                continueForLoop = true;
                                break;
                            }
                        }

                        if (continueForLoop)
                        {
                            newCodeFile.Contents.Add(codeBlock);
                            continue;
                        }

                        if (!currentLocalVariablesNode.ContainsLocalVariable((LocalVariable)codeBlock))
                        {
                            if (codeBlock.PhaserObjectType == PhaserObjectType.Text)
                            {
                                var resultFontTuple = GetFontForText(codeFile, i, codeBlock);

                                if (resultFontTuple.Item2 >= 0)
                                {
                                    codeFile.Contents.Insert(resultFontTuple.Item2 - 1, new CommentBlockBegin());
                                }

                                if (resultFontTuple.Item3 >= 0)
                                {
                                    codeFile.Contents.Insert(resultFontTuple.Item3 + 2, new CommentBlockEnd());
                                }

                                var resultSceneToAddThisTextToTuple = GetSceneToAddTextTo(codeFile, i, codeBlock);

                                if (resultSceneToAddThisTextToTuple.Item2 >= 0)
                                {
                                    codeFile.Contents.Insert(resultSceneToAddThisTextToTuple.Item2 - 1, new CommentBlockBegin());
                                }

                                if (resultSceneToAddThisTextToTuple.Item3 >= 0)
                                {
                                    codeFile.Contents.Insert(resultSceneToAddThisTextToTuple.Item3 + 2, new CommentBlockEnd());
                                }

                                for (var j = i + 1; j < codeFile.Contents.Count; j++)
                                {
                                    var codeBlock2 = codeFile.Contents[j];

                                    if (codeBlock2.GetType() == typeof(WhitespaceCharacter) || codeBlock2.GetType() == typeof(EqualsOperator) || codeBlock2.GetType() == typeof(NewOperator))
                                    {
                                        continue;
                                    }

                                    if (codeBlock2.GetType() == typeof(BasicCodeToken))
                                    {
                                        if (((BasicCodeToken)codeBlock2).Value.ToLower() == Torque2dConstants.TextSpriteClassName.ToLower())
                                        {
                                            for (var k = j + 1; k < codeFile.Contents.Count; k++)
                                            {
                                                var codeBlock3 = codeFile.Contents[k];

                                                if (codeBlock3.GetType() == typeof(WhitespaceCharacter))
                                                {
                                                    continue;
                                                }

                                                if (codeBlock3.GetType() == typeof(OpenRoundBracket))
                                                {
                                                    if (resultSceneToAddThisTextToTuple.Item1 == null)
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new LocalVariable{ Name = "%undefinedOccurredDuringPhaserConversionForSceneToAddTo", PhaserObjectType = PhaserObjectType.None },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new StringValue { Val = "\"\"" },
                                                            new Comma(),
                                                            new OpenCurlyBracket(),
                                                            new BasicCodeToken { Value = "fontFamily" },
                                                            new Colon(),
                                                            resultFontTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForFont" } : resultFontTuple.Item1.DeepCopy(),
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "fontSize" },
                                                            new Colon(),
                                                            new NumericValue { NumberAsString = "32" }, // TODO: Create some sort of fontSize conversion formula (based on T2D FontSize property) for this line.  Consider adding code to MathConvertUtil and PhaserTextBaseClass
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "color" },
                                                            new Colon(),
                                                            new StringValue { Val = "\"#ff0000\"" }, // TODO: Still need to implement how to set Font color
                                                            new ClosedCurlyBracket(),
                                                        });
                                                    }
                                                    else if (resultSceneToAddThisTextToTuple.Item1.GetType() == typeof(LocalVariable))
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new LocalVariable{ Name = resultSceneToAddThisTextToTuple.Item1.Name, PhaserObjectType = resultSceneToAddThisTextToTuple.Item1.PhaserObjectType },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new StringValue { Val = "\"\"" },
                                                            new Comma(),
                                                            new OpenCurlyBracket(),
                                                            new BasicCodeToken { Value = "fontFamily" },
                                                            new Colon(),
                                                            resultFontTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForFont" } : resultFontTuple.Item1.DeepCopy(),
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "fontSize" },
                                                            new Colon(),
                                                            new NumericValue { NumberAsString = "32" }, // TODO: Create some sort of fontSize conversion formula (based on T2D FontSize property) for this line.  Consider adding code to MathConvertUtil and PhaserTextBaseClass
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "color" },
                                                            new Colon(),
                                                            new StringValue { Val = "\"#ff0000\"" }, // TODO: Still need to implement how to set Font color
                                                            new ClosedCurlyBracket(),
                                                        });
                                                    }
                                                    else if (resultSceneToAddThisTextToTuple.Item1.GetType() == typeof(GlobalVariable))
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new GlobalVariable{ Name = resultSceneToAddThisTextToTuple.Item1.Name, PhaserObjectType = resultSceneToAddThisTextToTuple.Item1.PhaserObjectType },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new StringValue { Val = "\"\"" },
                                                            new Comma(),
                                                            new OpenCurlyBracket(),
                                                            new BasicCodeToken { Value = "fontFamily" },
                                                            new Colon(),
                                                            resultFontTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForFont" } : resultFontTuple.Item1.DeepCopy(),
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "fontSize" },
                                                            new Colon(),
                                                            new NumericValue { NumberAsString = "32" }, // TODO: Create some sort of fontSize conversion formula (based on T2D FontSize property) for this line.  Consider adding code to MathConvertUtil and PhaserTextBaseClass
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "color" },
                                                            new Colon(),
                                                            new StringValue { Val = "\"#ff0000\"" }, // TODO: Still need to implement how to set Font color
                                                            new ClosedCurlyBracket(),
                                                        });
                                                    }

                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                if (!currentLocalVariablesNode.ContainsLocalVariable((LocalVariable)codeBlock))
                                {
                                    currentLocalVariablesNode.LocalVariablesDiscovered.Add((LocalVariable)codeBlock);
                                }
                            }
                        }
                    }
                    else if (codeBlock.GetType() == typeof(OpenCurlyBracket))
                    {
                        var nextLevelLocalVariablesNode = new LocalVariablesTreeNode
                        {
                            Parent = currentLocalVariablesNode,
                            LocalVariablesDiscovered = new List<LocalVariable>()
                        };

                        currentLocalVariablesNode = nextLevelLocalVariablesNode;
                    }
                    else if (codeBlock.GetType() == typeof(ClosedCurlyBracket))
                    {
                        var tempNode = currentLocalVariablesNode.Parent;
                        currentLocalVariablesNode.LocalVariablesDiscovered.Clear();
                        currentLocalVariablesNode = tempNode;
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        // TODO - Extend functionality for cases where the sprite variable itself isn't just a local or global variable, but
        // instead a property of an object
        // TODO - should consider the Asset Key as more of 'blob' (ie List<CodeBlock>) rather than just an individual codeblock
        private static Tuple<CodeBlock, int, int> GetFontForText(CodeFile codeFile, int idx, CodeBlock inputCodeBlock)
        {
            var variable = (Variable)inputCodeBlock;

            CodeBlock font = null;
            int beginCommentIdx = -1;
            int endCommentIdx = -1;

            for (var i = idx + 1; i < codeFile.Contents.Count; i++)
            {
                var codeBlock = codeFile.Contents[i];

                if (codeBlock.GetType().IsSubclassOf(typeof(Variable)))
                {
                    if (variable.Name.ToLower() == ((Variable)codeBlock).Name.ToLower())
                    {
                        beginCommentIdx = i;

                        var codeBlock2 = codeFile.Contents[i + 1];

                        if (codeBlock2.GetType() == typeof(Dot))
                        {
                            var codeBlock3 = codeFile.Contents[i + 2];

                            if (codeBlock3.GetType() == typeof(BasicCodeToken))
                            {
                                if (((BasicCodeToken)codeBlock3).Value.ToLower() == "font")
                                {
                                    for (var j = i + 3; j < codeFile.Contents.Count; j++)
                                    {
                                        var codeBlock4 = codeFile.Contents[j];

                                        if (codeBlock4.GetType() == typeof(WhitespaceCharacter) || codeBlock4.GetType() == typeof(EqualsOperator))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            font = codeBlock4;

                                            for (var k = j + 1; k < codeFile.Contents.Count; k++)
                                            {
                                                var codeBlock5 = codeFile.Contents[k];

                                                if (codeBlock5.GetType() == typeof(Semicolon))
                                                {
                                                    endCommentIdx = k;
                                                    break;
                                                }
                                            }

                                            return Tuple.Create<CodeBlock, int, int>(font, beginCommentIdx, endCommentIdx);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return Tuple.Create<CodeBlock, int, int>(null, -1, -1);
        }

        // TODO - Extend functionality for cases where the scene variable itself isn't just a local or global variable, but
        // instead a property of an object
        private static Tuple<Variable, int, int> GetSceneToAddTextTo(CodeFile codeFile, int idx, CodeBlock inputCodeBlock)
        {
            var textVariable = (Variable)inputCodeBlock;

            Variable sceneToAddTextTo = null;
            int beginCommentIdx = -1;
            int endCommentIdx = -1;

            for (var i = idx + 1; i < codeFile.Contents.Count; i++)
            {
                var codeBlock = codeFile.Contents[i];

                if (codeBlock.GetType().IsSubclassOf(typeof(Variable)))
                {
                    if (codeBlock.PhaserObjectType == PhaserObjectType.Scene)
                    {
                        beginCommentIdx = i;
                        sceneToAddTextTo = (Variable)codeBlock;

                        var codeBlock2 = codeFile.Contents[i + 1];

                        if (codeBlock2.GetType() == typeof(Dot))
                        {
                            var codeBlock3 = codeFile.Contents[i + 2];

                            if (codeBlock3.GetType() == typeof(BasicCodeToken))
                            {
                                if (((BasicCodeToken)codeBlock3).Value.ToLower() == "add")
                                {
                                    for (var j = i + 3; j < codeFile.Contents.Count; j++)
                                    {
                                        var codeBlock4 = codeFile.Contents[j];

                                        if (codeBlock4.GetType() == typeof(WhitespaceCharacter))
                                        {
                                            continue;
                                        }

                                        if (codeBlock4.GetType() == typeof(OpenRoundBracket))
                                        {
                                            for (var k = j + 1; k < codeFile.Contents.Count; k++)
                                            {
                                                var codeBlock5 = codeFile.Contents[k];

                                                if (codeBlock5.GetType() == typeof(WhitespaceCharacter))
                                                {
                                                    continue;
                                                }
                                                if (codeBlock5.GetType().IsSubclassOf(typeof(Variable)))
                                                {
                                                    if (((Variable)codeBlock5).Name.ToLower() == textVariable.Name.ToLower() || codeBlock5.GetType() == textVariable.GetType())
                                                    {
                                                        for (var m = k + 1; m < codeFile.Contents.Count; m++)
                                                        {
                                                            var codeBlock6 = codeFile.Contents[m];

                                                            if (codeBlock6.GetType() == typeof(Semicolon))
                                                            {
                                                                endCommentIdx = m;
                                                                break;
                                                            }
                                                        }

                                                        return Tuple.Create<Variable, int, int>(sceneToAddTextTo, beginCommentIdx, endCommentIdx);
                                                    }
                                                    else
                                                    {
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                        }

                                        if (codeBlock4.GetType() == typeof(Semicolon))
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return Tuple.Create<Variable, int, int>(null, -1, -1);
        }

        // TODO - Extend functionality for cases where the text variable itself isn't just a global variable, but
        // instead a property of an object
        // TODO - need to handle for cases where the global variable is re-initialized to a 'new Text()' again later on
        private static void Process2ndPassForGlobalVariablePhaserTexts()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                // Note that in our case for this function, we simply add the global variables once we come across them the 'first' time,
                // and then dont worry about processing them again
                var globalVariablesCollection = new List<GlobalVariable>();

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(GlobalVariable))
                    {
                        var globalVariableRef = (GlobalVariable)codeBlock;

                        if (!globalVariablesCollection.Exists(gv => ((GlobalVariable)codeBlock).Name.ToLower() == gv.Name.ToLower()))
                        {
                            if (codeBlock.PhaserObjectType == PhaserObjectType.Text)
                            {
                                var resultFontTuple = GetFontForText(codeFile, i, codeBlock);

                                if (resultFontTuple.Item1 != null)
                                {
                                    if (resultFontTuple.Item2 >= 0)
                                    {
                                        codeFile.Contents.Insert(resultFontTuple.Item2 - 1, new CommentBlockBegin());
                                    }

                                    if (resultFontTuple.Item3 >= 0)
                                    {
                                        codeFile.Contents.Insert(resultFontTuple.Item3 + 2, new CommentBlockEnd());
                                    }
                                }
                                else
                                {
                                    foreach (var codeFileAssetKeySearch in GlobalVars.Torque2dModuleDatabase.CodeFileList)
                                    {
                                        resultFontTuple = GetFontForText(codeFileAssetKeySearch, i, codeBlock);

                                        if (resultFontTuple.Item1 != null)
                                        {
                                            if (resultFontTuple.Item2 >= 0)
                                            {
                                                codeFileAssetKeySearch.Contents.Insert(resultFontTuple.Item2 - 1, new CommentBlockBegin());
                                            }

                                            if (resultFontTuple.Item3 >= 0)
                                            {
                                                codeFileAssetKeySearch.Contents.Insert(resultFontTuple.Item3 + 2, new CommentBlockEnd());
                                            }

                                            break;
                                        }
                                    }
                                }

                                var resultSceneToAddThisTextToTuple = GetSceneToAddTextTo(codeFile, i, codeBlock);

                                if (resultSceneToAddThisTextToTuple.Item1 != null)
                                {
                                    if (resultSceneToAddThisTextToTuple.Item2 >= 0)
                                    {
                                        codeFile.Contents.Insert(resultSceneToAddThisTextToTuple.Item2 - 1, new CommentBlockBegin());
                                    }

                                    if (resultSceneToAddThisTextToTuple.Item3 >= 0)
                                    {
                                        codeFile.Contents.Insert(resultSceneToAddThisTextToTuple.Item3 + 2, new CommentBlockEnd());
                                    }
                                }
                                else
                                {
                                    foreach (var codeFileSceneToAddThisSpriteToSearch in GlobalVars.Torque2dModuleDatabase.CodeFileList)
                                    {
                                        resultSceneToAddThisTextToTuple = GetSceneToAddTextTo(codeFileSceneToAddThisSpriteToSearch, i, codeBlock);

                                        if (resultSceneToAddThisTextToTuple.Item1 != null)
                                        {
                                            if (resultSceneToAddThisTextToTuple.Item2 >= 0)
                                            {
                                                codeFileSceneToAddThisSpriteToSearch.Contents.Insert(resultSceneToAddThisTextToTuple.Item2 - 1, new CommentBlockBegin());
                                            }

                                            if (resultSceneToAddThisTextToTuple.Item3 >= 0)
                                            {
                                                codeFileSceneToAddThisSpriteToSearch.Contents.Insert(resultSceneToAddThisTextToTuple.Item3 + 2, new CommentBlockEnd());
                                            }

                                            break;
                                        }
                                    }
                                }

                                for (var j = i + 1; j < codeFile.Contents.Count; j++)
                                {
                                    var codeBlock2 = codeFile.Contents[j];

                                    if (codeBlock2.GetType() == typeof(WhitespaceCharacter) || codeBlock2.GetType() == typeof(EqualsOperator) || codeBlock2.GetType() == typeof(NewOperator))
                                    {
                                        continue;
                                    }

                                    if (codeBlock2.GetType() == typeof(BasicCodeToken))
                                    {
                                        if (((BasicCodeToken)codeBlock2).Value.ToLower() == Torque2dConstants.TextSpriteClassName.ToLower())
                                        {
                                            for (var k = j + 1; k < codeFile.Contents.Count; k++)
                                            {
                                                var codeBlock3 = codeFile.Contents[k];

                                                if (codeBlock3.GetType() == typeof(WhitespaceCharacter))
                                                {
                                                    continue;
                                                }

                                                if (codeBlock3.GetType() == typeof(OpenRoundBracket))
                                                {
                                                    if (resultSceneToAddThisTextToTuple.Item1 == null)
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new LocalVariable{ Name = "%undefinedOccurredDuringPhaserConversionForSceneToAddTo", PhaserObjectType = PhaserObjectType.None },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new StringValue { Val = "\"\"" },
                                                            new Comma(),
                                                            new OpenCurlyBracket(),
                                                            new BasicCodeToken { Value = "fontFamily" },
                                                            new Colon(),
                                                            resultFontTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForFont" } : resultFontTuple.Item1.DeepCopy(),
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "fontSize" },
                                                            new Colon(),
                                                            new NumericValue { NumberAsString = "32" }, // TODO: Create some sort of fontSize conversion formula (based on T2D FontSize property) for this line.  Consider adding code to MathConvertUtil and PhaserTextBaseClass
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "color" },
                                                            new Colon(),
                                                            new StringValue { Val = "\"#ff0000\"" }, // TODO: Still need to implement how to set Font color
                                                            new ClosedCurlyBracket(),
                                                        });
                                                    }
                                                    else if (resultSceneToAddThisTextToTuple.Item1.GetType() == typeof(LocalVariable))
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new LocalVariable{ Name = resultSceneToAddThisTextToTuple.Item1.Name, PhaserObjectType = resultSceneToAddThisTextToTuple.Item1.PhaserObjectType },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new StringValue { Val = "\"\"" },
                                                            new Comma(),
                                                            new OpenCurlyBracket(),
                                                            new BasicCodeToken { Value = "fontFamily" },
                                                            new Colon(),
                                                            resultFontTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForFont" } : resultFontTuple.Item1.DeepCopy(),
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "fontSize" },
                                                            new Colon(),
                                                            new NumericValue { NumberAsString = "32" }, // TODO: Create some sort of fontSize conversion formula (based on T2D FontSize property) for this line.  Consider adding code to MathConvertUtil and PhaserTextBaseClass
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "color" },
                                                            new Colon(),
                                                            new StringValue { Val = "\"#ff0000\"" }, // TODO: Still need to implement how to set Font color
                                                            new ClosedCurlyBracket(),
                                                        });
                                                    }
                                                    else if (resultSceneToAddThisTextToTuple.Item1.GetType() == typeof(GlobalVariable))
                                                    {
                                                        codeFile.Contents.InsertRange(k + 1, new List<CodeBlock>
                                                        {
                                                            new GlobalVariable{ Name = resultSceneToAddThisTextToTuple.Item1.Name, PhaserObjectType = resultSceneToAddThisTextToTuple.Item1.PhaserObjectType },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new NumericValue { NumberAsString = "0" },
                                                            new Comma(),
                                                            new StringValue { Val = "\"\"" },
                                                            new Comma(),
                                                            new OpenCurlyBracket(),
                                                            new BasicCodeToken { Value = "fontFamily" },
                                                            new Colon(),
                                                            resultFontTuple.Item1 == null ? new StringValue { Val = "undefinedOccurredDuringPhaserConversionForFont" } : resultFontTuple.Item1.DeepCopy(),
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "fontSize" },
                                                            new Colon(),
                                                            new NumericValue { NumberAsString = "32" }, // TODO: Create some sort of fontSize conversion formula (based on T2D FontSize property) for this line.  Consider adding code to MathConvertUtil and PhaserTextBaseClass
                                                            new Comma(),
                                                            new BasicCodeToken { Value = "color" },
                                                            new Colon(),
                                                            new StringValue { Val = "\"#ff0000\"" }, // TODO: Still need to implement how to set Font color
                                                            new ClosedCurlyBracket(),
                                                        });
                                                    }

                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                if (!globalVariablesCollection.Exists(gv => ((GlobalVariable)codeBlock).Name.ToLower() == gv.Name.ToLower()))
                                {
                                    globalVariablesCollection.Add((GlobalVariable)codeBlock);
                                }
                            }
                        }
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        public static void PerformUpconversionFromOlderTorqueObjects()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(BasicCodeToken))
                    {
                        // EX: convert t2dSceneObject to SceneObject.
                        // Later will also have a class PhaserSceneObject (that will probably extend a basic javascript Object)
                        if (((BasicCodeToken)codeBlock).Value.ToLower() == OldTorque2dConstants.SceneObjectClassName.ToLower())
                        {
                            ((BasicCodeToken)codeBlock).Value = Torque2dConstants.SceneObjectClassName;
                        }
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        public static void ProcessConvertingT2dFunctionCalls()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(BasicCodeToken))
                    {
                        if (((BasicCodeToken)codeBlock).Value.ToLower() == "getrandom")
                        {
                            var hasAtLeastOneParameter = false;
                            var hasAtLeastTwoParameters = false;

                            var openRoundBracketCount = 0;

                            for (var j = i + 1; j < codeFile.Contents.Count; j++)
                            {
                                var codeBlock2 = codeFile.Contents[j];

                                if (codeBlock2.GetType() == typeof(OpenRoundBracket))
                                {
                                    openRoundBracketCount++;
                                }
                                else if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                                {
                                    if (openRoundBracketCount > 1)
                                    {
                                        openRoundBracketCount--;
                                        continue;
                                    }

                                    ((BasicCodeToken)codeBlock).Value = "GeneralUtil";

                                    // conclude conversion
                                    if (hasAtLeastTwoParameters)
                                    {
                                        codeFile.Contents.InsertRange(i + 1, new List<CodeBlock>
                                        {
                                            new Dot(),
                                            new BasicCodeToken { Value = "t2dGetRandomMinMax" }
                                        });

                                        break;
                                    }
                                    else if (hasAtLeastOneParameter)
                                    {
                                        codeFile.Contents.InsertRange(i + 1, new List<CodeBlock>
                                        {
                                            new Dot(),
                                            new BasicCodeToken { Value = "t2dGetRandomMax" }
                                        });

                                        break;
                                    }
                                    else
                                    {
                                        codeFile.Contents.InsertRange(i + 1, new List<CodeBlock>
                                        {
                                            new Dot(),
                                            new BasicCodeToken { Value = "t2dGetRandom" }
                                        });

                                        break;
                                    }
                                }
                                else if (codeBlock2.GetType() == typeof(NumericValue))
                                {
                                    if (openRoundBracketCount < 2)
                                    {
                                        hasAtLeastOneParameter = true;
                                    }
                                }
                                else if (codeBlock2.GetType() == typeof(BasicCodeToken))
                                {
                                    if (openRoundBracketCount < 2)
                                    {
                                        hasAtLeastOneParameter = true;
                                    }
                                }
                                else if (codeBlock2.GetType() == typeof(Comma))
                                {
                                    if (openRoundBracketCount < 2)
                                    {
                                        hasAtLeastTwoParameters = true;
                                    }
                                }
                            }
                        }
                        else if (((BasicCodeToken)codeBlock).Value.ToLower() == "strlen")
                        {
                            var openRoundBracketCount = 0;

                            for (var j = i + 1; j < codeFile.Contents.Count; j++)
                            {
                                var codeBlock2 = codeFile.Contents[j];

                                if (codeBlock2.GetType() == typeof(OpenRoundBracket))
                                {
                                    openRoundBracketCount++;
                                }
                                else if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                                {
                                    if (openRoundBracketCount > 1)
                                    {
                                        openRoundBracketCount--;
                                        continue;
                                    }

                                    ((BasicCodeToken)codeBlock).Value = "GeneralUtil";

                                    // conclude conversion
                                    codeFile.Contents.InsertRange(i + 1, new List<CodeBlock>
                                    {
                                        new Dot(),
                                        new BasicCodeToken { Value = "t2dStrLen" }
                                    });

                                    break;
                                }
                            }
                        }
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        public static void ProcessAudioPlayFunctionCalls()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(BasicCodeToken))
                    {
                        if (((BasicCodeToken)codeBlock).Value.ToLower() == "alxplay")
                        {
                            // first, backtrack and see if there's a handle/variable associated with the alxplay function call
                            // keep a reference of the variable handy if there is
                            Variable musicHandleVariable = null;

                            bool foundWhiteSpaceOne = false;
                            bool foundWhiteSpaceTwo = false;
                            bool foundEqualsOp = false;

                            for (var j = i - 1; j >= 0; j--)
                            {
                                if (!foundWhiteSpaceOne)
                                {
                                    if (codeFile.Contents[j].GetType() == typeof(WhitespaceCharacter) ||
                                        codeFile.Contents[j].GetType() == typeof(NewLineCharacter))
                                    {
                                        foundWhiteSpaceOne = true;
                                    }
                                    continue;
                                }
                                else if (!foundEqualsOp)
                                {
                                    if (codeFile.Contents[j].GetType() == typeof(WhitespaceCharacter) ||
                                        codeFile.Contents[j].GetType() == typeof(NewLineCharacter))
                                    {
                                        // keep continuing; might be more than one whitespace/newling char
                                    }
                                    else if (codeFile.Contents[j].GetType() == typeof(EqualsOperator))
                                    {
                                        foundEqualsOp = true;
                                    }
                                    continue;
                                }
                                else if (!foundWhiteSpaceTwo)
                                {
                                    if (codeFile.Contents[j].GetType() == typeof(WhitespaceCharacter) ||
                                        codeFile.Contents[j].GetType() == typeof(NewLineCharacter))
                                    {
                                        foundWhiteSpaceTwo = true;
                                    }
                                    continue;
                                }
                                else
                                {
                                    if (codeFile.Contents[j].GetType() == typeof(WhitespaceCharacter) ||
                                        codeFile.Contents[j].GetType() == typeof(NewLineCharacter))
                                    {
                                        // keep continuing; might be more than one whitespace/newline char
                                        continue;
                                    }
                                    else if (codeFile.Contents[j].GetType() == typeof(GlobalVariable))
                                    {
                                        musicHandleVariable = (GlobalVariable)codeFile.Contents[j];
                                        break;
                                    }
                                    else if (codeFile.Contents[j].GetType() == typeof(LocalVariable))
                                    {
                                        musicHandleVariable = (LocalVariable)codeFile.Contents[j];
                                        break;
                                    }
                                    else
                                    {
                                        // if we run into any other type of codeBlock, then abort

                                        // TODO Handle when the musicHandle is assigned to something other than a Variable (ie like a BasicCodeToken)

                                        break;
                                    }
                                }
                            }

                            // will now proceed to adding CodeBlocks for the audio track being added/played
                            newCodeFile.Contents.AddRange(new List<CodeBlock>
                            {
                                new BasicCodeToken { Value = "this" },
                                new Dot(),
                                new BasicCodeToken { Value = "sound" },
                                new Dot(),
                                new BasicCodeToken { Value = "add" }
                            });

                            bool foundClosingBracket = false;
                            bool foundSemicolon = false;
                            bool foundWhitespaceCharacter = false;

                            bool isLooping = false;

                            for (var j = i + 1; j < codeFile.Contents.Count; j++)
                            {
                                var codeBlock2 = codeFile.Contents[j];

                                if (!foundClosingBracket && (codeBlock2.GetType() == typeof(Variable) || codeBlock2.GetType() == typeof(StringValue)))
                                {
                                    // found the asset key for the audio sound.  Will want to check to see if this sound asset is a 'looping' sound
                                    // if it is a looping sound, make note of it
                                    string assetName = null;

                                    if (codeBlock2.GetType() == typeof(Variable))
                                    {
                                        // TODO - broken; Need to actually get the 'value' assigned to the Variable.
                                        // will have to manually edit output project (Phaser output) for these cases...

                                        //assetName = ((Variable)codeBlock2).Name;
                                    }
                                    else if (codeBlock2.GetType() == typeof(StringValue))
                                    {
                                        assetName = ((StringValue)codeBlock2).Val.Substring(1, ((StringValue)codeBlock2).Val.Length - 2);
                                    }

                                    var audioAsset = (Torque2dAudioAsset)GlobalVars.Torque2dModuleDatabase.Torque2dAssetList.FirstOrDefault(asset => asset.Name == assetName);

                                    if (audioAsset != null)
                                    {
                                        isLooping = audioAsset.Looping.HasValue ? audioAsset.Looping.Value : false;
                                    }

                                    newCodeFile.Contents.Add(codeBlock2);
                                    continue;
                                }
                                else if (!foundClosingBracket)
                                {
                                    if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                                    {
                                        foundClosingBracket = true;
                                    }

                                    newCodeFile.Contents.Add(codeBlock2);

                                    if (foundClosingBracket)
                                    {
                                        if (musicHandleVariable == null)
                                        {
                                            // if 'musicHandleVariable' is null, then we don't need a reference to it.  In this case, we will call the 'setLoop' (if needed) and 'play' functions inline with the sound add function call

                                            if (isLooping)
                                            {
                                                newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                {
                                                    new Dot(),
                                                    new BasicCodeToken { Value = "setLoop" },
                                                    new OpenRoundBracket(),
                                                    new BooleanTrueValue(),
                                                    new ClosedRoundBracket()
                                                });
                                            }

                                            newCodeFile.Contents.AddRange(new List<CodeBlock>
                                            {
                                                new Dot(),
                                                new BasicCodeToken { Value = "play" },
                                                new OpenRoundBracket(),
                                                new ClosedRoundBracket()
                                            });

                                            i = j;
                                            break;
                                        }
                                    }

                                    continue;
                                }
                                else if (!foundSemicolon)
                                {
                                    if (codeBlock2.GetType() == typeof(Semicolon))
                                    {
                                        foundSemicolon = true;
                                    }

                                    newCodeFile.Contents.Add(codeBlock2);
                                    continue;
                                }
                                else if (!foundWhitespaceCharacter)
                                {
                                    if (codeFile.Contents[j].GetType() == typeof(WhitespaceCharacter) ||
                                        codeFile.Contents[j].GetType() == typeof(NewLineCharacter))
                                    {
                                        foundWhitespaceCharacter = true;
                                    }

                                    newCodeFile.Contents.Add(codeBlock2);
                                    continue;
                                }
                                else
                                {
                                    // will now proceed to adding more code to now 'play' the audio track, and also apply setLoop(true) if needed

                                    // will do setLoop first
                                    if (isLooping)
                                    {
                                        if (musicHandleVariable.GetType() == typeof(GlobalVariable))
                                        {
                                            newCodeFile.Contents.AddRange(new List<CodeBlock>
                                            {
                                                new NewLineCharacter(),
                                                (GlobalVariable)musicHandleVariable.DeepCopy(),
                                                new Dot(),
                                                new BasicCodeToken { Value = "setLoop" },
                                                new OpenRoundBracket(),
                                                new BooleanTrueValue(),
                                                new ClosedRoundBracket(),
                                                new Semicolon()
                                            });
                                        }
                                        else // musicHandleVariable.GetType() == typeof(LocalVariable)
                                        {
                                            newCodeFile.Contents.AddRange(new List<CodeBlock>
                                            {
                                                new NewLineCharacter(),
                                                (LocalVariable)musicHandleVariable.DeepCopy(),
                                                new Dot(),
                                                new BasicCodeToken { Value = "setLoop" },
                                                new OpenRoundBracket(),
                                                new BooleanTrueValue(),
                                                new ClosedRoundBracket(),
                                                new Semicolon()
                                            });
                                        }
                                    }


                                    // and now 'play' the audio track
                                    if (musicHandleVariable.GetType() == typeof(GlobalVariable))
                                    {
                                        newCodeFile.Contents.AddRange(new List<CodeBlock>
                                        {
                                            new NewLineCharacter(),
                                            (GlobalVariable)musicHandleVariable.DeepCopy(),
                                            new Dot(),
                                            new BasicCodeToken { Value = "play" },
                                            new OpenRoundBracket(),
                                            new ClosedRoundBracket(),
                                            new Semicolon()
                                        });
                                    }
                                    else // musicHandleVariable.GetType() == typeof(LocalVariable)
                                    {
                                        newCodeFile.Contents.AddRange(new List<CodeBlock>
                                        {
                                            new NewLineCharacter(),
                                            (LocalVariable)musicHandleVariable.DeepCopy(),
                                            new Dot(),
                                            new BasicCodeToken { Value = "play" },
                                            new OpenRoundBracket(),
                                            new ClosedRoundBracket(),
                                            new Semicolon()
                                        });
                                    }

                                    i = j - 1;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            newCodeFile.Contents.Add(codeBlock);
                        }
                    }
                    //
                    // 2021-09-09 > TODO other sound functions ie alxStop
                    //var introTrack = this.sound.add(); DONE
                    // introTrack.setLoop(true);   (? should work) DONE
                    // introTrack.stop();   (? should work)
                    //
                    else
                    {
                        newCodeFile.Contents.Add(codeBlock);
                    }
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        public static void ProcessAudioStopFunctionCalls()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(BasicCodeToken))
                    {
                        if (((BasicCodeToken)codeBlock).Value.ToLower() == "alxstop")
                        {
                            Variable musicHandleVariable = null;

                            for (var j = i + 1; j < codeFile.Contents.Count; j++)
                            {
                                var codeBlock2 = codeFile.Contents[j];

                                if (codeBlock2.GetType() == typeof(GlobalVariable))
                                {
                                    musicHandleVariable = (GlobalVariable)codeFile.Contents[j];
                                    continue;
                                }
                                else if (codeBlock2.GetType() == typeof(LocalVariable))
                                {
                                    musicHandleVariable = (LocalVariable)codeFile.Contents[j];
                                    continue;
                                }
                                else if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                                {
                                    if (musicHandleVariable.GetType() == typeof(GlobalVariable))
                                    {
                                        newCodeFile.Contents.AddRange(new List<CodeBlock>
                                            {
                                                (GlobalVariable)musicHandleVariable.DeepCopy(),
                                                new Dot(),
                                                new BasicCodeToken { Value = "stop" },
                                                new OpenRoundBracket(),
                                                new ClosedRoundBracket(),
                                            });
                                    }
                                    else // musicHandleVariable.GetType() == typeof(LocalVariable)
                                    {
                                        newCodeFile.Contents.AddRange(new List<CodeBlock>
                                            {
                                                (LocalVariable)musicHandleVariable.DeepCopy(),
                                                new Dot(),
                                                new BasicCodeToken { Value = "stop" },
                                                new OpenRoundBracket(),
                                                new ClosedRoundBracket(),
                                            });
                                    }

                                    i = j;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            newCodeFile.Contents.Add(codeBlock);
                        }
                    }
                    else
                    {
                        newCodeFile.Contents.Add(codeBlock);
                    }
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        public static void ProcessScheduleCalls()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(BasicCodeToken))
                    {
                        if (((BasicCodeToken)codeBlock).Value.ToLower() == "schedule")
                        {
                            List<CodeBlock> identifierBlob = new List<CodeBlock>();

                            // will first collect any preceding codeBlocks (ie form the 'identifier blob') if there is one
                            for (var j = i - 1; j >= 0; j--)
                            {
                                var codeBlock2 = codeFile.Contents[j];

                                if (codeBlock2.GetType() == typeof(WhitespaceCharacter) || codeBlock2.GetType() == typeof(NewLineCharacter) || codeBlock2.GetType() == typeof(OpenCurlyBracket))
                                {
                                    break;
                                }

                                identifierBlob.Add(codeBlock2);
                                newCodeFile.Contents.RemoveAt(newCodeFile.Contents.Count - 1);
                            }

                            if (identifierBlob.Count < 1)
                            {
                                int timeDelay = 0;
                                string functionName = null;
                                var parameterList = new List<CodeBlock>();

                                bool foundOpenRoundBacket = false;
                                bool foundTimeDelay = false;
                                bool foundFirstComma = false;
                                // note - will be ignoring the 2nd parameter to the schedule function
                                bool foundSecondComma = false;
                                bool foundFunctionNameAsString = false;
                                bool foundParametersComma = false;
                                bool foundClosedRoundBracket = false;
                                bool foundSemicolon = false;

                                for (var j = i + 1; j < codeFile.Contents.Count; j++)
                                {
                                    var codeBlock2 = codeFile.Contents[j];

                                    if (!foundOpenRoundBacket)
                                    {
                                        if (codeBlock2.GetType() == typeof(OpenRoundBracket))
                                        {
                                            foundOpenRoundBacket = true;
                                        }

                                        continue;
                                    }
                                    else if (!foundTimeDelay)
                                    {
                                        if (codeBlock2.GetType() == typeof(NumericValue))
                                        {
                                            timeDelay = System.Convert.ToInt32(((NumericValue)codeBlock2).NumberAsString);
                                            foundTimeDelay = true;
                                        }

                                        continue;
                                    }
                                    else if (!foundFirstComma)
                                    {
                                        if (codeBlock2.GetType() == typeof(Comma))
                                        {
                                            foundFirstComma = true;
                                        }

                                        continue;
                                    }
                                    else if (!foundSecondComma)
                                    {
                                        if (codeBlock2.GetType() == typeof(Comma))
                                        {
                                            foundSecondComma = true;
                                        }

                                        continue;
                                    }
                                    else if (!foundFunctionNameAsString)
                                    {
                                        if (codeBlock2.GetType() == typeof(StringValue))
                                        {
                                            functionName = ((StringValue)codeBlock2).Val.Substring(1, ((StringValue)codeBlock2).Val.Length - 2);
                                            foundFunctionNameAsString = true;
                                        }

                                        continue;
                                    }
                                    else if (!foundParametersComma && !foundClosedRoundBracket)
                                    {
                                        if (codeBlock2.GetType() == typeof(Comma))
                                        {
                                            foundParametersComma = true;
                                        }
                                        else if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                                        {
                                            foundClosedRoundBracket = true;
                                        }

                                        continue;
                                    }
                                    else if (!foundClosedRoundBracket)
                                    {
                                        if (codeBlock2.GetType() == typeof(BooleanFalseValue))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((BooleanFalseValue)codeBlock2);
                                            foundParametersComma = false;
                                        }
                                        else if (codeBlock2.GetType() == typeof(BooleanTrueValue))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((BooleanTrueValue)codeBlock2);
                                            foundParametersComma = false;
                                        }
                                        else if (codeBlock2.GetType() == typeof(GlobalVariable))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((GlobalVariable)codeBlock2);
                                            foundParametersComma = false;
                                        }
                                        else if (codeBlock2.GetType() == typeof(LocalVariable))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((LocalVariable)codeBlock2);
                                            foundParametersComma = false;
                                        }
                                        else if (codeBlock2.GetType() == typeof(NumericValue))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((NumericValue)codeBlock2);
                                            foundParametersComma = false;
                                        }
                                        else if (codeBlock2.GetType() == typeof(StringValue))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((StringValue)codeBlock2);
                                            foundParametersComma = false;
                                        }

                                        continue;
                                    }
                                    else if (!foundSemicolon)
                                    {
                                        if (codeBlock2.GetType() == typeof(Semicolon))
                                        {
                                            // will now add the 'setTimeout' function call to our newCodeFile
                                            newCodeFile.Contents.AddRange(new List<CodeBlock>
                                            {
                                                new BasicCodeToken { Value = "setTimeout" },
                                                new OpenRoundBracket(),
                                                new BasicCodeToken { Value = "function" },
                                                new OpenRoundBracket(),
                                                new ClosedRoundBracket(),
                                                new OpenCurlyBracket(),
                                                new WhitespaceCharacter { WhitespaceChar = ' ' },
                                                new BasicCodeToken { Value = functionName },
                                                new OpenRoundBracket()
                                            });

                                            bool atLeastOneParameter = false;

                                            foreach (var parameter in parameterList)
                                            {
                                                atLeastOneParameter = true;

                                                if (parameter.GetType() == typeof(BooleanFalseValue))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (BooleanFalseValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                                else if (parameter.GetType() == typeof(BooleanTrueValue))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (BooleanTrueValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                                else if (parameter.GetType() == typeof(GlobalVariable))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (GlobalVariable)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                                else if (parameter.GetType() == typeof(LocalVariable))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (LocalVariable)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                                else if (parameter.GetType() == typeof(NumericValue))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (NumericValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                                else if (parameter.GetType() == typeof(StringValue))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (StringValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                            }

                                            if (atLeastOneParameter)
                                            {
                                                newCodeFile.Contents.RemoveAt(newCodeFile.Contents.Count - 1);
                                            }

                                            newCodeFile.Contents.AddRange(new List<CodeBlock>
                                            {
                                                new ClosedRoundBracket(),
                                                new Semicolon(),
                                                new WhitespaceCharacter { WhitespaceChar = ' ' },
                                                new ClosedCurlyBracket(),
                                                new Comma(),
                                                new NumericValue { NumberAsString = timeDelay.ToString() },
                                                new ClosedRoundBracket(),
                                                new Semicolon()
                                            });

                                            i = j;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // in this block if 'identifierBlob.Count > 0'

                                int timeDelay = 0;
                                string methodName = null;
                                var parameterList = new List<CodeBlock>();

                                bool foundOpenRoundBacket = false;
                                bool foundTimeDelay = false;
                                bool foundFirstComma = false;
                                bool foundMethodNameAsString = false;
                                bool foundParametersComma = false;
                                bool foundClosedRoundBracket = false;
                                bool foundSemicolon = false;

                                for (var j = i + 1; j < codeFile.Contents.Count; j++)
                                {
                                    var codeBlock2 = codeFile.Contents[j];

                                    if (!foundOpenRoundBacket)
                                    {
                                        if (codeBlock2.GetType() == typeof(OpenRoundBracket))
                                        {
                                            foundOpenRoundBacket = true;
                                        }

                                        continue;
                                    }
                                    else if (!foundTimeDelay)
                                    {
                                        if (codeBlock2.GetType() == typeof(NumericValue))
                                        {
                                            timeDelay = System.Convert.ToInt32(((NumericValue)codeBlock2).NumberAsString);
                                            foundTimeDelay = true;
                                        }

                                        continue;
                                    }
                                    else if (!foundFirstComma)
                                    {
                                        if (codeBlock2.GetType() == typeof(Comma))
                                        {
                                            foundFirstComma = true;
                                        }

                                        continue;
                                    }
                                    else if (!foundMethodNameAsString)
                                    {
                                        if (codeBlock2.GetType() == typeof(StringValue))
                                        {
                                            methodName = ((StringValue)codeBlock2).Val.Substring(1, ((StringValue)codeBlock2).Val.Length - 2);
                                            foundMethodNameAsString = true;
                                        }

                                        continue;
                                    }
                                    else if (!foundParametersComma && !foundClosedRoundBracket)
                                    {
                                        if (codeBlock2.GetType() == typeof(Comma))
                                        {
                                            foundParametersComma = true;
                                        }
                                        else if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                                        {
                                            foundClosedRoundBracket = true;
                                        }

                                        continue;
                                    }
                                    else if (!foundClosedRoundBracket)
                                    {
                                        if (codeBlock2.GetType() == typeof(BooleanFalseValue))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((BooleanFalseValue)codeBlock2);
                                            foundParametersComma = false;
                                        }
                                        else if (codeBlock2.GetType() == typeof(BooleanTrueValue))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((BooleanTrueValue)codeBlock2);
                                            foundParametersComma = false;
                                        }
                                        else if (codeBlock2.GetType() == typeof(GlobalVariable))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((GlobalVariable)codeBlock2);
                                            foundParametersComma = false;
                                        }
                                        else if (codeBlock2.GetType() == typeof(LocalVariable))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((LocalVariable)codeBlock2);
                                            foundParametersComma = false;
                                        }
                                        else if (codeBlock2.GetType() == typeof(NumericValue))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((NumericValue)codeBlock2);
                                            foundParametersComma = false;
                                        }
                                        else if (codeBlock2.GetType() == typeof(StringValue))
                                        {
                                            // found a parameter; store in the parameterList
                                            parameterList.Add((StringValue)codeBlock2);
                                            foundParametersComma = false;
                                        }

                                        continue;
                                    }
                                    else if (!foundSemicolon)
                                    {
                                        if (codeBlock2.GetType() == typeof(Semicolon))
                                        {
                                            // will now add the 'setTimeout' method call to our newCodeFile
                                            newCodeFile.Contents.AddRange(new List<CodeBlock>
                                            {
                                                new BasicCodeToken { Value = "setTimeout" },
                                                new OpenRoundBracket(),
                                                new BasicCodeToken { Value = "function" },
                                                new OpenRoundBracket(),
                                                new ClosedRoundBracket(),
                                                new OpenCurlyBracket(),
                                                new WhitespaceCharacter { WhitespaceChar = ' ' }
                                            });

                                            identifierBlob.Reverse();
                                            newCodeFile.Contents.AddRange(identifierBlob);

                                            newCodeFile.Contents.AddRange(new List<CodeBlock>
                                            {
                                                new BasicCodeToken { Value = methodName },
                                                new OpenRoundBracket()
                                            });

                                            bool atLeastOneParameter = false;

                                            foreach (var parameter in parameterList)
                                            {
                                                atLeastOneParameter = true;

                                                if (parameter.GetType() == typeof(BooleanFalseValue))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (BooleanFalseValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                                else if (parameter.GetType() == typeof(BooleanTrueValue))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (BooleanTrueValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                                else if (parameter.GetType() == typeof(GlobalVariable))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (GlobalVariable)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                                else if (parameter.GetType() == typeof(LocalVariable))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (LocalVariable)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                                else if (parameter.GetType() == typeof(NumericValue))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (NumericValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                                else if (parameter.GetType() == typeof(StringValue))
                                                {
                                                    newCodeFile.Contents.AddRange(new List<CodeBlock>
                                                        {
                                                            (StringValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                }
                                            }

                                            if (atLeastOneParameter)
                                            {
                                                newCodeFile.Contents.RemoveAt(newCodeFile.Contents.Count - 1);
                                            }

                                            newCodeFile.Contents.AddRange(new List<CodeBlock>
                                            {
                                                new ClosedRoundBracket(),
                                                new Semicolon(),
                                                new WhitespaceCharacter { WhitespaceChar = ' ' },
                                                new ClosedCurlyBracket(),
                                                new Comma(),
                                                new NumericValue { NumberAsString = timeDelay.ToString() },
                                                new ClosedRoundBracket(),
                                                new Semicolon()
                                            });

                                            i = j;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            newCodeFile.Contents.Add(codeBlock);
                        }
                    }
                    else
                    {
                        newCodeFile.Contents.Add(codeBlock);
                    }
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        public static void ProcessScheduleCallsInPhaserClasses()
        {
            var newPhaserClassList = new List<PhaserClass>();

            foreach (var phaserClass in GlobalVars.PhaserCodeRepo.PhaserClassList)
            {
                var newPhaserClass = new PhaserClass
                {
                    ClassName = phaserClass.ClassName,
                    ClassMethods = new List<Tuple<string, List<string>, List<CodeBlock>>>()
                };

                foreach (var classMethod in phaserClass.ClassMethods)
                {
                    var newClassMethodContents = new List<CodeBlock>();

                    for (var i = 0; i < classMethod.Item3.Count; i++)
                    {
                        var codeBlock = classMethod.Item3[i];

                        if (codeBlock.GetType() == typeof(BasicCodeToken))
                        {
                            if (((BasicCodeToken)codeBlock).Value.ToLower() == "schedule")
                            {
                                List<CodeBlock> identifierBlob = new List<CodeBlock>();

                                // will first collect any preceding codeBlocks (ie form the 'identifier blob') if there is one
                                for (var j = i - 1; j >= 0; j--)
                                {
                                    var codeBlock2 = classMethod.Item3[j];

                                    if (codeBlock2.GetType() == typeof(WhitespaceCharacter) || codeBlock2.GetType() == typeof(NewLineCharacter) || codeBlock2.GetType() == typeof(OpenCurlyBracket))
                                    {
                                        break;
                                    }

                                    identifierBlob.Add(codeBlock2);
                                    newClassMethodContents.RemoveAt(newClassMethodContents.Count - 1);
                                }

                                if (identifierBlob.Count < 1)
                                {
                                    int timeDelay = 0;
                                    string functionName = null;
                                    var parameterList = new List<CodeBlock>();

                                    bool foundOpenRoundBacket = false;
                                    bool foundTimeDelay = false;
                                    bool foundFirstComma = false;
                                    // note - will be ignoring the 2nd parameter to the schedule function
                                    bool foundSecondComma = false;
                                    bool foundFunctionNameAsString = false;
                                    bool foundParametersComma = false;
                                    bool foundClosedRoundBracket = false;
                                    bool foundSemicolon = false;

                                    for (var j = i + 1; j < classMethod.Item3.Count; j++)
                                    {
                                        var codeBlock2 = classMethod.Item3[j];

                                        if (!foundOpenRoundBacket)
                                        {
                                            if (codeBlock2.GetType() == typeof(OpenRoundBracket))
                                            {
                                                foundOpenRoundBacket = true;
                                            }

                                            continue;
                                        }
                                        else if (!foundTimeDelay)
                                        {
                                            if (codeBlock2.GetType() == typeof(NumericValue))
                                            {
                                                timeDelay = System.Convert.ToInt32(((NumericValue)codeBlock2).NumberAsString);
                                                foundTimeDelay = true;
                                            }

                                            continue;
                                        }
                                        else if (!foundFirstComma)
                                        {
                                            if (codeBlock2.GetType() == typeof(Comma))
                                            {
                                                foundFirstComma = true;
                                            }

                                            continue;
                                        }
                                        else if (!foundSecondComma)
                                        {
                                            if (codeBlock2.GetType() == typeof(Comma))
                                            {
                                                foundSecondComma = true;
                                            }

                                            continue;
                                        }
                                        else if (!foundFunctionNameAsString)
                                        {
                                            if (codeBlock2.GetType() == typeof(StringValue))
                                            {
                                                functionName = ((StringValue)codeBlock2).Val.Substring(1, ((StringValue)codeBlock2).Val.Length - 2);
                                                foundFunctionNameAsString = true;
                                            }

                                            continue;
                                        }
                                        else if (!foundParametersComma && !foundClosedRoundBracket)
                                        {
                                            if (codeBlock2.GetType() == typeof(Comma))
                                            {
                                                foundParametersComma = true;
                                            }
                                            else if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                                            {
                                                foundClosedRoundBracket = true;
                                            }

                                            continue;
                                        }
                                        else if (!foundClosedRoundBracket)
                                        {
                                            if (codeBlock2.GetType() == typeof(BooleanFalseValue))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((BooleanFalseValue)codeBlock2);
                                                foundParametersComma = false;
                                            }
                                            else if (codeBlock2.GetType() == typeof(BooleanTrueValue))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((BooleanTrueValue)codeBlock2);
                                                foundParametersComma = false;
                                            }
                                            else if (codeBlock2.GetType() == typeof(GlobalVariable))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((GlobalVariable)codeBlock2);
                                                foundParametersComma = false;
                                            }
                                            else if (codeBlock2.GetType() == typeof(LocalVariable))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((LocalVariable)codeBlock2);
                                                foundParametersComma = false;
                                            }
                                            else if (codeBlock2.GetType() == typeof(NumericValue))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((NumericValue)codeBlock2);
                                                foundParametersComma = false;
                                            }
                                            else if (codeBlock2.GetType() == typeof(StringValue))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((StringValue)codeBlock2);
                                                foundParametersComma = false;
                                            }

                                            continue;
                                        }
                                        else if (!foundSemicolon)
                                        {
                                            if (codeBlock2.GetType() == typeof(Semicolon))
                                            {
                                                // will now add the 'setTimeout' function call to our newCodeFile
                                                newClassMethodContents.AddRange(new List<CodeBlock>
                                                {
                                                    new BasicCodeToken { Value = "setTimeout" },
                                                    new OpenRoundBracket(),
                                                    new BasicCodeToken { Value = "function" },
                                                    new OpenRoundBracket(),
                                                    new ClosedRoundBracket(),
                                                    new OpenCurlyBracket(),
                                                    new WhitespaceCharacter { WhitespaceChar = ' ' },
                                                    new BasicCodeToken { Value = functionName },
                                                    new OpenRoundBracket()
                                                });

                                                bool atLeastOneParameter = false;

                                                foreach (var parameter in parameterList)
                                                {
                                                    atLeastOneParameter = true;

                                                    if (parameter.GetType() == typeof(BooleanFalseValue))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (BooleanFalseValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                    else if (parameter.GetType() == typeof(BooleanTrueValue))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (BooleanTrueValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                    else if (parameter.GetType() == typeof(GlobalVariable))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (GlobalVariable)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                    else if (parameter.GetType() == typeof(LocalVariable))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (LocalVariable)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                    else if (parameter.GetType() == typeof(NumericValue))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (NumericValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                    else if (parameter.GetType() == typeof(StringValue))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (StringValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                }

                                                if (atLeastOneParameter)
                                                {
                                                    newClassMethodContents.RemoveAt(newClassMethodContents.Count - 1);
                                                }

                                                newClassMethodContents.AddRange(new List<CodeBlock>
                                                {
                                                    new ClosedRoundBracket(),
                                                    new Semicolon(),
                                                    new WhitespaceCharacter { WhitespaceChar = ' ' },
                                                    new ClosedCurlyBracket(),
                                                    new Comma(),
                                                    new NumericValue { NumberAsString = timeDelay.ToString() },
                                                    new ClosedRoundBracket(),
                                                    new Semicolon()
                                                });

                                                i = j;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // in this block if 'identifierBlob.Count > 0'

                                    int timeDelay = 0;
                                    string methodName = null;
                                    var parameterList = new List<CodeBlock>();

                                    bool foundOpenRoundBacket = false;
                                    bool foundTimeDelay = false;
                                    bool foundFirstComma = false;
                                    bool foundMethodNameAsString = false;
                                    bool foundParametersComma = false;
                                    bool foundClosedRoundBracket = false;
                                    bool foundSemicolon = false;

                                    for (var j = i + 1; j < classMethod.Item3.Count; j++)
                                    {
                                        var codeBlock2 = classMethod.Item3[j];

                                        if (!foundOpenRoundBacket)
                                        {
                                            if (codeBlock2.GetType() == typeof(OpenRoundBracket))
                                            {
                                                foundOpenRoundBacket = true;
                                            }

                                            continue;
                                        }
                                        else if (!foundTimeDelay)
                                        {
                                            if (codeBlock2.GetType() == typeof(NumericValue))
                                            {
                                                timeDelay = System.Convert.ToInt32(((NumericValue)codeBlock2).NumberAsString);
                                                foundTimeDelay = true;
                                            }

                                            continue;
                                        }
                                        else if (!foundFirstComma)
                                        {
                                            if (codeBlock2.GetType() == typeof(Comma))
                                            {
                                                foundFirstComma = true;
                                            }

                                            continue;
                                        }
                                        else if (!foundMethodNameAsString)
                                        {
                                            if (codeBlock2.GetType() == typeof(StringValue))
                                            {
                                                methodName = ((StringValue)codeBlock2).Val.Substring(1, ((StringValue)codeBlock2).Val.Length - 2);
                                                foundMethodNameAsString = true;
                                            }

                                            continue;
                                        }
                                        else if (!foundParametersComma && !foundClosedRoundBracket)
                                        {
                                            if (codeBlock2.GetType() == typeof(Comma))
                                            {
                                                foundParametersComma = true;
                                            }
                                            else if (codeBlock2.GetType() == typeof(ClosedRoundBracket))
                                            {
                                                foundClosedRoundBracket = true;
                                            }

                                            continue;
                                        }
                                        else if (!foundClosedRoundBracket)
                                        {
                                            if (codeBlock2.GetType() == typeof(BooleanFalseValue))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((BooleanFalseValue)codeBlock2);
                                                foundParametersComma = false;
                                            }
                                            else if (codeBlock2.GetType() == typeof(BooleanTrueValue))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((BooleanTrueValue)codeBlock2);
                                                foundParametersComma = false;
                                            }
                                            else if (codeBlock2.GetType() == typeof(GlobalVariable))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((GlobalVariable)codeBlock2);
                                                foundParametersComma = false;
                                            }
                                            else if (codeBlock2.GetType() == typeof(LocalVariable))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((LocalVariable)codeBlock2);
                                                foundParametersComma = false;
                                            }
                                            else if (codeBlock2.GetType() == typeof(NumericValue))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((NumericValue)codeBlock2);
                                                foundParametersComma = false;
                                            }
                                            else if (codeBlock2.GetType() == typeof(StringValue))
                                            {
                                                // found a parameter; store in the parameterList
                                                parameterList.Add((StringValue)codeBlock2);
                                                foundParametersComma = false;
                                            }

                                            continue;
                                        }
                                        else if (!foundSemicolon)
                                        {
                                            if (codeBlock2.GetType() == typeof(Semicolon))
                                            {
                                                // will now add the 'setTimeout' method call to our newCodeFile
                                                newClassMethodContents.AddRange(new List<CodeBlock>
                                                {
                                                    new BasicCodeToken { Value = "setTimeout" },
                                                    new OpenRoundBracket(),
                                                    new BasicCodeToken { Value = "function" },
                                                    new OpenRoundBracket(),
                                                    new ClosedRoundBracket(),
                                                    new OpenCurlyBracket(),
                                                    new WhitespaceCharacter { WhitespaceChar = ' ' }
                                                });

                                                identifierBlob.Reverse();
                                                newClassMethodContents.AddRange(identifierBlob);

                                                newClassMethodContents.AddRange(new List<CodeBlock>
                                                {
                                                    new BasicCodeToken { Value = methodName },
                                                    new OpenRoundBracket()
                                                });

                                                bool atLeastOneParameter = false;

                                                foreach (var parameter in parameterList)
                                                {
                                                    atLeastOneParameter = true;

                                                    if (parameter.GetType() == typeof(BooleanFalseValue))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (BooleanFalseValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                    else if (parameter.GetType() == typeof(BooleanTrueValue))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (BooleanTrueValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                    else if (parameter.GetType() == typeof(GlobalVariable))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (GlobalVariable)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                    else if (parameter.GetType() == typeof(LocalVariable))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (LocalVariable)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                    else if (parameter.GetType() == typeof(NumericValue))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (NumericValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                    else if (parameter.GetType() == typeof(StringValue))
                                                    {
                                                        newClassMethodContents.AddRange(new List<CodeBlock>
                                                        {
                                                            (StringValue)parameter.DeepCopy(),
                                                            new Comma()
                                                        });
                                                    }
                                                }

                                                if (atLeastOneParameter)
                                                {
                                                    newClassMethodContents.RemoveAt(newClassMethodContents.Count - 1);
                                                }

                                                newClassMethodContents.AddRange(new List<CodeBlock>
                                                {
                                                    new ClosedRoundBracket(),
                                                    new Semicolon(),
                                                    new WhitespaceCharacter { WhitespaceChar = ' ' },
                                                    new ClosedCurlyBracket(),
                                                    new Comma(),
                                                    new NumericValue { NumberAsString = timeDelay.ToString() },
                                                    new ClosedRoundBracket(),
                                                    new Semicolon()
                                                });

                                                i = j;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                newClassMethodContents.Add(codeBlock);
                            }
                        }
                        else
                        {
                            newClassMethodContents.Add(codeBlock);
                        }
                    }

                    newPhaserClass.ClassMethods.Add(
                        Tuple.Create(classMethod.Item1,
                            classMethod.Item2,
                            newClassMethodContents
                        )
                    );
                }

                newPhaserClassList.Add(newPhaserClass);
            }

            GlobalVars.PhaserCodeRepo.PhaserClassList = newPhaserClassList;
        }
        
        public static void ProcessSetFontSizeMethodUsageForPhaserTexts()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(Dot))
                    {
                        newCodeFile.Contents.Add(codeBlock);

                        i++;

                        var codeBlock2 = codeFile.Contents[i];

                        if (codeBlock2.GetType() == typeof(BasicCodeToken))
                        {
                            if (((BasicCodeToken)codeBlock2).Value.ToLower() == "fontsize")
                            {
                                bool foundEqualsSign = false;

                                for (var j = i + 1; j < codeFile.Contents.Count; j++)
                                {
                                    var codeBlock3 = codeFile.Contents[j];

                                    if (!foundEqualsSign)
                                    {
                                        if (codeBlock3.GetType() == typeof(EqualsOperator))
                                        {
                                            foundEqualsSign = true;
                                        }

                                        continue;
                                    }
                                    else
                                    {
                                        if (codeBlock3.GetType() == typeof(NumericValue))
                                        {
                                            newCodeFile.Contents.AddRange(new List<CodeBlock>
                                            {
                                                new BasicCodeToken { Value = "setFontSize" },
                                                new OpenRoundBracket(),
                                                new NumericValue { NumberAsString = ((NumericValue)codeBlock3).NumberAsString },
                                                new ClosedRoundBracket()
                                            });

                                            i = j;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                newCodeFile.Contents.Add(codeBlock2);
                            }
                        }
                        else
                        {
                            newCodeFile.Contents.Add(codeBlock2);
                        }
                    }
                    else
                    {
                        newCodeFile.Contents.Add(codeBlock);
                    }
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }

        public static void PerformConversionForT2dFunctions()
        {
            var newCodeFileList = new List<CodeFile>();

            foreach (var codeFile in GlobalVars.Torque2dModuleDatabase.CodeFileList)
            {
                var newCodeFile = new CodeFile
                {
                    Filename = codeFile.Filename,
                    Contents = new List<CodeBlock>()
                };

                for (var i = 0; i < codeFile.Contents.Count; i++)
                {
                    var codeBlock = codeFile.Contents[i];

                    if (codeBlock.GetType() == typeof(BasicCodeToken))
                    {
                        if (((BasicCodeToken)codeBlock).Value.ToLower() == "getword" ||
                            ((BasicCodeToken)codeBlock).Value.ToLower() == "vectdist" ||
                            ((BasicCodeToken)codeBlock).Value.ToLower() == "vectornormalize" ||
                            ((BasicCodeToken)codeBlock).Value.ToLower() == "vectorscale" ||
                            ((BasicCodeToken)codeBlock).Value.ToLower() == "vectorsub" ||
                            ((BasicCodeToken)codeBlock).Value.ToLower() == "msin" ||
                            ((BasicCodeToken)codeBlock).Value.ToLower() == "mabs" ||
                            ((BasicCodeToken)codeBlock).Value.ToLower() == "matan" ||
                            ((BasicCodeToken)codeBlock).Value.ToLower() == "mfloor" ||
                            ((BasicCodeToken)codeBlock).Value.ToLower() == "mceil")
                        {
                            newCodeFile.Contents.Add(new BasicCodeToken { Value = "T2dFunctionsUtil" });
                            newCodeFile.Contents.Add(new Dot());
                        }
                        else if (((BasicCodeToken)codeBlock).Value.ToLower() == "mRadToDeg")
                        {
                            newCodeFile.Contents.Add(new BasicCodeToken { Value = "JavascriptUtil" });
                            newCodeFile.Contents.Add(new Dot());

                            ((BasicCodeToken)codeBlock).Value = "radToDeg";
                        }
                        else if (((BasicCodeToken)codeBlock).Value.ToLower() == "mDegToRad")
                        {
                            newCodeFile.Contents.Add(new BasicCodeToken { Value = "JavascriptUtil" });
                            newCodeFile.Contents.Add(new Dot());

                            ((BasicCodeToken)codeBlock).Value = "degToRad";
                        }
                    }

                    newCodeFile.Contents.Add(codeBlock);
                }

                newCodeFileList.Add(newCodeFile);
            }

            GlobalVars.Torque2dModuleDatabase.CodeFileList = newCodeFileList;
        }
    }
}
