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
    Private _isSearchingForPart As Boolean = False
    Private _txtPartSearch As TextBox
    Private _lblPartPrompt As Label
    Private _partSearchResults As List(Of ProcedureItem)
    Private _currentPartResultIndex As Integer = 0
    Private _isPromptingForCustomer As Boolean = False
    Private _txtCustomerName As TextBox
    Private _lblCustomerPrompt As Label
    Private _lblPossibleHeader As Label
    Private _lblPossibleList As Label

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

        ' Enable right panel for (w) option - make it black (it only has spacers + one button)
        flpRight.Padding = New Padding(2, 0, 2, 0)  ' Minimal on both sides
        flpRight.Margin = New Padding(0)
        flpRight.Visible = True
        flpRight.Enabled = True
        flpRight.BackColor = Color.Black  ' Black background - right side only has (w) at bottom

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

        ' Check if customer file exists
        Dim testFile = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC", input + ".PRC")
        If Not File.Exists(testFile) Then
            ' Customer not found - show possible matches (DOS behavior)
            ShowPossibleCustomers(input)
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

    Private Sub ShowPossibleCustomers(searchTerm As String)
        ' Get all customer files
        Dim prcDir = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC")
        If Not Directory.Exists(prcDir) Then
            MessageBox.Show("No customer directory found.", "Not Found",
                           MessageBoxButtons.OK, MessageBoxIcon.Information)
            _txtCustomerName.Clear()
            _txtCustomerName.Focus()
            Return
        End If

        Dim files = Directory.GetFiles(prcDir, "*.PRC")
        If files.Length = 0 Then
            MessageBox.Show("No customer price lists found.", "Not Found",
                           MessageBoxButtons.OK, MessageBoxIcon.Information)
            _txtCustomerName.Clear()
            _txtCustomerName.Focus()
            Return
        End If

        ' Find fuzzy matches (starts with first 2 characters - DOS behavior)
        Dim searchPrefix = If(searchTerm.Length >= 2, searchTerm.Substring(0, 2), searchTerm)
        Dim matches = files.Where(Function(f)
                                       Dim name = Path.GetFileNameWithoutExtension(f)
                                       Return name.StartsWith(searchPrefix, StringComparison.OrdinalIgnoreCase)
                                   End Function).OrderBy(Function(f) Path.GetFileName(f)).ToList()

        If matches.Count = 0 Then
            ' No matches - show all customers (sorted)
            matches = files.OrderBy(Function(f) Path.GetFileName(f)).ToList()
        End If

        ' Clean up existing prompt controls
        If _lblCustomerPrompt IsNot Nothing Then
            Me.Controls.Remove(_lblCustomerPrompt)
            _lblCustomerPrompt.Dispose()
        End If
        If _txtCustomerName IsNot Nothing Then
            Me.Controls.Remove(_txtCustomerName)
            _txtCustomerName.Dispose()
        End If

        ' Show "POSSIBLE CUSTOMERS ARE:" message
        Dim lblPossible = New Label()
        lblPossible.AutoSize = False
        lblPossible.Location = New Point(30, 120)
        lblPossible.Size = New Size(900, 30)
        lblPossible.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        lblPossible.ForeColor = Color.FromArgb(170, 170, 170)
        lblPossible.BackColor = Color.Black
        lblPossible.Text = "Enter Customers Name [ENTER = " & searchTerm.ToUpper() & "] ?"

        Dim lblPossibleHeader = New Label()
        lblPossibleHeader.AutoSize = False
        lblPossibleHeader.Location = New Point(30, 150)
        lblPossibleHeader.Size = New Size(900, 30)
        lblPossibleHeader.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        lblPossibleHeader.ForeColor = Color.FromArgb(170, 170, 170)
        lblPossibleHeader.BackColor = Color.Black
        lblPossibleHeader.Text = "POSSIBLE CUSTOMERS ARE:"

        ' List possible customers
        Dim lblList = New Label()
        lblList.AutoSize = False
        lblList.Location = New Point(30, 190)
        lblList.Size = New Size(900, 400)
        lblList.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        lblList.ForeColor = Color.FromArgb(170, 170, 170)
        lblList.BackColor = Color.Black

        Dim customerList As New System.Text.StringBuilder()
        For Each f In matches.Take(20) ' Limit to first 20
            customerList.AppendLine(Path.GetFileName(f).ToLower())
        Next
        lblList.Text = customerList.ToString()

        ' Re-add input textbox
        _txtCustomerName = New TextBox()
        _txtCustomerName.Location = New Point(420, 118)
        _txtCustomerName.Size = New Size(300, 30)
        _txtCustomerName.Font = New Font("Consolas", 12.0F, FontStyle.Regular)
        _txtCustomerName.BackColor = Color.White
        _txtCustomerName.ForeColor = Color.Black
        _txtCustomerName.MaxLength = 8
        _txtCustomerName.CharacterCasing = CharacterCasing.Upper
        _txtCustomerName.Text = searchTerm
        AddHandler _txtCustomerName.KeyDown, AddressOf CustomerName_KeyDown

        Me.Controls.Add(lblPossible)
        Me.Controls.Add(lblPossibleHeader)
        Me.Controls.Add(lblList)
        Me.Controls.Add(_txtCustomerName)

        lblPossible.BringToFront()
        lblPossibleHeader.BringToFront()
        lblList.BringToFront()
        _txtCustomerName.BringToFront()
        _txtCustomerName.SelectAll()
        _txtCustomerName.Focus()

        ' Store references for cleanup
        _lblCustomerPrompt = lblPossible
        _lblPossibleHeader = lblPossibleHeader
        _lblPossibleList = lblList
    End Sub

    Private Sub ShowPriceListMenu()
        _isPromptingForCustomer = False

        ' Restore title to yellow (DOS style for menu)
        lblMainMenu.ForeColor = Color.Yellow

        ' Show both flow panels: left for main menu, right for (w)
        flpLeft.Visible = True
        flpRight.Visible = True

        ' Restore proper panel colors (left = dark gray buttons, right = black void with only (w) at bottom)
        flpLeft.BackColor = Color.FromArgb(32, 32, 32)
        flpRight.BackColor = Color.Black

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
        If _lblPossibleHeader IsNot Nothing Then
            Me.Controls.Remove(_lblPossibleHeader)
            _lblPossibleHeader.Dispose()
            _lblPossibleHeader = Nothing
        End If
        If _lblPossibleList IsNot Nothing Then
            Me.Controls.Remove(_lblPossibleList)
            _lblPossibleList.Dispose()
            _lblPossibleList = Nothing
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
            spacer.BackColor = Color.Black  ' Match black void on right
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

        ' Register hotkey (base class handles text-input protection)
        RegisterHotkey(key, handler)

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
        ' (B) Print procedures to printer
        If Not LoadProcedures() Then Return

        If _procedures.Count = 0 Then
            MessageBox.Show("No procedures to print.", "No Data",
                           MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Try
            ' Create print document with tighter margins
            Dim printDoc As New System.Drawing.Printing.PrintDocument()
            printDoc.DefaultPageSettings.Margins = New System.Drawing.Printing.Margins(50, 50, 50, 50)
            Dim currentIndex = 0
            Dim pageNum = 0

            AddHandler printDoc.PrintPage, Sub(sender, e)
                                                pageNum += 1
                                                Dim font As New Font("Courier New", 11)
                                                Dim boldFont As New Font("Courier New", 12, FontStyle.Bold)
                                                Dim x As Single = e.MarginBounds.Left
                                                Dim y As Single = e.MarginBounds.Top
                                                Dim lineHeight = font.GetHeight(e.Graphics)
                                                Dim pageWidth = e.MarginBounds.Width

                                                ' Column positions based on page width
                                                Dim colProcedure = x
                                                Dim colEftDate = x + CSng(pageWidth * 0.35)
                                                Dim colMinCharge = x + CSng(pageWidth * 0.55)
                                                Dim colPrice = x + CSng(pageWidth * 0.75)

                                                ' Title
                                                e.Graphics.DrawString("****** " & _currentCustomer & " ******    PAGE " & pageNum.ToString(), boldFont,
                                      Brushes.Black, x, y)
                                                y += lineHeight * 2

                                                ' Column headers
                                                e.Graphics.DrawString("PROCEDURE", boldFont, Brushes.Black, colProcedure, y)
                                                e.Graphics.DrawString("EFT DATE", boldFont, Brushes.Black, colEftDate, y)
                                                e.Graphics.DrawString("MIN. CHARGE", boldFont, Brushes.Black, colMinCharge, y)
                                                e.Graphics.DrawString("PRICE", boldFont, Brushes.Black, colPrice, y)
                                                y += lineHeight * 1.5

                                                ' Separator line
                                                e.Graphics.DrawString(New String("="c, 72), font, Brushes.Black, x, y)
                                                y += lineHeight

                                                ' Print procedures
                                                While currentIndex < _procedures.Count AndAlso y + lineHeight < e.MarginBounds.Bottom
                                                    Dim proc = _procedures(currentIndex)
                                                    e.Graphics.DrawString(proc.ProcedureName, font, Brushes.Black, colProcedure, y)
                                                    e.Graphics.DrawString(proc.EffectiveDate, font, Brushes.Black, colEftDate, y)
                                                    e.Graphics.DrawString("$ " & proc.MinCharge.ToString("F4"), font, Brushes.Black, colMinCharge, y)
                                                    e.Graphics.DrawString("$ " & proc.Price.ToString("F4") & proc.PriceType, font, Brushes.Black, colPrice, y)
                                                    y += lineHeight
                                                    currentIndex += 1
                                                End While

                                                ' Footer on last page
                                                If currentIndex >= _procedures.Count Then
                                                    y += lineHeight
                                                    e.Graphics.DrawString("Total: " & _procedures.Count & " procedures", boldFont,
                                          Brushes.Black, x, y)
                                                End If

                                                e.HasMorePages = (currentIndex < _procedures.Count)
                                            End Sub

            ' Show print dialog
            Dim printDialog As New PrintDialog()
            printDialog.Document = printDoc
            If printDialog.ShowDialog() = DialogResult.OK Then
                printDoc.Print()
            End If

        Catch ex As Exception
            MessageBox.Show("Error printing procedures: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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
        ' (D) Delete procedure - DOS-style
        Using frm As New FormPriceListDelete()
            frm.SetCustomer(_currentCustomer, _priceListFile)
            frm.ShowDialog(Me)
        End Using
    End Sub

    Private Sub ChangeCustomer()
        ' Don't clear _currentCustomer - keep it to show in prompt as default
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

            ' Load customer list with real names and file info
            Dim customers = FormCustomerList.LoadCustomerList()

            ' Show in DOS-style viewer
            Using frm As New FormCustomerList()
                frm.SetData(customers)
                frm.ShowDialog(Me)
            End Using

        Catch ex As Exception
            MessageBox.Show("Error listing customers: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub FindPartNumber()
        ' (H) Find a Part Number - DOS-style inline prompt
        If Not LoadProcedures() Then Return

        ' Hide the bottom prompt when entering search
        If _lblPrompt IsNot Nothing Then
            _lblPrompt.Visible = False
        End If

        ' Clear menu buttons
        flpLeft.Controls.Clear()
        flpRight.Controls.Clear()

        ' Set black background like DOS (not blinding white!)
        flpLeft.BackColor = Color.Black
        flpRight.BackColor = Color.Black

        _isSearchingForPart = True
        _partSearchResults = New List(Of ProcedureItem)()
        _currentPartResultIndex = 0

        ' Prompt label
        _lblPartPrompt = New Label()
        _lblPartPrompt.AutoSize = True
        _lblPartPrompt.Font = New Font("Consolas", 11, FontStyle.Regular)
        _lblPartPrompt.ForeColor = Color.White
        _lblPartPrompt.BackColor = Color.Black
        _lblPartPrompt.Text = "Procedure-Part# ?"
        _lblPartPrompt.Margin = New Padding(20, 80, 0, 0)
        flpLeft.Controls.Add(_lblPartPrompt)

        ' Input textbox positioned inline after prompt
        _txtPartSearch = New TextBox()
        _txtPartSearch.Font = New Font("Consolas", 11, FontStyle.Regular)
        _txtPartSearch.BackColor = Color.Black
        _txtPartSearch.ForeColor = Color.White
        _txtPartSearch.BorderStyle = BorderStyle.FixedSingle
        _txtPartSearch.Width = 300
        _txtPartSearch.Margin = New Padding(10, 0, 0, 0)
        flpLeft.Controls.Add(_txtPartSearch)

        ' Change flow direction to left-to-right for inline display
        flpLeft.FlowDirection = FlowDirection.LeftToRight
        flpLeft.WrapContents = True

        AddHandler _txtPartSearch.KeyPress, AddressOf OnPartSearchKeyPress

        _txtPartSearch.Focus()
    End Sub

    Private Sub OnPartSearchKeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = ChrW(Keys.Enter) Then
            e.Handled = True
            Dim searchTerm = _txtPartSearch.Text.Trim()
            If String.IsNullOrEmpty(searchTerm) Then Return

            ExecutePartSearch(searchTerm)
        ElseIf e.KeyChar = ChrW(Keys.Escape) Then
            e.Handled = True
            CancelPartSearch()
        End If
    End Sub

    Private Sub ExecutePartSearch(searchTerm As String)
        searchTerm = searchTerm.ToUpperInvariant()

        ' Search for exact and partial matches
        Dim exactMatch As ProcedureItem? = Nothing
        _partSearchResults.Clear()
        _currentPartResultIndex = 0

        For Each proc In _procedures
            If proc.ProcedureName.ToUpperInvariant() = searchTerm Then
                exactMatch = proc
                Exit For
            ElseIf proc.ProcedureName.ToUpperInvariant().Contains(searchTerm) Then
                _partSearchResults.Add(proc)
            End If
        Next

        ' Exact match - show and done
        If exactMatch.HasValue Then
            ShowPartResult(exactMatch.Value, isExactMatch:=True, searchTerm:=searchTerm)
            Return
        End If

        ' No matches
        If _partSearchResults.Count = 0 Then
            ShowNoMatchFound(searchTerm)
            Return
        End If

        ' Show first partial match
        ShowPartResult(_partSearchResults(_currentPartResultIndex), isExactMatch:=False, searchTerm:=searchTerm)
    End Sub

    Private Sub ShowPartResult(proc As ProcedureItem, isExactMatch As Boolean, searchTerm As String)
        ' Clear search prompt controls
        flpLeft.Controls.Clear()
        flpRight.Controls.Clear()

        ' Reset flow direction
        flpLeft.FlowDirection = FlowDirection.TopDown
        flpLeft.WrapContents = False

        ' Show procedure details
        Dim lbl As New Label()
        lbl.AutoSize = True
        lbl.Font = New Font("Consolas", 11, FontStyle.Regular)
        lbl.ForeColor = Color.White
        lbl.BackColor = Color.Black
        lbl.Margin = New Padding(20, 80, 20, 0)

        Dim details As New System.Text.StringBuilder()
        details.AppendLine()
        details.AppendLine($"PROCEDURE:               {proc.ProcedureName}")
        details.AppendLine($"EFFECTIVE DATE:          {proc.EffectiveDate}")
        details.AppendLine($"MIN. CHARGE:             {proc.MinCharge:C}")
        details.AppendLine($"PRICE:                   {proc.Price:C}{proc.PriceType}")
        details.AppendLine()

        If isExactMatch Then
            details.AppendLine("Hit [ENTER]")
        Else
            details.AppendLine("Is this the part# you were looking for (Y/N)")
        End If

        lbl.Text = details.ToString()
        flpLeft.Controls.Add(lbl)

        ' Handle keypress for Y/N or Enter
        Dim handler As KeyPressEventHandler = Nothing
        handler = Sub(s, e)
                      If isExactMatch Then
                          If e.KeyChar = ChrW(Keys.Enter) OrElse e.KeyChar = ChrW(Keys.Escape) Then
                              e.Handled = True
                              RemoveHandler Me.KeyPress, handler
                              ReturnToMenu()
                          End If
                      Else
                          If e.KeyChar = "Y"c OrElse e.KeyChar = "y"c Then
                              e.Handled = True
                              RemoveHandler Me.KeyPress, handler
                              ReturnToMenu()
                          ElseIf e.KeyChar = "N"c OrElse e.KeyChar = "n"c Then
                              e.Handled = True
                              RemoveHandler Me.KeyPress, handler
                              ShowNextPartMatch()
                          ElseIf e.KeyChar = ChrW(Keys.Escape) Then
                              e.Handled = True
                              RemoveHandler Me.KeyPress, handler
                              ReturnToMenu()
                          End If
                      End If
                  End Sub

        AddHandler Me.KeyPress, handler
        Me.Focus()
    End Sub

    Private Sub ShowNextPartMatch()
        _currentPartResultIndex += 1

        If _currentPartResultIndex >= _partSearchResults.Count Then
            ' No more matches
            ShowNoMoreMatches()
        Else
            ' Show next match
            ShowPartResult(_partSearchResults(_currentPartResultIndex), isExactMatch:=False, searchTerm:="")
        End If
    End Sub

    Private Sub ShowNoMatchFound(searchTerm As String)
        ' Clear search prompt
        flpLeft.Controls.Clear()
        flpRight.Controls.Clear()

        ' Reset flow direction
        flpLeft.FlowDirection = FlowDirection.TopDown

        Dim lbl As New Label()
        lbl.AutoSize = True
        lbl.Font = New Font("Consolas", 11, FontStyle.Regular)
        lbl.ForeColor = Color.White
        lbl.BackColor = Color.Black
        lbl.Margin = New Padding(20, 80, 0, 0)
        lbl.Text = searchTerm & " DOESN'T EXIST" & vbCrLf & vbCrLf & "Hit [ENTER]"
        flpLeft.Controls.Add(lbl)

        Dim handler As KeyPressEventHandler = Nothing
        handler = Sub(s, e)
                      If e.KeyChar = ChrW(Keys.Enter) OrElse e.KeyChar = ChrW(Keys.Escape) Then
                          e.Handled = True
                          RemoveHandler Me.KeyPress, handler
                          ReturnToMenu()
                      End If
                  End Sub

        AddHandler Me.KeyPress, handler
        Me.Focus()
    End Sub

    Private Sub ShowNoMoreMatches()
        flpLeft.Controls.Clear()
        flpRight.Controls.Clear()

        ' Reset flow direction
        flpLeft.FlowDirection = FlowDirection.TopDown

        Dim lbl As New Label()
        lbl.AutoSize = True
        lbl.Font = New Font("Consolas", 11, FontStyle.Regular)
        lbl.ForeColor = Color.White
        lbl.BackColor = Color.Black
        lbl.Margin = New Padding(20, 80, 0, 0)
        lbl.Text = "No more matches found." & vbCrLf & vbCrLf & "Hit [ENTER]"
        flpLeft.Controls.Add(lbl)

        Dim handler As KeyPressEventHandler = Nothing
        handler = Sub(s, e)
                      If e.KeyChar = ChrW(Keys.Enter) OrElse e.KeyChar = ChrW(Keys.Escape) Then
                          e.Handled = True
                          RemoveHandler Me.KeyPress, handler
                          ReturnToMenu()
                      End If
                  End Sub

        AddHandler Me.KeyPress, handler
        Me.Focus()
    End Sub

    Private Sub CancelPartSearch()
        ReturnToMenu()
    End Sub

    Private Sub ReturnToMenu()
        ' Clean up search controls
        flpLeft.Controls.Clear()
        flpRight.Controls.Clear()

        ' Reset flow direction
        flpLeft.FlowDirection = FlowDirection.TopDown
        flpLeft.WrapContents = False

        ' Restore panel colors (left = gray buttons, right = black void)
        flpLeft.BackColor = Color.FromArgb(32, 32, 32)
        flpRight.BackColor = Color.Black

        _isSearchingForPart = False
        _partSearchResults?.Clear()
        _currentPartResultIndex = 0
        _lblPartPrompt = Nothing
        _txtPartSearch = Nothing

        ' Show the bottom prompt again
        If _lblPrompt IsNot Nothing Then
            _lblPrompt.Visible = True
        End If

        ' Restore menu
        ShowPriceListMenu()
    End Sub

    Private Function ShowProcedureDetails(proc As ProcedureItem, isExactMatch As Boolean, searchTerm As String) As Boolean
        ' OLD METHOD - keeping for compatibility but not used anymore
        ' Returns True if user wants to keep searching (for partial matches)
        ' Returns False if user is done searching

        Dim msg As New System.Text.StringBuilder()
        msg.AppendLine()
        msg.AppendLine("PROCEDURE:        " & proc.ProcedureName)
        msg.AppendLine("EFFECTIVE DATE:   " & proc.EffectiveDate)
        msg.AppendLine("MIN. CHARGE:      " & proc.MinCharge.ToString("C"))
        msg.AppendLine("PRICE:            " & proc.Price.ToString("C") & proc.PriceType)
        msg.AppendLine()

        If isExactMatch Then
            ' Exact match - just show and return
            MessageBox.Show(msg.ToString(), "Exact Match Found",
                           MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False ' Done searching
        Else
            ' Partial match - ask if this is what they're looking for
            msg.AppendLine("Is this the part# you were looking for?")
            Dim result = MessageBox.Show(msg.ToString(), "Partial Match",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            Return (result = DialogResult.No) ' True = keep searching, False = found it
        End If
    End Function

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
        ' (G) Print all customers to printer
        Try
            Dim prcDir = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC")
            If Not Directory.Exists(prcDir) Then
                MessageBox.Show("No price list directory found.", "Not Found",
                               MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim files = Directory.GetFiles(prcDir, "*.PRC")
            If files.Length = 0 Then
                MessageBox.Show("No customer price lists found.", "No Customers",
                               MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Load customer list with real names and file info (reuse option F logic)
            Dim customers = FormCustomerList.LoadCustomerList()

            If customers.Count = 0 Then
                MessageBox.Show("No customer data loaded.", "No Customers",
                               MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Confirm print
            Dim result = MessageBox.Show(
                "Get STAR Printer ready to Print all Customers" & vbCrLf & vbCrLf &
                $"Total customers: {customers.Count}" & vbCrLf & vbCrLf &
                "Continue?",
                "Print Customer List",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question)

            If result <> DialogResult.OK Then Return

            ' Create print document
            Dim printDoc As New System.Drawing.Printing.PrintDocument()
            Dim currentIndex = 0
            Dim pageNum = 0

            AddHandler printDoc.PrintPage, Sub(sender, e)
                                               pageNum += 1
                                               Dim font As New Font("Courier New", 9)
                                               Dim boldFont As New Font("Courier New", 10, FontStyle.Bold)
                                               Dim y As Single = e.MarginBounds.Top
                                               Dim lineHeight = font.GetHeight(e.Graphics)

                                               ' Title
                                               e.Graphics.DrawString("*** CUSTOMER PRICE LISTS ***", boldFont,
                                                         Brushes.Black, e.MarginBounds.Left, y)
                                               y += lineHeight * 2

                                               ' Column headers
                                               e.Graphics.DrawString("FILENAME        REAL NAME                           SIZE    DATE",
                                                         font, Brushes.Black, e.MarginBounds.Left, y)
                                               y += lineHeight
                                               e.Graphics.DrawString(New String("="c, 78), font, Brushes.Black, e.MarginBounds.Left, y)
                                               y += lineHeight

                                               ' Print customers
                                               While currentIndex < customers.Count AndAlso y + lineHeight < e.MarginBounds.Bottom - (lineHeight * 3)
                                                   Dim cust = customers(currentIndex)
                                                   Dim fileName = cust.FileName.PadRight(16)
                                                   Dim realName = If(String.IsNullOrEmpty(cust.RealName), "(no name)", cust.RealName).PadRight(36)
                                                   Dim fileSize = cust.FileSize.ToString("N0").PadLeft(8)
                                                   Dim fileDate = cust.FileDate.ToString("MM-dd-yy").PadLeft(9)

                                                   Dim line = $"{fileName}{realName}{fileSize} {fileDate}"
                                                   e.Graphics.DrawString(line, font, Brushes.Black, e.MarginBounds.Left, y)
                                                   y += lineHeight
                                                   currentIndex += 1
                                               End While

                                               ' Footer
                                               If currentIndex >= customers.Count Then
                                                   y += lineHeight
                                                   e.Graphics.DrawString($"Total: {customers.Count} customers", font,
                                                             Brushes.Black, e.MarginBounds.Left, y)
                                               End If

                                               ' Page number
                                               e.Graphics.DrawString($"Page {pageNum}", font, Brushes.Black,
                                                         e.MarginBounds.Right - 60, e.MarginBounds.Top)

                                               e.HasMorePages = (currentIndex < customers.Count)
                                           End Sub

            ' Show print dialog
            Dim printDialog As New PrintDialog()
            printDialog.Document = printDoc
            If printDialog.ShowDialog() = DialogResult.OK Then
                printDoc.Print()
                MessageBox.Show("Customer list sent to printer.", "Print Complete",
                               MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            MessageBox.Show("Error printing customers: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SortPriceList()
        ' (I) Sort price list for current customer
        ' DOS source: PLIST.ASC lines 3160-3240
        If String.IsNullOrEmpty(_currentCustomer) Then
            MessageBox.Show("No customer selected.", "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If Not LoadProcedures() Then Return

        If _procedures.Count = 0 Then
            MessageBox.Show("No procedures to sort.", "No Data",
                           MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Show inline DOS-style confirmation prompt
        ShowSortConfirmation()
    End Sub

    Private Sub ShowSortConfirmation()
        ' Hide menu and show confirmation prompt (DOS style)
        flpLeft.Visible = False
        flpRight.Visible = False
        If _lblPrompt IsNot Nothing Then
            _lblPrompt.Visible = False
        End If

        ' Create confirmation label
        Dim lblConfirm As New Label()
        lblConfirm.Text = $"Are you sure you want to sort {_currentCustomer} (Y/N) ?"
        lblConfirm.AutoSize = True
        lblConfirm.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        lblConfirm.ForeColor = Color.White
        lblConfirm.BackColor = Color.Black
        lblConfirm.Location = New Point(30, 160)
        Me.Controls.Add(lblConfirm)
        lblConfirm.BringToFront()

        ' Wait for Y/N keypress
        Dim keyHandler As KeyEventHandler = Nothing
        keyHandler = Sub(sender As Object, e As KeyEventArgs)
                         If e.KeyCode = Keys.Y Then
                             e.Handled = True
                             e.SuppressKeyPress = True
                             Me.Controls.Remove(lblConfirm)
                             lblConfirm.Dispose()
                             RemoveHandler Me.KeyDown, keyHandler
                             ExecuteSort()
                         ElseIf e.KeyCode = Keys.N Then
                             e.Handled = True
                             e.SuppressKeyPress = True
                             Me.Controls.Remove(lblConfirm)
                             lblConfirm.Dispose()
                             RemoveHandler Me.KeyDown, keyHandler
                             ShowPriceListMenu()
                         End If
                     End Sub

        AddHandler Me.KeyDown, keyHandler
        Me.Focus()
    End Sub

    Private Sub ExecuteSort()
        ' Show "Sorting, Please Wait..." message
        Dim lblSorting As New Label()
        lblSorting.Text = "Sorting, Please Wait..."
        lblSorting.AutoSize = True
        lblSorting.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        lblSorting.ForeColor = Color.White
        lblSorting.BackColor = Color.Black
        lblSorting.Location = New Point(30, 180)
        Me.Controls.Add(lblSorting)
        lblSorting.BringToFront()

        ' Force display refresh so user sees the message
        Me.Refresh()
        Application.DoEvents()

        Try
            ' Sort by procedure name (alphabetically)
            _procedures = _procedures.OrderBy(Function(p) p.ProcedureName).ToList()

            ' Save sorted list back to file
            Using writer As New StreamWriter(_priceListFile, False)
                For Each proc In _procedures
                    writer.WriteLine($"""{proc.ProcedureName}"",""{proc.EffectiveDate}"",{proc.MinCharge},{proc.Price},""{proc.PriceType}""")
                Next
            End Using

            ' Brief delay so user sees the "Sorting" message (mimics DOS behavior)
            System.Threading.Thread.Sleep(200)

        Catch ex As Exception
            Me.Controls.Remove(lblSorting)
            lblSorting.Dispose()
            MessageBox.Show("Error sorting price list: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
            ShowPriceListMenu()
            Return
        End Try

        ' Clean up and return to menu
        Me.Controls.Remove(lblSorting)
        lblSorting.Dispose()
        ShowPriceListMenu()
    End Sub

    Private Sub ErasePriceList()
        ' (J) Erase entire price list - delete customer's .PRC file
        ' DOS source: PLIST.ASC lines 1670-1750
        If String.IsNullOrEmpty(_currentCustomer) Then
            MessageBox.Show("No customer selected.", "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Show DOS-style password prompt
        ShowErasePasswordPrompt()
    End Sub

    Private _txtErasePassword As TextBox
    Private _lblErasePrompt As Label

    Private Sub ShowErasePasswordPrompt()
        ' Hide menu and show password prompt (DOS style)
        flpLeft.Visible = False
        flpRight.Visible = False
        If _lblPrompt IsNot Nothing Then
            _lblPrompt.Visible = False
        End If

        ' Create password prompt label
        _lblErasePrompt = New Label()
        _lblErasePrompt.Text = $"Enter password to ERASE entire price list for {_currentCustomer}"
        _lblErasePrompt.AutoSize = True
        _lblErasePrompt.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _lblErasePrompt.ForeColor = Color.White
        _lblErasePrompt.BackColor = Color.Black
        _lblErasePrompt.Location = New Point(30, 160)
        Me.Controls.Add(_lblErasePrompt)
        _lblErasePrompt.BringToFront()

        ' Create password textbox (hidden input like DOS COLOR 0,0)
        _txtErasePassword = New TextBox()
        _txtErasePassword.Location = New Point(30, 190)
        _txtErasePassword.Size = New Size(200, 30)
        _txtErasePassword.Font = New Font("Consolas", 12.0F, FontStyle.Regular)
        _txtErasePassword.BackColor = Color.White
        _txtErasePassword.ForeColor = Color.Black
        _txtErasePassword.PasswordChar = "*"c  ' Hide password like DOS
        _txtErasePassword.MaxLength = 20
        AddHandler _txtErasePassword.KeyDown, AddressOf ErasePassword_KeyDown
        Me.Controls.Add(_txtErasePassword)
        _txtErasePassword.BringToFront()
        _txtErasePassword.Focus()
    End Sub

    Private Sub ErasePassword_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            e.SuppressKeyPress = True
            CheckErasePassword()
        ElseIf e.KeyCode = Keys.Escape Then
            e.Handled = True
            e.SuppressKeyPress = True
            CancelErase()
        End If
    End Sub

    Private Sub CheckErasePassword()
        Dim password = _txtErasePassword.Text.Trim()

        ' DOS password check: "DEAN" or "dean"
        If password.Equals("DEAN", StringComparison.OrdinalIgnoreCase) Then
            ' Password correct - execute erase
            CleanupErasePrompt()
            ExecuteErase()
        Else
            ' Wrong password - return to menu
            CleanupErasePrompt()
            ShowPriceListMenu()
        End If
    End Sub

    Private Sub CancelErase()
        CleanupErasePrompt()
        ShowPriceListMenu()
    End Sub

    Private Sub CleanupErasePrompt()
        If _lblErasePrompt IsNot Nothing Then
            Me.Controls.Remove(_lblErasePrompt)
            _lblErasePrompt.Dispose()
            _lblErasePrompt = Nothing
        End If
        If _txtErasePassword IsNot Nothing Then
            Me.Controls.Remove(_txtErasePassword)
            _txtErasePassword.Dispose()
            _txtErasePassword = Nothing
        End If
    End Sub

    Private Sub ExecuteErase()
        ' Show "Deleted, Please Wait..." message
        Dim lblDeleting As New Label()
        lblDeleting.Text = $"{_currentCustomer} Deleted, Please Wait..."
        lblDeleting.AutoSize = True
        lblDeleting.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        lblDeleting.ForeColor = Color.White
        lblDeleting.BackColor = Color.Black
        lblDeleting.Location = New Point(30, 180)
        Me.Controls.Add(lblDeleting)
        lblDeleting.BringToFront()

        ' Force display refresh
        Me.Refresh()
        Application.DoEvents()

        Try
            ' Delete the .PRC file
            If File.Exists(_priceListFile) Then
                File.Delete(_priceListFile)
            End If

            ' Remove from REALNAME.DAT (like DOS does)
            RemoveFromRealnameFile(_currentCustomer)

            ' Brief delay so user sees the message
            System.Threading.Thread.Sleep(500)

        Catch ex As Exception
            Me.Controls.Remove(lblDeleting)
            lblDeleting.Dispose()
            MessageBox.Show("Error erasing price list: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
            ShowPriceListMenu()
            Return
        End Try

        ' Clean up and close (customer no longer exists, return to main menu)
        Me.Controls.Remove(lblDeleting)
        lblDeleting.Dispose()
        _currentCustomer = ""
        _priceListFile = ""
        _procedures.Clear()
        Me.Close()
    End Sub

    Private Sub RemoveFromRealnameFile(customerName As String)
        ' DOS: Copies REALNAME.DAT to backup, then rebuilds without the deleted customer
        Try
            Dim realnameFile = Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.DAT")
            Dim tempFile = Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.TMP")

            If Not File.Exists(realnameFile) Then Return

            ' Backup original
            Dim backupFile = Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.BAC")
            File.Copy(realnameFile, backupFile, True)

            ' Read all entries except the one being deleted
            Dim entries As New List(Of String)()
            Using reader As New StreamReader(realnameFile)
                While Not reader.EndOfStream
                    Dim filenameLine = reader.ReadLine()
                    If reader.EndOfStream Then Exit While
                    Dim realnameLine = reader.ReadLine()

                    ' Parse filename (remove quotes)
                    Dim filename = filenameLine.Trim(""""c)

                    ' Remove .PRC extension if present for comparison
                    Dim filenameWithoutExt = filename
                    If filenameWithoutExt.EndsWith(".PRC", StringComparison.OrdinalIgnoreCase) Then
                        filenameWithoutExt = filenameWithoutExt.Substring(0, filenameWithoutExt.Length - 4)
                    End If

                    ' Keep entry if it's not the customer being deleted
                    If Not filenameWithoutExt.Equals(customerName, StringComparison.OrdinalIgnoreCase) Then
                        entries.Add(filenameLine)
                        entries.Add(realnameLine)
                    End If
                End While
            End Using

            ' Write back without the deleted customer
            Using writer As New StreamWriter(tempFile, False)
                For Each line In entries
                    writer.WriteLine(line)
                Next
            End Using

            ' Replace original with updated version
            File.Delete(realnameFile)
            File.Move(tempFile, realnameFile)

        Catch ex As Exception
            ' Log error but don't stop the erase operation
            Debug.WriteLine("Error updating REALNAME.DAT: " & ex.Message)
        End Try
    End Sub

    Private Sub IncreasePricesByPercent()
        ' (K) Increase some price lists by a Percent
        ' DOS source: PLIST.ASC lines 2770-3150
        ' Allows operator to specify percentage increases for multiple customers at once

        Try
            Dim form As New FormBatchPriceIncrease()
            form.ShowDialog(Me)
        Catch ex As Exception
            MessageBox.Show("Error launching batch price increase: " & ex.Message,
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ChangeFilenameOrRealname()
        ' (L) Permanently change filename or realname
        ' DOS source: PLIST.ASC lines 3260-3650
        ' Password-protected feature to rename customer file/display name

        Try
            Dim form As New FormRenameCustomer(_currentCustomer)
            If form.ShowDialog(Me) = DialogResult.OK Then
                ' Customer was renamed - reload menu with new name
                _currentCustomer = form.NewFilename
                _priceListFile = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC", _currentCustomer + ".PRC")
                ShowPriceListMenu()
            End If
        Catch ex As Exception
            MessageBox.Show("Error launching rename customer: " & ex.Message,
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ViewStandardProcedures()
        ' (M) View all Standard Procedures
        ' DOS source: PLIST.ASC lines 3700-3750
        Try
            Dim form As New FormStandardProcedures()
            form.ShowDialog()
        Catch ex As Exception
            MessageBox.Show("Error viewing standard procedures: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SortCustomerNames()
        ' (N) Sort Customers Actual Names
        ' DOS source: SORTNAME.ASC
        ' Sorts REALNAME.DAT alphabetically by customer real name
        Try
            Using form As New FormSortCustomerNames()
                form.ShowDialog(Me)
            End Using
        Catch ex As Exception
            MessageBox.Show("Error launching sort customer names: " & ex.Message,
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ScanForErrors()
        ' (O) Scan all price lists for errors
        ' DOS source: PLIST.ASC lines 3800-3890
        ' Checks for: duplicates, UPS pricing errors, procedures not in standard list
        Try
            Using form As New FormScanPriceListErrors()
                form.ShowDialog(Me)
            End Using
        Catch ex As Exception
            MessageBox.Show("Error launching price list scanner: " & ex.Message,
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ScanForLowMinCharges()
        ' (P) Scan price lists for errors and min. charge's that are too low
        ' DOS source: PLIST.ASC lines 4500-4670
        ' Cross-references against WORD\PROCDURE.DOC expected min charges
        Try
            Using form As New FormScanLowMinCharges()
                form.ShowDialog(Me)
            End Using
        Catch ex As Exception
            MessageBox.Show("Error launching min charge scanner: " & ex.Message,
                           "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ScanForAnything()
        ' (S) Scan all price lists for Anything Else
        ' DOS source: PLIST.ASC line 469
        ' Opens search form to find user-specified text in all .PRC files
        Dim form As New FormSearchPriceLists()
        form.ShowDialog(Me)
    End Sub

    Private Sub ChangeMinChargeForProcedure()
        ' (T) Change some customers min charge for a procedure
        ' DOS source: PLIST.ASC line 4700
        ' Prompts for procedure and new min charge, then asks Y/N/A for each customer
        Dim form As New FormChangeMinCharge()
        form.ShowDialog(Me)
    End Sub

    Private Sub UpdateEnvironmentalSurcharge()
        ' (U) Update Environmental Surcharge customer list
        ' DOS source: PLIST.ASC line 5100
        ' Opens SURCHARG.DAT for editing with validation
        Dim form As New FormEnvironmentalSurcharge()
        form.ShowDialog(Me)
    End Sub

    Private Sub EditInWordProcessor()
        ' (w) Edit in Word Processor
        ' DOS source: PLIST.ASC line 375
        ' Opens current customer's .PRC file in Microsoft Word
        ' DOS always used "word \prc\<file>.prc" - this was Microsoft Word for DOS
        If String.IsNullOrEmpty(_currentCustomer) Then
            DosMessageBox.Show(Me, "No customer selected.", "Error", MessageBoxButtons.OK)
            Return
        End If

        Try
            If Not File.Exists(_priceListFile) Then
                DosMessageBox.Show(Me, $"Price list file not found:{Environment.NewLine}{_priceListFile}", 
                                  "Error", MessageBoxButtons.OK)
                Return
            End If

            ' Try to launch Microsoft Word specifically (matches DOS behavior)
            ' If Word isn't installed, this will fall back to default .PRC handler
            Dim psi As New System.Diagnostics.ProcessStartInfo() With {
                .FileName = "winword.exe",
                .Arguments = $"""{_priceListFile}""",
                .UseShellExecute = True,
                .ErrorDialog = True
            }
            System.Diagnostics.Process.Start(psi)

        Catch ex As Exception
            ' If Word launch fails, show helpful message
            Dim msg = $"Cannot launch Microsoft Word.{Environment.NewLine}{Environment.NewLine}" &
                      $"This feature requires Microsoft Word to be installed.{Environment.NewLine}" &
                      $"(DOS version used 'WORD.EXE' for all document editing){Environment.NewLine}{Environment.NewLine}" &
                      $"File location: {_priceListFile}{Environment.NewLine}{Environment.NewLine}" &
                      $"Error: {ex.Message}"
            DosMessageBox.Show(Me, msg, "Word Processor Required", MessageBoxButtons.OK)
        End Try
    End Sub
End Class

