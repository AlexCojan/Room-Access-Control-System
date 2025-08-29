using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

using Room_Access_Control_System.Services;

// Enum aliases (short & unambiguous)
using UT = Room_Access_Control_System.Models.UserType;
using RT = Room_Access_Control_System.Models.RoomType;
using SM = Room_Access_Control_System.Models.SystemMode;

namespace Room_Access_Control_System
{
    public partial class Form1 : Form
    {
        // ===== Simulated clock state =====
        private DateTime _simNow;        // simulated "now"
        private DateTime _lastRealTick;  // last real UTC tick
        private Timer _clockTimer;       // drives simulated time
        private bool _editingDate;       // true while editing date
        private bool _editingTime;       // true while editing time

        // ===== System mode state =====
        private SM _systemMode = SM.Normal;

        // ===== User type selection =====
        private UT _currentUserType = UT.Student; // default

        public Form1()
        {
            InitializeComponent();

            // --- Formats for the two pickers ---
            datePicker.Format = DateTimePickerFormat.Custom;
            datePicker.CustomFormat = "dd/MM/yyyy";
            datePicker.ShowUpDown = false;

            timePicker.Format = DateTimePickerFormat.Custom;
            timePicker.CustomFormat = "HH:mm:ss"; // 24h
            timePicker.ShowUpDown = true;         // spinner

            // Start simulated time
            _simNow = DateTime.Now;
            _lastRealTick = DateTime.UtcNow;

            // Initialize pickers from simulated time
            datePicker.Value = _simNow.Date;
            timePicker.Value = DateTime.Today.Add(_simNow.TimeOfDay);

            // Timer
            _clockTimer = new Timer { Interval = 250 };
            _clockTimer.Tick += clockTimer_Tick;
            _clockTimer.Start();

            // --- Editing flags (so timer doesn't fight user) ---
            // Date
            datePicker.Enter += (s, e) => _editingDate = true;
            datePicker.DropDown += (s, e) => _editingDate = true;
            datePicker.CloseUp += (s, e) => _editingDate = false;
            datePicker.Leave += (s, e) => _editingDate = false;

            // Time
            timePicker.Enter += (s, e) => _editingTime = true;
            timePicker.Leave += (s, e) => _editingTime = false;

            // Apply immediately when either value changes (while editing)
            datePicker.ValueChanged += (s, e) => { if (_editingDate) ApplyDate(); };
            timePicker.ValueChanged += (s, e) => { if (_editingTime) ApplyTime(); };

            // === System mode + log buttons ===
            btnSysNormal.Click += (s, e) => SetSystemMode(SM.Normal);
            btnSysEmergency.Click += (s, e) => SetSystemMode(SM.Emergency);
            btnShowLogFile.Click += btnShowLogFile_Click;

            // === User type buttons (named) ===
            btnStaffMember.Click += (s, e)        => SelectUserType(UT.StaffMember,        btnStaffMember);
            btnContractCleaner.Click += (s, e)    => SelectUserType(UT.ContractCleaner,    btnContractCleaner);
            btnStudent.Click += (s, e)            => SelectUserType(UT.Student,            btnStudent);
            btnManager.Click += (s, e)            => SelectUserType(UT.Manager,            btnManager);
            btnVisitorGuest.Click += (s, e)       => SelectUserType(UT.VisitorGuest,       btnVisitorGuest);
            btnSecurity.Click += (s, e)           => SelectUserType(UT.Security,           btnSecurity);
            btnEmergencyResponder.Click += (s, e) => SelectUserType(UT.EmergencyResponder, btnEmergencyResponder);

            // Default highlight (no log on startup)
            SelectUserType(_currentUserType, btnStudent, log: false);

            // === Wire ALL rooms with one helper ===
            // Main Hallway (school entrance)
            WireRoom(btnEnterMH100, btnExitMH100, "M.H.100 (Main Hallway)", RT.Hallway);

            // Building Alpha
            WireRoom(btnEnterAR101, btnExitAR101, "A.R.101 (Alpha Teaching Room)", RT.TeachingRoom);
            WireRoom(btnEnterAR102, btnExitAR102, "A.R.102 (Alpha Lecture Hall)",  RT.LectureHall);
            WireRoom(btnEnterAR103, btnExitAR103, "A.R.103 (Alpha Staff Room)",    RT.StaffRoom);
            WireRoom(btnEnterAR104, btnExitAR104, "A.R.104 (Alpha Secure Room)",   RT.SecureRoom);
            WireRoom(btnEnterAH100, btnExitAH100, "A.H.100 (Alpha Hallway)",       RT.Hallway);

            // Building Beta
            WireRoom(btnEnterBR101, btnExitBR101, "B.R.101 (Beta Teaching Room)", RT.TeachingRoom);
            WireRoom(btnEnterBR102, btnExitBR102, "B.R.102 (Beta Lecture Hall)",  RT.LectureHall);
            WireRoom(btnEnterBR103, btnExitBR103, "B.R.103 (Beta Staff Room)",    RT.StaffRoom);
            WireRoom(btnEnterBR104, btnExitBR104, "B.R.104 (Beta Secure Room)",   RT.SecureRoom);
            WireRoom(btnEnterBH100, btnExitBH100, "B.H.100 (Beta Hallway)",       RT.Hallway);
        }

        // Public "now" for the rest of the app (logs/rules)
        public DateTime CurrentSimulatedTime => _simNow;
        public UT CurrentUserType => _currentUserType;

        // --- Timer tick: advance simulated time by real elapsed time ---
        private void clockTimer_Tick(object sender, EventArgs e)
        {
            var nowUtc = DateTime.UtcNow;
            var elapsed = nowUtc - _lastRealTick;
            _lastRealTick = nowUtc;

            _simNow = _simNow.Add(elapsed);

            // Keep pickers in sync when user isn't editing them
            if (!_editingDate) datePicker.Value = _simNow.Date;
            if (!_editingTime) timePicker.Value = DateTime.Today.Add(_simNow.TimeOfDay);
        }

        // --- Apply date part from datePicker, keep time-of-day ---
        private void ApplyDate()
        {
            var d = datePicker.Value.Date;
            _simNow = new DateTime(d.Year, d.Month, d.Day, _simNow.Hour, _simNow.Minute, _simNow.Second);
            _lastRealTick = DateTime.UtcNow;
        }

        // --- Apply time part from timePicker, keep date ---
        private void ApplyTime()
        {
            var t = timePicker.Value.TimeOfDay;
            _simNow = _simNow.Date.Add(t);
            _lastRealTick = DateTime.UtcNow;
        }

        // ===== System mode + logging =====
        private void SetSystemMode(SM mode)
        {
            _systemMode = mode;
            var text = mode == SM.Normal ? "Normal mode" : "Emergency mode";
            LogService.LogModeChange(CurrentSimulatedTime, text);
        }

        private void btnShowLogFile_Click(object sender, EventArgs e)
        {
            var path = LogService.GetLogFilePath(CurrentSimulatedTime);

            // Ensure file exists so it opens even if empty
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(path)) File.WriteAllText(path, "");

            try
            {
                var psi = new ProcessStartInfo { FileName = path, UseShellExecute = true };
                Process.Start(psi);
            }
            catch
            {
                Process.Start("notepad.exe", path);
            }
        }

        // ===== Helper: wire a room's Enter/Exit buttons =====
        private void WireRoom(Button enterBtn, Button exitBtn, string roomCode, RT roomType)
        {
            if (enterBtn != null) enterBtn.Click += (s, e) => HandleEnter(roomCode, roomType);
            if (exitBtn != null) exitBtn.Click += (s, e) => HandleExit(roomCode, roomType);
        }

        // ===== User Type selection =====
        private void SelectUserType(UT type, Button clicked, bool log = true)
        {
            _currentUserType = type;

            // Simple visual highlight
            btnStaffMember.BackColor        = (clicked == btnStaffMember)        ? Color.LightSkyBlue : SystemColors.Control;
            btnContractCleaner.BackColor    = (clicked == btnContractCleaner)    ? Color.LightSkyBlue : SystemColors.Control;
            btnStudent.BackColor            = (clicked == btnStudent)            ? Color.LightSkyBlue : SystemColors.Control;
            btnManager.BackColor            = (clicked == btnManager)            ? Color.LightSkyBlue : SystemColors.Control;
            btnVisitorGuest.BackColor       = (clicked == btnVisitorGuest)       ? Color.LightSkyBlue : SystemColors.Control;
            btnSecurity.BackColor           = (clicked == btnSecurity)           ? Color.LightSkyBlue : SystemColors.Control;
            btnEmergencyResponder.BackColor = (clicked == btnEmergencyResponder) ? Color.LightSkyBlue : SystemColors.Control;

            if (log)
                LogService.Append(CurrentSimulatedTime, $"[USER] {_currentUserType} selected.");
        }

        // ===== Enter / Exit handlers (use RuleEngine for Enter) =====
        private void HandleEnter(string roomCode, RT roomType)
        {
            var decision = RuleEngine.Evaluate(_currentUserType, roomType, _systemMode, CurrentSimulatedTime);
            var status = decision.Allowed ? "access granted" : "access denied";

            LogService.Append(CurrentSimulatedTime,
                $"[{_systemMode}] {_currentUserType} request access {roomCode} - {status} ({decision.Reason})");
        }

        private void HandleExit(string roomCode, RT roomType)
        {
            // Exits are logged; no rule check needed
            LogService.Append(CurrentSimulatedTime,
                $"[{_systemMode}] {_currentUserType} exit {roomCode}");
        }

        // ====== Other handlers (safe to keep empty) ======
        private void Form1_Load(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
        private void label1_Click_1(object sender, EventArgs e) { }
        private void button2_Click(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label5_Click(object sender, EventArgs e) { }
        private void button9_Click(object sender, EventArgs e) { }
        private void dateTimePicker1_ValueChanged(object sender, EventArgs e) { }
        private void label1_Click_2(object sender, EventArgs e) { }
    }
}
