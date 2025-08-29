using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Room_Access_Control_System.Models;

// aliases
using UT = Room_Access_Control_System.Models.UserType;

namespace Room_Access_Control_System.Services
{
    public static class UserStore
    {
        private static readonly string DataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RoomAccessData");

        private static readonly string UsersPath = Path.Combine(DataRoot, "users.txt");
        // Format per line: Name|CardId|PrimaryRole|Role1,Role2,Role3

        public static List<UserModel> LoadUsers()
        {
            Directory.CreateDirectory(DataRoot);
            if (!File.Exists(UsersPath)) return new List<UserModel>();

            var list = new List<UserModel>();
            foreach (var line in File.ReadAllLines(UsersPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('|');
                if (parts.Length < 3) continue;

                var name = parts[0].Trim();
                var card = parts[1].Trim();
                UT primary;
                if (!Enum.TryParse<UT>(parts[2].Trim(), out primary))
                    primary = UT.Student;

                var roles = new List<UT>();
                if (parts.Length >= 4 && !string.IsNullOrWhiteSpace(parts[3]))
                {
                    foreach (var rStr in parts[3].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        UT r;
                        if (Enum.TryParse<UT>(rStr.Trim(), out r))
                            roles.Add(r);
                    }
                }

                if (roles.Count == 0) roles.Add(primary); // ensure at least primary present

                list.Add(new UserModel
                {
                    Name = name,
                    CardId = card,
                    Role = primary,
                    Roles = roles.Distinct().ToList()
                });
            }

            return list.OrderBy(u => u.Name).ToList();
        }

        public static void SaveUsers(List<UserModel> users)
        {
            Directory.CreateDirectory(DataRoot);
            var lines = new List<string>();

            foreach (var u in users)
            {
                var roles = (u.Roles ?? new List<UT>());
                if (roles.Count == 0) roles.Add(u.Role);

                var rolesCsv = string.Join(",", roles.Select(r => r.ToString()));
                lines.Add($"{u.Name}|{u.CardId}|{u.Role}|{rolesCsv}");
            }

            File.WriteAllLines(UsersPath, lines);
        }
    }
}
