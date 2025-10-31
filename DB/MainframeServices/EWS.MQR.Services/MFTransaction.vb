Imports System.Data.SqlClient
Imports EWS.Diagnostics
Imports EWS.MQR.XML

Public Class MFTransaction

   Private Delegate Sub WriteTransactionDetailsDelegate(ByRef Data As TransactionData, Task As String, ByVal KeyStroke As KeyCommand)
   Private Shared WriteDelegate As New WriteTransactionDetailsDelegate(AddressOf DoWriteTransactionDetails)

   Public Class TransactionData

      Public _CallingApplication As String
      Public _Query As String
      Public _RequestId As Guid
      Public _SearchKey As String
      Public _MFUserID As String
      Public _SessionID As String

      Public Sub New(CallingApplication As String, Query As String, RequestId As Guid, SearchKey As String, MFUserID As String, SessionID As String)

         _CallingApplication = CallingApplication
         _Query = Query
         _RequestId = RequestId
         _SearchKey = SearchKey
         _MFUserID = MFUserID
         _SessionID = SessionID

      End Sub

      Public Sub New(ByRef MQRRequest As RequestItem, ByRef MFUserID As String, SessionID As String)
         Me.New(MQRRequest.Request.CallingApplication, MQRRequest.QueryIdentifier, MQRRequest.Request.ID, MQRRequest.Parameters.SearchKey, MFUserID, SessionID)
      End Sub

   End Class

   Public Shared Sub WriteTransactionDetails(ByRef Data As TransactionData, ByVal Task As String, ByVal KeyStroke As KeyCommand)

      Select Case KeyStroke
         Case Nothing,
              KeyCommand.Clear, _
              KeyCommand.Enter, _
              KeyCommand.PF1, _
              KeyCommand.PF2, _
              KeyCommand.PF3, _
              KeyCommand.PF4, _
              KeyCommand.PF5, _
              KeyCommand.PF6, _
              KeyCommand.PF7, _
              KeyCommand.PF8, _
              KeyCommand.PF9, _
              KeyCommand.PF10, _
              KeyCommand.PF11, _
              KeyCommand.PF12, _
              KeyCommand.PA1, _
              KeyCommand.PA2, _
              KeyCommand.PA3, _
              KeyCommand.PA4, _
              KeyCommand.PA5, _
              KeyCommand.PA6, _
              KeyCommand.PA7, _
              KeyCommand.PA8, _
              KeyCommand.PA9, _
              KeyCommand.PA10, _
              KeyCommand.PA11, _
              KeyCommand.PA12

            WriteDelegate.BeginInvoke(Data, Task, KeyStroke, Nothing, Nothing)

      End Select
   End Sub

   Public Const INSERT_SQL As String = "INSERT INTO MFTransaction (Machine, CallingApplication, Query, Task, RequestID, SearchKey, MFUserID, SessionID) VALUES (N'{0}', N'{1}', N'{2}', N'{3}', N'{4}', '{5}', N'{6}', '{7}')"

   Private Shared Sub DoWriteTransactionDetails(ByRef Data As TransactionData, Task As String, ByVal KeyStroke As KeyCommand)
      Using con As New SqlConnection(MQRConfig.Current.MQRServiceConfig.MFTransactionConnectionString)
         con.Open()
         Try
            With Data
               Using cmd As New SqlCommand(String.Format(INSERT_SQL, System.Environment.MachineName, ._CallingApplication, ._Query, Task & "-" & KeyStroke.ToString, ._RequestId.ToString, ._SearchKey, ._MFUserID, ._SessionID), con)
                  cmd.ExecuteNonQuery()
               End Using
            End With
         Catch ex As Exception
            EventLogging.Log(ex.ToString, "MFTransaction", EventLogEntryType.Error)
         Finally
            con.Close()
         End Try
      End Using
   End Sub

End Class
