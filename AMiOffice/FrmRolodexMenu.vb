Imports System.Diagnostics
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Public Class FrmRolodexMenu
    Inherits Form

    Private _inlineMode As RolodexMenuInlineMode = RolodexMenuInlineMode.None
    Private _areaCodeInput As String = ""

    Private _printPhoneBookCustomersOnly As Boolean = False
    Private _printPhoneBookHighQuality As Boolean = False
    Private _printPhoneBookLetterInput As String = ""

    Private _printLabelsCustomersOnly As Boolean = False
    Private _printLabelsStartLetterInput As String = ""

    Private lblTitle As Label
    Private lblDate As Label
    Private lblMenu As Label
    Private txtInline As TextBox

    Public Sub New()
        InitializeComponent()
        InitializeRolodexUi()
    End Sub

    Private Sub InitializeRolodexUi()
        Me.Text = "Rolodex Menu"
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Size = New Size(900, 650)
        Me.KeyPreview = True

        lblTitle = New Label() With {
            .AutoSize = False,
            .Location = New Point(10, 10),
            .Size = New Size(350, 30),
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Font = New Font("Consolas", 14.0F, FontStyle.Bold)
        }

        lblDate = New Label() With {
            .AutoSize = False,
            .Location = New Point(560, 10),
            .Size = New Size(300, 30),
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Font = New Font("Consolas", 12.0F, FontStyle.Regular),
            .TextAlign = ContentAlignment.MiddleRight
        }

        lblMenu = New Label() With {
            .AutoSize = False,
            .Location = New Point(10, 50),
            .Size = New Size(420, 260),
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Font = New Font("Consolas", 12.0F, FontStyle.Regular)
        }

        txtInline = New TextBox() With {
            .Multiline = True,
            .ReadOnly = True,
            .BorderStyle = BorderStyle.None,
            .Location = New Point(10, 320),
            .Size = New Size(860, 260),
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 12.0F, FontStyle.Regular),
            .ScrollBars = ScrollBars.Vertical
        }

        Me.Controls.Add(lblTitle)
        Me.Controls.Add(lblDate)
        Me.Controls.Add(lblMenu)
        Me.Controls.Add(txtInline)

        AddHandler Me.Load, AddressOf FrmRolodexMenu_Load
        AddHandler Me.KeyPress, AddressOf FrmRolodexMenu_KeyPress
        AddHandler Me.KeyDown, AddressOf FrmRolodexMenu_KeyDown
    End Sub

    Private Sub FrmRolodexMenu_Load(sender As Object, e As EventArgs)
        lblTitle.Text = "****** ROLODEX MENU ******"
        lblDate.Text = "Todays Date: " & DateTime.Now.ToString("MM-dd-yyyy")

        lblMenu.Text =
            "(A) Add a Person" & Environment.NewLine &
            "(B) Delete a Person" & Environment.NewLine &
            "(C) Look up a Person" & Environment.NewLine &
            "(D) Modify a Person" & Environment.NewLine &
            "(E) Print Phone Book" & Environment.NewLine &
            "(F) Print Labels" & Environment.NewLine &
            "(G) Look up Area Codes" & Environment.NewLine &
            "(H) Look up Zip Codes" & Environment.NewLine &
            "(I) Test Entire Rolodex for Errors" & Environment.NewLine &
            "(Z) Back to Main Menu"
    End Sub

    Private Sub FrmRolodexMenu_KeyPress(sender As Object, e As KeyPressEventArgs)
        If HandleInlineInput(e.KeyChar) Then
            Return
        End If

        Dim ch = Char.ToUpperInvariant(e.KeyChar)

        Select Case ch
            Case "A"c
                OpenPrompt(RolodexPromptMode.AddPerson)

            Case "B"c
                OpenPrompt(RolodexPromptMode.DeletePerson)

            Case "C"c
                OpenPrompt(RolodexPromptMode.LookupPerson)

            Case "D"c
                Dim sample = New RolodexPersonRecord With {
                    .PersonName = "TEST",
                    .Street = "100 MAIN ST.",
                    .City = "BURMA",
                    .StateCode = "CA",
                    .ZipCode = "91304",
                    .AreaCode = "661",
                    .PhoneNumber = "4780990",
                    .Misc = "TESTING"
                }
                OpenPrompt(RolodexPromptMode.ModifyPerson, sample)

            Case "E"c
                StartPrintPhoneBookFlow()

            Case "F"c
                StartPrintLabelsFlow()

            Case "G"c
                StartAreaCodeFlow()

            Case "H"c
                OpenZipKey()

            Case "I"c
                StartErrorCheckFlow()

            Case "Z"c
                Me.Close()
        End Select
    End Sub

    Private Sub FrmRolodexMenu_KeyDown(sender As Object, e As KeyEventArgs)
        Select Case _inlineMode
            Case RolodexMenuInlineMode.AreaCodes_Prompt
                If e.KeyCode = Keys.Back Then
                    If _areaCodeInput.Length > 0 Then
                        _areaCodeInput = _areaCodeInput.Substring(0, _areaCodeInput.Length - 1)
                        RenderAreaCodePrompt()
                    End If
                    e.SuppressKeyPress = True
                    Return
                ElseIf e.KeyCode = Keys.Enter Then
                    ExecuteAreaCodeLookup()
                    e.SuppressKeyPress = True
                    Return
                ElseIf e.KeyCode = Keys.Escape Then
                    ResetInlineMode()
                    e.SuppressKeyPress = True
                    Return
                End If

            Case RolodexMenuInlineMode.PrintPhoneBook_FirstLetter
                If e.KeyCode = Keys.Back Then
                    If _printPhoneBookLetterInput.Length > 0 Then
                        _printPhoneBookLetterInput = ""
                        RenderPrintPhoneBookFirstLetterPrompt()
                    End If
                    e.SuppressKeyPress = True
                    Return
                ElseIf e.KeyCode = Keys.Enter Then
                    ExecutePrintPhoneBook()
                    e.SuppressKeyPress = True
                    Return
                ElseIf e.KeyCode = Keys.Escape Then
                    ResetInlineMode()
                    e.SuppressKeyPress = True
                    Return
                End If

            Case RolodexMenuInlineMode.PrintLabels_FirstLetter
                If e.KeyCode = Keys.Back Then
                    If _printLabelsStartLetterInput.Length > 0 Then
                        _printLabelsStartLetterInput = ""
                        RenderPrintLabelsFirstLetterPrompt()
                    End If
                    e.SuppressKeyPress = True
                    Return
                ElseIf e.KeyCode = Keys.Enter Then
                    ExecutePrintLabels()
                    e.SuppressKeyPress = True
                    Return
                ElseIf e.KeyCode = Keys.Escape Then
                    ResetInlineMode()
                    e.SuppressKeyPress = True
                    Return
                End If
        End Select

        If e.KeyCode = Keys.Escape Then
            If _inlineMode <> RolodexMenuInlineMode.None Then
                ResetInlineMode()
                e.SuppressKeyPress = True
                Return
            End If

            Me.Close()
        ElseIf e.KeyCode = Keys.Enter Then
            If _inlineMode = RolodexMenuInlineMode.ErrorCheck_Running Then
                txtInline.Text =
                    "Checking Rolodex for Errors...     [ESC] to Exit" & Environment.NewLine & Environment.NewLine &
                    "Testing the Z's" & Environment.NewLine & Environment.NewLine &
                    "Done. Returning to menu."
                _inlineMode = RolodexMenuInlineMode.None
                e.SuppressKeyPress = True
                Return
            End If
        End If
    End Sub

    Private Function HandleInlineInput(ch As Char) As Boolean
        If _inlineMode = RolodexMenuInlineMode.None Then Return False

        If _inlineMode = RolodexMenuInlineMode.AreaCodes_Prompt Then
            If Char.IsControl(ch) Then
                Return True
            End If

            If Char.IsLetterOrDigit(ch) Then
                _areaCodeInput &= Char.ToUpperInvariant(ch)
                RenderAreaCodePrompt()
            End If

            Return True
        End If

        Dim up As Char = Char.ToUpperInvariant(ch)

        Select Case _inlineMode
            Case RolodexMenuInlineMode.PrintPhoneBook_CustomersOnly
                If up = "Y"c OrElse up = "N"c Then
                    _printPhoneBookCustomersOnly = (up = "Y"c)
                    _inlineMode = RolodexMenuInlineMode.PrintPhoneBook_HighQuality
                    RenderPrintPhoneBookHighQualityPrompt()
                    Return True
                End If
                Return True

            Case RolodexMenuInlineMode.PrintPhoneBook_HighQuality
                If up = "Y"c OrElse up = "N"c Then
                    _printPhoneBookHighQuality = (up = "Y"c)
                    _inlineMode = RolodexMenuInlineMode.PrintPhoneBook_FirstLetter
                    _printPhoneBookLetterInput = ""
                    RenderPrintPhoneBookFirstLetterPrompt()
                    Return True
                End If
                Return True

            Case RolodexMenuInlineMode.PrintPhoneBook_FirstLetter
                If Char.IsControl(ch) Then
                    Return True
                End If

                If Char.IsLetterOrDigit(ch) Then
                    _printPhoneBookLetterInput = Char.ToUpperInvariant(ch).ToString()
                    RenderPrintPhoneBookFirstLetterPrompt()
                End If

                Return True

            Case RolodexMenuInlineMode.PrintLabels_CustomersOnly
                If up = "Y"c OrElse up = "N"c Then
                    _printLabelsCustomersOnly = (up = "Y"c)
                    _inlineMode = RolodexMenuInlineMode.PrintLabels_FirstLetter
                    _printLabelsStartLetterInput = ""
                    RenderPrintLabelsFirstLetterPrompt()
                    Return True
                End If
                Return True

            Case RolodexMenuInlineMode.PrintLabels_FirstLetter
                If Char.IsControl(ch) Then
                    Return True
                End If

                If Char.IsLetterOrDigit(ch) Then
                    _printLabelsStartLetterInput = Char.ToUpperInvariant(ch).ToString()
                    RenderPrintLabelsFirstLetterPrompt()
                End If

                Return True
        End Select

        Return False
    End Function

    Private Sub OpenPrompt(mode As RolodexPromptMode, Optional record As RolodexPersonRecord = Nothing)
        Using frm As New FrmRolodexPrompt(New RolodexPromptEngine(mode, record))
            frm.ShowDialog(Me)
        End Using
    End Sub

    Private Sub StartPrintPhoneBookFlow()
        _printPhoneBookCustomersOnly = False
        _printPhoneBookHighQuality = False
        _printPhoneBookLetterInput = ""

        _inlineMode = RolodexMenuInlineMode.PrintPhoneBook_CustomersOnly

        txtInline.Text =
            "E" & Environment.NewLine & Environment.NewLine &
            "Do you want to print your customers only (Y/N) ?"
    End Sub

    Private Sub RenderPrintPhoneBookHighQualityPrompt()
        txtInline.Text =
            "E" & Environment.NewLine & Environment.NewLine &
            "Do you want to print your customers only (Y/N) ? " & If(_printPhoneBookCustomersOnly, "Yes", "No") &
            Environment.NewLine & Environment.NewLine &
            "Do you want High Quality but slow print (Y/N) ?"
    End Sub

    Private Sub RenderPrintPhoneBookFirstLetterPrompt()
        Dim suffix As String = If(_printPhoneBookLetterInput = "", "_", _printPhoneBookLetterInput)

        txtInline.Text =
            "E" & Environment.NewLine & Environment.NewLine &
            "Do you want to print your customers only (Y/N) ? " & If(_printPhoneBookCustomersOnly, "Yes", "No") &
            Environment.NewLine & Environment.NewLine &
            "Do you want High Quality but slow print (Y/N) ? " & If(_printPhoneBookHighQuality, "Yes", "No") &
            Environment.NewLine & Environment.NewLine &
            "Enter the 1st letter of the people you want to print (Enter = All) (Esc = Quit)" & Environment.NewLine &
            suffix
    End Sub

    Private Sub StartPrintLabelsFlow()
        _printLabelsCustomersOnly = False
        _printLabelsStartLetterInput = ""

        _inlineMode = RolodexMenuInlineMode.PrintLabels_CustomersOnly

        txtInline.Text =
            "F" & Environment.NewLine & Environment.NewLine &
            "Do you want to print your customers only (Y/N) ?"
    End Sub

    Private Sub RenderPrintLabelsFirstLetterPrompt()
        Dim suffix As String = If(_printLabelsStartLetterInput = "", "_", _printLabelsStartLetterInput)

        txtInline.Text =
            "F" & Environment.NewLine & Environment.NewLine &
            "Do you want to print your customers only (Y/N) ? " & If(_printLabelsCustomersOnly, "Yes", "No") &
            Environment.NewLine & Environment.NewLine &
            "Enter 1st Letter to start with [Enter = A]" & Environment.NewLine &
            suffix
    End Sub

    Private Sub ExecutePrintPhoneBook()
        Dim repo As New RolodexRepository()
        Dim customerSvc As New RolodexCustomerService()

        Dim customerNames As HashSet(Of String) = Nothing
        If _printPhoneBookCustomersOnly Then
            If Not customerSvc.DataFileExists() Then
                txtInline.Text =
                    "E" & Environment.NewLine & Environment.NewLine &
                    "REALNAME.DAT was not found:" & Environment.NewLine &
                    customerSvc.GetDataFilePath() & Environment.NewLine & Environment.NewLine &
                    "Press ESC to return."
                Return
            End If

            customerNames = customerSvc.LoadCustomerNames()
        End If

        Dim targetLetter As String = _printPhoneBookLetterInput.Trim().ToUpperInvariant()
        Dim allMode As Boolean = (targetLetter = "")

        Dim sb As New StringBuilder()

        If _printPhoneBookCustomersOnly Then
            sb.AppendLine("Print Customers Only.")
            sb.AppendLine()
        End If

        Dim anyPrinted As Boolean = False

        For Each ch In GetShardKeys()
            If Not allMode AndAlso Not String.Equals(ch.ToString(), targetLetter, StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If

            Dim records = repo.ReadAllRecordsForLetter(ch)
            If records Is Nothing OrElse records.Count = 0 Then
                Continue For
            End If

            Dim filtered As New List(Of RolodexPersonRecord)

            For Each r In records
                If r Is Nothing Then Continue For
                If String.IsNullOrWhiteSpace(r.PersonName) Then Continue For

                If _printPhoneBookCustomersOnly Then
                    If Not customerSvc.IsCustomerName(r.PersonName, customerNames) Then
                        Continue For
                    End If
                End If

                filtered.Add(r)
            Next

            If filtered.Count = 0 Then
                Continue For
            End If

            If anyPrinted Then
                sb.AppendLine()
                sb.AppendLine(New String("="c, 70))
                sb.AppendLine()
            End If

            sb.AppendLine("<<< " & ch & " >>>")
            sb.AppendLine()

            For Each r In filtered
                AppendPhoneBookRecord(sb, r)
                sb.AppendLine()
            Next

            anyPrinted = True
        Next

        If Not anyPrinted Then
            txtInline.Text =
                "E" & Environment.NewLine & Environment.NewLine &
                "No matching entries found." & Environment.NewLine & Environment.NewLine &
                "Press ESC to return."
            Return
        End If

        ResetInlineMode()

        Using frm As New FrmPagedTextViewer()
            frm.SetPages(New List(Of String) From {sb.ToString()})
            frm.ShowDialog(Me)
        End Using

        txtInline.Clear()
    End Sub

    Private Sub ExecutePrintLabels()
        Dim repo As New RolodexRepository()
        Dim customerSvc As New RolodexCustomerService()

        Dim customerNames As HashSet(Of String) = Nothing
        If _printLabelsCustomersOnly Then
            If Not customerSvc.DataFileExists() Then
                txtInline.Text =
                    "F" & Environment.NewLine & Environment.NewLine &
                    "REALNAME.DAT was not found:" & Environment.NewLine &
                    customerSvc.GetDataFilePath() & Environment.NewLine & Environment.NewLine &
                    "Press ESC to return."
                Return
            End If

            customerNames = customerSvc.LoadCustomerNames()
        End If

        Dim startChar As Char = "0"c
        If Not String.IsNullOrWhiteSpace(_printLabelsStartLetterInput) Then
            startChar = Char.ToUpperInvariant(_printLabelsStartLetterInput(0))
        End If

        Dim started As Boolean = False
        Dim sb As New StringBuilder()
        Dim anyPrinted As Boolean = False

        For Each ch In GetShardKeys()
            If Not started Then
                If ch = startChar Then
                    started = True
                Else
                    Continue For
                End If
            End If

            Dim records = repo.ReadAllRecordsForLetter(ch)
            If records Is Nothing OrElse records.Count = 0 Then
                Continue For
            End If

            For Each r In records
                If r Is Nothing Then Continue For
                If String.IsNullOrWhiteSpace(r.PersonName) Then Continue For
                If String.IsNullOrWhiteSpace(r.Street) Then Continue For

                If _printLabelsCustomersOnly Then
                    If Not customerSvc.IsCustomerName(r.PersonName, customerNames) Then
                        Continue For
                    End If
                End If

                sb.AppendLine(r.PersonName.Trim())
                sb.AppendLine(If(r.Street, "").Trim())
                sb.AppendLine(BuildCityStateZipLine(r))
                sb.AppendLine()
                sb.AppendLine()

                anyPrinted = True
            Next
        Next

        If Not anyPrinted Then
            txtInline.Text =
                "F" & Environment.NewLine & Environment.NewLine &
                "No matching label entries found." & Environment.NewLine & Environment.NewLine &
                "Press ESC to return."
            Return
        End If

        ResetInlineMode()

        Using frm As New FrmPagedTextViewer()
            frm.SetPages(New List(Of String) From {sb.ToString()})
            frm.ShowDialog(Me)
        End Using

        txtInline.Clear()
    End Sub

    Private Sub AppendPhoneBookRecord(sb As StringBuilder, record As RolodexPersonRecord)
        sb.AppendLine(record.PersonName.Trim())

        If Not String.IsNullOrWhiteSpace(record.Street) Then
            sb.AppendLine("     " & record.Street.Trim() & "   " & BuildCompactCityStateZipLine(record))
        End If

        Dim phoneLine As String =
            "     (" & SafeAreaCode(record.AreaCode) & ")" &
            BuildSevenDigitPhone(record.PhoneNumber) &
            "    "

        If String.IsNullOrWhiteSpace(record.Misc) Then
            sb.AppendLine(phoneLine)
            Return
        End If

        Dim misc As String = record.Misc.Trim()

        If misc.Length <= 55 Then
            sb.AppendLine(phoneLine & misc)
        Else
            sb.AppendLine(phoneLine & misc.Substring(0, 55))
            sb.AppendLine(New String(" "c, 23) & misc.Substring(55))
        End If
    End Sub

    Private Function BuildCompactCityStateZipLine(record As RolodexPersonRecord) As String
        Dim city As String = If(record.City, "").Trim()
        Dim stateCode As String = If(record.StateCode, "").Trim()
        Dim zipCode As String = If(record.ZipCode, "").Trim()

        If city = "" AndAlso stateCode = "" AndAlso zipCode = "" Then
            Return ""
        End If

        Return city & "," & stateCode & zipCode
    End Function

    Private Function BuildCityStateZipLine(record As RolodexPersonRecord) As String
        Dim city As String = If(record.City, "").Trim()
        Dim stateCode As String = If(record.StateCode, "").Trim()
        Dim zipCode As String = If(record.ZipCode, "").Trim()

        Return city & " , " & stateCode & "  " & zipCode
    End Function

    Private Function SafeAreaCode(value As String) As String
        Dim digits = New String((If(value, "")).Where(Function(c) Char.IsDigit(c)).ToArray())

        If digits.Length >= 3 Then
            Return digits.Substring(digits.Length - 3)
        End If

        Return digits.PadLeft(3, "0"c)
    End Function

    Private Function BuildSevenDigitPhone(value As String) As String
        Dim digits = New String((If(value, "")).Where(Function(c) Char.IsDigit(c)).ToArray())

        If digits.Length >= 7 Then
            digits = digits.Substring(digits.Length - 7)
            Return digits.Substring(0, 3) & "-" & digits.Substring(3, 4)
        End If

        Return value
    End Function

    Private Iterator Function GetShardKeys() As IEnumerable(Of Char)
        For i As Integer = AscW("0"c) To AscW("9"c)
            Yield ChrW(i)
        Next

        For i As Integer = AscW("A"c) To AscW("Z"c)
            Yield ChrW(i)
        Next
    End Function

    Private Sub StartErrorCheckFlow()
        _inlineMode = RolodexMenuInlineMode.ErrorCheck_Running
        txtInline.Text =
            "Checking Rolodex for Errors...     [ESC] to Exit" & Environment.NewLine & Environment.NewLine &
            "Testing the Z's" & Environment.NewLine & Environment.NewLine &
            "Hit [ENTER] to continue?"
    End Sub

    Private Sub StartAreaCodeFlow()
        _inlineMode = RolodexMenuInlineMode.AreaCodes_Prompt
        _areaCodeInput = ""
        RenderAreaCodePrompt()
    End Sub

    Private Sub RenderAreaCodePrompt()
        txtInline.Text =
            "<<< LOOK UP AREA CODES >>>" & Environment.NewLine & Environment.NewLine &
            "Enter Area Code to find city or 2 letter abreviation of state to find area code" & Environment.NewLine &
            " Example: California = CA" & Environment.NewLine & Environment.NewLine &
            "[A = All] [Q = Quit] ? " & _areaCodeInput & "_"

        txtInline.SelectionStart = txtInline.TextLength
        txtInline.ScrollToCaret()
    End Sub

    Private Sub ExecuteAreaCodeLookup()
        Dim svc As New RolodexAreaCodeService()
        Dim trimmed As String = _areaCodeInput.Trim()

        If String.IsNullOrWhiteSpace(trimmed) Then
            RenderAreaCodePrompt()
            Return
        End If

        If String.Equals(trimmed, "Q", StringComparison.OrdinalIgnoreCase) Then
            ResetInlineMode()
            Return
        End If

        If Not svc.DataFileExists() Then
            txtInline.Text =
                "<<< LOOK UP AREA CODES >>>" & Environment.NewLine & Environment.NewLine &
                "Area code data file not found:" & Environment.NewLine &
                svc.GetDataFilePath() & Environment.NewLine & Environment.NewLine &
                "Press ESC to return."
            Return
        End If

        If String.Equals(trimmed, "A", StringComparison.OrdinalIgnoreCase) Then
            ResetInlineMode()

            Using frm As New FrmPagedTextViewer()
                frm.SetPages(svc.GetAllPages())
                frm.ShowDialog(Me)
            End Using

            txtInline.Clear()
            Return
        End If

        Dim results = svc.Search(trimmed)

        If results Is Nothing OrElse results.Count = 0 Then
            txtInline.Text =
                "<<< LOOK UP AREA CODES >>>" & Environment.NewLine & Environment.NewLine &
                "No matches found." & Environment.NewLine & Environment.NewLine &
                "Press ESC to return."
            Return
        End If

        ResetInlineMode()

        Using frm As New FrmPagedTextViewer()
            frm.SetPages(New List(Of String) From {svc.FormatGroupedResults(results)})
            frm.ShowDialog(Me)
        End Using

        txtInline.Clear()
    End Sub

    Private Sub OpenZipKey()
        Using frm As New FrmZipKeyPopup()
            frm.ShowDialog(Me)
        End Using
    End Sub

    Private Sub ResetInlineMode()
        _inlineMode = RolodexMenuInlineMode.None
        _areaCodeInput = ""
        _printPhoneBookLetterInput = ""
        _printLabelsStartLetterInput = ""
        txtInline.Clear()
    End Sub

End Class
