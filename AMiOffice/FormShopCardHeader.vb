Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

''' <summary>
''' ShopCard Header Entry Form — mirrors the DOS sequential prompt flow from S.ASC.
''' Walks the user through each header field one at a time, matching the terminal scroll style.
''' On completion, exposes a populated ShopCardRecord for downstream use.
''' </summary>
Public Class FormShopCardHeader
    Inherits Form

    ' ── Material quick-pick table (matches S.ASC lines 1480-1641) ────────────────
    Private Shared ReadOnly MaterialShortcuts As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
        {"1", "A-286"},
        {"2", "Alloy Steel"},
        {"3", "Stellite #6"},
        {"4", "Alum"},
        {"5", "8740"},
        {"6", "6-4Ti"},
        {"7", "Stainless Steel"},
        {"8", "Alloy"},
        {"9", "Beryl-CU"},
        {"0", "17-4PH"},
        {"P", "15-5PH"},
        {"W", "WASP"},
        {"D", "356-T6"},
        {"G", "GRK. ASC."},
        {"I", "INCO-718"},
        {"C", "440C"},
        {"A", "ALLOY #6"},
        {"E", "2024-T851"},
        {"F", "7075-T851"},
        {"H", "2014-T6"},
        {"T", "7075-T73"},
        {"U", "ARP 2000"}
    }

    ' ── Layout controls ──────────────────────────────────────────────────────────
    Private WithEvents pnlHeader As New Panel()
    Private lblTitle As New Label()
    Private lblCustomerTag As New Label()      ' "Making shopcard for " prefix (yellow)
    Private lblCustomerTagName As New Label()  ' just the name, black-on-white highlight

    Private pnlScroll As New Panel()           ' scrolling transcript area
    Private lblTranscript As New Label()       ' stacked answered-fields display
    Private pnlPrompt As New Panel()           ' current active prompt area

    Private lblPrompt As New Label()
    Private lblHint As New Label()
    Private txtInput As New TextBox()
    Private lblInputUnderline As New Label()   ' DOS-style underscores shown before/during typing
    Private pnlMaterialList As New Panel()     ' material quick-pick grid
    Private lblMaterialList As New Label()
    Private lblPlusHint As New Label()         ' [+ = ] shown after underline on right
    Private lblPromptSuffix As New Label()     ' white "?" shown after cyan inline hint
    Private lblHint2 As New Label()            ' second hint line (e.g. [H = ] above HeatTreat)
    Private pnlSummary As New Panel()          ' header review panel
    Private lblMaterialConfirm As New Label()  ' "Material #:  [7075-T73]" black-on-white highlight

    ' ── State ────────────────────────────────────────────────────────────────────
    Private _record As New ShopCardRecord()
    Private _transcriptLines As New List(Of String)()
    Private _currentStep As EntryStep = EntryStep.CustomerName
    Private _accepted As Boolean = False

    Public ReadOnly Property Result As ShopCardRecord
        Get
            Return _record
        End Get
    End Property

    Public ReadOnly Property Accepted As Boolean
        Get
            Return _accepted
        End Get
    End Property

    Private Enum EntryStep
        CustomerName
        CustomerNameNotFound   ' DOS line 670: "X does not exist... Would you like to see a list (Y/N)?"
        CustomerNameList       ' DOS line 710: FILES "PRC\X*.PRC" then hit ENTER
        EntryDate
        PONumber
        NumberOfPans
        NumberOfBoxes
        NumberOfCrates
        Weight
        Quantity
        PartNumber
        JobRoute
        Material
        HeatTreat
        HotRush
        Summary
    End Enum

    Private Sub TightenButtons()
    End Sub

    ' ── Constructor ──────────────────────────────────────────────────────────────
    Private _startStep As EntryStep = EntryStep.EntryDate
    Private _nameOnly As Boolean = False
    Private _pendingBadName As String = ""  ' holds the rejected name for CustomerNameNotFound step
    Private _justMode As Boolean = False

    Public Sub New()
        InitializeLayout()
        _startStep = EntryStep.CustomerName
    End Sub

    ''' <summary>Name-only mode: shows only the customer name prompt, then closes with OK.</summary>
    Public Shared Function AskCustomerName(owner As Form) As String
        Using frm As New FormShopCardHeader(nameOnly:=True)
            If frm.ShowDialog(owner) = DialogResult.OK Then
                Return frm.Result.CustomerName
            End If
        End Using
        Return Nothing
    End Function

    Private Sub New(nameOnly As Boolean)
        InitializeLayout()
        _startStep = EntryStep.CustomerName
        _nameOnly = nameOnly
        ' Pre-fill carry-forward name from session so [ENTER = PSIBEARI] hint shows
        If ShopCardSession.LastCustomerName <> "" Then
            _record.CustomerName = ShopCardSession.LastCustomerName
        End If
    End Sub

    ''' <summary>Pre-populates customer name; starts at date prompt (customer already collected by menu).</summary>
    Public Sub New(customerName As String)
        InitializeLayout()
        _record.CustomerName = customerName
        _startStep = EntryStep.EntryDate
    End Sub

    ''' <summary>
    ''' Just (FAA) mode: carries forward all fields from the last card except Qty and Part#.
    ''' DOS line 1110: skips date/PO/pans/weight — jumps straight to Quantity then PartNumber then Summary.
    ''' DOS line 1400: carries JN$, MT$, HT$, A$(7,1), A$(7,2) from last card.
    ''' DOS line 1800: HWC$="YES", then GOTO 2000 (summary).
    ''' </summary>
    Public Sub New(customerName As String, lastRecord As ShopCardRecord)
        InitializeLayout()
        _justMode = True
        _startStep = EntryStep.Quantity
        ' Carry forward all header fields from last card (DOS line 1110: D$=DATE$, PO$=PO22$)
        _record.CustomerName = customerName
        _record.EntryDate = DateTime.Today.ToString("MM-dd-yyyy")
        _record.PONumber = If(lastRecord IsNot Nothing, lastRecord.PONumber, "")
        _record.NumberOfPans = If(lastRecord IsNot Nothing, lastRecord.NumberOfPans, "")
        _record.NumberOfBoxes = If(lastRecord IsNot Nothing, lastRecord.NumberOfBoxes, "")
        _record.NumberOfCrates = If(lastRecord IsNot Nothing, lastRecord.NumberOfCrates, "")
        _record.Weight = If(lastRecord IsNot Nothing, lastRecord.Weight, "")
        _record.JobRouteNumber = If(lastRecord IsNot Nothing, lastRecord.JobRouteNumber, "")
        _record.Material = If(lastRecord IsNot Nothing, lastRecord.Material, "")
        _record.HeatTreat = If(lastRecord IsNot Nothing, lastRecord.HeatTreat, "")
        _record.HotRush = If(lastRecord IsNot Nothing, lastRecord.HotRush, "")
        _record.HandleWithCare = "YES"
        ' Copy all section carry-forward from last card (DOS line 1400: A$(7,1), A$(7,2) etc.)
        If lastRecord IsNot Nothing Then
            _record.CopySectionsFrom(lastRecord)
        End If
    End Sub

    ' ── Layout ───────────────────────────────────────────────────────────────────
    Private Sub InitializeLayout()
        Me.Text = "ShopCard Generator"
        Me.BackColor = Color.Black
        Me.ForeColor = Color.Yellow
        Me.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        Me.Size = New Size(1024, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.KeyPreview = True
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False

        ' ── Full-window terminal panel ──────────────────────────────────────────
        ' Everything is drawn onto pnlScroll as a terminal surface.
        ' Title at top, underline below, then transcript lines, then active prompt.

        pnlScroll.Dock = DockStyle.Fill
        pnlScroll.BackColor = Color.Black
        pnlScroll.Padding = New Padding(0)
        pnlScroll.AutoScroll = False

        ' Title — matches DosMenuFormBase global title style (Castellar 28pt Bold)
        lblTitle.Text = "SHOPCARD GENERATOR"
        UiTheme.ApplyDosTitleStyle(lblTitle)
        lblTitle.Font = New Font("Castellar", 34.0F, FontStyle.Bold, GraphicsUnit.Point)  ' slightly larger than base 28pt to fill width
        lblTitle.AutoSize = False
        lblTitle.Dock = DockStyle.None
        lblTitle.TextAlign = ContentAlignment.MiddleLeft
        lblTitle.Height = 56
        lblTitle.Left = 8
        lblTitle.Top = 14          ' pushed down so double-line above is visible
        lblTitle.Width = 840

        ' Double line ABOVE title (Top=4 and Top=8, both clear of window chrome)
        Dim pnlTopLine1 As New Panel()
        pnlTopLine1.BackColor = Color.Yellow
        pnlTopLine1.Height = 1 : pnlTopLine1.Left = 0 : pnlTopLine1.Top = 4 : pnlTopLine1.Width = 860
        pnlTopLine1.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top

        Dim pnlTopLine2 As New Panel()
        pnlTopLine2.BackColor = Color.Yellow
        pnlTopLine2.Height = 1 : pnlTopLine2.Left = 0 : pnlTopLine2.Top = 8 : pnlTopLine2.Width = 860
        pnlTopLine2.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top

        ' Double line BELOW title (title bottom = 14+56=70, lines at 74 and 78)
        Dim pnlLine As New Panel()
        pnlLine.BackColor = Color.Yellow
        pnlLine.Height = 1 : pnlLine.Left = 0 : pnlLine.Top = 74 : pnlLine.Width = 860
        pnlLine.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top

        Dim pnlLine2 As New Panel()
        pnlLine2.BackColor = Color.Yellow
        pnlLine2.Height = 1 : pnlLine2.Left = 0 : pnlLine2.Top = 78 : pnlLine2.Width = 860
        pnlLine2.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top

        ' Customer tag prefix "Making shopcard for " — white, right side
        lblCustomerTag.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        lblCustomerTag.ForeColor = Color.White
        lblCustomerTag.BackColor = Color.Black
        lblCustomerTag.AutoSize = True
        lblCustomerTag.Text = "Making shopcard for "
        lblCustomerTag.Visible = False
        lblCustomerTag.Top = 90

        ' Customer name — black text on white highlight (DOS inverse video)
        lblCustomerTagName.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        lblCustomerTagName.ForeColor = Color.Black
        lblCustomerTagName.BackColor = Color.White
        lblCustomerTagName.AutoSize = True
        lblCustomerTagName.Padding = New Padding(2, 0, 2, 0)
        lblCustomerTagName.Text = ""
        lblCustomerTagName.Visible = False
        lblCustomerTagName.Top = 90

        ' Transcript — answered lines stacked from top, starting below title+line
        lblTranscript.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        lblTranscript.ForeColor = Color.White
        lblTranscript.BackColor = Color.Black
        lblTranscript.AutoSize = True
        lblTranscript.MaximumSize = New Size(980, 0)
        lblTranscript.Left = 8
        lblTranscript.Top = 108
        lblTranscript.Text = ""
        lblTranscript.UseCompatibleTextRendering = True
        lblTranscript.UseMnemonic = False  ' prevent & from being eaten as a keyboard shortcut prefix

        ' Hint label — appears just above the active prompt
        lblHint.Font = New Font("Consolas", 10, FontStyle.Regular, GraphicsUnit.Point)
        lblHint.ForeColor = Color.Cyan
        lblHint.BackColor = Color.Black
        lblHint.AutoSize = True
        lblHint.Left = 8
        lblHint.Top = 88   ' repositioned dynamically

        ' Prompt label — the current question
        lblPrompt.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        lblPrompt.ForeColor = Color.Yellow
        lblPrompt.BackColor = Color.Black
        lblPrompt.AutoSize = True
        lblPrompt.Left = 8
        lblPrompt.Top = 106   ' repositioned dynamically

        ' Input textbox — inline after prompt, no border, looks like cursor
        txtInput.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        txtInput.BackColor = Color.Black
        txtInput.ForeColor = Color.White
        txtInput.BorderStyle = BorderStyle.None
        txtInput.Width = 220
        txtInput.Left = 8
        txtInput.Top = 106   ' repositioned dynamically — will be set to lblPrompt.Top in RepositionPrompt
        txtInput.MaxLength = 40
        AddHandler txtInput.TextChanged, AddressOf OnInputTextChanged

        ' Underscore placeholder — DOS shows "________" after prompt, replaced as user types
        lblInputUnderline.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        lblInputUnderline.ForeColor = Color.White
        lblInputUnderline.BackColor = Color.Black
        lblInputUnderline.AutoSize = True
        lblInputUnderline.Text = New String("_"c, 10)
        lblInputUnderline.Left = 8
        lblInputUnderline.Top = 106

        ' [+ = ] hint label — positioned after input underline dynamically
        lblPlusHint.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        lblPlusHint.ForeColor = Color.White
        lblPlusHint.BackColor = Color.Black
        lblPlusHint.AutoSize = True
        lblPlusHint.Text = " [+ = ]"
        lblPlusHint.Visible = False
        lblPlusHint.BringToFront()

        ' Material list panel
        pnlMaterialList.BackColor = Color.Black
        pnlMaterialList.Left = 8
        pnlMaterialList.Size = New Size(840, 90)
        pnlMaterialList.Visible = False

        lblMaterialList.Font = New Font("Consolas", 10, FontStyle.Regular, GraphicsUnit.Point)
        lblMaterialList.ForeColor = Color.White
        lblMaterialList.BackColor = Color.Black
        lblMaterialList.AutoSize = True
        lblMaterialList.Location = New Point(0, 0)
        pnlMaterialList.Controls.Add(lblMaterialList)

        ' White "?" suffix — shown after cyan inline hints so the ? is white not cyan
        lblPromptSuffix.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        lblPromptSuffix.ForeColor = Color.White
        lblPromptSuffix.BackColor = Color.Black
        lblPromptSuffix.AutoSize = True
        lblPromptSuffix.Text = "?"
        lblPromptSuffix.Visible = False

        ' Second hint line — used for [H = ] above Heat Treat prompt
        lblHint2.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        lblHint2.ForeColor = Color.Cyan
        lblHint2.BackColor = Color.Black
        lblHint2.AutoSize = True
        lblHint2.Visible = False

        ' Material confirm line — black-on-white highlight (DOS inverse video), shown after material is picked
        lblMaterialConfirm.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        lblMaterialConfirm.ForeColor = Color.Black
        lblMaterialConfirm.BackColor = Color.White
        lblMaterialConfirm.AutoSize = True
        lblMaterialConfirm.Padding = New Padding(2, 0, 2, 0)
        lblMaterialConfirm.Visible = False
        lblMaterialConfirm.Left = 8

        ' Summary panel
        pnlSummary.BackColor = Color.Black
        pnlSummary.Visible = False
        pnlSummary.Left = 0
        pnlSummary.Top = 0
        pnlSummary.Size = Me.ClientSize

        pnlScroll.Controls.Add(lblTitle)
        pnlScroll.Controls.Add(pnlTopLine1)
        pnlScroll.Controls.Add(pnlTopLine2)
        pnlScroll.Controls.Add(pnlLine)
        pnlScroll.Controls.Add(pnlLine2)
        pnlScroll.Controls.Add(lblCustomerTag)
        pnlScroll.Controls.Add(lblCustomerTagName)
        pnlScroll.Controls.Add(lblTranscript)
        pnlScroll.Controls.Add(lblMaterialConfirm)
        pnlScroll.Controls.Add(lblHint2)
        pnlScroll.Controls.Add(lblHint)
        pnlScroll.Controls.Add(lblPrompt)
        pnlScroll.Controls.Add(lblPromptSuffix)
        pnlScroll.Controls.Add(lblInputUnderline)
        pnlScroll.Controls.Add(txtInput)
        pnlScroll.Controls.Add(lblPlusHint)
        pnlScroll.Controls.Add(pnlMaterialList)
        pnlScroll.Controls.Add(pnlSummary)

        Me.Controls.Add(pnlScroll)
    End Sub

    ' ── Load ─────────────────────────────────────────────────────────────────────
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        ShowStep(_startStep)
        txtInput.Focus()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        If _record.CustomerName <> "" Then UpdateCustomerTag()
        RepositionPrompt()
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        PositionCustomerTag()
        RepositionPrompt()
    End Sub

    Private Sub PositionCustomerTag()
        If lblCustomerTag.Visible Then
            ' Right-align: name label flush right, prefix label immediately left of it
            Dim nameLeft As Integer = pnlScroll.ClientSize.Width - lblCustomerTagName.Width - 8
            lblCustomerTagName.Left = nameLeft
            lblCustomerTag.Left = nameLeft - lblCustomerTag.Width
        End If
    End Sub

    Private Sub UpdateCustomerTag()
        If _record.CustomerName <> "" Then
            lblCustomerTag.Text = "Making shopcard for "
            lblCustomerTagName.Text = _record.CustomerName
            lblCustomerTag.Top = 90
            lblCustomerTagName.Top = 90
            lblCustomerTag.Visible = True
            lblCustomerTagName.Visible = True
            PositionCustomerTag()
        Else
            lblCustomerTag.Visible = False
        End If
    End Sub

    ''' <summary>
    ''' Repositions hint, prompt, material list, and input directly below the last transcript line.
    ''' This gives the DOS terminal "grow from top" feel.
    ''' </summary>
    Private Sub RepositionPrompt()
        Dim transcriptBottom As Integer = lblTranscript.Top + lblTranscript.Height + 8

        ' If material confirm label is visible, it sits right below transcript
        If lblMaterialConfirm.Visible Then
            lblMaterialConfirm.Top = transcriptBottom
            transcriptBottom = lblMaterialConfirm.Top + lblMaterialConfirm.Height + 4
        End If

        ' lblHint2 = above-line hint (e.g. [H = ] for HeatTreat), positioned BEFORE the prompt row
        If lblHint2.Visible Then
            lblHint2.Top = transcriptBottom
            ' Will be left-aligned to lblHint (inline) start after prompt is placed; defer until below
            transcriptBottom = lblHint2.Top + lblHint2.Height + 2
        End If

        If lblHint.Text <> "" Then
            If _hintInline Then
                ' Same row as prompt — hint appears to the right of prompt text
                lblHint.Top = transcriptBottom   ' will align with lblPrompt set below
                lblHint.Visible = True
                ' Don't advance transcriptBottom — hint shares the prompt row
            Else
                lblHint.Top = transcriptBottom
                ' For JobRoute: align hint LEFT to where the underline starts
                If _currentStep = EntryStep.JobRoute Then
                    lblHint.Left = lblPrompt.Left + lblPrompt.Width + 4
                Else
                    lblHint.Left = 8
                End If
                lblHint.Visible = True
                transcriptBottom = lblHint.Top + lblHint.Height + 2
            End If
        Else
            lblHint.Visible = False
        End If

        lblPrompt.Top = transcriptBottom

        ' For inline hints: position hint on same row as prompt, after prompt text
        If _hintInline AndAlso lblHint.Visible Then
            lblHint.Top = lblPrompt.Top + (lblPrompt.Height - lblHint.Height) \ 2
            lblHint.Left = lblPrompt.Left + lblPrompt.Width
        End If

        ' White ? suffix — sits right after the inline hint (or after prompt if no inline hint)
        If lblPromptSuffix.Text <> "" Then
            Dim suffixLeft As Integer
            If _hintInline AndAlso lblHint.Visible Then
                suffixLeft = lblHint.Left + lblHint.Width
            Else
                suffixLeft = lblPrompt.Left + lblPrompt.Width
            End If
            lblPromptSuffix.Left = suffixLeft
            lblPromptSuffix.Top = lblPrompt.Top + (lblPrompt.Height - lblPromptSuffix.Height) \ 2
            lblPromptSuffix.Visible = True
        End If

        ' Align lblHint2 ([H = ]) left edge to match where lblHint ([Enter = NONE]) starts
        If lblHint2.Visible AndAlso _hintInline AndAlso lblHint.Visible Then
            lblHint2.Left = lblHint.Left
        End If

        If pnlMaterialList.Visible Then
            ' Material list goes below hint (HELPFUL HINTS), prompt goes BELOW the list
            pnlMaterialList.Top = transcriptBottom + lblHint.Height + 4
            ' Prompt sits below the list
            lblPrompt.Top = pnlMaterialList.Top + pnlMaterialList.Height + 6
            ' Underline inline with prompt
            Dim matInputTop As Integer = lblPrompt.Top + (lblPrompt.Height - txtInput.Height) \ 2
            Dim matInputLeft As Integer = lblPrompt.Left + lblPrompt.Width + 4
            txtInput.Top = matInputTop
            txtInput.Left = matInputLeft
            lblInputUnderline.Top = matInputTop + (txtInput.Height - lblInputUnderline.Height) \ 2
            lblInputUnderline.Left = matInputLeft
            lblInputUnderline.Visible = True
        Else
            ' Inline: same row as prompt, immediately after suffix (or hint, or prompt)
            Dim inputTop As Integer = lblPrompt.Top + (lblPrompt.Height - txtInput.Height) \ 2
            Dim inputLeft As Integer
            If lblPromptSuffix.Visible Then
                inputLeft = lblPromptSuffix.Left + lblPromptSuffix.Width + 4
            ElseIf _hintInline AndAlso lblHint.Visible Then
                inputLeft = lblHint.Left + lblHint.Width + 4
            Else
                inputLeft = lblPrompt.Left + lblPrompt.Width + 4
            End If
            txtInput.Top = inputTop
            txtInput.Left = inputLeft
            ' Underscores sit behind the textbox
            lblInputUnderline.Top = inputTop + (txtInput.Height - lblInputUnderline.Height) \ 2
            lblInputUnderline.Left = inputLeft
            lblInputUnderline.Visible = True
            ' [+ = ] / (for MAG ONLY!) sits to the right of the input box
            Dim hintH As Integer = If(lblPlusHint.Height > 0, lblPlusHint.Height, txtInput.Height)
            lblPlusHint.Top = inputTop + (txtInput.Height - hintH) \ 2
            lblPlusHint.Left = inputLeft + txtInput.Width + 4
            lblPlusHint.Visible = _showPlusHint
            lblPlusHint.BringToFront()
        End If
    End Sub

    Private Sub OnInputTextChanged(sender As Object, e As EventArgs)
        ' Shrink underscores from left as characters are typed, like DOS overwrite style
        Dim typed As Integer = txtInput.Text.Length
        Dim remaining As Integer = Math.Max(0, Math.Min(20, 20 - typed))
        lblInputUnderline.Text = New String("_"c, remaining)
        lblInputUnderline.Left = txtInput.Left + typed * 9   ' approx Consolas 11pt char width
        lblInputUnderline.Visible = remaining > 0
        ' [+ = ] stays fixed to right of textbox (not the shrinking underline)
        If _showPlusHint Then
            lblPlusHint.Left = txtInput.Left + txtInput.Width + 4
        End If
    End Sub

    ' ── Step driver ──────────────────────────────────────────────────────────────
    Private _showPlusHint As Boolean = False
    Private _hintInline As Boolean = False  ' True = hint on same row as prompt (after it)

    Private Sub ShowStep(nextStep As EntryStep)
        _currentStep = nextStep
        lblHint.Text = ""
        txtInput.Text = ""
        txtInput.MaxLength = 40
        txtInput.Width = 220   ' reset each step; expanded for [+ = ] steps in RepositionPrompt
        pnlMaterialList.Visible = False
        txtInput.Visible = True
        lblInputUnderline.Text = New String("_"c, 20)
        lblInputUnderline.Visible = True
        lblInputUnderline.ForeColor = Color.White  ' default; CustomerName step overrides to yellow
        _showPlusHint = False
        _hintInline = False
        lblPrompt.ForeColor = Color.Yellow  ' default; overridden per-step as needed
        lblHint.ForeColor = Color.Cyan       ' default hint color
        lblPlusHint.Text = " [+ = ]"         ' reset; HeatTreat overrides to (for MAG ONLY!)
        lblPlusHint.ForeColor = Color.White    ' reset; HeatTreat overrides to Red
        lblPromptSuffix.Visible = False
        lblPromptSuffix.Text = ""
        lblHint2.Visible = False
        ' Hide material confirm unless we are on HeatTreat or later (it stays visible once set)
        If nextStep < EntryStep.HeatTreat Then lblMaterialConfirm.Visible = False  ' stays visible from HeatTreat onward (ConditionReceived, HotRush, Summary)

        Select Case nextStep
            Case EntryStep.CustomerName
                lblInputUnderline.ForeColor = Color.Yellow
                lblPrompt.Text = "Enter Customers Name" &
                    If(ShopCardSession.LastCustomerName <> "",
                       " [ENTER = " & ShopCardSession.LastCustomerName & "]", "") & "  ?"

            Case EntryStep.CustomerNameNotFound
                ' DOS line 670: "KIRKY does not exist...  Would you like to see a list (Y/N)?"
                lblInputUnderline.Visible = False
                txtInput.Visible = False
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = _pendingBadName & " does not exist...  Would you like to see a list (Y/N)?"

            Case EntryStep.CustomerNameList
                ' DOS line 710: show matching PRC file names, wait for ENTER
                lblInputUnderline.Visible = False
                txtInput.Visible = False
                Dim letter As String = If(_pendingBadName.Length > 0, _pendingBadName.Substring(0, 1), "")
                Dim listText As String = BuildPrcListText(letter)
                ' Append formatted list to transcript (DOS terminal scroll analog)
                ' Do NOT also set lblPrompt to the list — that causes double-render and overflows
                AppendTranscript(listText)
                AppendTranscript("Hit [ENTER]")
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = "Hit [ENTER] to continue"

            Case EntryStep.EntryDate
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = "Enter date"
                lblHint.Text = "  [ENTER = " & Format(Now, "MM-dd-yyyy") & "]  [C = Change Customers Name]"
                lblHint.ForeColor = Color.Cyan
                _hintInline = True
                lblPromptSuffix.Text = "  ?"
                lblInputUnderline.Visible = False

            Case EntryStep.PONumber
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = "P.O. #"
                lblHint.Text = "  [ENTER =]"
                lblHint.ForeColor = Color.Cyan
                _hintInline = True
                lblPromptSuffix.Text = "?"
                txtInput.MaxLength = 30
                _showPlusHint = True

            Case EntryStep.NumberOfPans
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = "Enter number of pans"
                lblHint.Text = "  [ENTER = none]"
                lblHint.ForeColor = Color.Cyan
                _hintInline = True
                lblPromptSuffix.Text = "  ?"
                txtInput.MaxLength = 10
                _showPlusHint = True

            Case EntryStep.NumberOfBoxes
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = "Enter number of boxes"
                lblHint.Text = "  [ENTER = none]"
                lblHint.ForeColor = Color.Cyan
                _hintInline = True
                lblPromptSuffix.Text = "  ?"
                txtInput.MaxLength = 10
                _showPlusHint = True

            Case EntryStep.NumberOfCrates
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = "Enter number of Crates"
                lblHint.Text = "  [ENTER = none]"
                lblHint.ForeColor = Color.Cyan
                _hintInline = True
                lblPromptSuffix.Text = "  ?"
                txtInput.MaxLength = 10
                _showPlusHint = True

            Case EntryStep.Weight
                lblHint.Text = "  [-2 = none]   [-1 = Enter Boxes or Crates]"
                lblHint.ForeColor = Color.Cyan
                _hintInline = True
                lblPromptSuffix.Text = "  ?"
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = "Enter weight"
                txtInput.MaxLength = 10
                _showPlusHint = False

            Case EntryStep.Quantity
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = "Enter quantity received or ordered  ?"
                txtInput.MaxLength = 10
                _showPlusHint = True

            Case EntryStep.PartNumber
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = "Enter Part# && name  ?"
                txtInput.MaxLength = 35
                _showPlusHint = True

            Case EntryStep.JobRoute
                lblHint.Text = "Misc. info in invoice body or Job #"
                lblHint.ForeColor = Color.White
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = "Job or Route number  ?"
                txtInput.MaxLength = 43
                txtInput.Width = 644   ' 500 + 16 chars * ~9px
                lblInputUnderline.Text = New String("_"c, 36)
                _showPlusHint = False

            Case EntryStep.Material
                ShowMaterialStep()
                Return

            Case EntryStep.HeatTreat
                ' [H = last] on its own line above, aligned to where [Enter = NONE] starts
                lblHint2.Text = "[H = " & ShopCardSession.LastHeatTreat & "]"
                lblHint2.ForeColor = Color.Cyan
                lblHint2.Visible = True
                ' Prompt = white "Enter Heat Treat", hint inline = cyan "[Enter = NONE]", suffix = white "  ?"
                lblPrompt.ForeColor = Color.White
                lblPrompt.Text = "Enter Heat Treat "
                lblHint.Text = "[Enter = NONE]"
                lblHint.ForeColor = Color.Cyan
                _hintInline = True
                lblPromptSuffix.Text = "  ?"
                txtInput.MaxLength = 22
                ' (for MAG ONLY!) shown to the right of the underline — red as a warning
                lblPlusHint.Text = "  (for MAG ONLY!)"
                lblPlusHint.ForeColor = Color.Yellow
                _showPlusHint = True

            Case EntryStep.HotRush
                lblPrompt.Text = "Hot Rush (Y/N)"
                txtInput.Visible = False   ' single keypress, no text box needed

            Case EntryStep.Summary
                ShowSummary()
                Return
        End Select

        txtInput.Focus()
        RepositionPrompt()
    End Sub

    Private Sub ShowMaterialStep()
        ' Build the hint text matching DOS layout
        Dim lines As String = BuildMaterialHintText()
        lblHint.Text = "HELPFUL HINTS:" & vbCrLf &
            "Chem Film & Anodize = Alum    PASS = Stainless Steel    Most MAG = Alloy Steel or 8740" & vbCrLf &
            "Western Cyln:    Pene = Alum    Mag = Alloy Steel"

        lblMaterialList.Text = lines
        pnlMaterialList.Visible = True
        pnlMaterialList.Size = New Size(980, 110)

        lblPrompt.ForeColor = Color.Yellow
        lblPrompt.Text = "Enter material #  ?"
        txtInput.MaxLength = 22
        txtInput.Width = 220
        txtInput.Visible = True
        lblInputUnderline.Text = New String("_"c, 20)
        lblInputUnderline.ForeColor = Color.White
        txtInput.Focus()
        RepositionPrompt()
    End Sub

    Private Function BuildMaterialHintText() As String
        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine("(1) A-286            (2) Alloy Steel    (3) Stellite #6    (4) Alum    (5) 8740    (6) 6-4Ti")
        sb.AppendLine("(7) Stainless Steel                     (8) Alloy          (9) Beryl-CU    (G) GRK. ASC.")
        sb.AppendLine("(0) 17-4PH           (P) 15-5PH         (I) INCO-718       (C) 440C        (A) Alloy #6")
        sb.AppendLine("(W) WASP             (D) 356-T6         (E) 2024-T851      (F) 7075-T851")
        Dim lastLine As String = "(H) 2014-T6          (T) 7075-T73       (U) ARP 2000"
        If ShopCardSession.LastMaterial <> "" Then
            lastLine &= "  (L) " & ShopCardSession.LastMaterial
        End If
        sb.Append(lastLine)
        Return sb.ToString()
    End Function

    ' ── Summary screen ───────────────────────────────────────────────────────────
    Private Sub ShowSummary()
        pnlScroll.Visible = False
        pnlPrompt.Visible = False
        pnlSummary.Visible = True
        pnlSummary.Controls.Clear()

        Dim fields As New List(Of (String, String)) From {
            ("Customers name:", _record.CustomerName),
            ("Date:", _record.EntryDate),
            ("P.O. #:", _record.PONumber),
            ("Number of pans:", _record.NumberOfPans),
            ("Number of boxes:", _record.NumberOfBoxes),
            ("Number of crates:", _record.NumberOfCrates),
            ("Weight:", _record.Weight),
            ("Quantity received or ordered:", _record.QuantityReceived),
            ("P/N & name:", _record.PartNumberAndName),
            ("Job or Route #:", _record.JobRouteNumber),
            ("Material #:", _record.Material),
            ("Heat treat:", _record.HeatTreat),
            ("Condition received:", _record.ConditionReceived),
            ("Hot rush:", _record.HotRush),
            ("Handle with care:", _record.HandleWithCare)
        }

        Dim y As Integer = 12
        For Each field In fields
            Dim lbl As New Label()
            lbl.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
            lbl.ForeColor = Color.White
            lbl.BackColor = Color.Black
            lbl.AutoSize = True
            lbl.Location = New Point(16, y)
            lbl.Text = field.Item1.PadRight(32) & field.Item2
            pnlSummary.Controls.Add(lbl)
            y += 22
        Next

        ' Customer name — black text on white highlight (DOS inverse video)
        Dim lblCust As New Label()
        lblCust.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        lblCust.ForeColor = Color.Black
        lblCust.BackColor = Color.White
        lblCust.AutoSize = True
        lblCust.Padding = New Padding(4, 2, 4, 2)
        lblCust.Text = "Customers name:  " & _record.CustomerName
        lblCust.Location = New Point(16, 8)
        ' Shift all other labels down
        For Each ctrl As Control In pnlSummary.Controls
            ctrl.Location = New Point(ctrl.Location.X, ctrl.Location.Y + 30)
        Next
        pnlSummary.Controls.Add(lblCust)

        ' Bottom confirm line — drawn on a custom-paint panel so segments butt together perfectly
        Dim confirmYPos As Integer = y + 36
        Dim confirmSegments As New List(Of (String, Color, Boolean)) From {
            ("Is this information correct (", Color.White, False),
            ("Y", Color.Lime, True),
            ("/", Color.White, False),
            ("N", Color.Red, True),
            (")     (", Color.White, False),
            ("M", Color.Yellow, True),
            (")odify Material or Heat Treat", Color.White, False)
        }

        Dim pnlConfirm As New Panel()
        pnlConfirm.BackColor = Color.Black
        pnlConfirm.Location = New Point(16, confirmYPos)
        pnlConfirm.Size = New Size(900, 24)

        AddHandler pnlConfirm.Paint, Sub(s, pe)
            Dim x As Integer = 0
            For Each seg In confirmSegments
                Dim f As New Font("Consolas", 11, If(seg.Item3, FontStyle.Bold, FontStyle.Regular), GraphicsUnit.Point)
                Dim sz As Size = TextRenderer.MeasureText(pe.Graphics, seg.Item1, f, New Size(Integer.MaxValue, Integer.MaxValue), TextFormatFlags.NoPadding Or TextFormatFlags.SingleLine)
                TextRenderer.DrawText(pe.Graphics, seg.Item1, f, New Point(x, 0), seg.Item2, TextFormatFlags.NoPadding Or TextFormatFlags.SingleLine)
                x += sz.Width
                f.Dispose()
            Next
        End Sub

        pnlSummary.Controls.Add(pnlConfirm)

        pnlSummary.Dock = DockStyle.Fill
        Me.Controls.Add(pnlSummary)
        pnlSummary.BringToFront()
        pnlSummary.Focus()
    End Sub

    ' ── Keyboard handling ────────────────────────────────────────────────────────
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)

        If _currentStep = EntryStep.CustomerNameNotFound Then
            ' DOS line 680/690: Y -> show list, N -> back to name prompt; anything else loops
            Dim ch As String = KeyCodeToChar(e.KeyCode).ToUpper()
            Select Case ch
                Case "Y"
                    AppendTranscript("? Y")
                    e.Handled = True
                    ShowStep(EntryStep.CustomerNameList)
                Case "N"
                    AppendTranscript("? N")
                    e.Handled = True
                    ShowStep(EntryStep.CustomerName)
            End Select
            Return
        End If

        If _currentStep = EntryStep.CustomerNameList Then
            ' DOS line 710: after list is shown, any ENTER returns to name prompt
            If e.KeyCode = Keys.Return Then
                e.Handled = True
                ShowStep(EntryStep.CustomerName)
            End If
            Return
        End If

        If _currentStep = EntryStep.HotRush Then
            Select Case e.KeyCode
                Case Keys.Y
                    CommitHotRush("YES")
                    e.Handled = True
                Case Keys.N
                    CommitHotRush("NO")
                    e.Handled = True
            End Select
            Return
        End If

        If _currentStep = EntryStep.Summary Then
            Dim ch As String = KeyCodeToChar(e.KeyCode)
            Select Case ch.ToUpper()
                Case "Y"
                    _accepted = True
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                    e.Handled = True
                Case "N"
                    ' Return to ShopCard menu (same as DOS behaviour)
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                    e.Handled = True
                Case "M"
                    ' Go back to material/heat treat — clear transcript entries from material onward
                    ' Remove Material #, Heat Treat, Hot Rush lines if present
                    Do While _transcriptLines.Count > 0 AndAlso
                             (_transcriptLines(_transcriptLines.Count - 1).StartsWith("Hot Rush") OrElse
                              _transcriptLines(_transcriptLines.Count - 1).StartsWith("Enter Heat Treat") OrElse
                              _transcriptLines(_transcriptLines.Count - 1).StartsWith("Material"))
                        _transcriptLines.RemoveAt(_transcriptLines.Count - 1)
                    Loop
                    UpdateTranscriptDisplay()
                    lblMaterialConfirm.Visible = False
                    _record.Material = ""
                    _record.HeatTreat = ""
                    _record.HotRush = ""
                    pnlSummary.Visible = False
                    pnlScroll.Visible = True
                    ShowStep(EntryStep.Material)
                    e.Handled = True
            End Select
            Return
        End If

        If e.KeyCode = Keys.Escape Then
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Protected Overrides Sub OnKeyPress(e As KeyPressEventArgs)
        MyBase.OnKeyPress(e)
        If e.KeyChar = ChrW(Keys.Return) Then
            CommitCurrentStep()
            e.Handled = True
        End If
    End Sub

    Private Function KeyCodeToChar(k As Keys) As String
        Try
            Return ChrW(CInt(k)).ToString()
        Catch
            Return ""
        End Try
    End Function

    ' ── Commit each step ────────────────────────────────────────────────────────
    Private Sub CommitCurrentStep()
        Dim input As String = txtInput.Text.Trim()

        Select Case _currentStep

            Case EntryStep.CustomerName
                If input = "" Then
                    input = ShopCardSession.LastCustomerName
                End If
                If input = "" Then
                    FlashPrompt("Please enter a customer name.")
                    Return
                End If
                ' DOS line 380: validate customer name against PRC\NAME.PRC file
                Dim prcPath As String = IO.Path.Combine(ShopCardSession.DataFolder, "PRC", input.ToUpper() & ".PRC")
                If Not IO.File.Exists(prcPath) Then
                    ' DOS line 670: name does not exist -> show Y/N prompt inline
                    _pendingBadName = input.ToUpper()
                    AppendTranscript("Enter Customers Name  ?  " & input.ToUpper())
                    ShowStep(EntryStep.CustomerNameNotFound)
                    Return
                End If
                _record.CustomerName = input.ToUpper()
                ShopCardSession.LastCustomerName = input.ToUpper()
                UpdateCustomerTag()
                AppendTranscript("Enter Customers Name  ?  " & input.ToUpper())
                If _nameOnly Then
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                    Return
                End If
                ShowStep(EntryStep.EntryDate)

            Case EntryStep.CustomerNameNotFound
                ' DOS line 680/690: Y -> show list, N -> back to name prompt
                Select Case input.ToUpper()
                    Case "Y"
                        AppendTranscript("? Y")
                        ShowStep(EntryStep.CustomerNameList)
                    Case "N"
                        AppendTranscript("? N")
                        ShowStep(EntryStep.CustomerName)
                    Case Else
                        ' Loop — no action, stay on this step
                End Select

            Case EntryStep.CustomerNameList
                ' DOS line 710: after ENTER, return to name prompt
                AppendTranscript("")
                ShowStep(EntryStep.CustomerName)

            Case EntryStep.EntryDate
                If input.ToUpper() = "C" Then
                    ' Change customer name — go back
                    _transcriptLines.Clear()
                    UpdateTranscriptDisplay()
                    _record.CustomerName = ""
                    UpdateCustomerTag()
                    ShowStep(EntryStep.CustomerName)
                    Return
                End If
                If input = "" Then input = Format(Now, "MM-dd-yyyy")
                _record.EntryDate = input
                AppendTranscript("Enter date  ?  " & input)
                ShowStep(EntryStep.PONumber)

            Case EntryStep.PONumber
                _record.PONumber = input
                AppendTranscript("P.O.#  = " & input)
                ShowStep(EntryStep.NumberOfPans)

            Case EntryStep.NumberOfPans
                _record.NumberOfPans = input   ' blank = none
                AppendTranscript("Enter number of pans  ?  " & If(input = "", "(none)", input))
                If input = "" Then
                    ShowStep(EntryStep.NumberOfBoxes)
                Else
                    ShowStep(EntryStep.Weight)
                End If

            Case EntryStep.NumberOfBoxes
                _record.NumberOfBoxes = input
                AppendTranscript("Enter number of boxes  ?  " & If(input = "", "(none)", input))
                ShowStep(EntryStep.NumberOfCrates)

            Case EntryStep.NumberOfCrates
                _record.NumberOfCrates = input
                AppendTranscript("Enter number of Crates  ?  " & If(input = "", "(none)", input))
                ShowStep(EntryStep.Weight)

            Case EntryStep.Weight
                If input = "-2" OrElse input = "" Then
                    _record.Weight = ""
                    AppendTranscript("Enter weight  ?  (none)")
                    ShowStep(EntryStep.Quantity)
                ElseIf input = "-1" Then
                    ' Boxes/Crates sub-flow — placeholder for now
                    _record.Weight = ""
                    AppendTranscript("Enter weight  ?  (Boxes or Crates)")
                    InputBoxesOrCrates()
                Else
                    _record.Weight = input
                    AppendTranscript("Enter weight  ?  " & input)
                    ShowStep(EntryStep.Quantity)
                End If

            Case EntryStep.Quantity
                _record.QuantityReceived = input
                AppendTranscript("Enter quantity received or ordered  ?  " & input)
                ShowStep(EntryStep.PartNumber)

            Case EntryStep.PartNumber
                If input.Length > 35 Then
                    FlashPrompt("Part# & name too long (max 35 characters).")
                    Return
                End If
                _record.PartNumberAndName = input
                AppendTranscript("Enter Part# & name  ?  " & input)
                ' DOS line 1460: IF JUST=1 THEN 1800 — skip JobRoute/Material/HeatTreat/HotRush, go to summary
                If _justMode Then
                    ' DOS line 1450: if last Mag/Pene A$(7,2) exists, replace with new QR$
                    If _record.GetSection(7, 2) <> "" Then
                        _record.SetSection(7, 2, _record.QuantityReceived)
                    End If
                    ShowStep(EntryStep.Summary)
                Else
                    ShowStep(EntryStep.JobRoute)
                End If

            Case EntryStep.JobRoute
                ' Append job/route to part number if entered
                If input <> "" Then
                    _record.JobRouteNumber = input
                    _record.PartNumberAndName = _record.PartNumberAndName & " " & input
                    AppendTranscript("Job or Route number  ?  " & input)
                    AppendTranscript("Part# & name changed to: " & _record.PartNumberAndName)
                Else
                    AppendTranscript("Job or Route number  ?  (none)")
                End If
                ShowStep(EntryStep.Material)

            Case EntryStep.Material
                CommitMaterial(input)

            Case EntryStep.HeatTreat
                If input.ToUpper() = "H" Then
                    input = ShopCardSession.LastHeatTreat
                End If
                _record.HeatTreat = input
                If input <> "" Then ShopCardSession.LastHeatTreat = input
                AppendTranscript("Enter Heat Treat  ?  " & If(input = "", "(none)", input))
                ShowStep(EntryStep.HotRush)

        End Select
    End Sub

    Private Sub CommitMaterial(input As String)
        Dim resolved As String = input

        ' Check (L) carry-forward
        If input.ToUpper() = "L" AndAlso ShopCardSession.LastMaterial <> "" Then
            resolved = ShopCardSession.LastMaterial
        ElseIf MaterialShortcuts.ContainsKey(input) Then
            resolved = MaterialShortcuts(input)
        End If

        If resolved.Length < 2 Then
            FlashPrompt("Please enter a valid material selection.")
            Return
        End If

        _record.Material = resolved
        ShopCardSession.LastMaterial = resolved

        ' Show confirmed material as black-on-white highlighted line (DOS inverse video)
        lblMaterialConfirm.Text = "Material #:  [" & resolved & "]"
        lblMaterialConfirm.Visible = True
        pnlMaterialList.Visible = False
        ShowStep(EntryStep.HeatTreat)
    End Sub

    Private Sub CommitHotRush(value As String)
        _record.HotRush = value
        ' Handle with care = YES always (matches S.ASC line 1800: HWC$ = "YES")
        _record.HandleWithCare = "YES"
        AppendTranscript("Hot Rush  ?  " & value)
        ShowStep(EntryStep.Summary)
    End Sub

    Private Sub InputBoxesOrCrates()
        ShowStep(EntryStep.NumberOfBoxes)
    End Sub

    Private Sub RestartHeader()
        _transcriptLines.Clear()
        UpdateTranscriptDisplay()
        lblMaterialConfirm.Visible = False
        _record = New ShopCardRecord()
        pnlSummary.Visible = False
        pnlScroll.Visible = True
        pnlPrompt.Visible = True
        UpdateCustomerTag()
        ShowStep(EntryStep.CustomerName)
    End Sub

    ' ── Transcript display ───────────────────────────────────────────────────────
    Private Sub AppendTranscript(line As String)
        _transcriptLines.Add(line)
        UpdateTranscriptDisplay()
        RepositionPrompt()
    End Sub

    Private Sub UpdateTranscriptDisplay()
        ' Reserve enough vertical space for: material hints (~55px) + material list (~120px) +
        ' prompt row (~24px) + material confirm (~24px) + padding (~30px) = ~253px minimum.
        ' Transcript starts at Top=108; available height = clientHeight - 108 - 253.
        Const ReservedBottom As Integer = 260
        Dim lineHeight As Integer = lblTranscript.Font.Height + 2  ' ~22px at Consolas 11pt
        Dim availableHeight As Integer = pnlScroll.ClientSize.Height - lblTranscript.Top - ReservedBottom
        Dim maxLines As Integer = Math.Max(1, availableHeight \ lineHeight)

        ' Show only the most recent maxLines lines (terminal scroll behaviour)
        Dim visible As IEnumerable(Of String) = _transcriptLines
        If _transcriptLines.Count > maxLines Then
            visible = _transcriptLines.Skip(_transcriptLines.Count - maxLines)
        End If
        lblTranscript.Text = String.Join(vbCrLf, visible)
    End Sub

    Private Sub FlashPrompt(message As String)
        Dim orig As String = lblPrompt.Text
        lblPrompt.ForeColor = Color.Red
        lblPrompt.Text = message
        Dim t As New Timer()
        t.Interval = 1500
        AddHandler t.Tick, Sub()
                               t.Stop()
                               lblPrompt.ForeColor = Color.Yellow
                               lblPrompt.Text = orig
                           End Sub
        t.Start()
    End Sub

    ''' <summary>
    ''' Builds the PRC list text for CustomerNameList step.
    ''' DOS line 710: FILES "PRC\" + LEFT$(N2$,1) + "*.PRC"
    ''' </summary>
    ''' <summary>
    ''' Builds the PRC customer list formatted like DOS GW-BASIC FILES output:
    ''' 4 columns, each 16 chars wide (name padded with spaces), wrapped to new rows.
    ''' Includes .PRC extension to match DOS display exactly.
    ''' DOS line 710: FILES "PRC\" + LEFT$(N2$,1) + "*.PRC"
    ''' </summary>
    Private Function BuildPrcListText(letter As String) As String
        Dim prcDir As String = IO.Path.Combine(ShopCardSession.DataFolder, "PRC")
        If Not IO.Directory.Exists(prcDir) Then Return "(no matching customers found)"
        Dim matches = IO.Directory.GetFiles(prcDir, letter.ToUpper() & "*.PRC") _
                                  .Select(Function(f) IO.Path.GetFileName(f).ToUpper()) _
                                  .OrderBy(Function(n) n) _
                                  .ToArray()
        If matches.Length = 0 Then Return "(no matching customers found)"
        ' Format into 4-column grid, 16 chars wide per column — matches GW-BASIC FILES output
        Const ColWidth As Integer = 16
        Const ColsPerRow As Integer = 4
        Dim rows As New System.Text.StringBuilder()
        For i As Integer = 0 To matches.Length - 1
            rows.Append(matches(i).PadRight(ColWidth))
            If (i + 1) Mod ColsPerRow = 0 Then
                rows.AppendLine()
            End If
        Next
        Return rows.ToString().TrimEnd()
    End Function

End Class


