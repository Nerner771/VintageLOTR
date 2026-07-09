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
        private static ICoreServerAPI _api;
        private Harmony _harmony;
        public static LOTRMap MapReaderInstance { get; private set; }

        public override void StartServerSide(ICoreServerAPI api)
        {

            base.StartServerSide(api);
            _api = api;
            HeightmapReader.LoadHeightmap(api);
            MapReaderInstance = new LOTRMap();



            _harmony = new Harmony(Mod.Info.ModID);
            _harmony.PatchAll();
            // Регистрируемся на событие генерации региона

            api.Logger.Notification("[VintageLOTR] Патч успешно применён! Значение высоты для всех блоков - ");


        }

        public static IntDataMap2D GetCustomLandformMap(IMapRegion mapRegion, int regionX, int regionZ)
        {
            // Вызываем ваш рабочий метод (переименуйте ChunkGen под вашу структуру, если нужно)
            return ChunkGen.CreateUniformMap(_api, mapRegion, regionX, regionZ);
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
