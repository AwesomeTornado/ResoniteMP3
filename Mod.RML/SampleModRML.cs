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

namespace resoniteMP3
{
    /// <summary>
    /// This mod is an implementation based on the example given in https://github.com/resonite-modding-group/ResoniteModLoader/blob/main/doc/making_mods.md.
    /// </summary>
    public class ResoniteMP3 : ResoniteMod
    {
        public override string Name => "ResoniteMP3";
        public override string Author => "__Choco__";
        public override string Version => "3.0.0"; //Version of the mod, should match the AssemblyVersion
        public override string Link => "https://github.com/mpmxyz/ResoniteSampleMod";


        //The following
        [AutoRegisterConfigKey]
        private static readonly ModConfigurationKey<bool> enabled = new ModConfigurationKey<bool>("enabled", "Should the mod be enabled", () => true); //Optional config settings

        private static ModConfiguration Config;//If you use config settings, this will be where you interface with them

        public bool Enabled => Config.GetValue(enabled);

        public override void OnEngineInit()
        {
            Config = GetConfiguration(); //Get this mods' current ModConfiguration
            Config.Save(true); //If you'd like to save the default config values to file

            Harmony harmony = new Harmony("com.github.AwesomeTornado.ResoniteMP3"); //typically a reverse domain name is used here (https://en.wikipedia.org/wiki/Reverse_domain_name_notation)
            //public static AssetClass ClassifyExtension(string ext)
            harmony.Patch(AccessTools.Method(typeof(AssetHelper), "ClassifyExtension"), postfix: AccessTools.Method(typeof(PatchMethods), "FixExtensionMapping"));
            harmony.Patch(AccessTools.Method(typeof(UniversalImporter), "ImportTask"), prefix: AccessTools.Method(typeof(PatchMethods), "ConvertMP3BeforeLoad"));
            //harmony.Patch(AccessTools.Method(typeof(UniversalImporter), "ImportTask"), prefix: AccessTools.Method(typeof(PatchMethods), "ConvertMP3BeforeLoad_2", new Type[] { typeof(string), typeof(World), typeof(float3), typeof(floatQ), typeof(bool), typeof(bool) }));
            harmony.PatchAll(); // do whatever LibHarmony patching you need, this will patch all [HarmonyPatch()] instances

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
            /*
                        public static void ConvertMP3BeforeLoad_1(AssetClass assetClass, IEnumerable<string> files, World world, float3 position, floatQ rotation, bool silent = false)
                        {
                            IEnumerable<string> files2 = files;
                            World world2 = world;
                            world2.Coroutines.StartTask(async delegate
                            {
                                await ImportTask(assetClass, files2, world2, position, rotation, world2.LocalUserGlobalScale, silent);
                            });
                        }

                        public static Task ConvertMP3BeforeLoad_2(string path, World world, float3 position, floatQ rotation, bool silent = false, bool rawFile = false)
                        {
                            string path2 = path;
                            World world2 = world;
                            AssetClass assetClass = ((!rawFile) ? AssetHelper.IdentifyClass(path2) : AssetClass.Unknown);
                            return world2.Coroutines.StartTask(async delegate
                            {
                                await ImportTask(assetClass, new string[1] { path2 }, world2, position, rotation, world2.LocalUserGlobalScale, silent);
                            });
                        }

            */

            public static void Mp3ToWav(string mp3File, string outputFile)
            {
                Msg("input file was " + mp3File);
                Msg("output file is " + mp3File);
                using (Mp3FileReader reader = new Mp3FileReader(mp3File))
                {
                    Msg("Reader initialized");
                    using (WaveStream pcmStream = WaveFormatConversionStream.CreatePcmStream(reader))
                    {
                        Msg("Writer initialized");
                        WaveFileWriter.CreateWaveFile(outputFile, pcmStream);
                        Msg("File should have been written.");
                    }
                }
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
                        Warn("Mp3 attempted load. Load was intercepted.");

                        Mp3ToWav(file, file + ".wav");
                        files2.Add(file + ".wav");
                        Msg("Adding " + file + ".wav to the list of files to load.");
                    }
                }
                Msg("Old file list was: " + files.Join<string>());
                files = files2;
                Msg("\n\nNew file list is: " + files.Join<string>());

                return true;
            }
        }
    }
}
