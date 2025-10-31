Imports EWS.Diagnostics
Imports EWS.MQR.XML

Public MustInherit Class QueueItemBase

   Private m_CoreQueueItemData As CoreQueueItemData
   Private m_Metrics As RequestMetrics
   Private m_CreatedUTC As Date

   Protected Sub New(ByVal CoreQueueItemData As CoreQueueItemData, ByVal Metrics As RequestMetrics)
      m_CoreQueueItemData = CoreQueueItemData
      m_Metrics = Metrics
      m_CreatedUTC = Date.UtcNow
   End Sub

   Public ReadOnly Property QueueItemData() As CoreQueueItemData
      Get
         Return m_CoreQueueItemData
      End Get
   End Property

   Public ReadOnly Property LogonIdentifier() As String
      Get
         Return QueueItemData.LogonIdentifier
      End Get
   End Property

   Public ReadOnly Property Metrics() As RequestMetrics
      Get
         Return m_Metrics
      End Get
   End Property

   Public ReadOnly Property QueryIdentifier() As String
      Get
         Return QueueItemData.QueryIdentifier
      End Get
   End Property

   Public ReadOnly Property ParseIdentifier() As String
      Get
         Return QueueItemData.ParseIdentifier
      End Get
   End Property


   Public ReadOnly Property MFTimeQuerySubmitted As Date
      Get
         Return QueueItemData.MFTimeQuerySubmitted
      End Get
   End Property

   Public ReadOnly Property MFTimeResponseRecieved() As Date
      Get
         Return QueueItemData.MFTimeResponseRecieved
      End Get
   End Property


   Public ReadOnly Property RequestSessionIdentifier() As String
      Get
         Return QueueItemData.RequestSessionIdentifier
      End Get
   End Property

   Public ReadOnly Property InstructionSetData() As String
      Get
         Return m_CoreQueueItemData.InstructionSetData
      End Get
   End Property

   Public Function GetQueueItemData() As QueueItemData

      Dim Result As New QueueItemData

      With Result
         If m_CoreQueueItemData Is Nothing Then
            .RequestID = ""
            .InstructionSetData = ""
            .SessionIdentifier = ""
            .SubmitDate = Date.MinValue
         Else
            .RequestID = m_CoreQueueItemData.RequestID.ToString
            .InstructionSetData = m_CoreQueueItemData.InstructionSetData
            .SessionIdentifier = m_CoreQueueItemData.RequestSessionIdentifier
            .SubmitDate = m_CoreQueueItemData.MFTimeQuerySubmitted
         End If

         If m_Metrics Is Nothing Then
            .Metrics = ""
         Else
            .Metrics = m_Metrics.ToString
         End If
      End With

      Return Result

   End Function

   Public Function TimeOutPeriod(ByVal QueueConfiguredTimeOut As TimeSpan) As TimeSpan
      If HasRequestSpecifedTimeOut Then
         Return RequestSpecifedTimeOutPeriod
      Else
         Return QueueConfiguredTimeOut
      End If
   End Function

   Public Function HasTimedOut(ByVal QueueConfiguredTimeOut As TimeSpan) As Boolean

      If HasRequestSpecifedTimeOut Then
         If m_CoreQueueItemData.RequestRecievedAtUTC.Add(RequestSpecifedTimeOutPeriod) < Date.UtcNow Then
            m_TimeoutTimeUtc = m_CoreQueueItemData.RequestRecievedAtUTC.Add(RequestSpecifedTimeOutPeriod)
            Return True
         Else
            Return False
         End If
      Else
         If m_CreatedUTC.Add(QueueConfiguredTimeOut) < Date.UtcNow Then
            m_TimeoutTimeUtc = m_CreatedUTC.Add(QueueConfiguredTimeOut)
            Return True
         Else
            Return False
         End If
      End If

   End Function

   Protected ReadOnly Property HasRequestSpecifedTimeOut() As Boolean
      Get
         Return Not RequestSpecifedTimeOutPeriod = TimeSpan.Zero
      End Get
   End Property

   Protected ReadOnly Property RequestSpecifedTimeOutPeriod() As TimeSpan
      Get
         Return m_CoreQueueItemData.RequestTimeOut
      End Get
   End Property

   Private m_TimeoutTimeUtc As Date = Date.MinValue
   Public ReadOnly Property TimeOutTimeUTC() As Date
      Get
         Return m_TimeoutTimeUtc
      End Get
   End Property

   Public Overridable ReadOnly Property Key() As String
      Get
         Return m_CoreQueueItemData.RequestID.ToString
      End Get
   End Property

End Class
