Imports System.Data.SqlClient
Imports System.Text
Imports EWS.Diagnostics
Imports System.Text.RegularExpressions
Imports EWS.MQR.Services.MQRManager
Imports EWS.MQR.XML
Imports EWS.MQR.XML.InstructionSetManager

Public Class CacheWriterProcessor : Inherits QueueProcessorBase(Of CacheWriterItem)

   Public Sub New(ByVal ProcessorType As String, ByVal Identifier As String)
      MyBase.New(ProcessorType, Identifier)
   End Sub

   Protected Overrides Function Dequeue() As CacheWriterItem
      If Not QueueManager.CacheWriterQueueInstance Is Nothing Then
         Return QueueManager.CacheWriterQueueInstance.Dequeue
      Else
         Return Nothing
      End If
   End Function

   Protected Overrides Sub ProcessItem(ByVal Item As CacheWriterItem)
      Try
         Dim Connection As New SqlConnection(MQRConfig.Current.MQRServiceConfig.CacheConnectionString)
         Dim Command As New SqlCommand()
         With Command
            .Connection = Connection
            .CommandType = CommandType.StoredProcedure
            .CommandText = "SP_InsertCache"

           With .Parameters.Add(.CreateParameter)
               .Direction = ParameterDirection.Input
               .ParameterName = "@Query"
               .SqlDbType = SqlDbType.NVarChar
               .SqlValue = Item.QueueItemData.QueryIdentifier
            End With

           With .Parameters.Add(.CreateParameter)
               .Direction = ParameterDirection.Input
               .ParameterName = "@Parameters"
               .SqlDbType = SqlDbType.NVarChar
               If Item.RequestParamters.CommandID = String.Empty Then
                  .SqlValue = Item.RequestParamters.ToString
               Else
                  .SqlValue = Item.RequestParamters.CommandID
               End If
            End With

            With .Parameters.Add(.CreateParameter)
               .Direction = ParameterDirection.Input
               .ParameterName = "@Result"
               .SqlDbType = SqlDbType.Text
               .SqlValue = Item.RawData.Trim
            End With

            With .Parameters.Add(.CreateParameter)
               .Direction = ParameterDirection.Input
               .ParameterName = "@QueryDate"
               .SqlDbType = SqlDbType.DateTime
               .SqlValue = Item.QueueItemData.MFTimeResponseRecieved
            End With
         End With

         Try

            With Connection
               .Open()

               With Command
                  Try
                     .ExecuteNonQuery()
                  Catch ex As Exception
                     Trace.WriteLine(String.Format("CacheWriter - {0}", ex.Message), TraceLevel.Error)
                  Finally
                     .Dispose()
                  End Try
               End With
            End With
         Catch ex As Exception
            EventLogging.Log(ex.ToString, "MQRMetrics", EventLogEntryType.Error)
         Finally
            With Connection
               .Close()
               .Dispose()
            End With
         End Try

      Catch ex As Exception
         EventLogging.Log(LogException(Item, ex), Me.GetType.Name, EventLogEntryType.Error)
      End Try
   End Sub

End Class

