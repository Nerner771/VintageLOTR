using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

public class HeightmapData
{
    public int width;
    public int height;
    public int[] data;
    
}

public static class HeightmapReader
{
    static private HeightmapData heightmap;
    static private int minX = 0;
    static private int minZ = 0;
    static private int maxX;
    static private int maxZ;
    public static int width;
    public static int height;


    public static void LoadHeightmap(ICoreServerAPI api)
    {
        AssetLocation location = new AssetLocation("vintagelotr", "textures/white_circle2.json");
        IAsset asset = api.Assets.Get(location);


        if (asset == null)
        {
            api.Logger.Error("Файл НЕ НАЙДЕН: textures/testmap.json");
            return;
        }

        api.Logger.Notification("Файл найден! Размер: {0} байт", asset.Data.Length);

        string jsonText = asset.ToText();
        heightmap = JsonConvert.DeserializeObject<HeightmapData>(jsonText);

        if (heightmap == null)
        {
            api.Logger.Error("Не удалось распарсить JSON");
            return;
        }

        maxX = heightmap.width - 1;
        maxZ = heightmap.height - 1;

        width = heightmap.width;
        height = heightmap.height;


        api.Logger.Notification("Карта загружена! {0}x{1}", heightmap.width, heightmap.height);
    }

    public static int[] GetPixelColor(int chunkX, int chunkZ)
    {
        if (heightmap == null)
        {

            System.Console.WriteLine("[GetPixelColor] Карта = null! Возвращаю 0! ");
            return new int[] { 0, 0, 0, 255 };
        }

        if (chunkX < 0 || chunkX >= heightmap.width || chunkZ < 0 || chunkZ >= heightmap.height)
        {

            System.Console.WriteLine("[GetPixelColor] Чанк за границами карты! Возвращаю 0! ");
            return new int[] { 0, 0, 0, 255 };  // за границами — море

        }




        int pixelIndex = (chunkZ * heightmap.width) + chunkX;
        int start = pixelIndex * 4;

        return new int[]
        {
            heightmap.data[start],
            heightmap.data[start + 1],
            heightmap.data[start + 2],
            heightmap.data[start + 3]
        };
    }
}