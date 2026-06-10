' LegacyDataPaths.vb  (Add as Class, NOT a form)
Option Strict On
Option Explicit On

Imports System.IO

Public NotInheritable Class LegacyDataPaths
    Private Sub New()
    End Sub

    Public Shared ReadOnly Property BaseDataDir As String = "\\invoice\MainMenu\Data"
    Public Shared ReadOnly Property WordDocDir As String = Path.Combine(BaseDataDir, "Word")

    ' Ed Dean's Personal Backup destination (Option Y)
    ' DOS used D: drive, modernized to UNC path
    Public Shared ReadOnly Property PersonalBackupPath As String = "\\192.168.1.3\Shared\EdDeanBU"

    Public Shared ReadOnly Property LedgerCur As String =
        Path.Combine(BaseDataDir, "LEDGER.CUR")
    Public Shared ReadOnly Property CheckInv As String = Path.Combine(BaseDataDir, "CHECK.INV")
    Public Shared ReadOnly Property InvoiceChk As String = Path.Combine(BaseDataDir, "INVOICE.CHK")
    Public Shared ReadOnly Property OtherChk As String = Path.Combine(BaseDataDir, "OTHER.CHK")
    Public Shared ReadOnly Property EmpNameDat As String = Path.Combine(BaseDataDir, "EMPNAME.DAT")
End Class