Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.IO

Public NotInheritable Class OtherChkReader
    Private Sub New()
    End Sub

    Public Shared Function ReadAll(path As String) As List(Of OtherCheckEntry)
        Dim results As New List(Of OtherCheckEntry)()
        If Not File.Exists(path) Then Return results

        Dim rawLines As String() = File.ReadAllLines(path)
        Dim lines As New List(Of String)(rawLines.Length)

        For Each raw In rawLines
            If raw Is Nothing Then Continue For

            ' Remove DOS EOF marker (^Z / ChrW(26)) which can appear at end-of-file.
            Dim s As String = raw.Replace(ChrW(26), "").Trim()
            If s = "" Then Continue For

            lines.Add(s)
        Next

        Dim i As Integer = 0
        While i + 5 < lines.Count
            results.Add(New OtherCheckEntry With {
                .Company = Unquote(lines(i)),
                .DateText = Unquote(lines(i + 1)),
                .CheckNumber = Unquote(lines(i + 2)),
                .Amount = ParseDecimal(lines(i + 3)),
                .Reference = Unquote(lines(i + 4)),
                .ReasonWhy = Unquote(lines(i + 5))
            })
            i += 6
        End While

        Return results
    End Function

    Private Shared Function Unquote(s As String) As String
        If s Is Nothing Then Return ""
        s = s.Trim()

        If s.Length >= 2 AndAlso
           s.StartsWith("""", StringComparison.Ordinal) AndAlso
           s.EndsWith("""", StringComparison.Ordinal) Then

            Return s.Substring(1, s.Length - 2)
        End If

        Return s
    End Function

    Private Shared Function ParseDecimal(s As String) As Decimal
        If s Is Nothing Then Return 0D

        s = s.Replace(ChrW(26), "").Trim()

        Dim d As Decimal
        If Decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, d) Then Return d
        If Decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, d) Then Return d
        Return 0D
    End Function
End Class

Public Class OtherCheckEntry
    Public Property Company As String
    Public Property DateText As String
    Public Property CheckNumber As String
    Public Property Amount As Decimal
    Public Property Reference As String
    Public Property ReasonWhy As String
End Class