Imports System
Imports System.Collections.Specialized

Friend Class IPMatcher
   Private m_Table As New StringCollection
   Private m_Shadow As New StringCollection

   Public Function Add(ByVal range As String) As Boolean
      Try
         If (range = "*") OrElse (range = "*.*.*.*") Then
            range = "0.0.0.0-255.255.255.255"
         End If
         If range.IndexOf("*"c) > 0 Then
            range = range.Replace("*", "0") + "-" + range.Replace("*", "255")
         End If
         If range.IndexOf("-"c) > 0 Then
            Dim t As String() = range.Split("-"c)
            Dim a As Long = IP2LONG(t(0))
            Dim b As Long = IP2LONG(t(1))
            Dim s As String = a.ToString + "-" + b.ToString
            m_Table.Add(s)
            m_Shadow.Add(range)
            Return True
         End If
         Dim val As Long = IP2LONG(range)
         m_Table.Add(val.ToString)
         m_Shadow.Add(range)
         Return True
      Catch
         Return False
      End Try
   End Function

   Public Function Match(ByVal ipaddress As String) As Boolean
      Try
         Dim val As Long = IP2LONG(ipaddress)
         Dim i As Integer
         While i < m_Table.Count
            If m_Table(i) = val.ToString Then
               Return True
            End If
            If m_Table(i).IndexOf("-"c) > 0 Then
               Dim s As String() = m_Table(i).Split("-"c)
               Dim a As Long = Long.Parse(s(0))
               If a <= val Then
                  Dim b As Long = Long.Parse(s(1))
                  If val <= b Then
                     Return True
                  End If
               End If
            End If
            System.Math.Min(System.Threading.Interlocked.Increment(i), i - 1)
         End While
      Catch
      End Try
      Return False
   End Function

   Private Shared Function IP2LONG(ByVal ipaddress As String) As Long
      Dim rv As Long
      Dim t As String() = ipaddress.Split("."c)
      Dim i As Integer
      While i < 4
         Dim x As Integer = Integer.Parse(t(i))
         rv = rv * 256 + x
         System.Math.Min(System.Threading.Interlocked.Increment(i), i - 1)
      End While
      Return rv
   End Function

End Class
