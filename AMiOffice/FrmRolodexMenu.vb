Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms

Public Class FrmRolodexMenu
    Inherits Form

    Private _inlineMode As RolodexMenuInlineMode = RolodexMenuInlineMode.None
    Private _areaCodeInput As String = ""

    Private lblTitle As Label
    Private lblDate As Label
    Private lblMenu As Label
    Private txtInline As TextBox

    Public Sub New()
        MyBase.New()
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
        If _inlineMode = RolodexMenuInlineMode.AreaCodes_Prompt Then
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
                _inlineMode = RolodexMenuInlineMode.None
                _areaCodeInput = ""
                txtInline.Clear()
                e.SuppressKeyPress = True
                Return
            End If
        End If

        If e.KeyCode = Keys.Escape Then
            If _inlineMode <> RolodexMenuInlineMode.None Then
                _inlineMode = RolodexMenuInlineMode.None
                _areaCodeInput = ""
                txtInline.Clear()
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
                    txtInline.Text =
                        "E" & Environment.NewLine & Environment.NewLine &
                        "Do you want to print your customers only (Y/N) ? " & If(up = "Y"c, "Yes", "No") &
                        Environment.NewLine & Environment.NewLine &
                        "Do you want High Quality but slow print (Y/N) ?"
                    _inlineMode = RolodexMenuInlineMode.PrintPhoneBook_HighQuality
                    Return True
                End If

            Case RolodexMenuInlineMode.PrintPhoneBook_HighQuality
                If up = "Y"c OrElse up = "N"c Then
                    Dim firstAnswer As String = ""
                    If txtInline.Text.Contains("customers only (Y/N) ? Yes") Then
                        firstAnswer = "Yes"
                    Else
                        firstAnswer = "No"
                    End If

                    txtInline.Text =
                        "E" & Environment.NewLine & Environment.NewLine &
                        "Do you want to print your customers only (Y/N) ? " & firstAnswer &
                        Environment.NewLine & Environment.NewLine &
                        "Do you want High Quality but slow print (Y/N) ? " & If(up = "Y"c, "Yes", "No") &
                        Environment.NewLine & Environment.NewLine &
                        "Enter the 1st letter of the people you want to print (Enter = All) (Esc = Quit)"
                    _inlineMode = RolodexMenuInlineMode.PrintPhoneBook_FirstLetter
                    Return True
                End If

            Case RolodexMenuInlineMode.PrintPhoneBook_FirstLetter
                txtInline.Text &= Environment.NewLine &
                    "PRINTER IS NOT READY!, Get it ready and hit [ENTER]?"
                _inlineMode = RolodexMenuInlineMode.None
                Return True

            Case RolodexMenuInlineMode.PrintLabels_CustomersOnly
                If up = "Y"c OrElse up = "N"c Then
                    txtInline.Text =
                        "F" & Environment.NewLine & Environment.NewLine &
                        "Do you want to print your customers only (Y/N) ? " & If(up = "Y"c, "Yes", "No") &
                        Environment.NewLine & Environment.NewLine &
                        "Enter 1st Letter to start with [Enter = A]"
                    _inlineMode = RolodexMenuInlineMode.PrintLabels_FirstLetter
                    Return True
                End If

            Case RolodexMenuInlineMode.PrintLabels_FirstLetter
                txtInline.Text &= Environment.NewLine &
                    "PRINTER IS NOT READY!, Get it ready and hit [ENTER]?"
                _inlineMode = RolodexMenuInlineMode.None
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
        _inlineMode = RolodexMenuInlineMode.PrintPhoneBook_CustomersOnly
        txtInline.Text =
            "E" & Environment.NewLine & Environment.NewLine &
            "Do you want to print your customers only (Y/N) ?"
    End Sub

    Private Sub StartPrintLabelsFlow()
        _inlineMode = RolodexMenuInlineMode.PrintLabels_CustomersOnly
        txtInline.Text =
            "F" & Environment.NewLine & Environment.NewLine &
            "Do you want to print your customers only (Y/N) ?"
    End Sub

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
            _inlineMode = RolodexMenuInlineMode.None
            _areaCodeInput = ""
            txtInline.Clear()
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
            _inlineMode = RolodexMenuInlineMode.None
            _areaCodeInput = ""

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

        _inlineMode = RolodexMenuInlineMode.None
        _areaCodeInput = ""

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
End Class