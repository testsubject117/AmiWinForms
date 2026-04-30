Option Strict On
Option Explicit On

Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text

Partial Public Class FormAbout

    Private Sub FormAbout_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Match the old look more closely
        Me.Text = "About"

        lblAbout.Text = "About:"
        lblAppName.Text = "Active Magnetic Inspection Main Menu Application"
        lblDevelopedBy.Text = "Developed By:"

        ' Restore the ESC logo (EmbeddedResource: "ESC Logo.jpg")
        picDeveloper.Image = LoadEmbeddedImage("ESC Logo.jpg")
        picDeveloper.Visible = (picDeveloper.Image IsNot Nothing)

        ' Build the text block similar to the "before" screenshot
        rtbInfo.Clear()
        rtbInfo.SelectionAlignment = HorizontalAlignment.Center

        Dim exePath As String = Application.ExecutablePath
        Dim fvi As FileVersionInfo = FileVersionInfo.GetVersionInfo(exePath)

        Dim sb As New StringBuilder()

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

        Dim dataPath As String = "\\invoice\MainMenu\Data"
        Dim backupsPath As String = "\\invoice\MainMenu\Backups"
        Dim wordPath As String = "\\invoice\word"

        sb.AppendLine($"Data:    {dataPath}  {ExistsTag(dataPath)}")
        sb.AppendLine($"Backups: {backupsPath}  {ExistsTag(backupsPath)}")
        sb.AppendLine($"Word:    {wordPath}  {ExistsTag(wordPath)}")

        rtbInfo.Text = sb.ToString()
    End Sub

    Private Shared Function ExistsTag(path As String) As String
        Try
            If Directory.Exists(path) Then Return "[OK]"
        Catch
            ' network/permission issues => treat as not found
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