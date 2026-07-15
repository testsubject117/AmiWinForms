Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO

Public Class RolodexCustomerService

    Private Shared ReadOnly RealNamePath As String = "\\invoice\mainmenu\data\REALNAME.DAT"

    Public Function GetDataFilePath() As String
        Return RealNamePath
    End Function

    Public Function DataFileExists() As Boolean
        Return File.Exists(RealNamePath)
    End Function

    Public Function LoadCustomerNames() As HashSet(Of String)
        Dim results As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        If Not File.Exists(RealNamePath) Then
            Return results
        End If

        Dim lines = File.ReadAllLines(RealNamePath)

        Dim i As Integer = 0
        While i < lines.Length
            Dim prcLine As String = Unquote(lines(i))

            Dim nameLine As String = ""
            If i + 1 < lines.Length Then
                nameLine = Unquote(lines(i + 1))
            End If

            If Not String.IsNullOrWhiteSpace(nameLine) Then
                results.Add(nameLine.Trim())
            End If

            i += 2
        End While

        Return results
    End Function

    Public Function IsCustomerName(personName As String, customerNames As HashSet(Of String)) As Boolean
        If String.IsNullOrWhiteSpace(personName) Then Return False
        If customerNames Is Nothing OrElse customerNames.Count = 0 Then Return False

        Return customerNames.Contains(personName.Trim())
    End Function

    Private Function Unquote(value As String) As String
        If value Is Nothing Then Return ""

        Dim s As String = value.Trim()

        If s.Length >= 2 AndAlso
           s.StartsWith("""", StringComparison.Ordinal) AndAlso
           s.EndsWith("""", StringComparison.Ordinal) Then

            s = s.Substring(1, s.Length - 2)
        End If

        Return s.Replace("""""", """").Trim()
    End Function

End Class

