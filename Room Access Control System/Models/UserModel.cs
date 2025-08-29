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

        // Primary role
        public UT Role { get; set; }

        // Multiple roles
        public List<UT> Roles { get; set; } = new List<UT>();

        // Display string for DataGridView
        public string RolesDisplay => (Roles != null && Roles.Count > 0)
            ? string.Join(", ", Roles)
            : Role.ToString();
    }
}
