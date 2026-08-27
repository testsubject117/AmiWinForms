Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

' Invoice Generator - Main Menu
' Legacy DOS: INVOICE.TXT (INVOICE.BAS) lines 920-1080
' Mirrors DOS splash screen: big title, next invoice #, printer toggle, separation instructions.
' Enter = Create Invoice (not yet implemented)
' P     = Toggle printer Texas/Lexmark
' Q     = Quit
Public Class FormInvoiceMenu
    Inherits DosMenuFormBase

    Private Const InvDataDir As String = "\\invoice\mainmenu\data"
    Private Const InvNumFile As String = "INVOICE.NUM"
    Private Const LexChkFile As String = "c:\LEXMARK.chk"

    Private _nextInvNum As Integer = 0
    Private _printer As String = "TEXAS"

    Public Sub New()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        SetMenuTitle("INVOICE GENERATOR")
        ShowVersionInHeader = False
        StretchButtonsToPanelWidth = True

        flpLeft.Padding = New Padding(8, 0, 8, 0)
        flpLeft.AutoScroll = False
        flpRight.Visible = False
        flpRight.Enabled = False

        ' Collapse right column so buttons fill full width
        Me.Controls.OfType(Of TableLayoutPanel)().
            Where(Function(t) t.ColumnCount = 2 AndAlso t.Controls.Contains(flpLeft)).
            ToList().ForEach(Sub(t)
                                 t.ColumnStyles(0).SizeType = SizeType.Percent
                                 t.ColumnStyles(0).Width = 100.0F
                                 t.ColumnStyles(1).SizeType = SizeType.Absolute
                                 t.ColumnStyles(1).Width = 0.0F
                             End Sub)

        Me.Width = 1024
        Me.Height = 720

        ' Full black background to match DOS -- recursive so nested panels are covered
        PaintBlack(Me)

        LoadPrinter()
        LoadNextInvoiceNum()
        BuildMenu()
        TightenMenuButtons()
        AddSeparationBlock()
        PaintBlack(Me)   ' second pass after controls are added
    End Sub

    Private Shared ReadOnly _darkGray As Color = Color.FromArgb(32, 32, 32)

    Private Sub PaintBlack(root As Control)
        ' Replace both SystemColors.Control (default grey) and the base-class DarkGray (32,32,32)
        ' Leave intentional colors alone (Yellow borders, Silver ESC button)
        Dim bg = root.BackColor
        If bg = SystemColors.Control OrElse bg = SystemColors.Window OrElse bg = _darkGray Then
            root.BackColor = Color.Black
        End If
        For Each c As Control In root.Controls
            PaintBlack(c)
        Next
    End Sub

    ' DOS line 945: OPEN "INVOICE.NUM" FOR INPUT AS 3: INPUT #3, X1: CLOSE 3: NEWINUM = X1 + 1
    Private Sub LoadNextInvoiceNum()
        Dim path As String = IO.Path.Combine(InvDataDir, InvNumFile)
        Try
            If IO.File.Exists(path) Then
                Dim n As Integer
                If Integer.TryParse(IO.File.ReadAllText(path).Trim(), n) Then
                    _nextInvNum = n + 1
                End If
            End If
        Catch
        End Try
    End Sub

    ' Printer state: LEXMARK if c:\LEXMARK.chk exists, else TEXAS
    Private Sub LoadPrinter()
        Try
            _printer = If(IO.File.Exists(LexChkFile), "LEXMARK", "TEXAS")
        Catch
            _printer = "TEXAS"
        End Try
    End Sub

    ' -----------------------------------------------------------------------
    ' Menu buttons
    ' NOTE: AddMenuButton prepends "(key) " automatically -- do NOT include
    ' the key prefix in the text argument or it will be doubled.
    ' -----------------------------------------------------------------------
    Private Sub BuildMenu()
        ClearMenu()
        ' DOS line 950: [Enter] Create an Invoice
        AddMenuButton(flpLeft, "Enter", "Create an Invoice", Sub() LaunchCreate())
        ' DOS: (P) Switch between Texas & Lexmark -- shows current printer state on the button
        AddMenuButton(flpLeft, "P", "Switch Printer  [Current: " & _printer & "]", Sub() TogglePrinter())
        ' DOS line 1070: (Q) quit
        AddMenuButton(flpLeft, "Q", "Quit", Sub() Me.Close())
    End Sub

    ' -----------------------------------------------------------------------
    ' Separation block -- added AFTER buttons so it appears below them,
    ' matching DOS layout: menu options first, then the paragraph.
    ' Uses a single multi-line Label so it never wraps mid-sentence.
    ' -----------------------------------------------------------------------
    Private Sub AddSeparationBlock()
        Dim nextText As String = If(_nextInvNum > 0, _nextInvNum.ToString(), "(no INVOICE.NUM)")
        Dim yellow As Color = Color.FromArgb(255, 255, 0)
        Dim mono As New Font("Courier New", 10, FontStyle.Regular)
        Dim monoBold As New Font("Courier New", 10, FontStyle.Bold)

        ' Spacer
        Dim spacer As New Label() With {
            .Height = 8, .AutoSize = False, .BackColor = Color.Black,
            .Width = 800
        }
        flpLeft.Controls.Add(spacer)

        Dim panelW As Integer = If(flpLeft.Width > 100, flpLeft.Width - 20, 960)

        ' Extra blank line to push separation block down (matches DOS spacing)
        Dim spacer2 As New Label() With {
            .Height = 18, .AutoSize = False, .BackColor = Color.Black, .Width = panelW
        }
        flpLeft.Controls.Add(spacer2)

        ' Free Mem -- shown in the right half of the form at the same vertical level as the Enter button
        ' We add it to flpRight which is hidden but still occupies space; instead use a floating label on Me
        Dim availMem As String
        Try
            Dim ci As New Microsoft.VisualBasic.Devices.ComputerInfo()
            availMem = Math.Round(ci.AvailablePhysicalMemory / 1024 / 1024).ToString() & "M"
        Catch
            availMem = "--"
        End Try
        Dim lblFreeMem As New Label() With {
            .Text = "Free Mem: " & availMem,
            .ForeColor = Color.White, .BackColor = Color.Black,
            .Font = mono, .AutoSize = True,
            .UseMnemonic = False
        }
        ' Position it: vertically aligned with the Enter button, right-aligned in form
        AddHandler Me.Shown, Sub(ss As Object, se As EventArgs)
            Dim enterBtn As Control = flpLeft.Controls.OfType(Of Button)().FirstOrDefault()
            Dim topY As Integer
            If enterBtn IsNot Nothing Then
                ' Convert button's position through the control hierarchy to form client coords
                Dim screenPt = flpLeft.PointToScreen(New Point(0, enterBtn.Top))
                Dim formPt = Me.PointToClient(screenPt)
                topY = formPt.Y + (enterBtn.Height - lblFreeMem.Height) \ 2
            Else
                topY = 160
            End If
            lblFreeMem.Left = Me.ClientSize.Width - lblFreeMem.Width - 16
            lblFreeMem.Top = topY
            lblFreeMem.BringToFront()
        End Sub
        Me.Controls.Add(lblFreeMem)
        lblFreeMem.BringToFront()

        ' INVOICE SEPARATION header line
        ' Two blank lines above to match DOS spacing
        For i As Integer = 1 To 2
            flpLeft.Controls.Add(New Label() With {
                .Height = 18, .AutoSize = False, .BackColor = Color.Black, .Width = panelW
            })
        Next
        Dim lblHeader As New Label() With {
            .Text = "INVOICE SEPARATION:    Get David Herrera to sign.",
            .ForeColor = yellow, .BackColor = Color.Black,
            .Font = monoBold, .AutoSize = False,
            .UseMnemonic = False,
            .Width = panelW, .Height = 20,
            .Margin = New Padding(0, 4, 0, 0)
        }
        flpLeft.Controls.Add(lblHeader)

        ' Separation instruction lines
        Dim sepLines() As String = {
            "Top page is put in yellow shelf on desk.",
            "Next page is put in the left blue holder on desk OR in manilla envelopes.",
            "Next 3 are given to customer.",
            "Bottom copy signed & put in bottom drawer."
        }
        For Each txt In sepLines
            Dim lbl As New Label() With {
                .Text = txt,
                .ForeColor = Color.White, .BackColor = Color.Black,
                .Font = mono, .AutoSize = False,
                .UseMnemonic = False,
                .Width = panelW, .Height = 18,
                .Margin = New Padding(0, 0, 0, 0)
            }
            flpLeft.Controls.Add(lbl)
        Next

        ' Set Next Invoice in header center -- must be set after UpdateHeaderClock
        ' so we override it.  Store value so OnHeaderTick can restore it.
        _nextInvoiceDisplay = "Next Invoice:  " & nextText
        lblDateTime.Text = _nextInvoiceDisplay
        lblDateTime.TextAlign = ContentAlignment.MiddleCenter
    End Sub

    ' Stored so the clock tick can restore it rather than clearing it
    Private _nextInvoiceDisplay As String = ""

    ' Keep Next Invoice in header center even after clock ticks
    Protected Overrides Sub UpdateHeaderClock()
        MyBase.UpdateHeaderClock()
        If _nextInvoiceDisplay <> "" Then
            lblDateTime.Text = _nextInvoiceDisplay
            lblDateTime.TextAlign = ContentAlignment.MiddleCenter
        End If
    End Sub

    ' -----------------------------------------------------------------------
    ' Actions
    ' -----------------------------------------------------------------------

    Private Sub LaunchCreate()
        NotYet("Invoice Creation")
    End Sub

    ' DOS lines 1051-1057: confirm then toggle printer and update LEXMARK.chk
    Private Sub TogglePrinter()
        Dim confirm As DialogResult = MessageBox.Show(
            "Are you sure you want to switch the printer?",
            "Switch Printer", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If confirm <> DialogResult.Yes Then Return

        Try
            If _printer = "LEXMARK" Then
                _printer = "TEXAS"
                If IO.File.Exists(LexChkFile) Then IO.File.Delete(LexChkFile)
            Else
                _printer = "LEXMARK"
                IO.File.WriteAllText(LexChkFile, DateTime.Now.ToShortDateString())
            End If
        Catch ex As Exception
            MessageBox.Show("Could not update printer state: " & ex.Message,
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try

        ' Remove old separation labels (non-button controls) and rebuild
        Dim toRemove As New List(Of Control)
        For Each c As Control In flpLeft.Controls
            If Not TypeOf c Is Button Then toRemove.Add(c)
        Next
        For Each c As Control In toRemove
            flpLeft.Controls.Remove(c)
            c.Dispose()
        Next
        AddSeparationBlock()

        ' Update P button text
        For Each c As Control In flpLeft.Controls
            Dim btn = TryCast(c, Button)
            If btn IsNot Nothing AndAlso TryCast(btn.Tag, String) = "P" Then
                btn.Text = "(P) Switch Printer  [Current: " & _printer & "]"
            End If
        Next
    End Sub

    ' -----------------------------------------------------------------------
    ' Keyboard -- Enter triggers Create (DOS line 1050: CHR$(13) THEN 1100)
    ' -----------------------------------------------------------------------
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.KeyCode = Keys.Enter Then
            LaunchCreate()
            e.Handled = True
        End If
    End Sub

    ' Local copy matching other menu forms
    Private Sub TightenMenuButtons()
        For Each c As Control In flpLeft.Controls
            Dim btn = TryCast(c, Button)
            If btn IsNot Nothing Then
                btn.Height = 34
                btn.Margin = New Padding(3, 3, 3, 4)
                btn.Font = New Font("Segoe UI", 11.0F, FontStyle.Bold, GraphicsUnit.Point)
            End If
        Next
    End Sub

End Class
