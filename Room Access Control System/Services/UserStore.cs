using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Room_Access_Control_System.Models;

namespace Room_Access_Control_System.Services
{
    public static class UserStore
    {
        private static readonly string DataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RoomAccessData");

        private static readonly string UsersPath = Path.Combine(DataRoot, "ID_Card_List.txt");

        public static List<UserModel> LoadUsers()
        {
            var list = new List<UserModel>();

            if (!File.Exists(UsersPath))
            {
                EnsureDir();
                File.WriteAllText(UsersPath, "Name,CardId,Role" + Environment.NewLine);
                return list;
            }

            var lines = File.ReadAllLines(UsersPath);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 3) continue;

                var name = parts[0].Trim();
                var cardId = parts[1].Trim();
                UserType role;
                if (!Enum.TryParse(parts[2].Trim(), out role)) role = UserType.Student;

                list.Add(new UserModel { Name = name, CardId = cardId, Role = role });
            }

            // Alphabetical by name
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            return list;
        }

        public static void SaveUsers(List<UserModel> users)
        {
            EnsureDir();
            using (var sw = new StreamWriter(UsersPath, false))
            {
                sw.WriteLine("Name,CardId,Role");
                foreach (var u in users)
                    sw.WriteLine($"{u.Name},{u.CardId},{u.Role}");
            }
        }

        private static void EnsureDir()
        {
            var dir = Path.GetDirectoryName(UsersPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
