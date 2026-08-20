Option Strict Off
Option Explicit On

Imports System
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Threading

''' <summary>
''' DOS-parity ShopCard Find screen.
''' Scrolls filenames as it searches, pauses on each match to ask Search for more (Y/N)?
''' Mirrors DOS S.ASC lines 6560-6800.
''' </summary>
Public Class FormShopCardFind
    Inherits Form

    ' ── UI ──────────────────────────────────────────────────────────────
    Private _output As RichTextBox
    Private _inputPanel As Panel
    Private _prompt As Label
    Private _inputBox As TextBox
    Private _flashTimer As System.Windows.Forms.Timer
    Private _flashState As Boolean = False
    ' List of (start, length) match ranges to flash
    Private _matchRanges As New List(Of (start As Integer, length As Integer))

    ' ── State ───────────────────────────────────────────────────────────
    Private _searchTerm As String = ""
    Private _searchPhase As SearchPhase = SearchPhase.Searching
    Private _searchThread As Thread = Nothing
    Private _yesNoReply As Char = Nothing
    Private _yesNoReady As New System.Threading.ManualResetEventSlim(False)
    Private _closing As Boolean = False

    Private Enum SearchPhase
        Searching
        Done
    End Enum

    ' ── Constructor ─────────────────────────────────────────────────────
    Public Sub New(searchTerm As String)
        _searchTerm = searchTerm
        InitializeUi()
    End Sub

    ' ── Layout ──────────────────────────────────────────────────────────
    Private Sub InitializeUi()
        Me.Text = "SHOPCARD GENERATOR"
        Me.ClientSize = New Size(1024, 680)
        Me.BackColor = Color.Black
        Me.ForeColor = Color.Yellow
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.KeyPreview = True

        ' Scrolling output area
        _output = New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.Black,
            .ForeColor = Color.Yellow,
            .Font = New Font("Courier New", 11.0F, FontStyle.Regular, GraphicsUnit.Point),
            .ReadOnly = True,
            .BorderStyle = BorderStyle.None,
            .ScrollBars = RichTextBoxScrollBars.Vertical,
            .WordWrap = False,
            .TabStop = False
        }
        Me.Controls.Add(_output)

        ' Bottom strip — used for Y/N prompt and end-of-search message
        _inputPanel = New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 28,
            .BackColor = Color.Black,
            .Visible = False
        }
        _prompt = New Label() With {
            .AutoSize = True,
            .ForeColor = Color.Yellow,
            .BackColor = Color.Black,
            .Font = New Font("Courier New", 11.0F, FontStyle.Regular, GraphicsUnit.Point),
            .Location = New Point(4, 4)
        }
        _inputPanel.Controls.Add(_prompt)

        _inputBox = New TextBox() With {
            .BackColor = Color.Black,
            .ForeColor = Color.Yellow,
            .Font = New Font("Courier New", 11.0F, FontStyle.Regular, GraphicsUnit.Point),
            .BorderStyle = BorderStyle.None,
            .Location = New Point(4, 5),
            .Width = 20,
            .MaxLength = 1,
            .Visible = False
        }
        AddHandler _inputBox.KeyDown, AddressOf InputBoxKeyDown
        _inputPanel.Controls.Add(_inputBox)
        Me.Controls.Add(_inputPanel)

        AddHandler Me.Activated, AddressOf OnFormActivated

        ' Flash timer for match highlights
        _flashTimer = New System.Windows.Forms.Timer() With {.Interval = 500}
        AddHandler _flashTimer.Tick, AddressOf FlashTick
    End Sub

    ' ── Form shown ──────────────────────────────────────────────────────
    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        StartSearch()
    End Sub

    ' ── Input handler — Y/N only ─────────────────────────────────────────
    Private Sub InputBoxKeyDown(sender As Object, e As KeyEventArgs)
        If _searchPhase = SearchPhase.Searching Then
            If e.KeyCode = Keys.Y Then
                e.SuppressKeyPress = True
                AppendLine("Y")
                _yesNoReply = "Y"c
                _yesNoReady.Set()
            ElseIf e.KeyCode = Keys.N Then
                e.SuppressKeyPress = True
                AppendLine("N")
                _yesNoReply = "N"c
                _yesNoReady.Set()
            End If
        End If
    End Sub

    ' ── Search engine (runs on background thread) ────────────────────────
    Private Sub StartSearch()
        _searchThread = New Thread(AddressOf RunSearch) With {
            .IsBackground = True,
            .Name = "ShopCardFind"
        }
        _searchThread.Start()
    End Sub

    Private Sub RunSearch()
        Dim dataFolder = ShopCardSession.DataFolder   ' \\invoice\mainmenu\data
        Dim shopFolder = Path.Combine(dataFolder, "SHOPCARD")

        If Not Directory.Exists(shopFolder) Then
            AppendLineUI("Shopcard data folder not found: " & shopFolder)
            DoneUI()
            Return
        End If

        ' DOS buckets: 0, 1, 2, 3, 4 (each holds up to 400 cards)
        Dim bucketDirs = Directory.GetDirectories(shopFolder, "*", SearchOption.TopDirectoryOnly)
        Array.Sort(bucketDirs)   ' numeric sort by bucket number

        Dim keepSearching As Boolean = True

        For Each bucketDir In bucketDirs
            If Not keepSearching Then Exit For

            Dim files = Directory.GetFiles(bucketDir, "*.CRD", SearchOption.TopDirectoryOnly)
            ' Sort numerically by filename (card number)
            Array.Sort(files, Function(a, b)
                                  Dim na, nb As Integer
                                  If Integer.TryParse(Path.GetFileNameWithoutExtension(a), na) AndAlso
                                     Integer.TryParse(Path.GetFileNameWithoutExtension(b), nb) Then
                                      Return na.CompareTo(nb)
                                  End If
                                  Return String.Compare(a, b, StringComparison.OrdinalIgnoreCase)
                              End Function)

            For Each filePath In files
                If Not keepSearching Then Exit For

                ' Show "Searching C:\SHOPCARD\0\132.CRD" — use DOS-style backslash path
                Dim dosPath = "C:\SHOPCARD\" & Path.GetFileName(bucketDir) & "\" & Path.GetFileName(filePath)
                AppendLineUI("Searching " & dosPath)

                ' Search lines of this file
                Dim lines() As String
                Try
                    lines = File.ReadAllLines(filePath)
                Catch
                    Continue For
                End Try

                Dim lineIdx As Integer = 0
                Do While lineIdx < lines.Length
                    Dim lineNum = lineIdx + 1
                    Dim ln = lines(lineIdx)
                    If ln.IndexOf(_searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 Then
                        ' Found — print match block
                        AppendLineUI("")
                        AppendLineUI("Found at line " & lineNum)
                        AppendLineUI("")
                        ' Print context: up to 3 lines before, the match line, and up to ~10 lines after
                        Dim startCtx = Math.Max(0, lineIdx - 3)
                        Dim endCtx = Math.Min(lines.Length - 1, lineIdx + 10)
                        For ctxIdx = startCtx To endCtx
                            Dim prefix = If(ctxIdx = lineIdx, "...", "   ")
                            Dim ctxLine = prefix & lines(ctxIdx)
                            If ctxIdx = lineIdx Then
                                AppendMatchLine(ctxLine)
                            Else
                                AppendLineUI(ctxLine)
                            End If
                        Next
                        AppendLineUI("...")
                        AppendLineUI("")
                        ' Re-show current filename then ask
                        AppendLineUI("Searching " & dosPath)
                        AskYesNoUI("Search for more (Y/N) ? ")

                        Dim reply = WaitForYesNo()
                        If reply = "Y"c Then
                            ' Continue searching from next line in this file
                            lineIdx += 1
                            Continue Do
                        Else
                            ' N — stop showing matches but keep scanning remaining files
                            keepSearching = False
                            Exit Do
                        End If
                    End If
                    lineIdx += 1
                Loop
            Next
        Next

        AppendLineUI("")
        AppendLineUI("Search complete.")
        DoneUI()
    End Sub

    ' ── Thread-safe UI helpers ───────────────────────────────────────────
    Private Sub AppendLine(text As String)
        If _closing OrElse Me.IsDisposed Then Return
        If _output.InvokeRequired Then
            Try
                _output.Invoke(New Action(Of String)(AddressOf AppendLine), text)
            Catch ex As ObjectDisposedException
            End Try
            Return
        End If
        _output.AppendText(text & Environment.NewLine)
        _output.ScrollToCaret()
    End Sub

    Private Sub AppendLineUI(text As String)
        AppendLine(text)
    End Sub

    ''' <summary>Append a line and set up flash highlighting on all occurrences of the search term.</summary>
    Private Sub AppendMatchLine(text As String)
        If _closing OrElse Me.IsDisposed Then Return
        If _output.InvokeRequired Then
            Try
                _output.Invoke(New Action(Of String)(AddressOf AppendMatchLine), text)
            Catch ex As ObjectDisposedException
            End Try
            Return
        End If
        Dim startPos = _output.TextLength
        _output.AppendText(text & Environment.NewLine)
        _output.ScrollToCaret()
        ' Record match ranges and apply initial highlight (yellow text on black)
        Dim search = _searchTerm
        Dim offset = 0
        Do
            Dim idx = text.IndexOf(search, offset, StringComparison.OrdinalIgnoreCase)
            If idx < 0 Then Exit Do
            _matchRanges.Add((startPos + idx, search.Length))
            ' Apply initial flash state
            _output.Select(startPos + idx, search.Length)
            _output.SelectionBackColor = Color.Black
            _output.SelectionColor = Color.Yellow
            _output.Select(_output.TextLength, 0)
            _output.SelectionBackColor = Color.Black
            _output.SelectionColor = Color.Yellow
            offset = idx + search.Length
        Loop
        If Not _flashTimer.Enabled Then _flashTimer.Start()
    End Sub

    Private Sub FlashTick(sender As Object, e As EventArgs)
        _flashState = Not _flashState
        For Each r In _matchRanges
            _output.Select(r.start, r.length)
            If _flashState Then
                _output.SelectionBackColor = Color.Yellow
                _output.SelectionColor = Color.Black
            Else
                _output.SelectionBackColor = Color.Black
                _output.SelectionColor = Color.Yellow
            End If
        Next
        _output.Select(_output.TextLength, 0)
        _output.SelectionBackColor = Color.Black
        _output.SelectionColor = Color.Yellow
    End Sub

    Private Sub AskYesNoUI(promptText As String)
        If _closing OrElse Me.IsDisposed Then Return
        If Me.InvokeRequired Then
            Try
                Me.Invoke(New Action(Of String)(AddressOf AskYesNoUI), promptText)
            Catch ex As ObjectDisposedException
            End Try
            Return
        End If
        _output.AppendText(Environment.NewLine & promptText)   ' prompt on its own line
        _output.ScrollToCaret()
        _prompt.Text = promptText
        _prompt.Location = New Point(4, 5)
        _inputBox.Location = New Point(_prompt.PreferredWidth + 8, 5)
        _inputBox.Visible = True
        _inputPanel.Visible = True
        _inputBox.Focus()
        _yesNoReady.Reset()
    End Sub

    Private Sub OnFormActivated(sender As Object, e As EventArgs)
        ' When form regains focus while waiting for Y/N, redirect focus to the input box
        If _inputBox IsNot Nothing AndAlso _inputBox.Visible Then
            _inputBox.Focus()
        End If
    End Sub

    Private Function WaitForYesNo() As Char
        _yesNoReady.Wait()
        HideInputUI()
        Return _yesNoReply
    End Function

    Private Sub HideInputUI()
        If _closing OrElse Me.IsDisposed Then Return
        If Me.InvokeRequired Then
            Try
                Me.Invoke(New Action(AddressOf HideInputUI))
            Catch ex As ObjectDisposedException
            End Try
            Return
        End If
        _inputPanel.Visible = False
        _inputBox.Visible = False
    End Sub

    Private Sub DoneUI()
        If _closing OrElse Me.IsDisposed Then Return
        If Me.InvokeRequired Then
            Try
                Me.Invoke(New Action(AddressOf DoneUI))
            Catch ex As ObjectDisposedException
            End Try
            Return
        End If
        _flashTimer.Stop()
        _searchPhase = SearchPhase.Done
        _inputBox.Visible = False
        _prompt.Text = "Press any key to return to menu..."
        _prompt.Location = New Point(4, 5)
        _inputPanel.Visible = True
        Me.KeyPreview = True
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If _searchPhase = SearchPhase.Done Then
            Me.DialogResult = DialogResult.OK
            Me.Close()
            Return
        End If

        ' Handle Y/N at form level so focus loss never breaks input
        If _inputBox IsNot Nothing AndAlso _inputBox.Visible Then
            If e.KeyCode = Keys.Y Then
                e.SuppressKeyPress = True
                e.Handled = True
                _yesNoReply = "Y"c
                _yesNoReady.Set()
                Return
            ElseIf e.KeyCode = Keys.N Then
                e.SuppressKeyPress = True
                e.Handled = True
                _yesNoReply = "N"c
                _yesNoReady.Set()
                Return
            End If
        End If

        MyBase.OnKeyDown(e)
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        _closing = True
        _flashTimer.Stop()
        _flashTimer.Dispose()
        _yesNoReply = "N"c
        _yesNoReady.Set()
        MyBase.OnFormClosing(e)
    End Sub

End Class
