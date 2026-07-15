Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports System.Linq

''' <summary>
''' Add procedure to customer price list - DOS-style text prompts
''' Matches PLIST.ASC lines 580-1170
''' </summary>
Public Class FormPriceListAdd
    Inherits Form

    Private _customer As String
    Private _priceListFile As String

    ' Session variables (reused across loop)
    Private _minCharge As Decimal = 0  ' MC2 in DOS
    Private _lastProcedure As String = ""  ' PN2$ in DOS
    Private _lastPrice As Decimal = 0  ' P2 in DOS
    Private _lastPriceType As String = "/EA"  ' PL2$ in DOS

    ' Standard procedures list
    Private _standardProcedures As New List(Of String)()

    ' Procedures loaded from file
    Private _procedures As New List(Of ProcedureItem)

    Private Structure ProcedureItem
        Public ProcedureName As String
        Public EffectiveDate As String
        Public MinCharge As Decimal
        Public Price As Decimal
        Public PriceType As String ' "/#" or "/EA"
    End Structure

    ' UI Controls
    Private _rtbDisplay As RichTextBox
    Private _txtInput As TextBox

    ' State machine
    Private _currentState As AddState = AddState.ShowMenu
    Private _tempProcedure As String = ""
    Private _tempPrice As Decimal = 0
    Private _tempPriceType As String = ""

    Private Enum AddState
        ShowMenu
        WaitingForMenuChoice
        PromptMinCharge
        PromptProcedure
        PromptPrice
        PromptPriceType
        ShowConfirmation
        WaitingForConfirmation
        NonStandardWarning
        WaitingForNinthLetter
        AskAddToStandard
        WaitingForYesNo
        Saving
    End Enum

    Public Sub New()
        InitializeComponent()
        LoadStandardProcedures()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Add Procedure"
        Me.Size = New Size(800, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.BackColor = Color.Black
        Me.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        Me.KeyPreview = True

        ' Display area - fill entire form
        _rtbDisplay = New RichTextBox()
        _rtbDisplay.Location = New Point(0, 0)
        _rtbDisplay.Size = New Size(Me.ClientSize.Width, Me.ClientSize.Height)
        _rtbDisplay.Dock = DockStyle.Fill
        _rtbDisplay.BackColor = Color.Black
        _rtbDisplay.ForeColor = Color.FromArgb(170, 170, 170)
        _rtbDisplay.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        _rtbDisplay.ReadOnly = True
        _rtbDisplay.BorderStyle = BorderStyle.None
        _rtbDisplay.TabStop = False
        Me.Controls.Add(_rtbDisplay)

        ' Input textbox - positioned inline, initially hidden
        _txtInput = New TextBox()
        _txtInput.BackColor = Color.Black
        _txtInput.ForeColor = Color.White
        _txtInput.Font = New Font("Consolas", 10.0F, FontStyle.Regular)
        _txtInput.BorderStyle = BorderStyle.None
        _txtInput.Visible = False
        Me.Controls.Add(_txtInput)
        _txtInput.BringToFront()

        AddHandler Me.KeyDown, AddressOf Form_KeyDown
        AddHandler _txtInput.KeyDown, AddressOf Input_KeyDown
        AddHandler Me.Shown, Sub() Me.BeginInvoke(New Action(Sub() ShowInitialMenu()))
    End Sub

    Public Sub SetCustomer(customer As String, priceListFile As String)
        _customer = customer
        _priceListFile = priceListFile
        Me.Text = "Add Procedure - " & customer
    End Sub

    Private Sub LoadStandardProcedures()
        Try
            Dim procFile = Path.Combine(LegacyDataPaths.BaseDataDir, "Word", "PROCDURE.DOC")
            If File.Exists(procFile) Then
                Using reader As New StreamReader(procFile)
                    While Not reader.EndOfStream
                        Dim line = reader.ReadLine()
                        If String.IsNullOrWhiteSpace(line) Then Continue While

                        ' Parse: "PROCEDURE",mincharge
                        Dim parts = line.Split(","c)
                        If parts.Length >= 1 Then
                            Dim proc = parts(0).Trim().Trim(""""c)
                            If Not String.IsNullOrEmpty(proc) Then
                                _standardProcedures.Add(proc.ToUpperInvariant())
                            End If
                        End If
                    End While
                End Using
            End If
        Catch ex As Exception
            ' Continue without standard procedures list
            Debug.WriteLine("Could not load PROCDURE.DOC: " & ex.Message)
        End Try
    End Sub

    Private Sub ShowInitialMenu()
        _currentState = AddState.ShowMenu
        _rtbDisplay.Clear()
        AppendLine("")
        AppendLine("****** ADD PROCEDURE ******")
        AppendLine("")
        AppendLine("(L) List procedures")
        AppendLine("")
        AppendLine("(Q) Quit")
        AppendLine("")
        AppendLine("[ENTER]  Add a procedure")
        AppendLine("")
        AppendColoredLine("Press a key listed above to continue", Color.Yellow)
        _currentState = AddState.WaitingForMenuChoice
        _txtInput.Visible = False
        Me.Focus()
    End Sub

    Private Sub Form_KeyDown(sender As Object, e As KeyEventArgs)
        If _currentState = AddState.WaitingForMenuChoice Then
            Select Case e.KeyCode
                Case Keys.L
                    e.Handled = True
                    ListProcedures()
                Case Keys.Q, Keys.Escape
                    e.Handled = True
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                Case Keys.Enter
                    e.Handled = True
                    PromptForMinCharge()
            End Select
        End If
    End Sub

    Private Sub ListProcedures()
        ' Show procedures like option (A), then return to menu
        If Not LoadProcedures() Then
            ShowInitialMenu()
            Return
        End If

        Using frm As New FormPriceListView()
            Dim objList As New List(Of Object)
            For Each proc In _procedures
                objList.Add(proc)
            Next
            frm.SetData(_customer, objList)
            frm.ShowDialog(Me)
        End Using
        ShowInitialMenu()
    End Sub

    Private Sub PromptForMinCharge()
        _currentState = AddState.PromptMinCharge
        _rtbDisplay.Clear()
        AppendLine("")
        AppendLine("****** ADD PROCEDURE ******")
        AppendLine("")
        AppendLine("(L) List procedures")
        AppendLine("")
        AppendLine("(Q) Quit")
        AppendLine("")
        AppendLine("[ENTER]  Add a procedure")
        AppendLine("")
        AppendColoredLine("Press a key listed above to continue", Color.Yellow)
        AppendLine("")
        Append("Enter minimum charge ? ")
        PositionInputInline()
    End Sub

    Private Sub PromptForProcedure()
        _currentState = AddState.PromptProcedure
        _rtbDisplay.Clear()
        AppendLine("")
        AppendLine("****** ADD PROCEDURE ******")
        AppendLine("")
        AppendLine("")
        If Not String.IsNullOrEmpty(_lastProcedure) Then
            AppendLine("       Last procedure entered was " & _lastProcedure)
        Else
            AppendLine("       Last procedure entered was")
        End If
        Append("Enter procedure name [Q = Quit] ? ")
        PositionInputInline()
    End Sub

    Private Sub PromptForPrice()
        _currentState = AddState.PromptPrice
        Append(vbCrLf)
        Append("Enter Price [ENTER = " & _lastPrice & "] ? ")
        PositionInputInline()
    End Sub

    Private Sub PromptForPriceType()
        _currentState = AddState.PromptPriceType
        Append(vbCrLf)
        Append("Is the price by (P)ound or (E)ach [ENTER = " & _lastPriceType & "] ? ")
        PositionInputInline()
    End Sub

    Private Sub Input_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            e.SuppressKeyPress = True
            ProcessInput()
        ElseIf e.KeyCode = Keys.Escape Then
            e.Handled = True
            If _currentState = AddState.PromptProcedure Then
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
            End If
        End If
    End Sub

    Private Sub ProcessInput()
        Dim input = _txtInput.Text.Trim()

        Select Case _currentState
            Case AddState.PromptMinCharge
                Dim mc As Decimal
                If Not Decimal.TryParse(input, mc) OrElse mc < 0 Then
                    MessageBox.Show("Please enter a valid minimum charge.", "Invalid",
                                   MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    _txtInput.Clear()
                    Return
                End If
                _minCharge = mc
                PromptForProcedure()

            Case AddState.PromptProcedure
                If String.IsNullOrEmpty(input) Then
                    _txtInput.Clear()
                    Return
                End If
                If input.ToUpperInvariant() = "Q" Then
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                    Return
                End If

                Dim procName = input.ToUpperInvariant()

                ' Validate
                If procName.Length > 27 Then
                    MessageBox.Show("Procedure name cannot exceed 27 characters.", "Too Long",
                                   MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    _txtInput.Clear()
                    Return
                End If
                If procName.Contains(""""c) Then
                    MessageBox.Show("Please don't use quotes in the procedure name.", "Invalid",
                                   MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    _txtInput.Clear()
                    Return
                End If
                If procName.Contains("--") Then
                    MessageBox.Show("Please don't use double dashes in the procedure name.", "Invalid",
                                   MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    _txtInput.Clear()
                    Return
                End If

                _tempProcedure = procName
                PromptForPrice()

            Case AddState.PromptPrice
                If String.IsNullOrEmpty(input) Then
                    _tempPrice = _lastPrice
                Else
                    Dim p As Decimal
                    If Not Decimal.TryParse(input, p) OrElse p < 0 Then
                        MessageBox.Show("Please enter a valid price.", "Invalid",
                                       MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        _txtInput.Clear()
                        Return
                    End If
                    _tempPrice = p
                End If
                PromptForPriceType()

            Case AddState.PromptPriceType
                If String.IsNullOrEmpty(input) Then
                    _tempPriceType = _lastPriceType
                ElseIf input.ToUpperInvariant() = "P" Then
                    _tempPriceType = "/#"
                ElseIf input.ToUpperInvariant() = "E" Then
                    _tempPriceType = "/EA"
                Else
                    _txtInput.Clear()
                    Return
                End If
                ValidateAndConfirm()

            Case AddState.WaitingForConfirmation
                If input.ToUpperInvariant() = "Y" Then
                    SaveProcedure()
                ElseIf input.ToUpperInvariant() = "N" Then
                    PromptForProcedure()  ' Start over
                Else
                    _txtInput.Clear()
                End If

            Case AddState.WaitingForNinthLetter
                If input.ToUpperInvariant() = "I" Then
                    AskAddToStandard()
                Else
                    PromptForProcedure()  ' Rejected, start over
                End If

            Case AddState.WaitingForYesNo
                If input.ToUpperInvariant() = "Y" Then
                    AddToStandardProcedures()
                    ShowFinalConfirmation()
                ElseIf input.ToUpperInvariant() = "N" Then
                    ShowFinalConfirmation()
                Else
                    _txtInput.Clear()
                End If
        End Select
    End Sub

    Private Sub ValidateAndConfirm()
        ' Check if procedure is standard
        Dim isStandard = False
        Dim procBase = _tempProcedure

        If _tempProcedure.Contains("-") Then
            procBase = _tempProcedure.Substring(0, _tempProcedure.IndexOf("-"))
        End If

        For Each stdProc In _standardProcedures
            If procBase.StartsWith(stdProc) OrElse stdProc.StartsWith(procBase) Then
                isStandard = True
                Exit For
            End If
        Next

        If Not isStandard Then
            ShowNonStandardWarning()
        Else
            ShowFinalConfirmation()
        End If
    End Sub

    Private Sub ShowNonStandardWarning()
        _currentState = AddState.NonStandardWarning
        _rtbDisplay.Clear()
        AppendLine("")
        AppendLine("Customer name:  " & _customer)
        AppendLine("")
        AppendLine("Procedure:  " & _tempProcedure)
        AppendLine("")
        AppendLine("Effective date:  " & DateTime.Now.ToString("MM-dd-yyyy"))
        AppendLine("")
        AppendLine("Minimum charge:  " & _minCharge.ToString("$####.0000"))
        AppendLine("")
        AppendLine("Price:  " & _tempPrice.ToString("$#####.0000") & _tempPriceType)
        AppendLine("")

        AppendColoredLine("Possible Correct Procedures:", Color.Black, Color.FromArgb(170, 170, 170))
        Dim firstLetter = _tempProcedure.Substring(0, 1)
        Dim shown = False
        For Each proc In _standardProcedures.Where(Function(p) p.StartsWith(firstLetter)).Take(10)
            Append(proc & "    ")
            shown = True
        Next
        If shown Then AppendLine("")

        AppendLine("")
        System.Media.SystemSounds.Beep.Play()
        AppendLine("WARNING, " & _tempProcedure & " is not a not a standard procedure.")
        AppendLine("")
        AppendLine("If you would like to use it anyway, enter the 9th letter of the alphabet.")

        _currentState = AddState.WaitingForNinthLetter
        PositionInputInline()
    End Sub

    Private Sub AskAddToStandard()
        _currentState = AddState.AskAddToStandard
        Append(vbCrLf)
        Append("Would you like to add " & _tempProcedure & " to the standard procedure list (Y/N)? ")
        _currentState = AddState.WaitingForYesNo
        PositionInputInline()
    End Sub

    Private Sub AddToStandardProcedures()
        Try
            Dim procFile = Path.Combine(LegacyDataPaths.BaseDataDir, "Word", "PROCDURE.DOC")
            Using writer As New StreamWriter(procFile, True)
                writer.WriteLine("""" & _tempProcedure & """,0")
            End Using
            _standardProcedures.Add(_tempProcedure)
        Catch ex As Exception
            MessageBox.Show("Error adding to standard procedures: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowFinalConfirmation()
        _currentState = AddState.ShowConfirmation
        _rtbDisplay.Clear()
        AppendLine("")
        AppendLine("Customer name:  " & _customer)
        AppendLine("")
        AppendLine("Procedure:  " & _tempProcedure)
        AppendLine("")
        AppendLine("Effective date:  " & DateTime.Now.ToString("MM-dd-yyyy"))
        AppendLine("")
        AppendLine("Minimum charge:  " & _minCharge.ToString("$####.0000"))
        AppendLine("")
        AppendLine("Price:  " & _tempPrice.ToString("$#####.0000") & _tempPriceType)
        AppendLine("")
        AppendLine("")
        Append("Is this correct (Y/N)")

        _currentState = AddState.WaitingForConfirmation
        PositionInputInline()
    End Sub

    Private Sub SaveProcedure()
        Try
            Dim effDate = DateTime.Now.ToString("MM-dd-yyyy")

            Using writer As New StreamWriter(_priceListFile, True)
                writer.WriteLine("""" & _tempProcedure & """,""" & effDate & """," &
                               _minCharge.ToString("F4") & "," & _tempPrice.ToString("F4") & ",""" & _tempPriceType & """")
            End Using

            ' Update last values for next loop
            _lastProcedure = _tempProcedure
            _lastPrice = _tempPrice
            _lastPriceType = _tempPriceType

            AppendLine("")
            AppendLine("")
            AppendLine(_customer & " HAS BEEN UPDATED")

            ' Loop back to procedure prompt (line 660 in DOS)
            System.Threading.Thread.Sleep(1000)
            PromptForProcedure()

        Catch ex As Exception
            MessageBox.Show("Error saving procedure: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub AppendLine(text As String)
        _rtbDisplay.AppendText(text & vbCrLf)
    End Sub

    Private Sub Append(text As String)
        _rtbDisplay.AppendText(text)
    End Sub

    Private Sub AppendColoredLine(text As String, foreColor As Color)
        Dim startPos = _rtbDisplay.TextLength
        _rtbDisplay.AppendText(text & vbCrLf)
        _rtbDisplay.Select(startPos, text.Length)
        _rtbDisplay.SelectionColor = foreColor
        _rtbDisplay.Select(_rtbDisplay.TextLength, 0)
    End Sub

    Private Sub AppendColoredLine(text As String, foreColor As Color, backColor As Color)
        Dim startPos = _rtbDisplay.TextLength
        _rtbDisplay.AppendText(text & vbCrLf)
        _rtbDisplay.Select(startPos, text.Length)
        _rtbDisplay.SelectionColor = foreColor
        _rtbDisplay.SelectionBackColor = backColor
        _rtbDisplay.Select(_rtbDisplay.TextLength, 0)
    End Sub

    Private Sub PositionInputInline()
        ' Position the textbox at the end of the current text
        Dim endPos = _rtbDisplay.TextLength
        Dim pt = _rtbDisplay.GetPositionFromCharIndex(endPos)

        _txtInput.Left = pt.X
        _txtInput.Top = pt.Y
        _txtInput.Width = 300
        _txtInput.Height = 20
        _txtInput.Visible = True
        _txtInput.Clear()
        _txtInput.Focus()
    End Sub

    Private Function LoadProcedures() As Boolean
        _procedures.Clear()

        If Not File.Exists(_priceListFile) Then
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
End Class

