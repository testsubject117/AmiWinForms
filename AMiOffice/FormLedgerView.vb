Option Strict On
Option Explicit On

Imports System.ComponentModel
Imports System.Drawing
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class FormLedgerView

    Private Const ScreenTitle As String = "Ledger - View Checks"

    Private ReadOnly _all As New List(Of LedgerEntry)()

    ' CHECK.INV index:
    '   normalized composite key -> invoice-number list
    '
    ' This is the bridge from a selected check to the invoice numbers attached to it.
    ' The invoice numbers themselves come from CHECK.INV, not from LEDGER.CUR.
    Private _invIndex As New Dictionary(Of String, List(Of Long))(StringComparer.OrdinalIgnoreCase)
    Private _invIndexReady As Boolean = False

    <Browsable(False)>
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property InitialCustomerFilter As String = ""

    <Browsable(False)>
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property InitialCheckFilter As String = ""

    <Browsable(False)>
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property InitialYearFilter As String = ""

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
        ApplyInitialFilters()

        LoadData()
        ApplyFilters()
        StartLoadCheckInvIndex()
    End Sub

    Private Sub BuildUi()
        BackColor = Color.FromArgb(32, 32, 32)
        ForeColor = Color.White

        Dim topPanel As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .Height = 70,
            .ColumnCount = 9,
            .RowCount = 2,
            .BackColor = Color.FromArgb(32, 32, 32),
            .Padding = New Padding(8)
        }

        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35))
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 15))
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 15))
        topPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))

        Dim labelFont As New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)

        Dim lblCustomer As New Label() With {
            .Text = "Customer:",
            .AutoSize = True,
            .Anchor = AnchorStyles.Left,
            .ForeColor = Color.White,
            .BackColor = topPanel.BackColor,
            .Font = labelFont
        }

        Dim lblCheck As New Label() With {
            .Text = "Check #:",
            .AutoSize = True,
            .Anchor = AnchorStyles.Left,
            .ForeColor = Color.White,
            .BackColor = topPanel.BackColor,
            .Font = labelFont
        }

        Dim lblYear As New Label() With {
            .Text = "Year:",
            .AutoSize = True,
            .Anchor = AnchorStyles.Left,
            .ForeColor = Color.White,
            .BackColor = topPanel.BackColor,
            .Font = labelFont
        }

        Dim lblMonth As New Label() With {
            .Text = "Month:",
            .AutoSize = True,
            .Anchor = AnchorStyles.Left,
            .ForeColor = Color.White,
            .BackColor = topPanel.BackColor,
            .Font = labelFont
        }

        txtCustomer.Dock = DockStyle.Fill
        txtCustomer.BackColor = Color.Black
        txtCustomer.ForeColor = Color.White
        txtCustomer.BorderStyle = BorderStyle.FixedSingle
        txtCustomer.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

        txtCheck.Dock = DockStyle.Fill
        txtCheck.BackColor = Color.Black
        txtCheck.ForeColor = Color.White
        txtCheck.BorderStyle = BorderStyle.FixedSingle
        txtCheck.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

        cmbYear.DropDownStyle = ComboBoxStyle.DropDownList
        cmbMonth.DropDownStyle = ComboBoxStyle.DropDownList
        cmbYear.Dock = DockStyle.Fill
        cmbMonth.Dock = DockStyle.Fill
        cmbYear.BackColor = Color.Black
        cmbYear.ForeColor = Color.White
        cmbMonth.BackColor = Color.Black
        cmbMonth.ForeColor = Color.White
        cmbYear.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        cmbMonth.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

        btnRefresh.Text = "Refresh"
        btnRefresh.AutoSize = True
        btnRefresh.Anchor = AnchorStyles.Left
        btnRefresh.UseVisualStyleBackColor = False
        btnRefresh.BackColor = Color.Black
        btnRefresh.ForeColor = Color.White
        btnRefresh.FlatStyle = FlatStyle.Flat
        btnRefresh.FlatAppearance.BorderColor = Color.DimGray
        btnRefresh.FlatAppearance.BorderSize = 1
        btnRefresh.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 48, 48)
        btnRefresh.FlatAppearance.MouseDownBackColor = Color.FromArgb(64, 64, 64)
        btnRefresh.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)

        lblTotal.Text = "Total: $0.00"
        lblTotal.AutoSize = True
        lblTotal.Dock = DockStyle.Fill
        lblTotal.TextAlign = ContentAlignment.MiddleLeft
        lblTotal.ForeColor = Color.Yellow
        lblTotal.BackColor = topPanel.BackColor
        lblTotal.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)

        topPanel.Controls.Add(lblCustomer, 0, 0)
        topPanel.Controls.Add(txtCustomer, 1, 0)
        topPanel.Controls.Add(lblCheck, 2, 0)
        topPanel.Controls.Add(txtCheck, 3, 0)
        topPanel.Controls.Add(lblYear, 4, 0)
        topPanel.Controls.Add(cmbYear, 5, 0)
        topPanel.Controls.Add(lblMonth, 6, 0)
        topPanel.Controls.Add(cmbMonth, 7, 0)
        topPanel.Controls.Add(btnRefresh, 8, 0)

        topPanel.SetColumnSpan(lblTotal, 9)
        topPanel.Controls.Add(lblTotal, 0, 1)

        dgv.Dock = DockStyle.Fill
        dgv.ReadOnly = True
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.AutoGenerateColumns = True
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgv.BackgroundColor = Color.Black
        dgv.GridColor = Color.DimGray
        dgv.BorderStyle = BorderStyle.FixedSingle
        dgv.EnableHeadersVisualStyles = False
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.Black
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Yellow
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.Black
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Yellow
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        dgv.DefaultCellStyle.BackColor = Color.Black
        dgv.DefaultCellStyle.ForeColor = Color.White
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(48, 48, 48)
        dgv.DefaultCellStyle.SelectionForeColor = Color.White
        dgv.DefaultCellStyle.Font = New Font("Consolas", 10.0F, FontStyle.Regular, GraphicsUnit.Point)
        dgv.RowHeadersVisible = False

        pnlInvoice.Dock = DockStyle.Bottom
        pnlInvoice.Height = 220
        pnlInvoice.BorderStyle = BorderStyle.FixedSingle
        pnlInvoice.BackColor = Color.FromArgb(32, 32, 32)

        lblInvoiceTitle.Text = "  Invoice Details"
        lblInvoiceTitle.Dock = DockStyle.Top
        lblInvoiceTitle.Height = 24
        lblInvoiceTitle.Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblInvoiceTitle.TextAlign = ContentAlignment.MiddleLeft
        lblInvoiceTitle.BackColor = Color.Black
        lblInvoiceTitle.ForeColor = Color.Yellow

        lblInvoiceStatus.Text = "Select a check row above to view invoice details."
        lblInvoiceStatus.Dock = DockStyle.Top
        lblInvoiceStatus.Height = 22
        lblInvoiceStatus.Padding = New Padding(4, 0, 0, 0)
        lblInvoiceStatus.TextAlign = ContentAlignment.MiddleLeft
        lblInvoiceStatus.BackColor = pnlInvoice.BackColor
        lblInvoiceStatus.ForeColor = Color.White
        lblInvoiceStatus.Font = New Font("Consolas", 10.0F, FontStyle.Regular, GraphicsUnit.Point)

        dgvInvoice.Dock = DockStyle.Fill
        dgvInvoice.ReadOnly = True
        dgvInvoice.AllowUserToAddRows = False
        dgvInvoice.AllowUserToDeleteRows = False
        dgvInvoice.AutoGenerateColumns = False
        dgvInvoice.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvInvoice.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgvInvoice.BackgroundColor = Color.Black
        dgvInvoice.GridColor = Color.DimGray
        dgvInvoice.BorderStyle = BorderStyle.FixedSingle
        dgvInvoice.EnableHeadersVisualStyles = False
        dgvInvoice.ColumnHeadersDefaultCellStyle.BackColor = Color.Black
        dgvInvoice.ColumnHeadersDefaultCellStyle.ForeColor = Color.Yellow
        dgvInvoice.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.Black
        dgvInvoice.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Yellow
        dgvInvoice.ColumnHeadersDefaultCellStyle.Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        dgvInvoice.DefaultCellStyle.BackColor = Color.Black
        dgvInvoice.DefaultCellStyle.ForeColor = Color.White
        dgvInvoice.DefaultCellStyle.SelectionBackColor = Color.FromArgb(48, 48, 48)
        dgvInvoice.DefaultCellStyle.SelectionForeColor = Color.White
        dgvInvoice.DefaultCellStyle.Font = New Font("Consolas", 10.0F, FontStyle.Regular, GraphicsUnit.Point)
        dgvInvoice.RowHeadersVisible = False

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
        AddHandler dgv.DataBindingComplete, AddressOf OnMainGridDataBindingComplete

        Controls.Add(dgv)
        Controls.Add(pnlInvoice)
        Controls.Add(topPanel)
    End Sub

    Private Sub OnMainGridDataBindingComplete(sender As Object, e As DataGridViewBindingCompleteEventArgs)
        If dgv.Columns.Contains("DateText") Then
            dgv.Columns("DateText").HeaderText = "Date"
        End If

        If dgv.Columns.Contains("CheckNumber") Then
            dgv.Columns("CheckNumber").HeaderText = "Check #"
        End If

        If dgv.Columns.Contains("InvoiceDiffText") Then
            dgv.Columns("InvoiceDiffText").HeaderText = "Invoice Diff"
        End If

        If dgv.Columns.Contains("InvoiceDiffText") Then
            dgv.Columns("InvoiceDiffText").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            dgv.Columns("InvoiceDiffText").Width = 95
        End If

        If dgv.Columns.Contains("Amount") Then
            dgv.Columns("Amount").DefaultCellStyle.Format = "G29"
        End If
    End Sub

    Private Sub ApplyInitialFilters()
        txtCustomer.Text = InitialCustomerFilter.Trim()
        txtCheck.Text = InitialCheckFilter.Trim()

        Dim yearText As String = InitialYearFilter.Trim()
        If yearText <> "" AndAlso cmbYear.Items.Contains(yearText) Then
            cmbYear.SelectedItem = yearText
        End If
    End Sub

    Private Sub PopulatePicklists()
        cmbMonth.Items.Clear()
        cmbMonth.Items.Add("")
        For m As Integer = 1 To 12
            cmbMonth.Items.Add(m.ToString("00"))
        Next
        cmbMonth.SelectedIndex = 0

        cmbYear.Items.Clear()
        cmbYear.Items.Add("")
        For y As Integer = Date.Today.Year To 1986 Step -1
            cmbYear.Items.Add(y.ToString())
        Next
        cmbYear.SelectedIndex = 0
    End Sub

    Private Sub LoadData()
        Dim path = LegacyDataPaths.LedgerCur

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
            Dim yy As String = year.Substring(year.Length - 2)
            q = q.Where(Function(e) If(e.DateText, "").EndsWith(yy, StringComparison.OrdinalIgnoreCase))
        End If

        If month <> "" Then
            q = q.Where(Function(e) If(e.DateText, "").StartsWith(month, StringComparison.OrdinalIgnoreCase))
        End If

        Dim list = q.ToList()

        If _invIndexReady Then
            Dim i As Integer
            For i = 0 To list.Count - 1
                Dim entry = list(i)
                Dim invoiceNumbers As List(Of Long) = GetInvoiceNumbersForEntry(entry)

                If String.IsNullOrWhiteSpace(entry.InvoiceDiffText) AndAlso invoiceNumbers.Count > 0 Then
                    entry.InvoiceDiffText = "0"
                End If
            Next
        End If

        dgv.DataSource = Nothing
        dgv.DataSource = list

        Dim total As Decimal = list.Sum(Function(e) e.Amount)
        lblTotal.Text = $"Total: {total:C}"
    End Sub

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

        Dim invoiceNumbers As List(Of Long) = GetInvoiceNumbersForEntry(entry)

        If invoiceNumbers Is Nothing OrElse invoiceNumbers.Count = 0 Then
            ClearInvoiceDetails("No invoice details available (check predates CHECK.INV coverage or has no invoices).")
            Return
        End If

        Dim invChkPath As String = LegacyDataPaths.InvoiceChk
        Dim rows As New List(Of InvoiceDetailRow)()

        ' Legacy DOS file flow for check/invoice detail:
        '   - LEDGER.CUR supplies the selected check row in the main grid
        '   - CHECK.INV supplies the list of invoice numbers attached to that check
        '   - INVOICE.CHK supplies invoice detail (amount, company, status)
        '     for each invoice using direct random-access lookup:
        '
        '         recordNumber = invoiceNumber - 75000
        '
        ' Original DOS "View Checks" did not inline invoice detail on the ledger screen;
        ' this lower grid is a modern enhancement built from the correct legacy file roles.
        '
        ' Important: invoice detail lookup must not depend on CHECK.INV containing amount
        ' or status information. CHECK.INV is only the relationship/bridge file.
        Dim i As Integer
        For i = 0 To invoiceNumbers.Count - 1
            Dim invNo As Long = invoiceNumbers(i)
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
            $"Check #{SafeTrim(entry.CheckNumber)}  |  Customer: {SafeTrim(entry.Customer)}  |  {invoiceNumbers.Count} invoice(s)"
    End Sub

    Private Function GetInvoiceNumbersForEntry(entry As LedgerEntry) As List(Of Long)
        If entry Is Nothing Then
            Return New List(Of Long)()
        End If

        Dim keys As List(Of String) = BuildCandidateKeys(entry.Customer, entry.CheckNumber)

        Dim i As Integer
        For i = 0 To keys.Count - 1
            Dim key As String = keys(i)
            Dim invoiceNumbers As List(Of Long) = Nothing

            If _invIndex.TryGetValue(key, invoiceNumbers) AndAlso invoiceNumbers IsNot Nothing AndAlso invoiceNumbers.Count > 0 Then
                Return New List(Of Long)(invoiceNumbers)
            End If
        Next

        Return New List(Of Long)()
    End Function

    Private Sub StartLoadCheckInvIndex()
        lblInvoiceStatus.Text = "Loading CHECK.INV index..."
        Dim path As String = LegacyDataPaths.CheckInv

        Task.Run(
            Sub()
                Dim idx As New Dictionary(Of String, List(Of Long))(StringComparer.OrdinalIgnoreCase)

                Try
                    If System.IO.File.Exists(path) Then
                        Dim blocks = CheckInvReader.ReadAll(path)

                        Dim bi As Integer
                        For bi = 0 To blocks.Count - 1
                            Dim b = blocks(bi)
                            Dim keys As List(Of String) = BuildCandidateKeys(b.CustomerCode, b.CheckNumber)
                            Dim parsedInvoices As New List(Of Long)()

                            If b.Invoices IsNot Nothing Then
                                Dim ii As Integer
                                For ii = 0 To b.Invoices.Count - 1
                                    Dim invStr As String = b.Invoices(ii)
                                    Dim invNo As Long
                                    If Long.TryParse(SafeTrim(invStr), invNo) Then
                                        If Not parsedInvoices.Contains(invNo) Then
                                            parsedInvoices.Add(invNo)
                                        End If
                                    End If
                                Next
                            End If

                            Dim ki As Integer
                            For ki = 0 To keys.Count - 1
                                Dim key As String = keys(ki)
                                If key = "" Then Continue For

                                Dim lst As List(Of Long) = Nothing
                                If Not idx.TryGetValue(key, lst) Then
                                    lst = New List(Of Long)()
                                    idx(key) = lst
                                End If

                                Dim pi As Integer
                                For pi = 0 To parsedInvoices.Count - 1
                                    Dim invNo As Long = parsedInvoices(pi)
                                    If Not lst.Contains(invNo) Then
                                        lst.Add(invNo)
                                    End If
                                Next
                            Next
                        Next
                    End If
                Catch
                End Try

                Try
                    If Not Me.IsDisposed Then
                        Me.Invoke(
                            Sub()
                                _invIndex = idx
                                _invIndexReady = True
                                ApplyFilters()
                                OnMainGridSelectionChanged()
                            End Sub)
                    End If
                Catch __e As ObjectDisposedException
                End Try
            End Sub)
    End Sub

    Private Shared Function BuildCandidateKeys(code As String, checkNum As String) As List(Of String)
        Dim results As New List(Of String)()

        Dim codeTrimmed As String = SafeTrim(code)
        Dim checkTrimmed As String = SafeTrim(checkNum)

        Dim codeUpper As String = codeTrimmed.ToUpperInvariant()
        Dim checkUpper As String = checkTrimmed.ToUpperInvariant()

        Dim codeCompact As String = codeUpper.Replace(" ", "")
        Dim checkCompact As String = checkUpper.Replace(" ", "")

        AddUnique(results, codeUpper & "|" & checkUpper)
        AddUnique(results, codeTrimmed & "|" & checkTrimmed)
        AddUnique(results, codeCompact & "|" & checkUpper)
        AddUnique(results, codeUpper & "|" & checkCompact)
        AddUnique(results, codeCompact & "|" & checkCompact)

        Return results
    End Function

    Private Shared Sub AddUnique(list As List(Of String), value As String)
        If list Is Nothing Then Return
        If String.IsNullOrWhiteSpace(value) Then Return

        Dim i As Integer
        For i = 0 To list.Count - 1
            If String.Equals(list(i), value, StringComparison.OrdinalIgnoreCase) Then
                Return
            End If
        Next

        list.Add(value)
    End Sub

    Private Shared Function SafeTrim(value As String) As String
        Return If(value, "").Trim()
    End Function

End Class
