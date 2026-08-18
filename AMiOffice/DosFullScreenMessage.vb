Option Strict Off
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Mimics DOS CLS + PRINT + INPUT behaviour: shows a completely blank black screen
''' with a single message line at the top, waits for ENTER, then closes.
''' Used wherever DOS does: CLS : PRINT "message" : INPUT QQ$
''' </summary>
Public Class DosFullScreenMessage
    Inherits Form

    ''' <summary>
    ''' Show a full-screen DOS-style message and block until the user presses ENTER or ESC.
    ''' </summary>
    Public Shared Sub Show(owner As Form, message As String)
        Using frm As New DosFullScreenMessage(message, owner)
            frm.ShowDialog(owner)
        End Using
    End Sub

    Private Sub New(message As String, owner As Form)
        Me.Text = "ShopCard Generator"
        Me.BackColor = Color.Black
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.ControlBox = True
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        ' Match the owner form's exact size and position
        Me.StartPosition = FormStartPosition.Manual
        Me.Bounds = owner.Bounds
        Me.KeyPreview = True

        ' Bottom bar matching DosMenuFormBase spec exactly
        Dim pnlBottom As New Panel()
        pnlBottom.Dock = DockStyle.Bottom
        pnlBottom.Height = 36
        pnlBottom.BackColor = Color.Black
        pnlBottom.Padding = New Padding(0)

        Dim btnClose As New Button()
        btnClose.Text = "(ESC) Close"
        btnClose.AutoSize = False
        btnClose.Width = 160
        btnClose.Height = 30
        btnClose.Anchor = AnchorStyles.Right Or AnchorStyles.Bottom
        btnClose.Margin = New Padding(0)
        btnClose.UseVisualStyleBackColor = False
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.BackColor = Color.Silver
        btnClose.ForeColor = Color.Black
        btnClose.FlatAppearance.BorderColor = Color.Gainsboro
        btnClose.FlatAppearance.MouseOverBackColor = Color.Gainsboro
        btnClose.FlatAppearance.MouseDownBackColor = Color.DarkGray
        btnClose.FlatAppearance.BorderSize = 1
        btnClose.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        AddHandler btnClose.Click, Sub()
                                       Me.DialogResult = DialogResult.OK
                                       Me.Close()
                                   End Sub

        ' Position button with padding from right and bottom edges
        Dim pad As Integer = 4
        AddHandler pnlBottom.Resize, Sub()
                                         btnClose.Left = pnlBottom.ClientSize.Width - btnClose.Width - pad
                                         btnClose.Top = pad \ 2
                                     End Sub
        pnlBottom.Controls.Add(btnClose)
        Me.Controls.Add(pnlBottom)

        Dim lbl As New Label()
        lbl.Text = message
        lbl.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        lbl.ForeColor = Color.White
        lbl.BackColor = Color.Black
        lbl.AutoSize = True
        lbl.Location = New Point(8, 24)
        lbl.UseMnemonic = False
        Me.Controls.Add(lbl)
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.KeyCode = Keys.Return OrElse e.KeyCode = Keys.Escape Then
            Me.DialogResult = DialogResult.OK
            Me.Close()
            e.Handled = True
        End If
    End Sub
End Class
