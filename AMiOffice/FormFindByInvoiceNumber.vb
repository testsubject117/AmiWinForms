Option Strict On
Option Explicit On

Imports System.IO
Imports System.Linq

Public Class FormFindByInvoiceNumber

    Private Const ScreenTitle As String = "Find by Invoice #"

    Private ReadOnly txtInvoice As New TextBox()
    Private ReadOnly btnFind As New Button()
    Private ReadOnly btnRefresh As New Button()
    Private ReadOnly lblResult As New Label()

    Private _all As List(Of CheckInvBlock) = New List(Of CheckInvBlock)()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = "Find by Invoice # (CHECK.INV)"
        Width = 750
        Height = 320
        StartPosition = FormStartPosition.CenterParent

        Dim top As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 45, .Padding = New Padding(10)}
        top.Controls.Add(New Label() With {.Text = "Invoice #:", .AutoSize = True, .Padding = New Padding(0, 10, 0, 0)})

        txtInvoice.Width = 170
        top.Controls.Add(txtInvoice)

        btnFind.Text = "Find"
        top.Controls.Add(btnFind)

        btnRefresh.Text = "Refresh"
        top.Controls.Add(btnRefresh)

        lblResult.Dock = DockStyle.Fill
        lblResult.Padding = New Padding(10)
        lblResult.Text = "Loading CHECK.INV..."

        Controls.Add(lblResult)
        Controls.Add(top)

        AddHandler btnFind.Click, Sub() DoFind()

        AddHandler btnRefresh.Click,
            Sub()
                LoadData()
            End Sub

        AddHandler txtInvoice.KeyDown,
            Sub(sender, args)
                If args.KeyCode = Keys.Enter Then
                    args.SuppressKeyPress = True
                    DoFind()
                End If
            End Sub

        LoadData()
    End Sub

    Private Sub LoadData()
        Dim path = LegacyDataPaths.CheckInv

        Try
            If Not File.Exists(path) Then
                _all = New List(Of CheckInvBlock)()
                lblResult.Text =
                    "CHECK.INV was not found at:" & Environment.NewLine &
                    path

                UiFileErrors.ShowMissingRequiredFile(Me, ScreenTitle, path)
                Return
            End If

            _all = CheckInvReader.ReadAll(path)
            lblResult.Text = $"Loaded {_all.Count:N0} check records from CHECK.INV."
        Catch ex As Exception
            _all = New List(Of CheckInvBlock)()
            lblResult.Text =
                "Unable to read CHECK.INV from:" & Environment.NewLine &
                path

            UiFileErrors.ShowUnableToReadRequiredFile(Me, ScreenTitle, path, ex)
        End Try
    End Sub

    Private Sub DoFind()
        Dim invNo = txtInvoice.Text.Trim()
        If invNo = "" Then Return

        If _all Is Nothing OrElse _all.Count = 0 Then
            LoadData()
            If _all Is Nothing OrElse _all.Count = 0 Then Return
        End If

        Dim hit = _all.FirstOrDefault(
            Function(b) b.Invoices IsNot Nothing AndAlso
                        b.Invoices.Any(Function(x) String.Equals(x.Trim(), invNo, StringComparison.OrdinalIgnoreCase))
        )

        If hit Is Nothing Then
            lblResult.Text = $"No match for invoice #{invNo}."
            Return
        End If

        lblResult.Text =
            $"Invoice {invNo} was paid by:" & Environment.NewLine &
            $"Customer: {hit.CustomerCode}" & Environment.NewLine &
            $"Check #:  {hit.CheckNumber}" & Environment.NewLine &
            $"Date:     {hit.DateText}" & Environment.NewLine &
            $"Amount:   {hit.Amount:C}" & Environment.NewLine &
            $"Invoices on that check: {hit.InvoiceCount}"
    End Sub

End Class