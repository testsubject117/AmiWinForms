Option Strict Off
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports System.Linq

''' <summary>
''' Delete procedure from customer price list - DOS-style text prompts
''' Matches PLIST.ASC lines 1370-1660
''' </summary>
Public Class FormPriceListDelete
    Inherits Form

    Private _customer As String
    Private _priceListFile As String

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
    Private _currentState As DeleteState = DeleteState.ShowMenu
    Private _procedureToDelete As String = ""
    Private _foundProcedure As ProcedureItem

    Private Enum DeleteState
        ShowMenu
        WaitingForMenuChoice
        PromptForProcedure
        ShowConfirmation
        WaitingForConfirmation
        Deleting
    End Enum

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Delete Procedure"
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
    End Sub

    Private Sub ShowInitialMenu()
        _currentState = DeleteState.ShowMenu
        _rtbDisplay.Clear()
        AppendLine("")
        AppendLine("****** PROCEDURE DELETION ******")
        AppendLine("")
        AppendLine("(L) List procedures")
        AppendLine("")
        AppendLine("(Q) Quit")
        AppendLine("")
        AppendLine("(D) Delete a procedure")
        AppendLine("")
        AppendColoredLine("Hit a Key", Color.Yellow)
        _currentState = DeleteState.WaitingForMenuChoice
        _txtInput.Visible = False
        Me.Focus()
    End Sub

    Private Sub Form_KeyDown(sender As Object, e As KeyEventArgs)
        If _currentState = DeleteState.WaitingForMenuChoice Then
            Select Case e.KeyCode
                Case Keys.L
                    e.Handled = True
                    ListProcedures()
                Case Keys.Q, Keys.Escape
                    e.Handled = True
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                Case Keys.D
                    e.Handled = True
                    PromptForProcedure()
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

    Private Sub PromptForProcedure()
        _currentState = DeleteState.PromptForProcedure
        _rtbDisplay.Clear()
        AppendLine("")
        AppendLine("****** PROCEDURE DELETION ******")
        AppendLine("")
        Append("Enter name of procedure to delete? ")
        PositionInputInline()
    End Sub

    Private Sub Input_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            Dim input = _txtInput.Text.Trim()

            Select Case _currentState
                Case DeleteState.PromptForProcedure
                    If String.IsNullOrEmpty(input) Then
                        _txtInput.Clear()
                        Return
                    End If

                    _procedureToDelete = input.ToUpperInvariant()
                    SearchAndConfirmDelete()

                Case DeleteState.WaitingForConfirmation
                    If String.IsNullOrEmpty(input) Then
                        _txtInput.Clear()
                        Return
                    End If

                    Dim answer = input.ToUpperInvariant()
                    If answer.StartsWith("Y") Then
                        DeleteProcedure()
                    Else
                        ' User said No - don't delete
                        AppendLine("")
                        AppendLine("Procedure NOT deleted, PLEASE WAIT...")
                        System.Threading.Thread.Sleep(1000)
                        ShowInitialMenu()
                    End If
            End Select
        End If
    End Sub

    Private Sub SearchAndConfirmDelete()
        ' Load all procedures
        If Not LoadProcedures() Then
            ShowInitialMenu()
            Return
        End If

        ' Search for the procedure
        Dim found = _procedures.FirstOrDefault(Function(p) p.ProcedureName.Equals(_procedureToDelete, StringComparison.OrdinalIgnoreCase))

        If String.IsNullOrEmpty(found.ProcedureName) Then
            ' Not found
            AppendLine("")
            AppendLine("")
            AppendColoredLine("Procedure NOT found, Hit [ENTER]", Color.Yellow)
            System.Media.SystemSounds.Beep.Play()

            ' Wait for Enter
            _currentState = DeleteState.ShowMenu
            _txtInput.Visible = False
            AddHandler Me.KeyDown, Sub(s, e)
                                       If e.KeyCode = Keys.Enter Then
                                           RemoveHandler Me.KeyDown, Nothing
                                           ShowInitialMenu()
                                       End If
                                   End Sub
            Return
        End If

        ' Found - show details and confirm
        _foundProcedure = found
        _currentState = DeleteState.ShowConfirmation
        AppendLine("")
        AppendLine("")
        AppendLine("Procedure:  " & found.ProcedureName)
        AppendLine("Effective Date:  " & found.EffectiveDate)
        AppendLine("Minimum Charge:  " & found.MinCharge.ToString("$####.##"))
        AppendLine("Price:  " & found.Price.ToString("$#####.###") & found.PriceType)
        AppendLine("")
        Append("Do you really want to delete this  (Y/N) ? ")

        _currentState = DeleteState.WaitingForConfirmation
        PositionInputInline()
    End Sub

    Private Sub DeleteProcedure()
        Try
            ' Create temp file
            Dim tempFile = _priceListFile.Replace(".PRC", ".TMP")
            Dim deleted = False

            ' Read original, write to temp, skip the one to delete
            Using writer As New StreamWriter(tempFile, False)
                Using reader As New StreamReader(_priceListFile)
                    While Not reader.EndOfStream
                        Dim line = reader.ReadLine()
                        If String.IsNullOrWhiteSpace(line) Then Continue While

                        Dim parts = ParseDosWriteLine(line)
                        If parts.Length >= 5 Then
                            Dim procName = parts(0)

                            ' Skip the procedure we're deleting
                            If procName.Equals(_procedureToDelete, StringComparison.OrdinalIgnoreCase) Then
                                deleted = True
                                Continue While
                            End If

                            ' Write all other procedures to temp file
                            writer.WriteLine(line)
                        End If
                    End While
                End Using
            End Using

            If deleted Then
                ' Replace original with temp
                File.Delete(_priceListFile)
                File.Move(tempFile, _priceListFile)

                AppendLine("")
                AppendLine("Procedure deleted, PLEASE WAIT...")
                System.Media.SystemSounds.Beep.Play()
                System.Threading.Thread.Sleep(1000)
            Else
                ' Clean up temp file
                If File.Exists(tempFile) Then
                    File.Delete(tempFile)
                End If
            End If

            ShowInitialMenu()

        Catch ex As Exception
            MessageBox.Show("Error deleting procedure: " & ex.Message, "Error",
                           MessageBoxButtons.OK, MessageBoxIcon.Error)
            ShowInitialMenu()
        End Try
    End Sub

    Private Function LoadProcedures() As Boolean
        _procedures.Clear()

        If Not File.Exists(_priceListFile) Then
            MessageBox.Show(_customer & " does not exist.", "Not Found",
                           MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If

        Try
            Using reader As New StreamReader(_priceListFile)
                While Not reader.EndOfStream
                    Dim line = reader.ReadLine()
                    If String.IsNullOrWhiteSpace(line) Then Continue While

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
End Class

