Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Partial Public Class FormLedgerMenu
    Inherits DosMenuFormBase

    Private Enum ViewChecksPromptStep
        None = 0
        Customer = 1
        CheckNumber = 2
        Year = 3
    End Enum

    Private ReadOnly pnlPromptHost As New Panel()
    Private ReadOnly pnlPromptSeparator As New Panel()
    Private ReadOnly lblPrompt As New Label()
    Private ReadOnly txtPrompt As New TextBox()

    Private _viewChecksPromptStep As ViewChecksPromptStep = ViewChecksPromptStep.None
    Private _viewChecksCustomer As String = ""
    Private _viewChecksCheckNumber As String = ""
    Private _viewChecksYear As String = ""

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        SetMenuTitle("CHECKS")

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

        BuildLedgerMenu()
        TightenChecksMenuButtons()
        BuildInlinePromptUi()
    End Sub

    Private Sub BuildLedgerMenu()
        ClearMenu()

        Dim p = flpLeft

        AddMenuButton(p, "1", "Add Checks to ledger & cash receipts", Sub()
                                                                          Using f As New FormAddChecks()
                                                                              f.ShowDialog(Me)
                                                                          End Using
                                                                      End Sub)
        AddMenuButton(p, "2", "Delete Checks", Sub()
                                                   Using f As New FormDeleteCheck()
                                                       f.ShowDialog(Me)
                                                   End Using
                                               End Sub)
        AddMenuButton(p, "3", "View Checks", Sub() BeginViewChecksPromptFlow())
        AddMenuButton(p, "5", "View Companys Totals", Sub()
                                                          Using f As New FormLedgerCompanyTotals()
                                                              f.ShowDialog(Me)
                                                          End Using
                                                      End Sub)
        AddMenuButton(p, "6", "Add OTHER Checks", Sub()
                                                      Using f As New FormAddOtherCheck()
                                                          f.ShowDialog(Me)
                                                      End Using
                                                  End Sub)
        AddMenuButton(p, "7", "Delete OTHER Checks", Sub()
                                                         Using f As New FormDeleteOtherCheck()
                                                             f.ShowDialog(Me)
                                                         End Using
                                                     End Sub)
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
        AddMenuButton(p, "A", "Find Checks that don't balance.", Sub()
                                                                     Using f As New FormLedgerDoesntBalance()
                                                                         f.ShowDialog(Me)
                                                                     End Using
                                                                 End Sub)
        AddMenuButton(p, "S", "Sales Employee's checks", Sub()
                                                             Using f As New FormSalesEmployeesChecksMenu()
                                                                 f.ShowDialog(Me)
                                                             End Using
                                                         End Sub)
    End Sub

    Private Sub TightenChecksMenuButtons()
        For Each c As Control In flpLeft.Controls
            Dim btn = TryCast(c, Button)
            If btn IsNot Nothing Then
                btn.Height = 30
                btn.Margin = New Padding(3, 3, 3, 4)
                btn.Font = New Font("Segoe UI", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
            End If
        Next
    End Sub

    Private Sub BuildInlinePromptUi()
        pnlPromptHost.Visible = False
        pnlPromptHost.Dock = DockStyle.Bottom
        pnlPromptHost.Height = 96
        pnlPromptHost.BackColor = Color.FromArgb(32, 32, 32)
        pnlPromptHost.Padding = New Padding(12, 8, 12, 10)

        pnlPromptSeparator.Dock = DockStyle.Top
        pnlPromptSeparator.Height = 2
        pnlPromptSeparator.BackColor = Color.Silver

        lblPrompt.AutoSize = False
        lblPrompt.Dock = DockStyle.Top
        lblPrompt.Height = 28
        lblPrompt.TextAlign = ContentAlignment.MiddleLeft
        lblPrompt.ForeColor = Color.White
        lblPrompt.BackColor = pnlPromptHost.BackColor
        lblPrompt.Font = New Font("Consolas", 13.0F, FontStyle.Regular, GraphicsUnit.Point)
        lblPrompt.Padding = New Padding(0, 8, 0, 0)

        txtPrompt.Dock = DockStyle.Top
        txtPrompt.Height = 30
        txtPrompt.BorderStyle = BorderStyle.FixedSingle
        txtPrompt.BackColor = Color.Black
        txtPrompt.ForeColor = Color.White
        txtPrompt.Font = New Font("Consolas", 13.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtPrompt.MaxLength = 100
        txtPrompt.Margin = New Padding(0, 6, 0, 0)

        AddHandler txtPrompt.KeyDown, AddressOf OnPromptTextBoxKeyDown

        pnlPromptHost.Controls.Add(txtPrompt)
        pnlPromptHost.Controls.Add(lblPrompt)
        pnlPromptHost.Controls.Add(pnlPromptSeparator)

        Controls.Add(pnlPromptHost)
        pnlPromptHost.BringToFront()
    End Sub

    Private Sub BeginViewChecksPromptFlow()
        _viewChecksCustomer = ""
        _viewChecksCheckNumber = ""
        _viewChecksYear = ""
        _viewChecksPromptStep = ViewChecksPromptStep.Customer

        ShowPrompt("Enter customer name [Enter = all customers] ?", "")
    End Sub

    Private Sub ShowPrompt(promptText As String, currentValue As String)
        lblPrompt.Text = promptText
        txtPrompt.Text = currentValue
        pnlPromptHost.Visible = True
        txtPrompt.SelectionStart = txtPrompt.TextLength
        txtPrompt.SelectionLength = 0
        txtPrompt.Focus()
    End Sub

    Private Sub HidePrompt()
        pnlPromptHost.Visible = False
        lblPrompt.Text = ""
        txtPrompt.Text = ""
        _viewChecksPromptStep = ViewChecksPromptStep.None
        Me.Focus()
    End Sub

    Private Sub OnPromptTextBoxKeyDown(sender As Object, e As KeyEventArgs)
        If _viewChecksPromptStep = ViewChecksPromptStep.None Then
            Return
        End If

        If e.KeyCode = Keys.Escape Then
            e.SuppressKeyPress = True
            e.Handled = True
            HidePrompt()
            Return
        End If

        If e.KeyCode <> Keys.Enter Then
            Return
        End If

        e.SuppressKeyPress = True
        e.Handled = True

        Dim value As String = txtPrompt.Text.Trim()

        Select Case _viewChecksPromptStep
            Case ViewChecksPromptStep.Customer
                _viewChecksCustomer = value
                _viewChecksPromptStep = ViewChecksPromptStep.CheckNumber
                ShowPrompt("Enter Check Number [Enter = All Checks] ?", "")
                Return

            Case ViewChecksPromptStep.CheckNumber
                _viewChecksCheckNumber = value
                _viewChecksPromptStep = ViewChecksPromptStep.Year
                ShowPrompt("Enter Year [Enter = All Years] ?", "")
                Return

            Case ViewChecksPromptStep.Year
                _viewChecksYear = value
                HidePrompt()
                OpenLedgerViewWithPromptValues()
                Return
        End Select
    End Sub

    Private Sub OpenLedgerViewWithPromptValues()
        Using f As New FormLedgerView()
            f.InitialCustomerFilter = _viewChecksCustomer
            f.InitialCheckFilter = _viewChecksCheckNumber
            f.InitialYearFilter = NormalizeYearFilter(_viewChecksYear)
            f.ShowDialog(Me)
        End Using
    End Sub

    Private Function NormalizeYearFilter(value As String) As String
        Dim s As String = If(value, "").Trim()

        If s = "" Then
            Return ""
        End If

        Dim yearNum As Integer
        If Integer.TryParse(s, yearNum) Then
            If s.Length <= 2 Then
                If yearNum <= 79 Then
                    Return (2000 + yearNum).ToString()
                Else
                    Return (1900 + yearNum).ToString()
                End If
            End If

            If s.Length = 4 Then
                Return s
            End If
        End If

        Return s
    End Function

End Class