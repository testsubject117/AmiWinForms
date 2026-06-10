Option Strict On
Option Explicit On

Imports System
Imports System.Windows.Forms
Imports System.Drawing

''' <summary>
''' Shows a match result and prompts "Search for more (Y/N) ?"
''' Mimics DOS TS.COM pause-at-match behavior
''' </summary>
Public Class FormTextSearchMatch
    Inherits Form

    Private lblFoundAt As Label
    Private txtMatchContent As RichTextBox
    Private lblPrompt As Label
    Private btnYes As Button
    Private btnNo As Button
    Private WithEvents flashTimer As Timer
    Private _continueSearch As Boolean = False
    Private _searchTerm As String = ""
    Private _flashState As Boolean = False

    Public Sub New(fileName As String, lineNumber As Integer, matchingLine As String, searchTerm As String)
        InitializeComponent()
        _searchTerm = searchTerm
        SetMatchData(fileName, lineNumber, matchingLine)
        Me.KeyPreview = True
    End Sub

    Public ReadOnly Property ContinueSearch As Boolean
        Get
            Return _continueSearch
        End Get
    End Property

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        ' Form setup
        Me.Text = "Text Search - Match Found"
        Me.ClientSize = New Size(800, 400)
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Font = New Font("Consolas", 10.0F, FontStyle.Regular, GraphicsUnit.Point)

        ' "Found at line X" label
        lblFoundAt = New Label() With {
            .Location = New Point(20, 20),
            .Size = New Size(760, 25),
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 11.0F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        Me.Controls.Add(lblFoundAt)

        ' Matching line content (changed to RichTextBox for colored/flashing text)
        txtMatchContent = New RichTextBox() With {
            .Location = New Point(20, 55),
            .Size = New Size(760, 250),
            .BackColor = Color.Black,
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular),
            .BorderStyle = BorderStyle.FixedSingle,
            .ReadOnly = True,
            .ScrollBars = RichTextBoxScrollBars.Vertical,
            .WordWrap = True
        }
        Me.Controls.Add(txtMatchContent)

        ' "Search for more (Y/N) ?" prompt
        lblPrompt = New Label() With {
            .Text = "Search for more (Y/N) ?",
            .Location = New Point(20, 320),
            .Size = New Size(760, 25),
            .ForeColor = Color.Cyan,
            .Font = New Font("Consolas", 11.0F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        Me.Controls.Add(lblPrompt)

        ' Yes button
        btnYes = New Button() With {
            .Text = "Y - Yes",
            .Location = New Point(500, 350),
            .Size = New Size(130, 35),
            .BackColor = Color.FromArgb(0, 64, 0),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Consolas", 11.0F, FontStyle.Bold)
        }
        btnYes.FlatAppearance.BorderColor = Color.Green
        AddHandler btnYes.Click, AddressOf btnYes_Click
        Me.Controls.Add(btnYes)

        ' No button
        btnNo = New Button() With {
            .Text = "N - No",
            .Location = New Point(650, 350),
            .Size = New Size(130, 35),
            .BackColor = Color.FromArgb(64, 0, 0),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Consolas", 11.0F, FontStyle.Bold)
        }
        btnNo.FlatAppearance.BorderColor = Color.Red
        AddHandler btnNo.Click, AddressOf btnNo_Click
        Me.Controls.Add(btnNo)

        ' Flash timer for search term
        flashTimer = New Timer() With {
            .Interval = 500
        }
        AddHandler flashTimer.Tick, AddressOf flashTimer_Tick

        Me.AcceptButton = btnYes
        Me.CancelButton = btnNo

        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Private Sub SetMatchData(fileName As String, lineNumber As Integer, matchingLine As String)
        lblFoundAt.Text = "Found at line " & lineNumber.ToString() & " in " & fileName

        ' Set the matching line text
        txtMatchContent.Text = matchingLine

        ' Highlight the search term in white
        HighlightSearchTerm()
    End Sub

    Private Sub HighlightSearchTerm()
        If String.IsNullOrEmpty(_searchTerm) Then Return

        ' First, reset ALL text to default yellow on black
        txtMatchContent.SelectAll()
        txtMatchContent.SelectionBackColor = Color.Black
        txtMatchContent.SelectionColor = Color.Yellow
        txtMatchContent.SelectionFont = New Font("Consolas", 10.0F, FontStyle.Regular)

        ' Now find and highlight each occurrence of the search term in white, bold, and underlined
        Dim startIndex As Integer = 0
        While startIndex < txtMatchContent.Text.Length
            ' Case-insensitive search
            Dim foundIndex As Integer = txtMatchContent.Text.IndexOf(_searchTerm, startIndex, StringComparison.OrdinalIgnoreCase)

            If foundIndex = -1 Then Exit While

            ' Select the found text
            txtMatchContent.Select(foundIndex, _searchTerm.Length)

            ' Make search term white, bold, and underlined (stands out from yellow text)
            txtMatchContent.SelectionBackColor = Color.Black
            txtMatchContent.SelectionColor = Color.White
            txtMatchContent.SelectionFont = New Font("Consolas", 10.0F, FontStyle.Bold Or FontStyle.Underline)

            startIndex = foundIndex + _searchTerm.Length
        End While

        ' Deselect
        txtMatchContent.Select(0, 0)
    End Sub

    Private Sub flashTimer_Tick(sender As Object, e As EventArgs) Handles flashTimer.Tick
        ' Timer no longer needed, but keeping handler to avoid build errors
    End Sub

    Private Sub btnYes_Click(sender As Object, e As EventArgs)
        flashTimer.Stop()
        _continueSearch = True
        Me.DialogResult = DialogResult.Yes
        Me.Close()
    End Sub

    Private Sub btnNo_Click(sender As Object, e As EventArgs)
        flashTimer.Stop()
        _continueSearch = False
        Me.DialogResult = DialogResult.No
        Me.Close()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Y Then
            btnYes_Click(Nothing, EventArgs.Empty)
            Return True
        ElseIf keyData = Keys.N OrElse keyData = Keys.Escape Then
            btnNo_Click(Nothing, EventArgs.Empty)
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If flashTimer IsNot Nothing Then
                flashTimer.Stop()
                flashTimer.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class
