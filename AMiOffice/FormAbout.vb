Option Strict On
Option Explicit On

Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Drawing
Imports System.Windows.Forms

Partial Public Class FormAbout

    Private Shared ReadOnly InfoPanelBackColor As Color = Color.FromArgb(40, 40, 40)

    Private Sub FormAbout_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "About"
        Me.KeyPreview = True

        If btnOK IsNot Nothing Then
            Me.CancelButton = btnOK
            Me.AcceptButton = btnOK
        End If

        Me.BackColor = UiTheme.DosBackColor
        Me.ForeColor = UiTheme.DosForeColor

        lblAbout.Text = "About:"
        lblDevelopedBy.Text = "Developed By:"
        lblAppName.Text = "Active Magnetic Inspection Main Menu Application"

        lblAbout.BackColor = UiTheme.DosBackColor
        lblDevelopedBy.BackColor = UiTheme.DosBackColor
        lblAppName.BackColor = UiTheme.DosBackColor

        lblAbout.ForeColor = UiTheme.DosForeColor
        lblDevelopedBy.ForeColor = UiTheme.DosForeColor
        lblAppName.ForeColor = UiTheme.DosAccentColor

        lblAbout.Font = UiTheme.CreateDosFont(12.0F, FontStyle.Bold)
        lblDevelopedBy.Font = UiTheme.CreateDosFont(12.0F, FontStyle.Bold)
        lblAppName.Font = UiTheme.CreateDosFont(14.0F, FontStyle.Bold)

        picDeveloper.Image = LoadEmbeddedImage("ESC Logo.jpg")
        picDeveloper.Visible = (picDeveloper.Image IsNot Nothing)
        picDeveloper.BackColor = UiTheme.DosBackColor

        Dim exePath As String = Application.ExecutablePath
        Dim fvi As FileVersionInfo = FileVersionInfo.GetVersionInfo(exePath)

        Dim sb As New StringBuilder()

        ' Move text down ~1 lines
        sb.AppendLine()

        sb.AppendLine("AMiOffice")
        sb.AppendLine($"Version: {My.Application.Info.Version}")
        sb.AppendLine($"File Version: {Safe(fvi.FileVersion)}")
        sb.AppendLine($"Build: {BuildInfo.DisplayVersion}")
        sb.AppendLine()

        sb.AppendLine("Author/Developer: Kirk Saffell")
        sb.AppendLine("Company: Enterprise Services & Consulting")
        sb.AppendLine("Phone: (661) 478-0990")
        sb.AppendLine("Contact: capnkirk@capnkirk.com")
        sb.AppendLine("Year: 2026")
        sb.AppendLine()

        sb.AppendLine($"Machine: {Environment.MachineName}")
        sb.AppendLine($"User: {Environment.UserName}")
        sb.AppendLine($"OS: {RuntimeInformation.OSDescription}")
        sb.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}")
        sb.AppendLine()

        sb.AppendLine("Shared paths:")

        ' CHANGED: use AppPaths so this stays correct if the shares move.
        Dim dataPath As String = AppPaths.DataDir
        Dim backupsPath As String = AppPaths.BackupDir

        sb.AppendLine($"Data:    {dataPath}  {ExistsTag(dataPath)}")
        sb.AppendLine($"Backups: {backupsPath}  {ExistsTag(backupsPath)}")
        sb.AppendLine()

        ' CHANGED: show Word new/legacy/active to support transition
        sb.AppendLine("Word paths:")
        sb.AppendLine($"Word (new):    {AppPaths.WordDocsDirNew}  {ExistsTag(AppPaths.WordDocsDirNew)}")
        sb.AppendLine($"Word (legacy): {AppPaths.WordDocsDirLegacy}  {ExistsTag(AppPaths.WordDocsDirLegacy)}")
        sb.AppendLine($"Word (active): {AppPaths.WordDocsDir}")

        rtbInfo.Clear()
        rtbInfo.ReadOnly = True
        rtbInfo.BackColor = InfoPanelBackColor
        rtbInfo.ForeColor = UiTheme.DosForeColor
        rtbInfo.BorderStyle = BorderStyle.FixedSingle
        rtbInfo.Font = UiTheme.CreateDosFont(9.5F, FontStyle.Regular)

        rtbInfo.SelectionAlignment = HorizontalAlignment.Center
        rtbInfo.Text = sb.ToString()
        rtbInfo.SelectAll()
        rtbInfo.SelectionAlignment = HorizontalAlignment.Center
        rtbInfo.SelectionLength = 0

        ThemeContainersOnly(Me)
        StyleOkButtonLikeApp()

        ' Ensure the RichTextBox can be moved/resized
        Try
            rtbInfo.Dock = DockStyle.None
            rtbInfo.Anchor = AnchorStyles.Top
        Catch
        End Try

        Try
            rtbInfo.TabStop = False
            rtbInfo.HideSelection = True
            Me.ActiveControl = Nothing
        Catch
        End Try

        ' Apply sizing once now and once after layout completes
        FitInfoBorderToDesiredLayout()
        AddHandler Me.Shown, Sub() FitInfoBorderToDesiredLayout()

        AddHandler Me.KeyDown,
            Sub(s, ev)
                If ev.KeyCode = Keys.Escape OrElse ev.KeyCode = Keys.Q Then
                    Me.Close()
                    ev.Handled = True
                End If
            End Sub
    End Sub

    Private Sub FitInfoBorderToDesiredLayout()
        If rtbInfo Is Nothing OrElse picDeveloper Is Nothing Then Return

        Dim gapUnderLogo As Integer = 20
        Dim widthRatio As Double = 0.62
        Dim minSideMargin As Integer = 120

        ' Centered width (narrower than form)
        Dim targetWidth As Integer = CInt(Me.ClientSize.Width * widthRatio)
        targetWidth = Math.Min(targetWidth, Me.ClientSize.Width - (minSideMargin * 2))
        targetWidth = Math.Max(420, targetWidth)

        rtbInfo.Width = targetWidth
        rtbInfo.Left = (Me.ClientSize.Width - rtbInfo.Width) \ 2
        rtbInfo.Top = picDeveloper.Bottom + gapUnderLogo

        ' Extend bottom border further down:
        ' previously +140px; now +180px total (another +40).
        Dim baseBottom As Integer
        If btnOK IsNot Nothing Then
            baseBottom = btnOK.Top - 8
        Else
            baseBottom = Me.ClientSize.Height - 20
        End If

        Dim baseHeight As Integer = Math.Max(160, baseBottom - rtbInfo.Top)

        Dim forcedExtra As Integer = 180
        Dim forcedHeight As Integer = baseHeight + forcedExtra

        ' Don't exceed the form client area (leave a tiny margin)
        Dim maxAllowedHeight As Integer = Math.Max(160, (Me.ClientSize.Height - 8) - rtbInfo.Top)
        rtbInfo.Height = Math.Min(forcedHeight, maxAllowedHeight)

        ' Re-assert no docking in case any layout tried to restore it
        Try
            rtbInfo.Dock = DockStyle.None
        Catch
        End Try
    End Sub

    Private Sub StyleOkButtonLikeApp()
        If btnOK Is Nothing Then Return

        btnOK.Text = "OK"
        btnOK.UseVisualStyleBackColor = False
        btnOK.BackColor = SystemColors.Control
        btnOK.ForeColor = Color.Black
        btnOK.FlatStyle = FlatStyle.Standard
        btnOK.TextAlign = ContentAlignment.MiddleCenter

        ' Avoid focus highlight/blue border by preventing tab focus
        btnOK.TabStop = False

        ' Avoid bottom anchoring that can make it look "sunk"
        btnOK.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnOK.Top = Math.Max(0, btnOK.Top - 6)
    End Sub

    Private Shared Sub ThemeContainersOnly(root As Control)
        If root Is Nothing Then Return

        For Each c As Control In root.Controls
            If TypeOf c Is Panel OrElse TypeOf c Is TableLayoutPanel OrElse TypeOf c Is FlowLayoutPanel OrElse TypeOf c Is GroupBox Then
                Try
                    c.BackColor = UiTheme.DosBackColor
                    c.ForeColor = UiTheme.DosForeColor
                Catch
                End Try
            End If

            ThemeContainersOnly(c)
        Next
    End Sub

    Private Shared Function ExistsTag(path As String) As String
        Try
            If Directory.Exists(path) Then Return "[OK]"
        Catch
        End Try
        Return "[NOT FOUND]"
    End Function

    Private Shared Function Safe(value As String, Optional fallback As String = "") As String
        If String.IsNullOrWhiteSpace(value) Then
            If String.IsNullOrWhiteSpace(fallback) Then Return "(not set)"
            Return fallback
        End If
        Return value.Trim()
    End Function

    Private Shared Function LoadEmbeddedImage(fileName As String) As Image
        Dim asm As Assembly = Assembly.GetExecutingAssembly()
        Dim resourceName As String =
            asm.GetManifestResourceNames().
                FirstOrDefault(Function(n) n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))

        If resourceName Is Nothing Then Return Nothing

        Using s As Stream = asm.GetManifestResourceStream(resourceName)
            If s Is Nothing Then Return Nothing
            Return Image.FromStream(s)
        End Using
    End Function

End Class