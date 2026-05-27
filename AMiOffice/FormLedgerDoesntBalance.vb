Option Strict On
Option Explicit On

Imports System.Linq
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Public Class FormLedgerDoesntBalance
    Inherits System.Windows.Forms.Form

    Private Const ScreenTitle As String = "Checks - Doesn't Balance"
    Private Const Tolerance As Decimal = 1D ' DOS treated < 1 as OK

    ' Row highlighting thresholds
    Private Const LargeDeltaWarn As Decimal = 25D
    Private Const LargeDeltaSevere As Decimal = 100D

    Private cts As CancellationTokenSource

    Private ReadOnly pnlHeader As New Panel()
    Private ReadOnly lblHeaderTitle As New Label()
    Private ReadOnly lblHeaderClock As New Label()
    Private ReadOnly tmrClock As System.Windows.Forms.Timer = New System.Windows.Forms.Timer()

    Private ReadOnly pnlCommands As New FlowLayoutPanel()
    Private ReadOnly pnlStatus As New Panel()

    Private ReadOnly dgv As New DataGridView()
    Private ReadOnly btnRefresh As New Button()
    Private ReadOnly btnCancel As New Button()
    Private ReadOnly btnCopyRow As New Button()
    Private ReadOnly btnExportCsv As New Button()
    Private ReadOnly btnClose As New Button()
    Private ReadOnly cboFilter As New ComboBox()
    Private ReadOnly lblFilter As New Label()
    Private ReadOnly lblStatusTop As New Label()
    Private ReadOnly lblStatusBottom As New Label()

    ' Keep the full results so we can filter/sort without re-running the report.
    Private allRows As List(Of BalanceRow) = New List(Of BalanceRow)()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = ScreenTitle
        Width = 1200
        Height = 850
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.FromArgb(34, 34, 34)
        ForeColor = Color.Gainsboro
        KeyPreview = True

        BuildUi()
        UpdateClock()

        tmrClock.Interval = 1000
        AddHandler tmrClock.Tick, Sub() UpdateClock()
        tmrClock.Start()

        FireAndForget(RunReportAsync())
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        If tmrClock IsNot Nothing Then
            tmrClock.Stop()
        End If

        If cts IsNot Nothing Then
            cts.Cancel()
            cts.Dispose()
            cts = Nothing
        End If

        MyBase.OnFormClosed(e)
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            Close()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub BuildUi()
        SuspendLayout()

        pnlHeader.Dock = DockStyle.Top
        pnlHeader.Height = 100
        pnlHeader.BackColor = Color.Black
        pnlHeader.Padding = New Padding(18, 10, 18, 10)

        lblHeaderTitle.AutoSize = False
        lblHeaderTitle.Left = 18
        lblHeaderTitle.Top = 8
        lblHeaderTitle.Width = 420
        lblHeaderTitle.Height = 70
        UiTheme.ApplyDosTitleStyle(lblHeaderTitle)
        lblHeaderTitle.Text = "CHECKS"

        lblHeaderClock.AutoSize = False
        lblHeaderClock.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        lblHeaderClock.Width = 420
        lblHeaderClock.Height = 40
        lblHeaderClock.Left = ClientSize.Width - lblHeaderClock.Width - 18
        lblHeaderClock.Top = 22
        lblHeaderClock.BackColor = Color.Black
        lblHeaderClock.ForeColor = Color.Yellow
        lblHeaderClock.Font = New Font("Segoe UI", 13.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblHeaderClock.TextAlign = ContentAlignment.MiddleRight

        AddHandler pnlHeader.Resize,
            Sub()
                lblHeaderClock.Left = pnlHeader.ClientSize.Width - lblHeaderClock.Width - 18
            End Sub

        pnlHeader.Controls.Add(lblHeaderTitle)
        pnlHeader.Controls.Add(lblHeaderClock)

        pnlCommands.Dock = DockStyle.Top
        pnlCommands.Height = 64
        pnlCommands.Padding = New Padding(12, 10, 12, 8)
        pnlCommands.FlowDirection = FlowDirection.LeftToRight
        pnlCommands.WrapContents = False
        pnlCommands.BackColor = Color.FromArgb(34, 34, 34)

        StyleMenuButton(btnRefresh, "Refresh", 110)
        StyleMenuButton(btnCancel, "Cancel", 110)
        StyleMenuButton(btnCopyRow, "Copy Row", 120)
        StyleMenuButton(btnExportCsv, "Export CSV", 130)
        StyleMenuButton(btnClose, "(ESC) Close", 130)

        btnCancel.Enabled = False
        btnCopyRow.Enabled = False
        btnExportCsv.Enabled = False

        AddHandler btnRefresh.Click, AddressOf btnRefresh_Click
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        AddHandler btnCopyRow.Click, AddressOf btnCopyRow_Click
        AddHandler btnExportCsv.Click, AddressOf btnExportCsv_Click
        AddHandler btnClose.Click, Sub() Close()

        lblFilter.AutoSize = True
        lblFilter.Padding = New Padding(10, 9, 0, 0)
        lblFilter.ForeColor = Color.Gainsboro
        lblFilter.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblFilter.Text = "Filter:"

        cboFilter.DropDownStyle = ComboBoxStyle.DropDownList
        cboFilter.Width = 220
        cboFilter.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular, GraphicsUnit.Point)
        cboFilter.BackColor = Color.White
        cboFilter.ForeColor = Color.Black
        cboFilter.Items.Clear()
        cboFilter.Items.Add(FilterMode.AllUnbalanced)
        cboFilter.Items.Add(FilterMode.NoMapping)
        cboFilter.Items.Add(FilterMode.DeltaOnly)
        cboFilter.SelectedIndex = 0
        AddHandler cboFilter.SelectedIndexChanged, AddressOf cboFilter_SelectedIndexChanged

        pnlStatus.Width = 250
        pnlStatus.Height = 38
        pnlStatus.Margin = New Padding(12, 0, 0, 0)
        pnlStatus.Padding = New Padding(0)
        pnlStatus.BackColor = Color.Transparent

        lblStatusTop.AutoSize = False
        lblStatusTop.Left = 0
        lblStatusTop.Top = 0
        lblStatusTop.Width = 250
        lblStatusTop.Height = 18
        lblStatusTop.Margin = New Padding(0)
        lblStatusTop.ForeColor = Color.Gainsboro
        lblStatusTop.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        lblStatusTop.TextAlign = ContentAlignment.MiddleLeft
        lblStatusTop.Text = ""

        lblStatusBottom.AutoSize = False
        lblStatusBottom.Left = 0
        lblStatusBottom.Top = 18
        lblStatusBottom.Width = 250
        lblStatusBottom.Height = 16
        lblStatusBottom.Margin = New Padding(0)
        lblStatusBottom.ForeColor = Color.Gainsboro
        lblStatusBottom.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point)
        lblStatusBottom.TextAlign = ContentAlignment.MiddleLeft
        lblStatusBottom.Text = ""

        pnlStatus.Controls.Add(lblStatusTop)
        pnlStatus.Controls.Add(lblStatusBottom)

        pnlCommands.Controls.Add(btnRefresh)
        pnlCommands.Controls.Add(btnCancel)
        pnlCommands.Controls.Add(btnCopyRow)
        pnlCommands.Controls.Add(btnExportCsv)
        pnlCommands.Controls.Add(btnClose)
        pnlCommands.Controls.Add(lblFilter)
        pnlCommands.Controls.Add(cboFilter)
        pnlCommands.Controls.Add(pnlStatus)

        dgv.Dock = DockStyle.Fill
        dgv.ReadOnly = True
        dgv.AllowUserToAddRows = False
        dgv.AllowUserToDeleteRows = False
        dgv.AllowUserToResizeRows = False
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgv.AutoGenerateColumns = False
        dgv.MultiSelect = False
        dgv.BackgroundColor = Color.Black
        dgv.BorderStyle = BorderStyle.None
        dgv.GridColor = Color.DimGray
        dgv.RowHeadersVisible = False
        dgv.EnableHeadersVisualStyles = False
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        dgv.ColumnHeadersHeight = 34
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dgv.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular, GraphicsUnit.Point)
        dgv.DefaultCellStyle.Font = New Font("Segoe UI", 10.0F, FontStyle.Regular, GraphicsUnit.Point)
        dgv.DefaultCellStyle.BackColor = Color.Black
        dgv.DefaultCellStyle.ForeColor = Color.Gainsboro
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 85, 140)
        dgv.DefaultCellStyle.SelectionForeColor = Color.White
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(18, 18, 18)

        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.Black
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gainsboro
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.Black
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Gainsboro
        dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)

        AddHandler dgv.SelectionChanged, AddressOf dgv_SelectionChanged
        AddHandler dgv.RowPrePaint, AddressOf dgv_RowPrePaint

        dgv.Columns.Clear()
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Customer", .DataPropertyName = NameOf(BalanceRow.Customer), .Width = 140})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Date", .DataPropertyName = NameOf(BalanceRow.DateText), .Width = 110})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Check #", .DataPropertyName = NameOf(BalanceRow.CheckNumber), .Width = 110})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Check Amount", .DataPropertyName = NameOf(BalanceRow.CheckAmount), .Width = 120})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Invoice Count", .DataPropertyName = NameOf(BalanceRow.InvoiceCount), .Width = 110})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Invoice Sum", .DataPropertyName = NameOf(BalanceRow.InvoiceSum), .Width = 120})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Delta", .DataPropertyName = NameOf(BalanceRow.Delta), .Width = 110})
        dgv.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = "Note", .DataPropertyName = NameOf(BalanceRow.Note), .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})

        Controls.Add(dgv)
        Controls.Add(pnlCommands)
        Controls.Add(pnlHeader)

        ResumeLayout()
    End Sub

    Private Sub StyleMenuButton(btn As Button, caption As String, width As Integer)
        btn.Text = caption
        btn.Width = width
        btn.Height = 30
        btn.Margin = New Padding(0, 0, 8, 0)
        btn.FlatStyle = FlatStyle.Standard
        btn.BackColor = Color.Silver
        btn.ForeColor = Color.Black
        btn.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        btn.UseVisualStyleBackColor = True
    End Sub

    Private Sub UpdateClock()
        lblHeaderClock.Text = DateTime.Now.ToString("dddd  MM-dd-yyyy      hh:mm:ss tt")
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        If cts IsNot Nothing Then cts.Cancel()
    End Sub

    Private Async Sub btnRefresh_Click(sender As Object, e As EventArgs)
        Await RunReportAsync()
    End Sub

    Private Sub btnCopyRow_Click(sender As Object, e As EventArgs)
        Dim r As BalanceRow = TryCast(dgv.CurrentRow?.DataBoundItem, BalanceRow)
        If r Is Nothing Then Return

        Dim text As String =
            r.Customer & vbTab &
            r.DateText & vbTab &
            r.CheckNumber & vbTab &
            r.CheckAmount & vbTab &
            r.InvoiceCount & vbTab &
            r.InvoiceSum & vbTab &
            r.Delta & vbTab &
            r.Note

        Clipboard.SetText(text)
        lblStatusTop.Text = "Copied selected row to clipboard."
        lblStatusBottom.Text = ""
    End Sub

    Private Sub btnExportCsv_Click(sender As Object, e As EventArgs)
        Dim rows As List(Of BalanceRow) = TryCast(dgv.DataSource, List(Of BalanceRow))
        If rows Is Nothing OrElse rows.Count = 0 Then
            MessageBox.Show(Me, "There are no rows to export.", ScreenTitle, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using sfd As New SaveFileDialog()
            sfd.Title = "Export CSV"
            sfd.Filter = "CSV (*.csv)|*.csv|All files (*.*)|*.*"
            sfd.FileName = $"LedgerDoesntBalance_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            If sfd.ShowDialog(Me) <> DialogResult.OK Then Return

            Try
                Dim sb As New StringBuilder()
                sb.AppendLine("Customer,Date,CheckNumber,CheckAmount,InvoiceCount,InvoiceSum,Delta,Note")

                For Each r In rows
                    sb.AppendLine(
                        Csv(r.Customer) & "," &
                        Csv(r.DateText) & "," &
                        Csv(r.CheckNumber) & "," &
                        Csv(r.CheckAmount) & "," &
                        Csv(r.InvoiceCount) & "," &
                        Csv(r.InvoiceSum) & "," &
                        Csv(r.Delta) & "," &
                        Csv(r.Note)
                    )
                Next

                IO.File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8)
                lblStatusTop.Text = $"Exported {rows.Count:N0} row(s) to CSV."
                lblStatusBottom.Text = ""
            Catch ex As Exception
                MessageBox.Show(Me, ex.ToString(), ScreenTitle, MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub cboFilter_SelectedIndexChanged(sender As Object, e As EventArgs)
        ApplyFilterAndBind()
    End Sub

    Private Sub dgv_SelectionChanged(sender As Object, e As EventArgs)
        btnCopyRow.Enabled = (dgv.CurrentRow IsNot Nothing AndAlso dgv.CurrentRow.DataBoundItem IsNot Nothing)
    End Sub

    Private Sub dgv_RowPrePaint(sender As Object, e As DataGridViewRowPrePaintEventArgs)
        Dim row As DataGridViewRow = dgv.Rows(e.RowIndex)
        Dim data As BalanceRow = TryCast(row.DataBoundItem, BalanceRow)
        If data Is Nothing Then Return

        row.DefaultCellStyle.BackColor = dgv.DefaultCellStyle.BackColor
        row.DefaultCellStyle.ForeColor = dgv.DefaultCellStyle.ForeColor
        row.DefaultCellStyle.Font = dgv.DefaultCellStyle.Font
        row.DefaultCellStyle.SelectionForeColor = Color.Black

        If data.IsNoMapping Then
            row.DefaultCellStyle.BackColor = Color.FromArgb(250, 245, 190)
            row.DefaultCellStyle.ForeColor = Color.Black
            row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(253, 249, 205)
            row.DefaultCellStyle.SelectionForeColor = Color.Black
            row.DefaultCellStyle.Font = dgv.DefaultCellStyle.Font
            Return
        End If

        Dim absDelta As Decimal = Math.Abs(data.DeltaValue)
        If absDelta >= LargeDeltaSevere Then
            row.DefaultCellStyle.BackColor = Color.FromArgb(249, 232, 236)
            row.DefaultCellStyle.ForeColor = Color.Black
            row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(252, 238, 241)
            row.DefaultCellStyle.SelectionForeColor = Color.Black
        ElseIf absDelta >= LargeDeltaWarn Then
            row.DefaultCellStyle.BackColor = Color.FromArgb(252, 239, 242)
            row.DefaultCellStyle.ForeColor = Color.Black
            row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 244, 246)
            row.DefaultCellStyle.SelectionForeColor = Color.Black
        End If
    End Sub

    Private Async Function RunReportAsync() As Task
        If cts IsNot Nothing Then
            cts.Cancel()
            cts.Dispose()
            cts = Nothing
        End If

        cts = New CancellationTokenSource()
        Dim token As CancellationToken = cts.Token

        btnRefresh.Enabled = False
        btnCancel.Enabled = True
        btnCopyRow.Enabled = False
        btnExportCsv.Enabled = False
        btnClose.Enabled = False
        cboFilter.Enabled = False
        lblStatusTop.Text = "Starting..."
        lblStatusBottom.Text = ""
        dgv.DataSource = Nothing
        allRows = New List(Of BalanceRow)()

        Dim ledgerPath As String = LegacyDataPaths.LedgerCur
        Dim checkInvPath As String = LegacyDataPaths.CheckInv
        Dim invoiceChkPath As String = LegacyDataPaths.InvoiceChk

        If Not IO.File.Exists(ledgerPath) Then
            UiFileErrors.ShowMissingRequiredFile(Me, ScreenTitle, ledgerPath)
            lblStatusTop.Text = "Missing LEDGER.CUR"
            lblStatusBottom.Text = ""
            btnRefresh.Enabled = True
            btnCancel.Enabled = False
            btnClose.Enabled = True
            cboFilter.Enabled = True
            Return
        End If

        Dim progress As IProgress(Of ProgressInfo) =
            New Progress(Of ProgressInfo)(
                Sub(p As ProgressInfo)
                    If token.IsCancellationRequested Then Return

                    lblStatusTop.Text = p.Message

                    If p.Total > 0 Then
                        lblStatusBottom.Text = $"{p.Current:N0}/{p.Total:N0}"
                    Else
                        lblStatusBottom.Text = ""
                    End If
                End Sub
            )

        Try
            Dim rows As List(Of BalanceRow) =
                Await Task.Run(Function() ComputeRows(ledgerPath, checkInvPath, invoiceChkPath, token, progress), token)

            If token.IsCancellationRequested Then
                lblStatusTop.Text = "Cancelled"
                lblStatusBottom.Text = ""
                Return
            End If

            allRows = rows
            ApplyFilterAndBind()
        Catch ex As OperationCanceledException
            lblStatusTop.Text = "Cancelled"
            lblStatusBottom.Text = ""
        Catch ex As Exception
            lblStatusTop.Text = "Error running report"
            lblStatusBottom.Text = ""
            MessageBox.Show(Me, ex.ToString(), ScreenTitle, MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            btnRefresh.Enabled = True
            btnCancel.Enabled = False
            btnClose.Enabled = True
            cboFilter.Enabled = True

            Dim current As List(Of BalanceRow) = TryCast(dgv.DataSource, List(Of BalanceRow))
            btnExportCsv.Enabled = (current IsNot Nothing AndAlso current.Count > 0)
        End Try
    End Function

    Private Sub ApplyFilterAndBind()
        Dim mode As String = TryCast(cboFilter.SelectedItem, String)
        If String.IsNullOrWhiteSpace(mode) Then mode = FilterMode.AllUnbalanced

        Dim filtered As IEnumerable(Of BalanceRow) = allRows

        Select Case mode
            Case FilterMode.NoMapping
                filtered = filtered.Where(Function(r) r.IsNoMapping)
            Case FilterMode.DeltaOnly
                filtered = filtered.Where(Function(r) Not r.IsNoMapping)
            Case Else
        End Select

        Dim sorted As List(Of BalanceRow) =
            filtered.OrderByDescending(Function(r) If(r.IsNoMapping, Decimal.MaxValue, Math.Abs(r.DeltaValue))).ToList()

        dgv.DataSource = sorted

        lblStatusTop.Text = "Report complete"
        lblStatusBottom.Text = $"{allRows.Count:N0} checked"

        btnExportCsv.Enabled = (sorted.Count > 0)
        btnCopyRow.Enabled = (dgv.CurrentRow IsNot Nothing AndAlso dgv.CurrentRow.DataBoundItem IsNot Nothing)
    End Sub

    Private Shared Function ComputeRows(
        ledgerPath As String,
        checkInvPath As String,
        invoiceChkPath As String,
        token As CancellationToken,
        progress As IProgress(Of ProgressInfo)
    ) As List(Of BalanceRow)

        token.ThrowIfCancellationRequested()
        progress?.Report(New ProgressInfo("Loading ledger...", 0, 0))

        Dim ledger As List(Of LedgerEntry) = LedgerCurReader.ReadAll(ledgerPath)
        token.ThrowIfCancellationRequested()

        progress?.Report(New ProgressInfo("Loading CHECK.INV...", 0, 0))

        Dim invIndex As New Dictionary(Of String, List(Of Long))(StringComparer.OrdinalIgnoreCase)
        If IO.File.Exists(checkInvPath) Then
            Try
                Dim blocks = CheckInvReader.ReadAll(checkInvPath)
                Dim i As Integer = 0
                For Each b In blocks
                    token.ThrowIfCancellationRequested()
                    i += 1
                    If (i Mod 250) = 0 Then
                        progress?.Report(New ProgressInfo("Indexing CHECK.INV...", i, blocks.Count))
                    End If

                    Dim key = BuildKey(b.CustomerCode, b.CheckNumber)
                    Dim lst As List(Of Long) = Nothing
                    If Not invIndex.TryGetValue(key, lst) Then
                        lst = New List(Of Long)()
                        invIndex(key) = lst
                    End If

                    If b.Invoices IsNot Nothing Then
                        For Each s In b.Invoices
                            Dim n As Long
                            If Long.TryParse(If(s, "").Trim(), n) Then
                                lst.Add(n)
                            End If
                        Next
                    End If
                Next
            Catch
                ' Best effort; keep what we have.
            End Try
        End If

        token.ThrowIfCancellationRequested()

        progress?.Report(New ProgressInfo("Computing rows...", 0, ledger.Count))

        Dim amtCache As New Dictionary(Of Long, Decimal)()
        Dim rows As New List(Of BalanceRow)()

        Dim idx As Integer = 0
        For Each e In ledger
            token.ThrowIfCancellationRequested()

            idx += 1
            If (idx Mod 250) = 0 Then
                progress?.Report(New ProgressInfo("Computing rows...", idx, ledger.Count))
            End If

            Dim key = BuildKey(e.Customer, e.CheckNumber)

            Dim invNos As List(Of Long) = Nothing
            Dim hasMapping As Boolean = invIndex.TryGetValue(key, invNos)

            If Not hasMapping OrElse invNos Is Nothing OrElse invNos.Count = 0 Then
                rows.Add(New BalanceRow With {
                    .Customer = SafeTrimEnd(e.Customer),
                    .DateText = SafeTrimEnd(e.DateText),
                    .CheckNumber = SafeTrim(e.CheckNumber),
                    .CheckAmountValue = e.Amount,
                    .InvoiceCountValue = 0,
                    .InvoiceSumValue = 0D,
                    .DeltaValue = 0D,
                    .Note = "No CHECK.INV mapping",
                    .IsNoMapping = True
                })
                Continue For
            End If

            Dim sum As Decimal = 0D
            Dim foundAny As Boolean = False

            For Each invNo In invNos
                token.ThrowIfCancellationRequested()

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
                    .CheckAmountValue = e.Amount,
                    .InvoiceCountValue = invNos.Count,
                    .InvoiceSumValue = sum,
                    .DeltaValue = delta,
                    .Note = note,
                    .IsNoMapping = False
                })
            End If
        Next

        ' Final exact progress update so the live counter reaches the true final row count.
        progress?.Report(New ProgressInfo("Computing rows...", ledger.Count, ledger.Count))
        progress?.Report(New ProgressInfo("Done.", ledger.Count, ledger.Count))

        Return rows
    End Function

    Private Shared Sub FireAndForget(t As Task)
        If t Is Nothing Then Return

        t.ContinueWith(
            Sub(completed As Task)
                Dim ignored = completed.Exception
            End Sub,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default
        )
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

    Private Shared Function Csv(s As String) As String
        Dim v As String = If(s, "")
        Dim mustQuote As Boolean =
            (v.Contains(","c) OrElse v.Contains(ControlChars.Quote) OrElse v.Contains(ControlChars.Cr) OrElse v.Contains(ControlChars.Lf))

        If mustQuote Then
            v = v.Replace("""", """""")
            v = """" & v & """"
        End If

        Return v
    End Function

    Private NotInheritable Class FilterMode
        Public Const AllUnbalanced As String = "All unbalanced"
        Public Const NoMapping As String = "No CHECK.INV mapping"
        Public Const DeltaOnly As String = "Delta rows (has mapping)"
    End Class

    Private NotInheritable Class ProgressInfo
        Public ReadOnly Property Message As String
        Public ReadOnly Property Current As Integer
        Public ReadOnly Property Total As Integer

        Public Sub New(message As String, current As Integer, total As Integer)
            Me.Message = message
            Me.Current = current
            Me.Total = total
        End Sub
    End Class

    Private Class BalanceRow
        Public Property Customer As String
        Public Property DateText As String
        Public Property CheckNumber As String

        Public Property CheckAmountValue As Decimal
        Public Property InvoiceCountValue As Integer
        Public Property InvoiceSumValue As Decimal
        Public Property DeltaValue As Decimal

        Public Property Note As String
        Public Property IsNoMapping As Boolean

        Public ReadOnly Property CheckAmount As String
            Get
                Return CheckAmountValue.ToString("C")
            End Get
        End Property

        Public ReadOnly Property InvoiceCount As String
            Get
                Return InvoiceCountValue.ToString()
            End Get
        End Property

        Public ReadOnly Property InvoiceSum As String
            Get
                Return If(IsNoMapping, "", InvoiceSumValue.ToString("C"))
            End Get
        End Property

        Public ReadOnly Property Delta As String
            Get
                Return If(IsNoMapping, "", DeltaValue.ToString("C"))
            End Get
        End Property
    End Class
End Class