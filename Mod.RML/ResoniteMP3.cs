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

namespace resoniteMPThree
{
    public class ResoniteMP3 : ResoniteMod
    {
        public override string Name => "ResoniteMP3";
        public override string Author => "__Choco__";
        public override string Version => "1.0.0"; //Version of the mod, should match the AssemblyVersion
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
            harmony.Patch(AccessTools.Method(typeof(UniversalImporter), "ImportTask"), prefix: AccessTools.Method(typeof(PatchMethods), "DeleteTempFiles"));
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

            public static string Mp3ToWav(string mp3File, string outputFile)
            {

                if (File.Exists(outputFile))
                {
                    return outputFile;
                }
                /*
                int retries = 20;
                while (retries > 0 && File.Exists(outputFile))
                {
                    Warn("Output file already exists, trying another name.");
                    outputFile = (10 - retries) + outputFile;
                    retries--;
                }

                if (File.Exists(outputFile))
                {
                    Error("Failed to create output file.");
                    return outputFile;
                }
                */
                using (var reader = new Mp3FileReader(mp3File))
                {
                    WaveFileWriter.CreateWaveFile(outputFile, reader);
                }

                return outputFile;
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
                        string newPath = Mp3ToWav(file, file + ".wav");
                        Msg("Creating temp file: " + newPath);
                        files2.Add(newPath);
                        __state.Add(newPath);
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
                        //Msg("Deleting temp file: " + file);
                        //File.Delete(file);
                        //deleting the file seemed to cause issues... hope its fine if I just leave the file there.
                    }
                }
            }
        }
    }
}
