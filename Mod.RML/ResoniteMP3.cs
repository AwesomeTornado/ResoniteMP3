using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using NAudio.Wave;
using ResoniteModLoader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static FrooxEngine.CubemapCreator;
using static FrooxEngine.Projection360Material;
using System.Runtime.CompilerServices;
using System;
using System.Runtime.Remoting.Messaging;

namespace resoniteMPThree
{
    public class ResoniteMP3 : ResoniteMod
    {
        public override string Name => "ResoniteMP3";
        public override string Author => "__Choco__";
        public override string Version => "2.1.0"; //Version of the mod, should match the AssemblyVersion
        public override string Link => "https://github.com/AwesomeTornado/ResoniteMP3";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("com.__Choco__.ResoniteMP3");

            harmony.Patch(AccessTools.Method(typeof(AssetHelper), "ClassifyExtension"), postfix: AccessTools.Method(typeof(PatchMethods), "FixExtensionMapping"));
            harmony.Patch(AccessTools.Method(typeof(UniversalImporter), "ImportTask"), prefix: AccessTools.Method(typeof(PatchMethods), "ConvertMP3BeforeLoad"));
            harmony.PatchAll();

            clearTempFiles();
            string tempDirectory = Path.GetTempPath() + "ResoniteMP3" + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(tempDirectory);

            Msg("ResoniteMP3 loaded.");
        }

        private void clearTempFiles()
        {
            string tempDirectory = Path.GetTempPath() + "ResoniteMP3" + Path.DirectorySeparatorChar;
            if (Directory.Exists(tempDirectory))
            {
                var directories = Directory.EnumerateDirectories(tempDirectory);
                foreach (string directory in directories)
                {
                    if (Directory.Exists(directory))
                    {
                        Msg("Deleting temp directory: " + directory);
                        Directory.Delete(directory, true);
                    }
                }
            }
        }

        public class PatchMethods
        {

            public static void FixExtensionMapping(ref AssetClass __result, string ext)
            {
                if (__result != AssetClass.Video)
                {
                    return;
                }
                if (ext == ".mp3")
                {
                    Msg("remapped mp3 from video to audio.");
                    __result = AssetClass.Audio;
                }
            }

            public static string Mp3ToWav(string mp3File)
            {
                string fileName = Path.GetTempPath() + "ResoniteMP3" + Path.DirectorySeparatorChar + Guid.NewGuid().ToString() + Path.DirectorySeparatorChar;
                Msg("Creating temp folder: " + fileName);
                Directory.CreateDirectory(fileName);
                fileName += Path.GetFileNameWithoutExtension(mp3File) + ".wav";
                Msg("Creating temp file: " + fileName);
                if (File.Exists(fileName))
                {
                    Error("This error message is astronomically unlikely, but still technically possible.");
                    Error("You have somehow generated a temp folder that conflicts with another pre existing temp folder.");
                    Error("Exiting ResoniteMP3 patch function...");
                    return mp3File;
                }
                
                using (var reader = new Mp3FileReader(mp3File))
                {
                    WaveFileWriter.CreateWaveFile(fileName, reader);
                }

                return fileName;
            }

            private static bool ConvertMP3BeforeLoad(AssetClass assetClass, ref IEnumerable<string> files, World world, float3 position, floatQ rotation, float3 scale, bool silent = false)
            {
                if (assetClass != AssetClass.Audio)
                {
                    return true;
                }
                
                List<string> files2 = new List<string>();
                
                foreach (string file in files)
                {
                    if (Path.GetExtension(file) == ".mp3")
                    {
                        Msg("Discovered mp3 file in import");
                        string newPath = Mp3ToWav(file);
                        Msg("Creating temp folder and file: " + newPath);
                        files2.Add(newPath);
                    }
                    else
                    {
                        files2.Add(file);
                    }
                }
                files = files2;
                return true;
            }
        }
    }
}
