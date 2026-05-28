using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace VintageLOTR
{
    /// <summary>
    /// Класс, отвечающий за генерацию мира по карте высот
    /// </summary>
    public class HeightmapWorldGen
    {
        // === ПАРАМЕТРЫ НАСТРОЙКИ (меняй здесь) ===
        private int minHeight = 40;        // Уровень моря (чёрный цвет)
        private int maxHeight = 200;       // Высота гор (белый цвет)
        private int scaleFactor = 50;      // 1 пиксель = 50 блоков

        // === ВНУТРЕННИЕ ПЕРЕМЕННЫЕ ===
        private ICoreServerAPI serverApi;
        private IWorldGenBlockAccessor worldGenAccessor;
        private float[,] heightmapData;
        private int mapWidth;
        private int mapHeight;
        private bool isInitialized = false;

        /// <summary>
        /// Конструктор - получает API при создании
        /// </summary>
        public HeightmapWorldGen(ICoreServerAPI api)
        {
            serverApi = api;
        }

        /// <summary>
        /// Инициализация генератора (вызывается один раз при старте)
        /// </summary>
        public void Initialize()
        {
            LoadHeightmap();
            if (heightmapData != null)
            {
                isInitialized = true;
                serverApi.Server.Logger.Event($"[VintageLOTR] Генератор инициализирован. Карта: {mapWidth}x{mapHeight}");
            }
            else
            {
                serverApi.Server.Logger.Error("[VintageLOTR] Не удалось инициализировать генератор!");
            }
        }

        /// <summary>
        /// Установка доступа к генератору блоков (вызывается игрой)
        /// </summary>
        public void SetWorldGenAccessor(IChunkProviderThread chunkProvider)
        {
            worldGenAccessor = chunkProvider.GetBlockAccessor(true);
            serverApi.Server.Logger.Event("[VintageLOTR] WorldGenAccessor получен!");
        }

        /// <summary>
        /// ПУНКТ 1: Загрузка картинки из ресурсов (без SkiaSharp)
        /// </summary>
        private void LoadHeightmap()
        {
            try
            {
                AssetLocation texturePath = new AssetLocation("vintagelotr", "textures/heightmap.png");
                IAsset asset = serverApi.Assets.Get(texturePath);

                if (asset == null)
                {
                    serverApi.Server.Logger.Error("[VintageLOTR] Файл карты не найден: assets/vintagelotr/textures/heightmap.png");
                    CreateFallbackHeightmap();
                    return;
                }

                // Получаем данные картинки
                byte[] data = asset.Data;

                // Пытаемся прочитать размеры из PNG заголовка
                if (data.Length >= 29)
                {
                    // Проверяем сигнатуру PNG
                    if (data[0] == 137 && data[1] == 80 && data[2] == 78 && data[3] == 71)
                    {
                        // Читаем ширину и высоту из IHDR чанка (байты 16-23)
                        mapWidth = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
                        mapHeight = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
                    }
                    else
                    {
                        mapWidth = 500;
                        mapHeight = 500;
                    }
                }
                else
                {
                    mapWidth = 500;
                    mapHeight = 500;
                }

                // Ограничиваем размер, чтобы не было слишком много памяти
                if (mapWidth > 2000) mapWidth = 2000;
                if (mapHeight > 2000) mapHeight = 2000;

                heightmapData = new float[mapWidth, mapHeight];

                // Заполняем тестовым ландшафтом (холмы и горы)
                // Это временное решение, чтобы проверить, работает ли генерация
                Random rand = new Random();
                for (int x = 0; x < mapWidth; x++)
                {
                    for (int z = 0; z < mapHeight; z++)
                    {
                        // Создаём плавные холмы с помощью синуса
                        float value = (float)(Math.Sin(x * 0.05) * Math.Cos(z * 0.05) + 1) / 2f;
                        // Добавляем немного случайного шума для естественности
                        value = value * 0.8f + (float)rand.NextDouble() * 0.2f;
                        // Ограничиваем значения
                        if (value < 0) value = 0;
                        if (value > 1) value = 1;
                        heightmapData[x, z] = value;
                    }
                }

                serverApi.Server.Logger.Event($"[VintageLOTR] Тестовая карта создана! Размер: {mapWidth}x{mapHeight}");
                serverApi.Server.Logger.Event($"[VintageLOTR] Диапазон высот: {minHeight}-{maxHeight}, масштаб: 1 пиксель = {scaleFactor} блоков");
            }
            catch (Exception e)
            {
                serverApi.Server.Logger.Error($"[VintageLOTR] Ошибка загрузки карты: {e.Message}");
                CreateFallbackHeightmap();
            }
        }

        /// <summary>
        /// Создание заглушки, если картинка не загрузилась
        /// </summary>
        private void CreateFallbackHeightmap()
        {
            mapWidth = 100;
            mapHeight = 100;
            heightmapData = new float[mapWidth, mapHeight];

            Random rand = new Random();
            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    heightmapData[x, z] = (float)rand.NextDouble() * 0.5f + 0.25f;
                }
            }

            serverApi.Server.Logger.Warning("[VintageLOTR] Используется заглушка вместо карты высот");
        }

        /// <summary>
        /// ПУНКТ 2: Получение высоты по координатам мира (с интерполяцией)
        /// </summary>
        private int GetHeightAt(int worldX, int worldZ)
        {
            if (!isInitialized || heightmapData == null) return minHeight;

            // Дробные координаты на карте
            float mapX = worldX / (float)scaleFactor;
            float mapZ = worldZ / (float)scaleFactor;

            // Целые координаты четырёх углов
            int x1 = (int)Math.Floor(mapX);
            int z1 = (int)Math.Floor(mapZ);
            int x2 = x1 + 1;
            int z2 = z1 + 1;

            // Фракции внутри квадрата (0..1)
            float fx = mapX - x1;
            float fz = mapZ - z1;

            // Защита от выхода за границы (зацикливание)
            x1 = ((x1 % mapWidth) + mapWidth) % mapWidth;
            x2 = ((x2 % mapWidth) + mapWidth) % mapWidth;
            z1 = ((z1 % mapHeight) + mapHeight) % mapHeight;
            z2 = ((z2 % mapHeight) + mapHeight) % mapHeight;

            // Четыре высоты углов
            float h11 = heightmapData[x1, z1];
            float h21 = heightmapData[x2, z1];
            float h12 = heightmapData[x1, z2];
            float h22 = heightmapData[x2, z2];

            // Двойная линейная интерполяция
            float top = h11 * (1 - fx) + h21 * fx;
            float bottom = h12 * (1 - fx) + h22 * fx;
            float normalizedHeight = top * (1 - fz) + bottom * fz;

            // Превращаем в высоту блоков
            int height = minHeight + (int)(normalizedHeight * (maxHeight - minHeight));

            // Ограничиваем высоту
            if (height < 0) height = 0;
            if (height > 255) height = 255;

            return height;
        }

        /// <summary>
        /// ПУНКТ 3: Заполнение столбца блоками от дна до вершины
        /// </summary>
        private void GenerateColumn(int worldX, int worldZ, int terrainHeight)
        {
            if (worldGenAccessor == null) return;

            for (int y = 0; y < terrainHeight && y < 256; y++)
            {
                BlockPos pos = new BlockPos(worldX, y, worldZ);
                Block blockToPlace;

                if (y == terrainHeight - 1)
                {
                    blockToPlace = serverApi.World.GetBlock(new AssetLocation("game", "grass-nofall"));
                }
                else if (y >= terrainHeight - 5)
                {
                    blockToPlace = serverApi.World.GetBlock(new AssetLocation("game", "soil"));
                }
                else
                {
                    blockToPlace = serverApi.World.GetBlock(new AssetLocation("game", "stone-granite"));
                }

                if (blockToPlace != null)
                {
                    worldGenAccessor.SetBlock(blockToPlace.BlockId, pos);
                }
            }
        }

        /// <summary>
        /// ПУНКТ 4: Заливка водой ниже уровня моря
        /// </summary>
        private void FillWater(int worldX, int worldZ, int terrainHeight)
        {
            if (worldGenAccessor == null) return;

            if (terrainHeight < minHeight)
            {
                for (int y = terrainHeight; y < minHeight && y < 256; y++)
                {
                    BlockPos pos = new BlockPos(worldX, y, worldZ);
                    Block waterBlock = serverApi.World.GetBlock(new AssetLocation("game", "water-still-7"));
                    if (waterBlock != null)
                    {
                        worldGenAccessor.SetBlock(waterBlock.BlockId, pos);
                    }
                }
            }
        }

        /// <summary>
        /// ПУНКТ 5: Генерация целого чанка (вызывается игрой для каждого чанка)
        /// </summary>
        public void OnChunkColumnGen(IChunkColumnGenerateRequest request)
        {
            if (!isInitialized || worldGenAccessor == null) return;

            int chunkX = request.ChunkX;
            int chunkZ = request.ChunkZ;
            int baseBlockX = chunkX * GlobalConstants.ChunkSize;
            int baseBlockZ = chunkZ * GlobalConstants.ChunkSize;

            for (int dx = 0; dx < GlobalConstants.ChunkSize; dx++)
            {
                for (int dz = 0; dz < GlobalConstants.ChunkSize; dz++)
                {
                    int worldX = baseBlockX + dx;
                    int worldZ = baseBlockZ + dz;
                    int terrainHeight = GetHeightAt(worldX, worldZ);

                    GenerateColumn(worldX, worldZ, terrainHeight);
                    FillWater(worldX, worldZ, terrainHeight);
                }
            }
        }

        /// <summary>
        /// Проверка, готов ли генератор к работе
        /// </summary>
        public bool IsReady()
        {
            return isInitialized && worldGenAccessor != null;
        }

        /// <summary>
        /// Обновление параметров генерации (можно вызвать из конфига)
        /// </summary>
        public void UpdateParameters(int newMinHeight, int newMaxHeight, int newScaleFactor)
        {
            minHeight = newMinHeight;
            maxHeight = newMaxHeight;
            scaleFactor = newScaleFactor;
            serverApi.Server.Logger.Event($"[VintageLOTR] Параметры обновлены: высоты {minHeight}-{maxHeight}, масштаб {scaleFactor}");
        }
    }
}