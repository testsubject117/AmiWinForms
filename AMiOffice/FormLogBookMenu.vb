Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class FormLogBookMenu
    Inherits Form

    Private Const DataDir As String = "\\invoice\mainmenu\data"

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

        pnlPrompt = New Panel() With {.Visible = False, .BackColor = Color.Black}

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

        AddHandler Me.Resize, Sub()
                                  If pnlPrompt.Visible Then PositionPromptOverlay()
                              End Sub
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If _mode = InputMode.Menu Then
            Select Case keyData
                Case Keys.Escape
                    Me.Close() : Return True
                Case Keys.D1, Keys.NumPad1
                    BeginCustomerPrompt() : Return True
                Case Keys.D2, Keys.NumPad2
                    RunViewAllCustomers() : Return True
                Case Keys.D3, Keys.NumPad3
                    BeginInvoiceLookupPrompt() : Return True
                Case Keys.Q
                    Me.Close() : Return True
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
        sb.AppendLine("(8) Scan 2026 Logbook to find ANYTHING")
        sb.AppendLine("(Q) QUIT")
        sb.AppendLine()

        If Not String.IsNullOrEmpty(promptLine) Then sb.AppendLine(promptLine)
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

        lblPrompt.Left = 0 : lblPrompt.Top = 0
        Dim promptW As Integer = TextRenderer.MeasureText(lblPrompt.Text, lblPrompt.Font).Width
        txtInput.Left = promptW : txtInput.Top = 0 : txtInput.Height = lineH
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
                ' IMPORTANT: async so progress can display
                Dim t As Task = RunInvoiceLookupAsync(input)

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

        Dim reader As New LogBookReader(DataDir)
        Dim pages As List(Of String) = BuildPagesFromEntries(reader.ReadEntries(_yearTwoDigit),
                                                        Function(entry) entry.Customer IsNot Nothing AndAlso entry.Customer.StartsWith(customerInput, StringComparison.OrdinalIgnoreCase),
                                                        includeSource:=False)

        ShowPagesOrEnd(pages)
    End Sub

    Private Sub RunViewAllCustomers()
        Dim reader As New LogBookReader(DataDir)
        Dim pages As List(Of String) = BuildPagesFromEntries(reader.ReadEntries(_yearTwoDigit),
                                                        Function(entry) True,
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

        Dim reader As New LogBookReader(DataDir)

        ' Search selected year first
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

        ' Show status viewer immediately
        Dim status As New FrmDosPagedViewer()
        status.Text = "LOG BOOK"
        status.BeginStatusMode("[ESC = Quit]")
        status.UpdateStatus("SEARCHING ALL YEARS..." & vbCrLf & vbCrLf & "PLEASE WAIT...")
        status.Show(Me)
        status.BringToFront()

        Dim files As String()
        Try
            files = Directory.GetFiles(DataDir, "LOGBOOK.*", SearchOption.TopDirectoryOnly)
            Array.Sort(files, StringComparer.OrdinalIgnoreCase)
        Catch ex As Exception
            status.UpdateStatus("ERROR:" & vbCrLf & ex.Message & vbCrLf & vbCrLf & "HIT [ESC] to Quit")
            Return
        End Try

        Dim matches As List(Of LogBookEntry) =
            Await Task.Run(Function()
                               Dim m As New List(Of LogBookEntry)()

                               For i As Integer = 0 To files.Length - 1
                                   Dim fp = files(i)
                                   Dim fileName = Path.GetFileName(fp)

                                   ' UI update (safe because UpdateStatus marshals to UI thread)
                                   status.UpdateStatus(
                                       "SEARCHING ALL YEARS..." & vbCrLf &
                                       $"SCANNING: {fileName}  ({i + 1}/{files.Length})" & vbCrLf & vbCrLf &
                                       "PLEASE WAIT...")

                                   For Each entry In reader.ReadEntriesFromSpecificFile(fp)
                                       If entry.InvoiceNumber.HasValue AndAlso entry.InvoiceNumber.Value = invoiceNum Then
                                           m.Add(entry)
                                       End If
                                   Next
                               Next

                               Return m
                           End Function)

        status.EndStatusMode()

        Dim pages As List(Of String) = BuildPagesFromEntries(
            matches,
            Function(entry) True,
            includeSource:=True)

        If pages.Count = 0 Then
            status.SetSinglePage($"INVOICE {invoiceNum} NOT FOUND IN ANY YEAR." & vbCrLf & vbCrLf &
                                 "HIT [ESC] to Quit")
            Return
        End If

        status.SetPages(pages)
        status.ShowDialog(Me)
        status.Close()
        status.Dispose()

        ShowMenu()
    End Function

    ' -----------------------------
    ' Helpers
    ' -----------------------------
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

        Dim block As String = (If(entryText, "")).TrimEnd()

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
End Class