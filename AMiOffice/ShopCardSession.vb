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
    ''' Searches ALL bucket subfolders under SHOPCARD\ for a given card number,
    ''' returning every matching file path found.
    ''' This matches DOS TS (Text Search) behavior — it scanned every bucket
    ''' regardless of the INT(n/200) formula, so all historical copies are visible.
    ''' Callers receive all matches; no duplicate alerts are shown to the user.
    ''' </summary>
    Public Function FindCardFiles(cardNum As String) As List(Of String)
        Dim results As New List(Of String)
        Try
            Dim shopcardRoot As String = IO.Path.Combine(DataFolder, "SHOPCARD")
            If Not IO.Directory.Exists(shopcardRoot) Then Return results
            Dim fileName As String = cardNum.Trim() & ".CRD"
            ' Scan every subfolder (buckets 0-N) and the root itself
            For Each folder As String In IO.Directory.GetDirectories(shopcardRoot)
                Dim candidate As String = IO.Path.Combine(folder, fileName)
                If IO.File.Exists(candidate) Then results.Add(candidate)
            Next
            ' Also check directly under SHOPCARD\ root (future flat writes)
            Dim rootCandidate As String = IO.Path.Combine(shopcardRoot, fileName)
            If IO.File.Exists(rootCandidate) Then results.Add(rootCandidate)
        Catch
        End Try
        Return results
    End Function

    ''' <summary>
    ''' Called at application startup. Reads crdnumbr.dat and compares it against
    ''' the actual highest .CRD number present on disk. If the counter is behind
    ''' (e.g. was reset or never advanced past a wrap), it is corrected silently
    ''' so that new cards always append after all existing historical data.
    ''' </summary>
    Public Sub SeedCounterFromDisk()
        Try
            Dim shopcardRoot As String = IO.Path.Combine(DataFolder, "SHOPCARD")
            If Not IO.Directory.Exists(shopcardRoot) Then Return

            ' Find the highest numeric card number anywhere on disk
            Dim highest As Integer = 0
            For Each f As String In IO.Directory.GetFiles(shopcardRoot, "*.CRD", IO.SearchOption.AllDirectories)
                Dim baseName As String = IO.Path.GetFileNameWithoutExtension(f)
                Dim n As Integer
                If Integer.TryParse(baseName, n) AndAlso n > highest Then highest = n
            Next

            If highest = 0 Then Return

            ' Read the current counter
            Dim crdPath As String = IO.Path.Combine(DataFolder, "crdnumbr.dat")
            Dim current As Integer = 0
            If IO.File.Exists(crdPath) Then
                Dim raw As String = IO.File.ReadAllText(crdPath).Trim().Trim(""""c)
                Integer.TryParse(raw, current)
            End If

            ' If counter is behind the highest card on disk, correct it
            If current < highest Then
                IO.File.WriteAllText(crdPath, """" & highest.ToString() & """" & vbCrLf)
            End If
        Catch
            ' Non-fatal — worst case next card number is read from file as-is
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
