Option Strict On
Option Explicit On

Imports System.IO

''' <summary>
''' Service for managing scrolling main menu messages and subliminal flash messages.
''' DOS-faithful implementation of NOTES-4.ED and SUBLIMAL.MES file-based messaging.
''' </summary>
Public Class MessageService
    Private Shared ReadOnly ScrollingMessageFile As String = Path.Combine(LegacyDataPaths.BaseDataDir, "NOTES-4.ED")
    Private Shared ReadOnly SubliminalMessageFile As String = Path.Combine(LegacyDataPaths.BaseDataDir, "SUBLIMAL.MES")
    Private Shared ReadOnly SubliminalLogFile As String = Path.Combine(LegacyDataPaths.BaseDataDir, "SUBLIMAL.LOG")

    ''' <summary>
    ''' Reads the scrolling message from NOTES-4.ED (Option V).
    ''' Returns empty string if file doesn't exist.
    ''' </summary>
    Public Shared Function ReadScrollingMessage() As String
        Try
            If Not File.Exists(ScrollingMessageFile) Then
                Return ""
            End If

            Dim content As String = File.ReadAllText(ScrollingMessageFile).Trim()

            ' DOS format: stored as a WRITE# record with quotes
            If content.StartsWith("""") AndAlso content.EndsWith("""") Then
                content = content.Substring(1, content.Length - 2)
            End If

            Return content
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ''' <summary>
    ''' Saves the scrolling message to NOTES-4.ED (Option V).
    ''' Max length: 129 characters per DOS behavior.
    ''' </summary>
    Public Shared Sub SaveScrollingMessage(message As String)
        If message Is Nothing Then
            message = ""
        End If

        If message.Length > 129 Then
            Throw New ArgumentException("Message too long. Maximum 129 characters.")
        End If

        ' Ensure directory exists
        Dim dir As String = Path.GetDirectoryName(ScrollingMessageFile)
        If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then
            Directory.CreateDirectory(dir)
        End If

        ' Write in DOS WRITE# format (quoted string)
        File.WriteAllText(ScrollingMessageFile, """" & message & """")
    End Sub

    ''' <summary>
    ''' Reads the subliminal flash message from SUBLIMAL.MES (Option H).
    ''' Returns empty string if file doesn't exist.
    ''' </summary>
    Public Shared Function ReadSubliminalMessage() As String
        Try
            If Not File.Exists(SubliminalMessageFile) Then
                Return ""
            End If

            Dim content As String = File.ReadAllText(SubliminalMessageFile).Trim()

            ' DOS format: stored as a WRITE# record with quotes
            If content.StartsWith("""") AndAlso content.EndsWith("""") Then
                content = content.Substring(1, content.Length - 2)
            End If

            Return content
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ''' <summary>
    ''' Saves the subliminal flash message to SUBLIMAL.MES (Option H).
    ''' Max length: 35 characters per DOS behavior.
    ''' Logs the change to SUBLIMAL.LOG with date.
    ''' </summary>
    Public Shared Sub SaveSubliminalMessage(message As String)
        If message Is Nothing Then
            message = ""
        End If

        If message.Length > 35 Then
            Throw New ArgumentException("Message too long. Maximum 35 characters.")
        End If

        ' Ensure directory exists
        Dim dir As String = Path.GetDirectoryName(SubliminalMessageFile)
        If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then
            Directory.CreateDirectory(dir)
        End If

        ' Write in DOS WRITE# format (quoted string)
        File.WriteAllText(SubliminalMessageFile, """" & message & """")

        ' Log the change
        Try
            Dim logEntry As String = """" & message & """,""" & DateTime.Now.ToString("MM/dd/yyyy") & """" & Environment.NewLine
            File.AppendAllText(SubliminalLogFile, logEntry)
        Catch
            ' Ignore log failures
        End Try
    End Sub

    ''' <summary>
    ''' Validates the password for Option H (Quick Message Flashing).
    ''' DOS password: "dean" or "DEAN"
    ''' </summary>
    Public Shared Function ValidateSubliminalPassword(password As String) As Boolean
        If String.IsNullOrEmpty(password) Then
            Return False
        End If

        Return password.Equals("dean", StringComparison.OrdinalIgnoreCase)
    End Function
End Class

