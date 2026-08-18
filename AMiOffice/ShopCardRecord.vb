Option Strict On
Option Explicit On

''' <summary>
''' Holds all data for a single ShopCard record, mirroring the DOS S.ASC data layout.
''' Header fields + 8 section arrays (A$(section, field)) + serial numbers.
''' </summary>
Public Class ShopCardRecord

    ' --- Header fields ---
    Public Property CustomerName As String = ""
    Public Property EntryDate As String = ""          ' MM-DD-YYYY string, as DOS stored it
    Public Property PONumber As String = ""
    Public Property NumberOfPans As String = ""        ' blank = none
    Public Property NumberOfBoxes As String = ""       ' blank = none (only when weight = -1)
    Public Property NumberOfCrates As String = ""      ' blank = none (only when boxes entered)
    Public Property Weight As String = ""              ' blank = none (-2 entered)
    Public Property QuantityReceived As String = ""
    Public Property PartNumberAndName As String = ""   ' combined "777 TESTPART1 99" style
    Public Property JobRouteNumber As String = ""
    Public Property Material As String = ""
    Public Property HeatTreat As String = ""
    Public Property ConditionReceived As String = ""   ' REMmed out in DOS but field exists
    Public Property HotRush As String = ""             ' "YES" or "NO"
    Public Property HandleWithCare As String = ""      ' "YES" auto-set

    ' --- Section arrays: A(section 1-8, field 1-19) ---
    ' Indexed 1-based to match S.ASC A$(X,Y) directly
    Private _sectionData(8, 19) As String

    Public Function GetSection(section As Integer, field As Integer) As String
        If section < 1 OrElse section > 8 Then Return ""
        If field < 1 OrElse field > 19 Then Return ""
        Return If(_sectionData(section, field), "")
    End Function

    Public Sub SetSection(section As Integer, field As Integer, value As String)
        If section < 1 OrElse section > 8 Then Return
        If field < 1 OrElse field > 19 Then Return
        _sectionData(section, field) = If(value, "")
    End Sub

    Public Sub ClearSection(section As Integer, field As Integer)
        SetSection(section, field, "")
    End Sub

    ' --- Serial numbers / invoice body text ---
    Public Property SerialNumbers As String = ""

    ' --- Shopcard number (assigned on save) ---
    Public Property CardNumber As String = ""

    ' --- Inspection type (set after save, before print) ---
    ' 1=In Process, 2=In Service, 3=Final, 4=I'm Not Sure
    Public Property InspectionType As Integer = 0

    ' --- Printer type carry-forward ---
    Public Property UseLaserPrinter As Boolean = False  ' False = Star Dot Matrix

    ''' <summary>
    ''' Returns True if at least one section field has a value (required before saving).
    ''' </summary>
    Public Function HasAnyProcedure() As Boolean
        For s As Integer = 1 To 8
            For f As Integer = 1 To 19
                If _sectionData(s, f) <> "" Then Return True
            Next
        Next
        Return False
    End Function

    ''' <summary>Copies all section data from another record into this one.</summary>
    Public Sub CopySectionsFrom(source As ShopCardRecord)
        If source Is Nothing Then Return
        For s As Integer = 1 To 8
            For f As Integer = 1 To 19
                _sectionData(s, f) = source.GetSection(s, f)
            Next
        Next
    End Sub

End Class
