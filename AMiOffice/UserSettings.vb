Option Strict On
Option Explicit On

Imports System.IO

''' <summary>
''' Manages simple user preferences stored in a local settings file.
''' </summary>
Friend Module UserSettings
    Private ReadOnly SettingsFilePath As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AMiOffice",
        "settings.txt"
    )

    ''' <summary>
    ''' Check if a setting flag exists.
    ''' </summary>
    Public Function GetFlag(key As String) As Boolean
        Try
            If Not File.Exists(SettingsFilePath) Then Return False

            Dim lines As String() = File.ReadAllLines(SettingsFilePath)
            For Each line In lines
                If line.Trim().Equals(key, StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If
            Next
        Catch
            ' Ignore errors, return False
        End Try

        Return False
    End Function

    ''' <summary>
    ''' Set a setting flag (adds it if not present).
    ''' </summary>
    Public Sub SetFlag(key As String)
        Try
            Dim dir As String = Path.GetDirectoryName(SettingsFilePath)
            If Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            Dim lines As New List(Of String)()

            If File.Exists(SettingsFilePath) Then
                lines.AddRange(File.ReadAllLines(SettingsFilePath))
            End If

            ' Check if already exists
            For Each line In lines
                If line.Trim().Equals(key, StringComparison.OrdinalIgnoreCase) Then
                    Return ' Already set
                End If
            Next

            ' Add the flag
            lines.Add(key)
            File.WriteAllLines(SettingsFilePath, lines)
        Catch
            ' Silent fail - don't block user if settings can't be saved
        End Try
    End Sub
End Module

