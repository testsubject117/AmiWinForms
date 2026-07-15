Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' DOS-style typewriter mode for Option X.
''' Replicates MAINM.ASC lines 2150-2290 behavior:
''' - Full-screen text entry with visible cursor
''' - Print each line to printer as Enter is pressed
''' - TAB = 5 spaces
''' - ESC = exit
''' - Backspace = delete last character
''' - Shows ruler for column guidance
''' </summary>
Public Class FormTypewriterMode
    Inherits Form

    Private lblTitle As Label
    Private lblInstructions As Label
    Private lblRuler As Label
    Private txtDisplay As TextBox
    Private WithEvents printDoc As PrintDocument

    Private _currentLine As New StringBuilder()
    Private _printQueue As New StringBuilder()

    Public Sub New()
        InitializeComponent()
        InitializeDosStyle()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        ' Form settings
        Me.Text = "TYPE WRITTER MODE" ' DOS typo preserved
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.KeyPreview = True
        Me.Size = New Size(900, 600)

        ' Title
        lblTitle = New Label() With {
            .Text = "<<< TYPE WRITTER MODE >>>",
            .Location = New Point(20, 20),
            .Size = New Size(400, 30),
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 12.0F, FontStyle.Bold)
        }

        ' Instructions
        lblInstructions = New Label() With {
            .Text = "[TAB = Move to Right 5 Spaces]  [ESC = Exit]  [ENTER = Print Line]",
            .Location = New Point(20, 50),
            .Size = New Size(800, 25),
            .ForeColor = Color.Cyan,
            .Font = New Font("Consolas", 9.0F, FontStyle.Regular)
        }

        ' Ruler (DOS: ....A....B....C....D....)
        lblRuler = New Label() With {
            .Text = "....A....B....C....D....E....F....G....H....I....J....K....L....M....N....O....P",
            .Location = New Point(20, 85),
            .Size = New Size(800, 25),
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        }

        ' Single multiline text display (DOS style - one field for everything)
        txtDisplay = New TextBox() With {
            .Location = New Point(20, 115),
            .Size = New Size(840, 420),
            .Multiline = True,
            .WordWrap = False,
            .ScrollBars = ScrollBars.Vertical,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular),
            .BorderStyle = BorderStyle.FixedSingle
        }

        ' Print document
        printDoc = New PrintDocument()

        ' Add controls
        Me.Controls.Add(lblTitle)
        Me.Controls.Add(lblInstructions)
        Me.Controls.Add(lblRuler)
        Me.Controls.Add(txtDisplay)

        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Private Sub InitializeDosStyle()
        ' Focus on the display field
        txtDisplay.Select()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        txtDisplay.Focus()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        ' Handle ESC = Exit
        If keyData = Keys.Escape Then
            Me.DialogResult = DialogResult.OK
            Me.Close()
            Return True
        End If

        ' Handle ENTER = Print current line and move to next
        If keyData = Keys.Enter Then
            PrintCurrentLine()
            Return True
        End If

        ' Handle TAB = 5 spaces (DOS behavior)
        If keyData = Keys.Tab Then
            txtDisplay.SelectedText = "     " ' Insert 5 spaces
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub PrintCurrentLine()
        ' Get the current line (text from last newline to cursor)
        Dim cursorPos As Integer = txtDisplay.SelectionStart
        Dim text As String = txtDisplay.Text

        ' Find the start of current line
        Dim lineStart As Integer = text.LastIndexOf(Environment.NewLine, Math.Max(0, cursorPos - 1))
        If lineStart < 0 Then
            lineStart = 0
        Else
            lineStart += Environment.NewLine.Length
        End If

        ' Extract current line
        Dim currentLine As String = text.Substring(lineStart, cursorPos - lineStart)

        ' Add to print queue
        If Not String.IsNullOrWhiteSpace(currentLine) Then
            _printQueue.AppendLine(currentLine)

            ' Try to print immediately
            Try
                printDoc.Print()
            Catch ex As Exception
                ' If printer not available, just queue it
                ' Don't show message every line - just fail silently like DOS
            End Try
        End If

        ' Add newline to display and continue typing
        txtDisplay.SelectedText = Environment.NewLine
    End Sub

    Private Sub printDoc_PrintPage(sender As Object, e As PrintPageEventArgs) Handles printDoc.PrintPage
        ' Get the line to print from the queue
        Dim lines() As String = _printQueue.ToString().Split(New String() {Environment.NewLine}, StringSplitOptions.None)

        If lines.Length > 0 AndAlso Not String.IsNullOrEmpty(lines(0)) Then
            Dim printFont As New Font("Courier New", 10, FontStyle.Regular)
            Dim yPos As Single = e.MarginBounds.Top
            Dim leftMargin As Single = e.MarginBounds.Left
            Dim lineHeight As Single = printFont.GetHeight(e.Graphics)

            ' Print the most recent line
            Dim lastLine As String = ""
            For i As Integer = lines.Length - 1 To 0 Step -1
                If Not String.IsNullOrEmpty(lines(i)) Then
                    lastLine = lines(i)
                    Exit For
                End If
            Next

            If Not String.IsNullOrEmpty(lastLine) Then
                e.Graphics.DrawString(lastLine, printFont, Brushes.Black, leftMargin, yPos)
            End If
        End If

        e.HasMorePages = False
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        ' No confirmation needed - just exit like DOS
    End Sub
End Class

