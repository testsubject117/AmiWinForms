Option Strict On
Option Explicit On

Imports System
Imports System.Text

Public Class LogBookEntry
    Public Property DateText As String = ""
    Public Property Customer As String = ""
    Public Property PartNumber As String = ""
    Public Property InvoiceNumber As Integer?
    Public Property PONumber As String = ""
    Public Property Spec As String = ""
    Public Property QtyAccepted As String = ""
    Public Property QtyRejected As String = ""
    Public Property Material As String = ""
    Public Property HeatTreat As String = ""
    Public Property ReasonRejected As String = ""
    Public Property Status As String = ""

    ' NEW: where this entry came from (useful when scanning all years)
    Public Property SourceFile As String = ""

    Public Function FormatForDosViewer(Optional includeSource As Boolean = False) As String
        Dim sb As New StringBuilder()

        If includeSource AndAlso Not String.IsNullOrWhiteSpace(SourceFile) Then
            sb.AppendLine($"Source: {SourceFile}")
        End If

        Dim inv As String = If(InvoiceNumber.HasValue, InvoiceNumber.Value.ToString(), "")

        sb.AppendLine($"Customer: {Customer}".PadRight(30) & $"Part#: {PartNumber}".PadRight(28) & $"{DateText}")
        sb.AppendLine($"Invoice#: {inv}".PadRight(30) & $"P.O.#: {PONumber}".PadRight(28) & $"Status: {Status}")
        sb.AppendLine($"Qty Acc/Rec: {QtyAccepted}".PadRight(22) & $"Qty Rejected: {QtyRejected}".PadRight(22) & $"Mat: {Material}")
        sb.AppendLine($"Heat Treat: {HeatTreat}".PadRight(22) & $"Rsn Rej: {ReasonRejected}")
        sb.AppendLine($"Spec: {Spec}")

        Return sb.ToString().TrimEnd()
    End Function
End Class