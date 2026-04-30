Option Strict On
Option Explicit On

Imports System.Linq

Public Class FormOtherChecksView

    Private Const ScreenTitle As String = "Other Checks"

    Private ReadOnly txtCompany As New TextBox()
    Private ReadOnly btnRefresh As New Button()
    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly lblTotal As New Label()

    Private _all As New List(Of OtherCheckEntry)()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = ScreenTitle
        Width = 1100
        Height = 750
        StartPosition = FormStartPosition.CenterParent

        Dim top As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 45}
        top.Controls.Add(New Label() With {.Text = "Company contains:", .AutoSize = True, .Padding = New Padding(0, 10, 0, 0)})

        txtCompany.Width = 250
        top.Controls.Add(txtCompany)

        btnRefresh.Text = "Refresh"
        top.Controls.Add(btnRefresh)

        lblTotal.AutoSize = True
        lblTotal.Padding = New Padding(20, 10, 0, 0)
        top.Controls.Add(lblTotal)

        dgv.Dock = DockStyle.Fill
        dgv.ReadOnly = True
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.AutoGenerateColumns = True

        Controls.Add(dgv)
        Controls.Add(top)

        AddHandler btnRefresh.Click,
            Sub()
                LoadData()
                ApplyFilter()
            End Sub
        AddHandler txtCompany.TextChanged, Sub() ApplyFilter()

        LoadData()
        ApplyFilter()
    End Sub

    Private Sub LoadData()
        Dim path = LegacyDataPaths.OtherChk

        ' Clear current view so we don't show stale rows if load fails.
        _all = New List(Of OtherCheckEntry)()
        dgv.DataSource = Nothing
        lblTotal.Text = "Total: $0.00"

        If Not System.IO.File.Exists(path) Then
            lblTotal.Text = "OTHER.CHK was not found at: " & path
            UiFileErrors.ShowMissingRequiredFile(Me, ScreenTitle, path)
            Return
        End If

        Try
            _all = OtherChkReader.ReadAll(path)
        Catch ex As Exception
            _all = New List(Of OtherCheckEntry)()
            lblTotal.Text = "Unable to read OTHER.CHK from: " & path

            UiFileErrors.ShowUnableToReadRequiredFile(Me, ScreenTitle, path, ex)
        End Try
    End Sub

    Private Sub ApplyFilter()
        Dim s = txtCompany.Text.Trim()
        Dim q = _all.AsEnumerable()

        If s <> "" Then
            q = q.Where(Function(x) If(x.Company, "").IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
        End If

        Dim list = q.ToList()
        dgv.DataSource = list

        Dim total As Decimal = list.Sum(Function(x) x.Amount)
        lblTotal.Text = $"Total: {total:C}"
    End Sub

End Class