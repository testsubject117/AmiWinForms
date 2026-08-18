Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' ShopCard Operation Description Menu — the section hub.
''' Appears after header entry is confirmed. User picks sections in any order.
''' Matches DOS SS-012: SHOPCARD OPERATION DESCRIPTION MENU
''' </summary>
Public Class FormShopCardSectionHub
    Inherits DosMenuFormBase

    Private _record As ShopCardRecord

    Public Sub New(record As ShopCardRecord)
        _record = record
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        SetMenuTitle("SHOPCARD OPERATION DESCRIPTION MENU")

        ShowVersionInHeader = False
        UpdateHeaderClock()

        StretchButtonsToPanelWidth = True
        flpLeft.Padding = New Padding(8, 0, 8, 0)
        flpLeft.AutoScroll = False

        flpRight.Visible = False
        flpRight.Enabled = False

        Me.Width = 1100
        Me.Height = 700

        BuildMenu()
        UpdateCustomerTag()
    End Sub

    Private Sub BuildMenu()
        ClearMenu()
        Dim p = flpLeft

        AddMenuButton(p, "1", SectionLabel(1, "Etch for Weld / Clean / Glassbead / Strip"),
                      Sub() LaunchSection(1))

        AddMenuButton(p, "2", SectionLabel(2, "Electro Polish / Passivate / Salt Spray / High Humidity / Copper Sulfate"),
                      Sub() LaunchSection(2))

        AddMenuButton(p, "3", SectionLabel(3, "Chem Film / Anodize"),
                      Sub() LaunchSection(3))

        AddMenuButton(p, "4", SectionLabel(4, "Magnetic Insp."),
                      Sub() LaunchSection(4))

        AddMenuButton(p, "5", SectionLabel(5, "Penetrant Insp."),
                      Sub() LaunchSection(5))

        AddMenuButton(p, "6", SectionLabel(6, "Dye / Stamp"),
                      Sub() LaunchSection(6))

        AddMenuButton(p, "7", SectionLabel(7, "Cad Plate"),
                      Sub() LaunchSection(7))

        AddMenuButton(p, "8", SectionLabel(8, "Qty to be Insp. / 100%"),
                      Sub() LaunchSection(8))

        AddMenuButton(p, "L", "Look up Spec.", Sub()
                                                   MessageBox.Show("Look up Spec — Not yet implemented.", "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                               End Sub)

        AddMenuButton(p, "S", "Serial Numbers or Anything in body of invoice",
                      Sub() LaunchSerialNumbers())

        AddMenuButton(p, "P", "Print the Shopcard", Sub() LaunchPrint())
    End Sub

    ''' <summary>
    ''' Returns the section button label, with a checkmark if any field in that section is filled.
    ''' </summary>
    Private Function SectionLabel(sectionNum As Integer, title As String) As String
        If SectionHasData(sectionNum) Then
            Return "(" & ChrW(10003) & ") " & title
        End If
        Return title
    End Function

    Private Function SectionHasData(sectionNum As Integer) As Boolean
        For f As Integer = 1 To 19
            If _record.GetSection(sectionNum, f) <> "" Then Return True
        Next
        Return False
    End Function

    Private Sub UpdateCustomerTag()
        lblDateTime.Text = "Customer:  " & _record.CustomerName
    End Sub

    Private Sub LaunchSection(sectionNum As Integer)
        Using frm As New FormShopCardSection(_record, sectionNum)
            frm.ShowDialog(Me)
        End Using
        BuildMenu()   ' refresh checkmarks after returning
        ResizeButtonsToPanel(flpLeft)
    End Sub

    Private Sub LaunchSerialNumbers()
        Dim warning As String =
            "DON'T REPEAT SERIAL #'S ON SHOPCARDS WITH SAME INVOICE  (3.5 LINES PER INVOICE ONLY)" &
            vbCrLf & vbCrLf &
            "Enter Serial #'s:"

        Dim current As String = _record.SerialNumbers
        Dim input As String = InputBox(warning, "Serial Numbers", current)
        If input IsNot Nothing Then
            _record.SerialNumbers = input
        End If
    End Sub

    Private Sub LaunchPrint()
        ' DOS line 2190: P -> GOTO 4300 immediately, no validation on the hub.
        ' Close with OK so the caller (FormShopCardMenu.RunSaveScreen) runs the
        ' write-to-disk + print instruction screens.
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

End Class
