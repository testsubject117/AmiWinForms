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

    ' --- Scrolling message (Option V) and subliminal flash (Option H) ---
    Private lblScrollingMessage As Label = Nothing
    Private _scrollingMessagePanel As Panel = Nothing
    Private tmrScrollingMessage As Timer = Nothing
    Private _scrollingMessageText As String = ""
    Private _scrollingMessagePosition As Integer = -250
    Private _scrollingMessagePadded As String = ""
    Private _paintDebugCount As Integer = 0

    Private lblSubliminalFlash As Label = Nothing
    Private tmrSubliminalFlash As Timer = Nothing
    Private _subliminalFlashText As String = ""
    Private _subliminalFlashCounter As Integer = 0

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

        ' Initialize scrolling message and subliminal flash
        InitializeScrollingMessage()
        InitializeSubliminalFlash()

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

        ' Left column (15 buttons)
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
        AddMenuButton(flpLeft, "O", "Copy Spec Index")

        ' Right column (15 buttons)
        AddMenuButton(flpRight, "P", "Entire Ledger Viewing")
        AddMenuButton(flpRight, "Q", "Word Processor")
        AddMenuButton(flpRight, "R", "Find Word Processor Text")
        AddMenuButton(flpRight, "T", "Change Date or Time")
        AddMenuButton(flpRight, "V", "Change Main Menu Message")
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
                Using f As New FormPriceList()
                    f.ShowDialog(Me)
                End Using

            Case "G"
                NotYet("Print/Void Invoices (BOOT)")

            Case "H"
                Using f As New FormQuickMessageFlashing()
                    If f.ShowDialog(Me) = DialogResult.OK Then
                        ' Reload subliminal message if changed
                        ReloadSubliminalMessage()
                    End If
                End Using

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
                Using f As New FormCopySpecIndex()
                    f.ShowDialog(Me)
                End Using

            Case "P"
                NotYet("Entire Ledger Viewing (ENTIRE)")

            Case "Q"
                NotYet("Word Processor")

            Case "R"
                Using promptForm As New FormWordProcessorSearch()
                    If promptForm.ShowDialog(Me) = DialogResult.OK Then
                        Dim searchTerm As String = promptForm.SearchTerm

                        If Not String.IsNullOrWhiteSpace(searchTerm) Then
                            Try
                                Dim searchSvc As New WordProcessorSearchService(LegacyDataPaths.WordDocDir)

                                If Not searchSvc.DirectoryExists() Then
                                    MessageBox.Show("Word processor directory not found:" & Environment.NewLine & LegacyDataPaths.WordDocDir,
                                                  "Find Word Processor Text",
                                                  MessageBoxButtons.OK,
                                                  MessageBoxIcon.Warning)
                                    Exit Select
                                End If

                                ' Show progress form
                                Using progressForm As New FormTextSearchProgress()
                                    progressForm.Show(Me)

                                    ' Interactive search with callbacks (mimics DOS TS.COM)
                                    Dim matchCount As Integer = searchSvc.SearchFilesInteractive(
                                        searchTerm,
                                        Sub(fileName)
                                            ' Report file being searched
                                            progressForm.AddSearchingFile(fileName)
                                        End Sub,
                                        Function(fileName, lineNumber, matchingLine) As Boolean
                                            ' Match found - pause and ask user
                                            progressForm.Hide()
                                            Using matchForm As New FormTextSearchMatch(fileName, lineNumber, matchingLine, searchTerm)
                                                Dim result As DialogResult = matchForm.ShowDialog(Me)
                                                progressForm.Show()
                                                Return result = DialogResult.Yes
                                            End Using
                                        End Function
                                    )

                                    progressForm.Close()

                                    ' Search complete
                                    If matchCount = 0 Then
                                        MessageBox.Show("No matches found for: " & searchTerm,
                                                      "Text Search Complete",
                                                      MessageBoxButtons.OK,
                                                      MessageBoxIcon.Information)
                                    End If
                                End Using

                            Catch ex As Exception
                                MessageBox.Show("Search failed:" & Environment.NewLine & ex.Message,
                                              "Find Word Processor Text",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Error)
                            End Try
                        End If
                    End If
                End Using

            Case "T"
                Using f As New FormDateTimeChange()
                    f.ShowDialog(Me)
                End Using

            Case "V"
                Using f As New FormChangeMenuMessage()
                    If f.ShowDialog(Me) = DialogResult.OK Then
                        ' Reload scrolling message if changed
                        ReloadScrollingMessage()
                    End If
                End Using

            Case "X"
                Using f As New FormTypewriterMode()
                    f.ShowDialog(Me)
                End Using

            Case "Y"
                Using f As New FormPersonalBackup()
                    f.ShowDialog(Me)
                End Using

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
                Using f As New FormLogBookEntry()
                    f.ShowDialog(Me)
                End Using

            Case "6"
                Using f As New FormCadmiumCards()
                    f.ShowDialog(Me)
                End Using

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

        ' Ensure prompt container is at the very bottom, below scrolling message
        pnlPromptContainer.SendToBack()
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

    ' -------------------------------
    ' Scrolling Message (Option V) - DOS line 700/730
    ' -------------------------------
    Private Sub InitializeScrollingMessage()
        ' Remove any existing scrolling message controls first
        Dim toRemove As New List(Of Control)
        For Each ctrl As Control In Me.Controls
            If TypeOf ctrl Is Panel AndAlso ctrl.BackColor = Color.White AndAlso ctrl.Dock = DockStyle.Bottom Then
                toRemove.Add(ctrl)
                Debug.WriteLine($"InitScrollMsg: Removing old panel at z-index {Me.Controls.GetChildIndex(ctrl)}")
            End If
        Next
        For Each ctrl In toRemove
            Me.Controls.Remove(ctrl)
            ctrl.Dispose()
        Next

        ' Create a custom panel for the scrolling message with proper layering
        Dim scrollPanel As New Panel() With {
            .Height = 22,
            .Dock = DockStyle.Bottom,
            .BackColor = Color.White,
            .Padding = New Padding(0),
            .Margin = New Padding(0)
        }

        ' Enable double buffering on the panel
        SetDoubleBuffered(scrollPanel)

        ' Store panel reference so we can invalidate it
        _scrollingMessagePanel = scrollPanel

        ' Create label (just for storing text, not for display)
        lblScrollingMessage = New Label() With {
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular),
            .Text = ""
        }

        ' Use custom paint to draw text at exact X position (no spaces, just offset)
        AddHandler scrollPanel.Paint, Sub(s As Object, e As PaintEventArgs)
            If Not String.IsNullOrEmpty(_scrollingMessagePadded) Then
                _paintDebugCount += 1

                ' Calculate character width
                Dim charWidth As Single = e.Graphics.MeasureString("M", lblScrollingMessage.Font).Width

                ' Calculate X offset: negative position means off-screen to the right
                ' When position = -100, text should start at panel.Width (far right)
                ' When position = 0, text should start at X=0 (far left)
                ' When position = 50, text should be scrolled left 50 characters
                Dim xOffset As Single = -(_scrollingMessagePosition * charWidth)

                If _paintDebugCount <= 5 Then
                    Debug.WriteLine($"PAINT {_paintDebugCount}: pos={_scrollingMessagePosition}, charWidth={charWidth:F1}, xOffset={xOffset:F1}, panelWidth={scrollPanel.Width}")
                End If

                TextRenderer.DrawText(e.Graphics, _scrollingMessagePadded, lblScrollingMessage.Font, New Point(CInt(xOffset), 2), Color.Black, Color.White, TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine)
            End If
        End Sub

        Debug.WriteLine($"InitScrollMsg: Panel created with custom paint handler")

        Me.Controls.Add(scrollPanel)
        scrollPanel.BringToFront()

        ' Force immediate layout so sizes are correct
        scrollPanel.PerformLayout()
        Me.PerformLayout()

        Debug.WriteLine($"InitScrollMsg: Panel added at z-index {Me.Controls.GetChildIndex(scrollPanel)}, Panel.Width={scrollPanel.Width}, Form.ClientWidth={Me.ClientSize.Width}")

        ' Load message - will calculate proper start position after layout
        ReloadScrollingMessage()

        tmrScrollingMessage = New Timer() With {
            .Interval = 80
        }
        AddHandler tmrScrollingMessage.Tick, AddressOf tmrScrollingMessage_Tick

        ' Delay start to ensure label is properly sized
        Dim startTimer As New Timer() With {.Interval = 100}
        AddHandler startTimer.Tick, Sub()
            startTimer.Stop()
            startTimer.Dispose()
            Debug.WriteLine($"InitScrollMsg DELAYED: Panel.Width={scrollPanel.Width}, Form.ClientWidth={Me.ClientSize.Width}")
            CalculateStartPosition()
            tmrScrollingMessage.Start()
        End Sub
        startTimer.Start()
    End Sub

    Private Sub CalculateStartPosition()
        If lblScrollingMessage IsNot Nothing Then
            Try
                Using g As Graphics = lblScrollingMessage.CreateGraphics()
                    Dim charWidth As Single = g.MeasureString("M", lblScrollingMessage.Font).Width
                    ' Use form width instead of label width
                    Dim visibleChars As Integer = Math.Max(1, CInt(Me.ClientSize.Width / charWidth))
                    ' Start with message completely off-screen to the right
                    ' Position should be NEGATIVE and LARGER than visibleChars so it starts off-screen
                    _scrollingMessagePosition = -(visibleChars + 20)
                    Debug.WriteLine($"ScrollMsg: Form width={Me.ClientSize.Width}, Label width={lblScrollingMessage.Width}, charWidth={charWidth}, visibleChars={visibleChars}, startPos={_scrollingMessagePosition}")
                End Using
            Catch ex As Exception
                _scrollingMessagePosition = -200
                Debug.WriteLine($"ScrollMsg: Error calculating, using default -200: {ex.Message}")
            End Try
        Else
            _scrollingMessagePosition = -200
            Debug.WriteLine("ScrollMsg: Label is Nothing, using default -200")
        End If
    End Sub

    Private Sub SetDoubleBuffered(ctrl As Control)
        ' Enable double buffering to reduce flicker
        Try
            Dim prop As System.Reflection.PropertyInfo = GetType(Control).GetProperty("DoubleBuffered", 
                System.Reflection.BindingFlags.Instance Or System.Reflection.BindingFlags.NonPublic)
            If prop IsNot Nothing Then
                prop.SetValue(ctrl, True, Nothing)
            End If
        Catch
            ' Ignore if we can't set it
        End Try
    End Sub

    Private Sub ReloadScrollingMessage()
        _scrollingMessageText = MessageService.ReadScrollingMessage()

        If String.IsNullOrEmpty(_scrollingMessageText) Then
            _scrollingMessagePadded = "Active Magnetic Inspection's computerized office system.       By: Dean Beiner       (C)opyright 1989-1993"
        Else
            _scrollingMessagePadded = _scrollingMessageText
        End If

        ' Add trailing spaces for clean separation between loops
        ' Using 30 spaces for reasonable gap between message repeats
        _scrollingMessagePadded = _scrollingMessagePadded & New String(" "c, 30)

        ' Recalculate start position when message changes
        CalculateStartPosition()
    End Sub

    Private Sub tmrScrollingMessage_Tick(sender As Object, e As EventArgs)
        If _scrollingMessagePanel Is Nothing OrElse String.IsNullOrEmpty(_scrollingMessagePadded) Then
            Return
        End If

        Try
            ' Calculate how many characters fit based on FORM CLIENT width
            Using g As Graphics = _scrollingMessagePanel.CreateGraphics()
                Dim charWidth As Single = g.MeasureString("M", lblScrollingMessage.Font).Width
                Dim visibleChars As Integer = Math.Max(1, CInt(Me.ClientSize.Width / charWidth))

                ' DOS behavior: scroll left character-by-character
                _scrollingMessagePosition += 1

                ' Reset when message has completely scrolled off the left
                If _scrollingMessagePosition > _scrollingMessagePadded.Length + visibleChars Then
                    _scrollingMessagePosition = -visibleChars - 5
                    Debug.WriteLine($"ScrollMsg RESET: visibleChars={visibleChars}, newPos={_scrollingMessagePosition}")
                End If

                ' Trigger repaint
                _scrollingMessagePanel.Invalidate()
            End Using
        Catch ex As Exception
            Debug.WriteLine($"ScrollMsg ERROR: {ex.Message}")
        End Try
    End Sub

    ' -------------------------------
    ' Subliminal Flash (Option H) - DOS line 705
    ' -------------------------------
    Private Sub InitializeSubliminalFlash()
        ' Create label at top-right of form for subliminal flash
        ' DOS behavior: LOCATE 1,34 (line 1, column 34)
        ' Subliminal message - use matching colors so it's nearly invisible
        lblSubliminalFlash = New Label() With {
            .AutoSize = True,
            .Location = New Point(Me.ClientSize.Width - 350, 8),
            .BackColor = Color.Black,
            .ForeColor = Color.Black,
            .Font = New Font("Consolas", 9.0F, FontStyle.Regular),
            .TextAlign = ContentAlignment.MiddleRight,
            .Text = "",
            .Visible = False
        }
        Me.Controls.Add(lblSubliminalFlash)
        lblSubliminalFlash.BringToFront()

        ' Load message
        ReloadSubliminalMessage()

        ' Start flash timer - faster interval for true subliminal effect
        tmrSubliminalFlash = New Timer() With {
            .Interval = 50
        }
        AddHandler tmrSubliminalFlash.Tick, AddressOf tmrSubliminalFlash_Tick
        tmrSubliminalFlash.Start()
    End Sub

    Private Sub ReloadSubliminalMessage()
        _subliminalFlashText = MessageService.ReadSubliminalMessage()
        _subliminalFlashCounter = 0
    End Sub

    Private Sub tmrSubliminalFlash_Tick(sender As Object, e As EventArgs)
        If lblSubliminalFlash Is Nothing OrElse String.IsNullOrEmpty(_subliminalFlashText) Then
            Return
        End If

        ' DOS behavior: flash every 40 idle-loop cycles (line 705)
        _subliminalFlashCounter += 1

        If _subliminalFlashCounter = 40 Then
            ' Show the message briefly
            lblSubliminalFlash.Text = _subliminalFlashText
            lblSubliminalFlash.Visible = True
            _subliminalFlashCounter = 0
        ElseIf _subliminalFlashCounter = 1 Then
            ' Hide it immediately (subliminal effect)
            lblSubliminalFlash.Visible = False
        End If
    End Sub

End Class
