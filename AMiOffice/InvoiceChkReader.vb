Option Strict On
Option Explicit On

Imports System.IO

''' <summary>
''' Reads random-access binary records from INVOICE.CHK.
''' Each record is 26 bytes, keyed by FILNUM = InvoiceNumber - 75000.
''' Field layout (GW-BASIC FIELD# declaration):
'''   INUM$ : 9 bytes  (MBF single float in first 4 bytes, rest padding)
'''   AMT$  : 8 bytes  (MBF single float in first 4 bytes, rest padding)
'''   CO$   : 8 bytes  (ASCII company code)
'''   FLAG$ : 1 byte   (status character: J/P/C/V)
''' </summary>
Public NotInheritable Class InvoiceChkReader
    Private Sub New()
    End Sub

    Private Const RecordSize As Integer = 26
    Private Const InvoiceBase As Long = 75000L

    ''' <summary>
    ''' Decodes a 4-byte Microsoft Binary Format (MBF) single-precision float
    ''' stored at <paramref name="offset"/> within <paramref name="buf"/>.
    ''' MBF byte layout (little-endian in memory):
    '''   buf(offset+0): low mantissa bits 7-0
    '''   buf(offset+1): mid mantissa bits 15-8
    '''   buf(offset+2): sign (bit7) + high mantissa bits 22-16
    '''   buf(offset+3): biased exponent  (MBF bias = 128)
    ''' IEEE 754 single bias = 127, so ieee_biased_exp = mbf_exp_byte - 1.
    ''' </summary>
    Public Shared Function DecodeMbfSingle(buf() As Byte, offset As Integer) As Single
        If buf Is Nothing OrElse offset < 0 OrElse offset + 4 > buf.Length Then Return 0.0F

        Dim b0 As Byte = buf(offset)       ' low mantissa
        Dim b1 As Byte = buf(offset + 1)   ' mid mantissa
        Dim b2 As Byte = buf(offset + 2)   ' sign + high mantissa
        Dim b3 As Byte = buf(offset + 3)   ' MBF biased exponent

        ' Exponent byte of 0 means the value is zero.
        If b3 = 0 Then Return 0.0F

        ' Build 32-bit IEEE 754 bit pattern.
        Dim signBit As UInt32 = (CUInt(b2) And &H80UI) << 24 ' sign → bit 31
        Dim ieeExp As UInt32 = CUInt(b3) - 1UI               ' MBF bias 128 → IEEE bias 127
        Dim mantissa As UInt32 = ((CUInt(b2) And &H7FUI) << 16) Or
                                  (CUInt(b1) << 8) Or
                                  CUInt(b0)

        Dim bits As UInt32 = signBit Or (ieeExp << 23) Or mantissa
        Dim ieee4(3) As Byte
        ieee4(0) = CByte(bits And &HFFUI)
        ieee4(1) = CByte((bits >> 8) And &HFFUI)
        ieee4(2) = CByte((bits >> 16) And &HFFUI)
        ieee4(3) = CByte((bits >> 24) And &HFFUI)

        Dim result As Single = BitConverter.ToSingle(ieee4, 0)
        If Single.IsNaN(result) OrElse Single.IsInfinity(result) Then Return 0.0F
        Return result
    End Function

    ''' <summary>
    ''' Reads a single 26-byte record from INVOICE.CHK for the given invoice number.
    ''' Returns an <see cref="InvoiceChkRecord"/> whose <see cref="InvoiceChkRecord.IsFound"/>
    ''' is False when the file is absent, the record slot falls outside the file,
    ''' or any I/O error occurs.
    ''' </summary>
    Public Shared Function ReadRecord(path As String, invoiceNumber As Long) As InvoiceChkRecord
        Dim rec As New InvoiceChkRecord() With {.InvoiceNumber = invoiceNumber}

        Dim filNum As Long = invoiceNumber - InvoiceBase
        If filNum < 0 Then Return rec

        If Not File.Exists(path) Then Return rec

        Dim fileSize As Long
        Try
            fileSize = New FileInfo(path).Length
        Catch
            Return rec
        End Try

        Dim recordOffset As Long = filNum * RecordSize
        If recordOffset + RecordSize > fileSize Then Return rec

        Try
            Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                fs.Seek(recordOffset, SeekOrigin.Begin)
                Dim buf(RecordSize - 1) As Byte
                Dim bytesRead As Integer = fs.Read(buf, 0, RecordSize)
                If bytesRead < RecordSize Then Return rec

                ' INUM$ field  (offsets 0-8): MBF single in first 4 bytes
                ' We trust the offset calculation; INUM value is used for verification only.

                ' AMT$ field (offsets 9-16): MBF single in first 4 bytes
                rec.Amount = CDec(DecodeMbfSingle(buf, 9))

                ' CO$ field (offsets 17-24): 8-byte ASCII company code
                rec.CompanyCode = System.Text.Encoding.ASCII.GetString(buf, 17, 8).TrimEnd()

                ' FLAG$ field (offset 25): single ASCII status character
                Dim flagByte As Byte = buf(25)
                If flagByte <> 0 AndAlso flagByte <> 32 Then
                    rec.Flag = ChrW(flagByte).ToString()
                End If

                rec.IsFound = True
            End Using
        Catch
            ' Treat any I/O / format error as "not found"
        End Try

        Return rec
    End Function
End Class

''' <summary>Decoded record from INVOICE.CHK.</summary>
Public Class InvoiceChkRecord
    Public Property InvoiceNumber As Long
    Public Property Amount As Decimal
    Public Property CompanyCode As String = ""
    Public Property Flag As String = ""
    Public Property IsFound As Boolean = False

    ''' <summary>Human-readable description of the status flag.</summary>
    Public ReadOnly Property FlagDescription As String
        Get
            Select Case Flag
                Case "J" : Return "Journal"
                Case "P" : Return "Paid"
                Case "C" : Return "Credit"
                Case "V" : Return "Void"
                Case Else : Return If(Flag, "")
            End Select
        End Get
    End Property
End Class

''' <summary>
''' Flat row bound to the invoice-details DataGridView inside FormLedgerView.
''' </summary>
Public Class InvoiceDetailRow
    Public Property InvoiceNumber As Long
    Public Property Amount As String = ""
    Public Property CompanyCode As String = ""
    Public Property Status As String = ""
End Class
