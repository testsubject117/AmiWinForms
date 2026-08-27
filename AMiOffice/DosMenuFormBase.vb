Option Strict Off
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

Public Class DosMenuFormBase
    Inherits Form

    Protected ReadOnly lblMainMenu As New Label()
    Protected ReadOnly lblDateTime As New Label()
    Protected ReadOnly tmrClock As New Timer()
    Private _lblInfoDate As Label = Nothing
    Private _lblInfoTime As Label = Nothing

    Protected ReadOnly flpLeft As New FlowLayoutPanel()
    Protected ReadOnly flpRight As New FlowLayoutPanel()

    ' NEW: bottom bar + close button
    Private ReadOnly pnlBottom As New Panel()
    Private ReadOnly btnEscClose As New Button()

    ' Header behavior
    Protected Property ShowVersionInHeader As Boolean = False

    ' Button sizing behavior (default matches Main Menu: stretch to panel width)
    Protected Property StretchButtonsToPanelWidth As Boolean = True
    Protected Property ButtonFixedWidthPx As Integer = 620

    ' Theme colors
    Private ReadOnly _bgDarkGray As Color = Color.FromArgb(32, 32, 32)
    Private ReadOnly _bgBlack As Color = Color.Black

    Private _displayVersion As String = ""
    Private _keyHandlers As New Dictionary(Of String, Action)(StringComparer.OrdinalIgnoreCase)

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Me.KeyPreview = True
        Me.BackColor = _bgDarkGray
        Me.ForeColor = Color.White
        Me.StartPosition = FormStartPosition.CenterParent

        _displayVersion = ""
        Try
            _displayVersion = BuildInfo.DisplayVersion
        Catch
            _displayVersion = ""
        End Try

        BuildLayout()
        UpdateHeaderClock()

        tmrClock.Interval = 1000
        AddHandler tmrClock.Tick, Sub() UpdateHeaderClock()
        tmrClock.Start()

        AddHandler Me.KeyPress, AddressOf OnBaseKeyPress
        AddHandler flpLeft.SizeChanged, Sub() ResizeButtonsToPanel(flpLeft)
        AddHandler flpRight.SizeChanged, Sub() ResizeButtonsToPanel(flpRight)

        Me.BeginInvoke(New Action(Sub()
                                      ResizeButtonsToPanel(flpLeft)
                                      ResizeButtonsToPanel(flpRight)
                                  End Sub))
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        Try
            tmrClock.Stop()
        Catch
        End Try
    End Sub

    Private Sub BuildLayout()
        ' Root host panel so we can Dock bottom bar and fill content above it
        Dim host As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = _bgDarkGray,
            .Padding = New Padding(12)
        }

        ' Bottom bar with small gray ESC Close at bottom-right
        pnlBottom.Dock = DockStyle.Bottom
        pnlBottom.Height = 32  ' Reduced from 35 to 32 to give more space for buttons
        pnlBottom.BackColor = _bgDarkGray
        pnlBottom.Padding = New Padding(0, 1, 0, 0)  ' Minimal top padding

        btnEscClose.Text = "(ESC) Close"
        btnEscClose.AutoSize = False
        btnEscClose.Width = 160
        btnEscClose.Height = 30
        btnEscClose.Anchor = AnchorStyles.Right Or AnchorStyles.Bottom
        btnEscClose.Left = pnlBottom.ClientSize.Width - btnEscClose.Width
        btnEscClose.Top = pnlBottom.Height - btnEscClose.Height
        btnEscClose.Margin = New Padding(0)

        ' Grey button styling (smaller than menu buttons)
        btnEscClose.UseVisualStyleBackColor = False
        btnEscClose.ForeColor = Color.White
        btnEscClose.FlatStyle = FlatStyle.Flat
        btnEscClose.BackColor = Color.Silver
        btnEscClose.ForeColor = Color.Black
        btnEscClose.FlatAppearance.BorderColor = Color.Gainsboro
        btnEscClose.FlatAppearance.MouseOverBackColor = Color.Gainsboro
        btnEscClose.FlatAppearance.MouseDownBackColor = Color.DarkGray
        btnEscClose.FlatAppearance.BorderSize = 1
        btnEscClose.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)

        AddHandler btnEscClose.Click, Sub() Me.Close()
        pnlBottom.Controls.Clear()
        pnlBottom.Controls.Add(btnEscClose)
        AddHandler pnlBottom.Resize,
            Sub()
                btnEscClose.Left = pnlBottom.ClientSize.Width - btnEscClose.Width
                btnEscClose.Top = pnlBottom.ClientSize.Height - btnEscClose.Height
            End Sub

        ' Main content (header + body) fills above bottom bar
        ' Header is a SINGLE panel so lines are never clipped by row boundaries.
        ' Layout (px from top of header panel):
        '   0-1   line 1 of top double-line
        '   4-5   line 2 of top double-line
        '   8-62  SHOPCARD GENERATOR title  (54px)
        '   64-65 line 1 of mid double-line
        '   68-69 line 2 of mid double-line
        '   72-94 date / customer / time bar (22px)
        '   96-97 line 1 of bottom double-line
        '  100-101 line 2 of bottom double-line
        ' Total header height = 102px
        Const HDR As Integer = 112

        Dim root As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .BackColor = _bgBlack,
            .Margin = New Padding(0)
        }
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, HDR))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

        ' ── Combined header panel ────────────────────────────────────────────────
        Dim pnlHeader As New Panel() With {.Dock = DockStyle.Fill, .BackColor = _bgBlack}

        Dim MkLine = Function(top As Integer) As Panel
                         Return New Panel() With {
                             .BackColor = Color.Yellow,
                             .Height = 1, .Left = 0, .Top = top,
                             .Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top
                         }
                     End Function

        Dim ln1 = MkLine(0)   ' top double-line
        Dim ln2 = MkLine(4)
        Dim ln3 = MkLine(64)  ' mid double-line (below title)
        Dim ln4 = MkLine(68)
        Dim ln5 = MkLine(96)  ' bottom double-line (below info bar)
        Dim ln6 = MkLine(100)

        AddHandler pnlHeader.Resize, Sub(s As Object, ev As EventArgs)
            Dim w As Integer = pnlHeader.Width
            For Each ln As Panel In {ln1, ln2, ln3, ln4, ln5, ln6}
                ln.Width = w
            Next
            lblMainMenu.Width = w - 16
        End Sub

        ' Title label — sits between top double-line and mid double-line
        lblMainMenu.AutoSize = False
        lblMainMenu.Dock = DockStyle.None
        lblMainMenu.Left = 8
        lblMainMenu.Top = 8
        lblMainMenu.Height = 54
        lblMainMenu.Width = 800
        UiTheme.ApplyDosTitleStyle(lblMainMenu)
        lblMainMenu.Font = New Font("Castellar", 32.0F, FontStyle.Bold, GraphicsUnit.Point)

        ' Info bar labels — sit between mid double-line and bottom double-line
        Dim lblDate As New Label() With {
            .AutoSize = False, .Top = 71, .Left = 6, .Height = 22,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point),
            .ForeColor = Color.Yellow, .BackColor = _bgBlack
        }
        lblDateTime.AutoSize = False
        lblDateTime.Dock = DockStyle.None
        lblDateTime.Top = 71
        lblDateTime.Height = 22
        lblDateTime.TextAlign = ContentAlignment.MiddleCenter
        lblDateTime.Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblDateTime.ForeColor = Color.Yellow
        lblDateTime.BackColor = _bgBlack

        Dim lblTime As New Label() With {
            .AutoSize = False, .Top = 71, .Height = 22,
            .TextAlign = ContentAlignment.MiddleRight,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point),
            .ForeColor = Color.Yellow, .BackColor = _bgBlack
        }

        AddHandler pnlHeader.Resize, Sub(s2 As Object, ev2 As EventArgs)
            Dim w As Integer = pnlHeader.Width
            lblDate.Width = w \ 4
            lblDateTime.Width = w \ 2
            lblDateTime.Left = w \ 4
            lblTime.Width = w \ 4
            lblTime.Left = w * 3 \ 4
        End Sub

        _lblInfoDate = lblDate
        _lblInfoTime = lblTime

        pnlHeader.Controls.AddRange({ln1, ln2, lblMainMenu, ln3, ln4,
                                     lblDate, lblDateTime, lblTime, ln5, ln6})

        ' ── Body (buttons) ──────────────────────────────────────────────────────
        Dim body As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .BackColor = _bgDarkGray
        }
        body.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        body.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))

        ConfigureMenuPanel(flpLeft, _bgDarkGray)
        ConfigureMenuPanel(flpRight, _bgDarkGray)

        body.Controls.Add(flpLeft, 0, 0)
        body.Controls.Add(flpRight, 1, 0)

        root.Controls.Add(pnlHeader, 0, 0)
        root.Controls.Add(body, 0, 1)

        host.Controls.Clear()
        host.Controls.Add(root)
        host.Controls.Add(pnlBottom)

        Controls.Clear()
        Controls.Add(host)

        ' Register ESC handler globally
        _keyHandlers("ESC") = Sub() Me.Close()
    End Sub

    Private Shared Sub ConfigureMenuPanel(panel As FlowLayoutPanel, bg As Color)
        panel.Dock = DockStyle.Fill
        panel.FlowDirection = FlowDirection.TopDown
        panel.WrapContents = False
        panel.AutoScroll = True
        panel.BackColor = bg
        panel.Padding = New Padding(0)
        panel.Margin = New Padding(0)  ' Reset to no margin - forms can override
    End Sub

    Protected Sub SetMenuTitle(title As String)
        lblMainMenu.Text = title
        Me.Text = title
    End Sub

    ' Customer name injected into the header clock line (DOS: "date    Customers name: X    time")
    Protected Property HeaderCustomerName As String = ""

    Protected Sub ClearMenu()
        flpLeft.Controls.Clear()
        flpRight.Controls.Clear()

        ' Keep ESC close handler
        Dim esc = _keyHandlers("ESC")
        _keyHandlers = New Dictionary(Of String, Action)(StringComparer.OrdinalIgnoreCase)
        _keyHandlers("ESC") = esc
    End Sub

    Protected Sub RegisterHotkey(key As String, handler As Action)
        If Not String.IsNullOrWhiteSpace(key) Then
            _keyHandlers(key) = handler
        End If
    End Sub

    Protected Sub AddMenuButton(panel As FlowLayoutPanel, key As String, text As String, handler As Action)
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

        AddHandler btn.Click, Sub() handler()

        panel.Controls.Add(btn)

        If Not String.IsNullOrWhiteSpace(key) Then
            _keyHandlers(key) = handler
        End If
    End Sub

    Protected Sub ResizeButtonsToPanel(panel As FlowLayoutPanel)
        If panel Is Nothing Then Return

        If StretchButtonsToPanelWidth Then
            Dim targetWidth As Integer =
                panel.ClientSize.Width -
                panel.Padding.Left - panel.Padding.Right -
                SystemInformation.VerticalScrollBarWidth - 6

            If targetWidth < 150 Then targetWidth = 150

            For Each c As Control In panel.Controls
                Dim btn = TryCast(c, Button)
                If btn IsNot Nothing Then btn.Width = targetWidth
            Next
        Else
            For Each c As Control In panel.Controls
                Dim btn = TryCast(c, Button)
                If btn IsNot Nothing Then btn.Width = ButtonFixedWidthPx
            Next
        End If
    End Sub

    Private Sub OnBaseKeyPress(sender As Object, e As KeyPressEventArgs)
        ' Don't process hotkeys if a text input control has focus
        If IsTextInputActive() Then
            Return
        End If

        ' ESC
        If e.KeyChar = ChrW(27) Then
            Dim escAct As Action = Nothing
            If _keyHandlers IsNot Nothing AndAlso _keyHandlers.TryGetValue("ESC", escAct) Then
                escAct()
            Else
                Me.Close()
            End If
            Return
        End If

        Dim ch As String = e.KeyChar.ToString()
        If ch = vbCr OrElse ch = vbLf Then Return

        Dim up As String = ch
        If up.Length = 1 AndAlso Char.IsLetter(up(0)) Then up = up.ToUpperInvariant()

        Dim act As Action = Nothing
        If _keyHandlers IsNot Nothing AndAlso _keyHandlers.TryGetValue(up, act) Then
            act()
        End If
    End Sub

    Protected Overridable Sub UpdateHeaderClock()
        Dim now As DateTime = DateTime.Now
        Dim datePart As String = now.ToString("MM-dd-yyyy")
        Dim timePart As String = now.ToString("HH:mm")

        If _lblInfoDate IsNot Nothing Then _lblInfoDate.Text = datePart
        If _lblInfoTime IsNot Nothing Then _lblInfoTime.Text = timePart

        If Not String.IsNullOrWhiteSpace(HeaderCustomerName) Then
            lblDateTime.Text = "Customers name: " & HeaderCustomerName
        Else
            lblDateTime.Text = ""
        End If
    End Sub

    ''' <summary>
    ''' Checks if a text input control currently has focus.
    ''' Returns True if the active control is a TextBox, RichTextBox, MaskedTextBox, or ComboBox.
    ''' This prevents hotkeys from firing while the user is typing in a text field.
    ''' </summary>
    Private Function IsTextInputActive() As Boolean
        Dim activeCtrl As Control = Me.ActiveControl

        ' Check if active control is a text input type
        If TypeOf activeCtrl Is TextBox OrElse
           TypeOf activeCtrl Is RichTextBox OrElse
           TypeOf activeCtrl Is MaskedTextBox OrElse
           TypeOf activeCtrl Is ComboBox Then
            Return True
        End If

        ' Check if active control is within a container that has a focused text input
        ' This handles cases where the TextBox is inside a Panel or other container
        If activeCtrl IsNot Nothing Then
            Dim focused As Control = FindFocusedControl(activeCtrl)
            If focused IsNot Nothing AndAlso focused IsNot activeCtrl Then
                If TypeOf focused Is TextBox OrElse
                   TypeOf focused Is RichTextBox OrElse
                   TypeOf focused Is MaskedTextBox OrElse
                   TypeOf focused Is ComboBox Then
                    Return True
                End If
            End If
        End If

        Return False
    End Function

    ''' <summary>
    ''' Recursively finds the deepest focused control within a container.
    ''' </summary>
    Private Function FindFocusedControl(parent As Control) As Control
        Dim container As ContainerControl = TryCast(parent, ContainerControl)
        If container IsNot Nothing AndAlso container.ActiveControl IsNot Nothing Then
            Return FindFocusedControl(container.ActiveControl)
        End If
        Return parent
    End Function

    Protected Sub NotYet(feature As String)
        DosMessageBox.Show(Me, "Not implemented yet: " & feature, "Port status", MessageBoxButtons.OK)
    End Sub

    ' ── Inline bottom prompt (DOS-style prompt below the menu) ───────────
    Private _hostPanel As Panel = Nothing
    Private _pnlInlinePrompt As Panel = Nothing
    Private _lblInlinePrompt As Label = Nothing
    Private _txtInlineInput As TextBox = Nothing
    Private _inlinePromptCallback As Action(Of String) = Nothing
    Private Const BottomBarNormalHeight As Integer = 32
    Private Const BottomBarExpandedHeight As Integer = 60

    Protected Sub ShowInlinePrompt(promptText As String, callback As Action(Of String))
        ShowInlinePromptCore(promptText, callback, False)
    End Sub

    Protected Sub ShowInlinePasswordPrompt(promptText As String, callback As Action(Of String))
        ShowInlinePromptCore(promptText, callback, True)
    End Sub

    Private Sub ShowInlinePromptCore(promptText As String, callback As Action(Of String), isPassword As Boolean)
        _inlinePromptCallback = callback

        If _pnlInlinePrompt Is Nothing Then
            _pnlInlinePrompt = New Panel() With {
                .Height = 28,
                .Left = 0,
                .BackColor = Color.Black,
                .Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Bottom
            }
            _lblInlinePrompt = New Label() With {
                .AutoSize = True,
                .ForeColor = Color.White,
                .BackColor = Color.Black,
                .Font = New Font("Courier New", 11.0F, FontStyle.Regular, GraphicsUnit.Point),
                .Location = New Point(4, 4)
            }
            _txtInlineInput = New TextBox() With {
                .BackColor = Color.Black,
                .ForeColor = Color.White,
                .Font = New Font("Courier New", 11.0F, FontStyle.Regular, GraphicsUnit.Point),
                .BorderStyle = BorderStyle.None,
                .MaxLength = 40
            }
            AddHandler _txtInlineInput.KeyDown, AddressOf InlineInputKeyDown
            _pnlInlinePrompt.Controls.Add(_lblInlinePrompt)
            _pnlInlinePrompt.Controls.Add(_txtInlineInput)
            ' Add to pnlBottom so it stays anchored correctly
            pnlBottom.Controls.Add(_pnlInlinePrompt)
        End If

        _lblInlinePrompt.Text = promptText
        _lblInlinePrompt.Location = New Point(4, 5)
        _txtInlineInput.Location = New Point(_lblInlinePrompt.PreferredWidth + 8, 5)
        _txtInlineInput.Width = pnlBottom.ClientSize.Width - _txtInlineInput.Left - 170  ' leave room for ESC btn
        _txtInlineInput.Text = ""
        _txtInlineInput.PasswordChar = If(isPassword, "*"c, ChrW(0))
        ' Expand pnlBottom to show the prompt row above the ESC button
        _pnlInlinePrompt.Top = 0
        _pnlInlinePrompt.Width = pnlBottom.ClientSize.Width
        pnlBottom.Height = BottomBarExpandedHeight
        _pnlInlinePrompt.Visible = True
        _txtInlineInput.Focus()
    End Sub

    Private Sub PositionInlinePrompt(sender As Object, e As EventArgs)
        ' No-op: layout is handled by pnlBottom anchor/dock
    End Sub

    Protected Sub HideInlinePrompt()
        If _pnlInlinePrompt IsNot Nothing Then
            _pnlInlinePrompt.Visible = False
            ' Clear ActiveControl so IsTextInputActive() doesn't keep blocking hotkeys
            If Me.ActiveControl Is _txtInlineInput Then
                Me.ActiveControl = Nothing
            End If
        End If
        pnlBottom.Height = BottomBarNormalHeight
        Me.Focus()
    End Sub

    Private Sub InlineInputKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            Dim val = If(_txtInlineInput IsNot Nothing, _txtInlineInput.Text.Trim(), "")
            HideInlinePrompt()
            _inlinePromptCallback?.Invoke(val)
        ElseIf e.KeyCode = Keys.Escape Then
            e.SuppressKeyPress = True
            HideInlinePrompt()
            _inlinePromptCallback?.Invoke("")
        End If
    End Sub

End Class

