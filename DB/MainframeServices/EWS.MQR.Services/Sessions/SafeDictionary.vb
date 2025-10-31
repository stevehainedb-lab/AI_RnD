Public Class SafeDictionary(Of TKey, TValue) : Implements IDictionary(Of TKey, TValue)

   Private _rwLock As New Threading.ReaderWriterLockSlim
   Private _dict As New Generic.Dictionary(Of TKey, TValue)

   Public Sub New()

   End Sub

   Public Sub New(dict As SafeDictionary(Of TKey, TValue))
      Using New AcquireWriteLock(_rwLock)
         _dict = New Generic.Dictionary(Of TKey, TValue)(dict)
      End Using
   End Sub

   Private Class AcquireWriteLock
      Implements IDisposable
      Private _rwLock As New Threading.ReaderWriterLockSlim
      Public Sub New(ByVal rwLock As Threading.ReaderWriterLockSlim)
         _rwLock = rwLock
         _rwLock.EnterWriteLock()
      End Sub
      Private disposedValue As Boolean = False                ' To detect redundant calls

      ' IDisposable
      Protected Overridable Sub Dispose(ByVal disposing As Boolean)
         If Not Me.disposedValue Then
            If disposing Then
               ' TODO: free other state (managed objects).
               _rwLock.ExitWriteLock()
            End If
            ' TODO: free your own state (unmanaged objects).
            ' TODO: set large fields to null.
         End If
         Me.disposedValue = True
      End Sub
#Region " IDisposable Support "
      ' This code added by Visual Basic to correctly implement the disposable pattern.
      Public Sub Dispose() Implements IDisposable.Dispose
         ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
         Dispose(True)
         GC.SuppressFinalize(Me)
      End Sub
#End Region
   End Class
   Private Class AcquireReadLock
      Implements IDisposable
      Private _rwLock As New Threading.ReaderWriterLockSlim
      Public Sub New(ByVal rwLock As Threading.ReaderWriterLockSlim)
         _rwLock = rwLock
         _rwLock.EnterReadLock()
      End Sub
      Private disposedValue As Boolean = False                ' To detect redundant calls

      ' IDisposable
      Protected Overridable Sub Dispose(ByVal disposing As Boolean)
         If Not Me.disposedValue Then
            If disposing Then
               ' TODO: free other state (managed objects).
               _rwLock.ExitReadLock()
            End If
            ' TODO: free your own state (unmanaged objects).
            ' TODO: set large fields to null.
         End If
         Me.disposedValue = True
      End Sub
#Region " IDisposable Support "
      ' This code added by Visual Basic to correctly implement the disposable pattern.
      Public Sub Dispose() Implements IDisposable.Dispose
         ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
         Dispose(True)
         GC.SuppressFinalize(Me)
      End Sub
#End Region
   End Class
   Public Sub Add(ByVal item As System.Collections.Generic.KeyValuePair(Of TKey, TValue)) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TKey, TValue)).Add
      Using New AcquireWriteLock(_rwLock)
         _dict(item.Key) = item.Value
      End Using
   End Sub
   Public Sub Clear() Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TKey, TValue)).Clear
      Using New AcquireWriteLock(_rwLock)
         _dict.Clear()
      End Using
   End Sub
   Public Function Contains(ByVal item As System.Collections.Generic.KeyValuePair(Of TKey, TValue)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TKey, TValue)).Contains
      Using New AcquireReadLock(_rwLock)
         Return _dict.ContainsKey(item.Key)
      End Using
   End Function

   Public Sub CopyTo(ByVal array() As System.Collections.Generic.KeyValuePair(Of TKey, TValue), ByVal arrayIndex As Integer) Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TKey, TValue)).CopyTo
      Throw New NotImplementedException
   End Sub

   Public ReadOnly Property Count() As Integer Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TKey, TValue)).Count
      Get
         Using New AcquireReadLock(_rwLock)
            Return _dict.Count
         End Using
      End Get
   End Property
   Public ReadOnly Property IsReadOnly() As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TKey, TValue)).IsReadOnly
      Get
         Return False
      End Get
   End Property
   Public Function Remove(ByVal item As System.Collections.Generic.KeyValuePair(Of TKey, TValue)) As Boolean Implements System.Collections.Generic.ICollection(Of System.Collections.Generic.KeyValuePair(Of TKey, TValue)).Remove
      Using New AcquireWriteLock(_rwLock)
         Return _dict.Remove(item.Key)
      End Using
   End Function

   Public Sub Add(ByVal key As TKey, ByVal value As TValue) Implements System.Collections.Generic.IDictionary(Of TKey, TValue).Add
      Using New AcquireWriteLock(_rwLock)
         _dict(key) = value
      End Using
   End Sub

   Public Function ContainsKey(ByVal key As TKey) As Boolean Implements System.Collections.Generic.IDictionary(Of TKey, TValue).ContainsKey
      Using New AcquireReadLock(_rwLock)
         Return _dict.ContainsKey(key)
      End Using
   End Function
   Default Public Property Item(ByVal key As TKey) As TValue Implements System.Collections.Generic.IDictionary(Of TKey, TValue).Item
      Get
         Using New AcquireReadLock(_rwLock)
            Return _dict(key)
         End Using
      End Get
      Set(ByVal value As TValue)
         Using New AcquireWriteLock(_rwLock)
            _dict(key) = value
         End Using
      End Set
   End Property
   Public ReadOnly Property Keys() As System.Collections.Generic.ICollection(Of TKey) Implements System.Collections.Generic.IDictionary(Of TKey, TValue).Keys
      Get
         Using New AcquireReadLock(_rwLock)
            Return _dict.Keys
         End Using
      End Get
   End Property
   Public Function Remove(ByVal key As TKey) As Boolean Implements System.Collections.Generic.IDictionary(Of TKey, TValue).Remove
      Using New AcquireWriteLock(_rwLock)
         Return _dict.Remove(key)
      End Using
   End Function

   Public Function TryGetValue(ByVal key As TKey, ByRef value As TValue) As Boolean Implements System.Collections.Generic.IDictionary(Of TKey, TValue).TryGetValue
      Using New AcquireReadLock(_rwLock)
         Return _dict.TryGetValue(key, value)
      End Using
   End Function

   Public ReadOnly Property Values() As System.Collections.Generic.ICollection(Of TValue) Implements System.Collections.Generic.IDictionary(Of TKey, TValue).Values
      Get
         Using New AcquireReadLock(_rwLock)
            Return _dict.Values
         End Using
      End Get
   End Property
   Public Function GetEnumeratorGeneric() As System.Collections.Generic.IEnumerator(Of System.Collections.Generic.KeyValuePair(Of TKey, TValue)) Implements System.Collections.Generic.IEnumerable(Of System.Collections.Generic.KeyValuePair(Of TKey, TValue)).GetEnumerator
      Using New AcquireReadLock(_rwLock)
         Return _dict.GetEnumerator
      End Using
   End Function
   Public Function GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
      Using New AcquireReadLock(_rwLock)
         Return _dict.GetEnumerator
      End Using
   End Function
End Class

