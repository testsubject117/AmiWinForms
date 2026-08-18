Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Section 7 — Mag/Penetrant quantity screen (DOS S.ASC lines 4170-4280).
''' Auto-shown after header confirmation. Loops until Z/ESC.
''' (A) = Qty To Be Insp. or Processed % Sample  -> A$(7,1)
''' (B) = 100%  (default = qty received)          -> A$(7,2)
''' (Z) = Exit to section hub
''' </summary>
Public Class FormShopCardMagPene
    Inherits Form

    Private _record As ShopCardRecord

    Private Enum ScreenMode
        PickLetter      ' waiting for A / B / Z keypress
        EnterValue      ' user is typing a value after picking A or B
    End Enum

    Private _mode As ScreenMode = ScreenMode.PickLetter
    Private _inputTarget As Integer = 0   ' 1 = A$(7,1),  2 = A$(7,2)

    ' ── Controls ──────────────────────────────────────────────────────────────
    Private pnlMain As New Panel()
    Private lblHeader As New Label()
    Private lblWestaero As New Label()
    Private lblA As New Label()
    Private lblB As New Label()
    Private lblZ As New Label()
    Private lblInstruct As New Label()
    Private lblInputPrompt As New Label()
    Private txtInput As New TextBox()
    Private lblUnderline As New Label()

    Public Sub New(record As ShopCardRecord)
        _record = record
        InitializeLayout()
    End Sub

    ' ── Layout ────────────────────────────────────────────────────────────────
    Private Sub InitializeLayout()
        Me.Text = "ShopCard Generator"
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        Me.Size = New Size(1024, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.KeyPreview = True   ' always on — form always sees keys first

        pnlMain.Dock = DockStyle.Fill
        pnlMain.BackColor = Color.Black

        Dim MakeLbl = Function(top As Integer) As Label
                          Dim l As New Label()
                          l.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
                          l.ForeColor = Color.White
                          l.BackColor = Color.Black
                          l.AutoSize = True
                          l.Location = New Point(8, top)
                          Return l
                      End Function

        lblHeader = MakeLbl(20)
        lblHeader.Text = "<<< Only for Mag. and Penetrant >>>"

        lblWestaero = MakeLbl(20)
        lblWestaero.ForeColor = Color.Cyan
        lblWestaero.Location = New Point(380, 20)
        lblWestaero.Text = "  WESTAERO IS 315 OR LESS"
        lblWestaero.Visible = _record.CustomerName.ToUpper() = "WESTAERO"

        lblA = MakeLbl(60)
        lblB = MakeLbl(84)

        lblZ = MakeLbl(108)
        lblZ.Text = "(Z)  EXIT"

        lblInstruct = MakeLbl(148)
        lblInstruct.Text = "*** Type Appropriate Letter ***"

        lblInputPrompt = MakeLbl(180)
        lblInputPrompt.Visible = False

        txtInput.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        txtInput.BackColor = Color.Black
        txtInput.ForeColor = Color.White
        txtInput.BorderStyle = BorderStyle.None
        txtInput.Width = 220
        txtInput.Location = New Point(8, 180)
        txtInput.Visible = False
        txtInput.MaxLength = 30
        AddHandler txtInput.TextChanged, AddressOf OnInputTextChanged

        lblUnderline = MakeLbl(180)
        lblUnderline.Text = New String("_"c, 20)
        lblUnderline.Visible = False

        For Each c As Control In {lblHeader, lblWestaero, lblA, lblB, lblZ,
                                   lblInstruct, lblInputPrompt, txtInput, lblUnderline}
            pnlMain.Controls.Add(c)
        Next
        Me.Controls.Add(pnlMain)
    End Sub

    ' ── Load ──────────────────────────────────────────────────────────────────
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        RefreshDisplay()
        Me.ActiveControl = Nothing   ' no control focused — form receives all keys
    End Sub

    ' ── Display ───────────────────────────────────────────────────────────────
    Private Sub RefreshDisplay()
        Dim aVal As String = _record.GetSection(7, 1)
        Dim bVal As String = _record.GetSection(7, 2)
        lblA.Text = "(A)  Qty To Be Insp. or Processed % Sample:  " & aVal
        lblB.Text = "(B)  100%:  " & bVal
        SetMode(ScreenMode.PickLetter)
    End Sub

    Private Sub SetMode(m As ScreenMode)
        _mode = m
        Dim picking As Boolean = (m = ScreenMode.PickLetter)
        lblInstruct.Visible = picking
        lblInputPrompt.Visible = Not picking
        txtInput.Visible = Not picking
        lblUnderline.Visible = Not picking
        If picking Then
            Me.ActiveControl = Nothing
        Else
            txtInput.Text = ""
            Dim inputLeft As Integer = lblInputPrompt.Left + lblInputPrompt.PreferredWidth + 4
            txtInput.Location = New Point(inputLeft, lblInputPrompt.Top + (lblInputPrompt.Height - txtInput.Height) \ 2)
            lblUnderline.Location = New Point(inputLeft, txtInput.Top + (txtInput.Height - lblUnderline.Height) \ 2)
            lblUnderline.Text = New String("_"c, 20)
            txtInput.Focus()
        End If
    End Sub

    ' ── Key handling ──────────────────────────────────────────────────────────
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)

        If _mode = ScreenMode.EnterValue Then
            Select Case e.KeyCode
                Case Keys.Return
                    CommitInput()
                    e.Handled = True
                Case Keys.Escape
                    RefreshDisplay()
                    e.Handled = True
            End Select
            Return
        End If

        ' PickLetter mode
        Select Case e.KeyCode
            Case Keys.A
                _inputTarget = 1
                lblInputPrompt.Text = "Qty To Be Insp. or Processed % Sample  "
                SetMode(ScreenMode.EnterValue)
                e.Handled = True

            Case Keys.B
                _inputTarget = 2
                Dim def As String = If(_record.QuantityReceived <> "",
                                       " [ENTER = " & _record.QuantityReceived & "]", "")
                lblInputPrompt.Text = "100%" & def & "  "
                SetMode(ScreenMode.EnterValue)
                e.Handled = True

            Case Keys.Z, Keys.Escape
                Me.DialogResult = DialogResult.OK
                e.Handled = True
        End Select
    End Sub

    Private Sub CommitInput()
        Dim value As String = txtInput.Text.Trim()
        If _inputTarget = 1 Then
            If value = "" Then
                RefreshDisplay()
                Return
            End If
            _record.SetSection(7, 1, value)
            _record.SetSection(7, 2, "")
        ElseIf _inputTarget = 2 Then
            If value = "" Then value = _record.QuantityReceived
            _record.SetSection(7, 1, "")
            _record.SetSection(7, 2, value)
        End If
        RefreshDisplay()
    End Sub

    Private Sub OnInputTextChanged(sender As Object, e As EventArgs)
        Dim typed As Integer = txtInput.Text.Length
        Dim remaining As Integer = Math.Max(0, 20 - typed)
        lblUnderline.Text = New String("_"c, remaining)
        lblUnderline.Left = txtInput.Left + typed * 9
        lblUnderline.Visible = remaining > 0
    End Sub

    ' ── Prevent Alt+F4 crash ─────────────────────────────────────────────────
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If Me.DialogResult = DialogResult.None Then
            Me.DialogResult = DialogResult.OK
        End If
        MyBase.OnFormClosing(e)
    End Sub

End Class
