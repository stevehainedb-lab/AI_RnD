Imports EWS.MQR.XML
Imports EWS.Diagnostics

Public Class ErrorResponseLog

   Private Shared m_ErrorsLog As New Dictionary(Of String, ErrorState)

   Public Shared Sub Clearup()

      Threading.Monitor.Enter(m_ErrorsLog)
      Try
         Dim HashesToRemove As New List(Of String)
         For Each Hash As String In m_ErrorsLog.Keys
            If Date.UtcNow > m_ErrorsLog(Hash).AllowedAfterDateUTC.AddHours(6) Then
               HashesToRemove.Add(Hash)
            End If
         Next

         For Each Hash As String In HashesToRemove
            m_ErrorsLog.Remove(Hash)
         Next

      Catch ex As Exception
         EventLogging.Log(ex.ToString, "ErrorResponseLog.Clearup", EventLogEntryType.Error)
      Finally
         Threading.Monitor.Exit(m_ErrorsLog)
      End Try

   End Sub

   Public Shared Function CheckCanRequestAgain(ByVal Request As QueryRequest, ByRef NextAllowedDateUTC As Date, ByRef FailedAttempts As Integer) As Boolean

      Dim Hash As String = Request.UniqueHashCode

      Threading.Monitor.Enter(m_ErrorsLog)
      Try

         If m_ErrorsLog.ContainsKey(Hash) Then
            With m_ErrorsLog(Hash)
               If .Failures > 1 Then
                  If Date.UtcNow < .AllowedAfterDateUTC Then
                     NextAllowedDateUTC = .AllowedAfterDateUTC
                     FailedAttempts = .Failures
                     Return False
                  Else
                     Return True
                  End If
               Else
                  Return True
               End If
            End With
         Else
            Return True
         End If
      Catch ex As Exception
         EventLogging.Log(ex.ToString, "ErrorResponseLog.CheckCanRequestAgain", EventLogEntryType.Error)
         Return True
      Finally
         Threading.Monitor.Exit(m_ErrorsLog)
      End Try
   End Function

   Public Shared Sub RequestFaulted(ByVal Request As QueryRequest, e As Exception, ByRef AdditionalText As String)
      if Request Is Nothing Then
         Return
      End If
      
      Try
         Threading.Monitor.Enter(m_ErrorsLog)
         
         Dim Hash As String = Request.UniqueHashCode
         If m_ErrorsLog.ContainsKey(Hash) Then
            With m_ErrorsLog(Hash)
               .Failures += 1
               Dim TimePeriod As TimeSpan
               If Request.TimeOutSeconds < 60 Then
                  TimePeriod = New TimeSpan(0, 1 * (.Failures - 1), 0)
               Else
                  TimePeriod = TimeSpan.FromTicks(Request.TimeOut.Ticks * (.Failures - 1))
               End If

               If TimePeriod.TotalMinutes > MQRConfig.Current.MQRServiceConfig.MaxQueryExclusionMinutes Then
                  .AllowedAfterDateUTC = Date.UtcNow.AddMinutes(MQRConfig.Current.MQRServiceConfig.MaxQueryExclusionMinutes)
               Else
                  .AllowedAfterDateUTC = Date.UtcNow.Add(TimePeriod)
               End If
               AdditionalText = "This query has failed " & .Failures & " times, which has caused a temporary restriction to be put in place this will be lifted at " & .AllowedAfterDateUTC.ToString & " (UTC)."
            End With
         Else
            Dim NewErrorState As New ErrorState
            With NewErrorState
               .Failures = 1
               .AllowedAfterDateUTC = Date.UtcNow.AddMinutes(MQRConfig.Current.MQRServiceConfig.MaxQueryExclusionMinutes)
            End With
            m_ErrorsLog.Add(Hash, NewErrorState)
         End If
      Catch ex As Exception
         EventLogging.Log(ex.ToString, "ErrorResponseLog.RequestFaulted", EventLogEntryType.Error)
      Finally
         Threading.Monitor.Exit(m_ErrorsLog)
      End Try
   End Sub

   Public Shared Sub GoodResponse(ByVal Request As QueryRequest)
      Try
         Dim Hash As String = Request.UniqueHashCode

         Threading.Monitor.Enter(m_ErrorsLog)
         If m_ErrorsLog.ContainsKey(Hash) Then
            m_ErrorsLog.Remove(Hash)
         End If

      Catch ex As Exception
         EventLogging.Log(ex.ToString, "ErrorResponseLog.GoodResponse", EventLogEntryType.Error)
      Finally
         Threading.Monitor.Exit(m_ErrorsLog)
      End Try
   End Sub

   Public Class ErrorState
      Public AllowedAfterDateUTC As Date
      Public Failures As Integer
   End Class



End Class
