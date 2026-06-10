Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' DOS-style logbook entry form for Option 4.
''' Replicates LOGENTER.ASC behavior:
''' - Prompts for date (MM-DD or MM-DD-YYYY)
''' - Looks up invoice to get customer
''' - Collects all quality control fields
''' - Shows confirmation screen
''' - Appends to LOGBOOK.XX file
''' </summary>
Public Class FormLogBookEntry
    Inherits Form

    Private _dataDir As String = ""

    ' Step 1: Date entry
    Private lblDatePrompt As Label
    Private txtDate As TextBox
    Private lblDateHelp As Label

    ' Step 2: Invoice lookup
    Private lblInvoicePrompt As Label
    Private txtInvoice As TextBox
    Private lblCustomerDisplay As Label

    ' Step 3: Quality control fields
    Private lblPartNumber As Label
    Private txtPartNumber As TextBox
    Private lblPONumber As Label
    Private txtPONumber As TextBox
    Private lblSpec As Label
    Private txtSpec As TextBox
    Private lblQtyAccepted As Label
    Private txtQtyAccepted As TextBox
    Private lblQtyRejected As Label
    Private txtQtyRejected As TextBox
    Private lblReasonRejected As Label
    Private txtReasonRejected As TextBox
    Private lblMaterial As Label
    Private txtMaterial As TextBox
    Private lblHeatTreat As Label
    Private txtHeatTreat As TextBox
    Private lblStatus As Label
    Private rbInProcess As RadioButton
    Private rbInService As RadioButton
    Private rbFinal As RadioButton
    Private rbDontKnow As RadioButton
    Private lblDataEntryComplete As Label
    Private btnOK As Button

    ' Confirmation panel
    Private pnlConfirmation As Panel
    Private lblConfirmTitle As Label
    Private txtConfirmation As TextBox
    Private lblConfirmPrompt As Label

    ' Current state
    Private _currentStep As Integer = 1 ' 1=Date, 2=Invoice, 3=Fields, 4=Confirmation
    Private _entryDate As String = ""
    Private _invoiceNumber As Integer = 0
    Private _customerName As String = ""

    Public Sub New()
        InitializeComponent()
        InitializeDosStyle()
        _dataDir = LegacyDataPaths.BaseDataDir
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        ' Form settings
        Me.Text = "LOGBOOK ENTRY PROGRAM"
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.KeyPreview = True
        Me.Size = New Size(800, 600)

        ' Step 1: Date entry controls
        lblDatePrompt = New Label() With {
            .Text = "Enter Date  [Q = Quit]",
            .Location = New Point(20, 60),
            .Size = New Size(400, 25),
            .ForeColor = Color.White
        }

        txtDate = New TextBox() With {
            .Location = New Point(20, 90),
            .Size = New Size(200, 25),
            .BackColor = Color.Black,
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 12.0F, FontStyle.Bold)
        }

        lblDateHelp = New Label() With {
            .Text = "Format: ##-##  or  ##-##-####",
            .Location = New Point(20, 120),
            .Size = New Size(400, 25),
            .ForeColor = Color.Cyan
        }

        ' Step 2: Invoice lookup controls
        lblInvoicePrompt = New Label() With {
            .Text = "Invoice Number",
            .Location = New Point(20, 60),
            .Size = New Size(200, 25),
            .ForeColor = Color.White,
            .Visible = False
        }

        txtInvoice = New TextBox() With {
            .Location = New Point(20, 90),
            .Size = New Size(200, 25),
            .BackColor = Color.Black,
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 12.0F, FontStyle.Bold),
            .Visible = False
        }

        lblCustomerDisplay = New Label() With {
            .Text = "Company: ",
            .Location = New Point(20, 120),
            .Size = New Size(600, 25),
            .ForeColor = Color.Cyan,
            .Visible = False
        }

        ' Step 3: Quality control fields (all hidden initially)
        Dim yPos As Integer = 50
        Dim labelWidth As Integer = 180
        Dim textWidth As Integer = 400

        lblPartNumber = CreateFieldLabel("PART #", 20, yPos)
        txtPartNumber = CreateFieldTextBox(200, yPos, textWidth)
        yPos += 35

        lblPONumber = CreateFieldLabel("P.O.#", 20, yPos)
        txtPONumber = CreateFieldTextBox(200, yPos, textWidth)
        yPos += 35

        lblSpec = CreateFieldLabel("SPEC", 20, yPos)
        txtSpec = CreateFieldTextBox(200, yPos, textWidth)
        yPos += 35

        lblQtyAccepted = CreateFieldLabel("QTY ACCEPTED", 20, yPos)
        txtQtyAccepted = CreateFieldTextBox(200, yPos, textWidth)
        yPos += 35

        lblQtyRejected = CreateFieldLabel("QTY REJECTED", 20, yPos)
        txtQtyRejected = CreateFieldTextBox(200, yPos, textWidth)
        yPos += 35

        lblReasonRejected = CreateFieldLabel("REASON REJECTED", 20, yPos)
        txtReasonRejected = CreateFieldTextBox(200, yPos, textWidth)
        yPos += 35

        lblMaterial = CreateFieldLabel("MATERIAL", 20, yPos)
        txtMaterial = CreateFieldTextBox(200, yPos, textWidth)
        yPos += 35

        lblHeatTreat = CreateFieldLabel("HEAT TREAT", 20, yPos)
        txtHeatTreat = CreateFieldTextBox(200, yPos, textWidth)
        yPos += 35

        lblStatus = CreateFieldLabel("STATUS", 20, yPos)
        yPos += 5

        ' Status radio buttons
        rbInProcess = New RadioButton() With {
            .Text = "(1) IN PROCESS",
            .Location = New Point(220, yPos),
            .Size = New Size(150, 25),
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Visible = False,
            .Tag = "IN PROCESS"
        }

        rbInService = New RadioButton() With {
            .Text = "(2) IN SERVICE",
            .Location = New Point(380, yPos),
            .Size = New Size(150, 25),
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Visible = False,
            .Tag = "IN SERVICE"
        }

        rbFinal = New RadioButton() With {
            .Text = "(3) FINAL",
            .Location = New Point(540, yPos),
            .Size = New Size(100, 25),
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Visible = False,
            .Tag = "FINAL"
        }
        yPos += 30

        rbDontKnow = New RadioButton() With {
            .Text = "(4) I DONT KNOW",
            .Location = New Point(220, yPos),
            .Size = New Size(200, 25),
            .ForeColor = Color.White,
            .BackColor = Color.Black,
            .Visible = False,
            .Checked = True,
            .Tag = ""
        }
        yPos += 45

        ' Step 3: Data entry complete prompt
        lblDataEntryComplete = New Label() With {
            .Text = "Press [ENTER] when data input complete",
            .Location = New Point(20, yPos),
            .Size = New Size(400, 25),
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 10.0F, FontStyle.Bold),
            .Visible = False
        }

        btnOK = New Button() With {
            .Text = "OK",
            .Location = New Point(650, yPos - 5),
            .Size = New Size(100, 35),
            .BackColor = Color.FromArgb(64, 64, 64),
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 12.0F, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat,
            .Visible = False,
            .TabIndex = 100
        }
        AddHandler btnOK.Click, AddressOf btnOK_Click

        ' Step 4: Confirmation panel
        pnlConfirmation = New Panel() With {
            .Location = New Point(10, 50),
            .Size = New Size(760, 480),
            .BackColor = Color.Black,
            .Visible = False
        }

        lblConfirmTitle = New Label() With {
            .Text = "Is This Correct (Y/N) ?",
            .Location = New Point(20, 420),
            .Size = New Size(600, 30),
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 12.0F, FontStyle.Bold)
        }

        txtConfirmation = New TextBox() With {
            .Location = New Point(20, 20),
            .Size = New Size(720, 390),
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical,
            .BackColor = Color.Black,
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular),
            .BorderStyle = BorderStyle.None
        }

        lblConfirmPrompt = New Label() With {
            .Text = "[Y = Save] [N = Re-enter] [ESC = Cancel]",
            .Location = New Point(20, 450),
            .Size = New Size(600, 25),
            .ForeColor = Color.Cyan
        }

        pnlConfirmation.Controls.Add(txtConfirmation)
        pnlConfirmation.Controls.Add(lblConfirmTitle)
        pnlConfirmation.Controls.Add(lblConfirmPrompt)

        ' Add all controls
        Me.Controls.Add(lblDatePrompt)
        Me.Controls.Add(txtDate)
        Me.Controls.Add(lblDateHelp)
        Me.Controls.Add(lblInvoicePrompt)
        Me.Controls.Add(txtInvoice)
        Me.Controls.Add(lblCustomerDisplay)
        Me.Controls.Add(lblPartNumber)
        Me.Controls.Add(txtPartNumber)
        Me.Controls.Add(lblPONumber)
        Me.Controls.Add(txtPONumber)
        Me.Controls.Add(lblSpec)
        Me.Controls.Add(txtSpec)
        Me.Controls.Add(lblQtyAccepted)
        Me.Controls.Add(txtQtyAccepted)
        Me.Controls.Add(lblQtyRejected)
        Me.Controls.Add(txtQtyRejected)
        Me.Controls.Add(lblReasonRejected)
        Me.Controls.Add(txtReasonRejected)
        Me.Controls.Add(lblMaterial)
        Me.Controls.Add(txtMaterial)
        Me.Controls.Add(lblHeatTreat)
        Me.Controls.Add(txtHeatTreat)
        Me.Controls.Add(lblStatus)
        Me.Controls.Add(rbInProcess)
        Me.Controls.Add(rbInService)
        Me.Controls.Add(rbFinal)
        Me.Controls.Add(rbDontKnow)
        Me.Controls.Add(lblDataEntryComplete)
        Me.Controls.Add(btnOK)
        Me.Controls.Add(pnlConfirmation)

        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Private Function CreateFieldLabel(text As String, x As Integer, y As Integer) As Label
        Return New Label() With {
            .Text = text,
            .Location = New Point(x, y),
            .Size = New Size(180, 25),
            .ForeColor = Color.White,
            .Visible = False
        }
    End Function

    Private Function CreateFieldTextBox(x As Integer, y As Integer, width As Integer) As TextBox
        Return New TextBox() With {
            .Location = New Point(x, y),
            .Size = New Size(width, 25),
            .BackColor = Color.Black,
            .ForeColor = Color.Yellow,
            .Font = New Font("Consolas", 10.0F, FontStyle.Regular),
            .Visible = False
        }
    End Function

    Private Sub InitializeDosStyle()
        ShowStep1()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        txtDate.Focus()
    End Sub

    Protected Overrides Sub OnKeyPress(e As KeyPressEventArgs)
        MyBase.OnKeyPress(e)

        ' Handle Q = Quit at any step
        If Char.ToUpperInvariant(e.KeyChar) = "Q"c AndAlso _currentStep < 4 Then
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
            e.Handled = True
            Return
        End If

        ' Confirmation step
        If _currentStep = 4 Then
            Dim ch As Char = Char.ToUpperInvariant(e.KeyChar)
            If ch = "Y"c Then
                SaveEntry()
                e.Handled = True
            ElseIf ch = "N"c Then
                ' Re-enter from beginning
                ResetToDateEntry()
                e.Handled = True
            End If
        End If
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            If _currentStep = 4 Then
                ' ESC from confirmation = cancel entire entry
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
                Return True
            Else
                ' ESC from other steps = quit
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
                Return True
            End If
        End If

        If keyData = Keys.Enter Then
            Select Case _currentStep
                Case 1
                    If ValidateAndProcessDate() Then
                        ShowStep2()
                    End If
                Case 2
                    If ValidateAndProcessInvoice() Then
                        ShowStep3()
                    End If
                Case 3
                    ShowStep4()
            End Select
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ' ===== Step 1: Date Entry =====
    Private Sub ShowStep1()
        _currentStep = 1
        HideAllControls()

        lblDatePrompt.Visible = True
        txtDate.Visible = True
        lblDateHelp.Visible = True

        ' Default to current date in MM-DD format
        Dim now As DateTime = DateTime.Now
        txtDate.Text = now.ToString("MM-dd")
        txtDate.SelectAll()
        txtDate.Focus()
    End Sub

    Private Function ValidateAndProcessDate() As Boolean
        Dim input As String = txtDate.Text.Trim()

        If String.IsNullOrWhiteSpace(input) Then
            MessageBox.Show("Please enter a date.", "Date Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If

        ' Handle Q = Quit
        If input.Equals("Q", StringComparison.OrdinalIgnoreCase) Then
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
            Return False
        End If

        ' Parse date: MM-DD or MM-DD-YYYY
        Dim parts() As String = input.Split("-"c)

        If parts.Length = 2 Then
            ' MM-DD format - append current year
            Dim now As DateTime = DateTime.Now
            input = input & "-" & now.ToString("yyyy")
        ElseIf parts.Length <> 3 Then
            MessageBox.Show("Invalid date format. Use MM-DD or MM-DD-YYYY", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If

        ' Validate date
        Dim parsedDate As DateTime
        If Not DateTime.TryParseExact(input, "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, parsedDate) Then
            MessageBox.Show("Invalid date. Please check the format.", "Invalid Date", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If

        _entryDate = parsedDate.ToString("MM-dd-yyyy")
        Return True
    End Function

    ' ===== Step 2: Invoice Lookup =====
    Private Sub ShowStep2()
        _currentStep = 2
        HideAllControls()

        lblInvoicePrompt.Visible = True
        txtInvoice.Visible = True
        lblCustomerDisplay.Visible = True

        txtInvoice.Text = ""
        lblCustomerDisplay.Text = "Company: "
        txtInvoice.Focus()
    End Sub

    Private Function ValidateAndProcessInvoice() As Boolean
        Dim input As String = txtInvoice.Text.Trim()

        Dim invNum As Integer
        If Not Integer.TryParse(input, invNum) Then
            MessageBox.Show("Please enter a valid invoice number.", "Invalid Invoice", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If

        ' DOS validation: INUM-75000! < 1
        If invNum - 75000 < 1 Then
            MessageBox.Show("Invoice number must be greater than 75000.", "Invalid Invoice", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtInvoice.SelectAll()
            Return False
        End If

        ' Look up invoice in INVOICE.CHK
        Try
            Dim invoicePath As String = LegacyDataPaths.InvoiceChk
            If Not File.Exists(invoicePath) Then
                MessageBox.Show("Invoice file not found: " & invoicePath, "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return False
            End If

            Dim record = InvoiceChkReader.ReadRecord(invoicePath, invNum)
            If String.IsNullOrWhiteSpace(record.CompanyCode) Then
                MessageBox.Show(invNum.ToString() & " DOSE'NT EXIST", "Invoice Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtInvoice.SelectAll()
                Return False
            End If

            _invoiceNumber = invNum
            _customerName = record.CompanyCode.Trim()
            lblCustomerDisplay.Text = "Company: " & _customerName

            Return True

        Catch ex As Exception
            MessageBox.Show("Error reading invoice file: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    ' ===== Step 3: Quality Control Fields =====
    Private Sub ShowStep3()
        _currentStep = 3
        HideAllControls()

        ' Show all QC fields
        lblPartNumber.Visible = True
        txtPartNumber.Visible = True
        lblPONumber.Visible = True
        txtPONumber.Visible = True
        lblSpec.Visible = True
        txtSpec.Visible = True
        lblQtyAccepted.Visible = True
        txtQtyAccepted.Visible = True
        lblQtyRejected.Visible = True
        txtQtyRejected.Visible = True
        lblReasonRejected.Visible = True
        txtReasonRejected.Visible = True
        lblMaterial.Visible = True
        txtMaterial.Visible = True
        lblHeatTreat.Visible = True
        txtHeatTreat.Visible = True
        lblStatus.Visible = True
        rbInProcess.Visible = True
        rbInService.Visible = True
        rbFinal.Visible = True
        rbDontKnow.Visible = True
        lblDataEntryComplete.Visible = True
        btnOK.Visible = True

        ' Clear all fields
        txtPartNumber.Text = ""
        txtPONumber.Text = ""
        txtSpec.Text = ""
        txtQtyAccepted.Text = ""
        txtQtyRejected.Text = ""
        txtReasonRejected.Text = ""
        txtMaterial.Text = ""
        txtHeatTreat.Text = ""
        rbDontKnow.Checked = True

        txtPartNumber.Focus()
    End Sub

    ' ===== Step 4: Confirmation =====
    Private Sub ShowStep4()
        _currentStep = 4
        HideAllControls()

        ' Build confirmation text
        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine("Customer: " & _customerName)
        sb.AppendLine("Part #: " & txtPartNumber.Text)
        sb.AppendLine("Date: " & _entryDate)
        sb.AppendLine("Invoice #: " & _invoiceNumber.ToString())
        sb.AppendLine("P.O. #: " & txtPONumber.Text)
        sb.AppendLine("Status: " & GetSelectedStatus())
        sb.AppendLine("Qty Accepted: " & txtQtyAccepted.Text)
        sb.AppendLine("Qty Rejected: " & txtQtyRejected.Text)
        sb.AppendLine("Reason Rejected: " & txtReasonRejected.Text)
        sb.AppendLine("Material: " & txtMaterial.Text)
        sb.AppendLine("Heat Treat: " & txtHeatTreat.Text)
        sb.AppendLine("spec:       " & txtSpec.Text)

        txtConfirmation.Text = sb.ToString()
        pnlConfirmation.Visible = True
        pnlConfirmation.Focus()
    End Sub

    Private Function GetSelectedStatus() As String
        If rbInProcess.Checked Then Return CStr(rbInProcess.Tag)
        If rbInService.Checked Then Return CStr(rbInService.Tag)
        If rbFinal.Checked Then Return CStr(rbFinal.Tag)
        If rbDontKnow.Checked Then Return CStr(rbDontKnow.Tag)
        Return ""
    End Function

    Private Sub btnOK_Click(sender As Object, e As EventArgs)
        ' Same as pressing Enter on Step 3
        If _currentStep = 3 Then
            ShowStep4()
        End If
    End Sub

    Private Sub HideAllControls()
        ' Step 1
        lblDatePrompt.Visible = False
        txtDate.Visible = False
        lblDateHelp.Visible = False

        ' Step 2
        lblInvoicePrompt.Visible = False
        txtInvoice.Visible = False
        lblCustomerDisplay.Visible = False

        ' Step 3
        lblPartNumber.Visible = False
        txtPartNumber.Visible = False
        lblPONumber.Visible = False
        txtPONumber.Visible = False
        lblSpec.Visible = False
        txtSpec.Visible = False
        lblQtyAccepted.Visible = False
        txtQtyAccepted.Visible = False
        lblQtyRejected.Visible = False
        txtQtyRejected.Visible = False
        lblReasonRejected.Visible = False
        txtReasonRejected.Visible = False
        lblMaterial.Visible = False
        txtMaterial.Visible = False
        lblHeatTreat.Visible = False
        txtHeatTreat.Visible = False
        lblStatus.Visible = False
        rbInProcess.Visible = False
        rbInService.Visible = False
        rbFinal.Visible = False
        rbDontKnow.Visible = False
        lblDataEntryComplete.Visible = False
        btnOK.Visible = False

        ' Step 4
        pnlConfirmation.Visible = False
    End Sub

    Private Sub SaveEntry()
        Try
            ' Determine log book file (LOGBOOK.XX where XX = last 2 digits of current year)
            Dim now As DateTime = DateTime.Now
            Dim yearSuffix As String = now.ToString("yy")
            Dim logFileName As String = $"LOGBOOK.{yearSuffix}"
            Dim logFilePath As String = Path.Combine(_dataDir, logFileName)

            ' Write entry using DOS WRITE# format (one field per line with quotes)
            Using writer As New StreamWriter(logFilePath, append:=True)
                ' Write all 12 fields as quoted strings (DOS WRITE# format)
                writer.WriteLine(QuoteField(_entryDate))
                writer.WriteLine(QuoteField(_customerName))
                writer.WriteLine(QuoteField(txtPartNumber.Text))
                writer.WriteLine(_invoiceNumber.ToString()) ' Numeric field - no quotes
                writer.WriteLine(QuoteField(txtPONumber.Text))
                writer.WriteLine(QuoteField(txtSpec.Text))
                writer.WriteLine(QuoteField(txtQtyAccepted.Text))
                writer.WriteLine(QuoteField(txtQtyRejected.Text))
                writer.WriteLine(QuoteField(txtMaterial.Text))
                writer.WriteLine(QuoteField(txtHeatTreat.Text))
                writer.WriteLine(QuoteField(txtReasonRejected.Text))
                writer.WriteLine(QuoteField(GetSelectedStatus()))
            End Using

            MessageBox.Show("Log book entry saved successfully!", "Entry Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)

            ' Ask if user wants to enter another
            Dim result As DialogResult = MessageBox.Show("Enter another log book entry?", "Continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                ResetToDateEntry()
            Else
                Me.DialogResult = DialogResult.OK
                Me.Close()
            End If

        Catch ex As Exception
            MessageBox.Show("Error saving log book entry:" & Environment.NewLine & ex.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function QuoteField(value As String) As String
        If value Is Nothing Then value = ""
        ' DOS WRITE# format: wrap in quotes, escape internal quotes by doubling them
        Return """" & value.Replace("""", """""") & """"
    End Function

    Private Sub ResetToDateEntry()
        ' Clear all fields and start over
        _entryDate = ""
        _invoiceNumber = 0
        _customerName = ""

        txtDate.Text = DateTime.Now.ToString("MM-dd")
        txtInvoice.Text = ""
        txtPartNumber.Text = ""
        txtPONumber.Text = ""
        txtSpec.Text = ""
        txtQtyAccepted.Text = ""
        txtQtyRejected.Text = ""
        txtReasonRejected.Text = ""
        txtMaterial.Text = ""
        txtHeatTreat.Text = ""
        rbDontKnow.Checked = True

        ShowStep1()
    End Sub
End Class
