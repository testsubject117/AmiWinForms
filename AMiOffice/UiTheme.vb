Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

Friend Module UiTheme

    Friend ReadOnly Property DosTitleForeColor As Color
        Get
            Return Color.Yellow
        End Get
    End Property

    Friend ReadOnly Property DosTitleBackColor As Color
        Get
            Return Color.Black
        End Get
    End Property

    Friend Function CreateDosTitleFont() As Font
        Return New Font("Castellar", 36.0F, FontStyle.Bold, GraphicsUnit.Point)
    End Function

    Friend Sub ApplyDosTitleStyle(lbl As Label)
        If lbl Is Nothing Then Return

        lbl.ForeColor = DosTitleForeColor
        lbl.BackColor = DosTitleBackColor
        lbl.Font = CreateDosTitleFont()
        lbl.TextAlign = ContentAlignment.MiddleLeft
    End Sub

End Module