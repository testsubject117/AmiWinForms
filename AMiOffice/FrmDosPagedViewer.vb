Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Diagnostics

Public Class FrmDosPagedViewer
    Inherits Form

    Private ReadOnly _txt As TextBox
    Private ReadOnly _lblHint As Label
    Private ReadOnly _pnlBottom As Panel
    Private ReadOnly _btnPrint As Button

    ' DOS-style cyan header/footer bars
    Private ReadOnly _pnlTopBar As Panel
    Private ReadOnly _pnlBottomBar As Panel
    Private _searchMode As Boolean = False
    Private _searchCommand As String = ""
    Private _searchFileName As String = ""
    Private _searchResultsFound As Boolean = False
    Private _flashTimer As Timer
    Private _flashVisible As Boolean = True

    Private _pages As List(Of String) = New List(Of String)()
    Private _pageIndex As Integer = 0

    ' NEW: status/progress mode (DOS-like "PLEASE WAIT...")
    Private _statusMode As Boolean = False

    ' Set True temporarily if you need to catch who is opening this viewer unexpectedly.
    Private Const DebugBreakOnOpen As Boolean = False

    Public Sub New()
        InitializeComponent()
        If DebugBreakOnOpen AndAlso Debugger.IsAttached Then
            Debugger.Break()
        End If

        Me.Text = "Viewer"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.KeyPreview = True
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
        Me.Size = New Size(900, 650)

        ' DOS-style cyan top bar (hidden by default, shown in search mode)
        _pnlTopBar = New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 25,
            .BackColor = Color.Cyan,
            .Visible = False
        }
        AddHandler _pnlTopBar.Paint, AddressOf OnPaintTopBar

        ' DOS-style cyan bottom bar (hidden by default, shown in search mode)
        _pnlBottomBar = New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 25,
            .BackColor = Color.Cyan,
            .Visible = False
        }
        AddHandler _pnlBottomBar.Paint, AddressOf OnPaintBottomBar

        ' Flash timer for "*** text not found ***"
        _flashTimer = New Timer() With {
            .Interval = 500,
            .Enabled = False
        }
        AddHandler _flashTimer.Tick, Sub()
                                         _flashVisible = Not _flashVisible
                                         _pnlBottomBar.Invalidate()
                                     End Sub

        _txt = New TextBox() With {
            .Multiline = True,
            .ReadOnly = True,
            .TabStop = False,
            .ScrollBars = ScrollBars.None,
            .BorderStyle = BorderStyle.None,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Dock = DockStyle.Fill,
            .Font = Me.Font,
            .ShortcutsEnabled = False,
            .WordWrap = False
        }

        ' Prevent the textbox from ever receiving focus (keeps keystrokes on the Form handler).
        AddHandler _txt.GotFocus, Sub()
                                      Me.Select()
                                  End Sub

        _pnlBottom = New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 40,
            .BackColor = Color.FromArgb(20, 20, 20)
        }

        _lblHint = New Label() With {
            .AutoSize = False,
            .Height = 40,
            .BackColor = Color.Transparent,
            .ForeColor = Color.White,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = Me.Font,
            .Text = "[ENTER = More]   [ESC = Quit]",
            .Padding = New Padding(10, 0, 0, 0),
            .Dock = DockStyle.Fill
        }

        _btnPrint = New Button() With {
            .Text = "Print",
            .Width = 100,
            .Height = 32,
            .Dock = DockStyle.Right,
            .BackColor = Color.Silver,
            .ForeColor = Color.Black,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
            .Margin = New Padding(5),
            .TabStop = False
        }
        _btnPrint.FlatAppearance.BorderColor = Color.Gainsboro
        _btnPrint.FlatAppearance.BorderSize = 1
        _btnPrint.FlatAppearance.MouseOverBackColor = Color.Gainsboro
        _btnPrint.FlatAppearance.MouseDownBackColor = Color.DarkGray

        AddHandler _btnPrint.Click, AddressOf OnPrintClick

        ' Explicitly ensure this form has NO accept button (Enter should page, not print)
        Me.AcceptButton = Nothing

        _pnlBottom.Controls.Add(_lblHint)
        _pnlBottom.Controls.Add(_btnPrint)

        Me.Controls.Add(_txt)
        Me.Controls.Add(_pnlTopBar)       ' Top cyan bar
        Me.Controls.Add(_pnlBottomBar)    ' Bottom cyan bar
        Me.Controls.Add(_pnlBottom)

        AddHandler Me.KeyDown, AddressOf OnViewerKeyDown
        AddHandler Me.Shown, Sub()
                                 ' Ensure the form (not the TextBox) owns keyboard input.
                                 Me.Select()
                             End Sub
    End Sub

    Public Sub SetPages(pages As IEnumerable(Of String))
        _statusMode = False
        _lblHint.Text = "[ENTER = More]   [ESC = Quit]"

        _pages = New List(Of String)(If(pages, Array.Empty(Of String)()))
        _pageIndex = 0
        Render()
    End Sub

    Public Sub SetSinglePage(text As String)
        SetPages(New String() {If(text, "")})
    End Sub

    ' -----------------------------
    ' NEW: DOS-like status/progress mode
    ' -----------------------------
    Public Sub BeginStatusMode(Optional hintText As String = "[ESC = Quit]")
        RunOnUiThread(
            Sub()
                _statusMode = True
                _pages = New List(Of String)(New String() {""})
                _pageIndex = 0
                _lblHint.Text = hintText
                Render()
            End Sub)
    End Sub

    Public Sub UpdateStatus(text As String)
        RunOnUiThread(
            Sub()
                If Not _statusMode Then
                    ' If someone updates status without explicitly beginning,
                    ' switch into status mode automatically.
                    BeginStatusMode()
                End If

                If _pages Is Nothing OrElse _pages.Count = 0 Then
                    _pages = New List(Of String)(New String() {If(text, "")})
                    _pageIndex = 0
                Else
                    _pages(0) = If(text, "")
                    _pageIndex = 0
                End If

                Render()
                ' Keep the UI responsive and visibly updating while work is happening.
                Application.DoEvents()
            End Sub)
    End Sub

    Public Sub EndStatusMode()
        RunOnUiThread(
            Sub()
                _statusMode = False
                _lblHint.Text = "[ENTER = More]   [ESC = Quit]"
            End Sub)
    End Sub

    ' -----------------------------
    ' NEW: Red screen search mode (DOS LIST.COM parity)
    ' -----------------------------
    Public Sub EnableSearchMode(Optional searchCommand As String = "", Optional fileName As String = "")
        RunOnUiThread(
            Sub()
                _searchMode = True
                _searchCommand = searchCommand
                _searchFileName = fileName

                ' Red background like DOS LIST.COM /F search mode
                _txt.BackColor = Color.DarkRed
                _txt.ForeColor = Color.FromArgb(255, 255, 100)  ' Greenish-yellow like DOS
                Me.BackColor = Color.DarkRed
                _pnlBottom.BackColor = Color.FromArgb(100, 0, 0)  ' Darker red
                _lblHint.Text = "[ENTER = More]   [F3 = Next Match]   [ESC = Quit]"

                ' Show cyan DOS-style bars
                _pnlTopBar.Visible = True
                _pnlBottomBar.Visible = True
            End Sub)
    End Sub

    Public Sub SetSearchResultNotFound()
        RunOnUiThread(
            Sub()
                ' Start flashing the "*** text not found ***" message
                _searchResultsFound = False
                _flashTimer.Enabled = True
                _pnlBottomBar.Invalidate()
            End Sub)
    End Sub

    Public Sub SetSearchResultFound()
        RunOnUiThread(
            Sub()
                ' Stop flashing, show normal status
                _searchResultsFound = True
                _flashTimer.Enabled = False
                _flashVisible = True
                _pnlBottomBar.Invalidate()
            End Sub)
    End Sub

    Private Sub RunOnUiThread(action As Action)
        If action Is Nothing Then Return

        If Me.IsDisposed Then Return

        If Me.InvokeRequired Then
            Try
                Me.BeginInvoke(action)
            Catch
                ' Ignore if closing/disposed mid-invoke
            End Try
        Else
            action()
        End If
    End Sub

    ' -----------------------------
    ' Input handling
    ' -----------------------------
    Private Sub OnViewerKeyDown(sender As Object, e As KeyEventArgs)
        ' IMPORTANT:
        ' Swallow digits and common typing keys so they can't trigger any global "type-to-search"
        ' handler elsewhere while this viewer is open.
        If (e.KeyCode >= Keys.D0 AndAlso e.KeyCode <= Keys.D9) OrElse
           (e.KeyCode >= Keys.NumPad0 AndAlso e.KeyCode <= Keys.NumPad9) Then

            e.Handled = True
            Return
        End If

        If e.KeyCode = Keys.Escape Then
            Me.Close()
            e.Handled = True
            Return
        End If

        ' In status mode, ignore ENTER so it feels like a locked "PLEASE WAIT" screen.
        If _statusMode Then
            If e.KeyCode = Keys.Enter Then
                e.Handled = True
                Return
            End If
        End If

        If e.KeyCode = Keys.Enter Then
            If _pageIndex < _pages.Count - 1 Then
                _pageIndex += 1
                Render()
            Else
                ' Stay on last page until ESC (DOS-like).
            End If
            e.Handled = True
            Return
        End If

        ' F3 = Find Next (search mode navigation, same as Enter)
        If e.KeyCode = Keys.F3 Then
            If _pageIndex < _pages.Count - 1 Then
                _pageIndex += 1
                Render()
            End If
            e.Handled = True
            Return
        End If
    End Sub

    Private Sub Render()
        If _pages Is Nothing OrElse _pages.Count = 0 Then
            _txt.Text = ""
            Return
        End If

        _txt.Text = _pages(_pageIndex)
        _txt.SelectionStart = 0
        _txt.SelectionLength = 0
    End Sub

    Private Sub OnPrintClick(sender As Object, e As EventArgs)
        If _pages Is Nothing OrElse _pages.Count = 0 Then
            MessageBox.Show("Nothing to print.", "Print", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Combine all pages into a single document for printing
        Dim allText As New System.Text.StringBuilder()
        For Each page In _pages
            allText.AppendLine(page)
            allText.AppendLine() ' Extra line between pages
        Next

        Dim printDoc As New System.Drawing.Printing.PrintDocument()
        Dim textToPrint As String = allText.ToString()

        AddHandler printDoc.PrintPage,
            Sub(s As Object, pea As System.Drawing.Printing.PrintPageEventArgs)
                Dim printFont As New Font("Courier New", 10)
                Dim linesPerPage As Integer = 0
                Dim yPos As Single = pea.MarginBounds.Top
                Dim count As Integer = 0
                Dim leftMargin As Single = pea.MarginBounds.Left
                Dim lineHeight As Single = printFont.GetHeight(pea.Graphics)

                Dim lines() As String = textToPrint.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.None)
                linesPerPage = CInt(pea.MarginBounds.Height / lineHeight)

                For Each line In lines
                    If count >= linesPerPage Then
                        pea.HasMorePages = True
                        Exit For
                    End If

                    pea.Graphics.DrawString(line, printFont, Brushes.Black, leftMargin, yPos, New StringFormat())
                    count += 1
                    yPos += lineHeight
                Next

                If count < lines.Length Then
                    ' Remove printed lines for next page
                    Dim remainingLines As New List(Of String)()
                    For i As Integer = count To lines.Length - 1
                        remainingLines.Add(lines(i))
                    Next
                    textToPrint = String.Join(vbCrLf, remainingLines)
                Else
                    pea.HasMorePages = False
                End If
            End Sub

        ' Show print dialog
        Dim printDlg As New PrintDialog() With {
            .Document = printDoc
        }

        If printDlg.ShowDialog() = DialogResult.OK Then
            Try
                printDoc.Print()
            Catch ex As Exception
                MessageBox.Show($"Print failed:{vbCrLf}{ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub

    ' -----------------------------
    ' DOS-style cyan bar painting
    ' -----------------------------
    Private Sub OnPaintTopBar(sender As Object, e As PaintEventArgs)
        If Not _searchMode Then Return

        ' DOS cyan bar: "04-01-<6 14:00 ? LOGBOOK.26" (centered)
        Dim g As Graphics = e.Graphics
        g.Clear(Color.Cyan)

        Dim dateTimeStr As String = DateTime.Now.ToString("MM-dd-<yy HH:mm")
        Dim fileNameStr As String = If(String.IsNullOrEmpty(_searchFileName), "LOGBOOK.??", _searchFileName)
        Dim fullText As String = $"{dateTimeStr} ? {fileNameStr}"

        Using brush As New SolidBrush(Color.Black)
            Using font As New Font("Consolas", 10.0F, FontStyle.Bold)
                Dim textSize As SizeF = g.MeasureString(fullText, font)
                Dim x As Single = (_pnlTopBar.Width - textSize.Width) / 2.0F
                g.DrawString(fullText, font, brush, New PointF(x, 4))
            End Using
        End Using
    End Sub

    Private Sub OnPaintBottomBar(sender As Object, e As PaintEventArgs)
        If Not _searchMode Then Return

        ' DOS cyan bar: "Command:" on left, "*** text not found ***" centered, "ESC=exit" on right
        Dim g As Graphics = e.Graphics
        g.Clear(Color.Cyan)

        Using brush As New SolidBrush(Color.Black)
            Using font As New Font("Consolas", 10.0F, FontStyle.Bold)
                Dim leftText As String = $"Command: {_searchCommand}"
                Dim centerText As String = "*** text not found ***"
                Dim rightText As String = "ESC=exit"

                ' Left: ~1 inch from left edge (96 DPI = ~96 pixels per inch)
                Dim leftMargin As Single = 80.0F
                g.DrawString(leftText, font, brush, New PointF(leftMargin, 4))

                ' Center: centered horizontally (only when not found and flashing)
                If Not _searchResultsFound AndAlso _flashVisible Then
                    Dim centerSize As SizeF = g.MeasureString(centerText, font)
                    Dim centerX As Single = (_pnlBottomBar.Width - centerSize.Width) / 2.0F
                    g.DrawString(centerText, font, brush, New PointF(centerX, 4))
                End If

                ' Right: ~1 inch from right edge
                Dim rightSize As SizeF = g.MeasureString(rightText, font)
                Dim rightX As Single = _pnlBottomBar.Width - rightSize.Width - 80.0F
                g.DrawString(rightText, font, brush, New PointF(rightX, 4))
            End Using
        End Using
    End Sub
End Class
