using System;
using System.Collections.Generic;
using System.Linq;
using Room_Access_Control_System.Models;

namespace Room_Access_Control_System.Services
{
    public class TimeWindow
    {
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public TimeWindow(TimeSpan start, TimeSpan end) { Start = start; End = end; }
    }

    public class AccessDecision
    {
        public bool Allowed { get; set; }
        public string Reason { get; set; }
        public AccessDecision(bool allowed, string reason) { Allowed = allowed; Reason = reason; }
    }

    public class AccessRule
    {
        public UserType UserType { get; set; }
        public HashSet<RoomType> AllowedRoomsNormal { get; set; } = new HashSet<RoomType>();
        public List<TimeWindow> NormalWindows { get; set; } = new List<TimeWindow>(); // empty = anytime
        public bool AllowAnyRoomNormal { get; set; } = false;

        // Emergency behaviour
        public bool AllowedInEmergency { get; set; } = false; // Security true
        public bool EmergencyOnly { get; set; } = false;      // Emergency Responder true
    }

    public static class RuleEngine
    {
        public static readonly Dictionary<UserType, AccessRule> Rules = CreateRules();

        private static Dictionary<UserType, AccessRule> CreateRules()
        {
            var rules = new Dictionary<UserType, AccessRule>();

            // Staff: Lecture/Teaching/Staff rooms, 05:30–23:59 in NORMAL, none in EMERGENCY
            rules[UserType.StaffMember] = new AccessRule
            {
                UserType = UserType.StaffMember,
                AllowedRoomsNormal = new HashSet<RoomType> { RoomType.LectureHall, RoomType.TeachingRoom, RoomType.StaffRoom, RoomType.Hallway },
                NormalWindows = new List<TimeWindow> { new TimeWindow(new TimeSpan(5, 30, 0), new TimeSpan(23, 59, 59)) }
            };

            // Student: Lecture/Teaching, 08:30–22:00
            rules[UserType.Student] = new AccessRule
            {
                UserType = UserType.Student,
                AllowedRoomsNormal = new HashSet<RoomType> { RoomType.LectureHall, RoomType.TeachingRoom, RoomType.Hallway },
                NormalWindows = new List<TimeWindow> { new TimeWindow(new TimeSpan(8, 30, 0), new TimeSpan(22, 0, 0)) }
            };

            // Visitor: Lecture halls only, 08:30–22:00
            rules[UserType.VisitorGuest] = new AccessRule
            {
                UserType = UserType.VisitorGuest,
                AllowedRoomsNormal = new HashSet<RoomType> { RoomType.LectureHall, RoomType.Hallway },
                NormalWindows = new List<TimeWindow> { new TimeWindow(new TimeSpan(8, 30, 0), new TimeSpan(22, 0, 0)) }
            };

            // Contract Cleaner: all EXCEPT Secure, 05:30–10:30 and 17:30–22:30
            rules[UserType.ContractCleaner] = new AccessRule
            {
                UserType = UserType.ContractCleaner,
                AllowedRoomsNormal = new HashSet<RoomType> { RoomType.LectureHall, RoomType.TeachingRoom, RoomType.StaffRoom, RoomType.Hallway },
                NormalWindows = new List<TimeWindow>
                {
                    new TimeWindow(new TimeSpan(5,30,0),  new TimeSpan(10,30,0)),
                    new TimeWindow(new TimeSpan(17,30,0), new TimeSpan(22,30,0))
                }
            };

            // Manager: any room, any time in NORMAL; none in EMERGENCY
            rules[UserType.Manager] = new AccessRule
            {
                UserType = UserType.Manager,
                AllowAnyRoomNormal = true
            };

            // Security: any room, any time, both NORMAL & EMERGENCY
            rules[UserType.Security] = new AccessRule
            {
                UserType = UserType.Security,
                AllowAnyRoomNormal = true,
                AllowedInEmergency = true
            };

            // Emergency Responder: any room ONLY in EMERGENCY
            rules[UserType.EmergencyResponder] = new AccessRule
            {
                UserType = UserType.EmergencyResponder,
                EmergencyOnly = true,
                AllowedInEmergency = true
            };

            return rules;
        }

        public static AccessDecision Evaluate(UserType user, RoomType room, SystemMode mode, DateTime timestamp)
        {
            var rule = Rules[user];
            var tod = timestamp.TimeOfDay;

            // Emergency mode rules
            if (mode == SystemMode.Emergency)
            {
                if (rule.EmergencyOnly || rule.AllowedInEmergency)
                    return new AccessDecision(true, "Emergency access");
                return new AccessDecision(false, "No access during emergency");
            }

            // Normal mode rules
            if (rule.EmergencyOnly)
                return new AccessDecision(false, "Emergency cards are inactive in normal mode");

            bool roomAllowed = rule.AllowAnyRoomNormal || rule.AllowedRoomsNormal.Contains(room);
            if (!roomAllowed)
                return new AccessDecision(false, "Room type not permitted");

            // Time windows (if any defined)
            if (rule.NormalWindows.Count > 0)
            {
                bool inWindow = rule.NormalWindows.Any(w => tod >= w.Start && tod <= w.End);
                if (!inWindow)
                    return new AccessDecision(false, "Outside permitted hours");
            }

            return new AccessDecision(true, "Within permitted hours");
        }
    }
}
