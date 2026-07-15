Option Strict On
Option Explicit On

Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports System.Drawing

Public Class FormFindByInvoiceNumber

    Private Const ScreenTitle As String = "Find by Invoice #"

    Private ReadOnly lblPromptTitle As New Label()
    Private ReadOnly lblPrompt As New Label()
    Private ReadOnly txtInvoice As New TextBox()
    Private ReadOnly txtOutput As New TextBox()
    Private ReadOnly btnClose As New Button()

    Private _all As New List(Of CheckInvBlock)()
    Private _showingResult As Boolean = False

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        BuildScreen()
        LoadData()

        txtInvoice.Focus()
        txtInvoice.SelectAll()
    End Sub

    Private Sub BuildScreen()
        Text = "Find by Invoice # (CHECK.INV)"
        Width = 950
        Height = 650
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.FromArgb(34, 34, 34)
        ForeColor = Color.Gainsboro
        KeyPreview = True
        Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

        lblPromptTitle.AutoSize = False
        lblPromptTitle.Left = 12
        lblPromptTitle.Top = 18
        lblPromptTitle.Width = 700
        lblPromptTitle.Height = 28
        lblPromptTitle.BackColor = Color.Black
        lblPromptTitle.ForeColor = Color.Gainsboro
        lblPromptTitle.Font = New Font("Consolas", 12.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblPromptTitle.Text = "<<<  Find the Check that Payed for an Invoice >>>"

        lblPrompt.AutoSize = False
        lblPrompt.Left = 12
        lblPrompt.Top = 58
        lblPrompt.Width = 420
        lblPrompt.Height = 24
        lblPrompt.BackColor = Color.Black
        lblPrompt.ForeColor = Color.Gainsboro
        lblPrompt.Text = "Enter the Invoice Number [-1 = Quit] ?"

        txtInvoice.Left = 440
        txtInvoice.Top = 55
        txtInvoice.Width = 140
        txtInvoice.BorderStyle = BorderStyle.FixedSingle
        txtInvoice.BackColor = Color.Black
        txtInvoice.ForeColor = Color.Gainsboro
        txtInvoice.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

        txtOutput.Left = 12
        txtOutput.Top = 100
        txtOutput.Width = 900
        txtOutput.Height = 500
        txtOutput.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtOutput.Multiline = True
        txtOutput.ReadOnly = True
        txtOutput.ScrollBars = ScrollBars.None
        txtOutput.BorderStyle = BorderStyle.None
        txtOutput.BackColor = Color.Black
        txtOutput.ForeColor = Color.Gainsboro
        txtOutput.WordWrap = False
        txtOutput.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

        btnClose.Text = "(ESC) Close"
        btnClose.Width = 160
        btnClose.Height = 30
        btnClose.Left = ClientSize.Width - btnClose.Width - 12
        btnClose.Top = ClientSize.Height - btnClose.Height - 12
        btnClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnClose.FlatStyle = FlatStyle.Standard
        btnClose.BackColor = Color.Silver
        btnClose.ForeColor = Color.Black
        btnClose.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        btnClose.TabStop = True
        AddHandler btnClose.Click, Sub() Close()

        Controls.Add(lblPromptTitle)
        Controls.Add(lblPrompt)
        Controls.Add(txtInvoice)
        Controls.Add(txtOutput)
        Controls.Add(btnClose)

        AddHandler txtInvoice.KeyDown, AddressOf TxtInvoice_KeyDown
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            Close()
            Return True
        End If

        If _showingResult Then
            If keyData = Keys.Enter Then
                Close()
                Return True
            End If

            If keyData = Keys.Q Then
                Close()
                Return True
            End If
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub TxtInvoice_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            StartSearch()
        ElseIf e.KeyCode = Keys.Escape Then
            e.SuppressKeyPress = True
            Close()
        End If
    End Sub

    Private Sub LoadData()
        Dim path = LegacyDataPaths.CheckInv

        Try
            If Not File.Exists(path) Then
                _all = New List(Of CheckInvBlock)()
                txtOutput.Text = "CHECK.INV was not found at:" & Environment.NewLine & path
                UiFileErrors.ShowMissingRequiredFile(Me, ScreenTitle, path)
                Return
            End If

            _all = CheckInvReader.ReadAll(path)
            txtOutput.Clear()
        Catch ex As Exception
            _all = New List(Of CheckInvBlock)()
            txtOutput.Text = "Unable to read CHECK.INV from:" & Environment.NewLine & path
            UiFileErrors.ShowUnableToReadRequiredFile(Me, ScreenTitle, path, ex)
        End Try
    End Sub

    Private Sub StartSearch()
        _showingResult = False

        Dim invNo As String = txtInvoice.Text.Trim()

        If invNo = "-1" Then
            Close()
            Return
        End If

        If invNo = "" Then
            txtOutput.Clear()
            Return
        End If

        If _all Is Nothing OrElse _all.Count = 0 Then
            LoadData()
            If _all Is Nothing OrElse _all.Count = 0 Then Return
        End If

        txtOutput.Text = "Please Wait..."
        txtOutput.Refresh()
        Application.DoEvents()

        Dim hit = _all.FirstOrDefault(
            Function(b) b.Invoices IsNot Nothing AndAlso
                        b.Invoices.Any(Function(x) String.Equals(x.Trim(), invNo, StringComparison.OrdinalIgnoreCase))
        )

        If hit Is Nothing Then
            txtOutput.Text =
                "No check was found for invoice # " & invNo & "." & Environment.NewLine &
                Environment.NewLine &
                "Replace Invoice # with another valid Invoice #, then hit ENTER."
            txtInvoice.Focus()
            txtInvoice.SelectAll()
            Return
        End If

        _showingResult = True
        txtOutput.Text = BuildResultText(hit)
        txtOutput.SelectionStart = 0
        txtOutput.SelectionLength = 0
    End Sub

    Private Function BuildResultText(hit As CheckInvBlock) As String
        Dim sb As New StringBuilder()

        Dim customer As String = If(hit.CustomerCode, "").Trim().ToUpperInvariant()
        Dim dateText As String = If(hit.DateText, "").Trim()
        Dim checkNo As String = If(hit.CheckNumber, "").Trim()

        sb.AppendLine(customer.PadRight(52) & "Date: " & dateText)
        sb.AppendLine("Check # " & checkNo & "   Amount $ " & hit.Amount.ToString("###,##0.00").PadLeft(10))
        sb.AppendLine("==================== Invoices =====================")

        If hit.Invoices IsNot Nothing AndAlso hit.Invoices.Count > 0 Then
            sb.AppendLine(WrapInvoices(hit.Invoices, 10))
        End If

        sb.AppendLine()
        sb.Append("Hit ENTER to look for another check [Q = Quit] ?")

        Return sb.ToString()
    End Function

    Private Function WrapInvoices(invoices As List(Of String), itemsPerLine As Integer) As String
        Dim sb As New StringBuilder()
        Dim columnCount As Integer = 0

        For Each inv In invoices
            Dim token As String = If(inv, "").Trim()
            If token = "" Then Continue For

            If columnCount > 0 Then
                sb.Append(" ")
            End If

            sb.Append(token)
            columnCount += 1

            If columnCount >= itemsPerLine Then
                sb.AppendLine()
                columnCount = 0
            End If
        Next

        Return sb.ToString().TrimEnd()
    End Function

End Class
