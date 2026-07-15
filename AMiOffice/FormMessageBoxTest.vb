Imports System
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Test form to compare old Windows MessageBox vs new DosMessageBox
''' </summary>
Public Class FormMessageBoxTest
    Inherits Form

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "MessageBox Comparison Test"
        Me.Size = New Size(600, 400)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.Black

        Dim lblTitle As New Label()
        lblTitle.Text = "Click buttons to compare message box styles"
        lblTitle.Location = New Point(50, 30)
        lblTitle.Size = New Size(500, 30)
        lblTitle.Font = New Font("Consolas", 12.0F, FontStyle.Bold)
        lblTitle.ForeColor = Color.Yellow
        lblTitle.BackColor = Color.Black
        Me.Controls.Add(lblTitle)

        ' Old Windows MessageBox button
        Dim btnOld As New Button()
        btnOld.Text = "Show OLD Windows MessageBox (Yes/No)"
        btnOld.Location = New Point(50, 100)
        btnOld.Size = New Size(500, 50)
        btnOld.Font = New Font("Consolas", 10.0F)
        btnOld.BackColor = Color.LightGray
        btnOld.ForeColor = Color.Black
        AddHandler btnOld.Click, Sub()
                                     Dim result = MessageBox.Show(Me,
                                         "This is the OLD Windows MessageBox style." & Environment.NewLine &
                                         "White/gray background, standard Windows buttons." & Environment.NewLine & Environment.NewLine &
                                         "Do you like this style?",
                                         "Old Style",
                                         MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question)
                                     MessageBox.Show(Me, "You clicked: " & result.ToString(), "Result")
                                 End Sub
        Me.Controls.Add(btnOld)

        ' New DosMessageBox button
        Dim btnNew As New Button()
        btnNew.Text = "Show NEW DosMessageBox (Yes/No)"
        btnNew.Location = New Point(50, 170)
        btnNew.Size = New Size(500, 50)
        btnNew.Font = New Font("Consolas", 10.0F)
        btnNew.BackColor = Color.LightGray
        btnNew.ForeColor = Color.Black
        AddHandler btnNew.Click, Sub()
                                     Dim result = DosMessageBox.Show(Me,
                                         "This is the NEW DosMessageBox style." & Environment.NewLine &
                                         "Black background, light gray buttons with bold black text." & Environment.NewLine & Environment.NewLine &
                                         "Do you like this style?",
                                         "New Style",
                                         MessageBoxButtons.YesNo)
                                     DosMessageBox.Show(Me, "You clicked: " & result.ToString(), "Result", MessageBoxButtons.OK)
                                 End Sub
        Me.Controls.Add(btnNew)

        ' OK button example
        Dim btnOK As New Button()
        btnOK.Text = "Show NEW DosMessageBox (OK only)"
        btnOK.Location = New Point(50, 240)
        btnOK.Size = New Size(500, 50)
        btnOK.Font = New Font("Consolas", 10.0F)
        btnOK.BackColor = Color.LightGray
        btnOK.ForeColor = Color.Black
        AddHandler btnOK.Click, Sub()
                                    DosMessageBox.Show(Me,
                                        "This is an informational message." & Environment.NewLine &
                                        "Only one OK button is shown.",
                                        "Information",
                                        MessageBoxButtons.OK)
                                End Sub
        Me.Controls.Add(btnOK)

        ' Close button
        Dim btnClose As New Button()
        btnClose.Text = "Close Test Form"
        btnClose.Location = New Point(200, 310)
        btnClose.Size = New Size(200, 40)
        btnClose.Font = New Font("Consolas", 10.0F)
        btnClose.BackColor = Color.DarkGray
        btnClose.ForeColor = Color.Black
        AddHandler btnClose.Click, Sub() Me.Close()
        Me.Controls.Add(btnClose)
    End Sub
End Class

