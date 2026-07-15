Option Strict On
Option Explicit On

Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports System.Drawing

Public Class FormLedgerCompanyTotals

    Private Const ScreenTitle As String = "Company Totals"
    Private Const LinesPerPage As Integer = 20

    Private ReadOnly txtOutput As New TextBox()

    Private _pages As New List(Of String)()
    Private _currentPageIndex As Integer = -1

    Private NotInheritable Class CompanyTotalLine
        Public Property Customer As String
        Public Property Total As Decimal
        Public Property Count As Integer
    End Class

    Public Sub New()
        InitializeComponent()
        BuildScreen()
    End Sub

    Private Sub BuildScreen()
        Text = "Ledger - Company Totals"
        Width = 900
        Height = 700
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.Black
        ForeColor = Color.Gainsboro
        KeyPreview = True
        Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

        txtOutput.Dock = DockStyle.Fill
        txtOutput.Multiline = True
        txtOutput.ReadOnly = True
        txtOutput.ScrollBars = ScrollBars.None
        txtOutput.BorderStyle = BorderStyle.None
        txtOutput.BackColor = Color.Black
        txtOutput.ForeColor = Color.Gainsboro
        txtOutput.WordWrap = False
        txtOutput.Font = New Font("Consolas", 12.0F, FontStyle.Regular, GraphicsUnit.Point)

        Controls.Add(txtOutput)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        LoadAndBuildPages()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Enter Then
            ShowNextPage()
            Return True
        End If

        If keyData = Keys.Escape Then
            Close()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub LoadAndBuildPages()
        Dim path = LegacyDataPaths.LedgerCur

        txtOutput.Clear()
        _pages = New List(Of String)()
        _currentPageIndex = -1

        If Not System.IO.File.Exists(path) Then
            txtOutput.Text = "LEDGER.CUR was not found at: " & path
            UiFileErrors.ShowMissingRequiredFile(Me, ScreenTitle, path)
            Return
        End If

        Try
            Dim entries = LedgerCurReader.ReadAll(path)
            ShowLoadingProgress(entries.Count)

            Dim totals As List(Of CompanyTotalLine) =
                entries.
                GroupBy(Function(x) (If(x.Customer, "")).Trim()).
                Select(Function(g) New CompanyTotalLine With {
                    .Customer = g.Key,
                    .Total = g.Sum(Function(x) x.Amount),
                    .Count = g.Count()
                }).
                OrderBy(Function(x) x.Customer).
                ToList()

            BuildPages(totals)
            ShowNextPage()
        Catch ex As Exception
            txtOutput.Text = "Unable to read LEDGER.CUR from: " & path
            UiFileErrors.ShowUnableToReadRequiredFile(Me, ScreenTitle, path, ex)
        End Try
    End Sub

    Private Sub ShowLoadingProgress(entryCount As Integer)
        If entryCount <= 0 Then Return

        For i As Integer = 1 To entryCount
            txtOutput.Text = "LOADING ENTRY # " & i.ToString()
            txtOutput.Refresh()
            Application.DoEvents()
        Next
    End Sub

    Private Sub BuildPages(totals As List(Of CompanyTotalLine))
        _pages.Clear()

        Dim currentLines As New List(Of String)()

        For Each item In totals
            currentLines.Add(FormatCompanyTotalLine(item.Customer, item.Total))

            If currentLines.Count >= LinesPerPage Then
                _pages.Add(BuildPageText(currentLines, False))
                currentLines = New List(Of String)()
            End If
        Next

        If currentLines.Count > 0 Then
            _pages.Add(BuildPageText(currentLines, True))
        End If

        If _pages.Count = 0 Then
            _pages.Add(BuildPageText(New List(Of String) From {"No company totals found."}, True))
        End If
    End Sub

    Private Function BuildPageText(lines As List(Of String), isLastPage As Boolean) As String
        Dim sb As New StringBuilder()

        sb.AppendLine(Space(35) & "Hit [ENTER]")
        sb.AppendLine()

        For Each line In lines
            sb.AppendLine(line)
        Next

        sb.AppendLine()
        sb.AppendLine(Space(35) & If(isLastPage, "End of List [ESC = Exit]", "Hit [ENTER]"))

        Return sb.ToString()
    End Function

    Private Function FormatCompanyTotalLine(customer As String, total As Decimal) As String
        Dim namePart As String = If(customer, "").Trim().ToUpperInvariant()
        If namePart.Length > 9 Then
            namePart = namePart.Substring(0, 9)
        End If

        namePart = namePart.PadRight(9)

        Dim amountPart As String = total.ToString("0.00")
        If total >= 0D Then
            amountPart = " " & amountPart
        End If

        Return $"{namePart} $ {amountPart,12}"
    End Function

    Private Sub ShowNextPage()
        If _pages Is Nothing OrElse _pages.Count = 0 Then Return

        If _currentPageIndex >= _pages.Count - 1 Then
            Close()
            Return
        End If

        _currentPageIndex += 1
        txtOutput.Text = _pages(_currentPageIndex)
        txtOutput.SelectionStart = 0
        txtOutput.SelectionLength = 0
    End Sub

End Class
