Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.IO
Imports System.Text

Public NotInheritable Class CheckInvReader
    Private Sub New()
    End Sub

    Public Shared Function ReadAll(path As String) As List(Of CheckInvBlock)
        Dim results As New List(Of CheckInvBlock)()
        If Not File.Exists(path) Then Return results

        Dim rawLines As String() = File.ReadAllLines(path)
        Dim i As Integer = 0

        While i < rawLines.Length
            Dim line As String = CleanLine(rawLines(i))

            If line.Length = 0 Then
                i += 1
                Continue While
            End If

            ' Header lines are GW-BASIC WRITE# output and start with a quote.
            If Not line.StartsWith("""", StringComparison.Ordinal) Then
                i += 1
                Continue While
            End If

            Dim header As CheckInvBlock = ParseHeader(line)
            i += 1

            ' Collect invoice numbers until we reach the invoice count or the next header.
            Dim invoices As New List(Of String)()

            ' Defensive: bad/blank counts happen
            Dim target As Integer = header.InvoiceCount
            If target < 0 Then target = 0
            If target > 5000 Then target = 5000 ' safety cap

            While i < rawLines.Length AndAlso invoices.Count < target
                Dim invLine As String = CleanLine(rawLines(i))

                If invLine.Length = 0 Then
                    i += 1
                    Continue While
                End If

                ' If we hit the next header early, stop this block.
                If invLine.StartsWith("""", StringComparison.Ordinal) Then
                    Exit While
                End If

                ' A single line may contain one invoice or many invoices.
                ' Try to parse as WRITE#-style CSV first. If it yields multiple parts, use them.
                Dim parts As List(Of String) = SplitBasicWriteLine(invLine)

                If parts.Count > 1 Then
                    For Each p In parts
                        Dim v = NormalizeInvoiceToken(p)
                        If v.Length > 0 Then invoices.Add(v)
                        If invoices.Count >= target Then Exit For
                    Next
                Else
                    ' Not a CSV-ish line; treat as a single invoice token
                    Dim v = NormalizeInvoiceToken(invLine)
                    If v.Length > 0 Then invoices.Add(v)
                End If

                i += 1
            End While

            header.Invoices = invoices
            results.Add(header)
        End While

        Return results
    End Function
    Public Shared Function DiagnoseFormat(path As String) As String
        If Not File.Exists(path) Then Return "File not found: " & path

        Dim rawLines = File.ReadAllLines(path)
        Dim headers As Integer = 0
        Dim invLines As Integer = 0
        Dim invLinesWithCommas As Integer = 0
        Dim invLinesThatLookQuoted As Integer = 0

        Dim seen As Integer = 0
        For Each raw In rawLines
            Dim line = If(raw, "").Replace(ChrW(26), "").Trim()
            If line = "" Then Continue For

            seen += 1
            If line.StartsWith("""", StringComparison.Ordinal) Then
                headers += 1
            Else
                invLines += 1
                If line.Contains(",", StringComparison.Ordinal) Then invLinesWithCommas += 1
                If line.StartsWith("""", StringComparison.Ordinal) OrElse line.Contains("""", StringComparison.Ordinal) Then invLinesThatLookQuoted += 1
            End If

            If seen >= 200 Then Exit For
        Next

        Return $"Lines scanned: {seen}" & Environment.NewLine &
           $"Header lines (start with quote): {headers}" & Environment.NewLine &
           $"Non-header lines: {invLines}" & Environment.NewLine &
           $"Non-header lines containing commas: {invLinesWithCommas}" & Environment.NewLine &
           $"Non-header lines containing quotes: {invLinesThatLookQuoted}"
    End Function
    Private Shared Function ParseHeader(line As String) As CheckInvBlock
        ' Example:
        ' "NATAIRCR","8508","4","01-03-1995",196.75,4
        Dim parts As List(Of String) = SplitBasicWriteLine(line)

        Dim b As New CheckInvBlock() With {
            .CustomerCode = GetPart(parts, 0),
            .CheckNumber = GetPart(parts, 1),
            .SalesmanCode = GetPart(parts, 2),
            .DateText = GetPart(parts, 3),
            .Amount = ParseDecimal(GetPart(parts, 4)),
            .InvoiceCount = ParseInt(GetPart(parts, 5)),
            .Invoices = New List(Of String)()
        }

        Return b
    End Function

    Private Shared Function CleanLine(s As String) As String
        If s Is Nothing Then Return ""
        Return s.Replace(ChrW(26), "").Trim()
    End Function

    Private Shared Function NormalizeInvoiceToken(s As String) As String
        If s Is Nothing Then Return ""
        Dim t = s.Replace(ChrW(26), "").Trim()

        ' Some legacy files may quote invoice numbers even on "invoice lines"
        ' SplitBasicWriteLine already unquotes, but for safety:
        If t.Length >= 2 AndAlso t.StartsWith("""", StringComparison.Ordinal) AndAlso t.EndsWith("""", StringComparison.Ordinal) Then
            t = t.Substring(1, t.Length - 2).Trim()
        End If

        ' Optional normalization that often matches legacy: collapse internal spaces
        ' (keep it conservative to avoid changing real invoice formats)
        t = t.Replace(vbTab, " ")
        While t.Contains("  ", StringComparison.Ordinal)
            t = t.Replace("  ", " ")
        End While

        Return t
    End Function

    Private Shared Function GetPart(parts As List(Of String), idx As Integer) As String
        If parts Is Nothing OrElse idx < 0 OrElse idx >= parts.Count Then Return ""
        Return If(parts(idx), "").Trim()
    End Function

    Private Shared Function ParseDecimal(s As String) As Decimal
        If s Is Nothing Then Return 0D
        s = s.Replace(ChrW(26), "").Trim()

        Dim d As Decimal
        If Decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, d) Then Return d
        If Decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, d) Then Return d
        Return 0D
    End Function

    Private Shared Function ParseInt(s As String) As Integer
        If s Is Nothing Then Return 0
        s = s.Replace(ChrW(26), "").Trim()

        Dim n As Integer
        If Integer.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, n) Then Return n
        If Integer.TryParse(s, NumberStyles.Integer, CultureInfo.CurrentCulture, n) Then Return n
        Return 0
    End Function

    ' Robust parser for GW-BASIC WRITE# "CSV-ish" lines:
    ' - quoted strings allowed
    ' - embedded quotes written as "" inside quoted strings
    ' - numeric fields unquoted
    Private Shared Function SplitBasicWriteLine(line As String) As List(Of String)
        Dim result As New List(Of String)()
        If line Is Nothing Then Return result

        Dim sb As New StringBuilder()
        Dim inQuotes As Boolean = False
        Dim i As Integer = 0

        While i < line.Length
            Dim ch As Char = line(i)

            If inQuotes Then
                If ch = """"c Then
                    ' Doubled quote => literal quote
                    If i + 1 < line.Length AndAlso line(i + 1) = """"c Then
                        sb.Append(""""c)
                        i += 2
                        Continue While
                    Else
                        inQuotes = False
                        i += 1
                        Continue While
                    End If
                Else
                    sb.Append(ch)
                    i += 1
                    Continue While
                End If
            Else
                If ch = """"c Then
                    inQuotes = True
                    i += 1
                    Continue While
                End If

                If ch = ","c Then
                    result.Add(sb.ToString().Trim())
                    sb.Clear()
                    i += 1
                    Continue While
                End If

                sb.Append(ch)
                i += 1
            End If
        End While

        result.Add(sb.ToString().Trim())
        Return result
    End Function
End Class

Public Class CheckInvBlock
    Public Property CustomerCode As String
    Public Property CheckNumber As String
    Public Property SalesmanCode As String
    Public Property DateText As String
    Public Property Amount As Decimal
    Public Property InvoiceCount As Integer
    Public Property Invoices As List(Of String)
End Class