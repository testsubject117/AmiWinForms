Option Strict On
Option Explicit On

Imports System.Linq

Public Class FormLedgerDoesntBalance
    Inherits System.Windows.Forms.Form
    Private Const ScreenTitle As String = "Checks - Doesn't Balance"

    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly btnRefresh As New Button()
    Private ReadOnly lblStatus As New Label()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = ScreenTitle
        Width = 1200
        Height = 850
        StartPosition = FormStartPosition.CenterParent

        BuildUi()
        RunReport()
    End Sub

    Private Sub BuildUi()
        Dim top As New FlowLayoutPanel() With {
            .Dock = DockStyle.Top,
            .Height = 40,
            .Padding = New Padding(8),
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False
        }

        btnRefresh.Text = "Refresh"
        btnRefresh.AutoSize = True
        AddHandler btnRefresh.Click, Sub() RunReport()

        lblStatus.AutoSize = True
        lblStatus.Padding = New Padding(12, 8, 0, 0)
        lblStatus.Text = ""

        top.Controls.Add(btnRefresh)
        top.Controls.Add(lblStatus)

        dgv.Dock = DockStyle.Fill
        dgv.ReadOnly = True
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgv.AutoGenerateColumns = False

        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Customer", .DataPropertyName = "Customer", .Width = 140})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Date", .DataPropertyName = "DateText", .Width = 110})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Check #", .DataPropertyName = "CheckNumber", .Width = 110})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Check Amount", .DataPropertyName = "CheckAmount", .Width = 120})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Invoice Count", .DataPropertyName = "InvoiceCount", .Width = 110})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Invoice Sum", .DataPropertyName = "InvoiceSum", .Width = 120})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Delta", .DataPropertyName = "Delta", .Width = 110})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Note", .DataPropertyName = "Note", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})

        Controls.Add(dgv)
        Controls.Add(top)
    End Sub

    Private Sub RunReport()
        lblStatus.Text = "Running..."
        dgv.DataSource = Nothing
        Application.DoEvents()

        Dim ledgerPath = LegacyDataPaths.LedgerCur
        Dim checkInvPath = LegacyDataPaths.CheckInv
        Dim invoiceChkPath = LegacyDataPaths.InvoiceChk

        If Not IO.File.Exists(ledgerPath) Then
            UiFileErrors.ShowMissingRequiredFile(Me, ScreenTitle, ledgerPath)
            lblStatus.Text = "Missing LEDGER.CUR"
            Return
        End If

        ' Load ledger
        Dim ledger As List(Of LedgerEntry)
        Try
            ledger = LedgerCurReader.ReadAll(ledgerPath)
        Catch ex As Exception
            UiFileErrors.ShowUnableToReadRequiredFile(Me, ScreenTitle, ledgerPath, ex)
            lblStatus.Text = "Unable to read LEDGER.CUR"
            Return
        End Try

        ' Load CHECK.INV blocks (if missing, treat as no mapping for all)
        Dim invIndex As New Dictionary(Of String, List(Of Long))(StringComparer.OrdinalIgnoreCase)
        If IO.File.Exists(checkInvPath) Then
            Try
                Dim blocks = CheckInvReader.ReadAll(checkInvPath)
                For Each b In blocks
                    Dim key = BuildKey(b.CustomerCode, b.CheckNumber)
                    Dim lst As List(Of Long) = Nothing
                    If Not invIndex.TryGetValue(key, lst) Then
                        lst = New List(Of Long)()
                        invIndex(key) = lst
                    End If

                    If b.Invoices IsNot Nothing Then
                        For Each s In b.Invoices
                            Dim n As Long
                            If Long.TryParse(If(s, "").Trim(), n) Then lst.Add(n)
                        Next
                    End If
                Next
            Catch
                ' Best-effort; keep what we have.
            End Try
        End If

        ' Cache invoice amounts to avoid re-reading INVOICE.CHK for duplicates
        Dim amtCache As New Dictionary(Of Long, Decimal)()

        Dim rows As New List(Of BalanceRow)()
        Const Tolerance As Decimal = 1D ' DOS treated < 1 as OK

        For Each e In ledger
            Dim key = BuildKey(e.Customer, e.CheckNumber)

            Dim invNos As List(Of Long) = Nothing
            Dim hasMapping = invIndex.TryGetValue(key, invNos)

            If Not hasMapping OrElse invNos Is Nothing OrElse invNos.Count = 0 Then
                ' Include "No mapping" rows (your requested scope)
                rows.Add(New BalanceRow With {
                    .Customer = SafeTrimEnd(e.Customer),
                    .DateText = SafeTrimEnd(e.DateText),
                    .CheckNumber = SafeTrim(e.CheckNumber),
                    .CheckAmount = e.Amount.ToString("C"),
                    .InvoiceCount = "0",
                    .InvoiceSum = "",
                    .Delta = "",
                    .Note = "No CHECK.INV mapping"
                })
                Continue For
            End If

            Dim sum As Decimal = 0D
            Dim foundAny As Boolean = False

            For Each invNo In invNos
                Dim a As Decimal = 0D
                If amtCache.TryGetValue(invNo, a) Then
                    sum += a
                    foundAny = True
                Else
                    Dim rec = InvoiceChkReader.ReadRecord(invoiceChkPath, invNo)
                    If rec.IsFound Then
                        a = rec.Amount
                        amtCache(invNo) = a
                        sum += a
                        foundAny = True
                    Else
                        ' Cache missing as 0 so we don't keep re-reading it
                        amtCache(invNo) = 0D
                    End If
                End If
            Next

            Dim delta As Decimal = sum - e.Amount
            Dim unbalanced As Boolean = (Not foundAny) OrElse (Math.Abs(delta) >= Tolerance)

            If unbalanced Then
                Dim note As String = If(Not foundAny, "Invoices not found in INVOICE.CHK", "")
                rows.Add(New BalanceRow With {
                    .Customer = SafeTrimEnd(e.Customer),
                    .DateText = SafeTrimEnd(e.DateText),
                    .CheckNumber = SafeTrim(e.CheckNumber),
                    .CheckAmount = e.Amount.ToString("C"),
                    .InvoiceCount = invNos.Count.ToString(),
                    .InvoiceSum = sum.ToString("C"),
                    .Delta = delta.ToString("C"),
                    .Note = note
                })
            End If
        Next

        ' Only mismatches requested: filter out balanced rows, but keep "No mapping"
        ' (No mapping rows already included above; they're inherently mismatches for this report)
        dgv.DataSource = rows
        lblStatus.Text = $"Rows: {rows.Count:N0}"
    End Sub

    Private Shared Function BuildKey(code As String, checkNum As String) As String
        Return If(code, "").TrimEnd() & "|" & If(checkNum, "").Trim()
    End Function

    Private Shared Function SafeTrim(s As String) As String
        Return If(s, "").Trim()
    End Function

    Private Shared Function SafeTrimEnd(s As String) As String
        Return If(s, "").TrimEnd()
    End Function

    Private NotInheritable Class BalanceRow
        Public Property Customer As String
        Public Property DateText As String
        Public Property CheckNumber As String
        Public Property CheckAmount As String
        Public Property InvoiceCount As String
        Public Property InvoiceSum As String
        Public Property Delta As String
        Public Property Note As String
    End Class
End Class