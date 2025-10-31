Imports EWS.Diagnostics
Imports EWS.MQR.XML

Public Class ResponseQueue : Inherits SyncQueueBase(Of ResponseItem)

   Protected Overrides Sub CloseDown()

   End Sub

   Private m_Timeoutperiod As TimeSpan = TimeSpan.Zero
   Public Overrides Function QueueTimeOutPeriod() As TimeSpan
      If m_Timeoutperiod = TimeSpan.Zero Then
         m_Timeoutperiod = New TimeSpan(0, 0, MQRConfig.Current.MQRServiceConfig.ResponseTimeoutSeconds)
      End If
      Return m_Timeoutperiod
   End Function

   Public Overrides Sub ProcessExpiredItem(ByVal TimedOutItem As QueueItemBase)

   End Sub

   Protected Overrides Function DequeueExpiredItem() As ResponseItem
      'Dont expire the response queue,
      Return Nothing
   End Function
End Class
