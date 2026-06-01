Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Partial Public Class FormMainMenu

    Private _buildInfo As String = ""

    ' --- DOS-style bottom prompt state (for J flow) ---
    Private _isActualNamesPromptActive As Boolean = False

    ' --- DOS-style bottom prompt state (for E flow) ---
    Private _isLogBookYearPromptActive As Boolean = False
    Private _logBookYearBuffer As String = ""
    Private _logBookDefaultYear As Integer = 0

    ' Remember AcceptButton so we can restore it after prompt mode
    Private _savedAcceptButton As IButtonControl = Nothing

    ' Outer container (gray) that sits at the bottom, and inner prompt (black)
    Private pnlPromptContainer As Panel = Nothing
    Private pnlPromptHost As Panel = Nothing
    Private lblPrompt As Label = Nothing

    ' Border color for the prompt box (yellow per request)
    Private _promptBorderColor As Color = Color.Yellow

    ' Border thickness for the prompt box (in pixels) - increased per request
    Private Const PromptBorderPx As Integer = 2

    ' How far above the bottom edge the black prompt sits (in pixels)
    ' Was 18; moved up another 5px -> 23
    Private Const PromptLiftPx As Integer = 23

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Me.KeyPreview = True

        BuildMenu()

        _buildInfo = "   " & BuildInfo.DisplayVersion
        Me.Text = "Active Magnetic Inspection Main Menu Application   " & BuildInfo.DisplayVersion

        lblMainMenu.Text = "MAIN MENU"
        UpdateHeaderClock()
        tmrClock.Interval = 1000
        tmrClock.Start()

        lblDateTime.TextAlign = ContentAlignment.MiddleLeft
        lblDateTime.Padding = New Padding(0, 10, 0, 0)
        lblDateTime.Margin = New Padding(0)

        Me.BeginInvoke(New Action(Sub()
                                      ResizeButtonsToPanel(flpLeft)
                                      ResizeButtonsToPanel(flpRight)
                                  End Sub))
    End Sub

    ' Catch Enter/Esc even when a Button has focus (most reliable).
    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If _isLogBookYearPromptActive Then
            If keyData = Keys.Escape Then
                StopLogBookYearPrompt()
                Return True
            End If

            If keyData = Keys.Enter Then
                AcceptLogBookYearPrompt()
                Return True
            End If
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub flpLeft_SizeChanged(sender As Object, e As EventArgs) Handles flpLeft.SizeChanged
        ResizeButtonsToPanel(flpLeft)
    End Sub

    Private Sub flpRight_SizeChanged(sender As Object, e As EventArgs) Handles flpRight.SizeChanged
        ResizeButtonsToPanel(flpRight)
    End Sub

    Private Sub tmrClock_Tick(sender As Object, e As EventArgs) Handles tmrClock.Tick
        UpdateHeaderClock()
    End Sub

    Private Sub UpdateHeaderClock()
        Dim now As DateTime = DateTime.Now

        lblDateTime.Text = _buildInfo & "          " &
                       now.ToString("dddd") & "  " &
                       now.ToString("MM-dd-yyyy") & "          " &
                       now.ToString("hh:mm:ss tt")
    End Sub

    Private Sub BuildMenu()
        If flpLeft Is Nothing OrElse flpRight Is Nothing Then Return

        flpLeft.Controls.Clear()
        flpRight.Controls.Clear()

        ' Left column
        AddMenuButton(flpLeft, "A", "Shop Card Generator")
        AddMenuButton(flpLeft, "B", "Invoice Generator")
        AddMenuButton(flpLeft, "C", "Checks and Cash Receipts")
        AddMenuButton(flpLeft, "D", "View Sales Journal")
        AddMenuButton(flpLeft, "E", "View Log Book")
        AddMenuButton(flpLeft, "F", "Price List Program")
        AddMenuButton(flpLeft, "G", "Print Records / Void Invoices")
        AddMenuButton(flpLeft, "H", "Quick Message Flashing")
        AddMenuButton(flpLeft, "I", "Backup Price List & Rolodex")
        AddMenuButton(flpLeft, "J", "Print Out Customers Actual Names")
        AddMenuButton(flpLeft, "K", "Cash Disbursements")
        AddMenuButton(flpLeft, "L", "Business Expenses Account")
        AddMenuButton(flpLeft, "M", "Quotation Form Generator")
        AddMenuButton(flpLeft, "N", "Rolodex")

        ' Right column
        AddMenuButton(flpRight, "O", "Copy Spec Index")
        AddMenuButton(flpRight, "P", "Entire Ledger Viewing")
        AddMenuButton(flpRight, "Q", "Word Processor")
        AddMenuButton(flpRight, "R", "Find Word Processor Text")
        AddMenuButton(flpRight, "T", "Change Date or Time")
        AddMenuButton(flpRight, "X", "Typewriter Mode")
        AddMenuButton(flpRight, "Y", "Ed Dean's Personal Backup")
        AddMenuButton(flpRight, "Z", "Personal Calendar")
        AddMenuButton(flpRight, "1", "Mileage Tracking")
        AddMenuButton(flpRight, "2", "Product Purchasing")
        AddMenuButton(flpRight, "3", "Miscellaneous Menu")
        AddMenuButton(flpRight, "4", "Add Entries to Log Book")
        AddMenuButton(flpRight, "6", "Cadmium Cards")
        AddMenuButton(flpRight, "7", "Emergency PAYROLL System")
        AddMenuButton(flpRight, "?", "About AMiOffice Menu System")
    End Sub

    Private Sub AddMenuButton(panel As FlowLayoutPanel, key As String, text As String)
        Dim btn As New Button()

        btn.AutoSize = False
        btn.Height = 36
        btn.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        btn.TextAlign = ContentAlignment.MiddleLeft
        btn.Text = "(" & key & ") " & text
        btn.Tag = key
        btn.Margin = New Padding(3, 3, 3, 6)

        btn.UseVisualStyleBackColor = False
        btn.BackColor = Color.Black
        btn.ForeColor = Color.White
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderColor = Color.DimGray
        btn.FlatAppearance.BorderSize = 1
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(32, 32, 32)
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(64, 64, 64)
        btn.AutoEllipsis = True

        AddHandler btn.Click, Sub(sender, args) HandleMenuKey(CStr(btn.Tag))

        panel.Controls.Add(btn)
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        ResizeButtonsToPanel(flpLeft)
        ResizeButtonsToPanel(flpRight)

        If pnlPromptHost IsNot Nothing AndAlso pnlPromptHost.Visible Then
            pnlPromptHost.Invalidate()
        End If

        If pnlPromptContainer IsNot Nothing AndAlso pnlPromptContainer.Visible Then
            LayoutPromptWithinContainer()
        End If
    End Sub

    Private Sub ResizeButtonsToPanel(panel As FlowLayoutPanel)
        If panel Is Nothing Then Return

        Dim targetWidth As Integer =
            panel.ClientSize.Width -
            panel.Padding.Left - panel.Padding.Right -
            SystemInformation.VerticalScrollBarWidth - 6

        If targetWidth < 150 Then targetWidth = 150

        For Each c As Control In panel.Controls
            Dim btn = TryCast(c, Button)
            Dim lbl = TryCast(c, Label)

            If btn IsNot Nothing Then
                btn.Width = targetWidth
            ElseIf lbl IsNot Nothing Then
                lbl.Width = targetWidth
            End If
        Next
    End Sub

    Private Function ShouldIgnoreKeyPress() As Boolean
        ' If another form (like the Log Book dialog) currently has focus,
        ' do NOT process menu keystrokes here.
        Try
            For Each f As Form In Application.OpenForms
                If f Is Nothing Then Continue For
                If Object.ReferenceEquals(f, Me) Then Continue For
                If Not f.Visible Then Continue For

                ' If that other form (or one of its controls) has focus, ignore.
                If f.ContainsFocus Then
                    Return True
                End If
            Next
        Catch
            ' If anything goes weird, fail open (do not ignore).
        End Try

        Return False
    End Function

    Private Sub FormMainMenu_KeyPress(sender As Object, e As KeyPressEventArgs) Handles Me.KeyPress
        ' CRITICAL: don't let main menu keystrokes fire while a modal child dialog is active.
        If ShouldIgnoreKeyPress() Then
            Return
        End If

        Dim ch As Char = e.KeyChar

        ' --- E year prompt mode: digits/backspace here; Enter/Esc handled in ProcessCmdKey ---
        If _isLogBookYearPromptActive Then
            e.Handled = True

            If ch = ChrW(Keys.Back) Then
                If _logBookYearBuffer.Length > 0 Then
                    _logBookYearBuffer = _logBookYearBuffer.Substring(0, _logBookYearBuffer.Length - 1)
                    RefreshLogBookYearPromptLine()
                End If
                Return
            End If

            If Char.IsDigit(ch) Then
                If _logBookYearBuffer.Length < 4 Then
                    _logBookYearBuffer &= ch
                    RefreshLogBookYearPromptLine()
                End If
                Return
            End If

            ' Ignore everything else while prompting
            Return
        End If

        ' Normal main menu behavior:
        Dim s As String = ch.ToString()
        If s = vbCr OrElse s = vbLf Then Return
        HandleMenuKey(s)
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)

        If _isActualNamesPromptActive AndAlso e.KeyCode = Keys.Escape Then
            StopActualNamesPrompt()
            e.Handled = True
            Return
        End If
    End Sub

    Private Sub HandleMenuKey(ch As String)
        If String.IsNullOrEmpty(ch) Then Return

        Dim up As String = ch
        If up.Length = 1 AndAlso Char.IsLetter(up(0)) Then
            up = up.ToUpperInvariant()
        End If

        ' If we're in the DOS-style J prompt mode, only P/Q matter.
        If _isActualNamesPromptActive Then
            Select Case up
                Case "P"
                    StopActualNamesPrompt()
                    OpenActualCustomerNames()
                    Return

                Case "Q"
                    StopActualNamesPrompt()
                    Return

                Case Else
                    Return
            End Select
        End If

        ' If we're in the DOS-style E year prompt mode, ignore all menu keys.
        If _isLogBookYearPromptActive Then
            Return
        End If

        Select Case up
            Case "A"
                NotYet("Shop Card Generator")

            Case "B"
                NotYet("Invoice Generator")

            Case "C"
                Using f As New FormLedgerMenu()
                    f.ShowDialog(Me)
                End Using

            Case "D"
                NotYet("View Sales Journal (SALES)")

            Case "E"
                StartLogBookYearPrompt()

            Case "F"
                NotYet("Price List Program (plist)")

            Case "G"
                NotYet("Print/Void Invoices (BOOT)")

            Case "H"
                NotYet("Quick Message Flashing")

            Case "I"
                Try
                    Cursor = Cursors.WaitCursor
                    MigrationService.EnsureFoldersAndMigrateOnce()
                    BackupService.RunBackupPriceListAndRolodex()
                    MessageBox.Show("Backup complete." & Environment.NewLine & AppPaths.BackupDir,
                                    "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show("Backup failed: " & ex.Message,
                                    "Backup", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Finally
                    Cursor = Cursors.Default
                End Try

            Case "J"
                StartActualNamesPrompt()

            Case "K"
                NotYet("Cash Disbursements (BILL)")

            Case "L"
                NotYet("Business Expenses Account (password)")

            Case "M"
                NotYet("Quotation Form Generator (QUOTE)")

            Case "N"
                Using f As New FrmRolodexMenu()
                    f.ShowDialog(Me)
                End Using

            Case "O"
                NotYet("Copy Spec Index")

            Case "P"
                NotYet("Entire Ledger Viewing (ENTIRE)")

            Case "Q"
                NotYet("Word Processor")

            Case "R"
                NotYet("Find Word Processor Text")

            Case "T"
                NotYet("Change Date or Time")

            Case "X"
                NotYet("Typewriter Mode")

            Case "Y"
                NotYet("Ed Dean's Personal Backup")

            Case "Z"
                Using f As New FormPersonalCalendar()
                    f.ShowDialog(Me)
                End Using

            Case "1"
                Using f As New FormMileageTracking()
                    f.ShowDialog(Me)
                End Using

            Case "2"
                NotYet("Product Purchasing")

            Case "3"
                Using f As New FormMiscMenu()
                    f.ShowDialog(Me)
                End Using

            Case "4"
                NotYet("Add Entries to Log Book")

            Case "6"
                NotYet("Cadmium Cards")

            Case "7"
                NotYet("Emergency PAYROLL System")

            Case "?"
                Using f As New FormAbout()
                    f.ShowDialog(Me)
                End Using

            Case Else
                ' ignore unknown keys
        End Select
    End Sub

    ' -------------------------------
    ' DOS-style E (Log Book year) prompt
    ' -------------------------------
    Private Sub StartLogBookYearPrompt()
        _isLogBookYearPromptActive = True
        _logBookYearBuffer = ""
        _logBookDefaultYear = DateTime.Now.Year

        ' Prevent Enter from being treated like "click focused button"
        _savedAcceptButton = Me.AcceptButton
        Me.AcceptButton = Nothing
        Me.ActiveControl = Nothing

        _promptBorderColor = Color.Yellow
        RefreshLogBookYearPromptLine()
    End Sub

    Private Sub RefreshLogBookYearPromptLine()
        Dim prompt As String =
            "What year do you want to use [Enter = " & _logBookDefaultYear.ToString() & "]? " &
            _logBookYearBuffer

        ShowBottomPrompt(prompt)
    End Sub

    Private Sub StopLogBookYearPrompt()
        _isLogBookYearPromptActive = False
        _logBookYearBuffer = ""

        ' Restore AcceptButton behavior
        Me.AcceptButton = _savedAcceptButton
        _savedAcceptButton = Nothing

        HideBottomPrompt()
    End Sub

    Private Sub AcceptLogBookYearPrompt()
        Dim chosenYear As Integer = _logBookDefaultYear

        If Not String.IsNullOrWhiteSpace(_logBookYearBuffer) Then
            Dim n As Integer
            If Integer.TryParse(_logBookYearBuffer, n) Then
                chosenYear = n
            End If
        End If

        Dim twoDigit As Integer
        If chosenYear >= 0 AndAlso chosenYear <= 99 Then
            twoDigit = chosenYear
        Else
            twoDigit = chosenYear Mod 100
        End If

        StopLogBookYearPrompt()

        Try
            Using f As New FormLogBookMenu(twoDigit)
                f.ShowDialog(Me)
            End Using
        Catch ex As Exception
            MessageBox.Show("Unable to open Log Book:" & Environment.NewLine & ex.Message,
                            "LOG BOOK",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub NotYet(feature As String)
        MessageBox.Show("Not implemented yet: " & feature, "Port status", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' -------------------------------
    ' DOS-style prompt bar (bottom)
    ' -------------------------------
    Private Sub EnsurePromptUi()
        If pnlPromptContainer IsNot Nothing Then Return

        pnlPromptContainer = New Panel() With {
            .Visible = False,
            .Height = 28 + (PromptLiftPx * 2),
            .Dock = DockStyle.Bottom,
            .BackColor = Color.FromArgb(45, 45, 45),
            .Padding = New Padding(0)
        }

        pnlPromptHost = New Panel() With {
            .Visible = True,
            .Height = 28,
            .BackColor = Color.Black,
            .Padding = New Padding(10, 3, 10, 3)
        }

        lblPrompt = New Label() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = New Font("Consolas", 12.0F, FontStyle.Bold, GraphicsUnit.Point),
            .AutoEllipsis = True
        }

        pnlPromptHost.Controls.Add(lblPrompt)
        pnlPromptContainer.Controls.Add(pnlPromptHost)
        Me.Controls.Add(pnlPromptContainer)

        pnlPromptContainer.BringToFront()
        LayoutPromptWithinContainer()

        AddHandler pnlPromptHost.Paint, AddressOf pnlPromptHost_Paint
    End Sub

    Private Sub LayoutPromptWithinContainer()
        If pnlPromptContainer Is Nothing OrElse pnlPromptHost Is Nothing Then Return

        pnlPromptHost.Left = 0
        pnlPromptHost.Width = pnlPromptContainer.ClientSize.Width
        pnlPromptHost.Top = PromptLiftPx
    End Sub

    Private Sub pnlPromptHost_Paint(sender As Object, e As PaintEventArgs)
        Dim p As Panel = DirectCast(sender, Panel)

        Using pen As New Pen(_promptBorderColor, CSng(PromptBorderPx))
            Dim inset As Integer = CInt(Math.Ceiling(PromptBorderPx / 2.0R))
            Dim r As Rectangle = p.ClientRectangle
            r.X += inset
            r.Y += inset
            r.Width -= (inset * 2) + 1
            r.Height -= (inset * 2) + 1

            If r.Width > 0 AndAlso r.Height > 0 Then
                e.Graphics.DrawRectangle(pen, r)
            End If
        End Using
    End Sub

    Private Sub ShowBottomPrompt(text As String)
        EnsurePromptUi()

        lblPrompt.Text = text
        pnlPromptContainer.Visible = True
        pnlPromptContainer.BringToFront()
        LayoutPromptWithinContainer()
        pnlPromptHost.Invalidate()
    End Sub

    Private Sub HideBottomPrompt()
        If pnlPromptContainer Is Nothing Then Return
        pnlPromptContainer.Visible = False
        If lblPrompt IsNot Nothing Then lblPrompt.Text = ""
    End Sub

    Private Sub StartActualNamesPrompt()
        _isActualNamesPromptActive = True
        _promptBorderColor = Color.Yellow
        ShowBottomPrompt("Push P to Print Out Actual Names, or Q to Quit")
    End Sub

    Private Sub StopActualNamesPrompt()
        _isActualNamesPromptActive = False
        HideBottomPrompt()
    End Sub

    ' -------------------------------
    ' Existing J action (invoked by P)
    ' -------------------------------
    Private Sub OpenActualCustomerNames()
        Try
            Dim realNamePath As String = System.IO.Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.DAT")
            Dim svc As New ActualCustomerNamesService(realNamePath)

            If Not svc.DataFileExists() Then
                MessageBox.Show("REALNAME.DAT was not found:" & Environment.NewLine & realNamePath,
                                "Actual Customer Names",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning)
                Return
            End If

            Dim text As String = svc.BuildDisplayText()

            Using f As New FrmPagedTextViewer()
                f.SetPages(New List(Of String) From {text})
                f.ShowDialog(Me)
            End Using

        Catch ex As Exception
            MessageBox.Show("Unable to load Actual Customer Names:" & Environment.NewLine & ex.Message,
                            "Actual Customer Names",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Try
    End Sub

End Class