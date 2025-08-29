using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Room_Access_Control_System.Models;

namespace Room_Access_Control_System.Services
{
    public static class RoomStore
    {
        private static readonly string DataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RoomAccessData");

        private static readonly string RoomsPath = Path.Combine(DataRoot, "Rooms.txt");

        public static List<RoomModel> LoadRooms()
        {
            var list = new List<RoomModel>();

            if (!File.Exists(RoomsPath))
            {
                EnsureDir();
                File.WriteAllText(RoomsPath, "Code,Type,State" + Environment.NewLine);
                return list;
            }

            var lines = File.ReadAllLines(RoomsPath);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 3) continue;

                var code = parts[0].Trim();

                RoomType type;
                SystemMode state;
                if (!Enum.TryParse(parts[1].Trim(), out type)) continue;
                if (!Enum.TryParse(parts[2].Trim(), out state)) state = SystemMode.Normal;

                list.Add(new RoomModel { Code = code, Type = type, State = state });
            }
            return list;
        }

        public static void SaveRooms(List<RoomModel> rooms)
        {
            EnsureDir();
            using (var sw = new StreamWriter(RoomsPath, false))
            {
                sw.WriteLine("Code,Type,State");
                foreach (var r in rooms)
                    sw.WriteLine($"{r.Code},{r.Type},{r.State}");
            }
        }

        private static void EnsureDir()
        {
            var dir = Path.GetDirectoryName(RoomsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
