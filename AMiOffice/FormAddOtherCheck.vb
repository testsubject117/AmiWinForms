Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

Public Class FormAddOtherCheck
    Inherits Form

    Private Const ScreenTitle As String = "Add A Check"

    Private ReadOnly lblIntro As New Label()

    Private ReadOnly lblCompany As New Label()
    Private ReadOnly txtCompany As New TextBox()

    Private ReadOnly lblCheckNumber As New Label()
    Private ReadOnly txtCheckNumber As New TextBox()

    Private ReadOnly lblReference As New Label()
    Private ReadOnly txtReference As New TextBox()

    Private ReadOnly lblDate As New Label()
    Private ReadOnly txtDate As New TextBox()

    Private ReadOnly lblAmount As New Label()
    Private ReadOnly txtAmount As New TextBox()

    Private ReadOnly lblReason As New Label()
    Private ReadOnly txtReason As New TextBox()

    Private ReadOnly btnAdd As New Button()
    Private ReadOnly btnReEnter As New Button()
    Private ReadOnly btnCancel As New Button()

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = ScreenTitle
        Width = 760
        Height = 500
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.Black
        ForeColor = Color.White

        BuildUi()
        ResetForm()
    End Sub

    Private Sub BuildUi()
        Dim outer As New Panel() With {
            .Dock = DockStyle.Fill,
            .Padding = New Padding(16),
            .BackColor = Color.Black
        }

        Dim title As New Label() With {
            .Dock = DockStyle.Top,
            .Height = 28,
            .Text = "***** Add A Check *****",
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Font = New Font("Consolas", 14.0F, FontStyle.Regular, GraphicsUnit.Point),
            .TextAlign = ContentAlignment.MiddleLeft
        }

        lblIntro.Dock = DockStyle.Top
        lblIntro.Height = 34
        lblIntro.Text = "This is usually for over payments or checks that are Not paying for invoices"
        lblIntro.ForeColor = Color.Black
        lblIntro.BackColor = Color.Silver
        lblIntro.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        lblIntro.TextAlign = ContentAlignment.MiddleLeft

        Dim grid As New TableLayoutPanel() With {
            .Dock = DockStyle.Top,
            .AutoSize = True,
            .ColumnCount = 2,
            .RowCount = 6,
            .Padding = New Padding(0, 20, 0, 0),
            .BackColor = Color.Black
        }

        grid.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180))
        grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

        ConfigureLabel(lblCompany, "Company Name")
        ConfigureLabel(lblCheckNumber, "Check #")
        ConfigureLabel(lblReference, "Check Reference")
        ConfigureLabel(lblDate, "Date")
        ConfigureLabel(lblAmount, "Check Amount")
        ConfigureLabel(lblReason, "Reason Why")

        ConfigureTextBox(txtCompany, 40)
        ConfigureTextBox(txtCheckNumber, 20)
        ConfigureTextBox(txtReference, 40)
        ConfigureTextBox(txtDate, 12)
        ConfigureTextBox(txtAmount, 20)
        ConfigureTextBox(txtReason, 80)

        grid.Controls.Add(lblCompany, 0, 0)
        grid.Controls.Add(txtCompany, 1, 0)
        grid.Controls.Add(lblCheckNumber, 0, 1)
        grid.Controls.Add(txtCheckNumber, 1, 1)
        grid.Controls.Add(lblReference, 0, 2)
        grid.Controls.Add(txtReference, 1, 2)
        grid.Controls.Add(lblDate, 0, 3)
        grid.Controls.Add(txtDate, 1, 3)
        grid.Controls.Add(lblAmount, 0, 4)
        grid.Controls.Add(txtAmount, 1, 4)
        grid.Controls.Add(lblReason, 0, 5)
        grid.Controls.Add(txtReason, 1, 5)

        Dim buttons As New FlowLayoutPanel() With {
            .Dock = DockStyle.Top,
            .Height = 48,
            .Padding = New Padding(0, 22, 0, 0),
            .BackColor = Color.Black
        }

        ConfigureButton(btnAdd, "Add This Check")
        ConfigureButton(btnReEnter, "Re-Enter")
        ConfigureButton(btnCancel, "Quit")

        AddHandler btnAdd.Click, Sub() SaveCheck()
        AddHandler btnReEnter.Click, Sub() ResetForm()
        AddHandler btnCancel.Click, Sub() Close()

        buttons.Controls.Add(btnAdd)
        buttons.Controls.Add(btnReEnter)
        buttons.Controls.Add(btnCancel)

        outer.Controls.Add(buttons)
        outer.Controls.Add(grid)
        outer.Controls.Add(lblIntro)
        outer.Controls.Add(title)

        Controls.Add(outer)
    End Sub

    Private Sub ConfigureLabel(lbl As Label, text As String)
        lbl.Text = text & ":"
        lbl.AutoSize = True
        lbl.ForeColor = Color.White
        lbl.BackColor = Color.Black
        lbl.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        lbl.Margin = New Padding(0, 8, 8, 8)
        lbl.Anchor = AnchorStyles.Left
    End Sub

    Private Sub ConfigureTextBox(txt As TextBox, maxLen As Integer)
        txt.Width = 420
        txt.MaxLength = maxLen
        txt.BorderStyle = BorderStyle.FixedSingle
        txt.BackColor = Color.Black
        txt.ForeColor = Color.White
        txt.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txt.Margin = New Padding(0, 4, 0, 4)
        txt.Anchor = AnchorStyles.Left
    End Sub

    Private Sub ConfigureButton(btn As Button, text As String)
        btn.Text = text
        btn.Width = 140
        btn.Height = 30
        btn.UseVisualStyleBackColor = False
        btn.BackColor = Color.Black
        btn.ForeColor = Color.White
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderColor = Color.DimGray
        btn.Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        btn.Margin = New Padding(0, 0, 12, 0)
    End Sub

    Private Sub ResetForm()
        txtCompany.Text = ""
        txtCheckNumber.Text = ""
        txtReference.Text = ""
        txtDate.Text = Date.Today.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture)
        txtAmount.Text = ""
        txtReason.Text = ""
        txtCompany.Focus()
    End Sub

    Private Sub SaveCheck()
        Dim company As String = txtCompany.Text.Trim()
        Dim checkNumber As String = txtCheckNumber.Text.Trim()
        Dim reference As String = txtReference.Text.Trim()
        Dim dateText As String = txtDate.Text.Trim()
        Dim amountText As String = txtAmount.Text.Trim()
        Dim reason As String = txtReason.Text.Trim()

        If String.Equals(company, "Q", StringComparison.OrdinalIgnoreCase) Then
            Close()
            Return
        End If

        If company = "" Then
            DosMessageBox.Show(Me, "Company Name is required.", ScreenTitle, MessageBoxButtons.OK)
            txtCompany.Focus()
            Return
        End If

        If checkNumber = "" Then
            DosMessageBox.Show(Me, "Check # is required.", ScreenTitle, MessageBoxButtons.OK)
            txtCheckNumber.Focus()
            Return
        End If

        If dateText = "" Then
            dateText = Date.Today.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture)
        End If

        Dim parsedDate As DateTime
        If Not DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, parsedDate) AndAlso
           Not DateTime.TryParse(dateText, parsedDate) Then
            DosMessageBox.Show(Me, "Enter a valid date.", ScreenTitle, MessageBoxButtons.OK)
            txtDate.Focus()
            Return
        End If

        Dim amount As Decimal
        If Not Decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, amount) AndAlso
           Not Decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.CurrentCulture, amount) Then
            DosMessageBox.Show(Me, "Enter a valid check amount.", ScreenTitle, MessageBoxButtons.OK)
            txtAmount.Focus()
            Return
        End If

        Dim entry As New OtherCheckEntry() With {
            .Company = company,
            .DateText = parsedDate.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture),
            .CheckNumber = checkNumber,
            .Amount = amount,
            .Reference = reference,
            .ReasonWhy = reason
        }

        Dim confirmText As String =
            "Company Name: " & entry.Company & Environment.NewLine &
            "Check #: " & entry.CheckNumber & Environment.NewLine &
            "Check Reference: " & entry.Reference & Environment.NewLine &
            "Date: " & entry.DateText & Environment.NewLine &
            "Check Amount: " & entry.Amount.ToString("0.##", CultureInfo.InvariantCulture) & Environment.NewLine &
            "Reason Why: " & entry.ReasonWhy & Environment.NewLine & Environment.NewLine &
            "Add this check?"

        Dim result = DosMessageBox.Show(Me, confirmText, ScreenTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)

        If result <> DialogResult.Yes Then
            Return
        End If

        Dim path As String = LegacyDataPaths.OtherChk

        Try
            OtherChkWriter.Append(path, entry)
            DosMessageBox.Show(Me, "Check added.", ScreenTitle, MessageBoxButtons.OK)
            ResetForm()
        Catch ex As Exception
            DosMessageBox.Show(Me, "Unable to write OTHER.CHK." & Environment.NewLine & Environment.NewLine & ex.Message,
                            ScreenTitle, MessageBoxButtons.OK)
        End Try
    End Sub
End Class

