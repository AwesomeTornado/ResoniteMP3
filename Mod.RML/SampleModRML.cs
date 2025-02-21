using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
            harmony.Patch(AccessTools.Method(typeof(UniversalImporter), "ImportTask"), postfix: AccessTools.Method(typeof(PatchMethods), "ConvertMP3BeforeLoad"));
            harmony.PatchAll(); // do whatever LibHarmony patching you need, this will patch all [HarmonyPatch()] instances

            //Various log methods provided by the mod loader, below is an example of how they will look
            //3:14:42 AM.069 ( -1 FPS)  [INFO] [ResoniteModLoader/SampleMod] a regular log
            Debug("a debug log");
            Msg("a regular log");
            Warn("a warn log");
            Error("an error log");
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

            private static bool ConvertMP3BeforeLoad(AssetClass assetClass, IEnumerable<string> files, World world, float3 position, floatQ rotation, float3 scale, bool silent = false)
            {
                if (assetClass != AssetClass.Audio)
                {
                    return true;
                }
                
                foreach (string file in files)
                {
                    if (Path.GetExtension(file) == ".mp3")
                    {
                        Warn("Mp3 attempted load. Load was intercepted, and stopped.");
                    }
                }

                return true;
            }
        }
    }
}
