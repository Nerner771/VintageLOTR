using System;
using Vintagestory.API.Common;

namespace VintageLOTR.worldgen
{
    public class LOTRMap
    {
        static ICoreAPI api;

        public static void Init(ICoreAPI api) {
            LOTRMap.api = api;
        }


   
        public int GetIndexAt(int chunkX, int chunkZ) {

            
            int color = HeightmapReader.GetPixelColor(chunkX, chunkZ)[0];
            int result = 0;

            if (color >= 200)
            {
                result = 18;  //realisticmountains

            }
            else 
            {
                result = 24;
                
            }



            System.Console.WriteLine($"Для участка {chunkX},{chunkZ} был выбран ландформ с индексом {result}, цвет - {color}");
            return result;
        
        }
    }
}
