Public Class MQRCacheRetrieveException : Inherits Exception

   Public Sub New(ByVal Item As RequestItem, Reason As String)
      MyBase.new(BuildMessage(Item, Reason))
   End Sub

   Private Shared Function BuildMessage(ByVal Item As RequestItem, Reason As String) As String

      Dim ParamsUsed As String

      With Item.Parameters
         If .CommandID = String.Empty Then
            ParamsUsed = "Parameters - " & .ToString
         Else
            ParamsUsed = "CommandID - " & .CommandID
         End If
      End With

      Dim ErrorMesage As String = String.Format("MQR is configured to use cache for {0} requests. Cache read failure because {1}. Query: {2} - paremeters were: {3}", Item.LogonIdentifier, Reason, Item.QueryIdentifier, ParamsUsed)

      Return ErrorMesage

   End Function

End Class
