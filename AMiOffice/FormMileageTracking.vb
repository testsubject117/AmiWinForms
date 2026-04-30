Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Public Class FormMileageTracking

    ' All non-backup/non-word data goes here (per your requirement)
    Private Const DataDir As String = "\\invoice\MainMenu\Data"
    Private Const MileageReportsDir As String = "\\invoice\MainMenu\Data\mileage"

    Private ReadOnly MileageDatPath As String = Path.Combine(DataDir, "MILEAGE.DAT")
    Private ReadOnly OdometerNumPath As String = Path.Combine(DataDir, "ODOMETER.NUM")
    Private ReadOnly MileageTmpPath As String = Path.Combine(DataDir, "MILEAGE.TMP")

    ' Always overwrite this one (in the mileage reports folder)
    Private ReadOnly MileageRptPath As String = Path.Combine(MileageReportsDir, "MILEAGE.RPT")

    Private _table As DataTable = Nothing
    Private _grid As DataGridView = Nothing

    ' Inputs
    Private _dtpDate As DateTimePicker = Nothing
    Private _txtDestination As TextBox = Nothing
    Private _txtPurpose As TextBox = Nothing
    Private _txtStart As TextBox = Nothing
    Private _txtEnd As TextBox = Nothing
    Private _txtMiles As TextBox = Nothing
    Private _cmbPayment As ComboBox = Nothing
    Private _lblMilesHint As Label = Nothing

    ' Buttons (for hotkeys/PerformClick)
    Private _btnAdd As Button = Nothing
    Private _btnDelete As Button = Nothing
    Private _btnSave As Button = Nothing
    Private _btnReload As Button = Nothing
    Private _btnViewRange As Button = Nothing
    Private _btnPrint As Button = Nothing
    Private _btnClose As Button = Nothing

    Private _lastOdometerDefault As Integer = 0
    Private _suppressAutoMiles As Boolean = False

    Public Sub New()
        InitializeComponent()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Me.Text = "Mileage Tracking"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.KeyPreview = True
        Me.MinimumSize = New Size(1120, 720)

        If Not EnsureDataDirExistsFriendly() Then
            Close()
            Return
        End If

        EnsureMileageDatExists()

        BuildUi()
        BuildTable()

        _lastOdometerDefault = LoadLastOdometer()
        ApplyDefaultStartOdometer()

        LoadFromLegacyDat()
        RecalcMilesHint()
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)

        If e.Control AndAlso e.KeyCode = Keys.S Then
            SaveAllToLegacyDat()
            e.Handled = True
            Return
        End If

        If e.KeyCode = Keys.Escape Then
            Close()
            e.Handled = True
            Return
        End If
    End Sub

    Private Function EnsureDataDirExistsFriendly() As Boolean
        Try
            If Directory.Exists(DataDir) Then Return True

            MessageBox.Show("Mileage Tracking cannot open because the data folder is not available:" & Environment.NewLine &
                            DataDir & Environment.NewLine & Environment.NewLine &
                            "Please run on the VM or connect to the share.",
                            "Mileage Tracking",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
            Return False
        Catch ex As Exception
            MessageBox.Show("Mileage Tracking cannot access the data folder:" & Environment.NewLine &
                            DataDir & Environment.NewLine & Environment.NewLine &
                            ex.Message,
                            "Mileage Tracking",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    ' You said ODOMETER.NUM already exists; only create MILEAGE.DAT if missing.
    Private Sub EnsureMileageDatExists()
        Try
            If Not File.Exists(MileageDatPath) Then
                File.WriteAllText(MileageDatPath, "", Encoding.ASCII)
            End If
        Catch ex As Exception
            MessageBox.Show("Unable to create/open MILEAGE.DAT in:" & Environment.NewLine &
                            DataDir & Environment.NewLine & Environment.NewLine &
                            ex.Message,
                            "Mileage Tracking",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
            Close()
        End Try
    End Sub

    Private Sub BuildUi()
        Dim root As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(12),
            .ColumnCount = 1,
            .RowCount = 4
        }
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        Dim lblHeader As New Label() With {
            .AutoSize = True,
            .Font = New Font("Segoe UI", 16.0F, FontStyle.Bold),
            .Text = "Mileage Tracking"
        }

        Dim row1 As New TableLayoutPanel() With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 10}
        row1.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        row1.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 140))
        row1.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        row1.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 240))
        row1.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        row1.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        row1.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        row1.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 95))
        row1.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        row1.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 95))

        _dtpDate = New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .Value = DateTime.Today, .Dock = DockStyle.Fill}
        _txtDestination = New TextBox() With {.Dock = DockStyle.Fill}
        _txtPurpose = New TextBox() With {.Dock = DockStyle.Fill}
        _txtStart = New TextBox() With {.Dock = DockStyle.Fill}
        _txtEnd = New TextBox() With {.Dock = DockStyle.Fill}

        AddHandler _txtStart.TextChanged, Sub() OnOdometerChanged()
        AddHandler _txtEnd.TextChanged, Sub() OnOdometerChanged()

        AddHandler _txtPurpose.KeyDown,
            Sub(sender, ev)
                If ev.KeyCode = Keys.Enter Then
                    AddEntry()
                    ev.SuppressKeyPress = True
                End If
            End Sub

        row1.Controls.Add(New Label() With {.AutoSize = True, .Text = "Date:", .Padding = New Padding(0, 8, 6, 0)}, 0, 0)
        row1.Controls.Add(_dtpDate, 1, 0)
        row1.Controls.Add(New Label() With {.AutoSize = True, .Text = "Destination:", .Padding = New Padding(12, 8, 6, 0)}, 2, 0)
        row1.Controls.Add(_txtDestination, 3, 0)
        row1.Controls.Add(New Label() With {.AutoSize = True, .Text = "Business Purpose:", .Padding = New Padding(12, 8, 6, 0)}, 4, 0)
        row1.Controls.Add(_txtPurpose, 5, 0)
        row1.Controls.Add(New Label() With {.AutoSize = True, .Text = "Start:", .Padding = New Padding(12, 8, 6, 0)}, 6, 0)
        row1.Controls.Add(_txtStart, 7, 0)
        row1.Controls.Add(New Label() With {.AutoSize = True, .Text = "End:", .Padding = New Padding(12, 8, 6, 0)}, 8, 0)
        row1.Controls.Add(_txtEnd, 9, 0)

        Dim row2 As New TableLayoutPanel() With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 10}
        row2.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        row2.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 95))
        row2.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        row2.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        row2.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 140))
        row2.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        row2.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))

        _txtMiles = New TextBox() With {.Dock = DockStyle.Fill}
        _lblMilesHint = New Label() With {.AutoSize = True, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold), .Text = "Miles: —", .Padding = New Padding(8, 7, 8, 7)}

        _cmbPayment = New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Dock = DockStyle.Fill}
        _cmbPayment.Items.Add("Cash")
        _cmbPayment.Items.Add("Charge")
        _cmbPayment.SelectedIndex = 0

        _btnAdd = New Button() With {.Text = "Add", .AutoSize = True}
        AddHandler _btnAdd.Click, Sub() AddEntry()

        _btnDelete = New Button() With {.Text = "Delete Selected", .AutoSize = True}
        AddHandler _btnDelete.Click, Sub() DeleteSelected()

        _btnSave = New Button() With {.Text = "Save (Ctrl+S)", .AutoSize = True}
        AddHandler _btnSave.Click, Sub() SaveAllToLegacyDat()

        _btnReload = New Button() With {.Text = "Reload", .AutoSize = True}
        AddHandler _btnReload.Click, Sub() ReloadFromDisk()

        _btnViewRange = New Button() With {.Text = "View Range…", .AutoSize = True}
        AddHandler _btnViewRange.Click, Sub() ViewRangeBASICStyle()

        _btnPrint = New Button() With {.Text = "Print Mileage…", .AutoSize = True}
        AddHandler _btnPrint.Click, Sub() PrintMileageReport()

        _btnClose = New Button() With {.Text = "(Esc) Close", .AutoSize = True}
        AddHandler _btnClose.Click, Sub() Close()

        Dim btnPanel As New FlowLayoutPanel() With {.AutoSize = True, .FlowDirection = FlowDirection.LeftToRight, .WrapContents = False}
        btnPanel.Controls.Add(_btnAdd)
        btnPanel.Controls.Add(_btnDelete)
        btnPanel.Controls.Add(_btnSave)
        btnPanel.Controls.Add(_btnReload)
        btnPanel.Controls.Add(_btnViewRange)
        btnPanel.Controls.Add(_btnPrint)
        btnPanel.Controls.Add(_btnClose)

        row2.Controls.Add(New Label() With {.AutoSize = True, .Text = "Business Miles:", .Padding = New Padding(0, 8, 6, 0)}, 0, 0)
        row2.Controls.Add(_txtMiles, 1, 0)
        row2.Controls.Add(_lblMilesHint, 2, 0)
        row2.Controls.Add(New Label() With {.AutoSize = True, .Text = "Payment:", .Padding = New Padding(12, 8, 6, 0)}, 3, 0)
        row2.Controls.Add(_cmbPayment, 4, 0)
        row2.Controls.Add(New Panel() With {.Dock = DockStyle.Fill}, 5, 0)
        row2.Controls.Add(btnPanel, 6, 0)

        _grid = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = False,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = True,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }

        root.Controls.Add(lblHeader, 0, 0)
        root.Controls.Add(row1, 0, 1)
        root.Controls.Add(row2, 0, 2)
        root.Controls.Add(_grid, 0, 3)

        Controls.Clear()
        Controls.Add(root)
    End Sub

    Private Sub BuildTable()
        _table = New DataTable("Mileage")
        _table.Columns.Add("Date", GetType(DateTime))
        _table.Columns.Add("Destination", GetType(String))
        _table.Columns.Add("BusinessPurpose", GetType(String))
        _table.Columns.Add("OdometerStart", GetType(Integer))
        _table.Columns.Add("OdometerEnd", GetType(Integer))
        _table.Columns.Add("BusinessMiles", GetType(Integer))
        _table.Columns.Add("PaymentType", GetType(String))

        _grid.DataSource = _table
        _grid.Columns("Date").DefaultCellStyle.Format = "d"
    End Sub

    Private Sub OnOdometerChanged()
        RecalcMilesHint()

        Dim os As Integer
        Dim oe As Integer
        If Integer.TryParse(_txtStart.Text.Trim(), os) AndAlso Integer.TryParse(_txtEnd.Text.Trim(), oe) Then
            Dim miles = oe - os
            If miles > 0 AndAlso String.IsNullOrWhiteSpace(_txtMiles.Text) Then
                _suppressAutoMiles = True
                _txtMiles.Text = miles.ToString(CultureInfo.InvariantCulture)
                _suppressAutoMiles = False
            End If
        End If
    End Sub

    Private Sub RecalcMilesHint()
        Dim os As Integer
        Dim oe As Integer
        If Integer.TryParse(_txtStart.Text.Trim(), os) AndAlso Integer.TryParse(_txtEnd.Text.Trim(), oe) Then
            Dim miles = oe - os
            If miles >= 0 Then
                _lblMilesHint.Text = "Miles: " & miles.ToString(CultureInfo.InvariantCulture)
            Else
                _lblMilesHint.Text = "Miles: (end < start)"
            End If
        Else
            _lblMilesHint.Text = "Miles: —"
        End If
    End Sub

    Private Sub ApplyDefaultStartOdometer()
        If _lastOdometerDefault > 0 Then
            _txtStart.Text = _lastOdometerDefault.ToString(CultureInfo.InvariantCulture)
        Else
            _txtStart.Clear()
        End If
    End Sub

    Private Shared Function StripDosEofAndControls(s As String) As String
        If s Is Nothing Then Return ""
        Dim sb As New StringBuilder(s.Length)
        For Each ch As Char In s
            If Char.IsControl(ch) Then Continue For
            sb.Append(ch)
        Next
        Return sb.ToString()
    End Function

    Private Function LoadLastOdometer() As Integer
        Try
            If Not File.Exists(OdometerNumPath) Then Return 0
            Dim raw = File.ReadAllText(OdometerNumPath, Encoding.ASCII)
            Dim cleaned = StripDosEofAndControls(raw).Trim()
            cleaned = cleaned.Trim().Trim(""""c).Trim().TrimEnd(","c).Trim()

            Dim v As Integer
            If Integer.TryParse(cleaned, NumberStyles.Integer, CultureInfo.InvariantCulture, v) Then
                Return v
            End If
            Return 0
        Catch
            Return 0
        End Try
    End Function

    Private Sub SaveLastOdometer(odometerEnding As Integer)
        Try
            File.WriteAllText(OdometerNumPath, odometerEnding.ToString(CultureInfo.InvariantCulture), Encoding.ASCII)
        Catch ex As Exception
            MessageBox.Show("Could not update ODOMETER.NUM: " & ex.Message, "Mileage", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub AddEntry()
        Dim dest = _txtDestination.Text.Trim()
        Dim purpose = _txtPurpose.Text.Trim()
        Dim payment = If(_cmbPayment.SelectedItem Is Nothing, "Cash", _cmbPayment.SelectedItem.ToString())

        Dim os As Integer
        Dim oe As Integer
        Dim bm As Integer

        If Not Integer.TryParse(_txtStart.Text.Trim(), os) Then
            MessageBox.Show("Odometer Start must be a whole number.", "Mileage", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _txtStart.Focus()
            Return
        End If
        If os < 999 Then
            MessageBox.Show("Odometer Start must be at least 999 (matches original BAS validation).", "Mileage", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _txtStart.Focus()
            Return
        End If
        If Not Integer.TryParse(_txtEnd.Text.Trim(), oe) Then
            MessageBox.Show("Odometer End must be a whole number.", "Mileage", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _txtEnd.Focus()
            Return
        End If
        If oe <= os Then
            MessageBox.Show("Odometer End must be greater than Start.", "Mileage", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _txtEnd.Focus()
            Return
        End If
        If Not Integer.TryParse(_txtMiles.Text.Trim(), bm) Then
            MessageBox.Show("Business Miles must be a whole number.", "Mileage", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _txtMiles.Focus()
            Return
        End If
        If bm < 1 Then
            MessageBox.Show("Business Miles must be at least 1.", "Mileage", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _txtMiles.Focus()
            Return
        End If

        Dim row = _table.NewRow()
        row("Date") = _dtpDate.Value.Date
        row("Destination") = dest
        row("BusinessPurpose") = purpose
        row("OdometerStart") = os
        row("OdometerEnd") = oe
        row("BusinessMiles") = bm
        row("PaymentType") = payment
        _table.Rows.Add(row)

        SaveLastOdometer(oe)
        _lastOdometerDefault = oe

        _txtDestination.Clear()
        _txtPurpose.Clear()
        _txtEnd.Clear()
        _txtMiles.Clear()

        ApplyDefaultStartOdometer()
        _txtEnd.Focus()
        RecalcMilesHint()
    End Sub

    Private Sub DeleteSelected()
        If _grid.SelectedRows.Count = 0 Then Return

        If MessageBox.Show($"Delete {_grid.SelectedRows.Count} selected row(s)?",
                           "Mileage", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
            Return
        End If

        Dim idxs = _grid.SelectedRows.Cast(Of DataGridViewRow)().
            Select(Function(r) r.Index).
            Where(Function(i) i >= 0).
            OrderByDescending(Function(i) i).
            ToList()

        For Each i In idxs
            If i < _table.Rows.Count Then _table.Rows.RemoveAt(i)
        Next
    End Sub

    Private Sub ReloadFromDisk()
        If MessageBox.Show("Reload from disk and lose unsaved changes?",
                           "Mileage", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
            Return
        End If

        LoadFromLegacyDat()
    End Sub

    Private Sub LoadFromLegacyDat()
        _table.Rows.Clear()
        If Not File.Exists(MileageDatPath) Then Return

        Dim lines = File.ReadAllLines(MileageDatPath, Encoding.ASCII).
            Select(Function(l) StripDosEofAndControls(l)).
            Where(Function(l) Not String.IsNullOrWhiteSpace(l)).
            ToList()

        Dim i As Integer = 0
        While i + 1 < lines.Count
            Dim parts1 = ParseBasicWriteLine(lines(i).Trim())
            Dim parts2 = ParseBasicWriteLine(lines(i + 1).Trim())

            If parts1.Count >= 3 AndAlso parts2.Count >= 4 Then
                Dim dtText = parts1(0)
                Dim dest = parts1(1)
                Dim purpose = parts1(2)

                Dim os As Integer
                Dim oe As Integer
                Dim bm As Integer
                Dim payment = parts2(3)

                If Integer.TryParse(parts2(0), NumberStyles.Integer, CultureInfo.InvariantCulture, os) AndAlso
                   Integer.TryParse(parts2(1), NumberStyles.Integer, CultureInfo.InvariantCulture, oe) AndAlso
                   Integer.TryParse(parts2(2), NumberStyles.Integer, CultureInfo.InvariantCulture, bm) Then

                    Dim dt As DateTime
                    If DateTime.TryParseExact(dtText, "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, dt) Then
                        Dim row = _table.NewRow()
                        row("Date") = dt.Date
                        row("Destination") = dest
                        row("BusinessPurpose") = purpose
                        row("OdometerStart") = os
                        row("OdometerEnd") = oe
                        row("BusinessMiles") = bm
                        row("PaymentType") = payment
                        _table.Rows.Add(row)
                    End If
                End If
            End If

            i += 2
        End While
    End Sub

    Private Sub SaveAllToLegacyDat()
        Try
            Using sw As New StreamWriter(MileageTmpPath, append:=False, encoding:=Encoding.ASCII)
                For Each r As DataRow In _table.Rows
                    Dim dt = DirectCast(r("Date"), DateTime).ToString("MM-dd-yyyy", CultureInfo.InvariantCulture)

                    Dim dest = SafeString(CStr(r("Destination")))
                    Dim purpose = SafeString(CStr(r("BusinessPurpose")))

                    Dim os = CInt(r("OdometerStart"))
                    Dim oe = CInt(r("OdometerEnd"))
                    Dim bm = CInt(r("BusinessMiles"))
                    Dim payment = SafeString(CStr(r("PaymentType")))

                    sw.WriteLine(ToBasicWriteLine(New String() {dt, dest, purpose}))
                    sw.WriteLine(ToBasicWriteLine(New String() {
                        os.ToString(CultureInfo.InvariantCulture),
                        oe.ToString(CultureInfo.InvariantCulture),
                        bm.ToString(CultureInfo.InvariantCulture),
                        payment
                    }))
                Next
            End Using

            If File.Exists(MileageDatPath) Then File.Delete(MileageDatPath)
            File.Move(MileageTmpPath, MileageDatPath)

            MessageBox.Show("Saved:" & Environment.NewLine & MileageDatPath,
                            "Mileage", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Save failed: " & ex.Message, "Mileage", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Shared Function SafeString(s As String) As String
        If s Is Nothing Then Return ""
        Return s.Trim()
    End Function

    ' === BAS-style View Range ===
    Private Sub ViewRangeBASICStyle()
        If _table.Rows.Count = 0 Then
            MessageBox.Show("No mileage entries loaded.", "View Mileage", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim beg As DateTime
        Dim endd As DateTime

        If Not PromptForDate("Beginning Date (MM-dd-yyyy)", "01-01-1987", beg) Then Return
        If Not PromptForDate("Ending Date (MM-dd-yyyy)", DateTime.Today.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture), endd) Then Return

        If beg.Date > endd.Date Then
            MessageBox.Show("Beginning date is after ending date.", "View Mileage", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim rows = _table.Rows.Cast(Of DataRow)().
            Where(Function(r)
                      Dim d = DirectCast(r("Date"), DateTime).Date
                      Return d >= beg.Date AndAlso d <= endd.Date
                  End Function).
            OrderBy(Function(r) DirectCast(r("Date"), DateTime)).
            ToList()

        If rows.Count = 0 Then
            MessageBox.Show("No mileage entries in that date range.", "View Mileage", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ShowPagedViewer(rows, beg, endd)
    End Sub

    Private Function PromptForDate(prompt As String, defaultValue As String, ByRef value As DateTime) As Boolean
        Dim input = Microsoft.VisualBasic.Interaction.InputBox(prompt, "Mileage", defaultValue)
        If input Is Nothing Then Return False
        input = input.Trim()
        If input.Length = 0 Then input = defaultValue

        Dim dt As DateTime
        If Not DateTime.TryParseExact(input, "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, dt) Then
            MessageBox.Show("Invalid date. Use MM-dd-yyyy (example: 04-24-2026).", "Mileage", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If

        value = dt.Date
        Return True
    End Function

    Private Sub ShowPagedViewer(rows As List(Of DataRow), beg As DateTime, endd As DateTime)
        Dim f As New Form() With {
            .Text = $"View Mileage ({beg:MM-dd-yyyy} to {endd:MM-dd-yyyy})",
            .StartPosition = FormStartPosition.CenterParent,
            .MinimizeBox = False,
            .MaximizeBox = False,
            .ShowInTaskbar = False,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .ClientSize = New Size(820, 360),
            .KeyPreview = True
        }

        Dim lbl As New Label() With {
            .Dock = DockStyle.Fill,
            .Font = New Font("Consolas", 11.0F, FontStyle.Regular),
            .AutoSize = False,
            .Padding = New Padding(12),
            .Text = ""
        }

        Dim btnNext As New Button() With {.Text = "Next (N)", .AutoSize = True}
        Dim btnQuit As New Button() With {.Text = "Quit (Q)", .AutoSize = True}

        Dim panel As New FlowLayoutPanel() With {.Dock = DockStyle.Bottom, .Height = 48, .Padding = New Padding(12), .FlowDirection = FlowDirection.RightToLeft}
        panel.Controls.Add(btnQuit)
        panel.Controls.Add(btnNext)

        f.Controls.Add(lbl)
        f.Controls.Add(panel)

        Dim idx As Integer = 0

        Dim render As Action =
            Sub()
                Dim r = rows(idx)
                Dim d = DirectCast(r("Date"), DateTime).ToString("MM-dd-yyyy", CultureInfo.InvariantCulture)
                Dim dest = CStr(r("Destination"))
                Dim purpose = CStr(r("BusinessPurpose"))
                Dim os = CInt(r("OdometerStart")).ToString(CultureInfo.InvariantCulture)
                Dim oe = CInt(r("OdometerEnd")).ToString(CultureInfo.InvariantCulture)
                Dim bm = CInt(r("BusinessMiles")).ToString(CultureInfo.InvariantCulture)
                Dim pay = CStr(r("PaymentType"))

                lbl.Text =
                    $"Date:               {d}" & Environment.NewLine &
                    $"Destination:        {dest}" & Environment.NewLine &
                    $"Business Purpose:   {purpose}" & Environment.NewLine &
                    $"Odometer Start:     {os}" & Environment.NewLine &
                    $"Odometer End:       {oe}" & Environment.NewLine &
                    $"Business Miles:     {bm}" & Environment.NewLine &
                    $"Payment Type:       {pay}" & Environment.NewLine & Environment.NewLine &
                    $"Record {idx + 1} of {rows.Count}   (N=Next, Q=Quit)"

                btnNext.Enabled = (idx < rows.Count - 1)
            End Sub

        AddHandler btnNext.Click,
            Sub()
                If idx < rows.Count - 1 Then
                    idx += 1
                    render()
                End If
            End Sub

        AddHandler btnQuit.Click, Sub() f.Close()

        AddHandler f.KeyDown,
            Sub(sender, ev)
                If ev.KeyCode = Keys.Q OrElse ev.KeyCode = Keys.Escape Then
                    f.Close()
                    ev.Handled = True
                ElseIf ev.KeyCode = Keys.N OrElse ev.KeyCode = Keys.Enter OrElse ev.KeyCode = Keys.Space Then
                    If idx < rows.Count - 1 Then
                        idx += 1
                        render()
                    End If
                    ev.Handled = True
                End If
            End Sub

        lbl.Text = "Please wait..."
        lbl.Refresh()
        Application.DoEvents()

        render()
        f.ShowDialog(Me)
    End Sub

    ' === Print Mileage (report export) ===
    Private Sub PrintMileageReport()
        If _table.Rows.Count = 0 Then
            MessageBox.Show("No mileage entries loaded.", "Print Mileage", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim beg As DateTime
        Dim endd As DateTime

        If Not PromptForDate("Beginning Date (MM-dd-yyyy)", "01-01-1987", beg) Then Return
        If Not PromptForDate("Ending Date (MM-dd-yyyy)", DateTime.Today.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture), endd) Then Return

        If beg.Date > endd.Date Then
            MessageBox.Show("Beginning date is after ending date.", "Print Mileage", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim rows = _table.Rows.Cast(Of DataRow)().
            Where(Function(r)
                      Dim d = DirectCast(r("Date"), DateTime).Date
                      Return d >= beg.Date AndAlso d <= endd.Date
                  End Function).
            OrderBy(Function(r) DirectCast(r("Date"), DateTime)).
            ToList()

        If rows.Count = 0 Then
            MessageBox.Show("No mileage entries in that date range.", "Print Mileage", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim reportLines = BuildMileageReportLines(rows, beg, endd)

        Dim archiveName As String = $"MILEAGE_{beg:yyyyMMdd}_{endd:yyyyMMdd}.RPT"
        Dim archivePath As String = Path.Combine(MileageReportsDir, archiveName)

        Try
            If Not Directory.Exists(MileageReportsDir) Then
                Directory.CreateDirectory(MileageReportsDir)
            End If

            ' Always overwrite the "current" report (in the mileage folder)
            File.WriteAllLines(MileageRptPath, reportLines, Encoding.ASCII)

            ' Also write an archived copy (in the mileage folder)
            File.WriteAllLines(archivePath, reportLines, Encoding.ASCII)

            MessageBox.Show("Report created:" & Environment.NewLine &
                            MileageRptPath & Environment.NewLine & Environment.NewLine &
                            "Archived copy:" & Environment.NewLine &
                            archivePath,
                            "Print Mileage",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Failed to create report: " & ex.Message, "Print Mileage", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Fixed-width, 80-column-safe text report (dot-matrix friendly)
    Private Function BuildMileageReportLines(rows As List(Of DataRow), beg As DateTime, endd As DateTime) As List(Of String)
        Dim lines As New List(Of String)()

        lines.Add("MILEAGE REPORT")
        lines.Add($"DATE RANGE: {beg:MM-dd-yyyy} TO {endd:MM-dd-yyyy}")
        lines.Add(New String("-"c, 80))

        lines.Add(PadRight("DATE", 10) & " " &
                  PadRight("DESTINATION", 28) & " " &
                  PadLeft("MILES", 5) & " " &
                  PadLeft("START", 6) & " " &
                  PadLeft("END", 6) & " " &
                  PadRight("PAY", 6) & " " &
                  "PURPOSE")
        lines.Add(New String("-"c, 80))

        Dim totalMiles As Integer = 0

        For Each r In rows
            Dim d = DirectCast(r("Date"), DateTime).ToString("MM-dd-yyyy", CultureInfo.InvariantCulture)
            Dim dest = SafeString(CStr(r("Destination")))
            Dim purpose = SafeString(CStr(r("BusinessPurpose")))
            Dim os = CInt(r("OdometerStart"))
            Dim oe = CInt(r("OdometerEnd"))
            Dim bm = CInt(r("BusinessMiles"))
            Dim pay = SafeString(CStr(r("PaymentType")))

            totalMiles += bm

            Dim line =
                PadRight(d, 10) & " " &
                PadRight(Trunc(dest, 28), 28) & " " &
                PadLeft(bm.ToString(CultureInfo.InvariantCulture), 5) & " " &
                PadLeft(os.ToString(CultureInfo.InvariantCulture), 6) & " " &
                PadLeft(oe.ToString(CultureInfo.InvariantCulture), 6) & " " &
                PadRight(Trunc(pay, 6), 6) & " " &
                Trunc(purpose, 80 - (10 + 1 + 28 + 1 + 5 + 1 + 6 + 1 + 6 + 1 + 6 + 1))

            lines.Add(line)
        Next

        lines.Add(New String("-"c, 80))
        lines.Add("TOTAL BUSINESS MILES: " & totalMiles.ToString(CultureInfo.InvariantCulture))
        lines.Add(New String("-"c, 80))

        Return lines
    End Function

    Private Shared Function PadRight(s As String, width As Integer) As String
        If s Is Nothing Then s = ""
        If s.Length >= width Then Return s.Substring(0, width)
        Return s.PadRight(width)
    End Function

    Private Shared Function PadLeft(s As String, width As Integer) As String
        If s Is Nothing Then s = ""
        If s.Length >= width Then Return s.Substring(0, width)
        Return s.PadLeft(width)
    End Function

    Private Shared Function Trunc(s As String, maxLen As Integer) As String
        If s Is Nothing Then Return ""
        If s.Length <= maxLen Then Return s
        Return s.Substring(0, maxLen)
    End Function

    ' === BASIC WRITE# compatibility for MILEAGE.DAT ===
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

End Class