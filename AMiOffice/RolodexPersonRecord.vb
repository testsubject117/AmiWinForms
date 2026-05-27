Public Class RolodexPersonRecord
    Public Property PersonName As String = ""
    Public Property Street As String = ""
    Public Property City As String = ""
    Public Property StateCode As String = ""
    Public Property ZipCode As String = ""
    Public Property AreaCode As String = ""
    Public Property PhoneNumber As String = ""
    Public Property Misc As String = ""
    Public Property IsCustomer As Boolean = False

    Public Function GetDisplayLines() As List(Of String)
        Return New List(Of String) From {
            PersonName,
            Street,
            $"{City} , {StateCode}   {ZipCode}",
            $"({AreaCode}) {FormatPhone(PhoneNumber)}",
            Misc
        }
    End Function

    Public Shared Function FormatPhone(raw As String) As String
        Dim digits = New String((If(raw, "")).Where(Function(c) Char.IsDigit(c)).ToArray())

        If digits.Length = 7 Then
            Return $"{digits.Substring(0, 3)}-{digits.Substring(3, 4)}"
        End If

        Return raw
    End Function
End Class
