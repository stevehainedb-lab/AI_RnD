
Public Class AwaitingOutputItem : Inherits QueueItemBase

   Private m_Request As QueryRequest
   Private m_TOPSAltName As String
   Private m_CollectedData As String
   Private m_PrintOutputsRecieved As Integer
   Private m_QueryCollectedData As String

   Public Sub New(ByVal Item As RequestItem, ByVal TOPSAltName As String)
      MyBase.New(Item.QueueItemData, Item.Metrics)
      m_Request = Item.Request
      m_TOPSAltName = TOPSAltName
   End Sub

   Public ReadOnly Property CollectedData() As String
      Get
         Return m_CollectedData + ControlChars.NewLine + ControlChars.NewLine + m_QueryCollectedData
      End Get
   End Property

   Public ReadOnly Property PrintOutputsRecieved() As Integer
      Get
         Return m_PrintOutputsRecieved
      End Get
   End Property

   Public Sub SetQueryCollectedData(ByVal Value As String)
      m_QueryCollectedData = Value
   End Sub

   Public Sub MatchedItem(ByVal Item As CompleteOutputItem)
      'Sync the core data properties
      QueueItemData.RecievedResponse(Item.QueueItemData.MFTimeResponseRecieved)
      m_CollectedData += ControlChars.NewLine + ControlChars.NewLine & Item.ResponseData
      m_PrintOutputsRecieved += 1
   End Sub

   Public ReadOnly Property Request() As QueryRequest
      Get
         Return m_Request
      End Get
   End Property

   Public ReadOnly Property QueryTime() As TimeSpan
      Get
         Return QueueItemData.QueryTime
      End Get
   End Property

   Public Overrides ReadOnly Property Key() As String
      Get
         Return m_TOPSAltName
      End Get
   End Property

End Class
