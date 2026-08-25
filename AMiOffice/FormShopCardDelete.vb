Option Strict Off
Option Explicit On

Imports System
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' DOS-parity ShopCard Delete/Void screen.
''' Mirrors S2.BAS lines 9999+:
'''   - Shows last card number as reference
'''   - Prompts for card # to void  ([Enter] = Quit)
'''   - Finds the .CRD file, clears its archive bit (ATTRIB -A equivalent)
'''   - Shows "SHOPCARD #nnn WAS VOIDED." then loops back for another entry
''' The file is NOT physically deleted -- only the archive bit is cleared,
''' which is exactly what the DOS ATTRIB -A command did.
''' </summary>
Public Class FormShopCardDelete
    Inherits Form

    ' -- UI -------------------------------------------------------------------
    Private _output As RichTextBox
    Private _inputPanel As Panel
    Private _prompt As Label
    Private _inputBox As TextBox

    ' -- State ----------------------------------------------------------------
    Private _phase As VoidPhase = VoidPhase.PromptCardNumber

    Private Enum VoidPhase
        PromptCardNumber
        PromptConfirm
        Done
    End Enum

    Private _pendingCardNum As String = ""
    Private _pendingFilePath As String = ""

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

        _inputPanel = New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 30,
            .BackColor = Color.Black
        }

        _prompt = New Label() With {
            .AutoSize = False,
            .Width = 600,
            .Height = 24,
            .Location = New Point(4, 4),
            .BackColor = Color.Black,
            .ForeColor = Color.Yellow,
            .Font = New Font("Courier New", 10, FontStyle.Regular),
            .TextAlign = ContentAlignment.MiddleLeft
        }

        _inputBox = New TextBox() With {
            .Width = 120,
            .Height = 22,
            .Location = New Point(610, 4),
            .BackColor = Color.Black,
            .ForeColor = Color.Yellow,
            .Font = New Font("Courier New", 10, FontStyle.Regular),
            .BorderStyle = BorderStyle.None,
            .MaxLength = 6
        }

        _inputPanel.Controls.Add(_prompt)
        _inputPanel.Controls.Add(_inputBox)
        Me.Controls.Add(_output)
        Me.Controls.Add(_inputPanel)

        AddHandler _inputBox.KeyDown, AddressOf InputBox_KeyDown
    End Sub

    ' -- Shown ----------------------------------------------------------------
    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        ShowCardNumberPrompt()
    End Sub

    ' -- Input handler --------------------------------------------------------
    Private Sub InputBox_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode <> Keys.Enter Then Return
        e.SuppressKeyPress = True
        Dim val As String = _inputBox.Text.Trim()
        _inputBox.Clear()

        Select Case _phase
            Case VoidPhase.PromptCardNumber
                HandleCardNumberEntry(val)
            Case VoidPhase.PromptConfirm
                HandleConfirmEntry(val)
        End Select
    End Sub

    ' -- Phase: ask for card number -------------------------------------------
    Private Sub ShowCardNumberPrompt()
        _phase = VoidPhase.PromptCardNumber
        AppendLine("", Color.Yellow)

        ' Show last card number as reference (matches DOS "The last shopcard printed was X")
        Dim lastNum As String = ReadLastCardNumber()
        If lastNum <> "" Then
            AppendLine("  The last shopcard number used was " & lastNum, Color.White)
        End If

        AppendLine("", Color.Yellow)
        SetPrompt("  Shopcard # to Delete/Void  [Enter = Quit]? ")
        _inputBox.Focus()
    End Sub

    Private Sub HandleCardNumberEntry(val As String)
        If val = "" Then
            ' Enter with blank = quit
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

        ' Find the file
        Dim paths = ShopCardSession.FindCardFiles(val)
        If paths.Count = 0 Then
            AppendLine("  Shopcard #" & val & " not found.", Color.Red)
            ShowCardNumberPrompt()
            Return
        End If

        ' Use the first match (same as DOS)
        _pendingCardNum = val
        _pendingFilePath = paths(0)

        ' Read and display the card header so user knows what they're voiding
        AppendLine("", Color.Yellow)
        ShowCardSummary(_pendingFilePath, _pendingCardNum)
        AppendLine("", Color.Yellow)

        _phase = VoidPhase.PromptConfirm
        SetPrompt("  Void Shopcard #" & _pendingCardNum & "?  (Y/N) [Enter = No]? ")
        _inputBox.Focus()
    End Sub

    ' -- Phase: confirm void --------------------------------------------------
    Private Sub HandleConfirmEntry(val As String)
        If val.ToUpper() <> "Y" Then
            AppendLine("  Void cancelled.", Color.Cyan)
            AppendLine("", Color.Yellow)
            ShowCardNumberPrompt()
            Return
        End If

        VoidCard(_pendingFilePath, _pendingCardNum)
        ShowCardNumberPrompt()
    End Sub

    ' -- Perform the void (clear archive bit -- matches DOS ATTRIB -A) --------
    Private Sub VoidCard(filePath As String, cardNum As String)
        Try
            Dim attrs As FileAttributes = File.GetAttributes(filePath)
            ' Clear the archive bit
            File.SetAttributes(filePath, attrs And Not FileAttributes.Archive)
            AppendLine("  SHOPCARD #" & cardNum & " WAS VOIDED.", Color.Green)
        Catch ex As Exception
            AppendLine("  Error voiding shopcard #" & cardNum & ": " & ex.Message, Color.Red)
        End Try
        AppendLine("", Color.Yellow)
    End Sub

    ' -- Show card header summary before confirming void ----------------------
    Private Sub ShowCardSummary(filePath As String, cardNum As String)
        Try
            Dim lines = File.ReadAllLines(filePath)
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
    Private Sub SetPrompt(text As String)
        _prompt.Text = text
        _prompt.Width = TextRenderer.MeasureText(text, _prompt.Font).Width + 4
        _inputBox.Left = _prompt.Left + _prompt.Width + 4
    End Sub

    Private Sub AppendLine(text As String, color As Color)
        _output.SelectionStart = _output.TextLength
        _output.SelectionLength = 0
        _output.SelectionColor = color
        _output.AppendText(text & vbCrLf)
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
