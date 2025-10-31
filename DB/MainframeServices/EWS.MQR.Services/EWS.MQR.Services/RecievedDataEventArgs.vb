Friend Class ReceivedDataEventArgs : Inherits EventArgs

   Private m_data As String

   Public Sub New(ByVal Data As String)

      m_data = Data

   End Sub

   Public ReadOnly Property Data() As String
      Get
         Return m_data
      End Get
   End Property

End Class
