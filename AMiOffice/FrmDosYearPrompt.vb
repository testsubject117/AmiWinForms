Option Strict On
Option Explicit On

Imports System.Windows.Forms

' This form is no longer used.
' The Log Book year prompt is now done inline on FormMainMenu (DOS-style).
Public Class FrmDosYearPrompt
    Inherits Form

    Public ReadOnly Property YearValue As Integer
        Get
            Return DateTime.Now.Year Mod 100
        End Get
    End Property

    Public Sub New(defaultYear As Integer)
        Me.Text = "LOG BOOK"
        Me.StartPosition = FormStartPosition.CenterParent
    End Sub
End Class