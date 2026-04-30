Option Strict On
Option Explicit On

Friend Module BuildInfo
    Friend Const ProductVersion As String = "1.0.0"
    Friend Const BuildNumber As String = "28"

    Friend ReadOnly Property DisplayVersion As String
        Get
            Return "AMiOffice v" & ProductVersion & " (Build " & BuildNumber & ")"
        End Get
    End Property
End Module
