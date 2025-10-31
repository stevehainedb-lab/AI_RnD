
Public Class CacheWriterItem : Inherits QueueItemBase

   Private m_Request As QueryRequest
   Private m_RawData As String

   Public Sub New(ByVal CoreQueueItemData As CoreQueueItemData, ByVal Request As QueryRequest, ByVal RawData As String, ByVal Metrics As RequestMetrics)
      MyBase.New(CoreQueueItemData, Metrics)
      m_Request = Request
      m_RawData = RawData
   End Sub

   Public ReadOnly Property RawData() As String
      Get
         Return m_RawData
      End Get
   End Property

   Public ReadOnly Property RequestParamters() As QueryRequestParameters
      Get
         Return m_Request.Parameters
      End Get
   End Property

End Class
