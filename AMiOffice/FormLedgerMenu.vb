Option Strict Off
Option Explicit On

Imports System
Imports System.Windows.Forms

Partial Public Class FormLedgerMenu
    Inherits DosMenuFormBase

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        SetMenuTitle("CHECKS")

        ' No version/build on this menu
        ShowVersionInHeader = False
        UpdateHeaderClock()

        ' One vertical column, indented left, fixed-width buttons
        StretchButtonsToPanelWidth = False
        ButtonFixedWidthPx = 620
        flpLeft.Padding = New Padding(24, 0, 0, 0)

        ' Disable the second column entirely
        flpRight.Visible = False
        flpRight.Enabled = False

        ' The bottom ESC Close button is now in the base bottom bar,
        ' so do NOT add it as a menu button.
        flpLeft.AutoScroll = False

        Me.Width = 1000
        Me.Height = 720

        BuildLedgerMenu()
    End Sub

    Private Sub BuildLedgerMenu()
        ClearMenu()

        Dim p = flpLeft

        AddMenuButton(p, "1", "Add Checks to ledger & cash receipts", Sub() NotYet("Add Checks to ledger & cash receipts"))
        AddMenuButton(p, "2", "Delete Checks", Sub() NotYet("Delete Checks"))
        AddMenuButton(p, "3", "View Checks", Sub() OpenViewChecksPromptFlow())
        AddMenuButton(p, "5", "View Companys Totals", Sub()
                                                          Using f As New FormLedgerCompanyTotals()
                                                              f.ShowDialog(Me)
                                                          End Using
                                                      End Sub)
        AddMenuButton(p, "6", "Add OTHER Checks", Sub() NotYet("Add OTHER Checks"))
        AddMenuButton(p, "7", "Delete OTHER Checks", Sub() NotYet("Delete OTHER Checks"))
        AddMenuButton(p, "8", "View OTHER Checks", Sub()
                                                       Using f As New FormOtherChecksView()
                                                           f.ShowDialog(Me)
                                                       End Using
                                                   End Sub)
        AddMenuButton(p, "9", "Find a Check #", Sub()
                                                    Using f As New FormFindByCheckNumber()
                                                        f.ShowDialog(Me)
                                                    End Using
                                                End Sub)
        AddMenuButton(p, "0", "Find an Invoice #", Sub()
                                                       Using f As New FormFindByInvoiceNumber()
                                                           f.ShowDialog(Me)
                                                       End Using
                                                   End Sub)
        AddMenuButton(p, "A", "Find Checks that don't balance.", Sub() NotYet("Find Checks that don't balance"))
        AddMenuButton(p, "S", "Sales Employee's checks", Sub() NotYet("Sales Employee's checks"))
    End Sub

    Private Sub OpenViewChecksPromptFlow()
        Dim cust As String = ShowPromptDialog("Enter customer name [Enter = all customers] ?", "")
        If cust Is Nothing Then Return

        Dim chk As String = ShowPromptDialog("Enter Check Number [Enter = All Checks] ?", "")
        If chk Is Nothing Then Return

        Dim yr As String = ShowPromptDialog("Enter Year [Enter = All Years] ?", "")
        If yr Is Nothing Then Return

        Using f As New FormLedgerView()
            f.ShowDialog(Me)
        End Using
    End Sub

    Private Function ShowPromptDialog(promptText As String, defaultValue As String) As String
        Using f As New FormLedgerPrompt(promptText, defaultValue)
            Dim dr = f.ShowDialog(Me)
            If dr <> DialogResult.OK Then Return Nothing
            Return f.Value
        End Using
    End Function

    Private NotInheritable Class FormLedgerPrompt
        Inherits Form

        Private ReadOnly txtValue As New TextBox()

        Public ReadOnly Property Value As String
            Get
                Return txtValue.Text
            End Get
        End Property

        Public Sub New(promptText As String, defaultValue As String)
            Me.Text = "CHECKS"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MinimizeBox = False
            Me.MaximizeBox = False
            Me.ShowInTaskbar = False
            Me.ClientSize = New Drawing.Size(620, 140)

            Dim layout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(10),
                .ColumnCount = 1,
                .RowCount = 3
            }
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            Dim lbl As New Label() With {.AutoSize = True, .Text = promptText}

            txtValue.Dock = DockStyle.Top
            txtValue.Text = defaultValue

            Dim buttons As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft, .AutoSize = True}
            Dim ok As New Button() With {.Text = "OK", .DialogResult = DialogResult.OK, .AutoSize = True}
            Dim cancel As New Button() With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .AutoSize = True}
            buttons.Controls.Add(ok)
            buttons.Controls.Add(cancel)

            layout.Controls.Add(lbl, 0, 0)
            layout.Controls.Add(txtValue, 0, 1)
            layout.Controls.Add(buttons, 0, 2)

            Me.Controls.Add(layout)
            Me.AcceptButton = ok
            Me.CancelButton = cancel
        End Sub
    End Class

End Class