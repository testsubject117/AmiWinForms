Imports System.Drawing
Imports System.Windows.Forms

Public Class FrmRolodexPrompt
    Inherits Form

    Private ReadOnly _engine As RolodexPromptEngine

    Private lblTitle As Label
    Private txtScreen As TextBox

    Private _currentInput As String = ""

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

        Me.Controls.Add(txtScreen)
        Me.Controls.Add(lblTitle)

        AddHandler Me.Load, AddressOf FrmRolodexPrompt_Load
        AddHandler Me.Shown, AddressOf FrmRolodexPrompt_Shown
        AddHandler Me.KeyPress, AddressOf FrmRolodexPrompt_KeyPress
        AddHandler Me.KeyDown, AddressOf FrmRolodexPrompt_KeyDown
        AddHandler txtScreen.Click, AddressOf RefocusScreen
        AddHandler Me.Click, AddressOf RefocusScreen
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
    End Sub

    Private Function AppendInlineInput(prompt As String, input As String) As String
        Dim parts = prompt.Split({Environment.NewLine}, StringSplitOptions.None)

        If parts.Length = 0 Then
            Return input
        End If

        parts(parts.Length - 1) &= " " & input

        Return String.Join(Environment.NewLine, parts)
    End Function
End Class