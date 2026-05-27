Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.IO

Public NotInheritable Class LedgerCurReader
    Private Sub New()
    End Sub

    Public Shared Function ReadAll(path As String) As List(Of LedgerEntry)
        Dim results As New List(Of LedgerEntry)()

        If Not File.Exists(path) Then
            Return results
        End If

        Dim rawLines As String() = File.ReadAllLines(path)
        Dim normalized As New List(Of String)(rawLines.Length)

        Dim i As Integer
        For i = 0 To rawLines.Length - 1
            Dim raw As String = rawLines(i)

            If raw Is Nothing Then
                normalized.Add("")
            Else
                normalized.Add(raw.Replace(ChrW(26), ""))
            End If
        Next

        i = 0
        While i + 5 < normalized.Count
            Dim e As New LedgerEntry()
            e.Customer = Unquote(normalized(i))
            e.DateText = Unquote(normalized(i + 1))
            e.CheckNumber = Unquote(normalized(i + 2))
            e.InvoiceDiffText = Unquote(normalized(i + 3))
            e.Amount = ParseDecimal(normalized(i + 4))
            e.Reference = Unquote(normalized(i + 5))

            results.Add(e)
            i += 6
        End While

        Return results
    End Function

    Private Shared Function Unquote(s As String) As String
        If s Is Nothing Then Return ""

        s = s.Replace(ChrW(26), "")
        s = s.Trim()

        If s.Length >= 2 AndAlso
           s.StartsWith("""", StringComparison.Ordinal) AndAlso
           s.EndsWith("""", StringComparison.Ordinal) Then
            s = s.Substring(1, s.Length - 2)
        End If

        Return s
    End Function

    Private Shared Function ParseDecimal(s As String) As Decimal
        If s Is Nothing Then Return 0D

        s = s.Replace(ChrW(26), "")
        s = s.Trim()

        Dim d As Decimal
        If Decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, d) Then Return d
        If Decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, d) Then Return d
        Return 0D
    End Function
End Class

Public Class LedgerEntry
    Public Property Customer As String
    Public Property DateText As String
    Public Property CheckNumber As String
    Public Property InvoiceDiffText As String
    Public Property Amount As Decimal
    Public Property Reference As String
End Class