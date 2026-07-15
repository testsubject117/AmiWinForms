Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms

Public Class FormCustomerList
    Inherits Form

    Private _customers As List(Of CustomerInfo)

    Public Structure CustomerInfo
        Public FileName As String
        Public RealName As String
        Public FileSize As Long
        Public FileDate As Date
    End Structure

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        Me.ClientSize = New Size(1000, 700)
        Me.Text = "CUSTOMER LIST"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.KeyPreview = True
        Me.BackColor = Color.Black
        Me.ForeColor = Color.LightGray
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        Me.ResumeLayout(False)
    End Sub

    Public Sub SetData(customers As List(Of CustomerInfo))
        _customers = customers
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        ' Display after form is fully shown
        DisplayCustomers()
    End Sub

    Private Sub DisplayCustomers()
        ' Clear any existing controls first
        Me.Controls.Clear()

        If _customers Is Nothing OrElse _customers.Count = 0 Then
            ' Show empty message
            Dim lbl As New Label()
            lbl.Dock = DockStyle.Fill
            lbl.BackColor = Color.Black
            lbl.ForeColor = Color.LightGray
            lbl.Font = New Font("Consolas", 11, FontStyle.Regular)
            lbl.Text = vbCrLf & vbCrLf & "                        No customers found."
            lbl.TextAlign = ContentAlignment.TopCenter
            Me.Controls.Add(lbl)
            Return
        End If

        Dim rtb As New RichTextBox()
        rtb.Dock = DockStyle.Fill
        rtb.BackColor = Color.Black
        rtb.ForeColor = Color.LightGray
        rtb.Font = New Font("Consolas", 11, FontStyle.Regular)
        rtb.ReadOnly = True
        rtb.BorderStyle = BorderStyle.None
        rtb.WordWrap = False
        rtb.ScrollBars = RichTextBoxScrollBars.Vertical

        Dim sb As New System.Text.StringBuilder()

        ' Header
        sb.AppendLine()
        sb.AppendLine("                        *** CUSTOMER PRICE LISTS ***")
        sb.AppendLine()
        sb.AppendLine("FILENAME        REAL NAME                           SIZE    DATE")
        sb.AppendLine(New String("="c, 78))

        ' Customer list
        For Each cust In _customers
            Dim fileName = cust.FileName.PadRight(16)
            Dim realName = If(String.IsNullOrEmpty(cust.RealName), "(no name)", cust.RealName).PadRight(36)
            Dim fileSize = cust.FileSize.ToString("N0").PadLeft(8)
            Dim fileDate = cust.FileDate.ToString("MM-dd-yy").PadLeft(9)

            sb.AppendLine($"{fileName}{realName}{fileSize} {fileDate}")
        Next

        sb.AppendLine()
        sb.AppendLine($"Total: {_customers.Count} customers")
        sb.AppendLine()
        sb.AppendLine()
        sb.AppendLine(New String(" "c, 55) & "***** HIT ENTER *****")

        rtb.Text = sb.ToString()
        Me.Controls.Add(rtb)

        ' Focus the textbox so scroll keys work
        rtb.Focus()
    End Sub

    Protected Overrides Sub OnKeyPress(e As KeyPressEventArgs)
        MyBase.OnKeyPress(e)

        If e.KeyChar = ChrW(13) OrElse e.KeyChar = ChrW(27) Then ' Enter or Escape
            e.Handled = True
            Me.Close()
        End If
    End Sub

    Public Shared Function LoadCustomerList() As List(Of CustomerInfo)
        Dim customers As New List(Of CustomerInfo)()

        Try
            Dim prcDir = Path.Combine(LegacyDataPaths.BaseDataDir, "PRC")
            If Not Directory.Exists(prcDir) Then
                Return customers
            End If

            ' Load real names from REALNAME.DAT
            Dim realNames As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Dim realNamePath = Path.Combine(LegacyDataPaths.BaseDataDir, "REALNAME.DAT")

            If File.Exists(realNamePath) Then
                Try
                    Dim lines = File.ReadAllLines(realNamePath)
                    Dim i = 0
                    While i < lines.Length - 1
                        Dim fileName = lines(i).Trim().Trim(""""c)
                        Dim realName = lines(i + 1).Trim().Trim(""""c)
                        If Not String.IsNullOrEmpty(fileName) Then
                            realNames(fileName) = realName
                        End If
                        i += 2
                    End While
                Catch
                    ' If REALNAME.DAT is corrupt, just skip it
                End Try
            End If

            ' Get all .PRC files
            Dim files = Directory.GetFiles(prcDir, "*.PRC").OrderBy(Function(f) f).ToArray()

            For Each filePath In files
                Dim fileInfo = New FileInfo(filePath)
                Dim fileName = Path.GetFileName(filePath)
                Dim fileNameKey = fileName

                Dim cust As New CustomerInfo()
                cust.FileName = Path.GetFileNameWithoutExtension(fileName)
                cust.FileSize = fileInfo.Length
                cust.FileDate = fileInfo.LastWriteTime

                ' Look up real name
                If realNames.ContainsKey(fileNameKey) Then
                    cust.RealName = realNames(fileNameKey)
                Else
                    cust.RealName = ""
                End If

                customers.Add(cust)
            Next

        Catch ex As Exception
            ' Return whatever we managed to load
        End Try

        Return customers
    End Function
End Class

