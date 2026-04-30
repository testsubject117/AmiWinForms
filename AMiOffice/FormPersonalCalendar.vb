Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Public Class FormPersonalCalendar

    ' Per your instruction: under the share
    Private Const DataDir As String = "\\invoice\mainmenu\data"

    Private ReadOnly DatesPmsPath As String = Path.Combine(DataDir, "DATES.PMS")
    Private ReadOnly DatesPmsTmpPath As String = Path.Combine(DataDir, "DATES.PMS.tmp")
    Private ReadOnly DatesPmsBakPath As String = Path.Combine(DataDir, "DATES.PMS.bak")
    Private ReadOnly DatesPmsLockPath As String = Path.Combine(DataDir, "DATES.PMS.lock")

    Private ReadOnly PmsNumPath As String = Path.Combine(DataDir, "PMS.NUM")
    Private ReadOnly PmsNumTmpPath As String = Path.Combine(DataDir, "PMS.NUM.tmp")
    Private ReadOnly PmsNumBakPath As String = Path.Combine(DataDir, "PMS.NUM.bak")
    Private ReadOnly PmsNumLockPath As String = Path.Combine(DataDir, "PMS.NUM.lock")

    ' View is a month/year, but entries are recurring by month/day regardless of stored year.
    Private _viewMonth As DateTime = New DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)

    Private _allEntries As New List(Of PmsEntry)()

    ' UI
    Private _lblHeaderRight As Label = Nothing
    Private _lblMonth As Label = Nothing
    Private _dtJump As DateTimePicker = Nothing

    Private _txtSearch As TextBox = Nothing
    Private _radSearchThisMonth As RadioButton = Nothing
    Private _radSearchAllMonths As RadioButton = Nothing

    Private _grid As DataGridView = Nothing
    Private _lblStatus As Label = Nothing

    Public Sub New()
        InitializeComponent()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Me.Text = "Personal Calendar"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.KeyPreview = True
        Me.MinimumSize = New Size(1180, 680)

        If Not EnsureDataDirExistsFriendly() Then
            Me.Close()
            Return
        End If

        EnsureFilesExist()
        EnsureBackupsExist()

        BuildUi()

        LoadAllData()
        LoadViewMonthFromPmsNum()
        SyncJumpPicker()
        RefreshMonthLabel()
        BindGrid()
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)

        If e.KeyCode = Keys.Escape Then
            Me.Close()
            e.Handled = True
            Return
        End If

        If e.KeyCode = Keys.PageDown Then
            NextMonth()
            e.Handled = True
            Return
        End If

        If e.KeyCode = Keys.PageUp Then
            PrevMonth()
            e.Handled = True
            Return
        End If

        If e.KeyCode = Keys.Insert Then
            AddEvent()
            e.Handled = True
            Return
        End If

        If e.KeyCode = Keys.Delete Then
            DeleteSelected()
            e.Handled = True
            Return
        End If

        If e.Control AndAlso e.KeyCode = Keys.G Then
            If _dtJump IsNot Nothing Then _dtJump.Focus()
            e.Handled = True
            Return
        End If

        If e.Control AndAlso e.KeyCode = Keys.F Then
            If _txtSearch IsNot Nothing Then _txtSearch.Focus()
            _txtSearch.SelectAll()
            e.Handled = True
            Return
        End If

        If e.Control AndAlso e.KeyCode = Keys.R Then
            LoadAllData()
            BindGrid()
            e.Handled = True
            Return
        End If

        ' Hidden maintenance hotkey: Ctrl+Shift+R
        If e.Control AndAlso e.Shift AndAlso e.KeyCode = Keys.R Then
            RepairDatesPms()
            e.Handled = True
            Return
        End If
    End Sub

    ' =========================
    ' Model
    ' =========================
    Private NotInheritable Class PmsEntry
        Public Property StoredDate As DateTime     ' Date in file
        Public Property Text As String
    End Class

    Private NotInheritable Class GridRow
        Public Property ViewMonth As DateTime      ' month/year the row is being shown in (for Day/Date)
        Public Property OccurrenceDate As DateTime ' computed from ViewMonth.Year + StoredDate (month/day)
        Public Property DayName As String
        Public Property Text As String
        Public Property StoredDate As DateTime
    End Class

    ' =========================
    ' UI
    ' =========================
    Private Sub BuildUi()
        Dim root As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(10),
            .ColumnCount = 1,
            .RowCount = 3
        }
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))

        ' Header
        Dim header As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1}
        header.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        header.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))

        Dim lblTitle As New Label() With {.AutoSize = True, .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold), .Text = "Personal Calendar"}
        _lblHeaderRight = New Label() With {.AutoSize = True, .Font = New Font("Segoe UI", 10.0F), .Text = DateTime.Now.ToString("MM-dd-yy  ddd  h:mm tt", CultureInfo.InvariantCulture)}
        header.Controls.Add(lblTitle, 0, 0)
        header.Controls.Add(_lblHeaderRight, 1, 0)

        ' Body
        Dim body As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2}
        body.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        body.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        Dim nav As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoSize = True, .WrapContents = True}

        Dim btnPrev As New Button() With {.Text = "< Previous Month (PgUp)", .AutoSize = True}
        AddHandler btnPrev.Click, Sub() PrevMonth()

        Dim btnNext As New Button() With {.Text = "Next Month (PgDn) >", .AutoSize = True}
        AddHandler btnNext.Click, Sub() NextMonth()

        _lblMonth = New Label() With {.AutoSize = True, .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold), .Padding = New Padding(12, 7, 12, 0)}

        Dim lblGo As New Label() With {.AutoSize = True, .Text = "Go to:", .Padding = New Padding(12, 10, 0, 0)}
        _dtJump = New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = "MMMM yyyy", .ShowUpDown = True, .Width = 160, .Value = _viewMonth}

        Dim btnGo As New Button() With {.Text = "Go (Enter)", .AutoSize = True}
        AddHandler btnGo.Click, Sub() GoToMonth(New DateTime(_dtJump.Value.Year, _dtJump.Value.Month, 1))
        AddHandler _dtJump.KeyDown,
            Sub(sender, e)
                If e.KeyCode = Keys.Enter Then
                    GoToMonth(New DateTime(_dtJump.Value.Year, _dtJump.Value.Month, 1))
                    e.Handled = True
                End If
            End Sub

        Dim lblSearch As New Label() With {.AutoSize = True, .Text = "Search:", .Padding = New Padding(12, 10, 0, 0)}
        _txtSearch = New TextBox() With {.Width = 220}
        AddHandler _txtSearch.TextChanged, Sub() BindGrid()

        _radSearchThisMonth = New RadioButton() With {.AutoSize = True, .Text = "This month", .Checked = True, .Padding = New Padding(6, 8, 0, 0)}
        _radSearchAllMonths = New RadioButton() With {.AutoSize = True, .Text = "All months", .Padding = New Padding(6, 8, 0, 0)}
        AddHandler _radSearchThisMonth.CheckedChanged, Sub() BindGrid()
        AddHandler _radSearchAllMonths.CheckedChanged, Sub() BindGrid()

        Dim btnClearSearch As New Button() With {.Text = "Clear", .AutoSize = True}
        AddHandler btnClearSearch.Click, Sub() _txtSearch.Clear()

        Dim btnAdd As New Button() With {.Text = "Add (Ins)", .AutoSize = True}
        AddHandler btnAdd.Click, Sub() AddEvent()

        Dim btnDelete As New Button() With {.Text = "Delete (Del)", .AutoSize = True}
        AddHandler btnDelete.Click, Sub() DeleteSelected()

        Dim btnReload As New Button() With {.Text = "Reload (Ctrl+R)", .AutoSize = True}
        AddHandler btnReload.Click, Sub()
                                        LoadAllData()
                                        BindGrid()
                                    End Sub

        Dim btnRepair As New Button() With {.Text = "Repair (Ctrl+Shift+R)", .AutoSize = True}
        AddHandler btnRepair.Click, Sub() RepairDatesPms()

        nav.Controls.Add(btnPrev)
        nav.Controls.Add(btnNext)
        nav.Controls.Add(_lblMonth)
        nav.Controls.Add(lblGo)
        nav.Controls.Add(_dtJump)
        nav.Controls.Add(btnGo)
        nav.Controls.Add(New Label() With {.AutoSize = True, .Text = "   "})
        nav.Controls.Add(lblSearch)
        nav.Controls.Add(_txtSearch)
        nav.Controls.Add(btnClearSearch)
        nav.Controls.Add(_radSearchThisMonth)
        nav.Controls.Add(_radSearchAllMonths)
        nav.Controls.Add(New Label() With {.AutoSize = True, .Text = "   "})
        nav.Controls.Add(btnAdd)
        nav.Controls.Add(btnDelete)
        nav.Controls.Add(btnReload)
        nav.Controls.Add(btnRepair)

        _grid = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = True,
            .AutoGenerateColumns = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }

        ConfigureGridColumns()

        AddHandler _grid.CellDoubleClick,
            Sub(sender, e)
                If e.RowIndex < 0 Then Return
                Dim r = TryCast(_grid.Rows(e.RowIndex).Tag, GridRow)
                If r Is Nothing Then Return

                If _radSearchAllMonths IsNot Nothing AndAlso _radSearchAllMonths.Checked Then
                    _radSearchThisMonth.Checked = True
                    GoToMonth(New DateTime(_viewMonth.Year, r.StoredDate.Month, 1))
                    _txtSearch.Focus()
                    _txtSearch.SelectAll()
                End If
            End Sub

        body.Controls.Add(nav, 0, 0)
        body.Controls.Add(_grid, 0, 1)

        ' Footer
        Dim footer As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1}
        footer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        footer.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))

        _lblStatus = New Label() With {.AutoSize = True}
        Dim btnClose As New Button() With {.Text = "Close (Esc)", .AutoSize = True}
        AddHandler btnClose.Click, Sub() Me.Close()

        footer.Controls.Add(_lblStatus, 0, 0)
        footer.Controls.Add(btnClose, 1, 0)

        root.Controls.Add(header, 0, 0)
        root.Controls.Add(body, 0, 1)
        root.Controls.Add(footer, 0, 2)

        Me.Controls.Clear()
        Me.Controls.Add(root)

        ' clock
        Dim t As New Timer() With {.Interval = 1000}
        AddHandler t.Tick, Sub()
                               If _lblHeaderRight IsNot Nothing Then
                                   _lblHeaderRight.Text = DateTime.Now.ToString("MM-dd-yy  ddd  h:mm tt", CultureInfo.InvariantCulture)
                               End If
                           End Sub
        t.Start()
    End Sub

    Private Sub ConfigureGridColumns()
        _grid.Columns.Clear()

        ' Month (only used in All-months mode)
        _grid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Month",
            .HeaderText = "Month",
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            .Width = 60
        })

        ' Stored Year (only used in All-months mode)
        _grid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "StoredYear",
            .HeaderText = "Year",
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            .Width = 55
        })

        _grid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Date",
            .HeaderText = "Date",
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            .Width = 95
        })
        _grid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Day",
            .HeaderText = "Day",
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            .Width = 55
        })
        _grid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Event",
            .HeaderText = "Event"
        })
    End Sub

    Private Sub RefreshMonthLabel()
        _lblMonth.Text = _viewMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture)
    End Sub

    Private Sub SyncJumpPicker()
        If _dtJump IsNot Nothing Then _dtJump.Value = _viewMonth
    End Sub

    ' =========================
    ' Month navigation
    ' =========================
    Private Sub PrevMonth()
        GoToMonth(_viewMonth.AddMonths(-1))
    End Sub

    Private Sub NextMonth()
        GoToMonth(_viewMonth.AddMonths(1))
    End Sub

    Private Sub GoToMonth(newMonth As DateTime)
        _viewMonth = New DateTime(newMonth.Year, newMonth.Month, 1)
        SyncJumpPicker()
        RefreshMonthLabel()
        BindGrid()
        SaveViewMonthToPmsNum()
    End Sub

    ' =========================
    ' Data I/O
    ' =========================
    Private Function EnsureDataDirExistsFriendly() As Boolean
        Try
            If Directory.Exists(DataDir) Then Return True
            MessageBox.Show("Personal Calendar cannot open because the data folder is not available:" & Environment.NewLine & DataDir,
                            "Personal Calendar", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        Catch ex As Exception
            MessageBox.Show("Personal Calendar cannot access the data folder:" & Environment.NewLine & DataDir & Environment.NewLine & Environment.NewLine & ex.Message,
                            "Personal Calendar", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Private Sub EnsureFilesExist()
        Try
            If Not File.Exists(DatesPmsPath) Then
                Dim headerLine = ToBasicWriteLine(New String() {New String("1"c, 60), "1", "31"})
                File.WriteAllText(DatesPmsPath, headerLine & Environment.NewLine, Encoding.ASCII)
            End If

            If Not File.Exists(PmsNumPath) Then
                Dim initLine = ToBasicWriteLine(New String() {"0", DateTime.Today.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture), "0"})
                File.WriteAllText(PmsNumPath, initLine & Environment.NewLine, Encoding.ASCII)
            End If
        Catch ex As Exception
            MessageBox.Show("Could not create/open PMS files in:" & Environment.NewLine & DataDir & Environment.NewLine & Environment.NewLine & ex.Message,
                            "Personal Calendar", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Me.Close()
        End Try
    End Sub

    Private Sub EnsureBackupsExist()
        Try
            If File.Exists(DatesPmsPath) AndAlso Not File.Exists(DatesPmsBakPath) Then
                File.Copy(DatesPmsPath, DatesPmsBakPath, overwrite:=False)
            End If
        Catch
        End Try

        Try
            If File.Exists(PmsNumPath) AndAlso Not File.Exists(PmsNumBakPath) Then
                File.Copy(PmsNumPath, PmsNumBakPath, overwrite:=False)
            End If
        Catch
        End Try
    End Sub

    Private Sub LoadAllData()
        _allEntries.Clear()

        Dim lines = File.ReadAllLines(DatesPmsPath, Encoding.ASCII).
            Select(Function(l) StripDosEofOnly(l).Trim()).
            Where(Function(l) l.Length > 0).
            ToList()

        If lines.Count = 0 Then Return

        Dim startIndex As Integer = 0

        Dim headerParts = ParseBasicWriteLine(lines(0))
        If headerParts.Count >= 3 Then
            Dim mask = headerParts(0)
            If Not String.IsNullOrEmpty(mask) AndAlso mask.All(Function(ch) ch = "1"c) Then startIndex = 1
        End If

        For i As Integer = startIndex To lines.Count - 1
            Dim parts = ParseBasicWriteLine(lines(i))
            If parts.Count < 2 Then Continue For

            Dim dtText = parts(0)
            Dim txt = parts(1)

            Dim dt As DateTime
            If DateTime.TryParseExact(dtText, "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, dt) Then
                _allEntries.Add(New PmsEntry With {.StoredDate = dt.Date, .Text = If(txt, "").Trim()})
            End If
        Next
    End Sub

    ' =========================
    ' Binding (month view OR all-months search)
    ' =========================
    Private Sub BindGrid()
        If _grid Is Nothing Then Return

        Dim q As String = If(_txtSearch Is Nothing, "", _txtSearch.Text.Trim())
        Dim allMonths As Boolean = (_radSearchAllMonths IsNot Nothing AndAlso _radSearchAllMonths.Checked)

        Dim visibleEntries As IEnumerable(Of PmsEntry) = _allEntries

        If q.Length > 0 Then
            visibleEntries = visibleEntries.Where(Function(e) e.Text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
        End If

        Dim rows As New List(Of GridRow)()

        If allMonths Then
            For Each e In visibleEntries
                Dim occ As DateTime
                Try
                    occ = New DateTime(_viewMonth.Year, e.StoredDate.Month, e.StoredDate.Day)
                Catch
                    Continue For
                End Try

                rows.Add(New GridRow With {
                    .ViewMonth = New DateTime(_viewMonth.Year, e.StoredDate.Month, 1),
                    .OccurrenceDate = occ,
                    .DayName = occ.ToString("ddd", CultureInfo.InvariantCulture),
                    .Text = e.Text,
                    .StoredDate = e.StoredDate
                })
            Next

            rows = rows.
                OrderBy(Function(r) r.StoredDate.Month).
                ThenBy(Function(r) r.StoredDate.Day).
                ThenBy(Function(r) r.StoredDate.Year).
                ThenBy(Function(r) r.Text).
                ToList()
        Else
            Dim month As Integer = _viewMonth.Month
            Dim year As Integer = _viewMonth.Year

            For Each e In visibleEntries.Where(Function(x) x.StoredDate.Month = month)
                Dim occ As DateTime
                Try
                    occ = New DateTime(year, e.StoredDate.Month, e.StoredDate.Day)
                Catch
                    Continue For
                End Try

                rows.Add(New GridRow With {
                    .ViewMonth = _viewMonth,
                    .OccurrenceDate = occ,
                    .DayName = occ.ToString("ddd", CultureInfo.InvariantCulture),
                    .Text = e.Text,
                    .StoredDate = e.StoredDate
                })
            Next

            rows = rows.
                OrderBy(Function(r) r.OccurrenceDate.Day).
                ThenBy(Function(r) r.Text).
                ToList()
        End If

        _grid.Rows.Clear()

        For Each r In rows
            Dim monthDisplay As String = If(allMonths, r.StoredDate.ToString("MM", CultureInfo.InvariantCulture), "")
            Dim yearDisplay As String = If(allMonths, r.StoredDate.ToString("yy", CultureInfo.InvariantCulture), "")
            Dim dateDisplay As String = r.OccurrenceDate.ToString("MM-dd-yy", CultureInfo.InvariantCulture)
            Dim idx = _grid.Rows.Add(monthDisplay, yearDisplay, dateDisplay, r.DayName, r.Text)
            _grid.Rows(idx).Tag = r
        Next

        UpdateStatus(rows.Count, q, allMonths)
    End Sub

    Private Sub UpdateStatus(visibleCount As Integer, search As String, allMonths As Boolean)
        Dim sb As New StringBuilder()
        sb.Append($"Total entries: {_allEntries.Count}   Showing: {visibleCount}")
        If Not String.IsNullOrWhiteSpace(search) Then sb.Append($"   Search: ""{search}""")
        sb.Append($"   Scope: {(If(allMonths, "All months", "This month"))}")
        sb.Append("   PgUp/PgDn = Month   Ctrl+G = Go   Ctrl+F = Search   Ins = Add   Del = Delete   Ctrl+R = Reload   Ctrl+Shift+R = Repair   Esc = Close")
        _lblStatus.Text = sb.ToString()
    End Sub

    Private Sub LoadViewMonthFromPmsNum()
        Try
            Dim raw = StripDosEofOnly(File.ReadAllText(PmsNumPath, Encoding.ASCII)).Trim()
            If raw.Length = 0 Then Return

            Dim parts = ParseBasicWriteLine(raw)
            If parts.Count < 2 Then Return

            Dim dtText = parts(1)
            Dim dt As DateTime
            If DateTime.TryParseExact(dtText, "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, dt) Then
                _viewMonth = New DateTime(DateTime.Today.Year, dt.Month, 1)
            End If
        Catch
        End Try
    End Sub

    Private Sub SaveViewMonthToPmsNum()
        Try
            Dim lastDate = New DateTime(DateTime.Today.Year, _viewMonth.Month, 1).ToString("MM-dd-yyyy", CultureInfo.InvariantCulture)
            Dim count = _allEntries.Count.ToString(CultureInfo.InvariantCulture)
            Dim line = ToBasicWriteLine(New String() {"0", lastDate, count})

            WithLock(PmsNumLockPath,
                     Sub()
                         WriteAllTextAtomic(PmsNumPath, PmsNumTmpPath, PmsNumBakPath, line & Environment.NewLine, Encoding.ASCII)
                     End Sub,
                     friendlyName:="PMS.NUM")
        Catch
        End Try
    End Sub

    ' =========================
    ' Add / Delete (with lock)
    ' =========================
    Private Sub AddEvent()
        Using dlg As New AddEventDialog(_viewMonth)
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

            Dim md = dlg.SelectedMonthDay
            Dim text = dlg.EventText.Trim()
            Dim stored = New DateTime(1993, md.Month, md.Day)

            WithLock(DatesPmsLockPath,
                     Sub()
                         LoadAllData()
                         _allEntries.Add(New PmsEntry With {.StoredDate = stored, .Text = text})
                         SaveDatesPms_NoLock()
                     End Sub,
                     friendlyName:="DATES.PMS")

            LoadAllData()
            BindGrid()
            SaveViewMonthToPmsNum()
        End Using
    End Sub

    Private Sub DeleteSelected()
        If _grid.SelectedRows.Count = 0 Then Return

        If MessageBox.Show($"Delete {_grid.SelectedRows.Count} selected event(s)?",
                           "Personal Calendar",
                           MessageBoxButtons.YesNo,
                           MessageBoxIcon.Warning) <> DialogResult.Yes Then
            Return
        End If

        Dim selected As New List(Of GridRow)()
        For Each r As DataGridViewRow In _grid.SelectedRows
            Dim gr = TryCast(r.Tag, GridRow)
            If gr IsNot Nothing Then selected.Add(gr)
        Next
        If selected.Count = 0 Then Return

        WithLock(DatesPmsLockPath,
                 Sub()
                     LoadAllData()

                     For Each s In selected
                         Dim idx = _allEntries.FindIndex(Function(e) e.StoredDate = s.StoredDate AndAlso String.Equals(e.Text, s.Text, StringComparison.Ordinal))
                         If idx >= 0 Then _allEntries.RemoveAt(idx)
                     Next

                     SaveDatesPms_NoLock()
                 End Sub,
                 friendlyName:="DATES.PMS")

        LoadAllData()
        BindGrid()
        SaveViewMonthToPmsNum()
    End Sub

    Private Sub SaveDatesPms_NoLock()
        Dim sb As New StringBuilder()
        sb.AppendLine(ToBasicWriteLine(New String() {New String("1"c, 60), "1", "31"}))

        For Each e In _allEntries.
            OrderBy(Function(x) x.StoredDate.Month).
            ThenBy(Function(x) x.StoredDate.Day).
            ThenBy(Function(x) x.StoredDate.Year).
            ThenBy(Function(x) x.Text)

            sb.AppendLine(ToBasicWriteLine(New String() {e.StoredDate.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture), e.Text}))
        Next

        WriteAllTextAtomic(DatesPmsPath, DatesPmsTmpPath, DatesPmsBakPath, sb.ToString(), Encoding.ASCII)
    End Sub

    ' =========================
    ' Repair / Audit tool
    ' =========================
    Private Sub RepairDatesPms()
        If MessageBox.Show("This will re-write DATES.PMS with normalized quoting and MM-dd-yyyy dates, removing blank/bad lines." &
                           Environment.NewLine & Environment.NewLine &
                           "Continue?",
                           "Repair DATES.PMS",
                           MessageBoxButtons.YesNo,
                           MessageBoxIcon.Warning) <> DialogResult.Yes Then
            Return
        End If

        Try
            WithLock(DatesPmsLockPath,
                     Sub()
                         LoadAllData()
                         SaveDatesPms_NoLock()
                     End Sub,
                     friendlyName:="DATES.PMS")

            LoadAllData()
            BindGrid()

            MessageBox.Show("DATES.PMS repair complete.", "Repair DATES.PMS", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Repair failed: " & ex.Message, "Repair DATES.PMS", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' =========================
    ' Locking helper
    ' =========================
    Private Shared Sub WithLock(lockPath As String, action As Action, friendlyName As String)
        Dim fs As FileStream = Nothing
        Try
            fs = New FileStream(lockPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)
            Dim msg = $"Locked by {Environment.MachineName}\{Environment.UserName} at {DateTime.Now.ToString("s", CultureInfo.InvariantCulture)}"
            Dim bytes = Encoding.ASCII.GetBytes(msg)
            fs.Write(bytes, 0, bytes.Length)
            fs.Flush()

            action()
        Catch ex As IOException
            MessageBox.Show($"{friendlyName} is currently in use by another user." & Environment.NewLine &
                            "Please try again in a moment.",
                            "Personal Calendar",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information)
        Finally
            Try
                If fs IsNot Nothing Then fs.Dispose()
            Catch
            End Try
            Try
                If File.Exists(lockPath) Then File.Delete(lockPath)
            Catch
            End Try
        End Try
    End Sub

    ' =========================
    ' Atomic write helper (tmp + replace)
    ' =========================
    Private Shared Sub WriteAllTextAtomic(targetPath As String, tmpPath As String, bakPath As String, content As String, enc As Encoding)
        File.WriteAllText(tmpPath, content, enc)

        Try
            If File.Exists(targetPath) AndAlso Not File.Exists(bakPath) Then
                File.Copy(targetPath, bakPath, overwrite:=False)
            End If
        Catch
        End Try

        Try
            If File.Exists(targetPath) Then
                File.Replace(tmpPath, targetPath, destinationBackupFileName:=Nothing, ignoreMetadataErrors:=True)
            Else
                File.Move(tmpPath, targetPath)
            End If
        Catch
            File.Copy(tmpPath, targetPath, overwrite:=True)
            Try
                File.Delete(tmpPath)
            Catch
            End Try
        End Try
    End Sub

    ' =========================
    ' BASIC WRITE# helpers
    ' =========================
    Private Shared Function StripDosEofOnly(s As String) As String
        If s Is Nothing Then Return ""
        Return s.Replace(ChrW(26), "")
    End Function

    Private Shared Function ToBasicWriteLine(values As IEnumerable(Of String)) As String
        Dim parts As New List(Of String)()
        For Each v In values
            If v Is Nothing Then
                parts.Add("""""")
            Else
                Dim n As Integer
                If Integer.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, n) AndAlso
                   v.All(Function(ch) Char.IsDigit(ch) OrElse ch = "-"c) Then
                    parts.Add(n.ToString(CultureInfo.InvariantCulture))
                Else
                    Dim escaped = v.Replace("""", """""")
                    parts.Add("""" & escaped & """")
                End If
            End If
        Next
        Return String.Join(",", parts)
    End Function

    Private Shared Function ParseBasicWriteLine(line As String) As List(Of String)
        Dim result As New List(Of String)()
        Dim sb As New StringBuilder()
        Dim inQuotes As Boolean = False
        Dim i As Integer = 0

        While i < line.Length
            Dim ch = line(i)

            If inQuotes Then
                If ch = """"c Then
                    If i + 1 < line.Length AndAlso line(i + 1) = """"c Then
                        sb.Append(""""c)
                        i += 2
                    Else
                        inQuotes = False
                        i += 1
                    End If
                Else
                    sb.Append(ch)
                    i += 1
                End If
            Else
                If ch = ","c Then
                    result.Add(sb.ToString().Trim())
                    sb.Clear()
                    i += 1
                ElseIf ch = """"c Then
                    inQuotes = True
                    i += 1
                Else
                    sb.Append(ch)
                    i += 1
                End If
            End If
        End While

        result.Add(sb.ToString().Trim())
        Return result
    End Function

    ' =========================
    ' Add dialog (Feb 29 warning)
    ' =========================
    Private NotInheritable Class AddEventDialog
        Inherits Form

        Private ReadOnly _dtp As DateTimePicker
        Private ReadOnly _txt As TextBox

        Public ReadOnly Property SelectedMonthDay As DateTime
            Get
                Return _dtp.Value.Date
            End Get
        End Property

        Public ReadOnly Property EventText As String
            Get
                Return _txt.Text
            End Get
        End Property

        Public Sub New(viewMonth As DateTime)
            Me.Text = "Add an Event"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MinimizeBox = False
            Me.MaximizeBox = False
            Me.ShowInTaskbar = False
            Me.ClientSize = New Size(600, 210)

            Dim layout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(10), .ColumnCount = 2, .RowCount = 4}
            layout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            layout.Controls.Add(New Label() With {.AutoSize = True, .Text = "Date (month/day):"}, 0, 0)

            _dtp = New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = "MM-dd", .ShowUpDown = True, .Value = New DateTime(2000, viewMonth.Month, 1)}
            layout.Controls.Add(_dtp, 1, 0)

            Dim lblWarn As New Label() With {.AutoSize = True, .ForeColor = Color.DarkRed, .Text = ""}
            layout.Controls.Add(New Label() With {.AutoSize = True, .Text = ""}, 0, 1)
            layout.Controls.Add(lblWarn, 1, 1)

            layout.Controls.Add(New Label() With {.AutoSize = True, .Text = "Event:"}, 0, 2)
            _txt = New TextBox() With {.Dock = DockStyle.Fill}
            layout.Controls.Add(_txt, 1, 2)

            AddHandler _dtp.ValueChanged,
                Sub()
                    If _dtp.Value.Month = 2 AndAlso _dtp.Value.Day = 29 Then
                        lblWarn.Text = "Note: Feb 29 only appears in leap years."
                    Else
                        lblWarn.Text = ""
                    End If
                End Sub

            Dim buttons As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft, .AutoSize = True, .WrapContents = False}
            Dim ok As New Button() With {.Text = "OK", .DialogResult = DialogResult.OK, .AutoSize = True}
            Dim cancel As New Button() With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .AutoSize = True}
            buttons.Controls.Add(ok)
            buttons.Controls.Add(cancel)
            layout.Controls.Add(buttons, 1, 3)

            Me.Controls.Add(layout)
            Me.AcceptButton = ok
            Me.CancelButton = cancel

            AddHandler Me.FormClosing,
                Sub(sender, e)
                    If Me.DialogResult = DialogResult.OK AndAlso String.IsNullOrWhiteSpace(_txt.Text) Then
                        MessageBox.Show("Please enter an event description.", "Add an Event", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        e.Cancel = True
                    End If
                End Sub
        End Sub
    End Class

End Class