Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Text
Imports System.Windows.Forms

' Internal Windows editor for \\invoice\MainMenu\Data\EMPNAME.DAT
' Mirrors DOS behavior (edit text file) but adds safe normalization on Save.
Public Class FormEmpNameEditor
    Inherits DosMenuFormBase

    Private ReadOnly _txt As New TextBox()
    Private ReadOnly _btnSave As New Button()
    Private ReadOnly _lblHelp As New Label()

    Private _filePath As String
    Private _loading As Boolean = False
    Private _dirty As Boolean = False

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        ' Match CHECKS menu sizing/layout
        Me.Width = 1000
        Me.Height = 720

        ShowVersionInHeader = False
        UpdateHeaderClock()

        ' This is not a menu; hide the left/right flow panels
        ClearMenu()
        flpLeft.Visible = False
        flpRight.Visible = False
        flpLeft.Enabled = False
        flpRight.Enabled = False

        SetMenuTitle("EMPNAME.DAT")

        _filePath = Path.Combine(LegacyDataPaths.BaseDataDir, "EMPNAME.DAT")

        BuildEditorUi()
        LoadFile()
    End Sub

    Private Sub BuildEditorUi()
        Dim host As New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Padding = New Padding(12)
        }
        host.RowStyles.Add(New RowStyle(SizeType.AutoSize))         ' help
        host.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))  ' editor
        host.RowStyles.Add(New RowStyle(SizeType.AutoSize))         ' buttons

        _lblHelp.AutoSize = True
        _lblHelp.ForeColor = Color.White
        _lblHelp.Text =
            "DOS rule: Enter the employee name, then customer names with a space in front of each." & Environment.NewLine &
            "This editor will keep customer indentation valid when saving."
        _lblHelp.Padding = New Padding(0, 0, 0, 8)

        _txt.Dock = DockStyle.Fill
        _txt.Multiline = True
        _txt.ScrollBars = ScrollBars.Both
        _txt.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        _txt.AcceptsReturn = True
        _txt.AcceptsTab = True
        _txt.WordWrap = False

        AddHandler _txt.TextChanged, Sub()
                                         If _loading Then Return
                                         _dirty = True
                                     End Sub

        Dim btnRow As New FlowLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.RightToLeft,
            .AutoSize = True
        }

        Dim btnClose As New Button() With {
            .Text = "Close",
            .AutoSize = True
        }
        AddHandler btnClose.Click, Sub() CloseEditor()

        _btnSave.Text = "Save"
        _btnSave.AutoSize = True
        AddHandler _btnSave.Click, Sub() SaveFileNormalized()

        ' Right-to-left: Close on far right, then Save just to its left
        btnRow.Controls.Add(btnClose)
        btnRow.Controls.Add(_btnSave)

        host.Controls.Add(_lblHelp, 0, 0)
        host.Controls.Add(_txt, 0, 1)
        host.Controls.Add(btnRow, 0, 2)

        ' Add to the form (base builds its own layout)
        Me.Controls.Add(host)
        host.BringToFront()
    End Sub

    Private Sub CloseEditor()
        ' Respect the unsaved-changes prompt already implemented in OnFormClosing
        Me.Close()
    End Sub

    Private Sub LoadFile()
        _loading = True
        Try
            If File.Exists(_filePath) Then
                _txt.Text = File.ReadAllText(_filePath)
            Else
                _txt.Text = ""
                MessageBox.Show("File not found:" & Environment.NewLine & _filePath,
                                "EMPNAME.DAT",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning)
            End If
            _dirty = False
        Catch ex As Exception
            MessageBox.Show("Failed to load file:" & Environment.NewLine & _filePath & Environment.NewLine & Environment.NewLine & ex.Message,
                            "EMPNAME.DAT",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        Finally
            _loading = False
        End Try
    End Sub

    ' Normalizes content to prevent program-breaking edits while staying DOS-compatible:
    ' - converts TABs to spaces
    ' - trims trailing spaces
    ' - ensures customer lines have at least one leading space (based on employee/customer grouping)
    Private Function NormalizeEmpNameDat(input As String) As String
        If input Is Nothing Then Return ""

        Dim lines As String() = input.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Split(ControlChars.Lf)
        Dim sb As New StringBuilder()

        Dim haveEmployee As Boolean = False

        For i As Integer = 0 To lines.Length - 1
            Dim raw As String = If(lines(i), "")
            raw = raw.Replace(ControlChars.Tab, " "c)

            ' Preserve blank lines, but trim trailing whitespace (common accidental corruption)
            Dim trimmedEnd As String = raw.TrimEnd()

            If trimmedEnd.Length = 0 Then
                sb.AppendLine("")
                Continue For
            End If

            Dim startsWithSpace As Boolean = (trimmedEnd.Length > 0 AndAlso trimmedEnd(0) = " "c)

            If Not startsWithSpace Then
                ' Employee header line
                haveEmployee = True
                sb.AppendLine(trimmedEnd)
            Else
                ' Customer line: normalize to ONE leading space (safe for DOS-style parsers)
                Dim customerText As String = trimmedEnd.TrimStart()
                If haveEmployee Then
                    sb.AppendLine(" " & customerText)
                Else
                    ' If the file begins with indented lines, keep them indented but normalize to one space.
                    sb.AppendLine(" " & customerText)
                End If
            End If
        Next

        Return sb.ToString()
    End Function

    Private Sub SaveFileNormalized()
        Try
            Dim normalized As String = NormalizeEmpNameDat(_txt.Text)

            ' Update textbox to reflect what actually got saved (transparent but no prompts).
            If normalized <> _txt.Text Then
                _loading = True
                Dim selStart As Integer = _txt.SelectionStart
                _txt.Text = normalized
                _txt.SelectionStart = Math.Min(selStart, _txt.TextLength)
                _loading = False
            End If

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath))
            File.WriteAllText(_filePath, _txt.Text)

            _dirty = False
            MessageBox.Show("Saved: " & _filePath, "EMPNAME.DAT", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Failed to save file:" & Environment.NewLine & _filePath & Environment.NewLine & Environment.NewLine & ex.Message,
                            "EMPNAME.DAT",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        Finally
            _loading = False
        End Try
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If _dirty Then
            Dim dr = MessageBox.Show("You have unsaved changes. Close anyway?",
                                     "EMPNAME.DAT",
                                     MessageBoxButtons.YesNo,
                                     MessageBoxIcon.Warning,
                                     MessageBoxDefaultButton.Button2)
            If dr <> DialogResult.Yes Then
                e.Cancel = True
                Return
            End If
        End If

        MyBase.OnFormClosing(e)
    End Sub
End Class