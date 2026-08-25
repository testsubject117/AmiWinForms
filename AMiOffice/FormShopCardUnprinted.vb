Option Strict Off
Option Explicit On

Imports System
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

''' <summary>
''' DOS-parity ShopCard "View Unprinted" screen.
''' Mirrors CARDSCAN.BAS two-phase flow:
'''   Phase 1: Scan + sort, then prompt [Enter] Print &amp; View / [V]iew only / [Q]uit
'''   Phase 2: Display the sorted list (V or Enter); Enter also sends to printer (NYI)
''' Archive bit SET = unprinted (not yet voided). Age filter = older than 1 day.
''' Enumerates bucket subdirectories one at a time to avoid a single blocking
''' GetFiles AllDirectories call over a slow network share.
''' </summary>
Public Class FormShopCardUnprinted
    Inherits Form

    ' -- UI -------------------------------------------------------------------
    Private _output As RichTextBox

    ' -- State ----------------------------------------------------------------
    Private _phase As ScanPhase = ScanPhase.Scanning
    Private _results As List(Of UnprintedCard)
    Private _cts As CancellationTokenSource

    Private Enum ScanPhase
        Scanning
        AwaitingChoice   ' "Scan complete. [Enter] Print & View  [V]iew only  [Q]uit"
        Viewing          ' list is displayed; Q/Enter/Esc exits
    End Enum

    ' -- Constructor ----------------------------------------------------------
    Public Sub New()
        InitializeUi()
    End Sub

    ' -- Layout ---------------------------------------------------------------
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

        _output = New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.Black,
            .ForeColor = Color.Yellow,
            .Font = New Font("Courier New", 10, FontStyle.Regular),
            .ReadOnly = True,
            .ScrollBars = RichTextBoxScrollBars.Vertical,
            .BorderStyle = BorderStyle.None,
            .WordWrap = False
        }

        Me.Controls.Add(_output)
    End Sub

    ' -- Shown ----------------------------------------------------------------
    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        RunScanAsync()
    End Sub

    ' -- FormClosed -----------------------------------------------------------
    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        If _cts IsNot Nothing Then _cts.Cancel()
        MyBase.OnFormClosed(e)
    End Sub

    ' -- Keyboard -------------------------------------------------------------
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        Select Case _phase
            Case ScanPhase.Scanning
                If e.KeyCode = Keys.Q OrElse e.KeyCode = Keys.Escape Then
                    If _cts IsNot Nothing Then _cts.Cancel()
                End If
                e.Handled = True

            Case ScanPhase.AwaitingChoice
                Select Case e.KeyCode
                    Case Keys.Q, Keys.Escape
                        Me.DialogResult = DialogResult.OK
                        Me.Close()
                    Case Keys.V
                        ShowList(printFirst:=False)
                    Case Keys.Enter
                        ' DOS: [Enter] = Print & View -- printing NYI, falls through to view
                        ShowList(printFirst:=True)
                End Select
                e.Handled = True

            Case ScanPhase.Viewing
                Select Case e.KeyCode
                    Case Keys.Q, Keys.Enter, Keys.Escape
                        Me.DialogResult = DialogResult.OK
                        Me.Close()
                End Select
                e.Handled = True
        End Select
        MyBase.OnKeyDown(e)
    End Sub

    ' -- Phase 1: Scan (async so UI stays responsive) ------------------------
    Private Async Sub RunScanAsync()
        _phase = ScanPhase.Scanning
        _cts = New CancellationTokenSource()
        Dim token As CancellationToken = _cts.Token

        Dim shopcardRoot As String = Path.Combine(ShopCardSession.DataFolder, "SHOPCARD")
        Dim cutoff As DateTime = DateTime.Now.AddDays(-1)

        AppendLine("Scanning for shopcards that are older than 1 day,", Color.Yellow)
        AppendLine("and have not been printed as invoices.", Color.Yellow)
        AppendLine("Please get printer ready to print.", Color.Yellow)
        AppendLine("", Color.Yellow)
        AppendLine("Scanning & sorting shopcards, please wait...  [Q] to cancel", Color.Cyan)

        Dim found As List(Of UnprintedCard) = Await Task.Run(Function()
            Dim list As New List(Of UnprintedCard)
            If Not Directory.Exists(shopcardRoot) Then Return list

            ' Enumerate bucket subdirectories one at a time so we don't issue
            ' a single blocking GetFiles AllDirectories call over the network.
            Dim buckets As String()
            Try
                buckets = Directory.GetDirectories(shopcardRoot)
            Catch
                Return list
            End Try

            For Each bucket As String In buckets
                If token.IsCancellationRequested Then Exit For
                Try
                    For Each f As String In Directory.GetFiles(bucket, "*.CRD")
                        If token.IsCancellationRequested Then Exit For
                        Try
                            Dim attrs As FileAttributes = File.GetAttributes(f)
                            If (attrs And FileAttributes.Archive) = FileAttributes.Archive Then
                                Dim lastWrite As DateTime = File.GetLastWriteTime(f)
                                If lastWrite <= cutoff Then
                                    list.Add(ReadCardHeader(f, lastWrite))
                                End If
                            End If
                        Catch
                        End Try
                    Next
                Catch
                End Try

                ' Report bucket progress back to UI thread
                Dim bucketName As String = Path.GetFileName(bucket)
                Me.BeginInvoke(Sub() AppendLine("  Scanned bucket " & bucketName & " ...", Color.DarkCyan))
            Next

            If token.IsCancellationRequested Then Return list

            Return list.OrderBy(Function(c) c.EntryDate).ThenBy(Function(c) c.CardNumber).ToList()
        End Function)

        ' Back on UI thread
        If Me.IsDisposed OrElse Not Me.IsHandleCreated Then Return

        If token.IsCancellationRequested Then
            AppendLine("", Color.Yellow)
            AppendLine("  Scan cancelled.", Color.Red)
            AppendLine("  Press [Q] or [Esc] to return to menu.", Color.Yellow)
            _results = New List(Of UnprintedCard)
            _phase = ScanPhase.Viewing
            Return
        End If

        _results = found

        AppendLine("", Color.Yellow)
        AppendLine("  Number of Shopcards not printed: " & _results.Count.ToString(), Color.White)
        AppendLine("", Color.Yellow)

        If _results.Count = 0 Then
            AppendLine("  *** No unprinted shopcards found. ***", Color.Red)
            AppendLine("", Color.Yellow)
            AppendLine("  Press [Q] or [Esc] to return to menu.", Color.Yellow)
            _phase = ScanPhase.Viewing
        Else
            AppendLine("  " & _results.Count.ToString() & " Shopcards found, please choose one of the following:", Color.Cyan)
            AppendLine("  Scan complete.    [Enter] Print & View    [V]iew only    [Q]uit", Color.Yellow)
            _phase = ScanPhase.AwaitingChoice
        End If
    End Sub

    ' -- Phase 2: Show list ---------------------------------------------------
    Private Sub ShowList(printFirst As Boolean)
        _phase = ScanPhase.Viewing

        If printFirst Then
            ' Printing NYI -- matches DOS fallthrough: shows list anyway
            AppendLine("  (Printing not yet implemented -- showing view.)", Color.DarkGoldenrod)
            AppendLine("", Color.Yellow)
        End If

        AppendLine("Shopcards that are older than 1 day and have Not been printed as invoices.", Color.White)
        AppendLine("", Color.Yellow)

        Const hdr As String = "Customer        Date      Crd# P.O. #               Part # / Name"
        Const sep As String = "────────────────┬─────────┬────┬─────────────────────┬──────────────────────"
        AppendLine(hdr, Color.Cyan)
        AppendLine(sep, Color.DarkCyan)

        For Each c In _results
            Dim line As String = String.Format("{0,-16} {1,-9} {2,4} {3,-21} {4}",
                Truncate(c.CustomerName, 16),
                Truncate(c.EntryDate, 9),
                Truncate(c.CardNumber, 4),
                Truncate(c.PONumber, 21),
                Truncate(c.PartNumberAndName, 22))
            AppendLine(line, Color.Yellow)
        Next

        AppendLine(sep, Color.DarkCyan)
        AppendLine("", Color.Yellow)

        AppendLine("", Color.Yellow)
        AppendLine("  Press [Q] or [Enter] or [Esc] to return to menu.", Color.Yellow)
        _output.ScrollToCaret()
    End Sub

    ' -- Read first few lines of a .CRD file ----------------------------------
    ' DOS CRD format (S.ASC lines 4470-4480):
    '   Line 0: CustomerName (N2$)
    '   Line 1: PartNumberAndName (PNN$)
    '   Line 2: PONumber (PO$)
    '   Line 3: HeatTreat (HT$)
    '   Line 4: Material (MT$)
    '   Line 5: NumberOfPans (PAN$)
    '   Line 6: NumberOfBoxes (BX$)
    '   Line 7: Weight (WT$)
    '   Line 8: QR$ (mag/pene qty)
    '   Line 9: QuantityReceived (QTYREC$)
    '   Line 10: 0
    '   Line 11: 0
    '   Line 12: JobRouteNumber (JN$)
    '   Then sparse triplets for sections, then -1/-1/SER$ if present.
    '   EntryDate is NOT stored in the file -- DOS uses file LastWriteTime.
    Private Function ReadCardHeader(filePath As String, fileDate As DateTime) As UnprintedCard
        Dim card As New UnprintedCard()
        card.CardNumber = Path.GetFileNameWithoutExtension(filePath)
        card.EntryDate = fileDate.ToString("MM-dd-yyyy")
        Try
            Dim lines = File.ReadAllLines(filePath)
            If lines.Length > 0 Then card.CustomerName = lines(0).Trim(""""c)
            If lines.Length > 1 Then card.PartNumberAndName = lines(1).Trim(""""c)
            If lines.Length > 2 Then card.PONumber = lines(2).Trim(""""c)
        Catch
        End Try
        Return card
    End Function

    ' -- Helpers --------------------------------------------------------------
    Private Sub AppendLine(text As String, color As Color)
        _output.SelectionStart = _output.TextLength
        _output.SelectionLength = 0
        _output.SelectionColor = color
        _output.AppendText(text & vbCrLf)
        _output.SelectionColor = _output.ForeColor
        _output.ScrollToCaret()
    End Sub

    Private Function Truncate(s As String, maxLen As Integer) As String
        If s Is Nothing Then Return "".PadRight(maxLen)
        If s.Length > maxLen Then Return s.Substring(0, maxLen)
        Return s.PadRight(maxLen)
    End Function

    ' -- Data class -----------------------------------------------------------
    Private Class UnprintedCard
        Public Property CardNumber As String = ""
        Public Property CustomerName As String = ""
        Public Property EntryDate As String = ""
        Public Property PONumber As String = ""
        Public Property PartNumberAndName As String = ""
    End Class

End Class
