using HarmonyLib;
using System;
using System.Reflection;
using Vintagestory.ServerMods;

namespace VintageLOTR.Patches
{
    [HarmonyPatch]
    public class MapLayerOceansPatch
    {
        private static bool IsSea(int x, int z)
        {
            int brightness = HeightmapReader.GetPixelColor(x, z)[0];
            return brightness < 30;
        }

        // Проверяем, есть ли в этом регионе море
        private static bool HasSeaInRegion(int xCoord, int zCoord, int sizeX, int sizeZ)
        {
            // Проверяем только углы региона для скорости
            // Или можно проверить все пиксели, но это медленнее
            for (int x = 0; x < sizeX; x += 4)
            {
                for (int z = 0; z < sizeZ; z += 4)
                {
                    int nx = xCoord + x;
                    int nz = zCoord + z;
                    if (IsSea(nx, nz))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [HarmonyPatch]
        [HarmonyPrefix]
        static bool Prefix(object __instance, int xCoord, int zCoord, int sizeX, int sizeZ, ref int[] __result)
        {
            try
            {
                // Проверяем: есть ли в этом регионе море?
                bool hasSea = HasSeaInRegion(xCoord, zCoord, sizeX, sizeZ);

                // Если в регионе НЕТ моря - пропускаем (используем стандартную генерацию)
                if (!hasSea)
                {
                    return true; // Пропускаем, используем оригинальный метод
                }

                // Создаем массив результатов
                __result = new int[sizeX * sizeZ];

                // Получаем приватное поле noiseOcean через рефлексию
                var noiseOceanField = __instance.GetType().GetField("noiseOcean",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (noiseOceanField == null)
                {
                    Console.WriteLine("[SeaGen] ERROR: noiseOcean field not found!");
                    return true;
                }

                var noiseOcean = noiseOceanField.GetValue(__instance);
                var getOceanIndexMethod = noiseOcean.GetType().GetMethod("GetOceanIndexAt",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                for (int x = 0; x < sizeX; x++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int nx = xCoord + x;
                        int nz = zCoord + z;

                        // Используем твою карту
                        if (IsSea(nx, nz))
                        {
                            __result[z * sizeX + x] = 255; // Океан
                        }
                        else
                        {
                            __result[z * sizeX + x] = 0; // Суша
                        }
                    }
                }

                Console.WriteLine($"[SeaGen] MapLayerOceans patched for region with sea at ({xCoord}, {zCoord})");
                return false; // Пропускаем оригинальный метод
            }
            catch (Exception e)
            {
                Console.WriteLine($"[SeaGen] ERROR in MapLayerOceans patch: {e.Message}");
                return true;
            }
        }

        static MethodBase TargetMethod()
        {
            // Ищем MapLayerOceans тип
            Type mapLayerOceansType = null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType("Vintagestory.ServerMods.MapLayerOceans");
                    if (type != null)
                    {
                        mapLayerOceansType = type;
                        break;
                    }
                }
                catch { }
            }

            if (mapLayerOceansType == null)
            {
                Console.WriteLine("[SeaGen] ERROR: MapLayerOceans type not found!");
                return null;
            }

            var method = mapLayerOceansType.GetMethod("GenLayer",
                BindingFlags.Public | BindingFlags.Instance);

            if (method == null)
            {
                Console.WriteLine("[SeaGen] ERROR: MapLayerOceans.GenLayer method not found!");
                return null;
            }

            Console.WriteLine("[SeaGen] Found MapLayerOceans.GenLayer!");
            return method;
        }
    }
}