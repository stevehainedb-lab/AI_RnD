Imports System.IO
Imports System.Net.Mail
Imports EWS.MQR.XML
Imports EWS.Diagnostics

Public Class EmailSender


   Public Shared Sub SendEMailMessage(ByVal recipient As String, _
                        ByVal subject As String, _
                        ByVal body As String, _
                        ByVal highPriority As Boolean)
      SendEMailMessage(recipient, subject, body, highPriority, Nothing)
   End Sub

   Public Shared Sub SendEMailMessage(ByVal recipient As String, _
                           ByVal subject As String, _
                           ByVal body As String, _
                           ByVal highPriority As Boolean, _
                           ByVal Attachments As List(Of FileInfo))

      Try


         ' Construct the email message
         Dim message As New MailMessage()
         AddEmailAddresses(recipient, message)

         With message
            .From = New MailAddress(MQRConfig.Current.MQRServiceConfig.SMTPMailFrom, "MQR")
            .Subject = String.Format("{0} [{1}]", subject, Environment.MachineName)
            .Body = body
            .IsBodyHtml = False
            If highPriority Then
               .Priority = MailPriority.High
            End If

            If Not Attachments Is Nothing Then
               For Each Attachment As FileInfo In Attachments
                  .Attachments.Add(New Attachment(Attachment.FullName))
               Next
            End If

         End With
         ' Send the email message
         Try
            Dim Client As New SmtpClient(MQRConfig.Current.MQRServiceConfig.SMTPServer)
            Client.Send(message)
            Client = Nothing
         Finally
            message.Attachments.Dispose()
            message.Dispose()
            message = Nothing
         End Try

      Catch ex As Exception
         EventLogging.Log("Failed Sending EMail for " & subject & ControlChars.NewLine & ex.ToString, "EmailSender", EventLogEntryType.Warning)
      End Try

   End Sub

   Private Shared Sub AddEmailAddresses(ByVal AddressString As String, ByRef Email As MailMessage)

      ' Split the component addresses up into individual addresses and add to the return collection
      Dim split As String() = AddressString.Split(New String() {";"}, StringSplitOptions.RemoveEmptyEntries)

      For Each address As String In split
         Select Case True
            Case address.ToLower.StartsWith("bcc:")

               Dim Item As New MailAddress(address.Substring(4))
               If Not Email.Bcc.Contains(Item) Then
                  Email.Bcc.Add(Item)
               End If
            Case address.ToLower.StartsWith("cc:")
               Dim Item As New MailAddress(address.Substring(3))
               If Not Email.CC.Contains(Item) Then
                  Email.CC.Add(Item)
               End If
            Case address.ToLower.StartsWith("to:")
               Dim Item As New MailAddress(address.Substring(3))
               If Not Email.To.Contains(Item) Then
                  Email.To.Add(Item)
               End If
            Case Else
               Dim Item As New MailAddress(address)
               If Not Email.To.Contains(Item) Then
                  Email.To.Add(Item)
               End If

         End Select
      Next

   End Sub



End Class
