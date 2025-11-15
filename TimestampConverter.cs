using System;

namespace RpbAnalyzer
{
    public static class TimestampConverter
    {
        private static readonly DateTime BaseDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Конвертирует hex значение в DateTime (секунды с 1 января 2000 года)
        /// </summary>
        /// <param name="hexTimestamp">Hex значение timestamp</param>
        /// <returns>Соответствующая дата и время</returns>
        public static DateTime ConvertFromHex(string hexTimestamp)
        {
            // Убираем префикс 0x если есть
            if (hexTimestamp.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hexTimestamp = hexTimestamp.Substring(2);

            uint seconds = Convert.ToUInt32(hexTimestamp, 16);
            return BaseDate.AddSeconds(seconds);
        }

        /// <summary>
        /// Конвертирует hex значение в DateTime (секунды с 1 января 2000 года)
        /// </summary>
        /// <param name="timestamp">Числовое значение timestamp</param>
        /// <returns>Соответствующая дата и время</returns>
        public static DateTime ConvertFromUInt(uint timestamp)
        {
            return BaseDate.AddSeconds(timestamp);
        }

        /// <summary>
        /// Конвертирует DateTime в hex timestamp (секунды с 1 января 2000 года)
        /// </summary>
        /// <param name="dateTime">Дата и время для конвертации</param>
        /// <returns>Hex представление timestamp</returns>
        public static string ConvertToHex(DateTime dateTime)
        {
            TimeSpan diff = dateTime.ToUniversalTime() - BaseDate;
            uint seconds = (uint)diff.TotalSeconds;
            return $"0x{seconds:X8}";
        }

        /// <summary>
        /// Конвертирует DateTime в числовое значение timestamp (секунды с 1 января 2000 года)
        /// </summary>
        /// <param name="dateTime">Дата и время для конвертации</param>
        /// <returns>Числовое значение timestamp</returns>
        public static uint ConvertToUInt(DateTime dateTime)
        {
            TimeSpan diff = dateTime.ToUniversalTime() - BaseDate;
            return (uint)diff.TotalSeconds;
        }
    }
}