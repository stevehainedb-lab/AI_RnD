Imports EWS.Network.TCPIP

Public Class ResponseItem : Inherits QueueItemBase

   Private m_Message As TCPMessage

   Public Sub New(ByVal QueueItemData As CoreQueueItemData, ByVal ResponseMessage As TCPMessage, ByVal Metrics As RequestMetrics)
      MyBase.New(QueueItemData, Metrics)
      m_Message = ResponseMessage
   End Sub

   Public ReadOnly Property Message() As TCPMessage
      Get
         Return m_Message
      End Get
   End Property

End Class
