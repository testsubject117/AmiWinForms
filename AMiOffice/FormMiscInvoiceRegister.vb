Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

' Matt Sprague Request #1 — Invoice Register
' View modes: Detail | By Customer | By Year
Public Class FormMiscInvoiceRegister
    Inherits Form

    Private WithEvents btnLoad    As New Button()
    Private WithEvents btnExport  As New Button()
    Private WithEvents btnPrint   As New Button()
    Private WithEvents btnClose   As New Button()
    Private _printRowIndex As Integer
    Private WithEvents dtpFrom    As New DateTimePicker()
    Private WithEvents dtpTo      As New DateTimePicker()
    Private WithEvents grid       As New DataGridView()
    Private WithEvents rbDetail   As New RadioButton()
    Private WithEvents rbCustomer As New RadioButton()
    Private WithEvents rbCustDetail As New RadioButton()
    Private WithEvents rbYear     As New RadioButton()
    Private lblStatus             As New Label()

    Private _filtered As List(Of JournalEntryEx)

    Private Class JournalEntryEx
        Public Property Entry As JournalEntry
        Public Property ParsedDate As DateTime
    End Class

    Public Sub New()
        Me.Text = "Invoice Register"
        Me.Size = New Size(1100, 720)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.Black
        Me.ForeColor = Color.Yellow
        Me.Font = New Font("Segoe UI", 10, FontStyle.Regular)
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.KeyPreview = True
        BuildUI()
    End Sub

    Private Sub BuildUI()
        Dim lblTitle As New Label() With {
            .Text = "INVOICE REGISTER",
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .ForeColor = Color.Yellow, .BackColor = Color.Black,
            .AutoSize = True, .Location = New Point(12, 10)
        }
        Dim lblFrom As New Label() With {.Text = "From:", .ForeColor = Color.White, .BackColor = Color.Black, .AutoSize = True, .Location = New Point(12, 52)}
        dtpFrom.Location = New Point(60, 48) : dtpFrom.Width = 130 : dtpFrom.Format = DateTimePickerFormat.Short : dtpFrom.Value = New DateTime(2021, 1, 1)
        Dim lblTo As New Label() With {.Text = "To:", .ForeColor = Color.White, .BackColor = Color.Black, .AutoSize = True, .Location = New Point(205, 52)}
        dtpTo.Location = New Point(230, 48) : dtpTo.Width = 130 : dtpTo.Format = DateTimePickerFormat.Short : dtpTo.Value = DateTime.Today

        btnLoad.Text = "Load" : btnLoad.Location = New Point(375, 46) : btnLoad.Size = New Size(80, 30)
        StyleBtn(btnLoad)

        btnExport.Text = "Export CSV" : btnExport.Location = New Point(465, 46) : btnExport.Size = New Size(105, 30)
        StyleBtn(btnExport)
        btnExport.Enabled = False
        btnPrint.Enabled = False

        btnPrint.Text = "Print" : btnPrint.Location = New Point(580, 46) : btnPrint.Size = New Size(80, 30)
        StyleBtn(btnPrint)
        btnPrint.Enabled = False

        btnClose.Text = "Close" : btnClose.Location = New Point(670, 46) : btnClose.Size = New Size(80, 30)
        StyleBtn(btnClose)

        Dim lblView As New Label() With {.Text = "View:", .ForeColor = Color.White, .BackColor = Color.Black, .AutoSize = True, .Location = New Point(12, 82)}
        StyleRadio(rbDetail, "Detail", New Point(60, 78), True)
        StyleRadio(rbCustomer, "By Customer", New Point(140, 78), False)
        StyleRadio(rbCustDetail, "Customer Detail", New Point(260, 78), False)
        StyleRadio(rbYear, "By Year", New Point(400, 78), False)

        lblStatus.Text = "Select date range and click Load."
        lblStatus.ForeColor = Color.Cyan : lblStatus.BackColor = Color.Black
        lblStatus.AutoSize = True : lblStatus.Location = New Point(500, 84)

        ConfigureGrid()
        grid.Location = New Point(12, 110)
        grid.Size = New Size(Me.ClientSize.Width - 24, Me.ClientSize.Height - 122)
        grid.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        SetDetailColumns()

        Me.Controls.AddRange({lblTitle, lblFrom, dtpFrom, lblTo, dtpTo,
                               btnLoad, btnExport, btnPrint, btnClose,
                               lblView, rbDetail, rbCustomer, rbCustDetail, rbYear,
                               lblStatus, grid})
    End Sub

    Private Shared Sub StyleBtn(btn As Button)
        btn.UseVisualStyleBackColor = False
        btn.BackColor = Color.LightGray
        btn.ForeColor = Color.Black
        btn.FlatStyle = FlatStyle.Flat
        btn.Font = New Font("Consolas", 10, FontStyle.Bold)
        btn.TextAlign = ContentAlignment.MiddleCenter
    End Sub

    Private Sub StyleRadio(rb As RadioButton, txt As String, loc As Point, checked As Boolean)
        rb.Text = txt : rb.Location = loc : rb.AutoSize = True
        rb.ForeColor = Color.White : rb.BackColor = Color.Black : rb.Checked = checked
    End Sub

    Private Sub ConfigureGrid()
        grid.BackgroundColor = Color.Black : grid.ForeColor = Color.White : grid.GridColor = Color.DimGray
        grid.DefaultCellStyle.BackColor = Color.Black : grid.DefaultCellStyle.ForeColor = Color.White
        grid.DefaultCellStyle.SelectionBackColor = Color.DarkSlateGray : grid.DefaultCellStyle.SelectionForeColor = Color.White
        grid.DefaultCellStyle.Font = New Font("Courier New", 9)
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40)
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Yellow
        grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        grid.EnableHeadersVisualStyles = False
        grid.ReadOnly = True : grid.AllowUserToAddRows = False : grid.AllowUserToDeleteRows = False
        grid.AllowUserToResizeRows = False : grid.RowHeadersVisible = False
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        grid.ScrollBars = ScrollBars.Both
        AddHandler grid.CellPainting, AddressOf Grid_CellPainting
    End Sub

    Private Sub Grid_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 Then Return
        Dim row = grid.Rows(e.RowIndex)
        If row.Tag IsNot Nothing AndAlso row.Tag.ToString() = "yearband" Then
            e.Paint(e.CellBounds, DataGridViewPaintParts.All)
            Using pen As New Pen(Color.FromArgb(0, 180, 0), 2)
                Dim r = e.CellBounds
                ' top border on first painted cell, bottom always, left on first col, right on last col
                If e.ColumnIndex = 0 Then
                    e.Graphics.DrawLine(pen, r.Left, r.Top, r.Left, r.Bottom - 1)
                End If
                If e.ColumnIndex = grid.Columns.Count - 1 Then
                    e.Graphics.DrawLine(pen, r.Right - 1, r.Top, r.Right - 1, r.Bottom - 1)
                End If
                e.Graphics.DrawLine(pen, r.Left, r.Top, r.Right, r.Top)
                e.Graphics.DrawLine(pen, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1)
            End Using
            e.Handled = True
        End If
    End Sub

    Private Sub SetDetailColumns()
        grid.Columns.Clear()
        grid.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colInvNum",   .HeaderText = "Invoice #",   .Width = 85})
        grid.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colDate",     .HeaderText = "Date",        .Width = 100})
        grid.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colCustomer", .HeaderText = "Customer",    .Width = 230})
        grid.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colPO",       .HeaderText = "PO #",        .Width = 110})
        grid.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colDesc",     .HeaderText = "Description", .Width = 220})
        grid.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colAmount",   .HeaderText = "Amount",      .Width = 110,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
    End Sub

    Private Sub SetGroupColumns(groupHeader As String)
        grid.Columns.Clear()
        grid.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colGroup", .HeaderText = groupHeader, .Width = 420})
        grid.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colCount", .HeaderText = "# Invoices", .Width = 100,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
        grid.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "colTotal", .HeaderText = "Total Amount", .Width = 150,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
    End Sub

    Private Sub btnLoad_Click(sender As Object, e As EventArgs) Handles btnLoad.Click
        Cursor = Cursors.WaitCursor
        Try
            lblStatus.Text = "Loading JOURNAL.CUR ..." : Application.DoEvents()
            Dim allRecords = JournalCurReader.ReadAllRecords(LegacyDataPaths.JournalCur)
            Dim fromDate = dtpFrom.Value.Date : Dim toDate = dtpTo.Value.Date
            _filtered = allRecords.
                Select(Function(r) New JournalEntryEx With {.Entry = r, .ParsedDate = JournalCurReader.ParseDosDate(r.DateString)}).
                Where(Function(x) x.ParsedDate >= fromDate AndAlso x.ParsedDate <= toDate).
                OrderBy(Function(x) x.ParsedDate).ThenBy(Function(x) x.Entry.InvoiceNumber).ToList()
            RefreshView()
            btnExport.Enabled = _filtered.Count > 0
            btnPrint.Enabled = _filtered.Count > 0
        Catch ex As Exception
            MessageBox.Show("Error loading: " & ex.Message, "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub RefreshView()
        If _filtered Is Nothing Then Return
        If rbCustomer.Checked Then
            ShowByCustomer()
        ElseIf rbCustDetail.Checked Then
            ShowCustomerDetail()
        ElseIf rbYear.Checked Then
            ShowByYear()
        Else
            ShowDetail()
        End If
    End Sub

    Private Sub ShowDetail()
        SetDetailColumns() : grid.Rows.Clear()
        Dim total As Decimal = 0D
        For Each x In _filtered
            Dim r = x.Entry
            grid.Rows.Add(r.InvoiceNumber.ToString(), r.DateString, r.CustomerName, r.PoNumber, r.ProcedureName, r.Amount.ToString("N2"))
            total += r.Amount
        Next
        AddTotalRow(6, "GRAND TOTAL", "", "", "", _filtered.Count & " invoices", total)
        lblStatus.Text = $"{_filtered.Count} invoices  |  Grand Total: {total:C}  |  {dtpFrom.Value:MM/dd/yyyy} – {dtpTo.Value:MM/dd/yyyy}"
    End Sub

    Private Sub ShowByCustomer()
        SetGroupColumns("Customer")
        grid.Rows.Clear()
        Dim groups = _filtered.GroupBy(Function(x) x.Entry.CustomerName.Trim()).OrderBy(Function(g) g.Key).ToList()
        Dim grandTotal As Decimal = 0D
        Dim grandCount As Integer = 0
        For Each g In groups
            Dim amt = g.Sum(Function(x) x.Entry.Amount)
            Dim cnt = g.Count()
            grid.Rows.Add(g.Key, cnt.ToString("N0"), amt.ToString("N2"))
            grandTotal += amt
            grandCount += cnt
        Next
        AddTotalRow(3, "GRAND TOTAL", grandCount.ToString("N0"), grandTotal)
        lblStatus.Text = $"{groups.Count} customers  |  {grandCount} invoices  |  Grand Total: {grandTotal:C}"
    End Sub

    Private Sub ShowCustomerDetail()
        SetDetailColumns()
        grid.Rows.Clear()
        Dim custGroups = _filtered.GroupBy(Function(x) x.Entry.CustomerName.Trim()).OrderBy(Function(g) g.Key).ToList()
        Dim grandTotal As Decimal = 0D
        Dim grandCount As Integer = 0

        For Each cg In custGroups
            ' ── Customer header ──────────────────────────────────────
            AddBandRow(cg.Key, 0, Color.FromArgb(20, 60, 20), Color.LightGreen)

            Dim custTotal As Decimal = 0D
            Dim custCount As Integer = 0

            ' ── Year groups within this customer ─────────────────────
            Dim yearGroups = cg.GroupBy(Function(x) x.ParsedDate.Year).OrderBy(Function(g) g.Key).ToList()
            For Each yg In yearGroups
                ' Year sub-header: grey background, year in PO column, bigger font
                Dim yhIdx = grid.Rows.Add()
                Dim yhRow = grid.Rows(yhIdx)
                yhRow.Tag = "yearband"
                yhRow.Cells(3).Value = yg.Key.ToString()
                For Each c As DataGridViewCell In yhRow.Cells
                    c.Style.BackColor = Color.FromArgb(175, 175, 175)
                    c.Style.ForeColor = Color.FromArgb(0, 100, 0)
                    c.Style.Font = New Font("Segoe UI", 11, FontStyle.Bold)
                Next

                Dim yearTotal As Decimal = 0D
                For Each x In yg.OrderBy(Function(e) e.ParsedDate)
                    Dim r = x.Entry
                    grid.Rows.Add(r.InvoiceNumber.ToString(), r.DateString, r.CustomerName, r.PoNumber, r.ProcedureName, r.Amount.ToString("N2"))
                    yearTotal += r.Amount
                    custTotal += r.Amount
                    grandTotal += r.Amount
                    custCount += 1
                    grandCount += 1
                Next
                ' Year subtotal
                AddSubtotalRow("    " & yg.Key.ToString() & " Subtotal", yg.Count() & " invoices", yearTotal, Color.FromArgb(15, 45, 45), Color.Cyan)
            Next

            ' ── Customer total ────────────────────────────────────────
            AddSubtotalRow(cg.Key & "  —  Total", custCount & " invoices", custTotal, Color.FromArgb(40, 40, 40), Color.Yellow)
        Next

        AddTotalRow(6, "GRAND TOTAL", "", "", "", grandCount & " invoices", grandTotal)
        lblStatus.Text = $"{custGroups.Count} customers  |  {grandCount} invoices  |  Grand Total: {grandTotal:C}"
    End Sub

    Private Sub AddBandRow(label As String, colIdx As Integer, bg As Color, fg As Color)
        Dim idx = grid.Rows.Add()
        Dim row = grid.Rows(idx)
        row.Cells(colIdx).Value = label
        For Each c As DataGridViewCell In row.Cells
            c.Style.BackColor = bg
            c.Style.ForeColor = fg
            c.Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        Next
    End Sub

    Private Sub AddSubtotalRow(label As String, countTxt As String, total As Decimal, bg As Color, fg As Color)
        Dim idx = grid.Rows.Add()
        Dim row = grid.Rows(idx)
        row.Cells(0).Value = label
        row.Cells(4).Value = countTxt
        row.Cells(5).Value = total.ToString("N2")
        row.Cells(5).Style.Alignment = DataGridViewContentAlignment.MiddleRight
        For Each c As DataGridViewCell In row.Cells
            c.Style.BackColor = bg
            c.Style.ForeColor = fg
            c.Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        Next
    End Sub

    Private Sub ShowByYear()
        SetGroupColumns("Year") : grid.Rows.Clear()
        Dim groups = _filtered.GroupBy(Function(x) x.ParsedDate.Year).OrderBy(Function(g) g.Key).ToList()
        Dim grandTotal As Decimal = 0D : Dim grandCount As Integer = 0
        For Each g In groups
            Dim amt = g.Sum(Function(x) x.Entry.Amount) : Dim cnt = g.Count()
            grid.Rows.Add(g.Key.ToString(), cnt.ToString("N0"), amt.ToString("N2"))
            grandTotal += amt : grandCount += cnt
        Next
        AddTotalRow(3, "GRAND TOTAL", grandCount.ToString("N0"), grandTotal)
        lblStatus.Text = $"{groups.Count} years  |  {grandCount} invoices  |  Grand Total: {grandTotal:C}"
    End Sub

    ' Overloads for detail (6 cols) and group (3 cols)
    Private Sub AddTotalRow(colCount As Integer, label As String, col1 As String, col2 As String, col3 As String, col4 As String, total As Decimal)
        Dim idx = grid.Rows.Add()
        Dim row = grid.Rows(idx)
        row.Cells(0).Value = label
        row.Cells(4).Value = col4
        row.Cells(5).Value = total.ToString("N2")
        row.Cells(5).Style.Alignment = DataGridViewContentAlignment.MiddleRight
        StyleTotalRow(row)
    End Sub

    Private Sub AddTotalRow(colCount As Integer, label As String, countText As String, total As Decimal)
        Dim idx = grid.Rows.Add()
        Dim row = grid.Rows(idx)
        row.Cells(0).Value = label
        row.Cells(1).Value = countText
        row.Cells(2).Value = total.ToString("N2")
        row.Cells(2).Style.Alignment = DataGridViewContentAlignment.MiddleRight
        StyleTotalRow(row)
    End Sub

    Private Sub StyleTotalRow(row As DataGridViewRow)
        For Each c As DataGridViewCell In row.Cells
            c.Style.BackColor = Color.FromArgb(40, 40, 40)
            c.Style.ForeColor = Color.Yellow
            c.Style.Font = New Font("Segoe UI", 9, FontStyle.Bold)
        Next
    End Sub

    Private Sub rbDetail_CheckedChanged(s As Object, e As EventArgs) Handles rbDetail.CheckedChanged
        If rbDetail.Checked Then RefreshView()
    End Sub
    Private Sub rbCustomer_CheckedChanged(s As Object, e As EventArgs) Handles rbCustomer.CheckedChanged
        If rbCustomer.Checked Then RefreshView()
    End Sub
    Private Sub rbCustDetail_CheckedChanged(s As Object, e As EventArgs) Handles rbCustDetail.CheckedChanged
        If rbCustDetail.Checked Then RefreshView()
    End Sub
    Private Sub rbYear_CheckedChanged(s As Object, e As EventArgs) Handles rbYear.CheckedChanged
        If rbYear.Checked Then RefreshView()
    End Sub

    Private Sub btnExport_Click(sender As Object, e As EventArgs) Handles btnExport.Click
        Dim viewName As String = If(rbCustomer.Checked, "ByCustomer", If(rbCustDetail.Checked, "CustomerDetail", If(rbYear.Checked, "ByYear", "Detail")))
        Using sfd As New SaveFileDialog()
            sfd.Title = "Export Invoice Register"
            sfd.Filter = "CSV Files (*.csv)|*.csv"
            sfd.FileName = $"InvoiceRegister_{viewName}_{dtpFrom.Value:yyyyMMdd}_{dtpTo.Value:yyyyMMdd}.csv"
            If sfd.ShowDialog() <> DialogResult.OK Then Return
            Try
                Using sw As New StreamWriter(sfd.FileName, False, System.Text.Encoding.UTF8)
                    sw.WriteLine(String.Join(",", grid.Columns.Cast(Of DataGridViewColumn)().Select(Function(c) c.HeaderText)))
                    For Each row As DataGridViewRow In grid.Rows
                        sw.WriteLine(String.Join(",", row.Cells.Cast(Of DataGridViewCell)().Select(Function(c) CsvQuote(If(c.Value?.ToString(), "")))))
                    Next
                End Using
                MessageBox.Show("Export complete." & vbCrLf & sfd.FileName, "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show("Export error: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub btnPrint_Click(sender As Object, e As EventArgs) Handles btnPrint.Click
        Dim doc As New PrintDocument()
        doc.DocumentName = "Invoice Register"
        _printRowIndex = 0
        AddHandler doc.PrintPage, AddressOf PrintDoc_PrintPage
        Using ppd As New PrintPreviewDialog()
            ppd.Document = doc
            ppd.WindowState = FormWindowState.Maximized
            ppd.ShowDialog(Me)
        End Using
    End Sub

    Private Sub PrintDoc_PrintPage(sender As Object, e As PrintPageEventArgs)
        Dim g = e.Graphics
        Dim colWidths() As Integer = {70, 80, 200, 110, 180, 80}
        Dim pageWidth = e.MarginBounds.Width
        Dim x0 = e.MarginBounds.Left
        Dim y = e.MarginBounds.Top
        Dim lineH = 16
        Dim titleFont As New Font("Segoe UI", 12, FontStyle.Bold)
        Dim hdrFont  As New Font("Segoe UI", 8, FontStyle.Bold)
        Dim rowFont  As New Font("Courier New", 8)
        Dim bandFont As New Font("Segoe UI", 9, FontStyle.Bold)

        If _printRowIndex = 0 Then
            g.DrawString("INVOICE REGISTER", titleFont, Brushes.Black, x0, y)
            y += 22
            Dim x = x0
            For i = 0 To grid.Columns.Count - 1
                g.DrawString(grid.Columns(i).HeaderText, hdrFont, Brushes.Black, x, y)
                x += colWidths(i)
            Next
            y += lineH
            g.DrawLine(Pens.Black, x0, y, x0 + colWidths.Sum(), y)
            y += 2
        End If

        Do While _printRowIndex < grid.Rows.Count
            If y + lineH > e.MarginBounds.Bottom Then
                e.HasMorePages = True
                Exit Do
            End If
            Dim row = grid.Rows(_printRowIndex)
            Dim isband = row.Tag IsNot Nothing AndAlso row.Tag.ToString() = "yearband"
            Dim isSub = row.Cells(0).Style.ForeColor = Color.Cyan OrElse row.DefaultCellStyle.ForeColor = Color.Cyan
            Dim fnt = If(isband, bandFont, rowFont)
            Dim brush As Brush = Brushes.Black
            If isband Then
                g.FillRectangle(Brushes.LightGray, x0, y, colWidths.Sum(), lineH)
                brush = New SolidBrush(Color.FromArgb(0, 100, 0))
            End If
            Dim cx = x0
            For i = 0 To grid.Columns.Count - 1
                Dim val = If(row.Cells(i).Value?.ToString(), "")
                g.DrawString(val, fnt, brush, cx, y)
                cx += colWidths(i)
            Next
            y += lineH
            _printRowIndex += 1
        Loop
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Close()
    End Sub

    Private Shared Function CsvQuote(s As String) As String
        If s.Contains(",") OrElse s.Contains("""") OrElse s.Contains(vbLf) Then Return """" & s.Replace("""", """""") & """"
        Return s
    End Function

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then Me.Close()
        MyBase.OnKeyDown(e)
    End Sub
End Class
