Imports System.Text
Imports EWS.Diagnostics
Imports System.Text.RegularExpressions
Imports EWS.MQR.XML

Public Class ParseQueueProcessor : Inherits QueueProcessorBase(Of RequiresParsingItem)

   Public Sub New(ByVal ProcessorType As String, ByVal Identifier As String)
      MyBase.New(ProcessorType, Identifier)
   End Sub

   Protected Overrides Function Dequeue() As RequiresParsingItem
      If Not QueueManager.RequiresParsingQueueInstance Is Nothing Then
         Return QueueManager.RequiresParsingQueueInstance.Dequeue
      Else
         Return Nothing
      End If
   End Function

   Protected Overrides Sub ProcessItem(ByVal Item As RequiresParsingItem)
      Try

         If Item.MFTimeResponseRecieved = Date.MinValue Then
            Item.RecievedResponse(ExtractResponseDate(Item.RawData))
         End If

         Dim Result As QueryResult = Parse(Item)
         Response(Item, Result)
      Catch ex As Exception
         Response(Item, ex)
      End Try
   End Sub

   Private Shared m_ScreenCatureRegEx As New Regex("<(?<key>ScreenCaptureValue\w*)>(?<value>.*)</\k<key>>")
   Private Shared Function ProcessQueryExtractedData(ByVal Item As RequiresParsingItem, ByVal Result As QueryResult) As String

      Dim SectionRow As QueryResultRow = Nothing
      For Each MatchItem As Match In m_ScreenCatureRegEx.Matches(Item.RawData)
         If SectionRow Is Nothing Then
            Dim Section As New QueryResultSection
            Section.Identifier = "QueryScreenCapture"
            Result.Sections.Add(Section)

            SectionRow = New QueryResultRow
            SectionRow.Identifier = "Values"
            Section.AddRow(SectionRow)
         End If

         Dim Key As String = MatchItem.Groups("key").Value.Substring(Len("ScreenCaptureValue"))
         Dim Value As String = MatchItem.Groups("value").Value

         Dim Field As New QueryResultField
         Field.Identifier = Key
         Field.Value = Value

         SectionRow.Fields.Add(Field)
      Next

      If Not SectionRow Is Nothing Then
         Return m_ScreenCatureRegEx.Replace(Item.RawData, "")
      Else
         Return Item.RawData
      End If

   End Function

   Private Shared Sub ParsePrintResponse(ByVal ParseInstructions As ParseInstructionSet, ByVal RequestSessionIdentifier As String, ByVal PrintResponse As String, ByRef result As QueryResult)

      Dim Data As String() = Regex.Split(PrintResponse, LINE_SPLIT_REGEX_PATTERN)
      Trace.WriteLine(Data.Length & " Row(s) to parse via " & ParseInstructions.Identifier, TraceLevel.Verbose)

      Dim DefaultOption As ParseOption = Nothing
      Dim ParseOptionToUse As ParseOption = Nothing

      For Each OptionItem As ParseOption In ParseInstructions.Options
         If OptionItem.ParseOptionIdentifiers.Count > 0 Then
            If OptionItem.IsMatch(PrintResponse) Then
               If ParseOptionToUse Is Nothing Then
                  ParseOptionToUse = OptionItem
                  Exit For
               Else
                  Throw New InvalidInstructionSetSyntaxException(ParseInstructions.Identifier & " InstructionSet has more than one ParseOption that maches.")
               End If
            End If
         Else
            If DefaultOption Is Nothing Then
               DefaultOption = OptionItem
            Else
               Throw New InvalidInstructionSetSyntaxException(ParseInstructions.Identifier & " InstructionSet has more than one Option with no OptionIdentificationMarks.")
            End If
         End If
      Next

      If ParseOptionToUse Is Nothing Then
         If DefaultOption Is Nothing Then
            Throw New InvalidInstructionSetSyntaxException(ParseInstructions.Identifier & " InstructionSet has not got a matching ParseOption.")
         Else
            ParseOptionToUse = DefaultOption
         End If
      End If

      Dim DataCategory As ParseCategory = Nothing
      Dim ResultSection As QueryResultSection = Nothing
      Dim ValidateRows As Boolean = Not ParseInstructions.ValidationCountIdentificationMark.RegexPattern.Value = String.Empty
      Dim ValidationCount As Integer = -1
      Dim ItemCount As Integer

      For LineCount As Integer = 1 To Data.Length

         Dim Row As String = Data(LineCount - 1)

         If Not Row = String.Empty Then

            If (DataCategory Is Nothing) OrElse (Not DataCategory.IsMatch(Row)) Then
               'this will except and fall out if error condition is found
               DataCategory = FindCorrectCategory(Row, ParseOptionToUse.ParseCategories)
               If Not DataCategory Is Nothing Then
                  If result.Sections.ContainsKey(DataCategory.Identifier) Then
                     ResultSection = result.Sections(DataCategory.Identifier)
                  Else
                     ResultSection = New QueryResultSection
                     ResultSection.Identifier = DataCategory.Identifier
                     result.Sections.Add(ResultSection.Identifier, ResultSection)
                  End If
               End If
            End If

            If DataCategory Is Nothing Then
               If ValidateRows Then
                  If ParseInstructions.ValidationCountIdentificationMark.IsMatch(Row) Then
                     ValidationCount = CInt(ParseInstructions.ValidationCountIdentificationMark.GetValue(Row))
                  End If
               End If

               If ParseInstructions.IsEndOfData(Row) Then
                  Exit For
               End If

            Else
               Dim ResultRow As New QueryResultRow
               ResultRow.Identifier = DataCategory.BuildKey(Row, ResultSection.Rows.Count + 1, LineCount)
               If ParseInstructions.FullLineCaptue Then
                  ResultRow.FullLine = Row
               End If

               If Not ResultSection.Rows.ContainsKey(ResultRow.Identifier) Then

                  Dim SearchRows(DataCategory.SubRowSearchLineCount) As String
                  SearchRows(0) = Row
                  For count As Integer = 1 To DataCategory.SubRowSearchLineCount
                     If Data.Length >= (LineCount + count) Then
                        SearchRows(count) = Data((LineCount - 1) + count)
                     Else
                        SearchRows(count) = ""
                     End If
                  Next

                  PopulateFields(ResultRow.Fields, DataCategory, SearchRows)
                  ResultSection.AddRow(ResultRow)
                  ItemCount += 1
               Else
                  If ParseInstructions.IncludeDuplicatesForValidation Then
                     ItemCount += 1
                  End If
               End If
               End If
         End If
      Next

      If ValidateRows Then
         If ValidationCount = -1 Then
            Throw New ValidationCountException("Could not find count row for validation")
         Else
            If Not ItemCount = ValidationCount Then
               Throw New ValidationCountException("Validation count does not match. Found " & ItemCount.ToString & " Items, Validation is " & ValidationCount.ToString & ". From " & RequestSessionIdentifier & " using " & ParseInstructions.Identifier & " parse InstructionSet!")
            End If
         End If
      End If


   End Sub

   'Const LINE_SPLIT_REGEX_PATTERN As String = "[\r\n|\f]"
   Const LINE_SPLIT_REGEX_PATTERN As String = "\r\n|\r|\n|$|\f"

   Private Shared Function Parse(ByVal Item As RequiresParsingItem) As QueryResult

      Dim ParseInstructions As ParseInstructionSet = InstructionSetManager.GetParseInstructionset(Item.ParseIdentifier)

      If Item.RawData.Length = 0 Then
         Throw New NoDataException("Parse Instructionset: " & ParseInstructions.Identifier & " returned NoData! RawData Length was " & Item.RawData.Length)
      End If

      ExceptIfErrorContitionMet(ParseInstructions, Item)
      ExceptIfEndNotFound(ParseInstructions, Item)

      Dim Result As New QueryResult(Item.QueueItemData.MFTimeResponseRecieved)

      Dim ParseableData As String = ProcessQueryExtractedData(Item, Result)

      If ParseInstructions.CaptureRaw Then
         Dim Data As String() = Regex.Split(ParseableData, LINE_SPLIT_REGEX_PATTERN)
         Trace.WriteLine(Data.Length & " Row(s) to parse raw", TraceLevel.Verbose)

         Dim RawResultSection As New QueryResultSection
         RawResultSection.Identifier = "RawResult"
         Result.Sections.Add(RawResultSection.Identifier, RawResultSection)
         For LineCount As Integer = 1 To Data.Length
            Dim ResultRow As New QueryResultRow
            With ResultRow
               .FullLine = Data(LineCount - 1)
               .Identifier = LineCount.ToString
            End With
            RawResultSection.AddRow(ResultRow)
         Next
      Else

         Dim PrintResponses As String() = Regex.Split(ParseableData, MQRConfig.Current.MQRServiceConfig.ParseSplitPrintRegEx)

         For Each PrintResponse As String In PrintResponses
            If Not PrintResponse.Trim = String.Empty Then
               ParsePrintResponse(ParseInstructions, Item.RequestSessionIdentifier, PrintResponse, Result)
            End If
         Next

      End If

      WriteParseOutput(Item, Result)

      Return Result
   End Function

   Private Shared Sub PopulateFields(ByRef Fields As QueryResultFields, ByVal DataCategory As ParseCategory, ByVal Data() As String)

      AddFieldsFromDefintions(Fields, DataCategory.FieldDefinitions, Data(0))

      For Each SubRowIdentification As SubRowIdentification In DataCategory.SubRowIdentificationMarks
         For Count As Integer = 1 To Data.Length - 1
            If SubRowIdentification.IsMatch(Data(Count)) Then
               AddFieldsFromDefintions(Fields, SubRowIdentification.FieldDefinitions, Data(Count))
            End If
         Next
      Next

   End Sub

   Private Shared Sub AddFieldsFromDefintions(ByRef Fields As QueryResultFields, ByVal Defintions As ParseTextIdentifications, ByVal Data As String)
      For Each FieldDefintion As ParseTextIdentification In Defintions
         Dim NewField As New QueryResultField
         NewField.Identifier = FieldDefintion.Identifier
         NewField.Value = FieldDefintion.GetValue(Data)
         Fields.Add(NewField.Key, NewField)
      Next
   End Sub

   Private Shared Function FindCorrectCategory(ByVal Data As String, ByVal DataCategories As ParseCategories) As ParseCategory
      Dim Result As ParseCategory = Nothing

      For Each Category As ParseCategory In DataCategories
         If Category.IsMatch(Data) Then
            Result = Category
            Exit For
         End If
      Next

      Return Result
   End Function

   Private Shared Sub WriteParseOutput(ByVal Item As RequiresParsingItem, ByVal Result As QueryResult)

      Dim Data As New StringBuilder
      Data.Append(vbNewLine)
      Data.Append(vbNewLine)
      Data.Append("---- REQUEST ----")
      Data.Append(vbNewLine)
      Data.Append(Item.QueueItemData.ToString)
      Data.Append(vbNewLine)
      Data.Append("TimeTaken: " & Item.QueueItemData.QueryTime.ToString)
      Data.Append(vbNewLine)
      Data.Append("".PadRight(20, "-"c))
      Data.Append(vbNewLine)
      Data.Append(vbNewLine)
      Data.Append(vbNewLine)
      Data.Append("---- RAWDATA ----")
      Data.Append(vbNewLine)
      Data.Append(Item.RawData)
      Data.Append(vbNewLine)
      Data.Append("".PadRight(10, "-"c))
      Data.Append(vbNewLine)
      Data.Append(vbNewLine)
      Data.Append(vbNewLine)
      Data.Append("---- QueryResult ----")
      Data.Append(vbNewLine)
      Data.Append(Result.ToXml(True))
      Data.Append(vbNewLine)
      Data.Append("".PadRight(10, "-"c))

      FileWriter.Write("ParseOutput", Data.ToString, Item.QueueItemData.QueryIdentifier, CreateSafeFilePrefix(Item.QueueItemData.QueryParameters.Replace(" ", "")) & "_")

   End Sub


   Public Shared Sub ExceptIfErrorContitionMet(ByVal InstructionSet As ParseInstructionSet, ByVal Item As RequiresParsingItem)

      Dim Data As String = Item.RawData

      For Each ErrorCondition As ErrorIdentificationMark In InstructionSet.ErrorIdentificationMarks
         If ErrorCondition.IsMatch(Data) Then
            If ErrorCondition.MakePoolUnavailable Then
               Dim UnavailableUntillutc As Date = Date.UtcNow.AddSeconds(ErrorCondition.PoolUnavailablePeriod)
               SessionManager.MakePoolsUnavailable(UnavailableUntillutc, "Error Identification Mark " & ErrorCondition.Identifier & " Found")
            End If
            Throw New ErrorConditionMetException(ErrorCondition.Identifier & " Has been found in: " & Data & vbCrLf & "Parse Instruction Set: " & InstructionSet.Identifier)
         End If
      Next

   End Sub

   Public Shared Sub ExceptIfEndNotFound(ByVal InstructionSet As ParseInstructionSet, ByVal Item As RequiresParsingItem)

      Dim Data As String = Item.RawData

      For Each EndOfDataIdentification As ScreenCaptureDataPoint In InstructionSet.EndOfDataIdentificationMarks
         If EndOfDataIdentification.ExceptIfNotFound AndAlso Not EndOfDataIdentification.IsMatch(Data) Then
            Throw New EndOfDataNotFoundException(EndOfDataIdentification.Identifier & " Has not been found in: " & Data & vbCrLf & "Parse Instruction Set: " & InstructionSet.Identifier)
         End If
      Next

   End Sub

End Class

