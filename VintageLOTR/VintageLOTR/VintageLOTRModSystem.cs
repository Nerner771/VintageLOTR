using HarmonyLib;
using System;
using System.Drawing;
using VintageLOTR.worldgen;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using static VintageLOTR.worldgen.LOTRMap;


namespace VintageLOTR
{
    public class VintageLOTRModSystem : ModSystem
    {
        private ICoreServerAPI _api;
        private Harmony _harmony;

        public override void StartServerSide(ICoreServerAPI api)
        {

            base.StartServerSide(api);
            _api = api;
            HeightmapReader.LoadHeightmap(api);
       


            _harmony = new Harmony(Mod.Info.ModID);
            _harmony.PatchAll();
            // Регистрируемся на событие генерации региона
            api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
            api.Logger.Notification("[VintageLOTR] Патч успешно применён! Значение высоты для всех блоков - ");

        
        }

        private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams)
        {
            // Создаём свою карту с одинаковым индексом
            IntDataMap2D customMap = ChunkGen.CreateUniformMap(_api, mapRegion, regionX, regionZ);

            // Подменяем стандартную карту ландшафта
            mapRegion.LandformMap = customMap;

            System.Console.WriteLine($"[VintageLOTR] Регион ({regionX}, {regionZ}) — сгенерирован");
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
