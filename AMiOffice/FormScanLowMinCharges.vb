Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' Option P - Scan price lists for errors and min. charge's that are too low
''' DOS source: PLIST.ASC lines 4500-4670
''' Cross-references customer price lists against WORD\PROCDURE.DOC expected min charges
''' </summary>
Public Class FormScanLowMinCharges
    Inherits Form

    Private _txtResults As TextBox
    Private _btnScan As Button
    Private _btnSaveToFile As Button
    Private _btnPrint As Button
    Private _btnClose As Button
    Private _lblStatus As Label
    Private _progressBar As ProgressBar
    Private _standardProcedures As New Dictionary(Of String, Decimal)() ' procedure -> expected min charge
    Private _scanResults As New StringBuilder()
    Private _cancelRequested As Boolean = False

    Public Sub New()
        InitializeComponent()
        LoadStandardProcedures()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Scan for Low Min Charges"
        Me.Size = New Size(1000, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.Black
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MinimumSize = New Size(800, 600)

        ' Status label
        _lblStatus = New Label()
        _lblStatus.Text = "Ready to scan all customer price lists for low minimum charges"
        _lblStatus.Location = New Point(20, 20)
        _lblStatus.Size = New Size(960, 30)
        _lblStatus.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        _lblStatus.ForeColor = Color.White
        _lblStatus.BackColor = Color.Black
        Me.Controls.Add(_lblStatus)

        ' Progress bar
        _progressBar = New ProgressBar()
        _progressBar.Location = New Point(20, 55)
        _progressBar.Size = New Size(960, 25)
        _progressBar.Visible = False
        Me.Controls.Add(_progressBar)

        ' Results textbox (scrollable, read-only)
        _txtResults = New TextBox()
        _txtResults.Location = New Point(20, 90)
        _txtResults.Size = New Size(960, 500)
        _txtResults.Font = New Font("Consolas", 9.0F, FontStyle.Regular)
        _txtResults.BackColor = Color.Black
        _txtResults.ForeColor = Color.White
        _txtResults.Multiline = True
        _txtResults.ScrollBars = ScrollBars.Both
        _txtResults.WordWrap = False
        _txtResults.ReadOnly = True
        _txtResults.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(_txtResults)

        ' Scan button
        _btnScan = New Button()
        _btnScan.Text = "Start Scan"
        _btnScan.Location = New Point(20, 610)
        _btnScan.Size = New Size(150, 40)
        _btnScan.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _btnScan.BackColor = Color.LightGray
        _btnScan.ForeColor = Color.Black
        _btnScan.FlatStyle = FlatStyle.Flat
        _btnScan.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        AddHandler _btnScan.Click, AddressOf OnScan
        Me.Controls.Add(_btnScan)

        ' Save to file button
        _btnSaveToFile = New Button()
        _btnSaveToFile.Text = "Save to File"
        _btnSaveToFile.Location = New Point(190, 610)
        _btnSaveToFile.Size = New Size(150, 40)
        _btnSaveToFile.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _btnSaveToFile.BackColor = Color.LightGray
        _btnSaveToFile.ForeColor = Color.Black
        _btnSaveToFile.FlatStyle = FlatStyle.Flat
        _btnSaveToFile.Enabled = False
        _btnSaveToFile.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        AddHandler _btnSaveToFile.Click, AddressOf OnSaveToFile
        Me.Controls.Add(_btnSaveToFile)

        ' Print button
        _btnPrint = New Button()
        _btnPrint.Text = "Print"
        _btnPrint.Location = New Point(360, 610)
        _btnPrint.Size = New Size(150, 40)
        _btnPrint.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _btnPrint.BackColor = Color.LightGray
        _btnPrint.ForeColor = Color.Black
        _btnPrint.FlatStyle = FlatStyle.Flat
        _btnPrint.Enabled = False
        _btnPrint.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        AddHandler _btnPrint.Click, AddressOf OnPrint
        Me.Controls.Add(_btnPrint)

        ' Close button
        _btnClose = New Button()
        _btnClose.Text = "Close"
        _btnClose.Location = New Point(830, 610)
        _btnClose.Size = New Size(150, 40)
        _btnClose.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _btnClose.BackColor = Color.LightGray
        _btnClose.ForeColor = Color.Black
        _btnClose.FlatStyle = FlatStyle.Flat
        _btnClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        AddHandler _btnClose.Click, AddressOf OnClose
        Me.Controls.Add(_btnClose)

        Me.CancelButton = _btnClose
    End Sub

    Private Sub LoadStandardProcedures()
        _standardProcedures.Clear()

        Try
            ' Load from WORD\PROCDURE.DOC (DOS source lines 4502-4506)
            Dim procedureFile = Path.Combine(LegacyDataPaths.BaseDataDir, "WORD", "PROCDURE.DOC")
            If File.Exists(procedureFile) Then
                Using reader As New StreamReader(procedureFile)
                    While Not reader.EndOfStream
                        Dim line = reader.ReadLine()?.Trim()
                        If Not String.IsNullOrEmpty(line) Then
                            ' Parse DOS INPUT format: "procedure",mincharge
                            Dim parts = line.Split(","c)
                            If parts.Length >= 2 Then
                                Dim proc = parts(0).Trim(""""c).Trim()
                                Dim minCharge As Decimal
                                If Decimal.TryParse(parts(1).Trim(), minCharge) Then
                                    If Not String.IsNullOrEmpty(proc) Then
                                        _standardProcedures(proc.ToUpperInvariant()) = minCharge
                                    End If
                                End If
                            End If
                        End If
                    End While
                End Using
            End If
        Catch ex As Exception
            ' If can't load standard procedures, warn user
            DosMessageBox.Show(Me, $"Warning: Could not load WORD\PROCDURE.DOC:{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}Scan will continue but cannot validate min charges.",
                              "Warning", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub OnScan(sender As Object, e As EventArgs)
        If _btnScan.Text = "Cancel" Then
            _cancelRequested = True
            _btnScan.Enabled = False
            Return
        End If

        _cancelRequested = False
        _btnScan.Text = "Cancel"
        _btnSaveToFile.Enabled = False
        _btnPrint.Enabled = False
        _txtResults.Clear()
        _scanResults.Clear()

        Try
            ExecuteScan()
        Finally
            _btnScan.Text = "Start Scan"
            _btnScan.Enabled = True
            _btnSaveToFile.Enabled = True
            _btnPrint.Enabled = True
        End Try
    End Sub

    Private Sub ExecuteScan()
        Dim realnameFile = Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.DAT")
        If Not File.Exists(realnameFile) Then
            DosMessageBox.Show(Me, "REALNAME.DAT not found.", "Error", MessageBoxButtons.OK)
            Return
        End If

        If _standardProcedures.Count = 0 Then
            DosMessageBox.Show(Me, "WORD\PROCDURE.DOC is empty or could not be loaded. Cannot validate min charges.", "Error", MessageBoxButtons.OK)
            Return
        End If

        ' Build header
        _scanResults.AppendLine($"List of all procedures with min charge too low - {DateTime.Now:MM/dd/yyyy}")
        _scanResults.AppendLine("Cross referenced by PROCDURE document in word processor.")
        _scanResults.AppendLine()
        _scanResults.AppendLine($"{"Company",-15} {"Actual MC",10} {"Eft Date",10} {"Expected",10} {"Procedure",-30}")
        _scanResults.AppendLine(New String("="c, 80))
        _scanResults.AppendLine()

        ' Load customer list
        Dim customers As New List(Of Tuple(Of String, String))() ' filename, realname
        Using reader As New StreamReader(realnameFile)
            While Not reader.EndOfStream
                Dim filename = reader.ReadLine()?.Trim()
                Dim realname = reader.ReadLine()?.Trim()
                If Not String.IsNullOrEmpty(filename) AndAlso Not String.IsNullOrEmpty(realname) Then
                    customers.Add(Tuple.Create(filename, realname))
                End If
            End While
        End Using

        _progressBar.Visible = True
        _progressBar.Maximum = customers.Count
        _progressBar.Value = 0

        Dim startTime = DateTime.Now
        Dim issueCount = 0

        For i = 0 To customers.Count - 1
            If _cancelRequested Then
                _lblStatus.Text = "Scan cancelled by user"
                _scanResults.AppendLine()
                _scanResults.AppendLine("*** SCAN CANCELLED ***")
                _txtResults.Text = _scanResults.ToString()
                _progressBar.Visible = False
                Return
            End If

            Dim customer = customers(i)
            Dim filename = customer.Item1.Trim(""""c)
            Dim realname = customer.Item2.Trim(""""c)

            ' Update status
            Dim elapsed = CInt((DateTime.Now - startTime).TotalMinutes)
            _lblStatus.Text = $"Scanning: {realname} ({i + 1}/{customers.Count})   Elapsed: {elapsed} Min."
            _progressBar.Value = i + 1
            Application.DoEvents()

            ' Scan this customer's price list
            Dim customerIssues = ScanCustomerMinCharges(filename, realname)
            issueCount += customerIssues

            ' Update results periodically (every 10 customers)
            If (i + 1) Mod 10 = 0 Then
                _txtResults.Text = _scanResults.ToString()
                _txtResults.SelectionStart = _txtResults.Text.Length
                _txtResults.ScrollToCaret()
            End If
        Next

        ' Final update
        _txtResults.Text = _scanResults.ToString()
        _progressBar.Visible = False

        _scanResults.AppendLine()
        _scanResults.AppendLine(New String("="c, 80))
        _scanResults.AppendLine($"Scan complete: {issueCount} issue(s) found in {customers.Count} customer(s)")
        _scanResults.AppendLine($"Elapsed time: {CInt((DateTime.Now - startTime).TotalMinutes)} minutes")

        _txtResults.Text = _scanResults.ToString()
        _lblStatus.Text = $"Scan complete: {issueCount} issue(s) found"

        If issueCount = 0 Then
            DosMessageBox.Show(Me, "No issues found! All min charges are correct.", "Scan Complete", MessageBoxButtons.OK)
        End If
    End Sub

    Private Function ScanCustomerMinCharges(filename As String, realname As String) As Integer
        Dim issueCount = 0
        Dim prcFile = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC", filename)

        If Not File.Exists(prcFile) Then
            Return 0
        End If

        Try
            Dim customerNameShort = Path.GetFileNameWithoutExtension(filename)

            Using reader As New StreamReader(prcFile)
                While Not reader.EndOfStream
                    Dim line = reader.ReadLine()
                    If String.IsNullOrWhiteSpace(line) Then Continue While

                    ' Parse DOS WRITE format: "procedure","date",mincharge,price,"type"
                    Dim parts = ParseDosWriteLine(line)
                    If parts.Length >= 5 Then
                        Dim procedureName = parts(0)
                        Dim effectiveDate = parts(1)
                        Dim minCharge = CDec(parts(2))
                        Dim price = CDec(parts(3))

                        ' Check 1: Min charge = 0 AND price = 0 (and doesn't end with -NC) (DOS line 4615)
                        If minCharge = 0 AndAlso price = 0 AndAlso Not procedureName.EndsWith("-NC") Then
                            _scanResults.AppendLine($"{customerNameShort,-15} {minCharge,10:C} {effectiveDate,10} {"N/A",10} {procedureName,-30}  Is this CORRECT?")
                            issueCount += 1
                        End If

                        ' Skip LOT procedures (DOS line 4662)
                        If procedureName.Contains("LOT") Then
                            Continue While
                        End If

                        ' Skip if min charge is already 0 (DOS line 4661)
                        If minCharge = 0 Then
                            Continue While
                        End If

                        ' Check 2: Find matching standard procedure and compare min charge (DOS lines 4620-4670)
                        Dim matchedStdProc As String = Nothing
                        Dim expectedMinCharge As Decimal = 0

                        For Each kvp In _standardProcedures
                            ' DOS logic: LEFT$(PN$,LEN(PN$(X)))=PN$(X)
                            ' Means: procedure name STARTS WITH standard procedure name
                            If procedureName.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase) Then
                                matchedStdProc = kvp.Key
                                expectedMinCharge = kvp.Value
                                Exit For
                            End If
                        Next

                        If matchedStdProc Is Nothing Then
                            ' Procedure not in standard list (DOS line 4630-4651)
                            _scanResults.AppendLine($"{customerNameShort,-15} {"N/A",10} {effectiveDate,10} {"N/A",10} {procedureName,-30}  (Bad Procedure)")
                            issueCount += 1
                        ElseIf minCharge < expectedMinCharge Then
                            ' Min charge is lower than expected (DOS line 4660-4670)
                            _scanResults.AppendLine($"{customerNameShort,-15} {minCharge,10:C} {effectiveDate,10} {expectedMinCharge,10:C} {procedureName,-30}")
                            issueCount += 1
                        End If
                    End If
                End While
            End Using
        Catch ex As Exception
            _scanResults.AppendLine($"{Path.GetFileNameWithoutExtension(filename),-15} ERROR reading file: {ex.Message}")
            issueCount += 1
        End Try

        Return issueCount
    End Function

    Private Function ParseDosWriteLine(line As String) As String()
        Dim parts As New List(Of String)()
        Dim current As New StringBuilder()
        Dim inQuotes = False

        For i = 0 To line.Length - 1
            Dim c = line(i)
            If c = """"c Then
                inQuotes = Not inQuotes
            ElseIf c = ","c AndAlso Not inQuotes Then
                parts.Add(current.ToString().Trim())
                current.Clear()
            Else
                current.Append(c)
            End If
        Next

        If current.Length > 0 Then
            parts.Add(current.ToString().Trim())
        End If

        Return parts.ToArray()
    End Function

    Private Sub OnSaveToFile(sender As Object, e As EventArgs)
        Try
            Using dialog As New SaveFileDialog()
                dialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
                dialog.DefaultExt = "txt"
                dialog.FileName = $"MinChargeScanResults_{DateTime.Now:yyyyMMdd_HHmmss}.txt"

                If dialog.ShowDialog() = DialogResult.OK Then
                    File.WriteAllText(dialog.FileName, _scanResults.ToString())
                    DosMessageBox.Show(Me, $"Results saved to:{Environment.NewLine}{dialog.FileName}",
                                      "Saved", MessageBoxButtons.OK)
                End If
            End Using
        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub OnPrint(sender As Object, e As EventArgs)
        Try
            Dim printDoc As New System.Drawing.Printing.PrintDocument()
            Dim lines = _scanResults.ToString().Split(New String() {Environment.NewLine}, StringSplitOptions.None)
            Dim currentLine = 0

            AddHandler printDoc.PrintPage, Sub(sender2, e2)
                                               Dim font As New Font("Courier New", 9)
                                               Dim y As Single = e2.MarginBounds.Top
                                               Dim lineHeight = font.GetHeight(e2.Graphics)

                                               While currentLine < lines.Length AndAlso y + lineHeight < e2.MarginBounds.Bottom
                                                   e2.Graphics.DrawString(lines(currentLine), font, Brushes.Black,
                                                                        e2.MarginBounds.Left, y)
                                                   y += lineHeight
                                                   currentLine += 1
                                               End While

                                               e2.HasMorePages = (currentLine < lines.Length)
                                           End Sub

            Dim printDialog As New PrintDialog()
            printDialog.Document = printDoc
            If printDialog.ShowDialog() = DialogResult.OK Then
                printDoc.Print()
            End If
        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error printing: {ex.Message}", "Error", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub OnClose(sender As Object, e As EventArgs)
        Me.Close()
    End Sub
End Class
