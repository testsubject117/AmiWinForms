Imports System.Drawing
Imports System.Windows.Forms

Public Class FrmPagedTextViewer
    Inherits Form

    Private txtPage As TextBox
    Private _content As String = ""

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
        Me.Size = New Size(800, 600)
        Me.KeyPreview = True

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

        Me.Controls.Add(txtPage)

        AddHandler Me.Load, AddressOf FrmPagedTextViewer_Load
        AddHandler Me.Shown, AddressOf FrmPagedTextViewer_Shown
        AddHandler Me.KeyDown, AddressOf FrmPagedTextViewer_KeyDown
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
        End If
    End Sub

    Private Sub ClearTextSelection()
        txtPage.SelectionStart = 0
        txtPage.SelectionLength = 0
        txtPage.HideSelection = True
        txtPage.ScrollToCaret()
    End Sub
End Class