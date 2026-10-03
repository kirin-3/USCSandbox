using AssetsTools.NET;
using AssetsTools.NET.Extra;
using USCSandbox.Processor;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("USCS [bundle path] [assets path] [shader path id] <--platform> <--version> <--all>");
                Console.WriteLine("  [bundle path (or \"null\" for no bundle)]");
                Console.WriteLine("  [assets path (or file name in bundle)]");
                Console.WriteLine("  [shader path id (or --all to load all shaders)]");
                Console.WriteLine("  --platform <[d3d11, Switch] (or skip this arg for d3d11)>");
                Console.WriteLine("  --version <unity version override>");
                Console.WriteLine("  --skip <file of shader names, one per line, not to decompile>");
                Console.WriteLine("  --only <file of shader path ids, one per line; with --all, decompile only these>");
                return;
            }

            var manager = new AssetsManager();
            AssetsFileInstance afileInst;

            GPUPlatform platform = GPUPlatform.d3d11;
            UnityVersion? ver = null;
            bool allSet = false;
            HashSet<string> skip = [];
            HashSet<long>? only = null;

            List<string> argList = [];
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith("--"))
                {
                    switch (arg)
                    {
                        case "--platform":
                            platform = Enum.Parse<GPUPlatform>(args[++i]);
                            break;
                        case "--version":
                            ver = UnityVersion.Parse(args[++i]);
                            break;
                        case "--all":
                            allSet = true;
                            break;
                        case "--skip":
                            // Newline-separated shader names another source provides.
                            skip = [.. File.ReadAllLines(args[++i]).Select(line => line.Trim()).Where(line => line.Length > 0)];
                            break;
                        case "--only":
                            // Newline-separated path ids: one process's share of the shaders.
                            only = [.. File.ReadAllLines(args[++i]).Select(line => line.Trim()).Where(line => line.Length > 0).Select(long.Parse)];
                            break;
                        default:
                            Console.WriteLine($"Optional argmuent {arg} is invalid.");
                            return;
                    }
                }
                else
                {
                    argList.Add(arg);
                }
            }

            var bundlePath = argList[0];
            if (argList.Count == 1)
            {
                var bundleFile = manager.LoadBundleFile(bundlePath, true);
                var dirInfs = bundleFile.file.BlockAndDirInfo.DirectoryInfos;
                Console.WriteLine("Available files in bundle:");
                foreach (var dirInf in dirInfs)
                {
                    if ((dirInf.Flags & 4) == 0)
                        continue;

                    Console.WriteLine($"  {dirInf.Name}");
                }
                return;
            }

            var assetsFileName = argList[1];
            if (argList.Count == 2 && !allSet)
            {
                if (bundlePath != "null")
                {
                    var bundleFile = manager.LoadBundleFile(bundlePath, true);
                    afileInst = manager.LoadAssetsFileFromBundle(bundleFile, assetsFileName);

                    manager.LoadClassPackage("classdata.tpk");
                    manager.LoadClassDatabaseFromPackage(bundleFile.file.Header.EngineVersion);

                    Console.WriteLine("Available shaders in bundle:");
                }
                else
                {
                    afileInst = manager.LoadAssetsFile(assetsFileName);

                    manager.LoadClassPackage("classdata.tpk");
                    manager.LoadClassDatabaseFromPackage(afileInst.file.Metadata.UnityVersion);

                    Console.WriteLine("Available shaders in assets file:");
                }

                foreach (var shaderInf in afileInst.file.GetAssetsOfType(AssetClassID.Shader))
                {
                    var tmpShaderBf = manager.GetBaseField(afileInst, shaderInf);
                    var tmpShaderName = tmpShaderBf["m_ParsedForm"]["m_Name"].AsString;
                    Console.WriteLine($"  {tmpShaderName} (path id {shaderInf.PathId})");
                }
                return;
            }

            long shaderPathId = 0;
            if (argList.Count > 2)
                shaderPathId = long.Parse(argList[2]);

            Dictionary<long, string> files = [];
            if (bundlePath != "null")
            {
                var bundleFile = manager.LoadBundleFile(bundlePath, true);
                afileInst = manager.LoadAssetsFileFromBundle(bundleFile, assetsFileName);

                if (ver is null)
                {
                    var verStr = bundleFile.file.Header.EngineVersion;
                    if (verStr != "0.0.0")
                    {
                        var fixedVerStr = new AssetsTools.NET.Extra.UnityVersion(verStr).ToString();
                        ver = UnityVersion.Parse(fixedVerStr);
                    }
                }
            }
            else
            {
                afileInst = manager.LoadAssetsFile(assetsFileName);

                if (ver is null)
                {
                    var verStr = afileInst.file.Metadata.UnityVersion;
                    if (verStr != "0.0.0")
                    {
                        var fixedVerStr = new AssetsTools.NET.Extra.UnityVersion(verStr).ToString();
                        ver = UnityVersion.Parse(fixedVerStr);
                    }
                }
            }

            if (ver is null)
            {
                Console.WriteLine("File version was stripped. Please set --version flag.");
                return;
            }

            manager.LoadClassPackage("classdata.tpk");
            manager.LoadClassDatabaseFromPackage(ver.ToString());

            var shadersToLoad = new List<AssetFileInfo>();
            if (shaderPathId != 0)
                shadersToLoad.Add(afileInst.file.GetAssetInfo(shaderPathId));
            else
                shadersToLoad.AddRange(afileInst.file.GetAssetsOfType(AssetClassID.Shader)
                    .Where(info => only is null || only.Contains(info.PathId)));

            foreach (var shaderInf in shadersToLoad)
            {
                var shaderBf = manager.GetBaseField(afileInst, shaderInf);
                if (shaderBf == null)
                {
                    Console.WriteLine("Shader asset not found or couldn't be read.");
                    continue;
                }

                var shaderName = shaderBf["m_ParsedForm"]["m_Name"].AsString;
                if (skip.Contains(shaderName))
                {
                    Console.WriteLine($"{shaderName} skipped");
                    continue;
                }
                // One bad shader must not end the run for the rest of the file.
                try
                {
                    var shaderProcessor = new ShaderProcessor(shaderBf, ver.Value, platform);
                    string shaderText = shaderProcessor.Process();

                    Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "out", Path.GetDirectoryName(shaderName)!));
                    File.WriteAllText($"{Path.Combine(Environment.CurrentDirectory, "out", shaderName)}.shader", shaderText);
                    Console.WriteLine($"{shaderName} decompiled");
                }
                catch (Exception error)
                {
                    Console.WriteLine($"{shaderName} failed: {error.GetType().Name}: {error.Message.ReplaceLineEndings(" ")}");
                }
            }
        }
    }
}