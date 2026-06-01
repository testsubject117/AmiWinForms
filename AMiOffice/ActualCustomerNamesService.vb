Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text

Public Class ActualCustomerNamesService

    Public Class ActualCustomerNameEntry
        Public Property CompanyCode As String = ""
        Public Property ActualName As String = ""
    End Class

    Private ReadOnly _realNamePath As String

    Public Sub New(realNamePath As String)
        _realNamePath = If(realNamePath, "")
    End Sub

    Public Function GetDataFilePath() As String
        Return _realNamePath
    End Function

    Public Function DataFileExists() As Boolean
        Return Not String.IsNullOrWhiteSpace(_realNamePath) AndAlso File.Exists(_realNamePath)
    End Function

    Public Function LoadEntries() As List(Of ActualCustomerNameEntry)
        Dim results As New List(Of ActualCustomerNameEntry)()

        If Not DataFileExists() Then
            Return results
        End If

        Dim lines() As String = File.ReadAllLines(_realNamePath)

        Dim i As Integer = 0
        While i < lines.Length
            Dim companyCode As String = Unquote(lines(i))
            Dim actualName As String = ""

            If i + 1 < lines.Length Then
                actualName = Unquote(lines(i + 1))
            End If

            If companyCode <> "" OrElse actualName <> "" Then
                results.Add(New ActualCustomerNameEntry With {
                    .CompanyCode = companyCode.Trim(),
                    .ActualName = actualName.Trim()
                })
            End If

            i += 2
        End While

        Return results
    End Function

    Public Function GetSortedEntries() As List(Of ActualCustomerNameEntry)
        Return LoadEntries().
            OrderBy(Function(x) x.ActualName, StringComparer.OrdinalIgnoreCase).
            ThenBy(Function(x) x.CompanyCode, StringComparer.OrdinalIgnoreCase).
            ToList()
    End Function

    Public Function BuildDisplayText() As String
        Dim entries = GetSortedEntries()
        Dim sb As New StringBuilder()

        sb.AppendLine("<<< ACTUAL CUSTOMER NAMES >>>")
        sb.AppendLine()

        If entries.Count = 0 Then
            sb.AppendLine("No entries found.")
            Return sb.ToString()
        End If

        For Each entry In entries
            Dim codeText As String = If(entry.CompanyCode, "").Trim()
            Dim nameText As String = If(entry.ActualName, "").Trim()

            If codeText = "" AndAlso nameText = "" Then
                Continue For
            End If

            If codeText = "" Then
                sb.AppendLine(nameText)
            ElseIf nameText = "" Then
                sb.AppendLine(codeText)
            Else
                sb.AppendLine(codeText.PadRight(16) & " " & nameText)
            End If
        Next

        Return sb.ToString()
    End Function

    Private Function Unquote(value As String) As String
        If value Is Nothing Then
            Return ""
        End If

        Dim s As String = value

        ' Remove Unicode BOM if present (can appear at start of first line)
        s = s.Replace(ChrW(&HFEFF), "")

        ' Trim first (after BOM removal)
        s = s.Trim()

        ' Remove surrounding quotes
        If s.Length >= 2 AndAlso
       s.StartsWith("""", StringComparison.Ordinal) AndAlso
       s.EndsWith("""", StringComparison.Ordinal) Then

            s = s.Substring(1, s.Length - 2)
        End If

        ' Unescape doubled quotes
        s = s.Replace("""""", """")

        ' Strip leading/trailing control characters (DOS artifacts, etc.)
        ' Keep normal printable characters and spaces.
        s = StripEdgeControlChars(s)

        Return s.Trim()
    End Function

    Private Function StripEdgeControlChars(input As String) As String
        If String.IsNullOrEmpty(input) Then Return ""

        Dim startIdx As Integer = 0
        Dim endIdx As Integer = input.Length - 1

        While startIdx <= endIdx AndAlso Char.IsControl(input(startIdx))
            startIdx += 1
        End While

        While endIdx >= startIdx AndAlso Char.IsControl(input(endIdx))
            endIdx -= 1
        End While

        If startIdx = 0 AndAlso endIdx = input.Length - 1 Then
            Return input
        End If

        If startIdx > endIdx Then
            Return ""
        End If

        Return input.Substring(startIdx, (endIdx - startIdx) + 1)
    End Function

End Class
