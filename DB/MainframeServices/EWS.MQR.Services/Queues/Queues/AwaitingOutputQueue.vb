Imports System.Threading
Imports EWS.MQR.XML

Public Class AwaitingOutputQueue : Inherits SyncQueueBase(Of AwaitingOutputItem)

   'Private m_SessionKeyedItems As New Dictionary(Of String, AwaitingOutputItem)

   '<Obsolete("Use the overloaded method that requires a sessionID")> _
   'Public Overrides Sub Enqueue(ByVal Item As AwaitingOutputItem)
   '   Throw New InvalidOperationException("Use the overloaded method that requires a sessionID")
   'End Sub

   '<Obsolete("Use GetItemForTOPSAltName Not Dequeue")> _
   'Public Overrides Function Dequeue() As AwaitingOutputItem
   '   Throw New InvalidOperationException("Use GetItemForTOPSAltName Not Dequeue")
   'End Function

   'Public Overloads Sub Enqueue(ByVal TOPSAltName As String, ByVal Item As AwaitingOutputItem)
   '   Monitor.Enter(m_SessionKeyedItems)
   '   Try
   '      If m_SessionKeyedItems.ContainsKey(TOPSAltName) Then
   '         Throw New SameSessionUsedException("The TOPSSession " & TOPSAltName & " already has an item waiting to for output!")
   '      End If
   '      MyBase.Enqueue(Item)
   '      m_SessionKeyedItems.Add(TOPSAltName, Item)
   '   Finally
   '      Monitor.Exit(m_SessionKeyedItems)
   '   End Try
   'End Sub

   'Public Function GetItemForTOPSAltName(ByVal TOPSAltName As String) As AwaitingOutputItem

   '   Dim Result As AwaitingOutputItem = Nothing
   '   Monitor.Enter(m_SessionKeyedItems)
   '   Try
   '      If m_SessionKeyedItems.ContainsKey(TOPSAltName) Then
   '         Result = m_SessionKeyedItems(TOPSAltName)
   '         m_SessionKeyedItems.Remove(TOPSAltName)
   '         MyBase.Remove(Result)
   '      End If
   '   Finally
   '      Monitor.Exit(m_SessionKeyedItems)
   '   End Try
   '   Return Result

   'End Function

   'Protected Overrides Sub CloseDown()
   '   m_SessionKeyedItems.Clear()
   'End Sub

   'Protected Overrides Function DequeueExpiredItem() As AwaitingOutputItem

   '   Dim Item As AwaitingOutputItem = MyBase.DequeueExpiredItem()

   '   If Not Item Is Nothing Then

   '      Monitor.Enter(m_SessionKeyedItems)
   '      Try
   '         m_SessionKeyedItems.Remove(Item.RequestSessionIdentifier)
   '      Finally
   '         Monitor.Exit(m_SessionKeyedItems)
   '      End Try
   '   End If

   '   Return Item
   'End Function

   Public Sub New()
      MyBase.New(MQRConfig.Current.MQRServiceConfig.AwaitingOutputTimeoutSeconds)
   End Sub

   Public Const TIME_OUT_RESPONSE As String = "Timed Out Item"
   Public Overrides Sub ProcessExpiredItem(ByVal TimedOutItem As QueueItemBase)

      With SessionManager.GetSessionByTOPSName(TimedOutItem.QueueItemData.RequestSessionIdentifier)
         .TimedOut()
         .ReceivedResponse(TIME_OUT_RESPONSE)
      End With

   End Sub

End Class
