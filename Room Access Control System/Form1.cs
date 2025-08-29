using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

using Room_Access_Control_System.Services;
using Room_Access_Control_System.Models;

// Enum aliases
using UT = Room_Access_Control_System.Models.UserType;
using RT = Room_Access_Control_System.Models.RoomType;
using SM = Room_Access_Control_System.Models.SystemMode;

namespace Room_Access_Control_System
{
    public partial class Form1 : Form
    {
        // ===== Users tab state =====
        private BindingList<UserModel> _users = new BindingList<UserModel>();
        private DataGridView dgvUsers;
        private TextBox txtUserName, txtCardId;
        private ComboBox cmbUserRole;
        private Button btnAddUser, btnUpdateUser, btnRemoveUser, btnSaveUsers, btnLoadUsers;

        // ===== Simulate named user on Simulation tab =====
        private ComboBox cmbSimUser;          // dropdown on Simulation tab
        private Button btnUseSimUser;         // button on Simulation tab
        private UserModel _simUser;           // currently selected simulated user (optional)

        // ===== Simulated clock state =====
        private DateTime _simNow;
        private DateTime _lastRealTick;
        private Timer _clockTimer;
        private bool _editingDate;
        private bool _editingTime;

        // ===== System mode state =====
        private SM _systemMode = SM.Normal;

        // ===== User type selection (role buttons) =====
        private UT _currentUserType = UT.Student; // default

        // ===== Rooms tab state =====
        private BindingList<RoomModel> _rooms = new BindingList<RoomModel>();
        private DataGridView dgvRooms;
        private TextBox txtRoomCode;
        private ComboBox cmbRoomType, cmbRoomState;
        private Button btnAddRoom, btnUpdateRoom, btnRemoveRoom, btnSaveRooms, btnLoadRooms;

        public Form1()
        {
            InitializeComponent();

            // --- Formats for the two pickers ---
            datePicker.Format = DateTimePickerFormat.Custom;
            datePicker.CustomFormat = "dd/MM/yyyy";
            datePicker.ShowUpDown = false;

            timePicker.Format = DateTimePickerFormat.Custom;
            timePicker.CustomFormat = "HH:mm:ss"; // 24h
            timePicker.ShowUpDown = true;

            // Start simulated time
            _simNow = DateTime.Now;
            _lastRealTick = DateTime.UtcNow;

            datePicker.Value = _simNow.Date;
            timePicker.Value = DateTime.Today.Add(_simNow.TimeOfDay);

            // Timer
            _clockTimer = new Timer { Interval = 250 };
            _clockTimer.Tick += clockTimer_Tick;
            _clockTimer.Start();

            // Editing flags
            datePicker.Enter += (s, e) => _editingDate = true;
            datePicker.DropDown += (s, e) => _editingDate = true;
            datePicker.CloseUp += (s, e) => _editingDate = false;
            datePicker.Leave += (s, e) => _editingDate = false;

            timePicker.Enter += (s, e) => _editingTime = true;
            timePicker.Leave += (s, e) => _editingTime = false;

            datePicker.ValueChanged += (s, e) => { if (_editingDate) ApplyDate(); };
            timePicker.ValueChanged += (s, e) => { if (_editingTime) ApplyTime(); };

            // System mode buttons
            btnSysNormal.Click += (s, e) => SetSystemMode(SM.Normal);
            btnSysEmergency.Click += (s, e) => SetSystemMode(SM.Emergency);
            btnShowLogFile.Click += btnShowLogFile_Click;

            // User type buttons
            btnStaffMember.Click += (s, e) => SelectUserType(UT.StaffMember, btnStaffMember);
            btnContractCleaner.Click += (s, e) => SelectUserType(UT.ContractCleaner, btnContractCleaner);
            btnStudent.Click += (s, e) => SelectUserType(UT.Student, btnStudent);
            btnManager.Click += (s, e) => SelectUserType(UT.Manager, btnManager);
            btnVisitorGuest.Click += (s, e) => SelectUserType(UT.VisitorGuest, btnVisitorGuest);
            btnSecurity.Click += (s, e) => SelectUserType(UT.Security, btnSecurity);
            btnEmergencyResponder.Click += (s, e) => SelectUserType(UT.EmergencyResponder, btnEmergencyResponder);

            // Default highlight (no log)
            SelectUserType(_currentUserType, btnStudent, log: false);

            // Wire rooms (Simulation tab)
            WireRoom(btnEnterMH100, btnExitMH100, "M.H.100 (Main Hallway)", RT.Hallway);
            WireRoom(btnEnterAR101, btnExitAR101, "A.R.101 (Alpha Teaching Room)", RT.TeachingRoom);
            WireRoom(btnEnterAR102, btnExitAR102, "A.R.102 (Alpha Lecture Hall)", RT.LectureHall);
            WireRoom(btnEnterAR103, btnExitAR103, "A.R.103 (Alpha Staff Room)", RT.StaffRoom);
            WireRoom(btnEnterAR104, btnExitAR104, "A.R.104 (Alpha Secure Room)", RT.SecureRoom);
            WireRoom(btnEnterAH100, btnExitAH100, "A.H.100 (Alpha Hallway)", RT.Hallway);
            WireRoom(btnEnterBR101, btnExitBR101, "B.R.101 (Beta Teaching Room)", RT.TeachingRoom);
            WireRoom(btnEnterBR102, btnExitBR102, "B.R.102 (Beta Lecture Hall)", RT.LectureHall);
            WireRoom(btnEnterBR103, btnExitBR103, "B.R.103 (Beta Staff Room)", RT.StaffRoom);
            WireRoom(btnEnterBR104, btnExitBR104, "B.R.104 (Beta Secure Room)", RT.SecureRoom);
            WireRoom(btnEnterBH100, btnExitBH100, "B.H.100 (Beta Hallway)", RT.Hallway);

            // Build Management tabs
            BuildRoomsTabUI();
            LoadRoomsToGrid();
            BuildUsersTabUI();
            LoadUsersToGrid();   // also populates the Simulation user picker

            // Add Simulation user picker (so we can swipe as a named person)
            SetupSimulationUserPicker();

            // Keep picker in sync when Users list changes
            _users.ListChanged += (s, e) => RefreshSimUserPicker();
        }

        // ===== Clock =====
        public DateTime CurrentSimulatedTime => _simNow;
        public UT CurrentUserType => _currentUserType;

        private void clockTimer_Tick(object sender, EventArgs e)
        {
            var nowUtc = DateTime.UtcNow;
            var elapsed = nowUtc - _lastRealTick;
            _lastRealTick = nowUtc;
            _simNow = _simNow.Add(elapsed);

            if (!_editingDate) datePicker.Value = _simNow.Date;
            if (!_editingTime) timePicker.Value = DateTime.Today.Add(_simNow.TimeOfDay);
        }

        private void ApplyDate()
        {
            var d = datePicker.Value.Date;
            _simNow = new DateTime(d.Year, d.Month, d.Day, _simNow.Hour, _simNow.Minute, _simNow.Second);
            _lastRealTick = DateTime.UtcNow;
        }

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

        // ===== Wire room buttons =====
        private void WireRoom(Button enterBtn, Button exitBtn, string roomCode, RT roomType)
        {
            if (enterBtn != null) enterBtn.Click += (s, e) => HandleEnter(roomCode, roomType);
            if (exitBtn != null) exitBtn.Click += (s, e) => HandleExit(roomCode, roomType);
        }

        // ===== User Type selection (role buttons) =====
        private void SelectUserType(UT type, Button clicked, bool log = true)
        {
            _currentUserType = type;

            btnStaffMember.BackColor = (clicked == btnStaffMember) ? Color.LightSkyBlue : SystemColors.Control;
            btnContractCleaner.BackColor = (clicked == btnContractCleaner) ? Color.LightSkyBlue : SystemColors.Control;
            btnStudent.BackColor = (clicked == btnStudent) ? Color.LightSkyBlue : SystemColors.Control;
            btnManager.BackColor = (clicked == btnManager) ? Color.LightSkyBlue : SystemColors.Control;
            btnVisitorGuest.BackColor = (clicked == btnVisitorGuest) ? Color.LightSkyBlue : SystemColors.Control;
            btnSecurity.BackColor = (clicked == btnSecurity) ? Color.LightSkyBlue : SystemColors.Control;
            btnEmergencyResponder.BackColor = (clicked == btnEmergencyResponder) ? Color.LightSkyBlue : SystemColors.Control;

            if (log) LogService.Append(CurrentSimulatedTime, $"[USER] {_currentUserType} selected.");
        }

        // ===== Simulation user picker (named people) =====
        private void SetupSimulationUserPicker()
        {
            // Place near top-right of Simulation tab
            cmbSimUser = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 220,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnUseSimUser = new Button
            {
                Text = "Use User",
                Width = 90,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // Position (adjust if needed)
            int margin = 10;
            cmbSimUser.Location = new Point(tabSimulation.ClientSize.Width - cmbSimUser.Width - btnUseSimUser.Width - (margin * 2),
                                            margin);
            btnUseSimUser.Location = new Point(tabSimulation.ClientSize.Width - btnUseSimUser.Width - margin, margin);

            // Reposition on resize
            tabSimulation.Resize += (s, e) =>
            {
                cmbSimUser.Location = new Point(tabSimulation.ClientSize.Width - cmbSimUser.Width - btnUseSimUser.Width - (margin * 2), margin);
                btnUseSimUser.Location = new Point(tabSimulation.ClientSize.Width - btnUseSimUser.Width - margin, margin);
            };

            btnUseSimUser.Click += (s, e) =>
            {
                var selected = cmbSimUser.SelectedItem as UserModel;
                if (selected == null) return;

                _simUser = selected;
                // set current role to user's role and highlight role button
                var roleBtn = ButtonForRole(_simUser.Role);
                SelectUserType(_simUser.Role, roleBtn ?? btnStudent);
                LogService.Append(CurrentSimulatedTime, $"[SIM USER] {_simUser.Name} (ID:{_simUser.CardId}) selected, role={_simUser.Role}");
            };

            tabSimulation.Controls.Add(cmbSimUser);
            tabSimulation.Controls.Add(btnUseSimUser);

            RefreshSimUserPicker();
        }

        private void RefreshSimUserPicker()
        {
            if (cmbSimUser == null) return;
            var remember = _simUser != null ? _simUser.CardId : null;

            cmbSimUser.DataSource = null;
            // copy to a new list so we can control display order (alphabetical)
            var list = _users.OrderBy(u => u.Name).ToList();
            cmbSimUser.DataSource = list;
            cmbSimUser.DisplayMember = "Name";
            cmbSimUser.ValueMember = "CardId";

            if (!string.IsNullOrEmpty(remember))
            {
                var idx = list.FindIndex(u => string.Equals(u.CardId, remember, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) cmbSimUser.SelectedIndex = idx;
            }
            else if (list.Count > 0)
            {
                cmbSimUser.SelectedIndex = 0;
            }
        }

        private Button ButtonForRole(UT role)
        {
            switch (role)
            {
                case UT.StaffMember: return btnStaffMember;
                case UT.ContractCleaner: return btnContractCleaner;
                case UT.Student: return btnStudent;
                case UT.Manager: return btnManager;
                case UT.VisitorGuest: return btnVisitorGuest;
                case UT.Security: return btnSecurity;
                case UT.EmergencyResponder: return btnEmergencyResponder;
                default: return btnStudent;
            }
        }

        // ===== Enter / Exit =====
        private void HandleEnter(string roomCode, RT roomType)
        {
            var decision = RuleEngine.Evaluate(_currentUserType, roomType, _systemMode, CurrentSimulatedTime);
            var status = decision.Allowed ? "access granted" : "access denied";

            string userTag = _simUser != null ? $"[{_simUser.Name}|ID:{_simUser.CardId}] " : "";

            LogService.Append(CurrentSimulatedTime,
                $"{userTag}[{_systemMode}] {_currentUserType} request access {roomCode} - {status} ({decision.Reason})");
        }

        private void HandleExit(string roomCode, RT roomType)
        {
            string userTag = _simUser != null ? $"[{_simUser.Name}|ID:{_simUser.CardId}] " : "";
            LogService.Append(CurrentSimulatedTime,
                $"{userTag}[{_systemMode}] {_currentUserType} exit {roomCode}");
        }

        // ===== Rooms Tab (Management v2) =====
        private void BuildRoomsTabUI()
        {
            // Grid
            dgvRooms = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = false,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            var colCode = new DataGridViewTextBoxColumn { HeaderText = "Room Code", DataPropertyName = "Code", Width = 140 };
            var colType = new DataGridViewComboBoxColumn
            {
                HeaderText = "Room Type",
                DataPropertyName = "Type",
                DataSource = Enum.GetValues(typeof(RT)),
                Width = 140
            };
            var colState = new DataGridViewComboBoxColumn
            {
                HeaderText = "Room State",
                DataPropertyName = "State",
                DataSource = Enum.GetValues(typeof(SM)),
                Width = 140
            };

            dgvRooms.Columns.AddRange(colCode, colType, colState);

            // Editor panel
            var editor = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(8) };
            var lblCode = new Label { Text = "Code:", Left = 8, Top = 6, AutoSize = true };
            txtRoomCode = new TextBox { Width = 140, Left = 8, Top = 24 };
            var lblType = new Label { Text = "Type:", Left = 170, Top = 6, AutoSize = true };
            cmbRoomType = new ComboBox { Width = 150, Left = 170, Top = 24, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRoomType.Items.AddRange(Enum.GetNames(typeof(RT)));
            var lblState = new Label { Text = "State:", Left = 340, Top = 6, AutoSize = true };
            cmbRoomState = new ComboBox { Width = 150, Left = 340, Top = 24, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRoomState.Items.AddRange(Enum.GetNames(typeof(SM)));
            btnAddRoom = new Button { Text = "Add", Left = 510, Top = 20, Width = 70 };
            btnUpdateRoom = new Button { Text = "Update", Left = 585, Top = 20, Width = 70 };
            btnRemoveRoom = new Button { Text = "Remove", Left = 660, Top = 20, Width = 70 };
            btnSaveRooms = new Button { Text = "Save", Left = 735, Top = 20, Width = 70 };
            btnLoadRooms = new Button { Text = "Load", Left = 810, Top = 20, Width = 70 };

            editor.Controls.AddRange(new Control[] {
                lblCode, txtRoomCode, lblType, cmbRoomType, lblState, cmbRoomState,
                btnAddRoom, btnUpdateRoom, btnRemoveRoom, btnSaveRooms, btnLoadRooms
            });

            tabRooms.Controls.Add(dgvRooms);
            tabRooms.Controls.Add(editor);

            // Bind + events
            dgvRooms.DataSource = _rooms;
            dgvRooms.SelectionChanged += DgvRooms_SelectionChanged;
            dgvRooms.CellValueChanged += DgvRooms_CellValueChanged;
            dgvRooms.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvRooms.IsCurrentCellDirty)
                    dgvRooms.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            btnAddRoom.Click += BtnAddRoom_Click;
            btnUpdateRoom.Click += BtnUpdateRoom_Click;
            btnRemoveRoom.Click += BtnRemoveRoom_Click;
            btnSaveRooms.Click += BtnSaveRooms_Click;
            btnLoadRooms.Click += BtnLoadRooms_Click;
        }

        private void LoadRoomsToGrid()
        {
            var loaded = RoomStore.LoadRooms();
            _rooms.Clear();
            foreach (var r in loaded) _rooms.Add(r);
            UpdateGlobalModeFromRoomStates();
        }

        private void DgvRooms_SelectionChanged(object sender, EventArgs e)
        {
            var r = dgvRooms.CurrentRow?.DataBoundItem as RoomModel;
            if (r == null) return;
            txtRoomCode.Text = r.Code;
            cmbRoomType.SelectedItem = r.Type.ToString();
            cmbRoomState.SelectedItem = r.State.ToString();
        }

        private void DgvRooms_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgvRooms.Columns[e.ColumnIndex];
            if (col == null) return;
            if (string.Equals(col.DataPropertyName, "State", StringComparison.OrdinalIgnoreCase))
            {
                UpdateGlobalModeFromRoomStates();
            }
        }

        private void UpdateGlobalModeFromRoomStates()
        {
            bool anyEmergency = _rooms.Any(r => r.State == SM.Emergency);
            if (anyEmergency && _systemMode != SM.Emergency)
                SetSystemMode(SM.Emergency);
            else if (!anyEmergency && _systemMode != SM.Normal)
                SetSystemMode(SM.Normal);
        }

        private void BtnAddRoom_Click(object sender, EventArgs e)
        {
            var code = (txtRoomCode.Text ?? "").Trim();
            if (string.IsNullOrEmpty(code)) { MessageBox.Show("Enter a room code."); return; }
            if (_rooms.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)))
            { MessageBox.Show("Room code already exists."); return; }

            RT type; SM state;
            if (!Enum.TryParse<RT>(SafeSel(cmbRoomType), out type)) { MessageBox.Show("Select a room type."); return; }
            if (!Enum.TryParse<SM>(SafeSel(cmbRoomState), out state)) state = SM.Normal;

            _rooms.Add(new RoomModel { Code = code, Type = type, State = state });
            UpdateGlobalModeFromRoomStates();
        }

        private void BtnUpdateRoom_Click(object sender, EventArgs e)
        {
            var r = dgvRooms.CurrentRow?.DataBoundItem as RoomModel;
            if (r == null) { MessageBox.Show("Select a room to update."); return; }

            var newCode = (txtRoomCode.Text ?? "").Trim();
            if (string.IsNullOrEmpty(newCode)) { MessageBox.Show("Enter a room code."); return; }
            if (_rooms.Any(x => !object.ReferenceEquals(x, r) &&
                                string.Equals(x.Code, newCode, StringComparison.OrdinalIgnoreCase)))
            { MessageBox.Show("Another room with this code exists."); return; }

            RT type; SM state;
            if (!Enum.TryParse<RT>(SafeSel(cmbRoomType), out type)) { MessageBox.Show("Select a room type."); return; }
            if (!Enum.TryParse<SM>(SafeSel(cmbRoomState), out state)) state = SM.Normal;

            r.Code = newCode; r.Type = type; r.State = state;
            dgvRooms.Refresh();
            UpdateGlobalModeFromRoomStates();
        }

        private void BtnRemoveRoom_Click(object sender, EventArgs e)
        {
            var r = dgvRooms.CurrentRow?.DataBoundItem as RoomModel;
            if (r == null) { MessageBox.Show("Select a room to remove."); return; }
            _rooms.Remove(r);
            UpdateGlobalModeFromRoomStates();
        }

        private void BtnSaveRooms_Click(object sender, EventArgs e)
        {
            RoomStore.SaveRooms(_rooms.ToList());
            MessageBox.Show("Rooms saved.");
        }

        private void BtnLoadRooms_Click(object sender, EventArgs e)
        {
            LoadRoomsToGrid();
            MessageBox.Show("Rooms loaded.");
        }

        private static string SafeSel(ComboBox cmb) =>
            cmb.SelectedItem != null ? cmb.SelectedItem.ToString() : "";

        // ===== Users Tab (Management v2) =====
        private void BuildUsersTabUI()
        {
            dgvUsers = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = false,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            var colName = new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "Name", Width = 200 };
            var colCard = new DataGridViewTextBoxColumn { HeaderText = "Card ID", DataPropertyName = "CardId", Width = 120 };
            var colRole = new DataGridViewComboBoxColumn
            {
                HeaderText = "Role",
                DataPropertyName = "Role",
                DataSource = Enum.GetValues(typeof(UT)),
                Width = 150
            };

            dgvUsers.Columns.AddRange(colName, colCard, colRole);

            // Editor panel
            var editor = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(8) };

            var lblName = new Label { Text = "Name:", Left = 8, Top = 6, AutoSize = true };
            txtUserName = new TextBox { Width = 180, Left = 8, Top = 24 };

            var lblCard = new Label { Text = "Card ID:", Left = 200, Top = 6, AutoSize = true };
            txtCardId = new TextBox { Width = 120, Left = 200, Top = 24 };

            var lblRole = new Label { Text = "Role:", Left = 340, Top = 6, AutoSize = true };
            cmbUserRole = new ComboBox { Width = 150, Left = 340, Top = 24, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbUserRole.Items.AddRange(Enum.GetNames(typeof(UT)));

            btnAddUser = new Button { Text = "Add", Left = 510, Top = 20, Width = 70 };
            btnUpdateUser = new Button { Text = "Update", Left = 585, Top = 20, Width = 70 };
            btnRemoveUser = new Button { Text = "Remove", Left = 660, Top = 20, Width = 70 };
            btnSaveUsers = new Button { Text = "Save", Left = 735, Top = 20, Width = 70 };
            btnLoadUsers = new Button { Text = "Load", Left = 810, Top = 20, Width = 70 };

            editor.Controls.AddRange(new Control[] {
                lblName, txtUserName, lblCard, txtCardId, lblRole, cmbUserRole,
                btnAddUser, btnUpdateUser, btnRemoveUser, btnSaveUsers, btnLoadUsers
            });

            tabUsers.Controls.Add(dgvUsers);
            tabUsers.Controls.Add(editor);

            // Bind + events
            dgvUsers.DataSource = _users;
            dgvUsers.SelectionChanged += DgvUsers_SelectionChanged;
            btnAddUser.Click += BtnAddUser_Click;
            btnUpdateUser.Click += BtnUpdateUser_Click;
            btnRemoveUser.Click += BtnRemoveUser_Click;
            btnSaveUsers.Click += BtnSaveUsers_Click;
            btnLoadUsers.Click += BtnLoadUsers_Click;
        }

        private void LoadUsersToGrid()
        {
            var loaded = UserStore.LoadUsers();
            _users.Clear();
            foreach (var u in loaded) _users.Add(u);
            RefreshSimUserPicker();
        }

        private void DgvUsers_SelectionChanged(object sender, EventArgs e)
        {
            var u = dgvUsers.CurrentRow?.DataBoundItem as UserModel;
            if (u == null) return;
            txtUserName.Text = u.Name;
            txtCardId.Text = u.CardId;
            cmbUserRole.SelectedItem = u.Role.ToString();
        }

        private void BtnAddUser_Click(object sender, EventArgs e)
        {
            var name = (txtUserName.Text ?? "").Trim();
            var card = (txtCardId.Text ?? "").Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(card))
            { MessageBox.Show("Enter both name and Card ID."); return; }

            if (_users.Any(x => string.Equals(x.CardId, card, StringComparison.OrdinalIgnoreCase)))
            { MessageBox.Show("Card ID already exists."); return; }

            UT role;
            if (!Enum.TryParse<UT>(SafeSel(cmbUserRole), out role)) role = UT.Student;

            _users.Add(new UserModel { Name = name, CardId = card, Role = role });
            SortUsers();
            RefreshSimUserPicker();
        }

        private void BtnUpdateUser_Click(object sender, EventArgs e)
        {
            var u = dgvUsers.CurrentRow?.DataBoundItem as UserModel;
            if (u == null) { MessageBox.Show("Select a user to update."); return; }

            var newName = (txtUserName.Text ?? "").Trim();
            var newCard = (txtCardId.Text ?? "").Trim();
            if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newCard))
            { MessageBox.Show("Enter both name and Card ID."); return; }

            if (_users.Any(x => !object.ReferenceEquals(x, u) &&
                                string.Equals(x.CardId, newCard, StringComparison.OrdinalIgnoreCase)))
            { MessageBox.Show("Another user already has this Card ID."); return; }

            UT role;
            if (!Enum.TryParse<UT>(SafeSel(cmbUserRole), out role)) role = UT.Student;

            u.Name = newName;
            u.CardId = newCard;
            u.Role = role;
            SortUsers();
            dgvUsers.Refresh();
            RefreshSimUserPicker();
        }

        private void BtnRemoveUser_Click(object sender, EventArgs e)
        {
            var u = dgvUsers.CurrentRow?.DataBoundItem as UserModel;
            if (u == null) { MessageBox.Show("Select a user to remove."); return; }
            _users.Remove(u);
            RefreshSimUserPicker();
        }

        private void BtnSaveUsers_Click(object sender, EventArgs e)
        {
            UserStore.SaveUsers(_users.ToList());
            MessageBox.Show("Users saved.");
        }

        private void BtnLoadUsers_Click(object sender, EventArgs e)
        {
            LoadUsersToGrid();
            MessageBox.Show("Users loaded.");
        }

        private void SortUsers()
        {
            var sorted = _users.OrderBy(u => u.Name).ToList();
            _users.Clear();
            foreach (var u in sorted) _users.Add(u);   // correctly add back into _users
        }

        // ====== Empty designer handlers ======
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
