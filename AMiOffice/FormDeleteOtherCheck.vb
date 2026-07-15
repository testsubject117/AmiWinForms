Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Public Class FormDeleteOtherCheck
    Inherits Form

    Private Const ScreenTitle As String = "Delete A Check"

    Private ReadOnly lblIntro As New Label()
    Private ReadOnly lblCheckNumber As New Label()
    Private ReadOnly txtCheckNumber As New TextBox()
    Private ReadOnly btnFind As New Button()
    Private ReadOnly btnClose As New Button()

    Private ReadOnly txtDetails As New TextBox()
    Private ReadOnly btnDelete As New Button()
    Private ReadOnly btnKeep As New Button()

    Private _matchedEntry As OtherCheckEntry = Nothing

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Text = ScreenTitle
        Width = 760
        Height = 460
        StartPosition = FormStartPosition.CenterParent
        BackColor = Color.Black
        ForeColor = Color.White

        BuildUi()
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
            .Text = "***** Delete A Check *****",
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

        Dim topRow As New FlowLayoutPanel() With {
            .Dock = DockStyle.Top,
            .Height = 44,
            .Padding = New Padding(0, 18, 0, 0),
            .BackColor = Color.Black
        }

        lblCheckNumber.Text = "Check # to Delete:"
        lblCheckNumber.AutoSize = True
        lblCheckNumber.ForeColor = Color.White
        lblCheckNumber.BackColor = Color.Black
        lblCheckNumber.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        lblCheckNumber.Padding = New Padding(0, 6, 8, 0)

        txtCheckNumber.Width = 180
        txtCheckNumber.MaxLength = 20
        txtCheckNumber.BorderStyle = BorderStyle.FixedSingle
        txtCheckNumber.BackColor = Color.Black
        txtCheckNumber.ForeColor = Color.White
        txtCheckNumber.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)

        ConfigureButton(btnFind, "Find")
        ConfigureButton(btnClose, "Quit")
        AddHandler btnFind.Click, Sub() FindEntry()
        AddHandler btnClose.Click, Sub() Close()

        topRow.Controls.Add(lblCheckNumber)
        topRow.Controls.Add(txtCheckNumber)
        topRow.Controls.Add(btnFind)
        topRow.Controls.Add(btnClose)

        txtDetails.Dock = DockStyle.Top
        txtDetails.Height = 160
        txtDetails.Multiline = True
        txtDetails.ReadOnly = True
        txtDetails.BorderStyle = BorderStyle.FixedSingle
        txtDetails.BackColor = Color.Black
        txtDetails.ForeColor = Color.White
        txtDetails.Font = New Font("Consolas", 11.0F, FontStyle.Regular, GraphicsUnit.Point)
        txtDetails.Visible = False

        Dim bottomButtons As New FlowLayoutPanel() With {
            .Dock = DockStyle.Top,
            .Height = 44,
            .Padding = New Padding(0, 12, 0, 0),
            .BackColor = Color.Black,
            .Visible = False
        }

        ConfigureButton(btnDelete, "Delete This Check")
        ConfigureButton(btnKeep, "Keep This Check")
        AddHandler btnDelete.Click, Sub() DeleteEntry()
        AddHandler btnKeep.Click, Sub() ClearMatch()

        bottomButtons.Controls.Add(btnDelete)
        bottomButtons.Controls.Add(btnKeep)

        outer.Controls.Add(bottomButtons)
        outer.Controls.Add(txtDetails)
        outer.Controls.Add(topRow)
        outer.Controls.Add(lblIntro)
        outer.Controls.Add(title)

        Controls.Add(outer)

        btnDelete.Tag = bottomButtons
        btnKeep.Tag = bottomButtons
    End Sub

    Private Sub ConfigureButton(btn As Button, text As String)
        btn.Text = text
        btn.Width = 150
        btn.Height = 30
        btn.UseVisualStyleBackColor = False
        btn.BackColor = Color.Black
        btn.ForeColor = Color.White
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderColor = Color.DimGray
        btn.Font = New Font("Consolas", 10.0F, FontStyle.Bold, GraphicsUnit.Point)
        btn.Margin = New Padding(0, 0, 12, 0)
    End Sub

    Private Sub FindEntry()
        Dim checkNumber As String = txtCheckNumber.Text.Trim()

        If String.Equals(checkNumber, "Q", StringComparison.OrdinalIgnoreCase) Then
            Close()
            Return
        End If

        If checkNumber = "" Then
            DosMessageBox.Show(Me, "Enter a check number.", ScreenTitle, MessageBoxButtons.OK)
            txtCheckNumber.Focus()
            Return
        End If

        Dim path As String = LegacyDataPaths.OtherChk
        Dim allEntries As List(Of OtherCheckEntry)

        Try
            allEntries = OtherChkReader.ReadAll(path)
        Catch ex As Exception
            DosMessageBox.Show(Me, "Unable to read OTHER.CHK." & Environment.NewLine & Environment.NewLine & ex.Message,
                            ScreenTitle, MessageBoxButtons.OK)
            Return
        End Try

        _matchedEntry =
            allEntries.FirstOrDefault(Function(x) String.Equals(If(x.CheckNumber, "").Trim(),
                                                                checkNumber,
                                                                StringComparison.OrdinalIgnoreCase))

        If _matchedEntry Is Nothing Then
            DosMessageBox.Show(Me, "Check not found.", ScreenTitle, MessageBoxButtons.OK)
            ClearMatch()
            Return
        End If

        txtDetails.Text =
            "Company Name: " & _matchedEntry.Company & Environment.NewLine &
            "Check #: " & _matchedEntry.CheckNumber & Environment.NewLine &
            "Check Reference: " & _matchedEntry.Reference & Environment.NewLine &
            "Date: " & _matchedEntry.DateText & Environment.NewLine &
            "Check Amount: " & _matchedEntry.Amount.ToString("0.##") & Environment.NewLine &
            "Reason Why: " & _matchedEntry.ReasonWhy

        txtDetails.Visible = True
        Dim buttonPanel = TryCast(btnDelete.Tag, FlowLayoutPanel)
        If buttonPanel IsNot Nothing Then
            buttonPanel.Visible = True
        End If
    End Sub

    Private Sub DeleteEntry()
        If _matchedEntry Is Nothing Then
            Return
        End If

        Dim confirm = DosMessageBox.Show(Me,
                                      "Delete this check?",
                                      ScreenTitle,
                                      MessageBoxButtons.YesNo,
                                      MessageBoxIcon.Warning,
                                      MessageBoxDefaultButton.Button2)

        If confirm <> DialogResult.Yes Then
            Return
        End If

        Dim path As String = LegacyDataPaths.OtherChk

        Try
            Dim allEntries = OtherChkReader.ReadAll(path)

            Dim removed As Boolean = False
            Dim remaining As New List(Of OtherCheckEntry)()

            For Each entry As OtherCheckEntry In allEntries
                If Not removed AndAlso EntriesMatch(entry, _matchedEntry) Then
                    removed = True
                Else
                    remaining.Add(entry)
                End If
            Next

            OtherChkWriter.WriteAll(path, remaining)

            DosMessageBox.Show(Me, "Check deleted.", ScreenTitle, MessageBoxButtons.OK)
            txtCheckNumber.Text = ""
            ClearMatch()
            txtCheckNumber.Focus()
        Catch ex As Exception
            DosMessageBox.Show(Me, "Unable to update OTHER.CHK." & Environment.NewLine & Environment.NewLine & ex.Message,
                            ScreenTitle, MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub ClearMatch()
        _matchedEntry = Nothing
        txtDetails.Text = ""
        txtDetails.Visible = False

        Dim buttonPanel = TryCast(btnDelete.Tag, FlowLayoutPanel)
        If buttonPanel IsNot Nothing Then
            buttonPanel.Visible = False
        End If
    End Sub

    Private Shared Function EntriesMatch(a As OtherCheckEntry, b As OtherCheckEntry) As Boolean
        If a Is Nothing OrElse b Is Nothing Then
            Return False
        End If

        Return String.Equals(If(a.Company, "").Trim(), If(b.Company, "").Trim(), StringComparison.OrdinalIgnoreCase) AndAlso
               String.Equals(If(a.DateText, "").Trim(), If(b.DateText, "").Trim(), StringComparison.OrdinalIgnoreCase) AndAlso
               String.Equals(If(a.CheckNumber, "").Trim(), If(b.CheckNumber, "").Trim(), StringComparison.OrdinalIgnoreCase) AndAlso
               a.Amount = b.Amount AndAlso
               String.Equals(If(a.Reference, "").Trim(), If(b.Reference, "").Trim(), StringComparison.OrdinalIgnoreCase) AndAlso
               String.Equals(If(a.ReasonWhy, "").Trim(), If(b.ReasonWhy, "").Trim(), StringComparison.OrdinalIgnoreCase)
    End Function
End Class

