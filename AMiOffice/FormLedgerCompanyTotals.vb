Option Strict On
Option Explicit On

Imports System.Linq

Public Class FormLedgerCompanyTotals

    Private Const ScreenTitle As String = "Company Totals"

    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly lblGrandTotal As New Label()
    Private ReadOnly btnClose As New Button()

    Public Sub New()
        InitializeComponent()

        Text = "Ledger - Company Totals"
        Width = 900
        Height = 700
        StartPosition = FormStartPosition.CenterParent

        Dim top As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 40}
        lblGrandTotal.AutoSize = True
        lblGrandTotal.Padding = New Padding(10, 10, 0, 0)
        top.Controls.Add(lblGrandTotal)

        dgv.Dock = DockStyle.Fill
        dgv.ReadOnly = True
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.AutoGenerateColumns = True
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect

        btnClose.Text = "Close"
        btnClose.Dock = DockStyle.Bottom
        btnClose.Height = 40
        AddHandler btnClose.Click, Sub() Close()

        Controls.Add(dgv)
        Controls.Add(btnClose)
        Controls.Add(top)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)

        Dim path = LegacyDataPaths.LedgerCur

        ' Clear view first so we never display stale totals if load fails.
        dgv.DataSource = Nothing
        lblGrandTotal.Text = "Grand total: $0.00"

        If Not System.IO.File.Exists(path) Then
            lblGrandTotal.Text = "LEDGER.CUR was not found at: " & path
            UiFileErrors.ShowMissingRequiredFile(Me, ScreenTitle, path)
            Return
        End If

        Try
            Dim entries = LedgerCurReader.ReadAll(path)

            Dim totals =
                entries.
                GroupBy(Function(x) (If(x.Customer, "")).Trim()).
                Select(Function(g) New With {
                    .Customer = g.Key,
                    .Total = g.Sum(Function(x) x.Amount),
                    .Count = g.Count()
                }).
                OrderBy(Function(x) x.Customer).
                ToList()

            dgv.DataSource = totals

            Dim grand = totals.Sum(Function(x) x.Total)
            lblGrandTotal.Text = $"Grand total: {grand:C}   (Customers: {totals.Count})"
        Catch ex As Exception
            dgv.DataSource = Nothing
            lblGrandTotal.Text = "Unable to read LEDGER.CUR from: " & path

            UiFileErrors.ShowUnableToReadRequiredFile(Me, ScreenTitle, path, ex)
        End Try
    End Sub

End Class