Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

' ShopCard Menu - Main Menu
' Legacy DOS: S.ASC (SHOPCARD.BAS)
' Mirrors the DOS submenu: 1, C, S, F, E, M, P, J, U, D, Q
Public Class FormShopCardMenu
    Inherits DosMenuFormBase

    Private _customerName As String = ""

    Public Sub New()
        ' Nothing here — name collection happens in OnShown
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        SetMenuTitle("SHOPCARD GENERATOR")
        ShowVersionInHeader = False
        UpdateHeaderClock()
        StretchButtonsToPanelWidth = True
        flpLeft.Padding = New Padding(8, 0, 8, 0)
        flpLeft.AutoScroll = False
        flpRight.Visible = False
        flpRight.Enabled = False
        ' Collapse right column so buttons fill full width
        Me.Controls.OfType(Of TableLayoutPanel)().
            Where(Function(t) t.ColumnCount = 2 AndAlso t.Controls.Contains(flpLeft)).
            ToList().ForEach(Sub(t)
                                 t.ColumnStyles(0).SizeType = SizeType.Percent
                                 t.ColumnStyles(0).Width = 100.0F
                                 t.ColumnStyles(1).SizeType = SizeType.Absolute
                                 t.ColumnStyles(1).Width = 0.0F
                             End Sub)
        Me.Width = 1024
        Me.Height = 720
        BuildMenu()
        TightenButtons()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        ' Ask for customer name before revealing the menu.
        ' If we already have a last-used name, pre-fill it but still prompt (DOS always prompts).
        ' Hide ourselves so only the name prompt is visible.
        Me.Visible = False
        Dim name As String = FormShopCardHeader.AskCustomerName(Nothing)
        If name Is Nothing OrElse name.Trim() = "" Then
            ' User cancelled — check if we have a carry-forward name to fall back on
            If ShopCardSession.LastCustomerName <> "" Then
                _customerName = ShopCardSession.LastCustomerName
            Else
                Me.Close()
                Return
            End If
        Else
            _customerName = name.Trim().ToUpper()
            ShopCardSession.SaveCustomerName(_customerName)
        End If
        HeaderCustomerName = _customerName
        UpdateHeaderClock()
        Me.Visible = True
    End Sub

    Private Sub BuildMenu()
        ClearMenu()
        Dim p = flpLeft

        AddMenuButton(p, "1", "Create A Shopcard", Sub() LaunchCreateShopCard())

        AddMenuButton(p, "C", "Change Customer Name", Sub()
                                                          MessageBox.Show("Change Customer — Not yet implemented.", "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                      End Sub)

        AddMenuButton(p, "S", "Create A Shopcard With The Same Procedures", Sub()
                                                                                 MessageBox.Show("Same Procedures — Not yet implemented.", "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                                             End Sub)

        AddMenuButton(p, "F", "Find A Shopcard", Sub()
                                                     MessageBox.Show("Find ShopCard — Not yet implemented.", "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                 End Sub)

        AddMenuButton(p, "E", "Edit Master Spec List", Sub()
                                                           MessageBox.Show("Edit Spec List — Not yet implemented.", "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                       End Sub)

        AddMenuButton(p, "M", "Modify A Shopcard That Has Already Been Printed", Sub()
                                                                                     MessageBox.Show("Modify ShopCard — Not yet implemented.", "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                                                 End Sub)

        AddMenuButton(p, "P", "Switch Between Laser & Star  [Current: " & ShopCardSession.PrinterDisplayName() & "]",
                      Sub() TogglePrinter())

        AddMenuButton(p, "J", "Just Enter Quantity & Part# For FAA", Sub()
                                                                          MessageBox.Show("FAA Entry — Not yet implemented.", "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                                      End Sub)

        AddMenuButton(p, "U", "View Shopcards That Have Not Been Printed As Invoices", Sub()
                                                                                            MessageBox.Show("View Unprinted — Not yet implemented.", "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                                                        End Sub)

        AddMenuButton(p, "D", "Delete/Void A Shopcard", Sub()
                                                             MessageBox.Show("Delete/Void — Not yet implemented.", "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                         End Sub)

        AddMenuButton(p, "Q", "Quit", Sub() Me.Close())
    End Sub

    Private Sub LaunchCreateShopCard()
        Using frm As New FormShopCardHeader(ShopCardSession.LastCustomerName)
            If frm.ShowDialog(Me) = DialogResult.OK AndAlso frm.Accepted Then
                Using hub As New FormShopCardSectionHub(frm.Result)
                    hub.ShowDialog(Me)
                End Using
            End If
        End Using
    End Sub

    Private Sub TogglePrinter()
        ShopCardSession.UseLaserPrinter = Not ShopCardSession.UseLaserPrinter
        BuildMenu()
        TightenButtons()
    End Sub

    Private Sub TightenButtons()
        For Each btn As Control In flpLeft.Controls
            If TypeOf btn Is Button Then
                btn.Width = flpLeft.ClientSize.Width - flpLeft.Padding.Horizontal
            End If
        Next
    End Sub

End Class
