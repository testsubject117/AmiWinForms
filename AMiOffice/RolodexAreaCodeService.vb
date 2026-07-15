Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text

Public Class RolodexAreaCodeService

    Private Shared ReadOnly DataPath As String = "\\invoice\mainmenu\data\phone\areacodes.csv"
    Private ReadOnly _entries As List(Of AreaCodeEntry)

    Public Sub New()
        _entries = LoadEntries()
    End Sub

    Public Function DataFileExists() As Boolean
        Return File.Exists(DataPath)
    End Function

    Public Function GetDataFilePath() As String
        Return DataPath
    End Function

    Public Function Search(input As String) As List(Of AreaCodeEntry)
        If String.IsNullOrWhiteSpace(input) Then
            Return New List(Of AreaCodeEntry)()
        End If

        Dim trimmed = input.Trim()

        If String.Equals(trimmed, "A", StringComparison.OrdinalIgnoreCase) Then
            Return _entries.
                OrderBy(Function(x) x.State).
                ThenBy(Function(x) x.AreaCode).
                ThenBy(Function(x) x.Location).
                ToList()
        End If

        If trimmed.Length = 2 AndAlso trimmed.All(Function(c) Char.IsLetter(c)) Then
            Dim stateCode = trimmed.ToUpperInvariant()

            Return _entries.
                Where(Function(x) String.Equals(x.State, stateCode, StringComparison.OrdinalIgnoreCase)).
                OrderBy(Function(x) x.AreaCode).
                ThenBy(Function(x) x.Location).
                ToList()
        End If

        Dim digitsOnly = New String(trimmed.Where(Function(c) Char.IsDigit(c)).ToArray())

        If digitsOnly.Length > 0 Then
            Return _entries.
                Where(Function(x) Not String.IsNullOrWhiteSpace(x.AreaCode) AndAlso
                                  x.AreaCode.StartsWith(digitsOnly, StringComparison.OrdinalIgnoreCase)).
                OrderBy(Function(x) x.AreaCode).
                ThenBy(Function(x) x.State).
                ThenBy(Function(x) x.Location).
                ToList()
        End If

        Return New List(Of AreaCodeEntry)()
    End Function

    Public Function FormatGroupedResults(entries As List(Of AreaCodeEntry)) As String
        Dim sb As New StringBuilder()

        If entries Is Nothing OrElse entries.Count = 0 Then
            sb.AppendLine("No matches found.")
            Return sb.ToString()
        End If

        Dim grouped = entries.
            GroupBy(Function(x) x.State).
            OrderBy(Function(g) g.Key)

        For Each grp In grouped
            sb.AppendLine(grp.Key & " area code(s):")

            For Each item In grp.OrderBy(Function(x) x.AreaCode).ThenBy(Function(x) x.Location)
                sb.AppendLine("   " & item.AreaCode.PadRight(4) & " " & item.Location)
            Next

            sb.AppendLine()
        Next

        Return sb.ToString().TrimEnd()
    End Function

    Public Function GetAllPages(Optional linesPerPage As Integer = 35) As List(Of String)
        Dim pages As New List(Of String)

        If linesPerPage <= 0 Then
            linesPerPage = 35
        End If

        Dim allText = FormatGroupedResults(
            _entries.
                OrderBy(Function(x) x.State).
                ThenBy(Function(x) x.AreaCode).
                ThenBy(Function(x) x.Location).
                ToList())

        If String.IsNullOrWhiteSpace(allText) Then
            Return pages
        End If

        Dim lines = allText.Replace(vbCrLf, vbLf).Split(ControlChars.Lf)
        Dim current As New List(Of String)

        For Each line In lines
            current.Add(line)

            If current.Count >= linesPerPage Then
                pages.Add(String.Join(Environment.NewLine, current))
                current.Clear()
            End If
        Next

        If current.Count > 0 Then
            pages.Add(String.Join(Environment.NewLine, current))
        End If

        Return pages
    End Function

    Private Function LoadEntries() As List(Of AreaCodeEntry)
        Dim results As New List(Of AreaCodeEntry)

        If Not File.Exists(DataPath) Then
            Return results
        End If

        Dim isFirstLine As Boolean = True

        For Each rawLine In File.ReadAllLines(DataPath)
            If String.IsNullOrWhiteSpace(rawLine) Then Continue For

            If isFirstLine Then
                isFirstLine = False
                If rawLine.Trim().StartsWith("AreaCode", StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
            End If

            Dim parts = rawLine.Split(","c)
            If parts.Length < 3 Then Continue For

            Dim entry As New AreaCodeEntry With {
                .AreaCode = parts(0).Trim(),
                .State = parts(1).Trim().ToUpperInvariant(),
                .Location = String.Join(",", parts.Skip(2)).Trim()
            }

            If String.IsNullOrWhiteSpace(entry.AreaCode) Then Continue For
            If String.IsNullOrWhiteSpace(entry.State) Then Continue For
            If String.IsNullOrWhiteSpace(entry.Location) Then Continue For

            results.Add(entry)
        Next

        Return results
    End Function
End Class
