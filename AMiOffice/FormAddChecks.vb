Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

Public Class FormAddChecks
    Inherits Form

    Private lblTitle As Label
    Private lblLine1 As Label
    Private lblLine2 As Label
    Private lblLine3 As Label
    Private lblLine4 As Label
    Private lblLine5 As Label
    Private lblLine6 As Label
    Private lblLine7 As Label
    Private lblLine8 As Label
    Private lblPrompt As Label
    Private txtInput As TextBox
    Private btnCancel As Button

    Private currentStep As Integer
    Private enteredCompany As String
    Private enteredAmount As String
    Private enteredLowestInvoice As String
    Private enteredHighestInvoice As String
    Private enteredCheckNumber As String
    Private enteredDateText As String
    Private enteredCheckReference As String

    Public Sub New()
        MyBase.New()
        InitializeCustomComponents()
    End Sub

    Private Sub FormAddChecks_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ShowCompanyPrompt()
    End Sub

    Private Sub InitializeCustomComponents()
        Me.Text = "Add A Check"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Size = New Size(980, 640)
        Me.MinimumSize = New Size(980, 640)
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 12.0!, FontStyle.Regular)
        Me.KeyPreview = True

        lblTitle = New Label()
        lblTitle.AutoSize = False
        lblTitle.Text = "***** Add A Check *****"
        lblTitle.TextAlign = ContentAlignment.MiddleLeft
        lblTitle.Font = New Font("Consolas", 16.0!, FontStyle.Regular)
        lblTitle.ForeColor = Color.White
        lblTitle.BackColor = Color.Black
        lblTitle.SetBounds(24, 20, 920, 32)

        lblLine1 = New Label()
        lblLine1.AutoSize = False
        lblLine1.SetBounds(24, 80, 920, 28)
        lblLine1.ForeColor = Color.White
        lblLine1.BackColor = Color.Black

        lblLine2 = New Label()
        lblLine2.AutoSize = False
        lblLine2.SetBounds(24, 112, 920, 28)
        lblLine2.ForeColor = Color.White
        lblLine2.BackColor = Color.Black

        lblLine3 = New Label()
        lblLine3.AutoSize = False
        lblLine3.SetBounds(24, 144, 920, 28)
        lblLine3.ForeColor = Color.White
        lblLine3.BackColor = Color.Black

        lblLine4 = New Label()
        lblLine4.AutoSize = False
        lblLine4.SetBounds(24, 176, 920, 28)
        lblLine4.ForeColor = Color.White
        lblLine4.BackColor = Color.Black

        lblLine5 = New Label()
        lblLine5.AutoSize = False
        lblLine5.SetBounds(24, 208, 920, 28)
        lblLine5.ForeColor = Color.White
        lblLine5.BackColor = Color.Black

        lblLine6 = New Label()
        lblLine6.AutoSize = False
        lblLine6.SetBounds(24, 240, 920, 28)
        lblLine6.ForeColor = Color.White
        lblLine6.BackColor = Color.Black

        lblLine7 = New Label()
        lblLine7.AutoSize = False
        lblLine7.SetBounds(24, 272, 920, 28)
        lblLine7.ForeColor = Color.White
        lblLine7.BackColor = Color.Black

        lblLine8 = New Label()
        lblLine8.AutoSize = False
        lblLine8.SetBounds(24, 304, 920, 28)
        lblLine8.ForeColor = Color.White
        lblLine8.BackColor = Color.Black

        lblPrompt = New Label()
        lblPrompt.AutoSize = False
        lblPrompt.SetBounds(24, 372, 920, 28)
        lblPrompt.ForeColor = Color.White
        lblPrompt.BackColor = Color.Black

        txtInput = New TextBox()
        txtInput.BorderStyle = BorderStyle.FixedSingle
        txtInput.Font = New Font("Consolas", 12.0!, FontStyle.Regular)
        txtInput.CharacterCasing = CharacterCasing.Upper
        txtInput.SetBounds(24, 407, 420, 30)

        btnCancel = New Button()
        btnCancel.Text = "Cancel"
        btnCancel.SetBounds(820, 540, 110, 34)
        AddHandler btnCancel.Click, AddressOf btnCancel_Click

        Me.Controls.Add(lblTitle)
        Me.Controls.Add(lblLine1)
        Me.Controls.Add(lblLine2)
        Me.Controls.Add(lblLine3)
        Me.Controls.Add(lblLine4)
        Me.Controls.Add(lblLine5)
        Me.Controls.Add(lblLine6)
        Me.Controls.Add(lblLine7)
        Me.Controls.Add(lblLine8)
        Me.Controls.Add(lblPrompt)
        Me.Controls.Add(txtInput)
        Me.Controls.Add(btnCancel)
    End Sub

    Private Sub ClearLines()
        lblLine1.Text = ""
        lblLine2.Text = ""
        lblLine3.Text = ""
        lblLine4.Text = ""
        lblLine5.Text = ""
        lblLine6.Text = ""
        lblLine7.Text = ""
        lblLine8.Text = ""
    End Sub

    Private Sub ShowInputPrompt(promptText As String)
        lblPrompt.Text = promptText
        txtInput.Visible = True
        txtInput.Text = ""
        txtInput.Focus()
        txtInput.SelectAll()
    End Sub

    Private Sub HideInputPrompt(promptText As String)
        lblPrompt.Text = promptText
        txtInput.Visible = False
        Me.Focus()
    End Sub

    Private Sub PopulateReviewLines()
        Dim invoiceCount As Integer = BuildInvoiceList().Count
        lblLine1.Text = enteredCompany & "  REF:  Check Amount:$  " & enteredAmount & "  { " & invoiceCount.ToString() & " Invoices }"
        lblLine2.Text = "Difference Between Total of Invoices & Check Amount: 0"
        lblLine3.Text = ""
        lblLine4.Text = "(C) Check is Correct, add to the ledger"
        lblLine5.Text = "(P) Print list of invoices"
        lblLine6.Text = "(Q) Quit to Main Menu"
        lblLine7.Text = ""
        lblLine8.Text = ""
    End Sub

    Private Sub PopulateEntrySummaryLines()
        PopulateReviewLines()
        lblLine7.Text = "Check # (Top Right)? " & enteredCheckNumber
        lblLine8.Text = "Date [ENTER = " & DateTime.Now.ToString("MM-dd-yyyy") & "] ? " & enteredDateText
    End Sub

    Private Sub ShowCompanyPrompt()
        currentStep = 1
        ClearLines()
        ShowInputPrompt("Company Name [Q = Quit] ?")
    End Sub

    Private Sub ShowAmountPrompt()
        currentStep = 2
        ClearLines()
        lblLine1.Text = "Company Name: " & enteredCompany
        ShowInputPrompt("Check Amount ?")
    End Sub

    Private Sub ShowLowestInvoicePrompt()
        currentStep = 3
        ClearLines()
        lblLine1.Text = "I need the 1st and last invoice numbers so I can automatically scan for invoices."
        ShowInputPrompt("Enter the LOWEST invoice number to be paid [-1 = No Auto Scan] ?")
    End Sub

    Private Sub ShowHighestInvoicePrompt()
        currentStep = 4
        ClearLines()
        lblLine1.Text = "I need the 1st and last invoice numbers so I can automatically scan for invoices."
        lblLine2.Text = "Lowest Invoice Number: " & enteredLowestInvoice
        ShowInputPrompt("Enter the HIGHEST invoice number to be paid ?")
    End Sub

    Private Sub ShowReviewPrompt()
        currentStep = 5
        ClearLines()
        PopulateReviewLines()
        HideInputPrompt("")
    End Sub

    Private Sub ShowCheckNumberPrompt()
        currentStep = 6
        ClearLines()
        PopulateReviewLines()
        ShowInputPrompt("Check # (Top Right)?")
    End Sub

    Private Sub ShowDatePrompt()
        currentStep = 7
        ClearLines()
        PopulateReviewLines()
        lblLine7.Text = "Check # (Top Right)? " & enteredCheckNumber
        ShowInputPrompt("Date [ENTER = " & DateTime.Now.ToString("MM-dd-yyyy") & "] ?")
    End Sub

    Private Sub ShowCheckReferencePrompt()
        currentStep = 8
        ClearLines()
        PopulateEntrySummaryLines()
        ShowInputPrompt("Check Reference (numbers on bottom right of check) ?")
    End Sub

    Private Sub ShowEverythingCorrectPrompt()
        currentStep = 9
        ClearLines()
        PopulateEntrySummaryLines()
        lblPrompt.Text = "Is EVERYTHING Correct ?  [Y = Yes] [N = NO] [1 = Discount] [2 = Debit]"
        txtInput.Visible = False
        Me.Focus()
    End Sub

    Private Sub FinishFlow()
        SaveCheck()
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub SaveCheck()
        Dim amountValue As Decimal = 0D
        Decimal.TryParse(enteredAmount, NumberStyles.Any, CultureInfo.InvariantCulture, amountValue)

        Dim entry As New LedgerEntry()
        entry.Customer = enteredCompany
        entry.DateText = enteredDateText
        entry.CheckNumber = enteredCheckNumber
        entry.InvoiceDiffText = "0"
        entry.Amount = amountValue
        entry.Reference = enteredCheckReference

        LedgerCurWriter.Append(LegacyDataPaths.LedgerCur, entry)

        Dim block As New CheckInvBlock()
        block.CustomerCode = enteredCompany
        block.CheckNumber = enteredCheckNumber
        block.SalesmanCode = enteredCheckReference
        block.DateText = enteredDateText
        block.Amount = amountValue
        block.Invoices = BuildInvoiceList()
        block.InvoiceCount = block.Invoices.Count

        CheckInvWriter.Append(LegacyDataPaths.CheckInv, block)
    End Sub

    Private Function BuildInvoiceList() As List(Of String)
        Dim results As New List(Of String)()

        If enteredLowestInvoice Is Nothing Then
            Return results
        End If

        If enteredLowestInvoice.Trim() = "-1" Then
            Return results
        End If

        Dim lowValue As Integer
        If Not Integer.TryParse(enteredLowestInvoice.Trim(), lowValue) Then
            Return results
        End If

        Dim highValue As Integer = lowValue
        If enteredHighestInvoice IsNot Nothing AndAlso enteredHighestInvoice.Trim() <> "" Then
            Integer.TryParse(enteredHighestInvoice.Trim(), highValue)
        End If

        If highValue < lowValue Then
            Dim temp As Integer = lowValue
            lowValue = highValue
            highValue = temp
        End If

        For i As Integer = lowValue To highValue
            results.Add(i.ToString(CultureInfo.InvariantCulture))
        Next

        Return results
    End Function

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub FormAddChecks_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyCode = Keys.Escape Then
            btnCancel.PerformClick()
            e.Handled = True
            e.SuppressKeyPress = True
            Return
        End If

        If currentStep = 5 Then
            If e.KeyCode = Keys.C Then
                ShowCheckNumberPrompt()
                e.Handled = True
                e.SuppressKeyPress = True
                Return
            End If

            If e.KeyCode = Keys.P Then
                DosMessageBox.Show(Me, "Print list of invoices is not implemented yet.",
                                "Add A Check",
                                MessageBoxButtons.OK)
                e.Handled = True
                e.SuppressKeyPress = True
                Return
            End If

            If e.KeyCode = Keys.Q Then
                btnCancel.PerformClick()
                e.Handled = True
                e.SuppressKeyPress = True
                Return
            End If

            Return
        End If

        If currentStep = 9 Then
            If e.KeyCode = Keys.Y Then
                FinishFlow()
                e.Handled = True
                e.SuppressKeyPress = True
                Return
            End If

            If e.KeyCode = Keys.N Then
                btnCancel.PerformClick()
                e.Handled = True
                e.SuppressKeyPress = True
                Return
            End If

            If e.KeyCode = Keys.D1 OrElse e.KeyCode = Keys.NumPad1 Then
                DosMessageBox.Show(Me, "Discount flow is not implemented yet.",
                                "Add A Check",
                                MessageBoxButtons.OK)
                e.Handled = True
                e.SuppressKeyPress = True
                Return
            End If

            If e.KeyCode = Keys.D2 OrElse e.KeyCode = Keys.NumPad2 Then
                DosMessageBox.Show(Me, "Debit flow is not implemented yet.",
                                "Add A Check",
                                MessageBoxButtons.OK)
                e.Handled = True
                e.SuppressKeyPress = True
                Return
            End If

            Return
        End If

        If e.KeyCode = Keys.Enter Then
            ProcessStep()
            e.Handled = True
            e.SuppressKeyPress = True
            Return
        End If
    End Sub

    Private Sub ProcessStep()
        Dim value As String
        value = txtInput.Text.Trim()

        If currentStep = 1 Then
            If UCase(value) = "Q" Then
                btnCancel.PerformClick()
                Return
            End If

            If value = "" Then
                txtInput.Focus()
                Return
            End If

            enteredCompany = value
            ShowAmountPrompt()
            Return
        End If

        If currentStep = 2 Then
            If value = "" Then
                txtInput.Focus()
                Return
            End If

            enteredAmount = value
            ShowLowestInvoicePrompt()
            Return
        End If

        If currentStep = 3 Then
            If value = "" Then
                txtInput.Focus()
                Return
            End If

            enteredLowestInvoice = value

            If value = "-1" Then
                enteredHighestInvoice = ""
                ShowReviewPrompt()
                Return
            End If

            ShowHighestInvoicePrompt()
            Return
        End If

        If currentStep = 4 Then
            If value = "" Then
                txtInput.Focus()
                Return
            End If

            enteredHighestInvoice = value
            ShowReviewPrompt()
            Return
        End If

        If currentStep = 6 Then
            If value = "" Then
                txtInput.Focus()
                Return
            End If

            enteredCheckNumber = value
            ShowDatePrompt()
            Return
        End If

        If currentStep = 7 Then
            If value = "" Then
                enteredDateText = DateTime.Now.ToString("MM-dd-yyyy")
            Else
                enteredDateText = value
            End If

            ShowCheckReferencePrompt()
            Return
        End If

        If currentStep = 8 Then
            If value = "" Then
                txtInput.Focus()
                Return
            End If

            enteredCheckReference = value
            ShowEverythingCorrectPrompt()
            Return
        End If
    End Sub

End Class
