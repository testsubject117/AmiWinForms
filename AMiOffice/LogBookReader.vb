Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO

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
        Dim filePath As String = System.IO.Path.Combine(_dataDir, fileName)

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

        Dim lines As String() = File.ReadAllLines(filePath)

        ' Each entry is 12 lines.
        For i As Integer = 0 To lines.Length - 12 Step 12
            Dim e As New LogBookEntry() With {
                .SourceFile = System.IO.Path.GetFileName(filePath),
                .DateText = SafeLine(lines, i + 0),
                .Customer = SafeLine(lines, i + 1),
                .PartNumber = SafeLine(lines, i + 2),
                .PONumber = SafeLine(lines, i + 4),
                .Spec = SafeLine(lines, i + 5),
                .QtyAccepted = SafeLine(lines, i + 6),
                .QtyRejected = SafeLine(lines, i + 7),
                .Material = SafeLine(lines, i + 8),
                .HeatTreat = SafeLine(lines, i + 9),
                .ReasonRejected = SafeLine(lines, i + 10),
                .Status = SafeLine(lines, i + 11)
            }

            Dim invText As String = SafeLine(lines, i + 3).Trim()
            Dim inv As Integer
            If Integer.TryParse(invText, NumberStyles.Integer, CultureInfo.InvariantCulture, inv) Then
                e.InvoiceNumber = inv
            Else
                e.InvoiceNumber = Nothing
            End If

            Yield e
        Next
    End Function

    Private Shared Function SafeLine(lines As String(), index As Integer) As String
        If lines Is Nothing Then Return ""
        If index < 0 OrElse index >= lines.Length Then Return ""
        Return If(lines(index), "").TrimEnd()
    End Function
End Class