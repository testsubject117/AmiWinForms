Option Strict On
Option Explicit On

Imports System.Globalization
Imports System.IO
Imports System.Text

Public NotInheritable Class OtherChkWriter
    Private Sub New()
    End Sub

    Public Shared Sub Append(path As String, entry As OtherCheckEntry)
        If entry Is Nothing Then
            Throw New ArgumentNullException(NameOf(entry))
        End If

        Dim directoryPath As String = System.IO.Path.GetDirectoryName(path)
        If directoryPath <> "" AndAlso Not Directory.Exists(directoryPath) Then
            Directory.CreateDirectory(directoryPath)
        End If

        Dim lines As New List(Of String) From {
            Quote(entry.Company),
            Quote(entry.DateText),
            Quote(entry.CheckNumber),
            entry.Amount.ToString("0.##", CultureInfo.InvariantCulture),
            Quote(entry.Reference),
            Quote(entry.ReasonWhy)
        }

        Using sw As New StreamWriter(path, append:=True, encoding:=Encoding.ASCII)
            For Each line As String In lines
                sw.WriteLine(line)
            Next
        End Using
    End Sub

    Public Shared Sub WriteAll(path As String, entries As IEnumerable(Of OtherCheckEntry))
        Dim directoryPath As String = System.IO.Path.GetDirectoryName(path)
        If directoryPath <> "" AndAlso Not Directory.Exists(directoryPath) Then
            Directory.CreateDirectory(directoryPath)
        End If

        Dim safeEntries As List(Of OtherCheckEntry) =
            If(entries IsNot Nothing, entries.ToList(), New List(Of OtherCheckEntry)())

        Using sw As New StreamWriter(path, append:=False, encoding:=Encoding.ASCII)
            For Each entry As OtherCheckEntry In safeEntries
                sw.WriteLine(Quote(entry.Company))
                sw.WriteLine(Quote(entry.DateText))
                sw.WriteLine(Quote(entry.CheckNumber))
                sw.WriteLine(entry.Amount.ToString("0.##", CultureInfo.InvariantCulture))
                sw.WriteLine(Quote(entry.Reference))
                sw.WriteLine(Quote(entry.ReasonWhy))
            Next

            sw.Write(ChrW(26))
        End Using
    End Sub

    Private Shared Function Quote(value As String) As String
        Dim s As String = If(value, "").Trim().Replace("""", """""")
        Return """" & s & """"
    End Function
End Class
