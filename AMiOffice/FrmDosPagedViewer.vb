Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Diagnostics

Public Class FrmDosPagedViewer
    Inherits Form

    Private ReadOnly _txt As TextBox
    Private ReadOnly _lblHint As Label

    Private _pages As List(Of String) = New List(Of String)()
    Private _pageIndex As Integer = 0

    ' NEW: status/progress mode (DOS-like "PLEASE WAIT...")
    Private _statusMode As Boolean = False

    ' Set True temporarily if you need to catch who is opening this viewer unexpectedly.
    Private Const DebugBreakOnOpen As Boolean = False

    Public Sub New()
        InitializeComponent()
        If DebugBreakOnOpen AndAlso Debugger.IsAttached Then
            Debugger.Break()
        End If

        Me.Text = "Viewer"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.KeyPreview = True
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        Me.Size = New Size(900, 650)

        _txt = New TextBox() With {
            .Multiline = True,
            .ReadOnly = True,
            .TabStop = False,
            .ScrollBars = ScrollBars.None,
            .BorderStyle = BorderStyle.None,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Dock = DockStyle.Fill,
            .Font = Me.Font,
            .ShortcutsEnabled = False,
            .WordWrap = False
        }

        ' Prevent the textbox from ever receiving focus (keeps keystrokes on the Form handler).
        AddHandler _txt.GotFocus, Sub()
                                      Me.Select()
                                  End Sub

        _lblHint = New Label() With {
            .Dock = DockStyle.Bottom,
            .Height = 28,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = Me.Font,
            .Text = "[ENTER = More]   [ESC = Quit]"
        }

        Me.Controls.Add(_txt)
        Me.Controls.Add(_lblHint)

        AddHandler Me.KeyDown, AddressOf OnViewerKeyDown
        AddHandler Me.Shown, Sub()
                                 ' Ensure the form (not the TextBox) owns keyboard input.
                                 Me.Select()
                             End Sub
    End Sub

    Public Sub SetPages(pages As IEnumerable(Of String))
        _statusMode = False
        _lblHint.Text = "[ENTER = More]   [ESC = Quit]"

        _pages = New List(Of String)(If(pages, Array.Empty(Of String)()))
        _pageIndex = 0
        Render()
    End Sub

    Public Sub SetSinglePage(text As String)
        SetPages(New String() {If(text, "")})
    End Sub

    ' -----------------------------
    ' NEW: DOS-like status/progress mode
    ' -----------------------------
    Public Sub BeginStatusMode(Optional hintText As String = "[ESC = Quit]")
        RunOnUiThread(
            Sub()
                _statusMode = True
                _pages = New List(Of String)(New String() {""})
                _pageIndex = 0
                _lblHint.Text = hintText
                Render()
            End Sub)
    End Sub

    Public Sub UpdateStatus(text As String)
        RunOnUiThread(
            Sub()
                If Not _statusMode Then
                    ' If someone updates status without explicitly beginning,
                    ' switch into status mode automatically.
                    BeginStatusMode()
                End If

                If _pages Is Nothing OrElse _pages.Count = 0 Then
                    _pages = New List(Of String)(New String() {If(text, "")})
                    _pageIndex = 0
                Else
                    _pages(0) = If(text, "")
                    _pageIndex = 0
                End If

                Render()
                ' Keep the UI responsive and visibly updating while work is happening.
                Application.DoEvents()
            End Sub)
    End Sub

    Public Sub EndStatusMode()
        RunOnUiThread(
            Sub()
                _statusMode = False
                _lblHint.Text = "[ENTER = More]   [ESC = Quit]"
            End Sub)
    End Sub

    Private Sub RunOnUiThread(action As Action)
        If action Is Nothing Then Return

        If Me.IsDisposed Then Return

        If Me.InvokeRequired Then
            Try
                Me.BeginInvoke(action)
            Catch
                ' Ignore if closing/disposed mid-invoke
            End Try
        Else
            action()
        End If
    End Sub

    ' -----------------------------
    ' Input handling
    ' -----------------------------
    Private Sub OnViewerKeyDown(sender As Object, e As KeyEventArgs)
        ' IMPORTANT:
        ' Swallow digits and common typing keys so they can't trigger any global "type-to-search"
        ' handler elsewhere while this viewer is open.
        If (e.KeyCode >= Keys.D0 AndAlso e.KeyCode <= Keys.D9) OrElse
           (e.KeyCode >= Keys.NumPad0 AndAlso e.KeyCode <= Keys.NumPad9) Then

            e.Handled = True
            Return
        End If

        If e.KeyCode = Keys.Escape Then
            Me.Close()
            e.Handled = True
            Return
        End If

        ' In status mode, ignore ENTER so it feels like a locked "PLEASE WAIT" screen.
        If _statusMode Then
            If e.KeyCode = Keys.Enter Then
                e.Handled = True
                Return
            End If
        End If

        If e.KeyCode = Keys.Enter Then
            If _pageIndex < _pages.Count - 1 Then
                _pageIndex += 1
                Render()
            Else
                ' Stay on last page until ESC (DOS-like).
            End If
            e.Handled = True
            Return
        End If
    End Sub

    Private Sub Render()
        If _pages Is Nothing OrElse _pages.Count = 0 Then
            _txt.Text = ""
            Return
        End If

        _txt.Text = _pages(_pageIndex)
        _txt.SelectionStart = 0
        _txt.SelectionLength = 0
    End Sub
End Class