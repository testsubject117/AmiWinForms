Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' View all Standard Procedures from PROCDURE.DAT
''' DOS source: PLIST.ASC lines 3700-3750
''' Paged viewer displaying standard procedure names in two columns
''' [ENTER = Next Page]? prompt for navigation, [Q = Quit] on last page
''' </summary>
Public Class FormStandardProcedures
    Inherits Form

    Private _procedures As List(Of String)
    Private _currentPage As Integer = 0
    Private _itemsPerPage As Integer = 60  ' ~30 rows x 2 columns (more than DOS to use available space)
    Private _lblContent As Label
    Private _lblPrompt As Label

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Standard Procedures"
        Me.Size = New Size(900, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.Black
        Me.KeyPreview = True
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False

        ' Content label (no scrolling)
        _lblContent = New Label()
        _lblContent.AutoSize = False
        _lblContent.Size = New Size(860, 580)
        _lblContent.Location = New Point(20, 20)
        _lblContent.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _lblContent.ForeColor = Color.FromArgb(192, 192, 192)
        _lblContent.BackColor = Color.Black
        Me.Controls.Add(_lblContent)

        ' Prompt label at bottom (moved up for visibility)
        _lblPrompt = New Label()
        _lblPrompt.AutoSize = True
        _lblPrompt.Location = New Point(20, 610)
        _lblPrompt.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _lblPrompt.ForeColor = Color.FromArgb(192, 192, 192)
        _lblPrompt.BackColor = Color.Black
        Me.Controls.Add(_lblPrompt)

        ' Load and display first page
        LoadProcedures()
        DisplayCurrentPage()

        AddHandler Me.KeyDown, AddressOf OnKeyDown
    End Sub

    Private Sub LoadProcedures()
        _procedures = New List(Of String)()

        Try
            Dim procedureFile = Path.Combine(LegacyDataPaths.BaseDataDir, "PROCDURE.DAT")
            If File.Exists(procedureFile) Then
                Using reader As New StreamReader(procedureFile, Encoding.Default, detectEncodingFromByteOrderMarks:=False)
                    While Not reader.EndOfStream
                        Dim line = reader.ReadLine()
                        If line IsNot Nothing Then
                            line = line.Trim()
                            ' Filter out control characters, BOM, EOF markers
                            Dim cleanLine = New StringBuilder()
                            For Each c As Char In line
                                If Char.IsLetter(c) OrElse Char.IsDigit(c) OrElse " /-&()%".Contains(c) Then
                                    cleanLine.Append(c)
                                End If
                            Next
                            line = cleanLine.ToString().Trim()

                            ' Remove quotes if present
                            If line.StartsWith("""") AndAlso line.EndsWith("""") Then
                                line = line.Substring(1, line.Length - 2)
                            End If

                            ' Only add non-empty lines
                            If Not String.IsNullOrWhiteSpace(line) AndAlso line.Length > 0 Then
                                _procedures.Add(line)
                            End If
                        End If
                    End While
                End Using
            End If
        Catch ex As Exception
            Debug.WriteLine("Error loading PROCDURE.DAT: " & ex.Message)
        End Try

        ' Sort alphabetically (DOS does this)
        _procedures.Sort()
    End Sub

    Private Sub DisplayCurrentPage()
        Dim sb As New StringBuilder()

        ' Header (only on first page)
        If _currentPage = 0 Then
            sb.AppendLine("STANDARD PROCEDURES")
            sb.AppendLine()
        End If

        Dim col1Width = 35
        Dim startIndex = _currentPage * _itemsPerPage
        Dim endIndex = Math.Min(startIndex + _itemsPerPage, _procedures.Count)

        ' Display in two columns
        For i As Integer = startIndex To endIndex - 1 Step 2
            ' Pad FIRST, then escape ampersands (so padding calculation is correct)
            Dim col1Text = _procedures(i).PadRight(col1Width).Replace("&", "&&")
            Dim line = col1Text
            If i + 1 < endIndex Then
                Dim col2Text = _procedures(i + 1).Replace("&", "&&")
                line &= col2Text
            End If
            sb.AppendLine(line)
        Next

        _lblContent.Text = sb.ToString()

        ' Update prompt based on whether there are more pages
        Dim totalPages = Math.Ceiling(_procedures.Count / CDbl(_itemsPerPage))
        If _currentPage < totalPages - 1 Then
            _lblPrompt.Text = "[ENTER = Next Page]? _"
        Else
            _lblPrompt.Text = "[Q = Quit]"
        End If
    End Sub

    Private Sub OnKeyDown(sender As Object, e As KeyEventArgs)
        Dim totalPages = Math.Ceiling(_procedures.Count / CDbl(_itemsPerPage))

        If e.KeyCode = Keys.Enter Then
            ' Advance to next page if available
            If _currentPage < totalPages - 1 Then
                _currentPage += 1
                DisplayCurrentPage()
                e.Handled = True
            End If
        ElseIf e.KeyCode = Keys.Q OrElse e.KeyCode = Keys.Escape Then
            ' Q or ESC closes
            Me.Close()
            e.Handled = True
        End If
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Me.Focus()
    End Sub
End Class

