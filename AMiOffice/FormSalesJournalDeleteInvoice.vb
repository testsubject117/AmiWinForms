Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

' Sales Journal Option 6 - Delete An Invoice
' DOS: Prompts for invoice#, searches JOURNAL.CUR, shows details, asks Y/N.
'      If Y: copies all OTHER records to JOURNAL.TMP, then renames to JOURNAL.CUR.
'      DOS was silent on success. VB adds a confirmation message.
' VB:  Same flow with DosMessageBox confirmation on success.
Public Class FormSalesJournalDeleteInvoice
    Inherits Form

    Private ReadOnly lblTitle As New Label()
    Private ReadOnly pnlPrompt As New Panel()
    Private ReadOnly lblPromptText As New Label()
    Private ReadOnly txtInvoice As New TextBox()
    Private ReadOnly btnSearch As New Button()
    Private ReadOnly lblWait As New Label()
    Private ReadOnly tmrFlash As New Timer()
    Private ReadOnly pnlDetails As New Panel()
    Private ReadOnly lblDetails As New Label()
    Private ReadOnly btnDelete As New Button()
    Private ReadOnly btnCancel As New Button()
    Private ReadOnly btnClose As New Button()
    Private ReadOnly lblStatus As New Label()

    ' Holds the found invoice records pending deletion decision
    Private _foundRecords As New List(Of JournalEntry)()
    Private _foundInvoiceNum As Long = 0

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = "Sales Journal - Delete An Invoice"
        Width = 900
        Height = 600
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.FromArgb(32, 32, 32)
        ForeColor = Color.White
        KeyPreview = True

        BuildUi()

        tmrFlash.Interval = 500
        AddHandler tmrFlash.Tick, Sub() lblWait.Visible = Not lblWait.Visible
    End Sub

    Private Sub BuildUi()
        lblTitle.Text = "<<<< DELETE AN INVOICE >>>>"
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

        lblPromptText.Text = "INVOICE NUMBER TO DELETE:"
        lblPromptText.AutoSize = True
        lblPromptText.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblPromptText.ForeColor = Color.White
        lblPromptText.Location = New Point(8, 12)
        pnlPrompt.Controls.Add(lblPromptText)

        txtInvoice.Width = 100
        txtInvoice.MaxLength = 10
        txtInvoice.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtInvoice.Location = New Point(270, 10)
        txtInvoice.BackColor = Color.Black
        txtInvoice.ForeColor = Color.White
        AddHandler txtInvoice.KeyDown, AddressOf TxtInvoice_KeyDown
        pnlPrompt.Controls.Add(txtInvoice)

        btnSearch.Text = "Find Invoice"
        btnSearch.Width = 110
        btnSearch.Height = 26
        btnSearch.Location = New Point(382, 10)
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
        lblWait.Location = New Point(506, 12)
        lblWait.Visible = False
        pnlPrompt.Controls.Add(lblWait)

        Controls.Add(pnlPrompt)
        pnlPrompt.BringToFront()

        ' Details panel (shown after invoice found)
        pnlDetails.Dock = DockStyle.Fill
        pnlDetails.BackColor = Color.FromArgb(25, 25, 25)
        pnlDetails.Padding = New Padding(16)
        pnlDetails.Visible = False

        lblDetails.AutoSize = False
        lblDetails.Dock = DockStyle.Top
        lblDetails.Height = 180
        lblDetails.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        lblDetails.ForeColor = Color.White
        lblDetails.BackColor = Color.FromArgb(25, 25, 25)
        pnlDetails.Controls.Add(lblDetails)

        ' Delete / Cancel buttons inside details panel
        Dim btnPanel As New FlowLayoutPanel()
        btnPanel.Dock = DockStyle.Bottom
        btnPanel.Height = 44
        btnPanel.FlowDirection = FlowDirection.LeftToRight
        btnPanel.BackColor = Color.FromArgb(25, 25, 25)
        btnPanel.Padding = New Padding(8, 6, 8, 4)

        btnDelete.Text = "(Y) Yes - Delete Invoice"
        btnDelete.Width = 220
        btnDelete.Height = 32
        btnDelete.FlatStyle = FlatStyle.Flat
        btnDelete.BackColor = Color.DarkRed
        btnDelete.ForeColor = Color.White
        btnDelete.Font = New Font("Segoe UI", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        AddHandler btnDelete.Click, AddressOf BtnDelete_Click
        btnPanel.Controls.Add(btnDelete)

        btnCancel.Text = "(N) No - Cancel"
        btnCancel.Width = 160
        btnCancel.Height = 32
        btnCancel.Margin = New Padding(12, 0, 0, 0)
        btnCancel.FlatStyle = FlatStyle.Flat
        btnCancel.BackColor = Color.DimGray
        btnCancel.ForeColor = Color.White
        btnCancel.Font = New Font("Segoe UI", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        AddHandler btnCancel.Click, AddressOf BtnCancel_Click
        btnPanel.Controls.Add(btnCancel)

        pnlDetails.Controls.Add(btnPanel)
        Controls.Add(pnlDetails)

        lblStatus.Dock = DockStyle.Bottom
        lblStatus.Height = 28
        lblStatus.Font = New Font("Consolas", 10.0F, FontStyle.Regular, GraphicsUnit.Point)
        lblStatus.ForeColor = Color.Cyan
        lblStatus.BackColor = Color.FromArgb(20, 20, 20)
        lblStatus.TextAlign = ContentAlignment.MiddleLeft
        lblStatus.Padding = New Padding(8, 0, 0, 0)
        Controls.Add(lblStatus)

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

        txtInvoice.Focus()
    End Sub

    Private Sub TxtInvoice_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then BtnSearch_Click(sender, e)
        If e.KeyCode = Keys.Y AndAlso pnlDetails.Visible Then BtnDelete_Click(sender, e)
        If e.KeyCode = Keys.N AndAlso pnlDetails.Visible Then BtnCancel_Click(sender, e)
    End Sub

    Private Sub BtnSearch_Click(sender As Object, e As EventArgs)
        Dim invoiceText As String = txtInvoice.Text.Trim()
        Dim invoiceNum As Long
        If Not Long.TryParse(invoiceText, invoiceNum) OrElse invoiceNum <= 0 Then
            DosMessageBox.Show(Me, "Please enter a valid invoice number.", "Delete Invoice", MessageBoxButtons.OK)
            Return
        End If

        tmrFlash.Start()
        lblWait.Visible = True
        pnlDetails.Visible = False
        lblStatus.Text = $"Looking for Invoice # {invoiceNum}..."
        Application.DoEvents()

        Try
            Dim allRecords = JournalCurReader.ReadAllRecords(LegacyDataPaths.JournalCur)
            _foundRecords = allRecords.Where(Function(r) r.InvoiceNumber = invoiceNum).ToList()
            _foundInvoiceNum = invoiceNum

            tmrFlash.Stop()
            lblWait.Visible = False

            If _foundRecords.Count = 0 Then
                lblStatus.Text = $"Invoice # {invoiceNum} not found."
                DosMessageBox.Show(Me, $"Invoice # {invoiceNum} was not found in the Sales Journal.", "Delete Invoice", MessageBoxButtons.OK)
                txtInvoice.Clear()
                txtInvoice.Focus()
                Return
            End If

            ' Show invoice details
            Dim first = _foundRecords.First()
            Dim sb As New System.Text.StringBuilder()
            sb.AppendLine($"Customer:   {first.CustomerName}")
            sb.AppendLine($"Invoice #:  {first.InvoiceNumber}")
            sb.AppendLine()
            For Each rec In _foundRecords
                sb.AppendLine($"  Procedure: {rec.ProcedureName}")
                sb.AppendLine($"  P.O. #:    {rec.PoNumber}     Date: {rec.DateString}     Charge: {rec.Amount:C2}")
                sb.AppendLine()
            Next
            Dim total As Decimal = _foundRecords.Sum(Function(r) r.Amount)
            sb.AppendLine($"  Total Charge: {total:C2}")
            sb.AppendLine()
            sb.AppendLine("Delete this invoice? Press (Y) Yes or (N) No")

            lblDetails.Text = sb.ToString()
            pnlDetails.Visible = True
            lblStatus.Text = $"Invoice # {invoiceNum} found.   Line items: {_foundRecords.Count}   Total: {total:C2}"

            btnDelete.Focus()

        Catch ex As Exception
            tmrFlash.Stop()
            lblWait.Visible = False
            DosMessageBox.Show(Me, $"Error reading Sales Journal: {ex.Message}", "Delete Invoice", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub BtnDelete_Click(sender As Object, e As EventArgs)
        If _foundRecords.Count = 0 Then Return

        ' DOS parity: write all records EXCEPT the deleted invoice to temp file, then rename.
        Dim journalPath As String = LegacyDataPaths.JournalCur
        Dim tmpPath As String = Path.Combine(Path.GetDirectoryName(journalPath), "JOURNAL.TMP")

        Try
            Dim allRecords = JournalCurReader.ReadAllRecords(journalPath)
            Dim remaining = allRecords.Where(Function(r) r.InvoiceNumber <> _foundInvoiceNum).ToList()

            ' Write to temp file
            Using writer As New StreamWriter(tmpPath, append:=False)
                For Each rec In remaining
                    writer.WriteLine($"""{rec.DateString}""")
                    writer.WriteLine(rec.InvoiceNumber)
                    writer.WriteLine($"""{rec.PoNumber}""")
                    writer.WriteLine(rec.Amount.ToString("G", System.Globalization.CultureInfo.InvariantCulture))
                    writer.WriteLine($"""{rec.CustomerName}""")
                    writer.WriteLine($"""{rec.ProcedureName}""")
                Next
            End Using

            ' Replace journal with temp file (DOS parity: KILL + NAME)
            If File.Exists(journalPath) Then File.Delete(journalPath)
            File.Move(tmpPath, journalPath)

            lblStatus.Text = $"Invoice # {_foundInvoiceNum} deleted successfully."

            ' VB improvement: confirmation message (DOS was silent)
            DosMessageBox.Show(Me, $"Invoice # {_foundInvoiceNum} has been deleted.", "Delete Invoice", MessageBoxButtons.OK)

            ' Reset for next lookup
            pnlDetails.Visible = False
            txtInvoice.Clear()
            _foundRecords.Clear()
            _foundInvoiceNum = 0
            txtInvoice.Focus()

        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error deleting invoice: {ex.Message}", "Delete Invoice", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs)
        pnlDetails.Visible = False
        txtInvoice.Clear()
        _foundRecords.Clear()
        _foundInvoiceNum = 0
        lblStatus.Text = "Deletion cancelled."
        txtInvoice.Focus()
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
