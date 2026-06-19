Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Add procedure to customer price list
''' </summary>
Public Class FormPriceListAdd
    Inherits Form

    Private _customer As String
    Private _priceListFile As String
    Private _lastProcedure As String = ""
    Private _lastMinCharge As Decimal = 0
    Private _lastPrice As Decimal = 0
    Private _lastPriceType As String = "/EA"

    Private txtProcedure As TextBox
    Private txtMinCharge As TextBox
    Private txtPrice As TextBox
    Private cboPriceType As ComboBox
    Private lblLastProc As Label
    Private btnSave As Button
    Private btnCancel As Button

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Add Procedure"
        Me.Size = New Size(600, 400)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.Black
        Me.ForeColor = Color.FromArgb(170, 170, 170)
        Me.Font = New Font("Consolas", 10.0F, FontStyle.Regular)

        Dim yPos = 20

        ' Title
        Dim lblTitle As New Label()
        lblTitle.Location = New Point(20, yPos)
        lblTitle.Size = New Size(560, 30)
        lblTitle.Text = "****** ADD PROCEDURE ******"
        lblTitle.Font = New Font("Consolas", 12.0F, FontStyle.Bold)
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        Me.Controls.Add(lblTitle)
        yPos += 50

        ' Last procedure label
        lblLastProc = New Label()
        lblLastProc.Location = New Point(20, yPos)
        lblLastProc.Size = New Size(560, 20)
        lblLastProc.Text = ""
        lblLastProc.ForeColor = Color.Yellow
        Me.Controls.Add(lblLastProc)
        yPos += 30

        ' Procedure name
        Dim lblProc As New Label()
        lblProc.Location = New Point(20, yPos)
        lblProc.Size = New Size(200, 20)
        lblProc.Text = "Procedure name:"
        Me.Controls.Add(lblProc)

        txtProcedure = New TextBox()
        txtProcedure.Location = New Point(220, yPos)
        txtProcedure.Size = New Size(350, 25)
        txtProcedure.MaxLength = 27
        txtProcedure.BackColor = Color.FromArgb(20, 20, 20)
        txtProcedure.ForeColor = Color.White
        Me.Controls.Add(txtProcedure)
        yPos += 40

        ' Min charge
        Dim lblMinCharge As New Label()
        lblMinCharge.Location = New Point(20, yPos)
        lblMinCharge.Size = New Size(200, 20)
        lblMinCharge.Text = "Minimum charge:"
        Me.Controls.Add(lblMinCharge)

        txtMinCharge = New TextBox()
        txtMinCharge.Location = New Point(220, yPos)
        txtMinCharge.Size = New Size(150, 25)
        txtMinCharge.BackColor = Color.FromArgb(20, 20, 20)
        txtMinCharge.ForeColor = Color.White
        Me.Controls.Add(txtMinCharge)
        yPos += 40

        ' Price
        Dim lblPrice As New Label()
        lblPrice.Location = New Point(20, yPos)
        lblPrice.Size = New Size(200, 20)
        lblPrice.Text = "Price:"
        Me.Controls.Add(lblPrice)

        txtPrice = New TextBox()
        txtPrice.Location = New Point(220, yPos)
        txtPrice.Size = New Size(150, 25)
        txtPrice.BackColor = Color.FromArgb(20, 20, 20)
        txtPrice.ForeColor = Color.White
        Me.Controls.Add(txtPrice)
        yPos += 40

        ' Price type
        Dim lblPriceType As New Label()
        lblPriceType.Location = New Point(20, yPos)
        lblPriceType.Size = New Size(200, 20)
        lblPriceType.Text = "Price by (P)ound or (E)ach:"
        Me.Controls.Add(lblPriceType)

        cboPriceType = New ComboBox()
        cboPriceType.Location = New Point(220, yPos)
        cboPriceType.Size = New Size(150, 25)
        cboPriceType.DropDownStyle = ComboBoxStyle.DropDownList
        cboPriceType.BackColor = Color.FromArgb(20, 20, 20)
        cboPriceType.ForeColor = Color.White
        cboPriceType.Items.AddRange(New String() {"/#", "/EA"})
        cboPriceType.SelectedIndex = 1
        Me.Controls.Add(cboPriceType)
        yPos += 60

        ' Buttons
        btnSave = New Button()
        btnSave.Location = New Point(220, yPos)
        btnSave.Size = New Size(150, 35)
        btnSave.Text = "Save"
        btnSave.BackColor = Color.FromArgb(40, 40, 40)
        btnSave.ForeColor = Color.FromArgb(170, 170, 170)
        btnSave.FlatStyle = FlatStyle.Flat
        AddHandler btnSave.Click, AddressOf BtnSave_Click
        Me.Controls.Add(btnSave)

        btnCancel = New Button()
        btnCancel.Location = New Point(390, yPos)
        btnCancel.Size = New Size(150, 35)
        btnCancel.Text = "Cancel [Q]"
        btnCancel.BackColor = Color.FromArgb(40, 40, 40)
        btnCancel.ForeColor = Color.FromArgb(170, 170, 170)
        btnCancel.FlatStyle = FlatStyle.Flat
        AddHandler btnCancel.Click, Sub()
                                        Me.DialogResult = DialogResult.Cancel
                                        Me.Close()
                                    End Sub
        Me.Controls.Add(btnCancel)

        ' ESC/Q to cancel
        Me.KeyPreview = True
        AddHandler Me.KeyDown, Sub(sender, e)
                                   If e.KeyCode = Keys.Escape OrElse e.KeyCode = Keys.Q Then
                                       Me.DialogResult = DialogResult.Cancel
                                       Me.Close()
                                   End If
                               End Sub
    End Sub

    Public Sub SetCustomer(customer As String, priceListFile As String)
        _customer = customer
        _priceListFile = priceListFile
        Me.Text = "Add Procedure - " & customer

        ' Set defaults from last entry
        If _lastMinCharge > 0 Then
            txtMinCharge.Text = _lastMinCharge.ToString("F2")
        End If
        If _lastPrice > 0 Then
            txtPrice.Text = _lastPrice.ToString("F4")
        End If
        If Not String.IsNullOrEmpty(_lastPriceType) Then
            cboPriceType.SelectedItem = _lastPriceType
        End If
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As EventArgs)
        ' Validate inputs
        Dim procName = txtProcedure.Text.Trim().ToUpperInvariant()
        If String.IsNullOrEmpty(procName) Then
            MessageBox.Show("Please enter a procedure name.", "Required",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtProcedure.Focus()
            Return
        End If

        If procName.Length > 27 Then
            MessageBox.Show("Procedure name cannot exceed 27 characters.", "Too Long",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtProcedure.Focus()
            Return
        End If

        If procName.Contains(""""c) Then
            MessageBox.Show("Please don't use quotes in the procedure name.", "Invalid Character",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtProcedure.Focus()
            Return
        End If

        If procName.Contains("--") Then
            MessageBox.Show("Please don't use double dashes in the procedure name.", "Invalid Character",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtProcedure.Focus()
            Return
        End If

        Dim minCharge As Decimal
        If Not Decimal.TryParse(txtMinCharge.Text, minCharge) OrElse minCharge < 0 Then
            MessageBox.Show("Please enter a valid minimum charge.", "Invalid Amount",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtMinCharge.Focus()
            Return
        End If

        Dim price As Decimal
        If Not Decimal.TryParse(txtPrice.Text, price) OrElse price < 0 Then
            MessageBox.Show("Please enter a valid price.", "Invalid Amount",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtPrice.Focus()
            Return
        End If

        Dim priceType = cboPriceType.SelectedItem.ToString()
        Dim effDate = DateTime.Now.ToString("MM-dd-yyyy")

        ' Confirmation
        Dim confirmMsg As New System.Text.StringBuilder()
        confirmMsg.AppendLine("Customer name: " & _customer)
        confirmMsg.AppendLine()
        confirmMsg.AppendLine("Procedure: " & procName)
        confirmMsg.AppendLine("Effective date: " & effDate)
        confirmMsg.AppendLine("Minimum charge: " & minCharge.ToString("C"))
        confirmMsg.AppendLine("Price: " & price.ToString("C") & priceType)
        confirmMsg.AppendLine()
        confirmMsg.AppendLine("Is this correct?")

        If MessageBox.Show(confirmMsg.ToString(), "Confirm",
                          MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
            Return
        End If

        ' Append to file
        Try
            Directory.CreateDirectory(Path.GetDirectoryName(_priceListFile))
            Using writer As New StreamWriter(_priceListFile, True)
                ' DOS WRITE# format: "procedure","date",mincharge,price,"type"
                writer.WriteLine("""" & procName & """,""" & effDate & """," &
                               minCharge.ToString("F4") & "," & price.ToString("F4") & ",""" & priceType & """")
            End Using

            ' Remember last values
            _lastProcedure = procName
            _lastMinCharge = minCharge
            _lastPrice = price
            _lastPriceType = priceType

            ' Update label
            lblLastProc.Text = "Last procedure entered was " & procName

            ' Clear for next entry
            txtProcedure.Clear()
            txtProcedure.Focus()

            ' Ask if user wants to add another
            If MessageBox.Show("Procedure added." & vbCrLf & vbCrLf & "Add another?",
                              "Success", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.No Then
                Me.DialogResult = DialogResult.OK
                Me.Close()
            End If
        Catch ex As Exception
            MessageBox.Show("Error saving procedure: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
