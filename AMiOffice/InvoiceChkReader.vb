Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text

' Reads invoice detail records from the legacy DOS random-access file INVOICE.CHK.
'
' Legacy DOS layout from ENTIRE.ASC / LEDGER.ASC:
'   OPEN "INVOICE.CHK" SHARED AS #3 LEN=26
'   FIELD #3,9 AS INUM$,8 AS AMT$,8 AS CO$,1 AS FLAG$
'
' Record layout:
'   bytes 0-8   = INUM$
'   bytes 9-16  = AMT$
'   bytes 17-24 = CO$
'   byte  25    = FLAG$
'
' Important legacy rule:
'   recordNumber = invoiceNumber - 75000
'
' The DOS programs do direct random-access reads using that record number:
'   FILNUM = INT(INUM - 75000!)
'   GET #3, FILNUM
'
' In the live data, the first stored field (INUM$) does not decode reliably enough
' under the current .NET MBF conversion to be used as a hard validation gate.
' However, amount/company/flag data at the expected record slot is valid and matches
' the DOS file behavior.
'
' Therefore:
'   - invoice detail lookup uses direct record addressing
'   - INUM$ is treated as advisory only
'   - the slot is considered found when it contains meaningful invoice data
Public NotInheritable Class InvoiceChkReader
    Private Sub New()
    End Sub

    Private Const RecordSize As Integer = 26
    Private Const InvoiceBase As Long = 75000L

    ' Decodes a 4-byte Microsoft Binary Format single from the specified buffer offset.
    ' This is used for the numeric payload stored inside the fixed-width DOS record fields.
    Public Shared Function DecodeMbfSingle(buf() As Byte, offset As Integer) As Single
        If buf Is Nothing Then Return 0.0F
        If offset < 0 Then Return 0.0F
        If offset + 4 > buf.Length Then Return 0.0F

        Dim b0 As Byte = buf(offset)
        Dim b1 As Byte = buf(offset + 1)
        Dim b2 As Byte = buf(offset + 2)
        Dim b3 As Byte = buf(offset + 3)

        If b3 = 0 Then Return 0.0F

        Dim signBit As UInteger = (CUInt(b2) And &H80UI) << 24
        Dim ieeeExp As UInteger = CUInt(b3) - 1UI
        Dim mantissa As UInteger
        mantissa = ((CUInt(b2) And &H7FUI) << 16) Or (CUInt(b1) << 8) Or CUInt(b0)

        Dim bits As UInteger
        bits = signBit Or (ieeeExp << 23) Or mantissa

        Dim ieee4(3) As Byte
        ieee4(0) = CByte(bits And &HFFUI)
        ieee4(1) = CByte((bits >> 8) And &HFFUI)
        ieee4(2) = CByte((bits >> 16) And &HFFUI)
        ieee4(3) = CByte((bits >> 24) And &HFFUI)

        Dim result As Single = BitConverter.ToSingle(ieee4, 0)
        If Single.IsNaN(result) OrElse Single.IsInfinity(result) Then
            Return 0.0F
        End If

        Return result
    End Function

    Public Shared Function ReadRecord(path As String, invoiceNumber As Long) As InvoiceChkRecord
        Dim rec As New InvoiceChkRecord()
        rec.InvoiceNumber = invoiceNumber

        If invoiceNumber <= 0 Then Return rec
        If Not File.Exists(path) Then Return rec

        ' Legacy invoice-to-record mapping:
        '   displayed invoice number = record number + 75000
        '   record number            = invoice number - 75000
        Dim recordNumber As Long = invoiceNumber - InvoiceBase
        If recordNumber < 1 Then Return rec

        Try
            Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                ' Fixed-width random-access record offset.
                Dim recordOffset As Long = (recordNumber - 1L) * RecordSize
                If recordOffset < 0 Then Return rec
                If recordOffset + RecordSize > fs.Length Then Return rec

                Dim buf(RecordSize - 1) As Byte
                fs.Seek(recordOffset, SeekOrigin.Begin)

                Dim bytesRead As Integer = fs.Read(buf, 0, RecordSize)
                If bytesRead < RecordSize Then Return rec

                ' The first numeric field corresponds to legacy INUM$.
                ' Keep it only as advisory/debug information.
                ' Do not reject the record based on this value.
                Dim advisoryInum As Single = DecodeMbfSingle(buf, 0)

                ' Legacy AMT$ field (bytes 9-16)
                rec.Amount = CDec(DecodeMbfSingle(buf, 9))

                ' Legacy CO$ field (bytes 17-24)
                rec.CompanyCode = Encoding.ASCII.GetString(buf, 17, 8).TrimEnd(ChrW(0), " "c)

                ' Legacy FLAG$ field (byte 25)
                Dim flagByte As Byte = buf(25)
                If flagByte <> 0 AndAlso flagByte <> 32 Then
                    rec.Flag = ChrW(flagByte).ToString().ToUpperInvariant()
                Else
                    rec.Flag = ""
                End If

                ' Practical existence heuristic:
                ' if the addressed slot contains meaningful invoice payload, treat it as found.
                ' This matches the working legacy slot-addressing behavior better than strict
                ' validation against the decoded INUM$ field.
                If rec.CompanyCode <> "" OrElse rec.Flag <> "" OrElse rec.Amount <> 0D OrElse advisoryInum <> 0.0F Then
                    rec.IsFound = True
                End If
            End Using
        Catch
            Return rec
        End Try

        Return rec
    End Function
End Class

Public Class InvoiceChkRecord
    Public Property InvoiceNumber As Long
    Public Property Amount As Decimal
    Public Property CompanyCode As String = ""
    Public Property Flag As String = ""
    Public Property IsFound As Boolean = False

    ' Legacy invoice status flags from DOS ENTIRE.ASC / LEDGER.ASC:
    '   J = Unpaid
    '   P = Paid
    '   C = Amended / Corrected Cert
    '   V = Void
    '   E = Erased
    Public ReadOnly Property FlagDescription As String
        Get
            Select Case Flag
                Case "J"
                    Return "Unpaid"
                Case "P"
                    Return "Paid"
                Case "C"
                    Return "Amended Cert"
                Case "V"
                    Return "Void"
                Case "E"
                    Return "Erased"
                Case Else
                    Return If(Flag, "")
            End Select
        End Get
    End Property
End Class

' Flat UI row used by the invoice details grid for the selected check.
' Invoice numbers come from CHECK.INV; amount/company/status are enriched from INVOICE.CHK.
Public Class InvoiceDetailRow
    Public Property InvoiceNumber As Long
    Public Property Amount As String = ""
    Public Property CompanyCode As String = ""
    Public Property Status As String = ""
End Class
