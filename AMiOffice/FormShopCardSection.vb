Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' DOS-parity section editing screen for ShopCard sections 1-8.
''' Matches DOS letter-menu pattern: (A) Field: value  (Z) EXIT
''' Single keypress toggles YES/blank, or opens inline sub-prompt for text entry.
''' </summary>
Public Class FormShopCardSection
    Inherits Form

    ' ── data ──────────────────────────────────────────────────────────────
    Private ReadOnly _record As ShopCardRecord
    Private ReadOnly _sectionNum As Integer

    ' ── layout controls ───────────────────────────────────────────────────
    Private ReadOnly pnlHeader As New Panel()
    Private ReadOnly lblTitle As New Label()
    Private ReadOnly lblInfoBar As New Label()
    Private ReadOnly pnlBody As New Panel()
    Private ReadOnly pnlPrompt As New Panel()
    Private ReadOnly lblPromptText As New Label()
    Private ReadOnly txtInline As New TextBox()
    Private ReadOnly lblHint As New Label()
    Private ReadOnly lblInstruct As New Label()

    ' ── field rows ────────────────────────────────────────────────────────
    Private _rows As List(Of SectionFieldDef)
    Private _rowLabels As New List(Of Label)()   ' value labels, one per row
    Private _activeFieldIdx As Integer = -1       ' which field is being prompted

    ' ── timer ─────────────────────────────────────────────────────────────
    Private ReadOnly _tmr As New Timer()

    ' ═══════════════════════════════════════════════════════════════════════
    '  Field definition model
    ' ═══════════════════════════════════════════════════════════════════════
    Private Enum FieldKind
        YesToggle       ' single keypress → YES / blank
        TextEntry       ' keypress → sub-prompt, user types value + ENTER
        Spacer          ' blank divider row — no hotkey
    End Enum

    Private Class SectionFieldDef
        Public Property HotKey As String = ""       ' "A".."Z", blank for spacer
        Public Property Label As String = ""
        Public Property Kind As FieldKind = FieldKind.YesToggle
        Public Property FieldIndex As Integer = 0   ' 1-based index into ShopCardRecord section
        Public Property Shortcuts As Dictionary(Of String, String) = Nothing  ' key → expansion
        Public Property PromptLabel As String = ""  ' override prompt text (blank = use Label)
    End Class

    ' ═══════════════════════════════════════════════════════════════════════
    '  Constructor
    ' ═══════════════════════════════════════════════════════════════════════
    Public Sub New(record As ShopCardRecord, sectionNum As Integer)
        _record = record
        _sectionNum = sectionNum
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════
    '  Section field definitions  (all 8 sections)
    ' ═══════════════════════════════════════════════════════════════════════
    Private Function BuildFieldDefs() As List(Of SectionFieldDef)
        Dim d As New List(Of SectionFieldDef)()

        Select Case _sectionNum

            Case 1  ' Etch / Clean / Glassbead / Strip (DOS DATA line 1820: 10 fields)
                d.Add(F("A", "Etch for Weld:", 1))
                d.Add(F("B", "Alkaline:", 2))
                d.Add(F("C", "Descale:", 3))
                d.Add(F("D", "Glass Bead:", 4))
                d.Add(F("E", "Powder Blast:", 5))
                d.Add(F("F", "Aluminum Oxide:", 6))
                d.Add(F("G", "Strip Copper:", 7))
                d.Add(F("H", "Degrease:", 8))
                d.Add(F("I", "Cetyl:", 9))
                d.Add(F("J", "Other:", 10, FieldKind.TextEntry))

            Case 2  ' Electro Polish / Passivate / Salt Spray / High Humidity / Copper Sulfate (DOS DATA line 1830)
                d.Add(F("A", "Electro Polish:", 1))
                d.Add(F("B", "Time:", 2, FieldKind.TextEntry))
                d.Add(F("C", "Amps:", 3, FieldKind.TextEntry))
                d.Add(F("D", "Passivate:", 4))
                d.Add(F("E", "AMS QQP35:", 5))
                d.Add(F("F", "MIL-S-5002DA1:", 6))
                d.Add(F("G", "Other:", 7, FieldKind.TextEntry))
                d.Add(F("H", "Type II:", 8))
                d.Add(F("I", "Type VI:", 9))
                d.Add(F("J", "Type VII:", 10))
                d.Add(F("K", "Type VIII:", 11))
                d.Add(F("L", "Time in tank:", 12, FieldKind.TextEntry))
                d.Add(F("M", "Temperature:", 13, FieldKind.TextEntry))
                d.Add(F("N", "Salt Spray pcs:", 14, FieldKind.TextEntry))
                d.Add(F("O", "Visual Insp. Before:", 15))
                d.Add(F("P", "Visual Insp. After:", 16))
                d.Add(F("Q", "Copper Sulfate pcs:", 17, FieldKind.TextEntry))
                d.Add(Spacer("R"))
                Dim hhShortcuts As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
                    {"M", "6 Min."}
                }
                Dim hh = F("S", "High Humidity:", 19, FieldKind.TextEntry)
                hh.Shortcuts = hhShortcuts
                hh.PromptLabel = "High Humidity pcs"
                d.Add(hh)

            Case 3  ' Chem Film / Anodize (DOS DATA line 1850: 6 fields)
                d.Add(F("A", "Chem Film:", 1))
                d.Add(F("B", "Clear:", 2))
                d.Add(F("C", "Gold:", 3))
                d.Add(F("D", "Other:", 4, FieldKind.TextEntry))
                d.Add(F("E", "Spec.:", 5, FieldKind.TextEntry))
                d.Add(F("F", "Anodize:", 6, FieldKind.TextEntry))

            Case 4  ' Magnetic Insp. (DOS DATA line 1860: 9 fields)
                d.Add(F("A", "Magnetic Insp.:", 1))
                d.Add(F("B", "ASTM-E-1444-01:", 2))
                d.Add(F("C", "Other:", 3, FieldKind.TextEntry))
                d.Add(F("D", "Head PSI:", 4, FieldKind.TextEntry))
                d.Add(F("E", "Centeral Conductor Amps:", 5, FieldKind.TextEntry))
                d.Add(F("F", "C.C. Size:", 6, FieldKind.TextEntry))
                d.Add(F("G", "Head Shot Amps:", 7, FieldKind.TextEntry))
                d.Add(F("H", "Coil Shot Amps:", 8, FieldKind.TextEntry))
                d.Add(Spacer("I"))

            Case 5  ' Penetrant Insp. (DOS DATA lines 1870-1871: 18 fields)
                d.Add(F("A", "Penetrant Insp.:", 1))
                d.Add(F("B", "MIL-STD-6866N1:", 2))
                d.Add(F("C", "Other:", 3, FieldKind.TextEntry))
                d.Add(F("D", "etch:", 4))
                d.Add(F("E", "Ardrox P135E:", 5))
                d.Add(F("F", "Ardrox 985-P13:", 6))
                d.Add(F("G", "Ardrox 985-P14:", 7))
                d.Add(F("H", "Batch:", 8, FieldKind.TextEntry))
                d.Add(F("I", "Developer Ardrox:", 9))
                d.Add(F("J", "Batch:", 10, FieldKind.TextEntry))
                d.Add(F("K", "Etch Rate:", 11, FieldKind.TextEntry))
                d.Add(F("L", "Etch Results:", 12, FieldKind.TextEntry))
                d.Add(F("M", "Etch Spec.:", 13, FieldKind.TextEntry))
                d.Add(F("N", "Method:", 14, FieldKind.TextEntry))
                d.Add(F("O", "Level:", 15, FieldKind.TextEntry))
                d.Add(F("P", "Tank No.:", 16, FieldKind.TextEntry))
                d.Add(F("Q", "Dwell Time:", 17, FieldKind.TextEntry))
                d.Add(F("R", "Min. Drying Temp. (160f Max):", 18, FieldKind.TextEntry))

            Case 6  ' Dye / Stamp
                Dim dyeShortcuts As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
                    {"B", "BLUE (mag)"},
                    {"M", "MAROON (pene)"}
                }
                Dim dy = F("A", "Dye:", 1, FieldKind.TextEntry)
                dy.Shortcuts = dyeShortcuts
                dy.PromptLabel = "Dye"
                d.Add(dy)
                d.Add(F("B", "Stamp:", 2, FieldKind.TextEntry))

            Case 7  ' Cad Plate
                d.Add(F("A", "Cad Plate:", 1))
                d.Add(F("B", "Type I:", 2))
                d.Add(F("C", "Type II:", 3))
                d.Add(F("D", "Type III:", 4))
                d.Add(F("E", "Class I:", 5))
                d.Add(F("F", "Class II:", 6))
                d.Add(F("G", "Class III:", 7))
                ' Other (H) has spec shortcuts
                Dim otherShortcuts As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
                    {"1", "QQ-P-416F AMENDMENT 3"},
                    {"2", "AMS-QQ-P-416F"},
                    {"3", "AMS-QQ-P-416 REV N/C"}
                }
                Dim oth = F("H", "Other:", 8, FieldKind.TextEntry)
                oth.Shortcuts = otherShortcuts
                oth.PromptLabel = "Other"
                d.Add(oth)
                d.Add(F("I", "RC #:", 9, FieldKind.TextEntry))
                ' PreBake / PostBake with shortcuts
                Dim pb = F("J", "PreBake Hrs:", 10, FieldKind.TextEntry)
                pb.Shortcuts = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
                    {"1", "STRESS RELIEF 4HRS 375F+-25F"}
                }
                pb.PromptLabel = "PreBake Hrs"
                d.Add(pb)
                Dim postb = F("K", "PostBake Hrs:", 11, FieldKind.TextEntry)
                postb.Shortcuts = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
                    {"1", "PostBake 23HRS 375F+-25F"}
                }
                postb.PromptLabel = "PostBake Hrs"
                d.Add(postb)
                d.Add(F("L", "Cad Strip:", 12))
                d.Add(F("M", "Nickle Strike:", 13))
                d.Add(F("N", "Masking:", 14))
                d.Add(F("O", "AMS-2400:", 15))
                d.Add(F("P", "AMS-2401:", 16))

            Case 8  ' Qty / 100% (same as pre-header Mag/Pene sub-screen)
                d.Add(F("A", "Qty To Be Insp. or Processed % Sample:", 1, FieldKind.TextEntry))
                d.Add(F("B", "100%:", 2, FieldKind.TextEntry))

        End Select

        Return d
    End Function

    ' ── helpers ───────────────────────────────────────────────────────────
    Private Function F(key As String, lbl As String, idx As Integer,
                       Optional kind As FieldKind = FieldKind.YesToggle) As SectionFieldDef
        Return New SectionFieldDef() With {
            .HotKey = key, .Label = lbl, .Kind = kind,
            .FieldIndex = idx, .PromptLabel = lbl.TrimEnd(":"c).Trim()
        }
    End Function

    Private Function Spacer(key As String) As SectionFieldDef
        Return New SectionFieldDef() With {.HotKey = key, .Label = " :", .Kind = FieldKind.Spacer, .FieldIndex = 0}
    End Function

    ' ═══════════════════════════════════════════════════════════════════════
    '  Form load / layout
    ' ═══════════════════════════════════════════════════════════════════════
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        _rows = BuildFieldDefs()

        Me.Text = SectionTitle()
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.KeyPreview = True
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Width = 1100
        Me.Height = 700

        BuildLayout()
        RenderRows()
        ShowInstructLine()

        _tmr.Interval = 1000
        AddHandler _tmr.Tick, Sub() UpdateClock()
        _tmr.Start()
        UpdateClock()
    End Sub

    Private Function SectionTitle() As String
        Select Case _sectionNum
            Case 1 : Return "ETCH FOR WELD / CLEAN / GLASSBEAD / STRIP"
            Case 2 : Return "ELECTRO POLISH / PASSIVATE / SALT SPRAY / HIGH HUMIDITY / COPPER SULFATE"
            Case 3 : Return "CHEM FILM / ANODIZE"
            Case 4 : Return "MAGNETIC INSPECTION"
            Case 5 : Return "PENETRANT INSPECTION"
            Case 6 : Return "DYE / STAMP"
            Case 7 : Return "CAD PLATE"
            Case 8 : Return "QTY TO BE INSP. / 100%"
            Case Else : Return "SECTION " & _sectionNum
        End Select
    End Function

    Private Sub BuildLayout()
        ' ── prompt panel (bottom, for text-entry sub-prompts) ───────────
        pnlPrompt.Dock = DockStyle.Bottom
        pnlPrompt.Height = 80
        pnlPrompt.BackColor = Color.Black
        pnlPrompt.Visible = False
        Me.Controls.Add(pnlPrompt)

        lblPromptText.AutoSize = True
        lblPromptText.ForeColor = Color.Yellow
        lblPromptText.Font = New Font("Courier New", 11, FontStyle.Regular)
        lblPromptText.Location = New Point(8, 8)
        pnlPrompt.Controls.Add(lblPromptText)

        txtInline.BackColor = Color.Black
        txtInline.ForeColor = Color.Yellow
        txtInline.Font = New Font("Courier New", 11, FontStyle.Regular)
        txtInline.BorderStyle = BorderStyle.None
        txtInline.Width = 500
        txtInline.Location = New Point(8, 36)
        AddHandler txtInline.KeyDown, AddressOf OnInlineKeyDown
        pnlPrompt.Controls.Add(txtInline)

        lblHint.AutoSize = True
        lblHint.ForeColor = Color.Cyan
        lblHint.Font = New Font("Courier New", 9, FontStyle.Regular)
        lblHint.Location = New Point(8, 58)
        pnlPrompt.Controls.Add(lblHint)

        ' ── instruct line ───────────────────────────────────────────────
        lblInstruct.Dock = DockStyle.Bottom
        lblInstruct.Height = 24
        lblInstruct.TextAlign = ContentAlignment.MiddleLeft
        lblInstruct.Padding = New Padding(8, 0, 0, 0)
        lblInstruct.ForeColor = Color.White
        lblInstruct.BackColor = Color.Black
        lblInstruct.Font = New Font("Courier New", 11, FontStyle.Regular)
        lblInstruct.Text = "*** Type Appropriate Letter ***"
        Me.Controls.Add(lblInstruct)

        ' ── body panel (scrollable field rows) ──────────────────────────
        pnlBody.Dock = DockStyle.Fill
        pnlBody.BackColor = Color.Black
        pnlBody.AutoScroll = True
        Me.Controls.Add(pnlBody)
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════
    '  Render field rows
    ' ═══════════════════════════════════════════════════════════════════════
    Private Sub RenderRows()
        pnlBody.Controls.Clear()
        _rowLabels.Clear()

        Dim y As Integer = 8
        Dim rowH As Integer = 22
        Dim monoFont As New Font("Courier New", 11, FontStyle.Regular)

        For Each row In _rows
            Dim lbl As New Label()
            lbl.AutoSize = False
            lbl.Width = pnlBody.Width - 20
            lbl.Height = rowH
            lbl.Top = y
            lbl.Left = 8
            lbl.Font = monoFont
            lbl.ForeColor = Color.White
            lbl.BackColor = Color.Black

            If row.Kind = FieldKind.Spacer Then
                lbl.Text = ""
            Else
                Dim val As String = _record.GetSection(_sectionNum, row.FieldIndex)
                Dim valStr As String = If(val <> "", "   " & val, "")
                lbl.Text = String.Format("({0})  {1}{2}", row.HotKey, row.Label, valStr)
            End If

            ' highlight active field
            If _activeFieldIdx >= 0 AndAlso _rows(_activeFieldIdx) Is row Then
                lbl.ForeColor = Color.Yellow
            End If

            pnlBody.Controls.Add(lbl)
            _rowLabels.Add(lbl)
            y += rowH
        Next

        ' EXIT row
        Dim lblExit As New Label()
        lblExit.AutoSize = False
        lblExit.Width = pnlBody.Width - 20
        lblExit.Height = rowH
        lblExit.Top = y + 4
        lblExit.Left = 8
        lblExit.Font = monoFont
        lblExit.ForeColor = Color.White
        lblExit.BackColor = Color.Black
        lblExit.Text = "(Z)  EXIT"
        pnlBody.Controls.Add(lblExit)
    End Sub

    Private Sub ShowInstructLine()
        lblInstruct.Text = "*** Type Appropriate Letter ***"
        lblInstruct.ForeColor = Color.Yellow
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════
    '  Key handling
    ' ═══════════════════════════════════════════════════════════════════════
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If pnlPrompt.Visible Then
            ' prompt is active — let txtInline handle it
            MyBase.OnKeyDown(e)
            Return
        End If

        If e.KeyCode = Keys.Escape Then
            Me.Close()
            e.Handled = True
            Return
        End If

        Dim key As String = e.KeyCode.ToString().ToUpper()
        If key.Length > 1 Then
            MyBase.OnKeyDown(e)
            Return
        End If

        ' Z = EXIT
        If key = "Z" Then
            Me.Close()
            e.Handled = True
            Return
        End If

        ' find matching field
        Dim idx As Integer = _rows.FindIndex(
            Function(r) r.HotKey.ToUpper() = key AndAlso r.Kind <> FieldKind.Spacer)

        If idx < 0 Then
            MyBase.OnKeyDown(e)
            Return
        End If

        Dim row = _rows(idx)
        e.Handled = True

        If row.Kind = FieldKind.YesToggle Then
            ' toggle YES / blank
            Dim cur = _record.GetSection(_sectionNum, row.FieldIndex)
            Dim newVal = If(cur = "YES", "", "YES")
            _record.SetSection(_sectionNum, row.FieldIndex, newVal)
            ' DOS line 2350: Section 1, pressing I (Cetyl/field 9) when BOTH Cetyl and Other are empty
            ' auto-fills Other (field 10) with "MIL-L-87132 Type1"
            If _sectionNum = 1 AndAlso row.FieldIndex = 9 AndAlso newVal = "YES" Then
                If _record.GetSection(1, 10) = "" Then
                    _record.SetSection(1, 10, "MIL-L-87132 Type1")
                End If
            End If
            RenderRows()
        ElseIf row.Kind = FieldKind.TextEntry Then
            ActivatePrompt(idx)
        End If
    End Sub

    Private Sub ActivatePrompt(idx As Integer)
        _activeFieldIdx = idx
        Dim row = _rows(idx)

        Dim promptLbl = If(row.PromptLabel <> "", row.PromptLabel, row.Label.TrimEnd(":"c).Trim())
        Dim cur = _record.GetSection(_sectionNum, row.FieldIndex)
        Dim enterHint = If(cur <> "", String.Format(" [ENTER = {0}]", cur), "")

        lblPromptText.Text = String.Format("{0}{1} ?", promptLbl, enterHint)

        ' show shortcuts hint
        If row.Shortcuts IsNot Nothing AndAlso row.Shortcuts.Count > 0 Then
            Dim parts As New List(Of String)()
            For Each kv In row.Shortcuts
                parts.Add(String.Format("{0} = {1}", kv.Key, kv.Value))
            Next
            lblHint.Text = String.Join("    ", parts)
            lblHint.Visible = True
        Else
            lblHint.Visible = False
        End If

        txtInline.Text = ""
        pnlPrompt.Visible = True
        RenderRows()
        txtInline.Focus()
    End Sub

    Private Sub OnInlineKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then
            CancelPrompt()
            e.Handled = True
            Return
        End If

        If e.KeyCode = Keys.Return Then
            CommitPrompt()
            e.Handled = True
            Return
        End If
    End Sub

    Private Sub CommitPrompt()
        If _activeFieldIdx < 0 Then Return
        Dim row = _rows(_activeFieldIdx)
        Dim entered = txtInline.Text.Trim()

        If entered = "" Then
            ' ENTER with blank = keep existing value (carry-forward)
        Else
            ' check shortcut expansion
            If row.Shortcuts IsNot Nothing Then
                Dim expanded As String = Nothing
                If row.Shortcuts.TryGetValue(entered, expanded) Then
                    entered = expanded
                End If
            End If

            ' Section 2 High Humidity special: pcs + Hours stored as one string
            If _sectionNum = 2 AndAlso row.HotKey.ToUpper() = "S" Then
                ' first call stores pcs, then we prompt for hours inline
                Dim existing = _record.GetSection(_sectionNum, row.FieldIndex)
                If Not existing.Contains("Hours:") Then
                    ' store pcs, then prompt for Hours
                    _record.SetSection(_sectionNum, row.FieldIndex, entered)
                    lblPromptText.Text = "Hours ?"
                    lblHint.Visible = False
                    txtInline.Text = ""
                    txtInline.Focus()
                    RenderRows()
                    Return
                Else
                    ' second call — append hours
                    Dim pcs = existing.Split(New String() {"   Hours:"}, StringSplitOptions.None)(0)
                    _record.SetSection(_sectionNum, row.FieldIndex,
                                       String.Format("{0}   Hours: {1}", pcs, entered))
                End If
            Else
                _record.SetSection(_sectionNum, row.FieldIndex, entered)
            End If
        End If

        DismissPrompt()
    End Sub

    Private Sub CancelPrompt()
        DismissPrompt()
    End Sub

    Private Sub DismissPrompt()
        _activeFieldIdx = -1
        pnlPrompt.Visible = False
        RenderRows()
        Me.Focus()
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════
    '  Clock
    ' ═══════════════════════════════════════════════════════════════════════
    Private Sub UpdateClock()
        Dim now = DateTime.Now
        Dim dateStr = now.ToString("MM-dd-yyyy")
        Dim timeStr = now.ToString("HH:mm")
        Dim customer = If(_record.CustomerName <> "", _record.CustomerName, "")
        Dim pad = New String(" "c, Math.Max(0, 60 - dateStr.Length - customer.Length - timeStr.Length - 20))
        lblInfoBar.Text = String.Format(" {0}    Customers name: {1}{2}{3}",
                                        dateStr, customer, pad, timeStr)
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        _tmr.Stop()
        MyBase.OnFormClosed(e)
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        If _rowLabels.Count > 0 Then
            For Each lbl In _rowLabels
                lbl.Width = pnlBody.Width - 20
            Next
        End If
    End Sub

End Class
