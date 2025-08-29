# Room Access Control System 🏫🔐

## Overview
This project is a **C# Windows Forms prototype** for a college **Room Access Control System**.  
It simulates swipe-card access to rooms across multiple buildings, enforcing rules based on:

- **User roles** (Student, Staff, Manager, Visitor, Cleaner, Security, Emergency Responder).
- **Room types** (Lecture Hall, Teaching Room, Staff Room, Secure Room, Hallway).
- **System mode** (Normal vs. Emergency).
- **Time windows** (different roles allowed at different hours).

Every swipe attempt is logged to a **daily text file** for auditing.  
The system includes management screens to add/update/remove **Rooms, Users, and Roles**.

-----------------------------------------------------------------------------------------------------

## Features
✅ Simulated **time travel** (change system clock to test rules).  
✅ **Normal/Emergency mode** toggle with logging.  
✅ Role-based access control (time + room restrictions).  
✅ **Enter/Exit** buttons for each room in two buildings (Alpha, Beta).  
✅ Daily **log files** (`room_access_log_YYYY-MM-DD.txt`).  
✅ **CRUD management** for Users and Rooms.  
✅ Assign **multiple roles** to a user.  

-----------------------------------------------------------------------------------------------------

## Setup Instructions ⚙️

### Requirements
- Windows 10/11
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) (Community Edition is fine)
- .NET Framework 4.8 (or later)

### Steps
1. Clone the repository:
   ```bash
   git clone https://github.com/YOUR-USERNAME/room-access-control-system.git
2. Open the solution file (Room_Access_Control_System.sln) in Visual Studio.
3. Build the project (Ctrl+Shift+B).
4. Run the app (F5).
  - The Simulation tab allows testing swipes and emergency scenarios.
  - The Management tabs let you manage rooms, users, and roles.

-----------------------------------------------------------------------------------------------------

### Logs & Data
   - Logs are created under:
      + Documents/RoomAccessLogs/room_access_log_YYYY-MM-DD.txt
   - Users and rooms are stored as text files in the Documents/RoomAccessData folder.

-----------------------------------------------------------------------------------------------------

## Contribution & Workflow Guidelines 🤝
   - This repository is set up for collaborative development. Please follow these rules:

---------------------------
### Branching Model
   - main → stable version (ready to run for demo/marking).
   - dev → active development branch.
   - Feature branches → feature/<short-name> (e.g., feature/add-role-multi).

---------------------------
### Workflow
1. Always branch from dev.
2. Implement your feature or fix.
3. Commit with meaningful messages:
   - git commit -m "Add multi-role support for UserModel"
4. Push your branch and create a Pull Request (PR) → target dev.
5. Another team member reviews before merging.

---------------------------
### Issues
   - Use GitHub Issues to track bugs, tasks, and enhancements.
   -Link commits/PRs to issues for traceability.

---------------------------
### Code Style
   - Follow standard C# naming conventions.
   - Keep UI code (Form1) clean: delegate logic to RuleEngine, LogService, etc.
   - Comment non-trivial logic.

---------------------------
## License
   - This project is for educational purposes. No production deployment intended.