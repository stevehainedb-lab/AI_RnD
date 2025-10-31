Imports EWS.Network.TCPIP
Public Class SendDataEventArgs : Inherits EventArgs

   Private m_Data As TCPMessage = Nothing
   Private m_ConnectionID As Integer = -1

   Public Sub New(ByVal ConnectionID As Integer, ByVal Data As TCPMessage)
      m_ConnectionID = ConnectionID
      m_Data = Data
   End Sub

   Public Property Data() As TCPMessage
      Get
         Return m_Data
      End Get
      Set(ByVal value As TCPMessage)
         m_Data = value
      End Set
   End Property

   Public ReadOnly Property ConnectionID() As Integer
      Get
         Return m_ConnectionID
      End Get
   End Property

End Class
