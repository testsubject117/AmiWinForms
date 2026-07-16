' LegacyDataPaths.vb  (Add as Class, NOT a form)
Option Strict On
Option Explicit On

Imports System.IO

Public NotInheritable Class LegacyDataPaths
    Private Sub New()
    End Sub

    ' NOTE: \\invoice resolves via hosts file on production machines.
    ' On dev machine with Tailscale active, use direct IP temporarily.
    ' TODO: Restore to \\invoice\MainMenu\Data before production deployment.
    Public Shared ReadOnly Property BaseDataDir As String = "\\192.168.1.124\MainMenu\Data"
    Public Shared ReadOnly Property WordDocDir As String = Path.Combine(BaseDataDir, "Word")

    ' Ed Dean's Personal Backup destination (Option Y)
    ' DOS used D: drive, modernized to UNC path
    ' Production path: \\192.168.1.176\EdDeanBU
    Public Shared ReadOnly Property PersonalBackupPath As String = "\\192.168.1.176\EdDeanBU"

    Public Shared ReadOnly Property JournalCur As String =
        Path.Combine(BaseDataDir, "JOURNAL.CUR")
    Public Shared ReadOnly Property LedgerCur As String =
        Path.Combine(BaseDataDir, "LEDGER.CUR")
    Public Shared ReadOnly Property CheckInv As String = Path.Combine(BaseDataDir, "CHECK.INV")
    Public Shared ReadOnly Property InvoiceChk As String = Path.Combine(BaseDataDir, "INVOICE.CHK")
    Public Shared ReadOnly Property OtherChk As String = Path.Combine(BaseDataDir, "OTHER.CHK")
    Public Shared ReadOnly Property EmpNameDat As String = Path.Combine(BaseDataDir, "EMPNAME.DAT")
End Class
