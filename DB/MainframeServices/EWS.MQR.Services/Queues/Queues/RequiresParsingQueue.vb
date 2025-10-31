Imports EWS.MQR.XML

Public Class RequiresParsingQueue : Inherits SyncQueueBase(Of RequiresParsingItem)

   Public Sub New()
      MyBase.New(MQRConfig.Current.MQRServiceConfig.RequiresParsingTimeoutSeconds)
   End Sub

End Class
