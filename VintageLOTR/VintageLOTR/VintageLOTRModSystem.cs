using Vintagestory.API.Common;
using Vintagestory.API.Server;
using System.Drawing;

namespace VintageLOTR
{
    public class VintageLOTRModSystem : ModSystem
    {
        private HeightmapWorldGen worldGenerator;

        /// <summary>
        /// Запуск на стороне сервера (мир генерируется здесь)
        /// </summary>
        public override void StartServerSide(ICoreServerAPI api)
        {
            // 1. Создаём экземпляр генератора
            worldGenerator = new HeightmapWorldGen(api);

            // 2. Загружаем карту высот и инициализируем
            worldGenerator.Initialize();

            // 3. Получаем доступ к генератору блоков
            api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);

            // 4. Подписываемся на генерацию чанков
            api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Terrain, "standard");

            api.Server.Logger.Event("[Средиземье] Мод загружен и готов к генерации!");
        }

        /// <summary>
        /// Колбэк получения доступа к генератору блоков
        /// </summary>
        private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
        {
            worldGenerator.SetWorldGenAccessor(chunkProvider);
        }

        /// <summary>
        /// Колбэк генерации чанка (пробрасываем вызов в наш генератор)
        /// </summary>
        private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
        {
            worldGenerator.OnChunkColumnGen(request);
        }
    }
}
