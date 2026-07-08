using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace VintageLOTR.worldgen
{
    public static class ChunkGen
    {
        public static IntDataMap2D CreateUniformMap(ICoreServerAPI api, IMapRegion mapRegion, int regionX, int regionZ)
        {
            IntDataMap2D originalMap = mapRegion.LandformMap;
            int regionSize = api.WorldManager.RegionSize;
            //    InnerSize — это размер карты в пикселях (без отступов)
            int mapInnerSize = originalMap.InnerSize;
            float scale = (float)regionSize / mapInnerSize;

            // 4. Координаты левого нижнего угла региона в блоках
            int regionBlockX = regionX * regionSize;
            int regionBlockZ = regionZ * regionSize;

            // 5. Создаём пустой объект и копируем размеры
            IntDataMap2D customMap = IntDataMap2D.CreateEmpty();
            customMap.Size = originalMap.Size;
            customMap.TopLeftPadding = originalMap.TopLeftPadding;
            customMap.BottomRightPadding = originalMap.BottomRightPadding;



            // 6. Создаём массивы данных нужного размера
            int totalSize = customMap.Size * customMap.Size;
            customMap.Data = new int[totalSize];

            // 7. Заполняем КАЖДЫЙ пиксель карты своим индексом
            //    Параметр "padding" — это пустая рамка вокруг полезной области
            for (int pixelX = 0; pixelX < customMap.Size; pixelX++)
            {
                for (int pixelZ = 0; pixelZ < customMap.Size; pixelZ++)
                {
                    // Проверяем: находится ли пиксель в полезной области (без отступов)
                    if (pixelX < customMap.TopLeftPadding ||
                        pixelX >= customMap.Size - customMap.BottomRightPadding ||
                        pixelZ < customMap.TopLeftPadding ||
                        pixelZ >= customMap.Size - customMap.BottomRightPadding)
                    {
                        // Это padding-пиксель — заполняем нулём или индексом соседа
                        customMap.Data[pixelZ * customMap.Size + pixelX] = 0;
                        continue;
                    }

                    // Переводим пиксель карты в координаты блока мира
                    // Вычитаем отступ, чтобы получить координаты в полезной области (0..InnerSize)
                    int unpaddedX = pixelX - customMap.TopLeftPadding;
                    int unpaddedZ = pixelZ - customMap.TopLeftPadding;

                    // Переводим в координаты блока
                    float blockX = regionBlockX + (unpaddedX + 0.5f) * scale;
                    float blockZ = regionBlockZ + (unpaddedZ + 0.5f) * scale;

                    int localChunkX = unpaddedX;  // уже от 0 до 31
                    int localChunkZ = unpaddedZ;

                    int globalChunkX = regionX * 16 + localChunkX;
                    int globalChunkZ = regionZ * 16 + localChunkZ;

                    // Получаем индекс ландшафта по координатам блока
                    LOTRMap mapReader = new LOTRMap();
                    int landformIndex = mapReader.GetIndexAt(globalChunkX, globalChunkZ);



                    // Записываем в массив
                    customMap.Data[pixelZ * customMap.Size + pixelX] = landformIndex;
                    //api.Logger.Notification($"Для чанка с координатами {pixelX},{pixelZ} присвоен индекс ландшафта {landformIndex}");
                }
            }

            api.Logger.Notification($"[VintageLOTR] Создана карта ландшафта для региона ({regionX}, {regionZ}), масштаб 1 пиксель = {scale:F2} блоков");

            return customMap;
        }
    }
}