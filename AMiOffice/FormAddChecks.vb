Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Collections.Generic

Public Class FormAddChecks
    Inherits Form

    Private lblTitle As Label
    Private lblPrompt As Label
    Private txtCompanyCode As TextBox
    Private btnValidate As Button
    Private btnContinue As Button
    Private btnCancel As Button
    Private lblStatusCaption As Label
    Private lblStatus As Label
    Private lblMatches As Label
    Private lstMatches As ListBox

    Private _lookup As CompanyLookupService
    Private _selectedCompany As CompanyInfo

    Public ReadOnly Property SelectedCompany As CompanyInfo
        Get
            Return _selectedCompany
        End Get
    End Property

    Public Sub New()
        MyBase.New()
        InitializeCustomComponents()
    End Sub

    Private Sub FormAddChecks_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            Dim realNamePath As String = "\\invoice\MainMenu\Data\REALNAME.DAT"

            _lookup = New CompanyLookupService(realNamePath)
            _lookup.LoadData()

            lblStatus.Text = "Enter company code."
            txtCompanyCode.Focus()

        Catch ex As Exception
            MessageBox.Show("Unable to load company data." & Environment.NewLine & Environment.NewLine & ex.Message,
                            "Add Checks",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
            lblStatus.Text = "Company data could not be loaded."
            btnValidate.Enabled = False
            btnContinue.Enabled = False
        End Try
    End Sub

    Private Sub InitializeCustomComponents()
        Me.Text = "Add Checks"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Size = New Size(760, 520)
        Me.MinimumSize = New Size(760, 520)
        Me.BackColor = Color.Black
        Me.ForeColor = Color.Lime
        Me.Font = New Font("Consolas", 11.0!, FontStyle.Regular)
        Me.KeyPreview = True

        lblTitle = New Label()
        lblTitle.AutoSize = False
        lblTitle.Text = "ADD CHECKS TO CASH RECEIPTS"
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        lblTitle.Font = New Font("Consolas", 16.0!, FontStyle.Bold)
        lblTitle.ForeColor = Color.Yellow
        lblTitle.BackColor = Color.Black
        lblTitle.SetBounds(40, 20, 660, 35)

        lblPrompt = New Label()
        lblPrompt.AutoSize = False
        lblPrompt.Text = "Company Code:"
        lblPrompt.TextAlign = ContentAlignment.MiddleLeft
        lblPrompt.ForeColor = Color.Lime
        lblPrompt.BackColor = Color.Black
        lblPrompt.SetBounds(60, 90, 140, 28)

        txtCompanyCode = New TextBox()
        txtCompanyCode.CharacterCasing = CharacterCasing.Upper
        txtCompanyCode.BorderStyle = BorderStyle.FixedSingle
        txtCompanyCode.Font = New Font("Consolas", 12.0!, FontStyle.Bold)
        txtCompanyCode.SetBounds(210, 90, 180, 28)
        txtCompanyCode.MaxLength = 8

        btnValidate = New Button()
        btnValidate.Text = "Validate"
        btnValidate.SetBounds(420, 88, 110, 32)
        AddHandler btnValidate.Click, AddressOf btnValidate_Click

        btnContinue = New Button()
        btnContinue.Text = "Continue"
        btnContinue.SetBounds(540, 88, 110, 32)
        btnContinue.Enabled = False
        AddHandler btnContinue.Click, AddressOf btnContinue_Click

        btnCancel = New Button()
        btnCancel.Text = "Cancel"
        btnCancel.SetBounds(540, 430, 110, 32)
        AddHandler btnCancel.Click, AddressOf btnCancel_Click

        lblStatusCaption = New Label()
        lblStatusCaption.AutoSize = False
        lblStatusCaption.Text = "Status:"
        lblStatusCaption.TextAlign = ContentAlignment.MiddleLeft
        lblStatusCaption.ForeColor = Color.Aqua
        lblStatusCaption.BackColor = Color.Black
        lblStatusCaption.SetBounds(60, 145, 80, 28)

        lblStatus = New Label()
        lblStatus.AutoSize = False
        lblStatus.Text = ""
        lblStatus.TextAlign = ContentAlignment.MiddleLeft
        lblStatus.ForeColor = Color.White
        lblStatus.BackColor = Color.Black
        lblStatus.BorderStyle = BorderStyle.FixedSingle
        lblStatus.SetBounds(140, 145, 510, 28)

        lblMatches = New Label()
        lblMatches.AutoSize = False
        lblMatches.Text = "Possible Matches:"
        lblMatches.TextAlign = ContentAlignment.MiddleLeft
        lblMatches.ForeColor = Color.Aqua
        lblMatches.BackColor = Color.Black
        lblMatches.SetBounds(60, 200, 180, 28)

        lstMatches = New ListBox()
        lstMatches.Font = New Font("Consolas", 11.0!, FontStyle.Regular)
        lstMatches.HorizontalScrollbar = True
        lstMatches.SetBounds(60, 230, 590, 180)
        AddHandler lstMatches.DoubleClick, AddressOf lstMatches_DoubleClick

        Me.Controls.Add(lblTitle)
        Me.Controls.Add(lblPrompt)
        Me.Controls.Add(txtCompanyCode)
        Me.Controls.Add(btnValidate)
        Me.Controls.Add(btnContinue)
        Me.Controls.Add(btnCancel)
        Me.Controls.Add(lblStatusCaption)
        Me.Controls.Add(lblStatus)
        Me.Controls.Add(lblMatches)
        Me.Controls.Add(lstMatches)
    End Sub

    Private Sub btnValidate_Click(sender As Object, e As EventArgs)
        ValidateCompany()
    End Sub

    Private Sub btnContinue_Click(sender As Object, e As EventArgs)
        If _selectedCompany Is Nothing Then
            MessageBox.Show("Please validate a company first.",
                            "Add Checks",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information)
            Exit Sub
        End If

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs)
        _selectedCompany = Nothing
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub lstMatches_DoubleClick(sender As Object, e As EventArgs)
        If lstMatches.SelectedItem Is Nothing Then Exit Sub

        Dim selectedText As String = lstMatches.SelectedItem.ToString()
        If selectedText Is Nothing Then Exit Sub

        Dim parts() As String = selectedText.Split("-"c)
        If parts.Length > 0 Then
            txtCompanyCode.Text = parts(0).Trim()
            ValidateCompany()
        End If
    End Sub

    Private Sub ValidateCompany()
        If _lookup Is Nothing Then Exit Sub

        lstMatches.Items.Clear()
        btnContinue.Enabled = False
        _selectedCompany = Nothing

        Dim enteredCode As String = txtCompanyCode.Text.Trim().ToUpperInvariant()

        If enteredCode = "" Then
            lblStatus.Text = "Company code is required."
            txtCompanyCode.Focus()
            Exit Sub
        End If

        If enteredCode.Length > 8 Then
            enteredCode = enteredCode.Substring(0, 8)
            txtCompanyCode.Text = enteredCode
        End If

        Dim exact As CompanyInfo = _lookup.FindExact(enteredCode)

        If exact IsNot Nothing Then
            _selectedCompany = exact
            lblStatus.Text = exact.CompanyCode & " - " & exact.CompanyName
            btnContinue.Enabled = True
            Exit Sub
        End If

        lblStatus.Text = "Company code not found."

        Dim matches As List(Of CompanyInfo) = _lookup.FindByPrefix(enteredCode)

        If matches.Count = 0 AndAlso enteredCode.Length > 0 Then
            matches = _lookup.FindByFirstLetter(enteredCode.Substring(0, 1))
        End If

        For Each item As CompanyInfo In matches
            lstMatches.Items.Add(item.CompanyCode & " - " & item.CompanyName)
        Next

        If matches.Count > 0 Then
            lblStatus.Text = "Company not found. Select a match or enter another code."
        End If
    End Sub

    Private Sub FormAddChecks_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyCode = Keys.Escape Then
            btnCancel.PerformClick()
            e.Handled = True
        ElseIf e.KeyCode = Keys.F5 Then
            btnValidate.PerformClick()
            e.Handled = True
        ElseIf e.KeyCode = Keys.Enter Then
            If txtCompanyCode.Focused Then
                btnValidate.PerformClick()
                e.Handled = True
            End If
        End If
    End Sub

End Class
