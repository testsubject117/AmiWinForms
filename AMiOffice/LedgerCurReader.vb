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

        Dim lines As String() = File.ReadAllLines(path)

        ' Collect non-null lines; skip truly blank lines.
        ' Also strip DOS EOF marker (^Z / ChrW(26)) which can appear at end-of-file.
        Dim cleaned As New List(Of String)(lines.Length)
        For Each raw In lines
            If raw Is Nothing Then Continue For

            ' Remove ^Z anywhere in the line (some legacy files include it at EOF).
            Dim s As String = raw.Replace(ChrW(26), "").Trim()

            If s = "" Then
                ' skip truly blank lines (not a data line)
                Continue For
            End If

            cleaned.Add(s)
        Next

        ' Each entry is 6 lines
        Dim i As Integer = 0
        While i + 5 < cleaned.Count
            Dim e As New LedgerEntry() With {
                .Customer = Unquote(cleaned(i)),
                .DateText = Unquote(cleaned(i + 1)),
                .CheckNumber = Unquote(cleaned(i + 2)),
                .InvoiceDiffText = Unquote(cleaned(i + 3)),
                .Amount = ParseDecimal(cleaned(i + 4)),
                .Reference = Unquote(cleaned(i + 5))
            }
            results.Add(e)
            i += 6
        End While

        Return results
    End Function

    Private Shared Function Unquote(s As String) As String
        If s Is Nothing Then Return ""
        s = s.Trim()

        ' Handle GW-BASIC style: "TEXT" or ""
        If s.Length >= 2 AndAlso
           s.StartsWith("""", StringComparison.Ordinal) AndAlso
           s.EndsWith("""", StringComparison.Ordinal) Then

            s = s.Substring(1, s.Length - 2)
        End If

        Return s
    End Function

    Private Shared Function ParseDecimal(s As String) As Decimal
        If s Is Nothing Then Return 0D
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