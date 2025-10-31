Public Class RequiresParsingItem : Inherits QueueItemBase

   Private m_RawData As String

   Public Sub New(ByVal CoreQueueItemData As CoreQueueItemData, ByVal RawData As String, ByVal Metrics As RequestMetrics)
      MyBase.New(CoreQueueItemData, Metrics)
      m_RawData = RawData
   End Sub

   Public Property RawData() As String
      Get
         Return m_RawData
      End Get
      Set(ByVal value As String)
         m_RawData = value
      End Set
   End Property

   Public Sub RecievedResponse(MFResponseTime As Date)
      MyBase.QueueItemData.RecievedResponse(MFResponseTime)
   End Sub

End Class
