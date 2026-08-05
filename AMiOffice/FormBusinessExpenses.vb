Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

' Business Expense Account — Menu Option L
' DOS source: PERSONAL.BAS / PERSONAL.ASC (92 lines)
'
' DOS menu had 3 options:
'   (A) Add an Expense
'   (B) List all Expenses to the Printer
'   (C) List all Expenses to the Screen
'
' VB implementation:
'   Same 3 options on a menu form.
'   (A) Add — inline entry form on same screen
'   (B) Print — Windows PrintDialog formatted report
'   (C) View — DataGridView with totals per person + grand total
'
' Data file: EXPENSE.DAT (sequential, comma-delimited)
'   Fields per record: Name, Date, CompanyName, Reason, Cost
'   DOS names hardcoded: "Ed Dean" (E) and "Larry Flynn" (L)
Public Class FormBusinessExpenses
    Inherits DosMenuFormBase

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        SetMenuTitle("BUSINESS EXPENSE ACCOUNT")
        ShowVersionInHeader = False
        UpdateHeaderClock()

        StretchButtonsToPanelWidth = True
        flpLeft.Padding = New Padding(8, 0, 8, 0)
        flpLeft.AutoScroll = False
        flpRight.Visible = False
        flpRight.Enabled = False

        Me.Width = 1100
        Me.Height = 560

        BuildMenu()
        TightenButtons()
    End Sub

    Private Sub TightenButtons()
        For Each c As Control In flpLeft.Controls
            Dim btn = TryCast(c, Button)
            If btn IsNot Nothing Then
                btn.Height = 34
                btn.Margin = New Padding(3, 3, 3, 4)
                btn.Font = New Font("Segoe UI", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
            End If
        Next
    End Sub

    Private Sub BuildMenu()
        ClearMenu()
        Dim p = flpLeft

        AddMenuButton(p, "A", "Add an Expense", AddressOf DoAdd)
        AddMenuButton(p, "B", "List all Expenses to the Printer", AddressOf DoPrint)
        AddMenuButton(p, "C", "List all Expenses to the Screen", AddressOf DoView)
        AddMenuButton(p, "Q", "Return to Main Menu", Sub() Me.Close())
    End Sub

    ' -----------------------------------------------------------------------
    ' Option A — Add an Expense
    ' -----------------------------------------------------------------------
    Private Sub DoAdd()
        Using frm As New FormBusinessExpenseAdd()
            frm.ShowDialog(Me)
        End Using
    End Sub

    ' -----------------------------------------------------------------------
    ' Option C — View on Screen
    ' -----------------------------------------------------------------------
    Private Sub DoView()
        Using frm As New FormBusinessExpenseView()
            frm.ShowDialog(Me)
        End Using
    End Sub

    ' -----------------------------------------------------------------------
    ' Option B — Print
    ' -----------------------------------------------------------------------
    Private Sub DoPrint()
        Try
            Dim records = ExpenseRecord.ReadAll(LegacyDataPaths.ExpenseDat)
            If records.Count = 0 Then
                DosMessageBox.Show(Me, "No expense records found.", "Business Expenses", MessageBoxButtons.OK)
                Return
            End If
            PrintExpenses(records, Me)
        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error reading expenses: {ex.Message}", "Business Expenses", MessageBoxButtons.OK)
        End Try
    End Sub

    Friend Shared Sub PrintExpenses(records As List(Of ExpenseRecord), owner As Form)
        Dim lines As New List(Of String)()
        lines.Add("<<<  BUSINESS EXPENSE REPORT  >>>")
        lines.Add($"TODAYS DATE: {Date.Now:MMMM dd, yyyy}")
        lines.Add("")
        lines.Add($"{"NAME",-20}{"DATE",-15}{"COST",12}")
        lines.Add("COMPANY NAME    ==    REASON")
        lines.Add(New String("="c, 79))

        Dim totE As Decimal = 0D
        Dim totL As Decimal = 0D
        For Each r In records
            If r.Name.StartsWith("E", StringComparison.OrdinalIgnoreCase) Then totE += r.Cost
            If r.Name.StartsWith("L", StringComparison.OrdinalIgnoreCase) Then totL += r.Cost
            lines.Add($"{r.Name,-20}{r.RecordDate,-15}{r.Cost,12:C2}")
            lines.Add($"{r.CompanyName} == {r.Reason}")
            lines.Add("")
        Next

        lines.Add(New String("="c, 79))
        lines.Add($">>>>> Total for Ed Dean:     {totE,12:C2}")
        lines.Add($">>>>> Total for Larry Flynn: {totL,12:C2}")
        lines.Add($">>>>> Grand Total:           {totE + totL,12:C2}")

        Dim currentLine As Integer = 0
        Dim printDoc As New System.Drawing.Printing.PrintDocument()
        AddHandler printDoc.PrintPage, Sub(s, ev)
                                           Dim font As New Font("Courier New", 9)
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
        If dlg.ShowDialog(owner) = DialogResult.OK Then
            printDoc.Print()
        End If
    End Sub
End Class

' ===========================================================================
' Add an Expense form — sequential DOS-style entry
' DOS flow (PERSONAL.ASC lines 300-450):
'   1. Press 1=Ed Dean or 2=Larry Flynn
'   2. Date (Enter = today)
'   3. Company Name (required)
'   4. Reason
'   5. Cost (non-zero required)
'   6. Confirmation screen — Y to save, N to restart
'
' Layout: RichTextBox accumulates all prior Q&A (like a DOS terminal).
'         lblInput shows the current typed buffer on the line right below.
' ===========================================================================
Public Class FormBusinessExpenseAdd
    Inherits Form

    ' ---- entry state machine ----
    Private Enum EntryStep
        NameEntry
        DateEntry
        Company
        Reason
        Cost
        Confirm
    End Enum

    Private _step As EntryStep = EntryStep.NameEntry
    Private _name As String = ""
    Private _date As String = ""
    Private _company As String = ""
    Private _reason As String = ""
    Private _cost As String = ""
    Private _inputBuffer As String = ""
    Private _inputStart As Integer = 0  ' position in rtb where inline input begins

    ' ---- UI: terminal-style RichTextBox only — typing is inline like DOS ----
    Private ReadOnly rtb As New RichTextBox()

    Private ReadOnly _mono As New Font("Courier New", 12.0F, FontStyle.Regular, GraphicsUnit.Point)
    Private ReadOnly _monoBold As New Font("Courier New", 14.0F, FontStyle.Bold, GraphicsUnit.Point)

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        Text = "Add an Expense"
        Width = 900
        Height = 560
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.Black
        ForeColor = Color.White
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        KeyPreview = True

        ' RichTextBox fills entire form — acts as a DOS terminal screen
        rtb.Dock = DockStyle.Fill
        rtb.BackColor = Color.Black
        rtb.ForeColor = Color.White
        rtb.Font = _mono
        rtb.ReadOnly = False   ' Must be False so SelectedText deletion works in code
        rtb.BorderStyle = BorderStyle.None
        rtb.ScrollBars = RichTextBoxScrollBars.None
        rtb.Cursor = Cursors.Default
        rtb.TabStop = False
        rtb.ShortcutsEnabled = False
        Controls.Add(rtb)

        ' Print the header then show the first prompt
        AppendLine("", Color.White)
        AppendLine("    *********** ADD AN EXPENSE ***********", Color.White, bold:=True)
        AppendLine("", Color.White)
        ShowPrompt()
    End Sub

    ' Append a complete line (with newline) to the RichTextBox terminal
    Private Sub AppendLine(text As String, color As Color, Optional bold As Boolean = False)
        Dim start As Integer = rtb.TextLength
        rtb.AppendText(text & vbCrLf)
        rtb.Select(start, text.Length)
        rtb.SelectionColor = color
        rtb.SelectionFont = If(bold, _monoBold, _mono)
        rtb.SelectionLength = 0
        rtb.ScrollToCaret()
    End Sub

    ' Append prompt text WITHOUT a trailing newline, then record where input starts
    Private Sub AppendPrompt(text As String, color As Color)
        Dim start As Integer = rtb.TextLength
        rtb.AppendText(text)
        rtb.Select(start, text.Length)
        rtb.SelectionColor = color
        rtb.SelectionFont = _mono
        rtb.SelectionLength = 0
        _inputStart = rtb.TextLength
        rtb.ScrollToCaret()
    End Sub

    ' Show the prompt for the current step — all prompts are inline (no trailing newline)
    Private Sub ShowPrompt()
        _inputBuffer = ""

        Select Case _step
            Case EntryStep.NameEntry
                AppendPrompt("Enter your name [1 = Ed Dean]  [2 = Larry Flynn]? ", Color.White)
            Case EntryStep.DateEntry
                AppendPrompt($"Date [ENTER = {Date.Now:MM-dd-yyyy}] ? ", Color.White)
            Case EntryStep.Company
                AppendPrompt("Company Name  (and Individual if needed)? ", Color.White)
            Case EntryStep.Reason
                AppendPrompt("Reason ? ", Color.White)
            Case EntryStep.Cost
                AppendPrompt("Cost ? ", Color.White)
            Case EntryStep.Confirm
                AppendLine("", Color.White)
                AppendLine($"{_name,-20}  {_date}", Color.White)
                AppendLine($"Company Name:  {_company}", Color.White)
                AppendLine($"Reason:        {_reason}", Color.White)
                AppendLine($"Cost:          {FormatCost(_cost)}", Color.White)
                AppendLine("", Color.White)
                AppendPrompt("Is this correct (Y/N)? ", Color.White)
        End Select
    End Sub

    ' Update the inline typed text in the RichTextBox after the prompt
    Private Sub RefreshInlineInput(color As Color)
        ' Remove everything from _inputStart to end of rtb, then re-append the buffer
        rtb.Select(_inputStart, rtb.TextLength - _inputStart)
        rtb.SelectedText = ""
        Dim start As Integer = rtb.TextLength
        rtb.AppendText(_inputBuffer)
        rtb.Select(start, _inputBuffer.Length)
        rtb.SelectionColor = color
        rtb.SelectionFont = _mono
        rtb.SelectionLength = 0
        rtb.ScrollToCaret()
    End Sub

    Private Shared Function FormatCost(s As String) As String
        Dim d As Decimal
        If Decimal.TryParse(s, Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, d) Then
            Return d.ToString("C2")
        End If
        Return s
    End Function

    Protected Overrides Sub OnKeyPress(e As KeyPressEventArgs)
        e.Handled = True  ' Set FIRST — prevents key from reaching RTB (no beep, no double-fire)
        MyBase.OnKeyPress(e)

        Dim ch As Char = e.KeyChar

        Select Case _step
            Case EntryStep.NameEntry
                If ch = "1"c Then
                    _name = "Ed Dean"
                    ' Show the keystroke inline then newline, then advance
                    RefreshInlineInput(Color.White)
                    AppendLine("", Color.White)
                    _step = EntryStep.DateEntry
                    ShowPrompt()
                ElseIf ch = "2"c Then
                    _name = "Larry Flynn"
                    _inputBuffer = "2"
                    RefreshInlineInput(Color.White)
                    AppendLine("", Color.White)
                    _step = EntryStep.DateEntry
                    ShowPrompt()
                End If

            Case EntryStep.DateEntry, EntryStep.Company, EntryStep.Reason, EntryStep.Cost
                If ch = ChrW(Keys.Back) Then
                    If _inputBuffer.Length > 0 Then
                        _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1)
                        RefreshInlineInput(Color.White)
                    End If
                ElseIf ch = ChrW(13) Then   ' Enter
                    CommitCurrentStep()
                ElseIf Not Char.IsControl(ch) Then
                    _inputBuffer &= Char.ToUpper(ch)
                    RefreshInlineInput(Color.White)
                End If

            Case EntryStep.Confirm
                If ch = "Y"c OrElse ch = "y"c Then
                    SaveRecord()
                ElseIf ch = "N"c OrElse ch = "n"c Then
                    ' Restart — clear terminal and start over
                    rtb.Clear()
                    AppendLine("", Color.White)
                    AppendLine("    *********** ADD AN EXPENSE ***********", Color.White, bold:=True)
                    AppendLine("", Color.White)
                    _step = EntryStep.NameEntry
                    ShowPrompt()
                End If
        End Select
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.KeyCode = Keys.Escape Then Me.Close()
    End Sub

    Private Sub CommitCurrentStep()
        Select Case _step
            Case EntryStep.DateEntry
                Dim val As String = If(String.IsNullOrWhiteSpace(_inputBuffer), Date.Now.ToString("MM-dd-yyyy"), _inputBuffer.Trim())
                _inputBuffer = val
                RefreshInlineInput(Color.White)
                AppendLine("", Color.White)
                _date = val
                _step = EntryStep.Company
                ShowPrompt()

            Case EntryStep.Company
                If String.IsNullOrWhiteSpace(_inputBuffer) Then
                    ' DOS loops: IF CN$="" THEN 330 — show error inline in red
                    RefreshInlineInput(Color.Red)
                    ' Clear and re-show prompt after a moment
                    Dim start As Integer = rtb.TextLength - _inputBuffer.Length
                    rtb.Select(_inputStart, rtb.TextLength - _inputStart)
                    rtb.SelectedText = ""
                    Dim errStart As Integer = rtb.TextLength
                    rtb.AppendText("(required — please enter a value)")
                    rtb.Select(errStart, rtb.TextLength - errStart)
                    rtb.SelectionColor = Color.Red
                    rtb.SelectionLength = 0
                    _inputBuffer = ""
                    _inputStart = rtb.TextLength
                    Return
                End If
                RefreshInlineInput(Color.White)
                AppendLine("", Color.White)
                _company = _inputBuffer.Trim()
                _step = EntryStep.Reason
                ShowPrompt()

            Case EntryStep.Reason
                RefreshInlineInput(Color.White)
                AppendLine("", Color.White)
                _reason = _inputBuffer.Trim()
                _step = EntryStep.Cost
                ShowPrompt()

            Case EntryStep.Cost
                Dim d As Decimal
                If Not Decimal.TryParse(_inputBuffer.Replace("$", "").Trim(),
                                        Globalization.NumberStyles.Any,
                                        Globalization.CultureInfo.CurrentCulture, d) OrElse d = 0 Then
                    ' DOS loops: IF C=0 THEN 350 — show error inline in red
                    rtb.Select(_inputStart, rtb.TextLength - _inputStart)
                    rtb.SelectedText = ""
                    Dim errStart As Integer = rtb.TextLength
                    rtb.AppendText("(must be non-zero — try again)")
                    rtb.Select(errStart, rtb.TextLength - errStart)
                    rtb.SelectionColor = Color.Red
                    rtb.SelectionLength = 0
                    _inputBuffer = ""
                    _inputStart = rtb.TextLength
                    Return
                End If
                RefreshInlineInput(Color.White)
                AppendLine("", Color.White)
                _cost = d.ToString(Globalization.CultureInfo.InvariantCulture)
                _step = EntryStep.Confirm
                ShowPrompt()
        End Select
    End Sub

    Private Sub SaveRecord()
        Try
            Dim rec As New ExpenseRecord() With {
                .Name = _name,
                .RecordDate = _date,
                .CompanyName = _company,
                .Reason = _reason,
                .Cost = Decimal.Parse(_cost, Globalization.CultureInfo.InvariantCulture)
            }
            rec.AppendTo(LegacyDataPaths.ExpenseDat)
        Catch ex As Exception
            MessageBox.Show("Error saving expense: " & ex.Message, "Business Expenses",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

        ' Return to menu (matches DOS: GOTO 100 after save)
        Me.Close()
    End Sub
End Class


' ===========================================================================
' View Expenses form — DOS terminal style
' DOS: <<< PERSONAL EXPENSE REPORT >>> with paged output (4 records/page)
' [ENTER = Next Page]  [Q = Quit]
' ===========================================================================
Public Class FormBusinessExpenseView
    Inherits Form

    ' DOS shows 4 records per page before prompting [ENTER = Next Page]
    Private Const RecordsPerPage As Integer = 4

    Private ReadOnly rtb As New RichTextBox()
    Private ReadOnly lblPrompt As New Label()
    Private ReadOnly _mono As New Font("Courier New", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

    Private _records As List(Of ExpenseRecord) = New List(Of ExpenseRecord)()
    Private _pageStart As Integer = 0     ' index of first record on current page
    Private _waiting As Boolean = False   ' True = waiting for ENTER/Q between pages
    Private _done As Boolean = False      ' True = all records shown, showing totals

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        Text = "Personal Expense Report"
        Width = 900
        Height = 620
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.Black
        ForeColor = Color.White
        KeyPreview = True

        rtb.Dock = DockStyle.Fill
        rtb.BackColor = Color.Black
        rtb.ForeColor = Color.White
        rtb.Font = _mono
        rtb.ReadOnly = True
        rtb.BorderStyle = BorderStyle.None
        rtb.ScrollBars = RichTextBoxScrollBars.None
        rtb.ShortcutsEnabled = False
        rtb.Cursor = Cursors.Default
        Controls.Add(rtb)

        ' Bottom prompt bar — mirrors DOS row 24
        lblPrompt.Dock = DockStyle.Bottom
        lblPrompt.Height = 24
        lblPrompt.Font = _mono
        lblPrompt.ForeColor = Color.White
        lblPrompt.BackColor = Color.Black
        lblPrompt.TextAlign = ContentAlignment.MiddleLeft
        lblPrompt.Padding = New Padding(4, 0, 0, 0)
        Controls.Add(lblPrompt)

        Try
            _records = ExpenseRecord.ReadAll(LegacyDataPaths.ExpenseDat)
        Catch ex As Exception
            _records = New List(Of ExpenseRecord)()
        End Try

        ShowPage()
    End Sub

    ' Build one screen-page of output, exactly matching DOS layout
    Private Sub ShowPage()
        rtb.Clear()

        ' Header — matches DOS exactly
        ' DOS 602: PRINT "<<< PERSONAL EXPENSE REPORT >>>";TAB(55);"TODAYS DATE: ";DATE$
        ' DOS 605: PRINT "NAME";TAB(20);"DATE";TAB(35);"COST"
        ' DOS 606: PRINT:PRINT "COMPANY NAME    ==    REASON":PRINT STRING$(79,"=")
        Dim hdr As String = "<<<  PERSONAL EXPENSE REPORT  >>>".PadRight(55) & $"TODAYS DATE: {Date.Now:MM-dd-yyyy}"
        AppendLine(hdr, Color.White)
        AppendLine("", Color.White)
        AppendLine("NAME".PadRight(20) & "DATE".PadRight(15) & "COST", Color.White)
        AppendLine("", Color.White)
        AppendLine("COMPANY NAME    ==    REASON", Color.White)
        AppendLine(New String("="c, 79), Color.White)

        If _records.Count = 0 Then
            AppendLine("", Color.White)
            AppendLine("  (No expense records found.)", Color.Gray)
            ShowDonePrompt()
            Return
        End If

        ' Print up to RecordsPerPage records starting at _pageStart
        Dim pageEnd As Integer = Math.Min(_pageStart + RecordsPerPage, _records.Count)
        For i As Integer = _pageStart To pageEnd - 1
            Dim r = _records(i)
            ' Emulate GWBASIC PRINT N$;TAB(20);DT$;TAB(35);USING"$#####.##";C
            ' TAB(n) moves cursor to col n; if already past col n it wraps to next line.
            ' USING"$#####.##" = $ followed by up to 5 digits, dot, 2 digits (9 chars total)
            Dim costStr As String = "$" & r.Cost.ToString("0.00").PadLeft(8)
            Dim buf As String = ""
            Dim col As Integer = 0
            ' N$ at col 0
            buf &= r.Name : col = r.Name.Length
            ' TAB(20)
            If col < 20 Then
                buf &= New String(" "c, 20 - col) : col = 20
            Else
                AppendLine(buf, Color.White) : buf = New String(" "c, 20) : col = 20
            End If
            ' DT$
            buf &= r.RecordDate : col += r.RecordDate.Length
            ' TAB(35)
            If col < 35 Then
                buf &= New String(" "c, 35 - col)
            Else
                AppendLine(buf, Color.White) : buf = New String(" "c, 35)
            End If
            ' Cost — always on same line as whatever buf is now
            buf &= costStr
            AppendLine(buf, Color.White)
            ' DOS line 640: PRINT CN$;" == ";R$:PRINT
            AppendLine($"{r.CompanyName} == {r.Reason}", Color.White)
            AppendLine("", Color.White)
        Next

        Dim morePages As Boolean = (pageEnd < _records.Count)

        If morePages Then
            ' More records remain — wait for ENTER or Q
            _waiting = True
            _done = False
            lblPrompt.Text = "[ENTER = Next Page]   [Q = Quit]"
        Else
            ' Last page — show totals then done prompt
            ShowTotals()
            ShowDonePrompt()
        End If
    End Sub

    Private Sub ShowTotals()
        Dim totE As Decimal = _records.Where(Function(r) r.Name.StartsWith("E", StringComparison.OrdinalIgnoreCase)).Sum(Function(r) r.Cost)
        Dim totL As Decimal = _records.Where(Function(r) r.Name.StartsWith("L", StringComparison.OrdinalIgnoreCase)).Sum(Function(r) r.Cost)
        Dim grand As Decimal = totE + totL
        AppendLine(New String("="c, 79), Color.White)
        AppendLine($">>>>> Total for Ed Dean:     {totE,12:C2}", Color.White)
        AppendLine($">>>>> Total for Larry Flynn: {totL,12:C2}", Color.White)
        AppendLine($">>>>> Grand Total:           {grand,12:C2}", Color.White)
    End Sub

    Private Sub ShowDonePrompt()
        _waiting = False
        _done = True
        lblPrompt.Text = "[Q = Quit]"
    End Sub

    Private Sub AppendLine(text As String, color As Color)
        rtb.SelectionStart = rtb.TextLength
        rtb.SelectionLength = 0
        rtb.SelectionColor = color
        rtb.AppendText(text & vbCrLf)
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)

        If _waiting AndAlso e.KeyCode = Keys.Return Then
            ' Advance to next page
            _pageStart += RecordsPerPage
            _waiting = False
            ShowPage()
            e.Handled = True
            Return
        End If

        If (e.KeyCode = Keys.Q) OrElse (e.KeyCode = Keys.Escape) Then
            Me.Close()
            e.Handled = True
            Return
        End If
    End Sub
End Class

' ===========================================================================
' Expense data record + file I/O
' ===========================================================================
Public Class ExpenseRecord
    Public Property Name As String = ""
    Public Property RecordDate As String = ""
    Public Property CompanyName As String = ""
    Public Property Reason As String = ""
    Public Property Cost As Decimal = 0D

    Public Shared Function ReadAll(path As String) As List(Of ExpenseRecord)
        Dim list As New List(Of ExpenseRecord)()
        If Not File.Exists(path) Then Return list

        ' Collect all non-blank tokens regardless of whether values are on one
        ' comma-delimited line (WRITE# style) or one-per-line (INPUT# style).
        ' DOS INPUT# treats commas and newlines as equivalent field separators,
        ' so both layouts produce identical field sequences.
        Dim tokens As New List(Of String)()
        Using sr As New StreamReader(path, Text.Encoding.Default)
            While Not sr.EndOfStream
                Dim line As String = sr.ReadLine()
                If String.IsNullOrWhiteSpace(line) Then Continue While
                For Each tok In ParseDosFields(line)
                    tokens.Add(tok)
                Next
            End While
        End Using

        ' GWBASIC INPUT#1,N$,DT$,CN$,R$,C reading one-quoted-string-per-line file:
        ' Observed DOS behaviour (tokens 0-25):
        '   Record 1 N$=tok0  (Payroll)        step=4 (C backs off on tok4 = first open)
        '   Record 2 N$=tok4  (Office Expense) step=5 (C consumes tok8 = Insurance)
        '   Record 3 N$=tok9  (Utilities)      step=5
        '   Record 4 N$=tok14 (Rent)           step=5
        ' First record: GWBASIC does not consume non-numeric (file just opened quirk).
        ' Subsequent records: GWBASIC consumes the non-numeric token and advances.
        ' VB-written records with a real numeric 5th field: always consume 5.
        Dim idx As Integer = 0
        Dim firstRecord As Boolean = True
        While idx + 3 < tokens.Count
            Dim rec As New ExpenseRecord()
            rec.Name = tokens(idx)
            rec.RecordDate = tokens(idx + 1)
            rec.CompanyName = tokens(idx + 2)
            rec.Reason = tokens(idx + 3)
            If idx + 4 < tokens.Count Then
                Dim parsedCost As Decimal
                If Decimal.TryParse(tokens(idx + 4), NumberStyles.Any, CultureInfo.InvariantCulture, parsedCost) Then
                    rec.Cost = parsedCost
                    idx += 5
                ElseIf firstRecord Then
                    rec.Cost = 0D
                    idx += 4
                Else
                    rec.Cost = 0D
                    idx += 5
                End If
            Else
                rec.Cost = 0D
                idx += 4
            End If
            firstRecord = False
            list.Add(rec)
        End While

        Return list
    End Function

    Public Sub AppendTo(path As String)
        ' Write in DOS WRITE# format: quoted strings, unquoted number
        Dim line As String = $"{QuoteDos(Name)},{QuoteDos(RecordDate)},{QuoteDos(CompanyName)},{QuoteDos(Reason)},{Cost.ToString(CultureInfo.InvariantCulture)}"
        File.AppendAllText(path, line & vbCrLf, Text.Encoding.Default)
    End Sub

    Private Shared Function QuoteDos(s As String) As String
        Return $"""{s.Replace("""", "")}"" "
    End Function

    Private Shared Function ParseDosFields(line As String) As String()
        ' Tokenises one line from EXPENSE.DAT.
        ' Handles DOS WRITE# comma-delimited: "val1","val2",...,123.45
        ' and the original one-quoted-value-per-line INPUT# layout.
        Dim result As New List(Of String)()
        Dim i As Integer = 0
        Dim n As Integer = line.Length
        While i < n
            Dim ch As Char = line(i)
            If ch = " "c OrElse ch = ","c Then
                ' skip separators
                i += 1
            ElseIf ch = """"c Then
                ' quoted string — find matching closing quote
                Dim j As Integer = line.IndexOf(""""c, i + 1)
                If j < 0 Then j = n   ' unterminated — take to end
                result.Add(line.Substring(i + 1, j - i - 1))
                i = j + 1  ' move past closing quote (comma handled next iteration)
            Else
                ' unquoted token (numeric field written by WRITE#)
                Dim j As Integer = line.IndexOf(","c, i)
                If j < 0 Then j = n
                Dim tok As String = line.Substring(i, j - i).Trim()
                If tok.Length > 0 Then result.Add(tok)
                i = j + 1
            End If
        End While
        Return result.ToArray()
    End Function
End Class
