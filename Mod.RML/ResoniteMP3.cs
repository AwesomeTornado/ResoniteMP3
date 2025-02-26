using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using MPOHeaderReader;
using NAudio.FileFormats.Mp3;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio;
using ResoniteModLoader;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static FrooxEngine.CubemapCreator;
using static FrooxEngine.Projection360Material;
using System.Runtime.CompilerServices;
using System;

namespace resoniteMPThree
{
    public class ResoniteMP3 : ResoniteMod
    {
        public override string Name => "ResoniteMP3";
        public override string Author => "__Choco__";
        public override string Version => "1.1.0"; //Version of the mod, should match the AssemblyVersion
        public override string Link => "https://github.com/AwesomeTornado/ResoniteMP3";


        //The following
        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> enabled = new ModConfigurationKey<bool>("enabled", "Should the mod be enabled", () => true); //Optional config settings

        private static ModConfiguration Config;//If you use config settings, this will be where you interface with them

        public bool Enabled => Config.GetValue(enabled);

        public override void OnEngineInit()
        {
            Config = GetConfiguration(); //Get this mods' current ModConfiguration
            Config.Save(true); //If you'd like to save the default config values to file

            Harmony harmony = new Harmony("com.__Choco__.ResoniteMP3");

            harmony.Patch(AccessTools.Method(typeof(AssetHelper), "ClassifyExtension"), postfix: AccessTools.Method(typeof(PatchMethods), "FixExtensionMapping"));
            harmony.Patch(AccessTools.Method(typeof(UniversalImporter), "ImportTask"), prefix: AccessTools.Method(typeof(PatchMethods), "ConvertMP3BeforeLoad"));
            harmony.Patch(AccessTools.Method(typeof(UniversalImporter), "ImportTask"), postfix: AccessTools.Method(typeof(PatchMethods), "DeleteTempFiles"));
            harmony.PatchAll();

            Msg("ResoniteMP3 loaded.");
        }

        public class PatchMethods
        {

            public static void FixExtensionMapping(ref AssetClass __result, string ext)
            {
                if (__result != AssetClass.Video)
                {
                    return;
                }
                if (ext == "mp3")
                {
                    Msg("remapped mp3 from video to audio.");
                    __result = AssetClass.Audio;
                }
            }

            public static string Mp3ToWav(string mp3File)
            {
                string fileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".wav";
                if (File.Exists(fileName))
                {
                    Error("This error message is incredibly unlikely.");
                    Error("You have somehow generated a temp file that conflicts with another pre existing temp file.");
                    Error("Exiting ResoniteMP3 patch function...");
                    return mp3File;
                }
                
                using (var reader = new Mp3FileReader(mp3File))
                {
                    WaveFileWriter.CreateWaveFile(fileName, reader);
                }

                return fileName;
            }

            private static bool ConvertMP3BeforeLoad(out List<string> __state, AssetClass assetClass, ref IEnumerable<string> files, World world, float3 position, floatQ rotation, float3 scale, bool silent = false)
            {
                __state = new List<string>();

                if (assetClass != AssetClass.Audio)
                {
                    return true;
                }
                
                List<string> files2 = new List<string>();
                
                foreach (string file in files)
                {
                    if (Path.GetExtension(file) == ".mp3")
                    {
                        string newPath = Mp3ToWav(file);
                        Msg("Creating temp file: " + newPath);
                        files2.Add(newPath);
                        __state.Add(newPath);
                    }
                    else
                    {
                        files2.Add(file);
                    }
                }
                files = files2;
                return true;
            }

            private static void DeleteTempFiles(List<string> __state)
            {
                foreach (string file in __state)
                {
                    if (File.Exists(file))
                    {
                        Msg("Deleting temp file: " + file);
                        File.Delete(file);
                    }
                }
            }
        }
    }
}
