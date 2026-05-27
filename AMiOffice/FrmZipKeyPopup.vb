Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms

Public Class FrmZipKeyPopup
    Inherits Form

    Private pnlPopup As Panel
    Private lblHeader As Label
    Private txtInput As TextBox
    Private txtResults As TextBox
    Private lblPrompt As Label
    Private lblFooter As Label

    Private ReadOnly _svc As New RolodexZipKeyService()
    Private _cityMode As Boolean = False

    Public Sub New()
        MyBase.New()
        InitializeRolodexUi()
    End Sub

    Private Sub InitializeRolodexUi()
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Size = New Size(900, 650)
        Me.KeyPreview = True
        Me.Text = "ZipKey"

        pnlPopup = New Panel() With {
            .Size = New Size(520, 500),
            .Location = New Point(190, 50),
            .BackColor = Color.DarkRed,
            .BorderStyle = BorderStyle.FixedSingle
        }

        lblHeader = New Label() With {
            .Location = New Point(12, 12),
            .Size = New Size(490, 150),
            .ForeColor = Color.White,
            .BackColor = Color.DarkRed,
            .Font = New Font("Consolas", 11.0F, FontStyle.Regular),
            .TextAlign = ContentAlignment.TopLeft
        }

        lblPrompt = New Label() With {
            .Location = New Point(12, 170),
            .Size = New Size(490, 24),
            .ForeColor = Color.White,
            .BackColor = Color.DarkRed,
            .Font = New Font("Consolas", 11.0F, FontStyle.Bold),
            .Text = "Enter ZIP / State / ? for city:"
        }

        txtInput = New TextBox() With {
            .Location = New Point(12, 198),
            .Size = New Size(490, 28),
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        }

        txtResults = New TextBox() With {
            .Location = New Point(12, 238),
            .Size = New Size(490, 210),
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .Font = New Font("Consolas", 10.5F, FontStyle.Regular)
        }

        lblFooter = New Label() With {
            .Location = New Point(12, 458),
            .Size = New Size(490, 24),
            .ForeColor = Color.White,
            .BackColor = Color.DarkRed,
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular),
            .Text = "ENTER = Search    ESC = Exit"
        }

        pnlPopup.Controls.Add(lblHeader)
        pnlPopup.Controls.Add(lblPrompt)
        pnlPopup.Controls.Add(txtInput)
        pnlPopup.Controls.Add(txtResults)
        pnlPopup.Controls.Add(lblFooter)
        Me.Controls.Add(pnlPopup)

        AddHandler Me.Load, AddressOf FrmZipKeyPopup_Load
        AddHandler Me.KeyDown, AddressOf FrmZipKeyPopup_KeyDown
        AddHandler txtInput.KeyDown, AddressOf TxtInput_KeyDown
        AddHandler Me.Shown, AddressOf FrmZipKeyPopup_Shown
    End Sub

    Private Sub FrmZipKeyPopup_Load(sender As Object, e As EventArgs)
        lblHeader.Text =
            "ZIPKEY, V1.07e current to 06/89" & Environment.NewLine &
            "Copyright 1989 Eric Isaacson" & Environment.NewLine & Environment.NewLine &
            "VB REIMPLEMENTATION" & Environment.NewLine &
            "Styled after the original ZIPKEY" & Environment.NewLine & Environment.NewLine &
            "Type a ZIP code, a two-letter" & Environment.NewLine &
            "state code, or ? for a city-" & Environment.NewLine &
            "only search:"
    End Sub

    Private Sub FrmZipKeyPopup_Shown(sender As Object, e As EventArgs)
        txtInput.Focus()

        If Not _svc.DataFileExists() Then
            txtResults.Text =
                "ZIP code data file was not found." & Environment.NewLine & Environment.NewLine &
                _svc.GetDataFilePath() & Environment.NewLine & Environment.NewLine &
                "Create zipcodes.csv to enable search."
        End If
    End Sub

    Private Sub FrmZipKeyPopup_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then
            Me.Close()
        End If
    End Sub

    Private Sub TxtInput_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            RunSearch()
        ElseIf e.KeyCode = Keys.Escape Then
            e.SuppressKeyPress = True
            Me.Close()
        End If
    End Sub

    Private Sub RunSearch()
        If Not _svc.DataFileExists() Then
            txtResults.Text =
                "ZIP code data file was not found." & Environment.NewLine & Environment.NewLine &
                _svc.GetDataFilePath() & Environment.NewLine & Environment.NewLine &
                "Create zipcodes.csv to enable search."
            Return
        End If

        Dim input = txtInput.Text.Trim()

        If _cityMode Then
            ShowResults(_svc.Search("?", input), "City search: " & input)
            _cityMode = False
            lblPrompt.Text = "Enter ZIP / State / ? for city:"
            txtInput.Clear()
            txtInput.Focus()
            Return
        End If

        If input = "?" Then
            _cityMode = True
            lblPrompt.Text = "Enter city name:"
            txtInput.Clear()
            txtResults.Text = "City-only search mode." & Environment.NewLine & "Type a city name and press ENTER."
            txtInput.Focus()
            Return
        End If

        Dim results = _svc.Search(input)
        ShowResults(results, "Search: " & input)
        txtInput.SelectAll()
        txtInput.Focus()
    End Sub

    Private Sub ShowResults(results As List(Of ZipKeyEntry), heading As String)
        Dim sb As New StringBuilder()

        sb.AppendLine(heading)
        sb.AppendLine(New String("-"c, 46))

        If results Is Nothing OrElse results.Count = 0 Then
            sb.AppendLine("No matches found.")
            txtResults.Text = sb.ToString()
            Return
        End If

        For Each item In results
            sb.AppendLine(item.ZipCode.PadRight(8) &
                      item.City.PadRight(24) &
                      item.State)
        Next

        txtResults.Text = sb.ToString()
        txtResults.SelectionStart = 0
        txtResults.SelectionLength = 0
    End Sub
End Class