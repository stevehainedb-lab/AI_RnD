Imports EWS.MQR.XML

Public Class CacheWriterQueue : Inherits SyncQueueBase(Of CacheWriterItem)

   Public Sub New()
      MyBase.New(MQRConfig.Current.MQRServiceConfig.CacheWriterTimeoutSeconds)
   End Sub

End Class
