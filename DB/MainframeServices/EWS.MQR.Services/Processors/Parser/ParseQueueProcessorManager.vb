Imports EWS.MQR.XML
Imports EWS.Diagnostics

Public Class ParseQueueProcessorManager

#Region " Member Variables  "

   Private m_Processors As New List(Of ParseQueueProcessor)
   Const QUEUE_PROCESSOR_TYPE As String = "Parse"

#End Region

#Region " Constructors "

   Public Sub New()
      NotificationData.UpdateState(MQRConfig.Current.MQRServiceConfig.ParseProcessorThreads & " Processor Thread(s)")
      For Count As Integer = 1 To MQRConfig.Current.MQRServiceConfig.ParseProcessorThreads
         m_Processors.Add(New ParseQueueProcessor(QUEUE_PROCESSOR_TYPE, Count.ToString))
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
      For Each item As ParseQueueProcessor In m_Processors
         item.StartProcess()
      Next
   End Sub

   Public Sub StopProcesses()
      For Each item As ParseQueueProcessor In m_Processors
         item.StopProcess()
      Next

      NotificationManager.RemoveProcess("Processors", QUEUE_PROCESSOR_TYPE)
   End Sub



#End Region

End Class


