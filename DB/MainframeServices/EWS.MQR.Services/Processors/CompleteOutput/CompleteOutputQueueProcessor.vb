Imports System.Collections.Specialized
Imports EWS.MQR.XML
Imports EWS.Diagnostics
Imports EWS.MQR.Services.MQRManager


Public Class CompleteOutputQueueProcessor : Inherits QueueProcessorBase(Of CompleteOutputItem)

   Public Sub New(ByVal ProcessorType As String, ByVal Identifier As String)
      MyBase.New(ProcessorType, Identifier)
   End Sub

   Protected Overrides Function Dequeue() As CompleteOutputItem
      If Not QueueManager.CompleteOutputQueueInstance Is Nothing Then
         Return QueueManager.CompleteOutputQueueInstance.Dequeue
      Else
         Return Nothing
      End If
   End Function

   Protected Overrides Sub ProcessItem(ByVal Item As CompleteOutputItem)

      'Find the Item off the Awaiting out output Queue
      Dim AwaitingOutputQueueItem As AwaitingOutputItem = QueueManager.AwaitingOutputQueueInstance.Dequeue(Item.TOPSAltName)

      If Not AwaitingOutputQueueItem Is Nothing Then
         AwaitingOutputQueueItem.MatchedItem(Item)

         Dim TimeTaken As TimeSpan = AwaitingOutputQueueItem.QueryTime
         Dim TimeOut As TimeSpan = AwaitingOutputQueueItem.TimeOutPeriod(QueueManager.AwaitingOutputQueueInstance.QueueTimeOutPeriod)

         Select Case True
            Case TimeTaken > TimeOut
               EventLogging.Log("Recieved a response but time taken exceeded timeout. TimeTaken:" & TimeTaken.ToString & " Timeout " & TimeOut.ToString, Me.GetType.Name, EventLogEntryType.Warning)
               Response(AwaitingOutputQueueItem, New MQRRetrieveTimeoutException("Response Time is not within the required parameter of " & TimeOut.TotalSeconds & " Seconds!"))

            Case TimeTaken.TotalMilliseconds < 0 - MQRConfig.Current.MQRServiceConfig.MaxTimeshiftMilliseconds
               Dim Msg As String = "Complete Output Item found but could not find a request for the correct time." & ControlChars.NewLine
               Msg += "Query: " & AwaitingOutputQueueItem.QueueItemData.QueryIdentifier & " " & AwaitingOutputQueueItem.QueueItemData.QueryParameters.ToString & ControlChars.NewLine
               Msg += "ID: " & AwaitingOutputQueueItem.Key & ControlChars.NewLine
               Msg += "Response TOP Line was: " & Item.ResponseData.Substring(0, 50) & ControlChars.NewLine
               Msg += "Response TOPSAltName was: " & Item.TOPSAltName & ControlChars.NewLine
               Msg += "Server to MF Timeshift was: " & SessionManager.ServerToMFTimeDiff.ToString & ControlChars.NewLine
               Msg += "MF Time Submitted (inc Timeshift) : " & AwaitingOutputQueueItem.MFTimeQuerySubmitted.ToString("O") & ControlChars.NewLine
               Msg += "MF Time Response                  : " & AwaitingOutputQueueItem.MFTimeResponseRecieved.ToString("O") & ControlChars.NewLine
               Msg += "Time Taken was: " & AwaitingOutputQueueItem.QueryTime.ToString() & ControlChars.NewLine
               Msg += "Allowed Negative Milliseconds Time Taken is: " & MQRConfig.Current.MQRServiceConfig.MaxTimeshiftMilliseconds & ControlChars.NewLine
               Msg += ControlChars.NewLine
               Msg += "This is most likley cased by an old request that faulted. The data will be ignored"

               'Ignore this message, put the item back on the awaiting item queue and log the fact we found it
               QueueManager.AwaitingOutputQueueInstance.Enqueue(AwaitingOutputQueueItem)
               EventLogging.Log(Msg, Me.GetType.Name, EventLogEntryType.Warning)

            Case Else
               Try
                  If InstructionSetManager.GetQueryInstructionSet(AwaitingOutputQueueItem.QueryIdentifier).PrintOutputs = AwaitingOutputQueueItem.PrintOutputsRecieved Then
                     Dim QueryCapturedData As Dictionary(Of String, String) = SessionManager.ReceivedSessionResponse(Item.TOPSAltName, AwaitingOutputQueueItem.CollectedData)

                     If QueryCapturedData.Count > 0 Then
                        AwaitingOutputQueueItem.SetQueryCollectedData(EncodeQueryData(QueryCapturedData))
                     End If

                     Dim NewRequiresParsingQueueItem As New RequiresParsingItem(AwaitingOutputQueueItem.QueueItemData, AwaitingOutputQueueItem.CollectedData, AwaitingOutputQueueItem.Metrics)
                     QueueManager.RequiresParsingQueueInstance.Enqueue(NewRequiresParsingQueueItem)

                     If MQRConfig.Current.MQRServiceConfig.CacheWriteEnabled Then
                        Dim NewCacheWriterItem As New CacheWriterItem(AwaitingOutputQueueItem.QueueItemData, AwaitingOutputQueueItem.Request, AwaitingOutputQueueItem.CollectedData, AwaitingOutputQueueItem.Metrics)
                        QueueManager.CacheWriterQueueInstance.Enqueue(NewCacheWriterItem)
                     End If
                  Else
                     QueueManager.AwaitingOutputQueueInstance.Enqueue(AwaitingOutputQueueItem)
                  End If

               Catch ex As Exception
                  Response(AwaitingOutputQueueItem, ex)
               End Try

         End Select
      Else

         ' Put it back in the Queue we have not got the request item back 
         ' i.e. this must have been a quick resposne! or a multile response and we are processing the other
         ' We should pick it up next time. If not it will eventually expire anyway.
         QueueManager.CompleteOutputQueueInstance.Enqueue(Item)

      End If

   End Sub

   Private Function EncodeQueryData(ByVal Items As Dictionary(Of String, String)) As String
      Dim result As New Text.StringBuilder

      '<ScreenCaptureValueTEST>VALUE</ScreenCaptureValueTEST>
      result.AppendLine()
      For Each key As String In Items.Keys
         If key.ToLower = "QueryScreen".ToLower Then
            result.Insert(0, ControlChars.NewLine & ControlChars.NewLine & Items(key) & ControlChars.NewLine)
         Else
            result.AppendLine(String.Format("<ScreenCaptureValue{0}>{1}</ScreenCaptureValue{0}>", key, Items(key)))
         End If
      Next

      Return result.ToString
   End Function

End Class

