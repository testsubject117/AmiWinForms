Option Strict On
Option Explicit On

Imports System.IO
Imports System.Linq

Public Class FormFindByCheckNumber

    Private Const ScreenTitle As String = "Find by Check #"

    Private ReadOnly txtCheck As New TextBox()
    Private ReadOnly btnFind As New Button()
    Private ReadOnly btnRefresh As New Button()
    Private ReadOnly lblHeader As New Label()
    Private ReadOnly lstInvoices As New ListBox()

    Private _all As List(Of CheckInvBlock) = New List(Of CheckInvBlock)()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = "Find by Check # (CHECK.INV)"
        Width = 700
        Height = 650
        StartPosition = FormStartPosition.CenterParent

        Dim top As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 45, .Padding = New Padding(10)}

        top.Controls.Add(New Label() With {.Text = "Check #:", .AutoSize = True, .Padding = New Padding(0, 10, 0, 0)})

        txtCheck.Width = 150
        top.Controls.Add(txtCheck)

        btnFind.Text = "Find"
        top.Controls.Add(btnFind)

        btnRefresh.Text = "Refresh"
        top.Controls.Add(btnRefresh)

        lblHeader.Dock = DockStyle.Top
        lblHeader.Height = 60
        lblHeader.Padding = New Padding(10)
        lblHeader.Text = "Loading CHECK.INV..."

        lstInvoices.Dock = DockStyle.Fill

        Controls.Add(lstInvoices)
        Controls.Add(lblHeader)
        Controls.Add(top)

        AddHandler btnFind.Click, Sub() DoFind()

        AddHandler btnRefresh.Click,
            Sub()
                LoadData()
                lstInvoices.Items.Clear()
            End Sub

        AddHandler txtCheck.KeyDown,
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
                lblHeader.Text =
                    "CHECK.INV was not found at:" & Environment.NewLine &
                    path

                UiFileErrors.ShowMissingRequiredFile(Me, ScreenTitle, path)
                Return
            End If

            _all = CheckInvReader.ReadAll(path)

            lblHeader.Text = $"Loaded {_all.Count:N0} check records from CHECK.INV."
        Catch ex As Exception
            _all = New List(Of CheckInvBlock)()
            lblHeader.Text =
                "Unable to read CHECK.INV from:" & Environment.NewLine &
                path

            UiFileErrors.ShowUnableToReadRequiredFile(Me, ScreenTitle, path, ex)
        End Try
    End Sub

    Private Sub DoFind()
        Dim checkNo = txtCheck.Text.Trim()
        If checkNo = "" Then Return

        If _all Is Nothing OrElse _all.Count = 0 Then
            ' If the user opens the form when the share is down, allow them to try again later.
            LoadData()
            If _all Is Nothing OrElse _all.Count = 0 Then Return
        End If

        Dim hit = _all.FirstOrDefault(
            Function(b) String.Equals(If(b.CheckNumber, "").Trim(),
                                      checkNo,
                                      StringComparison.OrdinalIgnoreCase)
        )

        lstInvoices.Items.Clear()

        If hit Is Nothing Then
            lblHeader.Text = $"No match for check #{checkNo}."
            Return
        End If

        lblHeader.Text =
            $"Customer: {hit.CustomerCode}    " &
            $"Check#: {hit.CheckNumber}    " &
            $"Date: {hit.DateText}    " &
            $"Amount: {hit.Amount:C}    " &
            $"Invoices: {hit.InvoiceCount}"

        If hit.Invoices IsNot Nothing Then
            For Each inv In hit.Invoices
                lstInvoices.Items.Add(inv)
            Next
        End If
    End Sub

End Class