Option Strict On
Option Explicit On

Imports System
Imports System.Windows.Forms

''' <summary>
''' Option R - Find Word Processor Text
''' DOS equivalent: Line 980 in MAINM.ASC
''' Prompts for search term, searches word processor files, displays results
''' </summary>
Public Class FormWordProcessorSearch
    Inherits Form

    Private lblPrompt As Label
    Private txtSearchTerm As TextBox
    Private btnSearch As Button
    Private btnCancel As Button

    Private _searchTerm As String = ""

    Public Sub New()
        InitializeComponent()
        Me.KeyPreview = True
    End Sub

    ''' <summary>
    ''' Gets the search term entered by the user.
    ''' </summary>
    Public ReadOnly Property SearchTerm As String
        Get
            Return _searchTerm
        End Get
    End Property

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        ' Form setup
        Me.Text = "Find Word Processor Text"
        Me.ClientSize = New Drawing.Size(600, 180)
        Me.BackColor = Drawing.Color.Black
        Me.ForeColor = Drawing.Color.White
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Font = New Drawing.Font("Consolas", 12.0F, Drawing.FontStyle.Regular, Drawing.GraphicsUnit.Point)

        ' Prompt label
        lblPrompt = New Label() With {
            .Text = "Please enter a word to look up:",
            .Location = New Drawing.Point(30, 30),
            .Size = New Drawing.Size(540, 25),
            .ForeColor = Drawing.Color.White,
            .Font = New Drawing.Font("Consolas", 12.0F, Drawing.FontStyle.Regular),
            .TextAlign = Drawing.ContentAlignment.MiddleLeft
        }
        Me.Controls.Add(lblPrompt)

        ' Search term textbox
        txtSearchTerm = New TextBox() With {
            .Location = New Drawing.Point(30, 65),
            .Size = New Drawing.Size(540, 25),
            .BackColor = Drawing.Color.White,
            .ForeColor = Drawing.Color.Black,
            .Font = New Drawing.Font("Consolas", 12.0F, Drawing.FontStyle.Regular),
            .MaxLength = 100
        }
        Me.Controls.Add(txtSearchTerm)

        ' Search button
        btnSearch = New Button() With {
            .Text = "Search",
            .Location = New Drawing.Point(350, 120),
            .Size = New Drawing.Size(100, 35),
            .BackColor = Drawing.Color.FromArgb(64, 64, 64),
            .ForeColor = Drawing.Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Drawing.Font("Consolas", 11.0F, Drawing.FontStyle.Bold)
        }
        btnSearch.FlatAppearance.BorderColor = Drawing.Color.Gray
        AddHandler btnSearch.Click, AddressOf btnSearch_Click
        Me.Controls.Add(btnSearch)

        ' Cancel button
        btnCancel = New Button() With {
            .Text = "Cancel",
            .Location = New Drawing.Point(470, 120),
            .Size = New Drawing.Size(100, 35),
            .BackColor = Drawing.Color.FromArgb(64, 64, 64),
            .ForeColor = Drawing.Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Drawing.Font("Consolas", 11.0F, Drawing.FontStyle.Bold)
        }
        btnCancel.FlatAppearance.BorderColor = Drawing.Color.Gray
        AddHandler btnCancel.Click, AddressOf btnCancel_Click
        Me.Controls.Add(btnCancel)

        Me.AcceptButton = btnSearch
        Me.CancelButton = btnCancel

        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        txtSearchTerm.Focus()
    End Sub

    Private Sub btnSearch_Click(sender As Object, e As EventArgs)
        Dim term As String = txtSearchTerm.Text.Trim()

        If String.IsNullOrWhiteSpace(term) Then
            MessageBox.Show("Please enter a search term.",
                          "Find Word Processor Text",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Information)
            txtSearchTerm.Focus()
            Return
        End If

        _searchTerm = term
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function
End Class

