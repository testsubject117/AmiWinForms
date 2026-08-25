Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

' ShopCard Menu - Main Menu
' Legacy DOS: S.ASC (SHOPCARD.BAS)
' Mirrors the DOS submenu: 1, C, S, F, E, M, P, J, U, D, Q
Public Class FormShopCardMenu
    Inherits DosMenuFormBase

    Private _customerName As String = ""
    Private _lastRecord As ShopCardRecord = Nothing

    Public Sub New()
        ' Nothing here — name collection happens in OnShown
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        SetMenuTitle("SHOPCARD GENERATOR")
        ShowVersionInHeader = False
        UpdateHeaderClock()
        StretchButtonsToPanelWidth = True
        flpLeft.Padding = New Padding(8, 0, 8, 0)
        flpLeft.AutoScroll = False
        flpRight.Visible = False
        flpRight.Enabled = False
        ' Collapse right column so buttons fill full width
        Me.Controls.OfType(Of TableLayoutPanel)().
            Where(Function(t) t.ColumnCount = 2 AndAlso t.Controls.Contains(flpLeft)).
            ToList().ForEach(Sub(t)
                                 t.ColumnStyles(0).SizeType = SizeType.Percent
                                 t.ColumnStyles(0).Width = 100.0F
                                 t.ColumnStyles(1).SizeType = SizeType.Absolute
                                 t.ColumnStyles(1).Width = 0.0F
                             End Sub)
        Me.Width = 1024
        Me.Height = 720
        BuildMenu()
        TightenButtons()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        ' Ask for customer name before revealing the menu.
        ' If we already have a last-used name, pre-fill it but still prompt (DOS always prompts).
        ' Hide ourselves so only the name prompt is visible.
        Me.Visible = False
        Dim name As String = FormShopCardHeader.AskCustomerName(Nothing)
        If name Is Nothing OrElse name.Trim() = "" Then
            ' User cancelled — check if we have a carry-forward name to fall back on
            If ShopCardSession.LastCustomerName <> "" Then
                _customerName = ShopCardSession.LastCustomerName
            Else
                Me.Close()
                Return
            End If
        Else
            _customerName = name.Trim().ToUpper()
            ShopCardSession.SaveCustomerName(_customerName)
        End If
        HeaderCustomerName = _customerName
        UpdateHeaderClock()
        Me.Visible = True
    End Sub

    Private Sub BuildMenu()
        ClearMenu()
        Dim p = flpLeft

        AddMenuButton(p, "1", "Create A Shopcard", Sub() LaunchCreateShopCard())

        AddMenuButton(p, "C", "Change Customer Name", Sub() ChangeCustomerName())

        AddMenuButton(p, "S", "Create A Shopcard With The Same Procedures", Sub() LaunchSameProcedures())

        AddMenuButton(p, "F", "Find A Shopcard", Sub() ShowInlinePrompt("Enter the P.O. # or something to search for [ENTER = Quit]? ", AddressOf LaunchFind))

        AddMenuButton(p, "E", "Edit Master Spec List", Sub() LaunchSpecEditor())

        AddMenuButton(p, "M", "Modify A Shopcard That Has Already Been Printed", Sub() ShowInlinePrompt("Shopcard # to Edit [ENTER = Exit]? ", AddressOf LaunchModifyShopCard))

        AddMenuButton(p, "P", "Switch Between Laser & Star  [Current: " & ShopCardSession.PrinterDisplayName() & "]",
                      Sub() TogglePrinter())

        AddMenuButton(p, "J", "Just Enter Quantity & Part# For FAA", Sub() LaunchJustFAA())

        AddMenuButton(p, "V", "View Shopcards That Have Not Been Printed As Invoices", Sub() LaunchViewUnprinted())

        AddMenuButton(p, "D", "Delete/Void A Shopcard", Sub() LaunchDeleteVoid())

        AddMenuButton(p, "Q", "Quit", Sub() Me.Close())
    End Sub

    Private Sub LaunchSpecEditor()
        ShowInlinePasswordPrompt("Enter password? ", AddressOf SpecEditorPasswordCallback)
    End Sub

    Private Sub SpecEditorPasswordCallback(pw As String)
        If String.Compare(pw.Trim(), "DEAN", StringComparison.OrdinalIgnoreCase) <> 0 Then Return
        Dim dataFile As String = System.IO.Path.Combine(ShopCardSession.DataFolder, "NEWSPECS.DAT")
        Dim wordPath As String = FindWordExe()
        Dim editorPath As String = If(wordPath, "notepad.exe")
        Try
            Dim proc = System.Diagnostics.Process.Start(editorPath, """" & dataFile & """")
            If proc IsNot Nothing Then proc.WaitForExit()
        Catch ex As Exception
            MessageBox.Show("Could not open NEWSPECS.DAT:" & Environment.NewLine & ex.Message,
                            "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
        Me.Focus()
        Me.Activate()
    End Sub

    Private Function FindWordExe() As String
        ' Check common registry locations for Word
        Dim regPaths As String() = {
            "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WINWORD.EXE",
            "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\WINWORD.EXE"
        }
        For Each regPath In regPaths
            Dim key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath)
            If key IsNot Nothing Then
                Dim val = TryCast(key.GetValue(""), String)
                If val IsNot Nothing AndAlso System.IO.File.Exists(val) Then Return val
            End If
        Next
        ' Fallback: search common install paths
        Dim searchPaths As String() = {
            "C:\Program Files\Microsoft Office",
            "C:\Program Files (x86)\Microsoft Office"
        }
        For Each folder In searchPaths
            If System.IO.Directory.Exists(folder) Then
                Dim found = System.IO.Directory.GetFiles(folder, "WINWORD.EXE", System.IO.SearchOption.AllDirectories)
                If found.Length > 0 Then Return found(0)
            End If
        Next
        Return Nothing
    End Function

    Private Sub LaunchModifyShopCard(cardNumStr As String)
        If cardNumStr.Trim() = "" Then Return
        Dim cardNum As Integer
        If Not Integer.TryParse(cardNumStr.Trim(), cardNum) OrElse cardNum < 1 OrElse cardNum > 999999 Then Return
        ' DOS line 4400: bucket = INT(cardNum / 400)
        Dim bucket As Integer = cardNum \ 400
        Dim crdFile As String = System.IO.Path.Combine(ShopCardSession.DataFolder, "SHOPCARD", bucket.ToString(), cardNum.ToString() & ".CRD")
        If Not System.IO.File.Exists(crdFile) Then
            MessageBox.Show("Shopcard #" & cardNum.ToString() & " not found.", "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        ' DOS line 930: prints warning then immediately shells Word — no keypress wait
        Dim wordPath As String = FindWordExe()
        Dim editorPath As String = If(wordPath, "notepad.exe")
        Try
            Dim proc = System.Diagnostics.Process.Start(editorPath, """" & crdFile & """")
            If proc IsNot Nothing Then proc.WaitForExit()
        Catch ex As Exception
            MessageBox.Show("Could not open shopcard file:" & Environment.NewLine & ex.Message,
                            "ShopCard", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
        Me.Focus()
        Me.Activate()
    End Sub

    Private Sub LaunchFind(term As String)
        If term = "" Then Return
        Using frm As New FormShopCardFind(term)
            frm.ShowDialog(Me)
        End Using
        ' Restore focus to the menu form so hotkeys work again
        Me.Focus()
        Me.Activate()
    End Sub

    Private Sub LaunchSameProcedures()
        ' DOS lines 1030-1060: check if any A$(X,Y) is non-empty (same procedures in memory)
        ' If not, show message, wait for keypress, return to menu
        ' If yes, launch header then go straight to section hub with sections preserved
        If _lastRecord Is Nothing OrElse Not _lastRecord.HasAnyProcedure() Then
            DosMessageBox.Show(Me,
                "No procedures exist in memory from the last shopcard, you must re-enter them",
                "ShopCard", MessageBoxButtons.OK)
            ' DOS behavior: after Enter on this message, falls through to full create flow
            LaunchCreateShopCard()
            Return
        End If
        ' Has procedures - run header for new card info but keep sections
        Using frm As New FormShopCardHeader(ShopCardSession.LastCustomerName)
            If frm.ShowDialog(Me) = DialogResult.OK AndAlso frm.Accepted Then
                Dim record = frm.Result
                ' Copy all sections from last record into new record
                For sec As Integer = 1 To 8
                    For fld As Integer = 1 To 19
                        record.SetSection(sec, fld, _lastRecord.GetSection(sec, fld))
                    Next
                Next
                ' Skip Mag/Pene screen (sections already populated) - go straight to hub
                Using hub As New FormShopCardSectionHub(record)
                    hub.ShowDialog(Me)
                End Using
                _lastRecord = record
                RunSaveScreen(record)
            End If
        End Using
    End Sub

    Private Sub ChangeCustomerName()
        Dim name As String = FormShopCardHeader.AskCustomerName(Nothing)
        If name IsNot Nothing AndAlso name.Trim() <> "" Then
            _customerName = name.Trim().ToUpper()
            ShopCardSession.SaveCustomerName(_customerName)
            HeaderCustomerName = _customerName
            UpdateHeaderClock()
        End If
    End Sub

    Private Sub LaunchJustFAA()
        ' DOS line 890: JUST=1 — carry all fields from last card, prompt only Qty + Part#
        ' DOS line 1050: if no procedures in memory, warn inline then fall back to full new card flow
        If _lastRecord Is Nothing OrElse Not _lastRecord.HasAnyProcedure() Then
            ' DOS line 1050: CLS : PRINT message : INPUT QQ$ — blank screen, message at top, waits for ENTER
            DosFullScreenMessage.Show(Me, "No procedures exist in memory from the last shopcard, you must re-enter them")
            LaunchCreateShopCard()
            Return
        End If
        Using frm As New FormShopCardHeader(ShopCardSession.LastCustomerName, _lastRecord)
            If frm.ShowDialog(Me) = DialogResult.OK AndAlso frm.Accepted Then
                Dim record = frm.Result
                ' DOS line 1460: JUST=1 skips MagPene
                ' DOS line 2110: JUST=1 skips section hub -> goes straight to save/print (4300)
                ' Sections are already carried forward from _lastRecord inside the header constructor
                _lastRecord = record
                RunSaveScreen(record)
            End If
        End Using
    End Sub

    Private Sub LaunchCreateShopCard()
        Using frm As New FormShopCardHeader(ShopCardSession.LastCustomerName)
            If frm.ShowDialog(Me) = DialogResult.OK AndAlso frm.Accepted Then
                Dim record = frm.Result
                Using magPene As New FormShopCardMagPene(record)
                    magPene.ShowDialog(Me)
                End Using
                Using hub As New FormShopCardSectionHub(record)
                    hub.ShowDialog(Me)
                End Using
                _lastRecord = record
                RunSaveScreen(record)
            End If
        End Using
    End Sub

    Private Sub TogglePrinter()
        ShowInlinePrompt("Are you Sure you want to switch the Printer (Y/N)? ", AddressOf TogglePrinterConfirm)
    End Sub

    ''' <summary>
    ''' DOS lines 4350-4700: write card to disk, show card number, show print instruction.
    ''' If user presses Q, DOS goes to line 100 which returns to customer name screen.
    ''' </summary>
    Private Sub RunSaveScreen(record As ShopCardRecord)
        Using save As New FormShopCardSave(record, ShopCardSession.LastCustomerName)
            Dim result = save.ShowDialog(Me)
            If result = DialogResult.Cancel Then
                ' DOS line 4680: Q -> GOTO 100 -> customer name screen
                ' Re-ask customer name (name pre-fills from session if already set)
                Dim newName As String = FormShopCardHeader.AskCustomerName(Me)
                If newName IsNot Nothing AndAlso newName.Trim() <> "" Then
                    _customerName = newName.Trim().ToUpper()
                    ShopCardSession.SaveCustomerName(_customerName)
                    HeaderCustomerName = _customerName
                    UpdateHeaderClock()
                End If
            End If
            ' ENTER (print) — printing not yet implemented; falls through to menu
        End Using
    End Sub

    Private Sub TogglePrinterConfirm(answer As String)
        If answer.Trim().ToUpper() <> "Y" Then Return
        ShopCardSession.UseLaserPrinter = Not ShopCardSession.UseLaserPrinter
        BuildMenu()
        TightenButtons()
    End Sub

    Private Sub LaunchViewUnprinted()
        Using frm As New FormShopCardUnprinted()
            frm.ShowDialog(Me)
        End Using
        Me.Focus()
        Me.Activate()
    End Sub

    Private Sub LaunchDeleteVoid()
        Using frm As New FormShopCardDelete()
            frm.ShowDialog(Me)
        End Using
        Me.Focus()
        Me.Activate()
    End Sub

    Private Sub TightenButtons()
        For Each btn As Control In flpLeft.Controls
            If TypeOf btn Is Button Then
                btn.Width = flpLeft.ClientSize.Width - flpLeft.Padding.Horizontal
            End If
        Next
    End Sub

End Class
