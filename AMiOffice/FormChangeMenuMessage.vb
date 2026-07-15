Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Option V: Change Main Menu Message
''' DOS-faithful implementation of the scrolling message editor from MAINM.ASC lines 2080-2130.
''' </summary>
Public Class FormChangeMenuMessage
    Inherits Form

    Private lblTitle As Label
    Private lblCurrentMessage As Label
    Private txtCurrentMessage As TextBox
    Private lblInstructions As Label
    Private txtNewMessage As TextBox
    Private lblCharCount As Label
    Private btnSave As Button
    Private btnNoMessage As Button
    Private btnCancel As Button

    Public Sub New()
        InitializeComponent()
        InitializeDosStyle()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        ' Form settings
        Me.Text = "Change Main Menu Message"
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Size = New Size(800, 450)
        Me.KeyPreview = True

        ' Title
        lblTitle = New Label() With {
            .Text = "<<< Change Main Menu Message >>>",
            .Location = New Point(20, 20),
            .Size = New Size(740, 30),
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 14.0F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        ' Current message label
        lblCurrentMessage = New Label() With {
            .Text = "Current Message is:",
            .Location = New Point(20, 70),
            .Size = New Size(740, 20),
            .ForeColor = Color.Cyan
        }

        ' Current message display
        txtCurrentMessage = New TextBox() With {
            .Location = New Point(20, 95),
            .Size = New Size(740, 60),
            .Multiline = True,
            .ReadOnly = True,
            .BackColor = Color.FromArgb(0, 0, 64),
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular),
            .BorderStyle = BorderStyle.FixedSingle
        }

        ' Instructions
        lblInstructions = New Label() With {
            .Text = "Enter New Message.  [ENTER = Keep Current Message]  [N = No Message]",
            .Location = New Point(20, 175),
            .Size = New Size(740, 40),
            .ForeColor = Color.Yellow
        }

        ' New message input
        txtNewMessage = New TextBox() With {
            .Location = New Point(20, 220),
            .Size = New Size(740, 80),
            .Multiline = True,
            .MaxLength = 129,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular),
            .BorderStyle = BorderStyle.FixedSingle
        }

        ' Character count
        lblCharCount = New Label() With {
            .Text = "0 / 129 characters",
            .Location = New Point(20, 305),
            .Size = New Size(740, 20),
            .ForeColor = Color.Gray,
            .TextAlign = ContentAlignment.MiddleRight
        }

        ' Save button
        btnSave = New Button() With {
            .Text = "Save Message",
            .Location = New Point(360, 350),
            .Size = New Size(140, 40),
            .ForeColor = Color.Black,
            .BackColor = Color.LightGreen,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        }

        ' No Message button
        btnNoMessage = New Button() With {
            .Text = "No Message",
            .Location = New Point(510, 350),
            .Size = New Size(130, 40),
            .ForeColor = Color.Black,
            .BackColor = Color.LightGray,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        }

        ' Cancel button
        btnCancel = New Button() With {
            .Text = "Cancel",
            .Location = New Point(650, 350),
            .Size = New Size(130, 40),
            .ForeColor = Color.Black,
            .BackColor = Color.LightGray,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold),
            .DialogResult = DialogResult.Cancel
        }

        ' Add controls
        Me.Controls.Add(lblTitle)
        Me.Controls.Add(lblCurrentMessage)
        Me.Controls.Add(txtCurrentMessage)
        Me.Controls.Add(lblInstructions)
        Me.Controls.Add(txtNewMessage)
        Me.Controls.Add(lblCharCount)
        Me.Controls.Add(btnSave)
        Me.Controls.Add(btnNoMessage)
        Me.Controls.Add(btnCancel)

        Me.CancelButton = btnCancel
        Me.AcceptButton = btnSave

        AddHandler txtNewMessage.TextChanged, AddressOf txtNewMessage_TextChanged
        AddHandler btnSave.Click, AddressOf btnSave_Click
        AddHandler btnNoMessage.Click, AddressOf btnNoMessage_Click
        AddHandler btnCancel.Click, AddressOf btnCancel_Click

        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Private Sub InitializeDosStyle()
        ' Load current message
        Dim currentMsg As String = MessageService.ReadScrollingMessage()
        txtCurrentMessage.Text = If(String.IsNullOrEmpty(currentMsg), "(no message)", currentMsg)

        ' Focus on new message input
        txtNewMessage.Select()
    End Sub

    Private Sub txtNewMessage_TextChanged(sender As Object, e As EventArgs)
        Dim len As Integer = txtNewMessage.Text.Length
        lblCharCount.Text = len.ToString() & " / 129 characters"

        If len > 100 Then
            lblCharCount.ForeColor = Color.Yellow
        ElseIf len > 120 Then
            lblCharCount.ForeColor = Color.Red
        Else
            lblCharCount.ForeColor = Color.Gray
        End If
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        Dim newMessage As String = txtNewMessage.Text.Trim()

        ' If empty, keep current message (DOS behavior: ENTER = Keep Current)
        If String.IsNullOrEmpty(newMessage) Then
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
            Return
        End If

        Try
            MessageService.SaveScrollingMessage(newMessage)
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Catch ex As Exception
            DosMessageBox.Show(Me, "Failed to save message: " & ex.Message, 
                          "Error", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub btnNoMessage_Click(sender As Object, e As EventArgs)
        ' DOS behavior: N = No Message (clears the message)
        Try
            MessageService.SaveScrollingMessage("")
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Catch ex As Exception
            DosMessageBox.Show(Me, "Failed to clear message: " & ex.Message, 
                          "Error", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
            Return True
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function
End Class


