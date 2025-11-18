using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;


namespace DeepRpbAnalyzer
{
    class Program
    {
        private static List<Record> allRecords = new List<Record>();
        class FileHeader
        {
            public int Version { get; set; }
            public int ChksumRecCounts { get; set; }
            public string SaveTick { get; set; }
            public int RecordSize { get; set; }
            public int StartSize { get; set; }
            public int RecordCount { get; set; }
            public int StartByte { get; set; }
            public byte[] RecordsBytes { get; set; }
        }

        // Главный класс для JSON структуры
        class JsonOutput
        {
            public FileHeader Header { get; set; }
            public List<object> Records { get; set; }
        }

        class Record
        {
            public uint UniqueId { get; set; }
            public uint ParentId { get; set; }
            public ushort IsFolder { get; set; }
            public string Name { get; set; }
            public uint Number { get; set; }

            // Параметры для элементов (не папок)
            public uint MaxFps { get; set; }
            public uint ScreenMode { get; set; }
            public uint ColorMode { get; set; }
            public uint UseSpeckey { get; set; }
            public uint CursorMode { get; set; }
            public uint Unknown1 { get; set; }
            public uint Unknown2 { get; set; }
            public uint VoiceTune { get; set; }
            public string UserVoice { get; set; }
            public string VoiceCont { get; set; }
            public string UserText { get; set; }
            public string TextCont { get; set; }
            public uint Unknown3 { get; set; }
            public uint Unknown4 { get; set; }
            public uint Unknown5 { get; set; }
            public string Ip { get; set; }
            public uint Port { get; set; }
            public string Kerb { get; set; }
            public string LoginUser { get; set; }
            public uint KerbOn { get; set; }
            public uint InterServer { get; set; }

            public List<Record> Children { get; set; }

            public Record()
            {
                Children = new List<Record>();
            }
        }

        // Переменные для хранения данных заголовка
        private static int version;
        private static int chksumRecCounts;
        private static string savetick;
        private static int recordSize;
        private static int startSize;
        private static int recordCount;
        private static int startByte;
        private static byte[] RecordsBytes;
        private static string startRecsStr;


        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Использование:");
                Console.WriteLine("  Анализ RPB файла: RpbAnalyzer.exe /t <RadminAddrBook.rpb>");
                Console.WriteLine("  Convert to JSON: RpbAnalyzer.exe /j <RadminAddrBook.rpb> [output.json]");
                Console.WriteLine("  Convert to RPB: RpbAnalyzer.exe /r <JsonAddrBook.json> [output.rpb]");
                Console.ReadKey();
                return;
            }

            try
            {
                if (args[0] == "/j" || args[0] == "-j") // Экспорт
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Ошибка: Укажите файл RPB для экспорта");
                        return;
                    }

                    string inputFile = args[1];
                    string outputFile = args.Length > 2 ? args[2] : Path.ChangeExtension(inputFile, ".json");
                    //outputFile = "output_" + outputFile;

                    if (!File.Exists(inputFile))
                    {
                        Console.WriteLine("Файл не найден: " + inputFile);
                        return;
                    }

                    ExportToJson(inputFile, outputFile);
                    Console.WriteLine("Экспорт завершен: " + outputFile);
                }
                else if (args[0] == "/r" || args[0] == "-r") // Импорт
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Ошибка: Укажите файл JSON для импорта");
                        return;
                    }

                    string inputFile = args[1];
                    string outputFile = args.Length > 2 ? args[2] : Path.ChangeExtension(inputFile, ".rpb");
                    //outputFile = "output_" + outputFile;

                    if (!File.Exists(inputFile))
                    {
                        Console.WriteLine("Файл не найден: " + inputFile);
                        return;
                    }

                    ImportFromJson(inputFile, outputFile);
                    Console.WriteLine("Импорт завершен: " + outputFile);
                }
                else if (args[0] == "/t" || args[0] == "-t") // Простой анализ (Тест rpb)
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Ошибка: Укажите файл RPB для анализа");
                        return;
                    }

                    string filePath = args[1];
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine("Файл не найден: " + filePath);
                        Console.ReadKey();
                        return;
                    }

                    AnalyzeRpb(filePath, true);
                    //GenerateJson("output.json");
                    Console.WriteLine("для ");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
                Console.ReadKey();
            }


            //string filePath = args[0];
            //if (!File.Exists(filePath))
            //{
            //    Console.WriteLine("Файл не найден!");
            //    Console.ReadKey();
            //    return;
            //}

            //try
            //{
            //    AnalyzeRpb(filePath);
            //    GenerateJson("output.json");

            //    Console.WriteLine("JSON файл успешно создан: output.json");

            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Ошибка: " + ex.Message);
            //}

            //Console.ReadKey();
        }

        // Экспорт RPB в JSON
        static void ExportToJson(string inputFile, string outputFile)
        {
            AnalyzeRpb(inputFile, false);
            GenerateJson(outputFile);
        }



        static void AnalyzeRpb(string filePath, bool showlog = false)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                // ---------- Заголовок ----------
                fs.Seek(0, SeekOrigin.Begin);
                version = br.ReadInt32();                   // 0x00  Версия
                chksumRecCounts = br.ReadInt32();           // 0x04  Количество записей *3 +1
                int rbTimestamp = br.ReadInt32();               // 0x08  миллисекунд от последней перезагрузки ОС
                recordSize = br.ReadInt32();                // 0x0C  = 0x17FA = 6138    Размер блока записи
                startSize = br.ReadInt32();                 // 0x10  = 0x80  = 128      Размер заполнителя
                recordCount = br.ReadInt32() + 1;             // 0x14  Кол-во записей
                startByte = br.ReadByte();                  // 0x18  Нач байт заполнителя
                RecordsBytes = br.ReadBytes(startSize);  // 0х19  Заполнитель, 01 каждый байт на запись
                startRecsStr = BitConverter.ToString(RecordsBytes);
                savetick = $"{HexToHMS($"0x{rbTimestamp:X8}")}";

                if (showlog)
                {
                    Console.WriteLine("=== Заголовок ===");
                    Console.WriteLine($"0x00 Версия              : {version}");
                    Console.WriteLine($"0x04 Кол-во записей*3+1  : {chksumRecCounts} (0x{chksumRecCounts:X8})");
                    Console.WriteLine($"0x08 Время от ребута ОС  : {savetick} (0x{rbTimestamp:X8})");
                    Console.WriteLine($"0x0C Размер блока записи : {recordSize} (0x{recordSize:X})");
                    Console.WriteLine($"0x10 Размер заполнителя  : {startSize} (0x{startSize:X})");
                    Console.WriteLine($"0x14 Кол-во записей      : {recordCount}");
                    Console.WriteLine($"0x18 Нач байт заполнителя: {startByte}");
                    Console.WriteLine();
                    Console.WriteLine($"0x19 Заполнитель 128байт : {startRecsStr}");
                    Console.WriteLine("Количество записей - 01 каждый следующий байт");
                    Console.WriteLine();
                }
                // ---------- Данные ----------
                long dataStart = fs.Position;
                //long dataStart = 0x19 + (long)startSize;    // начало первой записи (0x18 Нач байт заполнителя + 0x10 Размер заполнителя)
                if (dataStart + (long)recordSize * recordCount > fs.Length)
                    throw new Exception("Файл слишком мал для указанного количества записей.");

                fs.Seek(dataStart, SeekOrigin.Begin);

                for (int i = 0; i < recordCount; i++)
                {
                    long recordStart = fs.Position;
                    byte[] record = br.ReadBytes(recordSize);


                    Console.WriteLine($"=== Запись {i + 1} (абс. 0x{recordStart:X}) ============================================================\n");

                    AnalyzeSpecificAddresses(record, showlog);
                    var rec = ParseRecord(record);
                    allRecords.Add(rec);


                    Console.WriteLine();
                }

                Console.WriteLine($"=== Прочитано записей: {recordCount} =============\n");
            }
        }

        // -------------------------------------------------
        // Анализ конкретных адресов в записи
        // -------------------------------------------------
        static void AnalyzeSpecificAddresses(byte[] record, bool showlog = false)
        {
            //Console.WriteLine("--- Анализ конкретных адресов ---");



            uint fps = BitConverter.ToUInt32(record, 0x0000);           // 4 1-2000
            uint screenmode = BitConverter.ToUInt32(record, 0x0004);    // 4 0-3
            uint colormode = BitConverter.ToUInt32(record, 0x0008);     // 4 0-5
            uint kodekeyon = BitConverter.ToUInt32(record, 0x000C);     // 4 0-1
            uint nullInt1 = BitConverter.ToUInt32(record, 0x0010);      // 4 00 0x0010 -?
            uint nullInt2 = BitConverter.ToUInt32(record, 0x0014);      // 4 00 0x0014 -?
            uint cursormode = BitConverter.ToUInt32(record, 0x0018);    // 4 0-2
            uint unknown1 = BitConverter.ToUInt32(record, 0x001C);      // 4 01 0x001C  ?
                                                                        // ?????????????? 27 нулевых параметров по 4 байта
            uint unknown2 = BitConverter.ToUInt32(record, 0x008C);      // 4 01 0x008C  ?
            uint nullInt3 = BitConverter.ToUInt32(record, 0x0090);      // 4 00 0x0090  ?
            uint voicetune = BitConverter.ToUInt32(record, 0x0094);     // 4 0-5
            string uservoice = ReadUtf16String(record, 0x0098);         // 64
            string voicecont = ReadUtf16String(record, 0x00D8);         // 1024
            string usertext = ReadUtf16String(record, 0x04D8);          // 64
            string textcont = ReadUtf16String(record, 0x0518);          // 1024
            uint unknown3 = BitConverter.ToUInt32(record, 0x0918);      // 4 02 0x0918- ?
            uint unknown4 = BitConverter.ToUInt32(record, 0x091C);      // 4 02 0x091C- ?
            uint unknown5 = BitConverter.ToUInt32(record, 0x0920);      // 4 03 0х0920- ?
                                                                        // ?????????????? пустые блоки по 1024,1024,512,4
            string ip = ReadUtf16String(record, 0x1328);                // 200
            string name = ReadUtf16String(record, 0x13F0);              // 200
            uint port = BitConverter.ToUInt32(record, 0x14B8);          // 4 1-65535
            string nullStr1 = ReadUtf16String(record, 0x14BC);          // 200 0x14BC-0x1583
            string nullStr2 = ReadUtf16String(record, 0x1584);          // 200 0x1584-0x164B
            string kerb = ReadUtf16String(record, 0x164C);              // 200
            string loginUser = ReadUtf16String(record, 0x1714);         // 200 loginUser
            uint kerbon = BitConverter.ToUInt16(record, 0x17DC);        // 4 0-1
            uint uniqueId = BitConverter.ToUInt32(record, 0x17E0);      // 4 
            uint interServer = BitConverter.ToUInt32(record, 0x17E4);   // 4
            uint parentId = BitConverter.ToUInt32(record, 0x17E8);      // 4
            ushort isFolder = BitConverter.ToUInt16(record, 0x17EC);    // 2 0-1
            uint number = BitConverter.ToUInt32(record, 0x17EE);        // 4
            uint nullInt4 = BitConverter.ToUInt32(record, 0x17F2);      // 4 0x17F2-0x17F5  ?
            uint nullInt5 = BitConverter.ToUInt32(record, 0x17F6);      // 4 0x17F6-0x17F9  ? ---> 0x17FA (размер записи)




            if (isFolder == 0 && showlog)
            {
                Console.WriteLine($"0x0000 FPS : {fps}");
                Console.WriteLine($"0x0004 screenmode : {screenmode}");
                Console.WriteLine($"0x0008 colormode : {colormode}");
                Console.WriteLine($"0x000C use speckey : {kodekeyon}");
                Console.WriteLine();
                Console.WriteLine($"0x0018 cursormode : {cursormode}");
                Console.WriteLine($"0x001C unknown1 : {unknown1}");
                Console.WriteLine($"0x008C unknown2 : {unknown2}");
                Console.WriteLine();
                Console.WriteLine($"0x0094 voicetune : {voicetune}");
                Console.WriteLine($"0x0098 uservoice : {uservoice}");
                Console.WriteLine($"0x00D8 voicecont : {voicecont}");
                Console.WriteLine($"0x04D8 usertext : {usertext}");
                Console.WriteLine($"0x0518 textcont : {textcont}");
                Console.WriteLine();
                Console.WriteLine($"0x0918 unknown3 : {unknown3}");
                Console.WriteLine($"0x091C unknown4 : {unknown4}");
                Console.WriteLine($"0x0920 unknown5 : {unknown5}");
                Console.WriteLine();
                Console.WriteLine($"0x1328 IP : {ip}");
            }

            Console.WriteLine($"0x13F0 Имя : {name}");

            if (isFolder == 0 && showlog)
            {
                Console.WriteLine($"0x14B8 Порт: {port} (0x{port:X8})");
                Console.WriteLine();
                Console.WriteLine($"0x164C Имя хоста Kerberos : {kerb}");
                Console.WriteLine($"0x1714 Логин подключения : {loginUser}");
                Console.WriteLine($"0x17DC Не использ  данные текущ пользов : {kerbon}");
            }

            Console.WriteLine();
            Console.WriteLine($"0x17E0 Уникальный номер записи: {uniqueId} (0x{uniqueId:X8})");
            if (isFolder == 0 && showlog)
            {
                Console.WriteLine($"0x17E4 Номер записи промежуточного сервера: {interServer} (0x{interServer:X8})");
            }
            Console.WriteLine($"0x17E8 Номер родительской записи: {parentId} (0x{parentId:X8})");
            Console.WriteLine($"0x17EC Признак папки: {isFolder} (0x{isFolder:X4})");

            Console.WriteLine($"0x17EE Порядковый номер записи: {number} (0x{number:X8})");

            Console.WriteLine();
        }

        static string ReadUtf16String(byte[] data, int startOffset)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = startOffset; i < data.Length - 1; i += 2)
            {
                byte lo = data[i];
                byte hi = data[i + 1];
                char c = (char)(hi << 8 | lo);   // little-endian

                if (c == '\0')
                {
                    break;
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        static string HexToHMS(string hex)
        {

            // Удаляем префикс 0x, если есть
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);

            // Проверка на валидный hex
            if (!uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint ms))
                return "Неверный формат";

            // Преобразуем в long для безопасности (поддержка больших значений)
            long milliseconds = ms;

            long totalSeconds = milliseconds / 1000;
            long hours = totalSeconds / 3600;
            long minutes = (totalSeconds % 3600) / 60;
            long seconds = totalSeconds % 60;

            return $"{hours}:{minutes:D2}:{seconds:D2}";
        }

        static Record ParseRecord(byte[] record)
        {
            var rec = new Record();

            rec.MaxFps = BitConverter.ToUInt32(record, 0x0000);
            rec.ScreenMode = BitConverter.ToUInt32(record, 0x0004);
            rec.ColorMode = BitConverter.ToUInt32(record, 0x0008);
            rec.UseSpeckey = BitConverter.ToUInt32(record, 0x000C);
            rec.CursorMode = BitConverter.ToUInt32(record, 0x0018);
            rec.Unknown1 = BitConverter.ToUInt32(record, 0x001C);
            rec.Unknown2 = BitConverter.ToUInt32(record, 0x008C);
            rec.VoiceTune = BitConverter.ToUInt32(record, 0x0094);
            rec.UserVoice = ReadUtf16String(record, 0x0098);
            rec.VoiceCont = ReadUtf16String(record, 0x00D8);
            rec.UserText = ReadUtf16String(record, 0x04D8);
            rec.TextCont = ReadUtf16String(record, 0x0518);
            rec.Unknown3 = BitConverter.ToUInt32(record, 0x0918);
            rec.Unknown4 = BitConverter.ToUInt32(record, 0x091C);
            rec.Unknown5 = BitConverter.ToUInt32(record, 0x0920);
            rec.Ip = ReadUtf16String(record, 0x1328);
            rec.Name = ReadUtf16String(record, 0x13F0);
            rec.Port = BitConverter.ToUInt32(record, 0x14B8);
            rec.Kerb = ReadUtf16String(record, 0x164C);
            rec.LoginUser = ReadUtf16String(record, 0x1714);
            rec.KerbOn = BitConverter.ToUInt16(record, 0x17DC);
            rec.UniqueId = BitConverter.ToUInt32(record, 0x17E0);
            rec.InterServer = BitConverter.ToUInt32(record, 0x17E4);
            rec.ParentId = BitConverter.ToUInt32(record, 0x17E8);
            rec.IsFolder = BitConverter.ToUInt16(record, 0x17EC);
            rec.Number = BitConverter.ToUInt32(record, 0x17EE);

            return rec;
        }

        static void GenerateJson(string outputPath)
        {
            // Строим иерархическое дерево
            var rootItems = BuildHierarchy();

            // Создаем упрощенную структуру для JSON
            var simplifiedRootItems = CreateSimplifiedStructure(rootItems);

            // Создаем объект заголовка
            var header = new FileHeader
            {
                Version = version,
                ChksumRecCounts = chksumRecCounts,
                SaveTick = savetick,
                RecordSize = recordSize,
                StartSize = startSize,
                RecordCount = recordCount,
                StartByte = startByte,
                RecordsBytes = RecordsBytes
            };

            // Создаем главный объект JSON
            var jsonOutput = new JsonOutput
            {
                Header = header,
                Records = simplifiedRootItems
            };

            var serializer = new JavaScriptSerializer();

            // Увеличиваем максимальную длину JSON, если нужно
            serializer.MaxJsonLength = int.MaxValue;

            string json = serializer.Serialize(jsonOutput);
            File.WriteAllText(outputPath, json, Encoding.UTF8);
        }
        // Создаем упрощенную структуру для JSON
        static List<object> CreateSimplifiedStructure(List<Record> items)
        {
            var result = new List<object>();

            foreach (var item in items)
            {
                if (item.IsFolder == 1)
                {
                    // Для папок - только указанные поля
                    var folder = new
                    {
                        Name = item.Name,
                        UniqueId = item.UniqueId,
                        ParentId = item.ParentId,
                        IsFolder = item.IsFolder,
                        Number = item.Number,
                        Records = item.Children.Count > 0 ? CreateSimplifiedStructure(item.Children) : null
                    };
                    result.Add(folder);
                }
                else
                {
                    // Для элементов - все поля
                    var element = new
                    {
                        // Общие поля
                        Name = item.Name,
                        UniqueId = item.UniqueId,
                        InterServer = item.InterServer,
                        ParentId = item.ParentId,
                        IsFolder = item.IsFolder,
                        Number = item.Number,

                        // Специфичные поля для элементов
                        MaxFps = item.MaxFps,
                        ScreenMode = item.ScreenMode,
                        ColorMode = item.ColorMode,

                        UseSpeckey = item.UseSpeckey,
                        CursorMode = item.CursorMode,
                        Unknown1 = item.Unknown1,
                        Unknown2 = item.Unknown2,
                        VoiceTune = item.VoiceTune,
                        UserVoice = item.UserVoice,
                        VoiceCont = item.VoiceCont,
                        UserText = item.UserText,
                        TextCont = item.TextCont,
                        Unknown3 = item.Unknown3,
                        Unknown4 = item.Unknown4,
                        Unknown5 = item.Unknown5,
                        Ip = item.Ip,
                        Port = item.Port,
                        Kerb = item.Kerb,
                        LoginUser = item.LoginUser,
                        KerbOn = item.KerbOn
                    };
                    result.Add(element);
                }
            }

            return result;
        }
        static List<Record> BuildHierarchy()
        {
            // Создаем словарь для быстрого доступа по UniqueId
            var recordDict = allRecords.ToDictionary(r => r.UniqueId);

            // Список корневых элементов (parentId = 0)
            var rootItems = new List<Record>();

            foreach (var record in allRecords)
            {
                if (record.ParentId == 0)
                {
                    // Корневой элемент
                    rootItems.Add(record);
                }
                else if (recordDict.ContainsKey(record.ParentId))
                {
                    // Добавляем как дочерний элемент к родителю
                    recordDict[record.ParentId].Children.Add(record);
                }
            }

            // Сортируем элементы по полю Number
            SortHierarchy(rootItems);

            return rootItems;
        }

        static void SortHierarchy(List<Record> items)
        {
            if (items == null) return;

            // Сортируем текущий уровень по Number
            items.Sort((a, b) => a.Number.CompareTo(b.Number));

            // Рекурсивно сортируем дочерние элементы
            foreach (var item in items)
            {
                if (item.Children != null && item.Children.Count > 0)
                {
                    SortHierarchy(item.Children);
                }
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Рекурсивно собираем все записи в плоский список
        // Обновленный метод импорта
        static void ImportFromJson(string inputFile, string outputFile)
        {
            string json = File.ReadAllText(inputFile, Encoding.UTF8);
            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;

            var jsonOutput = serializer.Deserialize<JsonOutput>(json);

            if (jsonOutput == null)
            {
                throw new Exception("Неверный формат JSON файла");
            }

            // Преобразуем List<object> в List<Record>
            var records = ConvertObjectsToRecords(jsonOutput.Records);

            CreateRpbFromJson(jsonOutput, records, outputFile);
        }

        // Преобразуем List<object> в List<Record> с заполнением всех полей
        static List<Record> ConvertObjectsToRecords(List<object> objects)
        {
            var records = new List<Record>();

            foreach (var obj in objects)
            {
                var record = ConvertObjectToRecord(obj);
                if (record != null)
                {
                    records.Add(record);
                }
            }

            return records;
        }

        // Рекурсивно преобразуем object в Record с заполнением всех полей
        static Record ConvertObjectToRecord(object obj)
        {
            if (obj == null) return null;

            var dict = obj as Dictionary<string, object>;
            if (dict == null) return null;

            var record = new Record();

            // Заполняем основные поля (всегда присутствуют)
            record.Name = GetStringValue(dict, "Name");
            record.UniqueId = GetUIntValue(dict, "UniqueId");
            record.ParentId = GetUIntValue(dict, "ParentId");
            record.IsFolder = (ushort)GetUIntValue(dict, "IsFolder");
            record.Number = GetUIntValue(dict, "Number");

            // InterServer может быть как у папок, так и у элементов
            record.InterServer = GetUIntValue(dict, "InterServer");

            // Заполняем поля для элементов (даже если это папка - заполняем нулями)
            record.MaxFps = GetUIntValue(dict, "MaxFps");
            record.ScreenMode = GetUIntValue(dict, "ScreenMode");
            record.ColorMode = GetUIntValue(dict, "ColorMode");
            record.UseSpeckey = GetUIntValue(dict, "UseSpeckey");
            record.CursorMode = GetUIntValue(dict, "CursorMode");
            if (record.IsFolder == 1)
            {
                record.Unknown1 = 0;
                record.Unknown2 = 0;
                record.Unknown3 = 0;
                record.Unknown4 = 0;
                record.Unknown5 = 0;
            }
            else
            {
                record.Unknown1 = GetUIntValue(dict, "Unknown1", 1);
                record.Unknown2 = GetUIntValue(dict, "Unknown2", 1);
                record.Unknown3 = GetUIntValue(dict, "Unknown3", 2);
                record.Unknown4 = GetUIntValue(dict, "Unknown4", 2);
                record.Unknown5 = GetUIntValue(dict, "Unknown5", 3);
            }
            record.VoiceTune = GetUIntValue(dict, "VoiceTune");
            record.UserVoice = GetStringValue(dict, "UserVoice");
            record.VoiceCont = GetStringValue(dict, "VoiceCont");
            record.UserText = GetStringValue(dict, "UserText");
            record.TextCont = GetStringValue(dict, "TextCont");

            record.Ip = GetStringValue(dict, "Ip");
            record.Port = GetUIntValue(dict, "Port");
            record.Kerb = GetStringValue(dict, "Kerb");
            record.LoginUser = GetStringValue(dict, "LoginUser");
            record.KerbOn = GetUIntValue(dict, "KerbOn");

            // Рекурсивно обрабатываем детей
            if (dict.ContainsKey("Records") && dict["Records"] != null)
            {
                object childrenObj = dict["Records"];
                var childrenObjects = ((object[])childrenObj).ToList();
                if (childrenObjects != null)
                {
                    foreach (var childObj in childrenObjects)
                    {
                        var childRecord = ConvertObjectToRecord(childObj);
                        if (childRecord != null)
                        {
                            record.Children.Add(childRecord);
                        }
                    }
                }
            }

            return record;
        }

        // Рекурсивно собираем все записи в плоский список
        static List<Record> FlattenRecords(List<Record> records)
        {
            var result = new List<Record>();

            foreach (var record in records)
            {
                result.Add(record);

                // Рекурсивно обрабатываем детей
                if (record.Children != null && record.Children.Count > 0)
                {
                    result.AddRange(FlattenRecords(record.Children));
                }
            }

            return result;
        }

        // Обновленный метод создания RPB
        static void CreateRpbFromJson(JsonOutput jsonOutput, List<Record> records, string outputPath)
        {
            FileHeader header = jsonOutput.Header;

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                // Собираем все записи в плоский список
                var allRecords = FlattenRecords(records);
                var recSize = 0;
                //if (header != null)
                //{
                //    // Записываем заголовок
                //    bw.Write(header.Version);
                //    bw.Write(header.ChksumRecCounts);

                //    uint timestamp = HMSToMilliseconds(header.SaveTick);
                //    bw.Write((int)timestamp);
                //    recSize = header.RecordSize;
                //    bw.Write(recSize);
                //    bw.Write(header.StartSize);
                //    bw.Write(header.RecordCount - 1);
                //    bw.Write((byte)header.StartByte);
                //    bw.Write(header.RecordsBytes);
                //} 
                //else
                //{
                    // Записываем заголовок
                    bw.Write(4);
                    bw.Write(allRecords.Count*3+1);

                    uint timestamp = (uint)Environment.TickCount;
                    bw.Write((int)timestamp);
                    recSize = 6138;
                    bw.Write(recSize);
                    bw.Write(128);
                    bw.Write(allRecords.Count - 1);
                    bw.Write((byte)1); 
                    bw.Write(CreateBytesArrayFilledWithOnes(allRecords.Count));
                //}

                // Записываем записи
                foreach (var record in allRecords)
                {
                    WriteRecord(bw, record, recSize);
                }
            }
        }
        static byte[] CreateBytesArrayFilledWithOnes(int count)
        {
            if (count > 128) count = 128;

            byte[] array = new byte[128];
            for (int i = 0; i < count; i++)
            {
                array[i] = 0x01; // Заполняем только первые 'count' байт
            }
            // Остальные байты остаются 0x00
            return array;
        }
        // Запись Record в бинарный формат
        static void WriteRecord(BinaryWriter bw, Record record, int recordSize)
        {
            byte[] buffer = new byte[recordSize];

            WriteUInt32ToBuffer(buffer, 0x0000, record.MaxFps);
            WriteUInt32ToBuffer(buffer, 0x0004, record.ScreenMode);
            WriteUInt32ToBuffer(buffer, 0x0008, record.ColorMode);
            WriteUInt32ToBuffer(buffer, 0x000C, record.UseSpeckey);
            WriteUInt32ToBuffer(buffer, 0x0018, record.CursorMode);
            WriteUInt32ToBuffer(buffer, 0x001C, record.Unknown1);
            WriteUInt32ToBuffer(buffer, 0x008C, record.Unknown2);
            WriteUInt32ToBuffer(buffer, 0x0094, record.VoiceTune);

            WriteUtf16StringToBuffer(buffer, 0x0098, record.UserVoice);
            WriteUtf16StringToBuffer(buffer, 0x00D8, record.VoiceCont);
            WriteUtf16StringToBuffer(buffer, 0x04D8, record.UserText);
            WriteUtf16StringToBuffer(buffer, 0x0518, record.TextCont);

            WriteUInt32ToBuffer(buffer, 0x0918, record.Unknown3);
            WriteUInt32ToBuffer(buffer, 0x091C, record.Unknown4);
            WriteUInt32ToBuffer(buffer, 0x0920, record.Unknown5);

            WriteUtf16StringToBuffer(buffer, 0x1328, record.Ip);
            WriteUtf16StringToBuffer(buffer, 0x13F0, record.Name);

            WriteUInt32ToBuffer(buffer, 0x14B8, record.Port);

            WriteUtf16StringToBuffer(buffer, 0x164C, record.Kerb);
            WriteUtf16StringToBuffer(buffer, 0x1714, record.LoginUser);

            WriteUInt16ToBuffer(buffer, 0x17DC, (ushort)record.KerbOn);
            WriteUInt32ToBuffer(buffer, 0x17E0, record.UniqueId);
            WriteUInt32ToBuffer(buffer, 0x17E4, record.InterServer);
            WriteUInt32ToBuffer(buffer, 0x17E8, record.ParentId);
            WriteUInt16ToBuffer(buffer, 0x17EC, record.IsFolder);
            WriteUInt32ToBuffer(buffer, 0x17EE, record.Number);

            bw.Write(buffer);
        }

        // Вспомогательные методы для записи данных в буфер
        static void WriteUInt32ToBuffer(byte[] buffer, int offset, uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, 4);
        }

        static void WriteUInt16ToBuffer(byte[] buffer, int offset, ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, 2);
        }

        static void WriteUtf16StringToBuffer(byte[] buffer, int offset, string value, int maxLength = 0)
        {
            if (string.IsNullOrEmpty(value))
                return;

            byte[] stringBytes = Encoding.Unicode.GetBytes(value);
            //int copyLength = Math.Min(stringBytes.Length, maxLength * 2);
            Array.Copy(stringBytes, 0, buffer, offset, stringBytes.Length);
        }

        // Вспомогательные методы для извлечения значений из Dictionary
        static uint GetUIntValue(Dictionary<string, object> dict, string key, uint defReturn = 0)
        {
            if (dict.ContainsKey(key) && dict[key] != null)
            {
                try
                {
                    if (dict[key] is int) return (uint)(int)dict[key];
                    if (dict[key] is long) return (uint)(long)dict[key];
                    return Convert.ToUInt32(dict[key]);
                }
                catch
                {
                    return defReturn; // Если не удалось преобразовать, возвращаем 0
                }
            }
            return defReturn;
        }

        static string GetStringValue(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null)
            {
                return dict[key].ToString();
            }
            return string.Empty;
        }

        // Преобразование HMS формата обратно в миллисекунды
        static uint HMSToMilliseconds(string hms)
        {
            try
            {
                string[] parts = hms.Split(':');
                if (parts.Length == 3)
                {
                    long hours = long.Parse(parts[0]);
                    long minutes = long.Parse(parts[1]);
                    long seconds = long.Parse(parts[2]);

                    // Проверяем на переполнение
                    long totalMilliseconds = (hours * 3600L + minutes * 60L + seconds) * 1000L;

                    if (totalMilliseconds > uint.MaxValue)
                    {
                        Console.WriteLine($"Предупреждение: timestamp {totalMilliseconds} превышает максимальное значение uint, используется максимум");
                        return uint.MaxValue;
                    }

                    return (uint)totalMilliseconds;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка преобразования времени: {ex.Message}");
            }
            return 0;
        }
    }
}