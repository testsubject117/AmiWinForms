Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Text

Public NotInheritable Class CheckInvWriter
    Private Sub New()
    End Sub

    Public Shared Sub WriteAll(path As String, records As IEnumerable(Of CheckInvBlock))
        Dim lines As New List(Of String)()

        For Each record As CheckInvBlock In records
            Dim invoices As List(Of String) = NormalizeInvoices(record)

            lines.Add(BuildHeaderLine(record, invoices.Count))

            Dim i As Integer
            For i = 0 To invoices.Count - 1
                lines.Add(invoices(i))
            Next
        Next

        File.WriteAllLines(path, lines, Encoding.ASCII)
    End Sub

    Public Shared Sub Append(path As String, record As CheckInvBlock)
        Dim invoices As List(Of String) = NormalizeInvoices(record)

        Using sw As New StreamWriter(path, append:=True, encoding:=Encoding.ASCII)
            sw.WriteLine(BuildHeaderLine(record, invoices.Count))

            Dim i As Integer
            For i = 0 To invoices.Count - 1
                sw.WriteLine(invoices(i))
            Next
        End Using
    End Sub

    Private Shared Function NormalizeInvoices(record As CheckInvBlock) As List(Of String)
        Dim result As New List(Of String)()

        If record Is Nothing OrElse record.Invoices Is Nothing Then
            Return result
        End If

        Dim i As Integer
        For i = 0 To record.Invoices.Count - 1
            Dim raw As String = If(record.Invoices(i), "").Replace(ChrW(26), "").Trim()

            If raw = "" Then
                Continue For
            End If

            Dim n As Long
            If Long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, n) OrElse
               Long.TryParse(raw, NumberStyles.Integer, CultureInfo.CurrentCulture, n) Then
                result.Add(n.ToString(CultureInfo.InvariantCulture))
            End If
        Next

        Return result
    End Function

    Private Shared Function BuildHeaderLine(record As CheckInvBlock, invoiceCount As Integer) As String
        Return String.Join(",",
            Quote(If(record.CustomerCode, "")),
            Quote(If(record.CheckNumber, "")),
            Quote(If(record.SalesmanCode, "")),
            Quote(If(record.DateText, "")),
            record.Amount.ToString(CultureInfo.InvariantCulture),
            invoiceCount.ToString(CultureInfo.InvariantCulture))
    End Function

    Private Shared Function Quote(value As String) As String
        If value Is Nothing Then value = ""
        value = value.Replace("""", """""")
        Return """" & value & """"
    End Function
End Class
