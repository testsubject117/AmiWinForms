Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Option H: Quick Message Flashing (Subliminal Message Generator)
''' DOS-faithful implementation from MAINM.ASC lines 2300-2380.
''' Password protected: "dean" or "DEAN"
''' </summary>
Public Class FormQuickMessageFlashing
    Inherits Form

    Private lblTitle As Label
    Private lblPasswordPrompt As Label
    Private txtPassword As TextBox
    Private btnPasswordOK As Button
    Private btnCancel As Button

    ' Second screen after password
    Private pnlMessageEditor As Panel
    Private lblMessageTitle As Label
    Private lblCurrentMessage As Label
    Private txtCurrentMessage As TextBox
    Private lblInstructions As Label
    Private txtNewMessage As TextBox
    Private lblCharCount As Label
    Private btnSaveMessage As Button
    Private btnKeepCurrent As Button

    Private _passwordAccepted As Boolean = False

    Public Sub New()
        InitializeComponent()
        InitializeDosStyle()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        ' Form settings
        Me.Text = "Quick Message Flashing"
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Size = New Size(700, 400)
        Me.KeyPreview = True

        ' Password screen
        lblTitle = New Label() With {
            .Text = "Quick Message Flashing",
            .Location = New Point(20, 20),
            .Size = New Size(640, 30),
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 14.0F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        lblPasswordPrompt = New Label() With {
            .Text = "Enter Password:",
            .Location = New Point(180, 140),
            .Size = New Size(150, 25),
            .ForeColor = Color.Cyan,
            .Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        }

        txtPassword = New TextBox() With {
            .Location = New Point(340, 138),
            .Size = New Size(180, 25),
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 11.0F, FontStyle.Regular),
            .BorderStyle = BorderStyle.FixedSingle,
            .UseSystemPasswordChar = True
        }

        btnPasswordOK = New Button() With {
            .Text = "OK",
            .Location = New Point(260, 200),
            .Size = New Size(80, 35),
            .ForeColor = Color.Black,
            .BackColor = Color.LightGreen,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        }

        btnCancel = New Button() With {
            .Text = "Cancel",
            .Location = New Point(350, 200),
            .Size = New Size(90, 35),
            .ForeColor = Color.Black,
            .BackColor = Color.LightGray,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold),
            .DialogResult = DialogResult.Cancel
        }

        ' Message editor panel (initially hidden)
        pnlMessageEditor = New Panel() With {
            .Location = New Point(0, 0),
            .Size = Me.ClientSize,
            .BackColor = Color.Black,
            .Visible = False
        }

        lblMessageTitle = New Label() With {
            .Text = "<<< SUBLIMINAL MESSAGE GENERATOR >>>",
            .Location = New Point(20, 20),
            .Size = New Size(640, 30),
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 14.0F, FontStyle.Bold),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        lblCurrentMessage = New Label() With {
            .Text = "Current message:",
            .Location = New Point(20, 70),
            .Size = New Size(640, 20),
            .ForeColor = Color.Cyan
        }

        txtCurrentMessage = New TextBox() With {
            .Location = New Point(20, 95),
            .Size = New Size(640, 40),
            .Multiline = True,
            .ReadOnly = True,
            .BackColor = Color.FromArgb(0, 0, 64),
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular),
            .BorderStyle = BorderStyle.FixedSingle
        }

        lblInstructions = New Label() With {
            .Text = "Enter new message [Enter = Current Message]",
            .Location = New Point(20, 150),
            .Size = New Size(640, 20),
            .ForeColor = Color.Yellow
        }

        txtNewMessage = New TextBox() With {
            .Location = New Point(20, 175),
            .Size = New Size(640, 60),
            .Multiline = True,
            .MaxLength = 35,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular),
            .BorderStyle = BorderStyle.FixedSingle
        }

        lblCharCount = New Label() With {
            .Text = "0 / 35 characters",
            .Location = New Point(20, 240),
            .Size = New Size(640, 20),
            .ForeColor = Color.Gray,
            .TextAlign = ContentAlignment.MiddleRight
        }

        btnSaveMessage = New Button() With {
            .Text = "Save Message",
            .Location = New Point(400, 290),
            .Size = New Size(130, 40),
            .ForeColor = Color.Black,
            .BackColor = Color.LightGreen,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        }

        btnKeepCurrent = New Button() With {
            .Text = "Keep Current",
            .Location = New Point(540, 290),
            .Size = New Size(120, 40),
            .ForeColor = Color.Black,
            .BackColor = Color.LightGray,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        }

        ' Add password screen controls
        Me.Controls.Add(lblTitle)
        Me.Controls.Add(lblPasswordPrompt)
        Me.Controls.Add(txtPassword)
        Me.Controls.Add(btnPasswordOK)
        Me.Controls.Add(btnCancel)

        ' Add message editor controls to panel
        pnlMessageEditor.Controls.Add(lblMessageTitle)
        pnlMessageEditor.Controls.Add(lblCurrentMessage)
        pnlMessageEditor.Controls.Add(txtCurrentMessage)
        pnlMessageEditor.Controls.Add(lblInstructions)
        pnlMessageEditor.Controls.Add(txtNewMessage)
        pnlMessageEditor.Controls.Add(lblCharCount)
        pnlMessageEditor.Controls.Add(btnSaveMessage)
        pnlMessageEditor.Controls.Add(btnKeepCurrent)

        Me.Controls.Add(pnlMessageEditor)

        Me.CancelButton = btnCancel

        AddHandler btnPasswordOK.Click, AddressOf btnPasswordOK_Click
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        AddHandler txtNewMessage.TextChanged, AddressOf txtNewMessage_TextChanged
        AddHandler btnSaveMessage.Click, AddressOf btnSaveMessage_Click
        AddHandler btnKeepCurrent.Click, AddressOf btnKeepCurrent_Click

        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Private Sub InitializeDosStyle()
        txtPassword.Select()
    End Sub

    Private Sub btnPasswordOK_Click(sender As Object, e As EventArgs)
        If MessageService.ValidateSubliminalPassword(txtPassword.Text) Then
            ShowMessageEditor()
        Else
            ' DOS behavior: wrong password just returns to main menu
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Private Sub ShowMessageEditor()
        _passwordAccepted = True

        ' Hide password controls
        lblTitle.Visible = False
        lblPasswordPrompt.Visible = False
        txtPassword.Visible = False
        btnPasswordOK.Visible = False
        btnCancel.Visible = False

        ' Load current message
        Dim currentMsg As String = MessageService.ReadSubliminalMessage()
        txtCurrentMessage.Text = If(String.IsNullOrEmpty(currentMsg), "(no message)", currentMsg)

        ' Show message editor
        pnlMessageEditor.Visible = True
        txtNewMessage.Select()

        ' Change cancel behavior
        Me.CancelButton = btnKeepCurrent
    End Sub

    Private Sub txtNewMessage_TextChanged(sender As Object, e As EventArgs)
        Dim len As Integer = txtNewMessage.Text.Length
        lblCharCount.Text = len.ToString() & " / 35 characters"

        If len > 30 Then
            lblCharCount.ForeColor = If(len > 33, Color.Red, Color.Yellow)
        Else
            lblCharCount.ForeColor = Color.Gray
        End If
    End Sub

    Private Sub btnSaveMessage_Click(sender As Object, e As EventArgs)
        Dim newMessage As String = txtNewMessage.Text.Trim()

        ' DOS behavior: empty input means keep current message
        If String.IsNullOrEmpty(newMessage) Then
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
            Return
        End If

        Try
            MessageService.SaveSubliminalMessage(newMessage)
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Catch ex As ArgumentException
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Catch ex As Exception
            MessageBox.Show("Failed to save message: " & ex.Message, 
                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnKeepCurrent_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
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

        ' Password screen: Enter = OK button
        If Not _passwordAccepted AndAlso keyData = Keys.Enter Then
            btnPasswordOK.PerformClick()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function
End Class
