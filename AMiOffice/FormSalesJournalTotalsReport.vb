Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

' Sales Journal Options 4 & 5 - List All Processes With Totals & % Of Business
' DOS: Aggregates JOURNAL.CUR by procedure, sorts by amount desc, prints report to LPT1:.
'      Option 4: all companies.  Option 5: one company (filtered by company name).
' VB:  Same aggregation and sort, displays in DataGridView on screen.
'      Anodize-family procedures flagged with * and subtotaled (DOS parity).
Public Class FormSalesJournalTotalsReport
    Inherits Form

    Private ReadOnly _allCompanies As Boolean

    ' Anodize-family keywords from SALES.BAS line 855
    Private Shared ReadOnly AnodizeKeywords As String() = {
        "ANODIZE", "CHEM FILM", "DICHROMATE", "BRIGHT DIP", "MASK", "SLOW STRIP"
    }

    ' UI controls
    Private ReadOnly lblTitle As New Label()
    Private ReadOnly pnlInputs As New Panel()
    Private ReadOnly lblCompanyLabel As New Label()
    Private ReadOnly txtCompany As New TextBox()
    Private ReadOnly lblBegLabel As New Label()
    Private ReadOnly txtBeg As New TextBox()
    Private ReadOnly lblEndLabel As New Label()
    Private ReadOnly txtEnd As New TextBox()
    Private ReadOnly btnRun As New Button()
    Private ReadOnly lblWait As New Label()
    Private ReadOnly tmrFlash As New Timer()
    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly lblStatus As New Label()
    Private ReadOnly btnPrint As New Button()
    Private ReadOnly btnClose As New Button()

    Public Sub New(allCompanies As Boolean)
        _allCompanies = allCompanies
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = If(_allCompanies,
                  "Sales Journal - List All Processes - All Companies",
                  "Sales Journal - List All Processes - One Company")
        Width = 1100
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
        Dim titleText As String = If(_allCompanies,
            "<<<< LIST ALL PROCESSES WITH TOTALS & % OF BUSINESS >>>>",
            "<<<< LIST ALL PROCESSES WITH TOTALS & % OF BUSINESS - ONE COMPANY >>>>")

        lblTitle.Text = titleText
        lblTitle.Dock = DockStyle.Top
        lblTitle.Height = 36
        lblTitle.Font = New Font("Consolas", 12.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblTitle.ForeColor = Color.Yellow
        lblTitle.BackColor = Color.Black
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        Controls.Add(lblTitle)

        ' Input panel
        pnlInputs.Dock = DockStyle.Top
        pnlInputs.Height = If(_allCompanies, 56, 90)
        pnlInputs.BackColor = Color.FromArgb(32, 32, 32)
        pnlInputs.Padding = New Padding(8, 6, 8, 4)

        Dim yOff As Integer = 6

        ' Company name row (Option 5 only)
        If Not _allCompanies Then
            lblCompanyLabel.Text = "COMPANY NAME:"
            lblCompanyLabel.AutoSize = True
            lblCompanyLabel.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
            lblCompanyLabel.ForeColor = Color.White
            lblCompanyLabel.Location = New Point(8, yOff + 2)
            pnlInputs.Controls.Add(lblCompanyLabel)

            txtCompany.Width = 120
            txtCompany.MaxLength = 8
            txtCompany.CharacterCasing = CharacterCasing.Upper
            txtCompany.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
            txtCompany.Location = New Point(170, yOff)
            txtCompany.BackColor = Color.Black
            txtCompany.ForeColor = Color.White
            AddHandler txtCompany.KeyDown, AddressOf AnyInput_KeyDown
            pnlInputs.Controls.Add(txtCompany)

            yOff += 34
        End If

        ' Invoice range row
        lblBegLabel.Text = "BEGINNING INVOICE # [1=FIRST]:"
        lblBegLabel.AutoSize = True
        lblBegLabel.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblBegLabel.ForeColor = Color.White
        lblBegLabel.Location = New Point(8, yOff + 2)
        pnlInputs.Controls.Add(lblBegLabel)

        txtBeg.Width = 90
        txtBeg.MaxLength = 10
        txtBeg.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtBeg.Location = New Point(310, yOff)
        txtBeg.BackColor = Color.Black
        txtBeg.ForeColor = Color.White
        AddHandler txtBeg.KeyDown, AddressOf AnyInput_KeyDown
        pnlInputs.Controls.Add(txtBeg)

        lblEndLabel.Text = "ENDING INVOICE # [1=LAST]:"
        lblEndLabel.AutoSize = True
        lblEndLabel.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblEndLabel.ForeColor = Color.White
        lblEndLabel.Location = New Point(420, yOff + 2)
        pnlInputs.Controls.Add(lblEndLabel)

        txtEnd.Width = 90
        txtEnd.MaxLength = 10
        txtEnd.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtEnd.Location = New Point(672, yOff)
        txtEnd.BackColor = Color.Black
        txtEnd.ForeColor = Color.White
        AddHandler txtEnd.KeyDown, AddressOf AnyInput_KeyDown
        pnlInputs.Controls.Add(txtEnd)

        btnRun.Text = "Generate Report"
        btnRun.Width = 140
        btnRun.Height = 26
        btnRun.Location = New Point(776, yOff)
        btnRun.FlatStyle = FlatStyle.Flat
        btnRun.BackColor = Color.DimGray
        btnRun.ForeColor = Color.White
        btnRun.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        AddHandler btnRun.Click, AddressOf BtnRun_Click
        pnlInputs.Controls.Add(btnRun)

        lblWait.Text = "Please Wait, Reading Sales Journal..."
        lblWait.AutoSize = True
        lblWait.Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblWait.ForeColor = Color.Cyan
        lblWait.Location = New Point(8, yOff + 34)
        lblWait.Visible = False
        pnlInputs.Controls.Add(lblWait)

        Controls.Add(pnlInputs)
        pnlInputs.BringToFront()

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

        ' DataGridView for report
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

        dgv.Columns.Add("Procedure", "Procedure")
        dgv.Columns.Add("Total", "Total")
        dgv.Columns.Add("Percent", "Percent")
        dgv.Columns.Add("Bar", "% Chart")

        dgv.Columns("Procedure").FillWeight = 40
        dgv.Columns("Total").FillWeight = 18
        dgv.Columns("Total").DefaultCellStyle.Format = "C2"
        dgv.Columns("Total").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgv.Columns("Percent").FillWeight = 12
        dgv.Columns("Percent").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        dgv.Columns("Bar").FillWeight = 30

        Controls.Add(dgv)
        dgv.BringToFront()

        If Not _allCompanies Then
            txtCompany.Focus()
        Else
            txtBeg.Focus()
        End If
    End Sub

    Private Sub AnyInput_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then BtnRun_Click(sender, e)
    End Sub

    Private Sub BtnRun_Click(sender As Object, e As EventArgs)
        ' Validate company (Option 5 only)
        Dim companyName As String = ""
        If Not _allCompanies Then
            companyName = txtCompany.Text.Trim().ToUpperInvariant()
            If companyName.Length < 4 OrElse companyName.Length > 8 Then
                DosMessageBox.Show(Me, "Company name must be 4 to 8 characters.", "Totals Report", MessageBoxButtons.OK)
                txtCompany.Focus()
                Return
            End If
        End If

        ' Validate invoice range
        Dim ibeg As Long
        Dim iend As Long
        Dim begText As String = txtBeg.Text.Trim()
        Dim endText As String = txtEnd.Text.Trim()

        ' DOS: 1 = FIRST (78436), 1 = LAST (999999)
        Const MinInvoice As Long = 78436L
        Const MaxInvoice As Long = 999999L

        If begText = "1" Then
            ibeg = MinInvoice
        ElseIf Not Long.TryParse(begText, ibeg) OrElse ibeg < MinInvoice Then
            DosMessageBox.Show(Me, $"Beginning invoice must be at least {MinInvoice}, or enter 1 for first.", "Totals Report", MessageBoxButtons.OK)
            txtBeg.Focus()
            Return
        End If

        If endText = "1" Then
            iend = MaxInvoice
        ElseIf Not Long.TryParse(endText, iend) Then
            DosMessageBox.Show(Me, "Please enter a valid ending invoice number, or enter 1 for last.", "Totals Report", MessageBoxButtons.OK)
            txtEnd.Focus()
            Return
        End If

        If ibeg > iend Then
            DosMessageBox.Show(Me, "Beginning invoice must be less than or equal to ending invoice.", "Totals Report", MessageBoxButtons.OK)
            Return
        End If

        RunReport(companyName, ibeg, iend)
    End Sub

    Private Sub RunReport(companyName As String, ibeg As Long, iend As Long)
        tmrFlash.Start()
        lblWait.Visible = True
        dgv.Rows.Clear()
        lblStatus.Text = "Please Wait, Reading Sales Journal..."
        btnRun.Enabled = False
        Application.DoEvents()

        Try
            Dim allRecords = JournalCurReader.ReadAllRecords(LegacyDataPaths.JournalCur)

            ' Filter: company, invoice range, exclude VOID and zero-amount (DOS parity)
            Dim filtered = allRecords.Where(Function(r) Not r.IsVoid AndAlso
                                                        r.Amount <> 0D AndAlso
                                                        r.InvoiceNumber >= ibeg AndAlso
                                                        r.InvoiceNumber <= iend AndAlso
                                                        (String.IsNullOrEmpty(companyName) OrElse
                                                         String.Equals(r.CustomerName, companyName, StringComparison.OrdinalIgnoreCase))).ToList()

            ' Aggregate by procedure name (DOS parity: exact match accumulate)
            Dim grouped = filtered.GroupBy(Function(r) r.ProcedureName.Trim().ToUpperInvariant()) _
                                   .Select(Function(g) New With {
                                       .Procedure = g.First().ProcedureName.Trim(),
                                       .Total = g.Sum(Function(r) r.Amount)
                                   }) _
                                   .OrderByDescending(Function(x) x.Total) _
                                   .ToList()

            Dim grandTotal As Decimal = grouped.Sum(Function(x) x.Total)
            Dim anodizeTotal As Decimal = 0D
            Dim anodizePct As Double = 0.0

            tmrFlash.Stop()
            lblWait.Visible = False

            If grouped.Count = 0 Then
                lblStatus.Text = "No records found for the specified criteria."
                DosMessageBox.Show(Me, "No records found matching the specified criteria.", "Totals Report", MessageBoxButtons.OK)
                btnRun.Enabled = True
                Return
            End If

            ' Populate grid (DOS parity: anodize flagged with *, bar chart)
            For Each item In grouped
                Dim pct As Double = If(grandTotal = 0D, 0.0, CDbl(item.Total) / CDbl(grandTotal) * 100.0)
                Dim isAnodize As Boolean = AnodizeKeywords.Any(Function(k) item.Procedure.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                Dim displayName As String = If(isAnodize, "*" & item.Procedure, item.Procedure)
                Dim barLen As Integer = CInt(Math.Floor(pct * 2))
                Dim bar As String = New String("$"c, Math.Max(0, Math.Min(barLen, 100)))

                If isAnodize Then
                    anodizeTotal += item.Total
                    anodizePct += pct
                End If

                Dim row As Integer = dgv.Rows.Add(displayName, item.Total, $"{pct:F2}%", bar)

                ' Highlight anodize rows
                If isAnodize Then
                    dgv.Rows(row).DefaultCellStyle.ForeColor = Color.LightGreen
                End If
            Next

            ' Anodize subtotal row (DOS parity: always shown)
            Dim anodizeRow As Integer = dgv.Rows.Add($"*Anodize Total", anodizeTotal, $"{anodizePct:F2}%", "")
            dgv.Rows(anodizeRow).DefaultCellStyle.BackColor = Color.FromArgb(30, 60, 30)
            dgv.Rows(anodizeRow).DefaultCellStyle.ForeColor = Color.LightGreen
            dgv.Rows(anodizeRow).DefaultCellStyle.Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point)

            ' Grand total row
            Dim totalRow As Integer = dgv.Rows.Add("TOTAL", grandTotal, "100.00%", "")
            dgv.Rows(totalRow).DefaultCellStyle.BackColor = Color.FromArgb(20, 20, 60)
            dgv.Rows(totalRow).DefaultCellStyle.ForeColor = Color.White
            dgv.Rows(totalRow).DefaultCellStyle.Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point)

            Dim companyNote As String = If(String.IsNullOrEmpty(companyName), "All Companies", $"Company: {companyName}")
            lblStatus.Text = $"{companyNote}   Invoices: {ibeg} - {iend}   Procedures: {grouped.Count}   Grand Total: {grandTotal:C2}"
            btnPrint.Enabled = True

        Catch ex As Exception
            tmrFlash.Stop()
            lblWait.Visible = False
            DosMessageBox.Show(Me, $"Error reading Sales Journal: {ex.Message}", "Totals Report", MessageBoxButtons.OK)
        Finally
            btnRun.Enabled = True
        End Try
    End Sub

    Private Sub BtnPrint_Click(sender As Object, e As EventArgs)
        Try
            Dim title As String = If(_allCompanies,
                "Sales Journal - List All Processes With Totals & % Of Business",
                "Sales Journal - List All Processes With Totals & % Of Business - One Company")
            PrintGrid(dgv, title, lblStatus.Text)
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
