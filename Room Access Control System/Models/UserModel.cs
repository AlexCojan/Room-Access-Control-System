using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Room_Access_Control_System.Models
{
    public class UserModel
    {
        public string Name { get; set; }
        public string CardId { get; set; }   // unique swipe card number
        public UserType Role { get; set; }   // enum from Enums.cs
    }
}

