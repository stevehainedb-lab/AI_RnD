Imports System.Data.SqlClient
Imports EWS.Diagnostics
Imports EWS.MQR.XML

Public Class Metric

#Region " Private Methods "

   Public Shared Sub WriteMetric(ByVal Sys As String, ByVal Task As String, ByVal Parameters As String, ByVal Duration As TimeSpan)

      Dim connection As New SqlConnection(MQRConfig.Current.MQRServiceConfig.MetricsConnectionString)

      Dim TSQL As String

      'If System.Diagnostics.Debugger.IsAttached Then
      '   TSQL = String.Format("INSERT INTO Metrics (Machine, System, Task, Parameters, Duration) VALUES (N'{0}', N'{1}', N'DEBUG_{2}', N'{3}', {4})", System.Environment.MachineName, Sys, Task, Parameters, Duration.TotalMilliseconds)
      'Else
      TSQL = String.Format("INSERT INTO Metrics (Machine, System, Task, Parameters, Duration) VALUES (N'{0}', N'{1}', N'{2}', N'{3}', {4})", System.Environment.MachineName, Sys, Task, Parameters, Duration.TotalMilliseconds)
      'End If

      Dim command As New SqlCommand(TSQL, connection)

      Try

         With connection
            .Open()

            With command
               Try
                  .ExecuteNonQuery()
               Catch ex As Exception
                  Trace.WriteLine(String.Format("WriteMetric - {0}", ex.Message), TraceLevel.Error)
               Finally
                  .Dispose()
               End Try
            End With
         End With
      Catch ex As Exception
         EventLogging.Log(ex.ToString, "MQRMetrics", EventLogEntryType.Error)
      Finally
         With connection
            .Close()
            .Dispose()
         End With
      End Try
   End Sub

   Public Shared Function HaveMetricConnectivity() As Boolean

      Dim Result As Boolean
      Dim Connection As New SqlConnection(MQRConfig.Current.MQRServiceConfig.MetricsConnectionString)

      Try
         connection.Open()
         result = True
      Catch ex As Exception

         EventLogging.Log(ex.ToString, "MQRMetrics", EventLogEntryType.Error)
         result = False
      Finally
         With connection
            .Close()
            .Dispose()
         End With
      End Try

      Return result

   End Function

#End Region

End Class
