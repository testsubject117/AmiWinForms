Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Option F - Price List Program
''' DOS source: PLIST.ASC
''' Manages customer price lists stored in PRC\*.PRC files
''' </summary>
Public Class FormPriceList
    Inherits DosMenuFormBase

    Private _currentCustomer As String = ""
    Private _priceListFile As String = ""
    Private _procedures As New List(Of ProcedureItem)
    Private _lblPrompt As Label
    Private _isPromptingForCustomer As Boolean = False
    Private _txtCustomerName As TextBox
    Private _lblCustomerPrompt As Label

    Private Structure ProcedureItem
        Public ProcedureName As String
        Public EffectiveDate As String
        Public MinCharge As Decimal
        Public Price As Decimal
        Public PriceType As String ' "/#" or "/EA"
    End Structure

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        Me.Width = 1000
        Me.Height = 800  ' Increased from 720 to 800 for more vertical space
        Me.BackColor = Color.Black

        ShowVersionInHeader = False
        UpdateHeaderClock()

        ' Configure layout: left column + right column for (w)
        ' Disable AutoScroll - buttons should fit without scrolling
        flpLeft.AutoScroll = False
        flpRight.AutoScroll = False

        StretchButtonsToPanelWidth = True
        flpLeft.Padding = New Padding(2, 0, 2, 0)  ' Reduced left from 4 to 2, minimal on both sides
        flpLeft.Margin = New Padding(0)
        flpLeft.BackColor = Color.FromArgb(32, 32, 32)  ' Dark gray like main menu

        ' Enable right panel for (w) option - make it black
        flpRight.Padding = New Padding(2, 0, 2, 0)  ' Minimal on both sides
        flpRight.Margin = New Padding(0)
        flpRight.Visible = True
        flpRight.Enabled = True
        flpRight.BackColor = Color.Black  ' Black background for right side

        ' DON'T make flpLeft span both columns - keep it single column like DOS
        ' The buttons will be smaller with tighter spacing

        ' Make all parent panels dark gray to match main menu
        For Each ctrl As Control In Me.Controls
            If TypeOf ctrl Is Panel Then
                ctrl.BackColor = Color.FromArgb(32, 32, 32)
                For Each inner As Control In ctrl.Controls
                    If TypeOf inner Is Panel Then
                        inner.BackColor = Color.FromArgb(32, 32, 32)
                    ElseIf TypeOf inner Is TableLayoutPanel Then
                        inner.BackColor = Color.FromArgb(32, 32, 32)
                        For Each nested As Control In inner.Controls
                            If TypeOf nested Is Panel Then
                                nested.BackColor = Color.FromArgb(32, 32, 32)
                            End If
                        Next
                    End If
                Next
            ElseIf TypeOf ctrl Is TableLayoutPanel Then
                ctrl.BackColor = Color.FromArgb(32, 32, 32)
            End If
        Next

        ' Defer initialization until after form is fully loaded
        Me.BeginInvoke(New Action(Sub()
                                      Try
                                          If Me.IsDisposed OrElse Not Me.IsHandleCreated Then
                                              Return
                                          End If
                                          AddPromptLabel()
                                          BuildPriceListMenu()
                                      Catch ex As Exception
                                          ' Log the error but don't show message box or close on first init
                                          Debug.WriteLine("Price List init error: " & ex.Message & vbCrLf & ex.StackTrace)
                                      End Try
                                  End Sub))
    End Sub

    Private Sub AddPromptLabel()
        ' Find the bottom panel (it's added by DosMenuFormBase)
        Dim bottomPanel As Panel = Nothing
        For Each ctrl As Control In Me.Controls
            If TypeOf ctrl Is Panel Then
                Dim hostPanel = DirectCast(ctrl, Panel)
                For Each inner As Control In hostPanel.Controls
                    If TypeOf inner Is Panel AndAlso inner.Dock = DockStyle.Bottom Then
                        bottomPanel = DirectCast(inner, Panel)
                        Exit For
                    End If
                Next
                If bottomPanel IsNot Nothing Then Exit For
            End If
        Next

        If bottomPanel IsNot Nothing Then
            _lblPrompt = New Label()
            _lblPrompt.Text = "Choose a letter above"
            _lblPrompt.AutoSize = True
            _lblPrompt.Font = New Font("Consolas", 11.0F, FontStyle.Bold)
            _lblPrompt.ForeColor = Color.Yellow
            _lblPrompt.BackColor = Color.FromArgb(32, 32, 32)
            _lblPrompt.Anchor = AnchorStyles.Left Or AnchorStyles.Bottom
            _lblPrompt.Left = 12
            _lblPrompt.Top = bottomPanel.Height - 32
            bottomPanel.Controls.Add(_lblPrompt)
            _lblPrompt.BringToFront()
        End If
    End Sub

    Private Sub BuildPriceListMenu()
        ClearMenu()
        SetMenuTitle("PRICE LIST")

        ' Get last customer from CUSTOMER.PLS if exists
        LoadLastCustomer()

        ' Always show prompt (DOS behavior - lets user confirm or change customer)
        PromptForCustomer()
    End Sub

    Private Sub LoadLastCustomer()
        Try
            Dim customerFile = Path.Combine(LegacyDataPaths.BaseDataDir, "CUSTOMER.PLS")
            If File.Exists(customerFile) Then
                Dim content = File.ReadAllText(customerFile).Trim()
                If content.StartsWith("""") AndAlso content.EndsWith("""") Then
                    content = content.Substring(1, content.Length - 2)
                End If
                If Not String.IsNullOrEmpty(content) Then
                    _currentCustomer = content
                    _priceListFile = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC", _currentCustomer + ".PRC")
                End If
            End If
        Catch
            ' Ignore errors
        End Try
    End Sub

    Private Sub PromptForCustomer()
        _isPromptingForCustomer = True

        ' Hide the bottom prompt when entering customer name
        If _lblPrompt IsNot Nothing Then
            _lblPrompt.Visible = False
        End If

        ClearMenu()
        SetMenuTitle("PRICE LIST")

        ' Set title to white (DOS style for customer prompt)
        lblMainMenu.ForeColor = Color.White

        ' Hide the flow panels completely
        flpLeft.Visible = False
        flpRight.Visible = False

        ' Find all panels and make them black
        For Each ctrl As Control In Me.Controls
            If TypeOf ctrl Is Panel Then
                ctrl.BackColor = Color.Black
                For Each inner As Control In ctrl.Controls
                    If TypeOf inner Is Panel OrElse TypeOf inner Is TableLayoutPanel Then
                        inner.BackColor = Color.Black
                        For Each nested As Control In inner.Controls
                            If TypeOf nested Is Panel OrElse TypeOf nested Is TableLayoutPanel Then
                                nested.BackColor = Color.Black
                            End If
                        Next
                    End If
                Next
            End If
        Next

        ' Create DOS-style customer name prompt - inline with text field
        ' Position relative to the form
        _lblCustomerPrompt = New Label()
        _lblCustomerPrompt.AutoSize = True
        _lblCustomerPrompt.Location = New Point(30, 160)  ' Below header (header is ~140px)
        _lblCustomerPrompt.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _lblCustomerPrompt.ForeColor = Color.FromArgb(170, 170, 170)
        _lblCustomerPrompt.BackColor = Color.Black

        Dim promptText = "Enter Customers Name [ENTER = " &
                        If(String.IsNullOrEmpty(_currentCustomer), "NEW", _currentCustomer) & "] ?"
        _lblCustomerPrompt.Text = promptText

        _txtCustomerName = New TextBox()
        _txtCustomerName.Location = New Point(520, 158)  ' To the right of the prompt text, aligned
        _txtCustomerName.Size = New Size(300, 30)
        _txtCustomerName.Font = New Font("Consolas", 12.0F, FontStyle.Regular)
        _txtCustomerName.BackColor = Color.White
        _txtCustomerName.ForeColor = Color.Black
        _txtCustomerName.MaxLength = 8
        _txtCustomerName.CharacterCasing = CharacterCasing.Upper

        AddHandler _txtCustomerName.KeyDown, AddressOf CustomerName_KeyDown

        ' Add directly to the form
        Me.Controls.Add(_lblCustomerPrompt)
        Me.Controls.Add(_txtCustomerName)
        _lblCustomerPrompt.BringToFront()
        _txtCustomerName.BringToFront()
        _txtCustomerName.Focus()
    End Sub

    Private Sub CustomerName_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            e.SuppressKeyPress = True
            AcceptCustomerName()
        ElseIf e.KeyCode = Keys.Escape Then
            e.Handled = True
            e.SuppressKeyPress = True
            If String.IsNullOrEmpty(_currentCustomer) Then
                Me.Close()
            Else
                ShowPriceListMenu()
            End If
        End If
    End Sub

    Private Sub AcceptCustomerName()
        Dim input = _txtCustomerName.Text.Trim().ToUpperInvariant()

        ' If empty, keep current customer (if exists) or close
        If String.IsNullOrEmpty(input) Then
            If String.IsNullOrEmpty(_currentCustomer) Then
                Me.Close()
                Return
            End If
            ' Keep current customer
            ShowPriceListMenu()
            Return
        End If

        ' Validate customer name
        If input.Length > 8 Then input = input.Substring(0, 8)

        ' Basic validation
        If Not System.Text.RegularExpressions.Regex.IsMatch(input, "^[A-Z0-9]+$") Then
            MessageBox.Show("Customer name must contain only letters and numbers (no spaces, periods, or dashes).",
                           "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _txtCustomerName.Clear()
            _txtCustomerName.Focus()
            Return
        End If

        _currentCustomer = input
        _priceListFile = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC", _currentCustomer + ".PRC")

        ' Save last customer
        Try
            Dim customerFile = Path.Combine(LegacyDataPaths.BaseDataDir, "CUSTOMER.PLS")
            Directory.CreateDirectory(Path.GetDirectoryName(customerFile))
            File.WriteAllText(customerFile, """" & _currentCustomer & """")
        Catch
            ' Ignore errors
        End Try

        ShowPriceListMenu()
    End Sub

    Private Sub ShowPriceListMenu()
        _isPromptingForCustomer = False

        ' Restore title to yellow (DOS style for menu)
        lblMainMenu.ForeColor = Color.Yellow

        ' Show both flow panels: left for main menu, right for (w)
        flpLeft.Visible = True
        flpRight.Visible = True

        ' Show the bottom prompt for the menu
        If _lblPrompt IsNot Nothing Then
            _lblPrompt.Visible = True
        End If

        ' Clean up prompt controls if they exist
        If _lblCustomerPrompt IsNot Nothing Then
            Me.Controls.Remove(_lblCustomerPrompt)
            _lblCustomerPrompt.Dispose()
            _lblCustomerPrompt = Nothing
        End If
        If _txtCustomerName IsNot Nothing Then
            Me.Controls.Remove(_txtCustomerName)
            _txtCustomerName.Dispose()
            _txtCustomerName = Nothing
        End If

        ClearMenu()
        SetMenuTitle("PRICE LIST - " & _currentCustomer)

        ' Use smaller, more compact buttons (DOS style)
        AddCompactMenuButton("A", "List procedures to screen", Sub() ListProceduresToScreen())
        AddCompactMenuButton("B", "List procedures to printer", Sub() PrintProcedures())
        AddCompactMenuButton("C", "Add procedures", Sub() AddProcedure())
        AddCompactMenuButton("D", "Delete procedures", Sub() DeleteProcedure())
        AddCompactMenuButton("E", "Change customers name", Sub() ChangeCustomer())
        AddCompactMenuButton("F", "List all customers to Screen", Sub() ListCustomersToScreen())
        AddCompactMenuButton("G", "List all customers to Printer", Sub() PrintCustomersList())
        AddCompactMenuButton("H", "Find a Part Number", Sub() FindPartNumber())
        AddCompactMenuButton("I", "Sort price list for " & _currentCustomer, Sub() SortPriceList())
        AddCompactMenuButton("J", "Erase entire price list for " & _currentCustomer, Sub() ErasePriceList())
        AddCompactMenuButton("K", "Increase some price lists by a %", Sub() IncreasePricesByPercent())
        AddCompactMenuButton("L", "Permanently change filename or realname", Sub() ChangeFilenameOrRealname())
        AddCompactMenuButton("M", "View all Standard Procedures", Sub() ViewStandardProcedures())
        AddCompactMenuButton("N", "Sort Customers Actual Names", Sub() SortCustomerNames())
        AddCompactMenuButton("O", "Scan all price lists for errors", Sub() ScanForErrors())
        AddCompactMenuButton("P", "Scan price lists for errors and min. charge's that are too low (PROCEDURE)", Sub() ScanForLowMinCharges())
        AddCompactMenuButton("S", "Scan all price lists for Anything Else", Sub() ScanForAnything())
        AddCompactMenuButton("T", "Change some customers min charge for a procedure", Sub() ChangeMinChargeForProcedure())
        AddCompactMenuButton("U", "Update Environmental Surcharge customer list", Sub() UpdateEnvironmentalSurcharge())
        AddCompactMenuButton("Q", "Quit", Sub() Me.Close())

        ' Add spacer labels to right panel to align (w) with (Q)
        ' Each button is 30px + 2px margin (top+bottom) = 32px, so we need 19 spacers to match 19 left buttons before (Q)
        For i As Integer = 1 To 19
            Dim spacer As New Label()
            spacer.Height = 32
            spacer.Width = 1
            spacer.BackColor = Color.Black  ' Match black background on right
            spacer.Margin = New Padding(0)
            flpRight.Controls.Add(spacer)
        Next

        ' Now add (w) to the right panel so it appears next to (Q)
        AddCompactMenuButtonToPanel(flpRight, "w", "Edit in Word Processor", Sub() EditInWordProcessor())
    End Sub

    ' Compact button version with smaller font and tighter spacing (left panel)
    Private Sub AddCompactMenuButton(key As String, text As String, handler As Action)
        AddCompactMenuButtonToPanel(flpLeft, key, text, handler)
    End Sub

    ' Compact button helper that can target either panel
    Private Sub AddCompactMenuButtonToPanel(panel As FlowLayoutPanel, key As String, text As String, handler As Action)
        Dim btn As New Button()

        btn.AutoSize = False
        btn.Height = 30  ' Increased from 26 to 30
        btn.Font = New Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point)  ' Increased from 8.5 to 9.5
        btn.TextAlign = ContentAlignment.MiddleLeft
        btn.Text = "(" & key & ") " & text
        btn.Tag = key
        btn.Margin = New Padding(0, 1, 0, 1)  ' No left/right margin, small vertical gap

        btn.UseVisualStyleBackColor = False
        btn.BackColor = Color.Black
        btn.ForeColor = Color.White
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderColor = Color.DimGray  ' Restore gray borders
        btn.FlatAppearance.BorderSize = 1
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(32, 32, 32)
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(64, 64, 64)
        btn.AutoEllipsis = True

        AddHandler btn.Click, Sub() handler()

        ' Size to panel width leaving only padding space
        If StretchButtonsToPanelWidth Then
            btn.Width = panel.ClientSize.Width - panel.Padding.Left - panel.Padding.Right
        End If

        panel.Controls.Add(btn)
    End Sub

    Private Sub ListProceduresToScreen()
        If Not LoadProcedures() Then Return

        Using frm As New FormPriceListView()
            Dim objList As New List(Of Object)
            For Each proc In _procedures
                objList.Add(proc)
            Next
            frm.SetData(_currentCustomer, objList)
            frm.ShowDialog(Me)
        End Using
    End Sub

    Private Sub PrintProcedures()
        NotYet("Print procedures to printer")
    End Sub

    Private Sub AddProcedure()
        Using frm As New FormPriceListAdd()
            frm.SetCustomer(_currentCustomer, _priceListFile)
            If frm.ShowDialog(Me) = DialogResult.OK Then
                MessageBox.Show(_currentCustomer & " has been updated.", "Success",
                               MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Using
    End Sub

    Private Sub DeleteProcedure()
        NotYet("Delete procedure")
    End Sub

    Private Sub ChangeCustomer()
        _currentCustomer = ""
        PromptForCustomer()
    End Sub

    Private Sub ListCustomersToScreen()
        Try
            Dim prcDir = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC")
            If Not Directory.Exists(prcDir) Then
                MessageBox.Show("No price list directory found." & vbCrLf & vbCrLf &
                               "Expected: " & prcDir, "Not Found",
                               MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim files = Directory.GetFiles(prcDir, "*.PRC")
            If files.Length = 0 Then
                MessageBox.Show("No customer price lists found.", "No Customers",
                               MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim customers = files.Select(Function(f) Path.GetFileNameWithoutExtension(f)).OrderBy(Function(c) c).ToList()

            Dim msg As New System.Text.StringBuilder()
            msg.AppendLine("Customer Price Lists:")
            msg.AppendLine()
            For Each cust In customers
                msg.AppendLine("  " & cust)
            Next
            msg.AppendLine()
            msg.AppendLine("Total: " & customers.Count & " customers")

            MessageBox.Show(msg.ToString(), "All Customers",
                           MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Error listing customers: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub FindPartNumber()
        Dim search = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter procedure or part number to find:",
            "Find Part Number", "")

        If String.IsNullOrEmpty(search) Then Return

        If Not LoadProcedures() Then Return

        search = search.ToUpperInvariant()
        Dim matches = _procedures.Where(Function(p) p.ProcedureName.ToUpperInvariant().Contains(search)).ToList()

        If matches.Count = 0 Then
            MessageBox.Show(search & " doesn't exist.", "Not Found",
                           MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim msg As New System.Text.StringBuilder()
        msg.AppendLine("Found " & matches.Count & " match(es):")
        msg.AppendLine()
        For Each proc In matches
            msg.AppendLine("PROCEDURE: " & proc.ProcedureName)
            msg.AppendLine("EFFECTIVE DATE: " & proc.EffectiveDate)
            msg.AppendLine("MIN. CHARGE: " & proc.MinCharge.ToString("C"))
            msg.AppendLine("PRICE: " & proc.Price.ToString("C") & proc.PriceType)
            msg.AppendLine()
        Next

        MessageBox.Show(msg.ToString(), "Search Results",
                       MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Function LoadProcedures() As Boolean
        _procedures.Clear()

        If Not File.Exists(_priceListFile) Then
            Dim result = MessageBox.Show(
                _currentCustomer & " does not exist." & vbCrLf & vbCrLf &
                "Is this a brand new customer to be added?",
                "Customer Not Found", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ' Create empty price list file
                Try
                    Directory.CreateDirectory(Path.GetDirectoryName(_priceListFile))
                    File.WriteAllText(_priceListFile, "")
                    Return True
                Catch ex As Exception
                    MessageBox.Show("Error creating price list: " & ex.Message, "Error",
                                   MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Return False
                End Try
            End If
            Return False
        End If

        Try
            Using reader As New StreamReader(_priceListFile)
                While Not reader.EndOfStream
                    Dim line = reader.ReadLine()
                    If String.IsNullOrWhiteSpace(line) Then Continue While

                    ' Parse DOS WRITE# format: "procedure","date",mincharge,price,"type"
                    Dim parts = ParseDosWriteLine(line)
                    If parts.Length >= 5 Then
                        Dim item As New ProcedureItem With {
                            .ProcedureName = parts(0),
                            .EffectiveDate = parts(1),
                            .MinCharge = CDec(parts(2)),
                            .Price = CDec(parts(3)),
                            .PriceType = parts(4)
                        }
                        _procedures.Add(item)
                    End If
                End While
            End Using
            Return True
        Catch ex As Exception
            MessageBox.Show("Error loading price list: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Private Function ParseDosWriteLine(line As String) As String()
        ' Simple parser for DOS WRITE# format
        Dim parts As New List(Of String)()
        Dim current As New System.Text.StringBuilder()
        Dim inQuotes = False

        For i = 0 To line.Length - 1
            Dim c = line(i)
            If c = """"c Then
                inQuotes = Not inQuotes
            ElseIf c = ","c AndAlso Not inQuotes Then
                parts.Add(current.ToString().Trim())
                current.Clear()
            Else
                current.Append(c)
            End If
        Next

        If current.Length > 0 Then
            parts.Add(current.ToString().Trim())
        End If

        Return parts.ToArray()
    End Function

    ' ============================================================================
    ' Additional menu functions (not yet implemented)
    ' ============================================================================

    Private Sub PrintCustomersList()
        NotYet("Print all customers to printer")
    End Sub

    Private Sub SortPriceList()
        NotYet("Sort price list for " & _currentCustomer)
    End Sub

    Private Sub ErasePriceList()
        NotYet("Erase entire price list for " & _currentCustomer)
    End Sub

    Private Sub IncreasePricesByPercent()
        NotYet("Increase some price lists by a %")
    End Sub

    Private Sub ChangeFilenameOrRealname()
        NotYet("Permanently change filename or realname")
    End Sub

    Private Sub ViewStandardProcedures()
        NotYet("View all Standard Procedures")
    End Sub

    Private Sub SortCustomerNames()
        NotYet("Sort Customers Actual Names")
    End Sub

    Private Sub ScanForErrors()
        NotYet("Scan all price lists for errors")
    End Sub

    Private Sub ScanForLowMinCharges()
        NotYet("Scan price lists for errors and min. charge's that are too low")
    End Sub

    Private Sub ScanForAnything()
        NotYet("Scan all price lists for Anything Else")
    End Sub

    Private Sub ChangeMinChargeForProcedure()
        NotYet("Change some customers min charge for a procedure")
    End Sub

    Private Sub UpdateEnvironmentalSurcharge()
        NotYet("Update Environmental Surcharge customer list")
    End Sub

    Private Sub EditInWordProcessor()
        NotYet("Edit in Word Processor")
    End Sub
End Class
