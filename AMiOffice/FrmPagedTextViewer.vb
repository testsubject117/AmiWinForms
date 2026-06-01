Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Windows.Forms

Public Class FrmPagedTextViewer
    Inherits Form

    Private pnlTop As Panel
    Private btnPrint As Button
    Private btnClose As Button
    Private txtPage As TextBox

    Private _content As String = ""
    Private _printDocument As PrintDocument
    Private _printText As String = ""
    Private _printCharIndex As Integer = 0

    Public Sub New()
        MyBase.New()
        InitializeRolodexUi()
    End Sub

    Public Sub SetPages(pages As List(Of String))
        If pages Is Nothing OrElse pages.Count = 0 Then
            _content = ""
        Else
            _content = String.Join(Environment.NewLine & Environment.NewLine, pages)
        End If
    End Sub

    Private Sub InitializeRolodexUi()
        Me.Text = "Paged Viewer"
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Size = New Size(900, 650)
        Me.KeyPreview = True

        pnlTop = New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 44,
            .BackColor = Color.Black
        }

        btnPrint = New Button() With {
            .Text = "Print",
            .Width = 100,
            .Height = 30,
            .Location = New Point(10, 7),
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .TabStop = False
        }
        btnPrint.FlatAppearance.BorderColor = Color.White

        btnClose = New Button() With {
            .Text = "Close",
            .Width = 100,
            .Height = 30,
            .Location = New Point(120, 7),
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .TabStop = False
        }
        btnClose.FlatAppearance.BorderColor = Color.White

        txtPage = New TextBox() With {
            .Multiline = True,
            .ReadOnly = True,
            .BorderStyle = BorderStyle.None,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 12.0F, FontStyle.Regular),
            .Dock = DockStyle.Fill,
            .ScrollBars = ScrollBars.Vertical,
            .TabStop = False
        }

        _printDocument = New PrintDocument()

        pnlTop.Controls.Add(btnPrint)
        pnlTop.Controls.Add(btnClose)

        Me.Controls.Add(txtPage)
        Me.Controls.Add(pnlTop)

        AddHandler Me.Load, AddressOf FrmPagedTextViewer_Load
        AddHandler Me.Shown, AddressOf FrmPagedTextViewer_Shown
        AddHandler Me.KeyDown, AddressOf FrmPagedTextViewer_KeyDown
        AddHandler btnPrint.Click, AddressOf btnPrint_Click
        AddHandler btnClose.Click, AddressOf btnClose_Click
        AddHandler _printDocument.BeginPrint, AddressOf PrintDocument_BeginPrint
        AddHandler _printDocument.PrintPage, AddressOf PrintDocument_PrintPage
    End Sub

    Private Sub FrmPagedTextViewer_Load(sender As Object, e As EventArgs)
        txtPage.Text = _content
        ClearTextSelection()
    End Sub

    Private Sub FrmPagedTextViewer_Shown(sender As Object, e As EventArgs)
        Me.ActiveControl = Nothing
        ClearTextSelection()
    End Sub

    Private Sub FrmPagedTextViewer_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then
            Me.Close()
        ElseIf e.Control AndAlso e.KeyCode = Keys.P Then
            ShowPrintDialogAndPrint()
            e.SuppressKeyPress = True
        End If
    End Sub

    Private Sub btnPrint_Click(sender As Object, e As EventArgs)
        ShowPrintDialogAndPrint()
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

    Private Sub ShowPrintDialogAndPrint()
        If String.IsNullOrWhiteSpace(_content) Then
            MessageBox.Show("There is nothing to print.",
                            "Print",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information)
            Return
        End If

        Using dlg As New PrintDialog()
            dlg.AllowSomePages = False
            dlg.ShowHelp = False
            dlg.UseEXDialog = True
            dlg.Document = _printDocument

            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Try
                    _printDocument.Print()
                Catch ex As Exception
                    MessageBox.Show("Printing failed:" & Environment.NewLine & ex.Message,
                                    "Print",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    Private Sub PrintDocument_BeginPrint(sender As Object, e As PrintEventArgs)
        _printText = If(_content, "")
        _printCharIndex = 0
    End Sub

    Private Sub PrintDocument_PrintPage(sender As Object, e As PrintPageEventArgs)
        Dim printFont As Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        Dim leftMargin As Single = e.MarginBounds.Left
        Dim topMargin As Single = e.MarginBounds.Top
        Dim printableArea As New SizeF(e.MarginBounds.Width, e.MarginBounds.Height)

        If _printCharIndex >= _printText.Length Then
            e.HasMorePages = False
            Return
        End If

        Dim format As New StringFormat(StringFormatFlags.LineLimit)
        Dim charactersFitted As Integer = 0
        Dim linesFilled As Integer = 0

        Dim remainingText As String = _printText.Substring(_printCharIndex)

        e.Graphics.MeasureString(remainingText,
                                 printFont,
                                 printableArea,
                                 format,
                                 charactersFitted,
                                 linesFilled)

        If charactersFitted > 0 Then
            Dim textToPrint As String = remainingText.Substring(0, charactersFitted)

            e.Graphics.DrawString(textToPrint,
                                  printFont,
                                  Brushes.Black,
                                  New RectangleF(leftMargin, topMargin, printableArea.Width, printableArea.Height),
                                  format)

            _printCharIndex += charactersFitted
        End If

        e.HasMorePages = (_printCharIndex < _printText.Length)
    End Sub

    Private Sub ClearTextSelection()
        txtPage.SelectionStart = 0
        txtPage.SelectionLength = 0
        txtPage.HideSelection = True
        txtPage.ScrollToCaret()
    End Sub

End Class