Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Partial Public Class FormMainMenu

    Private _buildInfo As String = ""

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Dim V = BuildInfo.DisplayVersion
        Dim built = BuildInfo.BuildNumber

        Me.KeyPreview = True

        BuildMenu()


        _buildInfo = "   " & BuildInfo.DisplayVersion
        Me.Text = "Active Magnetic Inspection Main Menu Application   " & BuildInfo.DisplayVersion
        ' Force the Show of a small window with version and build - 'MessageBox.Show(BuildInfo.DisplayVersion, "DisplayVersion")

        lblMainMenu.Text = "MAIN MENU"
        UpdateHeaderClock()
        tmrClock.Interval = 1000
        tmrClock.Start()

        ' Force the version/date text to start as far left as possible in lblDateTime
        lblDateTime.TextAlign = ContentAlignment.MiddleLeft
        lblDateTime.Padding = New Padding(0, 10, 0, 0)  ' remove right padding; keep top padding
        lblDateTime.Margin = New Padding(0)

        ' IMPORTANT: run resize after layout is finalized
        Me.BeginInvoke(New Action(Sub()
                                      ResizeButtonsToPanel(flpLeft)
                                      ResizeButtonsToPanel(flpRight)
                                  End Sub))
    End Sub

    Private Sub flpLeft_SizeChanged(sender As Object, e As EventArgs) Handles flpLeft.SizeChanged
        ResizeButtonsToPanel(flpLeft)
    End Sub

    Private Sub flpRight_SizeChanged(sender As Object, e As EventArgs) Handles flpRight.SizeChanged
        ResizeButtonsToPanel(flpRight)
    End Sub
    Private Sub tmrClock_Tick(sender As Object, e As EventArgs) Handles tmrClock.Tick
        UpdateHeaderClock()
    End Sub
    Private Sub UpdateHeaderClock()
        Dim now As DateTime = DateTime.Now

        lblDateTime.Text = _buildInfo & "          " &
                       now.ToString("dddd") & "  " &
                       now.ToString("MM-dd-yyyy") & "          " &
                       now.ToString("hh:mm:ss tt")
    End Sub
    Private Sub BuildMenu()
        If flpLeft Is Nothing OrElse flpRight Is Nothing Then Return

        flpLeft.Controls.Clear()
        flpRight.Controls.Clear()
        ' Left column
        AddMenuButton(flpLeft, "A", "Shop Card Generator")
        AddMenuButton(flpLeft, "B", "Invoice Generator")
        AddMenuButton(flpLeft, "C", "Checks and Cash Receipts")
        AddMenuButton(flpLeft, "D", "View Sales Journal")
        AddMenuButton(flpLeft, "E", "View Log Book")
        AddMenuButton(flpLeft, "F", "Price List Program")
        AddMenuButton(flpLeft, "G", "Print Records / Void Invoices")
        AddMenuButton(flpLeft, "H", "Quick Message Flashing")
        AddMenuButton(flpLeft, "I", "Backup Price List & Rolodex")
        AddMenuButton(flpLeft, "J", "Print Out Customers Actual Names")
        AddMenuButton(flpLeft, "K", "Cash Disbursements")
        AddMenuButton(flpLeft, "L", "Business Expenses Account")
        AddMenuButton(flpLeft, "M", "Quotation Form Generator")
        AddMenuButton(flpLeft, "N", "Rolodex")

        ' Right column (removed: S, U, V, W, +, 5)
        AddMenuButton(flpRight, "O", "Copy Spec Index")
        AddMenuButton(flpRight, "P", "Entire Ledger Viewing")
        AddMenuButton(flpRight, "Q", "Word Processor")
        AddMenuButton(flpRight, "R", "Find Word Processor Text")
        AddMenuButton(flpRight, "T", "Change Date or Time")
        AddMenuButton(flpRight, "X", "Typewriter Mode")
        AddMenuButton(flpRight, "Y", "Ed Dean's Personal Backup")
        AddMenuButton(flpRight, "Z", "Personal Calendar")

        AddMenuButton(flpRight, "1", "Mileage Tracking")
        AddMenuButton(flpRight, "2", "Product Purchasing")
        AddMenuButton(flpRight, "3", "Miscellaneous Menu")
        AddMenuButton(flpRight, "4", "Add Entries to Log Book")
        AddMenuButton(flpRight, "6", "Cadmium Cards")
        AddMenuButton(flpRight, "7", "Emergency PAYROLL System")
        AddMenuButton(flpRight, "?", "About AMiOffice Menu System")
    End Sub

    Private Function MakeHeaderLabel(text As String) As Label
        Dim lbl As New Label()
        lbl.AutoSize = False
        lbl.Height = 32
        lbl.TextAlign = ContentAlignment.MiddleLeft
        lbl.Font = New Font(Me.Font, FontStyle.Bold)
        lbl.Text = text
        lbl.Margin = New Padding(3, 3, 3, 8)
        lbl.Width = 1000 ' will be resized later
        Return lbl
    End Function

    Private Sub AddMenuButton(panel As FlowLayoutPanel, key As String, text As String)
        Dim btn As New Button()

        btn.AutoSize = False
        btn.Height = 36
        btn.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        btn.TextAlign = ContentAlignment.MiddleLeft
        btn.Text = "(" & key & ") " & text
        btn.Tag = key
        btn.Margin = New Padding(3, 3, 3, 6)

        ' Force black buttons with white text
        btn.UseVisualStyleBackColor = False
        btn.BackColor = Color.Black
        btn.ForeColor = Color.White
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderColor = Color.DimGray
        btn.FlatAppearance.BorderSize = 1

        ' Optional: nicer hover / click feedback
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(32, 32, 32)
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(64, 64, 64)

        btn.AutoEllipsis = True

        AddHandler btn.Click, Sub(sender, args) HandleMenuKey(CStr(btn.Tag))

        panel.Controls.Add(btn)
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        ResizeButtonsToPanel(flpLeft)
        ResizeButtonsToPanel(flpRight)
    End Sub

    Private Sub ResizeButtonsToPanel(panel As FlowLayoutPanel)
        If panel Is Nothing Then Return

        Dim targetWidth As Integer =
        panel.ClientSize.Width -
        panel.Padding.Left - panel.Padding.Right -
        SystemInformation.VerticalScrollBarWidth - 6

        If targetWidth < 150 Then targetWidth = 150

        For Each c As Control In panel.Controls
            Dim btn = TryCast(c, Button)
            Dim lbl = TryCast(c, Label)

            If btn IsNot Nothing Then
                btn.Width = targetWidth
            ElseIf lbl IsNot Nothing Then
                lbl.Width = targetWidth
            End If
        Next
    End Sub

    Private Sub FormMainMenu_KeyPress(sender As Object, e As KeyPressEventArgs) Handles Me.KeyPress
        Dim ch As String = e.KeyChar.ToString()
        If ch = vbCr OrElse ch = vbLf Then Return
        HandleMenuKey(ch)
    End Sub

    Private Sub HandleMenuKey(ch As String)
        If String.IsNullOrEmpty(ch) Then Return

        Dim up As String = ch
        If up.Length = 1 AndAlso Char.IsLetter(up(0)) Then
            up = up.ToUpperInvariant()
        End If

        Select Case up
            Case "A" : NotYet("Shop Card Generator")
            Case "B" : NotYet("Invoice Generator")
            Case "C"
                Using f As New FormLedgerMenu()
                    f.ShowDialog(Me)
                End Using
            Case "D" : NotYet("View Sales Journal (SALES)")
            Case "E" : NotYet("View Log Book (LOGBOOK)")
            Case "F" : NotYet("Price List Program (plist)")
            Case "G" : NotYet("Print/Void Invoices (BOOT)")
            Case "H" : NotYet("Quick Message Flashing")

            Case "I"
                Try
                    Cursor = Cursors.WaitCursor
                    MigrationService.EnsureFoldersAndMigrateOnce()
                    BackupService.RunBackupPriceListAndRolodex()
                    MessageBox.Show("Backup complete." & Environment.NewLine & AppPaths.BackupDir,
                                    "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show("Backup failed: " & ex.Message,
                                    "Backup", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Finally
                    Cursor = Cursors.Default
                End Try

            Case "J" : NotYet("Print Out Customers Actual Names (spool real names)")
            Case "K" : NotYet("Cash Disbursements (BILL)")
            Case "L" : NotYet("Business Expenses Account (password)")
            Case "M" : NotYet("Quotation Form Generator (QUOTE)")
            Case "N" : NotYet("Rolodex (PHONE)")

            Case "O" : NotYet("Copy Spec Index")
            Case "P" : NotYet("Entire Ledger Viewing (ENTIRE)")
            Case "Q" : NotYet("Word Processor")
            Case "R" : NotYet("Find Word Processor Text")

            Case "T" : NotYet("Change Date or Time")
            Case "X" : NotYet("Typewriter Mode")
            Case "Y" : NotYet("Ed Dean's Personal Backup")
            Case "Z"
                Using f As New FormPersonalCalendar()
                    f.ShowDialog(Me)
                End Using

            Case "1"
                Using f As New FormMileageTracking()
                    f.ShowDialog(Me)
                End Using
            Case "2" : NotYet("Product Purchasing")
            Case "3" : NotYet("Miscellaneous Menu")
            Case "4" : NotYet("Add Entries to Log Book")
            Case "6" : NotYet("Cadmium Cards")
            Case "7" : NotYet("Emergency PAYROLL System")

            Case "?"
                Using f As New FormAbout()
                    f.ShowDialog(Me)
                End Using

            Case Else
                ' ignore unknown keys
        End Select
    End Sub

    Private Sub NotYet(feature As String)
        MessageBox.Show("Not implemented yet: " & feature, "Port status", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

End Class