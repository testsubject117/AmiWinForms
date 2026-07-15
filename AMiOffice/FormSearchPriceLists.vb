Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' Option S - Scan all price lists for Anything Else
''' DOS source: PLIST.ASC line 469 - uses TS.COM text search
''' Mimics DOS TS behavior: shows matches one at a time, prompts to continue
''' </summary>
Public Class FormSearchPriceLists
    Inherits Form

    Private _txtSearchTerm As TextBox
    Private _lblPrompt As Label
    Private _btnSearch As Button
    Private _btnClose As Button
    Private _pnlResults As Panel
    Private _txtCurrentMatch As TextBox
    Private _lblSearchStatus As Label
    Private _btnContinue As Button
    Private _btnStop As Button

    Private _allFiles As String()
    Private _currentFileIndex As Integer
    Private _currentSearchTerm As String
    Private _totalMatches As Integer
    Private _isSearching As Boolean

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Search All Price Lists"
        Me.Size = New Size(900, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.Black
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False

        ' Top section - search input
        _lblPrompt = New Label()
        _lblPrompt.Text = "Enter a part number or something else to look for:"
        _lblPrompt.Location = New Point(20, 20)
        _lblPrompt.Size = New Size(850, 25)
        _lblPrompt.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _lblPrompt.ForeColor = Color.White
        _lblPrompt.BackColor = Color.Black
        Me.Controls.Add(_lblPrompt)

        _txtSearchTerm = New TextBox()
        _txtSearchTerm.Location = New Point(20, 50)
        _txtSearchTerm.Size = New Size(700, 30)
        _txtSearchTerm.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _txtSearchTerm.BackColor = Color.White
        _txtSearchTerm.ForeColor = Color.Black
        _txtSearchTerm.CharacterCasing = CharacterCasing.Upper
        Me.Controls.Add(_txtSearchTerm)

        _btnSearch = New Button()
        _btnSearch.Text = "Search"
        _btnSearch.Location = New Point(740, 48)
        _btnSearch.Size = New Size(130, 34)
        _btnSearch.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _btnSearch.BackColor = Color.LightGray
        _btnSearch.ForeColor = Color.Black
        _btnSearch.FlatStyle = FlatStyle.Flat
        AddHandler _btnSearch.Click, AddressOf OnStartSearch
        Me.Controls.Add(_btnSearch)

        ' Results panel (hidden until search starts)
        _pnlResults = New Panel()
        _pnlResults.Location = New Point(20, 100)
        _pnlResults.Size = New Size(850, 490)
        _pnlResults.BackColor = Color.Black
        _pnlResults.Visible = False
        Me.Controls.Add(_pnlResults)

        ' Search status label
        _lblSearchStatus = New Label()
        _lblSearchStatus.Location = New Point(0, 10)
        _lblSearchStatus.Size = New Size(850, 25)
        _lblSearchStatus.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        _lblSearchStatus.ForeColor = Color.Yellow
        _lblSearchStatus.BackColor = Color.Black
        _lblSearchStatus.Text = "Searching..."
        _pnlResults.Controls.Add(_lblSearchStatus)

        ' Current match display
        _txtCurrentMatch = New TextBox()
        _txtCurrentMatch.Location = New Point(0, 50)
        _txtCurrentMatch.Size = New Size(850, 350)
        _txtCurrentMatch.Font = New Font("Consolas", 9.0F, FontStyle.Regular)
        _txtCurrentMatch.BackColor = Color.Black
        _txtCurrentMatch.ForeColor = Color.Yellow
        _txtCurrentMatch.Multiline = True
        _txtCurrentMatch.ScrollBars = ScrollBars.Both
        _txtCurrentMatch.WordWrap = False
        _txtCurrentMatch.ReadOnly = True
        _pnlResults.Controls.Add(_txtCurrentMatch)

        ' Continue button
        _btnContinue = New Button()
        _btnContinue.Text = "Continue (Y)"
        _btnContinue.Location = New Point(500, 420)
        _btnContinue.Size = New Size(160, 50)
        _btnContinue.Font = New Font("Consolas", 11.0F, FontStyle.Bold)
        _btnContinue.BackColor = Color.LightGray
        _btnContinue.ForeColor = Color.Black
        _btnContinue.FlatStyle = FlatStyle.Flat
        AddHandler _btnContinue.Click, AddressOf OnContinueSearch
        _pnlResults.Controls.Add(_btnContinue)

        ' Stop button
        _btnStop = New Button()
        _btnStop.Text = "Stop && Close (N)"
        _btnStop.Location = New Point(680, 420)
        _btnStop.Size = New Size(160, 50)
        _btnStop.Font = New Font("Consolas", 11.0F, FontStyle.Bold)
        _btnStop.BackColor = Color.LightGray
        _btnStop.ForeColor = Color.Black
        _btnStop.FlatStyle = FlatStyle.Flat
        AddHandler _btnStop.Click, AddressOf OnStopSearch
        _pnlResults.Controls.Add(_btnStop)

        Me.AcceptButton = _btnSearch
    End Sub

    Private Sub OnStartSearch(sender As Object, e As EventArgs)
        Dim searchTerm = _txtSearchTerm.Text.Trim()

        If String.IsNullOrEmpty(searchTerm) Then
            DosMessageBox.Show(Me, "Please enter a search term.", "Error", MessageBoxButtons.OK)
            _txtSearchTerm.Focus()
            Return
        End If

        Try
            Dim prcDir = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC")
            If Not Directory.Exists(prcDir) Then
                DosMessageBox.Show(Me, "PRC directory not found.", "Error", MessageBoxButtons.OK)
                Return
            End If

            _allFiles = Directory.GetFiles(prcDir, "*.PRC")
            _currentSearchTerm = searchTerm
            _currentFileIndex = 0
            _totalMatches = 0
            _isSearching = True

            ' Hide search input, show results panel
            _txtSearchTerm.Enabled = False
            _btnSearch.Enabled = False
            _pnlResults.Visible = True

            ' Start searching
            FindNextMatch()

        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error starting search:{Environment.NewLine}{ex.Message}",
                              "Error", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub FindNextMatch()
        If Not _isSearching Then Return

        _btnContinue.Enabled = False
        _btnStop.Enabled = False
        Application.DoEvents()

        Try
            While _currentFileIndex < _allFiles.Length
                Dim file = _allFiles(_currentFileIndex)
                Dim customerName = Path.GetFileNameWithoutExtension(file)

                _lblSearchStatus.Text = $"Searching {customerName}.PRC..."
                Application.DoEvents()

                Try
                    Using reader As New StreamReader(file)
                        Dim lineNum = 0
                        While Not reader.EndOfStream
                            lineNum += 1
                            Dim line = reader.ReadLine()
                            If String.IsNullOrWhiteSpace(line) Then Continue While

                            ' Check if line contains search term (case-insensitive)
                            If line.IndexOf(_currentSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0 Then
                                ' Found a match! Display it and pause
                                _totalMatches += 1
                                ShowMatch(file, customerName, lineNum, line)
                                _currentFileIndex += 1 ' Move to next file after this match
                                Return ' Wait for user to continue or stop
                            End If
                        End While
                    End Using
                Catch ex As Exception
                    ' Skip files that can't be read
                End Try

                _currentFileIndex += 1
            End While

            ' Search complete
            SearchComplete()

        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error during search:{Environment.NewLine}{ex.Message}",
                              "Error", MessageBoxButtons.OK)
            ResetSearch()
        End Try
    End Sub

    Private Sub ShowMatch(filePath As String, customerName As String, lineNum As Integer, matchLine As String)
        Dim display As New StringBuilder()

        display.AppendLine($"Found at line {lineNum}")
        display.AppendLine()
        display.AppendLine("...")
        display.AppendLine(matchLine)
        display.AppendLine()
        display.AppendLine($"Searching {filePath}")
        display.AppendLine($"Search for more (Y/N) ?")

        _txtCurrentMatch.Text = display.ToString()
        _lblSearchStatus.Text = $"Found {_totalMatches} match(es)"

        _btnContinue.Enabled = True
        _btnStop.Enabled = True
        _btnContinue.Focus()
    End Sub

    Private Sub OnContinueSearch(sender As Object, e As EventArgs)
        ' Continue searching from next file
        FindNextMatch()
    End Sub

    Private Sub OnStopSearch(sender As Object, e As EventArgs)
        _isSearching = False
        Me.Close()
    End Sub

    Private Sub SearchComplete()
        _isSearching = False
        _btnContinue.Enabled = False
        _btnStop.Text = "Close (N)"
        _btnStop.Enabled = True

        If _totalMatches = 0 Then
            _txtCurrentMatch.Text = $"No matches found for: {_currentSearchTerm}"
            _lblSearchStatus.Text = "Search complete: 0 matches"
        Else
            _lblSearchStatus.Text = $"Search complete: {_totalMatches} match(es) found"
        End If
    End Sub

    Private Sub ResetSearch()
        _isSearching = False
        _pnlResults.Visible = False
        _txtSearchTerm.Enabled = True
        _btnSearch.Enabled = True
        _txtSearchTerm.Text = ""
        _txtSearchTerm.Focus()
    End Sub

    Private Sub OnClose(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Protected Overrides Function ProcessDialogKey(keyData As Keys) As Boolean
        If _pnlResults.Visible AndAlso _btnContinue.Enabled Then
            If keyData = Keys.Y Then
                OnContinueSearch(Nothing, Nothing)
                Return True
            ElseIf keyData = Keys.N Then
                OnStopSearch(Nothing, Nothing)
                Return True
            End If
        End If
        Return MyBase.ProcessDialogKey(keyData)
    End Function
End Class
