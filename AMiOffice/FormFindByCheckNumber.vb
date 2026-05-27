Option Strict On
Option Explicit On

Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports System.Drawing

Public Class FormFindByCheckNumber

    Private Const ScreenTitle As String = "Find by Check #"

    Private ReadOnly lblPromptTitle As New Label()
    Private ReadOnly lblPrompt As New Label()
    Private ReadOnly txtCheck As New TextBox()
    Private ReadOnly txtOutput As New TextBox()

    Private _all As New List(Of CheckInvBlock)()
    Private _matches As New List(Of CheckInvBlock)()
    Private _currentIndex As Integer = -1
    Private _showingResults As Boolean = False

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        BuildScreen()
        LoadData()

        txtCheck.Focus()
        txtCheck.SelectAll()
    End Sub

    Private Sub BuildScreen()
        Text = "Find by Check # (CHECK.INV)"
        Width = 950
        Height = 650
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.Black
        ForeColor = Color.Gainsboro
        KeyPreview = True
        Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

        lblPromptTitle.AutoSize = False
        lblPromptTitle.Left = 12
        lblPromptTitle.Top = 18
        lblPromptTitle.Width = 720
        lblPromptTitle.Height = 28
        lblPromptTitle.BackColor = Color.Black
        lblPromptTitle.ForeColor = Color.Gainsboro
        lblPromptTitle.Font = New Font("Consolas", 12.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblPromptTitle.Text = "<<<  Find the Invoices that a Check Payed for  >>>"

        lblPrompt.AutoSize = False
        lblPrompt.Left = 12
        lblPrompt.Top = 58
        lblPrompt.Width = 770
        lblPrompt.Height = 24
        lblPrompt.BackColor = Color.Black
        lblPrompt.ForeColor = Color.Gainsboro
        lblPrompt.Text = "Enter Part or All of Check Number [ENTER = All Checks] [Q = Quit] ?"

        txtCheck.Left = 790
        txtCheck.Top = 55
        txtCheck.Width = 120
        txtCheck.BorderStyle = BorderStyle.FixedSingle
        txtCheck.BackColor = Color.Black
        txtCheck.ForeColor = Color.Gainsboro
        txtCheck.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

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

        Controls.Add(lblPromptTitle)
        Controls.Add(lblPrompt)
        Controls.Add(txtCheck)
        Controls.Add(txtOutput)

        AddHandler txtCheck.KeyDown, AddressOf TxtCheck_KeyDown
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            Close()
            Return True
        End If

        If keyData = Keys.Enter AndAlso _showingResults Then
            ShowNextMatch()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub TxtCheck_KeyDown(sender As Object, e As KeyEventArgs)
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
        Dim checkText As String = txtCheck.Text.Trim()

        If String.Equals(checkText, "Q", StringComparison.OrdinalIgnoreCase) Then
            Close()
            Return
        End If

        If _all Is Nothing OrElse _all.Count = 0 Then
            LoadData()
            If _all Is Nothing OrElse _all.Count = 0 Then Return
        End If

        txtOutput.Text = "Please wait..."
        txtOutput.Refresh()
        Application.DoEvents()

        If checkText = "" Then
            _matches = _all.ToList()
        Else
            _matches =
                _all.
                Where(Function(b) If(b.CheckNumber, "").IndexOf(checkText, StringComparison.OrdinalIgnoreCase) >= 0).
                ToList()
        End If

        _currentIndex = -1
        _showingResults = True

        If _matches.Count = 0 Then
            _showingResults = False
            txtOutput.Text =
                "No checks matched that search." & Environment.NewLine &
                Environment.NewLine &
                "Press ESC to quit."
            Return
        End If

        ShowNextMatch()
    End Sub

    Private Sub ShowNextMatch()
        If _matches Is Nothing OrElse _matches.Count = 0 Then
            Close()
            Return
        End If

        _currentIndex += 1

        If _currentIndex >= _matches.Count Then
            Close()
            Return
        End If

        Dim hit As CheckInvBlock = _matches(_currentIndex)
        txtOutput.Text = BuildMatchText(hit)
        txtOutput.SelectionStart = 0
        txtOutput.SelectionLength = 0
    End Sub

    Private Function BuildMatchText(hit As CheckInvBlock) As String
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
        sb.Append("Hit ENTER to look for another check [ESC = Quit]")

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