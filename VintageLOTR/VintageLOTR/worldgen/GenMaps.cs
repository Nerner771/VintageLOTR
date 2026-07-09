using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.ServerMods;

namespace VintageLOTR.worldgen
{
    [HarmonyPatch(typeof(GenMaps))]
    [HarmonyPatch("OnMapRegionGen")]
    public class Patch_GenMaps_OnMapRegionGen
    {
        [HarmonyPostfix]
        public static void Postfix(GenMaps __instance, IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams)
        {
            try
            {

                if (VintageLOTRModSystem.MapReaderInstance == null) return;


                int mapSize = mapRegion.LandformMap?.Size ?? 32;

                int[] customData = new int[mapSize * mapSize];


                float scale = 16f / (mapSize - 1);

                for (int z = 0; z < mapSize; z++)
                {
                    for (int x = 0; x < mapSize; x++)
                    {

                        float localChunkX = x * scale;
                        float localChunkZ = z * scale;


                        int globalChunkX = (regionX * 16) + (int)Math.Round(localChunkX);
                        int globalChunkZ = (regionZ * 16) + (int)Math.Round(localChunkZ);

                        if (x == mapSize - 1) globalChunkX = (regionX + 1) * 16;
                        if (z == mapSize - 1) globalChunkZ = (regionZ + 1) * 16;

                        // Записываем индекс ландформа из вашего LOTRMap
                        customData[z * mapSize + x] = VintageLOTRModSystem.MapReaderInstance.GetIndexAt(globalChunkX, globalChunkZ);
                    }
                }


                if (mapRegion.LandformMap != null)
                {
                    mapRegion.LandformMap.Data = customData;
                }
                else
                {
                    mapRegion.LandformMap = new IntDataMap2D { Data = customData, Size = mapSize };
                }

                System.Console.WriteLine($"[LOTR-MOD] Карта региона [{regionX}, {regionZ}] успешно подменена вашим рельефом!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("[LOTR-MOD-ERROR] Ошибка в Postfix: " + ex.Message);
            }
        }
    }
}
