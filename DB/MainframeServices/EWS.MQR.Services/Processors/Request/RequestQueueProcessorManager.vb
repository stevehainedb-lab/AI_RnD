Imports EWS.MQR.XML
Imports EWS.Diagnostics
Imports System.Threading

Public Class RequestQueueProcessorManager

#Region " Member Variables  "

   Private m_Processors As New List(Of RequestQueueProcessor)
   Const QUEUE_PROCESSOR_TYPE As String = "Request"

#End Region

#Region " Constructors "

   Public Sub New()
      NotificationData.UpdateState(MQRConfig.Current.MQRServiceConfig.RequestProcessorThreads & " Processor Thread(s)")
      For Count As Integer = 1 To MQRConfig.Current.MQRServiceConfig.RequestProcessorThreads
         m_Processors.Add(New RequestQueueProcessor(QUEUE_PROCESSOR_TYPE, Count.ToString))
      Next
   End Sub

#End Region

#Region " Private Methods "

   Private ReadOnly Property NotificationData() As NotifyData
      Get
         Return NotificationManager.Instance("Processors", QUEUE_PROCESSOR_TYPE)
      End Get
   End Property

#End Region

#Region " Public Methods "

   Public Sub StartProcesses()
      For Each Item As RequestQueueProcessor In m_Processors
         Item.StartProcess()
      Next
   End Sub

   Public Sub StopProcesses()
      For Each Item As RequestQueueProcessor In m_Processors
         Item.StopProcess()
      Next

      NotificationManager.RemoveProcess("Processors", QUEUE_PROCESSOR_TYPE)

   End Sub

#End Region

End Class
