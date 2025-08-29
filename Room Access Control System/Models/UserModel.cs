using System.Collections.Generic;
using Room_Access_Control_System.Models;

// alias for brevity
using UT = Room_Access_Control_System.Models.UserType;

namespace Room_Access_Control_System.Models
{
    public class UserModel
    {
        public string Name { get; set; }
        public string CardId { get; set; }

        // Primary role (kept for compatibility with existing UI)
        public UT Role { get; set; }

        // Multiple roles (new)
        public List<UT> Roles { get; set; } = new List<UT>();
    }
}
