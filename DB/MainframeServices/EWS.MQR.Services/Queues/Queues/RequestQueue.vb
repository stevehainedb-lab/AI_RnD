Imports EWS.MQR.XML

Public Class RequestQueue : Inherits SyncQueueBase(Of RequestItem)

   Public Sub New()
      MyBase.New(MQRConfig.Current.MQRServiceConfig.RequestTimeoutSeconds)
   End Sub

End Class
