Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

' Sales Journal Option 7 - Look Up A Procedure
' DOS: Prompts for EXACT procedure name, shows all matching invoices (hundreds/thousands of pages).
' VB:  Prompts for procedure name (partial match supported), loads results into DataGridView.
Public Class FormSalesJournalViewProcedure
    Inherits Form

    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly lblStatus As New Label()
    Private ReadOnly lblTitle As New Label()
    Private ReadOnly pnlPrompt As New Panel()
    Private ReadOnly lblPromptText As New Label()
    Private ReadOnly txtProcedure As New TextBox()
    Private ReadOnly btnSearch As New Button()
    Private ReadOnly btnPrint As New Button()
    Private ReadOnly btnClose As New Button()
    Private ReadOnly lblWait As New Label()
    Private ReadOnly tmrFlash As New Timer()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = "Sales Journal - Look Up A Procedure"
        Width = 1200
        Height = 750
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.FromArgb(32, 32, 32)
        ForeColor = Color.White
        KeyPreview = True

        BuildUi()

        tmrFlash.Interval = 500
        AddHandler tmrFlash.Tick, Sub() lblWait.Visible = Not lblWait.Visible
    End Sub

    Private Sub BuildUi()
        lblTitle.Text = "<<<< LOOK UP A PROCEDURE >>>>"
        lblTitle.Dock = DockStyle.Top
        lblTitle.Height = 36
        lblTitle.Font = New Font("Consolas", 14.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblTitle.ForeColor = Color.Yellow
        lblTitle.BackColor = Color.Black
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        Controls.Add(lblTitle)

        pnlPrompt.Dock = DockStyle.Top
        pnlPrompt.Height = 50
        pnlPrompt.BackColor = Color.FromArgb(32, 32, 32)
        pnlPrompt.Padding = New Padding(8, 8, 8, 4)

        lblPromptText.Text = "PROCEDURE NAME:"
        lblPromptText.AutoSize = True
        lblPromptText.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblPromptText.ForeColor = Color.White
        lblPromptText.Location = New Point(8, 12)
        pnlPrompt.Controls.Add(lblPromptText)

        txtProcedure.Width = 280
        txtProcedure.CharacterCasing = CharacterCasing.Upper
        txtProcedure.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtProcedure.Location = New Point(180, 10)
        txtProcedure.BackColor = Color.Black
        txtProcedure.ForeColor = Color.White
        AddHandler txtProcedure.KeyDown, AddressOf TxtProcedure_KeyDown
        pnlPrompt.Controls.Add(txtProcedure)

        btnSearch.Text = "Search"
        btnSearch.Width = 90
        btnSearch.Height = 26
        btnSearch.Location = New Point(472, 10)
        btnSearch.FlatStyle = FlatStyle.Flat
        btnSearch.BackColor = Color.DimGray
        btnSearch.ForeColor = Color.White
        btnSearch.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        AddHandler btnSearch.Click, AddressOf BtnSearch_Click
        pnlPrompt.Controls.Add(btnSearch)

        lblWait.Text = "Please Wait"
        lblWait.AutoSize = True
        lblWait.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblWait.ForeColor = Color.Cyan
        lblWait.Location = New Point(576, 12)
        lblWait.Visible = False
        pnlPrompt.Controls.Add(lblWait)

        Controls.Add(pnlPrompt)
        pnlPrompt.BringToFront()

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
        dgv.Columns.Add("Customer", "Customer")
        dgv.Columns.Add("Procedure", "Procedure")
        dgv.Columns.Add("Charge", "Charge")

        dgv.Columns("Date").FillWeight = 12
        dgv.Columns("Invoice").FillWeight = 10
        dgv.Columns("PO").FillWeight = 12
        dgv.Columns("Customer").FillWeight = 18
        dgv.Columns("Procedure").FillWeight = 30
        dgv.Columns("Charge").FillWeight = 10
        dgv.Columns("Charge").DefaultCellStyle.Format = "C2"
        dgv.Columns("Charge").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        Controls.Add(dgv)
        dgv.BringToFront()

        txtProcedure.Focus()
    End Sub

    Private Sub TxtProcedure_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then BtnSearch_Click(sender, e)
    End Sub

    Private Sub BtnSearch_Click(sender As Object, e As EventArgs)
        Dim procName As String = txtProcedure.Text.Trim().ToUpperInvariant()
        If String.IsNullOrWhiteSpace(procName) Then
            DosMessageBox.Show(Me, "Please enter a procedure name.", "Look Up Procedure", MessageBoxButtons.OK)
            Return
        End If

        tmrFlash.Start()
        lblWait.Visible = True
        dgv.Rows.Clear()
        lblStatus.Text = $"Please Wait, Reading Sales Journal..."
        Application.DoEvents()

        Try
            Dim allRecords = JournalCurReader.ReadAllRecords(LegacyDataPaths.JournalCur)
            Dim matches = allRecords.Where(Function(r) Not r.IsVoid AndAlso
                                                       r.ProcedureName.IndexOf(procName, StringComparison.OrdinalIgnoreCase) >= 0) _
                                    .OrderBy(Function(r) r.InvoiceNumber) _
                                    .ToList()

            tmrFlash.Stop()
            lblWait.Visible = False

            For Each rec In matches
                dgv.Rows.Add(rec.DateString, rec.InvoiceNumber, rec.PoNumber, rec.CustomerName, rec.ProcedureName, rec.Amount)
            Next

            Dim total As Decimal = matches.Sum(Function(r) r.Amount)
            lblStatus.Text = $"Procedure: {procName}   Records found: {matches.Count}   Total charges: {total:C2}"
            btnPrint.Enabled = matches.Count > 0

            If matches.Count = 0 Then
                DosMessageBox.Show(Me, $"No records found for procedure: {procName}", "Look Up Procedure", MessageBoxButtons.OK)
            End If

        Catch ex As Exception
            tmrFlash.Stop()
            lblWait.Visible = False
            DosMessageBox.Show(Me, $"Error reading Sales Journal: {ex.Message}", "Look Up Procedure", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub BtnPrint_Click(sender As Object, e As EventArgs)
        Try
            PrintGrid(dgv, $"Sales Journal - Procedure: {txtProcedure.Text.Trim().ToUpperInvariant()}", lblStatus.Text)
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

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        tmrFlash.Stop()
        tmrFlash.Dispose()
    End Sub
End Class
