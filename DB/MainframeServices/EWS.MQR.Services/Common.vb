Imports EWS.Diagnostics
Imports EWS.MQR.XML

Module Common

   Public Function RemoveIllegalChars(ByVal data As String) As String
      '? [ ] / \ = + < > : ; " , * | 
      Dim Chars() As Char = New Char() {"?"c, "["c, "]"c, "/"c, "\"c, "="c, "+"c, "<"c, ">"c, ":"c, ";"c, """"c, ","c, "*"c, "|"c}
      For Each Item As Char In Chars
         data = data.Replace(Item, "")
      Next
      Return data
   End Function

   Public Function CreateSafeFilePrefix(ByVal FolderName As String) As String
      Return Left(RemoveIllegalChars(FolderName), 100)
   End Function

   Public Function ExtractResponseDate(ByVal Data As String) As Date

      Dim DateString As String = System.Text.RegularExpressions.Regex.Match(Data, "\d{2}/\d{2}/\d{2}\s\d{2}\.\d{2}\.\d{2}").Value
      Dim result As Date
      If Date.TryParseExact(DateString, "dd/MM/yy HH.mm.ss", System.Globalization.DateTimeFormatInfo.InvariantInfo, Globalization.DateTimeStyles.None, result) Then
         Return result
      Else
         Dim Msg As String = "Could not parse the date:" & DateString & ControlChars.NewLine & "Raw data was:" & Data
         EventLogging.Log(Msg, "ExtractResponseDate", EventLogEntryType.Error)
         Throw New UnreconisedResponseException(Msg)
      End If

   End Function

End Module
