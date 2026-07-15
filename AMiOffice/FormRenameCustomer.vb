Imports System
Imports System.Drawing
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Forms

''' <summary>
''' Option L - Permanently change filename or realname
''' DOS source: PLIST.ASC lines 3260-3650
''' Password-protected feature to rename customer file and display name
''' </summary>
Public Class FormRenameCustomer
    Inherits Form

    Private _oldFilename As String
    Private _oldRealname As String
    Private _newFilename As String = ""
    Private _newRealname As String = ""

    Private _lblContent As Label
    Private _txtPassword As TextBox
    Private _txtNewFilename As TextBox
    Private _txtNewRealname As TextBox
    Private _currentStep As Integer = 0 ' 0=password, 1=filename, 2=realname, 3=confirm

    Public ReadOnly Property NewFilename As String
        Get
            Return _newFilename
        End Get
    End Property

    Public Sub New(currentFilename As String)
        _oldFilename = currentFilename
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.Text = "Rename Customer"
        Me.Size = New Size(900, 500)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.Black
        Me.KeyPreview = True
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False

        ' Load old realname
        LoadOldRealname()

        ' Content label
        _lblContent = New Label()
        _lblContent.AutoSize = False
        _lblContent.Size = New Size(860, 400)
        _lblContent.Location = New Point(20, 20)
        _lblContent.Font = New Font("Consolas", 11.0F, FontStyle.Regular)
        _lblContent.ForeColor = Color.FromArgb(192, 192, 192)
        _lblContent.BackColor = Color.Black
        Me.Controls.Add(_lblContent)

        ' Password textbox (hidden input)
        _txtPassword = New TextBox()
        _txtPassword.Size = New Size(200, 25)
        _txtPassword.Font = New Font("Consolas", 11.0F)
        _txtPassword.BackColor = Color.Black
        _txtPassword.ForeColor = Color.Black  ' Hidden
        _txtPassword.BorderStyle = BorderStyle.None
        _txtPassword.Visible = False
        _txtPassword.PasswordChar = "*"c
        AddHandler _txtPassword.KeyDown, AddressOf OnPasswordKeyDown
        Me.Controls.Add(_txtPassword)

        ' New filename textbox
        _txtNewFilename = New TextBox()
        _txtNewFilename.Size = New Size(200, 25)
        _txtNewFilename.Font = New Font("Consolas", 11.0F)
        _txtNewFilename.BackColor = Color.White
        _txtNewFilename.ForeColor = Color.Black
        _txtNewFilename.MaxLength = 8
        _txtNewFilename.CharacterCasing = CharacterCasing.Upper
        _txtNewFilename.Visible = False
        AddHandler _txtNewFilename.KeyDown, AddressOf OnFilenameKeyDown
        Me.Controls.Add(_txtNewFilename)

        ' New realname textbox
        _txtNewRealname = New TextBox()
        _txtNewRealname.Size = New Size(300, 25)
        _txtNewRealname.Font = New Font("Consolas", 11.0F)
        _txtNewRealname.BackColor = Color.White
        _txtNewRealname.ForeColor = Color.Black
        _txtNewRealname.MaxLength = 40
        _txtNewRealname.Visible = False
        AddHandler _txtNewRealname.KeyDown, AddressOf OnRealnameKeyDown
        Me.Controls.Add(_txtNewRealname)

        ' Start with password prompt
        ShowPasswordPrompt()

        AddHandler Me.KeyDown, AddressOf OnFormKeyDown
    End Sub

    Private Sub LoadOldRealname()
        Try
            Dim realnameFile = Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.DAT")
            If Not File.Exists(realnameFile) Then Return

            Using reader As New StreamReader(realnameFile)
                While Not reader.EndOfStream
                    Dim filename = reader.ReadLine()?.Trim()
                    Dim realname = reader.ReadLine()?.Trim()

                    If Not String.IsNullOrEmpty(filename) Then
                        ' Strip .PRC if present
                        If filename.EndsWith(".PRC", StringComparison.OrdinalIgnoreCase) Then
                            filename = filename.Substring(0, filename.Length - 4)
                        End If

                        If filename.Equals(_oldFilename, StringComparison.OrdinalIgnoreCase) Then
                            _oldRealname = realname
                            Exit While
                        End If
                    End If
                End While
            End Using
        Catch ex As Exception
            Debug.WriteLine("Error loading old realname: " & ex.Message)
        End Try
    End Sub

    Private Sub ShowPasswordPrompt()
        _currentStep = 0
        _lblContent.Text = "Enter password"
        _txtPassword.Location = New Point(20 + 170, 20)
        _txtPassword.Visible = True
        _txtPassword.Text = ""
        _txtPassword.Focus()
    End Sub

    Private Sub OnPasswordKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            e.SuppressKeyPress = True
            CheckPassword()
        ElseIf e.KeyCode = Keys.Escape Then
            e.Handled = True
            e.SuppressKeyPress = True
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Private Sub CheckPassword()
        ' Changed from DOS password to more professional "DOH!"
        If _txtPassword.Text.Equals("DOH!", StringComparison.Ordinal) OrElse 
           _txtPassword.Text.Equals("doh!", StringComparison.OrdinalIgnoreCase) Then
            _txtPassword.Visible = False
            ShowFilenamePrompt()
        Else
            ' Wrong password - return to menu
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Private Sub ShowFilenamePrompt()
        _currentStep = 1

        Dim sb As New StringBuilder()
        sb.AppendLine("<<< PERMANENTLY CHANGE CUSTOMERS FILENAME & REALNAME >>>")
        sb.AppendLine()
        sb.AppendLine("Only use this if it is absolutely necessary!")
        sb.AppendLine()
        sb.AppendLine()
        sb.Append($"Customers new filename [Enter = {_oldFilename}] ")

        _lblContent.Text = sb.ToString()

        Dim graphics = Me.CreateGraphics()
        Dim textSize = graphics.MeasureString(sb.ToString(), _lblContent.Font)
        graphics.Dispose()

        _txtNewFilename.Location = New Point(20 + CInt(textSize.Width) - 100, 20 + CInt(textSize.Height) - 25)
        _txtNewFilename.Visible = True
        _txtNewFilename.Text = ""
        _txtNewFilename.Focus()
    End Sub

    Private Sub OnFilenameKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            e.SuppressKeyPress = True
            ProcessFilename()
        ElseIf e.KeyCode = Keys.Escape Then
            e.Handled = True
            e.SuppressKeyPress = True
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Private Sub ProcessFilename()
        Dim input = _txtNewFilename.Text.Trim().ToUpperInvariant()

        ' Default to old filename if empty
        If String.IsNullOrEmpty(input) Then
            input = _oldFilename
        End If

        ' Validate: min 5 chars, starts with letter/digit, no spaces or periods
        If input.Length < 5 Then
            Beep()
            _txtNewFilename.SelectAll()
            Return
        End If

        Dim firstChar = Char.ToUpperInvariant(input(0))
        If Not ((firstChar >= "A"c AndAlso firstChar <= "Z"c) OrElse (firstChar >= "0"c AndAlso firstChar <= "9"c)) Then
            Beep()
            _txtNewFilename.SelectAll()
            Return
        End If

        If input.Contains(" ") OrElse input.Contains(".") Then
            Beep()
            _txtNewFilename.SelectAll()
            Return
        End If

        _newFilename = input
        _txtNewFilename.Visible = False
        ShowRealnamePrompt()
    End Sub

    Private Sub ShowRealnamePrompt()
        _currentStep = 2

        Dim sb As New StringBuilder()
        sb.AppendLine("<<< PERMANENTLY CHANGE CUSTOMERS FILENAME & REALNAME >>>")
        sb.AppendLine()
        sb.AppendLine("Only use this if it is absolutely necessary!")
        sb.AppendLine()
        sb.AppendLine()
        sb.AppendLine($"Customers new filename: {_newFilename}")
        sb.AppendLine()
        sb.AppendLine()
        sb.Append($"Customers new realname [Enter = {_oldRealname}] ")

        _lblContent.Text = sb.ToString()

        Dim graphics = Me.CreateGraphics()
        Dim textSize = graphics.MeasureString(sb.ToString(), _lblContent.Font)
        graphics.Dispose()

        _txtNewRealname.Location = New Point(20 + CInt(textSize.Width) - 150, 20 + CInt(textSize.Height) - 25)
        _txtNewRealname.Visible = True
        _txtNewRealname.Text = ""
        _txtNewRealname.Focus()
    End Sub

    Private Sub OnRealnameKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.Handled = True
            e.SuppressKeyPress = True
            ProcessRealname()
        ElseIf e.KeyCode = Keys.Escape Then
            e.Handled = True
            e.SuppressKeyPress = True
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Private Sub ProcessRealname()
        Dim input = _txtNewRealname.Text.Trim()

        ' Default to old realname if empty
        If String.IsNullOrEmpty(input) Then
            input = _oldRealname
        End If

        ' Validate: min 3 chars
        If input.Length < 3 Then
            Beep()
            _txtNewRealname.SelectAll()
            Return
        End If

        _newRealname = input
        _txtNewRealname.Visible = False
        ShowConfirmation()
    End Sub

    Private Sub ShowConfirmation()
        _currentStep = 3

        Dim sb As New StringBuilder()
        sb.AppendLine("<<< PERMANENTLY CHANGE CUSTOMERS FILENAME & REALNAME >>>")
        sb.AppendLine()
        sb.AppendLine("Only use this if it is absolutely necessary!")
        sb.AppendLine()
        sb.AppendLine()
        sb.AppendLine($"Customers new Filename: {_newFilename}")
        sb.AppendLine()
        sb.AppendLine()
        sb.AppendLine($"Customers new Realname: {_newRealname}")
        sb.AppendLine()
        sb.AppendLine()
        sb.AppendLine()
        sb.AppendLine("Is this correct (Y/N) ?")

        _lblContent.Text = sb.ToString()
    End Sub

    Private Sub OnFormKeyDown(sender As Object, e As KeyEventArgs)
        If _currentStep <> 3 Then Return

        If e.KeyCode = Keys.Y Then
            e.Handled = True
            ApplyRename()
        ElseIf e.KeyCode = Keys.N OrElse e.KeyCode = Keys.Escape Then
            e.Handled = True
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Private Sub ApplyRename()
        Try
            ' 1. Rename the .PRC file
            Dim oldPrcFile = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC", _oldFilename & ".PRC")
            Dim newPrcFile = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC", _newFilename & ".PRC")

            If File.Exists(oldPrcFile) Then
                If File.Exists(newPrcFile) Then
                    DosMessageBox.Show(Me, $"A price list already exists for {_newFilename}!",
                                   "Error", MessageBoxButtons.OK)
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                    Return
                End If

                File.Move(oldPrcFile, newPrcFile)
            End If

            ' 2. Update REALNAME.DAT
            UpdateRealnameFile()

            DosMessageBox.Show(Me, $"Customer renamed successfully.{Environment.NewLine}{Environment.NewLine}" &
                           "NOTE: You must also change the name in the Ledger (P) using option (L)!",
                           "Complete", MessageBoxButtons.OK)

            Me.DialogResult = DialogResult.OK
            Me.Close()
        Catch ex As Exception
            DosMessageBox.Show(Me, "Error renaming customer: " & ex.Message,
                           "Error", MessageBoxButtons.OK)
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End Try
    End Sub

    Private Sub UpdateRealnameFile()
        Dim realnameFile = Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.DAT")
        Dim backupFile = Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.BAK")

        ' Read all entries
        Dim entries As New List(Of Tuple(Of String, String))

        If File.Exists(realnameFile) Then
            Using reader As New StreamReader(realnameFile)
                While Not reader.EndOfStream
                    Dim filename = reader.ReadLine()?.Trim()
                    Dim realname = reader.ReadLine()?.Trim()

                    If Not String.IsNullOrEmpty(filename) Then
                        ' Strip .PRC if present for comparison
                        Dim cleanFilename = filename
                        If cleanFilename.EndsWith(".PRC", StringComparison.OrdinalIgnoreCase) Then
                            cleanFilename = cleanFilename.Substring(0, cleanFilename.Length - 4)
                        End If

                        ' Replace old entry with new
                        If cleanFilename.Equals(_oldFilename, StringComparison.OrdinalIgnoreCase) Then
                            entries.Add(Tuple.Create(_newFilename & ".PRC", _newRealname))
                        Else
                            entries.Add(Tuple.Create(filename, realname))
                        End If
                    End If
                End While
            End Using
        End If

        ' Backup
        If File.Exists(realnameFile) Then
            File.Copy(realnameFile, backupFile, True)
        End If

        ' Write back
        Using writer As New StreamWriter(realnameFile, False)
            For Each entry In entries
                writer.WriteLine(entry.Item1)
                writer.WriteLine(entry.Item2)
            Next
        End Using
    End Sub
End Class

