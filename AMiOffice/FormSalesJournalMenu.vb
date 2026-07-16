Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

' Sales Journal Menu - Main Menu Option D
' Legacy DOS: SALES.BAS
' Mirrors the DOS submenu with 8 options.
Public Class FormSalesJournalMenu
    Inherits DosMenuFormBase

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        SetMenuTitle("SALES JOURNAL")

        ShowVersionInHeader = False
        UpdateHeaderClock()

        StretchButtonsToPanelWidth = True
        flpLeft.Padding = New Padding(8, 0, 8, 0)
        flpLeft.AutoScroll = False

        flpRight.Visible = False
        flpRight.Enabled = False

        Me.Width = 1100
        Me.Height = 560

        BuildMenu()
        TightenButtons()
    End Sub

    Private Sub BuildMenu()
        ClearMenu()
        Dim p = flpLeft

        AddMenuButton(p, "1", "View One Customer", Sub()
                                                       Using f As New FormSalesJournalViewCustomer()
                                                           f.ShowDialog(Me)
                                                       End Using
                                                   End Sub)

        AddMenuButton(p, "2", "View All Customers", Sub()
                                                        Using f As New FormSalesJournalViewAll()
                                                            f.ShowDialog(Me)
                                                        End Using
                                                    End Sub)

        AddMenuButton(p, "3", "Look Up An Invoice", Sub()
                                                        Using f As New FormSalesJournalLookupInvoice()
                                                            f.ShowDialog(Me)
                                                        End Using
                                                    End Sub)

        AddMenuButton(p, "4", "List All Processes With Totals & % Of Business", Sub()
                                                                                     Using f As New FormSalesJournalTotalsReport(allCompanies:=True)
                                                                                         f.ShowDialog(Me)
                                                                                     End Using
                                                                                 End Sub)

        AddMenuButton(p, "5", "List All Processes With Totals & % Of Business - One Company", Sub()
                                                                                                  Using f As New FormSalesJournalTotalsReport(allCompanies:=False)
                                                                                                      f.ShowDialog(Me)
                                                                                                  End Using
                                                                                              End Sub)

        AddMenuButton(p, "6", "Delete An Invoice", Sub()
                                                       Using f As New FormSalesJournalDeleteInvoice()
                                                           f.ShowDialog(Me)
                                                       End Using
                                                   End Sub)

        AddMenuButton(p, "7", "Look Up A Procedure", Sub()
                                                         Using f As New FormSalesJournalViewProcedure()
                                                             f.ShowDialog(Me)
                                                         End Using
                                                     End Sub)

        AddMenuButton(p, "Q", "Quit - Return To Main Menu", Sub() Me.Close())
    End Sub

    Private Sub TightenButtons()
        For Each c As Control In flpLeft.Controls
            Dim btn = TryCast(c, Button)
            If btn IsNot Nothing Then
                btn.Height = 34
                btn.Margin = New Padding(3, 3, 3, 4)
                btn.Font = New Font("Segoe UI", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
            End If
        Next
    End Sub
End Class
