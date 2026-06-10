Option Strict On
Option Explicit On

Imports System
Imports System.Windows.Forms
Imports System.Drawing

''' <summary>
''' Shows real-time search progress mimicking DOS TS.COM behavior
''' </summary>
Public Class FormTextSearchProgress
    Inherits Form

    Private lblSearching As Label
    Private lstProgress As ListBox
    Private _userCancelled As Boolean = False

    Public Sub New()
        InitializeComponent()
    End Sub

    Public ReadOnly Property UserCancelled As Boolean
        Get
            Return _userCancelled
        End Get
    End Property

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        ' Form setup
        Me.Text = "Text Search"
        Me.ClientSize = New Size(700, 500)
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Font = New Font("Consolas", 10.0F, FontStyle.Regular, GraphicsUnit.Point)
        Me.KeyPreview = True

        ' Searching label
        lblSearching = New Label() With {
            .Text = "Searching...",
            .Location = New Point(20, 20),
            .Size = New Size(660, 30),
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 12.0F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        Me.Controls.Add(lblSearching)

        ' Progress listbox (shows file-by-file progress)
        lstProgress = New ListBox() With {
            .Location = New Point(20, 60),
            .Size = New Size(660, 420),
            .BackColor = Color.Black,
            .ForeColor = Color.Cyan,
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular),
            .BorderStyle = BorderStyle.FixedSingle,
            .SelectionMode = SelectionMode.None
        }
        Me.Controls.Add(lstProgress)

        Me.ResumeLayout(False)
    End Sub

    ''' <summary>
    ''' Add a file name to the progress display
    ''' </summary>
    Public Sub AddSearchingFile(fileName As String)
        If Me.InvokeRequired Then
            Me.Invoke(New Action(Of String)(AddressOf AddSearchingFile), fileName)
            Return
        End If

        lstProgress.Items.Add("Searching " & fileName)
        lstProgress.TopIndex = Math.Max(0, lstProgress.Items.Count - 1)
        Application.DoEvents()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            _userCancelled = True
            Me.Close()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function
End Class
