Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

''' <summary>
''' Option K - Increase some price lists by a Percent
''' DOS source: PLIST.ASC lines 2770-3150
''' Modern VB.NET implementation with DataGridView for batch selection
''' </summary>
Public Class FormBatchPriceIncrease
    Inherits Form

    Private _dgvCustomers As DataGridView
    Private _btnSelectAll As Button
    Private _btnDeselectAll As Button
    Private _btnApply As Button
    Private _btnCancel As Button
    Private _txtSearch As TextBox
    Private _lblInstructions As Label

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Batch Price Increase"
        Me.Size = New Size(1000, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.Black
        Me.KeyPreview = True

        ' Instructions
        _lblInstructions = New Label()
        _lblInstructions.Text = "Select customers and enter percentage increase (0-900%). Leave blank or 0 to skip."
        _lblInstructions.Location = New Point(20, 20)
        _lblInstructions.Size = New Size(950, 40)
        _lblInstructions.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        _lblInstructions.ForeColor = Color.FromArgb(192, 192, 192)
        _lblInstructions.BackColor = Color.Black
        Me.Controls.Add(_lblInstructions)

        ' Search box
        Dim lblSearch = New Label()
        lblSearch.Text = "Search:"
        lblSearch.Location = New Point(20, 70)
        lblSearch.Size = New Size(60, 25)
        lblSearch.Font = New Font("Consolas", 9.0F)
        lblSearch.ForeColor = Color.FromArgb(192, 192, 192)
        lblSearch.BackColor = Color.Black
        lblSearch.TextAlign = ContentAlignment.MiddleLeft
        Me.Controls.Add(lblSearch)

        _txtSearch = New TextBox()
        _txtSearch.Location = New Point(85, 68)
        _txtSearch.Size = New Size(300, 25)
        _txtSearch.Font = New Font("Consolas", 9.0F)
        _txtSearch.BackColor = Color.White
        _txtSearch.ForeColor = Color.Black
        AddHandler _txtSearch.TextChanged, AddressOf OnSearchTextChanged
        Me.Controls.Add(_txtSearch)

        ' Select All button
        _btnSelectAll = New Button()
        _btnSelectAll.Text = "Select All"
        _btnSelectAll.Location = New Point(400, 65)
        _btnSelectAll.Size = New Size(100, 30)
        _btnSelectAll.BackColor = Color.Black
        _btnSelectAll.ForeColor = Color.White
        _btnSelectAll.FlatStyle = FlatStyle.Flat
        _btnSelectAll.FlatAppearance.BorderColor = Color.DimGray
        AddHandler _btnSelectAll.Click, AddressOf OnSelectAll
        Me.Controls.Add(_btnSelectAll)

        ' Deselect All button
        _btnDeselectAll = New Button()
        _btnDeselectAll.Text = "Deselect All"
        _btnDeselectAll.Location = New Point(510, 65)
        _btnDeselectAll.Size = New Size(100, 30)
        _btnDeselectAll.BackColor = Color.Black
        _btnDeselectAll.ForeColor = Color.White
        _btnDeselectAll.FlatStyle = FlatStyle.Flat
        _btnDeselectAll.FlatAppearance.BorderColor = Color.DimGray
        AddHandler _btnDeselectAll.Click, AddressOf OnDeselectAll
        Me.Controls.Add(_btnDeselectAll)

        ' DataGridView with DOS styling
        _dgvCustomers = New DataGridView()
        _dgvCustomers.Location = New Point(20, 110)
        _dgvCustomers.Size = New Size(950, 480)
        _dgvCustomers.AllowUserToAddRows = False
        _dgvCustomers.AllowUserToDeleteRows = False
        _dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _dgvCustomers.MultiSelect = False
        _dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _dgvCustomers.BackgroundColor = Color.FromArgb(32, 32, 32)
        _dgvCustomers.BorderStyle = BorderStyle.Fixed3D
        _dgvCustomers.GridColor = Color.DimGray
        _dgvCustomers.EnableHeadersVisualStyles = False

        ' Header styling (DOS-like)
        _dgvCustomers.ColumnHeadersDefaultCellStyle.BackColor = Color.Black
        _dgvCustomers.ColumnHeadersDefaultCellStyle.ForeColor = Color.Yellow
        _dgvCustomers.ColumnHeadersDefaultCellStyle.Font = New Font("Consolas", 9.0F, FontStyle.Bold)
        _dgvCustomers.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.Black

        ' Row styling (DOS-like)
        _dgvCustomers.DefaultCellStyle.BackColor = Color.Black
        _dgvCustomers.DefaultCellStyle.ForeColor = Color.FromArgb(192, 192, 192)
        _dgvCustomers.DefaultCellStyle.Font = New Font("Consolas", 9.0F)
        _dgvCustomers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(64, 64, 64)
        _dgvCustomers.DefaultCellStyle.SelectionForeColor = Color.White

        ' Alternating row color
        _dgvCustomers.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(16, 16, 16)
        _dgvCustomers.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(64, 64, 64)

        ' Add columns
        Dim colSelect = New DataGridViewCheckBoxColumn()
        colSelect.HeaderText = "Select"
        colSelect.Name = "Select"
        colSelect.Width = 60
        colSelect.FillWeight = 10
        _dgvCustomers.Columns.Add(colSelect)

        Dim colRealname = New DataGridViewTextBoxColumn()
        colRealname.HeaderText = "Customer Name"
        colRealname.Name = "Realname"
        colRealname.ReadOnly = True
        colRealname.FillWeight = 50
        _dgvCustomers.Columns.Add(colRealname)

        Dim colFilename = New DataGridViewTextBoxColumn()
        colFilename.HeaderText = "Filename"
        colFilename.Name = "Filename"
        colFilename.ReadOnly = True
        colFilename.FillWeight = 20
        _dgvCustomers.Columns.Add(colFilename)

        Dim colPercent = New DataGridViewTextBoxColumn()
        colPercent.HeaderText = "% Increase"
        colPercent.Name = "Percent"
        colPercent.Width = 100
        colPercent.FillWeight = 15
        _dgvCustomers.Columns.Add(colPercent)

        Me.Controls.Add(_dgvCustomers)

        ' Apply button (DOS-style)
        _btnApply = New Button()
        _btnApply.Text = "Apply Increases"
        _btnApply.Location = New Point(770, 610)
        _btnApply.Size = New Size(120, 35)
        _btnApply.Font = New Font("Consolas", 9.0F, FontStyle.Bold)
        _btnApply.BackColor = Color.Black
        _btnApply.ForeColor = Color.Yellow
        _btnApply.FlatStyle = FlatStyle.Flat
        _btnApply.FlatAppearance.BorderColor = Color.DimGray
        AddHandler _btnApply.Click, AddressOf OnApply
        Me.Controls.Add(_btnApply)

        ' Cancel button (DOS-style)
        _btnCancel = New Button()
        _btnCancel.Text = "Cancel"
        _btnCancel.Location = New Point(900, 610)
        _btnCancel.Size = New Size(70, 35)
        _btnCancel.BackColor = Color.Black
        _btnCancel.ForeColor = Color.White
        _btnCancel.FlatStyle = FlatStyle.Flat
        _btnCancel.FlatAppearance.BorderColor = Color.DimGray
        AddHandler _btnCancel.Click, AddressOf OnCancel
        Me.Controls.Add(_btnCancel)

        ' Load customers
        LoadCustomers()
    End Sub

    Private Sub LoadCustomers()
        Try
            Dim realnameFile = Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.DAT")
            If Not File.Exists(realnameFile) Then Return

            Using reader As New StreamReader(realnameFile)
                While Not reader.EndOfStream
                    Dim filename = reader.ReadLine()?.Trim()
                    Dim realname = reader.ReadLine()?.Trim()

                    If Not String.IsNullOrEmpty(filename) AndAlso Not String.IsNullOrEmpty(realname) Then
                        ' Strip .PRC extension if present
                        If filename.EndsWith(".PRC", StringComparison.OrdinalIgnoreCase) Then
                            filename = filename.Substring(0, filename.Length - 4)
                        End If

                        Dim row = _dgvCustomers.Rows.Add()
                        _dgvCustomers.Rows(row).Cells("Select").Value = False
                        _dgvCustomers.Rows(row).Cells("Realname").Value = realname
                        _dgvCustomers.Rows(row).Cells("Filename").Value = filename
                        _dgvCustomers.Rows(row).Cells("Percent").Value = ""
                    End If
                End While
            End Using
        Catch ex As Exception
            DosMessageBox.Show(Me, "Error loading customers: " & ex.Message, "Error", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub OnSearchTextChanged(sender As Object, e As EventArgs)
        Dim searchText = _txtSearch.Text.Trim().ToUpperInvariant()

        For Each row As DataGridViewRow In _dgvCustomers.Rows
            If String.IsNullOrEmpty(searchText) Then
                row.Visible = True
            Else
                Dim realname = If(row.Cells("Realname").Value?.ToString(), "").ToUpperInvariant()
                Dim filename = If(row.Cells("Filename").Value?.ToString(), "").ToUpperInvariant()
                row.Visible = realname.Contains(searchText) OrElse filename.Contains(searchText)
            End If
        Next
    End Sub

    Private Sub OnSelectAll(sender As Object, e As EventArgs)
        For Each row As DataGridViewRow In _dgvCustomers.Rows
            If row.Visible Then
                row.Cells("Select").Value = True
            End If
        Next
    End Sub

    Private Sub OnDeselectAll(sender As Object, e As EventArgs)
        For Each row As DataGridViewRow In _dgvCustomers.Rows
            row.Cells("Select").Value = False
        Next
    End Sub

    Private Sub OnApply(sender As Object, e As EventArgs)
        Try
            ' Collect selected customers with percentages
            Dim toProcess As New List(Of Tuple(Of String, String, Decimal))

            For Each row As DataGridViewRow In _dgvCustomers.Rows
                Dim isSelected = CBool(row.Cells("Select").Value)
                If Not isSelected Then Continue For

                Dim filename = row.Cells("Filename").Value?.ToString()
                Dim realname = row.Cells("Realname").Value?.ToString()
                Dim percentText = row.Cells("Percent").Value?.ToString()?.Trim()

                If String.IsNullOrEmpty(filename) Then Continue For

                Dim percent As Decimal = 0
                If Not String.IsNullOrEmpty(percentText) Then
                    If Not Decimal.TryParse(percentText, percent) Then
                        DosMessageBox.Show(Me, $"Invalid percentage for {realname}: '{percentText}'",
                                       "Validation Error", MessageBoxButtons.OK)
                        Return
                    End If

                    If percent < 0 OrElse percent > 900 Then
                        DosMessageBox.Show(Me, $"Percentage for {realname} must be between 0 and 900.",
                                       "Validation Error", MessageBoxButtons.OK)
                        Return
                    End If
                End If

                If percent > 0 Then
                    toProcess.Add(Tuple.Create(filename, realname, percent))
                End If
            Next

            If toProcess.Count = 0 Then
                DosMessageBox.Show(Me, "No customers selected with valid percentages.", "Nothing to Process",
                               MessageBoxButtons.OK)
                Return
            End If

            ' Confirmation
            Dim msg = $"Apply price increases to {toProcess.Count} customer(s)?" & Environment.NewLine & Environment.NewLine
            For Each item In toProcess.Take(10)
                msg &= $"{item.Item3}% - {item.Item2}" & Environment.NewLine
            Next
            If toProcess.Count > 10 Then
                msg &= $"... and {toProcess.Count - 10} more"
            End If

            Dim result = DosMessageBox.Show(Me, msg, "Confirm", MessageBoxButtons.YesNo)
            If result <> DialogResult.Yes Then Return

            ' Apply increases
            Dim successCount = 0
            For Each item In toProcess
                If ApplyIncreaseToCustomer(item.Item1, item.Item3) Then
                    successCount += 1
                End If
            Next

            DosMessageBox.Show(Me, $"Successfully increased prices for {successCount} of {toProcess.Count} customers.",
                           "Complete", MessageBoxButtons.OK)

            Me.DialogResult = DialogResult.OK
            Me.Close()
        Catch ex As Exception
            DosMessageBox.Show(Me, "Error applying increases: " & ex.Message, "Error",
                           MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Function ApplyIncreaseToCustomer(filename As String, percent As Decimal) As Boolean
        Try
            Dim prcFile = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC", filename & ".PRC")
            If Not File.Exists(prcFile) Then Return False

            Dim procedures As New List(Of ProcedureItem)

            ' Read existing procedures
            Using reader As New StreamReader(prcFile)
                While Not reader.EndOfStream
                    Dim line = reader.ReadLine()
                    If String.IsNullOrEmpty(line) Then Continue While

                    Dim parts = line.Split("|"c)
                    If parts.Length < 5 Then Continue While

                    Dim proc As New ProcedureItem With {
                        .ProcedureName = parts(0).Trim(),
                        .EffectiveDate = parts(1).Trim(),
                        .MinCharge = Decimal.Parse(parts(2)),
                        .Price = Decimal.Parse(parts(3)),
                        .PriceType = parts(4).Trim()
                    }

                    ' Apply percentage increase
                    proc.MinCharge = proc.MinCharge * (1 + (percent / 100))
                    proc.Price = proc.Price * (1 + (percent / 100))

                    procedures.Add(proc)
                End While
            End Using

            ' Write back updated procedures
            Using writer As New StreamWriter(prcFile, False)
                For Each proc In procedures
                    writer.WriteLine($"{proc.ProcedureName}|{proc.EffectiveDate}|{proc.MinCharge:F2}|{proc.Price:F2}|{proc.PriceType}")
                Next
            End Using

            Return True
        Catch ex As Exception
            Debug.WriteLine($"Error increasing prices for {filename}: {ex.Message}")
            Return False
        End Try
    End Function

    Private Structure ProcedureItem
        Public ProcedureName As String
        Public EffectiveDate As String
        Public MinCharge As Decimal
        Public Price As Decimal
        Public PriceType As String
    End Structure
End Class

