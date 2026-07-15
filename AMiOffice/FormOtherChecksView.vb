Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports System.Drawing

Public Class FormOtherChecksView

    Private Const ScreenTitle As String = "VIEW OTHER CHECKS"
    Private Const DefaultBeginDateText As String = "01-01-1988"

    Private ReadOnly lblTitle As New Label()
    Private ReadOnly lblInfo As New Label()
    Private ReadOnly lblCompany As New Label()
    Private ReadOnly txtCompany As New TextBox()
    Private ReadOnly lblBeginDate As New Label()
    Private ReadOnly txtBeginDate As New TextBox()
    Private ReadOnly lblEndDate As New Label()
    Private ReadOnly txtEndDate As New TextBox()
    Private ReadOnly btnView As New Button()
    Private ReadOnly btnQuit As New Button()
    Private ReadOnly txtOutput As New TextBox()

    Private _all As New List(Of OtherCheckEntry)()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        BuildScreen()
        LoadData()

        txtCompany.Focus()
        txtCompany.SelectAll()
    End Sub

    Private Sub BuildScreen()
        Me.Text = "Other Checks"
        Me.Width = 1100
        Me.Height = 760
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.Black
        Me.ForeColor = Color.Gainsboro
        Me.KeyPreview = True
        Me.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

        Dim topPanel As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 175,
            .BackColor = Color.Black
        }

        lblTitle.AutoSize = False
        lblTitle.Left = 12
        lblTitle.Top = 10
        lblTitle.Width = 700
        lblTitle.Height = 28
        lblTitle.ForeColor = Color.Gainsboro
        lblTitle.BackColor = Color.Black
        lblTitle.Font = New Font("Consolas", 14.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblTitle.Text = "***** VIEW OTHER CHECKS *****"

        lblInfo.AutoSize = False
        lblInfo.Left = 12
        lblInfo.Top = 45
        lblInfo.Width = 1040
        lblInfo.Height = 22
        lblInfo.ForeColor = Color.Black
        lblInfo.BackColor = Color.Silver
        lblInfo.Font = New Font("Consolas", 10.5F, FontStyle.Regular, GraphicsUnit.Point)
        lblInfo.Text = "This is usually for over payments or checks that are Not paying for invoices"

        lblCompany.AutoSize = False
        lblCompany.Left = 12
        lblCompany.Top = 85
        lblCompany.Width = 520
        lblCompany.Height = 24
        lblCompany.ForeColor = Color.Gainsboro
        lblCompany.BackColor = Color.Black
        lblCompany.Text = "Enter All or Part of Company Name [Q = Quit] [A = All Companys]?"

        txtCompany.Left = 540
        txtCompany.Top = 82
        txtCompany.Width = 250
        txtCompany.BorderStyle = BorderStyle.FixedSingle
        txtCompany.BackColor = Color.Black
        txtCompany.ForeColor = Color.Gainsboro
        txtCompany.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

        lblBeginDate.AutoSize = False
        lblBeginDate.Left = 12
        lblBeginDate.Top = 118
        lblBeginDate.Width = 300
        lblBeginDate.Height = 24
        lblBeginDate.ForeColor = Color.Gainsboro
        lblBeginDate.BackColor = Color.Black
        lblBeginDate.Text = "BEGINNING DATE [1 = 01-01-1988] ?"

        txtBeginDate.Left = 320
        txtBeginDate.Top = 115
        txtBeginDate.Width = 120
        txtBeginDate.BorderStyle = BorderStyle.FixedSingle
        txtBeginDate.BackColor = Color.Black
        txtBeginDate.ForeColor = Color.Gainsboro
        txtBeginDate.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtBeginDate.Text = DefaultBeginDateText

        lblEndDate.AutoSize = False
        lblEndDate.Left = 470
        lblEndDate.Top = 118
        lblEndDate.Width = 260
        lblEndDate.Height = 24
        lblEndDate.ForeColor = Color.Gainsboro
        lblEndDate.BackColor = Color.Black
        lblEndDate.Text = "ENDING DATE ?"

        txtEndDate.Left = 735
        txtEndDate.Top = 115
        txtEndDate.Width = 120
        txtEndDate.BorderStyle = BorderStyle.FixedSingle
        txtEndDate.BackColor = Color.Black
        txtEndDate.ForeColor = Color.Gainsboro
        txtEndDate.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtEndDate.Text = Date.Today.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture)

        btnView.Left = 880
        btnView.Top = 82
        btnView.Width = 90
        btnView.Height = 28
        btnView.Text = "View"
        btnView.FlatStyle = FlatStyle.Flat
        btnView.BackColor = Color.Black
        btnView.ForeColor = Color.Gainsboro
        btnView.FlatAppearance.BorderColor = Color.Gainsboro
        btnView.FlatAppearance.BorderSize = 1

        btnQuit.Left = 880
        btnQuit.Top = 115
        btnQuit.Width = 90
        btnQuit.Height = 28
        btnQuit.Text = "Quit"
        btnQuit.FlatStyle = FlatStyle.Flat
        btnQuit.BackColor = Color.Black
        btnQuit.ForeColor = Color.Gainsboro
        btnQuit.FlatAppearance.BorderColor = Color.Gainsboro
        btnQuit.FlatAppearance.BorderSize = 1

        topPanel.Controls.Add(lblTitle)
        topPanel.Controls.Add(lblInfo)
        topPanel.Controls.Add(lblCompany)
        topPanel.Controls.Add(txtCompany)
        topPanel.Controls.Add(lblBeginDate)
        topPanel.Controls.Add(txtBeginDate)
        topPanel.Controls.Add(lblEndDate)
        topPanel.Controls.Add(txtEndDate)
        topPanel.Controls.Add(btnView)
        topPanel.Controls.Add(btnQuit)

        txtOutput.Dock = DockStyle.Fill
        txtOutput.Multiline = True
        txtOutput.ScrollBars = ScrollBars.Vertical
        txtOutput.ReadOnly = True
        txtOutput.BorderStyle = BorderStyle.None
        txtOutput.BackColor = Color.Black
        txtOutput.ForeColor = Color.Gainsboro
        txtOutput.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtOutput.WordWrap = False

        Controls.Add(txtOutput)
        Controls.Add(topPanel)

        AddHandler btnView.Click, AddressOf ViewOtherChecks
        AddHandler btnQuit.Click, Sub() Me.Close()

        AddHandler txtCompany.KeyDown, AddressOf Input_KeyDown
        AddHandler txtBeginDate.KeyDown, AddressOf Input_KeyDown
        AddHandler txtEndDate.KeyDown, AddressOf Input_KeyDown
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            Me.Close()
            Return True
        End If

        If keyData = Keys.F5 Then
            ViewOtherChecks(Me, EventArgs.Empty)
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub Input_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            ViewOtherChecks(Me, EventArgs.Empty)
        End If
    End Sub

    Private Sub LoadData()
        Dim path = LegacyDataPaths.OtherChk

        _all = New List(Of OtherCheckEntry)()
        txtOutput.Clear()

        If Not System.IO.File.Exists(path) Then
            txtOutput.Text = "OTHER.CHK was not found at: " & path
            UiFileErrors.ShowMissingRequiredFile(Me, "Other Checks", path)
            Return
        End If

        Try
            _all = OtherChkReader.ReadAll(path)
        Catch ex As Exception
            _all = New List(Of OtherCheckEntry)()
            txtOutput.Text = "Unable to read OTHER.CHK from: " & path
            UiFileErrors.ShowUnableToReadRequiredFile(Me, "Other Checks", path, ex)
        End Try
    End Sub

    Private Sub ViewOtherChecks(sender As Object, e As EventArgs)
        Dim companyInput As String = txtCompany.Text.Trim()

        If String.Equals(companyInput, "Q", StringComparison.OrdinalIgnoreCase) Then
            Me.Close()
            Return
        End If

        Dim includeAllCompanies As Boolean = String.Equals(companyInput, "A", StringComparison.OrdinalIgnoreCase)
        Dim companyCaption As String = If(includeAllCompanies OrElse companyInput = "", "ALL COMPANYS", companyInput.ToUpperInvariant())

        Dim beginDate As Date
        If Not TryParseDosDateInput(txtBeginDate.Text.Trim(), DefaultBeginDateText, beginDate) Then
            DosMessageBox.Show(Me, "Beginning date must be in MM-dd-yyyy format, or enter 1 for 01-01-1988.", "Invalid Beginning Date", MessageBoxButtons.OK)
            txtBeginDate.Focus()
            txtBeginDate.SelectAll()
            Return
        End If

        Dim endDate As Date
        If Not TryParseDosDateInput(txtEndDate.Text.Trim(), Date.Today.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture), endDate) Then
            DosMessageBox.Show(Me, "Ending date must be in MM-dd-yyyy format.", "Invalid Ending Date", MessageBoxButtons.OK)
            txtEndDate.Focus()
            txtEndDate.SelectAll()
            Return
        End If

        Dim filtered = _all.
            Where(Function(x) MatchesCompany(x, companyInput, includeAllCompanies)).
            Select(Function(x) New With {
                .Entry = x,
                .ParsedDate = ParseEntryDate(x.DateText)
            }).
            Where(Function(x) x.ParsedDate.HasValue AndAlso x.ParsedDate.Value.Date >= beginDate.Date AndAlso x.ParsedDate.Value.Date <= endDate.Date).
            OrderBy(Function(x) x.ParsedDate.Value).
            ThenBy(Function(x) If(x.Entry.Company, "")).
            ThenBy(Function(x) If(x.Entry.CheckNumber, "")).
            ToList()

        Dim sb As New StringBuilder()

        If filtered.Count = 0 Then
            sb.AppendLine("***** VIEW OTHER CHECKS *****")
            sb.AppendLine()
            sb.AppendLine("No records matched the requested criteria.")
            sb.AppendLine()
            sb.AppendLine($"TOTAL from {beginDate:MM-dd-yyyy} to {endDate:MM-dd-yyyy}")
            sb.AppendLine($"for {companyCaption}: $      0.00   End of List [ESC = Exit]")
            txtOutput.Text = sb.ToString()
            Return
        End If

        For Each item In filtered
            Dim x = item.Entry

            sb.AppendLine($"Customer: {PadRightSafe(x.Company, 28)}Date: {item.ParsedDate.Value:MM-dd-yyyy}")
            sb.AppendLine($"Check #: {PadRightSafe(x.CheckNumber, 10)}  Reference: {PadRightSafe(x.Reference, 12)}  Amount: $ {x.Amount,8:N2}")
            sb.AppendLine($"Reason Why: {If(x.ReasonWhy, "")}")
            sb.AppendLine(New String("*"c, 88))
        Next

        Dim total As Decimal = filtered.Sum(Function(x) x.Entry.Amount)

        sb.AppendLine($"TOTAL from {beginDate:MM-dd-yyyy} to {endDate:MM-dd-yyyy}")
        sb.AppendLine($"for {companyCaption}: $ {total,8:N2}   End of List [ESC = Exit]")

        txtOutput.Text = sb.ToString()
        txtOutput.SelectionStart = 0
        txtOutput.SelectionLength = 0
    End Sub

    Private Function MatchesCompany(entry As OtherCheckEntry, companyInput As String, includeAllCompanies As Boolean) As Boolean
        If includeAllCompanies Then Return True

        Dim s As String = companyInput.Trim()
        If s = "" Then Return True

        Return If(entry.Company, "").IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
    End Function

    Private Function TryParseDosDateInput(value As String, defaultValue As String, ByRef result As Date) As Boolean
        Dim textToParse As String = value

        If textToParse = "" Then
            textToParse = defaultValue
        ElseIf textToParse = "1" Then
            textToParse = DefaultBeginDateText
        End If

        Return Date.TryParseExact(
            textToParse,
            "MM-dd-yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            result)
    End Function

    Private Function ParseEntryDate(value As String) As Date?
        Dim d As Date

        If Date.TryParseExact(value,
                              "MM-dd-yyyy",
                              CultureInfo.InvariantCulture,
                              DateTimeStyles.None,
                              d) Then
            Return d
        End If

        Return Nothing
    End Function

    Private Function PadRightSafe(value As String, width As Integer) As String
        Dim s As String = If(value, "")
        If s.Length >= width Then
            Return s.Substring(0, width)
        End If

        Return s.PadRight(width)
    End Function

End Class

