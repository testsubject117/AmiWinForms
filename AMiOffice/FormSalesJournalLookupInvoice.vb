Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

' Sales Journal Option 3 - Look Up An Invoice
' DOS: Prompts for invoice number, shows all line items for that invoice.
'      Returns to prompt so user can look up another. ESC exits.
' VB:  Same flow - input invoice#, show results, clear and repeat.
Public Class FormSalesJournalLookupInvoice
    Inherits Form

    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly lblStatus As New Label()
    Private ReadOnly lblTitle As New Label()
    Private ReadOnly pnlPrompt As New Panel()
    Private ReadOnly lblPromptText As New Label()
    Private ReadOnly txtInvoice As New TextBox()
    Private ReadOnly btnSearch As New Button()
    Private ReadOnly btnClose As New Button()
    Private ReadOnly lblWait As New Label()
    Private ReadOnly tmrFlash As New Timer()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = "Sales Journal - Look Up An Invoice"
        Width = 1100
        Height = 650
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.FromArgb(32, 32, 32)
        ForeColor = Color.White
        KeyPreview = True

        BuildUi()

        tmrFlash.Interval = 500
        AddHandler tmrFlash.Tick, Sub() lblWait.Visible = Not lblWait.Visible
    End Sub

    Private Sub BuildUi()
        lblTitle.Text = "<<<< LOOK UP AN INVOICE >>>>"
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

        lblPromptText.Text = "INVOICE NUMBER TO LOOK UP:"
        lblPromptText.AutoSize = True
        lblPromptText.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblPromptText.ForeColor = Color.White
        lblPromptText.Location = New Point(8, 12)
        pnlPrompt.Controls.Add(lblPromptText)

        txtInvoice.Width = 100
        txtInvoice.MaxLength = 10
        txtInvoice.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtInvoice.Location = New Point(280, 10)
        txtInvoice.BackColor = Color.Black
        txtInvoice.ForeColor = Color.White
        AddHandler txtInvoice.KeyDown, AddressOf TxtInvoice_KeyDown
        pnlPrompt.Controls.Add(txtInvoice)

        btnSearch.Text = "Look Up"
        btnSearch.Width = 90
        btnSearch.Height = 26
        btnSearch.Location = New Point(392, 10)
        btnSearch.FlatStyle = FlatStyle.Flat
        btnSearch.BackColor = Color.DimGray
        btnSearch.ForeColor = Color.White
        btnSearch.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        AddHandler btnSearch.Click, AddressOf BtnSearch_Click
        pnlPrompt.Controls.Add(btnSearch)

        ' Flashing "Please Wait" label (DOS parity)
        lblWait.Text = "Please Wait"
        lblWait.AutoSize = True
        lblWait.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblWait.ForeColor = Color.Cyan
        lblWait.Location = New Point(500, 12)
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

        txtInvoice.Focus()
    End Sub

    Private Sub TxtInvoice_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then BtnSearch_Click(sender, e)
    End Sub

    Private Sub BtnSearch_Click(sender As Object, e As EventArgs)
        Dim invoiceText As String = txtInvoice.Text.Trim()
        Dim invoiceNum As Long
        If Not Long.TryParse(invoiceText, invoiceNum) OrElse invoiceNum <= 0 Then
            DosMessageBox.Show(Me, "Please enter a valid invoice number.", "Look Up Invoice", MessageBoxButtons.OK)
            Return
        End If

        tmrFlash.Start()
        lblWait.Visible = True
        dgv.Rows.Clear()
        lblStatus.Text = $"Looking for Invoice # {invoiceNum}..."
        Application.DoEvents()

        Try
            Dim allRecords = JournalCurReader.ReadAllRecords(LegacyDataPaths.JournalCur)
            Dim matches = allRecords.Where(Function(r) r.InvoiceNumber = invoiceNum).ToList()

            tmrFlash.Stop()
            lblWait.Visible = False

            If matches.Count = 0 Then
                lblStatus.Text = $"Invoice # {invoiceNum} not found."
                DosMessageBox.Show(Me, $"Invoice # {invoiceNum} was not found in the Sales Journal.", "Look Up Invoice", MessageBoxButtons.OK)
            Else
                For Each rec In matches
                    dgv.Rows.Add(rec.DateString, rec.InvoiceNumber, rec.PoNumber, rec.CustomerName, rec.ProcedureName, rec.Amount)
                Next
                Dim total As Decimal = matches.Sum(Function(r) r.Amount)
                Dim voidNote As String = If(matches.Any(Function(r) r.IsVoid), "  [VOID]", "")
                lblStatus.Text = $"Invoice # {invoiceNum}   Line items: {matches.Count}   Total: {total:C2}{voidNote}"
            End If

        Catch ex As Exception
            tmrFlash.Stop()
            lblWait.Visible = False
            DosMessageBox.Show(Me, $"Error reading Sales Journal: {ex.Message}", "Look Up Invoice", MessageBoxButtons.OK)
        End Try

        txtInvoice.Clear()
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
