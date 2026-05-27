Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Linq

Public NotInheritable Class CheckRepository

    Private Sub New()
    End Sub

    Public Class CheckRecord
        Public Property CustomerName As String = ""
        Public Property CheckNumber As String = ""
        Public Property Reference As String = ""
        Public Property CheckDate As String = ""
        Public Property PaidInvoices As New List(Of String)()
        Public Property Amount As Decimal
    End Class

    Private Shared ReadOnly _checks As New List(Of CheckRecord)()

    Public Shared Sub AddOrReplace(record As CheckRecord)
        If record Is Nothing Then Return

        Dim existing = _checks.FirstOrDefault(Function(x) String.Equals(x.CheckNumber, record.CheckNumber, StringComparison.OrdinalIgnoreCase))
        If existing IsNot Nothing Then
            _checks.Remove(existing)
        End If

        _checks.Add(record)
    End Sub

    Public Shared Function FindByCheckNumber(checkNo As String) As CheckRecord
        If String.IsNullOrWhiteSpace(checkNo) Then Return Nothing

        Return _checks.FirstOrDefault(Function(x) String.Equals(x.CheckNumber, checkNo.Trim(), StringComparison.OrdinalIgnoreCase))
    End Function

    Public Shared Function DeleteByCheckNumber(checkNo As String) As Boolean
        Dim existing = FindByCheckNumber(checkNo)
        If existing Is Nothing Then Return False

        _checks.Remove(existing)
        Return True
    End Function

    Public Shared Function GetAll() As List(Of CheckRecord)
        Return _checks.Select(Function(x) New CheckRecord With {
            .CustomerName = x.CustomerName,
            .CheckNumber = x.CheckNumber,
            .Reference = x.Reference,
            .CheckDate = x.CheckDate,
            .PaidInvoices = New List(Of String)(x.PaidInvoices),
            .Amount = x.Amount
        }).ToList()
    End Function

End Class
