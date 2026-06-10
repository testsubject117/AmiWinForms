Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class FormLogBookMenu
    Inherits Form

    Private ReadOnly _yearTwoDigit As Integer

    Private ReadOnly txtScreen As TextBox
    Private ReadOnly pnlPrompt As Panel
    Private ReadOnly lblPrompt As Label
    Private ReadOnly txtInput As TextBox

    Private _mode As InputMode = InputMode.Menu

    Private Enum InputMode
        Menu
        ViewOneCustomerPrompt
        InvoiceLookupPrompt
        PoLookupPrompt
        LastDateOfBusinessPrompt
        SpecOrPartScanPrompt
        FindAnythingPrompt
    End Enum

    Public Sub New(yearTwoDigit As Integer)
        _yearTwoDigit = yearTwoDigit

        Me.Text = $"LOG BOOK ({_yearTwoDigit:00})"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.KeyPreview = True
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        Me.Size = New Size(980, 680)

        txtScreen = New TextBox() With {
            .Multiline = True,
            .ReadOnly = True,
            .TabStop = False,
            .ScrollBars = ScrollBars.None,
            .BorderStyle = BorderStyle.None,
            .Dock = DockStyle.Fill,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Font = Me.Font,
            .HideSelection = True,
            .ShortcutsEnabled = False,
            .WordWrap = False
        }

        pnlPrompt = New Panel() With {
            .Visible = False,
            .BackColor = Color.Black
        }

        lblPrompt = New Label() With {
            .AutoSize = True,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Font = Me.Font,
            .Left = 0,
            .Top = 0,
            .Text = ""
        }

        txtInput = New TextBox() With {
            .BorderStyle = BorderStyle.None,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Font = Me.Font,
            .Left = 0,
            .Top = 0,
            .Width = 350
        }

        pnlPrompt.Controls.Add(lblPrompt)
        pnlPrompt.Controls.Add(txtInput)

        Me.Controls.Add(txtScreen)
        Me.Controls.Add(pnlPrompt)

        AddHandler Me.Shown, Sub() ShowMenu()
        AddHandler txtInput.KeyDown, AddressOf OnInputKeyDown

        AddHandler Me.Resize,
            Sub()
                If pnlPrompt.Visible Then PositionPromptOverlay()
            End Sub
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If _mode = InputMode.Menu Then
            Select Case keyData
                Case Keys.Escape
                    Me.Close()
                    Return True

                Case Keys.D1, Keys.NumPad1
                    BeginCustomerPrompt()
                    Return True

                Case Keys.D2, Keys.NumPad2
                    RunViewAllCustomers()
                    Return True

                Case Keys.D3, Keys.NumPad3
                    BeginInvoiceLookupPrompt()
                    Return True

                Case Keys.D4, Keys.NumPad4
                    BeginPoLookupPrompt()
                    Return True

                Case Keys.D5, Keys.NumPad5
                    BeginLastDateOfBusinessPrompt()
                    Return True

                Case Keys.D6, Keys.NumPad6
                    BeginSpecOrPartScanPrompt()
                    Return True

                Case Keys.D7, Keys.NumPad7
                    Dim t As Task = RunLogbookErrorScanAsync()
                    Return True

                Case Keys.D8, Keys.NumPad8
                    BeginFindAnythingPrompt()
                    Return True

                Case Keys.Q
                    Me.Close()
                    Return True
            End Select
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub ShowMenu()
        _mode = InputMode.Menu
        pnlPrompt.Visible = False
        txtInput.Text = ""
        lblPrompt.Text = ""
        RenderMenu(promptLine:=Nothing)
    End Sub

    Private Sub RenderMenu(Optional promptLine As String = Nothing)
        Dim leftDate As String = Date.Now.ToString("MM-dd-yyyy")
        Dim rightTime As String = Date.Now.ToString("HH:mm")

        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine("LOG BOOK")
        sb.AppendLine()
        sb.AppendLine($"{leftDate}    Year in use: {_yearTwoDigit:00}".PadRight(60) & rightTime)
        sb.AppendLine()
        sb.AppendLine("(1) View one customer")
        sb.AppendLine("(2) View all customers")
        sb.AppendLine("(3) Look up an invoice")
        sb.AppendLine("(4) Look up a P.O. Number")
        sb.AppendLine("(5) Look up the last date of business for a customer")
        sb.AppendLine("(6) Scan Entire Logbook to find Customers with a certain Spec. or Part Number")
        sb.AppendLine("(7) Scan logbook for errors")
        sb.AppendLine($"(8) Scan 20{_yearTwoDigit:00} Logbook to find ANYTHING")
        sb.AppendLine("(Q) QUIT")
        sb.AppendLine()

        If Not String.IsNullOrEmpty(promptLine) Then
            sb.AppendLine(promptLine)
        End If

        txtScreen.Text = sb.ToString()
    End Sub

    Private Sub BeginCustomerPrompt()
        _mode = InputMode.ViewOneCustomerPrompt
        RenderMenu(promptLine:="CUSTOMER NAME?")

        lblPrompt.Text = "CUSTOMER NAME? "
        txtInput.Text = ""
        pnlPrompt.Visible = True
        PositionPromptOverlay()

        pnlPrompt.BringToFront()
        txtInput.Focus()
        Me.BeginInvoke(New Action(Sub() txtInput.Clear()))
    End Sub

    Private Sub BeginInvoiceLookupPrompt()
        _mode = InputMode.InvoiceLookupPrompt
        RenderMenu(promptLine:="INVOICE NUMBER TO LOOK UP?")

        lblPrompt.Text = "INVOICE NUMBER TO LOOK UP? "
        txtInput.Text = ""
        pnlPrompt.Visible = True
        PositionPromptOverlay()

        pnlPrompt.BringToFront()
        txtInput.Focus()
        Me.BeginInvoke(New Action(Sub() txtInput.Clear()))
    End Sub

    Private Sub BeginPoLookupPrompt()
        _mode = InputMode.PoLookupPrompt
        RenderMenu(promptLine:="P.O. NUMBER TO LOOK UP?")

        lblPrompt.Text = "P.O. NUMBER TO LOOK UP? "
        txtInput.Text = ""
        pnlPrompt.Visible = True
        PositionPromptOverlay()

        pnlPrompt.BringToFront()
        txtInput.Focus()
        Me.BeginInvoke(New Action(Sub() txtInput.Clear()))
    End Sub

    Private Sub BeginLastDateOfBusinessPrompt()
        _mode = InputMode.LastDateOfBusinessPrompt
        RenderMenu(promptLine:="Enter Part or all of Customers abbreviation [ENTER = All Customers] ?")

        lblPrompt.Text = "Enter Part or all of Customers abbreviation [ENTER = All Customers] ? "
        txtInput.Text = ""
        pnlPrompt.Visible = True
        PositionPromptOverlay()

        pnlPrompt.BringToFront()
        txtInput.Focus()
        Me.BeginInvoke(New Action(Sub() txtInput.Clear()))
    End Sub

    Private Sub BeginSpecOrPartScanPrompt()
        _mode = InputMode.SpecOrPartScanPrompt
        RenderMenu(promptLine:="Enter Part or all of the Spec to search for ")

        lblPrompt.Text = "Enter Part or all of the Spec to search for "
        txtInput.Text = ""
        pnlPrompt.Visible = True
        PositionPromptOverlay()

        pnlPrompt.BringToFront()
        txtInput.Focus()
        Me.BeginInvoke(New Action(Sub() txtInput.Clear()))
    End Sub

    Private Sub BeginFindAnythingPrompt()
        _mode = InputMode.FindAnythingPrompt
        RenderMenu(promptLine:="While in red screen, hit [F3] to scan for the next match." & vbCrLf &
                              "What do you want to look for")

        lblPrompt.Text = "What do you want to look for "
        txtInput.Text = ""
        pnlPrompt.Visible = True
        PositionPromptOverlay()

        pnlPrompt.BringToFront()
        txtInput.Focus()
        Me.BeginInvoke(New Action(Sub() txtInput.Clear()))
    End Sub
    Private Sub PositionPromptOverlay()
        Dim allLines As String() = txtScreen.Text.Split(New String() {vbCrLf}, StringSplitOptions.None)

        Dim promptLineIndex As Integer = -1
        Dim promptText As String = lblPrompt.Text.TrimEnd()

        For i As Integer = allLines.Length - 1 To 0 Step -1
            If allLines(i).Trim().Equals(promptText.Trim(), StringComparison.OrdinalIgnoreCase) Then
                promptLineIndex = i
                Exit For
            End If
        Next

        Dim lineH As Integer = TextRenderer.MeasureText("W", txtScreen.Font).Height
        Dim insetX As Integer = txtScreen.Left + 2
        Dim insetY As Integer = txtScreen.Top + 2
        If promptLineIndex < 0 Then promptLineIndex = allLines.Length - 1

        pnlPrompt.Left = insetX
        pnlPrompt.Top = insetY + (promptLineIndex * lineH)
        pnlPrompt.Width = txtScreen.ClientSize.Width - 4
        pnlPrompt.Height = lineH

        lblPrompt.Left = 0
        lblPrompt.Top = 0

        Dim promptW As Integer = TextRenderer.MeasureText(lblPrompt.Text, lblPrompt.Font).Width
        txtInput.Left = promptW
        txtInput.Top = 0
        txtInput.Height = lineH
        txtInput.Width = Math.Max(250, pnlPrompt.Width - txtInput.Left - 10)
    End Sub

    Private Sub OnInputKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then
            ShowMenu()
            e.Handled = True
            Return
        End If

        If e.KeyCode <> Keys.Enter Then Return

        Dim input As String = txtInput.Text.Trim()
        pnlPrompt.Visible = False

        Select Case _mode
            Case InputMode.ViewOneCustomerPrompt
                RunViewOneCustomer(input)

            Case InputMode.InvoiceLookupPrompt
                Dim t As Task = RunInvoiceLookupAsync(input)

            Case InputMode.PoLookupPrompt
                RunPoLookup(input)

            Case InputMode.LastDateOfBusinessPrompt
                Dim t As Task = RunLastDateOfBusinessAsync(input)

            Case InputMode.SpecOrPartScanPrompt
                Dim t As Task = RunSpecOrPartScanAllYearsAsync(input)

            Case InputMode.FindAnythingPrompt
                Dim t As Task = RunFindAnythingAsync(input)

            Case Else
                ShowMenu()
        End Select

        e.Handled = True
    End Sub

    Private Sub RunViewOneCustomer(customerInput As String)
        If String.IsNullOrWhiteSpace(customerInput) Then
            ShowMenu()
            Return
        End If

        Dim reader As New LogBookReader(AppPaths.DataDir)
        Dim pages As List(Of String) = BuildPagesFromEntries(
            reader.ReadEntries(_yearTwoDigit),
            Function(entry) entry.Customer IsNot Nothing AndAlso entry.Customer.StartsWith(customerInput, StringComparison.OrdinalIgnoreCase),
            includeSource:=False)

        ShowPagesOrEnd(pages)
    End Sub

    Private Sub RunViewAllCustomers()
        Dim reader As New LogBookReader(AppPaths.DataDir)
        Dim pages As List(Of String) = BuildPagesFromEntries(
            reader.ReadEntries(_yearTwoDigit),
            Function(entry) True,
            includeSource:=False)

        ShowPagesOrEnd(pages)
    End Sub

    Private Sub RunPoLookup(poInput As String)
        If String.IsNullOrWhiteSpace(poInput) Then
            ShowMenu()
            Return
        End If

        Dim poSearch As String = poInput.Trim()

        Dim reader As New LogBookReader(AppPaths.DataDir)
        Dim pages As List(Of String) = BuildPagesFromEntries(
            reader.ReadEntries(_yearTwoDigit),
            Function(entry)
                If entry Is Nothing Then Return False

                Dim poValue As String = entry.PONumber
                If poValue Is Nothing Then Return False

                Return poValue.IndexOf(poSearch, StringComparison.OrdinalIgnoreCase) >= 0
            End Function,
            includeSource:=False)

        ShowPagesOrEnd(pages)
    End Sub

    Private Async Function RunInvoiceLookupAsync(invoiceInput As String) As Task
        If String.IsNullOrWhiteSpace(invoiceInput) Then
            ShowMenu()
            Return
        End If

        Dim invoiceNum As Integer
        If Not Integer.TryParse(invoiceInput.Trim(), invoiceNum) Then
            Using v As New FrmDosPagedViewer()
                v.Text = "LOG BOOK"
                v.SetSinglePage("INVALID INVOICE NUMBER, HIT [ESC] to Exit")
                v.ShowDialog(Me)
            End Using
            ShowMenu()
            Return
        End If

        Dim reader As New LogBookReader(AppPaths.DataDir)

        Dim yearPages As List(Of String) = BuildPagesFromEntries(
            reader.ReadEntries(_yearTwoDigit),
            Function(entry) entry.InvoiceNumber.HasValue AndAlso entry.InvoiceNumber.Value = invoiceNum,
            includeSource:=False)

        If yearPages.Count > 0 Then
            ShowPagesOrEnd(yearPages)
            Return
        End If

        Dim r = MessageBox.Show(
            $"Invoice {invoiceNum} was not found in year 20{_yearTwoDigit:00}." & vbCrLf &
            "Search all years (all LOGBOOK.* files)?",
            "LOG BOOK",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If r <> DialogResult.Yes Then
            ShowMenu()
            Return
        End If

        Dim status As New FrmDosPagedViewer()
        status.Text = "LOG BOOK"
        status.BeginStatusMode("[ESC = Quit]")
        status.UpdateStatus("SEARCHING ALL YEARS..." & vbCrLf & vbCrLf & "PLEASE WAIT...")
        status.Show(Me)
        status.BringToFront()

        Dim files As List(Of String)
        Try
            files = GetLogbookDataFilesInFileNameOrder()
        Catch ex As Exception
            status.UpdateStatus("ERROR:" & vbCrLf & ex.Message & vbCrLf & vbCrLf & "HIT [ESC] to Quit")
            Return
        End Try

        Dim matches As List(Of LogBookEntry) =
            Await Task.Run(Function()
                               Dim m As New List(Of LogBookEntry)()

                               For i As Integer = 0 To files.Count - 1
                                   Dim fp As String = files(i)
                                   Dim fileName As String = Path.GetFileName(fp)

                                   status.UpdateStatus(
                                       "SEARCHING ALL YEARS..." & vbCrLf &
                                       $"SCANNING: {fileName}  ({i + 1}/{files.Count})" & vbCrLf & vbCrLf &
                                       "PLEASE WAIT...")

                                   For Each entry In reader.ReadEntriesFromSpecificFile(fp)
                                       If entry IsNot Nothing AndAlso entry.InvoiceNumber.HasValue AndAlso entry.InvoiceNumber.Value = invoiceNum Then
                                           m.Add(entry)
                                       End If
                                   Next
                               Next

                               Return m
                           End Function)

        status.EndStatusMode()

        Dim pages As List(Of String) = BuildPagesFromEntries(matches, Function(entry) True, includeSource:=True)

        If pages.Count = 0 Then
            status.SetSinglePage($"INVOICE {invoiceNum} NOT FOUND IN ANY YEAR." & vbCrLf & vbCrLf &
                                 "HIT [ESC] to Quit")
            status.ShowDialog(Me)
            status.Close()
            status.Dispose()
            ShowMenu()
            Return
        End If

        status.SetPages(pages)
        status.ShowDialog(Me)
        status.Close()
        status.Dispose()

        ShowMenu()
    End Function

    Private Async Function RunLastDateOfBusinessAsync(customerFilter As String) As Task
        Dim filter As String = If(customerFilter, "").Trim()

        Dim reader As New LogBookReader(AppPaths.DataDir)

        Dim yearResults As Dictionary(Of String, (dt As DateTime, inv As Integer?)) =
            ComputeLastDateByCustomer(reader.ReadEntries(_yearTwoDigit), filter)

        If yearResults.Count > 0 Then
            ShowLastDateReport(yearResults, includeSourceLabel:=$"YEAR 20{_yearTwoDigit:00}")
            Return
        End If

        Dim r = MessageBox.Show(
            If(String.IsNullOrEmpty(filter),
               $"No entries were found in year 20{_yearTwoDigit:00}." & vbCrLf &
               "Search all years (all LOGBOOK.* files)?",
               $"Customer '{filter}' was not found in year 20{_yearTwoDigit:00}." & vbCrLf &
               "Search all years (all LOGBOOK.* files)?"),
            "LOG BOOK",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If r <> DialogResult.Yes Then
            ShowMenu()
            Return
        End If

        ' Hide the prompt BEFORE opening the viewer
        pnlPrompt.Visible = False
        txtInput.Text = ""
        Me.Refresh()

        Dim status As New FrmDosPagedViewer()
        status.Text = "LOG BOOK"
        status.BeginStatusMode("[ESC = Quit]")
        status.UpdateStatus("SEARCHING ALL YEARS..." & vbCrLf & vbCrLf & "PLEASE WAIT...")
        status.Show(Me)
        status.BringToFront()

        Dim files As List(Of String)
        Try
            files = GetLogbookDataFilesInFileNameOrder()
        Catch ex As Exception
            status.UpdateStatus("ERROR:" & vbCrLf & ex.Message & vbCrLf & vbCrLf & "HIT [ESC] to Quit")
            Return
        End Try

        Dim allYearResults As Dictionary(Of String, (dt As DateTime, inv As Integer?)) =
            Await Task.Run(Function()
                               Dim acc As New Dictionary(Of String, (dt As DateTime, inv As Integer?))(StringComparer.OrdinalIgnoreCase)

                               For i As Integer = 0 To files.Count - 1
                                   Dim fp As String = files(i)
                                   Dim fileName As String = Path.GetFileName(fp)

                                   status.UpdateStatus(
                                       "SEARCHING ALL YEARS..." & vbCrLf &
                                       $"SCANNING: {fileName}  ({i + 1}/{files.Count})" & vbCrLf & vbCrLf &
                                       "PLEASE WAIT...")

                                   For Each entry In reader.ReadEntriesFromSpecificFile(fp)
                                       If entry Is Nothing Then Continue For

                                       If Not String.IsNullOrEmpty(filter) Then
                                           If entry.Customer Is Nothing OrElse entry.Customer.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 Then
                                               Continue For
                                           End If
                                       End If

                                       Dim cust As String = NormalizeLegacyText(entry.Customer)
                                       If cust.Length = 0 Then Continue For

                                       Dim dtVal As DateTime
                                       If Not TryParseLogbookDate(entry.DateText, dtVal) Then Continue For

                                       Dim invVal As Integer? = entry.InvoiceNumber

                                       If Not acc.ContainsKey(cust) Then
                                           acc(cust) = (dtVal, invVal)
                                       Else
                                           Dim cur = acc(cust)
                                           If dtVal > cur.dt Then
                                               acc(cust) = (dtVal, invVal)
                                           ElseIf dtVal = cur.dt Then
                                               Dim curInv As Integer = If(cur.inv, Integer.MinValue)
                                               Dim newInv As Integer = If(invVal, Integer.MinValue)
                                               If newInv > curInv Then
                                                   acc(cust) = (dtVal, invVal)
                                               End If
                                           End If
                                       End If
                                   Next
                               Next

                               Return acc
                           End Function)

        status.EndStatusMode()

        If allYearResults.Count = 0 Then
            status.SetSinglePage("NOT FOUND IN ANY YEAR." & vbCrLf & vbCrLf & "HIT [ESC] to Quit")
            status.ShowDialog()
            status.Close()
            status.Dispose()
            Me.Show()
            Me.Activate()
            ShowMenu()
            Return
        End If

        status.SetPages(BuildLastDateReportPages(allYearResults, includeSourceLabel:="ALL YEARS"))
        status.ShowDialog()
        status.Close()
        status.Dispose()

        Me.Show()
        Me.Activate()
        ShowMenu()
    End Function

    Private Async Function RunSpecOrPartScanAllYearsAsync(searchText As String) As Task
        Dim s As String = If(searchText, "").Trim()

        If s.Length < 2 Then
            BeginSpecOrPartScanPrompt()
            Return
        End If

        Dim reader As New LogBookReader(AppPaths.DataDir)
        Dim testFp As String = Path.Combine(AppPaths.DataDir, $"LOGBOOK.{_yearTwoDigit:00}")

        Dim n As Integer = 0
        Dim sampleSpec As String = ""
        Dim samplePart As String = ""

        Try
            For Each e In reader.ReadEntriesFromSpecificFile(testFp)
                If e Is Nothing Then Continue For
                n += 1
                If n = 1 Then
                    sampleSpec = If(e.Spec, "")
                    samplePart = If(e.PartNumber, "")
                End If
                If n >= 5 Then Exit For
            Next
        Catch ex As Exception
            MessageBox.Show("READ FAILED:" & vbCrLf & ex.Message, "LOG BOOK DEBUG")
            Return
        End Try

        ' Hide the prompt panel
        pnlPrompt.Visible = False
        txtInput.Text = ""
        Me.Refresh()

        Dim status As New FrmDosPagedViewer()
        status.Text = "LOG BOOK"
        status.BeginStatusMode("[ESC = Quit]")
        status.UpdateStatus("SEARCHING ALL YEARS..." & vbCrLf & vbCrLf & "PLEASE WAIT...")

        ' Hide THIS menu form completely
        Me.Hide()
        status.Show()
        status.BringToFront()

        Dim matches As List(Of LogBookEntry) =
            Await Task.Run(Function()
                               Dim found As New List(Of LogBookEntry)()
                               Dim currentYY As Integer = Date.Now.Year Mod 100
                               Dim totalEntries As Integer = 0
                               Dim filesScanned As Integer = 0
                               Dim needle As String = NormalizeForSearch(s)

                               ' Scan from 1988 (yy=88) through 1999 (yy=99), then 2000 (yy=00) through current year
                                For yy As Integer = 88 To 99
                                    Dim fileName As String = $"LOGBOOK.{yy:00}"
                                    Dim fp As String = Path.Combine(AppPaths.DataDir, fileName)
                                    Dim fullYear As Integer = 1900 + yy

                                    If Not File.Exists(fp) Then
                                        status.UpdateStatus($"LOG BOOK does't exist for the year of  {yy}")
                                        Threading.Thread.Sleep(50)
                                        Continue For
                                    End If

                                    status.UpdateStatus($"Scanning the year of {fullYear}")
                                    Threading.Thread.Sleep(50)

                                    filesScanned += 1
                                    Dim entriesInFile As Integer = 0

                                    For Each entry In reader.ReadEntriesFromSpecificFile(fp)
                                       If entry Is Nothing Then Continue For

                                       totalEntries += 1
                                       entriesInFile += 1

                                               Dim specVal As String = NormalizeForSearch(entry.Spec)
                                               Dim partVal As String = NormalizeForSearch(entry.PartNumber)

                                               If specVal.Contains(needle) OrElse partVal.Contains(needle) Then
                                                   found.Add(entry)
                                               End If
                                           Next
                                       Next

                                               ' Now scan 2000s (yy = 00 through currentYY)
                                       For yy As Integer = 0 To currentYY
                                           Dim fileName As String = $"LOGBOOK.{yy:00}"
                                           Dim fp As String = Path.Combine(AppPaths.DataDir, fileName)
                                           Dim fullYear As Integer = 2000 + yy

                                           If Not File.Exists(fp) Then
                                               status.UpdateStatus($"LOG BOOK does't exist for the year of  {yy}")
                                               Threading.Thread.Sleep(50)
                                               Continue For
                                           End If

                                           status.UpdateStatus($"Scanning the year of {fullYear}")
                                           Threading.Thread.Sleep(50)

                                           filesScanned += 1

                                           For Each entry In reader.ReadEntriesFromSpecificFile(fp)
                                       If entry Is Nothing Then Continue For

                                       totalEntries += 1

                                               Dim specVal As String = NormalizeForSearch(entry.Spec)
                                               Dim partVal As String = NormalizeForSearch(entry.PartNumber)

                                               If specVal.Contains(needle) OrElse partVal.Contains(needle) Then
                                                   found.Add(entry)
                                               End If
                                                   Next
                                               Next

                                               Return found
                                           End Function)

                               ' Write LOGSPEC.DOC (Fix 3)
                               Dim logspecPath As String = Path.Combine(AppPaths.WordDocsDir, "LOGSPEC.DOC")
                               Try
                                   Using writer As New StreamWriter(logspecPath, append:=False, encoding:=Encoding.ASCII)
                                       writer.WriteLine("SCAN ENTIRE LOGBOOK")
                                       writer.WriteLine()
                                       writer.WriteLine($"SEARCH TEXT: {s}")
                                       writer.WriteLine($"MATCHES FOUND: {matches.Count}")
                                       writer.WriteLine()
                                       writer.WriteLine("[ENTER = More]   [ESC = Quit]")
                                       writer.WriteLine()

                                       For Each entry In matches
                                           writer.WriteLine(entry.FormatForDosViewer(includeSource:=True))
                                           writer.WriteLine()
                                       Next

                                       writer.WriteLine("END OF LIST")
                                   End Using
                               Catch ex As Exception
                                   ' Silent fail - don't block user if write fails
                               End Try

                                                           status.EndStatusMode()
                                                           status.Close()
                                                           status.Dispose()

                                                           ' Show one-time informational message about Word
                                                           If Not UserSettings.GetFlag("LogBookSpec_WordLaunchSeen") Then
                                                               MessageBox.Show(
                                                                   "Search complete!" & vbCrLf & vbCrLf &
                                                                   "Results will open in Microsoft Word." & vbCrLf & vbCrLf &
                                                                   "You can view, search, and print the results, then close Word when finished.",
                                                                   "LOG BOOK",
                                                                   MessageBoxButtons.OK,
                                                                   MessageBoxIcon.Information)
                                                               UserSettings.SetFlag("LogBookSpec_WordLaunchSeen")
                                                           End If

                                                           ' Launch Word to view LOGSPEC.DOC (matching DOS behavior)
                                                           Try
                                                               Dim psi As New ProcessStartInfo With {
                                                                   .FileName = logspecPath,
                                                                   .UseShellExecute = True
                                                               }
                                                               Process.Start(psi)
                                                           Catch ex As Exception
                                                               MessageBox.Show($"Unable to open LOGSPEC.DOC:{vbCrLf}{ex.Message}",
                                                                               "LOG BOOK",
                                                                               MessageBoxButtons.OK,
                                                                                                               MessageBoxIcon.Warning)
                                                                                                           End Try

                                                                                                           ' Re-show menu after Word launch
                                                                                                           Me.Show()
                                                                                                           Me.Activate()
                                                                                                           ShowMenu()
                                                                               End Function

    Private Async Function RunLogbookErrorScanAsync() As Task
        Dim reader As New LogBookReader(AppPaths.DataDir)

        ' Hide the prompt panel
        pnlPrompt.Visible = False
        txtInput.Text = ""
        Me.Refresh()

        Dim status As New FrmDosPagedViewer()
        status.Text = "LOG BOOK"
        status.BeginStatusMode("[ESC = Quit]")
        status.UpdateStatus("Finding & printing logbook errors.   Please Wait.   Press [ESC] to Exit")

        ' Hide THIS menu form completely
        Me.Hide()
        status.Show()
        status.BringToFront()

        Dim lines As List(Of String) =
            Await Task.Run(Function()
                               Dim out As New List(Of String)()
                               Dim first As Boolean = True
                               Dim prevInv As Integer = 0

                               For Each entry In reader.ReadEntries(_yearTwoDigit)
                                   If entry Is Nothing Then Continue For
                                   If Not entry.InvoiceNumber.HasValue Then Continue For

                                   Dim inv As Integer = entry.InvoiceNumber.Value
                                   Dim po As String = NormalizeLegacyText(entry.PONumber)
                                   Dim isVoid As Boolean = po.Equals("VOID", StringComparison.OrdinalIgnoreCase)

                                   If Not first Then
                                       If Math.Abs(inv - prevInv) > 1 AndAlso Not isVoid Then
                                           out.Add($"INVOICE JUMP: {prevInv} -> {inv}")
                                           out.Add($"Cust: {NormalizeLegacyText(entry.Customer)}   Date: {NormalizeLegacyText(entry.DateText)}   PO: {po}")
                                           out.Add("")
                                       End If
                                   End If

                                   If inv < 10000 Then
                                       out.Add($"LOW INVOICE#: {inv}")
                                       out.Add($"Cust: {NormalizeLegacyText(entry.Customer)}   Date: {NormalizeLegacyText(entry.DateText)}   PO: {po}")
                                       out.Add("")
                                   End If

                                   prevInv = inv
                                   first = False
                               Next

                               Return out
                           End Function)

        status.EndStatusMode()

        If lines.Count = 0 Then
            status.SetSinglePage($"NO ERRORS FOUND IN LOGBOOK.{_yearTwoDigit:00}" & vbCrLf & vbCrLf &
                                 "HIT [ESC] to Quit")
            status.ShowDialog()
            status.Close()
            status.Dispose()
            Me.Show()
            Me.Activate()
            ShowMenu()
            Return
        End If

        Dim pages As New List(Of String)()
        Dim sb As New System.Text.StringBuilder()

        Const MaxLinesPerPage As Integer = 20
        Dim n As Integer = 0

        Dim header As String = $"LOGBOOK ERRORS (YEAR 20{_yearTwoDigit:00})"
        sb.AppendLine(header)
        sb.AppendLine(New String("-"c, Math.Min(78, header.Length)))

        For Each line In lines
            sb.AppendLine(line)
            n += 1

            If n >= MaxLinesPerPage Then
                pages.Add(sb.ToString().TrimEnd())
                sb.Clear()
                sb.AppendLine(header)
                sb.AppendLine(New String("-"c, Math.Min(78, header.Length)))
                n = 0
            End If
        Next

        If sb.Length > 0 Then
            pages.Add(sb.ToString().TrimEnd())
        End If

        pages.Add("END OF LIST, HIT [ESC] to Exit")

        status.SetPages(pages)
        status.ShowDialog()
        status.Close()
        status.Dispose()

        Me.Show()
        Me.Activate()
        ShowMenu()
    End Function

    Private Async Function RunFindAnythingAsync(searchText As String) As Task
        Dim s As String = If(searchText, "").Trim()
        If s.Length = 0 Then
            ShowMenu()
            Return
        End If

        ' Hide the prompt panel
        pnlPrompt.Visible = False
        txtInput.Text = ""
        Me.Refresh()

        Dim reader As New LogBookReader(AppPaths.DataDir)
        Dim matches As List(Of LogBookEntry)

        Dim status As New FrmDosPagedViewer()
        status.Text = "LOG BOOK"
        status.EnableSearchMode(s, $"LOGBOOK.{_yearTwoDigit:00}")  ' Pass search term and filename
        status.BeginStatusMode("[ESC = Quit]")
        status.UpdateStatus("Please Wait..." & vbCrLf & vbCrLf &
                            $"SCANNING: LOGBOOK.{_yearTwoDigit:00}")

        ' Hide THIS menu form completely before showing the viewer
        Me.Hide()
        status.Show()
        status.BringToFront()

        matches = Await Task.Run(Function()
                                     Dim found As New List(Of LogBookEntry)()

                                     For Each entry In reader.ReadEntries(_yearTwoDigit)
                                         If entry Is Nothing Then Continue For

                                         Dim invText As String = If(entry.InvoiceNumber.HasValue, entry.InvoiceNumber.Value.ToString(), "")

                                         Dim hay As String =
                                             (NormalizeLegacyText(entry.DateText) & " " &
                                              NormalizeLegacyText(entry.Customer) & " " &
                                              NormalizeLegacyText(entry.PartNumber) & " " &
                                              invText & " " &
                                              NormalizeLegacyText(entry.PONumber) & " " &
                                              NormalizeLegacyText(entry.Spec) & " " &
                                              NormalizeLegacyText(entry.QtyAccepted) & " " &
                                              NormalizeLegacyText(entry.QtyRejected) & " " &
                                              NormalizeLegacyText(entry.Material) & " " &
                                              NormalizeLegacyText(entry.HeatTreat) & " " &
                                              NormalizeLegacyText(entry.ReasonRejected) & " " &
                                              NormalizeLegacyText(entry.Status)).Trim()

                                         If hay.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0 Then
                                             found.Add(entry)
                                         End If
                                     Next

                                     Return found
                                 End Function)

        status.EndStatusMode()

        If matches.Count = 0 Then
            ' DOS behavior: Stay in red screen, show "not found" with flashing message
            status.SetSearchResultNotFound()
            status.SetSinglePage("")  ' Empty content, the cyan bars show the message
        Else
            status.SetSearchResultFound()
            Dim pages As New List(Of String)()
            pages.Add("While in red screen, hit [F3] to scan for the next match." & vbCrLf &
                      "[ENTER = More]   [ESC = Quit]" & vbCrLf & vbCrLf &
                      $"SEARCH: {s}" & vbCrLf &
                      $"MATCHES: {matches.Count}")

            For Each entry In matches
                pages.Add(entry.FormatForDosViewer(includeSource:=False))
            Next

            pages.Add("END OF LIST, HIT [ESC] to Exit")

            status.SetPages(pages)
        End If

        ' Show as standalone modal dialog (no parent)
        status.ShowDialog()
        status.Close()
        status.Dispose()

        ' Re-show and re-activate the menu
        Me.Show()
        Me.Activate()
        ShowMenu()
    End Function

    Private Function BuildPagesFromEntries(entries As IEnumerable(Of LogBookEntry),
                                          includePredicate As Func(Of LogBookEntry, Boolean),
                                          includeSource As Boolean) As List(Of String)

        Dim pages As New List(Of String)()
        Dim pageSb As New System.Text.StringBuilder()
        Dim pageEntryCount As Integer = 0

        Try
            For Each entry In entries
                If entry Is Nothing Then Continue For
                If includePredicate IsNot Nothing AndAlso Not includePredicate(entry) Then Continue For

                AddEntryToPages(entry.FormatForDosViewer(includeSource), pages, pageSb, pageEntryCount)
            Next
        Catch ex As Exception
            Using v As New FrmDosPagedViewer()
                v.Text = "LOG BOOK"
                v.SetSinglePage("ERROR READING LOG BOOK:" & vbCrLf & ex.Message)
                v.ShowDialog(Me)
            End Using
            Return New List(Of String)()
        End Try

        FlushPageIfNeeded(pages, pageSb, pageEntryCount)

        If pages.Count > 0 Then
            pages.Add("END OF LOG BOOK, HIT [ESC] to Exit")
        End If

        Return pages
    End Function

    Private Sub ShowPagesOrEnd(pages As List(Of String))
        If pages Is Nothing OrElse pages.Count = 0 Then
            Using v As New FrmDosPagedViewer()
                v.Text = "LOG BOOK"
                v.SetSinglePage("END OF LOG BOOK, HIT [ESC] to Exit")
                v.ShowDialog(Me)
            End Using
            ShowMenu()
            Return
        End If

        Using viewer As New FrmDosPagedViewer()
            viewer.Text = "LOG BOOK"
            viewer.SetPages(pages)
            viewer.ShowDialog(Me)
        End Using

        ShowMenu()
    End Sub

    Private Sub AddEntryToPages(entryText As String,
                                pages As List(Of String),
                                pageSb As System.Text.StringBuilder,
                                ByRef pageEntryCount As Integer)

        Const EntriesPerPage As Integer = 3

        Dim block As String = If(entryText, "").TrimEnd()

        If pageEntryCount >= EntriesPerPage Then
            pages.Add(pageSb.ToString().TrimEnd())
            pageSb.Clear()
            pageEntryCount = 0
        End If

        If pageSb.Length > 0 Then
            pageSb.AppendLine()
        End If

        pageSb.Append(block)
        pageSb.AppendLine()
        pageEntryCount += 1
    End Sub

    Private Sub FlushPageIfNeeded(pages As List(Of String),
                                  pageSb As System.Text.StringBuilder,
                                  ByRef pageEntryCount As Integer)
        If pageSb.Length > 0 Then
            pages.Add(pageSb.ToString().TrimEnd())
            pageSb.Clear()
            pageEntryCount = 0
        End If
    End Sub

    Private Function ComputeLastDateByCustomer(entries As IEnumerable(Of LogBookEntry),
                                               filter As String) As Dictionary(Of String, (dt As DateTime, inv As Integer?))

        Dim acc As New Dictionary(Of String, (dt As DateTime, inv As Integer?))(StringComparer.OrdinalIgnoreCase)

        For Each entry In entries
            If entry Is Nothing Then Continue For

            If Not String.IsNullOrEmpty(filter) Then
                If entry.Customer Is Nothing OrElse entry.Customer.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 Then
                    Continue For
                End If
            End If

            Dim cust As String = NormalizeLegacyText(entry.Customer)
            If cust.Length = 0 Then Continue For

            Dim dtVal As DateTime
            If Not TryParseLogbookDate(entry.DateText, dtVal) Then Continue For

            Dim invVal As Integer? = entry.InvoiceNumber

            If Not acc.ContainsKey(cust) Then
                acc(cust) = (dtVal, invVal)
            Else
                Dim cur = acc(cust)
                If dtVal > cur.dt Then
                    acc(cust) = (dtVal, invVal)
                ElseIf dtVal = cur.dt Then
                    Dim curInv As Integer = If(cur.inv, Integer.MinValue)
                    Dim newInv As Integer = If(invVal, Integer.MinValue)
                    If newInv > curInv Then
                        acc(cust) = (dtVal, invVal)
                    End If
                End If
            End If
        Next

        Return acc
    End Function

    Private Function TryParseLogbookDate(dateText As String, ByRef dt As DateTime) As Boolean
        dt = DateTime.MinValue
        If String.IsNullOrWhiteSpace(dateText) Then Return False

        Dim s As String = NormalizeLegacyText(dateText)

        Dim formats As String() = {
            "MM-dd-yyyy",
            "M-d-yyyy",
            "MM/dd/yyyy",
            "M/d/yyyy",
            "MM-dd-yy",
            "M-d-yy",
            "MM/dd/yy",
            "M/d/yy"
        }

        Return DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, dt)
    End Function

    Private Sub ShowLastDateReport(results As Dictionary(Of String, (dt As DateTime, inv As Integer?)),
                                   includeSourceLabel As String)

        Using viewer As New FrmDosPagedViewer()
            viewer.Text = "LOG BOOK"
            viewer.SetPages(BuildLastDateReportPages(results, includeSourceLabel))
            viewer.ShowDialog(Me)
        End Using

        ShowMenu()
    End Sub

    Private Function BuildLastDateReportPages(results As Dictionary(Of String, (dt As DateTime, inv As Integer?)),
                                              includeSourceLabel As String) As List(Of String)

        Dim keys As New List(Of String)(results.Keys)
        keys.Sort(StringComparer.OrdinalIgnoreCase)

        Dim pages As New List(Of String)()
        Dim sb As New System.Text.StringBuilder()

        Dim header As String =
            "Company".PadRight(12) &
            "Last Date".PadRight(14) &
            "Invoice#" &
            If(String.IsNullOrEmpty(includeSourceLabel), "", "   (" & includeSourceLabel & ")")

        sb.AppendLine(header)
        sb.AppendLine(New String("-"c, Math.Min(78, header.Length)))

        Dim linesOnPage As Integer = 0
        Const MaxLinesPerPage As Integer = 20

        For Each cust In keys
            Dim v = results(cust)
            Dim dtStr As String = v.dt.ToString("MM-dd-yyyy")
            Dim invStr As String = If(v.inv.HasValue, v.inv.Value.ToString(), "")

            sb.AppendLine(cust.PadRight(12) & dtStr.PadRight(14) & invStr)
            linesOnPage += 1

            If linesOnPage >= MaxLinesPerPage Then
                pages.Add(sb.ToString().TrimEnd())
                sb.Clear()
                sb.AppendLine(header)
                sb.AppendLine(New String("-"c, Math.Min(78, header.Length)))
                linesOnPage = 0
            End If
        Next

        If sb.Length > 0 Then
            pages.Add(sb.ToString().TrimEnd())
        End If

        pages.Add("END OF LIST, HIT [ESC] to Exit")
        Return pages
    End Function
    Private Function NormalizeLegacyText(value As String) As String
        If value Is Nothing Then Return ""
        Return value.Trim().Trim(""""c)
    End Function
    Private Function NormalizeForSearch(value As String) As String
        If value Is Nothing Then Return ""
        Dim sb As New System.Text.StringBuilder(value.Length)
        For Each ch In value.ToUpperInvariant()
            If Char.IsLetterOrDigit(ch) Then sb.Append(ch)
        Next
        Return sb.ToString()
    End Function

    Private Function GetLogbookDataFilesInFileNameOrder() As List(Of String)
        Dim all As String() = Directory.GetFiles(AppPaths.DataDir, "LOGBOOK.*", SearchOption.TopDirectoryOnly)
        Dim valid As New List(Of String)()

        For Each fp In all
            Dim name As String = Path.GetFileName(fp)

            ' Skip helper/derived files
            If String.Equals(name, "LOGBOOK.BAK", StringComparison.OrdinalIgnoreCase) Then Continue For
            If String.Equals(name, "LOGBOOK.PRN", StringComparison.OrdinalIgnoreCase) Then Continue For
            If String.Equals(name, "LOGBOOK.SRT", StringComparison.OrdinalIgnoreCase) Then Continue For
            If String.Equals(name, "LOGBOOK.DAT", StringComparison.OrdinalIgnoreCase) Then Continue For
            If String.Equals(name, "LOGBOOK.BAC", StringComparison.OrdinalIgnoreCase) Then Continue For

            If name.StartsWith("LOGBOOK.", StringComparison.OrdinalIgnoreCase) Then
                Dim suffix As String = name.Substring("LOGBOOK.".Length).Trim()
                Dim yy As Integer
                If Integer.TryParse(suffix, yy) Then
                    valid.Add(fp)
                End If
            End If
        Next

        valid.Sort(
        Function(a, b)
            Dim ea As String = Path.GetFileName(a).Substring("LOGBOOK.".Length)
            Dim eb As String = Path.GetFileName(b).Substring("LOGBOOK.".Length)

            Dim ya As Integer
            Dim yb As Integer

            If Not Integer.TryParse(ea, ya) Then ya = Integer.MaxValue
            If Not Integer.TryParse(eb, yb) Then yb = Integer.MaxValue

            Return ya.CompareTo(yb)
        End Function)

        Return valid
    End Function

    Private Function BuildSpecOrPartScanIntroPage(searchText As String, matchCount As Integer) As String
        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine("SCAN ENTIRE LOGBOOK")
        sb.AppendLine()
        sb.AppendLine($"SEARCH TEXT: {searchText}")
        sb.AppendLine($"MATCHES FOUND: {matchCount}")
        sb.AppendLine()
        sb.AppendLine("[ENTER = More]   [ESC = Quit]")
        Return sb.ToString().TrimEnd()
    End Function
End Class
