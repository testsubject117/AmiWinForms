Option Strict Off
Option Explicit On

Imports System
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' DOS-parity ShopCard Delete/Void screen.
''' Mirrors S2.BAS lines 9999+: prompts for card number, confirms, then clears
''' the archive bit (ATTRIB -A equivalent). File is NOT physically deleted.
''' All input captured at form level via KeyPreview so clicking the output
''' area never breaks input.
''' </summary>
Public Class FormShopCardDelete
    Inherits Form

    ' -- UI -------------------------------------------------------------------
    Private _output As RichTextBox

    ' -- State ----------------------------------------------------------------
    Private _phase As VoidPhase = VoidPhase.PromptCardNumber
    Private _inputBuffer As String = ""
    Private _pendingCardNum As String = ""
    Private _pendingFilePath As String = ""

    Private Enum VoidPhase
        PromptCardNumber
        PromptConfirm
        Done
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
            .WordWrap = False,
            .Cursor = Cursors.Arrow
        }

        ' Prevent clicks from moving the caret or stealing focus from the form
        AddHandler _output.MouseDown, Sub(s, e) Me.Focus()

        Me.Controls.Add(_output)
    End Sub

    ' -- Shown ----------------------------------------------------------------
    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Me.Focus()
        ShowCardNumberPrompt()
    End Sub

    ' -- Form-level key capture -----------------------------------------------
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If _phase = VoidPhase.Done Then Return

        Select Case e.KeyCode
            Case Keys.Enter
                e.SuppressKeyPress = True
                Dim val As String = _inputBuffer.Trim()
                _inputBuffer = ""
                ProcessInput(val)

            Case Keys.Back
                If _inputBuffer.Length > 0 Then
                    _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1)
                    ' Remove exactly the last character from the display
                    If _output.TextLength > 0 Then
                        _output.SelectionStart = _output.TextLength - 1
                        _output.SelectionLength = 1
                        _output.SelectedText = ""
                    End If
                End If
                e.SuppressKeyPress = True

            Case Keys.Escape
                _phase = VoidPhase.Done
                Me.DialogResult = DialogResult.OK
                Me.Close()
        End Select

        MyBase.OnKeyDown(e)
    End Sub

    Protected Overrides Sub OnKeyPress(e As KeyPressEventArgs)
        If _phase = VoidPhase.Done Then Return
        Dim c As Char = e.KeyChar
        If Not Char.IsControl(c) Then
            _inputBuffer &= c
            ' Append the single character at the end — no backward selection needed
            _output.SelectionStart = _output.TextLength
            _output.SelectionLength = 0
            _output.SelectionFont = New Font("Courier New", 10, FontStyle.Regular)
            _output.SelectionColor = Color.Yellow
            _output.AppendText(c.ToString())
            _output.ScrollToCaret()
            e.Handled = True
        End If
        MyBase.OnKeyPress(e)
    End Sub

    Private Function GetCurrentPromptText() As String
        Select Case _phase
            Case VoidPhase.PromptCardNumber
                Return "  Shopcard # to Delete/Void  [Enter = Quit]? "
            Case VoidPhase.PromptConfirm
                Return "  Void Shopcard #" & _pendingCardNum & "?  (Y/N) [Enter = No]? "
            Case Else
                Return ""
        End Select
    End Function

    ' -- Route input ----------------------------------------------------------
    Private Sub ProcessInput(val As String)
        Select Case _phase
            Case VoidPhase.PromptCardNumber
                HandleCardNumberEntry(val)
            Case VoidPhase.PromptConfirm
                HandleConfirmEntry(val)
        End Select
    End Sub

    ' -- Phase: card number prompt --------------------------------------------
    Private Sub ShowCardNumberPrompt()
        _phase = VoidPhase.PromptCardNumber
        _inputBuffer = ""
        AppendLine("", Color.Yellow)

        Dim lastNum As String = ReadLastCardNumber()
        If lastNum <> "" Then
            AppendLine("  The last shopcard number used was " & lastNum, Color.White)
        End If

        AppendLine("", Color.Yellow)
        AppendInline(GetCurrentPromptText(), Color.Yellow)
    End Sub

    Private Sub HandleCardNumberEntry(val As String)
        AppendLine("", Color.Yellow)

        If val = "" Then
            _phase = VoidPhase.Done
            Me.DialogResult = DialogResult.OK
            Me.Close()
            Return
        End If

        Dim cardNum As Integer
        If Not Integer.TryParse(val, cardNum) OrElse cardNum < 1 Then
            AppendLine("  Invalid shopcard number.", Color.Red)
            ShowCardNumberPrompt()
            Return
        End If

        Dim paths = ShopCardSession.FindCardFiles(val)
        If paths.Count = 0 Then
            AppendLine("  Shopcard #" & val & " not found.", Color.Red)
            ShowCardNumberPrompt()
            Return
        End If

        _pendingCardNum = val
        _pendingFilePath = paths(0)

        ShowCardSummary(_pendingFilePath, _pendingCardNum)
        AppendLine("", Color.Yellow)

        _phase = VoidPhase.PromptConfirm
        _inputBuffer = ""
        AppendInline(GetCurrentPromptText(), Color.Yellow)
    End Sub

    ' -- Phase: confirm void --------------------------------------------------
    Private Sub HandleConfirmEntry(val As String)
        AppendLine("", Color.Yellow)
        If val.ToUpper() <> "Y" Then
            AppendLine("  Void cancelled.", Color.Cyan)
            ShowCardNumberPrompt()
            Return
        End If

        VoidCard(_pendingFilePath, _pendingCardNum)
        ShowCardNumberPrompt()
    End Sub

    ' -- Clear archive bit (ATTRIB -A equivalent) -----------------------------
    Private Sub VoidCard(filePath As String, cardNum As String)
        Try
            Dim attrs As FileAttributes = File.GetAttributes(filePath)
            File.SetAttributes(filePath, attrs And Not FileAttributes.Archive)
            AppendLine("  SHOPCARD #" & cardNum & " WAS VOIDED.", Color.Green)
        Catch ex As Exception
            AppendLine("  Error voiding shopcard #" & cardNum & ": " & ex.Message, Color.Red)
        End Try
        AppendLine("", Color.Yellow)
    End Sub

    ' -- Display card header before confirming --------------------------------
    Private Sub ShowCardSummary(filePath As String, cardNum As String)
        Try
            Dim lines() As String = File.ReadAllLines(filePath)
            Dim customer As String = If(lines.Length > 0, lines(0).Trim(""""c), "")
            Dim dateStr As String = If(lines.Length > 1, lines(1).Trim(""""c), "")
            Dim po As String = If(lines.Length > 2, lines(2).Trim(""""c), "")
            Dim part As String = If(lines.Length > 7, lines(7).Trim(""""c), "")
            AppendLine("  Card #:   " & cardNum, Color.White)
            AppendLine("  Customer: " & customer, Color.White)
            AppendLine("  Date:     " & dateStr, Color.White)
            AppendLine("  P.O. #:   " & po, Color.White)
            AppendLine("  Part:     " & part, Color.White)
        Catch
            AppendLine("  (Could not read card details)", Color.DarkGoldenrod)
        End Try
    End Sub

    ' -- Helpers --------------------------------------------------------------
    Private Sub AppendLine(text As String, color As Color)
        _output.SelectionStart = _output.TextLength
        _output.SelectionLength = 0
        _output.SelectionColor = color
        _output.AppendText(text & vbCrLf)
        _output.SelectionColor = _output.ForeColor
        _output.ScrollToCaret()
    End Sub

    Private Sub AppendInline(text As String, color As Color)
        _output.SelectionStart = _output.TextLength
        _output.SelectionLength = 0
        _output.SelectionColor = color
        _output.AppendText(text)
        _output.SelectionColor = _output.ForeColor
        _output.ScrollToCaret()
    End Sub

    Private Function ReadLastCardNumber() As String
        Try
            Dim path As String = IO.Path.Combine(ShopCardSession.DataFolder, "crdnumbr.dat")
            If File.Exists(path) Then
                Return File.ReadAllText(path).Trim().Trim(""""c)
            End If
        Catch
        End Try
        Return ""
    End Function

End Class
