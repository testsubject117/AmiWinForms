Option Strict On
Option Explicit On

Imports System.Linq
Imports System.Threading.Tasks

Public Class FormLedgerView

    Private Const ScreenTitle As String = "Ledger - View Checks"

    Private ReadOnly _all As New List(Of LedgerEntry)()

    ' CHECK.INV index: composite key -> invoice-number list
    Private _invIndex As New Dictionary(Of String, List(Of Long))(StringComparer.OrdinalIgnoreCase)
    Private _invIndexReady As Boolean = False

    ' Main filter controls
    Private ReadOnly txtCustomer As New TextBox()
    Private ReadOnly txtCheck As New TextBox()
    Private ReadOnly cmbYear As New ComboBox()
    Private ReadOnly cmbMonth As New ComboBox()
    Private ReadOnly btnRefresh As New Button()
    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly lblTotal As New Label()

    ' Invoice details panel
    Private ReadOnly pnlInvoice As New Panel()
    Private ReadOnly lblInvoiceTitle As New Label()
    Private ReadOnly lblInvoiceStatus As New Label()
    Private ReadOnly dgvInvoice As New DataGridView()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = ScreenTitle
        Width = 1100
        Height = 900
        StartPosition = FormStartPosition.CenterParent

        BuildUi()
        PopulatePicklists()

        LoadData()
        ApplyFilters()
        StartLoadCheckInvIndex()
    End Sub

    Private Sub BuildUi()
        ' top filter bar
        Dim topPanel As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .Height = 70,
            .ColumnCount = 9,
            .RowCount = 2
        }

        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))       ' Customer lbl
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35))    ' Customer txt
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))       ' Check lbl
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))    ' Check txt
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))       ' Year lbl
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 15))    ' Year cmb
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))       ' Month lbl
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 15))    ' Month cmb
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))       ' Refresh btn

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

        ' main check grid
        dgv.Dock = DockStyle.Fill
        dgv.ReadOnly = True
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.AutoGenerateColumns = True
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect

        ' invoice details panel (bottom)
        pnlInvoice.Dock = DockStyle.Bottom
        pnlInvoice.Height = 220
        pnlInvoice.BorderStyle = BorderStyle.Fixed3D

        lblInvoiceTitle.Text = "  Invoice Details"
        lblInvoiceTitle.Dock = DockStyle.Top
        lblInvoiceTitle.Height = 24
        lblInvoiceTitle.Font = New Drawing.Font(Font.FontFamily, 9.0F, Drawing.FontStyle.Bold)
        lblInvoiceTitle.TextAlign = ContentAlignment.MiddleLeft
        lblInvoiceTitle.BackColor = Drawing.SystemColors.ControlDark
        lblInvoiceTitle.ForeColor = Drawing.SystemColors.ControlLightLight

        lblInvoiceStatus.Text = "Select a check row above to view invoice details."
        lblInvoiceStatus.Dock = DockStyle.Top
        lblInvoiceStatus.Height = 22
        lblInvoiceStatus.Padding = New Padding(4, 0, 0, 0)
        lblInvoiceStatus.TextAlign = ContentAlignment.MiddleLeft

        dgvInvoice.Dock = DockStyle.Fill
        dgvInvoice.ReadOnly = True
        dgvInvoice.AllowUserToAddRows = False
        dgvInvoice.AllowUserToDeleteRows = False
        dgvInvoice.AutoGenerateColumns = False
        dgvInvoice.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvInvoice.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize

        dgvInvoice.Columns.Add(New DataGridViewTextBoxColumn() With {
            .DataPropertyName = "InvoiceNumber",
            .HeaderText = "Invoice #",
            .Width = 110
        })
        dgvInvoice.Columns.Add(New DataGridViewTextBoxColumn() With {
            .DataPropertyName = "Amount",
            .HeaderText = "Amount",
            .Width = 110
        })
        dgvInvoice.Columns.Add(New DataGridViewTextBoxColumn() With {
            .DataPropertyName = "CompanyCode",
            .HeaderText = "Company Code",
            .Width = 130
        })
        dgvInvoice.Columns.Add(New DataGridViewTextBoxColumn() With {
            .DataPropertyName = "Status",
            .HeaderText = "Status",
            .Width = 110
        })

        pnlInvoice.Controls.Add(dgvInvoice)
        pnlInvoice.Controls.Add(lblInvoiceStatus)
        pnlInvoice.Controls.Add(lblInvoiceTitle)

        ' Wire up events
        AddHandler btnRefresh.Click,
            Sub()
                LoadData()
                ApplyFilters()
            End Sub

        AddHandler txtCustomer.TextChanged, Sub() ApplyFilters()
        AddHandler txtCheck.TextChanged, Sub() ApplyFilters()
        AddHandler cmbYear.SelectedIndexChanged, Sub() ApplyFilters()
        AddHandler cmbMonth.SelectedIndexChanged, Sub() ApplyFilters()
        AddHandler dgv.SelectionChanged, Sub() OnMainGridSelectionChanged()

        ' Add to form; order determines docking priority
        Controls.Add(dgv)        ' Fill  (added first)
        Controls.Add(pnlInvoice) ' Bottom
        Controls.Add(topPanel)   ' Top   (added last)
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
        ' We don't know min/max upfront; just provide a reasonable range + all.
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

    ' Invoice details

    Private Sub OnMainGridSelectionChanged()
        If dgv.SelectedRows.Count = 0 Then
            ClearInvoiceDetails("Select a check row above to view invoice details.")
            Return
        End If

        Dim entry = TryCast(dgv.SelectedRows(0).DataBoundItem, LedgerEntry)
        If entry Is Nothing Then
            ClearInvoiceDetails("Select a check row above to view invoice details.")
            Return
        End If

        ShowInvoiceDetails(entry)
    End Sub

    Private Sub ClearInvoiceDetails(statusMsg As String)
        dgvInvoice.DataSource = Nothing
        lblInvoiceStatus.Text = statusMsg
    End Sub

    Private Sub ShowInvoiceDetails(entry As LedgerEntry)
        If Not _invIndexReady Then
            ClearInvoiceDetails("Loading invoice index, please wait...")
            Return
        End If

        Dim key As String = BuildKey(entry.Customer, entry.CheckNumber)
        Dim invoiceNumbers As List(Of Long) = Nothing

        If Not _invIndex.TryGetValue(key, invoiceNumbers) OrElse
           invoiceNumbers Is Nothing OrElse invoiceNumbers.Count = 0 Then
            ClearInvoiceDetails("No invoice details available (check predates CHECK.INV coverage or has no invoices).")
            Return
        End If

        Dim invChkPath As String = LegacyDataPaths.InvoiceChk
        Dim rows As New List(Of InvoiceDetailRow)()

        For Each invNo As Long In invoiceNumbers
            Dim rec = InvoiceChkReader.ReadRecord(invChkPath, invNo)
            rows.Add(New InvoiceDetailRow() With {
                .InvoiceNumber = invNo,
                .Amount = If(rec.IsFound, rec.Amount.ToString("C"), "Unavailable"),
                .CompanyCode = If(rec.IsFound, rec.CompanyCode, ""),
                .Status = If(rec.IsFound, rec.FlagDescription, "Unavailable")
            })
        Next

        dgvInvoice.DataSource = rows
        lblInvoiceStatus.Text =
            $"Check #{entry.CheckNumber.Trim()}  |  Customer: {entry.Customer.TrimEnd()}  |  {invoiceNumbers.Count} invoice(s)"
    End Sub

    Private Sub StartLoadCheckInvIndex()
        lblInvoiceStatus.Text = "Loading CHECK.INV index..."
        Dim path As String = LegacyDataPaths.CheckInv

        Task.Run(
            Sub()
                Dim idx As New Dictionary(Of String, List(Of Long))(StringComparer.OrdinalIgnoreCase)

                Try
                    If System.IO.File.Exists(path) Then
                        Dim blocks = CheckInvReader.ReadAll(path)
                        For Each b In blocks
                            Dim key As String = BuildKey(b.CustomerCode, b.CheckNumber)
                            Dim lst As List(Of Long) = Nothing
                            If Not idx.TryGetValue(key, lst) Then
                                lst = New List(Of Long)()
                                idx(key) = lst
                            End If
                            If b.Invoices IsNot Nothing Then
                                For Each invStr In b.Invoices
                                    Dim invNo As Long
                                    If Long.TryParse(invStr.Trim(), invNo) Then
                                        lst.Add(invNo)
                                    End If
                                Next
                            End If
                        Next
                    End If
                Catch
                    ' Best-effort; proceed with the empty index built so far.
                End Try

                ' Guard against disposal race: IsDisposed check then Invoke,
                ' catching ObjectDisposedException should the form close between the two.
                Try
                    If Not Me.IsDisposed Then
                        Me.Invoke(Sub()
                                      _invIndex = idx
                                      _invIndexReady = True
                                      ' Refresh panel if a row is already selected.
                                      OnMainGridSelectionChanged()
                                  End Sub)
                    End If
                Catch __e As ObjectDisposedException
                    ' Form was closed while the background task was running; ignore.
                End Try
            End Sub)
    End Sub

    Private Shared Function BuildKey(code As String, checkNum As String) As String
        Return If(code, "").TrimEnd() & "|" & If(checkNum, "").Trim()
    End Function

End Class
