Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' Option U - Update Environmental Surcharge
''' DOS source: PLIST.ASC line 5100
''' Opens SURCHARG.DAT for editing and validates surcharge percentages
''' </summary>
Public Class FormEnvironmentalSurcharge
    Inherits Form

    Private _txtSurchargeData As TextBox
    Private _btnSave As Button
    Private _btnClose As Button
    Private _lblInstructions As Label
    Private _surchargeFile As String

    Public Sub New()
        InitializeComponent()
        LoadSurchargeFile()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Update Environmental Surcharge"
        Me.Size = New Size(750, 700)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.Black
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MinimumSize = New Size(750, 500)

        _lblInstructions = New Label()
        _lblInstructions.Text = "Environmental Surcharge Customer List" & Environment.NewLine &
                                "Format: Customer file name, then surcharge % (must be 1-100, cannot be 8)"
        _lblInstructions.Location = New Point(20, 20)
        _lblInstructions.Size = New Size(700, 40)
        _lblInstructions.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        _lblInstructions.ForeColor = Color.Yellow
        _lblInstructions.BackColor = Color.Black
        Me.Controls.Add(_lblInstructions)

        _txtSurchargeData = New TextBox()
        _txtSurchargeData.Location = New Point(20, 70)
        _txtSurchargeData.Size = New Size(700, 530)
        _txtSurchargeData.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        _txtSurchargeData.BackColor = Color.Black
        _txtSurchargeData.ForeColor = Color.White
        _txtSurchargeData.Multiline = True
        _txtSurchargeData.ScrollBars = ScrollBars.Both
        _txtSurchargeData.WordWrap = False
        _txtSurchargeData.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.Controls.Add(_txtSurchargeData)

        _btnSave = New Button()
        _btnSave.Text = "Save &&" & Environment.NewLine & "Validate"
        _btnSave.Location = New Point(430, 620)
        _btnSave.Size = New Size(120, 40)
        _btnSave.Font = New Font("Consolas", 9.0F, FontStyle.Bold)
        _btnSave.BackColor = Color.LightGray
        _btnSave.ForeColor = Color.Black
        _btnSave.FlatStyle = FlatStyle.Flat
        _btnSave.Anchor = AnchorStyles.Bottom
        AddHandler _btnSave.Click, AddressOf OnSave
        Me.Controls.Add(_btnSave)

        _btnClose = New Button()
        _btnClose.Text = "Close"
        _btnClose.Location = New Point(560, 620)
        _btnClose.Size = New Size(120, 40)
        _btnClose.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
        _btnClose.BackColor = Color.LightGray
        _btnClose.ForeColor = Color.Black
        _btnClose.FlatStyle = FlatStyle.Flat
        _btnClose.Anchor = AnchorStyles.Bottom
        AddHandler _btnClose.Click, AddressOf OnClose
        Me.Controls.Add(_btnClose)

        Me.CancelButton = _btnClose
    End Sub

    Private Sub LoadSurchargeFile()
        Try
            _surchargeFile = Path.Combine(LegacyDataPaths.BaseDataDir, "SURCHARG.DAT")

            If File.Exists(_surchargeFile) Then
                _txtSurchargeData.Text = File.ReadAllText(_surchargeFile)
            Else
                ' Create template matching DOS format exactly
                _txtSurchargeData.Text = "ENVIRONMENTAL SURCHARGE LIST" & Environment.NewLine &
                                         Environment.NewLine &
                                         "Enter Customers abbriviated name followed by the surcharge %" & Environment.NewLine &
                                         "Do NOT enter customers that are 8% because that is automatically done." & Environment.NewLine &
                                         "Enter a number between .1 and 100 percent" & Environment.NewLine &
                                         "Example: 3V FASTENERS with a 6% surcharge would look like this ""3VFASTEN"",6" & Environment.NewLine &
                                         "**************************************************************" & Environment.NewLine
            End If

        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error loading surcharge file:{Environment.NewLine}{ex.Message}",
                              "Error", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        Try
            ' Save the file first
            File.WriteAllText(_surchargeFile, _txtSurchargeData.Text)

            ' Now validate
            Dim errors As New System.Text.StringBuilder()
            Dim lineNum = 0
            Dim inDataSection = False
            Dim prcDir = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC")

            Using reader As New StringReader(_txtSurchargeData.Text)
                Dim line As String
                While (InlineAssignHelper(line, reader.ReadLine())) IsNot Nothing
                    lineNum += 1

                    If String.IsNullOrWhiteSpace(line) Then Continue While

                    ' Check for section marker
                    If line.Contains("****************") Then
                        inDataSection = True
                        Continue While
                    End If

                    ' Only validate lines after the marker
                    If Not inDataSection Then Continue While

                    ' Parse customer name and surcharge %
                    Dim parts = line.Split(","c)
                    If parts.Length <> 2 Then
                        errors.AppendLine($"Line {lineNum}: Invalid format (expected: customer,percentage)")
                        Continue While
                    End If

                    Dim customerFile = parts(0).Trim().Trim(""""c)  ' Remove quotes
                    Dim surchargeText = parts(1).Trim()

                    ' Validate percentage
                    Dim surchargePercent As Decimal
                    If Not Decimal.TryParse(surchargeText, surchargePercent) Then
                        errors.AppendLine($"Line {lineNum}: Invalid percentage '{surchargeText}'")
                        Continue While
                    End If

                    If surchargePercent < 1 OrElse surchargePercent > 100 Then
                        errors.AppendLine($"Line {lineNum}: % MUST BE BETWEEN 1 && 100 (got {surchargePercent})")
                    End If

                    If surchargePercent = 8 Then
                        errors.AppendLine($"Line {lineNum}: % cannot be 8 (got {surchargePercent})")
                    End If

                    ' Check if customer file exists
                    Dim prcFileName = customerFile
                    If Not prcFileName.EndsWith(".PRC", StringComparison.OrdinalIgnoreCase) Then
                        prcFileName &= ".PRC"
                    End If
                    Dim prcFile = Path.Combine(prcDir, prcFileName)

                    If Not File.Exists(prcFile) Then
                        errors.AppendLine($"Line {lineNum}: Customer file not found: {prcFileName}")
                    End If
                End While
            End Using

            If errors.Length > 0 Then
                ' Show validation errors
                Using errorForm As New Form()
                    errorForm.Text = "Validation Errors"
                    errorForm.Size = New Size(700, 500)
                    errorForm.StartPosition = FormStartPosition.CenterParent
                    errorForm.BackColor = Color.Black

                    Dim txtErrors As New TextBox()
                    txtErrors.Location = New Point(10, 10)
                    txtErrors.Size = New Size(670, 400)
                    txtErrors.Multiline = True
                    txtErrors.ScrollBars = ScrollBars.Both
                    txtErrors.ReadOnly = True
                    txtErrors.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
                    txtErrors.BackColor = Color.Black
                    txtErrors.ForeColor = Color.Yellow
                    txtErrors.Text = errors.ToString()
                    txtErrors.WordWrap = False
                    txtErrors.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
                    errorForm.Controls.Add(txtErrors)

                    Dim btnCloseErrors As New Button()
                    btnCloseErrors.Text = "Close"
                    btnCloseErrors.Location = New Point(580, 425)
                    btnCloseErrors.Size = New Size(100, 35)
                    btnCloseErrors.Font = New Font("Consolas", 10.0F, FontStyle.Bold)
                    btnCloseErrors.BackColor = Color.LightGray
                    btnCloseErrors.ForeColor = Color.Black
                    btnCloseErrors.FlatStyle = FlatStyle.Flat
                    btnCloseErrors.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
                    AddHandler btnCloseErrors.Click, Sub() errorForm.Close()
                    errorForm.Controls.Add(btnCloseErrors)

                    errorForm.ShowDialog(Me)
                End Using
            Else
                DosMessageBox.Show(Me, "File saved and validated successfully!", "Success", MessageBoxButtons.OK)
                ' Reset the Modified flag after successful save
                _txtSurchargeData.Modified = False
            End If

        Catch ex As Exception
            DosMessageBox.Show(Me, $"Error saving file:{Environment.NewLine}{ex.Message}",
                              "Error", MessageBoxButtons.OK)
        End Try
    End Sub

    Private Sub OnClose(sender As Object, e As EventArgs)
        ' Check if modified
        If _txtSurchargeData.Modified Then
            Dim result = DosMessageBox.Show(Me, "Save changes before closing?", "Unsaved Changes",
                                           MessageBoxButtons.YesNo)
            If result = DialogResult.Yes Then
                OnSave(Nothing, Nothing)
            End If
        End If
        Me.Close()
    End Sub

    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function
End Class
