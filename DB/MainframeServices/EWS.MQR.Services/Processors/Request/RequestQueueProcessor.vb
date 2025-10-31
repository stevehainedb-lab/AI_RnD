Imports System.Data.SqlClient
Imports EWS.MQR.XML.InstructionSetManager
Imports EWS.MQR.XML
Imports EWS.Diagnostics

Public Class RequestQueueProcessor : Inherits QueueProcessorBase(Of RequestItem)

   Public Sub New(ByVal ProcessorType As String, ByVal Identifier As String)
      MyBase.New(ProcessorType, Identifier)
   End Sub

   Protected Overrides Function Dequeue() As RequestItem
      If Not QueueManager.RequestQueueInstance Is Nothing Then
         Return QueueManager.RequestQueueInstance.Dequeue
      Else
         Return Nothing
      End If
   End Function

   Protected Overrides Sub ProcessItem(ByVal Item As RequestItem)
      Try
         Dim IsCacheReadEnabled As Boolean = MQRConfig.Current.MQRServiceConfig.CacheReadEnabled
         Dim IsStopCacheReadInstructionSet As Boolean = LogonInstructionSetsToUseMFFor.Contains(Item.LogonIdentifier.ToUpper)

         Dim OverrideData As String = GetOverrideData(Item.QueryIdentifier, Item.Parameters)
         If String.IsNullOrWhiteSpace(OverrideData) Then
            Select Case True

               Case Item.LogonIdentifier.ToUpper = "CACHE"
                  If Not ReadFromCache(Item) Then
                     Response(Item, New MQRCacheRetrieveException(Item, " there is no data in the cache for your request!"))
                  End If

               Case Item.CacheTTLSecs > 0
                  If Not ReadFromCache(Item, Item.CacheTTLSecs) Then
                     ReadFromSession(Item)
                  End If

               Case IsCacheReadEnabled And Not IsStopCacheReadInstructionSet
                  If Not ReadFromCache(Item) Then
                     Response(Item, New MQRCacheRetrieveException(Item, " there is no data in the cache for your request and MQR is configured to use Cache!"))
                  End If

               Case (Not IsCacheReadEnabled) And Not IsStopCacheReadInstructionSet
                  Throw New MQRCacheRetrieveException(Item, " cache read is not enabled!")

               Case Else
                  ReadFromSession(Item)

            End Select
         Else
            Dim NewRequiresParsingQueueItem As New RequiresParsingItem(Item.QueueItemData, OverrideData, Item.Metrics)
            QueueManager.RequiresParsingQueueInstance.Enqueue(NewRequiresParsingQueueItem)
         End If

      Catch ex As Exception
         Response(Item, ex)
      End Try

   End Sub

   Private Sub ReadFromSession(Item As RequestItem)

      Dim NewAwaitingOutputItem As AwaitingOutputItem = Nothing
      Dim Session As SessionInstance = Nothing


      'this will mark the session as locked
      Session = SessionManager.GetFreeSessionForQuery(Item)

      Try
         Dim NoResponseData As String = String.Empty
         Dim WaitForResponse As Boolean = False
         Session.DoQuery(Item, WaitForResponse, NoResponseData)

         If WaitForResponse Then
            If Item.ExpectResponse Then
               NewAwaitingOutputItem = New AwaitingOutputItem(Item, Session.TOPSAltName)
               QueueManager.AwaitingOutputQueueInstance.Enqueue(NewAwaitingOutputItem)
            Else
               EventLogging.Log("About to submit a query but am not expecting a respone!", Me.GetType.Name)
            End If
         Else
            Dim NoDataField As New QueryResultField
            NoDataField.Identifier = "NoDataReason"
            NoDataField.Value = NoResponseData
            Dim NoDataRow As New QueryResultRow
            NoDataRow.Identifier = "NoDataReason"
            NoDataRow.Fields.Add(NoDataField)

            For Each Key As String In Session.CollectedDataFromQuery.Keys
               Dim Field As New QueryResultField
               Field.Identifier = Key
               Field.Value = Session.CollectedDataFromQuery(Key)
               NoDataRow.Fields.Add(Field)
            Next

            Dim Section As New QueryResultSection
            Section.Identifier = "NoData"
            Section.AddRow(NoDataRow)

            Dim Result As New QueryResult
            Result.Sections.Add(Section)

            Response(Item, Result)
         End If

      Catch ex As Exception
         EventLogging.Log("EXCEPTION whilest running query:" & Item.QueryIdentifier & " on Session " & Session.ToString & ControlChars.NewLine & " Exception was: " & vbCrLf & ex.ToString, Me.GetType.Name, EventLogEntryType.Warning)
         QueueManager.EmptyQueuesForSession(Session.TOPSAltName, "")
         Throw
      End Try
   End Sub

   Private Function ReadFromCache(Item As RequestItem) As Boolean

      Dim CachedData As String = GetCachedData(Item.QueryIdentifier, Item.Parameters, Item.AsAt)
      If String.IsNullOrWhiteSpace(CachedData) Then
         Return False
      Else
         Dim NewRequiresParsingQueueItem As New RequiresParsingItem(Item.QueueItemData, CachedData, Item.Metrics)
         QueueManager.RequiresParsingQueueInstance.Enqueue(NewRequiresParsingQueueItem)
         Return True
      End If
   End Function

   Private Function ReadFromCache(Item As RequestItem, CacheTTLSecs As Integer) As Boolean

      Dim CachedData As String = GetCachedData(Item.QueryIdentifier, Item.Parameters, Item.AsAt, Now.AddSeconds(0 - CacheTTLSecs))
      If String.IsNullOrWhiteSpace(CachedData) Then
         Return False
      Else
         Dim NewRequiresParsingQueueItem As New RequiresParsingItem(Item.QueueItemData, CachedData, Item.Metrics)
         QueueManager.RequiresParsingQueueInstance.Enqueue(NewRequiresParsingQueueItem)
         Return True
      End If
   End Function

   Private Function GetCachedData(ByVal Query As String, ByVal Parameters As QueryRequestParameters, ByVal AsAt As Date, ByVal MinAge As Date) As String
      Dim Result As String = String.Empty

      Try
         Dim Connection As New SqlConnection(MQRConfig.Current.MQRServiceConfig.CacheConnectionString)
         Dim Command As New SqlCommand()
         With Command
            .Connection = Connection
            .CommandType = CommandType.StoredProcedure
            .CommandText = "SP_GetDataCacheAfter"
            .Parameters.Add(.CreateParameter)
            With .Parameters(.Parameters.Count - 1)
               .Direction = ParameterDirection.Input
               .ParameterName = "@Query"
               .SqlDbType = SqlDbType.NVarChar
               .SqlValue = Query
            End With

            .Parameters.Add(.CreateParameter)
            With .Parameters(.Parameters.Count - 1)
               .Direction = ParameterDirection.Input
               .ParameterName = "@Parameters"
               .SqlDbType = SqlDbType.NVarChar
               If Parameters.CommandID = String.Empty Then
                  .SqlValue = Parameters.ToString
               Else
                  .SqlValue = Parameters.CommandID
               End If
            End With

            .Parameters.Add(.CreateParameter)
            With .Parameters(.Parameters.Count - 1)
               .Direction = ParameterDirection.Input
               .ParameterName = "@AsAt"
               .SqlDbType = SqlDbType.DateTime
               .SqlValue = AsAt
            End With

            .Parameters.Add(.CreateParameter)
            With .Parameters(.Parameters.Count - 1)
               .Direction = ParameterDirection.Input
               .ParameterName = "@MinAge"
               .SqlDbType = SqlDbType.DateTime
               .SqlValue = MinAge
            End With

         End With

         Try

            With Connection
               .Open()

               With Command
                  Try
                     Dim Data As SqlClient.SqlDataReader = .ExecuteReader(CommandBehavior.SingleRow)
                     With Data
                        Try
                           If .Read Then
                              Result = .GetString(0)
                           End If
                        Finally
                           .Close()
                        End Try
                     End With

                  Catch ex As Exception
                     Trace.WriteLine(String.Format("GetCachedData - {0}", ex.Message), TraceLevel.Error)
                  Finally
                     .Dispose()
                  End Try
               End With
            End With
         Catch ex As Exception
            EventLogging.Log(ex.ToString, "MQRMetrics", EventLogEntryType.Error)
         Finally
            With Connection
               .Close()
               .Dispose()
            End With
         End Try

      Catch ex As Exception
         EventLogging.Log(ex.ToString, Me.GetType.Name, EventLogEntryType.Error)
      End Try

      Return Result

   End Function

   Private Function GetCachedData(ByVal Query As String, ByVal Parameters As QueryRequestParameters, ByVal AsAt As Date) As String
      Dim Result As String = String.Empty

      Try
         Dim Connection As New SqlConnection(MQRConfig.Current.MQRServiceConfig.CacheConnectionString)
         Dim Command As New SqlCommand()
         With Command
            .Connection = Connection
            .CommandType = CommandType.StoredProcedure
            .CommandText = "SP_GetDataCache"
            .Parameters.Add(.CreateParameter)
            With .Parameters(.Parameters.Count - 1)
               .Direction = ParameterDirection.Input
               .ParameterName = "@Query"
               .SqlDbType = SqlDbType.NVarChar
               .SqlValue = Query
            End With

            .Parameters.Add(.CreateParameter)
            With .Parameters(.Parameters.Count - 1)
               .Direction = ParameterDirection.Input
               .ParameterName = "@Parameters"
               .SqlDbType = SqlDbType.NVarChar
               If Parameters.CommandID = String.Empty Then
                  .SqlValue = Parameters.ToString
               Else
                  .SqlValue = Parameters.CommandID
               End If
            End With

            .Parameters.Add(.CreateParameter)
            With .Parameters(.Parameters.Count - 1)
               .Direction = ParameterDirection.Input
               .ParameterName = "@AsAt"
               .SqlDbType = SqlDbType.DateTime
               .SqlValue = AsAt
            End With


         End With

         Try

            With Connection
               .Open()

               With Command
                  Try
                     Dim Data As SqlClient.SqlDataReader = .ExecuteReader(CommandBehavior.SingleRow)
                     With Data
                        Try
                           If .Read Then
                              Result = .GetString(0)
                           End If
                        Finally
                           .Close()
                        End Try
                     End With

                  Catch ex As Exception
                     Trace.WriteLine(String.Format("GetCachedData - {0}", ex.Message), TraceLevel.Error)
                  Finally
                     .Dispose()
                  End Try
               End With
            End With
         Catch ex As Exception
            EventLogging.Log(ex.ToString, "MQRMetrics", EventLogEntryType.Error)
         Finally
            With Connection
               .Close()
               .Dispose()
            End With
         End Try

      Catch ex As Exception
         EventLogging.Log(ex.ToString, Me.GetType.Name, EventLogEntryType.Error)
      End Try

      Return Result

   End Function


   Private Function GetOverrideData(ByVal Query As String, ByVal Parameters As QueryRequestParameters) As String
      Dim Result As String = String.Empty

      Try

         Dim OverrideFileName As String
         Select Case True
            Case Not Parameters.CommandID = String.Empty
               OverrideFileName = FileOverridePath & RemoveIllegalChars(Query) & "\" & RemoveIllegalChars(Parameters.CommandID) & ".txt"
            Case Not Parameters.ToString = String.Empty
               OverrideFileName = FileOverridePath & RemoveIllegalChars(Query) & "\" & RemoveIllegalChars(Parameters.ToString) & ".txt"
            Case Else
               OverrideFileName = FileOverridePath & RemoveIllegalChars(Query) & "\ScreenData.txt"
         End Select

         If OverrideFileName.Length <= 200 Then
            If IO.File.Exists(OverrideFileName) Then
               Result = MQRConfig.ReadFile(New IO.FileInfo(OverrideFileName))
            End If
         Else
            Trace.WriteLine("File name too long for override data: " & OverrideFileName, TraceLevel.Info)
         End If

      Catch ex As Exception
         EventLogging.Log(ex.ToString & ControlChars.NewLine & "Query was: " & Query & " Paramters are: " & Parameters.ToString, Me.GetType.Name, EventLogEntryType.Error)
      End Try

      Return Result

   End Function

   Private ReadOnly Property FileOverridePath() As String
      Get
         Dim result As String = Replace(MQRConfig.Current.MQRServiceConfig.FileOverridePath, "EXEPath", EXEPath)

         If result.EndsWith("\") Then
            Return result
         Else
            Return result & "\"
         End If

      End Get
   End Property

End Class
