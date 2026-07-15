Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Windows.Forms

Public Class FrmRolodexPrompt
    Inherits Form

    Private ReadOnly _engine As RolodexPromptEngine

    Private lblTitle As Label
    Private txtScreen As TextBox
    Private btnPrint As Button

    Private _currentInput As String = ""

    ' Show the Print button only when this text is present on-screen.
    ' (We match a stable substring rather than the whole line.)
    Private Const PrintHintNeedle As String = "Click ""Print"""

    ' Printing
    Private ReadOnly _printDoc As New PrintDocument()
    Private _printLines As String() = Array.Empty(Of String)()
    Private _printLineIndex As Integer = 0

    Public Sub New(engine As RolodexPromptEngine)
        MyBase.New()
        _engine = engine
        InitializeRolodexUi()
    End Sub

    Private Sub InitializeRolodexUi()
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Size = New Size(900, 650)
        Me.KeyPreview = True
        Me.Text = "Rolodex Prompt"

        lblTitle = New Label() With {
            .AutoSize = False,
            .Height = 40,
            .Dock = DockStyle.Top,
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Font = New Font("Consolas", 14.0F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(10, 0, 0, 0)
        }

        txtScreen = New TextBox() With {
            .Multiline = True,
            .ReadOnly = True,
            .BorderStyle = BorderStyle.None,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 12.0F, FontStyle.Regular),
            .Dock = DockStyle.Fill,
            .ScrollBars = ScrollBars.Vertical,
            .TabStop = False
        }

        btnPrint = New Button() With {
            .Text = "Print (Ctrl+P)",
            .Size = New Size(140, 28),
            .BackColor = SystemColors.Control,
            .ForeColor = SystemColors.ControlText,
            .FlatStyle = FlatStyle.Standard,
            .TabStop = False,
            .Visible = False
        }

        Me.Controls.Add(txtScreen)
        Me.Controls.Add(btnPrint)
        Me.Controls.Add(lblTitle)

        AddHandler Me.Load, AddressOf FrmRolodexPrompt_Load
        AddHandler Me.Shown, AddressOf FrmRolodexPrompt_Shown
        AddHandler Me.KeyPress, AddressOf FrmRolodexPrompt_KeyPress
        AddHandler Me.KeyDown, AddressOf FrmRolodexPrompt_KeyDown
        AddHandler txtScreen.Click, AddressOf RefocusScreen
        AddHandler Me.Click, AddressOf RefocusScreen
        AddHandler btnPrint.Click, Sub() ShowPrintDialogAndPrint()

        ' Reposition button when content/layout changes.
        AddHandler Me.Resize, Sub() PositionPrintButton()
        AddHandler txtScreen.TextChanged, Sub() PositionPrintButton()
        AddHandler txtScreen.MouseWheel, Sub() PositionPrintButton()

        ' Print handlers
        AddHandler _printDoc.BeginPrint, AddressOf PrintDoc_BeginPrint
        AddHandler _printDoc.PrintPage, AddressOf PrintDoc_PrintPage
    End Sub

    Private Sub FrmRolodexPrompt_Load(sender As Object, e As EventArgs)
        lblTitle.Text = _engine.Title
        RefreshScreen()
    End Sub

    Private Sub FrmRolodexPrompt_Shown(sender As Object, e As EventArgs)
        RefocusScreen(Nothing, EventArgs.Empty)
    End Sub

    Private Sub RefocusScreen(sender As Object, e As EventArgs)
        Me.Activate()
        txtScreen.Focus()
    End Sub

    Private Sub FrmRolodexPrompt_KeyPress(sender As Object, e As KeyPressEventArgs)
        If Char.IsControl(e.KeyChar) Then Return

        _currentInput &= e.KeyChar
        RefreshScreen()
        e.Handled = True
    End Sub

    Private Sub FrmRolodexPrompt_KeyDown(sender As Object, e As KeyEventArgs)
        ' Ctrl+P => only allow printing when the print hint is on-screen.
        If e.Control AndAlso e.KeyCode = Keys.P Then
            e.SuppressKeyPress = True

            If txtScreen.Text.IndexOf(PrintHintNeedle, StringComparison.OrdinalIgnoreCase) >= 0 Then
                ShowPrintDialogAndPrint()
            End If

            Return
        End If

        If e.KeyCode = Keys.Back Then
            If _currentInput.Length > 0 Then
                _currentInput = _currentInput.Substring(0, _currentInput.Length - 1)
                RefreshScreen()
            End If
            e.SuppressKeyPress = True
            Return
        End If

        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True

            Dim submitted = _currentInput
            _currentInput = ""

            _engine.SubmitInput(submitted)
            RefreshScreen()

            If _engine.IsComplete() Then
                Me.DialogResult = DialogResult.OK
                Me.Close()
                Return
            End If

            Return
        End If

        If e.KeyCode = Keys.Escape Then
            e.SuppressKeyPress = True
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Private Sub RefreshScreen()
        Dim lines As New List(Of String)
        lines.AddRange(_engine.Transcript)

        Dim prompt = _engine.GetCurrentPrompt()
        If Not String.IsNullOrWhiteSpace(prompt) Then
            lines.Add(AppendInlineInput(prompt, _currentInput))
        End If

        txtScreen.Text = String.Join(Environment.NewLine & Environment.NewLine, lines)
        txtScreen.SelectionStart = txtScreen.TextLength
        txtScreen.ScrollToCaret()

        PositionPrintButton()
    End Sub

    Private Sub PositionPrintButton()
        Dim text = txtScreen.Text

        ' Only show the button when the print instruction is on-screen (after answering Y).
        Dim idx = text.LastIndexOf(PrintHintNeedle, StringComparison.OrdinalIgnoreCase)
        If idx < 0 Then
            btnPrint.Visible = False
            Return
        End If

        btnPrint.Visible = True

        ' Find the line that contains the hint
        Dim lineIndex As Integer = txtScreen.GetLineFromCharIndex(idx)
        Dim lineCharIndex As Integer = txtScreen.GetFirstCharIndexFromLine(lineIndex)

        ' If we can't compute the line position, keep it bottom-left.
        Dim x = txtScreen.Left + 10
        Dim y = txtScreen.Bottom - btnPrint.Height - 12

        If lineCharIndex >= 0 Then
            ' Place directly under the hint line (left aligned with the hint).
            Dim pt As Point = txtScreen.GetPositionFromCharIndex(lineCharIndex)
            Dim formPt As Point = Me.PointToClient(txtScreen.PointToScreen(pt))

            x = formPt.X
            y = formPt.Y + txtScreen.Font.Height + 8
        End If

        ' Clamp into txtScreen bounds
        x = Math.Max(x, txtScreen.Left + 10)
        x = Math.Min(x, txtScreen.Right - btnPrint.Width - 10)

        y = Math.Max(y, txtScreen.Top + 10)
        y = Math.Min(y, txtScreen.Bottom - btnPrint.Height - 10)

        btnPrint.Location = New Point(x, y)
        btnPrint.BringToFront()
    End Sub

    Private Function AppendInlineInput(prompt As String, input As String) As String
        Dim parts = prompt.Split({Environment.NewLine}, StringSplitOptions.None)

        If parts.Length = 0 Then
            Return input
        End If

        parts(parts.Length - 1) &= " " & input

        Return String.Join(Environment.NewLine, parts)
    End Function

    Private Sub ShowPrintDialogAndPrint()
        Dim textToPrint = txtScreen.Text
        If String.IsNullOrWhiteSpace(textToPrint) Then Return

        Using dlg As New PrintDialog()
            dlg.UseEXDialog = True
            dlg.AllowSomePages = False
            dlg.AllowSelection = False
            dlg.Document = _printDoc

            If dlg.ShowDialog(Me) = DialogResult.OK Then
                _printDoc.PrinterSettings = dlg.PrinterSettings
                _printDoc.DefaultPageSettings = dlg.PrinterSettings.DefaultPageSettings
                _printDoc.Print()
            End If
        End Using
    End Sub

    Private Sub PrintDoc_BeginPrint(sender As Object, e As PrintEventArgs)
        _printLineIndex = 0
        Dim textToPrint = txtScreen.Text
        _printLines = textToPrint.Replace(vbCrLf, vbLf).Split(ControlChars.Lf)
    End Sub

    Private Sub PrintDoc_PrintPage(sender As Object, e As PrintPageEventArgs)
        Dim fontToUse As Font = txtScreen.Font

        Dim left = e.MarginBounds.Left
        Dim y = e.MarginBounds.Top

        Dim lineHeight As Integer = CInt(Math.Ceiling(fontToUse.GetHeight(e.Graphics)))

        While _printLineIndex < _printLines.Length
            Dim line = _printLines(_printLineIndex)

            If y + lineHeight > e.MarginBounds.Bottom Then
                e.HasMorePages = True
                Return
            End If

            e.Graphics.DrawString(line, fontToUse, Brushes.Black, left, y)
            y += lineHeight
            _printLineIndex += 1
        End While

        e.HasMorePages = False
    End Sub
End Class
