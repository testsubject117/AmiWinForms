Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Option T - Change Min Charge for a Procedure
''' DOS source: PLIST.ASC line 4700
''' Modern grid-based UI for updating min charge across multiple customers
''' </summary>
Public Class FormChangeMinCharge
    Inherits Form

    Private _txtProcedure As TextBox
    Private _txtNewMinCharge As TextBox
    Private _lblProcedure As Label
    Private _lblNewMinCharge As Label
    Private _btnSearch As Button
    Private _btnClose As Button
    Private _btnApply As Button
    Private _btnSelectAll As Button
    Private _btnDeselectAll As Button
    Private _dgvCustomers As DataGridView
    Private _pnlInput As Panel
    Private _pnlGrid As Panel

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Change Min Charge for Procedure"
        Me.Size = New Size(1000, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.Black
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MinimumSize = New Size(800, 600)

        ' Grid panel - add first so it's behind
        _pnlGrid = New Panel()
        _pnlGrid.Location = New Point(20, 150)
        _pnlGrid.Size = New Size(960, 435)
        _pnlGrid.BackColor = Color.Black
        _pnlGrid.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(_pnlGrid)

        _dgvCustomers = New DataGridView()
        _dgvCustomers.Dock = DockStyle.Fill
        _dgvCustomers.BackgroundColor = Color.Black
        _dgvCustomers.ForeColor = Color.White
        _dgvCustomers.GridColor = Color.DarkGray
        _dgvCustomers.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkSlateGray
        _dgvCustomers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        _dgvCustomers.ColumnHeadersDefaultCellStyle.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _dgvCustomers.DefaultCellStyle.BackColor = Color.Black
        _dgvCustomers.DefaultCellStyle.ForeColor = Color.White
        _dgvCustomers.DefaultCellStyle.SelectionBackColor = Color.DarkBlue
        _dgvCustomers.DefaultCellStyle.SelectionForeColor = Color.White
        _dgvCustomers.DefaultCellStyle.Font = New Font("Consolas", 9.0F, FontStyle.Regular)
        _dgvCustomers.AllowUserToAddRows = False
        _dgvCustomers.AllowUserToDeleteRows = False
        _dgvCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _dgvCustomers.MultiSelect = False
        _dgvCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _dgvCustomers.RowHeadersVisible = False
        _pnlGrid.Controls.Add(_dgvCustomers)

        ' Input panel (top) - add after grid so it's on top
        _pnlInput = New Panel()
        _pnlInput.Dock = DockStyle.Top
        _pnlInput.Height = 140
        _pnlInput.BackColor = Color.Black
        Me.Controls.Add(_pnlInput)

        _lblProcedure = New Label()
        _lblProcedure.Text = "Procedure to change (partial name ok):"
        _lblProcedure.Location = New Point(20, 20)
        _lblProcedure.Size = New Size(400, 25)
        _lblProcedure.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _lblProcedure.ForeColor = Color.White
        _lblProcedure.BackColor = Color.Black
        _pnlInput.Controls.Add(_lblProcedure)

        _txtProcedure = New TextBox()
        _txtProcedure.Location = New Point(20, 50)
        _txtProcedure.Size = New Size(500, 30)
        _txtProcedure.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _txtProcedure.CharacterCasing = CharacterCasing.Upper
        _pnlInput.Controls.Add(_txtProcedure)

        _lblNewMinCharge = New Label()
        _lblNewMinCharge.Text = "New Min. Charge:"
        _lblNewMinCharge.Location = New Point(540, 20)
        _lblNewMinCharge.Size = New Size(200, 25)
        _lblNewMinCharge.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _lblNewMinCharge.ForeColor = Color.White
        _lblNewMinCharge.BackColor = Color.Black
        _pnlInput.Controls.Add(_lblNewMinCharge)

        _txtNewMinCharge = New TextBox()
        _txtNewMinCharge.Location = New Point(540, 50)
        _txtNewMinCharge.Size = New Size(150, 30)
        _txtNewMinCharge.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _pnlInput.Controls.Add(_txtNewMinCharge)

        _btnSearch = New Button()
        _btnSearch.Text = "Search"
        _btnSearch.Location = New Point(710, 48)
        _btnSearch.Size = New Size(120, 34)
        _btnSearch.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _btnSearch.BackColor = Color.LightGray
        _btnSearch.ForeColor = Color.Black
        _btnSearch.FlatStyle = FlatStyle.Flat
        AddHandler _btnSearch.Click, AddressOf OnSearch
        _pnlInput.Controls.Add(_btnSearch)

        _btnSelectAll = New Button()
        _btnSelectAll.Text = "Select All"
        _btnSelectAll.Location = New Point(20, 100)
        _btnSelectAll.Size = New Size(120, 30)
        _btnSelectAll.Font = New Font("Consolas", 9.0F, FontStyle.Bold)
        _btnSelectAll.BackColor = Color.LightGray
        _btnSelectAll.ForeColor = Color.Black
        _btnSelectAll.FlatStyle = FlatStyle.Flat
        _btnSelectAll.Enabled = False
        AddHandler _btnSelectAll.Click, AddressOf OnSelectAll
        _pnlInput.Controls.Add(_btnSelectAll)

        _btnDeselectAll = New Button()
        _btnDeselectAll.Text = "Deselect All"
        _btnDeselectAll.Location = New Point(150, 100)
        _btnDeselectAll.Size = New Size(120, 30)
        _btnDeselectAll.Font = New Font("Consolas", 9.0F, FontStyle.Bold)
        _btnDeselectAll.BackColor = Color.LightGray
        _btnDeselectAll.ForeColor = Color.Black
        _btnDeselectAll.FlatStyle = FlatStyle.Flat
        _btnDeselectAll.Enabled = False
        AddHandler _btnDeselectAll.Click, AddressOf OnDeselectAll
        _pnlInput.Controls.Add(_btnDeselectAll)

        ' Buttons at bottom
        _btnApply = New Button()
        _btnApply.Text = "Apply Changes"
        _btnApply.Location = New Point(730, 600)
        _btnApply.Size = New Size(140, 40)
        _btnApply.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _btnApply.BackColor = Color.LightGray
        _btnApply.ForeColor = Color.Black
        _btnApply.FlatStyle = FlatStyle.Flat
        _btnApply.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        _btnApply.Enabled = False
        AddHandler _btnApply.Click, AddressOf OnApply
        Me.Controls.Add(_btnApply)

        _btnClose = New Button()
        _btnClose.Text = "Close"
        _btnClose.Location = New Point(880, 600)
        _btnClose.Size = New Size(100, 40)
        _btnClose.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _btnClose.BackColor = Color.LightGray
        _btnClose.ForeColor = Color.Black
        _btnClose.FlatStyle = FlatStyle.Flat
        _btnClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        AddHandler _btnClose.Click, AddressOf OnClose
        Me.Controls.Add(_btnClose)

        Me.AcceptButton = _btnSearch
        Me.CancelButton = _btnClose
    End Sub

    Private Sub OnSearch(sender As Object, e As EventArgs)
        Dim procedureName = _txtProcedure.Text.Trim()
        If String.IsNullOrEmpty(procedureName) Then
            DosMessageBox.Show(Me, "Please enter a procedure name.", "Error", MessageBoxButtons.OK)
            _txtProcedure.Focus()
            Return
        End If

        Dim newMinChargeText = _txtNewMinCharge.Text.Trim()
        Dim newMinCharge As Decimal
        If Not Decimal.TryParse(newMinChargeText, newMinCharge) Then
            DosMessageBox.Show(Me, "Please enter a valid min charge amount.", "Error", MessageBoxButtons.OK)
            _txtNewMinCharge.Focus()
            Return
        End If

        Try
            Cursor = Cursors.WaitCursor

            ' Load all customers
            Dim realnameFile = Path.Combine(LegacyDataPaths.BaseDataDir, "realname.dat")
            If Not File.Exists(realnameFile) Then
                DosMessageBox.Show(Me, "realname.dat not found.", "Error", MessageBoxButtons.OK)
                Return
            End If

            Dim allCustomers As New List(Of Tuple(Of String, String))()
            Using reader As New StreamReader(realnameFile)
                While Not reader.EndOfStream
                    Dim fileName = reader.ReadLine()?.Trim(""""c)
                    Dim displayName = reader.ReadLine()?.Trim(""""c)
                    If Not String.IsNullOrEmpty(fileName) AndAlso Not String.IsNullOrEmpty(displayName) Then
                        allCustomers.Add(Tuple.Create(fileName, displayName))
                    End If
                End While
            End Using

            ' Find customers that have this procedure
            Dim matches As New List(Of CustomerProcedureMatch)()
            For Each customer In allCustomers
                Dim prcFile = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC", customer.Item1)
                If File.Exists(prcFile) Then
                    Using reader As New StreamReader(prcFile)
                        While Not reader.EndOfStream
                            Dim line = reader.ReadLine()
                            If String.IsNullOrWhiteSpace(line) Then Continue While

                            Dim parts = ParseDosWriteLine(line)
                            If parts.Length >= 5 Then
                                Dim procName = parts(0)
                                Dim currentMinCharge = CDec(parts(2))

                                ' Check if procedure matches and has min charge > 0
                                If procName.IndexOf(procedureName, StringComparison.OrdinalIgnoreCase) >= 0 AndAlso currentMinCharge > 0 Then
                                    matches.Add(New CustomerProcedureMatch() With {
                                        .FileName = customer.Item1,
                                        .DisplayName = customer.Item2,
                                        .ProcedureName = procName,
                                        .CurrentMinCharge = currentMinCharge,
                                        .NewMinCharge = newMinCharge,
                                        .Selected = False
                                    })
                                    Exit While ' Only need to find it once per customer
                                End If
                            End If
                        End While
                    End Using
                End If
            Next

            If matches.Count = 0 Then
                DosMessageBox.Show(Me, $"No customers found with procedure: {procedureName}", "No Matches", MessageBoxButtons.OK)
                Return
            End If

            ' Populate grid
            _dgvCustomers.Rows.Clear()
            _dgvCustomers.Columns.Clear()

            Dim colSelect As New DataGridViewCheckBoxColumn()
            colSelect.Name = "Select"
            colSelect.HeaderText = "Select"
            colSelect.Width = 60
            colSelect.FillWeight = 10
            _dgvCustomers.Columns.Add(colSelect)

            _dgvCustomers.Columns.Add("CustomerName", "Customer Name")
            _dgvCustomers.Columns.Add("ProcedureName", "Procedure")
            _dgvCustomers.Columns.Add("CurrentMinCharge", "Current Min Charge")
            _dgvCustomers.Columns.Add("NewMinCharge", "New Min Charge")

            _dgvCustomers.Columns("CustomerName").FillWeight = 30
            _dgvCustomers.Columns("ProcedureName").FillWeight = 30
            _dgvCustomers.Columns("CurrentMinCharge").FillWeight = 15
            _dgvCustomers.Columns("NewMinCharge").FillWeight = 15

            _dgvCustomers.Columns("CurrentMinCharge").DefaultCellStyle.Format = "C2"
            _dgvCustomers.Columns("NewMinCharge").DefaultCellStyle.Format = "C2"
            _dgvCustomers.Columns("NewMinCharge").ReadOnly = False

            For Each match In matches
                Dim rowIndex = _dgvCustomers.Rows.Add()
                Dim row = _dgvCustomers.Rows(rowIndex)
                row.Cells("Select").Value = True  ' Default to checked - more intuitive!
                row.Cells("CustomerName").Value = match.DisplayName
                row.Cells("ProcedureName").Value = match.ProcedureName
                row.Cells("CurrentMinCharge").Value = match.CurrentMinCharge
                row.Cells("NewMinCharge").Value = match.NewMinCharge
                row.Tag = match
            Next

            _btnSelectAll.Enabled = True
            _btnDeselectAll.Enabled = True
            _btnApply.Enabled = True

        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error searching:{Environment.NewLine}{ex.Message}",
                              "Error", MessageBoxButtons.OK)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub OnSelectAll(sender As Object, e As EventArgs)
        For Each row As DataGridViewRow In _dgvCustomers.Rows
            row.Cells("Select").Value = True
        Next
    End Sub

    Private Sub OnDeselectAll(sender As Object, e As EventArgs)
        For Each row As DataGridViewRow In _dgvCustomers.Rows
            row.Cells("Select").Value = False
        Next
    End Sub

    Private Sub OnApply(sender As Object, e As EventArgs)
        Try
            Cursor = Cursors.WaitCursor
            Dim changeCount = 0

            For Each row As DataGridViewRow In _dgvCustomers.Rows
                Dim selected = CBool(row.Cells("Select").Value)
                If Not selected Then Continue For

                Dim match = DirectCast(row.Tag, CustomerProcedureMatch)
                Dim newMinCharge = CDec(row.Cells("NewMinCharge").Value)

                ' Update the customer's .PRC file
                Dim prcFile = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC", match.FileName)
                Dim tempFile = Path.Combine(LegacyDataPaths.BaseDataDir, "pricelst.tmp")

                Using reader As New StreamReader(prcFile)
                    Using writer As New StreamWriter(tempFile, False)
                        While Not reader.EndOfStream
                            Dim line = reader.ReadLine()
                            If String.IsNullOrWhiteSpace(line) Then Continue While

                            Dim parts = ParseDosWriteLine(line)
                            If parts.Length >= 5 Then
                                Dim procName = parts(0)
                                Dim effDate = parts(1)
                                Dim minCharge = CDec(parts(2))
                                Dim price = parts(3)
                                Dim priceType = parts(4)

                                ' Check if this is the procedure to update
                                If procName = match.ProcedureName AndAlso minCharge > 0 Then
                                    minCharge = newMinCharge
                                    effDate = DateTime.Now.ToString("MM-dd-yyyy")
                                End If

                                ' Write back in DOS WRITE# format
                                writer.WriteLine($"""{procName}"",""{effDate}"",{minCharge},{price},""{priceType}""")
                            End If
                        End While
                    End Using
                End Using

                File.Copy(tempFile, prcFile, True)
                File.Delete(tempFile)
                changeCount += 1
            Next

            DosMessageBox.Show(Me, $"Successfully updated {changeCount} customer(s).", "Success", MessageBoxButtons.OK)
            Me.Close()

        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error applying changes:{Environment.NewLine}{ex.Message}",
                              "Error", MessageBoxButtons.OK)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    Private Function ParseDosWriteLine(line As String) As String()
        Dim parts As New List(Of String)()
        Dim current As New Text.StringBuilder()
        Dim inQuotes = False

        For i = 0 To line.Length - 1
            Dim c = line(i)
            If c = """"c Then
                inQuotes = Not inQuotes
            ElseIf c = ","c AndAlso Not inQuotes Then
                parts.Add(current.ToString().Trim())
                current.Clear()
            Else
                current.Append(c)
            End If
        Next

        If current.Length > 0 Then
            parts.Add(current.ToString().Trim())
        End If

        Return parts.ToArray()
    End Function

    Private Sub OnClose(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Class CustomerProcedureMatch
        Public Property FileName As String
        Public Property DisplayName As String
        Public Property ProcedureName As String
        Public Property CurrentMinCharge As Decimal
        Public Property NewMinCharge As Decimal
        Public Property Selected As Boolean
    End Class
End Class
