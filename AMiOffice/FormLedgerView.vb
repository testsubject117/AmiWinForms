Option Strict On
Option Explicit On

Imports System.Linq

Public Class FormLedgerView

    Private Const ScreenTitle As String = "Ledger - View Checks"

    Private ReadOnly _all As New List(Of LedgerEntry)()

    ' UI controls (created in code so you don’t have to design it yet)
    Private ReadOnly txtCustomer As New TextBox()
    Private ReadOnly txtCheck As New TextBox()
    Private ReadOnly cmbYear As New ComboBox()
    Private ReadOnly cmbMonth As New ComboBox()
    Private ReadOnly btnRefresh As New Button()
    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly lblTotal As New Label()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = ScreenTitle
        Width = 1100
        Height = 750
        StartPosition = FormStartPosition.CenterParent

        BuildUi()
        PopulatePicklists()

        LoadData()
        ApplyFilters()
    End Sub

    Private Sub BuildUi()
        Dim topPanel As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .Height = 70,
            .ColumnCount = 9,
            .RowCount = 2
        }

        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize)) ' Customer lbl
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35)) ' Customer txt
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize)) ' Check lbl
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20)) ' Check txt
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize)) ' Year lbl
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 15)) ' Year cmb
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize)) ' Month lbl
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 15)) ' Month cmb
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize)) ' Refresh btn

        Dim lblCustomer As New Label() With {.Text = "Customer:", .AutoSize = True, .Anchor = AnchorStyles.Left}
        Dim lblCheck As New Label() With {.Text = "Check #:", .AutoSize = True, .Anchor = AnchorStyles.Left}
        Dim lblYear As New Label() With {.Text = "Year:", .AutoSize = True, .Anchor = AnchorStyles.Left}
        Dim lblMonth As New Label() With {.Text = "Month:", .AutoSize = True, .Anchor = AnchorStyles.Left}

        txtCustomer.Dock = DockStyle.Fill
        txtCheck.Dock = DockStyle.Fill

        cmbYear.DropDownStyle = ComboBoxStyle.DropDownList
        cmbMonth.DropDownStyle = ComboBoxStyle.DropDownList
        cmbYear.Dock = DockStyle.Fill
        cmbMonth.Dock = DockStyle.Fill

        btnRefresh.Text = "Refresh"
        btnRefresh.AutoSize = True
        btnRefresh.Anchor = AnchorStyles.Left

        lblTotal.Text = "Total: $0.00"
        lblTotal.AutoSize = True
        lblTotal.Dock = DockStyle.Fill
        lblTotal.TextAlign = ContentAlignment.MiddleLeft

        dgv.Dock = DockStyle.Fill
        dgv.ReadOnly = True
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.AutoGenerateColumns = True
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect

        ' Row 0: filters
        topPanel.Controls.Add(lblCustomer, 0, 0)
        topPanel.Controls.Add(txtCustomer, 1, 0)
        topPanel.Controls.Add(lblCheck, 2, 0)
        topPanel.Controls.Add(txtCheck, 3, 0)
        topPanel.Controls.Add(lblYear, 4, 0)
        topPanel.Controls.Add(cmbYear, 5, 0)
        topPanel.Controls.Add(lblMonth, 6, 0)
        topPanel.Controls.Add(cmbMonth, 7, 0)
        topPanel.Controls.Add(btnRefresh, 8, 0)

        ' Row 1: total
        topPanel.SetColumnSpan(lblTotal, 9)
        topPanel.Controls.Add(lblTotal, 0, 1)

        Controls.Add(dgv)
        Controls.Add(topPanel)

        AddHandler btnRefresh.Click,
            Sub()
                LoadData()
                ApplyFilters()
            End Sub

        AddHandler txtCustomer.TextChanged, Sub() ApplyFilters()
        AddHandler txtCheck.TextChanged, Sub() ApplyFilters()
        AddHandler cmbYear.SelectedIndexChanged, Sub() ApplyFilters()
        AddHandler cmbMonth.SelectedIndexChanged, Sub() ApplyFilters()
    End Sub

    Private Sub PopulatePicklists()
        cmbMonth.Items.Clear()
        cmbMonth.Items.Add("") ' all
        For m As Integer = 1 To 12
            cmbMonth.Items.Add(m.ToString("00"))
        Next
        cmbMonth.SelectedIndex = 0

        cmbYear.Items.Clear()
        cmbYear.Items.Add("") ' all
        ' We don’t know min/max upfront; just provide a reasonable range + all.
        For y As Integer = Date.Today.Year To 1986 Step -1
            cmbYear.Items.Add(y.ToString())
        Next
        cmbYear.SelectedIndex = 0
    End Sub

    Private Sub LoadData()
        Dim path = LegacyDataPaths.LedgerCur

        ' Clear current view so we never display stale data if the file is missing/unreadable.
        _all.Clear()
        dgv.DataSource = Nothing
        lblTotal.Text = "Total: $0.00"

        If Not System.IO.File.Exists(path) Then
            UiFileErrors.ShowMissingRequiredFile(Me, ScreenTitle, path)
            Return
        End If

        Try
            _all.AddRange(LedgerCurReader.ReadAll(path))
        Catch ex As Exception
            UiFileErrors.ShowUnableToReadRequiredFile(Me, ScreenTitle, path, ex)
        End Try
    End Sub

    Private Sub ApplyFilters()
        Dim cust As String = txtCustomer.Text.Trim()
        Dim chk As String = txtCheck.Text.Trim()
        Dim year As String = If(TryCast(cmbYear.SelectedItem, String), "").Trim()
        Dim month As String = If(TryCast(cmbMonth.SelectedItem, String), "").Trim()

        Dim q = _all.AsEnumerable()

        If cust <> "" Then
            q = q.Where(Function(e) If(e.Customer, "").IndexOf(cust, StringComparison.OrdinalIgnoreCase) >= 0)
        End If

        If chk <> "" Then
            q = q.Where(Function(e) If(e.CheckNumber, "").IndexOf(chk, StringComparison.OrdinalIgnoreCase) >= 0)
        End If

        If year <> "" Then
            Dim yy As String = year.Substring(year.Length - 2) ' DOS compared last 2 digits
            q = q.Where(Function(e) If(e.DateText, "").EndsWith(yy, StringComparison.OrdinalIgnoreCase))
        End If

        If month <> "" Then
            q = q.Where(Function(e) If(e.DateText, "").StartsWith(month, StringComparison.OrdinalIgnoreCase))
        End If

        Dim list = q.ToList()
        dgv.DataSource = list

        Dim total As Decimal = list.Sum(Function(e) e.Amount)
        lblTotal.Text = $"Total: {total:C}"
    End Sub

End Class