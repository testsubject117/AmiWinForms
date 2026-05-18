Option Strict Off
Option Explicit On

Imports System.IO
Imports System.Collections.Generic
Imports System.Linq

Public Class CompanyLookupService

    Private ReadOnly _realNamePath As String
    Private _items As List(Of CompanyInfo)

    Public Sub New(realNamePath As String)
        _realNamePath = realNamePath
        _items = New List(Of CompanyInfo)()
    End Sub

    Public Sub LoadData()
        _items.Clear()

        If String.IsNullOrWhiteSpace(_realNamePath) Then
            Throw New ApplicationException("REALNAME.DAT path is blank.")
        End If

        If Not File.Exists(_realNamePath) Then
            Throw New FileNotFoundException("REALNAME.DAT was not found.", _realNamePath)
        End If

        Dim lines() As String = File.ReadAllLines(_realNamePath)

        For Each rawLine As String In lines
            If rawLine Is Nothing Then Continue For

            Dim line As String = rawLine.Trim()
            If line = "" Then Continue For

            Dim info As CompanyInfo = ParseRealNameLine(line)
            If info IsNot Nothing Then
                _items.Add(info)
            End If
        Next
    End Sub

    Public Function GetAllCompanies() As List(Of CompanyInfo)
        Return _items _
            .OrderBy(Function(x) x.CompanyCode) _
            .ToList()
    End Function

    Public Function FindExact(companyCode As String) As CompanyInfo
        Dim normalized As String = NormalizeCompanyCode(companyCode)
        If normalized = "" Then Return Nothing

        Return _items.FirstOrDefault(
            Function(x) String.Equals(x.CompanyCode, normalized, StringComparison.OrdinalIgnoreCase)
        )
    End Function

    Public Function Exists(companyCode As String) As Boolean
        Return FindExact(companyCode) IsNot Nothing
    End Function

    Public Function FindByPrefix(prefix As String) As List(Of CompanyInfo)
        Dim normalized As String = NormalizeCompanyCode(prefix)
        If normalized = "" Then
            Return New List(Of CompanyInfo)()
        End If

        Return _items _
            .Where(Function(x) x.CompanyCode IsNot Nothing AndAlso
                               x.CompanyCode.StartsWith(normalized, StringComparison.OrdinalIgnoreCase)) _
            .OrderBy(Function(x) x.CompanyCode) _
            .ToList()
    End Function

    Public Function FindByFirstLetter(letter As String) As List(Of CompanyInfo)
        Dim normalized As String = NormalizeCompanyCode(letter)
        If normalized = "" Then
            Return New List(Of CompanyInfo)()
        End If

        Dim firstChar As String = normalized.Substring(0, 1)

        Return _items _
            .Where(Function(x) x.CompanyCode IsNot Nothing AndAlso
                               x.CompanyCode.StartsWith(firstChar, StringComparison.OrdinalIgnoreCase)) _
            .OrderBy(Function(x) x.CompanyCode) _
            .ToList()
    End Function

    Private Function NormalizeCompanyCode(value As String) As String
        If value Is Nothing Then Return ""

        Dim s As String = value.Trim().ToUpperInvariant()

        If s.EndsWith(".PRC", StringComparison.OrdinalIgnoreCase) Then
            s = s.Substring(0, s.Length - 4)
        End If

        If s.Length > 8 Then
            s = s.Substring(0, 8)
        End If

        Return s
    End Function

    Private Function ParseRealNameLine(line As String) As CompanyInfo
        ' Expected legacy pattern is roughly:
        ' COMPANY.PRC,Company Name
        ' or similar.
        '
        ' We keep parsing tolerant because the legacy file may not be perfectly uniform.

        Dim commaPos As Integer = line.IndexOf(","c)

        If commaPos <= 0 Then
            Return Nothing
        End If

        Dim leftPart As String = line.Substring(0, commaPos).Trim()
        Dim rightPart As String = line.Substring(commaPos + 1).Trim()

        If leftPart = "" Then Return Nothing

        Dim prcName As String = leftPart
        Dim code As String = prcName

        If code.EndsWith(".PRC", StringComparison.OrdinalIgnoreCase) Then
            code = code.Substring(0, code.Length - 4)
        End If

        code = NormalizeCompanyCode(code)

        Dim info As New CompanyInfo()
        info.CompanyCode = code
        info.PrcFileName = code & ".PRC"
        info.CompanyName = rightPart

        Return info
    End Function

End Class
