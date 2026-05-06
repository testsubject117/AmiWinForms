Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO

Public Class FormSalesEmployeesChecksMenu
    Inherits DosMenuFormBase

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        ' Match CHECKS menu sizing + layout
        Me.Width = 1000
        Me.Height = 720

        ShowVersionInHeader = False
        UpdateHeaderClock()

        StretchButtonsToPanelWidth = False
        ButtonFixedWidthPx = 620
        flpLeft.Padding = New Padding(24, 0, 0, 0)

        flpRight.Visible = False
        flpRight.Enabled = False
        flpLeft.AutoScroll = False

        ClearMenu()
        SetMenuTitle("Sales Employee's checks")

        ' (A) Open EMPNAME.DAT editor (DOS opened EDIT)
        AddMenuButton(flpLeft, "A", "Modify/View Sales Employee List && their customers", Sub()
                                                                                              Using f As New FormEmpNameEditor()
                                                                                                  f.ShowDialog(Me)
                                                                                              End Using
                                                                                          End Sub)

        ' (B) Return to the already-open CHECKS menu (don't open another one)
        AddMenuButton(flpLeft, "B", "Make a check for an Employee.", Sub()
                                                                         Me.DialogResult = System.Windows.Forms.DialogResult.OK
                                                                         Me.Close()

                                                                         If Me.Owner IsNot Nothing Then
                                                                             Me.Owner.Activate()
                                                                             Me.Owner.BringToFront()
                                                                         End If
                                                                     End Sub)

        ' No (Q); closing handled by base "(ESC) Close"
    End Sub
End Class