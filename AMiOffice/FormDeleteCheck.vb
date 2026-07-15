Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Threading
Imports System.Windows.Forms

Public Class FormDeleteCheck
    Inherits Form

    Private Enum DeleteCheckState
        EnterCheckNumber
        ConfirmDelete
        Deleting
    End Enum

    Private _state As DeleteCheckState = DeleteCheckState.EnterCheckNumber
    Private _currentCheck As LedgerEntry = Nothing
    Private _matchingChecks As New List(Of LedgerEntry)()
    Private _matchIndex As Integer = -1
    Private _hasMultipleMatches As Boolean = False

    Private lblTitle As Label
    Private lblLine1 As Label
    Private txtInput As TextBox
    Private lblCustomer As Label
    Private lblDate As Label
    Private lblCheckInfo As Label
    Private lblInvoices As Label
    Private lblAmount As Label
    Private lblPrompt As Label
    Private lblStatus1 As Label
    Private lblStatus2 As Label
    Private lblStatus3 As Label

    Public Sub New()
        MyBase.New()
        InitializeComponent()
        InitializeDeleteCheckForm()
    End Sub

    Private Sub InitializeDeleteCheckForm()
        Me.Text = "Delete Check"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 12.0!, FontStyle.Regular)
        Me.ClientSize = New Size(900, 500)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.KeyPreview = True

        lblTitle = CreateLabel(20, 20, 700, 25)
        lblLine1 = CreateLabel(20, 60, 850, 25)

        txtInput = New TextBox()
        txtInput.Name = "txtInput"
        txtInput.Location = New Point(20, 95)
        txtInput.Size = New Size(200, 26)
        txtInput.Font = New Font("Consolas", 12.0!, FontStyle.Regular)
        AddHandler txtInput.KeyDown, AddressOf txtInput_KeyDown

        lblCustomer = CreateLabel(20, 140, 350, 25)
        lblDate = CreateLabel(520, 140, 250, 25)
        lblCheckInfo = CreateLabel(20, 170, 700, 25)
        lblInvoices = CreateLabel(20, 200, 800, 25)
        lblAmount = CreateLabel(20, 230, 300, 25)
        lblPrompt = CreateLabel(20, 290, 850, 25)
        lblStatus1 = CreateLabel(20, 330, 850, 25)
        lblStatus2 = CreateLabel(20, 360, 850, 25)
        lblStatus3 = CreateLabel(20, 390, 850, 50)

        Me.Controls.Add(lblTitle)
        Me.Controls.Add(lblLine1)
        Me.Controls.Add(txtInput)
        Me.Controls.Add(lblCustomer)
        Me.Controls.Add(lblDate)
        Me.Controls.Add(lblCheckInfo)
        Me.Controls.Add(lblInvoices)
        Me.Controls.Add(lblAmount)
        Me.Controls.Add(lblPrompt)
        Me.Controls.Add(lblStatus1)
        Me.Controls.Add(lblStatus2)
        Me.Controls.Add(lblStatus3)

        SetupInitialScreen()
    End Sub

    Private Function CreateLabel(x As Integer, y As Integer, w As Integer, h As Integer) As Label
        Dim lbl As New Label()
        lbl.AutoSize = False
        lbl.Location = New Point(x, y)
        lbl.Size = New Size(w, h)
        lbl.ForeColor = Color.White
        lbl.BackColor = Color.Black
        lbl.Font = New Font("Consolas", 12.0!, FontStyle.Regular)
        Return lbl
    End Function

    Private Sub SetupInitialScreen()
        _state = DeleteCheckState.EnterCheckNumber
        _currentCheck = Nothing
        _matchingChecks.Clear()
        _matchIndex = -1
        _hasMultipleMatches = False

        lblTitle.Text = "***** Delete A Check *****"
        lblLine1.Text = "Check # [ENTER = Quit]?"
        txtInput.Visible = True
        txtInput.Text = ""
        txtInput.Enabled = True
        txtInput.Focus()

        lblCustomer.Text = ""
        lblDate.Text = ""
        lblCheckInfo.Text = ""
        lblInvoices.Text = ""
        lblAmount.Text = ""
        lblPrompt.Text = ""
        lblStatus1.Text = ""
        lblStatus2.Text = ""
        lblStatus3.Text = ""
    End Sub

    Private Sub ShowCheckDetails(info As LedgerEntry)
        _state = DeleteCheckState.ConfirmDelete
        _currentCheck = info

        lblTitle.Text = ""
        lblLine1.Text = ""
        txtInput.Visible = False

        lblCustomer.Text = "Customer: " & info.Customer
        lblDate.Text = "Date: " & info.DateText
        lblCheckInfo.Text = "Check #: " & info.CheckNumber & "    Reference: " & info.Reference
        lblInvoices.Text = "Difference: " & info.InvoiceDiffText
        lblAmount.Text = "Amount: $    " & info.Amount.ToString("0.00")

        If _hasMultipleMatches Then
            lblPrompt.Text = "Match " & (_matchIndex + 1).ToString() & " of " & _matchingChecks.Count.ToString() &
                             "   [Y = Delete] [N = Next Match] [ESC = Cancel]"
        Else
            lblPrompt.Text = "Delete the check Above & Reset the Invoices to UnPaid (Y\N) ?"
        End If

        lblStatus1.Text = ""
        lblStatus2.Text = ""
        lblStatus3.Text = ""

        Me.Focus()
    End Sub

    Private Sub ShowDeletingScreen()
        _state = DeleteCheckState.Deleting

        lblStatus1.Text = "Deleting Check..."
        lblStatus2.Text = ""
        lblStatus3.Text = "Please Wait.." & Environment.NewLine & "Purging part of .INV file..."

        Me.Refresh()
        Application.DoEvents()
    End Sub

    Private Sub txtInput_KeyDown(sender As Object, e As KeyEventArgs)
        If _state <> DeleteCheckState.EnterCheckNumber Then
            Return
        End If

        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True

            Dim checkNo As String = txtInput.Text.Trim()

            If checkNo = "" Then
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
                Return
            End If

            Dim entries As List(Of LedgerEntry) = LedgerCurReader.ReadAll(LegacyDataPaths.LedgerCur)
            Dim matches As New List(Of LedgerEntry)()
            Dim i As Integer

            For i = 0 To entries.Count - 1
                If String.Equals(entries(i).CheckNumber, checkNo, StringComparison.OrdinalIgnoreCase) Then
                    matches.Add(entries(i))
                End If
            Next

            If matches.Count = 0 Then
                DosMessageBox.Show(Me, "Check not found.", "Delete Check", MessageBoxButtons.OK)
                txtInput.SelectAll()
                txtInput.Focus()
                Return
            End If

            _matchingChecks = matches
            _matchIndex = 0
            _hasMultipleMatches = (matches.Count > 1)

            ShowCheckDetails(_matchingChecks(_matchIndex))
        End If
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If _state = DeleteCheckState.ConfirmDelete Then
            If keyData = Keys.Y Then
                If _currentCheck IsNot Nothing Then
                    ShowDeletingScreen()
                    DeleteCheck(_currentCheck)
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                    Return True
                End If
            End If

            If keyData = Keys.N Then
                If _hasMultipleMatches AndAlso _matchingChecks.Count > 1 Then
                    _matchIndex += 1
                    If _matchIndex >= _matchingChecks.Count Then
                        _matchIndex = 0
                    End If

                    ShowCheckDetails(_matchingChecks(_matchIndex))
                    Return True
                Else
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                    Return True
                End If
            End If

            If keyData = Keys.Escape Then
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
                Return True
            End If
        End If

        If _state = DeleteCheckState.EnterCheckNumber Then
            If keyData = Keys.Escape Then
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
                Return True
            End If
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub DeleteCheck(info As LedgerEntry)
        ResetInvoicesToUnpaid(info)
        DeleteFromLedger(info)
        DeleteFromCheckInv(info)
        Thread.Sleep(750)
    End Sub

    Private Sub DeleteFromLedger(info As LedgerEntry)
        Dim entries As List(Of LedgerEntry) = LedgerCurReader.ReadAll(LegacyDataPaths.LedgerCur)
        Dim updated As New List(Of LedgerEntry)()
        Dim i As Integer

        For i = 0 To entries.Count - 1
            If Not LedgerEntriesMatch(entries(i), info) Then
                updated.Add(entries(i))
            End If
        Next

        LedgerCurWriter.WriteAll(LegacyDataPaths.LedgerCur, updated)
    End Sub

    Private Sub DeleteFromCheckInv(info As LedgerEntry)
        Dim blocks As List(Of CheckInvBlock) = CheckInvReader.ReadAll(LegacyDataPaths.CheckInv)
        Dim updated As New List(Of CheckInvBlock)()
        Dim i As Integer

        For i = 0 To blocks.Count - 1
            If Not CheckInvMatchesLedgerEntry(blocks(i), info) Then
                updated.Add(blocks(i))
            End If
        Next

        CheckInvWriter.WriteAll(LegacyDataPaths.CheckInv, updated)
    End Sub
    Private Sub ResetInvoicesToUnpaid(info As LedgerEntry)
        Dim blocks As List(Of CheckInvBlock) = CheckInvReader.ReadAll(LegacyDataPaths.CheckInv)
        Dim i As Integer

        For i = 0 To blocks.Count - 1
            If CheckInvMatchesLedgerEntry(blocks(i), info) Then
                Dim j As Integer

                For j = 0 To blocks(i).Invoices.Count - 1
                    Dim invoiceText As String = blocks(i).Invoices(j)
                    Dim invoiceNumber As Long

                    If Long.TryParse(invoiceText, invoiceNumber) Then
                        InvoiceChkWriter.SetFlag(LegacyDataPaths.InvoiceChk, invoiceNumber, "J")
                    End If
                Next

                Exit For
            End If
        Next
    End Sub

    Private Function LedgerEntriesMatch(x As LedgerEntry, info As LedgerEntry) As Boolean
        If x Is Nothing OrElse info Is Nothing Then
            Return False
        End If

        If Not String.Equals(x.Customer, info.Customer, StringComparison.OrdinalIgnoreCase) Then
            Return False
        End If

        If Not String.Equals(x.DateText, info.DateText, StringComparison.OrdinalIgnoreCase) Then
            Return False
        End If

        If Not String.Equals(x.CheckNumber, info.CheckNumber, StringComparison.OrdinalIgnoreCase) Then
            Return False
        End If

        If Not String.Equals(x.Reference, info.Reference, StringComparison.OrdinalIgnoreCase) Then
            Return False
        End If

        If x.Amount <> info.Amount Then
            Return False
        End If

        Return True
    End Function

    Private Function CheckInvMatchesLedgerEntry(block As CheckInvBlock, info As LedgerEntry) As Boolean
        If block Is Nothing OrElse info Is Nothing Then
            Return False
        End If

        If Not String.Equals(block.CustomerCode, info.Customer, StringComparison.OrdinalIgnoreCase) Then
            Return False
        End If

        If Not String.Equals(block.DateText, info.DateText, StringComparison.OrdinalIgnoreCase) Then
            Return False
        End If

        If Not String.Equals(block.CheckNumber, info.CheckNumber, StringComparison.OrdinalIgnoreCase) Then
            Return False
        End If

        If Not String.Equals(block.SalesmanCode, info.Reference, StringComparison.OrdinalIgnoreCase) Then
            Return False
        End If

        If block.Amount <> info.Amount Then
            Return False
        End If

        Return True
    End Function
End Class
