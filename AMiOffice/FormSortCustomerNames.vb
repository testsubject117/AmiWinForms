Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

''' <summary>
''' Option N - Sort Customers Actual Names
''' DOS source: SORTNAME.ASC
''' Sorts REALNAME.DAT alphabetically by customer realname
''' </summary>
Public Class FormSortCustomerNames
    Inherits Form

    Private _lblStatus As Label
    Private _btnSort As Button
    Private _btnCancel As Button

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Sort Customer Names"
        Me.Size = New Size(800, 500)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.Black
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False

        ' Status label
        _lblStatus = New Label()
        _lblStatus.Text = "Are you sure you want to sort the Customers Actual Names?" & Environment.NewLine & Environment.NewLine &
                         "This will sort REALNAME.DAT alphabetically by customer name." & Environment.NewLine &
                         "A backup will be saved to REALNAME.BAC."
        _lblStatus.Location = New Point(30, 30)
        _lblStatus.Size = New Size(740, 350)
        _lblStatus.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _lblStatus.ForeColor = Color.White
        _lblStatus.BackColor = Color.Black
        _lblStatus.AutoSize = False
        Me.Controls.Add(_lblStatus)

        ' Sort button
        _btnSort = New Button()
        _btnSort.Text = "Yes - Sort Names"
        _btnSort.Location = New Point(200, 410)
        _btnSort.Size = New Size(180, 40)
        _btnSort.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _btnSort.BackColor = Color.LightGray
        _btnSort.ForeColor = Color.Black
        _btnSort.FlatStyle = FlatStyle.Flat
        AddHandler _btnSort.Click, AddressOf OnSort
        Me.Controls.Add(_btnSort)

        ' Cancel button
        _btnCancel = New Button()
        _btnCancel.Text = "No - Cancel"
        _btnCancel.Location = New Point(420, 410)
        _btnCancel.Size = New Size(180, 40)
        _btnCancel.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _btnCancel.BackColor = Color.LightGray
        _btnCancel.ForeColor = Color.Black
        _btnCancel.FlatStyle = FlatStyle.Flat
        AddHandler _btnCancel.Click, AddressOf OnCancel
        Me.Controls.Add(_btnCancel)

        Me.AcceptButton = _btnSort
        Me.CancelButton = _btnCancel
    End Sub

    Private Sub OnSort(sender As Object, e As EventArgs)
        Dim realnameFile = Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.DAT")
        Dim backupFile = Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.BAC")

        ' Retry logic for file-in-use errors (DOS Error 70)
        Dim maxRetries = 5
        Dim retryDelay = 500 ' milliseconds

        For attempt = 1 To maxRetries
            Try
                _lblStatus.Text = "READING, Please Wait..."
                _btnSort.Enabled = False
                _btnCancel.Enabled = False
                Application.DoEvents()

                If Not File.Exists(realnameFile) Then
                    DosMessageBox.Show(Me, "REALNAME.DAT not found.", "Error", MessageBoxButtons.OK)
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                    Return
                End If

                ' Read all customer entries with live progress
                Dim entries As New List(Of Tuple(Of String, String))
                Dim duplicates As New List(Of String)
                Dim progressText As New System.Text.StringBuilder()

                Using reader As New StreamReader(realnameFile)
                    Dim count = 0
                    While Not reader.EndOfStream
                        Dim filename = reader.ReadLine()?.Trim()
                        Dim realname = reader.ReadLine()?.Trim()

                        If Not String.IsNullOrEmpty(filename) AndAlso Not String.IsNullOrEmpty(realname) Then
                            count += 1
                            entries.Add(Tuple.Create(filename, realname))

                            ' Check for duplicates
                            If count > 1 AndAlso entries(count - 1).Item2 = entries(count - 2).Item2 Then
                                duplicates.Add(realname)
                            End If

                            ' Show live progress like DOS (update every 10 records to avoid flickering)
                            If count Mod 10 = 0 OrElse count <= 20 Then
                                progressText.Clear()
                                progressText.AppendLine("READING, Please Wait...")
                                progressText.AppendLine()

                                ' Show last 15 entries (like DOS scrolling display)
                                Dim startIdx = Math.Max(0, count - 15)
                                For i = startIdx To count - 1
                                    progressText.AppendLine($"{i + 1}  {entries(i).Item2}")
                                Next

                                _lblStatus.Text = progressText.ToString()
                                Application.DoEvents()
                            End If
                        End If
                    End While
                End Using

                _lblStatus.Text = $"Read {entries.Count} customer(s)." & Environment.NewLine & Environment.NewLine &
                                 "SORTING, Please Wait..."
                Application.DoEvents()

                ' Sort by realname (case-insensitive)
                entries = entries.OrderBy(Function(entry) entry.Item2, StringComparer.OrdinalIgnoreCase).ToList()

                ' Write backup with retry on file-in-use
                Using writer As New StreamWriter(backupFile, False)
                    For Each entry In entries
                        writer.WriteLine(entry.Item1)
                        writer.WriteLine(entry.Item2)
                    Next
                End Using

                ' Copy backup to main file
                File.Copy(backupFile, realnameFile, True)

                Dim resultMsg = $"All Done!" & Environment.NewLine & Environment.NewLine &
                               $"Sorted {entries.Count} customer(s) alphabetically."

                If duplicates.Count > 0 Then
                    resultMsg &= Environment.NewLine & Environment.NewLine &
                                $"Warning: Found {duplicates.Count} duplicate name(s):" & Environment.NewLine &
                                String.Join(Environment.NewLine, duplicates.Take(10))
                    If duplicates.Count > 10 Then
                        resultMsg &= Environment.NewLine & $"... and {duplicates.Count - 10} more"
                    End If
                End If

                DosMessageBox.Show(Me, resultMsg, "Sort Complete", MessageBoxButtons.OK)

                Me.DialogResult = DialogResult.OK
                Me.Close()
                Return ' Success - exit retry loop

            Catch ex As IOException When attempt < maxRetries
                ' File in use - retry after delay (DOS Error 70 handling)
                _lblStatus.Text = $"Please Wait, someone is using your file.... (Retry {attempt}/{maxRetries})"
                Application.DoEvents()
                System.Threading.Thread.Sleep(retryDelay)
                ' Loop will retry

            Catch ex As Exception
                ' Non-recoverable error
                DosMessageBox.Show(Me, "Error sorting names: " & ex.Message, "Error", MessageBoxButtons.OK)
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
                Return
            End Try
        Next

        ' If we exhausted retries
        DosMessageBox.Show(Me, "Unable to access file after multiple attempts. File may be in use by another program.", 
                          "Error", MessageBoxButtons.OK)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub OnCancel(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
