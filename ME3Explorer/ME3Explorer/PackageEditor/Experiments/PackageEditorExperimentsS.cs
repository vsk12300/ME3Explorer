﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ME3Explorer.SharedUI;
using ME3Explorer.StaticLighting;
using ME3Explorer.Unreal.Classes;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Gammtek.IO;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;
using ME3ExplorerCore.Unreal.Classes;
using ME3ExplorerCore.UnrealScript;
using ME3ExplorerCore.UnrealScript.Compiling;
using ME3ExplorerCore.UnrealScript.Compiling.Errors;
using ME3ExplorerCore.UnrealScript.Decompiling;
using ME3ExplorerCore.UnrealScript.Language.Tree;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using UsefulThings;

namespace ME3Explorer.PackageEditor.Experiments
{
    /// <summary>
    /// Class for SirCxyrtyx experimental code
    /// </summary>
    public class PackageEditorExperimentsS
    {

        class OpcodeInfo
        {
            public readonly HashSet<string> PropTypes = new HashSet<string>();
            public readonly HashSet<string> PropLocations = new HashSet<string>();

            public readonly List<(string filePath, int uIndex, int position)> Usages =
                new List<(string filePath, int uIndex, int position)>();
        }
        public static void ScanStuff(PackageEditorWPF pewpf)
        {
            //var game = MEGame.ME3;
            //var filePaths = MELoadedFiles.GetOfficialFiles(game).Concat(MELoadedFiles.GetOfficialFiles(MEGame.ME2)).Concat(MELoadedFiles.GetOfficialFiles(MEGame.ME1));
            //var filePaths = MELoadedFiles.GetAllFiles(game);
            /*"Core.pcc", "Engine.pcc", "GameFramework.pcc", "GFxUI.pcc", "WwiseAudio.pcc", "SFXOnlineFoundation.pcc", "SFXGame.pcc" */
            //var filePaths = new[] { "Core.pcc", "Engine.pcc", "GameFramework.pcc", "GFxUI.pcc", "WwiseAudio.pcc", "SFXOnlineFoundation.pcc" }.Select(f => Path.Combine(ME3Directory.CookedPCPath, f));
            var interestingExports = new List<EntryRefAndMessage>();
            var foundClasses = new HashSet<string>(); //new HashSet<string>(BinaryInterpreterWPF.ParsableBinaryClasses);
            var foundProps = new Dictionary<string, string>();
            var problematicPaths = new HashSet<string>();

            var unkOpcodes = new List<int>();//Enumerable.Range(0x5B, 8).ToList();
            unkOpcodes.Add(0);
            unkOpcodes.Add(1);
            var unkOpcodesInfo = unkOpcodes.ToDictionary(i => i, i => new OpcodeInfo());
            var comparisonDict = new Dictionary<string, (byte[] original, byte[] newData)>();

            var extraInfo = new HashSet<string>();

            pewpf.IsBusy = true;
            pewpf.BusyText = "Scanning";
            Task.Run(async () =>
            {
                MEGame[] games =
                {
                    //MEGame.ME3, 
                    //MEGame.ME2, 
                    MEGame.ME1
                };
                foreach (MEGame meGame in games)
                {
                    var filePaths = MELoadedFiles.GetOfficialFiles(meGame);
                    //preload base files for faster scanning
                    using var baseFiles = MEPackageHandler.OpenMEPackages(EntryImporter.FilesSafeToImportFrom(meGame)
                                                                                       .Select(f => Path.Combine(MEDirectories.GetCookedPath(meGame), f)));
                    if (meGame is MEGame.ME3)
                    {
                        baseFiles.Add(MEPackageHandler.OpenMEPackage(Path.Combine(ME3Directory.CookedPCPath, "BIOP_MP_COMMON.pcc")));
                    }

                    foreach (string filePath in filePaths)
                    {
                        if (filePath.EndsWith("SFXOnlineFoundation.pcc") || filePath.EndsWith("SFXTest.pcc") || filePath.Contains("UnrealScriptTest."))
                        {
                            continue;
                        }
                        //ScanShaderCache(filePath);
                        //ScanMaterials(filePath);
                        //ScanStaticMeshComponents(filePath);
                        //ScanLightComponents(filePath);
                        //ScanLevel(filePath);
                        //if (findClass(filePath, "ShaderCache", true)) break;
                        //findClassesWithBinary(filePath);
                        //await ScanScripts2(filePath);
                        await RecompileAllFunctions(filePath);
                        //if (interestingExports.Count > 0)
                        //{
                        //    break;
                        //}
                        //if (resolveImports(filePath)) break;
                        if (interestingExports.Count > 25)
                        {
                            goto end;
                        }
                    }
                }
                end: ;
            }).ContinueWithOnUIThread(prevTask =>
            {
                //the base files will have been in memory for so long at this point that they take a looong time to clear out automatically, so force it.
                MemoryAnalyzer.ForceFullGC();
                pewpf.IsBusy = false;
                interestingExports.Add(new EntryRefAndMessage(0, null, string.Join("\n", extraInfo)));
                var listDlg = new ListDialog(interestingExports, "Interesting Exports", "", pewpf)
                {
                    DoubleClickEntryHandler2 = entryItem =>
                    {
                        if (entryItem?.FilePath is not null)
                        {
                            var p = new PackageEditorWPF();
                            p.Show();
                            p.LoadFile(entryItem.FilePath, entryItem.UIndex);
                            p.Activate();
                            if (comparisonDict.TryGetValue($"{entryItem.UIndex} {entryItem.FilePath}", out (byte[] original, byte[] newData) val))
                            {
                                File.WriteAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "original.bin"), val.original);
                                File.WriteAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "new.bin"), val.newData);
                            }
                        }
                    }
                };
                listDlg.Show();
            });

            #region extra scanning functions

            bool findClass(string filePath, string className, bool withBinary = false)
            {
                Debug.WriteLine($" {filePath}");
                using (IMEPackage pcc = MEPackageHandler.OpenMEPackage(filePath))
                {
                    //if (!pcc.IsCompressed) return false;

                    var exports = pcc.Exports.Where(exp => !exp.IsDefaultObject && exp.IsA(className));
                    foreach (ExportEntry exp in exports)
                    {
                        try
                        {
                            //Debug.WriteLine($"{exp.UIndex}: {filePath}");
                            var originalData = exp.Data;
                            exp.WriteBinary(ObjectBinary.From(exp));
                            var newData = exp.Data;
                            if (!originalData.SequenceEqual(newData))
                            {
                                interestingExports.Add(new EntryRefAndMessage(exp));
                                File.WriteAllBytes(
                                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                        "original.bin"), originalData);
                                File.WriteAllBytes(
                                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                        "new.bin"), newData);
                                return true;
                            }
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                            interestingExports.Add(new EntryRefAndMessage(exp, $"{exception}"));
                            return true;
                        }
                    }
                }

                return false;
            }

            void findClassesWithBinary(string filePath)
            {
                using (IMEPackage pcc = MEPackageHandler.OpenMEPackage(filePath))
                {
                    foreach (ExportEntry exp in pcc.Exports.Where(exp => !exp.IsDefaultObject))
                    {
                        try
                        {
                            if (!foundClasses.Contains(exp.ClassName) && exp.propsEnd() < exp.DataSize)
                            {
                                if (ObjectBinary.From(exp) != null)
                                {
                                    foundClasses.Add(exp.ClassName);
                                }
                                else if (exp.GetBinaryData().Any(b => b != 0))
                                {
                                    foundClasses.Add(exp.ClassName);
                                    interestingExports.Add(new EntryRefAndMessage(exp));
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                            interestingExports.Add(new EntryRefAndMessage(exp, $"{exp.UIndex}: {filePath}\n{exception}"));
                        }
                    }
                }
            }

            void ScanShaderCache(string filePath)
            {
                using (IMEPackage pcc = MEPackageHandler.OpenMEPackage(filePath))
                {
                    ExportEntry shaderCache = pcc.Exports.FirstOrDefault(exp => exp.ClassName == "ShaderCache");
                    if (shaderCache == null) return;
                    int oldDataOffset = shaderCache.DataOffset;

                    try
                    {
                        MemoryStream binData = new MemoryStream(shaderCache.Data);
                        binData.JumpTo(shaderCache.propsEnd() + 1);

                        int nameList1Count = binData.ReadInt32();
                        binData.Skip(nameList1Count * 12);

                        int namelist2Count = binData.ReadInt32(); //namelist2
                        binData.Skip(namelist2Count * 12);

                        int shaderCount = binData.ReadInt32();
                        for (int i = 0; i < shaderCount; i++)
                        {
                            binData.Skip(24);
                            int nextShaderOffset = binData.ReadInt32() - oldDataOffset;
                            binData.Skip(14);
                            if (binData.ReadInt32() != 1111577667) //CTAB
                            {
                                interestingExports.Add(new EntryRefAndMessage(null, $"{binData.Position - 4}: {filePath}"));
                                return;
                            }

                            binData.JumpTo(nextShaderOffset);
                        }

                        int vertexFactoryMapCount = binData.ReadInt32();
                        binData.Skip(vertexFactoryMapCount * 12);

                        int materialShaderMapCount = binData.ReadInt32();
                        for (int i = 0; i < materialShaderMapCount; i++)
                        {
                            binData.Skip(16);

                            int switchParamCount = binData.ReadInt32();
                            binData.Skip(switchParamCount * 32);

                            int componentMaskParamCount = binData.ReadInt32();
                            //if (componentMaskParamCount != 0)
                            //{
                            //    interestingExports.Add($"{i}: {filePath}");
                            //    return;
                            //}

                            binData.Skip(componentMaskParamCount * 44);

                            int normalParams = binData.ReadInt32();
                            if (normalParams != 0)
                            {
                                interestingExports.Add(new EntryRefAndMessage(null, $"{i}: {filePath}"));
                                return;
                            }

                            binData.Skip(normalParams * 29);

                            int unrealVersion = binData.ReadInt32();
                            int licenseeVersion = binData.ReadInt32();
                            if (unrealVersion != 684 || licenseeVersion != 194)
                            {
                                interestingExports.Add(new EntryRefAndMessage(null, $"{binData.Position - 8}: {filePath}"));
                                return;
                            }

                            int nextMaterialShaderMapOffset = binData.ReadInt32() - oldDataOffset;
                            binData.JumpTo(nextMaterialShaderMapOffset);
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        interestingExports.Add(new EntryRefAndMessage(null, $"{filePath}\n{exception}"));
                    }
                }
            }

            void ScanScripts(string filePath)
            {
                using IMEPackage pcc = MEPackageHandler.OpenMEPackage(filePath);
                foreach (ExportEntry exp in pcc.Exports.Where(exp => !exp.IsDefaultObject))
                {
                    try
                    {
                        if ((exp.ClassName == "State" || exp.ClassName == "Function") &&
                            ObjectBinary.From(exp) is UStruct uStruct)
                        {
                            byte[] data = exp.Data;
                            (_, List<BytecodeSingularToken> tokens) = Bytecode.ParseBytecode(uStruct.ScriptBytes, exp);
                            foreach (var token in tokens)
                            {
                                if (token.CurrentStack.Contains("UNKNOWN") || token.OpCodeString.Contains("UNKNOWN"))
                                {
                                    interestingExports.Add(new EntryRefAndMessage(exp));
                                }

                                if (unkOpcodes.Contains(token.OpCode))
                                {
                                    int refUIndex = EndianReader.ToInt32(data, token.StartPos + 1, pcc.Endian);
                                    IEntry entry = pcc.GetEntry(refUIndex);
                                    if (entry != null && (entry.ClassName == "ByteProperty"))
                                    {
                                        var info = unkOpcodesInfo[token.OpCode];
                                        info.Usages.Add(pcc.FilePath, exp.UIndex, token.StartPos);
                                        info.PropTypes.Add(refUIndex switch
                                        {
                                            0 => "Null",
                                            _ when entry != null => entry.ClassName,
                                            _ => "Invalid"
                                        });
                                        if (entry != null)
                                        {
                                            if (entry.Parent == exp)
                                            {
                                                info.PropLocations.Add("Local");
                                            }
                                            else if (entry.Parent == (exp.Parent.ClassName == "State" ? exp.Parent.Parent : exp.Parent))
                                            {
                                                info.PropLocations.Add("ThisClass");
                                            }
                                            else if (entry.Parent.ClassName == "Function")
                                            {
                                                info.PropLocations.Add("OtherFunction");
                                            }
                                            else if (exp.Parent.IsA(entry.Parent.ObjectName))
                                            {
                                                info.PropLocations.Add("AncestorClass");
                                            }
                                            else
                                            {
                                                info.PropLocations.Add("OtherClass");
                                            }
                                        }
                                    }


                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        interestingExports.Add(new EntryRefAndMessage(exp, $"{exp.UIndex}: {filePath}\n{exception}"));
                    }
                }
            }

            async Task ScanScripts2(string filePath)
            {
                using IMEPackage pcc = MEPackageHandler.OpenMEPackage(filePath);
                var fileLib = new FileLib(pcc);
                if (await fileLib.Initialize())
                {
                    foreach (ExportEntry exp in pcc.Exports.Reverse().Where(exp => exp.ClassName == "Function" && exp.Parent.ClassName == "Class" && !exp.GetBinaryData<UFunction>().FunctionFlags.Has(FunctionFlags.Native)))
                    {
                        if (exp.Parent.ObjectName == "SFXSeqAct_ScreenShake")
                        {
                            continue;
                        }
                        try
                        {
                            var originalData = exp.Data;
                            (_, string originalScript) = UnrealScriptCompiler.DecompileExport(exp, fileLib);
                            (ASTNode ast, MessageLog log) = UnrealScriptCompiler.CompileFunction(exp, originalScript, fileLib);
                            if (log.AllErrors.Count > 0)
                            {
                                interestingExports.Add(new EntryRefAndMessage(exp));
                                continue;
                            }

                            if (!originalData.SequenceEqual(exp.Data))
                            {
                                interestingExports.Add(new EntryRefAndMessage(exp));
                                comparisonDict.Add($"{exp.UIndex} {exp.FileRef.FilePath}", (originalData, exp.Data));
                                File.WriteAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "original.bin"), originalData);
                                File.WriteAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "new.bin"), exp.Data);
                                continue;
                            }
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                            interestingExports.Add(new EntryRefAndMessage(exp, $"{exp.UIndex}: {filePath}\n{exception}"));
                            return;
                        }
                    }
                }
                else
                {
                    interestingExports.Add(new EntryRefAndMessage(null, $"{pcc.FilePath} failed to compile!"));
                }
            }

            async Task RecompileAllFunctions(string filePath)
            {
                try
                {
                    using IMEPackage pcc = MEPackageHandler.OpenMEPackage(filePath);
                    var fileLib = new FileLib(pcc);
                    if (await fileLib.Initialize())
                    {
                        foreach (ExportEntry exp in pcc.Exports.Where(exp => exp.ClassName == "Function"))
                        {
                            if (problematicPaths.Contains(exp.InstancedFullPath))
                            {
                                continue;
                            }
                            try
                            {
                                //var originalData = exp.Data;
                                int exportCount = pcc.ExportCount;
                                UFunction originalFunction = exp.GetBinaryData<UFunction>();
                                var originalFlags = originalFunction.FunctionFlags;
                                var children = ScriptObjectCompiler.GetMembers(originalFunction).ToList();
                                (_, string originalScript) = UnrealScriptCompiler.DecompileExport(exp, fileLib);
                                (ASTNode ast, MessageLog log) = UnrealScriptCompiler.CompileFunction(exp, originalScript, fileLib);
                                if (ast == null || log.AllErrors.Count > 0)
                                {
                                    interestingExports.Add(new EntryRefAndMessage(exp));
                                    problematicPaths.Add(exp.InstancedFullPath);
                                }

                                if (exportCount != pcc.ExportCount)
                                {
                                    interestingExports.Add(new EntryRefAndMessage(exp, $"{$"#{exp.UIndex}",-9}: {filePath}\nAdded Exports!"));
                                    problematicPaths.Add(exp.InstancedFullPath);
                                }

                                if (exp.GetBinaryData<UFunction>().FunctionFlags != originalFlags)
                                {
                                    interestingExports.Add(new EntryRefAndMessage(exp, $"{$"#{exp.UIndex}",-9}: {filePath}\nChanged Flags!"));
                                    problematicPaths.Add(exp.InstancedFullPath);
                                }

                                foreach (UField field in children)
                                {
                                    if (field.Export.EntryHasPendingChanges ||
                                        field is UArrayProperty arrProp && pcc.GetEntry(arrProp.ElementType) is ExportEntry {EntryHasPendingChanges: true})
                                    {
                                        interestingExports.Add(new EntryRefAndMessage(exp, $"{$"#{exp.UIndex}",-9}: {filePath}\nChanged Variable(s)!"));
                                        problematicPaths.Add(exp.InstancedFullPath);
                                        break;
                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(exception);
                                interestingExports.Add(new EntryRefAndMessage(exp, $"{$"#{exp.UIndex}",-9}: {filePath}\n{exception}"));
                                problematicPaths.Add(exp.InstancedFullPath);
                                return;
                            }
                        }
                    }
                    else
                    {
                        interestingExports.Add(new EntryRefAndMessage(0, filePath, $"{filePath} failed to compile!"));
                    }
                }
                catch (Exception e)
                {
                    interestingExports.Add(new EntryRefAndMessage(0, filePath, $"{filePath} failed to compile!\n{e}"));
                }
            }

            bool resolveImports(string filePath)
            {
                using IMEPackage pcc = MEPackageHandler.OpenMEPackage(filePath);

                //pre-load associated files
                var gameFiles = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME3);
                using var associatedFiles = MEPackageHandler.OpenMEPackages(EntryImporter
                    .GetPossibleAssociatedFiles(pcc)
                    .Select(fileName => gameFiles.TryGetValue(fileName, out string path) ? path : null)
                    .Where(File.Exists));

                var filesSafeToImportFrom = EntryImporter.FilesSafeToImportFrom(pcc.Game)
                    .Select(Path.GetFileNameWithoutExtension).ToList();
                Debug.WriteLine(filePath);
                foreach (ImportEntry import in pcc.Imports.Where(imp =>
                    !filesSafeToImportFrom.Contains(imp.FullPath.Split('.')[0])))
                {
                    try
                    {
                        if (EntryImporter.ResolveImport(import) is ExportEntry exp)
                        {
                            extraInfo.Add(Path.GetFileName(exp.FileRef.FilePath));
                        }
                        else
                        {
                            interestingExports.Add(new EntryRefAndMessage(import));
                            return true;
                        }

                    }
                    catch (Exception exception)
                    {
                        interestingExports.Add(new EntryRefAndMessage(import, $"{$"#{import.UIndex}",-9} {filePath}\n{exception}"));
                        return true;
                    }
                }

                return false;
            }

            bool CheckIfFound(IEntry entry)
            {
                string fullPath = entry.InstancedFullPath;
                if (problematicPaths.Contains(fullPath))
                {
                    return true;
                }
                problematicPaths.Add(fullPath);
                return false;
            }

            #endregion
        }

        public static void CreateDynamicLighting(IMEPackage Pcc)
        {
            foreach (ExportEntry exp in Pcc.Exports.Where(exp => exp.IsA("MeshComponent") || exp.IsA("BrushComponent")))
            {
                PropertyCollection props = exp.GetProperties();
                if (props.GetProp<ObjectProperty>("StaticMesh")?.Value != 11483 &&
                    (props.GetProp<BoolProperty>("bAcceptsLights")?.Value == false ||
                     props.GetProp<BoolProperty>("CastShadow")?.Value == false))
                {
                    // shadows/lighting has been explicitly forbidden, don't mess with it.
                    continue;
                }

                props.AddOrReplaceProp(new BoolProperty(false, "bUsePreComputedShadows"));
                props.AddOrReplaceProp(new BoolProperty(false, "bBioForcePreComputedShadows"));
                //props.AddOrReplaceProp(new BoolProperty(true, "bCastDynamicShadow"));
                //props.AddOrReplaceProp(new BoolProperty(true, "CastShadow"));
                //props.AddOrReplaceProp(new BoolProperty(true, "bAcceptsDynamicDominantLightShadows"));
                props.AddOrReplaceProp(new BoolProperty(true, "bAcceptsLights"));
                //props.AddOrReplaceProp(new BoolProperty(true, "bAcceptsDynamicLights"));

                var lightingChannels = props.GetProp<StructProperty>("LightingChannels") ??
                                       new StructProperty("LightingChannelContainer", false,
                                           new BoolProperty(true, "bIsInitialized"))
                                       {
                                           Name = "LightingChannels"
                                       };
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Static"));
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Dynamic"));
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "CompositeDynamic"));
                props.AddOrReplaceProp(lightingChannels);

                exp.WriteProperties(props);
            }

            foreach (ExportEntry exp in Pcc.Exports.Where(exp => exp.IsA("LightComponent")))
            {
                PropertyCollection props = exp.GetProperties();
                //props.AddOrReplaceProp(new BoolProperty(true, "bCanAffectDynamicPrimitivesOutsideDynamicChannel"));
                //props.AddOrReplaceProp(new BoolProperty(true, "bForceDynamicLight"));

                var lightingChannels = props.GetProp<StructProperty>("LightingChannels") ??
                                       new StructProperty("LightingChannelContainer", false,
                                           new BoolProperty(true, "bIsInitialized"))
                                       {
                                           Name = "LightingChannels"
                                       };
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Static"));
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "Dynamic"));
                lightingChannels.Properties.AddOrReplaceProp(new BoolProperty(true, "CompositeDynamic"));
                props.AddOrReplaceProp(lightingChannels);

                exp.WriteProperties(props);
            }

            MessageBox.Show("Done!");
        }

        public static void ConvertAllDialogueToSkippable(PackageEditorWPF pewpf)
        {
            var gameString = InputComboBoxWPF.GetValue(pewpf,
                            "Select which game's files you want converted to having skippable dialogue",
                            "Game selector", new[] { "ME1", "ME2", "ME3" }, "ME1");
            if (Enum.TryParse(gameString, out MEGame game) && MessageBoxResult.Yes ==
                MessageBox.Show(pewpf,
                    $"WARNING! This will edit every dialogue-containing file in {gameString}, including in DLCs and installed mods. Do you want to begin?",
                    "", MessageBoxButton.YesNo))
            {
                pewpf.IsBusy = true;
                pewpf.BusyText = $"Making all {gameString} dialogue skippable";
                Task.Run(() =>
                {
                    foreach (string file in MELoadedFiles.GetAllFiles(game))
                    {
                        using IMEPackage pcc = MEPackageHandler.OpenMEPackage(file);
                        bool hasConv = false;
                        foreach (ExportEntry conv in pcc.Exports.Where(exp => exp.ClassName == "BioConversation"))
                        {
                            hasConv = true;
                            PropertyCollection props = conv.GetProperties();
                            if (props.GetProp<ArrayProperty<StructProperty>>("m_EntryList") is
                                ArrayProperty<StructProperty> entryList)
                            {
                                foreach (StructProperty entryNode in entryList)
                                {
                                    entryNode.Properties.AddOrReplaceProp(new BoolProperty(true, "bSkippable"));
                                }
                            }

                            if (props.GetProp<ArrayProperty<StructProperty>>("m_ReplyList") is
                                ArrayProperty<StructProperty> replyList)
                            {
                                foreach (StructProperty entryNode in replyList)
                                {
                                    entryNode.Properties.AddOrReplaceProp(new BoolProperty(false, "bUnskippable"));
                                }
                            }

                            conv.WriteProperties(props);
                        }

                        if (hasConv)
                            pcc.Save();
                    }
                }).ContinueWithOnUIThread(prevTask =>
                {
                    pewpf.IsBusy = false;
                    MessageBox.Show(pewpf, "Done!");
                });
            }
        }

        public static void DumpAllShaders(IMEPackage Pcc)
        {
            if (Pcc.Exports.FirstOrDefault(exp => exp.ClassName == "ShaderCache") is ExportEntry shaderCacheExport)
            {
                var dlg = new CommonOpenFileDialog("Pick a folder to save Shaders to.")
                {
                    IsFolderPicker = true,
                    EnsurePathExists = true
                };
                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    var shaderCache = ObjectBinary.From<ShaderCache>(shaderCacheExport);
                    foreach (Shader shader in shaderCache.Shaders.Values())
                    {
                        string shaderType = shader.ShaderType;
                        string pathWithoutInvalids = Path.Combine(dlg.FileName,
                            $"{shaderType.GetPathWithoutInvalids()} - {shader.Guid}.txt");
                        File.WriteAllText(pathWithoutInvalids,
                            SharpDX.D3DCompiler.ShaderBytecode.FromStream(new MemoryStream(shader.ShaderByteCode))
                                .Disassemble());
                    }

                    MessageBox.Show("Done!");
                }
            }
        }

        public static void DumpMaterialShaders(ExportEntry matExport)
        {
            var dlg = new CommonOpenFileDialog("Pick a folder to save Shaders to.")
            {
                IsFolderPicker = true,
                EnsurePathExists = true
            };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var matInst = new MaterialInstanceConstant(matExport);
                matInst.GetShaders();
                foreach (Shader shader in matInst.Shaders)
                {
                    string shaderType = shader.ShaderType;
                    string pathWithoutInvalids = Path.Combine(dlg.FileName,
                        $"{shaderType.GetPathWithoutInvalids()} - {shader.Guid} - OFFICIAL.txt");
                    File.WriteAllText(pathWithoutInvalids,
                        SharpDX.D3DCompiler.ShaderBytecode.FromStream(new MemoryStream(shader.ShaderByteCode))
                            .Disassemble());

                    pathWithoutInvalids = Path.Combine(dlg.FileName,
                        $"{shaderType.GetPathWithoutInvalids()} - {shader.Guid} - SirCxyrtyx.txt");
                    File.WriteAllText(pathWithoutInvalids, shader.ShaderDisassembly);
                }

                MessageBox.Show("Done!");
            }
        }

        public static void ReserializeExport(ExportEntry export)
        {
            PropertyCollection props = export.GetProperties();
            ObjectBinary bin = ObjectBinary.From(export) ?? export.GetBinaryData();
            byte[] original = export.Data;

            export.WriteProperties(props);

            EndianReader ms = new EndianReader(new MemoryStream()) { Endian = export.FileRef.Endian };
            ms.Writer.Write(export.Data, 0, export.propsEnd());
            bin.WriteTo(ms.Writer, export.FileRef, export.DataOffset);

            byte[] changed = ms.ToArray();
            //export.Data = changed;
            if (original.SequenceEqual(changed))
            {
                MessageBox.Show("reserialized identically!");
            }
            else
            {
                string userFolder = Path.Combine(@"C:\Users", Environment.UserName);
                File.WriteAllBytes(Path.Combine(userFolder, "converted.bin"), changed);
                File.WriteAllBytes(Path.Combine(userFolder, "original.bin"), original);
                if (original.Length != changed.Length)
                {
                    MessageBox.Show($"Differences detected: Lengths are not the same. Original {original.Length}, Reserialized {changed.Length}");
                }
                else
                {
                    for (int i = 0; i < Math.Min(changed.Length, original.Length); i++)
                    {
                        if (original[i] != changed[i])
                        {
                            MessageBox.Show($"Differences detected: Bytes differ first at 0x{i:X8}");
                            break;
                        }
                    }
                }
            }
        }

        public static void RunPropertyCollectionTest(PackageEditorWPF pewpf)
        {
            var filePaths = /*MELoadedFiles.GetOfficialFiles(MEGame.ME3).Concat*/
                            (MELoadedFiles.GetOfficialFiles(MEGame.ME2)).Concat(MELoadedFiles.GetOfficialFiles(MEGame.ME1));

            pewpf.IsBusy = true;
            pewpf.BusyText = "Scanning";
            Task.Run(() =>
            {
                foreach (string filePath in filePaths)
                {
                    using IMEPackage pcc = MEPackageHandler.OpenMEPackage(filePath);
                    Debug.WriteLine(filePath);
                    foreach (ExportEntry export in pcc.Exports)
                    {
                        try
                        {
                            byte[] originalData = export.Data;
                            PropertyCollection props = export.GetProperties();
                            export.WriteProperties(props);
                            byte[] resultData = export.Data;
                            if (!originalData.SequenceEqual(resultData))
                            {
                                string userFolder = Path.Combine(@"C:\Users", Environment.UserName);
                                File.WriteAllBytes(Path.Combine(userFolder, $"c.bin"), resultData);
                                File.WriteAllBytes(Path.Combine(userFolder, $"o.bin"), originalData);
                                return (filePath, export.UIndex);
                            }
                        }
                        catch (Exception e)
                        {
                            return (filePath, export.UIndex);
                        }
                    }
                }

                return (null, 0);

            }).ContinueWithOnUIThread(prevTask =>
            {
                pewpf.IsBusy = false;
                (string filePath, int uIndex) = prevTask.Result;
                if (filePath == null)
                {
                    MessageBox.Show(pewpf, "No errors occured!");
                }
                else
                {
                    pewpf.LoadFile(filePath, uIndex);
                    MessageBox.Show(pewpf, $"Error at #{uIndex} in {filePath}!");
                }
            });
        }

        public static void UDKifyTest(PackageEditorWPF pewpf)
        {
            var Pcc = pewpf.Pcc;
            var udkPath = Properties.Settings.Default.UDKCustomPath;
            if (udkPath == null || !Directory.Exists(udkPath))
            {
                var udkDlg = new System.Windows.Forms.FolderBrowserDialog();
                udkDlg.Description = @"Select UDK\Custom folder";
                System.Windows.Forms.DialogResult result = udkDlg.ShowDialog();

                if (result != System.Windows.Forms.DialogResult.OK ||
                    string.IsNullOrWhiteSpace(udkDlg.SelectedPath))
                    return;
                udkPath = udkDlg.SelectedPath;
                Properties.Settings.Default.UDKCustomPath = udkPath;
            }

            string fileName = Path.GetFileNameWithoutExtension(Pcc.FilePath);
            bool convertAll = fileName.StartsWith("BioP") && MessageBoxResult.Yes ==
                MessageBox.Show("Convert BioA and BioD files for this level?", "", MessageBoxButton.YesNo);

            pewpf.IsBusy = true;
            pewpf.BusyText = $"Converting {fileName}";
            Task.Run(() =>
            {
                string persistentPath = StaticLightingGenerator.GenerateUDKFileForLevel(udkPath, Pcc);
                if (convertAll)
                {
                    var levelFiles = new List<string>();
                    string levelName = fileName.Split('_')[1];
                    foreach ((string fileName, string filePath) in MELoadedFiles.GetFilesLoadedInGame(Pcc.Game))
                    {
                        if (!fileName.Contains("_LOC_") && fileName.Split('_') is { } parts && parts.Length >= 2 &&
                            parts[1] == levelName)
                        {
                            pewpf.BusyText = $"Converting {fileName}";
                            using IMEPackage levPcc = MEPackageHandler.OpenMEPackage(filePath);
                            levelFiles.Add(StaticLightingGenerator.GenerateUDKFileForLevel(udkPath, levPcc));
                        }
                    }

                    using IMEPackage persistentUDK = MEPackageHandler.OpenUDKPackage(persistentPath);
                    IEntry levStreamingClass =
                        persistentUDK.getEntryOrAddImport("Engine.LevelStreamingAlwaysLoaded");
                    IEntry theWorld = persistentUDK.Exports.First(exp => exp.ClassName == "World");
                    int i = 1;
                    int firstLevStream = persistentUDK.ExportCount;
                    foreach (string levelFile in levelFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(levelFile);
                        persistentUDK.AddExport(new ExportEntry(persistentUDK, properties: new PropertyCollection
                            {
                                new NameProperty(fileName, "PackageName"),
                                CommonStructs.ColorProp(
                                    System.Drawing.Color.FromArgb(255, (byte) (i % 256), (byte) ((255 - i) % 256),
                                        (byte) ((i * 7) % 256)), "DrawColor")
                            })
                        {
                            ObjectName = new NameReference("LevelStreamingAlwaysLoaded", i),
                            Class = levStreamingClass,
                            Parent = theWorld
                        });
                        i++;
                    }

                    var streamingLevelsProp = new ArrayProperty<ObjectProperty>("StreamingLevels");
                    for (int j = firstLevStream; j < persistentUDK.ExportCount; j++)
                    {
                        streamingLevelsProp.Add(new ObjectProperty(j));
                    }

                    persistentUDK.Exports.First(exp => exp.ClassName == "WorldInfo")
                        .WriteProperty(streamingLevelsProp);
                    persistentUDK.Save();
                }

                pewpf.IsBusy = false;
            });
        }

        public static void MakeME1TextureFileList(PackageEditorWPF pewpf)
        {
            var filePaths = MELoadedFiles.GetOfficialFiles(MEGame.ME1).ToList();

            pewpf.IsBusy = true;
            pewpf.BusyText = "Scanning";
            Task.Run(() =>
            {
                var textureFiles = new HashSet<string>();
                foreach (string filePath in filePaths)
                {
                    using IMEPackage pcc = MEPackageHandler.OpenMEPackage(filePath);

                    foreach (ExportEntry export in pcc.Exports)
                    {
                        try
                        {
                            if (export.IsTexture() && !export.IsDefaultObject)
                            {
                                List<Texture2DMipInfo> mips = Texture2D.GetTexture2DMipInfos(export, null);
                                foreach (Texture2DMipInfo mip in mips)
                                {
                                    if (mip.storageType == StorageTypes.extLZO ||
                                        mip.storageType == StorageTypes.extZlib ||
                                        mip.storageType == StorageTypes.extUnc)
                                    {
                                        var fullPath = filePaths.FirstOrDefault(x =>
                                            Path.GetFileName(x).Equals(mip.TextureCacheName,
                                                StringComparison.InvariantCultureIgnoreCase));
                                        if (fullPath != null)
                                        {
                                            var baseIdx = fullPath.LastIndexOf("CookedPC");
                                            textureFiles.Add(fullPath.Substring(baseIdx));
                                            break;
                                        }
                                        else
                                        {
                                            throw new FileNotFoundException(
                                                $"Externally referenced texture file not found in game: {mip.TextureCacheName}.");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.Message);
                        }
                    }
                }

                return textureFiles;

            }).ContinueWithOnUIThread(prevTask =>
            {
                pewpf.IsBusy = false;
                List<string> files = prevTask.Result.OrderBy(s => s).ToList();
                File.WriteAllText(Path.Combine(App.ExecFolder, "ME1TextureFiles.json"),
                    JsonConvert.SerializeObject(files, Formatting.Indented));
                ListDialog dlg = new ListDialog(files, "", "ME1 files with externally referenced textures", pewpf);
                dlg.Show();
            });
        }

        public static void BuildNativeTable(PackageEditorWPF pewpf)
        {
            pewpf.IsBusy = true;
            pewpf.BusyText = "Building Native Tables";
            Task.Run(() =>
            {
                foreach (MEGame game in new []{MEGame.ME1, MEGame.ME2})
                {
                    string cookedPath = MEDirectories.GetCookedPath(game);
                    var entries = new List<(int, string)>();
                    foreach (string fileName in BaseFileNames(game))
                    {
                        using IMEPackage pcc = MEPackageHandler.OpenMEPackage(Path.Combine(cookedPath, fileName));
                        foreach (ExportEntry export in pcc.Exports.Where(exp => exp.ClassName == "Function"))
                        {
                            var func = export.GetBinaryData<UFunction>();
                            ushort nativeIndex = func.NativeIndex;
                            if (nativeIndex > 0)
                            {
                                NativeType type = NativeType.Function;
                                if (func.FunctionFlags.Has(FunctionFlags.PreOperator))
                                {
                                    type = NativeType.PreOperator;
                                }
                                else if (func.FunctionFlags.Has(FunctionFlags.Operator))
                                {
                                    var nextItem = func.Children;
                                    int paramCount = 0;
                                    while (export.FileRef.TryGetUExport(nextItem, out ExportEntry nextChild))
                                    {
                                        var objBin = ObjectBinary.From(nextChild);
                                        switch (objBin)
                                        {
                                            case UProperty uProperty:
                                                if (uProperty.PropertyFlags.HasFlag(UnrealFlags.EPropertyFlags.ReturnParm))
                                                {
                                                }
                                                else if (uProperty.PropertyFlags.HasFlag(UnrealFlags.EPropertyFlags.Parm))
                                                {
                                                    paramCount++;
                                                }
                                                nextItem = uProperty.Next;
                                                break;
                                            default:
                                                nextItem = 0;
                                                break;
                                        }
                                    }

                                    type = paramCount == 1 ? NativeType.PostOperator : NativeType.Operator;
                                }
                                entries.Add(nativeIndex, $"{{ 0x{nativeIndex:X}, new NativeTableEntry {{ Name=\"{func.FriendlyName}\", Type=NativeType.{type}, Precedence={func.OperatorPrecedence}}} }},");
                            }
                        }
                    }

                    using var fileStream = new FileStream(Path.Combine(App.ExecFolder, $"{game}NativeTable.cs"), FileMode.Create);
                    using var writer = new CodeWriter(fileStream);
                    writer.WriteLine("using System.Collections.Generic;");
                    writer.WriteLine();
                    writer.WriteBlock("namespace ME3Script.Decompiling", () =>
                    {
                        writer.WriteBlock("public partial class ByteCodeDecompiler", () =>
                        {
                            writer.WriteLine($"public static readonly Dictionary<int, NativeTableEntry> {game}NativeTable = new() ");
                            writer.WriteLine("{");
                            writer.IncreaseIndent();

                            foreach ((_, string entry) in entries.OrderBy(tup => tup.Item1))
                            {
                                writer.WriteLine(entry);
                            }

                            writer.DecreaseIndent();
                            writer.WriteLine("};");
                        });

                    });
                }
            }).ContinueWithOnUIThread(prevTask =>
            {
                pewpf.IsBusy = false;
            });

            static string[] BaseFileNames(MEGame game) => game switch
            {
                MEGame.ME3 => new[] { "Core.pcc", "Engine.pcc", "GameFramework.pcc", "GFxUI.pcc", "WwiseAudio.pcc", "SFXOnlineFoundation.pcc", "SFXGame.pcc" },
                MEGame.ME2 => new[] { "Core.pcc", "Engine.pcc", "GameFramework.pcc", "GFxUI.pcc", "WwiseAudio.pcc", "SFXOnlineFoundation.pcc", "PlotManagerMap.pcc", "SFXGame.pcc", "Startup_INT.pcc" },
                MEGame.ME1 => new[] { "Core.u", "Engine.u", "GameFramework.u", "BIOC_Base.u" },
                _ => throw new ArgumentOutOfRangeException(nameof(game))
            };
        }

        public static void DumpSound(PackageEditorWPF packEd)
        {
            if (InputComboBoxWPF.GetValue(packEd, "Choose game:", "Game to dump sound for", new []{"ME3", "ME2"}, "ME3") is string gameStr && 
                Enum.TryParse(gameStr, out MEGame game))
            {
                string tag = PromptDialog.Prompt(packEd, "Character tag:", defaultValue: "player_f", selectText: true);
                if (string.IsNullOrWhiteSpace(tag))
                {
                    return;
                }
                var dlg = new CommonOpenFileDialog("Pick a folder to save WAVs to.")
                {
                    IsFolderPicker = true,
                    EnsurePathExists = true
                };
                if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
                {
                    return;
                }

                string outFolder = dlg.FileName;
                var filePaths = MELoadedFiles.GetOfficialFiles(game);
                packEd.IsBusy = true;
                packEd.BusyText = "Scanning";
                Task.Run(() =>
                {
                    //preload base files for faster scanning
                    using var baseFiles = MEPackageHandler.OpenMEPackages(EntryImporter.FilesSafeToImportFrom(game)
                                                                                       .Select(f => Path.Combine(MEDirectories.GetCookedPath(game), f)));
                    if (game is MEGame.ME3)
                    {
                        baseFiles.Add(MEPackageHandler.OpenMEPackage(Path.Combine(ME3Directory.CookedPCPath, "BIOP_MP_COMMON.pcc")));
                    }

                    foreach (string filePath in filePaths)
                    {
                        using IMEPackage pcc = MEPackageHandler.OpenMEPackage(filePath);
                        if (game is MEGame.ME3 or MEGame.ME2)
                        {
                            foreach (ExportEntry export in pcc.Exports.Where(exp => exp.ClassName == "WwiseStream"))
                            {
                                if (export.ObjectNameString.Split(',') is string[] { Length: > 1 } parts && parts[0] == "en-us" && parts[1] == tag)
                                {
                                    string fileName = Path.Combine(outFolder, $"{export.ObjectNameString}.wav");
                                    using var fs = new FileStream(fileName, FileMode.Create);
                                    Stream wavStream = export.GetBinaryData<WwiseStream>().CreateWaveStream();
                                    wavStream.SeekBegin();
                                    wavStream.CopyTo(fs);
                                }
                            }
                        }
                    }
                }).ContinueWithOnUIThread(prevTask =>
                {
                    packEd.IsBusy = false;
                    MessageBox.Show("Done");
                });
            }
            
        }
    }
}
