Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Text

Public NotInheritable Class LedgerCurWriter
    Private Sub New()
    End Sub

    Public Shared Sub WriteAll(path As String, entries As IEnumerable(Of LedgerEntry))
        Dim lines As New List(Of String)()

        For Each entry In entries
            lines.AddRange(ToLines(entry))
        Next

        File.WriteAllLines(path, lines, Encoding.ASCII)
    End Sub

    Public Shared Sub Append(path As String, entry As LedgerEntry)
        Dim lines = ToLines(entry)

        Using sw As New StreamWriter(path, append:=True, encoding:=Encoding.ASCII)
            For Each line In lines
                sw.WriteLine(line)
            Next
        End Using
    End Sub

    Public Shared Function ToLines(entry As LedgerEntry) As List(Of String)
        Dim lines As New List(Of String)()

        lines.Add(Quote(entry.Customer))
        lines.Add(Quote(entry.DateText))
        lines.Add(Quote(entry.CheckNumber))
        lines.Add(Quote(entry.InvoiceDiffText))
        lines.Add(entry.Amount.ToString(CultureInfo.InvariantCulture))
        lines.Add(Quote(entry.Reference))

        Return lines
    End Function

    Private Shared Function Quote(value As String) As String
        If value Is Nothing Then value = ""
        value = value.Replace("""", "")
        Return """" & value & """"
    End Function
End Class