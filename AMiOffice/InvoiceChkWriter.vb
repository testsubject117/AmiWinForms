Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text

Public NotInheritable Class InvoiceChkWriter
    Private Sub New()
    End Sub

    Private Const RecordSize As Integer = 26
    Private Const InvoiceBase As Long = 75000L

    Public Shared Sub SetFlag(path As String, invoiceNumber As Long, flag As String)
        If String.IsNullOrEmpty(path) Then Return
        If Not File.Exists(path) Then Return

        Dim filNum As Long = invoiceNumber - InvoiceBase
        If filNum < 1 Then Return

        Dim recordOffset As Long = (filNum - 1) * RecordSize

        Using fs As New FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)
            If recordOffset + RecordSize > fs.Length Then
                Return
            End If

            fs.Seek(recordOffset, SeekOrigin.Begin)

            Dim buf(RecordSize - 1) As Byte
            Dim bytesRead As Integer = fs.Read(buf, 0, RecordSize)
            If bytesRead < RecordSize Then
                Return
            End If

            Dim flagByte As Byte = 0
            If Not String.IsNullOrEmpty(flag) Then
                flagByte = CByte(AscW(flag.Substring(0, 1)))
            End If

            buf(25) = flagByte

            fs.Seek(recordOffset, SeekOrigin.Begin)
            fs.Write(buf, 0, buf.Length)
            fs.Flush()
        End Using
    End Sub
End Class
