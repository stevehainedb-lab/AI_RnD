Imports EWS.Diagnostics
Imports EWS.MQR.XML

Public Class QueueManager

   Private Shared m_RequestQueueInstance As New RequestQueue
   Private Shared m_AwaitingOutputQueueInstance As New AwaitingOutputQueue
   Private Shared m_CompleteOutputQueueInstance As New CompleteOutputQueue
   Private Shared m_RequiresParsingQueueInstance As New RequiresParsingQueue
   Private Shared m_CacheWriterQueueInstance As New CacheWriterQueue

   Public Shared Sub CheckQueuesForTimedOutItems()
      m_RequestQueueInstance.CheckQueueForTimedOutItems()
      m_AwaitingOutputQueueInstance.CheckQueueForTimedOutItems()
      m_CompleteOutputQueueInstance.CheckQueueForTimedOutItems()
      m_RequiresParsingQueueInstance.CheckQueueForTimedOutItems()
   End Sub

   Public Shared ReadOnly Property RequestQueueInstance() As RequestQueue
      Get
         Return m_RequestQueueInstance
      End Get
   End Property

   Public Shared ReadOnly Property AwaitingOutputQueueInstance() As AwaitingOutputQueue
      Get
         Return m_AwaitingOutputQueueInstance
      End Get
   End Property

   Public Shared ReadOnly Property CompleteOutputQueueInstance() As CompleteOutputQueue
      Get
         Return m_CompleteOutputQueueInstance
      End Get
   End Property

   Public Shared ReadOnly Property RequiresParsingQueueInstance() As RequiresParsingQueue
      Get
         Return m_RequiresParsingQueueInstance
      End Get
   End Property

   Public Shared ReadOnly Property CacheWriterQueueInstance() As CacheWriterQueue
      Get
         Return m_CacheWriterQueueInstance
      End Get
   End Property

   Public Shared Sub InitaliseQueues()

      m_RequestQueueInstance.Initalise()
      m_AwaitingOutputQueueInstance.Initalise()
      m_CompleteOutputQueueInstance.Initalise()
      m_RequiresParsingQueueInstance.Initalise()
      m_CacheWriterQueueInstance.Initalise()

   End Sub

   Public Shared Sub ShutDownQueues()

      m_RequestQueueInstance.Close()
      m_AwaitingOutputQueueInstance.Close()
      m_CompleteOutputQueueInstance.Close()
      m_RequiresParsingQueueInstance.Close()
      m_CacheWriterQueueInstance.Close()

   End Sub

   Public Shared Function GetQueueData() As String

      Dim Result As New QueuesData

      Result.Queues.Add("RequestQueue", m_RequestQueueInstance.QueueData)
      Result.Queues.Add("AwaitingOutputQueue", m_AwaitingOutputQueueInstance.QueueData)
      Result.Queues.Add("CompleteOutputQueue", m_CompleteOutputQueueInstance.QueueData)
      Result.Queues.Add("RequiresParsingQueue", m_RequiresParsingQueueInstance.QueueData)
      Result.Queues.Add("CacheWriterQueue", m_CacheWriterQueueInstance.QueueData)

      Return Result.ToXml(False)

   End Function

   Public Shared Sub EmptyQueuesForRequest(ByVal RequestID As Guid, ByVal Reason As String)

      Dim RequestQueueItemForRequestID As RequestItem = m_RequestQueueInstance.Dequeue(RequestID.ToString)
      If Not RequestQueueItemForRequestID Is Nothing Then
         If Not Reason = String.Empty Then
            EventLogging.Log("An item has been removed off the RequestQueue for RequestID " & RequestID.ToString & " because " & Reason & _
            ". It was on the queue for " & RequestQueueItemForRequestID.Metrics.AgePerformingAction("Request").TotalSeconds.ToString & " seconds.", _
            "QueueManager", EventLogEntryType.Warning)
         End If
      End If

      Dim RequiresParsingItemForRequest As RequiresParsingItem = m_RequiresParsingQueueInstance.Dequeue(RequestID.ToString)
      If Not RequiresParsingItemForRequest Is Nothing Then
         If Not Reason = String.Empty Then
            EventLogging.Log("An item has been removed off the RequiresParsingQueue for RequestID " & RequestID.ToString & " because " & Reason & _
            ". It was on the queue for " & RequiresParsingItemForRequest.Metrics.AgePerformingAction(" RequiresParsing").TotalSeconds.ToString & " seconds.", _
            "QueueManager", EventLogEntryType.Warning)
         End If
      End If
   End Sub

   Public Shared Sub EmptyQueuesForSession(ByVal SessionID As String, ByVal Reason As String)

      Dim AwaitingOutputItemForSession As AwaitingOutputItem = m_AwaitingOutputQueueInstance.Dequeue(SessionID)
      If Not AwaitingOutputItemForSession Is Nothing Then
         If Not Reason = String.Empty Then
            EventLogging.Log("An item has been removed off the AwaitingOutputQueue for session " & SessionID & " because " & Reason & _
            ". It was on the queue for " & AwaitingOutputItemForSession.Metrics.AgePerformingAction("AwaitingOutput").TotalSeconds.ToString & " seconds.", _
            "QueueManager", EventLogEntryType.Warning)
         End If
      End If

      Dim CompleteOutputItemForSession As CompleteOutputItem = m_CompleteOutputQueueInstance.Dequeue(SessionID)
      If Not CompleteOutputItemForSession Is Nothing Then
         If Not Reason = String.Empty Then
            EventLogging.Log("An item has been removed off the CompleteOutputQueue for session " & SessionID & " because " & Reason & _
            ". It was on the queue for " & CompleteOutputItemForSession.Metrics.AgePerformingAction("CompleteOutput").TotalSeconds.ToString & " seconds.", _
            "QueueManager", EventLogEntryType.Warning)
         End If
      End If
   End Sub

End Class
