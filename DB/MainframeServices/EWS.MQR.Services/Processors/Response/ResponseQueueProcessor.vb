Imports EWS.Diagnostics
Imports EWS.Network.TCPIP
Imports EWS.MQR.Services.MQRManager
Imports EWS.MQR.Services.ResponseQueueProcessorManager

Public Class ResponseQueueProcessor : Inherits QueueProcessorBase(Of ResponseItem)

   Private m_SendMessageMethod As ResponseSendMessageDelegate

   Public Sub New(ByVal ProcessorType As String, ByVal Identifier As String, ByVal SendMessageMethod As ResponseSendMessageDelegate)
      MyBase.New(ProcessorType, Identifier)
      m_SendMessageMethod = SendMessageMethod
   End Sub

   Protected Overrides Function Dequeue() As ResponseItem
      If Not QueueManager.ResponseQueueInstance Is Nothing Then
         Return QueueManager.ResponseQueueInstance.Dequeue
      Else
         Return Nothing
      End If
   End Function

   Protected Overrides Sub ProcessItem(ByVal Item As ResponseItem)

      m_SendMessageMethod.Invoke(New SendDataEventArgs(Item.RequestorConnectionID, Item.Message))
      Trace.WriteLine("Completed processing item. Metrics:" & Item.Metrics.ToString, TraceLevel.Verbose)

   End Sub

End Class