Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

' Implements DOS Log Book menu option (4) Look up a P.O. Number:
' - scans only the selected year's LOGBOOK.##
' - partial match using INSTR semantics (substring match)
' - shows 3 matches per page
' - page prompt: [ENTER = More]   [ESC = Quit]
' - end screen: END OF LOG BOOK, HIT [ESC] to Exit
Public Module LogBookPoLookup

    ' Represents one record in LOGBOOK.## (12 fields).
    Private NotInheritable Class LogBookRecord
        Public Property Dt As String = ""
        Public Property Customer As String = ""
        Public Property PartNo As String = ""
        Public Property InvoiceNum As String = ""
        Public Property Po As String = ""
        Public Property Spec As String = ""
        Public Property QtyAccepted As String = ""
        Public Property QtyRejected As String = ""
        Public Property Mat As String = ""
        Public Property HeatTreat As String = ""
        Public Property ReasonRejected As String = ""
        Public Property Status As String = ""
    End Class

    ' Builds DOS-style pages for PO lookup from a single LOGBOOK.## file.
    ' Returns a list of pages. Last page is always the "END OF LOG BOOK..." screen.
    Public Function BuildPoLookupPages(logBookFilePath As String, poSearch As String) As List(Of String)
        Dim pages As New List(Of String)()

        If poSearch Is Nothing Then poSearch = ""
        poSearch = poSearch.Trim()

        ' DOS: blank input would effectively quit out.
        If poSearch.Length = 0 Then
            Return pages
        End If

        If String.IsNullOrWhiteSpace(logBookFilePath) OrElse Not File.Exists(logBookFilePath) Then
            pages.Add("LOGBOOK FILE NOT FOUND:" & Environment.NewLine &
                      If(logBookFilePath, "") & Environment.NewLine & Environment.NewLine &
                      "HIT [ESC] to Exit")
            pages.Add("END OF LOG BOOK, HIT [ESC] to Exit")
            Return pages
        End If

        Dim hitsOnPage As Integer = 0
        Dim sbPage As New StringBuilder()

        Using sr As New StreamReader(logBookFilePath, Encoding.ASCII, detectEncodingFromByteOrderMarks:=True)
            While True
                Dim rec As LogBookRecord = Nothing
                If Not TryReadLegacyRecord(sr, rec) Then
                    Exit While
                End If

                ' DOS: IF INSTR(PO$,PO5$)=0 THEN skip
                If InStr(rec.Po, poSearch, CompareMethod.Text) = 0 Then
                    Continue While
                End If

                AppendRecordBlock(sbPage, rec)
                hitsOnPage += 1

                ' DOS pages after 3 matches
                If hitsOnPage >= 3 Then
                    sbPage.AppendLine()
                    sbPage.AppendLine("[ENTER = More]   [ESC = Quit]")
                    pages.Add(sbPage.ToString())

                    sbPage.Clear()
                    hitsOnPage = 0
                End If
            End While
        End Using

        ' Any leftover hits become the last result page
        If sbPage.Length > 0 Then
            sbPage.AppendLine()
            sbPage.AppendLine("[ENTER = More]   [ESC = Quit]")
            pages.Add(sbPage.ToString())
        End If

        ' DOS end-of-log screen
        pages.Add("END OF LOG BOOK, HIT [ESC] to Exit")

        Return pages
    End Function

    ' Format a record block similar to the DOS output.
    ' Tweak spacing here if you want to match your viewer width exactly.
    Private Sub AppendRecordBlock(sb As StringBuilder, rec As LogBookRecord)
        ' Your DOS screenshot shows date on the right; we include it inline to keep this simple and stable.
        ' If your viewer supports fixed columns, you can right-align rec.Dt yourself.
        sb.AppendLine(("Customer: " & rec.Customer).PadRight(26) & ("Part#: " & rec.PartNo).PadRight(40) & rec.Dt)
        sb.AppendLine(("Invoice#: " & rec.InvoiceNum).PadRight(26) & ("P.O.#: " & rec.Po).PadRight(30) & ("Status: " & rec.Status))
        sb.AppendLine(("Qty Acc/Rec: " & rec.QtyAccepted).PadRight(26) & ("Qty Rejected: " & rec.QtyRejected).PadRight(30) & ("Mat: " & rec.Mat))
        sb.AppendLine(("Heat Treat: " & rec.HeatTreat).PadRight(26) & ("Rsn Rej: " & rec.ReasonRejected))
        sb.AppendLine("Spec: " & rec.Spec)
        sb.AppendLine()
    End Sub

    ' Reads 12 fields from LOGBOOK.##:
    '  DT$, N2$, PN$, INUM, PO$, SPEC$, QA$, QR$, MT$, HT$, RR$, ST$
    '
    ' In your data, string fields are quoted (e.g., "PAMCO") and INUM is unquoted numeric text (e.g., 736254).
    Private Function TryReadLegacyRecord(sr As StreamReader, ByRef rec As LogBookRecord) As Boolean
        rec = Nothing
        If sr.EndOfStream Then Return False

        ' Read date; skip any accidental blank lines
        Dim dt As String = ReadFieldLine(sr)
        While dt IsNot Nothing AndAlso dt.Length = 0 AndAlso Not sr.EndOfStream
            dt = ReadFieldLine(sr)
        End While
        If dt Is Nothing OrElse dt.Length = 0 Then Return False

        Dim customer As String = ReadFieldLine(sr)
        Dim partNo As String = ReadFieldLine(sr)
        Dim invoiceNum As String = ReadFieldLine(sr) ' numeric line, but read as string
        Dim po As String = ReadFieldLine(sr)
        Dim spec As String = ReadFieldLine(sr)
        Dim qa As String = ReadFieldLine(sr)
        Dim qr As String = ReadFieldLine(sr)
        Dim mat As String = ReadFieldLine(sr)
        Dim ht As String = ReadFieldLine(sr)
        Dim rr As String = ReadFieldLine(sr)
        Dim st As String = ReadFieldLine(sr)

        ' If file ended mid-record, stop cleanly.
        If st Is Nothing Then Return False

        rec = New LogBookRecord() With {
            .Dt = dt,
            .Customer = customer,
            .PartNo = partNo,
            .InvoiceNum = invoiceNum,
            .Po = po,
            .Spec = spec,
            .QtyAccepted = qa,
            .QtyRejected = qr,
            .Mat = mat,
            .HeatTreat = ht,
            .ReasonRejected = rr,
            .Status = st
        }

        Return True
    End Function

    ' Reads one line/field, trims it, and strips surrounding quotes if present.
    ' Also normalizes whitespace-only fields to empty.
    Private Function ReadFieldLine(sr As StreamReader) As String
        If sr.EndOfStream Then Return Nothing

        Dim line As String = sr.ReadLine()
        If line Is Nothing Then Return Nothing

        line = line.Trim()

        ' Strip surrounding quotes if present:  "PAMCO" -> PAMCO
        If line.Length >= 2 AndAlso line.StartsWith("""", StringComparison.Ordinal) AndAlso line.EndsWith("""", StringComparison.Ordinal) Then
            line = line.Substring(1, line.Length - 2)
        End If

        ' Normalize whitespace-only content to empty (handles " " and blanks)
        If line.Trim().Length = 0 Then
            line = ""
        End If

        Return line
    End Function

End Module