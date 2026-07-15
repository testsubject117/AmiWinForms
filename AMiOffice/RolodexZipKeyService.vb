Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq

Public Class RolodexZipKeyService

    Private Shared ReadOnly CsvPath As String = "\\invoice\mainmenu\data\phone\zipcodes.csv"
    Private ReadOnly _entries As List(Of ZipKeyEntry)

    Public Sub New()
        _entries = LoadEntries()
    End Sub

    Public Function Search(input As String, Optional citySearchText As String = Nothing) As List(Of ZipKeyEntry)
        Dim results As New List(Of ZipKeyEntry)

        If String.IsNullOrWhiteSpace(input) Then
            Return results
        End If

        Dim trimmed = input.Trim()

        If trimmed = "?" Then
            If String.IsNullOrWhiteSpace(citySearchText) Then
                Return results
            End If

            Dim cityTerm = citySearchText.Trim().ToUpperInvariant()

            results = _entries.
                Where(Function(x) Not String.IsNullOrWhiteSpace(x.City) AndAlso
                                  x.City.ToUpperInvariant().Contains(cityTerm)).
                OrderBy(Function(x) x.State).
                ThenBy(Function(x) x.City).
                ThenBy(Function(x) x.ZipCode).
                ToList()

            Return results
        End If

        If trimmed.Length = 2 AndAlso trimmed.All(Function(c) Char.IsLetter(c)) Then
            Dim stateCode = trimmed.ToUpperInvariant()

            results = _entries.
                Where(Function(x) String.Equals(x.State, stateCode, StringComparison.OrdinalIgnoreCase)).
                OrderBy(Function(x) x.City).
                ThenBy(Function(x) x.ZipCode).
                ToList()

            Return results
        End If

        Dim digitsOnly = New String(trimmed.Where(Function(c) Char.IsDigit(c)).ToArray())

        If digitsOnly.Length > 0 Then
            results = _entries.
                Where(Function(x) Not String.IsNullOrWhiteSpace(x.ZipCode) AndAlso
                                  x.ZipCode.StartsWith(digitsOnly, StringComparison.OrdinalIgnoreCase)).
                OrderBy(Function(x) x.ZipCode).
                ThenBy(Function(x) x.City).
                ThenBy(Function(x) x.State).
                ToList()

            Return results
        End If

        Dim citySearch = trimmed.ToUpperInvariant()

        results = _entries.
            Where(Function(x) Not String.IsNullOrWhiteSpace(x.City) AndAlso
                              x.City.ToUpperInvariant().Contains(citySearch)).
            OrderBy(Function(x) x.State).
            ThenBy(Function(x) x.City).
            ThenBy(Function(x) x.ZipCode).
            ToList()

        Return results
    End Function

    Public Function DataFileExists() As Boolean
        Return File.Exists(CsvPath)
    End Function

    Public Function GetDataFilePath() As String
        Return CsvPath
    End Function

    Private Function LoadEntries() As List(Of ZipKeyEntry)
        Dim results As New List(Of ZipKeyEntry)

        If Not File.Exists(CsvPath) Then
            Return results
        End If

        Dim isFirstLine As Boolean = True

        For Each rawLine In File.ReadAllLines(CsvPath)
            If String.IsNullOrWhiteSpace(rawLine) Then Continue For

            If isFirstLine Then
                isFirstLine = False
                If rawLine.Trim().StartsWith("ZipCode", StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
            End If

            Dim parts = rawLine.Split(","c)
            If parts.Length < 3 Then Continue For

            Dim entry As New ZipKeyEntry With {
                .ZipCode = parts(0).Trim(),
                .City = parts(1).Trim(),
                .State = parts(2).Trim().ToUpperInvariant()
            }

            If String.IsNullOrWhiteSpace(entry.ZipCode) Then Continue For
            If String.IsNullOrWhiteSpace(entry.City) Then Continue For
            If String.IsNullOrWhiteSpace(entry.State) Then Continue For

            results.Add(entry)
        Next

        Return results
    End Function
End Class
