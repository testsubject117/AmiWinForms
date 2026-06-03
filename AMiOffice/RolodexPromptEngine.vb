Imports System
Imports System.Collections.Generic
Imports System.Linq

Public Class RolodexPromptEngine
    Private ReadOnly _mode As RolodexPromptMode
    Private ReadOnly _repo As New RolodexRepository()

    Private _stepIndex As Integer = 0
    Private _isComplete As Boolean = False
    Private _searchText As String = ""
    Private _globalSearchMode As Boolean = False
    Private _searchResults As New List(Of RolodexPersonRecord)
    Private _currentResultIndex As Integer = -1
    Private _resultDisplayStartIndex As Integer = -1

    Public Property WorkingRecord As RolodexPersonRecord
    Public Property Transcript As New List(Of String)

    Public Sub New(mode As RolodexPromptMode, Optional existingRecord As RolodexPersonRecord = Nothing)
        _mode = mode

        If existingRecord IsNot Nothing Then
            WorkingRecord = CloneRecord(existingRecord)
        Else
            WorkingRecord = New RolodexPersonRecord()
        End If
    End Sub

    Public ReadOnly Property Title As String
        Get
            Select Case _mode
                Case RolodexPromptMode.AddPerson
                    Return "<<<< ADD A PERSON >>>>"
                Case RolodexPromptMode.ModifyPerson
                    Return "<<<< MODIFY A PERSON >>>>"
                Case RolodexPromptMode.DeletePerson
                    Return "<<<< DELETE A PERSON >>>>"
                Case RolodexPromptMode.LookupPerson
                    Return "<<<< LOOK UP A PERSON >>>>"
                Case Else
                    Return "<<<< ROLODEX >>>>"
            End Select
        End Get
    End Property

    Public Function GetCurrentPrompt() As String
        If _isComplete Then Return ""

        Select Case _mode
            Case RolodexPromptMode.AddPerson
                Return GetAddPrompt()
            Case RolodexPromptMode.ModifyPerson
                Return GetModifyPrompt()
            Case RolodexPromptMode.DeletePerson
                Return GetDeletePrompt()
            Case RolodexPromptMode.LookupPerson
                Return GetLookupPrompt()
            Case Else
                Return ""
        End Select
    End Function

    Public Function SubmitInput(input As String) As Boolean
        If _isComplete Then Return False

        Select Case _mode
            Case RolodexPromptMode.AddPerson
                Return SubmitAddInput(input)
            Case RolodexPromptMode.ModifyPerson
                Return SubmitModifyInput(input)
            Case RolodexPromptMode.DeletePerson
                Return SubmitDeleteInput(input)
            Case RolodexPromptMode.LookupPerson
                Return SubmitLookupInput(input)
            Case Else
                Return False
        End Select
    End Function

    Public Function IsComplete() As Boolean
        Return _isComplete
    End Function

    Public Function GetReviewLines() As List(Of String)
        Return WorkingRecord.GetDisplayLines()
    End Function

    Private Function GetAddPrompt() As String
        Select Case _stepIndex
            Case 0 : Return "Person or Company Name: ?"
            Case 1 : Return "Address Number & Street: [N = Nothing] ?"
            Case 2 : Return "City: ?"
            Case 3 : Return "State [ENTER = CA] ?"
            Case 4 : Return "Zip Code: ?"
            Case 5 : Return "Area Code [ENTER = 818] ?"
            Case 6 : Return "Phone Number ?"
            Case 7
                Return "When entering in a Fax #, put a 1 in front of it if it is not 818." & Environment.NewLine &
                       "Make sure there is a space after the #.      Example:  FAX:1805-222-3333" & Environment.NewLine &
                       Environment.NewLine &
                       "Misc: ?"
            Case 8 : Return "IS THIS CORRECT (Y/N) ?"
            Case Else : Return ""
        End Select
    End Function

    Private Function GetModifyPrompt() As String
        If _globalSearchMode AndAlso _stepIndex = 1 Then
            Return "When viewing [F3] will search for another occurance." & Environment.NewLine &
                   "Enter something to search for in any category like Phone# or Name." & Environment.NewLine &
                   "Search ?"
        End If

        Select Case _stepIndex
            Case 0
                Return "Enter 1st Part of Whole Name" & Environment.NewLine & Environment.NewLine &
                       "Person or Company Name [G = Global Search] ?"
            Case 2
                Return $"Person or Company Name: [ENTER = {WorkingRecord.PersonName}] ?"
            Case 3
                Return $"Address Number & Street: [N = Nothing] [ENTER = {WorkingRecord.Street}] ?"
            Case 4
                Return $"City: [ENTER = {WorkingRecord.City}] ?"
            Case 5
                Return $"State [ENTER = {If(String.IsNullOrWhiteSpace(WorkingRecord.StateCode), "CA", WorkingRecord.StateCode)}] ?"
            Case 6
                Return $"Zip Code [ENTER = {WorkingRecord.ZipCode}] ?"
            Case 7
                Return $"Area Code [ENTER = {WorkingRecord.AreaCode}] ?"
            Case 8
                Return $"Phone Number [ENTER = {WorkingRecord.PhoneNumber}] ?"
            Case 9
                Return "When entering in a Fax #, put a 1 in front of it if it is not 818." & Environment.NewLine &
                       "Make sure there is a space after the #.      Example:  FAX:1805-222-3333" & Environment.NewLine &
                       Environment.NewLine &
                       $"Misc: [A = Append] [ENTER = {WorkingRecord.Misc}] ?"
            Case 10
                Return "Is this correct (Y/N) ?"
            Case Else
                Return ""
        End Select
    End Function

    Private Function GetDeletePrompt() As String
        If _globalSearchMode AndAlso _stepIndex = 1 Then
            Return "When viewing [F3] will search for another occurance." & Environment.NewLine &
                   "Enter something to search for in any category like Phone# or Name." & Environment.NewLine &
                   "Search ?"
        End If

        Select Case _stepIndex
            Case 0
                Return "Enter 1st Part of Whole Name" & Environment.NewLine & Environment.NewLine &
                       "Person or Company Name [G = Global Search] ?"
            Case 2
                Return "Delete this Person (Y/N) [Q = Quit] ?"
            Case Else
                Return ""
        End Select
    End Function

    Private Function GetLookupPrompt() As String
        If _globalSearchMode AndAlso _stepIndex = 1 Then
            Return "When viewing [F3] will search for another occurance." & Environment.NewLine &
                   "Enter something to search for in any category like Phone# or Name." & Environment.NewLine &
                   "Search ?"
        End If

        Select Case _stepIndex
            Case 0
                Return "Enter 1st Part of Whole Name" & Environment.NewLine & Environment.NewLine &
                       "Person or Company Name [G = Global Search] ?"
            Case 2
                Return "Is this the Person you are looking for (Y/N) ?"
            Case 3
                Return "Click ""Print"" (Ctrl+P) to Print"
            Case Else
                Return ""
        End Select
    End Function

    Private Function SubmitAddInput(input As String) As Boolean
        Select Case _stepIndex
            Case 0
                WorkingRecord.PersonName = input.Trim()
            Case 1
                If input.Trim().Equals("N", StringComparison.OrdinalIgnoreCase) Then
                    WorkingRecord.Street = ""
                Else
                    WorkingRecord.Street = input.Trim()
                End If
            Case 2
                WorkingRecord.City = input.Trim()
            Case 3
                WorkingRecord.StateCode = If(String.IsNullOrWhiteSpace(input), "CA", input.Trim().ToUpperInvariant())
            Case 4
                WorkingRecord.ZipCode = input.Trim()
            Case 5
                WorkingRecord.AreaCode = If(String.IsNullOrWhiteSpace(input), "818", input.Trim())
            Case 6
                WorkingRecord.PhoneNumber = New String(input.Where(Function(c) Char.IsDigit(c)).ToArray())
            Case 7
                WorkingRecord.Misc = input.Trim()
            Case 8
                Transcript.Add("IS THIS CORRECT (Y/N) ? " & input.Trim())

                If input.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) Then
                    _repo.AddRecord(WorkingRecord)
                Else
                    Transcript.Add("")
                    Transcript.Add("(NOT ADDED)")
                End If

                _isComplete = True
                Return True
        End Select

        Transcript.Add(GetPromptSummaryLine(input))
        _stepIndex += 1
        Return True
    End Function

    Private Function SubmitModifyInput(input As String) As Boolean
        Select Case _stepIndex
            Case 0
                Transcript.Add("Enter 1st Part of Whole Name")
                Transcript.Add("Person or Company Name [G = Global Search] ? " & input.Trim())

                If input.Trim().Equals("G", StringComparison.OrdinalIgnoreCase) Then
                    _globalSearchMode = True
                    _stepIndex = 1
                    Return True
                End If

                _searchText = input.Trim()
                _searchResults = _repo.FindAllByNamePrefix(_searchText)
                _currentResultIndex = 0

                Return LoadCurrentSearchResult()

            Case 1
                If _globalSearchMode Then
                    Transcript.Add("Search ? " & input.Trim())
                    _searchResults = _repo.GlobalSearch(input.Trim())
                    _currentResultIndex = 0
                    _globalSearchMode = False
                    Return LoadCurrentSearchResult()
                End If

            Case 2
                If Not String.IsNullOrWhiteSpace(input) Then
                    WorkingRecord.PersonName = input.Trim()
                End If
            Case 3
                If input.Trim().Equals("N", StringComparison.OrdinalIgnoreCase) Then
                    WorkingRecord.Street = ""
                ElseIf Not String.IsNullOrWhiteSpace(input) Then
                    WorkingRecord.Street = input.Trim()
                End If
            Case 4
                If Not String.IsNullOrWhiteSpace(input) Then
                    WorkingRecord.City = input.Trim()
                End If
            Case 5
                If Not String.IsNullOrWhiteSpace(input) Then
                    WorkingRecord.StateCode = input.Trim().ToUpperInvariant()
                ElseIf String.IsNullOrWhiteSpace(WorkingRecord.StateCode) Then
                    WorkingRecord.StateCode = "CA"
                End If
            Case 6
                If Not String.IsNullOrWhiteSpace(input) Then
                    WorkingRecord.ZipCode = input.Trim()
                End If
            Case 7
                If Not String.IsNullOrWhiteSpace(input) Then
                    WorkingRecord.AreaCode = input.Trim()
                End If
            Case 8
                If Not String.IsNullOrWhiteSpace(input) Then
                    WorkingRecord.PhoneNumber = New String(input.Where(Function(c) Char.IsDigit(c)).ToArray())
                End If
            Case 9
                Dim value = input.Trim()
                If value.Equals("A", StringComparison.OrdinalIgnoreCase) Then
                    WorkingRecord.Misc = WorkingRecord.Misc & " "
                ElseIf Not String.IsNullOrWhiteSpace(value) Then
                    WorkingRecord.Misc = value
                End If
            Case 10
                Transcript.Add("Is this correct (Y/N) ? " & input.Trim())

                If input.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) Then
                    _repo.UpdateFirstByNamePrefix(_searchText, WorkingRecord)
                Else
                    Transcript.Add("")
                    Transcript.Add("(NOT CHANGED)")
                End If

                _isComplete = True
                Return True
        End Select

        Transcript.Add(GetPromptSummaryLine(input))
        _stepIndex += 1
        Return True
    End Function

    Private Function SubmitDeleteInput(input As String) As Boolean
        Select Case _stepIndex
            Case 0
                Transcript.Add("Enter 1st Part of Whole Name")
                Transcript.Add("Person or Company Name [G = Global Search] ? " & input.Trim())

                If input.Trim().Equals("G", StringComparison.OrdinalIgnoreCase) Then
                    _globalSearchMode = True
                    _stepIndex = 1
                    Return True
                End If

                _searchText = input.Trim()
                _searchResults = _repo.FindAllByNamePrefix(_searchText)
                _currentResultIndex = 0

                Return LoadCurrentSearchResult()

            Case 1
                If _globalSearchMode Then
                    Transcript.Add("Search ? " & input.Trim())
                    _searchResults = _repo.GlobalSearch(input.Trim())
                    _currentResultIndex = 0
                    _globalSearchMode = False
                    Return LoadCurrentSearchResult()
                End If

            Case 2
                Dim answer = input.Trim().ToUpperInvariant()

                Transcript.Add("")
                Transcript.Add("Delete this Person (Y/N) [Q = Quit] ? " & input.Trim())

                Select Case answer
                    Case "Y"
                        _repo.DeleteFirstByNamePrefix(_searchText)
                        Transcript.Add("(DELETED)")
                        _isComplete = True
                        Return True

                    Case "N"
                        _currentResultIndex += 1
                        Return LoadCurrentSearchResult()

                    Case "Q"
                        _isComplete = True
                        Return True

                    Case Else
                        Transcript.Add("Please answer Y, N, or Q.")
                        Return True
                End Select
        End Select

        Return False
    End Function

    Private Function SubmitLookupInput(input As String) As Boolean
        Select Case _stepIndex
            Case 0
                Transcript.Add("Enter 1st Part of Whole Name")
                Transcript.Add("Person or Company Name [G = Global Search] ? " & input.Trim())

                If input.Trim().Equals("G", StringComparison.OrdinalIgnoreCase) Then
                    _globalSearchMode = True
                    _stepIndex = 1
                    Return True
                End If

                _searchText = input.Trim()
                _searchResults = _repo.FindAllByNamePrefix(_searchText)
                _currentResultIndex = 0

                Return LoadCurrentSearchResult()

            Case 1
                If _globalSearchMode Then
                    Transcript.Add("Search ? " & input.Trim())
                    _searchResults = _repo.GlobalSearch(input.Trim())
                    _currentResultIndex = 0
                    _globalSearchMode = False
                    Return LoadCurrentSearchResult()
                End If

            Case 2
                Dim answer = input.Trim().ToUpperInvariant()

                Transcript.Add("")
                Transcript.Add("Is this the Person you are looking for (Y/N) ? " & input.Trim())

                If answer = "Y" Then
                    _stepIndex = 3
                    Return True
                ElseIf answer = "N" Then
                    _currentResultIndex += 1
                    Return LoadCurrentSearchResult()
                Else
                    Transcript.Add("Please answer Y or N.")
                    Return True
                End If

            Case 3
                Transcript.Add("")
                Transcript.Add("Click ""Print"" (Ctrl+P) to Print")
                _isComplete = True
                Return True
        End Select

        Return False
    End Function

    Private Function LoadCurrentSearchResult() As Boolean
        ClearDisplayedSearchResult()

        If _searchResults Is Nothing OrElse _currentResultIndex < 0 OrElse _currentResultIndex >= _searchResults.Count Then
            Transcript.Add("")
            Transcript.Add("NO MATCH FOUND.")
            Transcript.Add("")
            Transcript.Add("Press ENTER to return.")
            _isComplete = True
            Return True
        End If

        WorkingRecord = CloneRecord(_searchResults(_currentResultIndex))
        _searchText = WorkingRecord.PersonName

        _resultDisplayStartIndex = Transcript.Count

        Transcript.Add("")
        For Each line In WorkingRecord.GetDisplayLines()
            Transcript.Add(line)
        Next

        _stepIndex = 2
        Return True
    End Function

    Private Sub ClearDisplayedSearchResult()
        If _resultDisplayStartIndex >= 0 AndAlso _resultDisplayStartIndex < Transcript.Count Then
            Transcript.RemoveRange(_resultDisplayStartIndex, Transcript.Count - _resultDisplayStartIndex)
        End If
        _resultDisplayStartIndex = -1
    End Sub

    Private Function CloneRecord(record As RolodexPersonRecord) As RolodexPersonRecord
        If record Is Nothing Then Return Nothing

        Return New RolodexPersonRecord With {
            .PersonName = record.PersonName,
            .Street = record.Street,
            .City = record.City,
            .StateCode = record.StateCode,
            .ZipCode = record.ZipCode,
            .AreaCode = record.AreaCode,
            .PhoneNumber = record.PhoneNumber,
            .Misc = record.Misc,
            .IsCustomer = record.IsCustomer
        }
    End Function

    Private Function GetPromptSummaryLine(input As String) As String
        Dim prompt = GetCurrentPrompt()

        If prompt.Contains(Environment.NewLine) Then
            Dim parts = prompt.Split({Environment.NewLine}, StringSplitOptions.None)
            prompt = parts(parts.Length - 1)
        End If

        Return $"{prompt} {input}".TrimEnd()
    End Function
End Class