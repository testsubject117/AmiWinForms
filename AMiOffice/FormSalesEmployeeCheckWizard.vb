Option Strict Off
Option Explicit On

Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class FormSalesEmployeeCheckWizard
    Inherits Form

    Private employees As List(Of EmpEntry)
    Private idx As Integer = 0
    Private selectedEmployee As EmpEntry = Nothing

    Private lowInv As Long
    Private highInv As Long

    Private scanCancelled As Boolean = False
    Private scanRows As List(Of InvoiceDetailRow) = Nothing
    Private scanTotal As Decimal = 0D
    Private scanCommissionRate As Decimal = 0D
    Private scanCommission As Decimal = 0D

    Private currentStep As Integer = 1

    ' Host panels for steps
    Private ReadOnly pnlStep1 As New Panel()
    Private ReadOnly pnlStep2 As New Panel()
    Private ReadOnly pnlStep3 As New Panel()
    Private ReadOnly pnlStep4 As New Panel()

    ' Step 1 controls
    Private ReadOnly lblPrompt As New Label()
    Private ReadOnly lstCustomers As New ListBox()
    Private ReadOnly btnYes As New Button()
    Private ReadOnly btnNo As New Button()
    Private ReadOnly btnCancel1 As New Button()

    ' Step 2 controls
    Private ReadOnly lblLow As New Label()
    Private ReadOnly txtLow As New TextBox()
    Private ReadOnly lblHigh As New Label()
    Private ReadOnly txtHigh As New TextBox()
    Private ReadOnly btnBack2 As New Button()
    Private ReadOnly btnNext2 As New Button()
    Private ReadOnly btnCancel2 As New Button()

    ' Step 3 controls
    Private ReadOnly lblProgress As New Label()
    Private ReadOnly progress As New ProgressBar()
    Private ReadOnly btnCancel3 As New Button()

    ' Step 4 controls
    Private ReadOnly grid As New DataGridView()
    Private ReadOnly txtReport As New TextBox()
    Private ReadOnly lblTotals As New Label()
    Private ReadOnly btnFinish4 As New Button()

    Private Class EmpEntry
        Public Property Name As String
        Public Property Customers As List(Of String)
    End Class

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = "Sales Employee's checks"
        StartPosition = FormStartPosition.CenterParent
        Width = 1000
        Height = 720

        BuildUi()

        Dim path As String = LegacyDataPaths.EmpNameDat
        Dim usedFallback As Boolean = False
        employees = LoadEmployees(path, usedFallback)

        If employees Is Nothing OrElse employees.Count = 0 Then
            MessageBox.Show(Me,
                            "No employees found in EMPNAME.DAT." & vbCrLf &
                            "Path: " & path,
                            Text,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)
            DialogResult = DialogResult.Cancel
            Close()
            Return
        End If

        If usedFallback Then
            MessageBox.Show(Me,
                            "NOTE: EMPNAME.DAT did not contain any non-indented employee header lines." & vbCrLf &
                            "Using compatibility mode: first non-empty line treated as employee name." & vbCrLf &
                            "Path: " & path,
                            Text,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information)
        End If

        idx = 0
        currentStep = 1
        ShowStep(1)
        ShowEmployee()
    End Sub

    Private Sub BuildUi()
        pnlStep1.Dock = DockStyle.Fill
        pnlStep2.Dock = DockStyle.Fill
        pnlStep3.Dock = DockStyle.Fill
        pnlStep4.Dock = DockStyle.Fill

        Controls.Add(pnlStep4)
        Controls.Add(pnlStep3)
        Controls.Add(pnlStep2)
        Controls.Add(pnlStep1)

        BuildStep1Ui()
        BuildStep2Ui()
        BuildStep3Ui()
        BuildStep4Ui()
    End Sub

    Private Sub ShowStep(n As Integer)
        pnlStep1.Visible = (n = 1)
        pnlStep2.Visible = (n = 2)
        pnlStep3.Visible = (n = 3)
        pnlStep4.Visible = (n = 4)
        currentStep = n
    End Sub

    ' ---------------------------
    ' Step 1 (employee yes/no)
    ' ---------------------------
    Private Sub BuildStep1Ui()
        lblPrompt.AutoSize = False
        lblPrompt.Dock = DockStyle.Top
        lblPrompt.Height = 90
        lblPrompt.Font = New Drawing.Font("Segoe UI", 14, Drawing.FontStyle.Bold)
        lblPrompt.Padding = New Padding(16)

        lstCustomers.Dock = DockStyle.Fill
        lstCustomers.Font = New Drawing.Font("Consolas", 11)

        Dim bottom As New FlowLayoutPanel() With {
            .Dock = DockStyle.Bottom,
            .Height = 64,
            .Padding = New Padding(12),
            .FlowDirection = FlowDirection.RightToLeft,
            .WrapContents = False
        }

        btnYes.Text = "Yes"
        btnYes.Width = 120
        AddHandler btnYes.Click, AddressOf btnYes_Click

        btnNo.Text = "No"
        btnNo.Width = 120
        AddHandler btnNo.Click, AddressOf btnNo_Click

        btnCancel1.Text = "Cancel"
        btnCancel1.Width = 120
        AddHandler btnCancel1.Click, Sub() Close()

        bottom.Controls.Add(btnCancel1)
        bottom.Controls.Add(btnNo)
        bottom.Controls.Add(btnYes)

        pnlStep1.Controls.Add(lstCustomers)
        pnlStep1.Controls.Add(bottom)
        pnlStep1.Controls.Add(lblPrompt)
    End Sub

    Private Sub ShowEmployee()
        If idx < 0 Then idx = 0

        If idx >= employees.Count Then
            MessageBox.Show(Me, "No employee selected.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
            DialogResult = DialogResult.Cancel
            Close()
            Return
        End If

        Dim emp = employees(idx)
        lblPrompt.Text = "Do you want to make a check for " & emp.Name & " (Y/N) ?"

        lstCustomers.Items.Clear()
        If emp.Customers IsNot Nothing Then
            For Each c In emp.Customers
                lstCustomers.Items.Add(c)
            Next
        End If
    End Sub

    Private Sub btnYes_Click(sender As Object, e As EventArgs)
        selectedEmployee = employees(idx)

        txtLow.Text = "150000"
        txtHigh.Text = "150000"

        ShowStep(2)
        txtLow.Focus()
        txtLow.SelectAll()
    End Sub

    Private Sub btnNo_Click(sender As Object, e As EventArgs)
        idx += 1
        ShowEmployee()
    End Sub

    ' ---------------------------
    ' Step 2 (invoice range)
    ' ---------------------------
    Private Sub BuildStep2Ui()
        Dim top As New Panel() With {.Dock = DockStyle.Top, .Height = 180, .Padding = New Padding(16)}

        lblLow.AutoSize = True
        lblLow.Text = "Enter the LOWEST Invoice number to be paid [-1 = Quit]"
        lblLow.Top = 10
        lblLow.Left = 10

        txtLow.Width = 200
        txtLow.Top = 40
        txtLow.Left = 10

        lblHigh.AutoSize = True
        lblHigh.Text = "Enter the HIGHEST Invoice number to be paid [-1 = Quit]"
        lblHigh.Top = 85
        lblHigh.Left = 10

        txtHigh.Width = 200
        txtHigh.Top = 115
        txtHigh.Left = 10

        top.Controls.Add(lblLow)
        top.Controls.Add(txtLow)
        top.Controls.Add(lblHigh)
        top.Controls.Add(txtHigh)

        Dim bottom As New FlowLayoutPanel() With {
            .Dock = DockStyle.Bottom,
            .Height = 64,
            .Padding = New Padding(12),
            .FlowDirection = FlowDirection.RightToLeft,
            .WrapContents = False
        }

        btnNext2.Text = "Next"
        btnNext2.Width = 120
        AddHandler btnNext2.Click, AddressOf btnNext2_Click

        btnBack2.Text = "Back"
        btnBack2.Width = 120
        AddHandler btnBack2.Click, Sub()
                                       ShowStep(1)
                                       ShowEmployee()
                                   End Sub

        btnCancel2.Text = "Cancel"
        btnCancel2.Width = 120
        AddHandler btnCancel2.Click, Sub() Close()

        bottom.Controls.Add(btnCancel2)
        bottom.Controls.Add(btnNext2)
        bottom.Controls.Add(btnBack2)

        pnlStep2.Controls.Add(bottom)
        pnlStep2.Controls.Add(top)
    End Sub

    Private Sub btnNext2_Click(sender As Object, e As EventArgs)
        Dim lo As Long
        Dim hi As Long

        If Not Long.TryParse(txtLow.Text.Trim(), lo) Then
            MessageBox.Show(Me, "Lowest invoice must be a number.", Text)
            txtLow.Focus()
            txtLow.SelectAll()
            Return
        End If

        If lo = -1 Then
            Close()
            Return
        End If

        If lo < 150000 Then
            MessageBox.Show(Me, "Lowest invoice must be >= 150000.", Text)
            txtLow.Focus()
            txtLow.SelectAll()
            Return
        End If

        If Not Long.TryParse(txtHigh.Text.Trim(), hi) Then
            MessageBox.Show(Me, "Highest invoice must be a number.", Text)
            txtHigh.Focus()
            txtHigh.SelectAll()
            Return
        End If

        If hi = -1 Then
            Close()
            Return
        End If

        If hi < 150000 Then
            MessageBox.Show(Me, "Highest invoice must be >= 150000.", Text)
            txtHigh.Focus()
            txtHigh.SelectAll()
            Return
        End If

        If hi < lo Then
            MessageBox.Show(Me, "Highest invoice must be >= lowest invoice.", Text)
            txtHigh.Focus()
            txtHigh.SelectAll()
            Return
        End If

        lowInv = lo
        highInv = hi

        ShowStep(3)
        StartScanAsync()
    End Sub

    ' ---------------------------
    ' Step 3 (processing)
    ' ---------------------------
    Private Sub BuildStep3Ui()
        Dim top As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

        lblProgress.AutoSize = True
        lblProgress.Text = "Scanning invoices..."
        lblProgress.Top = 20
        lblProgress.Left = 20

        progress.Top = 60
        progress.Left = 20
        progress.Width = 900
        progress.Style = ProgressBarStyle.Continuous

        btnCancel3.Text = "Cancel"
        btnCancel3.Width = 120
        btnCancel3.Top = 110
        btnCancel3.Left = 20
        AddHandler btnCancel3.Click, Sub() scanCancelled = True

        top.Controls.Add(lblProgress)
        top.Controls.Add(progress)
        top.Controls.Add(btnCancel3)

        pnlStep3.Controls.Add(top)
    End Sub

    Private Async Sub StartScanAsync()
        scanCancelled = False
        lblProgress.Text = "Scanning invoices..."
        progress.Value = 0

        Dim invoiceChkPath As String = LegacyDataPaths.InvoiceChk
        Dim emp As EmpEntry = selectedEmployee
        Dim lo As Long = lowInv
        Dim hi As Long = highInv

        If emp Is Nothing Then
            MessageBox.Show(Me, "No employee selected.", Text)
            ShowStep(1)
            Return
        End If

        If Not File.Exists(invoiceChkPath) Then
            MessageBox.Show(Me,
                            "Missing required file:" & vbCrLf & invoiceChkPath,
                            Text,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)
            ShowStep(2)
            Return
        End If

        Dim totalCount As Long = (hi - lo + 1)
        If totalCount < 1 Then totalCount = 1
        progress.Maximum = CInt(Math.Min(Integer.MaxValue, totalCount))

        Dim result As ScanResult = Await Task.Run(Function() ScanInvoices(emp, invoiceChkPath, lo, hi))

        If scanCancelled Then
            MessageBox.Show(Me, "Cancelled.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
            ShowStep(2)
            Return
        End If

        scanRows = result.Rows
        scanTotal = result.Total
        scanCommissionRate = result.CommissionRate
        scanCommission = result.Commission

        BindResults()
        ShowStep(4)
    End Sub

    Private Class ScanResult
        Public Property Rows As List(Of InvoiceDetailRow)
        Public Property Total As Decimal
        Public Property CommissionRate As Decimal
        Public Property Commission As Decimal
    End Class

    Private Function ScanInvoices(emp As EmpEntry, invoiceChkPath As String, lo As Long, hi As Long) As ScanResult
        Dim rows As New List(Of InvoiceDetailRow)()
        Dim total As Decimal = 0D

        Dim customers As New List(Of String)()
        If emp.Customers IsNot Nothing Then
            For Each c In emp.Customers
                Dim t = (If(c, "")).Trim()
                If t <> "" Then customers.Add(t)
            Next
        End If

        For inv As Long = lo To hi
            If scanCancelled Then Exit For

            If (inv - lo) Mod 50 = 0 Then
                Dim current As Long = inv
                BeginInvoke(Sub()
                                lblProgress.Text = "Scanning invoice " & current & "..."
                                Dim val As Long = current - lo + 1
                                If val >= 0 AndAlso val <= progress.Maximum Then
                                    progress.Value = CInt(val)
                                End If
                            End Sub)
            End If

            Dim rec As InvoiceChkRecord = InvoiceChkReader.ReadRecord(invoiceChkPath, inv)
            If Not rec.IsFound Then Continue For
            If rec.Amount < 0.1D Then Continue For

            Dim co As String = If(rec.CompanyCode, "").Trim()
            If co = "" Then Continue For

            Dim matches As Boolean = False
            For Each cust As String In customers
                If cust.Length > 0 AndAlso co.StartsWith(cust, StringComparison.OrdinalIgnoreCase) Then
                    matches = True
                    Exit For
                End If
            Next
            If Not matches Then Continue For

            Dim status As String = "paid"
            If String.Equals(rec.Flag, "J", StringComparison.OrdinalIgnoreCase) Then status = "UNpaid"

            rows.Add(New InvoiceDetailRow With {
                .InvoiceNumber = inv,
                .CompanyCode = co,
                .Amount = rec.Amount.ToString("0.00"),
                .Status = status
            })

            total += rec.Amount
        Next

        Dim nameUpper As String = ""
        If emp IsNot Nothing AndAlso emp.Name IsNot Nothing Then nameUpper = emp.Name.ToUpperInvariant()
        Dim rate As Decimal = If(nameUpper.StartsWith("STEV"), 0.1D, 0.15D)
        Dim comm As Decimal = Decimal.Round(total * rate, 2, MidpointRounding.AwayFromZero)

        Return New ScanResult With {
            .Rows = rows,
            .Total = Decimal.Round(total, 2, MidpointRounding.AwayFromZero),
            .CommissionRate = rate,
            .Commission = comm
        }
    End Function

    ' ---------------------------
    ' Step 4 (results)
    ' ---------------------------
    Private Sub BuildStep4Ui()
        Dim split As New SplitContainer() With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal}
        split.SplitterDistance = 360

        grid.Dock = DockStyle.Fill
        grid.ReadOnly = True
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.AutoGenerateColumns = True

        txtReport.Dock = DockStyle.Fill
        txtReport.Multiline = True
        txtReport.ScrollBars = ScrollBars.Both
        txtReport.Font = New Drawing.Font("Consolas", 10)
        txtReport.ReadOnly = True
        txtReport.WordWrap = False

        split.Panel1.Controls.Add(grid)
        split.Panel2.Controls.Add(txtReport)

        Dim bottom As New Panel() With {.Dock = DockStyle.Bottom, .Height = 64, .Padding = New Padding(12)}
        lblTotals.AutoSize = True
        lblTotals.Left = 12
        lblTotals.Top = 18

        btnFinish4.Text = "Finish"
        btnFinish4.Width = 120
        btnFinish4.Anchor = AnchorStyles.Right Or AnchorStyles.Top
        btnFinish4.Top = 12
        AddHandler btnFinish4.Click, Sub() Close()
        AddHandler bottom.Resize, Sub()
                                      btnFinish4.Left = bottom.ClientSize.Width - btnFinish4.Width - 12
                                  End Sub

        bottom.Controls.Add(lblTotals)
        bottom.Controls.Add(btnFinish4)

        pnlStep4.Controls.Add(split)
        pnlStep4.Controls.Add(bottom)
    End Sub

    Private Sub BindResults()
        Dim rows As List(Of InvoiceDetailRow) = If(scanRows, New List(Of InvoiceDetailRow)())
        grid.DataSource = rows

        Dim ratePct As String = (scanCommissionRate * 100D).ToString("0.##") & "%"
        lblTotals.Text = "Total $" & scanTotal.ToString("0.00") & "    " & ratePct & " = $" & scanCommission.ToString("0.00")

        txtReport.Text = BuildDosReportText()
    End Sub

    Private Function BuildDosReportText() As String
        Dim empName As String = ""
        If selectedEmployee IsNot Nothing AndAlso selectedEmployee.Name IsNot Nothing Then
            empName = selectedEmployee.Name
        End If

        Dim ratePct As String = (scanCommissionRate * 100D).ToString("0.##")

        Dim sw As New StringWriter()
        sw.WriteLine(empName & "  Sales from Invoice #" & lowInv & "to" & highInv)
        sw.WriteLine()

        If scanRows IsNot Nothing Then
            For Each r As InvoiceDetailRow In scanRows
                sw.WriteLine(r.InvoiceNumber.ToString().PadRight(10) &
                             " " & (If(r.CompanyCode, "")).PadRight(10) &
                             " " & (If(r.Amount, "")).PadLeft(10) &
                             " " & (If(r.Status, "")))
            Next
        End If

        sw.WriteLine()
        sw.WriteLine(DateTime.Now.ToString("MM/dd/yyyy") &
                     " Total $" & scanTotal.ToString("0.00") &
                     "   ." & ratePct & "% = $" & scanCommission.ToString("0.00"))
        sw.WriteLine("====================================")

        Return sw.ToString()
    End Function

    ' ---------------------------
    ' EMPNAME.DAT parsing (strict + fallback)
    ' ---------------------------
    Private Function LoadEmployees(path As String, ByRef usedFallback As Boolean) As List(Of EmpEntry)
        usedFallback = False

        If Not File.Exists(path) Then
            MessageBox.Show(Me,
                            "Missing required file:" & vbCrLf & path,
                            Text,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning)
            Return New List(Of EmpEntry)()
        End If

        Dim lines() As String
        Try
            lines = File.ReadAllLines(path)
        Catch ex As Exception
            MessageBox.Show(Me,
                            "Could not read file:" & vbCrLf & path & vbCrLf & vbCrLf & ex.Message,
                            Text,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
            Return New List(Of EmpEntry)()
        End Try

        ' Strict DOS parse: employee header lines are NOT indented.
        Dim strictResult As New List(Of EmpEntry)()
        Dim current As EmpEntry = Nothing

        For Each rawLine As String In lines
            Dim line As String = If(rawLine, "")
            If line.Trim() = "" Then Continue For

            If line.StartsWith(" "c) OrElse line.StartsWith(ControlChars.Tab) Then
                If current IsNot Nothing Then
                    Dim cust As String = line.Trim()
                    If cust <> "" Then current.Customers.Add(cust)
                End If
            Else
                current = New EmpEntry() With {.Name = line.Trim(), .Customers = New List(Of String)()}
                strictResult.Add(current)
            End If
        Next

        If strictResult.Count > 0 Then Return strictResult

        ' Fallback: file is readable but every line is indented.
        ' Treat first non-empty trimmed line as employee name, rest are customers.
        Dim nonEmpty As New List(Of String)()
        For Each rawLine As String In lines
            Dim t As String = If(rawLine, "").Trim()
            If t <> "" Then nonEmpty.Add(t)
        Next

        If nonEmpty.Count = 0 Then Return New List(Of EmpEntry)()

        usedFallback = True
        Dim fallbackResult As New List(Of EmpEntry)()
        Dim emp As New EmpEntry() With {.Name = nonEmpty(0), .Customers = New List(Of String)()}
        For i As Integer = 1 To nonEmpty.Count - 1
            emp.Customers.Add(nonEmpty(i))
        Next
        fallbackResult.Add(emp)
        Return fallbackResult
    End Function
End Class