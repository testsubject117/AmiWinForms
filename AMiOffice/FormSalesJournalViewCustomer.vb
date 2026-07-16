Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

' Sales Journal Option 1 - View One Customer
' DOS: Prompts for customer name (up to 8 chars), shows paginated list.
' VB:  Prompts for customer name, loads all matching records into DataGridView.
Public Class FormSalesJournalViewCustomer
    Inherits Form

    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly lblStatus As New Label()
    Private ReadOnly lblTitle As New Label()
    Private ReadOnly pnlPrompt As New Panel()
    Private ReadOnly lblPromptText As New Label()
    Private ReadOnly txtCustomer As New TextBox()
    Private ReadOnly btnSearch As New Button()
    Private ReadOnly btnPrint As New Button()
    Private ReadOnly btnClose As New Button()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = "Sales Journal - View One Customer"
        Width = 1200
        Height = 750
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.FromArgb(32, 32, 32)
        ForeColor = Color.White
        KeyPreview = True

        BuildUi()
    End Sub

    Private Sub BuildUi()
        ' Title
        lblTitle.Text = "<<<< VIEW ONE CUSTOMER >>>>"
        lblTitle.Dock = DockStyle.Top
        lblTitle.Height = 36
        lblTitle.Font = New Font("Consolas", 14.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblTitle.ForeColor = Color.Yellow
        lblTitle.BackColor = Color.Black
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        Controls.Add(lblTitle)

        ' Prompt panel
        pnlPrompt.Dock = DockStyle.Top
        pnlPrompt.Height = 50
        pnlPrompt.BackColor = Color.FromArgb(32, 32, 32)
        pnlPrompt.Padding = New Padding(8, 8, 8, 4)

        lblPromptText.Text = "CUSTOMER NAME:"
        lblPromptText.AutoSize = True
        lblPromptText.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblPromptText.ForeColor = Color.White
        lblPromptText.Location = New Point(8, 12)
        pnlPrompt.Controls.Add(lblPromptText)

        txtCustomer.Width = 120
        txtCustomer.MaxLength = 8
        txtCustomer.CharacterCasing = CharacterCasing.Upper
        txtCustomer.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtCustomer.Location = New Point(170, 10)
        txtCustomer.BackColor = Color.Black
        txtCustomer.ForeColor = Color.White
        AddHandler txtCustomer.KeyDown, AddressOf TxtCustomer_KeyDown
        pnlPrompt.Controls.Add(txtCustomer)

        btnSearch.Text = "Search"
        btnSearch.Width = 90
        btnSearch.Height = 26
        btnSearch.Location = New Point(300, 10)
        btnSearch.FlatStyle = FlatStyle.Flat
        btnSearch.BackColor = Color.DimGray
        btnSearch.ForeColor = Color.White
        btnSearch.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        AddHandler btnSearch.Click, AddressOf BtnSearch_Click
        pnlPrompt.Controls.Add(btnSearch)

        Controls.Add(pnlPrompt)
        pnlPrompt.BringToFront()

        ' Status label
        lblStatus.Dock = DockStyle.Bottom
        lblStatus.Height = 28
        lblStatus.Font = New Font("Consolas", 10.0F, FontStyle.Regular, GraphicsUnit.Point)
        lblStatus.ForeColor = Color.Cyan
        lblStatus.BackColor = Color.FromArgb(20, 20, 20)
        lblStatus.TextAlign = ContentAlignment.MiddleLeft
        lblStatus.Padding = New Padding(8, 0, 0, 0)
        Controls.Add(lblStatus)

        ' Close button
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

        ' DataGridView
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

        dgv.Columns.Add("Date", "Date")
        dgv.Columns.Add("Invoice", "Invoice #")
        dgv.Columns.Add("PO", "P.O. #")
        dgv.Columns.Add("Procedure", "Procedure")
        dgv.Columns.Add("Customer", "Customer")
        dgv.Columns.Add("Charge", "Charge")

        dgv.Columns("Date").FillWeight = 12
        dgv.Columns("Invoice").FillWeight = 10
        dgv.Columns("PO").FillWeight = 12
        dgv.Columns("Procedure").FillWeight = 28
        dgv.Columns("Customer").FillWeight = 18
        dgv.Columns("Charge").FillWeight = 10
        dgv.Columns("Charge").DefaultCellStyle.Format = "C2"
        dgv.Columns("Charge").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        Controls.Add(dgv)
        dgv.BringToFront()

        txtCustomer.Focus()
    End Sub

    Private Sub TxtCustomer_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            BtnSearch_Click(sender, e)
        End If
    End Sub

    Private Sub BtnSearch_Click(sender As Object, e As EventArgs)
        Dim customerName As String = txtCustomer.Text.Trim().ToUpperInvariant()
        If String.IsNullOrWhiteSpace(customerName) Then
            DosMessageBox.Show(Me, "Please enter a customer name.", "View One Customer", MessageBoxButtons.OK)
            Return
        End If

        lblStatus.Text = "Please Wait, Reading Sales Journal..."
        dgv.Rows.Clear()
        Application.DoEvents()

        Try
            If Not System.IO.File.Exists(LegacyDataPaths.JournalCur) Then
                DosMessageBox.Show(Me, $"JOURNAL.CUR not found at:{vbCrLf}{LegacyDataPaths.JournalCur}", "View One Customer", MessageBoxButtons.OK)
                lblStatus.Text = "Error: JOURNAL.CUR not found."
                Return
            End If

            Dim allRecords = JournalCurReader.ReadAllRecords(LegacyDataPaths.JournalCur)
            Dim filtered = allRecords.Where(Function(r) Not r.IsVoid AndAlso
                                                        r.CustomerName.IndexOf(customerName, StringComparison.OrdinalIgnoreCase) >= 0) _
                                     .OrderBy(Function(r) r.InvoiceNumber) _
                                     .ToList()

            For Each rec In filtered
                dgv.Rows.Add(
                    rec.DateString,
                    rec.InvoiceNumber,
                    rec.PoNumber,
                    rec.ProcedureName,
                    rec.CustomerName,
                    rec.Amount)
            Next

            lblStatus.Text = $"Customer: {customerName}   Records found: {filtered.Count}   Total: {filtered.Sum(Function(r) r.Amount):C2}"
            btnPrint.Enabled = filtered.Count > 0

            If filtered.Count = 0 Then
                DosMessageBox.Show(Me, $"No records found for customer: {customerName}", "View One Customer", MessageBoxButtons.OK)
            End If

        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error reading Sales Journal: {ex.Message}", "View One Customer", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub BtnPrint_Click(sender As Object, e As EventArgs)
        Try
            PrintGrid(dgv, $"Sales Journal - Customer: {txtCustomer.Text.Trim().ToUpperInvariant()}", lblStatus.Text)
        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error printing: {ex.Message}", "Print", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Shared Sub PrintGrid(grid As DataGridView, title As String, statusLine As String)
        Dim lines As New List(Of String)()
        lines.Add(title)
        lines.Add(Date.Now.ToString("dddd, MMMM dd, yyyy  hh:mm tt"))
        lines.Add(statusLine)
        lines.Add(New String("-"c, 120))
        Dim header As String = ""
        For Each col As DataGridViewColumn In grid.Columns
            header &= col.HeaderText.PadRight(Math.Max(col.Width \ 7, 8))
        Next
        lines.Add(header)
        lines.Add(New String("-"c, 120))
        For Each row As DataGridViewRow In grid.Rows
            Dim line As String = ""
            For i As Integer = 0 To grid.Columns.Count - 1
                Dim val As String = If(row.Cells(i).FormattedValue?.ToString(), "")
                line &= val.PadRight(Math.Max(grid.Columns(i).Width \ 7, 8))
            Next
            lines.Add(line)
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

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.KeyCode = Keys.Escape Then Me.Close()
    End Sub
End Class
