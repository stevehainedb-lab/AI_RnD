Imports EWS.Diagnostics
Imports EWS.MQR.XML

Public Class RequestService

   Private Shared m_SyncronisedOutstandingRequests As New SyncLockDictionaryQueue(Of Guid, Object)
   Private Shared m_RequestsRecieved As Long
   Private Shared m_GoodResponses As Long
   Private Shared m_TimedOutResponses As Long
   Private Shared m_ErrorResponses As Long

   Shared Sub New()

   End Sub

   Public Shared Sub Initalise(ByVal Address As String)
      With NotificationManager.Instance("Comms", "WCFRequest")
         .UpdateState(Address)
         .SetDelegate("OutstandingRequests", New GetNotifyDataValue(AddressOf OutstandingRequestCount))
         .SetDelegate("RequestsRecieved", New GetNotifyDataValue(AddressOf RequestsRecieved))
         .SetDelegate("GoodResponses", New GetNotifyDataValue(AddressOf GoodResponses))
         .SetDelegate("TimedOutResponses", New GetNotifyDataValue(AddressOf TimedOutResponses))
         .SetDelegate("ErrorResponses", New GetNotifyDataValue(AddressOf ErrorResponses))
      End With
   End Sub

   Public Shared Sub Shutdown()
      NotificationManager.RemoveProcess("Comms", "WCFRequest")
   End Sub

   Public Shared Function RequestsRecieved() As String
      Return m_RequestsRecieved.ToString
   End Function

   Public Shared Function GoodResponses() As String
      Return m_GoodResponses.ToString
   End Function

   Public Shared Function TimedOutResponses() As String
      Return m_TimedOutResponses.ToString
   End Function

   Public Shared Function ErrorResponses() As String
      Return m_ErrorResponses.ToString
   End Function

   Public Shared Function OutstandingRequestCount() As String
      Return m_SyncronisedOutstandingRequests.Count.ToString
   End Function

   Public Shared Sub RequestRecieved(ByVal Id As Guid, ByVal Result As Object)
      QueueManager.EmptyQueuesForRequest(Id, "response being sent")
      If m_SyncronisedOutstandingRequests.ContainsKey(Id) Then
         m_SyncronisedOutstandingRequests(Id) = Result
      Else
         If TypeOf Result Is QueryResult Then
            EventLogging.Log("Recieved a response which is not in the Outstanding Requests: " & Id.ToString & ControlChars.NewLine & DirectCast(Result, QueryResult).ToXML(True), "WCFRequestService", EventLogEntryType.Warning)
         Else
            EventLogging.Log("Recieved a response which is not in the Outstanding Requests: " & Id.ToString & ControlChars.NewLine & Result.ToString, "WCFRequestService", EventLogEntryType.Warning)
         End If
      End If
   End Sub
   
   Public Function Query(ByVal Request As QueryRequest) As QueryResult 
      Return ProcessRequest(Request)
   End Function

   Public Shared Function ProcessRequest(Request As QueryRequest) As QueryResult

      Try

         'this will throw an exception if this are not right....
         ValidateRequest(Request)

         m_RequestsRecieved += 1

         'queue the request
         m_SyncronisedOutstandingRequests.Add(Request.ID, Nothing)

         Dim RequestItem As New RequestItem(Request)
         QueueManager.RequestQueueInstance.Enqueue(RequestItem)

         Dim Timeout As Date = Date.UtcNow.Add(Request.TimeOut)

         Do While m_SyncronisedOutstandingRequests(Request.ID) Is Nothing
            If Date.UtcNow < Timeout Then
               System.Threading.Thread.Sleep(100)
            Else
               Dim Ex As New TimeoutException(Request.TimeOutSeconds & " Has passed and we have no data returned for request '" & Request.ID.ToString & "' Query '" & Request.QueryInstructionSet & "' Session '" & RequestItem.RequestSessionIdentifier & "'.")
               SessionManager.QueueSessionShutdown(Request.ID, "Request Timedout - " & Request.TimeOutSeconds)
               m_SyncronisedOutstandingRequests(Request.ID) = Ex
            End If
         Loop

         Dim result As Object = m_SyncronisedOutstandingRequests(Request.ID)
         m_SyncronisedOutstandingRequests.Remove(Request.ID)

         SessionManager.RequestComplete(Request.ID)

         Select Case True

            Case TypeOf result Is QueryResult
               m_GoodResponses += 1
               ErrorResponseLog.GoodResponse(Request)
               Return DirectCast(result, QueryResult)

            Case TypeOf result Is Exception

               If TypeOf result Is TimeoutException Then
                  m_TimedOutResponses += 1
               Else
                  m_ErrorResponses += 1
               End If

               Dim Exception As Exception = DirectCast(result, Exception)
               Throw New QueryFailureException("Request failed due to an exception response: " & Exception.Message, Exception)

            Case Else
               m_ErrorResponses += 1
               Throw New UnreconisedResponseException("A object of type " & result.GetType.Name & " was returned to WCF.")

         End Select


      Catch ex As FailedQueryAttemptExceededException
         EventLogging.Log(ex.ToString, "MQRQuery", EventLogEntryType.Warning)
         Throw

      Catch ex As QueryFailureException When TypeOf ex.InnerException Is FailedQueryAttemptExceededException
         EventLogging.Log(ex.ToString, "MQRQuery", EventLogEntryType.Warning)
         Throw

      Catch ex As QueryFailureException When TypeOf ex.InnerException Is MQRCacheRetrieveException
         EventLogging.Log(ex.ToString, "MQRQuery", EventLogEntryType.Warning)
         Throw

      Catch ex As QueryFailureException When TypeOf ex.InnerException Is MQRRetrieveTimeoutException
         EventLogging.Log(ex.ToString, "MQRQuery", EventLogEntryType.Warning)
         Throw

      Catch ex As Exception

         Dim AdditionalText As String = String.Empty
         ErrorResponseLog.RequestFaulted(Request, ex, AdditionalText)

         If AdditionalText = String.Empty Then
            EventLogging.Log(ex.ToString, "MQRQuery", EventLogEntryType.Error)
         Else
            EventLogging.Log(New FailedQueryAttemptExceededException(AdditionalText, ex).ToString, "MQRQuery", EventLogEntryType.Error)
         End If

         Throw

      End Try
   End Function

      Private shared Sub ValidateRequest(ByVal Request As QueryRequest)

      If Request is nothing Then
         throw new InvalidMQRRequest("The request is null")
      End If
      
      Dim NextAllowedAtteptUTC As Date
      Dim FailedAttempts As Integer

      Select Case False

         Case Not m_SyncronisedOutstandingRequests.ContainsKey(Request.ID)
            Throw New InvalidMQRRequest("The request ID specified " + Request.ID.ToString + " on query " & Request.QueryInstructionSet & " is already being requested!, please re submit you request with a new ID")

         Case ErrorResponseLog.CheckCanRequestAgain(Request, NextAllowedAtteptUTC, FailedAttempts)
            Throw New FailedQueryAttemptExceededException("The request you are making has failed too many times (" & FailedAttempts & ") a temporary restriction has been put on this query. The next attempt can be made at " & NextAllowedAtteptUTC.ToString & " (UTC). RequestID" + Request.ID.ToString + " Query " & Request.QueryInstructionSet & " Parameters " & Request.Parameters.ToString)

         Case InstructionSetManager.HasLogonInstructionSet(Request.LogonInstructionSet)
            Throw New InvalidMQRRequest("The Logon '" & Request.LogonInstructionSet & "' is not a reconised instruction set.")

         Case InstructionSetManager.HasQueryInstructionSet(Request.QueryInstructionSet)
            Throw New InvalidMQRRequest("The Query '" & Request.QueryInstructionSet & "' is not a reconised instruction set.")

         Case InstructionSetManager.GetLogonInstructionset(Request.LogonInstructionSet).StaticConfiguration.IsQueryCompatable(Request.QueryInstructionSet)
            Throw New InvalidMQRRequest("The Query '" & Request.QueryInstructionSet & "' is not compatable with the logon '" & Request.LogonInstructionSet & "'.")

         Case InstructionSetManager.HasParseInstructionSet(Request.ParseInstructionSet)
            Throw New InvalidMQRRequest("The Parse '" & Request.ParseInstructionSet & "' is not a reconised instruction set.")

         Case InstructionSetManager.GetQueryInstructionSet(Request.QueryInstructionSet).IsParseCompatable(Request.ParseInstructionSet)
            Throw New InvalidMQRRequest("The Parse '" & Request.ParseInstructionSet & "' is not compatable with the query '" & Request.QueryInstructionSet & "'.")

      End Select

   End Sub

   
   Public Function InstructionSetData() As InstructionSetData 

      Dim Result As New InstructionSetData

      For Each Item As LogonInstructionSet In InstructionSetManager.GetAllLogonInstructionsets
         Dim NewItem As New LogonInstructionSetData
         NewItem.Name = Item.Identifier

         If Item.StaticConfiguration.CompatableQueryList.Count = 1 And Item.StaticConfiguration.CompatableQueryList(0) = "*" Then
            For Each Query As QueryInstructionSet In InstructionSetManager.GetAllQueryInstructionsets
               NewItem.CompatableQueries.Add(BuildQueryData(Query))
            Next
         Else
            For Each CompatableQuery As String In Item.StaticConfiguration.CompatableQueryList
               Dim Query As QueryInstructionSet = InstructionSetManager.GetQueryInstructionSet(CompatableQuery)
               NewItem.CompatableQueries.Add(BuildQueryData(Query))
            Next
         End If
         Result.LogonInstructionSetData.Add(NewItem)
      Next

      For Each Item As QueryInstructionSet In InstructionSetManager.GetAllQueryInstructionsets
         Result.QueryInstructionSetData.Add(BuildQueryData(Item))
      Next

      For Each Item As ParseInstructionSet In InstructionSetManager.GetAllParseInstructionsets
         Dim NewItem As New ParseInstructionSetData
         NewItem.Name = Item.Identifier
         Result.ParseInstructionSetData.Add(NewItem)
      Next

      Return Result

   End Function

   Private Function BuildQueryData(ByVal Query As QueryInstructionSet) As QueryInstructionSetData
      Dim NewItem As New QueryInstructionSetData
      NewItem.Name = Query.Identifier
      For Each Param As String In Query.QueryParameters
         Dim NewParamItem As New QueryInstructionSetParameter
         NewParamItem.Name = Param
         NewItem.Parameters.Add(NewParamItem)
      Next
      NewItem.CompatableParses = GetCompatableParses(Query)
      Return NewItem
   End Function

   Private Function GetCompatableParses(ByVal Query As QueryInstructionSet) As ParseInstructionSetDatas

      Dim Result As New ParseInstructionSetDatas

      For Each Item As String In Query.CompatableParseList
         Dim NewItem As New ParseInstructionSetData
         NewItem.Name = Item
         Result.Add(NewItem)
      Next

      Return Result
   End Function

End Class
