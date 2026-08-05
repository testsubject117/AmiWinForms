Option Strict On
Option Explicit On

''' <summary>
''' Carries forward last-used values across shopcards within a session,
''' matching the DOS S.ASC behavior for [ENTER = last value] prompts.
''' </summary>
Friend Module ShopCardSession

    ' Data folder path — matches \\invoice\mainmenu\data on the production machine
    Public Const DataFolder As String = "\\invoice\mainmenu\data"

    ' Last used customer name — shown as [ENTER = PSIBEARI] default
    Public Property LastCustomerName As String = ""

    ' Last used material — shown as (L) carry-forward in material list
    Public Property LastMaterial As String = ""

    ' Last used heat treat — shown as [H = value] hint
    Public Property LastHeatTreat As String = ""

    ' Current printer selection — persists across shopcards
    Public Property UseLaserPrinter As Boolean = False

    ' Error log filename (replaces DOS "FUCKUP.FIL")
    Public Const BadCardLogFile As String = "badcard.log"

    ''' <summary>
    ''' Reads CUSTOMER.NAM from the data folder on startup.
    ''' GW-BASIC WRITE format: "PSIBEARI" (quoted string, one line).
    ''' </summary>
    Public Sub LoadFromDisk()
        Try
            Dim path As String = IO.Path.Combine(DataFolder, "CUSTOMER.NAM")
            If IO.File.Exists(path) Then
                Dim line As String = IO.File.ReadAllText(path).Trim()
                ' Strip GW-BASIC quotes: "PSIBEARI" -> PSIBEARI
                LastCustomerName = line.Trim(""""c)
            End If
        Catch
            ' Non-fatal — session just starts with no carry-forward
        End Try
    End Sub

    ''' <summary>
    ''' Writes CUSTOMER.NAM to the data folder whenever a new customer name is entered.
    ''' Matches DOS: WRITE #1, N2$ which produces "CUSTOMERNAME"
    ''' </summary>
    Public Sub SaveCustomerName(name As String)
        Try
            LastCustomerName = name
            Dim path As String = IO.Path.Combine(DataFolder, "CUSTOMER.NAM")
            IO.File.WriteAllText(path, """" & name & """" & vbCrLf)
        Catch
            ' Non-fatal — carry-forward just won't persist to next session
        End Try
    End Sub

    ''' <summary>
    ''' Returns the printer label string for display on the menu,
    ''' matching DOS: "Current Printer = STAR (Dot Matrix)" or "LASER"
    ''' </summary>
    Public Function PrinterDisplayName() As String
        If UseLaserPrinter Then
            Return "LASER"
        Else
            Return "STAR (Dot Matrix)"
        End If
    End Function

End Module
