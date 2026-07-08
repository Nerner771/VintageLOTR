using System;
using System.Reflection;
using Vintagestory.API.MathTools;

namespace VintageLOTR.worldgen
{
    public static class ColumnNoiseHelper
    {
        private static FieldInfo _orderedOctaveEntriesField;
        private static FieldInfo _xField;
        private static FieldInfo _zField;

        static ColumnNoiseHelper()
        {
            // Получаем поле orderedOctaveEntries
            _orderedOctaveEntriesField = typeof(NewNormalizedSimplexFractalNoise.ColumnNoise)
                .GetField("orderedOctaveEntries", BindingFlags.NonPublic | BindingFlags.Instance);

            // Получаем поля X и Z из структуры OctaveEntry
            Type octaveEntryType = typeof(NewNormalizedSimplexFractalNoise.ColumnNoise)
                .GetNestedType("OctaveEntry", BindingFlags.NonPublic);

            _xField = octaveEntryType.GetField("X", BindingFlags.Public | BindingFlags.Instance);
            _zField = octaveEntryType.GetField("Z", BindingFlags.Public | BindingFlags.Instance);
        }

        public static (int X, int Z) GetCoordinates(NewNormalizedSimplexFractalNoise.ColumnNoise column)
        {
            // Получаем массив orderedOctaveEntries
            var entries = _orderedOctaveEntriesField.GetValue(column) as Array;
            if (entries == null || entries.Length == 0)
                return (-1, -1);

            // Берём первую октаву (индекс 0)
            var firstEntry = entries.GetValue(0);
            if (firstEntry == null)
                return (-1, -1);

            // Читаем X и Z через рефлексию
            double x = (double)_xField.GetValue(firstEntry);
            double z = (double)_zField.GetValue(firstEntry);

            return ((int)x, (int)z);
        }
    }
}