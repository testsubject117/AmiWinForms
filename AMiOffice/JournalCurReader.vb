Option Strict On
Option Explicit On

Imports System.IO
Imports System.Globalization

' Reads sales journal records from the legacy DOS sequential file JOURNAL.CUR.
'
' Legacy DOS layout from SALES.BAS:
'   OPEN "JOURNAL.CUR" FOR INPUT AS #1
'   INPUT#,DT$:INPUT#,INUM:INPUT#,PO$:INPUT#,AMT#:INPUT#,N2$:INPUT#,PN$
'
' Record layout (6 lines per record):
'   Line 1: Date string (quoted, format "MM-DD-YYYY")
'   Line 2: Invoice Number (integer)
'   Line 3: PO Number (quoted string)
'   Line 4: Amount (decimal)
'   Line 5: Customer Name (quoted string)
'   Line 6: Procedure Name (quoted string)
'
' The file is a sequential text file where each journal entry spans 6 lines.
'
' PERFORMANCE NOTE: The file is read in one large buffered pass (64 KB buffer)
' to avoid repeated SMB round-trips over the network share.  All parsing is
' done against the already-in-memory string array, not against the network stream.
Public NotInheritable Class JournalCurReader
    Private Sub New()
    End Sub

    Public Shared Function ReadAllRecords(path As String) As List(Of JournalEntry)
        Dim records As New List(Of JournalEntry)()
        If Not File.Exists(path) Then Return records

        Try
            ' Read the entire file in one buffered network call, then parse in RAM.
            ' 64 KB buffer absorbs the full SMB MTU and cuts round-trips dramatically.
            Dim allLines() As String
            Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536, FileOptions.SequentialScan)
                Using reader As New StreamReader(fs, Text.Encoding.Default, True, 65536)
                    Dim sb As New Text.StringBuilder(CInt(fs.Length))
                    sb.Append(reader.ReadToEnd())
                    allLines = sb.ToString().Split({vbCrLf, vbLf, vbCr}, StringSplitOptions.None)
                End Using
            End Using

            ' Pre-size the list to avoid repeated reallocations (~6 lines per record)
            records = New List(Of JournalEntry)(allLines.Length \ 6)

            Dim i As Integer = 0
            While i + 5 < allLines.Length
                Dim entry As JournalEntry = ParseRecord(allLines, i)
                If entry IsNot Nothing Then records.Add(entry)
                i += 6
            End While

        Catch ex As Exception
            ' Return whatever was successfully parsed
        End Try

        Return records
    End Function

    Private Shared Function ParseRecord(lines() As String, offset As Integer) As JournalEntry
        Try
            Dim entry As New JournalEntry()

            entry.DateString = UnquoteString(lines(offset))
            Long.TryParse(lines(offset + 1).Trim(), entry.InvoiceNumber)
            entry.PoNumber = UnquoteString(lines(offset + 2))
            Decimal.TryParse(lines(offset + 3).Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, entry.Amount)
            entry.CustomerName = UnquoteString(lines(offset + 4))
            entry.ProcedureName = UnquoteString(lines(offset + 5))

            Return entry
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Shared Function UnquoteString(value As String) As String
        If String.IsNullOrEmpty(value) Then Return ""
        Dim trimmed As String = value.Trim()
        If trimmed.Length >= 2 AndAlso trimmed(0) = """"c AndAlso trimmed(trimmed.Length - 1) = """"c Then
            Return trimmed.Substring(1, trimmed.Length - 2)
        End If
        Return trimmed
    End Function

    ' Kept for any callers that still need a parsed Date; not called during bulk load.
    Public Shared Function ParseDosDate(dateStr As String) As Date
        If String.IsNullOrEmpty(dateStr) Then Return Date.MinValue
        Try
            Dim parts() As String = dateStr.Split("-"c)
            If parts.Length = 3 Then
                Dim month, day, year As Integer
                If Integer.TryParse(parts(0), month) AndAlso
                   Integer.TryParse(parts(1), day) AndAlso
                   Integer.TryParse(parts(2), year) Then
                    Return New Date(year, month, day)
                End If
            End If
        Catch
        End Try
        Return Date.MinValue
    End Function
End Class

Public Class JournalEntry
    Public Property DateString As String = ""
    Public Property TransactionDate As Date = Date.MinValue
    Public Property InvoiceNumber As Long = 0
    Public Property PoNumber As String = ""
    Public Property Amount As Decimal = 0D
    Public Property CustomerName As String = ""
    Public Property ProcedureName As String = ""

    Public ReadOnly Property IsVoid As Boolean
        Get
            Return String.Equals(PoNumber, "VOID", StringComparison.OrdinalIgnoreCase)
        End Get
    End Property
End Class
