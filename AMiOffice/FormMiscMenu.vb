Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Partial Public Class FormMiscMenu
    Inherits DosMenuFormBase

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        SetMenuTitle("MISC. MENU (FUTURE USE)")

        ShowVersionInHeader = False
        UpdateHeaderClock()

        StretchButtonsToPanelWidth = False
        ButtonFixedWidthPx = 620
        flpLeft.Padding = New Padding(24, 0, 0, 0)

        flpRight.Visible = False
        flpRight.Enabled = False

        flpLeft.AutoScroll = False

        Me.Width = 1000
        Me.Height = 720

        BuildMiscMenu()
        TightenMiscMenuButtons()
    End Sub

    Private Sub BuildMiscMenu()
        ClearMenu()

        Dim p = flpLeft

        AddMenuButton(p, "T", "Test Message Box Styles (Old vs New)", Sub()
                                                                          Dim testForm As New FormMessageBoxTest()
                                                                          testForm.ShowDialog(Me)
                                                                      End Sub)
        AddMenuButton(p, "A", "Placeholder Item A - Not Yet Implemented", Sub()
                                                                              NotYet("Placeholder Item A")
                                                                          End Sub)
        AddMenuButton(p, "B", "Placeholder Item B - Not Yet Implemented", Sub()
                                                                              NotYet("Placeholder Item B")
                                                                          End Sub)
        AddMenuButton(p, "C", "Placeholder Item C - Not Yet Implemented", Sub()
                                                                              NotYet("Placeholder Item C")
                                                                          End Sub)
        AddMenuButton(p, "D", "Placeholder Item D - Not Yet Implemented", Sub()
                                                                              NotYet("Placeholder Item D")
                                                                          End Sub)
        AddMenuButton(p, "E", "Placeholder Item E - Not Yet Implemented", Sub()
                                                                              NotYet("Placeholder Item E")
                                                                          End Sub)
        AddMenuButton(p, "F", "Placeholder Item F - Not Yet Implemented", Sub()
                                                                              NotYet("Placeholder Item F")
                                                                          End Sub)
    End Sub

    Private Sub TightenMiscMenuButtons()
        For Each c As Control In flpLeft.Controls
            Dim btn = TryCast(c, Button)
            If btn IsNot Nothing Then
                btn.Height = 30
                btn.Margin = New Padding(3, 3, 3, 4)
                btn.Font = New Font("Segoe UI", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
            End If
        Next
    End Sub

End Class
