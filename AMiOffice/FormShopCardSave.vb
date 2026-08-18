Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing

''' <summary>
''' DOS parity: write-to-disk screen + print instruction screen.
''' DOS lines 4350-4700:
'''   4350 OPEN CARDLOCK.dat
'''   4360-4420 read/increment/write crdnumbr.dat
'''   4430 PRINT "******* WRITING SHOPCARD #NNN TO DISK ********"
'''   4560-4640 If customer = PSIBEARI: show inspection type picker (1)-(4)
'''   4660 CLS: inverse-video "Write NNN on P.O. and tear off last shopcard."
'''   4670-4680 ENTER = print, Q = quit back to customer name screen
''' </summary>
Public Class FormShopCardSave
    Inherits Form

    ' ── Results ───────────────────────────────────────────────────────────────
    ''' <summary>True if user pressed ENTER to print; False if Q to quit.</summary>
    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property UserWantsPrint As Boolean = False

    ' ── Private state ─────────────────────────────────────────────────────────
    Private ReadOnly _record As ShopCardRecord
    Private ReadOnly _customerName As String
    Private _assignedCardNumber As String = ""
    Private _phase As SavePhase = SavePhase.WritingToDisk

    Private Enum SavePhase
        WritingToDisk       ' show "WRITING SHOPCARD #NNN TO DISK" + inspection picker if PSIBEARI
        PrintInstruction    ' show inverse-video "Write NNN on P.O..." + ENTER/Q prompt
    End Enum

    ' ── Layout controls ───────────────────────────────────────────────────────
    Private lblWriting As Label
    Private lblInspectionPrompt As Label
    Private lblInverseInstruction As Label
    Private lblPrintPrompt As Label

    ' ── Constructor ───────────────────────────────────────────────────────────
    Public Sub New(record As ShopCardRecord, customerName As String)
        _record = record
        _customerName = customerName.Trim().ToUpper()
        InitializeLayout()
    End Sub

    ' ── Layout ────────────────────────────────────────────────────────────────
    Private Sub InitializeLayout()
        Me.Text = "ShopCard"
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point)
        Me.ClientSize = New Size(900, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.KeyPreview = True

        ' Writing to disk label
        lblWriting = New Label() With {
            .AutoSize = False,
            .Size = New Size(860, 30),
            .Location = New Point(20, 40),
            .Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point),
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Text = ""
        }

        ' Inspection type prompt (only shown for PSIBEARI) — DOS lines 4570-4620
        lblInspectionPrompt = New Label() With {
            .AutoSize = False,
            .Size = New Size(860, 80),
            .Location = New Point(20, 90),
            .Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point),
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Text = "(1) In Process     (2) In Service    (3) Final     (4) I'm Not Sure" & vbCrLf & vbCrLf & "Pick a Number",
            .Visible = False
        }

        ' Inverse-video "Write NNN on the P.O." instruction — DOS line 4660
        lblInverseInstruction = New Label() With {
            .AutoSize = False,
            .Size = New Size(860, 30),
            .Location = New Point(20, 40),
            .Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point),
            .ForeColor = Color.Black,
            .BackColor = Color.White,
            .Text = "",
            .Visible = False
        }

        ' "Press [ENTER] to Print shopcard  or  [Q] to Quit."
        lblPrintPrompt = New Label() With {
            .AutoSize = False,
            .Size = New Size(860, 30),
            .Location = New Point(20, 80),
            .Font = New Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Point),
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Text = "Press [ENTER] to Print shopcard  or  [Q] to Quit.",
            .Visible = False
        }

        Me.Controls.AddRange({lblWriting, lblInspectionPrompt, lblInverseInstruction, lblPrintPrompt})
    End Sub

    ' ── Startup ───────────────────────────────────────────────────────────────
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        RunWriteToDisk()
    End Sub

    ' ── Write to disk (DOS lines 4350-4430) ───────────────────────────────────
    Private Sub RunWriteToDisk()
        _assignedCardNumber = AssignCardNumber()

        ' Store card number on the record so callers can read it
        _record.CardNumber = _assignedCardNumber

        ' DOS line 4430: PRINT "******* WRITING SHOPCARD #NNN TO DISK ********"
        lblWriting.Text = "******* WRITING SHOPCARD #" & _assignedCardNumber & " TO DISK ********"

        ' Write the actual .CRD file
        WriteShopCardFile(_assignedCardNumber, _record)

        ' DOS line 4560: IF N2$ <> "PSIBEARI" THEN 4660 (skip inspection picker)
        If _customerName = "PSIBEARI" Then
            _phase = SavePhase.WritingToDisk
            lblInspectionPrompt.Visible = True
        Else
            ShowPrintInstruction()
        End If
    End Sub

    ' ── Assign card number ────────────────────────────────────────────────────
    ' Replaces DOS lines 4360-4420. Key differences from DOS:
    '   - No wrap at 1500. Counter increments forever, avoiding the DOS-era
    '     duplicate-number problem where cards 200-1500 were written twice.
    '   - If the computed file already exists in any bucket (historical collision),
    '     we skip forward until we find a slot with no existing file anywhere on disk.
    Private Function AssignCardNumber() As String
        Dim crdPath As String = Path.Combine(ShopCardSession.DataFolder, "crdnumbr.dat")
        Dim tmp As Integer = 0

        If File.Exists(crdPath) Then
            Try
                Dim raw As String = File.ReadAllText(crdPath).Trim().Trim(""""c)
                Integer.TryParse(raw, tmp)
            Catch
            End Try
        End If

        ' Increment then skip past any number whose .CRD already exists anywhere on disk.
        ' This guarantees we never overwrite historical DOS data.
        Do
            tmp += 1
            ' Safety ceiling — no real shop would reach 999999
            If tmp > 999999 Then tmp = 1
        Loop While ShopCardExistsAnywhere(tmp)

        Try
            File.WriteAllText(crdPath, """" & tmp.ToString() & """")
        Catch
        End Try

        Return tmp.ToString()
    End Function

    ''' <summary>
    ''' Returns True if a .CRD file for this card number exists in ANY bucket folder.
    ''' Prevents silent overwrites of historical data.
    ''' </summary>
    Private Function ShopCardExistsAnywhere(cardNum As Integer) As Boolean
        Dim shopcardRoot As String = Path.Combine(ShopCardSession.DataFolder, "SHOPCARD")
        If Not Directory.Exists(shopcardRoot) Then Return False
        Dim fileName As String = cardNum.ToString() & ".CRD"
        For Each folder As String In Directory.GetDirectories(shopcardRoot)
            If File.Exists(Path.Combine(folder, fileName)) Then Return True
        Next
        Return File.Exists(Path.Combine(shopcardRoot, fileName))
    End Function

    ' ── Write .CRD file (DOS lines 4440-4540) ─────────────────────────────────
    Private Sub WriteShopCardFile(cardNum As String, record As ShopCardRecord)
        Try
            Dim num As Integer = Integer.Parse(cardNum)
            ' DOS line 4400: CDN2$ = STR$(INT(TMP/200)) — bucket subfolder
            Dim bucket As Integer = num \ 200
            Dim folder As String = Path.Combine(ShopCardSession.DataFolder, "SHOPCARD", bucket.ToString())
            Directory.CreateDirectory(folder)
            Dim filePath As String = Path.Combine(folder, cardNum & ".CRD")

            Using sw As New StreamWriter(filePath, False)
                ' Write header fields matching DOS WRITE #1 sequence (lines 4460-4540)
                sw.WriteLine("""" & record.CustomerName & """")
                sw.WriteLine("""" & record.EntryDate & """")
                sw.WriteLine("""" & record.PONumber & """")
                sw.WriteLine("""" & record.NumberOfPans & """")
                sw.WriteLine("""" & record.NumberOfBoxes & """")
                sw.WriteLine("""" & record.Weight & """")
                sw.WriteLine("""" & record.QuantityReceived & """")
                sw.WriteLine("""" & record.PartNumberAndName & """")
                sw.WriteLine("""" & record.JobRouteNumber & """")
                sw.WriteLine("""" & record.Material & """")
                sw.WriteLine("""" & record.HeatTreat & """")
                sw.WriteLine("""" & record.ConditionReceived & """")
                sw.WriteLine("""" & record.HotRush & """")
                sw.WriteLine("""" & record.HandleWithCare & """")
                ' TODO: write sections 1-8 fields here during section parity pass
            End Using
        Catch ex As Exception
            ' Non-fatal — log and continue to print instruction screen
        End Try
    End Sub

    ' ── Show print instruction screen (DOS line 4660) ─────────────────────────
    Private Sub ShowPrintInstruction()
        _phase = SavePhase.PrintInstruction
        lblWriting.Visible = False
        lblInspectionPrompt.Visible = False

        ' DOS line 4660: CLS then inverse-video instruction then normal ENTER/Q prompt
        lblInverseInstruction.Text = " Write " & _assignedCardNumber & " on the P.O. and tear off the last shopcard. "
        lblInverseInstruction.Visible = True
        lblPrintPrompt.Visible = True
    End Sub

    ' ── Keyboard handling ──────────────────────────────────────────────────────
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)

        If _phase = SavePhase.WritingToDisk AndAlso _customerName = "PSIBEARI" Then
            ' Inspection type picker: 1-4 (DOS lines 4590-4620)
            Select Case e.KeyCode
                Case Keys.D1, Keys.NumPad1
                    ' (1) In Process — DOS line 4590: A$(7,6) = "YES"
                    _record.SetSection(7, 6, "YES")
                    ShowPrintInstruction()
                    e.Handled = True
                Case Keys.D2, Keys.NumPad2
                    ' (2) In Service — DOS line 4600: A$(7,7) = "YES"
                    _record.SetSection(7, 7, "YES")
                    ShowPrintInstruction()
                    e.Handled = True
                Case Keys.D3, Keys.NumPad3
                    ' (3) Final — DOS line 4610: A$(7,8) = "YES"
                    _record.SetSection(7, 8, "YES")
                    ShowPrintInstruction()
                    e.Handled = True
                Case Keys.D4, Keys.NumPad4
                    ' (4) I'm Not Sure — DOS line 4620: no field set, continue
                    ShowPrintInstruction()
                    e.Handled = True
            End Select
            Return
        End If

        If _phase = SavePhase.PrintInstruction Then
            Select Case e.KeyCode
                Case Keys.Return
                    ' DOS line 4690: PRINT "Printing Shopcard..." — print not yet implemented
                    UserWantsPrint = True
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                    e.Handled = True
                Case Keys.Q
                    ' DOS line 4680: Q -> GOTO 100 -> back to customer name screen
                    UserWantsPrint = False
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                    e.Handled = True
            End Select
        End If
    End Sub

End Class
