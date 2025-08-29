using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Room_Access_Control_System.Models
{
    public enum UserType
    {
        StaffMember,
        ContractCleaner,
        Student,
        Manager,
        VisitorGuest,
        Security,
        EmergencyResponder
    }

    public enum RoomType
    {
        LectureHall,
        TeachingRoom,
        StaffRoom,
        SecureRoom,
        Hallway
    }

    public enum SystemMode { Normal, Emergency }
}

