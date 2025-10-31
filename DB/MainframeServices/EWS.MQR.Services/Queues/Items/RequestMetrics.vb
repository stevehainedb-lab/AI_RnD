Imports System.Threading
Imports EWS.Diagnostics

Public Class RequestMetrics

   Private Class StartEndTime
      Public StartDateTimeUTC As Date = Date.UtcNow
      Public EndDateTimeUTC As Date = Date.MaxValue
      Public ReadOnly Property Duration() As TimeSpan
         Get
            If EndDateTimeUTC = Date.MaxValue Then
               Return Date.UtcNow - StartDateTimeUTC
            Else
               Return EndDateTimeUTC - StartDateTimeUTC
            End If

         End Get
      End Property
   End Class

   Private m_Metrics As New Dictionary(Of String, StartEndTime)
   Private m_Order As New List(Of String)

   Public Function AgePerformingAction(ByVal QueueName As String) As TimeSpan
      If Not m_Metrics.ContainsKey(QueueName.ToUpper) Then
         Return TimeSpan.Zero
      Else
         Return m_Metrics(QueueName.ToUpper).Duration
      End If
   End Function

   Friend Sub StartedAction(ByVal Name As String)
      Monitor.Enter(m_Metrics)
      Try
         If m_Metrics.ContainsKey(Name.ToUpper) Then
            m_Metrics(Name.ToUpper).StartDateTimeUTC = Date.UtcNow
         Else
            Add(Name.ToUpper)
         End If
      Finally
         Monitor.Exit(m_Metrics)
      End Try
   End Sub

   Friend Sub EndedAction(ByVal Name As String)
      Monitor.Enter(m_Metrics)
      Try
         If m_Metrics.ContainsKey(name.ToUpper) Then
            m_Metrics(name.ToUpper).EndDateTimeUTC = Date.UtcNow
         Else
            Add(name.ToUpper)
         End If
      Finally
         Monitor.Exit(m_Metrics)
      End Try
   End Sub

   Public Overrides Function ToString() As String
      Dim result As String = ""

      Monitor.Enter(m_Metrics)
      Try
         For Each Key As String In m_Order
            result += String.Format("{0}:{1}ms ", Key, CLng(m_Metrics(Key.ToUpper).Duration.TotalMilliseconds))
         Next
      Finally
         Monitor.Exit(m_Metrics)
      End Try

      Return result
   End Function

   Private Sub Add(ByVal Name As String)
      m_Metrics.Add(Name.ToUpper, New StartEndTime)
      m_Order.Add(Name.ToUpper)
   End Sub


End Class
