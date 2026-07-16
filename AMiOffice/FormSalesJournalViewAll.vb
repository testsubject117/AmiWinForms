Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms

' Sales Journal Option 2 - View All Customers
' DOS: No prompt, shows all invoices for all customers (thousands of pages).
' VB:  Loads all non-void records async. DataGridView runs in Virtual Mode so
'      zero rows are ever added to the grid — it requests cell values on demand
'      as the user scrolls. This keeps both load time and search time near-instant
'      regardless of how many records are in the journal.
Public Class FormSalesJournalViewAll
    Inherits Form

    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly lblStatus As New Label()
    Private ReadOnly lblTitle As New Label()
    Private ReadOnly pnlSearch As New Panel()
    Private ReadOnly txtSearch As New TextBox()
    Private ReadOnly lblSearchLabel As New Label()
    Private ReadOnly btnPrint As New Button()
    Private ReadOnly btnClose As New Button()

    ' Debounce timer - fires 400ms after user stops typing
    Private ReadOnly tmrSearch As New Timer() With {.Interval = 400}

    ' _allRows = full unfiltered list loaded from disk
    ' _viewRows = what the grid currently displays (filtered subset or same as _allRows)
    Private _allRows As New List(Of JournalEntry)()
    Private _viewRows As New List(Of JournalEntry)()
    Private _isLoading As Boolean = False

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = "Sales Journal - View All Customers"
        Width = 1200
        Height = 800
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.FromArgb(32, 32, 32)
        ForeColor = Color.White
        KeyPreview = True

        AddHandler tmrSearch.Tick, AddressOf TmrSearch_Tick

        BuildUi()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        FireAndForget(LoadAllRecordsAsync())
    End Sub

    Private Sub BuildUi()
        lblTitle.Text = "<<<< VIEW ALL CUSTOMERS >>>>"
        lblTitle.Dock = DockStyle.Top
        lblTitle.Height = 36
        lblTitle.Font = New Font("Consolas", 14.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblTitle.ForeColor = Color.Yellow
        lblTitle.BackColor = Color.Black
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        Controls.Add(lblTitle)

        pnlSearch.Dock = DockStyle.Top
        pnlSearch.Height = 44
        pnlSearch.BackColor = Color.FromArgb(32, 32, 32)
        pnlSearch.Padding = New Padding(8, 6, 8, 4)

        lblSearchLabel.Text = "SEARCH:"
        lblSearchLabel.AutoSize = True
        lblSearchLabel.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblSearchLabel.ForeColor = Color.White
        lblSearchLabel.Location = New Point(8, 10)
        pnlSearch.Controls.Add(lblSearchLabel)

        txtSearch.Width = 250
        txtSearch.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtSearch.Location = New Point(90, 8)
        txtSearch.BackColor = Color.Black
        txtSearch.ForeColor = Color.White
        txtSearch.Enabled = False
        AddHandler txtSearch.TextChanged, AddressOf TxtSearch_TextChanged
        pnlSearch.Controls.Add(txtSearch)

        Controls.Add(pnlSearch)
        pnlSearch.BringToFront()

        lblStatus.Dock = DockStyle.Bottom
        lblStatus.Height = 28
        lblStatus.Font = New Font("Consolas", 10.0F, FontStyle.Regular, GraphicsUnit.Point)
        lblStatus.ForeColor = Color.Cyan
        lblStatus.BackColor = Color.FromArgb(20, 20, 20)
        lblStatus.TextAlign = ContentAlignment.MiddleLeft
        lblStatus.Padding = New Padding(8, 0, 0, 0)
        Controls.Add(lblStatus)

        btnPrint.Text = "Print"
        btnPrint.Width = 80
        btnPrint.Height = 28
        btnPrint.Dock = DockStyle.Bottom
        btnPrint.FlatStyle = FlatStyle.Flat
        btnPrint.BackColor = Color.DimGray
        btnPrint.ForeColor = Color.White
        btnPrint.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        btnPrint.Enabled = False
        AddHandler btnPrint.Click, AddressOf BtnPrint_Click
        Controls.Add(btnPrint)

        btnClose.Text = "(ESC) Close"
        btnClose.Width = 120
        btnClose.Height = 28
        btnClose.Dock = DockStyle.Bottom
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.BackColor = Color.Silver
        btnClose.ForeColor = Color.Black
        btnClose.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        AddHandler btnClose.Click, Sub() Me.Close()
        Controls.Add(btnClose)

        dgv.Dock = DockStyle.Fill
        dgv.BackgroundColor = Color.FromArgb(20, 20, 20)
        dgv.GridColor = Color.DimGray
        dgv.DefaultCellStyle.BackColor = Color.FromArgb(20, 20, 20)
        dgv.DefaultCellStyle.ForeColor = Color.White
        dgv.DefaultCellStyle.Font = New Font("Consolas", 10.0F, FontStyle.Regular, GraphicsUnit.Point)
        dgv.DefaultCellStyle.SelectionBackColor = Color.DarkSlateBlue
        dgv.DefaultCellStyle.SelectionForeColor = Color.White
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.Black
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Yellow
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        dgv.RowHeadersVisible = False
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.ReadOnly = True
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        ' Virtual mode: grid never holds row objects - requests values on demand as user scrolls
        dgv.VirtualMode = True
        AddHandler dgv.CellValueNeeded, AddressOf Dgv_CellValueNeeded

        dgv.Columns.Add("Date", "Date")
        dgv.Columns.Add("Invoice", "Invoice #")
        dgv.Columns.Add("PO", "P.O. #")
        dgv.Columns.Add("Customer", "Customer")
        dgv.Columns.Add("Procedure", "Procedure")
        dgv.Columns.Add("Charge", "Charge")

        dgv.Columns("Date").FillWeight = 12
        dgv.Columns("Invoice").FillWeight = 10
        dgv.Columns("PO").FillWeight = 12
        dgv.Columns("Customer").FillWeight = 18
        dgv.Columns("Procedure").FillWeight = 28
        dgv.Columns("Charge").FillWeight = 10
        dgv.Columns("Charge").DefaultCellStyle.Format = "C2"
        dgv.Columns("Charge").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        Controls.Add(dgv)
        dgv.BringToFront()
    End Sub

    Private Async Function LoadAllRecordsAsync() As Task
        _isLoading = True
        lblStatus.Text = "Please Wait, Reading Sales Journal..."
        txtSearch.Enabled = False
        btnPrint.Enabled = False

        Try
            ' File read happens on background thread - no UI access inside Task.Run
            Dim result As List(Of JournalEntry) = Await Task.Run(Function()
                                            Return JournalCurReader.ReadAllRecords(LegacyDataPaths.JournalCur) _
                                                                    .Where(Function(r) Not r.IsVoid) _
                                                                    .OrderBy(Function(r) r.InvoiceNumber) _
                                                                    .ToList()
                                                                   End Function)

            ' Await returns us to the UI thread - safe to touch controls here
            _allRows = result
            _isLoading = False
            SetView(_allRows)
            txtSearch.Enabled = True
            txtSearch.Focus()
        Catch ex As Exception
            _isLoading = False
            DosMessageBox.Show(Me, $"Error reading Sales Journal: {ex.Message}", "View All Customers", MessageBoxButtons.OK)
        End Try
    End Function

    ' SetView replaces PopulateGrid - with virtual mode we just set RowCount.
    ' The grid displays instantly regardless of list size because no row objects are created.
    Private Sub SetView(records As List(Of JournalEntry))
        _viewRows = records
        dgv.RowCount = records.Count
        If dgv.RowCount > 0 Then dgv.FirstDisplayedScrollingRowIndex = 0
        lblStatus.Text = $"Total records: {records.Count}   Total charges: {records.Sum(Function(r) r.Amount):C2}"
        btnPrint.Enabled = records.Count > 0
    End Sub

    ' Grid calls this for every visible cell as the user scrolls - no row objects ever created
    Private Sub Dgv_CellValueNeeded(sender As Object, e As DataGridViewCellValueEventArgs)
        If e.RowIndex < 0 OrElse e.RowIndex >= _viewRows.Count Then Return
        Dim rec As JournalEntry = _viewRows(e.RowIndex)
        Select Case e.ColumnIndex
            Case 0 : e.Value = rec.DateString
            Case 1 : e.Value = rec.InvoiceNumber
            Case 2 : e.Value = rec.PoNumber
            Case 3 : e.Value = rec.CustomerName
            Case 4 : e.Value = rec.ProcedureName
            Case 5 : e.Value = rec.Amount
        End Select
    End Sub

    ' TextChanged just resets the debounce timer - does NOT filter immediately
    Private Sub TxtSearch_TextChanged(sender As Object, e As EventArgs)
        If _isLoading Then Return
        tmrSearch.Stop()
        tmrSearch.Start()
    End Sub

    ' Fires 400ms after user stops typing
    Private Sub TmrSearch_Tick(sender As Object, e As EventArgs)
        tmrSearch.Stop()
        If _isLoading Then Return

        Dim term As String = txtSearch.Text.Trim()
        If String.IsNullOrWhiteSpace(term) Then
            SetView(_allRows)
            Return
        End If

        lblStatus.Text = "Filtering..."
        Dim filtered = _allRows.Where(Function(r) r.CustomerName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                                   r.ProcedureName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                                   r.InvoiceNumber.ToString().Contains(term)).ToList()
        SetView(filtered)
    End Sub

    Private Sub BtnPrint_Click(sender As Object, e As EventArgs)
        Try
            Dim searchTerm As String = txtSearch.Text.Trim()
            Dim title As String = If(String.IsNullOrWhiteSpace(searchTerm),
                "Sales Journal - All Customers",
                $"Sales Journal - All Customers (Filter: {searchTerm})")
            PrintRows(_viewRows, title, lblStatus.Text)
        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error printing: {ex.Message}", "Print", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Shared Sub PrintRows(records As List(Of JournalEntry), title As String, statusLine As String)
        Dim lines As New List(Of String)()
        lines.Add(title)
        lines.Add(Date.Now.ToString("dddd, MMMM dd, yyyy  hh:mm tt"))
        lines.Add(statusLine)
        lines.Add(New String("-"c, 120))
        lines.Add("Date        Invoice #   P.O. #      Customer            Procedure                   Charge")
        lines.Add(New String("-"c, 120))
        For Each rec In records
            lines.Add($"{rec.DateString,-12}{rec.InvoiceNumber,-12}{rec.PoNumber,-12}{rec.CustomerName,-20}{rec.ProcedureName,-28}{rec.Amount,10:C2}")
        Next
        Dim currentLine As Integer = 0
        Dim printDoc As New System.Drawing.Printing.PrintDocument()
        AddHandler printDoc.PrintPage, Sub(s, ev)
                                           Dim font As New Font("Courier New", 8)
                                           Dim y As Single = ev.MarginBounds.Top
                                           Dim lineH As Single = font.GetHeight(ev.Graphics)
                                           While currentLine < lines.Count AndAlso y + lineH < ev.MarginBounds.Bottom
                                               ev.Graphics.DrawString(lines(currentLine), font, Brushes.Black, ev.MarginBounds.Left, y)
                                               y += lineH
                                               currentLine += 1
                                           End While
                                           ev.HasMorePages = (currentLine < lines.Count)
                                       End Sub
        Dim dlg As New PrintDialog()
        dlg.Document = printDoc
        If dlg.ShowDialog() = DialogResult.OK Then
            printDoc.Print()
        End If
    End Sub

    Private Shared Sub FireAndForget(t As Task)
        ' Intentionally not awaited - fire and forget pattern for background loads
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.KeyCode = Keys.Escape Then Me.Close()
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        tmrSearch.Stop()
        tmrSearch.Dispose()
    End Sub
End Class
