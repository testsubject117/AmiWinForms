Imports System
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Custom DOS-style message box to match application theme
''' Replaces standard MessageBox with black/yellow DOS aesthetic
''' </summary>
Public Class DosMessageBox
    Inherits Form

    Private _result As DialogResult = DialogResult.OK

    Public Shared Function Show(owner As IWin32Window, message As String, title As String, buttons As MessageBoxButtons) As DialogResult
        Using dlg As New DosMessageBox(message, title, buttons)
            If owner IsNot Nothing Then
                dlg.ShowDialog(owner)
            Else
                dlg.ShowDialog()
            End If
            Return dlg._result
        End Using
    End Function

    Public Shared Function Show(owner As IWin32Window, message As String, title As String, buttons As MessageBoxButtons, icon As MessageBoxIcon) As DialogResult
        ' Ignore icon parameter, use same styling
        Return Show(owner, message, title, buttons)
    End Function

    Public Shared Function Show(owner As IWin32Window, message As String, title As String, buttons As MessageBoxButtons, icon As MessageBoxIcon, defaultButton As MessageBoxDefaultButton) As DialogResult
        ' Ignore icon and defaultButton parameters, use same styling
        Return Show(owner, message, title, buttons)
    End Function

    Public Shared Sub Show(owner As IWin32Window, message As String, title As String)
        Show(owner, message, title, MessageBoxButtons.OK)
    End Sub

    Private Sub New(message As String, title As String, buttons As MessageBoxButtons)
        InitializeComponent(message, title, buttons)
    End Sub

    Private Sub InitializeComponent(message As String, title As String, buttons As MessageBoxButtons)
        Me.Text = title
        Me.Size = New Size(500, 250)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.Black
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowInTaskbar = False

        ' Message label
        Dim lblMessage = New Label()
        lblMessage.Text = message
        lblMessage.Location = New Point(20, 30)
        lblMessage.Size = New Size(450, 120)
        lblMessage.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        lblMessage.ForeColor = Color.White
        lblMessage.BackColor = Color.Black
        lblMessage.AutoSize = False
        Me.Controls.Add(lblMessage)

        ' Add buttons based on MessageBoxButtons
        Select Case buttons
            Case MessageBoxButtons.OK
                Dim btnOK = CreateButton("OK", 200, 170)
                AddHandler btnOK.Click, Sub()
                                            _result = DialogResult.OK
                                            Me.Close()
                                        End Sub
                Me.Controls.Add(btnOK)
                Me.AcceptButton = btnOK

            Case MessageBoxButtons.YesNo
                Dim btnYes = CreateButton("Yes", 150, 170)
                AddHandler btnYes.Click, Sub()
                                             _result = DialogResult.Yes
                                             Me.Close()
                                         End Sub
                Me.Controls.Add(btnYes)

                Dim btnNo = CreateButton("No", 270, 170)
                AddHandler btnNo.Click, Sub()
                                            _result = DialogResult.No
                                            Me.Close()
                                        End Sub
                Me.Controls.Add(btnNo)
                Me.AcceptButton = btnYes
                Me.CancelButton = btnNo

            Case MessageBoxButtons.OKCancel
                Dim btnOK = CreateButton("OK", 150, 170)
                AddHandler btnOK.Click, Sub()
                                            _result = DialogResult.OK
                                            Me.Close()
                                        End Sub
                Me.Controls.Add(btnOK)

                Dim btnCancel = CreateButton("Cancel", 270, 170)
                AddHandler btnCancel.Click, Sub()
                                                _result = DialogResult.Cancel
                                                Me.Close()
                                            End Sub
                Me.Controls.Add(btnCancel)
                Me.AcceptButton = btnOK
                Me.CancelButton = btnCancel
        End Select
    End Sub

    Private Function CreateButton(text As String, x As Integer, y As Integer) As Button
        Dim btn As New Button()
        btn.Text = text
        btn.Location = New Point(x, y)
        btn.Size = New Size(100, 35)
        btn.Font = New Font("Consolas", 9.0F, FontStyle.Bold)
        btn.BackColor = Color.LightGray
        btn.ForeColor = Color.Black
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderColor = Color.DimGray
        Return btn
    End Function
End Class

