Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Text

Public Class LogBookReader
    Private ReadOnly _dataDir As String

    Public Sub New(dataDir As String)
        _dataDir = dataDir
    End Sub

    Public Iterator Function ReadEntriesFromSpecificFile(filePath As String) As IEnumerable(Of LogBookEntry)
        For Each e In ReadEntriesFromPath(filePath)
            Yield e
        Next
    End Function

    Public Iterator Function ReadEntries(yearTwoDigit As Integer) As IEnumerable(Of LogBookEntry)
        Dim fileName As String = $"LOGBOOK.{yearTwoDigit:00}"
        Dim filePath As String = Path.Combine(_dataDir, fileName)

        For Each e In ReadEntriesFromPath(filePath)
            Yield e
        Next
    End Function

    Public Iterator Function ReadEntriesAllYears() As IEnumerable(Of LogBookEntry)
        If Not Directory.Exists(_dataDir) Then
            Return
        End If

        Dim files = Directory.EnumerateFiles(_dataDir, "LOGBOOK.*", SearchOption.TopDirectoryOnly)

        Dim ordered As New List(Of String)(files)
        ordered.Sort(StringComparer.OrdinalIgnoreCase)

        For Each filePath In ordered
            For Each e In ReadEntriesFromPath(filePath)
                Yield e
            Next
        Next
    End Function

    Private Iterator Function ReadEntriesFromPath(filePath As String) As IEnumerable(Of LogBookEntry)
        If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
            Return
        End If

        Dim allLines As String() = File.ReadAllLines(filePath)
        If allLines Is Nothing OrElse allLines.Length = 0 Then
            Return
        End If

        Dim fields As New List(Of String)(capacity:=12)

        For Each raw In allLines
            Dim s As String = If(raw, "").TrimEnd()

            ' Skip truly empty lines (but keep quoted blanks like " " which are meaningful)
            If s.Length = 0 Then
                Continue For
            End If

            fields.Add(NormalizeQuotedField(s))

            If fields.Count = 12 Then
                Dim e As New LogBookEntry() With {
                .SourceFile = Path.GetFileName(filePath),
                .DateText = SafeField(fields, 0),
                .Customer = SafeField(fields, 1),
                .PartNumber = SafeField(fields, 2),
                .PONumber = SafeField(fields, 4),
                .Spec = SafeField(fields, 5),
                .QtyAccepted = SafeField(fields, 6),
                .QtyRejected = SafeField(fields, 7),
                .Material = SafeField(fields, 8),
                .HeatTreat = SafeField(fields, 9),
                .ReasonRejected = SafeField(fields, 10),
                .Status = SafeField(fields, 11)
            }

                Dim invText As String = SafeField(fields, 3).Trim()
                Dim inv As Integer
                If Integer.TryParse(invText, NumberStyles.Integer, CultureInfo.InvariantCulture, inv) Then
                    e.InvoiceNumber = inv
                Else
                    e.InvoiceNumber = Nothing
                End If

                Yield e
                fields.Clear()
            End If
        Next

        ' If leftover partial record exists, ignore it (file may end mid-write)
    End Function

    Private Shared Function SafeField(fields As List(Of String), index As Integer) As String
        If fields Is Nothing Then Return ""
        If index < 0 OrElse index >= fields.Count Then Return ""
        Return If(fields(index), "")
    End Function

    Private Iterator Function ParseCsvStyleRecords(lines As String(), filePath As String) As IEnumerable(Of LogBookEntry)
        Dim buffer As New List(Of String)()

        For Each rawLine In lines
            Dim line As String = If(rawLine, "").Trim()

            If line.Length = 0 Then
                Continue For
            End If

            Dim fields As List(Of String) = SplitDosCsvLine(line)

            If fields.Count = 0 Then
                Continue For
            End If

            For Each f In fields
                buffer.Add(NormalizeQuotedField(f))
            Next

            Do While buffer.Count >= 12
                Dim rec As List(Of String) = buffer.GetRange(0, 12)
                buffer.RemoveRange(0, 12)

                Dim entry As LogBookEntry = MapFieldsToEntry(rec, filePath)

                ' Heuristic: consider this a real parsed record if it has at least
                ' a date/customer/part/spec/po/invoice signal.
                If LooksLikeRealEntry(entry) Then
                    Yield entry
                End If
            Loop
        Next
    End Function

    Private Iterator Function ParseFixed12LineRecords(lines As String(), filePath As String) As IEnumerable(Of LogBookEntry)
        For i As Integer = 0 To lines.Length - 12 Step 12
            Dim rec As New List(Of String) From {
                SafeLine(lines, i + 0),
                SafeLine(lines, i + 1),
                SafeLine(lines, i + 2),
                SafeLine(lines, i + 3),
                SafeLine(lines, i + 4),
                SafeLine(lines, i + 5),
                SafeLine(lines, i + 6),
                SafeLine(lines, i + 7),
                SafeLine(lines, i + 8),
                SafeLine(lines, i + 9),
                SafeLine(lines, i + 10),
                SafeLine(lines, i + 11)
            }

            Dim entry As LogBookEntry = MapFieldsToEntry(rec, filePath)
            Yield entry
        Next
    End Function

    Private Shared Function MapFieldsToEntry(fields As IList(Of String), filePath As String) As LogBookEntry
        Dim e As New LogBookEntry() With {
            .SourceFile = Path.GetFileName(filePath),
            .DateText = SafeField(fields, 0),
            .Customer = SafeField(fields, 1),
            .PartNumber = SafeField(fields, 2),
            .PONumber = SafeField(fields, 4),
            .Spec = SafeField(fields, 5),
            .QtyAccepted = SafeField(fields, 6),
            .QtyRejected = SafeField(fields, 7),
            .Material = SafeField(fields, 8),
            .HeatTreat = SafeField(fields, 9),
            .ReasonRejected = SafeField(fields, 10),
            .Status = SafeField(fields, 11)
        }

        Dim invText As String = SafeField(fields, 3).Trim()
        Dim inv As Integer
        If Integer.TryParse(invText, NumberStyles.Integer, CultureInfo.InvariantCulture, inv) Then
            e.InvoiceNumber = inv
        Else
            e.InvoiceNumber = Nothing
        End If

        Return e
    End Function

    Private Shared Function LooksLikeRealEntry(entry As LogBookEntry) As Boolean
        If entry Is Nothing Then Return False

        If Not String.IsNullOrWhiteSpace(entry.DateText) Then Return True
        If Not String.IsNullOrWhiteSpace(entry.Customer) Then Return True
        If Not String.IsNullOrWhiteSpace(entry.PartNumber) Then Return True
        If Not String.IsNullOrWhiteSpace(entry.Spec) Then Return True
        If Not String.IsNullOrWhiteSpace(entry.PONumber) Then Return True
        If entry.InvoiceNumber.HasValue Then Return True

        Return False
    End Function

    Private Shared Function SplitDosCsvLine(line As String) As List(Of String)
        Dim result As New List(Of String)()
        If String.IsNullOrEmpty(line) Then Return result

        Dim sb As New StringBuilder()
        Dim inQuotes As Boolean = False

        For i As Integer = 0 To line.Length - 1
            Dim ch As Char = line(i)

            If ch = """"c Then
                inQuotes = Not inQuotes
                sb.Append(ch)
            ElseIf ch = ","c AndAlso Not inQuotes Then
                result.Add(sb.ToString())
                sb.Clear()
            Else
                sb.Append(ch)
            End If
        Next

        result.Add(sb.ToString())
        Return result
    End Function

    Private Shared Function SafeField(fields As IList(Of String), index As Integer) As String
        If fields Is Nothing Then Return ""
        If index < 0 OrElse index >= fields.Count Then Return ""
        Return NormalizeQuotedField(fields(index))
    End Function

    Private Shared Function SafeLine(lines As String(), index As Integer) As String
        If lines Is Nothing Then Return ""
        If index < 0 OrElse index >= lines.Length Then Return ""

        Dim raw As String = If(lines(index), "").TrimEnd()
        Return NormalizeQuotedField(raw)
    End Function

    Private Shared Function NormalizeQuotedField(value As String) As String
        If value Is Nothing Then Return ""

        Dim s As String = value.Trim()

        If s.Length >= 2 AndAlso s(0) = """"c AndAlso s(s.Length - 1) = """"c Then
            s = s.Substring(1, s.Length - 2).Trim()
        End If

        Return s
    End Function
End Class