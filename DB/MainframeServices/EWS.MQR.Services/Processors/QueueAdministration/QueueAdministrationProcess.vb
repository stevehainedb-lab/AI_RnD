Imports EWS.Diagnostics
Imports System.Threading
Imports EWS.MQR.Services.MQRManager
Imports EWS.MQR.XML

Public Class QueueAdministrationProcess

#Region " Constructors "

   Private Sub New()

   End Sub

#End Region

#Region " Public Methods "

   Public Shared Sub StartProcess()
      StartProcessorThread()
   End Sub

   Public Shared Sub StopProcess()
      ThreadManager.ShutdownThread("QueueAdministration")
   End Sub

#End Region

#Region " Private Methods "

   Private Shared ReadOnly Property NotificationData() As NotifyData
      Get
         Return NotificationManager.Instance("Processors", "QueueAdministration")
      End Get
   End Property

   Private Shared Sub StartProcessorThread()

      ThreadManager.Thread("QueueAdministration", New ThreadManager.DoWork(AddressOf ProcessThread), ThreadPriority.Normal, 1000, 1000, True, True)

   End Sub

   Private Shared Function ProcessThread() As Boolean

      Try

         NotificationData.UpdateState(ProcessState.Processing)
         QueueManager.CheckQueuesForTimedOutItems()
         NotificationData.UpdateState(ProcessState.Running)

      Catch ex As Exception
         EventLogging.Log("EXCEPTION:" & ex.ToString, "QueueAdministration", EventLogEntryType.Error)
      End Try

   End Function

#End Region

End Class
