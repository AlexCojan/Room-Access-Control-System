using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Room_Access_Control_System.Models;

namespace Room_Access_Control_System.Models
{
    public class RoomModel
    {
        public string Code { get; set; }          // e.g., A.R.101
        public RoomType Type { get; set; }        // LectureHall, TeachingRoom, etc.
        public SystemMode State { get; set; }     // Normal / Emergency (per-room state)
    }
}
