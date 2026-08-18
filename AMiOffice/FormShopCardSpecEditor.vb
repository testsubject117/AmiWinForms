Option Strict Off
Option Explicit On

Imports System
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Collections.Generic

''' <summary>
''' DOS-parity Master Spec List editor for NEWSPECS.DAT.
''' Displays all spec entries, allows Add / Edit / Delete, saves back in quoted format.
''' Lines beginning with &lt; are comments (shown but not editable as specs).
''' Lines beginning with * are superseded (shown with * prefix).
''' </summary>
Public Class FormShopCardSpecEditor
    Inherits Form

    ' ── UI ──────────────────────────────────────────────────────────────
    Private _listBox As ListBox
    Private _statusLabel As Label
    Private _pnlBottom As Panel

    ' ── Data ────────────────────────────────────────────────────────────
    Private _filePath As String
    Private _lines As List(Of String)   ' raw unquoted lines (comments kept as-is)
    Private _dirty As Boolean = False

    ' ── Constructor ─────────────────────────────────────────────────────
    Public Sub New()
        _filePath = Path.Combine(ShopCardSession.DataFolder, "NEWSPECS.DAT")
        InitializeUi()
        LoadSpecs()
    End Sub

    ' ── Layout ──────────────────────────────────────────────────────────
    Private Sub InitializeUi()
        Me.Text = "EDIT MASTER SPEC LIST"
        Me.ClientSize = New Size(1024, 680)
        Me.BackColor = Color.Black
        Me.ForeColor = Color.Yellow
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.KeyPreview = True

        ' Title bar
        Dim title As New Label() With {
            .Text = "MASTER SPEC LIST",
            .Dock = DockStyle.Top,
            .Height = 44,
            .Font = New Font("Courier New", 22.0F, FontStyle.Bold, GraphicsUnit.Point),
            .ForeColor = Color.Yellow,
            .BackColor = Color.Black,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(title)

        ' Instructions
        Dim hint As New Label() With {
            .Text = "(A) Add  (E) Edit  (D) Delete  (S) Save  (ESC) Quit",
            .Dock = DockStyle.Top,
            .Height = 22,
            .Font = New Font("Courier New", 10.0F, FontStyle.Regular, GraphicsUnit.Point),
            .ForeColor = Color.Cyan,
            .BackColor = Color.Black,
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(hint)

        ' Spec list
        _listBox = New ListBox() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.Black,
            .ForeColor = Color.Yellow,
            .Font = New Font("Courier New", 11.0F, FontStyle.Regular, GraphicsUnit.Point),
            .BorderStyle = BorderStyle.None,
            .SelectionMode = SelectionMode.One
        }
        Me.Controls.Add(_listBox)

        ' Bottom status bar
        _pnlBottom = New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 28,
            .BackColor = Color.Black
        }
        _statusLabel = New Label() With {
            .AutoSize = False,
            .Dock = DockStyle.Fill,
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Font = New Font("Courier New", 10.0F, FontStyle.Regular, GraphicsUnit.Point),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Text = ""
        }
        _pnlBottom.Controls.Add(_statusLabel)
        Me.Controls.Add(_pnlBottom)

        ' Bring list above title/hint (reverse dock order)
        _listBox.BringToFront()
    End Sub

    ' ── Load ────────────────────────────────────────────────────────────
    Private Sub LoadSpecs()
        _lines = New List(Of String)
        If File.Exists(_filePath) Then
            For Each raw In File.ReadAllLines(_filePath)
                ' Strip surrounding quotes if present
                Dim s = raw.Trim()
                If s.StartsWith("""") AndAlso s.EndsWith("""") AndAlso s.Length >= 2 Then
                    s = s.Substring(1, s.Length - 2)
                End If
                _lines.Add(s)
            Next
        End If
        RefreshList()
    End Sub

    Private Sub RefreshList()
        Dim sel = _listBox.SelectedIndex
        _listBox.BeginUpdate()
        _listBox.Items.Clear()
        For i = 0 To _lines.Count - 1
            Dim s = _lines(i)
            If s.StartsWith("<") Then
                _listBox.Items.Add("  [COMMENT] " & s.Substring(1).Trim())
            ElseIf s.StartsWith("*") Then
                _listBox.Items.Add(String.Format("{0,3}.  * {1}", i + 1, s.Substring(1)))
            Else
                _listBox.Items.Add(String.Format("{0,3}.    {1}", i + 1, s))
            End If
        Next
        _listBox.EndUpdate()
        If sel >= 0 AndAlso sel < _listBox.Items.Count Then
            _listBox.SelectedIndex = sel
        ElseIf _listBox.Items.Count > 0 Then
            _listBox.SelectedIndex = 0
        End If
        _listBox.Focus()
    End Sub

    ' ── Save ────────────────────────────────────────────────────────────
    Private Sub SaveSpecs()
        Try
            Dim quoted = New List(Of String)
            For Each s In _lines
                ' Comments and lines that already start with < stay unquoted-ish
                ' but match the original format: wrap in quotes
                quoted.Add("""" & s & """")
            Next
            File.WriteAllLines(_filePath, quoted)
            _dirty = False
            SetStatus("Saved OK.")
        Catch ex As Exception
            SetStatus("ERROR saving: " & ex.Message)
        End Try
    End Sub

    ' ── Commands ────────────────────────────────────────────────────────
    Private Sub AddSpec()
        Dim val = DosInputBox("Enter new spec (prefix * = superseded, < = comment):", "")
        If val Is Nothing Then Return
        val = val.Trim()
        If val = "" Then Return
        ' Insert after current selection, or at end
        Dim insertAt = If(_listBox.SelectedIndex >= 0, _listBox.SelectedIndex + 1, _lines.Count)
        _lines.Insert(insertAt, val)
        _dirty = True
        RefreshList()
        _listBox.SelectedIndex = Math.Min(insertAt, _listBox.Items.Count - 1)
        SetStatus("Added.")
    End Sub

    Private Sub EditSpec()
        Dim idx = _listBox.SelectedIndex
        If idx < 0 OrElse idx >= _lines.Count Then Return
        Dim current = _lines(idx)
        If current.StartsWith("<") Then
            SetStatus("Comments cannot be edited as specs.")
            Return
        End If
        Dim val = DosInputBox("Edit spec:", current)
        If val Is Nothing Then Return
        val = val.Trim()
        If val = "" Then Return
        _lines(idx) = val
        _dirty = True
        RefreshList()
        _listBox.SelectedIndex = idx
        SetStatus("Updated.")
    End Sub

    Private Sub DeleteSpec()
        Dim idx = _listBox.SelectedIndex
        If idx < 0 OrElse idx >= _lines.Count Then Return
        Dim current = _lines(idx)
        Dim confirm = DosInputBox("Delete: " & current & "  (Y to confirm):", "")
        If confirm IsNot Nothing AndAlso confirm.Trim().ToUpper() = "Y" Then
            _lines.RemoveAt(idx)
            _dirty = True
            RefreshList()
            If _listBox.Items.Count > 0 Then
                _listBox.SelectedIndex = Math.Min(idx, _listBox.Items.Count - 1)
            End If
            SetStatus("Deleted.")
        End If
    End Sub

    ' ── Input helper ────────────────────────────────────────────────────
    Private Function DosInputBox(prompt As String, defaultVal As String) As String
        ' Simple inline DOS-style input dialog
        Using dlg As New Form()
            dlg.Text = "Master Spec List"
            dlg.ClientSize = New Size(700, 100)
            dlg.BackColor = Color.Black
            dlg.ForeColor = Color.Yellow
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog
            dlg.StartPosition = FormStartPosition.CenterParent
            dlg.MaximizeBox = False
            dlg.MinimizeBox = False
            dlg.KeyPreview = True

            Dim lbl As New Label() With {
                .Text = prompt,
                .ForeColor = Color.White,
                .BackColor = Color.Black,
                .Font = New Font("Courier New", 10.0F),
                .Location = New Point(10, 12),
                .Size = New Size(680, 20)
            }
            Dim txt As New TextBox() With {
                .BackColor = Color.Black,
                .ForeColor = Color.Yellow,
                .Font = New Font("Courier New", 11.0F),
                .BorderStyle = BorderStyle.FixedSingle,
                .Location = New Point(10, 38),
                .Size = New Size(680, 24),
                .Text = defaultVal,
                .MaxLength = 80
            }
            txt.SelectAll()
            dlg.Controls.Add(lbl)
            dlg.Controls.Add(txt)

            Dim result As String = Nothing
            AddHandler txt.KeyDown, Sub(s, e)
                If e.KeyCode = Keys.Enter Then
                    e.SuppressKeyPress = True
                    result = txt.Text
                    dlg.Close()
                ElseIf e.KeyCode = Keys.Escape Then
                    e.SuppressKeyPress = True
                    dlg.Close()
                End If
            End Sub

            AddHandler dlg.Shown, Sub(s, e) txt.Focus()
            dlg.ShowDialog(Me)
            Return result
        End Using
    End Function

    ' ── Key handling ────────────────────────────────────────────────────
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        Select Case e.KeyCode
            Case Keys.A
                e.Handled = True
                AddSpec()
            Case Keys.E
                e.Handled = True
                EditSpec()
            Case Keys.D
                e.Handled = True
                DeleteSpec()
            Case Keys.S
                e.Handled = True
                SaveSpecs()
            Case Keys.Escape
                e.Handled = True
                TryClose()
            Case Keys.Up
                If _listBox.SelectedIndex > 0 Then _listBox.SelectedIndex -= 1
                e.Handled = True
            Case Keys.Down
                If _listBox.SelectedIndex < _listBox.Items.Count - 1 Then _listBox.SelectedIndex += 1
                e.Handled = True
        End Select
        MyBase.OnKeyDown(e)
    End Sub

    Private Sub TryClose()
        If _dirty Then
            Dim ans = DosInputBox("Unsaved changes. Save before closing? (Y/N):", "Y")
            If ans IsNot Nothing AndAlso ans.Trim().ToUpper() = "Y" Then
                SaveSpecs()
            End If
        End If
        Me.Close()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If _dirty AndAlso e.CloseReason = CloseReason.UserClosing Then
            Dim ans = DosInputBox("Unsaved changes. Save before closing? (Y/N):", "Y")
            If ans IsNot Nothing AndAlso ans.Trim().ToUpper() = "Y" Then
                SaveSpecs()
            End If
        End If
        MyBase.OnFormClosing(e)
    End Sub

    Private Sub SetStatus(msg As String)
        _statusLabel.Text = "  " & msg
    End Sub

End Class
