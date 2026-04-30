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
        pnlBottom.Height = 52
        pnlBottom.BackColor = _bgDarkGray
        pnlBottom.Padding = New Padding(0, 10, 0, 0)

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
        AddHandler pnlBottom.Resize, Sub()
                                         btnEscClose.Left = pnlBottom.ClientSize.Width - btnEscClose.Width
                                         btnEscClose.Top = pnlBottom.ClientSize.Height - btnEscClose.Height
                                     End Sub

        ' Main content (header + body) fills above bottom bar
        Dim root As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .BackColor = _bgDarkGray,
            .Margin = New Padding(0)
        }
        root.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' header row
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F)) ' body

        ' Header row: title left, clock right (same line)
        Dim header As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .BackColor = _bgBlack,
            .ColumnCount = 2,
            .RowCount = 1,
            .Margin = New Padding(0),
            .Padding = New Padding(8, 6, 8, 6)
        }
        header.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        header.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

        lblMainMenu.AutoSize = True
        lblMainMenu.TextAlign = ContentAlignment.MiddleLeft
        lblMainMenu.Font = New Font("Castellar", 36.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblMainMenu.ForeColor = Color.Yellow
        lblMainMenu.BackColor = _bgBlack
        lblMainMenu.Margin = New Padding(0)

        lblDateTime.AutoSize = False
        lblDateTime.Dock = DockStyle.Fill
        lblDateTime.TextAlign = ContentAlignment.MiddleRight
        lblDateTime.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold, GraphicsUnit.Point) ' bold
        lblDateTime.ForeColor = Color.Yellow
        lblDateTime.BackColor = _bgBlack
        lblDateTime.Margin = New Padding(0)
        lblDateTime.Padding = New Padding(0) ' moved up (no top padding)

        header.Controls.Add(lblMainMenu, 0, 0)
        header.Controls.Add(lblDateTime, 1, 0)

        ' Body: dark gray background so black buttons contrast
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

        root.Controls.Add(header, 0, 0)
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
        panel.Margin = New Padding(0, 10, 0, 0)
    End Sub

    Protected Sub SetMenuTitle(title As String)
        lblMainMenu.Text = title
        Me.Text = title
    End Sub

    Protected Sub ClearMenu()
        flpLeft.Controls.Clear()
        flpRight.Controls.Clear()

        ' Keep ESC close handler
        Dim esc = _keyHandlers("ESC")
        _keyHandlers = New Dictionary(Of String, Action)(StringComparer.OrdinalIgnoreCase)
        _keyHandlers("ESC") = esc
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

    Private Sub ResizeButtonsToPanel(panel As FlowLayoutPanel)
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

    Protected Sub UpdateHeaderClock()
        Dim now As DateTime = DateTime.Now

        Dim verPrefix As String = ""
        If ShowVersionInHeader AndAlso Not String.IsNullOrWhiteSpace(_displayVersion) Then
            verPrefix = "   " & _displayVersion & "          "
        End If

        lblDateTime.Text = verPrefix &
                           now.ToString("dddd") & "  " &
                           now.ToString("MM-dd-yyyy") & "          " &
                           now.ToString("hh:mm:ss tt")
    End Sub

    Protected Sub NotYet(feature As String)
        MessageBox.Show("Not implemented yet: " & feature, "Port status", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

End Class