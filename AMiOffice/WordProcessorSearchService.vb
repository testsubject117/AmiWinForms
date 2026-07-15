Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Text

''' <summary>
''' Service for searching text within word processor documents.
''' DOS equivalent: TS (text search) command on F:\WORD\*.DOC files
''' Mimics interactive pause-at-match behavior of TS.COM
''' </summary>
Public Class WordProcessorSearchService

    Private ReadOnly _searchDirectory As String

    Public Sub New(searchDirectory As String)
        _searchDirectory = searchDirectory
    End Sub

    ''' <summary>
    ''' Check if the search directory exists.
    ''' </summary>
    Public Function DirectoryExists() As Boolean
        Return Directory.Exists(_searchDirectory)
    End Function

    ''' <summary>
    ''' Delegate for reporting file search progress
    ''' </summary>
    Public Delegate Sub FileSearchingCallback(fileName As String)

    ''' <summary>
    ''' Delegate for reporting match found - returns True to continue, False to stop
    ''' </summary>
    Public Delegate Function MatchFoundCallback(fileName As String, lineNumber As Integer, matchingLine As String) As Boolean

    ''' <summary>
    ''' Search for a term in *.DOC files (mimics DOS TS.COM behavior).
    ''' Calls fileSearchingCallback for each file being searched.
    ''' Calls matchFoundCallback for each match - if it returns False, search stops.
    ''' Returns total number of matches found.
    ''' </summary>
    Public Function SearchFilesInteractive(searchTerm As String,
                                          fileSearchingCallback As FileSearchingCallback,
                                          matchFoundCallback As MatchFoundCallback) As Integer

        If String.IsNullOrWhiteSpace(searchTerm) Then
            Return 0
        End If

        If Not Directory.Exists(_searchDirectory) Then
            Return 0
        End If

        Dim matchCount As Integer = 0

        ' DOS TS.COM searches only *.DOC files
        Dim docFiles() As String
        Try
            docFiles = Directory.GetFiles(_searchDirectory, "*.DOC", SearchOption.TopDirectoryOnly)
        Catch ex As Exception
            Return 0
        End Try

        If docFiles.Length = 0 Then
            Return 0
        End If

        ' Sort files alphabetically (DOS did this)
        Array.Sort(docFiles)

        For Each filePath As String In docFiles
            Dim fileName As String = Path.GetFileName(filePath)

            ' Report progress: "Searching F:\WORD\<filename>"
            If fileSearchingCallback IsNot Nothing Then
                fileSearchingCallback("F:\WORD\" & fileName)
            End If

            Try
                Dim lineNumber As Integer = 0

                ' Read file with default encoding (like DOS did)
                Using reader As New StreamReader(filePath, Encoding.Default, True)
                    Dim line As String
                    While (InlineAssignHelper(line, reader.ReadLine())) IsNot Nothing
                        lineNumber += 1

                        ' Case-insensitive search (like DOS TS.COM)
                        If line.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 Then
                            matchCount += 1

                            ' Pause at match - ask user if they want to continue
                            If matchFoundCallback IsNot Nothing Then
                                Dim continueSearch As Boolean = matchFoundCallback(fileName, lineNumber, line)
                                If Not continueSearch Then
                                    ' User said "No" - stop searching
                                    Return matchCount
                                End If
                            End If
                        End If
                    End While
                End Using

            Catch ex As Exception
                ' Skip files that can't be read (binary files, access denied, etc.)
                Continue For
            End Try
        Next

        Return matchCount
    End Function

    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function

End Class

